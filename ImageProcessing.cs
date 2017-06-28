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

        #region
        // Blur, Canny, and Threshold values.
        public double LowerC { get; set; }
        public double UpperC { get; set; } = 255;
        public int ColourC { get; set; }
        public int LowerH { get; set; }
        public int LowerS { get; set; }
        public int LowerV { get; set; }
        public int UpperH { get; set; }
        public int UpperS { get; set; }
        public int UpperV { get; set; }

        private Robot[] RobotList = new Robot[6];
        #endregion

        public ImageProcessing()
        {
            OverlayImage = new UMat();
            Filter = FilterType.NONE;
            // BRAE: Robots are currently initialized in the ImageProcessing class
            for (int i = 0; i < RobotList.Length; i++)
            {
                RobotList[i] = new Robot();
            }
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
                    UMat image = CvInvoke.Imread("...\\...\\Images\\ColourCodes.png").GetUMat(AccessType.Read);
                    GetRobots(image);
                    DrawOverlay(image);
                    break;
                default:
                    break;
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
                CvInvoke.ArrowedLine(Input, RobotList[i].Location, RobotList[i].Heading, new MCvScalar(20, 20, 20), 2, LineType.AntiAlias, 0, 0.5);
                CvInvoke.PutText(Input, i.ToString(), new Point(RobotList[i].Location.X + 20, RobotList[i].Location.Y + 10), FontFace.HersheyScriptSimplex, 1, new MCvScalar(50, 50, 50), 2, LineType.AntiAlias);
                RobotList[i].IsTracked = false;
            }
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
            bool IsRed, IsYellow, IsGreen, IsCyan, IsBlue, IsMagenta;          

            int RobotID = -1;

            using (UMat Mask = new Image<Gray, byte>(Frame.Size).Mat.GetUMat(AccessType.Read))
            using (UMat Out = new Image<Bgr, byte>(Frame.Size).Mat.GetUMat(AccessType.Read))
            using (VectorOfVectorOfPoint ContourVect = new VectorOfVectorOfPoint())
            {
                ContourVect.Push(Contour);
                CvInvoke.DrawContours(Mask, ContourVect, 0, new MCvScalar(255, 255, 255), -1);
                // This is done to focus colour detection to one robot
                CvInvoke.Subtract(Frame, Out, Out, Mask);
                // Look for colours on the robot
                IsRed = FindColour(Out, Color.Red);
                IsYellow = FindColour(Out, Color.Yellow);
                IsGreen = FindColour(Out, Color.Green);
                IsCyan = FindColour(Out, Color.Cyan);
                IsBlue = FindColour(Out, Color.Blue);
                IsMagenta = FindColour(Out, Color.Magenta);  
            }

            if (IsRed && IsYellow && IsGreen) RobotID = 0;

            if (IsRed && IsYellow && IsCyan) RobotID = 1;

            if (IsRed && IsYellow && IsBlue) RobotID = 2;

            if (IsRed && IsYellow && IsMagenta) RobotID = 3;

            if (IsRed && IsGreen && IsBlue) RobotID = 4;

            if (IsRed && IsGreen && IsMagenta) RobotID = 5;

            return RobotID;
        }
        private MCvPoint2D64f FindDirection(MCvPoint2D64f RobotLocation, UMat Frame)
        {
            MCvPoint2D64f TriangleCOM = new MCvPoint2D64f();
            MCvMoments TriangleMoment = new MCvMoments();
            double angle = 0;
            VectorOfVectorOfPoint Contours = new VectorOfVectorOfPoint();
            VectorOfVectorOfPoint LargeContours = new VectorOfVectorOfPoint();
            VectorOfVectorOfPoint ApproxContours = new VectorOfVectorOfPoint();
            double Area;

            using (UMat Input = Frame.Clone())
            {
                CvInvoke.CvtColor(Input, Input, ColorConversion.Bgr2Gray);
                //CvInvoke.Blur(Input, Input, new Size(4, 4), new Point(0, 0));
                //CvInvoke.Threshold(Input, Input, LowerC, UpperC, ThresholdType.Binary);
                CvInvoke.AdaptiveThreshold(Input, Input, UpperC, AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 3, 0);
                CvInvoke.FindContours(Input, Contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
            }
            //
            for (int i = 0; i < Contours.Size; i++)
            {
                Area = Math.Abs(CvInvoke.ContourArea(Contours[i]));
                // Remove high/low freq noise contours
                if (Area > 5 && Area <= 500)
                {
                    LargeContours.Push(Contours[i]);
                }
            }
            ApproxContours.Push(LargeContours);

            for (int i = 0; i < LargeContours.Size; i++)
            {
                // Get approximate polygonal shape of contour
                CvInvoke.ApproxPolyDP(LargeContours[i], ApproxContours[i], 3.0, true);
                // Check if contour is the right shape (hexagon)
                if (IsEquilTriangle(ApproxContours[i]))
                {
                    TriangleMoment = CvInvoke.Moments(ApproxContours[i]);
                    TriangleCOM = TriangleMoment.GravityCenter;

                    int xDiff = (int)RobotLocation.X - (int)TriangleCOM.X; 
                    int yDiff = (int)RobotLocation.Y - (int)TriangleCOM.Y;
                    angle = Math.Atan2(yDiff, xDiff) * 180 / Math.PI;
                }
            }
            return TriangleMoment.GravityCenter;
            //return angle;
        }
        public bool FindColour(UMat Frame, Color TargetColour)
        {
            int Count;
            double LowerH = 0;
            double UpperH = 0;

            LowerH = TargetColour.GetHue() / 2 - 10;
            UpperH = TargetColour.GetHue() / 2 + 10;

            using (UMat Out = Frame.Clone())
            using (Mat GrayOut = new Image<Gray, byte>(Frame.Size).Mat)
            {
                CvInvoke.CvtColor(Out, Out, ColorConversion.Bgr2Hsv);
                CvInvoke.Blur(Out, Out, new Size(1, 1), new Point(0, 0));

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
                    CvInvoke.Threshold(SOut, SOut, 10, 255, ThresholdType.Binary);
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
                CvInvoke.Blur(Input, Input, new Size(1, 1), new Point(0, 0));
                //CvInvoke.Threshold(Input, Input, LowerC, UpperC, ThresholdType.Binary);
                CvInvoke.AdaptiveThreshold(Input, Input, UpperC, AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 3, 0);
                CvInvoke.FindContours(Input, Contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
            }

            //
            for (int i = 0; i < Contours.Size; i++)
            {
                Area = Math.Abs(CvInvoke.ContourArea(Contours[i]));
                // Remove high/low freq noise contours
                if (Area > 500 && Area <= 100000)
                {
                    LargeContours.Push(Contours[i]);
                }
            }
            ApproxContours.Push(LargeContours);

            for (int i = 0; i < LargeContours.Size; i++)
            {
                // Get approximate polygonal shape of contour
                CvInvoke.ApproxPolyDP(LargeContours[i], ApproxContours[i], 3.0, true);
                // Check if contour is the right shape (hexagon)
                if (IsHexagon(ApproxContours[i]))
                {
                    int RobotID;
                    // Rectangular region that encompasses the contour
                    Rectangle RobotBounds = CvInvoke.BoundingRectangle(ApproxContours[i]);
                    // Create an image from the frame cropped down to the contour region
                    using (UMat RobotImage = new UMat(Frame, RobotBounds))
                    {
                        // New contour points relative to cropped region
                        Point[] RelativePoints = new Point[ApproxContours[i].Size];
                        // Old contour points relative to original frame
                        Point[] AbsolutePoints = ApproxContours[i].ToArray();
                        // Reevaluate the contour points based on the new image coordinates
                        for (int j = 0; j < ApproxContours[i].Size; j++)
                        {
                            RelativePoints[j].X = AbsolutePoints[j].X - RobotBounds.X;
                            RelativePoints[j].Y = AbsolutePoints[j].Y - RobotBounds.Y;
                        }
                        MCvMoments RelativeRobotMoment = CvInvoke.Moments(new VectorOfPoint(RelativePoints));
                        MCvPoint2D64f RelativeRobotCOM = RelativeRobotMoment.GravityCenter;
                        
                        // Returns robot ID based on colour code
                        RobotID = IdentifyRobot(new VectorOfPoint(RelativePoints), RobotImage);

                        if (RobotID != -1)
                        {
                            MCvPoint2D64f RelativeHeading = FindDirection(RelativeRobotCOM, RobotImage);
                            RobotList[RobotID].Heading = new Point((int)RelativeHeading.X + RobotBounds.X, (int)RelativeHeading.Y + RobotBounds.Y);
                            RobotList[RobotID].Location = new Point((int)RelativeRobotCOM.X + RobotBounds.X, (int)RelativeRobotCOM.Y + RobotBounds.Y);
                            RobotList[RobotID].Contour = ApproxContours[i].ToArray();
                            RobotList[RobotID].IsTracked = true;
                        }
                    }
                }
            }
            return true;
        }

    }// CLASS END

}// NAMESPACE END
