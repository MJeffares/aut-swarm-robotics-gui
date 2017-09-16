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
        private enum Shape
        {
            [Description("Triangle")]
            TRIANGLE,
            [Description("Square")]
            SQUARE,
            [Description("Pentagon")]
            PENTAGON,
            [Description("Hexagon")]
            HEXAGON
        };

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

            //const double REAL_DISTANCE = 1664.882954;
            const double REAL_DISTANCE = 297;

            // Four points at arena corners
            var ArenaContour = new VectorOfPoint();
            // Arena corner closest to pixel origin
            var Origin = new Point();
           

            // TEMP: real-world values to display
            double displayFactor = 1024 / REAL_DISTANCE;

            // Pixel to real-world scaling factor
            double factor = IndentifyArena(Frame, ArenaContour);
            if (factor != 0)
                Origin = FindOrigin(Frame, ArenaContour);

            //var Contours = new VectorOfVectorOfPoint();
            var Hexagons = new VectorOfVectorOfPoint();
            using (var Contours = new VectorOfVectorOfPoint())
            {
                // Find every contour in the image
                GetCountours(Frame, Contours, 1, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                // Filter out small and large contours
                FilterContourArea(Contours, Contours, 1000, 100000);
                
                int HexCount = GetHexagons(Contours, Hexagons);
            }
            int RobotCount = 0;

            // Loop through the hexagons in the frame
            for (int i = 0; i < Hexagons.Size; i++)
            {
                VectorOfPoint Hexagon = Hexagons[i];
                var RobotFrame = new UMat();
                GetRobotFrame(Frame, Hexagon, RobotFrame);

                // Check for the colour ID, Returns (-1) if no robot ID
                int RobotID = IdentifyRobot(RobotFrame);
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
                RobotList[index].Contour = Hexagon.ToArray();
                // DEBUG: Robot counter
                RobotCount++;
                // Get the robots center
                MCvPoint2D64f COM = CvInvoke.Moments(Hexagon).GravityCenter;

                // Store the robots pixel location
                RobotList[index].Pixel = new Point((int)COM.X, (int)COM.Y);
                if (factor != 0)
                {
                    // Store the robots real-world location
                    RobotList[index].Location = new System.Windows.Point((COM.X - Origin.X) * factor,
                        (COM.Y - Origin.Y) * factor);
                    // Store the robots display location
                    RobotList[index].DisplayLocation = new System.Windows.Point(RobotList[index].Location.X * displayFactor,
                        RobotList[index].Location.Y * displayFactor);
                }
                else
                {
                    RobotList[index].Location = new System.Windows.Point((int)COM.X, (int)COM.Y);
                    RobotList[index].DisplayLocation = new System.Windows.Point((int)COM.X, (int)COM.Y);
                }

                // Get the robots direction
                Point Direction = FindDirection(RobotFrame);
                // Jump to next contour if true
                if (Direction.IsEmpty) continue;

                if (RobotList[index].Pixel.X > 0 && RobotList[index].Pixel.Y > 0)
                {
                    // Get robot facing using Atan function
                    int dy = Direction.Y - RobotList[index].Pixel.Y;
                    int dx = Direction.X - RobotList[index].Pixel.X;
                    RobotList[index].Facing = Math.Atan2(dy, dx);
                    RobotList[index].FacingDeg = Math.Atan2(dy, dx) * 180 / Math.PI;

                    //int DirectionX = (int)(RobotList[index].Width * Math.Cos(RobotList[index].Heading) * 0.9);
                    //int DirectionY = (int)(RobotList[index].Width * Math.Sin(RobotList[index].Heading) * 0.9);
                    int DirectionX = (int)(RobotList[index].Width * 0.4);
                    int DirectionY = 0;
                    RobotList[index].Direction = new Point(DirectionX, DirectionY);
                }
            }
        }
        #endregion

        #region Private Methods
        private static void GetCountours(IInputArray Frame, IOutputArray Contours, int BlurSize, RetrType Mode, ChainApproxMethod Approx)
        {
            //bool HasCuda = CudaInvoke.HasCuda;
            // BRAE: Don't use Cuda here because it doesnt work correctly
            bool HasCuda = false;

            if (HasCuda)
            {
                // The image arrays
                var Input = new GpuMat(Frame);
                // Convert to grayscale
                CudaInvoke.CvtColor(Input, Input, ColorConversion.Bgr2Gray);
                // Noise removal but keeps edges
                if (BlurSize > 0 && BlurSize % 2 != 0)
                {
                    new CudaGaussianFilter(DepthType.Default, 1, DepthType.Default, 1, new Size(BlurSize, BlurSize), 0).Apply(Input, Input);
                    // More expensive operation therefore only used on Cuda
                    //CudaInvoke.BilateralFilter(Input, Input, BlurSize, 75, 75);
                }
                // Find edges using Canny                     
                new CudaCannyEdgeDetector(0, 255).Detect(Input, Input, null);
                // Find only the external contours applying no shape approximations
                // FindContours has no Cuda counterpart so the GpuMat is converted to a Mat
                CvInvoke.FindContours(Input.ToMat(), Contours, null, Mode, Approx);
                // Dispose of the image arrays
                Input.Dispose();
            }
            else
            {
                // Create an image array
                var Input = new UMat();
                // Convert to grayscale
                CvInvoke.CvtColor(Frame, Input, ColorConversion.Bgr2Gray);
                // Noise removal
                if (BlurSize > 0 && BlurSize % 2 != 0)
                    CvInvoke.GaussianBlur(Input, Input, new Size(BlurSize, BlurSize), 0);
                // Threshold the image to find the edges     
                CvInvoke.Canny(Input, Input, 0, 255);
                // Find only the external contours applying no shape approximations
                CvInvoke.FindContours(Input, Contours, null, Mode, Approx);
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
        private static void GetRobotFrame(IInputArray Frame, IInputArray Contour, IOutputArray RobotFrame)
        {
            var Input = Frame as UMat;
            Input = new UMat(Input, CvInvoke.BoundingRectangle(Contour));
            Input.CopyTo(RobotFrame);
            RobotFrame.GetOutputArray().GetUMat().CopyTo(RobotFrame);
        }
        private static int GetHexagons(IInputArray Contours, IOutputArray Hexagons)
        {
            var Input = Contours as VectorOfVectorOfPoint;
            var Output = Hexagons as VectorOfVectorOfPoint;
            for (int i = 0; i < Input.Size; i++)
            {
                VectorOfPoint Contour = Input[i];
                // Get approximate polygonal shape of contour
                CvInvoke.ApproxPolyDP(Contour, Contour, CvInvoke.ArcLength(Contour, true) * 0.02, true);
                // If contour is not the right shape (hexagon), check next shape
                if (IsShape(Contour, Shape.HEXAGON))
                {
                    Output.Push(Contour);
                }
            }
            Hexagons = Output;
            return Output.Size;
        }
        private static bool IsShape(IInputArray Contour, Shape Shape)
        {
            var contour = Contour as VectorOfPoint;
            const int tolerance = 20;
            int sides = 0;
            switch(Shape)
            {
                case Shape.TRIANGLE:
                    sides = 3;
                    break;
                case Shape.SQUARE:
                    sides = 4;
                    break;
                case Shape.PENTAGON:
                    sides = 5;
                    break;
                case Shape.HEXAGON:
                    sides = 6;
                    break;
            }
            double exterior = 360 / sides;

            // Check for correct number of vertices
            if (contour.Size != sides)
            {
                return false;
            }

            LineSegment2D[] edges = PointCollection.PolyLine(contour.ToArray(), true);
            for (int j = 0; j < edges.Length; j++)
            {
                double angle = Math.Abs(edges[(j + 1) % edges.Length].GetExteriorAngleDegree(edges[j]));
                // Angle is outside the tolerance
                if (angle < exterior - tolerance || angle > exterior + tolerance)
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
                    HueRange.End = 13;
                    break;
                case KnownColor.Yellow:
                    HueRange.Start = 17;
                    HueRange.End = 30;
                    break;
                case KnownColor.Green:
                    HueRange.Start = 40;
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
            int Width = Frame.GetInputArray().GetSize().Width;
            int Height = Frame.GetInputArray().GetSize().Height;
            int ColourCount = Width * Height / 40;
            var Out = new Mat();
            var HOut = new Mat();

            //bool HasCuda = CudaInvoke.HasCuda;
            // BRAE: No more cuda for now
            bool HasCuda = false;

            if (HasCuda)
            {
                var GpuFrame = new GpuMat(Frame);
                CudaInvoke.CvtColor(GpuFrame, GpuFrame, ColorConversion.Bgr2Hsv);
                GpuFrame.Download(Out);
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
            var Input = Frame as UMat;
            var Contours = new VectorOfVectorOfPoint();
            var ProcessedContours = new VectorOfVectorOfPoint();
            double MaxArea = Input.Cols * Input.Rows;
            double MinArea = MaxArea * 0.005;


            GetCountours(Input, Contours, 1, RetrType.List, ChainApproxMethod.ChainApproxSimple);
            FilterContourArea(Contours, ProcessedContours, MinArea, MaxArea);

            MCvPoint2D64f TriangleCOM = new MCvPoint2D64f();

            for (int i = 0; i < ProcessedContours.Size; i++)
            {
                var ProcessedContour = ProcessedContours[i];
                // Get approximate polygonal shape of contour
                CvInvoke.ApproxPolyDP(ProcessedContour, ProcessedContour, CvInvoke.ArcLength(ProcessedContour, true) * 0.1, true);
                // Check if contour is the right shape (triangle)
                if (IsShape(ProcessedContour, Shape.TRIANGLE))
                {
                    TriangleCOM = CvInvoke.Moments(ProcessedContour).GravityCenter;
                    return new Point((int)TriangleCOM.X, (int)TriangleCOM.Y);
                }
            }
            return new Point();
        }
        private static double IndentifyArena(IInputArray Frame, IOutputArray Contour)
        {
            var ArenaContour = Contour as VectorOfPoint;

            double factor = 0;
            //const double REAL_DISTANCE = 1664.882954;
            const double REAL_DISTANCE = 297;

            var Contours = new VectorOfVectorOfPoint();
            var ProcessedContours = new VectorOfVectorOfPoint();
            // Find every contour in the image
            GetCountours(Frame, Contours, 0, RetrType.List, ChainApproxMethod.ChainApproxSimple);
            // Filter out small and large contours
            FilterContourArea(Contours, ProcessedContours, 100000, 1500000);

            // Loop through the filtered contours in the frame
            for (int i = 0; i < ProcessedContours.Size; i++)
            {
                VectorOfPoint ProcessedContour = ProcessedContours[i];
                // Get approximate polygonal shape of contour
                CvInvoke.ApproxPolyDP(ProcessedContour, ProcessedContour, CvInvoke.ArcLength(ProcessedContour, true) * 0.02, true);
                // If contour is not the right shape (square), check next shape
                if (!IsShape(ProcessedContour, Shape.SQUARE)) continue;

                int originIndex = -1;
                int oppositeIndex = -1;
                double pixelDistance = -1;

                for (int index = 0; index < 4; index++)
                {
                    int xy = 0;
                    int x = ProcessedContour[index].X;
                    int y = ProcessedContour[index].Y;

                    if (x + y > xy)
                    {
                        xy = x + y;
                        originIndex = index + 2;
                        oppositeIndex = index;  
                    }
                }

                if (originIndex > 3)
                    originIndex -= 4;

                Point Origin = ProcessedContour[originIndex];
                Point Opposite = ProcessedContour[oppositeIndex];
                LineSegment2D Distance = new LineSegment2D(Origin, Opposite);

                pixelDistance = Distance.Length;

                factor = REAL_DISTANCE / pixelDistance;

                ArenaContour.Push(ProcessedContour);
                break;
                
            }
            // Test square is 210x210mm with area 44100mm^2
            // this area = square area / factor^2
            // for desk2floor setup this should be roughly 140,000
            //var area = CvInvoke.ContourArea(ArenaContour);

            return factor;
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
            //if (IsOrange && IsYellow && IsGreen && !IsLightBlue && !IsDarkBlue && !IsPurple) RobotID = 0;
            if (IsOrange && IsYellow && IsGreen) RobotID = 0;
            // DarkBlue, Yellow, Orange
            //if (IsDarkBlue && IsYellow && IsOrange && !IsLightBlue && !IsGreen && !IsPurple) RobotID = 1;
            if (IsDarkBlue && IsYellow && IsOrange) RobotID = 1;
            // Green, Yellow, DarkBlue
            //if (IsGreen && IsYellow && IsDarkBlue && !IsLightBlue && !IsOrange && !IsPurple) RobotID = 2;
            if (IsGreen && IsYellow && IsDarkBlue) RobotID = 2;
            // Orange, Yellow, Purple
            //if (IsOrange && IsYellow && IsPurple && !IsLightBlue && !IsDarkBlue && !IsGreen) RobotID = 3;
            if (IsOrange && IsYellow && IsPurple) RobotID = 3;
            // LightBlue, Green, DarkBlue
            //if (IsLightBlue && IsGreen && IsDarkBlue && !IsYellow && !IsOrange && !IsPurple) RobotID = 4;
            if (IsLightBlue && IsGreen && IsDarkBlue) RobotID = 4;
            // Orange, Green, Purple
            //if (IsOrange && IsGreen && IsPurple && !IsLightBlue && !IsDarkBlue && !IsYellow) RobotID = 5;
            if (IsOrange && IsGreen && IsPurple) RobotID = 5;

            return RobotID;
        }    
        private static Point FindOrigin(IInputArray Frame, IInputArray Contour)
        {
            var origin = new Point();
            var contour = Contour as VectorOfPoint;

            for (int index = 0; index < 4; index++)
            {
                int xy = 0;
                int x = contour[index].X;
                int y = contour[index].Y;

                if (x + y > xy)
                {
                    xy = x + y;
                    origin = contour[index];
                }
            }
            return origin;
        }
        #endregion
    }
}
