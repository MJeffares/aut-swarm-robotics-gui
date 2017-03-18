/*********************************************************************************************************
* File: MainWindow.xaml.cs
*
* Developed By: Mansel Jeffares
* Date: 18 March 2017
*
* Description :
*     Graphics User Interface for Swarm Robotics Project
*     Built for x64, .NET 4.5.2
*
* Limitations :
*   Build for x64, will only detect Cameras with x64 drivers
*   Camera List Populating is archaic and is limitied to a maximum of 10 Cameras
* 
********************************************************************************************************/



#region Namespaces
//Possibly Excessive 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

//Emgu
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.Util;

//DirectShow
using DirectShowLib;

//required namespaces
using System.Drawing;
//using System.Windows.Forms;
using System.Windows.Threading;
using System.Timers;

#endregion



//Video_device Struct Define
struct Video_Device
{
    public string Device_Name;
    public int Device_ID;
    public Guid Identifier;
    //public int Menu_Index;

    public Video_Device(int ID, string Name, Guid Identity = new Guid())
    {
        Device_ID = ID;
        Device_Name = Name;
        Identifier = Identity;
        //Menu_Index = -1;
    }

    ///<summary>
    ///Represent the Device as a string
    /// </summary>
    /// <returns>The string representation of this colour</returns>
    public override string ToString()
    {
        return String.Format("[{0}]{1}", Device_ID, Device_Name);
    }
}



