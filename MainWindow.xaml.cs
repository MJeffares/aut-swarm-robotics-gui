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

        int shit = 0;

        public MainWindow()
        {
            InitializeComponent();
            CvInvoke.UseOpenCL = false;


            WebCams = new Video_Device[10];
            PopulateCameras();

            /*
            for (int i = 0; i < 10; i++)
            {
                
                if(WebCams[i].ToString == currentlyConnectedCamera)
                {

                }
                

                if (WebCams[i].Device_Name == currentlyConnectedCamera)
                {
                    shit = 2; // this happens
                }
            }   */

        }


        //Find Connected Cameras by using Directshow .net dll library by carles iloret
        private void PopulateCameras()
        {

            if (!_captureInProgress)
            {
                DsDevice[] _SystemCameras = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
                WebCams = new Video_Device[_SystemCameras.Length];
                menuCameraList.Items.Clear();

                //int i = 0;
                for (int i = 0; i < _SystemCameras.Length; i++)
                {
                    WebCams[i] = new Video_Device(i, _SystemCameras[i].Name);
                    //cbCameraList.Items.Add(WebCams[i].ToString());
                    MenuItem item = new MenuItem { Header = WebCams[i].ToString() };
                    item.Click += new RoutedEventHandler(menuCameraListItem_Click);
                    item.IsCheckable = true;
                    menuCameraList.Items.Add(item);                

                    // if (WebCams[x].Device_Name == currentlyConnectedCamera)
                    if (item.ToString() == currentlyConnectedCamera)
                    {
                        //menuCameraListItem_Click(item, RoutedEventArgs.Empty);
                        item.IsEnabled = true;
                        item.IsChecked = true;
                        CameraDevice = menuCameraList.Items.IndexOf(item);
                        menuCameraConnect.IsEnabled = true;
                        //item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));


                        //menuCameraListItem_Click
                        //click on the item
                        //menuCameraConnect.Items.IndexOf(item);
                        //menuCameraListItem_Click(item,RoutedEventArgs e)
                    }
                    //}

                }


                //WebCams[x].Menu_Index = menuCameraList.Items.IndexOf(item);
                //}
                //else
                //{
                //  camgood = false;
                //}



            }
        }

        #region oldpopulatecameras
        //Find Connected Cameras by using Directshow .net dll library by carles iloret

        /*private void PopulateCameras()
{

    if (!_captureInProgress)
    {
        DsDevice[] _SystemCameras = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);

        //var allitems = WebCams.Items.Cast<System.Windows.Controls.MenuItem>().ToArray();

        bool camgood = false;
        int num = 0;

        /*
        for (int i = 0; i < WebCams.Length; i++)
        {
            if(WebCams[i].Device_Name != null)
            {
                num++;
            }
        }   


        for (int i = 0; i < 10; i++)
        {
            for(int a = 0; a < _SystemCameras.Length; a++)
            {
                if(WebCams[i].Device_Name == _SystemCameras[a].Name)
                {
                    camgood = true;
                    a = _SystemCameras.Length;
                }
            }

            if (!camgood)
            {
                //menuCameraList.Items.Remove(WebCams[i].Device_Name);
                // menuCameraList.Items.RemoveAt(menuCameraList.Items.IndexOf(WebCams[i].Device_Name));
                //*********************************************************************************************************if we could remove it 
                //menuCameraList.Items.RemoveAt(WebCams[i].Device_ID);
                if (WebCams[i].Device_Name != null)
                {
                    //menuCameraList.Items.RemoveAt(WebCams[i].Menu_Index);
                }
                //menuCameraList.Items.Remove(WebCams.ToString());

                WebCams[i].Device_ID = -1;
                WebCams[i].Device_Name = null;
                WebCams[i].Identifier = Guid.Empty;
                //WebCams[i].Menu_Index = -1;
             }
            else
            {
                camgood = false;
            }

        }

        List<string> lst = WebCams.OfType<string>().ToList();
        //camgood = false;

        //var allitems = menuCameraList.Items.Cast<System.Windows.Controls.MenuItem>().ToArray();

        /*
        //foreach (var item in allitems)
        for(int i = 0; i < menuCameraList.Items.Count; i++)
        {
            //item.IsChecked = false;
            //item.IsEnabled = false;
            String item = (String)menuCameraList.Items.GetItemAt(i);

            for (int a = 0; a < 10; a++)
            {

                //if(WebCams[i].ToString == (string)item.Header)
                if (String.Format("[{0}]{1}", WebCams[i].Device_ID, WebCams[i].Device_Name) == (string)item.Header)
                {
                    camgood = true;
                }
            }
               if (!camgood)
               {
                //menuCameraList.Items.Remove(WebCams[i].Device_Name);
                // menuCameraList.Items.RemoveAt(menuCameraList.Items.IndexOf(WebCams[i].Device_Name));
                //*********************************************************************************************************if we could remove it 
                //menuCameraList.Items.RemoveAt(WebCams[i].Device_ID);
                //menuCameraList.Items.RemoveAt(WebCams[i].Menu_Index);
                //menuCameraList.Items.Remove(WebCams.ToString());

                menuCameraList.Items.Remove(item);

                   //WebCams[i].Device_ID = -1;
                   //WebCams[i].Device_Name = null;
                   //WebCams[i].Identifier = Guid.Empty;
                   //WebCams[i].Menu_Index = -1;
               }
               else
               {
                   camgood = false;
               }
            }




        //WebCams = new Video_Device[_SystemCameras.Length];
        //cbCameraList.Items.Clear();

        camgood = false;


        //menuCameraList.Items.Clear();

        for (int i = 0; i < _SystemCameras.Length; i++)
        {
            for (int a = 0; a < WebCams.Length; a++)
            {
                if (_SystemCameras[i].Name == WebCams[a].Device_Name)
                {
                    camgood = true;
                }
            }

            if (!camgood)
            {
                int x = 0;
                bool numgood = false;

                while (!numgood)
                {
                    if (WebCams[x].Device_ID == -1)
                    {
                        numgood = true;
                    }
                    x++;
                }

                WebCams[x] = new Video_Device(i, _SystemCameras[i].Name);

                //cbCameraList.Items.Add(WebCams[i].ToString());
                MenuItem item = new MenuItem { Header = WebCams[x].ToString() };
                item.Click += new RoutedEventHandler(menuCameraListItem_Click);
                item.IsCheckable = true;
                menuCameraList.Items.Add(item);

                //WebCams[x].Menu_Index = menuCameraList.Items.IndexOf(item);
            }
            else
            {
                camgood = false;
            }


        }


        //List<string> mylist = WebCams.OfType<string>().ToList();

        //menuCameraList.Remove



        //if (cbCameraList.Items.Count == 0)
        if (menuCameraList.Items.Count == 0)
        {
            //cbCameraList.Items.Add("No Cameras Found");
            menuCameraList.Items.Add("No Cameras Found");
            //butCameraConnect.Visibility = Visibility.Hidden;
            menuCameraConnect.IsEnabled = false;
        }
        else
        {
            menuCameraList.Items.Remove("No Cameras Found");
        }
        //cbCameraList.SelectedIndex = 0;
    }
}
*/
        #endregion


        private void SetupCapture()
        {
            //CameraDevice = cbCameraList.SelectedIndex;

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
                //currentlyConnectedCamera = menuCameraList.Item
                currentlyConnectedCamera = menusender.ToString();
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
                currentlyConnectedCamera = null;
                CameraDevice = -1;
                menuCameraConnect.IsEnabled = false;
            }
        }

        //dont use checked use click and program change the checked
        private void menuCameraConnect_Checked(object sender, RoutedEventArgs e)
        {
            if (!_captureInProgress)
            {
                SetupCapture();
                _captureInProgress = true;
                var allitems = menuCameraList.Items.Cast<System.Windows.Controls.MenuItem>().ToArray();

                foreach (var item in allitems)
                {
                    item.IsEnabled = false;
                }
            }
            else
            {
                //_captureInProgress = false;
                //_capture.Pause();
            }
        }

        private void menuCameraList_MouseEnter(object sender, MouseEventArgs e)
        {
            PopulateCameras();
        }

        private void MenuItem_MouseEnter(object sender, MouseEventArgs e)
        {
            //PopulateCameras();
        }
    }
}