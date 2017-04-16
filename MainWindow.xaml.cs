/**********************************************************************************************************************************************
*	File: MainWindow.xaml.cs
*
*	Developed By: Mansel Jeffares
*	First Build: 7 March 2017
*	Current Build:  23 March 2017
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
using Emgu.CV.Structure;
using Emgu.CV.Util;
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

#endregion



/**********************************************************************************************************************************************
* Structures and Classes
**********************************************************************************************************************************************/
#region

/* structure for direct show detected camera/video device
 *
 */
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


//currently a static class, needs to be rewritten as non static Process Frame
/* A Class for handling computer vision processing and visual filters
 * Currently only realy good for filters as computer vision processing 
 * will most likely require additional variables that wont be accessables
 * 
 * To add a new filter simply add:
 *                                  An index for the filter
 *                                  Case for the filter and processing itself
 *                                  The ToString case for the filter
 *                                  And update the number of filters
 * 
 * The menu handling is automatically handled by the PopulateFilters and 
 * menuFilterListItem_Click methods
 */
static public class CaptureFilters
{
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
				//CvInvoke.FindContours(outputframe, mycontours, null, RetrType.External, ChainApproxMethod.ChainApproxNone, 0);
				
				
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




//cannot use precomplier defines therefore we use a static class
static class CaptureStatuses
{
	public const int PLAYING = 0;
	public const int PAUSED = 1;
	public const int STOPPED = 2;
	public const int REPLAY_ACTIVE = 3;
	public const int REPLAY_PAUSED = 4;
	public const int RECORDING = 5;
}



static class TimeDisplayMode
{
	public const int CURRENT = 0;
	public const int FROM_START = 1;
	public const int START = 2;
}



static class CameraWindowStatus
{
	public const int MAXIMISED = 0;
	public const int MINIMISED = 1;
	public const int POPPED_OUT = 2;
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
		private VideoCapture _capture = null;                       // the capture itself (ie camera when recording/viewing or a video file when replaying)
		private int cameradeviceindex = 0;                          // the camera device index
		private Video_Device[] webcams;                             // a list of connected video devices
		private Mat _frame;                                         // openCV Matrix image
		private string currentlyconnectedcamera = null;             // the name of the currently connected camera
		private int _capturestatus = CaptureStatuses.STOPPED;       // the current capture status
		private int filter = CaptureFilters.NO_FILTER;              // the current filter to apply the the capture
		private int captureblockedframes = 0;                       // number of frames to delay drawing to screen
		private Mat outputframe = new Mat();                        // openCV Matric image of filter output

		//fps timer variables
		private int _fpscount = 0;                                  // number of frames captured in the current second
		private DispatcherTimer FpsTimer = new DispatcherTimer();   // one second timer to calculate and update the fps count

		// video record/replay variables
		OpenFileDialog openvideodialog = new OpenFileDialog();
		SaveFileDialog savevideodialog = new SaveFileDialog();
		private double replayframerate = 0;
		private double replaytotalframes = 0;
		//private int recordframewidth;
		//private int recordframeheight;
		private double replayframecount;
		private VideoWriter _videowriter;
		//private Stopwatch _stopwatch;
		private double replayspeed = 1;
		private int recordframerate = 30;
		private System.Drawing.Size recordsize = new System.Drawing.Size();
		private int _timeDisplayMode = TimeDisplayMode.FROM_START;
		private DateTime startTime;
		//public SerialUARTCommunication serial = null;
		public SerialUARTCommunication serial;


		//private SerialPort _serialPort;

		private double cameraWindowSize = 0;
		//private bool cameraWindowMaximised = true;
		private int _cameraWindowStatus = CameraWindowStatus.MAXIMISED;
		public CameraPopOutWindow popoutCameraWindow;

		//System.Windows.Forms.FolderBrowserDialog test;

		//public Mat mycontours = new Mat();
		//VectorCollection mycontours = new VectorCollection();
		//VectorOfVectorOfPoint mycontours = new VectorOfVectorOfPoint();

