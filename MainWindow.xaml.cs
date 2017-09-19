/**********************************************************************************************************************************************
*	File: MainWindow.xaml.cs
*
*	Developed By: Mansel Jeffares and Brae Hartley
*	First Build: 7 March 2017
*	Current Build: 12 September 2017
*
*	Description :
*		Does most of the important UI stuff
*		Developed for a final year university project at Auckland University of Technology (AUT), New Zealand
*		
*	Useage :
*		
*
*	Limitations :
*		Build for x64, .NET 4.5.2
*   
*		Naming Conventions:
*			Variables, camelCase, start lower case, subsequent words also upper case, if another object goes by the same name, then also with an underscore
*			Methods, PascalCase, start upper case, subsequent words also upper case
*			Constants, all upper case, unscores for seperation
* 
**********************************************************************************************************************************************/

#region notestoself

// MANSEL: This is an example of a Mansel task
// BRAE: Use this to get Brae to do something for once
// TODO: This is for general things that need doing
// UNDONE: This is life

#endregion


/**********************************************************************************************************************************************
* Namespaces
**********************************************************************************************************************************************/
#region

using AForge.Video;
using AForge.Video.DirectShow;
using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.Util;
using folderHack;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using XbeeHandler;

#endregion


namespace SwarmRoboticsGUI
{
    public enum WindowStatusType { MAXIMISED, MINIMISED, POPPED_OUT };
    public enum TimeDisplayModeType { CURRENT, FROM_START, START };



	public partial class MainWindow : Window
	{		
		// Declarations
		#region Public Properties
		// TODO: comment declarations
		public Camera camera1;
		public SerialUARTCommunication serial;
		public XbeeAPI xbee;
		public ProtocolClass protocol;
		public CommunicationManager commManger;
		public CameraPopOutWindow popoutWindow;
		public OverlayWindow overlayWindow;
        public SwarmManager swarmManager;
		public Dictionary<string, UInt64> robotsDictionary;
        public List<Item> RobotList;
        


        public int HueLower { get; set; }
        public int HueUpper { get; set; }


		public WindowStatusType WindowStatus { get; set; }
		public TimeDisplayModeType TimeDisplayMode { get; set; }
		public double WindowSize { get; set; }
        #endregion

        #region Private Properties
        // one second timer to calculate and update the fps count
        private DispatcherTimer InterfaceTimer;
		//
		private OpenFileDialog openvideodialog = new OpenFileDialog();
		private SaveFileDialog savevideodialog = new SaveFileDialog();
		private FilterInfoCollection VideoDevices { get; set; }
		private VideoCaptureDevice VideoDevice { get; set; }
	#endregion


		// Main
		public MainWindow()
		{
			InitializeComponent();
			DataContext = this;

			//
			CvInvoke.UseOpenCL = true;
			//
			camera1 = new Camera();
			xbee = new XbeeAPI(this);
			protocol = new ProtocolClass(this);
			serial = new SerialUARTCommunication(this, menuCommunication);
			commManger = new CommunicationManager(this, serial, xbee, protocol);			

            //
            PopulateFilters();
            PopulateOverlays();
            PopulateCameras();
            PopulateSources();
            PopulateRobots();

            swarmManager = new SwarmManager(this);

            overlayWindow = new OverlayWindow(this);

			dispSelectRobot.ItemsSource = RobotList;

			//
			openvideodialog.Filter = "Video Files|*.avi;*.mp4;*.mpg";
            savevideodialog.Filter = "Video Files|*.avi;*.mp4;*.mpg";
            savevideodialog.Title = "Record: Save As";
            //
            InterfaceTimer = new DispatcherTimer();
            InterfaceTimer.Tick += Interface_Tick;
            InterfaceTimer.Interval = new TimeSpan(0, 0, 1);
            InterfaceTimer.Start();
            //
            TimeDisplayMode = TimeDisplayModeType.CURRENT;
            WindowStatus = WindowStatusType.MAXIMISED;


            //var what = ImageProcessing.TestImage;
            // TEMP: display overlay on starup for debugging
            overlayWindow.Show();
            camera1.Process += new EventHandler(DrawCameraFrame);

            // BRAE: Default setup for testing
            overlayWindow.Display1.Source = SourceType.CAMERA;
            //camera1.Index = 1;
            // This will run the camera at 640x480
            //camera1.StartCapture();

            setupSystemTest();
		}