namespace SwarmRoboticsGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>



    public partial class MainWindow : Window
    {
        //Camera Capture Variables
        public VideoCapture _capture = null;
        public bool _captureInProgress = false;
        public int CameraDevice = 0;
        Video_Device[] WebCams;
        public Mat _frame;
        public string currentlyConnectedCamera = null;
        public int FPS_Count = 0;
        Timer FPS_Timer = new Timer();

        Timer test_timer = new Timer();

        public MainWindow()
        {
            InitializeComponent();
            CvInvoke.UseOpenCL = false;
            PopulateCameras();
           
            //FPS_Timer.Elapsed += new ElapsedEventHandler(FPS_Timer_Tick);
            //FPS_Timer.Interval = 1000;
            DispatcherTimer FPS_Timer = new DispatcherTimer();
            FPS_Timer.Tick += FPS_Timer_Tick;
            FPS_Timer.Interval = new TimeSpan(0, 0, 1);
            FPS_Timer.Start();  //place where connect

            /*
            DispatcherTimer test_Timer = new DispatcherTimer();
            test_Timer.Tick += test_Timer_Tick;
            test_Timer.Interval = new TimeSpan(0, 0, 0, 5,0);
            test_Timer.Start();  //place where connect  */
            //System.Windows.Forms.Application.Idle += UpdateUI;
        }

        int number = 0;
        private void FPS_Timer_Tick(object sender, EventArgs arg)
        {
            cameraStatusFPS.Text = "FPS: " + FPS_Count.ToString();
            FPS_Count = 0;

            if (_captureInProgress)
            {
                //_capture.SetCaptureProperty(CapProp.Autograb, number);
                //number++;
            }
        }
        /*
        private void test_Timer_Tick(object sender, EventArgs arg)
        {
            DispatcherTimer timesender = (DispatcherTimer)sender;
            timesender.Interval = new TimeSpan(0, 0, 0, 0, 34);
            _capture.QueryFrame();
        }   */

        //Find Connected Cameras by using Directshow.net dll library by carles iloret
        //As the project is build for x64, only cameras with x64 drivers will be found/displayed
        private void PopulateCameras()
        {

            if (!_captureInProgress)
            {
                DsDevice[] _SystemCameras = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);      //gets currently connected devices
                WebCams = new Video_Device[_SystemCameras.Length];                                          //creates a new array of devices
                menuCameraList.Items.Clear();                                                               //clears cameras from menu

                //loops through devices and adds them to menu
                for (int i = 0; i < _SystemCameras.Length; i++)
                {
                    WebCams[i] = new Video_Device(i, _SystemCameras[i].Name);;
                    MenuItem item = new MenuItem { Header = WebCams[i].ToString() };
                    item.Click += new RoutedEventHandler(menuCameraListItem_Click);
                    item.IsCheckable = true;
                    menuCameraList.Items.Add(item);                

                    //restores currently connect camera selection
                    if (item.ToString() == currentlyConnectedCamera)
                    {
                        item.IsEnabled = true;
                        item.IsChecked = true;
                        CameraDevice = menuCameraList.Items.IndexOf(item);
                        menuCameraConnect.IsEnabled = true;
                    }

                }
                //displays helpful message if no cameras found
                if(menuCameraList.Items.Count == 0)
                {
                    MenuItem nonefound = new MenuItem { Header = "No Cameras Found" };
                    menuCameraList.Items.Add(nonefound);
                    nonefound.IsEnabled = false;
                }
            }
        }
        


        private void SetupCapture()
        {
            if (_capture != null)
            {
                _capture.Dispose();
            }

            try
            {
                //Set up capture device
                host1.Visibility = Visibility.Visible;
                _capture = new VideoCapture(CameraDevice);
                _capture.ImageGrabbed += ProcessFrame;

                //_capture.
                //.SetCaptureProperty(CapProp.Autograb)
                //_capture.SetCaptureProperty(CapProp.Brightness, 11);
                //_capture.SetCaptureProperty(CapProp.Settings, 1);
                
                _frame = new Mat();
                _capture.Start();
                menuCameraConnect.Header = "Stop Capture";

            }
            catch (NullReferenceException excpt)
            {
                System.Windows.MessageBox.Show(excpt.Message);
            }
        }



        private void ProcessFrame(object sender, EventArgs arg)
        {
            if (_capture != null && _capture.Ptr != IntPtr.Zero)
            {
                _capture.Retrieve(_frame, 0);

                captureImageBox.Image = _frame;
                FPS_Count++;
               // _capture.SetCaptureProperty(CapProp.Brightness, FPS_Count);
            }
        }



        public void menuCameraListItem_Click(Object sender, RoutedEventArgs e)
        {
            MenuItem menusender = (MenuItem)sender;
            String menusenderstring = menusender.ToString();

            if (!_captureInProgress && currentlyConnectedCamera != menusenderstring) //also check if the same menu option is clicked twice
            {
                var allitems = menuCameraList.Items.Cast<System.Windows.Controls.MenuItem>().ToArray();

                foreach (var item in allitems)
                {
                    item.IsChecked = false;
                    item.IsEnabled = false;
                }

                menusender.IsEnabled = true;
                menusender.IsChecked = true;
                currentlyConnectedCamera = menusender.ToString();
                cameraStatusName.Text = menusender.Header.ToString();
                CameraDevice = menuCameraList.Items.IndexOf(menusender);
                menuCameraConnect.IsEnabled = true;

            }
            else if(currentlyConnectedCamera == menusenderstring)
            {
                var allitems = menuCameraList.Items.Cast<System.Windows.Controls.MenuItem>().ToArray();

                foreach (var item in allitems)
                {
                    item.IsChecked = false;
                    item.IsEnabled = true;
                }
                currentlyConnectedCamera = "No Camera Selected";
                cameraStatusName.Text = null;
                CameraDevice = -1;
                menuCameraConnect.IsEnabled = false;
            }
        }

        private void MenuItem_MouseEnter(object sender, MouseEventArgs e)
        {
            PopulateCameras();
        }

        private void menuCameraConnect_Click(object sender, RoutedEventArgs e)
        {
            if (!_captureInProgress)
            {
                SetupCapture();
                _captureInProgress = true;
                menuCameraConnect.Header = "Stop Capture";
                var allitems = menuCameraList.Items.Cast<System.Windows.Controls.MenuItem>().ToArray();

                foreach (var item in allitems)
                {
                    item.IsEnabled = false;
                }
            }
            else
            {
                _captureInProgress = false;
                _capture.Pause();
                _captureInProgress = false;
                menuCameraConnect.Header = "Start Capture";
                var allitems = menuCameraList.Items.Cast<System.Windows.Controls.MenuItem>().ToArray();

                foreach (var item in allitems)
                {
                    item.IsEnabled = true;
                }
                //menuCameraConnect.IsChecked = false;
            }
        }

        private void menCameraOptions_Click(object sender, RoutedEventArgs e)
        {
            //need try/catch or checks 
            _capture.SetCaptureProperty(CapProp.Settings, 1);
        }
    }
}