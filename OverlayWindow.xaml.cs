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
using System.Windows.Shapes;

namespace SwarmRoboticsGUI
{
    /// <summary>
    /// Interaction logic for Overlay.xaml
    /// </summary>
    public partial class OverlayWindow : Window
    {
        private MainWindow mainWindow;

        public OverlayWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            DataContext = this;
        }

        public double UpperC { get; set; } = 255;
        public double LowerC { get; set; } = 128;
        public double LowerH { get; set; } = 0;
        public double LowerS { get; set; } = 0;
        public double LowerV { get; set; } = 0;
        public double UpperH { get; set; } = 255;
        public double UpperS { get; set; } = 255;
        public double UpperV { get; set; } = 255;

        private void Overlay_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }
    }
}
