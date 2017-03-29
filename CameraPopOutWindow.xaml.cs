/**********************************************************************************************************************************************
* Namespaces
**********************************************************************************************************************************************/
#region 

using DirectShowLib;
using Emgu.CV;
using Emgu.CV.CvEnum;
using folderHack;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WPFCustomMessageBox;


using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

#endregion






namespace SwarmRoboticsGUI
{
	/// <summary>
	/// Interaction logic for CameraPopOutWindow.xaml
	/// </summary>
	public partial class CameraPopOutWindow : Window
	{
		public CameraPopOutWindow()
		{
			InitializeComponent();
			
		}

		/*
		public delegate void UpdateImageCallback(Mat _image);


		private void UpdateImage(Mat _image)
		{
			captureImageBox.Image = _image;
		}
		*/
	}
}