		#endregion



		///<summary>
		///Window Class
		/// </summary>
		public MainWindow()
		{
			InitializeComponent();      
            CenterWindowOnScreen(0.75);
			CvInvoke.UseOpenCL = false;
			PopulateFilters();
			PopulateCameras();

			serial = new SerialUARTCommunication(menuCommunicationPortList, menuCommunicationBaudList, menuCommunicationParityList, menuCommunicationDataList, menuCommunicationStopBitsList, menuCommunicationHandshakeList, menuCommunicationConnect);
			serial._serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

			openvideodialog.Filter = "Video Files|*.avi;*.mp4;*.mpg";
			savevideodialog.Filter = "Video Files|*.avi;*.mp4;*.mpg";
			savevideodialog.Title = "Record: Save As";
			FpsTimer.Tick += FpsTimerTick;
			FpsTimer.Interval = new TimeSpan(0, 0, 1);
			FpsTimer.Start();


			//popoutCameraWindow = new CameraPopOutWindow();

			//example of select folder dialog
			/* 
			var selectFolderDialog = new FolderSelectDialog { Title = "Select a folder to save data to" };
			
			if (selectFolderDialog.Show())
			{
				rtbSerial.AppendText(selectFolderDialog.FileName.ToString());
			}
			*/
		}



		/**********************************************************************************************************************************************
		* Programmably Activated Methods
		**********************************************************************************************************************************************/
		#region

