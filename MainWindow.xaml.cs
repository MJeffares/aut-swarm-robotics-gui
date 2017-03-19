/**********************************************************************************************************************************************
*	File: MainWindow.xaml.cs
*
*	Developed By: Mansel Jeffares
*	Date: 19 March 2017
*
*	Description :
*		Graphics User Interface for Swarm Robotics Project
*		Built for x64, .NET 4.5.2
*
*	Limitations :
*		Build for x64, will only detect Cameras with x64 drivers
*   
*		Naming Conventions:
*			CamelCase
*			Variables start lower case, if another object goes by the same name, then also with an underscore
*			Methods start upper case
*			Constants, all upper case
* 
**********************************************************************************************************************************************/



/**********************************************************************************************************************************************
* Namespaces
**********************************************************************************************************************************************/
#region 

using DirectShowLib;
using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

#endregion



/**********************************************************************************************************************************************
* Structures and Classes
**********************************************************************************************************************************************/
#region

struct Video_Device
{
	public string deviceName;
	public int deviceId;
	public Guid identifier;

	public Video_Device(int id, string name, Guid identity = new Guid())
	{
		deviceId = id;
		deviceName = name;
		identifier = identity;
	}

	///<summary>
	///Represent the Device as a string
	/// </summary>
	/// <returns>The string representation of this Device</returns>
	public override string ToString()
	{
		return String.Format("[{0}]{1}", deviceId, deviceName);
	}
}



static class CaptureStatuses
{
	public const int PLAYING = 0;
	public const int PAUSED = 1;
	public const int STOPPED = 2;
}

#endregion



