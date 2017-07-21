using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmRoboticsGUI
{
    public class ImageDisplay
    {
        #region Enumerations
        public enum OverlayType { NONE, DEBUG, PRETTY, INFO, GRID, TEST, NUM_OVERLAYS };
        public enum SourceType { NONE, CAMERA, CUTOUTS, NUM_SOURCES };
        #endregion

        #region Public Properties
        public OverlayType Overlay { get; set; }
        public SourceType Source { get; set; }
        public UMat Image { get; set; }
        #endregion

        #region Private Properties
        public int width { get; private set; }
        public int height { get; private set; }
        public int Swidth { get; private set; }
        public int Sheight { get; private set; }

        private PointF CursorPosition { get; set; }
        private ImageAnimation IA { get; set; }
        #endregion

        public ImageDisplay(int width, int height)
        {
            Image = new UMat();
            IA = new ImageAnimation(0, 0, 100, 200);
            IA.AnimationUpdate += new ImageAnimation.AnimationHandler(AnimateRectangle);
            this.width = width;
            this.height = height;
            Swidth = width;
            Sheight = height;
        }

        #region Public Methods
        // ToString Overloads
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
        // Event Methods
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
        public void Click(System.Windows.Point pos)
        {
            CursorPosition = new PointF(Math.Abs((float)(pos.X)), Math.Abs((float)(pos.Y)));
            RectWidth = 0;
            IA.Start();
        }
        // Resize
        public void Resize(int width, int height)
        {
            // Calculate scale factor using ratios
            float widthScale = width / (float)this.width;
            float heightScale = height / (float)this.height;
            // Scale cursor postion
            CursorPosition = new PointF((CursorPosition.X * widthScale), (CursorPosition.Y * heightScale));
            // Set new dimensions
            this.width = width;
            this.height = height;
        }
        #endregion

        #region Private Methods
        private void DrawRobot(IInputOutputArray img, Robot robot)
        {
            // Scale robot position due to resize
            Point ScaledRobotLocation = new Point((int)(robot.Location.X * width/Swidth), (int)(robot.Location.Y * height/Sheight));
            //
            float MaxScale = Math.Max((float)width / Swidth, (float)height / Sheight);

            // Draw Robots as hexagons
            VectorOfVectorOfPoint Contour = GetShapeContour(ScaledRobotLocation, 6, (int)(50 * MaxScale), robot.Heading + Math.PI / 6);
            CvInvoke.DrawContours(img, Contour, -1, new MCvScalar(200, 200, 200), -1, LineType.AntiAlias);

            float length = 30 * MaxScale;
            Point Direction = new Point(
                (int)(length * Math.Cos(robot.Heading) + ScaledRobotLocation.X),
                (int)(length * Math.Sin(robot.Heading) + ScaledRobotLocation.Y));
            Contour = GetShapeContour(Direction, 3, (int)(10 * MaxScale), robot.Heading);
            CvInvoke.DrawContours(img, Contour, -1, new MCvScalar(0, 0, 0), -1, LineType.AntiAlias);

            // Not selected
            robot.IsSelected = false;
        }

        private void DrawSelectedRobot(IInputOutputArray img, Robot robot)
        {
            // Scale robot position due to resize
            Point ScaledRobotLocation = new Point((int)(robot.Location.X * width/Swidth), (int)(robot.Location.Y * height/Sheight));
            //
            float MaxScale = Math.Max((float)width / Swidth, (float)height / Sheight);
            float MinScale = Math.Min((float)width / Swidth, (float)height / Sheight);

            // Information box position
            Point InfoBoxLocation = new Point(ScaledRobotLocation.X, ScaledRobotLocation.Y - (int)(40 * MaxScale));
            // Box to hold information about current robot
            CvInvoke.Rectangle(img, new Rectangle(InfoBoxLocation, new Size((int)(RectWidth * width / Swidth), (int)(80 * MaxScale))), new MCvScalar(100, 100, 100), -1);

            // Draw robots as hexagons
            VectorOfVectorOfPoint Contour = GetShapeContour(ScaledRobotLocation, 6, (int)(50 * MaxScale), robot.Heading + Math.PI / 6);
            CvInvoke.DrawContours(img, Contour, -1, new MCvScalar(255, 255, 255), -1, LineType.AntiAlias);
            // Draw robot direction indicator
            Point Direction = new Point(
                (int)(30 * MaxScale * Math.Cos(robot.Heading) + ScaledRobotLocation.X),
                (int)(30 * MaxScale * Math.Sin(robot.Heading) + ScaledRobotLocation.Y));
            Contour = GetShapeContour(Direction, 3, (int)(10 * MaxScale), robot.Heading);
            CvInvoke.DrawContours(img, Contour, -1, new MCvScalar(0, 0, 0), -1, LineType.AntiAlias);
            
            // Current robot is selected
            robot.IsSelected = true;
            // Draw robot information
            CvInvoke.PutText(img, robot.ID.ToString(), new Point(ScaledRobotLocation.X + 20, ScaledRobotLocation.Y + 0), FontFace.HersheyScriptSimplex, 0.5 * MinScale, new MCvScalar(50, 50, 50), 1, LineType.AntiAlias);
            CvInvoke.PutText(img, robot.Battery.ToString(), new Point(ScaledRobotLocation.X + 20, ScaledRobotLocation.Y + (int)(20 * MinScale)), FontFace.HersheyScriptSimplex, 0.5 * MinScale, new MCvScalar(50, 50, 50), 1, LineType.AntiAlias);
        }

        private void DrawLinearGradient(IInputOutputArray img, Point start, Point end, double angle, int colorStart, int colorEnd)
        {
            // Colour value gradient
            double grad = Math.Abs((double)colorEnd - colorStart) / (end.X - start.X);

            // BRAE: Allow for reverse gradients
            // BRAE: Use angle value

            for (int i = start.X + start.Y; i < end.X + end.Y;i++)
            {
                // Draw
                CvInvoke.Line(img, new Point(i, start.X), new Point(start.Y, i), new MCvScalar(i * grad, i * grad, i * grad), 1);
                CvInvoke.Line(img, new Point(i, end.X), new Point(end.Y, i), new MCvScalar(i * grad, i * grad, i * grad), 1);
            }
        }

        private void DrawRadialGradient(IInputOutputArray img, Point center, Point end, int colorStart, int colorEnd)
        {
            // BRAE: Do more efficient way at some point

            // Distance between center and end point
            double distance = Math.Sqrt(Math.Pow((end.X - center.X), 2) + Math.Pow((end.Y - center.Y), 2));
            // Colour value gradient
            double grad = Math.Abs(colorEnd - colorStart) / distance;

            // Start colour is larger than end
            if (Math.Sign(colorEnd - colorStart) < 0)
            {
                // Start from end point
                for (int i = 0; i < (int)distance; i++)
                {
                    CvInvoke.Circle(img, center, (int)distance - i, new MCvScalar(i * grad, i * grad, i * grad), 2);
                }
            }
            else
            {
                // Start from center
                for (int i = 0; i < (int)distance; i++)
                {
                    CvInvoke.Circle(img, center, i, new MCvScalar(i * grad, i * grad, i * grad), 2);
                }
            }
        }


        // Overlay Drawing
        private void DrawPrettyOverlay(UMat Frame, Robot[] RobotList)
        {
            float MaxScale = Math.Max((float)width / Swidth, (float)height / Sheight);
            
            using (Mat Out = new Image<Bgr, byte>(width, height).Mat)
            {
                //DrawLinearGradient(Out, new Point(0, 0), new Point(width, height), Math.PI/4, 100, 160);
                DrawRadialGradient(Out, new Point(width / 2, height / 2), new Point(width, height), 160, 100);


                for (int i = 0; i < RobotList.Length; i++)
                {
                    // Scale robot position due to resize
                    Point ScaledRobotLocation = new Point((int)(RobotList[i].Location.X * width/Swidth), (int)(RobotList[i].Location.Y * height/Sheight));
                    // Draw Robots as hexagons
                    VectorOfVectorOfPoint Contour = GetShapeContour(ScaledRobotLocation, 6, (int)(50 * MaxScale), RobotList[i].Heading + Math.PI / 6);
                    
                    double IsPointInContour = CvInvoke.PointPolygonTest(Contour[0], CursorPosition, false);
                    if (IsPointInContour >= 0)
                    {
                        DrawSelectedRobot(Out, RobotList[i]);
                    }
                    else
                    {
                        DrawRobot(Out, RobotList[i]);
                    }
                    // DEBUG: Draw valid mouse region
                    CvInvoke.DrawContours(Out, Contour, -1, new MCvScalar(0, 255, 0), 1, LineType.AntiAlias);
                    // DEBUG: draw circle at cursor position
                    CvInvoke.Circle(Out, Point.Round(CursorPosition), 5, new MCvScalar(0, 0, 255), -1);
                }
                //
                Image = Out.Clone().GetUMat(AccessType.Read);
            }
        }

        // TEMP:
        private int RectWidth { get; set; }
        private void AnimateRectangle(int Property, EventArgs e)
        {
            RectWidth = Property;
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


        private VectorOfVectorOfPoint GetShapeContour(Point center, int sides, int radius, double angle)
        {
            var shape = new Point[sides];

            double externalAngle = 2 * Math.PI / sides;
            //Create 6 points
            for (int i = 0; i < sides; i++)
            {
                //
                shape[i] = new Point(
                  (int)(center.X + radius * Math.Cos(i * externalAngle + angle)),
                  (int)(center.Y + radius * Math.Sin(i * externalAngle + angle)));
                  //(int)(center.Y + radius * Math.Sin(i * 60 * Math.PI/180 + (angle + Math.PI / 6))));
        }
            //
            VectorOfVectorOfPoint ContourVect = new VectorOfVectorOfPoint();
            //
            ContourVect.Push(new VectorOfPoint(shape));
            //
            return ContourVect;
        }

        private void DrawArrow(IInputOutputArray img, Point p1, double angle)
        {
            Point p2 = new Point();
            const int length = 30;
            p2.X = (int)(length * Math.Cos(angle) + p1.X);
            p2.Y = (int)(length * Math.Sin(angle) + p1.Y);

            CvInvoke.ArrowedLine(img, p1, p2, new MCvScalar(20, 20, 20), 2, LineType.AntiAlias, 0, 0.5);
        }
        #endregion

    }
}
