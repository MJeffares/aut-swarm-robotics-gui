#define DEBUG

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.Cuda;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.ComponentModel;
using System.Reflection;

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
        public static UMat TestImage { get; }
        #endregion

        #region Public Methods
        static ImageProcessing()
        {
            TestImage = new UMat();
            var Image = CvInvoke.Imread("...\\...\\Brae\\Images\\robotcutouts3.png").GetUMat(AccessType.Read);
            CvInvoke.Resize(Image, TestImage, new Size(1920, 1080));
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
        public static void GetRobots(UMat Frame, List<RobotItem> RobotList)
        {
            // Find every contour in the image
            VectorOfVectorOfPoint Contours = new VectorOfVectorOfPoint();// = GetCountours(Frame, 5, RetrType.External);
            GetCountours(Frame, Contours, 5, RetrType.External);
            // Filter out small and large contours
            VectorOfVectorOfPoint ProcessedContours = FilterContourArea(Contours, 0, 1000000);

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
                // Create an image from the frame cropped down to the contour region
                using (UMat RobotImage = new UMat(Frame, RobotBounds))
                {
                    // New contour points relative to cropped region
                    Point[] RelativeContourPoints = ProcessedContour.ToArray();
                    // Get coordinates relative to bounding rectangle
                    for (int j = 0; j < ProcessedContour.Size; j++)
                    {
                        // Subtracting upper-left corner of the bounding box
                        RelativeContourPoints[j].Offset(new Point(-RobotBounds.X, -RobotBounds.Y));
                    }
                    // Check for the colour ID, Returns (-1) if no robot ID
                    int RobotID = IdentifyRobot(new VectorOfPoint(RelativeContourPoints), RobotImage);
                    // Goto next robot if not true
                    if (RobotID == -1) continue;
                    // Robot is tracked by image processing

                    // Look for the robot in the collection
                    Predicate<RobotItem> TrackedID = (RobotItem Robot) => { return Robot.ID == RobotID; };
                    // Look for robot in collection
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
                    MCvPoint2D64f AbsoluteCOM = CvInvoke.Moments(ProcessedContour).GravityCenter;
                    // Store the robots location
                    RobotList[index].Location = new Point((int)AbsoluteCOM.X, (int)AbsoluteCOM.Y);
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
        private static VectorOfVectorOfPoint FilterContourArea(VectorOfVectorOfPoint Contours, double LowerBound, double UpperBound)
        {
            VectorOfVectorOfPoint FilteredContours = new VectorOfVectorOfPoint();
            double Area;
            for (int i = 0; i < Contours.Size; i++)
            {
                VectorOfPoint Contour = Contours[i];
                Area = Math.Abs(CvInvoke.ContourArea(Contour));
                // Remove high/low freq noise contours
                if (Area > LowerBound && Area <= UpperBound)
                {
                    FilteredContours.Push(Contour);
                }
            }
            return FilteredContours;
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
            //LowerH = this.LowerH;
            //UpperH = this.UpperH;
            return HueRange;
        }
        private static bool HasHueRange(IInputArray Frame, Range HueRange)
        {
            int Count;
            const int LowerS = 25;
            const int UpperS = 255;
            const int ColourCount = 1000;
            Mat Out = new Mat();
            Mat HOut = new Mat();

            CvInvoke.CvtColor(Frame, Out, ColorConversion.Bgr2Hsv);
            //CvInvoke.Blur(Out, Out, new Size(1, 1), new Point(0, 0));

            //
            ScalarArray lower = new ScalarArray(HueRange.Start);
            ScalarArray upper = new ScalarArray(HueRange.End);
            //
            CvInvoke.ExtractChannel(Out, HOut, 0);
            CvInvoke.InRange(HOut, lower, upper, HOut);
            //
            CvInvoke.ExtractChannel(Out, Out, 1);
            CvInvoke.Threshold(Out, Out, LowerS, UpperS, ThresholdType.Binary);
            CvInvoke.BitwiseAnd(HOut, Out, HOut);
            //
            Count = CvInvoke.CountNonZero(HOut);

            //
            if (Count > ColourCount)
            {
                return true;
            }
            return false;
        }
        private static Point FindDirection(IInputArray Frame)
        {
            VectorOfVectorOfPoint Contours = new VectorOfVectorOfPoint();
            VectorOfVectorOfPoint ProcessedContours = new VectorOfVectorOfPoint();
            using (Mat Input = new Image<Gray, byte>(Frame.GetInputArray().GetSize()).Mat)
            {
                // Convert to grayscale
                CvInvoke.CvtColor(Frame, Input, ColorConversion.Bgr2Gray);
                // Apply binary threshold
                CvInvoke.AdaptiveThreshold(Input, Input, 255, AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 3, 0);
                // Find contours
                CvInvoke.FindContours(Input, Contours, null, RetrType.Ccomp, ChainApproxMethod.ChainApproxSimple);
            }
            ProcessedContours = FilterContourArea(Contours, 50, 100000);

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
        private static int IdentifyRobot(VectorOfPoint Contour, IInputArray Frame)
        {
            bool IsOrange = false, IsYellow = false, IsGreen = false, IsDarkBlue = false, IsLightBlue = false, IsPurple = false;
            int RobotID = -1;

            using (Mat Out = new Image<Bgr, byte>(Frame.GetInputArray().GetSize()).Mat)
            {
                using (Mat Mask = new Image<Gray, byte>(Frame.GetInputArray().GetSize()).Mat)
                {
                    using (VectorOfVectorOfPoint ContourVect = new VectorOfVectorOfPoint())
                    {
                        ContourVect.Push(Contour);
                        CvInvoke.DrawContours(Mask, ContourVect, 0, new MCvScalar(255, 255, 255), -1);
                    }
                    // This is done to focus colour detection to one robot
                    CvInvoke.Subtract(Frame, Out, Out, Mask);
                }
                // Look for colours on the robot
                IsOrange = HasHueRange(Out, GetHueRange(KnownColor.Orange));
                IsYellow = HasHueRange(Out, GetHueRange(KnownColor.Yellow));
                IsGreen = HasHueRange(Out, GetHueRange(KnownColor.Green));
                IsLightBlue = HasHueRange(Out, GetHueRange(KnownColor.LightBlue));
                IsDarkBlue = HasHueRange(Out, GetHueRange(KnownColor.DarkBlue));
                IsPurple = HasHueRange(Out, GetHueRange(KnownColor.Purple));
            }
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
