/**********************************************************************************************************************************************
* Namespaces
**********************************************************************************************************************************************/
using Emgu.CV;
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
        private Timer FrameTimer { get; set; }
        private MainWindow mainWindow { get; set; }

        public CameraPopOutWindow(MainWindow mainWindow)
		{ 
			InitializeComponent();
            this.mainWindow = mainWindow;

            FrameTimer = new Timer(50);
            FrameTimer.Elapsed += Frame_Tick;
            FrameTimer.Start();
        }
        private void Frame_Tick(object sender, ElapsedEventArgs e)
        {
            //captureImageBox.Image = mainWindow.captureImageBox.Image;
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            FrameTimer.Dispose();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            mainWindow.ToggleCameraWindow();
        }

        
    }
}