		//Finds the Connected Cameras by using Directshow.net dll library by carles iloret
		//As the project is build for x64, only cameras with x64 drivers will be found/displayed
		private void PopulateCameras()
		{
            if (_capturestatus != CaptureStatuses.PLAYING && _capturestatus != CaptureStatuses.RECORDING)   //we dont want to update the if we are connected to a camera
			{
				DsDevice[] _SystemCameras = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);      //gets currently connected devices
				webcams = new Video_Device[_SystemCameras.Length];                                          //creates a new array of devices
				menuCameraList.Items.Clear();                                                               //clears cameras from menu
                menuCameraConnect.IsEnabled = false;

				//loops through cameras and adds them to menu
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
						cameradeviceindex = menuCameraList.Items.IndexOf(item);
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


        // starts capturing stream from video camera (update to support video files?, currently we use seperate methods)
		private void StartCapture()
		{
            //if the capture has perviously been used and not disposed of we should dispose of it now
			if (_capture != null)
			{
				_capture.Dispose();
			}

            //if the frame has perviously been used and not disposed of we should dispose of it now     //******************************************************************************* make a clean up method?
            if(_frame != null)
            {
                _frame.Dispose();
            }

			try
			{
				host1.Visibility = Visibility.Visible;              // change the visibility of our winforms host (our stream viewer is inside this)
				_capture = new VideoCapture(cameradeviceindex);     // update the capture object
				_capture.ImageGrabbed += ProcessFrame;              // add event handler for our new capture
				_frame = new Mat();                                 // create a new matrix to hold our image

				menuCameraConnect.Header = "Stop Capture";          // Update the header on our connect/disconnect button
				menuCameraFreeze.IsEnabled = true;                  // enable the freeze frame button
                menuRecordNew.IsEnabled = true;

                
                _capturestatus = CaptureStatuses.PLAYING;           //update our status
				_capture.Start();                                   //start the capture
				//FpsTimer.Start();                                   //start our fps timer
				_fpscount = 1;

				//set start date time as a variable 
				startTime = DateTime.Now;

			}
			catch (NullReferenceException excpt)
			{
				MessageBox.Show(excpt.Message);
			}
		}



		private void StopCapture()
		{
            if (_capturestatus == CaptureStatuses.PLAYING)
            {
                try
                {
                    _capture.Stop();
                    //FpsTimer.Stop();
                    statusFPS.Text = "FPS: ";
                    _capture.Dispose();
                    host1.Visibility = Visibility.Hidden;
                    _capturestatus = CaptureStatuses.STOPPED;
                    menuCameraConnect.Header = "Start Capture";
                    menuCameraFreeze.Header = "Freeze";
                    menuCameraFreeze.IsChecked = false;
                    menuCameraFreeze.IsEnabled = false;
                    menuRecordNew.IsEnabled = false;
                }
                catch (Exception excpt)
                {
                    MessageBox.Show(excpt.Message);
                }
            }
            else
            {
                MessageBox.Show("stop recording before stopping capture");  //we should provide an options/confirmation box
            }
		}


        //but what if were recording. we just dont want to display
		private void PauseCapture()
		{
			try
			{
				_capture.Pause();
				//FpsTimer.Stop();
				statusFPS.Text = "FPS: ";
				_capturestatus = CaptureStatuses.PAUSED;
				menuCameraFreeze.Header = "Un-Freeze";
				menuCameraFreeze.IsChecked = true;
                menuRecordNew.IsEnabled = false;
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
				//FpsTimer.Start();
				_capturestatus = CaptureStatuses.PLAYING;
				menuCameraFreeze.Header = "Freeze";
				menuCameraFreeze.IsChecked = false;
                menuRecordNew.IsEnabled = true;
			}
			catch (Exception excpt)
			{
				MessageBox.Show(excpt.Message);
			}
		}


        // auto matically adds the filters defined in the class into our menu for selecting filters
		private void PopulateFilters()
		{
            //loops through our filters and adds them to our menu
			for(int i =0; i < CaptureFilters.NUM_FILTERS; i++)
			{
				MenuItem item = new MenuItem { Header = CaptureFilters.ToString(i) };
				item.Click += new RoutedEventHandler(menuFilterListItem_Click);
				item.IsCheckable = true;

                //by default select our first filter (no filter)
				if(i == 0)
				{
					item.IsChecked = true;
				}

				menuFilterList.Items.Add(item);
			}

            //add our seperator and settings menu items
			Separator sep = new Separator();
			menuFilterList.Items.Add(sep);

			MenuItem settingsmenuitem = new MenuItem { Header = "Settings" };
			menuFilterList.Items.Add(settingsmenuitem);
            settingsmenuitem.Click += menuPlaceHolder_Click;
		}



		VectorOfVectorOfPoint mycontours = new VectorOfVectorOfPoint();
		VectorOfVectorOfPoint largecontours = new VectorOfVectorOfPoint();
		VectorOfVectorOfPoint approx = new VectorOfVectorOfPoint();
		//double area;
		Mat testFrame = new Mat();


		// menu will not display correctly if image refreshed while menu is being rendered 
		// blocking display for a frame if the menu has been clicked/hovered over
		private void DisplayFrame()
		{
			if (captureblockedframes == 0)
			{
				_fpscount++;

				switch(_cameraWindowStatus)
				{
					case CameraWindowStatus.MAXIMISED:
						captureImageBox.Image = CaptureFilters.Process(filter, _frame, outputframe);
						//captureImageBox2.Image = CaptureFilters.Process(filter, _frame, outputframe);
						//testFrame = _frame;
						//captureImageBox2.Image = testFrame;

						/*
						//testing code for detection of hexagon
						_frame = CaptureFilters.Process(CaptureFilters.BRAE_EDGES, _frame, outputframe);

						CvInvoke.FindContours(_frame, mycontours, null, RetrType.External, ChainApproxMethod.ChainApproxNone);

						largecontours.Clear();
						for(int i = 0; i < mycontours.Size; i++)
						{
							area = Math.Abs(CvInvoke.ContourArea(mycontours[i]));
							if(area > 100 && area <= 100000)
							{
								largecontours.Push(mycontours[i]);
							}
						}

						approx.Push(largecontours);
						for(int i = 0; i < largecontours.Size; i++)
						{
							CvInvoke.ApproxPolyDP(largecontours[i], approx[i], 4.0, true);

							if(approx[i].Size == 6)
							{
								bool isHexagon = true;
								System.Drawing.Point[] pts = approx[i].ToArray();
								LineSegment2D[] edges = Emgu.CV.PointCollection.PolyLine(pts, true);

								for(int j = 0; j < edges.Length; j++)
								{
									double angle = Math.Abs(edges[(j + 1) % edges.Length].GetExteriorAngleDegree(edges[j]));
									if(angle < 40 || angle > 80)
									{
										isHexagon = false;
									}
								}

								if (isHexagon)
								{
									CvInvoke.DrawContours(_frame, approx, i, new MCvScalar(255, 0, 0), 5);
								}

								
								System.Drawing.Point a = approx[i][0];
								System.Drawing.Point b = approx[i][1];
								System.Drawing.Point c = approx[i][2];
								System.Drawing.Point d = approx[i][3];
								
								//System.Drawing.Point center = (c.X - a.x, c.Y - a.Y);

								
								CvInvoke.Circle(_frame, a, 10, new MCvScalar(255, 0, 0),5, LineType.EightConnected, 0);
								CvInvoke.Circle(_frame, b, 10, new MCvScalar(255, 0, 0), 5, LineType.EightConnected, 0);
								CvInvoke.Circle(_frame, c, 10, new MCvScalar(255, 0, 0), 5, LineType.EightConnected, 0);
								CvInvoke.Circle(_frame, d, 10, new MCvScalar(255, 0, 0), 5, LineType.EightConnected, 0);
								
								

							}
						}
						captureImageBox.Image = _frame;
						*/
						break;

					case CameraWindowStatus.MINIMISED:

						break;

					case CameraWindowStatus.POPPED_OUT:

						Dispatcher.Invoke(() =>
						{
							//((CameraPopOutWindow)System.Windows.Application.Current.cameraPopOutWindow).captureImageBox.Image = CaptureFilters.Process(filter, _frame, outputframe);
							popoutCameraWindow.captureImageBox.Image = CaptureFilters.Process(filter, _frame, outputframe);
						});
						break;
				}


				
			}
			else
			{
				captureblockedframes--;
			}
		}


        // automatically centres the window and makes it a percentage (size) of the screen              //should add maximising support if size is one or seperate parameter
        private void CenterWindowOnScreen(double size)
        {
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            double windowWidth = screenWidth * size;
            double windowHeight = screenHeight * size;

            this.Width = windowWidth;
            this.Height = windowHeight;
            this.Left = (screenWidth / 2) - (windowWidth / 2);
            this.Top = (screenHeight / 2) - (windowHeight / 2);
        }

		#endregion



		/**********************************************************************************************************************************************
		* Repeated/Time Activated Methods
		**********************************************************************************************************************************************/
		#region

        //process's the currently recieved frame 
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
				else if(_capturestatus == CaptureStatuses.RECORDING)
				{
					DisplayFrame();
					if (_videowriter.Ptr != IntPtr.Zero)
					{
                        //need to write based on the option selected
						//_videowriter.Write(_frame);
						_videowriter.Write(CaptureFilters.Process(filter, _frame, outputframe));
					}
				}
			}
		}



