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
        public double UpperC { get; set; }
        public double LowerC { get; set; }
        public int ColourC { get; set; }
        public int LowerH { get; set; } 
        public int LowerS { get; set; }
        public int LowerV { get; set; }
        public int UpperH { get; set; }
        public int UpperS { get; set; }
        public int UpperV { get; set; }

        public ImageProcessing imgProc { get; set; }
        private MainWindow mainWindow { get; set; }
        private Timer FrameTimer { get; set; }
        private Timer InterfaceTimer { get; set; }

        public OverlayWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            imgProc = new ImageProcessing();
            this.mainWindow = mainWindow;
            DataContext = this;

            ColourC = 1000;
            LowerS = 25;

            FrameTimer = new Timer(34);
            FrameTimer.Elapsed += Frame_Tick;
            FrameTimer.Start();

            InterfaceTimer = new Timer(200);
            InterfaceTimer.Elapsed += Interface_Tick;
            InterfaceTimer.Start();
        }
        private void Frame_Tick(object sender, ElapsedEventArgs e)
        {
            // Apply image processing
            if (mainWindow.Camera1.Frame != null)
            {
                imgProc.ProcessFilter(mainWindow.Camera1.Frame);
                OverlayImageBox.Image = imgProc.OverlayImage;
            }
            //imgProc.ProcessFilter(null);
            //OverlayImageBox.Image = imgProc.OverlayImage;
        }

        private void Interface_Tick(object sender, ElapsedEventArgs e)
        {
            // HACK: update them every frame               
            imgProc.ColourC = ColourC;
            //Camera1.imgProc.UpperC = Overlay.UpperC;
            //Camera1.imgProc.LowerC = Overlay.LowerC;
            imgProc.LowerH = LowerH;
            imgProc.LowerS = LowerS;
            //Camera1.imgProc.LowerV = Overlay.LowerV;
            imgProc.UpperH = UpperH;
            //Camera1.imgProc.UpperV = Overlay.UpperV;
        }

        private void Overlay_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            FrameTimer.Dispose();
            InterfaceTimer.Dispose();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            imgProc.ClearRobots();
        }
    }
}
