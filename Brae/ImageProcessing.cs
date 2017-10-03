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

        #region Private Properties
        private static Range SaturationRange = new Range(25, 250);
        private static Range ValueRange = new Range(25, 250);
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
            if (Input != null)
            {
                switch (Filter)
                {
                    case FilterType.NONE:
                        Input.GetInputArray().CopyTo(Output);
                        break;
                    case FilterType.GREYSCALE:
                        using (var In = new UMat())
                        {
                            //CvInvoke.CvtColor(Input, Output, ColorConversion.Bgr2Gray);
                            CvInvoke.CvtColor(Input, In, ColorConversion.Bgr2Hsv);
                            CvInvoke.ExtractChannel(In, Output, 2);
                        }
                        break;
                    case FilterType.CANNY_EDGES:
                        using (var In = new UMat())
                        using (var Out = new UMat(Input.GetInputArray().GetSize(), Input.GetInputArray().GetDepth(), Input.GetInputArray().GetChannels()))
                        using (var Contours = new VectorOfVectorOfPoint())
                        using (var ProcessedContours = new VectorOfVectorOfPoint())
                        {
                            // Convert to grayscale
                            CvInvoke.CvtColor(Input, In, ColorConversion.Bgr2Gray);
                            // Noise removal
                            CvInvoke.GaussianBlur(In, In, new Size(3, 3), 0);

                            //CvInvoke.DetailEnhance(Input, Input);
                            // Find every contour in the image
                            //CvInvoke.Canny(In, In, 0, 255);
                            CvInvoke.AdaptiveThreshold(In, In, 255, AdaptiveThresholdType.MeanC, ThresholdType.Binary, 21, 0);
                            // Find only the external contours applying no shape approximations
                            CvInvoke.FindContours(In, Contours, null, RetrType.External, ChainApproxMethod.ChainApproxNone);
                            // Filter out small and large contours
                            //FilterContourArea(Contours, ProcessedContours, 1000000, 2000000);
                            FilterContourArea(Contours, ProcessedContours, 200, 2000000);
                            //
                            CvInvoke.DrawContours(Out, ProcessedContours, -1, new MCvScalar(255, 255, 255), 2);
                            //
                            Out.GetOutputArray().CopyTo(Output);

                        }
                        break;
                    case FilterType.COLOUR:
                        using (var Out = new Mat())
                        using (var HOut = new Mat())
                        using (var SOut = new Mat())
                        using (var VOut = new Mat())
                        using (ScalarArray lower = new ScalarArray(HueLower))
                        using (ScalarArray upper = new ScalarArray(HueUpper))
                        {
                            //
                            CvInvoke.CvtColor(Input, Out, ColorConversion.Bgr2Hsv);

                            //Range SaturationRange = new Range(20, 200);
                            //Range ValueRange = new Range(10, 200);
                            //CvInvoke.Imwrite("MaskedHSV.png", Masked);

                            CvInvoke.ExtractChannel(Out, SOut, 1);
                            CvInvoke.ExtractChannel(Out, VOut, 2);
                            CvInvoke.Threshold(SOut, SOut, SaturationRange.Start, SaturationRange.End, ThresholdType.Binary);
                            //CvInvoke.Imwrite("SOut.png", SOut);
                            CvInvoke.Threshold(VOut, VOut, ValueRange.Start, ValueRange.End, ThresholdType.Binary);
                            //CvInvoke.Imwrite("VOut.png", VOut);
                            //CvInvoke.AdaptiveThreshold(SOut, SOut, 254, AdaptiveThresholdType.MeanC, ThresholdType.Binary, 21, 0);
                            //CvInvoke.AdaptiveThreshold(VOut, VOut, 254, AdaptiveThresholdType.MeanC, ThresholdType.Binary, 21, 0);

                            CvInvoke.BitwiseAnd(SOut, VOut, SOut);
                            //
                            CvInvoke.ExtractChannel(Out, HOut, 0);
                            CvInvoke.InRange(HOut, lower, upper, HOut);


                            CvInvoke.BitwiseAnd(SOut, HOut, Output);


                            ////
                            //CvInvoke.ExtractChannel(Out, SOut, 1);
                            //CvInvoke.Threshold(SOut, SOut, 40, 230, ThresholdType.Binary);
                            //CvInvoke.BitwiseAnd(SOut, HOut, SOut);
                            ////
                            //CvInvoke.ExtractChannel(Out, Out, 2);
                            //CvInvoke.Threshold(Out, Out, 60, 195, ThresholdType.Binary);
                            //CvInvoke.BitwiseAnd(SOut, Out, Output);
                            ////
                            //CvInvoke.ExtractChannel(Out, HOut, 0);
                            //CvInvoke.InRange(HOut, lower, upper, HOut);
                            
                        }
                        break;
                    default:
                        break;
                }
            }
        }
        public static void GetRobots(IInputArray Frame, List<RobotItem> RobotList, Arena Arena)
        {
            // DEBUG: Real corner-to-corner distance in millimetres
            //const double REAL_DISTANCE = 1664.882954;
            //const double REAL_DISTANCE = 840;
            //const double REAL_DISTANCE = 297;

            if (Arena.Contour == null) return;
            
            var Bounds = CvInvoke.BoundingRectangle(new VectorOfPoint(Arena.Contour));           
            var Input = new UMat(Frame as UMat, Bounds);

            // DEBUG: Report image - Frame cropped to arena
            //CvInvoke.Imwrite("Arena-Cropped.png", Input);

            // DEBUG: Draw arena contour on frame
            //var Input = Frame as UMat;
            //CvInvoke.DrawContours(Input, new VectorOfVectorOfPoint(new VectorOfPoint(Arena.Contour)), -1, new MCvScalar(255, 0, 0), 3);

            var Hexagons = new VectorOfVectorOfPoint();

            using (var Contours = new VectorOfVectorOfPoint())
            using (var FilteredContours = new VectorOfVectorOfPoint())
            {
                // Find every contour in the image
                GetCountours(Input, Contours, 1, RetrType.List, ChainApproxMethod.ChainApproxSimple);


                // DEBUG: Report images - Arena cropped with all internal contours
                //var ContourMat = new Mat(Input.Size, Input.Depth, Input.NumberOfChannels);
                //ContourMat.SetTo(new MCvScalar(0, 0, 0));
                //CvInvoke.DrawContours(ContourMat, Contours, -1, new MCvScalar(255, 255, 255), 2);
                //CvInvoke.Imwrite("Arena-CroppedAllContours.png", ContourMat);


                // Filter out small and large contours
                FilterContourArea(Contours, FilteredContours, 1000, 5000);

                // DEBUG: Report images - Arena cropped with internal contours filtered by area
                //var ContourMat = new Mat(Input.Size, Input.Depth, Input.NumberOfChannels);
                //ContourMat.SetTo(new MCvScalar(0, 0, 0));
                //CvInvoke.DrawContours(ContourMat, FilteredContours, -1, new MCvScalar(255, 255, 255), 2);
                //CvInvoke.Imwrite("Arena-CroppedFilteredContours.png", ContourMat);

                int HexCount = GetHexagons(FilteredContours, Hexagons);

                // DEBUG: Report images - Arena cropped with hexagon contours
                //var ContourMat = new Mat(Input.Size, Input.Depth, Input.NumberOfChannels);
                //ContourMat.SetTo(new MCvScalar(0, 0, 0));
                //CvInvoke.DrawContours(ContourMat, Hexagons, -1, new MCvScalar(255, 255, 255), 2);
                //CvInvoke.Imwrite("Arena-CroppedHexagons.png", ContourMat);
            }           

            // DEBUG: Testing CLAHE
            //CvInvoke.Imwrite("arenaFrame.png", Input);
            //var InputHSV = new UMat();
            //CvInvoke.CvtColor(Input, InputHSV, ColorConversion.Bgr2Hsv);
            //VectorOfUMat Channels = new VectorOfUMat();
            //CvInvoke.Split(InputHSV, Channels);            
            //CvInvoke.CLAHE(Channels[2], 40, new Size(8, 8), Channels[2]);            
            //CvInvoke.Merge(Channels, InputHSV);
            //CvInvoke.CvtColor(InputHSV, Input, ColorConversion.Hsv2Bgr);
            //CvInvoke.Imwrite("clahe result.png", Input);

            // DEBUG: Testing Histogram equalization
            //VectorOfUMat Channels = new VectorOfUMat();
            //CvInvoke.Imwrite("zbefore.png", Input);    
            //CvInvoke.CvtColor(Input, Input, ColorConversion.Bgr2Hsv);           
            //CvInvoke.Split(Input, Channels);
            ////HistogramViewer.Show(Input);            
            //CvInvoke.EqualizeHist(Channels[2], Channels[2]);
            //CvInvoke.Merge(Channels, Input);
            ////HistogramViewer.Show(Input);
            //CvInvoke.CvtColor(Input, Input, ColorConversion.Hsv2Bgr);
            //CvInvoke.Imwrite("zafter.png", Input);

            int RobotCount = 0;
            // Loop through the hexagons in the frame
            for (int i = 0; i < Hexagons.Size; i++)
            {
                VectorOfPoint Hexagon = Hexagons[i];
                var RobotFrame = new UMat();
                var RobotFrameOffset = GetRobotFrame(Input, Hexagon, RobotFrame);

                // DEBUG: Report images - Robot input image
                //CvInvoke.Imwrite("Robot-Frame.png", RobotFrame);

                VectorOfPoint RelativeHex = new VectorOfPoint();
                Point[] Points = new Point[Hexagon.Size];
                for (int h = 0; h < Hexagon.Size; h++)
                {
                    Points[h] = Point.Subtract(Hexagon[h], new Size(RobotFrameOffset.X, RobotFrameOffset.Y));
                }
                RelativeHex.Push(Points);
                // Check for the colour ID, Returns (-1) if no robot ID
                int RobotID = IdentifyRobot(RobotFrame, RelativeHex);
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

                // Get the robot reference as a RobotItem
                RobotItem robot = RobotList[index];
                // Get the robot reference as an object that implements IObstacle
                IObstacle obstacle = RobotList[index];

                obstacle.IsTracked = true;
                // DEBUG: Store the vertices of the hexagonal shape
                obstacle.Contour = Hexagon.ToArray();

                // Get the robots center
                MCvPoint2D64f COM = CvInvoke.Moments(Hexagon).GravityCenter;

                // Store the robots pixel location
                obstacle.PixelLocation = new Point((int)COM.X, (int)COM.Y);

                // Use the arena's size and location in frame to scale robots location to real world
                if (Arena.ScaleFactor != 0 && !Arena.Origin.IsEmpty)
                {
                    // BRAE: Calculate robot width dynamically
                    // Get the robot width using the arena scale factor
                    //var radius = GetRobotRadius(Hexagon);
                    //RobotList[index].Width = (int)(2 * radius * Arena.ScaleFactor);
                    // Calulate height using sqrt(3)*radius
                    //RobotList[index].Height = (int)(radius * Math.Sqrt(3));
                    obstacle.IsVisible = true;
                    obstacle.LastVisible = DateTime.Now;
                    // Store the robots real-world location
                    obstacle.Location = new System.Windows.Point((COM.X - Arena.Origin.X) * Arena.ScaleFactor,
                                                                 (COM.Y - Arena.Origin.Y) * Arena.ScaleFactor);
                }

                // Get the robots facing
                var RobotFrameLocation = new Point((int)COM.X - RobotFrameOffset.X, (int)COM.Y - RobotFrameOffset.Y);
                double Facing = FindFacing(RobotFrame, RobotFrameLocation);
                if (Facing == 0) continue;

                if (obstacle.PixelLocation.X > 0 && obstacle.PixelLocation.Y > 0)
                {
                    robot.HasFacing = true;
                    robot.Facing = Facing;
                    robot.FacingDeg = Facing * 180 / Math.PI;

                    int DirectionX = (int)(obstacle.Width * 0.4);
                    robot.FacingMarker = DirectionX;
                }
            }
        }
        public static void GetArena(IInputArray Frame, Arena Arena)
        {
            var Input = (Frame as UMat).Clone();
            var ArenaContour = new VectorOfPoint();

            //double factor = 0;
            Point Origin = new Point();
            //const double REAL_DISTANCE = 1664.882954;
            //const double REAL_DISTANCE = 297;

            var Contours = new VectorOfVectorOfPoint();
            var ProcessedContours = new VectorOfVectorOfPoint();

            // Convert to a single channel image
            CvInvoke.CvtColor(Frame, Input, ColorConversion.Bgr2Gray);

            // Noise removal
            CvInvoke.GaussianBlur(Input, Input, new Size(3, 3), 0);
            CvInvoke.Dilate(Input, Input, null, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(0));

            // DEBUG: Report images - Arena dilated image
            //CvInvoke.Imwrite("Arena-BlurDilate.png", Input);

            // Threshold the image to find the edges             
            CvInvoke.AdaptiveThreshold(Input, Input, 255, AdaptiveThresholdType.MeanC, ThresholdType.Binary, 3, 0);
            //CvInvoke.Canny(Input, Input, 0, 255);

            // DEBUG: Report images - Arena threshold image
            //CvInvoke.Imwrite("Arena-Threshold.png", Input);

            // Find only the external contours applying no shape approximations
            CvInvoke.FindContours(Input, Contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
            //GetCountours(Frame, Contours, 0, RetrType.External, ChainApproxMethod.ChainApproxNone);

            // DEBUG: Report images - Arena all contours
            //var ContourMat = new Mat(Input.Size, Input.Depth, Input.NumberOfChannels);
            //ContourMat.SetTo(new MCvScalar(0, 0, 0));
            //CvInvoke.DrawContours(ContourMat, Contours, -1, new MCvScalar(255, 255, 255), 3);
            //CvInvoke.Imwrite("Arena-AllContours.png", ContourMat);

            // Filter out small and large contours
            FilterContourArea(Contours, ProcessedContours, 1000000, 1500000);

            // DEBUG: Report images - Arena filtered contours based on area
            //var ContourMat = new Mat(Input.Size, Input.Depth, Input.NumberOfChannels);
            //ContourMat.SetTo(new MCvScalar(0,0,0));
            //CvInvoke.DrawContours(ContourMat, ProcessedContours, -1, new MCvScalar(255, 255, 255), 3);
            //CvInvoke.Imwrite("Arena-FilteredContours.png", ContourMat);

            // Loop through the filtered contours in the frame until the arena is found
            for (int i = 0; i < ProcessedContours.Size; i++)
            {
                VectorOfPoint ProcessedContour = ProcessedContours[i];
                // Get approximate polygonal shape of contour
                CvInvoke.ApproxPolyDP(ProcessedContour, ProcessedContour, CvInvoke.ArcLength(ProcessedContour, true) * 0.08, true);

                // If contour is the right shape (square), 
                if (IsShape(ProcessedContour, Shape.SQUARE))
                {
                    //factor = GetScaleFactor(Frame, ProcessedContour);    
                    ArenaContour.Push(ProcessedContour);
                    break;
                }                 
            }

            // DEBUG: Report images - Arena contour approximation
            //ContourMat.SetTo(new MCvScalar(0,0,0));
            //CvInvoke.DrawContours(ContourMat, ProcessedContours, i, new MCvScalar(255, 255, 255), 3);
            //CvInvoke.Imwrite("Arena-ApproxContour.png", ContourMat);

            // Test square is 210x210mm with area 44100mm^2
            // this area = square area / factor^2
            // for desk2floor setup this should roughly be 140,000 area
            // Arena is 1177x1177mm with area 1,385,329mm^2
            // this area = square area / factor^2
            //var area = CvInvoke.ContourArea(ArenaContour);

            // If the arena was identified
            if (ArenaContour.Size != 0)
            {
                // Find the origin by looking for the point that is top-left most in the frame
                Origin = FindOrigin(Frame, ArenaContour);                
                if (!Origin.IsEmpty)
                {
                    // Bounds of arena contour used as reference frame for further pixel locations
                    var Bounds = CvInvoke.BoundingRectangle(ArenaContour);
                    // Get the pixel to real-world scale factor
                    Arena.ScaleFactor = GetScaleFactor(Frame, ArenaContour); ;
                    // Store origin point relative to bounds
                    Arena.Origin = new Point(Origin.X - Bounds.X, Origin.Y - Bounds.Y);
                    // Store the arena contour
                    Arena.Contour = ArenaContour.ToArray();
                }
            }
        }

        // TODO: Implement auto white balance that actually works unlike the built-in camera one
        public static void WhiteBalance()
        {
            // WHERE IS WHITEBALANCE
        }
        #endregion

        #region Private Methods
        private static void GetCountours(IInputArray Frame, IOutputArray Contours, int BlurSize, RetrType Mode, ChainApproxMethod Approx)
        {
            //bool HasCuda = CudaInvoke.HasCuda;
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
                //CvInvoke.CvtColor(Frame, Input, ColorConversion.Bgr2Gray);

                CvInvoke.CvtColor(Frame, Input, ColorConversion.Bgr2Hsv);
                CvInvoke.ExtractChannel(Input, Input, 2);

                // Noise removal
                if (BlurSize > 0 && BlurSize % 2 != 0)
                    CvInvoke.GaussianBlur(Input, Input, new Size(BlurSize, BlurSize), 0);
                // Threshold the image to find the edges  
                //CvInvoke.Canny(Input, Input, 0, 255);
                CvInvoke.AdaptiveThreshold(Input, Input, 255, AdaptiveThresholdType.MeanC, ThresholdType.Binary, 9, 0);
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
            // BRAE: Make GetHexagons work with any shape
            // TODO: Make GetHexagons work with any shape

            var Input = Contours as VectorOfVectorOfPoint;
            var Output = Hexagons as VectorOfVectorOfPoint;
            for (int i = 0; i < Input.Size; i++)
            {
                VectorOfPoint Contour = Input[i];
                // Get approximate polygonal shape of contour
                CvInvoke.ApproxPolyDP(Contour, Contour, CvInvoke.ArcLength(Contour, true) * 0.05, true);

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
            // 10 degrees of tolerance
            const int tolerance = 10;

            var contour = Contour as VectorOfPoint;

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
                    HueRange.Start = 4;
                    HueRange.End = 12;
                    break;
                case KnownColor.Yellow:
                    HueRange.Start = 17;
                    HueRange.End = 32;
                    break;
                case KnownColor.Green:
                    HueRange.Start = 40;
                    HueRange.End = 87;
                    break;
                case KnownColor.LightBlue:
                    HueRange.Start = 99;
                    HueRange.End = 105;
                    break;
                case KnownColor.DarkBlue:
                    HueRange.Start = 107;
                    HueRange.End = 116;
                    break;
                case KnownColor.Red:
                    HueRange.Start = 172;
                    HueRange.End = 179;
                    break;
                default:
                    HueRange.Start = 0;
                    HueRange.End = 0;
                    break;
            }
            // Try using ideal hue values
            //LowerH = TargetColour.GetHue() * 255 / 360 - 2;
            //UpperH = TargetColour.GetHue() * 255 / 360 + 2;
            return HueRange;
        }
        private static bool HasHueRange(IInputArray Frame, Range HueRange)
        {
            const int ColourFraction = 20;      // 1/20=5% of the image needs to be within HueRange
            int Count = 0;
            //Range SaturationRange = new Range(25, 230);
            //Range ValueRange = new Range(60, 195);
            int Width = Frame.GetInputArray().GetSize().Width;
            int Height = Frame.GetInputArray().GetSize().Height;
            int ColourCount = Width * Height / ColourFraction;

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
            bool IsOrange = false, IsYellow = false, IsGreen = false;
            bool IsDarkBlue = false, IsLightBlue = false, IsRed = false;
            int RobotID = -1;
            var hexagon = Contour as VectorOfPoint;
            var Image = Frame.GetInputArray();

            // DEBUG: Report Images - Robot input frame
            //CvInvoke.Imwrite("Robot-Frame.png", Frame);

            var Mask = new Mat(Image.GetSize(), Image.GetDepth(), Image.GetChannels());
            Mask.SetTo(new MCvScalar(0, 0, 0));
            CvInvoke.DrawContours(Mask, new VectorOfVectorOfPoint(hexagon), -1, new MCvScalar(255, 255, 255), -1);

            // DEBUG: Report Images - Robot mask without erode
            //CvInvoke.Imwrite("Robot-MaskNoErode.png", Mask);
            // DEBUG: Report Images - Robot image masked with mask that has no erode passes
            //var MaskedNoErode = new Mat();
            //Image.CopyTo(MaskedNoErode, Mask);
            //CvInvoke.Imwrite("Robot-FrameMaskedNoErode.png", MaskedNoErode);

            // Apply three passes of erode to remove edges where the colour of the robot has bled
            CvInvoke.Erode(Mask, Mask, null, new Point(-1, -1), 3, BorderType.Constant, new MCvScalar(0));

            // DEBUG: Report Images - Robot mask with erode
            //CvInvoke.Imwrite("Robot-MaskEroded.png", Mask);
            // DEBUG: Report Images - Robot image masked with mask that has erode passes
            //var MaskedEroded = new Mat();
            //Image.CopyTo(MaskedEroded, Mask);
            //CvInvoke.Imwrite("Robot-FrameMaskedEroded.png", MaskedEroded);

            var Masked = new Mat();
            Image.CopyTo(Masked, Mask);

            var SOut = new Mat();
            var VOut = new Mat();
            //
            CvInvoke.CvtColor(Masked, Masked, ColorConversion.Bgr2Hsv);

            // DEBUG: Report Images
            //CvInvoke.Imwrite("MaskedHSV.png", Masked);

            CvInvoke.ExtractChannel(Masked, SOut, 1);
            CvInvoke.ExtractChannel(Masked, VOut, 2);
            CvInvoke.Threshold(SOut, SOut, SaturationRange.Start, SaturationRange.End, ThresholdType.Binary);        
            CvInvoke.Threshold(VOut, VOut, ValueRange.Start, ValueRange.End, ThresholdType.Binary);
            //CvInvoke.AdaptiveThreshold(SOut, SOut, 254, AdaptiveThresholdType.MeanC, ThresholdType.Binary, 21, 0);
            //CvInvoke.AdaptiveThreshold(VOut, VOut, 254, AdaptiveThresholdType.MeanC, ThresholdType.Binary, 21, 0);

            // DEBUG: Report Images
            //CvInvoke.Imwrite("SOut.png", SOut);
            //CvInvoke.Imwrite("VOut.png", VOut);  

            CvInvoke.BitwiseAnd(SOut, VOut, SOut);

            var Result = new Mat();
            Masked.CopyTo(Result, SOut);

            // DEBUG: Report Images - Robot masked with saturation and value is HSV colourspace
            CvInvoke.Imwrite("Robot-FrameHSVColourMasked.png", Result);

            // DEBUG: Report Images - Robot masked with saturation and value is RGB colourspace
            var ResultBGR = new Mat();
            CvInvoke.CvtColor(Result, ResultBGR, ColorConversion.Hsv2Bgr);
            CvInvoke.Imwrite("Robot-FrameRGBColourMasked.png", ResultBGR);

            // Look for colours on the robot
            IsOrange = HasHueRange(Result, GetHueRange(KnownColor.Orange));
            IsYellow = HasHueRange(Result, GetHueRange(KnownColor.Yellow));
            IsGreen = HasHueRange(Result, GetHueRange(KnownColor.Green));
            IsLightBlue = HasHueRange(Result, GetHueRange(KnownColor.LightBlue));
            IsDarkBlue = HasHueRange(Result, GetHueRange(KnownColor.DarkBlue));
            IsRed = HasHueRange(Result, GetHueRange(KnownColor.Red));

            // LIGHTBLUE GREEN RED
            if (!IsOrange && !IsYellow && IsGreen && IsLightBlue && !IsDarkBlue && IsRed) RobotID = 0;       // RED
            //if (IsGreen && IsLightBlue && IsRed) RobotID = 0;       // RED
            // ORANGE LIGHTBLUE RED
            else if (IsOrange && !IsYellow && !IsGreen && IsLightBlue && !IsDarkBlue && IsRed) RobotID = 1;  // YELLOW
            //else if (IsOrange && IsLightBlue && IsRed) RobotID = 1;  // YELLOW
            // ORANGE GREEN RED
            else if (IsOrange && !IsYellow && IsGreen && !IsLightBlue && !IsDarkBlue && IsRed) RobotID = 2;  // PURPLE
            //else if (IsOrange && IsGreen && IsRed) RobotID = 2;  // PURPLE
            // GREEN YELLOW DARKBLUE
            else if (!IsOrange && IsYellow && IsGreen && !IsLightBlue && IsDarkBlue && !IsRed) RobotID = 3;  // LIGHTBLUE
            //else if (IsYellow && IsGreen && IsDarkBlue) RobotID = 3;  // LIGHTBLUE
            // LIGHTBLUE GREEN DARKBLUE
            else if (!IsOrange && !IsYellow && IsGreen && IsLightBlue && IsDarkBlue && !IsRed) RobotID = 4;  // DARKBLUE
            //else if (IsGreen && IsLightBlue && IsDarkBlue) RobotID = 4;  // DARKBLUE
            // ORANGE YELLOW GREEN
            else if (IsOrange && IsYellow && IsGreen && !IsLightBlue && !IsDarkBlue && !IsRed) RobotID = 5;  // BROWN
            //else if (IsOrange && IsYellow && IsGreen) RobotID = 5;  // BROWN
            // LIGHTBLUE YELLOW ORANGE
            else if (IsOrange && IsYellow && !IsGreen && IsLightBlue && !IsDarkBlue && !IsRed) RobotID = 6;  // PINK
            //else if (IsOrange && IsYellow && IsLightBlue) RobotID = 6;  // PINK
            // ORANGE YELLOW RED
            else if (IsOrange && IsYellow && !IsGreen && !IsLightBlue && !IsDarkBlue && IsRed) RobotID = 7;  // ORANGE
            //else if (IsOrange && IsYellow && IsRed) RobotID = 7;  // ORANGE

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
            // BRAE: Make GetScaleFactor work for any shape
            // TODO: Make GetScaleFactor work for any shape

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
        private static int GetRobotRadius(IInputArray Hexagon)
        {
            var hexagon = Hexagon as VectorOfPoint;
            // Lines through centre of the robot
            LineSegment2D d1 = new LineSegment2D(hexagon[0], hexagon[3]);
            LineSegment2D d2 = new LineSegment2D(hexagon[1], hexagon[4]);
            LineSegment2D d3 = new LineSegment2D(hexagon[2], hexagon[5]);
            // Average to get radius of arena
            double radius = (d1.Length + d2.Length + d3.Length) / 3;

            return (int)radius;
        }
        #endregion
    }
}
