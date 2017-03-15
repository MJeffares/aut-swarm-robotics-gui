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

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.Util;

using System.Drawing;
using System.Windows.Forms;


namespace SwarmRoboticsGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private VideoCapture _capture = null;
        private Mat _frame;     

        public MainWindow()
        {
            InitializeComponent();
            CvInvoke.UseOpenCL = false;
            ImageBox imagebox1 = new ImageBox();

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
            _capture.Start();
        }

        private void ProcessFrame(object sender, EventArgs arg)
        {
            if(_capture != null && _capture.Ptr != IntPtr.Zero)
            {
                _capture.Retrieve(_frame, 0);

                captureImageBox.Image = _frame;
            }  
        }
    }
}
