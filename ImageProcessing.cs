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
        public double LowerC = 128;
        public double UpperC = 255;
        public int ColourCount = 100;
        public int LowerH = 0;
        public int LowerS = 0;
        public int LowerV = 0;
        public int UpperH = 255;
        public int UpperS = 255;
        public int UpperV = 255;

        private Mat test = new Mat();

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
                    Mat image = CvInvoke.Imread("D:/testimg.png");
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
            for (int i = 0; i < RobotList.Length; i++)
            {
                if (RobotList[i].RobotImage != null)
                {
                    CvInvoke.Add(OverlayImage, RobotList[i].RobotImage, OverlayImage);

                    if (!RobotList[i].Tracked)
                    {
                        // Red indicates robot image is unchanged
                        CvInvoke.Circle(OverlayImage, RobotList[i].Location, 10, new MCvScalar(0, 0, 255), -1);
                    }
                    else
                    {
                        // Green indicates new image
                        CvInvoke.Circle(OverlayImage, RobotList[i].Location, 10, new MCvScalar(0, 255, 0), -1);
                        RobotList[i].Tracked = false;
                    }
                }
            }
        }

        private bool IsHexagon(VectorOfPoint Contour)
        {
            if (Contour.Size == 6)
            {
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
            return false;
        }
        private int IdentifyRobot(VectorOfPoint Contour, Mat Frame)
        {
            // TEMP: detected the red robot
            bool IsRed;
            int RobotID = -1;

            using (Mat Mask = new Image<Gray, byte>(Frame.Size).Mat)
            using (Mat Out = new Image<Bgr, byte>(Frame.Size).Mat)
            using (VectorOfVectorOfPoint ContourVect = new VectorOfVectorOfPoint())
            {
                ContourVect.Push(Contour);
                CvInvoke.DrawContours(Mask, ContourVect, 0, new MCvScalar(255, 255, 255), -1);
                // This is done to focus colour detection to one robot
                CvInvoke.Subtract(Frame, Out, Out, Mask);

                IsRed = ColourRecognition(Out, Colour.RED);
            }

            if (IsRed)
            {
                RobotID = 0;
            }
            else
            {
                RobotID = 1;
            }
            return RobotID;
        }


        public bool ColourRecognition(Mat Frame, Colour TargetColour)
        {
            Mat Out = Frame.Clone();
            Hsv Lower = new Hsv();
            Hsv Upper = new Hsv();
            int Count;
            // BRAE: do other colours
            // could just use hsv values instead of an enum
            // then this wouldnt need to know corresponding hue values
            if (TargetColour == Colour.RED)
            {
                Lower.Hue = (double)LowerH;
                Lower.Satuation = (double)LowerS;
                Lower.Value = (double)LowerV;
                Upper.Hue = (double)UpperH;
                Upper.Satuation = (double)UpperS;
                Upper.Value = (double)UpperV;
            }

            using (var LowerHsv = new Image<Hsv, byte>(1, 1, Lower))
            using (var UpperHsv = new Image<Hsv, byte>(1, 1, Upper))
            using (Mat GrayOut = new Image<Gray, byte>(Frame.Size).Mat)
            using (Mat HsvOut = new Image<Gray, byte>(Frame.Size).Mat)
            {

                CvInvoke.CvtColor(Out, HsvOut, ColorConversion.Bgr2Hsv);

                CvInvoke.Blur(HsvOut, HsvOut, new Size(1, 1), new Point(0, 0));
                CvInvoke.InRange(HsvOut, LowerHsv, UpperHsv, GrayOut);

                Count = CvInvoke.CountNonZero(GrayOut);
                test = GrayOut.Clone();
                CvInvoke.CvtColor(test, test, ColorConversion.Gray2Bgr);
            }
            if (Count > ColourCount)
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

            using (Mat Input = Frame.Clone())
            {
                CvInvoke.CvtColor(Input, Input, ColorConversion.Bgr2Gray);
                CvInvoke.Blur(Input, Input, new Size(1, 1), new Point(0, 0));
                //CvInvoke.Threshold(Input, Input, LowerC, UpperC, ThresholdType.Binary);
                CvInvoke.AdaptiveThreshold(Input, Input, UpperC, AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 3, 0);
                CvInvoke.FindContours(Input, Contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
            }

            double Area;
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
                // Get approximate shape of contours
                CvInvoke.ApproxPolyDP(LargeContours[i], ApproxContours[i], 3.0, true);
                //
                if (IsHexagon(ApproxContours[i]))
                {
                    // Returns robot ID based on colour code
                    int RobotID = IdentifyRobot(ApproxContours[i], Frame.Clone());

                    if (!RobotList[i].HasImage)
                    {
                        RobotList[i].RobotImage = new Image<Bgr, byte>(Frame.Width, Frame.Height).Mat;
                        RobotList[i].HasImage = true;
                    }
                    if (RobotID != -1)
                    {
                        MCvMoments RobotMoment = CvInvoke.Moments(ApproxContours[i]);
                        MCvPoint2D64f RobotCOM = RobotMoment.GravityCenter;
                        RobotList[i].Location = new Point((int)RobotCOM.X, (int)RobotCOM.Y);
                        RobotList[i].Tracked = true;

                        using (Mat RobotImage = new Image<Bgr, byte>(Frame.Width, Frame.Height).Mat)
                        {
                            CvInvoke.DrawContours(RobotImage, ApproxContours, i, new MCvScalar(255, 255, 255), -1);
                            CvInvoke.PutText(RobotImage, i.ToString(), RobotList[i].Location, FontFace.HersheyPlain, 10, new MCvScalar(50, 50, 50), 2);
                            RobotList[i].RobotImage = test.Clone();//RobotImage.Clone();
                        }
                    }
                }
            }
            return true;
        }
    }
}
