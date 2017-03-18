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
* 
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
using System.Windows.Forms;
using System.Windows.Threading;

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

    public static class ExtensionMethods
    {
        private static Action EmptyDelegate = delegate () { };

        public static void Refresh(this UIElement uiElement)
        {
            uiElement.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
        }
    }



    public partial class MainWindow : Window
    {
        //Camera Capture Variables
        public VideoCapture _capture = null;
        //private bool _captureInProgress = false;
        public int CameraDevice = 0;
        Video_Device[] WebCams;
        public Mat _frame;

        int CameraBoxSelectedIndex = 0;
        
        public MainWindow()
        {
            InitializeComponent();
            CvInvoke.UseOpenCL = false;

            PopulateCameras();
        }



        //Find Connected Cameras by using Directshow .net dll library by carles iloret
        private void PopulateCameras()
        {
            DsDevice[] _SystemCameras = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            WebCams = new Video_Device[_SystemCameras.Length];
            cbCameraList.Items.Clear();
            //menuCameraList.Items.Clear();
            
            for (int i = 0; i < _SystemCameras.Length; i++)
            {
                WebCams[i] = new Video_Device(i, _SystemCameras[i].Name);
                cbCameraList.Items.Add(WebCams[i].ToString());
                //menuCameraList.Items.Add(WebCams[i].ToString());
                butCameraConnect.Visibility = Visibility.Visible;
            }

            if (cbCameraList.Items.Count == 0)
            {
                cbCameraList.Items.Add("No Cameras Found");
                butCameraConnect.Visibility = Visibility.Hidden;
            }   
            cbCameraList.SelectedIndex = 0;
            
        }



        private void SetupCapture()
        {
            CameraDevice = cbCameraList.SelectedIndex;
            if (_capture != null) _capture.Dispose();
            try
            {
                //Set up capture device
                host1.Visibility = Visibility.Visible;
                _capture = new VideoCapture(CameraDevice);
                _capture.ImageGrabbed += ProcessFrame;
                //System.Windows.Forms.Application.Idle += ProcessFrame;
                _frame = new Mat();
                _capture.Start();

            }
            catch (NullReferenceException excpt)
            {
                System.Windows.MessageBox.Show(excpt.Message);
            }
        }

        


        private void ProcessFrame(object sender, EventArgs arg)
        {
            if(_capture != null && _capture.Ptr != IntPtr.Zero)
            {
                _capture.Retrieve(_frame, 0);

                captureImageBox.Image = _frame;
               
            }
        }



        private void cbCameraList_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            PopulateCameras();
        }



        private void cbCameraList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CameraBoxSelectedIndex = cbCameraList.SelectedIndex;
        }



        private void butCameraConnect_Click(object sender, RoutedEventArgs e)
        {
            SetupCapture();
        }

        
    }
}

