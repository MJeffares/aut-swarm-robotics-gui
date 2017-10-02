using AForge.Video;
using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SwarmRoboticsGUI
{
    /// <summary>
    /// Interaction logic for Overlay.xaml
    /// </summary>
    public partial class OverlayWindow : Window
    {
        #region HSV Variables
        public double UpperC { get; set; }
        public double LowerC { get; set; }
        public int ColourC { get; set; }
        public int LowerH { get; set; } 
        public int LowerS { get; set; }
        public int LowerV { get; set; }
        public int UpperH { get; set; }
        public int UpperS { get; set; }
        public int UpperV { get; set; }
        #endregion
        private System.Timers.Timer InterfaceTimer { get; set; }
        public List<RobotItem> RobotList { get; set; }
        public List<IObstacle> Obstacles { get; set; }
        public List<Item> ItemList { get; set; }
        public Arena RobotArena { get; set; }
        public XbeeHandler.XbeeAPI Xbee { get; set; }

        private Camera camera1;

        public OverlayWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            // Create an image display class for drawing to the image box
            // Set the window to the data context for data binding
            DataContext = this;
            // Default colour amount
            ColourC = 1000;
            // Default lower saturation cutoff
            LowerS = 25;
            LowerH = 0;
            UpperH = 130;

            // Create 100ms timer to drive interface changes
            InitializeTimer();

            RobotArena = new Arena();         
            // TEMP: reference mainWindow camera until setup is moved to overlay
            camera1 = mainWindow.camera1;
            // Create event driven by new frames from the camera
            camera1.Process += new EventHandler(DrawOverlayFrame);

            //Creates a local copy of the robotlist only containing the robots themselves
            ItemList = mainWindow.ItemList;
            Obstacles = mainWindow.ItemList.Where(R => R is IObstacle).Cast<IObstacle>().ToList();
            RobotList = mainWindow.ItemList.Where(R => R is RobotItem).Cast<RobotItem>().ToList();

            Xbee = mainWindow.xbee;
        }

        #region Public Methods
        public void ClearRobots(List<Item> RobotList)
        {
            RobotList.Clear();
        }
        #endregion


        #region Time Events
        private void Interface_Tick(object sender, ElapsedEventArgs e)
        {
            // Update the display with the interface when using the cutouts
            switch (display1.Source)
            {
                case SourceType.NONE:
                    break;
                case SourceType.CAMERA:
                    LowerH = Properties.Settings.Default.CV_Filter_HueLower;
                    UpperH = Properties.Settings.Default.CV_Filter_HueUpper;
                    
                    break;
                case SourceType.CUTOUTS:
                    break;
                default:
                    break;
            }
        }

        
        private int counter { get; set; }
        private void DrawOverlayFrame(object sender, EventArgs e)
        {
            var Frame = sender as UMat;

            switch (display1.Source)
            {
                case SourceType.NONE:
                    break;
                case SourceType.CAMERA:
                    // Make sure there is a frame
                    if (Frame != null)
                    {
                        //Apply the currently selected filter
                        if (camera1.Filter != FilterType.NONE)
                        {
                            var proc = new Mat();
                            ImageProcessing.ProcessFilter(Frame, proc, camera1.Filter, LowerH, UpperH);
                            if (proc != null)
                                CameraDisplay1.Image = proc;
                        }
                        else
                        {
                            if (RobotArena.Contour != null)
                            {
                                var rect = CvInvoke.BoundingRectangle(new VectorOfPoint(RobotArena.Contour));
                                var proc = new UMat(Frame, rect);
                                CvInvoke.Circle(proc, RobotArena.Origin, 10, new MCvScalar(0, 0, 255), -1);

                                CameraDisplay1.Image = proc;
                            }
                            else
                                CameraDisplay1.Image = Frame;
                        }

                        // TEMP: Counter to get the arena every 30 frames
                        counter++;
                        if (counter > 30)
                        {
                            if (camera1 != null)
                                camera1.SetWhiteBalance(Properties.Settings.Default.CV_WhiteBalance);

                            ImageProcessing.GetArena(Frame, RobotArena);

                            counter = 0;
                        }
                        //RobotArena.Origin = new System.Drawing.Point(0, 0);
                        //RobotArena.ScaleFactor = 1;

                        // Apply image processing to find the robots
                        ImageProcessing.GetRobots(Frame, RobotList, RobotArena);
                    }                   
                    break;
                case SourceType.CUTOUTS:
                    // Apply image processing to find the robots
                    ImageProcessing.GetRobots(ImageProcessing.TestImage, RobotList, RobotArena);

                    //// Draw the testimage to the overlay imagebox
                    //if (ImageProcessing.TestImage != null)
                    //{
                    //    captureImageBox.Image = (UMat)ImageProcessing.TestImage;
                    //}


                    break;
                default:
                    break;
            }
        }

        #endregion

        #region Private Events 
        private void InitializeTimer()
        {
            // Create 100ms timer to drive interface changes
            InterfaceTimer = new System.Timers.Timer(100);
            InterfaceTimer.Elapsed += Interface_Tick;
            InterfaceTimer.Start();
        }
        private void Overlay_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (InterfaceTimer != null)
            {
                InterfaceTimer.Stop();
                InterfaceTimer.Dispose();
            }
            camera1.StopCapture();
            camera1.Process -= new EventHandler(DrawOverlayFrame);
            CameraDisplay1.Image = null;
        }

        //private void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    // Find robot with ID of 1
        //    var Robot = RobotList.Where(f => f.ID == 1).SingleOrDefault();
        //    // Get the robot index inside the group
        //    int RobotIndex = RobotList.IndexOf(Robot);
        //    // Move to a new group
        //    RobotList[RobotIndex].Group = "Formation 1";

        //    // Find robot with ID of 2
        //    Robot = RobotList.Where(f => f.ID == 2).SingleOrDefault();
        //    // Get the robot index inside the group
        //    RobotIndex = RobotList.IndexOf(Robot);
        //    // Set the battery to 50%
        //    RobotList[RobotIndex].Battery = 50;
        //}
        #endregion

        private void OverlaySelect_Click(object sender, RoutedEventArgs e)
        {
            display1.Visibility = Visibility.Visible;
            CameraDisplay1.Visibility = Visibility.Collapsed;
        }

        private void OverlayCameraSelect_Click(object sender, RoutedEventArgs e)
        {
            display1.Visibility = Visibility.Visible;
            CameraDisplay1.Visibility = Visibility.Visible;
        }

        private void CameraSelect_Click(object sender, RoutedEventArgs e)
        {
            display1.Visibility = Visibility.Collapsed;
            CameraDisplay1.Visibility = Visibility.Visible;
        }

        private void Display1_TargetChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var pos = ((System.Windows.Point)e.NewValue);
            if (pos != null && Display1.SelectedItem != null)
            {
                byte[] data;
                data = new byte[5];
                data[0] = ROBOT_CONTROL_MESSAGE.MoveToPosition;
                data[1] = (byte)((int)pos.X >> 8);
                data[2] = (byte)((int)pos.X);
                data[3] = (byte)((int)pos.Y >> 8);
                data[4] = (byte)((int)pos.Y);

                Xbee.SendTransmitRequest(((ICommunicates)Display1.SelectedItem).Address64, data);
            }
        }
    }
}