		private void FpsTimerTick(object sender, EventArgs arg)
		{
			//serial._serialPort.Write("Test");

			if (_capturestatus == CaptureStatuses.PLAYING || _capturestatus == CaptureStatuses.RECORDING)
			{
				if(_capturestatus == CaptureStatuses.PLAYING)
				{
					if (_fpscount == 0)
					{
						MessageBoxResult result = CustomMessageBox.ShowYesNo(
							"Warning Camera disconnected or significant failure, Please reconnect camera and press continue or stop capturing now",
							"Camera Error",
							"Continue",
							"Stop Capture",
							MessageBoxImage.Error
							);
						//check result do things based on it, add to seperate function
					}
				}
				else if (_capturestatus == CaptureStatuses.RECORDING)
				{
					if (_fpscount == 0)
					{
						MessageBoxResult result = CustomMessageBox.ShowYesNo(
							"Warning Camera disconnected or significant failure, Please reconnect camera and press continue or end the recording now",
							"Camera Error",
							"Continue",
							"End Recording",
							MessageBoxImage.Error
							);
						//check result do things based on it, add to seperate function
					}


					//flashes recording dot red/black if recroding is in progress 
					if (statusRecordingDot.Foreground.Equals(Brushes.Red))
					{
						statusRecordingDot.Foreground = Brushes.Black;
					}
					else
					{
						statusRecordingDot.Foreground = Brushes.Red;
					}
				}

				



				//updates FPS counter
				statusFPS.Text = "FPS: " + _fpscount.ToString();
				_fpscount = 0;
			}


			switch(_timeDisplayMode)
				{
					case TimeDisplayMode.CURRENT:
					//statusTime.Text = DateTime.Now.ToString();
					//statusTime.Text = String.Format("{0:d dd HH:mm:ss}" ,DateTime.Now);
					//statusTime.Text = String.Format("{0:T}", DateTime.Now.ToString());
					statusTime.Text = DateTime.Now.ToString("t");
					break;


					case TimeDisplayMode.FROM_START:
						if (_capturestatus == CaptureStatuses.RECORDING)
						{
						//TimeSpan displayTime = (DateTime.Now - startTime);
						//statusTime.Text = displayTime.ToString(@"dd\.hh\:mm\:ss");

						statusTime.Text = (DateTime.Now - startTime).ToString(@"dd\.hh\:mm\:ss");


						//displayTime = displayTime.Add(-((TimeSpan)displayTime.Ticks % TimeSpan.TicksPerSecond));
						//statusTime.Text = (DateTime.Now - startTime).ToString("t");
					}
					break;
				}


		}

