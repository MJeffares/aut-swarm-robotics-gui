#define DEBUG

using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.Util;
using Emgu.CV.Features2D;
using Emgu.CV.XFeatures2D;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.ComponentModel;

namespace SwarmRoboticsGUI
{
    public enum FilterType
    {
        [Description("No Filter")]
        NONE,
        [Description("Greyscale")]
        GREYSCALE,
        [Description("Canny Edges")]
        CANNY_EDGES,
        [Description("Colour Filtering")]
        COLOUR
    };
    public static class ImageProcessing
    {

        #region Public Properties
        
        public static IInputArray TestImage { get; set; } 
        #endregion

        #region Public Methods
        static ImageProcessing()
        {
            try
            {
                var Image = CvInvoke.Imread("...\\...\\Brae\\Images\\robotcutouts3.png").GetUMat(AccessType.Read);
                CvInvoke.Resize(Image, Image, new Size(1920, 1080));
                TestImage = Image.Clone();
            }
            catch(Exception)
            {
                TestImage = new Image<Bgr, byte>(1920, 1080, new Bgr(0, 0, 0)).Mat.GetUMat(AccessType.Read);
            }
        }

        public static void ProcessFilter(IInputArray Input, IOutputArray Output, FilterType Filter)
        {
            switch (Filter)
            {
                case FilterType.NONE:
                    Input.GetInputArray().CopyTo(Output);
                    break;
                case FilterType.GREYSCALE:
                    CvInvoke.CvtColor(Input, Output, ColorConversion.Bgr2Gray);
                    break;
                case FilterType.CANNY_EDGES:
                    using (var Out = new UMat())
                    {
                        CvInvoke.CvtColor(Input, Out, ColorConversion.Bgr2Gray);
                        CvInvoke.PyrDown(Out, Out);
                        CvInvoke.PyrUp(Out, Out);
                        CvInvoke.Canny(Out, Output, 80, 40);
                    }
                    break;
                case FilterType.COLOUR:
                    using (var Out = new Mat())
                    using (var HOut = new Mat())
                    using (ScalarArray lower = new ScalarArray(0))
                    using (ScalarArray upper = new ScalarArray(150))
                    {
                        //
                        CvInvoke.CvtColor(Input, Out, ColorConversion.Bgr2Hsv);
                        //
                        CvInvoke.ExtractChannel(Out, HOut, 0);
                        CvInvoke.InRange(HOut, lower, upper, HOut);
                        //
                        CvInvoke.ExtractChannel(Out, Out, 1);
                        CvInvoke.Threshold(Out, Out, 0, 25, ThresholdType.Binary);
                        CvInvoke.BitwiseAnd(HOut, Out, Output);
                    }
                    break;
                default:
                    break;
            }
        }
        public static void GetRobots(IInputArray Frame, List<RobotItem> RobotList)
        {
            var Contours = new VectorOfVectorOfPoint();
            var ProcessedContours = new VectorOfVectorOfPoint();
            // Find every contour in the image
            GetCountours(Frame, Contours, 5, RetrType.External);
            // Filter out small and large contours
            FilterContourArea(Contours, ProcessedContours, 0, 1000000);

            // DEBUG: Counters
            int HexCount = 0, RobotCount = 0;
            // Loop through the filtered contours in the frame
            for (int i = 0; i < ProcessedContours.Size; i++)
            {
                VectorOfPoint ProcessedContour = ProcessedContours[i];
                // Get approximate polygonal shape of contour
                CvInvoke.ApproxPolyDP(ProcessedContour, ProcessedContour, 5.0, true);
                // If contour is not the right shape (hexagon), check next shape
                if (!IsHexagon(ProcessedContour)) continue;
                // DEBUG: Hexagon counter
                HexCount++;
                // Rectangular region that encompasses the contour
                Rectangle RobotBounds = CvInvoke.BoundingRectangle(ProcessedContour);
                var RobotImage = new UMat();
                GetRobotRegion(Frame, RobotBounds, RobotImage);

                // Check for the colour ID, Returns (-1) if no robot ID
                int RobotID = IdentifyRobot(RobotImage);
                // Goto next robot if not true
                if (RobotID == -1) continue;
                // Robot is tracked by image processing

                // Look for the robot in the collection
                Predicate<RobotItem> TrackedID = (RobotItem Robot) => { return Robot.ID == RobotID; };
                // Look for robot index in collection
                int index = RobotList.FindIndex(TrackedID);
                // If the index is negative, create a new robot
                if (index < 0)
                {
                    RobotItem Robot = new RobotItem("Robot " + RobotID.ToString(), RobotID);
                    RobotList.Add(Robot);
                    index = RobotList.IndexOf(Robot);
                }
                RobotList[index].IsTracked = true;
                // DEBUG: Store the vertices of the hexagonal shape
                RobotList[index].Contour = ProcessedContour.ToArray();
                // DEBUG: Robot counter
                RobotCount++;
                // Get the robots center
                MCvPoint2D64f COM = CvInvoke.Moments(ProcessedContour).GravityCenter;
                // Store the robots location
                RobotList[index].Location = new Point((int)COM.X, (int)COM.Y);
                // Get the robots direction
                Point Direction = FindDirection(RobotImage);
                // Goto next robot if true
                if (Direction == null) continue;

                Direction = new Point(Direction.X + RobotBounds.X, Direction.Y + RobotBounds.Y);
                if (RobotList[index].Location.X > 0 && RobotList[index].Location.Y > 0)
                {
                    // Get robot heading using Atan function
                    int dy = Direction.Y - RobotList[index].Location.Y;
                    int dx = Direction.X - RobotList[index].Location.X;
                    RobotList[index].Heading = Math.Atan2(dy, dx);
                    RobotList[index].HeadingDeg = Math.Atan2(dy, dx) * 180 / Math.PI;
                    RobotList[index].Direction = new Point((int)(60 * Math.Cos(RobotList[index].Heading)),
                                                           (int)(60 * Math.Sin(RobotList[index].Heading)));
                }
            }
        }
        #endregion

