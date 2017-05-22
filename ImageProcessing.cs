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

        public FilterType Filter { get; set; }
        public Mat OverlayImage { get; set; }

        #region
        // HSV ranges.
        public double LowerH = 0;
        public double UpperH = 255;
        public double LowerS = 0;
        public double UpperS = 255;
        public double LowerV = 0;
        public double UpperV = 255;
        // Blur, Canny, and Threshold values.
        private const int BlurC = 1;
        public double LowerC = 128;
        public double UpperC = 255;
        VectorOfVectorOfPoint mycontours = new VectorOfVectorOfPoint();
        VectorOfVectorOfPoint largecontours = new VectorOfVectorOfPoint();
        VectorOfVectorOfPoint approx = new VectorOfVectorOfPoint();
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
                    OverlayImage = ShapeRecognition(Frame);
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


        private Mat IdentifyRobot(Mat Visual, Mat Frame)
        {
            Mat Thresh = new Mat();
            Mat Test = new Mat();

            using (Mat VisualHsv = new Image<Hsv, byte>(Frame.Size).Mat)
            using (Mat mask = new Image<Gray,byte>(Frame.Size).Mat)
            using (Mat Black = new Image<Bgr, byte>(Frame.Size).Mat)
            using (Mat LowerHsv = new Image<Hsv, byte>(1, 1, new Hsv(LowerH, LowerS, LowerV)).Mat)
            using (Mat UpperHsv = new Image<Hsv, byte>(1, 1, new Hsv(UpperH, UpperS, UpperV)).Mat)
            {
                CvInvoke.CvtColor(Visual, mask, ColorConversion.Bgr2Gray);
                // TODO: Possibly an easier way to mask
                // TODO: Could use rectangle region to mask instead
                // HACK: Removes pixels outside the hexagon from the frame
                // This is done to focus colour detection to one robot
                CvInvoke.Subtract(Frame, Black, Thresh, mask);

                CvInvoke.CvtColor(Thresh, VisualHsv, ColorConversion.Bgr2Hsv);
                CvInvoke.Blur(VisualHsv, VisualHsv, new Size(1, 1), new Point(0, 0));
                CvInvoke.InRange(VisualHsv, LowerHsv, UpperHsv, Thresh);
            }
            CvInvoke.CvtColor(Thresh, Thresh, ColorConversion.Gray2Bgr);
            return Thresh;
        }

        public Mat ShapeRecognition(Mat Frame)
        {
            Mat Out = Frame.Clone();
            CvInvoke.CvtColor(Out, Out, ColorConversion.Bgr2Gray);
            CvInvoke.Threshold(Out, Out, LowerC, UpperC, ThresholdType.Binary);
            CvInvoke.AdaptiveThreshold(Out, Out, UpperC, AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 3, 0);
            CvInvoke.FindContours(Out, mycontours, null, RetrType.External, ChainApproxMethod.ChainApproxNone);

            double area;
            Mat Visual = new Image<Bgr,byte>(Frame.Size).Mat;
            //
            largecontours.Clear();
            //
            for (int i = 0; i < mycontours.Size; i++)
            {
                area = Math.Abs(CvInvoke.ContourArea(mycontours[i]));
                // Remove noise contours
                if (area > 2000 && area <= 100000)
                {
                    largecontours.Push(mycontours[i]);
                }
            }
            approx.Push(largecontours);
            for (int i = 0; i < largecontours.Size; i++)
            {
                // Get approximate shape of contours
                CvInvoke.ApproxPolyDP(largecontours[i], approx[i], 4.0, true);
                //
                if (IsHexagon(approx[i]))
                {               
                    // Creates mask of current robot
                    CvInvoke.DrawContours(Visual, approx, i, new MCvScalar(255, 255, 255), -1);
                    // TEMP: Returns threshold value to see if colours are found/correct
                    Visual = IdentifyRobot(Visual.Clone(), Frame.Clone());

                    // Draws a circle in the center
                    //MCvScalar HexCenter = CvInvoke.Mean(approx[i]);
                    //CvInvoke.Circle(Visual, new Point((int)HexCenter.V0, (int)HexCenter.V1), 5, new MCvScalar(100, 0, 0), -1);
                }
            }
            return Visual;
        }
    }
}