		#endregion



		/**********************************************************************************************************************************************
		* User Control Activated Methods
		**********************************************************************************************************************************************/
		#region

		private void menuCameraListItem_Click(Object sender, RoutedEventArgs e)
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
				statusCameraName.Text = menusender.Header.ToString();
				cameradeviceindex = menuCameraList.Items.IndexOf(menusender);
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
				statusCameraName.Text = null;
				cameradeviceindex = -1;
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
				statusDisplayFilter.Text = CaptureFilters.ToString(filter);
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
			captureblockedframes++;
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
            if (currentlyconnectedcamera != null)
            {
                try
                {
                    _capture.SetCaptureProperty(CapProp.Settings, 1);
                }
                catch (Exception excpt)
                {
                    MessageBox.Show(excpt.Message);
                }
            }
            else
            {
                MessageBox.Show("No Currently Connected Camera!");
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
			captureblockedframes++;
		}



        private void menuReplayOpen_Click(object sender, RoutedEventArgs e)
        {
            if (_capturestatus == CaptureStatuses.STOPPED)
            {
                if (openvideodialog.ShowDialog() == true)
                {
                    if (_capture != null)
                    {
                        _capture.Dispose();
                    }

                    try
                    {
                        statusRecordingText.Text = "Replaying Video: " + openvideodialog.FileName;

                        _capturestatus = CaptureStatuses.REPLAY_ACTIVE;
                        _capture = new VideoCapture(openvideodialog.FileName);
                        _capture.ImageGrabbed += ProcessFrame;
                        host1.Visibility = Visibility.Visible;


                        replayframerate = _capture.GetCaptureProperty(CapProp.Fps);
                        replaytotalframes = _capture.GetCaptureProperty(CapProp.FrameCount);

                        _frame = new Mat();
                        _capture.Start();
                        //FpsTimer.Start();


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
            if (_capturestatus == CaptureStatuses.PLAYING)
            {
                if (savevideodialog.ShowDialog() == true)
                {
                    try
                    {
                        statusRecordingText.Text = "Recording Video: " + savevideodialog.FileName;

                        //_capture = new VideoCapture(savevideodialog.FileName);
                        //_capture.ImageGrabbed += ProcessFrame;
                        //host1.Visibility = Visibility.Visible;

                        //recordframewidth = (int)_capture.GetCaptureProperty(CapProp.FrameWidth);
                        //recordframeheight = (int)_capture.GetCaptureProperty(CapProp.FrameHeight);

                        //recordsize.Width = recordframewidth;
                        //recordsize.Height = recordframeheight;

                        menuCameraConnect.IsEnabled = false;
                        menuCameraFreeze.IsEnabled = false;  //************************************************************************ we should be able to pause display while keeping recording running

                        recordsize.Width = 640;
                        recordsize.Height = 480;

                        //recordframerate = 15;

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
                        _capturestatus = CaptureStatuses.RECORDING;
						menuRecordStop.IsEnabled = true;
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
            menuCameraConnect.IsEnabled = true;
            menuCameraFreeze.IsEnabled = true;
			menuRecordStop.IsEnabled = false;
			statusRecordingText.Text = "Not Recording";
			statusRecordingDot.Foreground = Brushes.Black;
		}


		#endregion

        private void menuPlaceHolder_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Sorry Placeholder");
        }



		//minimises camera window when camera minimise button is clicked
		private void CameraMinimise(object sender, MouseButtonEventArgs e)
		{
			if (_cameraWindowStatus == CameraWindowStatus.MAXIMISED)
			{
				cameraWindowSize = mainGrid.ColumnDefinitions[2].ActualWidth;		//set size of window when it was minimised
				mainGrid.ColumnDefinitions[2].Width = new GridLength((double)0);    //minimise window (make width = 0)

				_cameraWindowStatus = CameraWindowStatus.MINIMISED;                  //set variable/flag
				cameraGridSplitter.IsEnabled = false;								//disable the grid splitter so window cannont be changed size until it is expanded

				//update arrow direction
				cameraArrowTop.Content = "  < ";
				cameraArrowBottom.Content = "  <  ";
			}
			else if(_cameraWindowStatus == CameraWindowStatus.MINIMISED)
			{
				mainGrid.ColumnDefinitions[2].Width = new GridLength(cameraWindowSize);     //set window to the size it had been before it was minimised

				_cameraWindowStatus = CameraWindowStatus.MAXIMISED;							//set variable/flag
				cameraGridSplitter.IsEnabled = true;										//re-enable the grid splitter so its size can be changed
												
				//update arrow direction
				cameraArrowTop.Content = "   >";
				cameraArrowBottom.Content = "   >";
			}
		}

		private void menuDisplayPopOut_Click(object sender, RoutedEventArgs e)
		{
			

			if(_cameraWindowStatus != CameraWindowStatus.POPPED_OUT)
			{
				cameraWindowSize = mainGrid.ColumnDefinitions[2].ActualWidth;       //set size of window when it was minimised
				mainGrid.ColumnDefinitions[2].Width = new GridLength((double)0);    //minimise window (make width = 0)

				_cameraWindowStatus = CameraWindowStatus.POPPED_OUT;                  //set variable/flag
				cameraGridSplitter.IsEnabled = false;                               //disable the grid splitter so window cannont be changed size until it is expanded

				//update arrow direction
				cameraArrowTop.Content = "  < ";
				cameraArrowBottom.Content = "  <  ";
				

				//create and show the window
				popoutCameraWindow = new CameraPopOutWindow();
				popoutCameraWindow.Show();
			}
			else
			{
				popoutCameraWindow.Close();


				mainGrid.ColumnDefinitions[2].Width = new GridLength(cameraWindowSize);     //set window to the size it had been before it was minimised

				_cameraWindowStatus = CameraWindowStatus.MAXIMISED;                         //set variable/flag
				cameraGridSplitter.IsEnabled = true;                                        //re-enable the grid splitter so its size can be changed

				//update arrow direction
				cameraArrowTop.Content = "   >";
				cameraArrowBottom.Content = "   >";
				
			}
		}

		
	}
}
