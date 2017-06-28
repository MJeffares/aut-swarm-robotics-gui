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
        public Mat OverlayImage { get; set; }

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
            OverlayImage = new Mat();
            Filter = FilterType.NONE;
            // BRAE: Robots are currently initialized in the ImageProcessing class?
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

        public void ProcessFilter(Mat Frame)
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
                    Mat image = CvInvoke.Imread("D:/ColourCodes.png");
                    ShapeRecognition(image);
                    DrawOverlay(image);
                    break;
                default:
                    break;
            }
        }
        private void DrawOverlay(Mat Frame)
        {
            OverlayImage = new Image<Bgr, byte>(Frame.Width, Frame.Height).Mat;

            // Creates mask of current robot
            using (VectorOfVectorOfPoint ContourVect = new VectorOfVectorOfPoint())
            {
                for (int i = 0; i < RobotList.Length; i++)
                {
                    if (RobotList[i].RobotContour != null)
                    {

                        ContourVect.Push(new VectorOfPoint(RobotList[i].RobotContour));
                    }
                }
                CvInvoke.DrawContours(OverlayImage, ContourVect, -1, new MCvScalar(255, 255, 255), -1);
            }

            for (int i = 0; i < RobotList.Length; i++)
            {
                if (!RobotList[i].IsTracked)
                {
                    // Red indicates robot is not currently tracked, the image will be its previous location
                    CvInvoke.Circle(OverlayImage, RobotList[i].Location, 10, new MCvScalar(0, 0, 255), -1);
                }
                else
                {
                    // Green indicates new image
                    CvInvoke.Circle(OverlayImage, RobotList[i].Location, 10, new MCvScalar(0, 255, 0), -1);
                }                
                CvInvoke.PutText(OverlayImage, i.ToString(), RobotList[i].Location, FontFace.HersheyPlain, 10, new MCvScalar(50, 50, 50), 2);
                RobotList[i].IsTracked = false;
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
        private int IdentifyRobot(VectorOfPoint Contour, Mat Frame)
        {
            bool IsRed, IsYellow, IsGreen, IsCyan, IsBlue, IsMagenta;          

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
                IsRed = ColourRecognition(Out, Color.Red);
                IsYellow = ColourRecognition(Out, Color.Yellow);
                IsGreen = ColourRecognition(Out, Color.Green);
                IsCyan = ColourRecognition(Out, Color.Cyan);
                IsBlue = ColourRecognition(Out, Color.Blue);
                IsMagenta = ColourRecognition(Out, Color.Magenta);  
            }

            if (IsRed && IsYellow && IsGreen) RobotID = 0;

            if (IsRed && IsYellow && IsCyan) RobotID = 1;

            if (IsRed && IsYellow && IsBlue) RobotID = 2;

            if (IsRed && IsYellow && IsMagenta) RobotID = 3;

            if (IsRed && IsGreen && IsBlue) RobotID = 4;

            if (IsRed && IsGreen && IsMagenta) RobotID = 5;

            return RobotID;
        }


        public bool ColourRecognition(Mat Frame, Color TargetColour)
        {
            int Count;
            double LowerH = 0;
            double UpperH = 0;

            LowerH = TargetColour.GetHue() / 2 - 10;
            UpperH = TargetColour.GetHue() / 2 + 10;

            using (Mat Out = Frame.Clone())
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

        public bool ShapeRecognition(Mat Frame)
        {
            VectorOfVectorOfPoint Contours = new VectorOfVectorOfPoint();
            VectorOfVectorOfPoint LargeContours = new VectorOfVectorOfPoint();
            VectorOfVectorOfPoint ApproxContours = new VectorOfVectorOfPoint();
            double Area;

            using (Mat Input = Frame.Clone())
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
                    using (Mat RobotImage = new Mat(Frame, RobotBounds))
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
                        VectorOfPoint RelativeContour = new VectorOfPoint(RelativePoints);
                        // Returns robot ID based on colour code
                        RobotID = IdentifyRobot(RelativeContour, RobotImage.Clone());
                    }
                    if (RobotID != -1)
                    {
                        MCvMoments RobotMoment = CvInvoke.Moments(ApproxContours[i]);
                        MCvPoint2D64f RobotCOM = RobotMoment.GravityCenter;
                        RobotList[RobotID].Location = new Point((int)RobotCOM.X, (int)RobotCOM.Y);
                        RobotList[RobotID].IsTracked = true;
                        RobotList[RobotID].RobotContour = ApproxContours[i].ToArray();
                    }
                }
            }
            return true;
        }

    }// CLASS END

}// NAMESPACE END
