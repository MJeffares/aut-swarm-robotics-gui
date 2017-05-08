using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.Util;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms.Integration;
using System.Windows.Threading;

namespace SwarmRoboticsGUI
{
    public class Camera
    {
        public enum StatusType { PLAYING, PAUSED, STOPPED, REPLAY_ACTIVE, REPLAY_PAUSED, RECORDING };
        public enum WindowStatusType { MAXIMISED, MINIMISED, POPPED_OUT };
        public enum TimeDisplayMode { CURRENT, FROM_START, START };
        public enum FilterType { NONE, GREYSCALE, CANNY_EDGES, BRAE_EDGES, NUM_FILTERS };

        // Stay
        private ImageBox captureImageBox = new ImageBox();
        private WindowsFormsHost host1 = new WindowsFormsHost();
        private VideoCapture Capture;                       // the capture itself (ie camera when recording/viewing or a video file when replaying)
        private Video_Device[] webcams;                             // a list of connected video devices
        private Mat frame;                                         // openCV Matrix image
        private Mat outputframe = new Mat();                        // openCV Matric image of filter output   
        private DispatcherTimer FpsTimer = new DispatcherTimer();   // one second timer to calculate and update the fps count

        // Sort
        #region
        // HSV ranges.
        private const int LowerH = 0;
        private const int UpperH = 255;
        private const int LowerS = 0;
        private const int UpperS = 255;
        private const int LowerV = 0;
        private const int UpperV = 255;
        // Blur, Canny, and Threshold values.
        private const int BlurC = 1;
        private const int LowerC = 128;
        private const int UpperC = 255;
        // TODO:
        private int captureblockedframes = 0;                       // number of frames to delay drawing to screen                                                                 //fps timer variables
        private int _fpscount = 0;                                  // number of frames captured in the current second

        // video record/replay variables
        OpenFileDialog openvideodialog = new OpenFileDialog();
        SaveFileDialog savevideodialog = new SaveFileDialog();
        private double replayframerate = 0;
        private double replaytotalframes = 0;
        private double replayframecount;
        private VideoWriter _videowriter;

        private double replayspeed = 1;
        private int recordframerate = 30;
        private System.Drawing.Size recordsize = new System.Drawing.Size();
        
        private DateTime startTime;
        #endregion

        public string Name { get; set; }
        public int Index { get; set; }
        public FilterType Filter { get; set; }
        public StatusType Status { get; set; }
        public WindowStatusType WindowStatus { get; set; }
        public TimeDisplayMode _timeDisplayMode { get; set; }




        public Camera()
        {
            // Stay
            Name = null;
            Index = 0;
            Status = StatusType.STOPPED;
            Filter = FilterType.NONE;

            // Possibly go
            WindowStatus = WindowStatusType.MAXIMISED;
            _timeDisplayMode = TimeDisplayMode.FROM_START;


        captureImageBox.Parent.Name = host1.Name;
        }

        public Mat Process()
        {
            switch (Filter)
            {
                case FilterType.NONE:
                    return frame;
                case FilterType.GREYSCALE:
                    CvInvoke.CvtColor(frame, outputframe, ColorConversion.Bgr2Gray);
                    break;
                case FilterType.CANNY_EDGES:
                    CvInvoke.CvtColor(frame, outputframe, ColorConversion.Bgr2Gray);
                    CvInvoke.PyrDown(outputframe, outputframe);
                    CvInvoke.PyrUp(outputframe, outputframe);
                    CvInvoke.Canny(outputframe, outputframe, 80, 40);
                    break;
                case FilterType.BRAE_EDGES:
                    CvInvoke.CvtColor(frame, outputframe, ColorConversion.Bgr2Gray);
                    CvInvoke.Threshold(outputframe, outputframe, LowerC, UpperC, ThresholdType.Binary);
                    CvInvoke.AdaptiveThreshold(outputframe, outputframe, UpperC, AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 3, 0);
                    //CvInvoke.FindContours(outputframe, mycontours, null, RetrType.External, ChainApproxMethod.ChainApproxNone, 0);
                    break;
                default:
                    break;
            }
            return outputframe;
        }

