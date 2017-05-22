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
// BRAE: Use this to get Brae to do shit
// TODO: This is for general shit
// UNDONE: This is life

// Namespaces
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
        public Camera camera;
        public SerialUARTCommunication serial;
        public XbeeHandler xbee;
        public ProtocolClass protocol;
        public CameraPopOutWindow PopoutWindow;
        public OverlayWindow Overlay;
        // one second timer to calculate and update the fps count
        private DispatcherTimer Timer1;
        // timer to draw new frame
        private Timer Timer2;
        // 
        private DateTime startTime;
        //
        private OpenFileDialog openvideodialog = new OpenFileDialog();
        private SaveFileDialog savevideodialog = new SaveFileDialog();
        // 
        private Video_Device[] webcams;
        #endregion

        // Main
        public MainWindow()
        {
            InitializeComponent();
            //
            camera = new Camera();

            // MANSEL: remove class dependency on main if possible
            xbee = new XbeeHandler(this);
            protocol = new ProtocolClass(this);
            // MANSEL: why is it so long? maybe make a struct. Also look at SerialPort class
            serial = new SerialUARTCommunication(this, menuCommunicationPortList, menuCommunicationBaudList, menuCommunicationParityList, menuCommunicationDataList, menuCommunicationStopBitsList, menuCommunicationHandshakeList, menuCommunicationConnect);
            //serial._serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

            CvInvoke.UseOpenCL = false;
            PopulateFilters();
            PopulateCameras();
            //
            openvideodialog.Filter = "Video Files|*.avi;*.mp4;*.mpg";
            savevideodialog.Filter = "Video Files|*.avi;*.mp4;*.mpg";
            savevideodialog.Title = "Record: Save As";
            //
            startTime = new DateTime();
            startTime = DateTime.Now;
            //
            Timer1 = new DispatcherTimer();
            Timer1.Tick += Timer1_Tick;
            Timer1.Interval = new TimeSpan(0, 0, 1);
            Timer1.Start();
            //
            Timer2 = new Timer(50);
            Timer2.Elapsed += Timer2_Tick;
            Timer2.Start();

            ///example of select folder dialog
            ///var selectFolderDialog = new FolderSelectDialog { Title = "Select a folder to save data to" };
            ///if (selectFolderDialog.Show())
            ///{
            ///	rtbSerial.AppendText(selectFolderDialog.FileName.ToString());
            ///}

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
            if (camera.Status != Camera.StatusType.PLAYING && camera.Status != Camera.StatusType.RECORDING)
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
        public void ToggleCameraWindow()
        {
            switch (camera.WindowStatus)
            {
                case Camera.WindowStatusType.POPPED_OUT:
                    PopoutWindow.Close();
                    //set window to the size it had been before it was minimised
                    mainGrid.ColumnDefinitions[3].Width = new GridLength(camera.WindowSize);
                    //set variable/flag
                    camera.WindowStatus = Camera.WindowStatusType.MAXIMISED;
                    //re-enable the grid splitter so its size can be changed
                    cameraGridSplitter.IsEnabled = true;
                    // update arrow direction
                    cameraArrowTop.Content = "   >";
                    cameraArrowBottom.Content = "   >";
                    // TEMP: toggles the name of the button
                    menuDisplayPopOut.Header = "Pop Out Window";

                    break;
                default:
                    // set size of window to original
                    camera.WindowSize = mainGrid.ColumnDefinitions[3].ActualWidth;
                    // minimise window (make width = 0)     
                    mainGrid.ColumnDefinitions[3].Width = new GridLength((double)0);
                    // change camera window status to popped out
                    camera.WindowStatus = Camera.WindowStatusType.POPPED_OUT;
                    // disable the grid splitter so window cannont be changed size until it is expanded               
                    cameraGridSplitter.IsEnabled = false;
                    // update arrow direction
                    cameraArrowTop.Content = "  < ";
                    cameraArrowBottom.Content = "  <  ";
                    // TEMP: toggles the name of the button
                    menuDisplayPopOut.Header = "Pop In Window";
                    // create and show the window
                    PopoutWindow = new CameraPopOutWindow(this);
                    PopoutWindow.Show();
                    break;
            }
        }
        #endregion

        // Timer events
        #region
        // TODO: Rename and comment timers
        private void Timer1_Tick(object sender, EventArgs arg)
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

            //updates FPS counter
            statusFPS.Text = "FPS: " + camera.FPS;

            switch (camera.TimeDisplayMode)
            {
                case Camera.TimeDisplayModeType.CURRENT:
                    statusTime.Text = DateTime.Now.ToString("t");
                    ///statusTime.Text = DateTime.Now.ToString();
                    ///statusTime.Text = String.Format("{0:d dd HH:mm:ss}" ,DateTime.Now);
                    ///statusTime.Text = String.Format("{0:T}", DateTime.Now.ToString());
                    break;


                case Camera.TimeDisplayModeType.FROM_START:
                    if (camera.Status == Camera.StatusType.RECORDING)
                    {
                        statusTime.Text = (DateTime.Now - startTime).ToString(@"dd\.hh\:mm\:ss");
                        ///TimeSpan displayTime = (DateTime.Now - startTime);
                        ///statusTime.Text = displayTime.ToString(@"dd\.hh\:mm\:ss");
                        ///displayTime = displayTime.Add(-((TimeSpan)displayTime.Ticks % TimeSpan.TicksPerSecond));
                        ///statusTime.Text = (DateTime.Now - startTime).ToString("t");
                    }
                    break;
            }
        }
        private void Timer2_Tick(object sender, ElapsedEventArgs e)
        {
            if (Overlay != null)
            {
                // TODO: Find a better way to bind properties between windows
                // HACK: update them every frame
                camera.imgProc.UpperC = Overlay.UpperC;
                camera.imgProc.LowerC = Overlay.LowerC;

                camera.imgProc.LowerH = Overlay.LowerH;
                camera.imgProc.LowerS = Overlay.LowerS;
                camera.imgProc.LowerV = Overlay.LowerV;
                camera.imgProc.UpperH = Overlay.UpperH;
                camera.imgProc.UpperS = Overlay.UpperS;
                camera.imgProc.UpperV = Overlay.UpperV;

                Overlay.captureImageBox.Image = camera.imgProc.OverlayImage;
            }
            switch (camera.WindowStatus)
            {
                case Camera.WindowStatusType.POPPED_OUT:
                    PopoutWindow.captureImageBox.Image = camera.Frame;                    
                    break;
                case Camera.WindowStatusType.MAXIMISED:
                    captureImageBox.Image = camera.Frame;
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
            String menusenderstring = menusender.ToString();

            if (menusenderstring != ImageProcessing.ToString(camera.imgProc.Filter))
            {
                MenuItem[] allitems = menuFilterList.Items.OfType<MenuItem>().ToArray();

                foreach (var item in allitems)
                {
                    item.IsChecked = false;
                }
                menusender.IsChecked = true;
                camera.imgProc.Filter = (ImageProcessing.FilterType)menuFilterList.Items.IndexOf(menusender);
                //
                statusDisplayFilter.Text = ImageProcessing.ToString(camera.imgProc.Filter);
            }
            else if (camera.imgProc.Filter != ImageProcessing.FilterType.NONE)
            {
                MenuItem[] allitems = menuFilterList.Items.OfType<MenuItem>().ToArray();

                foreach (var item in allitems)
                {
                    item.IsChecked = false;
                    item.IsEnabled = true;
                }
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
                menuCameraConnect.IsEnabled = true;
                statusCameraName.Text = menusender.Header.ToString();

                camera.Name = menusender.ToString();
                camera.Index = menuCameraList.Items.IndexOf(menusender);
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
        private void MenuItem_MouseEnter(object sender, MouseEventArgs e)
        {
            // TODO: What is this and why does it populate the cameras?
            PopulateCameras();
        }
        private void menuCameraConnect_Click(object sender, RoutedEventArgs e)
        {
            if (camera.Status == Camera.StatusType.PLAYING || camera.Status == Camera.StatusType.PAUSED)
            {
                // Stop capturing
                camera.StopCapture();
                //
                captureImageBox.Visible = false;
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
                Overlay.Close();
            }
            else if (camera.Status == Camera.StatusType.STOPPED)
            {
                // Start capturing
                camera.StartCapture();
                //
                captureImageBox.Visible = true;
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
                //
                Overlay = new OverlayWindow(this);
                Overlay.Show();
            }
        }
        private void menuCameraOptions_Click(object sender, RoutedEventArgs e)
        {
            camera.OpenSettings();
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
            camera.FlipVertical();
        }
        private void menuFilterFlipHorizontal_Click(object sender, RoutedEventArgs e)
        {
            camera.FlipHorizontal();
        }
        // Replay menu
        private void menuReplayOpen_Click(object sender, RoutedEventArgs e)
        {
            if (camera.Status == Camera.StatusType.STOPPED)
            {
                if (openvideodialog.ShowDialog() == true)
                {
                    try
                    {
                        statusRecordingText.Text = "Replaying Video: " + openvideodialog.FileName;
                        camera.StartReplaying(openvideodialog.FileName);
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
                    //
                    camera.StartRecording(savevideodialog.FileName);
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
            camera.StopRecording();
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
            protocol.SendMessage(ProtocolClass.MESSAGE_TYPES.COMMUNICATION_TEST);
        }
        // Other
        private void menu_Hover(object sender, RoutedEventArgs e)
        {
            // TODO: Work out why this was here
        }
        private void btnCameraMinimise_Click(object sender, MouseButtonEventArgs e)
        {
            // TODO: Will probably move window status out of camera class
            switch (camera.WindowStatus)
            {
                case Camera.WindowStatusType.MAXIMISED:
                    // set size of window when it was minimised
                    camera.WindowSize = mainGrid.ColumnDefinitions[3].ActualWidth;
                    //minimise window (make width = 0)
                    mainGrid.ColumnDefinitions[3].Width = new GridLength((double)0);
                    //set variable/flag
                    camera.WindowStatus = Camera.WindowStatusType.MINIMISED;
                    //disable the grid splitter so window cannont be changed size until it is expanded         
                    cameraGridSplitter.IsEnabled = false;
                    //update arrow direction
                    cameraArrowTop.Content = "  < ";
                    cameraArrowBottom.Content = "  <  ";
                    break;
                case Camera.WindowStatusType.MINIMISED:
                    //set window to the size it had been before it was minimised
                    mainGrid.ColumnDefinitions[3].Width = new GridLength(camera.WindowSize);
                    //set variable/flag
                    camera.WindowStatus = Camera.WindowStatusType.MAXIMISED;
                    //re-enable the grid splitter so its size can be changed                
                    cameraGridSplitter.IsEnabled = true;
                    //update arrow direction
                    cameraArrowTop.Content = "   >";
                    cameraArrowBottom.Content = "   >";
                    break;
                default:
                    break;
            }
        }
        private void menuPlaceHolder_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Sorry Placeholder");
        }
        #endregion

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // BRAE: Implement child control
            if (PopoutWindow != null)
            {
                PopoutWindow.Close();
            }
            if (Overlay != null)
            {
                Overlay.Close();
            }
        }
    }
}
