/**********************************************************************************************************************************************
*	File: MainWindow.xaml.cs
*
*	Developed By: Mansel Jeffares
*	First Build: 7 March 2017
*	Current Build:  19 March 2017
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
*			Constants, all upper case, unscores for seperation
* 
**********************************************************************************************************************************************/



/**********************************************************************************************************************************************
* Namespaces
**********************************************************************************************************************************************/
#region 

using DirectShowLib;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
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


//cannot use precomplier defines therefore we use a static class
static class CaptureStatuses
{
	public const int PLAYING = 0;
	public const int PAUSED = 1;
	public const int STOPPED = 2;
	public const int REPLAY_ACTIVE = 3;
	public const int REPLAY_PAUSED = 4;
	public const int RECORD = 5;
}



static public class CaptureFilters
{
	//static public Mat outputframe;

	public const int NUM_FILTERS = 4;

	public const int NO_FILTER = 0;
	public const int GREYSCALE = 1;
	public const int CANNY_EDGES = 2;
	public const int BRAE_EDGES = 3;

	// HSV ranges.
	private const int LowerH = 0;
	private const int UpperH = 255;
	private const int LowerS = 0;
	private const int UpperS = 255;
	private const int LowerV = 0;
	private const int UpperV = 255;

	// Blur, Canny, and Threshold values.
	private const int BlurC = 1;
	private const int LowerC = 128;
	private const int UpperC = 255;

	///<summary>
	///Calculates the output for the current filter
	/// </summary>
	/// <returns>The proccessed frame matrix</returns>
	static public Mat Process(int filter, Mat inputframe, Mat outputframe)
	{
		
		switch (filter)
		{
			case CaptureFilters.NO_FILTER:
				return inputframe;
				

			case CaptureFilters.GREYSCALE:
				CvInvoke.CvtColor(inputframe, outputframe, ColorConversion.Bgr2Gray);
				return outputframe;

			case CaptureFilters.CANNY_EDGES:
				CvInvoke.CvtColor(inputframe, outputframe, ColorConversion.Bgr2Gray);
				CvInvoke.PyrDown(outputframe, outputframe);
				CvInvoke.PyrUp(outputframe, outputframe);
				CvInvoke.Canny(outputframe, outputframe, 80, 40);
				return outputframe;

			case CaptureFilters.BRAE_EDGES:
				CvInvoke.CvtColor(inputframe, outputframe, ColorConversion.Bgr2Gray);
				CvInvoke.Threshold(outputframe, outputframe, LowerC, UpperC, ThresholdType.Binary);
				CvInvoke.AdaptiveThreshold(outputframe, outputframe, UpperC, AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 3, 0);

				return outputframe;

			default:
				return inputframe;
		}
	}


	///<summary>
	///Represent the Filter as a string
	/// </summary>
	/// <returns>The string representation of this Filter</returns>
	static public string ToString(int filter)
	{
		switch (filter)
		{
			case CaptureFilters.NO_FILTER:
				return String.Format("No Filter");

			case CaptureFilters.GREYSCALE:
				return String.Format("Greyscale");

			case CaptureFilters.CANNY_EDGES:
				return String.Format("Canny Edges");

			case CaptureFilters.BRAE_EDGES:
				return String.Format("Brae Edges");

			default:
				return String.Format("Filter Text Error");

		}
	}
}

#endregion



