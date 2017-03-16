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
        public VideoCapture _capture = null;
        //private bool _captureInProgress = false;
        public int CameraDevice = 0;
        Video_Device[] WebCams;
        //List<Video_Device> ConnectedCamerasList = new List<Video_Device>(10);   //no-one should have more than 10 cameras connected
        public Mat _frame;

        int CameraBoxSelectedIndex = 0;


        public MainWindow()
        {
            InitializeComponent();
            CvInvoke.UseOpenCL = false;
            

            PopulateCameras();
            //SetupCapture();
            //_capture.Start();
            
            /*
            try
            {
                //Set up capture device
                host1.Visibility = Visibility.Visible;
                _capture = new VideoCapture(CameraDevice);
                _capture.ImageGrabbed += ProcessFrame;
                _frame = new Mat();
                _capture.Start();

            }
            catch (NullReferenceException excpt)
            {
                System.Windows.MessageBox.Show(excpt.Message);
            }   */
            
            

        }

        //Find Connected Cameras by using Directshow .net dll library by carles iloret
        private void PopulateCameras()
        {
            DsDevice[] _SystemCameras = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            WebCams = new Video_Device[_SystemCameras.Length];
            cbCameraList.Items.Clear();

            if (_SystemCameras.Length > 0)
            {
                for (int i = 0; i < _SystemCameras.Length; i++)
                {
                    WebCams[i] = new Video_Device(i, _SystemCameras[i].Name);
                    cbCameraList.Items.Add(WebCams[i].ToString());
                }
                butCameraConnect.Visibility = Visibility.Visible;
            }
            else
            {
                cbCameraList.Items.Add("No Cameras Found");
                butCameraConnect.Visibility = Visibility.Hidden;
            }

            cbCameraList.SelectedIndex = 0;
        }


        /*
        //Find Connected Cameras by using Directshow .net dll library by carles iloret
        private void PopulateCameras()  //object sender, EventArgs arg
        {
            //get old list of cameras
            //get connected cameras
            //check all old list of cameras are valid
            //if not remove them
            //free spot up
            //add connected cameras not in list

            List<DsDevice> _SystemCameras = new List<DsDevice>(DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice));
            bool CameraGood = false;

            for (int a = 0; a < ConnectedCamerasList.Count; a++)
            {                
                for(int b = 0; b <_SystemCameras.Count; b++)
                {
                    if(ConnectedCamerasList[a].ToString() ==_SystemCameras[b].ToString())
                    {
                        CameraGood = true;
                        b = _SystemCameras.Count;
                    }
                }
                if(!CameraGood)
                {
                    cbCameraList.Items.RemoveAt(cbCameraList.Items.IndexOf(ConnectedCamerasList[a]));
                    ConnectedCamerasList.RemoveAt(a);
                }
                CameraGood = false;
            }

            for (int a = 0; a < _SystemCameras.Count; a++)
            {
                for (int b = 0; b < ConnectedCamerasList.Count; b++)
                {
                    if (ConnectedCamerasList[a].ToString() == _SystemCameras[b].ToString())
                    {
                        CameraGood = true;
                        b = _SystemCameras.Count;
                    }
                }
                if (!CameraGood)
                {
                    ConnectedCamerasList.Add(_SystemCameras[a]);


                    cbCameraList.Items.RemoveAt(cbCameraList.Items.IndexOf(ConnectedCamerasList[a]));
                    ConnectedCamerasList.RemoveAt(a);
                }
                CameraGood = false;
            }







            
            int a = 0;
            int b = 0;
            bool CurrentCameraGood = false;
            bool[] CameraAdded;

            CameraAdded = new bool[_SystemCameras.Length];

            for(a = 0; a < WebCams.Length; a++)
            {
                for(b = 0; b < _SystemCameras.Length; b++)
                {
                    if(WebCams[a].Device_Name == _SystemCameras[b].Name)
                    {
                        CurrentCameraGood = true;
                    }
                }
                if(CurrentCameraGood)
                {
                    CameraAdded[b] = true;
                }
                else
                {
                    cbCameraList.Items.Remove(WebCams[a].Device_Name);
                    cbCameraList.Items.Refresh();
                    WebCams[a].Device_ID = 0;
                    WebCams[a].Device_Name = null;
                    WebCams[a].Identifier = Guid.Empty;
                }
                b = 0;
            }

            a = 0;

            for(a = 0; a < _SystemCameras.Length; a++)
            {
                if(CameraAdded[a] == false)
                {
                    for (b = 0; b < WebCams.Length; b++)
                    {
                        if (WebCams[b].Device_ID == 0)
                        {
                            WebCams[b] = new Video_Device(i, _SystemCameras[b].Name);
                            cbCameraList.Items.Add(WebCams[i].ToString());
                            b = WebCams.Length;
                        }
                        else if(b+1 == WebCams.Length)
                        {
                            //not enough space need to add more
                        }
                    }
                    b = 0;
                }
            }
            


            
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
        
        private void SetupCapture(int Camera_Identifier)
        {
            // update the selected device
            CameraDevice = cbCameraList.SelectedIndex;

            //Dispose of Capture if it was created before
            //if (_capture != null) _capture.Dispose();
            try
            {
                //Set up capture device
                host1.Visibility = Visibility.Visible;
                _capture = new VideoCapture(CameraDevice);
                _capture.ImageGrabbed += ProcessFrame;
                _frame = new Mat();
                _capture.Start();
                
            }
            catch (NullReferenceException excpt)
            {
                System.Windows.MessageBox.Show(excpt.Message);
            }
        }   */

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


        /*
        private void butCameraConnect_Click(object sender, MouseButtonEventArgs e)
        {
            //SetupCapture(cbCameraList.SelectedIndex);
            SetupCapture();
            //_capture.Start();
        }   */


    }
}

