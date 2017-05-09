/**********************************************************************************************************************************************
* Namespaces
**********************************************************************************************************************************************/
using Emgu.CV;
using System;
using System.Windows;


namespace SwarmRoboticsGUI
{
	/// <summary>
	/// Interaction logic for CameraPopOutWindow.xaml
	/// </summary>
	public partial class CameraPopOutWindow : Window
	{
        public CameraPopOutWindow(MainWindow main)
		{ 
			InitializeComponent();
            this.main = main;
        }

        private MainWindow main;

        private void CameraPopOutWindow_Closed(object sender, EventArgs e)
        {
            main.ToggleCameraWindow();
        }
    }
}
