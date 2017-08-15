///	File: MainWindow.xaml.cs
///
/// Developed By: Mansel Jeffares
/// First Build: 7 March 2017
/// Current Build:  27 April 2017
///
/// Description :
///     Graphics User Interface for Swarm Robotics Project
///     Built for x64, .NET 4.5.2
///
/// Limitations :
///     Build for x64, will only detect Cameras with x64 drivers
///
/// Naming Conventions:
///     CamelCase
///     Variables start lower case, if another object goes by the same name, then also with an underscore
///     Methods start upper case
///     Constants, all upper case, unscores for seperation
///

// MANSEL: This is an example of a Mansel task
// BRAE: Use this to get Brae to do something for once
// TODO: This is for general things that need doing
// UNDONE: This is life

// Namespaces
using DirectShowLib;
using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.Util;
using folderHack;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

// Structures
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
    public override string ToString()
    {
        return String.Format("[{0}]{1}", deviceId, deviceName);
    }
}

namespace SwarmRoboticsGUI
{
    public partial class MainWindow : Window
    {
        // Declarations
        #region
        // TODO: comment declarations
        public Camera camera1;
        public SerialUARTCommunication serial;
        public XbeeAPI xbee;
        public ProtocolClass protocol;
		public CommunicationManager commManger;
        public CameraPopOutWindow popoutWindow;
        public OverlayWindow overlayWindow;
        // one second timer to calculate and update the fps count
        private DispatcherTimer InterfaceTimer;
        // 
        private DateTime startTime;
        //
        private OpenFileDialog openvideodialog = new OpenFileDialog();
        private SaveFileDialog savevideodialog = new SaveFileDialog();
        // 
        private Video_Device[] webcams;

        public enum WindowStatusType { MAXIMISED, MINIMISED, POPPED_OUT };
        public enum TimeDisplayModeType { CURRENT, FROM_START, START };
        public WindowStatusType WindowStatus { get; set; }
        public TimeDisplayModeType TimeDisplayMode { get; set; }
        public double WindowSize { get; set; }
        #endregion    
        
        // Main
        public MainWindow()
        {
            InitializeComponent();
            //
            CvInvoke.UseOpenCL = true;
            //
            //camera1 = new Camera(640, 480);
            //camera1 = new Camera(1280, 720);
            camera1 = new Camera(1920, 1080);

            xbee = new XbeeAPI(this);
            protocol = new ProtocolClass(this);
			

            // MANSEL: Maybe make a struct. Also look at SerialPort class
            serial = new SerialUARTCommunication(this, menuCommunicationPortList, menuCommunicationBaudList, menuCommunicationParityList, menuCommunicationDataList, menuCommunicationStopBitsList, menuCommunicationHandshakeList, menuCommunicationConnect);
			//

			commManger = new CommunicationManager(this,serial, xbee, protocol);

			overlayWindow = new OverlayWindow(this);                      
            //
            PopulateFilters();
            PopulateOverlays();
            PopulateCameras();
            PopulateSources();
            //
            openvideodialog.Filter = "Video Files|*.avi;*.mp4;*.mpg";
            savevideodialog.Filter = "Video Files|*.avi;*.mp4;*.mpg";
            savevideodialog.Title = "Record: Save As";
            //
            startTime = new DateTime();
            startTime = DateTime.Now;
            //
            InterfaceTimer = new DispatcherTimer();
            InterfaceTimer.Tick += Interface_Tick;
            InterfaceTimer.Interval = new TimeSpan(0, 0, 1);
            InterfaceTimer.Start();
            //
            TimeDisplayMode = TimeDisplayModeType.CURRENT;
            WindowStatus = WindowStatusType.MAXIMISED;
            
            // TEMP: display overlay on starup for debugging
            overlayWindow.Show();

            camera1.FrameUpdate += new Camera.FrameHandler(DrawCameraFrame);
            
			//serial._serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

			setupSystemTest();
		}      

