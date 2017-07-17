using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmRoboticsGUI
{
    public class ImageDisplay
    {
        public enum OverlayType { NONE, DEBUG, PRETTY, INFO, GRID, TEST, NUM_OVERLAYS };

        public OverlayType Overlay { get; set; }

        public UMat Image { get; set; }

        public UMat testImage { get; private set; }
        public double testAngle { get; set; }

        public ImageDisplay()
        {
            Image = new UMat();
            testImage = CvInvoke.Imread("...\\...\\Brae\\Images\\robotcutouts2.png").GetUMat(AccessType.Read);
            CvInvoke.Resize(testImage, testImage, new Size(640, 480));
        }

        public static string ToString(OverlayType Overlay)
        {
            switch (Overlay)
            {
                case OverlayType.NONE:
                    return string.Format("No Overlay");
                case OverlayType.DEBUG:
                    return string.Format("Debugging");
                case OverlayType.PRETTY:
                    return string.Format("Pretty");
                case OverlayType.INFO:
                    return string.Format("Information");
                case OverlayType.GRID:
                    return string.Format("Grid");
                case OverlayType.TEST:
                    return string.Format("Test Image");
                default:
                    return string.Format("Overlay Text Error");
            }
        }
        public void ProcessOverlay(UMat Frame, Robot[] RobotList)
        {

            switch (Overlay)
            {
                case OverlayType.NONE:
                    Image = Frame;
                    break;
                case OverlayType.DEBUG:
                    DrawDebugOverlay(testImage, RobotList);
                    break;
                case OverlayType.PRETTY:
                    DrawPrettyOverlay(testImage, RobotList);
                    break;
                case OverlayType.INFO:
                    break;
                case OverlayType.GRID:
                    break;
                case OverlayType.TEST:
                    break;
                default:
                    break;
            }
        }

        private void DrawPrettyOverlay(UMat Frame, Robot[] RobotList)
        {
            Mat Input = new Image<Bgr, byte>(Frame.Cols, Frame.Rows).Mat;
            for (int i = 0; i < RobotList.Length; i++)
            {
                if (RobotList[i].Location.X > 0 && RobotList[i].Location.Y > 0)
                {
                    DrawHexagon(Input, RobotList[i].Location, 50, RobotList[i].Heading);
                    
                }
                if (RobotList[i].Location.X > 0 && RobotList[i].Location.Y > 0)
                {
                    DrawArrow(Input, RobotList[i].Location, RobotList[i].Heading);
                }
            }
            DrawHexagon(Input, new Point(200, 200), 100, testAngle * Math.PI / 180);
            DrawArrow(Input, new Point(200, 200), testAngle * Math.PI / 180);
            
            Image = Input.Clone().GetUMat(AccessType.Read);
        }

        private void DrawDebugOverlay(UMat Frame, Robot[] RobotList)
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
                    if (RobotList[i].DirectionMarker.X > 0 && RobotList[i].DirectionMarker.Y > 0)
                    {
                        CvInvoke.ArrowedLine(Input, RobotList[i].Location, RobotList[i].DirectionMarker, new MCvScalar(20, 20, 20), 2, LineType.AntiAlias, 0, 0.5);
                    }
                    CvInvoke.PutText(Input, i.ToString(), new Point(RobotList[i].Location.X + 20, RobotList[i].Location.Y + 10), FontFace.HersheyScriptSimplex, 1, new MCvScalar(50, 50, 50), 2, LineType.AntiAlias);
                }
                RobotList[i].IsTracked = false;
            }
            //CvInvoke.PutText(Input, LargeContourCount.ToString(), new Point(20, 40), FontFace.HersheyScriptSimplex, 1, new MCvScalar(50, 50, 50), 2, LineType.AntiAlias);
            //CvInvoke.PutText(Input, HexCount.ToString(), new Point(20, 80), FontFace.HersheyScriptSimplex, 1, new MCvScalar(50, 50, 50), 2, LineType.AntiAlias);
            //CvInvoke.PutText(Input, RobotCount.ToString(), new Point(20, 120), FontFace.HersheyScriptSimplex, 1, new MCvScalar(50, 50, 50), 2, LineType.AntiAlias);
            Image = Input.Clone().GetUMat(AccessType.Read);
        }

        private void DrawHexagon(IInputOutputArray img, Point center, int radius, double angle)
        {
            var shape = new Point[6];

            //Create 6 points
            for (int i = 0; i < 6; i++)
            {
                shape[i] = new Point(
                  (int)(center.X + radius * Math.Cos(i * 60 * Math.PI / 180 + (angle + Math.PI / 6))),
                  (int)(center.Y + radius * Math.Sin(i * 60 * Math.PI / 180 + (angle + Math.PI / 6))));
            }

            using (VectorOfVectorOfPoint ContourVect = new VectorOfVectorOfPoint())
            {
                ContourVect.Push(new VectorOfPoint(shape));
                CvInvoke.DrawContours(img, ContourVect, -1, new MCvScalar(255, 255, 255), -1, LineType.AntiAlias);
            }
        }

        private void DrawArrow(IInputOutputArray img, Point p1, double angle)
        {
            Point p2 = new Point();
            const int length = 30;
            p2.X = (int)(length * Math.Cos(angle) + p1.X);
            p2.Y = (int)(length * Math.Sin(angle) + p1.Y);

            CvInvoke.ArrowedLine(img, p1, p2, new MCvScalar(20, 20, 20), 2, LineType.AntiAlias, 0, 0.5);
        }
    }
}
