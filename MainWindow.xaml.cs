﻿/**********************************************************************************************************************************************
*	File: MainWindow.xaml.cs
*
*	Developed By: Mansel Jeffares
*	First Build: 7 March 2017
*	Current Build:  27 April 2017
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

//Namespaces
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

// Structures and Classes
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
}
//cannot use precomplier defines therefore we use a static class
#endregion

namespace SwarmRoboticsGUI
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		// Variables
		#region
        public Camera camera;

		//fps timer variables
		//private int _fpscount = 0;                                  // number of frames captured in the current second
		private DispatcherTimer FpsTimer = new DispatcherTimer();   // one second timer to calculate and update the fps count

		// video record/replay variables
		OpenFileDialog openvideodialog = new OpenFileDialog();
		SaveFileDialog savevideodialog = new SaveFileDialog();
		private double replayframerate = 0;
		private double replaytotalframes = 0;
		//private int recordframewidth;
		//private int recordframeheight;
		//private double replayframecount;
		//private double replayspeed = 1;
		private int recordframerate = 30;
		private System.Drawing.Size recordsize = new System.Drawing.Size();
		//private DateTime startTime;

		public SerialUARTCommunication serial;
		public XbeeHandler xbee;
		public ProtocolClass protocol;
		//public CameraPopOutWindow popoutCameraWindow;


		#endregion

		///<summary>
		///Window Class
		/// </summary>
		public MainWindow()
		{
			InitializeComponent();

            host1.Visibility = Visibility.Visible;

            camera = new Camera(captureImageBox);
            CvInvoke.UseOpenCL = false;
			PopulateFilters();
			PopulateCameras();

			xbee = new XbeeHandler(this);
			protocol = new ProtocolClass(this);
			serial = new SerialUARTCommunication(this, menuCommunicationPortList, menuCommunicationBaudList, menuCommunicationParityList, menuCommunicationDataList, menuCommunicationStopBitsList, menuCommunicationHandshakeList, menuCommunicationConnect);
			
            //serial._serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

			openvideodialog.Filter = "Video Files|*.avi;*.mp4;*.mpg";
			savevideodialog.Filter = "Video Files|*.avi;*.mp4;*.mpg";
			savevideodialog.Title = "Record: Save As";
			FpsTimer.Tick += FpsTimerTick;
			FpsTimer.Interval = new TimeSpan(0, 0, 1);
			FpsTimer.Start();

			//popoutCameraWindow = new CameraPopOutWindow();
			///example of select folder dialog
			///var selectFolderDialog = new FolderSelectDialog { Title = "Select a folder to save data to" };
			///if (selectFolderDialog.Show())
			///{
			///	rtbSerial.AppendText(selectFolderDialog.FileName.ToString());
			///}
			
		}

        //Programmably Activated Methods
        #region
        /// <summary>
        /// Finds the Connected Cameras by using Directshow.net dll library by carles iloret
        /// As the project is build for x64, only cameras with x64 drivers will be found/displayed
        /// </summary>
        private void PopulateCameras()
		{
            // we dont want to update this if we are connected to a camera
            if (camera.Status != Camera.StatusType.PLAYING && camera.Status != Camera.StatusType.RECORDING)   
			{
                // gets currently connected devices
                DsDevice[] _SystemCameras = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
                // creates a new array of devices    
                webcams = new Video_Device[_SystemCameras.Length];
                // clears cameras from menu                                      
                menuCameraList.Items.Clear();                                                               
                menuCameraConnect.IsEnabled = false;

				// loops through cameras and adds them to menu
				for (int i = 0; i < _SystemCameras.Length; i++)
				{
					webcams[i] = new Video_Device(i, _SystemCameras[i].Name); ;
					MenuItem item = new MenuItem { Header = webcams[i].ToString() };
					item.Click += new RoutedEventHandler(menuCameraListItem_Click);
					item.IsCheckable = true;
					menuCameraList.Items.Add(item);

					// restores currently connect camera selection
					if (item.ToString() == camera.Name)
					{
						item.IsEnabled = true;
						item.IsChecked = true;
						camera.Index = menuCameraList.Items.IndexOf(item);
						menuCameraConnect.IsEnabled = true;
					}
				}

				// displays helpful message if no cameras found
				if (menuCameraList.Items.Count == 0)
				{
					MenuItem nonefound = new MenuItem { Header = "No Cameras Found" };
					menuCameraList.Items.Add(nonefound);
					nonefound.IsEnabled = false;
				}
			}
		}

        // auto matically adds the filters defined in the class into our menu for selecting filters
		private void PopulateFilters()
		{
            //loops through our filters and adds them to our menu
			for(int i =0; i < (int)Camera.FilterType.NUM_FILTERS; i++)
			{
				MenuItem item = new MenuItem { Header = Camera.ToString((Camera.FilterType)i) };
				item.Click += new RoutedEventHandler(menuFilterListItem_Click);
				item.IsCheckable = true;
                //by default select our first filter (no filter)
				if(i == 0)
				{
					item.IsChecked = true;
				}
				menuFilterList.Items.Add(item);
			}
            // add our seperator and settings menu items
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
        private Video_Device[] webcams;
        //private VideoCapture capture;


		#endregion

		// Repeated/Time Activated Methods
		#region

		private void FpsTimerTick(object sender, EventArgs arg)
		{
			//serial._serialPort.Write("Test");

            /// Error message for zero frames
			///if (camera.Status == Camera.StatusType.PLAYING || camera.Status == Camera.StatusType.RECORDING)
			///{
			///	if(camera.Status == Camera.StatusType.PLAYING)
			///	{
			///		if (_fpscount == 0)
			///		{
			///			MessageBoxResult result = CustomMessageBox.ShowYesNo(
			///				"Warning Camera disconnected or significant failure, Please reconnect camera and press continue or stop capturing now",
			///				"Camera Error",
			///				"Continue",
			///				"Stop Capture",
			///				MessageBoxImage.Error
			///				);
			///			//check result do things based on it, add to seperate function
			///		}
			///	}
			///	else if (camera.Status == Camera.StatusType.RECORDING)
			///	{
			///		if (_fpscount == 0)
			///		{
			///			MessageBoxResult result = CustomMessageBox.ShowYesNo(
			///				"Warning Camera disconnected or significant failure, Please reconnect camera and press continue or end the recording now",
			///				"Camera Error",
			///				"Continue",
			///				"End Recording",
			///				MessageBoxImage.Error
			///				);
			///			//check result do things based on it, add to seperate function
			///		}
			///		//flashes recording dot red/black if recroding is in progress 
			///		if (statusRecordingDot.Foreground.Equals(Brushes.Red))
			///		{
			///			statusRecordingDot.Foreground = Brushes.Black;
			///		}
			///		else
			///		{
			///			statusRecordingDot.Foreground = Brushes.Red;
			///		}
			///	}
			///	//updates FPS counter
			///	statusFPS.Text = "FPS: " + _fpscount.ToString();
			///	_fpscount = 0;
			///}

			switch(camera.TimeDisplayMode)
				{
					case Camera.TimeDisplayModeType.CURRENT:
					//statusTime.Text = DateTime.Now.ToString();
					//statusTime.Text = String.Format("{0:d dd HH:mm:ss}" ,DateTime.Now);
					//statusTime.Text = String.Format("{0:T}", DateTime.Now.ToString());
					statusTime.Text = DateTime.Now.ToString("t");
					break;


					case Camera.TimeDisplayModeType.FROM_START:
						if (camera.Status == Camera.StatusType.RECORDING)
						{
						//TimeSpan displayTime = (DateTime.Now - startTime);
						//statusTime.Text = displayTime.ToString(@"dd\.hh\:mm\:ss");

						//statusTime.Text = (DateTime.Now - startTime).ToString(@"dd\.hh\:mm\:ss");


						//displayTime = displayTime.Add(-((TimeSpan)displayTime.Ticks % TimeSpan.TicksPerSecond));
						//statusTime.Text = (DateTime.Now - startTime).ToString("t");
					}
					break;
				}
		}
		#endregion
		
		// User Control Activated Methods
		#region
		private void menuCameraListItem_Click(Object sender, RoutedEventArgs e)
		{
			MenuItem menusender = (MenuItem)sender;
			String menusenderstring = menusender.ToString();

			if (camera.Status == Camera.StatusType.STOPPED && camera.Name != menusenderstring) //also check if the same menu option is clicked twice
			{
				//var allitems = menuCameraList.Items.Cast<System.Windows.Controls.MenuItem>().ToArray();
				MenuItem[] allitems = menuCameraList.Items.OfType<MenuItem>().ToArray();

				foreach (var item in allitems)
				{
					item.IsChecked = false;
					item.IsEnabled = false;
				}

				menusender.IsEnabled = true;
				menusender.IsChecked = true;
                camera.Name = menusender.ToString();
				statusCameraName.Text = menusender.Header.ToString();
                camera.Index = menuCameraList.Items.IndexOf(menusender);
				menuCameraConnect.IsEnabled = true;

			}
			else if (camera.Name == menusenderstring)
			{
                //var allitems = menuCameraList.Items.Cast<System.Windows.Controls.MenuItem>().ToArray();
                MenuItem[] allitems = menuCameraList.Items.OfType<MenuItem>().ToArray();

				foreach (var item in allitems)
				{
					item.IsChecked = false;
					item.IsEnabled = true;
				}
                camera.Name = "No Camera Selected";
				statusCameraName.Text = null;
                camera.Index = -1;
				menuCameraConnect.IsEnabled = false;
			}
		}
		private void menuFilterListItem_Click(object sender, RoutedEventArgs e)
		{
			MenuItem menusender = (MenuItem)sender;
			String menusenderstring = menusender.ToString();

            if (menusenderstring != Camera.ToString(camera.Filter))
            {
                MenuItem[] allitems = menuFilterList.Items.OfType<MenuItem>().ToArray();

                foreach (var item in allitems)
                {
                    item.IsChecked = false;
                }
                menusender.IsChecked = true;
                camera.Filter = (Camera.FilterType)menuFilterList.Items.IndexOf(menusender);
                //
                statusDisplayFilter.Text = Camera.ToString(camera.Filter);
            }
            else if (menusenderstring == Camera.ToString(camera.Filter) && camera.Filter != Camera.FilterType.NONE);
			{
				MenuItem[] allitems = menuFilterList.Items.OfType<MenuItem>().ToArray();

				foreach (var item in allitems)
				{
					item.IsChecked = false;
					item.IsEnabled = true;
				}
			}
		}
		private void MenuItem_MouseEnter(object sender, MouseEventArgs e)
		{
			camera.captureblockedframes++;
			PopulateCameras();
		}
		private void menuCameraConnect_Click(object sender, RoutedEventArgs e)
		{
			if (camera.Status == Camera.StatusType.PLAYING || camera.Status == Camera.StatusType.PAUSED)
			{
				camera.StopCapture();

				MenuItem[] allitems = menuCameraList.Items.Cast<MenuItem>().ToArray();

				foreach (var item in allitems)
				{
					item.IsEnabled = true;
				}
			}
			else if (camera.Status == Camera.StatusType.STOPPED)
			{
				camera.StartCapture();

                MenuItem[] allitems = menuCameraList.Items.OfType<MenuItem>().ToArray();

				foreach (var item in allitems)
				{
					item.IsEnabled = false;
				}
			}
		}
		private void menuCameraOptions_Click(object sender, RoutedEventArgs e)
		{
			//need try/catch or checks 
            if (camera.Name != null)
            {
                try
                {
                    camera.Capture.SetCaptureProperty(CapProp.Settings, 1);
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
			if (camera.Status == Camera.StatusType.PLAYING)
			{
                camera.PauseCapture();
			}
			else if (camera.Status == Camera.StatusType.PAUSED)
			{
				camera.ResumeCapture();
			}
		}
		private void menuFilterFlipVertical_Click(object sender, RoutedEventArgs e)
		{
			if (camera.Capture != null)
			{
                camera.Capture.FlipVertical = !camera.Capture.FlipVertical;
			}
		}
		private void menuFilterFlipHorizontal_Click(object sender, RoutedEventArgs e)
		{
			if (camera.Capture != null)
			{
                camera.Capture.FlipHorizontal = !camera.Capture.FlipHorizontal;
			}
		}
		private void menu_hover(object sender, RoutedEventArgs e)
		{
            // TODO: work out why this was here
			//camera.captureblockedframes++;
		}
        private void menuReplayOpen_Click(object sender, RoutedEventArgs e)
        {
            if (camera.Status == Camera.StatusType.STOPPED)
            {
                if (openvideodialog.ShowDialog() == true)
                {
                    if (camera.Capture != null)
                    {
                        camera.Capture.Dispose();
                    }
                    try
                    {
                        statusRecordingText.Text = "Replaying Video: " + openvideodialog.FileName;

                        camera.Status = Camera.StatusType.REPLAY_ACTIVE;
                        camera.Capture = new VideoCapture(openvideodialog.FileName);
                        camera.Capture.ImageGrabbed += camera.ProcessFrame;
                        host1.Visibility = Visibility.Visible;


                        replayframerate = camera.Capture.GetCaptureProperty(CapProp.Fps);
                        replaytotalframes = camera.Capture.GetCaptureProperty(CapProp.FrameCount);

                        camera.frame = new Mat();
                        camera.Capture.Start();
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
            if (camera.Status == Camera.StatusType.PLAYING)
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
                        camera.videowriter = new VideoWriter(savevideodialog.FileName, -1, recordframerate, recordsize, true);
                        camera.Status = Camera.StatusType.RECORDING;
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
            camera.videowriter.Dispose();
            camera.Status = Camera.StatusType.PLAYING;
            menuCameraConnect.IsEnabled = true;
            menuCameraFreeze.IsEnabled = true;
			menuRecordStop.IsEnabled = false;
			statusRecordingText.Text = "Not Recording";
			statusRecordingDot.Foreground = Brushes.Black;
		}		
        private void menuPlaceHolder_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Sorry Placeholder");
        }
        private void menuDisplayPopOut_Click(object sender, RoutedEventArgs e)
        {
            switch (camera.WindowStatus)
            {
                case Camera.WindowStatusType.POPPED_OUT:
                    camera.popoutWindow.Close();

                    mainGrid.ColumnDefinitions[3].Width = new GridLength(camera.WindowSize);     //set window to the size it had been before it was minimised

                    camera.WindowStatus = Camera.WindowStatusType.MAXIMISED;                         //set variable/flag
                    cameraGridSplitter.IsEnabled = true;                                        //re-enable the grid splitter so its size can be changed

                    //update arrow direction
                    cameraArrowTop.Content = "   >";
                    cameraArrowBottom.Content = "   >";
                    break;
                default:
                    camera.WindowSize = mainGrid.ColumnDefinitions[3].ActualWidth;       //set size of window when it was minimised
                    mainGrid.ColumnDefinitions[3].Width = new GridLength((double)0);    //minimise window (make width = 0)

                    camera.WindowStatus = Camera.WindowStatusType.POPPED_OUT;                  //set variable/flag
                    cameraGridSplitter.IsEnabled = false;                               //disable the grid splitter so window cannont be changed size until it is expanded

                    //update arrow direction
                    cameraArrowTop.Content = "  < ";
                    cameraArrowBottom.Content = "  <  ";

                    //create and show the window
                    camera.popoutWindow = new CameraPopOutWindow();
                    camera.popoutWindow.Show();
                    camera.popoutWindow.captureImageBox.Image = captureImageBox.Image;
                    break;
            }
        }
        private void btnCommunicationTest_Click(object sender, RoutedEventArgs e)
        {
            protocol.SendMessage(ProtocolClass.MESSAGE_TYPES.COMMUNICATION_TEST);
        }
        //minimises camera window when camera minimise button is clicked
        private void CameraMinimise(object sender, MouseButtonEventArgs e)
		{
            switch (camera.WindowStatus)
            {
                case Camera.WindowStatusType.MAXIMISED:
                    camera.WindowSize = mainGrid.ColumnDefinitions[3].ActualWidth;      //set size of window when it was minimised
                    mainGrid.ColumnDefinitions[3].Width = new GridLength((double)0);    //minimise window (make width = 0)

                    camera.WindowStatus = Camera.WindowStatusType.MINIMISED;                  //set variable/flag
                    cameraGridSplitter.IsEnabled = false;                               //disable the grid splitter so window cannont be changed size until it is expanded

                    //update arrow direction
                    cameraArrowTop.Content = "  < ";
                    cameraArrowBottom.Content = "  <  ";
                    break;
                case Camera.WindowStatusType.MINIMISED:
                    mainGrid.ColumnDefinitions[3].Width = new GridLength(camera.WindowSize);     //set window to the size it had been before it was minimised

                    camera.WindowStatus = Camera.WindowStatusType.MAXIMISED;                            //set variable/flag
                    cameraGridSplitter.IsEnabled = true;                                        //re-enable the grid splitter so its size can be changed

                    //update arrow direction
                    cameraArrowTop.Content = "   >";
                    cameraArrowBottom.Content = "   >";
                    break;
                default:
                    break;
            }
		}
        #endregion
    }
}
