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


        private void Overlay_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        public double UpperC { get; set; } = 255;
        public double LowerC { get; set; } = 0;

        public int ColourCount { get; set; } = 100;
        public int LowerH { get; set; } = 0;
        public int LowerS { get; set; } = 0;
        public int LowerV { get; set; } = 0;
        public int UpperH { get; set; } = 255;
        public int UpperS { get; set; } = 255;
        public int UpperV { get; set; } = 255;

    }
}
