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
    public partial class MainWindow : Window
    {
        //Camera Capture Variables
        private VideoCapture _capture = null;
        private bool _captureInProgress = false;
        int CameraDeivce = 0;
        Video_Device[] WebCams;
        private Mat _frame;

        int CameraBoxSelectedIndex = 0;


        public MainWindow()
        {
            InitializeComponent();
            CvInvoke.UseOpenCL = false;

            
           



            try
            {
                _capture = new VideoCapture();
                _capture.ImageGrabbed += ProcessFrame;
            }
            catch(NullReferenceException excpt)
            {
                System.Windows.MessageBox.Show(excpt.Message);
            }

            _frame = new Mat();


            cbCameraList.Items.Add("Select A Camera");
            cbCameraList.SelectedIndex = 0;

            DsDevice[] _SystemCameras = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            WebCams = new Video_Device[_SystemCameras.Length];
            if (_SystemCameras.Length > 0)
            {
                for (int i = 0; i < _SystemCameras.Length; i++)
                {
                    WebCams[i] = new Video_Device(i, _SystemCameras[i].Name);
                    cbCameraList.Items.Add(WebCams[i].ToString());
                }
            }
            else
            {
                cbCameraList.Items.Add("No Cameras Found");
            }
            //cbCameraList.SelectedIndex = CameraBoxSelectedIndex;
            //WebCams = new Video_Device[_SystemCameras.Length];


            //PopulateCameras();
            

            //_capture.Start();
        }

        //Find Connected Cameras by using Directshow .net dll library by carles iloret
        private void PopulateCameras()  //object sender, EventArgs arg
        {
            DsDevice[] _SystemCameras = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            //WebCams = new Video_Device[_SystemCameras.Length];
            bool currentindexmatch = false;


            for(int a = 0; a < _SystemCameras.Length; a++)
            {
                for(int b = 0; b < WebCams.Length; b++)
                {
                    if(WebCams[b].Device_Name == _SystemCameras[a].Name)
                    {
                        currentindexmatch = true;
                    }
                }
                if (currentindexmatch == false)
                {
                    WebCams[WebCams.Length + 1] = new Video_Device(a, _SystemCameras[a].Name);
                    //cbCameraList.Items.Add(WebCams[])
                }
            }


            if(WebCams.Length == _SystemCameras.Length)
            {

            }
            else
            {

            }

            //cbCameraList.SelectedIndex.ToString(): 
            //System.Windows.Controls.ComboBox cbCameraList = sender as ComboBox;
           // int index = cbCameraList.Items.IndexOf()
            //cbCameraList.Items.Remove()
            //cbCameraList.Items.Add("Select A Camera");
            
            //cbCameraList.SelectedIndex = 0;

            if (_SystemCameras.Length > 0)
            {
                for (int i = 0; i < _SystemCameras.Length; i++)
                    {
                        WebCams[i] = new Video_Device(i, _SystemCameras[i].Name);
                        cbCameraList.Items.Add(WebCams[i].ToString());
                    }
            }
            else
            {
               cbCameraList.Items.Add("No Cameras Found");
            }
            cbCameraList.SelectedIndex = CameraBoxSelectedIndex;
        }



    private void ProcessFrame(object sender, EventArgs arg)
        {
            if(_capture != null && _capture.Ptr != IntPtr.Zero)
            {
                _capture.Retrieve(_frame, 0);

                //captureImageBox.Image = _frame;
            }  
        }

        private void cbCameraList_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //PopulateCameras();
        }

        private void cbCameraList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CameraBoxSelectedIndex = cbCameraList.SelectedIndex;
        }
    }
}
