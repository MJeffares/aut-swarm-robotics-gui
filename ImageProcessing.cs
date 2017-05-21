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
        public double UpperH = 0;
        public double LowerS = 0;
        public double UpperS = 0;
        public double LowerV = 0;
        public double UpperV = 0;
        // Blur, Canny, and Threshold values.
        private const int BlurC = 1;
        public int LowerC = 128;
        public int UpperC = 255;
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

            using (Mat VisualHsv = new Mat())
            using (Mat mask = Visual.Clone())
            using (Mat LowerHsv = new Image<Hsv, byte>(1, 1, new Hsv(LowerH, LowerS, LowerV)).Mat)
            using (Mat UpperHsv = new Image<Hsv, byte>(1, 1, new Hsv(UpperH, UpperS, UpperV)).Mat)
            {
                CvInvoke.CvtColor(mask, mask, ColorConversion.Bgr2Gray);
                CvInvoke.Subtract(Frame, Visual, Visual, mask);
                CvInvoke.CvtColor(Visual, VisualHsv, ColorConversion.Bgr2Hsv);
                CvInvoke.Blur(VisualHsv, VisualHsv, new Size(1, 1), new Point(0, 0));
                CvInvoke.InRange(VisualHsv, LowerHsv, UpperHsv, Thresh);
            }
            if (Thresh != null)
            {
                return Thresh;
            }
            else
                return Visual;
        }

        public Mat ShapeRecognition(Mat Frame)
        {
            Mat Out = new Mat();
            CvInvoke.CvtColor(Frame, Out, ColorConversion.Bgr2Gray);
            CvInvoke.Threshold(Out, Out, LowerC, UpperC, ThresholdType.Binary);
            CvInvoke.AdaptiveThreshold(Out, Out, UpperC, AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 3, 0);
            CvInvoke.FindContours(Out, mycontours, null, RetrType.External, ChainApproxMethod.ChainApproxNone);

            double area;
            // TODO: Need to find a way to create blank image w/o causing graphical issues
            // TEMP: Clones Frame then covers image with black rectancle
            Mat Visual = Frame.Clone();
            CvInvoke.Rectangle(Visual, new Rectangle(0, 0, 640, 480), new MCvScalar(0, 0, 0), -1);
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
                    MCvScalar HexCenter = CvInvoke.Mean(approx[i]);
                    CvInvoke.DrawContours(Visual, approx, i, new MCvScalar(1, 1, 1), -1);
                    Visual = IdentifyRobot(Visual, Frame);
                    //CvInvoke.Circle(Visual, new Point((int)HexCenter.V0, (int)HexCenter.V1), 5, new MCvScalar(100, 0, 0), -1);
                }
            }
            return Visual;
        }
    }
}
