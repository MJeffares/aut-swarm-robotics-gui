using AForge.Video;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
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

        public ObservableCollection<RobotItem> RobotList { get; set; }
        private SynchronizationContext uiContext { get; set; }


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
            // Create event driven by new frames from the camera
            mainWindow.camera1.FrameUpdate += new Camera.FrameHandler(DrawOverlayFrame);

            RobotList = new ObservableCollection<RobotItem>();
            // Stores the UI context to be used to marshal 
            // code from other threads to the UI thread.
            uiContext = SynchronizationContext.Current;
        }


        #region Public Methods
        public void ClearRobots(ObservableCollection<RobotItem> RobotList)
        {
            RobotList.Clear();
        }
        #endregion


        private void InitializeTimer()
        {
            // Create 100ms timer to drive interface changes
            InterfaceTimer = new System.Timers.Timer(100);
            InterfaceTimer.Elapsed += Interface_Tick;
            InterfaceTimer.Start();
        }

        #region Time Events
        
        private void DrawOverlayFrame(object sender, NewFrameEventArgs e)
        {
            var List = RobotList.ToList();

            switch (Display1.Source)
            {
                case Display.SourceType.NONE:
                    break;
                case Display.SourceType.CAMERA:
                    // Make sure there is a frame
                    if (e.Frame != null)
                    {
                        // Apply image processing to find the robots
                        using (UMat UFrame = new Image<Bgr, byte>(e.Frame).Mat.GetUMat(AccessType.Read))
                        {
                            ImageProcessing.GetRobots(UFrame, List);
                        }
                        // Update the robotlist on the UI thread
                        Update(uiContext, List);
                    }
                    break;
                case Display.SourceType.CUTOUTS:
                    // Apply image processing to find the robots
                    ImageProcessing.GetRobots(ImageProcessing.TestImage, List);
                    // Update the robotlist on the UI thread
                    Update(uiContext, List);
                    break;
                default:
                    break;
            }
            
        }
        #endregion

        #region Private Events 
        private void Interface_Tick(object sender, ElapsedEventArgs e)
        {
            // Update the display with the interface when using the cutouts
            switch (Display1.Source)
            {
                case Display.SourceType.NONE:
                    break;
                case Display.SourceType.CAMERA:
                    break;
                case Display.SourceType.CUTOUTS:
                    break;
                default:
                    break;
            }
        }
        private void Overlay_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            InterfaceTimer.Dispose();
        }
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearRobots(RobotList);
        }
        #endregion

        private void Update(object state, object data)
        {
            // Get the UI context from state
            SynchronizationContext uiContext = state as SynchronizationContext;
            // Execute the UpdateRobots function on the UI thread
            uiContext.Post(UpdateRobots, data);
        }
        private void UpdateRobots(object data)
        {
            // Updates from another thread
            var RobotList1 = new ObservableCollection<RobotItem>((List<RobotItem>)data);
            foreach(RobotItem R in RobotList1)
            {
                // Robot with the same ID
                var Robot = RobotList.Where(f => f.ID == R.ID).FirstOrDefault();
                // Robot exist in list
                if (Robot != null)
                {
                    var index = RobotList.IndexOf(Robot);
                    if (Robot.IsSelected)
                    {
                        R.IsSelected = true;
                    }
                    RobotList.RemoveAt(index);
                    RobotList.Insert(index, R);
                }
                else
                {
                    RobotList.Add(R);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Find robot with ID of 1
            var Robot = RobotList.Where(f => f.ID == 1).SingleOrDefault();
            // Get the robot index inside the group
            int RobotIndex = RobotList.IndexOf(Robot);
            // Move to a new group
            RobotList[RobotIndex].Group = "Formation 1";

            // Find robot with ID of 2
            Robot = RobotList.Where(f => f.ID == 2).SingleOrDefault();
            // Get the robot index inside the group
            RobotIndex = RobotList.IndexOf(Robot);
            // Set the battery to 50%
            RobotList[RobotIndex].Battery = 50;
        }
    }
}
