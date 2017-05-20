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

        #region
        // HSV ranges.
        private const int LowerH = 0;
        private const int UpperH = 255;
        private const int LowerS = 0;
        private const int UpperS = 255;
        private const int LowerV = 0;
        private const int UpperV = 255;
        // Blur, Canny, and Threshold values.
        private const int BlurC = 1;
        private const int LowerC = 128;
        private const int UpperC = 255;

        VectorOfVectorOfPoint mycontours = new VectorOfVectorOfPoint();
        VectorOfVectorOfPoint largecontours = new VectorOfVectorOfPoint();
        VectorOfVectorOfPoint approx = new VectorOfVectorOfPoint();
        #endregion

        public ImageProcessing()
        {
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


        public Mat ProcessFilter(Mat Frame)
        {
            switch (Filter)
            {
                case FilterType.NONE:
                    return Frame;
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
                    CvInvoke.CvtColor(Frame, Frame, ColorConversion.Bgr2Gray);
                    CvInvoke.Threshold(Frame, Frame, LowerC, UpperC, ThresholdType.Binary);
                    CvInvoke.AdaptiveThreshold(Frame, Frame, UpperC, AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 3, 0);
                    CvInvoke.FindContours(Frame, mycontours, null, RetrType.External, ChainApproxMethod.ChainApproxNone);
                    Frame = ShapeRecognition(Frame);
                    break;
                default:
                    break;
            }
            return Frame;
        }

        public Mat ShapeRecognition(Mat Frame)
        {
            double area;

            largecontours.Clear();

            for (int i = 0; i < mycontours.Size; i++)
            {
                area = Math.Abs(CvInvoke.ContourArea(mycontours[i]));
                if (area > 1000 && area <= 100000)
                {
                    largecontours.Push(mycontours[i]);
                }
            }
            approx.Push(largecontours);
            for (int i = 0; i < largecontours.Size; i++)
            {
                CvInvoke.ApproxPolyDP(largecontours[i], approx[i], 4.0, true);

                if (approx[i].Size == 4)
                {
                    bool isHexagon = true;
                    Point[] pts = approx[i].ToArray();
                    LineSegment2D[] edges = PointCollection.PolyLine(pts, true);

                    for (int j = 0; j < edges.Length; j++)
                    {
                        double angle = Math.Abs(edges[(j + 1) % edges.Length].GetExteriorAngleDegree(edges[j]));
                        if (angle < 40 || angle > 80)
                        {
                            isHexagon = false;
                        }
                    }

                    if (isHexagon)
                    {
                        CvInvoke.DrawContours(Frame, approx, i, new MCvScalar(255, 0, 0), 5);
                    }


                    Point a = approx[i][0];
                    Point b = approx[i][1];
                    Point c = approx[i][2];
                    Point d = approx[i][3];

                    //System.Drawing.Point center = (c.X - a.x, c.Y - a.Y);


                    CvInvoke.Circle(Frame, a, 10, new MCvScalar(255, 0, 0), 5, LineType.EightConnected, 0);
                    CvInvoke.Circle(Frame, b, 10, new MCvScalar(255, 0, 0), 5, LineType.EightConnected, 0);
                    CvInvoke.Circle(Frame, c, 10, new MCvScalar(255, 0, 0), 5, LineType.EightConnected, 0);
                    CvInvoke.Circle(Frame, d, 10, new MCvScalar(255, 0, 0), 5, LineType.EightConnected, 0);
                }
            }
            return Frame;
        }
    }
}
