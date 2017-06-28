using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
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
        public double UpperC { get; set; } = 255;
        public double LowerC { get; set; } = 0;
        public int ColourC { get; set; } = 10;
        public int LowerH { get; set; } = 0;
        public int LowerS { get; set; } = 0;
        public int LowerV { get; set; } = 0;
        public int UpperH { get; set; } = 255;
        public int UpperS { get; set; } = 255;
        public int UpperV { get; set; } = 255;

        public ImageProcessing imgProc { get; set; }
        private MainWindow mainWindow { get; set; }
        private Timer FrameTimer { get; set; }

        public OverlayWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            DataContext = this;

            FrameTimer = new Timer(50);
            FrameTimer.Elapsed += Frame_Tick;
            FrameTimer.Start();

            imgProc = new ImageProcessing();
        }
        private void Frame_Tick(object sender, ElapsedEventArgs e)
        {
            // Apply image processing
            imgProc.ProcessFilter(mainWindow.Camera1.Frame);
            OverlayImageBox.Image = imgProc.OverlayImage;
        }
        private void Overlay_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }
    }
}
