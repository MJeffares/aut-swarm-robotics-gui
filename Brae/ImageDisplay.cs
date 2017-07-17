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
        // Enumerations
        #region
        public enum OverlayType { NONE, DEBUG, PRETTY, INFO, GRID, TEST, NUM_OVERLAYS };
        public enum SourceType { NONE, CAMERA, CUTOUTS, NUM_SOURCES };
        #endregion

        // Variable Declarations
        #region
        public OverlayType Overlay { get; set; }
        public SourceType Source { get; set; }

        public UMat Image { get; set; }

        private PointF CursorPosition { get; set; }

        public int width { get; private set; }
        public int height { get; private set; }
        public double widthScale { get; private set; }
        public double heightScale { get; private set; }
        #endregion

        public ImageDisplay()
        {
            Image = new UMat();

            width = 640;
            height = 480;
            widthScale = 1;
            heightScale = 1;
        }

        // ToString Overloads
        #region
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

        public static string ToString(SourceType Source)
        {
            switch (Source)
            {
                case SourceType.NONE:
                    return string.Format("No Source");
                case SourceType.CAMERA:
                    return string.Format("Camera");
                case SourceType.CUTOUTS:
                    return string.Format("Cutouts");
                default:
                    return string.Format("Source Text Error");
            }
        }
        #endregion

        // Methods
        #region
        public void ProcessOverlay(UMat Frame, Robot[] RobotList)
        {
            if (Frame != null)
            {
                switch (Overlay)
                {
                    case OverlayType.NONE:
                        Image = Frame;
                        break;
                    case OverlayType.DEBUG:
                        DrawDebugOverlay(Frame, RobotList);
                        break;
                    case OverlayType.PRETTY:
                        DrawPrettyOverlay(Frame, RobotList);
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
        }

        public void Resize(int width, int height)
        {

            double newWidthScale = (double)width / (double)this.width;
            double newHeightScale = (double)height / (double)this.height;
            //CursorPosition = new PointF((float)(CursorPosition.X * newWidthScale / widthScale), (float)(CursorPosition.Y * newHeightScale / heightScale));
            widthScale = newWidthScale;
            heightScale = newHeightScale;

        }
        public void Resize(double widthScale, double heightScale)
        {
            CursorPosition = new PointF((float)(CursorPosition.X * widthScale / this.widthScale), (float)(CursorPosition.Y * heightScale / this.heightScale));
            this.widthScale = widthScale;
            this.heightScale = heightScale;
        }
        public void Resize(double Scale)
        {
            CursorPosition = new PointF((float)(CursorPosition.X * Scale / this.widthScale), (float)(CursorPosition.Y * Scale / this.heightScale));
            this.widthScale = Scale;
            this.heightScale = Scale;
        }

        #endregion

        // Drawing Methods
        #region
        private void DrawPrettyOverlay(UMat Frame, Robot[] RobotList)
        {
            using (Mat Input = new Image<Bgr, byte>(Frame.Cols, Frame.Rows).Mat)
            {
                for (int i = 0; i < RobotList.Length; i++)
                {
                    if (RobotList[i].Location.X > 0 && RobotList[i].Location.Y > 0)
                    {
                        //Point ScaledRobotLocation = new Point((int)(RobotList[i].Location.X), (int)(RobotList[i].Location.Y));
                        //
                        VectorOfVectorOfPoint ContourVect = DrawHexagon(Input, RobotList[i].Location, (int)(50), RobotList[i].Heading);
                        //
                        double IsPointInContour = CvInvoke.PointPolygonTest(ContourVect[0], CursorPosition, false);
                        //
                        if (IsPointInContour >= 0)
                        {
                            RobotList[i].IsSelected = true;
                            CvInvoke.PutText(Input, i.ToString(), new Point(RobotList[i].Location.X + 20, RobotList[i].Location.Y + 0), FontFace.HersheyScriptSimplex, 0.5, new MCvScalar(50, 50, 50), 1, LineType.AntiAlias);
                            CvInvoke.PutText(Input, RobotList[i].Battery.ToString(), new Point(RobotList[i].Location.X + 20, RobotList[i].Location.Y + 20), FontFace.HersheyScriptSimplex, 0.5, new MCvScalar(50, 50, 50), 1, LineType.AntiAlias);
                        }
                        else
                        {
                            RobotList[i].IsSelected = false;
                        }
                    }
                }
                //DrawHexagon(Input, test, 10, 0);
                CvInvoke.Resize(Input, Input, new Size((int)(Input.Cols * widthScale), (int)(Input.Rows * heightScale)));
                Image = Input.Clone().GetUMat(AccessType.Read);

            }
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

        private VectorOfVectorOfPoint DrawHexagon(IInputOutputArray img, Point center, int radius, double angle)
        {
            var shape = new Point[6];

            //Create 6 points
            for (int i = 0; i < 6; i++)
            {
                shape[i] = new Point(
                  (int)(center.X + radius * Math.Cos(i * 60 * Math.PI / 180 + angle)),
                  (int)(center.Y + radius * Math.Sin(i * 60 * Math.PI / 180 + angle)));
            }
            //
            VectorOfVectorOfPoint ContourVect = new VectorOfVectorOfPoint();
            //
            ContourVect.Push(new VectorOfPoint(shape));
            //
            CvInvoke.DrawContours(img, ContourVect, -1, new MCvScalar(255, 255, 255), -1, LineType.AntiAlias);
            //
            return ContourVect;
        }
        #endregion

        // Input Events
        #region
        public void UserClick(System.Windows.Point pos, Robot[] RobotList)
        {
            CursorPosition = new PointF(Math.Abs((float)(pos.X)), Math.Abs((float)(pos.Y)));
        }
        #endregion
    }
}
