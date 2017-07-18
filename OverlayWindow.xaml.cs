﻿using Emgu.CV;
using Emgu.CV.CvEnum;
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
        public ImageDisplay Display { get; set; }
        private Timer InterfaceTimer { get; set; }
        // MANSEL: This robot list has seen the world
        // TODO: give these robots a home
        public Robot[] RobotList = new Robot[6];

        public void ClearRobots(Robot[] RobotList)
        {
            for (int i = 0; i < RobotList.Length; i++)
            {
                RobotList[i] = new Robot();
            }
        }

        public OverlayWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            ClearRobots(RobotList);

            imgProc = new ImageProcessing();
            Display = new ImageDisplay();

            DataContext = this;

            ColourC = 1000;
            LowerS = 25;

            InterfaceTimer = new Timer(100);
            InterfaceTimer.Elapsed += Interface_Tick;
            InterfaceTimer.Start();

            mainWindow.camera1.FrameUpdate += new Camera.FrameHandler(DrawOverlayFrame);
        }

        private void DrawOverlayFrame(object sender, EventArgs e)
        {
            switch (Display.Source)
            {
                case ImageDisplay.SourceType.NONE:
                    break;
                case ImageDisplay.SourceType.CAMERA:
                    Camera cam = (Camera)sender;
                    if (cam.Frame != null)
                    {
                        RobotList = imgProc.GetRobots(cam.Frame, RobotList);
                        Display.ProcessOverlay(cam.Frame, RobotList);
                        OverlayImageBox.Image = Display.Image;
                    }
                    break;
                case ImageDisplay.SourceType.CUTOUTS:
                    RobotList = imgProc.GetRobots(imgProc.testImage, RobotList);
                    Display.ProcessOverlay(imgProc.testImage, RobotList);
                    OverlayImageBox.Image = Display.Image;
                    break;
                default:
                    break;
            }
            //
            
        }

        private void Interface_Tick(object sender, ElapsedEventArgs e)
        {             
            imgProc.ColourC = ColourC;
            imgProc.LowerH = LowerH;
            imgProc.LowerS = LowerS;
            imgProc.UpperH = UpperH;
            //Camera1.imgProc.UpperV = Overlay.UpperV;

            switch (Display.Source)
            {
                case ImageDisplay.SourceType.NONE:
                    break;
                case ImageDisplay.SourceType.CAMERA:
                    break;
                case ImageDisplay.SourceType.CUTOUTS:
                    DrawOverlayFrame(this, new EventArgs());
                    break;
                default:
                    break;
            }
        }

        private void Overlay_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            InterfaceTimer.Dispose();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearRobots(RobotList);
        }

        private void OverlayImageBox_Click(object sender, EventArgs e)
        {
            Mouse.Capture(this);
            Point pos = Mouse.GetPosition(this);
            Mouse.Capture(null);
            Display.UserClick(pos);
        }

        private void Overlay_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Display != null)
                Display.Resize(OverlayImageBox.Width, OverlayImageBox.Height);
        }
    }
}
