#define DEBUG

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Drawing;

namespace SwarmRoboticsGUI
{
    public class ImageProcessing
    {
        #region Enumerations
        public enum FilterType { NONE, GREYSCALE, CANNY_EDGES, COLOUR, NUM_FILTERS };
        #endregion

        #region Public Properties
        public UMat Image { get; private set; }
        public UMat TestImage { get; private set; }
        public FilterType Filter { get; set; }
        public int LowerH { get; set; }
        public int LowerV { get; set; }
        public int UpperH { get; set; }
        public int UpperS { get; set; }
        public int UpperV { get; set; }
        #endregion

        #region Private Properties
        private int HexCount { get; set; }
        private int LargeContourCount { get; set; }
        private int RobotCount { get; set; }
        #endregion

        public ImageProcessing()
        {
            Filter = FilterType.NONE;
            try
            {
                // Load test image
                UMat image = CvInvoke.Imread("...\\...\\Brae\\Images\\robotcutouts2.png").GetUMat(AccessType.Read);
                // Resize
                CvInvoke.Resize(image, image, new Size(1280, 720));
                TestImage = image.Clone();
            }
            catch (Exception)
            {
                // If load fails, return black image of default size
                TestImage = new Image<Gray, byte>(new Size(640, 480)).Mat.GetUMat(AccessType.Read);
            }          
        }

        #region Public Methods
        public static string ToString(FilterType filter)
        {
            switch (filter)
            {
                case FilterType.NONE:
                    return string.Format("No Filter");
                case FilterType.GREYSCALE:
                    return string.Format("Greyscale");
                case FilterType.CANNY_EDGES:
                    return string.Format("Canny Edges");
                case FilterType.COLOUR:
                    return string.Format("Colour Filtering");
                default:
                    return string.Format("Filter Text Error");
            }
        }
        public void ProcessFilter(UMat Frame)
        {
            switch (Filter)
            {
                case FilterType.NONE:
                    Image = Frame;
                    break;
                case FilterType.GREYSCALE:
                    CvInvoke.CvtColor(Frame, Image, ColorConversion.Bgr2Gray);
                    break;
                case FilterType.CANNY_EDGES:
                    using (UMat In = Frame.Clone())
                    using (UMat Out = Frame.Clone())
                    {
                        CvInvoke.CvtColor(In, In, ColorConversion.Bgr2Gray);
                        CvInvoke.PyrDown(In, In);
                        CvInvoke.PyrUp(In, In);
                        CvInvoke.Canny(In, Out, 80, 40);
                        Image = Out.Clone();
                    }
                    break;
                case FilterType.COLOUR:
                    using (UMat In = Frame.Clone())
                    using (UMat Out = Frame.Clone())
                    {
                        CvInvoke.CvtColor(In, In, ColorConversion.Bgr2Hsv);
                        using (ScalarArray lower = new ScalarArray(LowerH))
                        using (ScalarArray upper = new ScalarArray(UpperH))
                        using (Mat HueIn = new Image<Gray, byte>(Frame.Size).Mat)
                        {
                            CvInvoke.ExtractChannel(In, HueIn, 0);
                            CvInvoke.InRange(HueIn, lower, upper, Out);
                        }
                        using (Mat SatIn = new Image<Gray, byte>(Frame.Size).Mat)
                        {
                            CvInvoke.ExtractChannel(In, SatIn, 1);
                            CvInvoke.Threshold(SatIn, SatIn, 25, 255, ThresholdType.Binary);
                            CvInvoke.BitwiseAnd(Out, SatIn, Out);
                        }
                        CvInvoke.PutText(Out, CvInvoke.CountNonZero(Out).ToString(), new Point(20, 20), FontFace.HersheySimplex, 1, new MCvScalar(128, 128, 128), 2);
                        Image = Out.Clone();
                    }
                    break;
                default:
                    break;
            }
        }
        public Robot[] GetRobots(UMat Frame, Robot[] RobotList)
        {
            // Find every contour in the image
            VectorOfVectorOfPoint Contours = GetCountours(Frame, 5, RetrType.External);
            // Filter out small and large contours
            VectorOfVectorOfPoint ProcessedContours = FilterContourArea(Contours, 1000, 100000);

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
                    RobotList[RobotID].IsTracked = true;
                    // Give corresponding ID value to the robot
                    RobotList[RobotID].ID = RobotID;
                    // DEBUG: Store the vertices of the hexagonal shape
                    RobotList[RobotID].Contour = ProcessedContour.ToArray();
                    // DEBUG: Robot counter
                    RobotCount++;
                    // Get the robots center
                    MCvPoint2D64f AbsoluteCOM = CvInvoke.Moments(ProcessedContour).GravityCenter;
                    // Store the robots location
                    RobotList[RobotID].Location = new Point((int)AbsoluteCOM.X, (int)AbsoluteCOM.Y);
                    // Get the robots direction
                    Point Direction = FindDirection(RobotImage);
                    // Goto next robot if true
                    if (Direction == null) continue;

                    Direction = new Point(Direction.X + RobotBounds.X, Direction.Y + RobotBounds.Y);
                    if (RobotList[RobotID].Location.X > 0 && RobotList[RobotID].Location.Y > 0)
                    {
                        // Get robot heading using Atan function
                        int dy = Direction.Y - RobotList[RobotID].Location.Y;
                        int dx = Direction.X - RobotList[RobotID].Location.X;
                        RobotList[RobotID].Heading = Math.Atan2(dy, dx);
                    }
                }
                // DEBUG: Store counters
                this.HexCount = HexCount;
                this.RobotCount = RobotCount;
                LargeContourCount = ProcessedContours.Size;
            }
            return RobotList;
        }
        #endregion

        #region Private Methods
        private static VectorOfVectorOfPoint GetCountours(UMat Frame, int BlurSize, RetrType Mode)
        {
            VectorOfVectorOfPoint Contours = new VectorOfVectorOfPoint();
            using (UMat Input = Frame.Clone())
            {
                CvInvoke.CvtColor(Input, Input, ColorConversion.Bgr2Gray);
                CvInvoke.GaussianBlur(Input, Input, new Size(BlurSize, BlurSize), 0);
                CvInvoke.BitwiseNot(Input, Input);
                CvInvoke.AdaptiveThreshold(Input, Input, 255, AdaptiveThresholdType.MeanC, ThresholdType.Binary, 3, 0);
                CvInvoke.FindContours(Input, Contours, null, Mode, ChainApproxMethod.ChainApproxNone);
            }
            return Contours;
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
            ProcessedContours = FilterContourArea(Contours, 50, 10000);

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
