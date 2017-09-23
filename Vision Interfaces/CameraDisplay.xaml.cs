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
using Emgu.CV.Cuda;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace SwarmRoboticsGUI
{
    /// <summary>
    /// Interaction logic for CameraDisplay.xaml
    /// </summary>
    public partial class CameraDisplay : UserControl
    {

        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register("Image",
                typeof(IImage),
                typeof(CameraDisplay),
                null);


        public IImage Image
        {
            set { captureImageBox.Image = value; }
        }


        public CameraDisplay()
        {
            InitializeComponent();
            Image = new Mat();
        }
    }
}