        #region Public Methods

        public void ToggleCameraWindow()
        {
            switch (WindowStatus)
            {
                case WindowStatusType.POPPED_OUT:
                    popoutWindow.Close();
                    //set window to the size it had been before it was minimised
                    mainGrid.ColumnDefinitions[3].Width = new GridLength(WindowSize);
                    //set variable/flag
                    WindowStatus = WindowStatusType.MAXIMISED;
                    //re-enable the grid splitter so its size can be changed
                    cameraGridSplitter.IsEnabled = true;
                    // update arrow direction
                    displayArrowTop.Content = "   >";
                    displayArrowBottom.Content = "   >";
                    // TEMP: toggles the name of the button
                    menuDisplayPopOut.Header = "Pop Out Window";

                    break;
                default:
                    // set size of window to original
                    WindowSize = mainGrid.ColumnDefinitions[3].ActualWidth;
                    // minimise window (make width = 0)     
                    mainGrid.ColumnDefinitions[3].Width = new GridLength((double)0);
                    // change camera window status to popped out
                    WindowStatus = WindowStatusType.POPPED_OUT;
                    // disable the grid splitter so window cannont be changed size until it is expanded               
                    cameraGridSplitter.IsEnabled = false;
                    // update arrow direction
                    displayArrowTop.Content = "  < ";
                    displayArrowBottom.Content = "  <  ";
                    // TEMP: toggles the name of the button
                    menuDisplayPopOut.Header = "Pop In Window";
                    // create and show the window
                    if (camera1.Status == StatusType.PLAYING)
                    {
                        popoutWindow = new CameraPopOutWindow(this);
                        popoutWindow.Show();
                        camera1.StartCapture();
                    }
                    else
                    {
                        popoutWindow = new CameraPopOutWindow(this);
                        popoutWindow.Show();
                    }
                    break;
            }
        }

        #endregion

        #region Private Methods