namespace SwarmRoboticsGUI
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>

	public partial class MainWindow : Window
	{
		//camera capture variables
		private VideoCapture _capture = null;
		private int cameradevice = 0;
		private Video_Device[] webcams;
		private Mat _frame;
		private string currentlyconnectedcamera = null;
		private int _capturestatus = CaptureStatuses.STOPPED;

		//fps timer variables
		private int _fpscount = 0;
		private DispatcherTimer FpsTimer = new DispatcherTimer();

		public MainWindow()
		{
			InitializeComponent();
			CvInvoke.UseOpenCL = false;
			PopulateCameras();


			FpsTimer.Tick += FpsTimerTick;
			FpsTimer.Interval = new TimeSpan(0, 0, 1);
		}



		/**********************************************************************************************************************************************
		* Programmably Activated Methods
		**********************************************************************************************************************************************/
		#region

		//Find Connected Cameras by using Directshow.net dll library by carles iloret
		//As the project is build for x64, only cameras with x64 drivers will be found/displayed
		private void PopulateCameras()
		{

			if (_capturestatus == CaptureStatuses.STOPPED)
			{
				DsDevice[] _SystemCameras = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);      //gets currently connected devices
				webcams = new Video_Device[_SystemCameras.Length];                                          //creates a new array of devices
				menuCameraList.Items.Clear();                                                               //clears cameras from menu

				//loops through devices and adds them to menu
				for (int i = 0; i < _SystemCameras.Length; i++)
				{
					webcams[i] = new Video_Device(i, _SystemCameras[i].Name); ;
					MenuItem item = new MenuItem { Header = webcams[i].ToString() };
					item.Click += new RoutedEventHandler(menuCameraListItem_Click);
					item.IsCheckable = true;
					menuCameraList.Items.Add(item);

					//restores currently connect camera selection
					if (item.ToString() == currentlyconnectedcamera)
					{
						item.IsEnabled = true;
						item.IsChecked = true;
						cameradevice = menuCameraList.Items.IndexOf(item);
						menuCameraConnect.IsEnabled = true;
					}

				}
				//displays helpful message if no cameras found
				if (menuCameraList.Items.Count == 0)
				{
					MenuItem nonefound = new MenuItem { Header = "No Cameras Found" };
					menuCameraList.Items.Add(nonefound);
					nonefound.IsEnabled = false;
				}
			}
		}



		private void StartCapture()
		{
			if (_capture != null)
			{
				_capture.Dispose();
			}

			try
			{
				//Set up capture device
				host1.Visibility = Visibility.Visible;
				_capture = new VideoCapture(cameradevice);
				_capture.ImageGrabbed += ProcessFrame;
				_frame = new Mat();

				menuCameraConnect.Header = "Stop Capture";
				menuCameraFreeze.IsEnabled = true;

				_capture.Start();
				FpsTimer.Start();
				_capturestatus = CaptureStatuses.PLAYING;
			}
			catch (NullReferenceException excpt)
			{
				MessageBox.Show(excpt.Message);
			}
		}



		private void StopCapture()
		{
			try
			{
				_capture.Stop();
				FpsTimer.Stop();
				cameraStatusFPS.Text = "FPS: ";
				_capture.Dispose();
				host1.Visibility = Visibility.Hidden;
				//_captureInProgress = false;
				_capturestatus = CaptureStatuses.STOPPED;
				menuCameraConnect.Header = "Start Capture";
				menuCameraFreeze.Header = "Freeze";
				menuCameraFreeze.IsChecked = false;
				menuCameraFreeze.IsEnabled = false;
			}
			catch (Exception excpt)
			{
				MessageBox.Show(excpt.Message);
			}
		}



		private void PauseCapture()
		{
			try
			{
				_capture.Pause();
				FpsTimer.Stop();
				cameraStatusFPS.Text = "FPS: ";
				//_captureInProgress = false;
				_capturestatus = CaptureStatuses.PAUSED;
				menuCameraFreeze.Header = "Un-Freeze";
				menuCameraFreeze.IsChecked = true;
			}
			catch (Exception excpt)
			{
				MessageBox.Show(excpt.Message);
			}
		}



		private void ResumeCapture()
		{
			try
			{
				_capture.Start();
				FpsTimer.Start();
				//_captureInProgress = true;
				_capturestatus = CaptureStatuses.PLAYING;
				menuCameraFreeze.Header = "Freeze";
				menuCameraFreeze.IsChecked = false;
			}
			catch (Exception excpt)
			{
				MessageBox.Show(excpt.Message);
			}
		}

		#endregion



		/**********************************************************************************************************************************************
		* Repeated/Time Activated Methods
		**********************************************************************************************************************************************/
		#region

		private void ProcessFrame(object sender, EventArgs arg)
		{
			if (_capture != null && _capture.Ptr != IntPtr.Zero)
			{
				_capture.Retrieve(_frame, 0);

				captureImageBox.Image = _frame;
				_fpscount++;
				// _capture.SetCaptureProperty(CapProp.Brightness, FPS_Count);
			}
		}



		private void FpsTimerTick(object sender, EventArgs arg)
		{
			cameraStatusFPS.Text = "FPS: " + _fpscount.ToString();
			_fpscount = 0;
		}

		#endregion



		/**********************************************************************************************************************************************
		* User Control Activated Methods
		**********************************************************************************************************************************************/
		#region

		public void menuCameraListItem_Click(Object sender, RoutedEventArgs e)
		{
			MenuItem menusender = (MenuItem)sender;
			String menusenderstring = menusender.ToString();

			if (_capturestatus == CaptureStatuses.STOPPED && currentlyconnectedcamera != menusenderstring) //also check if the same menu option is clicked twice
			{
				var allitems = menuCameraList.Items.Cast<System.Windows.Controls.MenuItem>().ToArray();

				foreach (var item in allitems)
				{
					item.IsChecked = false;
					item.IsEnabled = false;
				}

				menusender.IsEnabled = true;
				menusender.IsChecked = true;
				currentlyconnectedcamera = menusender.ToString();
				cameraStatusName.Text = menusender.Header.ToString();
				cameradevice = menuCameraList.Items.IndexOf(menusender);
				menuCameraConnect.IsEnabled = true;

			}
			else if (currentlyconnectedcamera == menusenderstring)
			{
				var allitems = menuCameraList.Items.Cast<System.Windows.Controls.MenuItem>().ToArray();

				foreach (var item in allitems)
				{
					item.IsChecked = false;
					item.IsEnabled = true;
				}
				currentlyconnectedcamera = "No Camera Selected";
				cameraStatusName.Text = null;
				cameradevice = -1;
				menuCameraConnect.IsEnabled = false;
			}
		}



		private void MenuItem_MouseEnter(object sender, MouseEventArgs e)
		{
			PopulateCameras();
		}



		private void menuCameraConnect_Click(object sender, RoutedEventArgs e)
		{
			if (_capturestatus == CaptureStatuses.PLAYING || _capturestatus == CaptureStatuses.PAUSED)
			{
				StopCapture();

				var allitems = menuCameraList.Items.Cast<MenuItem>().ToArray();

				foreach (var item in allitems)
				{
					item.IsEnabled = true;
				}
			}
			else if (_capturestatus == CaptureStatuses.STOPPED)
			{
				StartCapture();

				var allitems = menuCameraList.Items.Cast<MenuItem>().ToArray();

				foreach (var item in allitems)
				{
					item.IsEnabled = false;
				}
			}
		}



		private void menCameraOptions_Click(object sender, RoutedEventArgs e)
		{
			//need try/catch or checks 
			try
			{
				_capture.SetCaptureProperty(CapProp.Settings, 1);
			}
			catch (Exception excpt)
			{
				MessageBox.Show(excpt.Message);
			}
		}



		private void menuCameraFreeze_Click(object sender, RoutedEventArgs e)
		{
			if (_capturestatus == CaptureStatuses.PLAYING)
			{
				PauseCapture();
			}
			else if (_capturestatus == CaptureStatuses.PAUSED)
			{
				ResumeCapture();
			}
		}

		#endregion



	}
}