        private void StartCapture()
        {
            //if the capture has perviously been used and not disposed of we should dispose of it now
            if (Capture != null)
            {
                Capture.Dispose();
            }

            //if the frame has perviously been used and not disposed of we should dispose of it now     // TODO: make a clean up method?
            if (frame != null)
            {
                frame.Dispose();
            }

            try
            {
                // change the visibility of our winforms host (our stream viewer is inside this)
                host1.Visibility = Visibility.Visible;
                // update the capture object           
                Capture = new VideoCapture(Index);
                // add event handler for our new capture  
                Capture.ImageGrabbed += ProcessFrame;
                // create a new matrix to hold our image
                frame = new Mat();
                // update our status
                Status = CaptureStatuses.PLAYING;
                // start the capture       
                Capture.Start();
                // start our fps timer                             

            }
            catch (NullReferenceException excpt)
            {
                MessageBox.Show(excpt.Message);
            }
        }
        private void StopCapture()
        {
            if (Status == StatusType.PLAYING)
            {
                try
                {
                    Capture.Stop();
                    Capture.Dispose();
                    host1.Visibility = Visibility.Hidden;
                    Status = StatusType.STOPPED;
                }
                catch (Exception excpt)
                {
                    MessageBox.Show(excpt.Message);
                }
            }
            else
            {
                MessageBox.Show("stop recording before stopping capture");  //we should provide an options/confirmation box
            }
        }
        // TODO: but what if were recording. we just dont want to display
        private void PauseCapture()
        {
            try
            {
                Capture.Pause();
                Status = StatusType.PAUSED;
            }
            catch (Exception excpt)
            {
                MessageBox.Show(excpt.Message);
            }
        }
        private void ResumeCapture()
        {
            try
            {
                Capture.Start();
                Status = StatusType.PLAYING;
            }
            catch (Exception excpt)
            {
                MessageBox.Show(excpt.Message);
            }
        }
        private void DisplayFrame()
        {
            if (captureblockedframes == 0)
            {
                switch (WindowStatus)
                {
                    case WindowStatusType.MAXIMISED:
                        captureImageBox.Image = Process();
                        break;

                    case WindowStatusType.MINIMISED:
                        // TODO: camera minimized
                        break;

                    case WindowStatusType.POPPED_OUT:
                        // TODO: camera popout window
                        ///Dispatcher.Invoke(() =>
                        ///{
                        ///    //((CameraPopOutWindow)System.Windows.Application.Current.cameraPopOutWindow).captureImageBox.Image = CaptureFilters.Process(filter, _frame, outputframe);
                        ///    popoutCameraWindow.captureImageBox.Image = CaptureFilters.Process(filter, _frame, outputframe);
                        ///});
                        break;
                }



            }
            else
            {
                captureblockedframes--;
            }
        }


        private void ProcessFrame(object sender, EventArgs arg)
        {
            if (Capture != null && Capture.Ptr != IntPtr.Zero)
            {
                Capture.Retrieve(frame, 0);

                if (Status == StatusType.PLAYING)
                {
                    DisplayFrame();
                }
                else if (Status == StatusType.REPLAY_ACTIVE)
                {
                    replayframecount = Capture.GetCaptureProperty(CapProp.PosFrames);
                    //display current frame/time
                    DisplayFrame();
                    Thread.Sleep((int)(1000.0 / (replayframerate * replayspeed)));
                }
                else if (Status == StatusType.RECORDING)
                {
                    DisplayFrame();
                    if (_videowriter.Ptr != IntPtr.Zero)
                    {
                        //need to write based on the option selected
                        //_videowriter.Write(_frame);
                        _videowriter.Write(Process());
                    }
                }
            }
        }


    }
}
