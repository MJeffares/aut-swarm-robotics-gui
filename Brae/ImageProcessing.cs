#define DEBUG

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace SwarmRoboticsGUI
{
    public class ImageProcessing
    {
        public enum FilterType { NONE, GREYSCALE, CANNY_EDGES, COLOUR, NUM_FILTERS };
        

        public FilterType Filter { get; set; }
        
        
        public UMat Image { get; private set; }
        public UMat testImage { get; private set; }

        // Debugging variables
        #region
        // Blur, Canny, and Threshold values.
        public double LowerC { get; set; }
        public int ColourC { get; set; }
        public int LowerH { get; set; }
        public int LowerS { get; set; }
        public int LowerV { get; set; }
        public int UpperH { get; set; }
        public int UpperS { get; set; }
        public int UpperV { get; set; }
        private int HexCount { get; set; }
        private int LargeContourCount { get; set; }
        private int RobotCount { get; set; }
        #endregion

        public ImageProcessing()
        {
            
            Filter = FilterType.NONE;

            testImage = CvInvoke.Imread("...\\...\\Brae\\Images\\robotcutouts2.png").GetUMat(AccessType.Read);
            CvInvoke.Resize(testImage, testImage, new Size(640, 480));
        }
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
                            CvInvoke.Threshold(SatIn, SatIn, LowerS, 255, ThresholdType.Binary);
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
        
        private bool IsHexagon(VectorOfPoint Contour)
        {
            if (Contour.Size != 6)
            {
                return false;
            }
            LineSegment2D[] edges = PointCollection.PolyLine(Contour.ToArray(), true);
            for (int j = 0; j < edges.Length; j++)
            {
                double angle = Math.Abs(edges[(j + 1) % edges.Length].GetExteriorAngleDegree(edges[j]));
                if (angle < 30 || angle > 90)
                {
                    return false;
                }
            }
            return true;
        }
        private bool IsEquilTriangle(VectorOfPoint Contour)
        {
            if (Contour.Size != 3)
            {
                return false;
            }
            LineSegment2D[] edges = PointCollection.PolyLine(Contour.ToArray(), true);
            for (int j = 0; j < edges.Length; j++)
            {
                double angle = Math.Abs(edges[(j + 1) % edges.Length].GetExteriorAngleDegree(edges[j]));
                if (angle < 100 || angle > 140)
                {
                    return false;
                }
            }
            return true;
        }
        private int IdentifyRobot(VectorOfPoint Contour, UMat Frame)
        {
            bool IsOrange = false, IsYellow = false, IsGreen = false, IsDarkBlue = false, IsLightBlue = false, IsPurple = false;

            int RobotID = -1;

            using (Mat Mask = new Image<Gray, byte>(Frame.Size).Mat)
            using (Mat Out = new Image<Bgr, byte>(Frame.Size).Mat)
            using (VectorOfVectorOfPoint ContourVect = new VectorOfVectorOfPoint())
            {
                ContourVect.Push(Contour);
                CvInvoke.DrawContours(Mask, ContourVect, 0, new MCvScalar(255, 255, 255), -1);
                // This is done to focus colour detection to one robot
                CvInvoke.Subtract(Frame, Out, Out, Mask);
                // Look for colours on the robot
                IsOrange = FindColour(Out, Color.Orange);
                IsYellow = FindColour(Out, Color.Yellow);
                IsGreen = FindColour(Out, Color.Green);
                IsLightBlue = FindColour(Out, Color.LightBlue);
                IsDarkBlue = FindColour(Out, Color.DarkBlue);
                IsPurple = FindColour(Out, Color.Purple);
            }

            if (IsOrange && IsYellow && IsGreen && !IsLightBlue && !IsDarkBlue && !IsPurple) RobotID = 0;

            if (IsDarkBlue && IsYellow && IsOrange && !IsLightBlue && !IsGreen && !IsPurple) RobotID = 1;

            if (IsGreen && IsYellow && IsDarkBlue && !IsLightBlue && !IsOrange && !IsPurple) RobotID = 2;

            if (IsOrange && IsYellow && IsPurple && !IsLightBlue && !IsDarkBlue && !IsGreen) RobotID = 3;

            if (IsLightBlue && IsGreen && IsDarkBlue && !IsYellow && !IsOrange && !IsPurple) RobotID = 4;

            if (IsOrange && IsGreen && IsPurple && !IsLightBlue && !IsDarkBlue && !IsYellow) RobotID = 5;

            return RobotID;
        }
        private Point FindDirection(UMat Frame)
        {
            MCvPoint2D64f TriangleCOM = new MCvPoint2D64f();
            MCvMoments TriangleMoment = new MCvMoments();
            VectorOfVectorOfPoint Contours = new VectorOfVectorOfPoint();
            VectorOfVectorOfPoint LargeContours = new VectorOfVectorOfPoint();
            VectorOfVectorOfPoint ApproxContours = new VectorOfVectorOfPoint();
            double Area;
            using (UMat Input = Frame.Clone())
            {
                CvInvoke.CvtColor(Input, Input, ColorConversion.Bgr2Gray);
                CvInvoke.BitwiseNot(Input, Input);
                CvInvoke.AdaptiveThreshold(Input, Input, 255, AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 3, 0);
                CvInvoke.FindContours(Input, Contours, null, RetrType.Ccomp, ChainApproxMethod.ChainApproxSimple);
            }
            //
            for (int i = 0; i < Contours.Size; i++)
            {
                Area = Math.Abs(CvInvoke.ContourArea(Contours[i]));
                // Remove high/low freq noise contours
                if (Area > 0 && Area < 50000)
                {
                    LargeContours.Push(Contours[i]);
                }
            }
            ApproxContours.Push(LargeContours);

            for (int i = 0; i < LargeContours.Size; i++)
            {
                // Get approximate polygonal shape of contour
                CvInvoke.ApproxPolyDP(LargeContours[i], ApproxContours[i], 15.0, true);
                // Check if contour is the right shape (triangle)
                if (IsEquilTriangle(ApproxContours[i]))
                {
                    TriangleMoment = CvInvoke.Moments(ApproxContours[i]);
                    TriangleCOM = TriangleMoment.GravityCenter;
                }
            }
            return new Point((int)TriangleMoment.GravityCenter.X, (int)TriangleMoment.GravityCenter.Y);
        }
        public bool FindColour(Mat Frame, Color TargetColour)
        {
            int Count;
            double LowerH = 0;
            double UpperH = 0;

            if (TargetColour == Color.Orange)
            {
                LowerH = 0;
                UpperH = 15;
            }
            if (TargetColour == Color.Yellow)
            {
                LowerH = 15;
                UpperH = 30;
            }
            if (TargetColour == Color.Green)
            {
                LowerH = 30;
                UpperH = 90;
            }
            if (TargetColour == Color.LightBlue)
            {
                LowerH = 90;
                UpperH = 105;
            }
            if (TargetColour == Color.DarkBlue)
            {
                LowerH = 105;
                UpperH = 125;
            }
            if (TargetColour == Color.Purple)
            {
                LowerH = 125;
                UpperH = 175;
            }

            //LowerH = TargetColour.GetHue() * 255 / 360 - 2;
            //UpperH = TargetColour.GetHue() * 255 / 360 + 2;
            //LowerH = this.LowerH;
            //UpperH = this.UpperH;

            using (Mat Out = Frame.Clone())
            using (Mat GrayOut = new Image<Gray, byte>(Frame.Size).Mat)
            {
                CvInvoke.CvtColor(Out, Out, ColorConversion.Bgr2Hsv);
                //CvInvoke.Blur(Out, Out, new Size(1, 1), new Point(0, 0));

                using (ScalarArray lower = new ScalarArray(LowerH))
                using (ScalarArray upper = new ScalarArray(UpperH))
                using (Mat HOut = new Image<Gray, byte>(Frame.Size).Mat)
                {
                    CvInvoke.ExtractChannel(Out, HOut, 0);
                    CvInvoke.InRange(HOut, lower, upper, GrayOut);
                }

                using (Mat SOut = new Image<Gray, byte>(Frame.Size).Mat)
                {
                    CvInvoke.ExtractChannel(Out, SOut, 1);
                    CvInvoke.Threshold(SOut, SOut, LowerS, 255, ThresholdType.Binary);
                    CvInvoke.BitwiseAnd(GrayOut, SOut, GrayOut);
                }
                //
                Count = CvInvoke.CountNonZero(GrayOut);
            }

            if (Count > ColourC)
            {
                return true;
            }
            return false;
        }
        public Robot[] GetRobots(UMat Frame, Robot[] RobotList)
        {
            VectorOfVectorOfPoint Contours = new VectorOfVectorOfPoint();
            VectorOfVectorOfPoint LargeContours = new VectorOfVectorOfPoint();
            VectorOfVectorOfPoint ApproxContours = new VectorOfVectorOfPoint();
            double Area;

            using (UMat Input = Frame.Clone())
            {
                CvInvoke.CvtColor(Input, Input, ColorConversion.Bgr2Gray);
                CvInvoke.GaussianBlur(Input, Input, new Size(5, 5), 0);
                CvInvoke.BitwiseNot(Input, Input);
                CvInvoke.AdaptiveThreshold(Input, Input, 255, AdaptiveThresholdType.MeanC, ThresholdType.Binary, 3, 0);
                CvInvoke.FindContours(Input, Contours, null, RetrType.External, ChainApproxMethod.ChainApproxNone);
            }

            //
            for (int i = 0; i < Contours.Size; i++)
            {
                Area = Math.Abs(CvInvoke.ContourArea(Contours[i]));
                // Remove high/low freq noise contours
                if (Area > 1000 && Area <= 100000)
                {
                    LargeContours.Push(Contours[i]);
                }
            }
            ApproxContours.Push(LargeContours);
#if DEBUG
            int HexCount = 0;
            int RobotCount = 0;
#endif
            for (int i = 0; i < LargeContours.Size; i++)
            {

                // Get approximate polygonal shape of contour
                CvInvoke.ApproxPolyDP(LargeContours[i], ApproxContours[i], 5.0, true);
                // Check if contour is the right shape (hexagon)
                if (IsHexagon(ApproxContours[i]))
                {
#if DEBUG
                    HexCount++;
#endif
                    int RobotID;
                    // Rectangular region that encompasses the contour
                    Rectangle RobotBounds = CvInvoke.BoundingRectangle(ApproxContours[i]);
                    // Create an image from the frame cropped down to the contour region
                    using (UMat RobotImage = new UMat(Frame, RobotBounds))
                    {
                        // New contour points relative to cropped region
                        Point[] RelativeContourPoints = new Point[ApproxContours[i].Size];
                        // Old contour points relative to original frame
                        Point[] AbsoluteContourPoints = ApproxContours[i].ToArray();
                        // Reevaluate the contour points based on the new image coordinates
                        for (int j = 0; j < ApproxContours[i].Size; j++)
                        {
                            RelativeContourPoints[j].X = AbsoluteContourPoints[j].X - RobotBounds.X;
                            RelativeContourPoints[j].Y = AbsoluteContourPoints[j].Y - RobotBounds.Y;
                        }
                        MCvMoments RelativeRobotMoment = CvInvoke.Moments(new VectorOfPoint(RelativeContourPoints));
                        MCvPoint2D64f RelativeRobotCOM = RelativeRobotMoment.GravityCenter;

                        // Returns robot ID based on colour code
                        RobotID = IdentifyRobot(new VectorOfPoint(RelativeContourPoints), RobotImage);

                        if (RobotID != -1)
                        {
#if DEBUG
                            RobotCount++;
#endif
                            RobotList[RobotID].Location = new Point((int)RelativeRobotCOM.X + RobotBounds.X, (int)RelativeRobotCOM.Y + RobotBounds.Y);

                            Point RelativeDirectionMarker = FindDirection(RobotImage);
                            if (RelativeDirectionMarker.X > 0 && RelativeDirectionMarker.Y > 0)
                            {
                                RobotList[RobotID].DirectionMarker = new Point(RelativeDirectionMarker.X + RobotBounds.X, RelativeDirectionMarker.Y + RobotBounds.Y);
                                
                                if (RobotList[RobotID].Location.X > 0 && RobotList[RobotID].Location.Y > 0)
                                {
                                    int dy = RobotList[RobotID].DirectionMarker.Y - RobotList[RobotID].Location.Y;
                                    int dx = RobotList[RobotID].DirectionMarker.X - RobotList[RobotID].Location.X;
                                    RobotList[RobotID].Heading = Math.Atan2(dy, dx);
                                }
                            }
                            RobotList[RobotID].Contour = ApproxContours[i].ToArray();
                            RobotList[RobotID].IsTracked = true;
                        }
                    }
                }
#if DEBUG
                this.HexCount = HexCount;
                this.RobotCount = RobotCount;
                LargeContourCount = LargeContours.Size;
#endif
            }
            return RobotList;
        }
    }// CLASS END

}// NAMESPACE END
