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
        public enum FilterType { NONE, GREYSCALE, CANNY_EDGES, BRAE_EDGES, NUM_FILTERS };
        public enum Colour { RED, GREEN, BLUE, YELLOW, CYAN, MAGENTA };

        public FilterType Filter { get; set; }
        public UMat OverlayImage { get; set; }
        private UMat image { get; set; }

        #region
        // Blur, Canny, and Threshold values.
        public double LowerC { get; set; }
        //public double UpperC { get; set; } = 255;
        public int ColourC { get; set; }
        public int LowerH { get; set; }
        public int LowerS { get; set; }
        public int LowerV { get; set; }
        public int UpperH { get; set; }
        public int UpperS { get; set; }
        public int UpperV { get; set; }

        private Robot[] RobotList = new Robot[6];

        #endregion

#if DEBUG
        private int HexCount { get; set; }
        private int LargeContourCount { get; set; }
        private int RobotCount { get; set; }

        public UMat test = new UMat();
#endif

        public ImageProcessing()
        {
            OverlayImage = new UMat();
            Filter = FilterType.NONE;
            // BRAE: Robots are currently initialized in the ImageProcessing class
            ClearRobots();

            //UMat image = CvInvoke.Imread("...\\...\\Images\\ColourCodes.png").GetUMat(AccessType.Read);
            //image = CvInvoke.Imread("...\\...\\Images\\robotcutouts2.png").GetUMat(AccessType.Read);
            //CvInvoke.Resize(image, image, new Size(480, 640));
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
                case FilterType.BRAE_EDGES:
                    return string.Format("Brae Edges");
                default:
                    return string.Format("Filter Text Error");
            }
        }
        public void ProcessFilter(UMat Frame)
        {
            switch (Filter)
            {
                case FilterType.NONE:
                    break;
                case FilterType.GREYSCALE:
                    CvInvoke.CvtColor(Frame, Frame, ColorConversion.Bgr2Gray);
                    break;
                case FilterType.CANNY_EDGES:
                    CvInvoke.CvtColor(Frame, Frame, ColorConversion.Bgr2Gray);
                    CvInvoke.PyrDown(Frame, Frame);
                    CvInvoke.PyrUp(Frame, Frame);
                    CvInvoke.Canny(Frame, Frame, 80, 40);
                    break;
                case FilterType.BRAE_EDGES:
                    //GetRobots(image);
                    //DrawOverlay(image);
                    //OverlayImage = test;
                    if (Frame != null)
                    {
                        GetRobots(Frame);
                        DrawOverlay(Frame);
                        //using (UMat Out = Frame.Clone())
                        //{
                        //    CvInvoke.CvtColor(Out, Out, ColorConversion.Bgr2Hsv);
                        //    using (ScalarArray lower = new ScalarArray(LowerH))
                        //    using (ScalarArray upper = new ScalarArray(UpperH))
                        //    using (Mat HOut = new Image<Gray, byte>(Frame.Size).Mat)
                        //    {
                        //        CvInvoke.ExtractChannel(Out, HOut, 0);
                        //        CvInvoke.InRange(HOut, lower, upper, test);
                        //    }
                        //    using (Mat SOut = new Image<Gray, byte>(Frame.Size).Mat)
                        //    {
                        //        CvInvoke.ExtractChannel(Out, SOut, 1);
                        //        CvInvoke.Threshold(SOut, SOut, LowerS, 255, ThresholdType.Binary);
                        //        CvInvoke.BitwiseAnd(test, SOut, test);
                        //    }
                        //}
                        //CvInvoke.PutText(test, CvInvoke.CountNonZero(test).ToString(), new Point(20, 20), FontFace.HersheySimplex, 1, new MCvScalar(128, 128, 128), 2);
                        //OverlayImage = test;
                    }
                    break;
                default:
                    break;
            }
        }
        public void ClearRobots()
        {
            for (int i = 0; i < RobotList.Length; i++)
            {
                RobotList[i] = new Robot();
            }
        }
        private void DrawOverlay(UMat Frame)
        {
            Mat Input = new Image<Bgr, byte>(Frame.Cols, Frame.Rows).Mat;

            // Creates mask of current robot
            using (VectorOfVectorOfPoint ContourVect = new VectorOfVectorOfPoint())
            {
                for (int i = 0; i < RobotList.Length; i++)
                {
                    if (RobotList[i].Contour != null)
                    {

                        ContourVect.Push(new VectorOfPoint(RobotList[i].Contour));
                    }
                }
                CvInvoke.DrawContours(Input, ContourVect, -1, new MCvScalar(255, 255, 255), -1, LineType.AntiAlias);
            }

            for (int i = 0; i < RobotList.Length; i++)
            {
                if (RobotList[i].Location.X > 0 && RobotList[i].Location.Y > 0)
                {
                    if (!RobotList[i].IsTracked)
                    {
                        // Red indicates robot is not currently tracked, the image will be its previous location
                        CvInvoke.Circle(Input, RobotList[i].Location, 10, new MCvScalar(0, 0, 255), -1, LineType.AntiAlias);
                    }
                    else
                    {
                        // Green indicates new image
                        CvInvoke.Circle(Input, RobotList[i].Location, 10, new MCvScalar(0, 255, 0), -1, LineType.AntiAlias);
                    }
                    if (RobotList[i].Heading.X > 0 && RobotList[i].Heading.Y > 0)
                    {
                        CvInvoke.ArrowedLine(Input, RobotList[i].Location, RobotList[i].Heading, new MCvScalar(20, 20, 20), 2, LineType.AntiAlias, 0, 0.5);
                    }
                    CvInvoke.PutText(Input, i.ToString(), new Point(RobotList[i].Location.X + 20, RobotList[i].Location.Y + 10), FontFace.HersheyScriptSimplex, 1, new MCvScalar(50, 50, 50), 2, LineType.AntiAlias);
                }
                RobotList[i].IsTracked = false;
            }
#if DEBUG
            CvInvoke.PutText(Input, LargeContourCount.ToString(), new Point(20, 40), FontFace.HersheyScriptSimplex, 1, new MCvScalar(50, 50, 50), 2, LineType.AntiAlias);
            CvInvoke.PutText(Input, HexCount.ToString(), new Point(20, 80), FontFace.HersheyScriptSimplex, 1, new MCvScalar(50, 50, 50), 2, LineType.AntiAlias);
            CvInvoke.PutText(Input, RobotCount.ToString(), new Point(20, 120), FontFace.HersheyScriptSimplex, 1, new MCvScalar(50, 50, 50), 2, LineType.AntiAlias);
#endif
            OverlayImage = Input.Clone().GetUMat(AccessType.Read);
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
        private Point FindHeading(UMat Frame)
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
                CvInvoke.FindContours(Input, Contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
#if DEBUG
                test = Input.Clone();
#endif
            }
            //
            for (int i = 0; i < Contours.Size; i++)
            {
                Area = Math.Abs(CvInvoke.ContourArea(Contours[i]));
                // Remove high/low freq noise contours
                if (Area > 50 && Area < 500)
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
#if DEBUG         
            CvInvoke.DrawContours(test, ApproxContours, -1, new MCvScalar(255, 255, 255), 2);
#endif
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

            //LowerH = TargetColour.GetHue() / 2 - 10;
            //UpperH = TargetColour.GetHue() / 2 + 10;
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
        public bool GetRobots(UMat Frame)
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
                            Point RelativeHeading = FindHeading(RobotImage);
                            if (RelativeHeading.X > 0 && RelativeHeading.Y > 0)
                                RobotList[RobotID].Heading = new Point(RelativeHeading.X + RobotBounds.X, RelativeHeading.Y + RobotBounds.Y);
                            RobotList[RobotID].Location = new Point((int)RelativeRobotCOM.X + RobotBounds.X, (int)RelativeRobotCOM.Y + RobotBounds.Y);
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
            return true;
        }
        public void Dispose()
        {
            image.Dispose();
            OverlayImage.Dispose();
#if DEBUG
            test.Dispose();
#endif
        }
    }// CLASS END

}// NAMESPACE END
