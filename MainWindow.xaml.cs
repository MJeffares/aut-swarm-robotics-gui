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

    public Video_Device(int ID, string Name, Guid Identity = new Guid())
    {
        Device_ID = ID;
        Device_Name = Name;
        Identifier = Identity;
    }

    ///<summary>
    ///Represent the Device as a string
    /// </summary>
    /// <returns>The string representation of this Device</returns>
    public override string ToString()
    {
        return String.Format("[{0}]{1}", Device_ID, Device_Name);
    }
}

static class CaptureStatuses
{
    public const int PLAYING = 0;
    public const int PAUSED = 1;
    public const int STOPPED = 2;
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
        public int CameraDevice = 0;
        Video_Device[] WebCams;
        public Mat _frame;
        public string currentlyConnectedCamera = null;
        public int FPS_Count = 0;
        Timer FPS_Timer = new Timer();

        //public bool _captureInProgress = false;
        public int CaptureStatus = CaptureStatuses.STOPPED;
        
        

        public MainWindow()
        {
            InitializeComponent();
            CvInvoke.UseOpenCL = false;
            PopulateCameras();
           
            DispatcherTimer FPS_Timer = new DispatcherTimer();
            FPS_Timer.Tick += FPS_Timer_Tick;
            FPS_Timer.Interval = new TimeSpan(0, 0, 1);
            //FPS_Timer.Start();  //place where connect
        }



        private void FPS_Timer_Tick(object sender, EventArgs arg)
        {
            cameraStatusFPS.Text = "FPS: " + FPS_Count.ToString();
            FPS_Count = 0;
        }
        

        //Find Connected Cameras by using Directshow.net dll library by carles iloret
        //As the project is build for x64, only cameras with x64 drivers will be found/displayed
        private void PopulateCameras()
        {

            if (CaptureStatus == CaptureStatuses.STOPPED)
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
                _capture = new VideoCapture(CameraDevice);
                _capture.ImageGrabbed += ProcessFrame;
                _frame = new Mat();

                menuCameraConnect.Header = "Stop Capture";
                menuCameraFreeze.IsEnabled = true;

                _capture.Start();
                FPS_Timer.Start();
                CaptureStatus = CaptureStatuses.PLAYING;
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
                FPS_Timer.Stop();
                _capture.Dispose();
                host1.Visibility = Visibility.Hidden;
                //_captureInProgress = false;
                CaptureStatus = CaptureStatuses.STOPPED;
                menuCameraConnect.Header = "Start Capture";
                menuCameraFreeze.Header = "Freeze";
                menuCameraFreeze.IsChecked = false;
                menuCameraFreeze.IsEnabled = false;
            }
            catch(Exception excpt)
            {
                MessageBox.Show(excpt.Message);
            }
        }

        private void PauseCapture()
        {
            try
            {
                _capture.Pause();
                FPS_Timer.Stop();
                //_captureInProgress = false;
                CaptureStatus = CaptureStatuses.PAUSED;
                menuCameraFreeze.Header = "Un-Freeze";
                menuCameraFreeze.IsChecked = true;
            }
            catch(Exception excpt)
            {
                MessageBox.Show(excpt.Message);
            }
        }

        private void ResumeCapture()
        {
            try
            {
                _capture.Start();
                FPS_Timer.Start();
                //_captureInProgress = true;
                CaptureStatus = CaptureStatuses.PLAYING;
                menuCameraFreeze.Header = "Freeze";
                menuCameraFreeze.IsChecked = false;
            }
            catch (Exception excpt)
            {
                MessageBox.Show(excpt.Message);
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

            if (CaptureStatus == CaptureStatuses.STOPPED && currentlyConnectedCamera != menusenderstring) //also check if the same menu option is clicked twice
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
            if(CaptureStatus == CaptureStatuses.PLAYING || CaptureStatus == CaptureStatuses.PAUSED)
            {
                StopCapture();

                var allitems = menuCameraList.Items.Cast<MenuItem>().ToArray();

                foreach (var item in allitems)
                {
                    item.IsEnabled = true;
                }
            }
            else if(CaptureStatus == CaptureStatuses.STOPPED)
            {
                StartCapture();

                var allitems = menuCameraList.Items.Cast<MenuItem>().ToArray();

                foreach (var item in allitems)
                {
                    item.IsEnabled = false;
                }
            }

            /*
            if (!_captureInProgress)
            {
                StartCapture();
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
            }   */
        }

        private void menCameraOptions_Click(object sender, RoutedEventArgs e)
        {
            //need try/catch or checks 
            try
            {
                _capture.SetCaptureProperty(CapProp.Settings, 1);
            }
            catch(Exception excpt)
            {
                MessageBox.Show(excpt.Message);
            }
        }

        private void menuCameraFreeze_Click(object sender, RoutedEventArgs e)
        {
            if(CaptureStatus == CaptureStatuses.PLAYING)
            {
                PauseCapture();
            }
            else if (CaptureStatus == CaptureStatuses.PAUSED)
            {
                ResumeCapture();
            }
        }
    }
}