        private void PopulateCameras()
        {
            // we dont want to update this if we are connected to a camera
            if (camera1.Status != StatusType.PLAYING && camera1.Status != StatusType.RECORDING)
            {
                // gets currently connected devices
                VideoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                // clears cameras from menu                                      
                menuCameraList.Items.Clear();
                menuCameraConnect.IsEnabled = false;
                menuCameraCapabilityList.IsEnabled = false;

                // loops through cameras and adds them to menu
                //for (int i = 0; i < VideoDevices.Count; i++)
                for (int i = VideoDevices.Count; i > 0; i--)
                {
                    MenuItem item = new MenuItem { Header = VideoDevices[i-1].Name };
                    item.Click += new RoutedEventHandler(menuCameraListItem_Click);
                    item.IsCheckable = true;
                    menuCameraList.Items.Add(item);

                    // restores currently connect camera selection
                    if (item.ToString() == camera1.Name)
                    {
                        item.IsEnabled = true;
                        item.IsChecked = true;
                        camera1.Index = menuCameraList.Items.IndexOf(item);
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

        private void PopulateCameraCapabilities()
        {
            // we dont want to update this if we are connected to a camera
            if (camera1.Status == StatusType.STOPPED)
            {
                // clears cameras from menu                                      
                menuCameraCapabilityList.Items.Clear();
                // loops through cameras video options and adds them to menu
                foreach (VideoCapabilities capabilityInfo in VideoDevice.VideoCapabilities)
                {
                    MenuItem item = new MenuItem {
                        Header = string.Format("{0} by {1} @ {2} FPS", 
                        capabilityInfo.FrameSize.Width,
                        capabilityInfo.FrameSize.Height,
                        capabilityInfo.AverageFrameRate) };
                    item.Click += new RoutedEventHandler(menuCameraCapabilityListItem_Click);
                    item.IsCheckable = true;
                    menuCameraCapabilityList.Items.Add(item);
                }

                // displays "helpful" message if no options are found
                if (menuCameraCapabilityList.Items.Count == 0)
                {
                    MenuItem nonefound = new MenuItem { Header = "No" };
                    menuCameraCapabilityList.Items.Add(nonefound);
                    nonefound.IsEnabled = false;
                }
                else
                    (menuCameraCapabilityList.Items[camera1.CapabilityIndex] as MenuItem).IsChecked = true;
            }
        }

        private void PopulateFilters()
        {
            //loops through our filters and adds them to our menu
            foreach (FilterType Filter in Enum.GetValues(typeof(FilterType)))
            {
                MenuItem item = new MenuItem { Header = EnumUtils<FilterType>.GetDescription(Filter)};
                item.Click += new RoutedEventHandler(menuFilterListItem_Click);
                item.IsCheckable = true;
                //by default select our first filter (no filter)
                if (Filter == FilterType.NONE)
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

        private void PopulateOverlays()
        {
            //loops through our filters and adds them to our menu
            foreach (OverlayType Overlay in Enum.GetValues(typeof(OverlayType)))
            {
                MenuItem item = new MenuItem { Header = EnumUtils<OverlayType>.GetDescription(Overlay) };
                item.Click += new RoutedEventHandler(menuOverlayListItem_Click);
                item.IsCheckable = true;
                //by default select our first filter (no filter)
                if (Overlay == OverlayType.NONE)
                {
                    item.IsChecked = true;
                }
                menuOverlayList.Items.Add(item);
            }
            // add our seperator and settings menu items
            Separator sep = new Separator();
            menuOverlayList.Items.Add(sep);

            MenuItem settingsmenuitem = new MenuItem { Header = "Settings" };
            menuOverlayList.Items.Add(settingsmenuitem);
            settingsmenuitem.Click += menuPlaceHolder_Click;
        }

        private void PopulateSources()
        {
            //loops through our filters and adds them to our menu
            foreach (SourceType Source in Enum.GetValues(typeof(SourceType)))
            {
                MenuItem item = new MenuItem { Header = EnumUtils<SourceType>.GetDescription(Source) };
                item.Click += new RoutedEventHandler(menuSourceListItem_Click);
                item.IsCheckable = true;
                //by default select our first source (no source)
                if (Source == SourceType.NONE)
                {
                    item.IsChecked = true;
                }
                menuSourceList.Items.Add(item);
            }
            // add our seperator and settings menu items
            Separator sep = new Separator();
            menuSourceList.Items.Add(sep);

            MenuItem settingsmenuitem = new MenuItem { Header = "Settings" };
            menuSourceList.Items.Add(settingsmenuitem);
            settingsmenuitem.Click += menuPlaceHolder_Click;
        }

        private void PopulateRobots()
        {
            RobotList = new List<Item>();
            // BRAE: add drawing.color as "ID"
            // MANSEL: add drawing.color as "ID"
            // TODO: add drawing.color as "ID"
            //System.Drawing.Color.SaddleBrown
            RobotList.Add(new RobotItem("Red Robot", 0x0013A2004147F9DD, "Red", 0));
            RobotList.Add(new RobotItem("Yellow Robot", 0x0013A200415B8C38, "Yellow", 1));
            RobotList.Add(new RobotItem("Purple Robot", 0x0013A200415B8BDD, "Purple", 2));
            RobotList.Add(new RobotItem("Light Blue Robot", 0x0013A2004152F256, "Cyan", 3));
            RobotList.Add(new RobotItem("Dark Blue Robot", 0x0013A200415B8C3A, "MidnightBlue", 4));
			RobotList.Add(new RobotItem("Brown Robot", 0x0013A20041065FB3, "SaddleBrown", 5));
            RobotList.Add(new RobotItem("Pink Robot", 0x0013A200415B8C18, "Plum", 6));		
			RobotList.Add(new RobotItem("Orange Robot", 0x0013A200415B8BE5, "Orange", 7));

            RobotList.Add(new ChargingDockItem("Tower Base Station", 0x0013A200415B8C2A, "Lime"));
            RobotList.Add(new CommunicationItem("Broadcast", 0x000000000000FFFF, "White"));
        }

        private void DisplayTime()
        {
            switch (TimeDisplayMode)
            {
                case TimeDisplayModeType.CURRENT:
                    statusTime.Text = DateTime.Now.ToString("t");
                    statusTime.Text = DateTime.Now.ToString();
                    statusTime.Text = String.Format("{0:d dd HH:mm:ss}", DateTime.Now);
                    statusTime.Text = String.Format("{0:T}", DateTime.Now.ToString());
                    break;

                case TimeDisplayModeType.FROM_START:
                    if (camera1.Status == StatusType.RECORDING)
                    {
                        statusTime.Text = (camera1.RecordingTime).ToString(@"dd\.hh\:mm\:ss");
                    }
                    break;
            }
        }

        #endregion


        #region Time Events

        private void Interface_Tick(object sender, EventArgs arg)
        {
            // BRAE: You hate this block of code. Does it belong here? Maybe? Does it even work? It did. Replace its functionality elsewhere? somewhere more appropiate like imgproc?
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
            statusFPS.Text = camera1.Fps.ToString();

            DisplayTime();
        }

        private void DrawCameraFrame(object sender, EventArgs e)
        {
            var Frame = sender as UMat;

            switch (overlayWindow.Display1.Source)
            {
                case SourceType.NONE:
                    break;
                case SourceType.CAMERA:
                    // Make sure there is a frame
                    if (Frame != null)
                    {
                        //Apply the currently selected filter
                        if (camera1.Filter != FilterType.NONE)
                        {
                            var proc = new Mat();
                            ImageProcessing.ProcessFilter(Frame, proc, camera1.Filter, HueLower, HueUpper);
                            if (proc != null)
                                captureImageBox.Image = proc;
                        }
                        else
                            captureImageBox.Image = Frame;
                    }
                    break;
                case SourceType.CUTOUTS:
                    // Draw the testimage to the overlay imagebox
                    if (ImageProcessing.TestImage != null)
                    {
                        captureImageBox.Image = (UMat)ImageProcessing.TestImage;
                    }
                    break;
                default:
                    break;
            }
        }
        
        #endregion

        #region Input Handlers

        //Display Menu
        private void menuFilterListItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menusender = (MenuItem)sender;
            string menusenderstring = menusender.ToString();

            if (menusenderstring != EnumUtils<FilterType>.GetDescription(camera1.Filter))
            {
                MenuItem[] allitems = menuFilterList.Items.OfType<MenuItem>().ToArray();

                foreach (var item in allitems)
                {
                    item.IsChecked = false;
                }
                menusender.IsChecked = true;
                camera1.Filter = (FilterType)menuFilterList.Items.IndexOf(menusender);
                //
                statusDisplayFilter.Text = EnumUtils<FilterType>.GetDescription(camera1.Filter);
            }
            else if (camera1.Filter != FilterType.NONE)
            {
                MenuItem[] allitems = menuFilterList.Items.OfType<MenuItem>().ToArray();

                foreach (var item in allitems)
                {
                    item.IsChecked = false;
                    item.IsEnabled = true;
                }
            }
        }

        private void menuOverlayListItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menusender = (MenuItem)sender;
            string menusenderstring = menusender.ToString();

            if (menusenderstring != EnumUtils<OverlayType>.GetDescription(overlayWindow.Display1.Overlay))
            {
                MenuItem[] allitems = menuOverlayList.Items.OfType<MenuItem>().ToArray();

                foreach (var item in allitems)
                {
                    item.IsChecked = false;
                }
                menusender.IsChecked = true;
                overlayWindow.Display1.Overlay = (OverlayType)menuOverlayList.Items.IndexOf(menusender);
                // TODO: Not sure where to display this right now
                //statusDisplayFilter.Text = ImageDisplay.ToString(overlayWindow.Display.Overlay);
            }
        }

        private void menuSourceListItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menusender = (MenuItem)sender;
            string menusenderstring = menusender.ToString();

            if (menusenderstring != EnumUtils<SourceType>.GetDescription(overlayWindow.Display1.Source))
            {
                MenuItem[] allitems = menuSourceList.Items.OfType<MenuItem>().ToArray();

                foreach (var item in allitems)
                {
                    item.IsChecked = false;
                }
                menusender.IsChecked = true;
                overlayWindow.Display1.Source = (SourceType)menuSourceList.Items.IndexOf(menusender);
                // TODO: Not sure where to display this right now
                //statusDisplayFilter.Text = ImageDisplay.ToString(overlayWindow.Display.Overlay);
            }
        }

        private void menuDisplayPopOut_Click(object sender, RoutedEventArgs e)
        {
            ToggleCameraWindow();
        }


        //Camera menu
        private void menuCameraListItem_Click(object sender, RoutedEventArgs e)
        {
            var menusender = sender as MenuItem;
            string menusenderstring = menusender.ToString();
            // Make sure a capture isn't running.
            if (camera1.Status == StatusType.STOPPED)
            {
                // If it isn't already selected
                if (camera1.Name != menusenderstring)
                {
                    MenuItem[] allitems = menuCameraList.Items.OfType<MenuItem>().ToArray();

                    foreach (var item in allitems) item.IsChecked = false;
                    // Display feedback to user
                    menusender.IsChecked = true;
                    statusCameraName.Text = menusender.Header.ToString();
                    // Capture can be started
                    menuCameraConnect.IsEnabled = true;
                    // Resolution can be selected
                    menuCameraCapabilityList.IsEnabled = true;
                    // Update camera
                    camera1.Name = menusender.ToString();
                    camera1.Index = menuCameraList.Items.IndexOf(menusender);

                    VideoDevice = new VideoCaptureDevice(VideoDevices[camera1.Index].MonikerString);
                    

                    camera1.CapabilityIndex = VideoDevice.VideoCapabilities.Length - 1;

                    // Populate resolution options
                    PopulateCameraCapabilities();
                }
            }
        }

        private void menuCameraCapabilityListItem_Click(object sender, RoutedEventArgs e)
        {
            var menusender = sender as MenuItem;

            if (camera1.Status == StatusType.STOPPED)
            {
                // If it isn't already selected
                if (camera1.CapabilityIndex != menuCameraCapabilityList.Items.IndexOf(menusender))
                {
                    // Uncheck all options
                    MenuItem[] allitems = menuCameraCapabilityList.Items.OfType<MenuItem>().ToArray();
                    foreach (var item in allitems) item.IsChecked = false;
                    // Display feedback to user
                    menusender.IsChecked = true;
                    // BRAE: Set selected resolution here
                    camera1.CapabilityIndex = menuCameraCapabilityList.Items.IndexOf(menusender);
                }
            }
        }

        private void menuCameraConnect_Click(object sender, RoutedEventArgs e)
        {
            if (camera1.Status == StatusType.PLAYING || camera1.Status == StatusType.PAUSED)
            {
                // Stop capturing
                camera1.StopCapture();
                //
                menuCameraConnect.Header = "Start Capture";
                menuCameraFreeze.Header = "Freeze";
                menuCameraFreeze.IsChecked = false;
                menuCameraFreeze.IsEnabled = false;
                menuRecordNew.IsEnabled = false;
                //
                MenuItem[] allitems = menuCameraList.Items.Cast<MenuItem>().ToArray();
                foreach (var item in allitems)
                {
                    item.IsEnabled = true;
                }
                MenuItem[] allitems2 = menuCameraCapabilityList.Items.Cast<MenuItem>().ToArray();
                foreach (var item in allitems2)
                {
                    item.IsEnabled = true;
                }
            }
            else if (camera1.Status == StatusType.STOPPED)
            {
                // Start capturing
                camera1.StartCapture();
                //
                menuCameraConnect.Header = "Stop Capture";          // Update the header on our connect/disconnect button
                // TODO: What should this say?
                menuCameraFreeze.Header = "Freeze";
                menuCameraFreeze.IsEnabled = true;                  // enable the freeze frame button
                menuRecordNew.IsEnabled = true;
                //
                MenuItem[] allitems = menuCameraList.Items.OfType<MenuItem>().ToArray();
                foreach (var item in allitems)
                {
                    item.IsEnabled = false;
                }
                MenuItem[] allitems2 = menuCameraCapabilityList.Items.Cast<MenuItem>().ToArray();
                foreach (var item in allitems2)
                {
                    item.IsEnabled = false;
                }
            }
        }

        private void menuCameraOptions_Click(object sender, RoutedEventArgs e)
        {
            camera1.OpenSettings();
        }

        private void menuCameraFreeze_Click(object sender, RoutedEventArgs e)
        {
            if (camera1.Status == StatusType.PLAYING)
            {
                camera1.PauseCapture();
            }
            else if (camera1.Status == StatusType.PAUSED)
            {
                camera1.ResumeCapture();
            }
        }

        private void menuFilterFlipVertical_Click(object sender, RoutedEventArgs e)
        {
            camera1.FlipVertical();
        }

        private void menuFilterFlipHorizontal_Click(object sender, RoutedEventArgs e)
        {
            camera1.FlipHorizontal();
        }

        // Replay menu
        private void menuReplayOpen_Click(object sender, RoutedEventArgs e)
        {
            if (camera1.Status == StatusType.STOPPED)
            {
                if (openvideodialog.ShowDialog() == true)
                {
                    try
                    {
                        statusRecordingText.Text = "Replaying Video: " + openvideodialog.FileName;
                        camera1.StartReplaying(openvideodialog.FileName);
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
            if (camera1.Status == StatusType.PLAYING)
            {
                if (savevideodialog.ShowDialog() == true)
                {
                    //
                    camera1.StartRecording(savevideodialog.FileName);
                    //
                    statusRecordingText.Text = "Recording Video: " + savevideodialog.FileName;
                    menuRecordStop.IsEnabled = true;
                    menuCameraConnect.IsEnabled = false;
                    menuCameraFreeze.IsEnabled = false;
                }
            }
        }

        private void menuRecordStop_Click(object sender, RoutedEventArgs e)
        {
            //
            camera1.StopRecording();
            //
            menuCameraConnect.IsEnabled = true;
            menuCameraFreeze.IsEnabled = true;
            menuRecordStop.IsEnabled = false;
            statusRecordingText.Text = "Not Recording";
            statusRecordingDot.Foreground = System.Windows.Media.Brushes.Black;
        }


        private void btnCommunicationTest_Click(object sender, RoutedEventArgs e)
        {
            //protocol.SendMessage(ProtocolClass.MESSAGE_TYPES.COMMUNICATION_TEST);
        }

        private void btnCameraMinimise_Click(object sender, MouseButtonEventArgs e)
        {
            switch (WindowStatus)
            {
                case WindowStatusType.MAXIMISED:
                    // set size of window when it was minimised
                    WindowSize = mainGrid.ColumnDefinitions[3].ActualWidth;
                    //minimise window (make width = 0)
                    mainGrid.ColumnDefinitions[3].Width = new GridLength((double)0);
                    //set variable/flag
                    WindowStatus = WindowStatusType.MINIMISED;
                    //disable the grid splitter so window cannont be changed size until it is expanded         
                    cameraGridSplitter.IsEnabled = false;
                    //update arrow direction
                    displayArrowTop.Content = "  < ";
                    displayArrowBottom.Content = "  <  ";
                    break;
                case WindowStatusType.MINIMISED:
                    //set window to the size it had been before it was minimised
                    mainGrid.ColumnDefinitions[3].Width = new GridLength(WindowSize);
                    //set variable/flag
                    WindowStatus = WindowStatusType.MAXIMISED;
                    //re-enable the grid splitter so its size can be changed                
                    cameraGridSplitter.IsEnabled = true;
                    //update arrow direction
                    displayArrowTop.Content = "   >";
                    displayArrowBottom.Content = "   >";
                    break;
                default:
                    break;
            }
        }

        private void menuPlaceHolder_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Sorry Placeholder");
        }

        private void btnBatteryVoltage_Click(object sender, RoutedEventArgs e)
        {
            //protocol.SendMessage(ProtocolClass.MESSAGE_TYPES.BATTERY_VOLTAGE);
        }


		#endregion

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            camera1.CloseCapture();
        }          
	}
}
