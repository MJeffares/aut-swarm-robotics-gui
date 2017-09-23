/**********************************************************************************************************************************************
* Namespaces
**********************************************************************************************************************************************/
using AForge.Video;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using System;
using System.Timers;
using System.Windows;


namespace SwarmRoboticsGUI
{
	/// <summary>
	/// Interaction logic for CameraPopOutWindow.xaml
	/// </summary>
	public partial class CameraPopOutWindow : Window
	{
        private MainWindow mainWindow { get; set; }

        public CameraPopOutWindow(MainWindow mainWindow)
		{ 
			InitializeComponent();
            this.mainWindow = mainWindow;

            // Create event driven by new frames from the camera
            // TODO: fix the popout window
            //mainWindow.camera1.NewFrame += new NewFrameEventHandler(DrawCameraFrame);
        }
        private void DrawCameraFrame(object sender, NewFrameEventArgs e)
        {
            using (var Frame = new Image<Bgr, byte>(e.Frame).Mat)
            using (var Image = new Mat())
            {
                if (Frame != null)
                {
                    // Apply the currently selected filter
                    ImageProcessing.ProcessFilter(Frame, Image, mainWindow.camera1.Filter);
                    // Draw the frame to the overlay imagebox
                    if (Image != null)
                        captureImageBox.Image = Image.Clone();
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //mainWindow.ToggleCameraWindow();
        }

        
    }
}