namespace SwarmRoboticsGUI
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>

	public partial class MainWindow : Window
	{
		/**********************************************************************************************************************************************
		* Variables
		**********************************************************************************************************************************************/
		#region

		//camera capture variables
		private VideoCapture _capture = null;
		private int cameradevice = 0;
		private Video_Device[] webcams;
		private Mat _frame;
		private string currentlyconnectedcamera = null;
		private int _capturestatus = CaptureStatuses.STOPPED;
		private int filter = CaptureFilters.NO_FILTER;
		private bool captureblocked = false;
		private int captureblockedframes = 0;
		private Mat outputframe = new Mat();
		//private bool smoothed = false;


		//fps timer variables
		private int _fpscount = 0;
		private DispatcherTimer FpsTimer = new DispatcherTimer();

		// video record/replay variables
		OpenFileDialog openvideodialog = new OpenFileDialog();
		SaveFileDialog savevideodialog = new SaveFileDialog();
		private double replayframerate = 0;
		private double replaytotalframes = 0;
		private int recordframewidth;
		private int recordframeheight;
		private double replayframecount;
		private VideoWriter _videowriter;
		private Stopwatch _stopwatch;
		private double replayspeed = 1;
		private int recordframerate = 0;
		private System.Drawing.Size recordsize = new System.Drawing.Size();

		

		#endregion

		public MainWindow()
		{
			InitializeComponent();
			CvInvoke.UseOpenCL = false;
			PopulateFilters();
			PopulateCameras();

			openvideodialog.Filter = "Video Files|*.avi;*.mp4;*.mpg";
			savevideodialog.Filter = "Video Files|*.avi;*.mp4;*.mpg";
			FpsTimer.Tick += FpsTimerTick;
			FpsTimer.Interval = new TimeSpan(0, 0, 1);
			FpsTimer.Start();
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



		private void PopulateFilters()
		{

			for(int i =0; i < CaptureFilters.NUM_FILTERS; i++)
			{
				MenuItem item = new MenuItem { Header = CaptureFilters.ToString(i) };
				item.Click += new RoutedEventHandler(menuFilterListItem_Click);
				item.IsCheckable = true;

				if(i == 0)
				{
					item.IsChecked = true;
				}

				menuFilterList.Items.Add(item);
			}

			Separator sep = new Separator();
			menuFilterList.Items.Add(sep);

			MenuItem settingsmenuitem = new MenuItem { Header = "Settings" };
			menuFilterList.Items.Add(settingsmenuitem);
		}



		//menu will not display correctly if image refreshed while menu is being rendered 
		//blocking display for two frames if the menu has been clicked/hovered over
		private void DisplayFrame()
		{
			if (!captureblocked)
			{
				_fpscount++;
				captureImageBox.Image = CaptureFilters.Process(filter, _frame, outputframe);
			}
			else
			{
				captureblockedframes++;
			}

			if (captureblockedframes > 2)
			{
				captureblocked = false;
				captureblockedframes = 0;
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

				if (_capturestatus == CaptureStatuses.PLAYING)
				{
					DisplayFrame();
				}
				else if(_capturestatus == CaptureStatuses.REPLAY_ACTIVE)
				{
					replayframecount = _capture.GetCaptureProperty(CapProp.PosFrames);
					//display current frame/time
					DisplayFrame();	
					Thread.Sleep((int)(1000.0 / (replayframerate * replayspeed)));
				}
				else if(_capturestatus == CaptureStatuses.RECORD)
				{
					DisplayFrame();
					if (_videowriter.Ptr != IntPtr.Zero)
					{
						//_videowriter.Write(_frame);
						_videowriter.Write(CaptureFilters.Process(filter, _frame, outputframe));
					}
				}
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
				//var allitems = menuCameraList.Items.Cast<System.Windows.Controls.MenuItem>().ToArray();
				var allitems = menuCameraList.Items.OfType<MenuItem>().ToArray();

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
				//var allitems = menuCameraList.Items.Cast<System.Windows.Controls.MenuItem>().ToArray();
				var allitems = menuCameraList.Items.OfType<MenuItem>().ToArray();

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


		private void menuFilterListItem_Click(object sender, RoutedEventArgs e)
		{
			
			MenuItem menusender = (MenuItem)sender;
			String menusenderstring = menusender.ToString();

			if(menusenderstring != CaptureFilters.ToString(filter))
			{
				var allitems = menuFilterList.Items.OfType<MenuItem>().ToArray();

				foreach (var item in allitems)
				{
					item.IsChecked = false;
				}

				menusender.IsChecked = true;

				filter = menuFilterList.Items.IndexOf(menusender);

				//
				cameraStatusFilter.Text = CaptureFilters.ToString(filter);
			}
			else if (menusenderstring == CaptureFilters.ToString(filter) && CaptureFilters.ToString(filter) != CaptureFilters.ToString(CaptureFilters.NO_FILTER))
			{
				var allitems = menuFilterList.Items.OfType<MenuItem>().ToArray();

				foreach (var item in allitems)
				{
					item.IsChecked = false;
					item.IsEnabled = true;
				}
			}
		}



		private void MenuItem_MouseEnter(object sender, MouseEventArgs e)
		{
			captureblocked = true;
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

				var allitems = menuCameraList.Items.OfType<MenuItem>().ToArray();

				foreach (var item in allitems)
				{
					item.IsEnabled = false;
				}
			}
		}



		private void menuCameraOptions_Click(object sender, RoutedEventArgs e)
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

		/*
		private void menuFilterSmooth_Click(object sender, RoutedEventArgs e)
		{
			if (smoothed)
			{
				menuFilterSmooth.IsChecked = false;
				smoothed = false;
			}
			else
			{
				menuFilterSmooth.IsChecked = true;
				smoothed = true;
			}
		}
		*/


		private void menuFilterFlipVertical_Click(object sender, RoutedEventArgs e)
		{
			if (_capture != null)
			{
				_capture.FlipVertical = !_capture.FlipVertical;
			}
		}



		private void menuFilterFlipHorizontal_Click(object sender, RoutedEventArgs e)
		{
			if (_capture != null)
			{
				_capture.FlipHorizontal = !_capture.FlipHorizontal;
			}
		}



		private void menu_hover(object sender, RoutedEventArgs e)
		{
			captureblocked = true;
		}


		#endregion

		private void menuReplayOpen_Click(object sender, RoutedEventArgs e)
		{
			if(_capturestatus ==  CaptureStatuses.STOPPED)
			{
				if(openvideodialog.ShowDialog() == true)
				{
					if(_capture != null)
					{
						_capture.Dispose();
					}

					try
					{
						cameraStatusRecordingText.Text = "Replaying Video: " + openvideodialog.FileName;

						_capturestatus = CaptureStatuses.REPLAY_ACTIVE;
						_capture = new VideoCapture(openvideodialog.FileName);
						_capture.ImageGrabbed += ProcessFrame;
						host1.Visibility = Visibility.Visible;


						replayframerate = _capture.GetCaptureProperty(CapProp.Fps);
						replaytotalframes = _capture.GetCaptureProperty(CapProp.FrameCount);

						_frame = new Mat();
						_capture.Start();
						FpsTimer.Start();


					}
					catch (NullReferenceException excpt)
					{
						MessageBox.Show(excpt.Message);
					}
				}
			}
		}

		private void menuRecordNew_Click(object sender, RoutedEventArgs e)
		{
			if(_capturestatus == CaptureStatuses.PLAYING)
			{
				if(savevideodialog.ShowDialog() == true)
				{
					//if(_capture != null)
					//{
						//_capture.Dispose();
					//}

					try
					{
						cameraStatusRecordingText.Text = "Recording Video: " + savevideodialog.FileName;
						
						//_capture = new VideoCapture(savevideodialog.FileName);
						//_capture.ImageGrabbed += ProcessFrame;
						//host1.Visibility = Visibility.Visible;

						//recordframewidth = (int)_capture.GetCaptureProperty(CapProp.FrameWidth);
						//recordframeheight = (int)_capture.GetCaptureProperty(CapProp.FrameHeight);

						//recordsize.Width = recordframewidth;
						//recordsize.Height = recordframeheight;

						recordsize.Width = 640;
						recordsize.Height = 480;

						recordframerate = 15;

						//Set up a video writer component
						/*                                        ---USE----
						/* VideoWriter(string fileName, int compressionCode, int fps, int width, int height, bool isColor)
						 *
						 * Compression code. 
						 *      Usually computed using CvInvoke.CV_FOURCC. On windows use -1 to open a codec selection dialog. 
						 *      On Linux, use CvInvoke.CV_FOURCC('I', 'Y', 'U', 'V') for default codec for the specific file name. 
						 * 
						 * Compression code. 
						 *      -1: allows the user to choose the codec from a dialog at runtime 
						 *       0: creates an uncompressed AVI file (the filename must have a .avi extension) 
						 *
						 * isColor.
						 *      true if this is a color video, false otherwise
						 */
						_videowriter = new VideoWriter(savevideodialog.FileName, -1, recordframerate, recordsize, true);
						_capturestatus = CaptureStatuses.RECORD;
						//_capture.Start();
					}
					catch (NullReferenceException excpt)
					{
						MessageBox.Show(excpt.Message);
					}
				}
			}
		}

		private void menuRecordStop_Click(object sender, RoutedEventArgs e)
		{
			_videowriter.Dispose();
			_capturestatus = CaptureStatuses.PLAYING;
		}
	}
}
