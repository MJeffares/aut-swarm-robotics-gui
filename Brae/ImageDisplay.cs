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
        private Size _DisplaySize;
        public Size DisplaySize
        {
            get { return _DisplaySize; }
            private set
            {
                _DisplaySize = value;
                if (!FrameSize.IsEmpty)
                    Scale = new SizeF((float)_DisplaySize.Width / FrameSize.Width, (float)_DisplaySize.Height / FrameSize.Height);
            }
        }
        public Size FrameSize { get; set; }
        public SizeF Scale { get; private set; }


        private PointF CursorPosition { get; set; }
        private ImageAnimation IA { get; set; }
        #endregion

        //private static VectorOfVectorOfPoint HexagonContour;


        public ImageDisplay(Size FrameSize, Size DisplaySize)
        {
            Image = new UMat();
            this.FrameSize = FrameSize;
            this.DisplaySize = DisplaySize;

            IA = new ImageAnimation(0, 0, 20, 200);
            IA.AnimationUpdate += new ImageAnimation.AnimationHandler(AnimateRectangle);
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
        public void ProcessOverlay(Robot[] RobotList)
        {
            switch (Overlay)
            {
                case OverlayType.NONE:
                    break;
                case OverlayType.DEBUG:
                    DrawDebugOverlay(RobotList);
                    break;
                case OverlayType.PRETTY:
                    DrawPrettyOverlay(RobotList);
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
        public void Click(Point pos)
        {
            CursorPosition = new PointF(Math.Abs((float)(pos.X)), Math.Abs((float)(pos.Y)));
            RectWidth = 0;
            IA.Start();
        }
        // Resize
        public void Resize(int Width, int Height)
        {
            // Calculate scale factor using ratios
            float widthScale = (float)Width / DisplaySize.Width;
            float heightScale = (float)Height / DisplaySize.Height;
            // Scale cursor postion
            CursorPosition = new PointF((CursorPosition.X * widthScale), (CursorPosition.Y * heightScale));
            // Set new dimensions
            DisplaySize = new Size(Width, Height);
        }
        #endregion

        #region Private Methods
        private void DrawRobot(IInputOutputArray img, Robot Robot)
        {
            // Scale robot position due to resize
            Point ScaledRobotLocation = new Point((int)(Robot.Location.X * Scale.Width), (int)(Robot.Location.Y * Scale.Height));

            // Average Reolution Dimension divided by 100 to use as percentage scale
            int ObjectScale = (DisplaySize.Height + DisplaySize.Width) / 200;

            // Draw robots as hexagons
            VectorOfPoint Contour = GetShapeContour(ScaledRobotLocation, 6, 10 * ObjectScale, Robot.Heading + Math.PI / 6);
            CvInvoke.FillConvexPoly(img, Contour, new MCvScalar(200, 200, 200, 255), LineType.AntiAlias);

            // Location of direction indicator
            Point Direction = new Point(
                (int)(3 * ObjectScale * Math.Cos(Robot.Heading)) + ScaledRobotLocation.X,
                (int)(3 * ObjectScale * Math.Sin(Robot.Heading)) + ScaledRobotLocation.Y);

            // Draw robot direction indicator as triangle
            Contour = GetShapeContour(Direction, 3, 1 * ObjectScale, Robot.Heading);
            CvInvoke.FillConvexPoly(img, Contour, new MCvScalar(0, 0, 0, 255), LineType.AntiAlias);
        }
        private void DrawSelectedRobot(IInputOutputArray img, Robot Robot)
        {
            // Scale robot position due to resize
            Point ScaledRobotLocation = new Point((int)(Robot.Location.X * Scale.Width), (int)(Robot.Location.Y * Scale.Height));

            // Average Reolution Dimension divided by 100 to use as percentage scale
            int ObjectScale = (DisplaySize.Height + DisplaySize.Width) / 200;

            // Information box position
            Point InfoBoxLocation = new Point(ScaledRobotLocation.X, ScaledRobotLocation.Y - 8 * ObjectScale);

            // Box to hold information about current robot
            CvInvoke.Rectangle(img, new Rectangle(InfoBoxLocation, new Size(RectWidth * ObjectScale, 16 * ObjectScale)), new MCvScalar(100, 100, 100, 70), -1);

            // Draw robots as hexagons
            VectorOfPoint Contour = GetShapeContour(ScaledRobotLocation, 6, 10 * ObjectScale, Robot.Heading + Math.PI / 6);
            CvInvoke.FillConvexPoly(img, Contour, new MCvScalar(255, 255, 255, 255), LineType.AntiAlias);

            // Location of direction indicator
            Point Direction = new Point(
                (int)(3 * ObjectScale * Math.Cos(Robot.Heading)) + ScaledRobotLocation.X,
                (int)(3 * ObjectScale * Math.Sin(Robot.Heading)) + ScaledRobotLocation.Y);

            // Draw robot direction indicator as triangle
            Contour = GetShapeContour(Direction, 3, 1 * ObjectScale, Robot.Heading);
            CvInvoke.FillConvexPoly(img, Contour, new MCvScalar(0, 0, 0, 255), LineType.AntiAlias);
            
            // Draw robot information
            CvInvoke.PutText(img, Robot.ID.ToString(), new Point(ScaledRobotLocation.X + 2 * ObjectScale, ScaledRobotLocation.Y + 0), FontFace.HersheyScriptSimplex, 0.05 * ObjectScale, new MCvScalar(50, 50, 50, 255), 1, LineType.AntiAlias);
            CvInvoke.PutText(img, Robot.Battery.ToString(), new Point(ScaledRobotLocation.X + 2 * ObjectScale, ScaledRobotLocation.Y + 2 * ObjectScale), FontFace.HersheyScriptSimplex, 0.05 * ObjectScale, new MCvScalar(50, 50, 50, 255), 1, LineType.AntiAlias);
        }

        // Overlay Drawing
        private void DrawPrettyOverlay(Robot[] RobotList)
        {
            float MaxScale = Math.Max(Scale.Width, Scale.Height);

            using (UMat Out = new Image<Bgra, byte>(DisplaySize.Width, DisplaySize.Height, new Bgra(0, 0, 0, 0)).Mat.GetUMat(AccessType.Read))
            {
                foreach (Robot Robot in RobotList)
                {
                    // Scale robot position due to resize
                    Point ScaledRobotLocation = new Point((int)(Robot.Location.X * Scale.Width), (int)(Robot.Location.Y * Scale.Height));
                    // Draw Robots as hexagons
                    VectorOfPoint Contour = GetShapeContour(ScaledRobotLocation, 6, (int)(100 * MaxScale), Robot.Heading + Math.PI / 6);

                    double IsPointInContour = CvInvoke.PointPolygonTest(Contour, CursorPosition, false);
                    if (IsPointInContour < 0)
                    {
                        // Not selected
                        Robot.IsSelected = false;
                        DrawRobot(Out, Robot);
                    }
                    else
                    {
                        // Current robot is selected
                        Robot.IsSelected = true;
                    }
                }
                // Draw the selected robot last
                foreach (Robot Robot in RobotList)
                {
                    if(Robot.IsSelected)
                    {
                        DrawSelectedRobot(Out, Robot);
                    }
                }

                // DEBUG: Draw valid mouse region
                //CvInvoke.DrawContours(Out, Contour, -1, new MCvScalar(0, 255, 0, 255), 1, LineType.AntiAlias);
                // DEBUG: draw circle at cursor position
                CvInvoke.Circle(Out, Point.Round(CursorPosition), 5, new MCvScalar(0, 0, 255, 255), -1);
                // Blue, Display size
                CvInvoke.Circle(Out, new Point(DisplaySize), 5, new MCvScalar(255, 0, 0, 255), -1);
                // Cyan, Frame size
                CvInvoke.Circle(Out, new Point(FrameSize), 5, new MCvScalar(255, 255, 0, 255), -1);

                // Update display image
                Image = Out.Clone();
            }
        }
        private void DrawDebugOverlay(Robot[] RobotList)
        {
            Mat Input = new Image<Bgr, byte>(FrameSize.Width, FrameSize.Height).Mat;

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

        // TEMP: Information box animation property
        private int RectWidth { get; set; }
        private void AnimateRectangle(int Property, EventArgs e)
        {
            RectWidth = Property;
        }

        private VectorOfPoint GetShapeContour(Point center, int sides, int radius, double angle)
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
            }
            //
            VectorOfVectorOfPoint ContourVect = new VectorOfVectorOfPoint();
            //
            ContourVect.Push(new VectorOfPoint(shape));
            //
            //return ContourVect;
            return new VectorOfPoint(shape);
        }

        // Drawing Utilities
        private void DrawArrow(IInputOutputArray img, Point start, double angle)
        {
            Point end = new Point();
            const int length = 30;
            end.X = (int)(length * Math.Cos(angle) + start.X);
            end.Y = (int)(length * Math.Sin(angle) + start.Y);

            CvInvoke.ArrowedLine(img, start, end, new MCvScalar(20, 20, 20, 255), 2, LineType.AntiAlias, 0, 0.5);
        }
        private void DrawLinearGradient(IInputOutputArray img, Point start, Point end, double angle, int colorStart, int colorEnd)
        {
            // Colour value gradient
            double grad = Math.Abs((double)colorEnd - colorStart) / (end.X - start.X);

            // BRAE: Allow for reverse gradients
            // BRAE: Use angle value

            for (int i = start.X + start.Y; i < end.X + end.Y; i++)
            {
                // Draw
                CvInvoke.Line(img, new Point(i, start.X), new Point(start.Y, i), new MCvScalar(i * grad, i * grad, i * grad, 255), 1);
                CvInvoke.Line(img, new Point(i, end.X), new Point(end.Y, i), new MCvScalar(i * grad, i * grad, i * grad, 255), 1);
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
                    CvInvoke.Circle(img, center, (int)distance - i, new MCvScalar(i * grad, i * grad, i * grad, 255), 2);
                }
            }
            else
            {
                // Start from center
                for (int i = 0; i < (int)distance; i++)
                {
                    CvInvoke.Circle(img, center, i, new MCvScalar(i * grad, i * grad, i * grad, 255), 2);
                }
            }
        }
        #endregion
    }
}