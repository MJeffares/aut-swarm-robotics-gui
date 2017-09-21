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

        public static void ProcessFilter(IInputArray Input, IOutputArray Output, FilterType Filter, int HueLower = 0, int HueUpper = 255)
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
                    using (var Out = new UMat(Input.GetInputArray().GetSize(), Input.GetInputArray().GetDepth(), Input.GetInputArray().GetChannels()))
                    {
                        var In = new UMat();
                        var Contours = new VectorOfVectorOfPoint();
                        var ProcessedContours = new VectorOfVectorOfPoint();

                        // Convert to grayscale
                        CvInvoke.CvtColor(Input, In, ColorConversion.Bgr2Gray);
                        // Find every contour in the image
                        //CvInvoke.Canny(In, In, 0, 255);
                        CvInvoke.AdaptiveThreshold(In, In, 255, AdaptiveThresholdType.MeanC, ThresholdType.Binary, 3, 0);
                        // Find only the external contours applying no shape approximations
                        CvInvoke.FindContours(In, Contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                        // Filter out small and large contours
                        FilterContourArea(Contours, ProcessedContours, 1000000, 2000000);

                        CvInvoke.DrawContours(Out, ProcessedContours, -1, new MCvScalar(255, 255, 255), -1);
                        Out.CopyTo(Output);
                        Output.GetOutputArray().GetUMat().CopyTo(Output);
                    }
                    break;
                case FilterType.COLOUR:
                    using (var Out = new Mat())
                    using (var HOut = new Mat())
                    using (var SOut = new Mat())
                    using (ScalarArray lower = new ScalarArray(HueLower))
                    using (ScalarArray upper = new ScalarArray(HueUpper))
                    {
                        //
                        CvInvoke.CvtColor(Input, Out, ColorConversion.Bgr2Hsv);
                        
                        //
                        CvInvoke.ExtractChannel(Out, HOut, 0);
                        CvInvoke.InRange(HOut, lower, upper, HOut);
                        //
                        CvInvoke.ExtractChannel(Out, SOut, 1);
                        CvInvoke.Threshold(SOut, SOut, 40, 230, ThresholdType.Binary);
                        CvInvoke.BitwiseAnd(SOut, HOut, SOut);
                        //
                        CvInvoke.ExtractChannel(Out, Out, 2);
                        CvInvoke.Threshold(Out, Out, 60, 195, ThresholdType.Binary);
                        CvInvoke.BitwiseAnd(SOut, Out, Output);
                    }
                    break;
                default:
                    break;
            }
        }
        public static void GetRobots(IInputArray Frame, List<RobotItem> RobotList, Arena Arena)
        {
            //const double REAL_DISTANCE = 1664.882954;
            //const double REAL_DISTANCE = 840;
            //const double REAL_DISTANCE = 297;

            var Hexagons = new VectorOfVectorOfPoint();

            if (Arena.Contour == null) return;

            var Bounds = CvInvoke.BoundingRectangle(new VectorOfPoint(Arena.Contour));
            var Input = new UMat(Frame as UMat, Bounds);

            using (var Contours = new VectorOfVectorOfPoint())
            using (var FilteredContours = new VectorOfVectorOfPoint())
            {
                // Find every contour in the image
                GetCountours(Input, Contours, 1, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                // Filter out small and large contours
                FilterContourArea(Contours, FilteredContours, 1000, 5000);

                int HexCount = GetHexagons(FilteredContours, Hexagons);

                /*
                 * //used to check sizes
                for (int i = 0; i < Hexagons.Size; i++)
                {
                    var size = CvInvoke.ContourArea(Hexagons[i]);
                }
                 * */
            }
            int RobotCount = 0;

            CvInvoke.Imwrite("zbefore.png", Input);
            

            CvInvoke.CvtColor(Input, Input, ColorConversion.Bgr2Yuv);
            VectorOfUMat Channels = new VectorOfUMat();
            CvInvoke.Split(Input, Channels);
            CvInvoke.EqualizeHist(Channels[0], Channels[0]);
            CvInvoke.Merge(Channels, Input);
            CvInvoke.CvtColor(Input, Input, ColorConversion.Yuv2Bgr);

            CvInvoke.Imwrite("zafter.png", Input);

            // Loop through the hexagons in the frame
            for (int i = 0; i < Hexagons.Size; i++)
            {
                VectorOfPoint Hexagon = Hexagons[i];
                var RobotFrame = new UMat();
                var RobotFrameOffset = GetRobotFrame(Input, Hexagon, RobotFrame);


                //CvInvoke.Imwrite("frame.png", RobotFrame);

                // Check for the colour ID, Returns (-1) if no robot ID
                int RobotID = IdentifyRobot(RobotFrame, Hexagon);
                // Goto next contour if not true
                if (RobotID == -1) continue;

                // Look for the robot in the collection
                Predicate<RobotItem> TrackedID = (RobotItem Robot) => { return Robot.ID == RobotID; };
                // Look for robot index in collection
                int index = RobotList.FindIndex(TrackedID);
                // If the index is negative, then no matching robot exists. Jump to next contour
                if (index < 0) continue;
                // DEBUG: Robot counter
                RobotCount++;

                RobotList[index].IsTracked = true;
                // DEBUG: Store the vertices of the hexagonal shape
                RobotList[index].Contour = Hexagon.ToArray();

                // Get the robots center
                MCvPoint2D64f COM = CvInvoke.Moments(Hexagon).GravityCenter;

                // Store the robots pixel location
                RobotList[index].PixelLocation = new Point((int)COM.X, (int)COM.Y);

                // Use the arena's size and location in frame to scale robots location to real world
                if (Arena.ScaleFactor != 0 && !Arena.Origin.IsEmpty)
                {
                    // BRAE: Calculate robot width dynamically
                    // Get the robot width using the arena scale factor
                    //RobotList[index].Width += (int)(GetRobotWidth(Hexagon) * Arena.ScaleFactor);
                    // Take the average of the new and previous width value
                    //RobotList[index].Width /= 2;
                    // Calulate height using sqrt(3)*radius
                    //RobotList[index].Height = (int)(RobotList[index].Width / 2 * Math.Sqrt(3));

                    // Store the robots real-world location
                    RobotList[index].Location = new System.Windows.Point((COM.X - Arena.Origin.X) * Arena.ScaleFactor,
                        (COM.Y - Arena.Origin.Y) * Arena.ScaleFactor);
                }

                // Get the robots facing
                var RobotFrameLocation = new Point((int)COM.X - RobotFrameOffset.X, (int)COM.Y - RobotFrameOffset.Y);
                double Facing = FindFacing(RobotFrame, RobotFrameLocation);
                if (Facing == 0) continue;

                if (RobotList[index].PixelLocation.X > 0 && RobotList[index].PixelLocation.Y > 0)
                {
                    RobotList[index].Facing = Facing;
                    RobotList[index].FacingDeg = Facing * 180 / Math.PI;

                    int DirectionX = (int)(RobotList[index].Width * 0.4);
                    RobotList[index].FacingMarker = DirectionX;
                }
            }
        }
        public static void GetArena(IInputArray Frame, Arena Arena)
        {
            var Input = (Frame as UMat).Clone();
            var ArenaContour = new VectorOfPoint();

            double factor = 0;
            Point Origin = new Point();
            //const double REAL_DISTANCE = 1664.882954;
            //const double REAL_DISTANCE = 297;

            var Contours = new VectorOfVectorOfPoint();
            var ProcessedContours = new VectorOfVectorOfPoint();

            // Find every contour in the image
            CvInvoke.CvtColor(Frame, Input, ColorConversion.Bgr2Gray);
            // Noise removal
            // Threshold the image to find the edges  
            //CvInvoke.Canny(Input, Input, 0, 255);
            CvInvoke.AdaptiveThreshold(Input, Input, 255, AdaptiveThresholdType.MeanC, ThresholdType.Binary, 21, 0);

            CvInvoke.GaussianBlur(Input, Input, new Size(3, 3), 0);
            // Find only the external contours applying no shape approximations
            CvInvoke.FindContours(Input, Contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);

            //GetCountours(Frame, Contours, 0, RetrType.List, ChainApproxMethod.ChainApproxNone);
            // Filter out small and large contours
            FilterContourArea(Contours, ProcessedContours, 100000, 2000000);

            // Loop through the filtered contours in the frame
            for (int i = 0; i < ProcessedContours.Size; i++)
            {
                VectorOfPoint ProcessedContour = ProcessedContours[i];
                // Get approximate polygonal shape of contour
                CvInvoke.ApproxPolyDP(ProcessedContour, ProcessedContour, CvInvoke.ArcLength(ProcessedContour, true) * 0.06, true);

                // If contour is not the right shape (square), check next shape
                if (!IsShape(ProcessedContour, Shape.SQUARE)) continue;

                factor = GetScaleFactor(Frame, ProcessedContour);

                ArenaContour.Push(ProcessedContour);
                break;

            }
            // Test square is 210x210mm with area 44100mm^2
            // this area = square area / factor^2
            // for desk2floor setup this should roughly be 140,000
            //var area = CvInvoke.ContourArea(ArenaContour);

            // If the arena was identified and a distance factor was calculated, find the origin point
            if (factor != 0)
            {
                Origin = FindOrigin(Frame, ArenaContour);                
                if (!Origin.IsEmpty)
                {
                    var Bounds = CvInvoke.BoundingRectangle(ArenaContour);
                    
                    Arena.ScaleFactor = factor;
                    Arena.Origin = new Point(Origin.X - Bounds.X, Origin.Y - Bounds.Y);
                    Arena.Contour = ArenaContour.ToArray();
                }
            }
        }
        #endregion

        #region Private Methods
        private static void GetCountours(IInputArray Frame, IOutputArray Contours, int BlurSize, RetrType Mode, ChainApproxMethod Approx)
        {
            //bool HasCuda = CudaInvoke.HasCuda;
            // BRAE: Don't use Cuda here
            bool HasCuda = false;

            if (HasCuda)
            {
                // The image arrays
                var Input = new GpuMat(Frame as UMat);
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
                //CvInvoke.Canny(Input, Input, 0, 255);
                CvInvoke.AdaptiveThreshold(Input, Input, 255, AdaptiveThresholdType.MeanC, ThresholdType.Binary, 21, 0);
                // Find only the external contours applying no shape approximations
                CvInvoke.FindContours(Input, Contours, null, Mode, Approx);
                // Dispose of the image arrays
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
        private static Point GetRobotFrame(IInputArray Frame, IInputArray Contour, IOutputArray RobotFrame)
        {
            var Input = Frame as UMat;
            var Bounds = CvInvoke.BoundingRectangle(Contour);
            Input = new UMat(Input, Bounds);
            Input.CopyTo(RobotFrame);
            RobotFrame.GetOutputArray().GetUMat().CopyTo(RobotFrame);

            return Bounds.Location;
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
                    //HueRange.Start = 5;
                    //HueRange.End = 10;
                    HueRange.Start = 8;
                    HueRange.End = 19;
                    break;
                case KnownColor.Yellow:
                    //HueRange.Start = 20;
                    //HueRange.End = 30;
                    HueRange.Start = 28;
                    HueRange.End = 32;
                    break;
                case KnownColor.Green:
                    //HueRange.Start = 30;
                    //HueRange.End = 100;
                    HueRange.Start = 29;
                    HueRange.End = 62;
                    break;
                case KnownColor.LightBlue:
                    //HueRange.Start = 100;
                    //HueRange.End = 110;
                    HueRange.Start = 98;
                    HueRange.End = 109;
                    break;
                case KnownColor.DarkBlue:
                    //HueRange.Start = 110;
                    //HueRange.End = 130;
                    HueRange.Start = 109;
                    HueRange.End = 125;
                    break;
                case KnownColor.Red:
                    //HueRange.Start = 150;
                    //HueRange.End = 190;
                    HueRange.Start = 160;
                    HueRange.End = 180;
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
            //Range SaturationRange = new Range(25, 230);
            //Range ValueRange = new Range(60, 195);
            int Width = Frame.GetInputArray().GetSize().Width;
            int Height = Frame.GetInputArray().GetSize().Height;
            int ColourCount = Width * Height / 20;

            var HOut = new Mat();

            //CvInvoke.Blur(Out, Out, new Size(1, 1), new Point(0, 0));
            //
            ScalarArray lower = new ScalarArray(HueRange.Start);
            ScalarArray upper = new ScalarArray(HueRange.End);
            //
            CvInvoke.ExtractChannel(Frame, HOut, 0);
            CvInvoke.InRange(HOut, lower, upper, HOut);

            Count = CvInvoke.CountNonZero(HOut);

            lower.Dispose();
            upper.Dispose();
            HOut.Dispose();           
            //
            if (Count > ColourCount)
            {
                return true;
            }
            return false;
        }
        private static double FindFacing(IInputArray Frame, Point Centre)
        {
            var Input = Frame as UMat;
            var Contours = new VectorOfVectorOfPoint();
            var ProcessedContours = new VectorOfVectorOfPoint();
            double MaxArea = Input.Cols * Input.Rows;
            double MinArea = MaxArea * 0.005;

            GetCountours(Input, Contours, 0, RetrType.List, ChainApproxMethod.ChainApproxSimple);
            FilterContourArea(Contours, ProcessedContours, MinArea, MaxArea);

            MCvPoint2D64f TriangleCOM = new MCvPoint2D64f();

            for (int i = 0; i < ProcessedContours.Size; i++)
            {
                var ProcessedContour = ProcessedContours[i];
                // Get approximate polygonal shape of contour
                CvInvoke.ApproxPolyDP(ProcessedContour, ProcessedContour, CvInvoke.ArcLength(ProcessedContour, true) * 0.075, true);
                // Check if contour is the right shape (triangle)
                if (IsShape(ProcessedContour, Shape.TRIANGLE))
                {
                    TriangleCOM = CvInvoke.Moments(ProcessedContour).GravityCenter;
                    // Get robot facing using Atan function
                    double dy = TriangleCOM.Y - Centre.Y;
                    double dx = TriangleCOM.X - Centre.X;
                    double Facing = Math.Atan2(dy, dx);
                    return Facing;
                }
            }
            return 0;
        }
        private static int IdentifyRobot(IInputArray Frame, IInputArray Contour)
        {
            bool IsOrange = false, IsYellow = false, IsGreen = false, IsDarkBlue = false, IsLightBlue = false, IsRed = false;
            int RobotID = -1;
            var hexagon = Contour as VectorOfPoint;
            var Image = Frame as UMat;

            var Mask = new UMat();
            CvInvoke.DrawContours(Mask, new VectorOfVectorOfPoint(hexagon), -1, new MCvScalar(255, 255, 255), -1);

            var Masked = new UMat();
            Image.CopyTo(Masked, Mask);

            //RangeF histrange = new RangeF(0, 256);
            //var hist = new DenseHistogram(256, histrange);
            //CvInvoke.CalcHist(Masked, 0, Mask, hist, 256, histrange, false);
                
            /*
            var Equalised = new UMat();
            CvInvoke.CvtColor(Masked, Equalised, ColorConversion.Bgr2Gray);
            CvInvoke.EqualizeHist(Equalised, Equalised);
            CvInvoke.CvtColor(Equalised, Equalised, ColorConversion.Gray2Bgr);
            CvInvoke.Imwrite("zbefore.png", Masked);
            CvInvoke.Imwrite("zafter.png", Equalised);
             * */

            Range SaturationRange = new Range(15, 240);
            Range ValueRange = new Range(40, 200);
            var SOut = new Mat();
            var VOut = new Mat();
            //
            CvInvoke.CvtColor(Masked, Masked, ColorConversion.Bgr2Hsv);
            CvInvoke.ExtractChannel(Masked, SOut, 1);
            CvInvoke.ExtractChannel(Masked, VOut, 2);
            CvInvoke.Threshold(SOut, SOut, SaturationRange.Start, SaturationRange.End, ThresholdType.Binary);
            CvInvoke.Threshold(VOut, VOut, ValueRange.Start, ValueRange.End, ThresholdType.Binary);
            //CvInvoke.AdaptiveThreshold(SOut, SOut, 254, AdaptiveThresholdType.MeanC, ThresholdType.Binary, 21, 0);
            //CvInvoke.AdaptiveThreshold(VOut, VOut, 254, AdaptiveThresholdType.MeanC, ThresholdType.Binary, 21, 0);

            CvInvoke.BitwiseAnd(SOut, VOut, SOut);

            Masked.CopyTo(Image, SOut);

            // Look for colours on the robot
            IsOrange = HasHueRange(Image, GetHueRange(KnownColor.Orange));
            IsYellow = HasHueRange(Image, GetHueRange(KnownColor.Yellow));
            IsGreen = HasHueRange(Image, GetHueRange(KnownColor.Green));
            IsLightBlue = HasHueRange(Image, GetHueRange(KnownColor.LightBlue));
            IsDarkBlue = HasHueRange(Image, GetHueRange(KnownColor.DarkBlue));
            IsRed = HasHueRange(Image, GetHueRange(KnownColor.Red));

            // LIGHTBLUE GREEN RED
            if (!IsOrange && !IsYellow && IsGreen && IsLightBlue && !IsDarkBlue && IsRed) RobotID = 0;       // RED
            // ORANGE LIGHTBLUE RED
            else if (IsOrange && !IsYellow && !IsGreen && IsLightBlue && !IsDarkBlue && IsRed) RobotID = 1;  // YELLOW
            // ORANGE GREEN RED
            else if (IsOrange && !IsYellow && IsGreen && !IsLightBlue && !IsDarkBlue && IsRed) RobotID = 2;  // PURPLE
            // GREEN YELLOW DARKBLUE
            else if (!IsOrange && IsYellow && IsGreen && !IsLightBlue && IsDarkBlue && !IsRed) RobotID = 3;  // LIGHTBLUE
            // LIGHTBLUE GREEN DARKBLUE
            else if (!IsOrange && !IsYellow && IsGreen && IsLightBlue && IsDarkBlue && !IsRed) RobotID = 4;  // DARKBLUE
            // ORANGE YELLOW GREEN
            else if (IsOrange && IsYellow && IsGreen && !IsLightBlue && !IsDarkBlue && !IsRed) RobotID = 5;  // BROWN
            // LIGHTBLUE YELLOW ORANGE
            else if (IsOrange && IsYellow && !IsGreen && IsLightBlue && !IsDarkBlue && !IsRed) RobotID = 6;  // PINK
            // ORANGE YELLOW RED
            else if (IsOrange && IsYellow && !IsGreen && !IsLightBlue && !IsDarkBlue && IsRed) RobotID = 7;  // ORANGE

            return RobotID;
        }    
        private static Point FindOrigin(IInputArray Frame, IInputArray Contour)
        {
            var contour = Contour as VectorOfPoint;

            int originIndex = -1;
            var size = Frame.GetInputArray().GetSize();           
            int xy = size.Width + size.Height;
            int x = size.Width, y = size.Height;

            for (int index = 0; index < 4; index++)
            {
                
                x = contour[index].X;
                y = contour[index].Y;

                if (x + y < xy)
                {
                    xy = x + y;
                    originIndex = index;
                }
            }
            return contour[originIndex];
        }
        private static double GetScaleFactor(IInputArray Frame, IInputArray Contour)
        {
            var contour = Contour as VectorOfPoint;

            int originIndex = -1;
            int oppositeIndex = -1;
            double pixelDistance = -1;

            double factor = 0;
            const double REAL_DISTANCE = 1664.882954;
            //const double REAL_DISTANCE = 840;
            //const double REAL_DISTANCE = 297;

            var size = Frame.GetInputArray().GetSize();
            int xy = size.Width + size.Height;
            int x = size.Width, y = size.Height;

            for (int index = 0; index < 4; index++)
            {
                x = contour[index].X;
                y = contour[index].Y;

                if (x + y < xy)
                {
                    xy = x + y;
                    originIndex = index;
                    oppositeIndex = index + 2;
                }
            }

            if (oppositeIndex > 3)
                oppositeIndex -= 4;

            Point Origin = contour[originIndex];
            Point Opposite = contour[oppositeIndex];
            LineSegment2D Distance = new LineSegment2D(Origin, Opposite);

            pixelDistance = Distance.Length;

            factor = REAL_DISTANCE / pixelDistance;

            return factor;
        }
        private static int GetRobotWidth(IInputArray Hexagon)
        {
            var hexagon = Hexagon as VectorOfPoint;

            LineSegment2D d1 = new LineSegment2D(hexagon[0], hexagon[3]);
            LineSegment2D d2 = new LineSegment2D(hexagon[1], hexagon[4]);
            LineSegment2D d3 = new LineSegment2D(hexagon[2], hexagon[5]);

            double width = (d1.Length + d2.Length + d3.Length) / 3;

            return (int)width;
        }
        #endregion
    }
}
