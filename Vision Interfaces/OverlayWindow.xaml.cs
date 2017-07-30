using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        #region Variables
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

        #region Public Properties
        public Robot[] RobotList = new Robot[6];
        public ImageProcessing imgProc { get; set; }
        public ImageDisplay Display { get; set; }

        #endregion

        #region Private Properties
        private System.Timers.Timer InterfaceTimer { get; set; }
        #endregion

        public ObservableCollection<RobotItem> Robots { get; set; }
        private SynchronizationContext uiContext { get; set; }
        public OverlayWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            // Initialize the robot collection
            ClearRobots(RobotList);
            // Create an image processing class for processing the camera frames
            imgProc = new ImageProcessing();
            // Create an image display class for drawing to the image box
            Display = new ImageDisplay(mainWindow.camera1.Resolution, OverlayImageBox.Size);
            // Set the window to the data context for data binding
            DataContext = this;
            // Default colour amount
            ColourC = 1000;
            // Default lower saturation cutoff
            LowerS = 25;
            // Create 100ms timer to drive interface changes
            InitializeTimer();
            // Create event driven by new frames from the camera
            mainWindow.camera1.FrameUpdate += new Camera.FrameHandler(DrawOverlayFrame);

            Robots = new ObservableCollection<RobotItem>();
            // Stores the UI context to be used to marshal 
            // code from other threads to the UI thread.
            uiContext = SynchronizationContext.Current;
        }

        #region Public Methods
        public void ClearRobots(Robot[] RobotList)
        {
            for (int i = 0; i < RobotList.Length; i++)
            {
                // Initialize each robot
                RobotList[i] = new Robot();
            }
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
        
        private void DrawOverlayFrame(object sender, EventArgs e)
        {
            switch (Display.Source)
            {
                case ImageDisplay.SourceType.NONE:
                    break;
                case ImageDisplay.SourceType.CAMERA:
                    // Typecast object to get passed UMat class
                    UMat Frame = (UMat)sender;
                    // Make sure there is a frame
                    if (Frame != null)
                    {
                        // Apply image processing to find the robots
                        RobotList = imgProc.GetRobots(Frame, RobotList);
                        // Create the overlay image from the robot list
                        // BRAE: Maybe only pass frame size since its only used for that
                        Display.ProcessOverlay(RobotList);
                        // Draw overlay image in window image box
                        OverlayImageBox.Image = Display.Image;
                    }
                    break;
                case ImageDisplay.SourceType.CUTOUTS:
                    // Apply image processing to find the robots
                    RobotList = imgProc.GetRobots(imgProc.TestImage, RobotList);
                    // Create the overlay image from the robot list
                    Display.ProcessOverlay(RobotList);
                    // Draw overlay image in window image box
                    OverlayImageBox.Image = Display.Image;
                    break;
                default:
                    break;
            }

            Update(uiContext);
        }
        #endregion

        #region Private Events 
        private void Interface_Tick(object sender, ElapsedEventArgs e)
        {
            // Update imgProc values from inputs on UI
            imgProc.LowerH = LowerH;
            imgProc.UpperH = UpperH;
            // Update the display with the interface when using the cutouts
            switch (Display.Source)
            {
                case ImageDisplay.SourceType.NONE:
                    break;
                case ImageDisplay.SourceType.CAMERA:
                    break;
                case ImageDisplay.SourceType.CUTOUTS:
                    DrawOverlayFrame(this, new EventArgs());
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
        private void OverlayImageBox_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            // Notify the display where it was clicked
            Display.Click(e.Location);
            // Update the frame
            DrawOverlayFrame(this, new EventArgs());
        }
        private void Overlay_SizeChanged(object sender, EventArgs e)
        {
            // Check if the display has been initialized
            if (Display != null)
                // Resize the overlay image to fix the resized imagebox
                Display.Resize(OverlayImageBox.Width, OverlayImageBox.Height);
        }
        #endregion

        private void Update(object state)
        {
            // Get the UI context from state
            SynchronizationContext uiContext = state as SynchronizationContext;
            // Execute the UpdateRobots function on the UI thread
            uiContext.Post(UpdateRobots, null);
        }
        private void UpdateRobots(object data)
        {
            foreach (Robot R in RobotList)
            {
                // The robot has been initiallized
                if (R != null)
                {
                    bool IsItem = false;
                    // Check if the robot ID matches with an item in the collection
                    foreach (RobotItem I in Robots)
                    {
                        if (I.ID == R.ID)
                        {
                            IsItem = true;
                        }
                    }
                    // Add the robot if it doesn't already exist
                    if (!IsItem)
                        Robots.Add(new RobotItem(R));
                }
            }
        }
    }
}
