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
        public int UpperH = 255;

        private VectorOfVectorOfPoint mycontours = new VectorOfVectorOfPoint();
        private VectorOfVectorOfPoint largecontours = new VectorOfVectorOfPoint();
        private VectorOfVectorOfPoint approx = new VectorOfVectorOfPoint();

        private Robot[] RobotList = new Robot[6];


        #endregion

        public ImageProcessing()
        {
            OverlayImage = new Mat();
            Filter = FilterType.NONE;

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
                    ShapeRecognition(Frame);
                    DrawOverlay(Frame);
                    break;
                default:
                    break;
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
                    if (angle < 40 || angle > 80)
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
        private int IdentifyRobot(int ContourID, Mat Frame)
        {
            // TEMP: detected the red robot
            bool IsRed;
            int RobotID = -1;

            using (Mat Mask = new Image<Gray, byte>(Frame.Size).Mat)
            using (Mat Out = new Image<Bgr, byte>(Frame.Size).Mat)
            {
                CvInvoke.DrawContours(Mask, approx, ContourID, new MCvScalar(255, 255, 255), -1);
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
            //return RobotID;
            return RobotID;
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

        public bool ColourRecognition(Mat Frame, Colour TargetColour)
        {
            Mat Out = Frame.Clone();
            Hsv Lower = new Hsv(1, 1, 1);
            Hsv Upper = new Hsv(254, 254, 254);
            int Count;       
            // BRAE: do other colours
            // could just use hsv values instead of an enum
            // then this wouldnt need to know corresponding hue values
            if (TargetColour == Colour.RED)
            {
                Lower.Hue = LowerH;
                Upper.Hue = UpperH;
            }   
                    
            using (Mat LowerHsv = new Image<Hsv, byte>(1, 1, Lower).Mat)
            using (Mat UpperHsv = new Image<Hsv, byte>(1, 1, Upper).Mat)
            {
                CvInvoke.CvtColor(Out, Out, ColorConversion.Bgr2Hsv);
                CvInvoke.Blur(Out, Out, new Size(1, 1), new Point(0, 0));
                CvInvoke.InRange(Out, LowerHsv, UpperHsv, Out);
                Count = CvInvoke.CountNonZero(Out);
            }
            if (Count > ColourCount)
            {
                return true;
            }
            return false;
        }

        public bool ShapeRecognition(Mat Frame)
        {
            
            using (Mat Input = Frame.Clone())
            {
                CvInvoke.CvtColor(Input, Input, ColorConversion.Bgr2Gray);
                //CvInvoke.Threshold(Input, Input, LowerC, UpperC, ThresholdType.Binary);
                CvInvoke.AdaptiveThreshold(Input, Input, UpperC, AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 3, 0);
                CvInvoke.FindContours(Input, mycontours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
            }

            double Area;
            //
            largecontours.Clear();
            approx.Clear();
            //
            for (int i = 0; i < mycontours.Size; i++)
            {
                Area = Math.Abs(CvInvoke.ContourArea(mycontours[i]));
                // Remove high/low freq noise contours
                if (Area > 2000 && Area <= 100000)
                {
                    largecontours.Push(mycontours[i]);
                }
            }
            approx.Push(largecontours);
            for (int i = 0; i < largecontours.Size; i++)
            {
                // Get approximate shape of contours
                CvInvoke.ApproxPolyDP(largecontours[i], approx[i], 2.0, true);
                //
                if (IsHexagon(approx[i]))
                {
                    // Returns robot ID based on colour code
                    int RobotID = IdentifyRobot(i, Frame.Clone());

                    if (!RobotList[RobotID].hasImage)
                    {
                        RobotList[RobotID].RobotImage = new Image<Bgr, byte>(Frame.Width, Frame.Height).Mat;
                        RobotList[RobotID].hasImage = true;
                    }                   
                    if (RobotID != -1)
                    {
                        RobotList[RobotID].Tracked = true;
                        MCvMoments RobotMoment = CvInvoke.Moments(approx[i]);
                        MCvPoint2D64f RobotCOM = RobotMoment.GravityCenter;
                        RobotList[RobotID].Location = new Point((int)RobotCOM.X, (int)RobotCOM.Y);

                        using (Mat RobotImage = new Image<Bgr, byte>(Frame.Width, Frame.Height).Mat)
                        {
                            CvInvoke.DrawContours(RobotImage, approx, i, new MCvScalar(255, 255, 255), -1);
                            CvInvoke.PutText(RobotImage, RobotID.ToString(), RobotList[RobotID].Location, FontFace.HersheyPlain, 10, new MCvScalar(50, 50, 50), 2);
                            RobotList[RobotID].RobotImage = RobotImage.Clone();
                        }            
                    }
                }
            }
            return true;
        }
    }
}