        #region Private Methods
        private static void GetCountours(IInputArray Frame, IOutputArray Contours, int BlurSize, RetrType Mode)
        {
            if (CudaInvoke.HasCuda)
            {
                // The image arrays
                var Input = new GpuMat(Frame);
                var Canny = new GpuMat();
                // Convert to grayscale
                CudaInvoke.CvtColor(Input, Input, ColorConversion.Bgr2Gray);
                // Noise removal but keeps edges
                // More expensive operation therefore only used on Cuda
                CudaInvoke.BilateralFilter(Input, Input, 9, 75, 75);
                // Invert image
                CudaInvoke.BitwiseNot(Input, Input);    
                // Find edges using Canny                     
                new CudaCannyEdgeDetector(0, 255).Detect(Input, Canny, null);
                // Find only the external contours applying no shape approximations
                // FindContours has no Cuda counterpart so the GpuMat is converted to a Mat
                CvInvoke.FindContours(Canny.ToMat(), Contours, null, Mode, ChainApproxMethod.ChainApproxNone);
                // Dispose of the image arrays
                Input.Dispose();
                Canny.Dispose();
            }
            else
            {
                // Create an image array
                var Input = new UMat();
                // Convert to grayscale
                CvInvoke.CvtColor(Frame, Input, ColorConversion.Bgr2Gray);
                // Noise removal
                CvInvoke.GaussianBlur(Input, Input, new Size(BlurSize, BlurSize), 0);
                // Invert image
                CvInvoke.BitwiseNot(Input, Input);
                // Threshold the image to find the edges     
                CvInvoke.AdaptiveThreshold(Input, Input, 255, AdaptiveThresholdType.MeanC, ThresholdType.Binary, 3, 0);
                // Find only the external contours applying no shape approximations
                CvInvoke.FindContours(Input, Contours, null, Mode, ChainApproxMethod.ChainApproxNone);
                // Dispose of the image array
                Input.Dispose();
            }
        }
        private static void FilterContourArea(IInputArray Contours, IOutputArray FilteredContours, double LowerBound, double UpperBound)
        {
            var FilteredC = FilteredContours as VectorOfVectorOfPoint;
            var C = (Contours as VectorOfVectorOfPoint).ToArrayOfArray();

            double Area;
            foreach (Point[] Contour in C)
            {
                Area = CvInvoke.ContourArea(new VectorOfPoint(Contour));
                // Remove high/low freq noise contours
                if (Area > LowerBound && Area <= UpperBound)
                {
                    FilteredC.Push(new VectorOfPoint(Contour));
                }
            }
        }
        private static void GetRobotRegion(IInputArray Frame, Rectangle Region, IOutputArray Result)
        {
            if (CudaInvoke.HasCuda)
            {
                var Input = new GpuMat(Frame);
                // Create an image from the frame cropped down to the contour region
                Input = Input.ColRange(Region.Left, Region.Right)
                           .RowRange(Region.Top, Region.Bottom);
                Input.CopyTo(Result);
                Input.Dispose();
            }
            else
            {
                var Input = new UMat(Frame as UMat, Region);
                Input.CopyTo(Result);
                Input.Dispose();
            }
            
        }
        
