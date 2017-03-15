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
using Emgu.CV.Structure;
using Emgu.CV.UI;

namespace SwarmRoboticsGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Create the interop host control
            System.Windows.Forms.Integration.WindowsFormsHost host = new System.Windows.Forms.Integration.WindowsFormsHost();

            //Create imagebox Control
            ImageBox imagebox1 = new ImageBox();

            string FileName = "g:\\lena.jpg";
            Image<Bgr, byte> imgInput = new Image<Bgr, byte>(FileName);
            imagebox1.Image = imgInput;

            //Assign ImageBox as the host controller
            host.Child = imagebox1;

            //Add the interop host control to the grid control's collection of child controls
            this.grid1.Children.Add(host);

        }
    }
}