        // Methods
        #region
        /// <summary>
        /// Finds the Connected Cameras by using Directshow.net dll library by carles iloret.
        /// As the project is build for x64, only cameras with x64 drivers will be found/displayed.
        /// </summary>
        private void PopulateCameras()
        {
            // we dont want to update this if we are connected to a camera
            if (camera1.Status != Camera.StatusType.PLAYING && camera1.Status != Camera.StatusType.RECORDING)
            {
                // gets currently connected devices
                DsDevice[] _SystemCameras = DsDevice.GetDevicesOfCat(DirectShowLib.FilterCategory.VideoInputDevice);
                // creates a new array of devices    
                webcams = new Video_Device[_SystemCameras.Length];
                // clears cameras from menu                                      
                menuCameraList.Items.Clear();
                menuCameraConnect.IsEnabled = false;

                // loops through cameras and adds them to menu
                for (int i = 0; i < _SystemCameras.Length; i++)
                {
                    webcams[i] = new Video_Device(i, _SystemCameras[i].Name);
                    MenuItem item = new MenuItem { Header = webcams[i].ToString() };
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
        /// <summary>
        /// Automatically adds the filters defined in the class into our menu for selecting filters.
        /// </summary>
        private void PopulateFilters()
        {
            //loops through our filters and adds them to our menu
            for (int i = 0; i < (int)ImageProcessing.FilterType.NUM_FILTERS; i++)
            {
                MenuItem item = new MenuItem { Header = ImageProcessing.ToString((ImageProcessing.FilterType)i) };
                item.Click += new RoutedEventHandler(menuFilterListItem_Click);
                item.IsCheckable = true;
                //by default select our first filter (no filter)
                if (i == 0)
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
            for (int i = 0; i < (int)Display.OverlayType.NUM_OVERLAYS; i++)
            {
                MenuItem item = new MenuItem { Header = Display.ToString((Display.OverlayType)i) };
                item.Click += new RoutedEventHandler(menuOverlayListItem_Click);
                item.IsCheckable = true;
                //by default select our first filter (no filter)
                if (i == 0)
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
            for (int i = 0; i < (int)Display.SourceType.NUM_SOURCES; i++)
            {
                MenuItem item = new MenuItem { Header = Display.ToString((Display.SourceType)i) };
                item.Click += new RoutedEventHandler(menuSourceListItem_Click);
                item.IsCheckable = true;
                //by default select our first filter (no filter)
                if (i == 0)
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
                    popoutWindow = new CameraPopOutWindow(this);
                    popoutWindow.Show();
                    break;
            }
        }
        #endregion

        // Time events
        #region
        private void Interface_Tick(object sender, EventArgs arg)
        {
            // MANSEL: I hate this block of code. Does it belong here? Does it even work?
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

            switch (TimeDisplayMode)
            {
                case TimeDisplayModeType.CURRENT:
                    statusTime.Text = DateTime.Now.ToString("t");
                    ///statusTime.Text = DateTime.Now.ToString();
                    ///statusTime.Text = String.Format("{0:d dd HH:mm:ss}" ,DateTime.Now);
                    ///statusTime.Text = String.Format("{0:T}", DateTime.Now.ToString());
                    break;
                case TimeDisplayModeType.FROM_START:
                    if (camera1.Status == Camera.StatusType.RECORDING)
                    {
                        statusTime.Text = (DateTime.Now - startTime).ToString(@"dd\.hh\:mm\:ss");
                    }
                    break;
            }

            statusFPS.Text = camera1.FPS.ToString();

            switch (overlayWindow.Display1.Source)
            {
                case Display.SourceType.NONE:
                    break;
                case Display.SourceType.CAMERA:
                    
                    break;
                case Display.SourceType.CUTOUTS:
                    DrawCameraFrame(this, new EventArgs());
                    break;
                default:
                    break;
            }
        }


        private void DrawCameraFrame(object sender, EventArgs e)
        {
            switch (overlayWindow.Display1.Source)
            {
                case Display.SourceType.NONE:
                    // Do nothing
                    break;
                case Display.SourceType.CAMERA:
                    // Sender is a frame
                    UMat Frame = sender as UMat;
                    if (Frame != null)
                    {
                        // Apply the currently selected filter
                        overlayWindow.imgProc.ProcessFilter(Frame);
                        // Draw the frame to the overlay imagebox
                        captureImageBox.Image = overlayWindow.imgProc.Image;
                    }
                    break;
                case Display.SourceType.CUTOUTS:
                    // Apply the currently selected filter
                    overlayWindow.imgProc.ProcessFilter(overlayWindow.imgProc.TestImage);
                    // Draw the frame to the overlay imagebox
                    captureImageBox.Image = overlayWindow.imgProc.Image;
                    break;
                default:
                    break;
            }
        }


        #endregion

        // Input events
        #region       
        // Display menu
        private void menuFilterListItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menusender = (MenuItem)sender;
            string menusenderstring = menusender.ToString();

            if (menusenderstring != ImageProcessing.ToString(overlayWindow.imgProc.Filter))
            {
                MenuItem[] allitems = menuFilterList.Items.OfType<MenuItem>().ToArray();

                foreach (var item in allitems)
                {
                    item.IsChecked = false;
                }
                menusender.IsChecked = true;
                overlayWindow.imgProc.Filter = (ImageProcessing.FilterType)menuFilterList.Items.IndexOf(menusender);
                //
                statusDisplayFilter.Text = ImageProcessing.ToString(overlayWindow.imgProc.Filter);
            }
            else if (overlayWindow.imgProc.Filter != ImageProcessing.FilterType.NONE)
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

            if (menusenderstring != Display.ToString(overlayWindow.Display1.Overlay))
            {
                MenuItem[] allitems = menuOverlayList.Items.OfType<MenuItem>().ToArray();

                foreach (var item in allitems)
                {
                    item.IsChecked = false;
                }
                menusender.IsChecked = true;
                overlayWindow.Display1.Overlay = (Display.OverlayType)menuOverlayList.Items.IndexOf(menusender);
                // TODO: Not sure where to display this right now
                //statusDisplayFilter.Text = ImageDisplay.ToString(overlayWindow.Display.Overlay);
            }
        }

        private void menuSourceListItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menusender = (MenuItem)sender;
            string menusenderstring = menusender.ToString();

            if (menusenderstring != Display.ToString(overlayWindow.Display1.Source))
            {
                MenuItem[] allitems = menuSourceList.Items.OfType<MenuItem>().ToArray();

                foreach (var item in allitems)
                {
                    item.IsChecked = false;
                }
                menusender.IsChecked = true;
                overlayWindow.Display1.Source = (Display.SourceType)menuSourceList.Items.IndexOf(menusender);
                // TODO: Not sure where to display this right now
                //statusDisplayFilter.Text = ImageDisplay.ToString(overlayWindow.Display.Overlay);
            }
        }

        private void menuDisplayPopOut_Click(object sender, RoutedEventArgs e)
        {
            ToggleCameraWindow();
        }
        // Camera menu
        private void menuCameraListItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menusender = (MenuItem)sender;
            string menusenderstring = menusender.ToString();

            if (camera1.Status == Camera.StatusType.STOPPED && camera1.Name != menusenderstring) //also check if the same menu option is clicked twice
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
                menuCameraConnect.IsEnabled = true;
                statusCameraName.Text = menusender.Header.ToString();

                camera1.Name = menusender.ToString();
                camera1.Index = menuCameraList.Items.IndexOf(menusender);
            }
            else if (camera1.Name == menusenderstring)
            {
                //var allitems = menuCameraList.Items.Cast<System.Windows.Controls.MenuItem>().ToArray();
                MenuItem[] allitems = menuCameraList.Items.OfType<MenuItem>().ToArray();

                foreach (var item in allitems)
                {
                    item.IsChecked = false;
                    item.IsEnabled = true;
                }
                camera1.Name = "No Camera Selected";
                statusCameraName.Text = null;
                camera1.Index = -1;
                menuCameraConnect.IsEnabled = false;
            }
        }
        private void MenuItem_MouseEnter(object sender, MouseEventArgs e)
        {
            // TODO: What is this and why does it populate the cameras?
            PopulateCameras();
        }
        private void menuCameraConnect_Click(object sender, RoutedEventArgs e)
        {
            if (camera1.Status == Camera.StatusType.PLAYING || camera1.Status == Camera.StatusType.PAUSED)
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
                //
                overlayWindow.Close();
            }
            else if (camera1.Status == Camera.StatusType.STOPPED)
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
                //set start date time as a variable 
                startTime = DateTime.Now;
            }
        }
        private void menuCameraOptions_Click(object sender, RoutedEventArgs e)
        {
            camera1.OpenSettings();
        }
        private void menuCameraFreeze_Click(object sender, RoutedEventArgs e)
        {
            if (camera1.Status == Camera.StatusType.PLAYING)
            {
                camera1.PauseCapture();
            }
            else if (camera1.Status == Camera.StatusType.PAUSED)
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
            if (camera1.Status == Camera.StatusType.STOPPED)
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
            if (camera1.Status == Camera.StatusType.PLAYING)
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
            statusRecordingDot.Foreground = Brushes.Black;
        }
        // Communications menu

        private void btnCommunicationTest_Click(object sender, RoutedEventArgs e)
        {
            //protocol.SendMessage(ProtocolClass.MESSAGE_TYPES.COMMUNICATION_TEST);
        }
        // Other
        private void menu_Hover(object sender, RoutedEventArgs e)
        {
            // TODO: Work out why this was here
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

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            byte[] data;
            data = new byte[2];
            data[0] = SYSTEM_TEST_MESSAGE.COMMUNICATION;
            data[1] = 0x01;

            xbee.SendTransmitRequest(XbeeAPI.DESTINATION.BROADCAST, data);
        }
	}
}