        // BRAE: Make these IsShape(Contour,Shape.Hexagon)?
        private static bool IsHexagon(VectorOfPoint Contour)
        {
            // Check for 6 vertices
            if (Contour.Size != 6)
            {
                return false;
            }
            LineSegment2D[] edges = PointCollection.PolyLine(Contour.ToArray(), true);
            for (int j = 0; j < edges.Length; j++)
            {
                double angle = Math.Abs(edges[(j + 1) % edges.Length].GetExteriorAngleDegree(edges[j]));
                // Target Angle of 60 degrees
                if (angle < 30 || angle > 90)
                {
                    return false;
                }
            }
            return true;
        }
        private static bool IsEquilTriangle(VectorOfPoint Contour)
        {
            if (Contour.Size != 3)
            {
                return false;
            }
            LineSegment2D[] edges = PointCollection.PolyLine(Contour.ToArray(), true);
            for (int j = 0; j < edges.Length; j++)
            {
                double angle = Math.Abs(edges[(j + 1) % edges.Length].GetExteriorAngleDegree(edges[j]));
                // Target angle of 120 degrees
                if (angle < 100 || angle > 140)
                {
                    return false;
                }
            }
            return true;
        }

        private static Range GetHueRange(KnownColor TargetColour)
        {
            Range HueRange = new Range();
            switch (TargetColour)
            {
                case KnownColor.Orange:
                    HueRange.Start = 0;
                    HueRange.End = 15;
                    break;
                case KnownColor.Yellow:
                    HueRange.Start = 15;
                    HueRange.End = 30;
                    break;
                case KnownColor.Green:
                    HueRange.Start = 30;
                    HueRange.End = 90;
                    break;
                case KnownColor.LightBlue:
                    HueRange.Start = 90;
                    HueRange.End = 105;
                    break;
                case KnownColor.DarkBlue:
                    HueRange.Start = 105;
                    HueRange.End = 125;
                    break;
                case KnownColor.Purple:
                    HueRange.Start = 125;
                    HueRange.End = 175;
                    break;
                default:
                    HueRange.Start = 0;
                    HueRange.End = 0;
                    break;
            }
            //LowerH = TargetColour.GetHue() * 255 / 360 - 2;
            //UpperH = TargetColour.GetHue() * 255 / 360 + 2;
            return HueRange;
        }
        private static bool HasHueRange(IInputArray Frame, Range HueRange)
        {
            int Count = 0;
            const int LowerS = 25;
            const int ColourCount = 1000;
            var Out = new Mat();
            var HOut = new Mat();


            if (CudaInvoke.HasCuda)
            {
                var GpuFrame = new GpuMat(Frame);
                CudaInvoke.CvtColor(GpuFrame, GpuFrame, ColorConversion.Bgr2Hsv);
                Out = GpuFrame.ToMat();
                GpuFrame.Dispose();
            }
            else
            {
                CvInvoke.CvtColor(Frame, Out, ColorConversion.Bgr2Hsv);
            }
            //CvInvoke.Blur(Out, Out, new Size(1, 1), new Point(0, 0));
            //
            ScalarArray lower = new ScalarArray(HueRange.Start);
            ScalarArray upper = new ScalarArray(HueRange.End);
            //
            CvInvoke.ExtractChannel(Out, HOut, 0);
            CvInvoke.InRange(HOut, lower, upper, HOut);
            //
            CvInvoke.ExtractChannel(Out, Out, 1);
            CvInvoke.Threshold(Out, Out, LowerS, 255, ThresholdType.Binary);
            CvInvoke.BitwiseAnd(HOut, Out, HOut);
            //
            Count = CvInvoke.CountNonZero(HOut);

            lower.Dispose();
            upper.Dispose();
            Out.Dispose();
            HOut.Dispose();           
            //
            if (Count > ColourCount)
            {
                return true;
            }
            return false;
        }
        private static Point FindDirection(IInputArray Frame)
        {
            var Contours = new VectorOfVectorOfPoint();
            var ProcessedContours = new VectorOfVectorOfPoint();

            GetCountours(Frame, Contours, 1, RetrType.Ccomp);
            FilterContourArea(Contours, ProcessedContours, 50, 100000);

            MCvPoint2D64f TriangleCOM = new MCvPoint2D64f();
            for (int i = 0; i < ProcessedContours.Size; i++)
            {
                // Get approximate polygonal shape of contour
                CvInvoke.ApproxPolyDP(ProcessedContours[i], ProcessedContours[i], 5.0, true);
                // Check if contour is the right shape (triangle)
                if (!IsEquilTriangle(ProcessedContours[i])) continue;
                TriangleCOM = CvInvoke.Moments(ProcessedContours[i]).GravityCenter;
            }
            return new Point((int)TriangleCOM.X, (int)TriangleCOM.Y);
        }
        private static int IdentifyRobot(IInputArray Frame)
        {
            bool IsOrange = false, IsYellow = false, IsGreen = false, IsDarkBlue = false, IsLightBlue = false, IsPurple = false;
            int RobotID = -1;

            // Look for colours on the robot
            IsOrange = HasHueRange(Frame, GetHueRange(KnownColor.Orange));
            IsYellow = HasHueRange(Frame, GetHueRange(KnownColor.Yellow));
            IsGreen = HasHueRange(Frame, GetHueRange(KnownColor.Green));
            IsLightBlue = HasHueRange(Frame, GetHueRange(KnownColor.LightBlue));
            IsDarkBlue = HasHueRange(Frame, GetHueRange(KnownColor.DarkBlue));
            IsPurple = HasHueRange(Frame, GetHueRange(KnownColor.Purple));

            // Orange, Yellow, Green
            if (IsOrange && IsYellow && IsGreen && !IsLightBlue && !IsDarkBlue && !IsPurple) RobotID = 0;
            // DarkBlue, Yellow, Orange
            if (IsDarkBlue && IsYellow && IsOrange && !IsLightBlue && !IsGreen && !IsPurple) RobotID = 1;
            // Green, Yellow, DarkBlue
            if (IsGreen && IsYellow && IsDarkBlue && !IsLightBlue && !IsOrange && !IsPurple) RobotID = 2;
            // Orange, Yellow, Purple
            if (IsOrange && IsYellow && IsPurple && !IsLightBlue && !IsDarkBlue && !IsGreen) RobotID = 3;
            // LightBlue, Green, DarkBlue
            if (IsLightBlue && IsGreen && IsDarkBlue && !IsYellow && !IsOrange && !IsPurple) RobotID = 4;
            // Orange, Green, Purple
            if (IsOrange && IsGreen && IsPurple && !IsLightBlue && !IsDarkBlue && !IsYellow) RobotID = 5;

            return RobotID;
        }    
        #endregion
    }
}
