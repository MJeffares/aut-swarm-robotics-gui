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
        VectorOfVectorOfPoint mycontours = new VectorOfVectorOfPoint();
        VectorOfVectorOfPoint largecontours = new VectorOfVectorOfPoint();
        VectorOfVectorOfPoint approx = new VectorOfVectorOfPoint();

        const int robotCount = 6;
        Robot[] RobotList = new Robot[robotCount];


        #endregion

        public ImageProcessing()
        {
            OverlayImage = new Mat();
            Filter = FilterType.NONE;
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
                    DrawOverlay();
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

            using (Mat Mask = new Image<Gray,byte>(Frame.Size).Mat)
            using (Mat Out = new Image<Bgr, byte>(Frame.Size).Mat)
            {
                CvInvoke.DrawContours(Mask, approx, ContourID, new MCvScalar(255, 255, 255), -1);
                // This is done to focus colour detection to one robot
                CvInvoke.Subtract(Frame, Out, Out, Mask);

                IsRed = ColourRecognition(Out, Colour.RED);
            }
            if (IsRed)
            {
                RobotID = 1;
            }
            //return RobotID;
            return 1;
        }
        private void DrawOverlay()
        {
            if (RobotList[0] != null)
            {
                OverlayImage = new Image<Gray, byte>(RobotList[0].RobotImage.Width, RobotList[0].RobotImage.Height).Mat;
            }
            // Creates mask of current robot
            for (int i = 0; i < RobotList.Length; i++)
            {
                if (RobotList[i] != null)
                {
                    
                    CvInvoke.Add(OverlayImage, RobotList[i].RobotImage, OverlayImage);
                }
            }
        }

        public bool ColourRecognition(Mat Frame, Colour TargetColour)
        {
            Mat Out = Frame.Clone();
            Hsv Lower = new Hsv(0, 0, 0);
            Hsv Upper = new Hsv(255, 255, 255);
            int Count;       
            // BRAE: other colours
            if (TargetColour == Colour.RED)
            {
                Lower.Hue = 160;
                Upper.Hue = 179;
            }   
                    
            using (Mat LowerHsv = new Image<Hsv, byte>(1, 1, Lower).Mat)
            using (Mat UpperHsv = new Image<Hsv, byte>(1, 1, Upper).Mat)
            {
                CvInvoke.CvtColor(Out, Out, ColorConversion.Bgr2Hsv);
                CvInvoke.Blur(Out, Out, new Size(1, 1), new Point(0, 0));
                CvInvoke.InRange(Out, LowerHsv, UpperHsv, Out);
                Count = CvInvoke.CountNonZero(Out);
            }
            if (Count > 100)
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
                CvInvoke.Threshold(Input, Input, LowerC, UpperC, ThresholdType.Binary);
                CvInvoke.AdaptiveThreshold(Input, Input, UpperC, AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 3, 0);
                CvInvoke.FindContours(Input, mycontours, null, RetrType.External, ChainApproxMethod.ChainApproxNone);
            }

            double Area;
            int CurrentRobot = 0;
            MCvMoments RobotMoment;
            MCvPoint2D64f RobotCOM;
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
                    RobotList[CurrentRobot] = new Robot();            
                    RobotList[CurrentRobot].RobotImage = new Image<Gray, byte>(Frame.Width, Frame.Height).Mat;
                    // Returns robot ID based on colour code
                    RobotList[CurrentRobot].ID = IdentifyRobot(i, Frame.Clone());
                    
                    if (RobotList[CurrentRobot].ID != -1)
                    {                       
                        RobotMoment = CvInvoke.Moments(approx[i]);
                        RobotCOM = RobotMoment.GravityCenter;
                        RobotList[CurrentRobot].Location = new Point((int)RobotCOM.X, (int)RobotCOM.Y);

                        using (Mat RobotImage = new Image<Gray, byte>(Frame.Width, Frame.Height).Mat)
                        {
                            CvInvoke.DrawContours(RobotImage, approx, i, new MCvScalar(255, 255, 255), -1);
                            CvInvoke.Circle(RobotImage, RobotList[CurrentRobot].Location, 10, new MCvScalar(100, 100, 100), -1);
                            RobotList[CurrentRobot].RobotImage = RobotImage.Clone();
                        }

                        CurrentRobot++;
                        
                    }
                }
            }
            return true;
        }
    }
}
