using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.Util;
using Microsoft.Win32;
using System;
using System.Timers;
using System.Text;
using System.Windows;

namespace SwarmRoboticsGUI
{
    public class Camera
    {
        // Enumerations
        #region
        public enum StatusType { PLAYING, PAUSED, STOPPED, REPLAY_ACTIVE, REPLAY_PAUSED, RECORDING };
        public enum WindowStatusType { MAXIMISED, MINIMISED, POPPED_OUT };
        public enum TimeDisplayModeType { CURRENT, FROM_START, START };
        public enum FilterType { NONE, GREYSCALE, CANNY_EDGES, BRAE_EDGES, NUM_FILTERS };
        #endregion

        // Camera Properties
        public string Name { get; set; }
        public int Index { get; set; }
        public int FPS { get; private set; }   
        public FilterType Filter { get; set; }
        public StatusType Status { get; set; }
        public WindowStatusType WindowStatus { get; set; }
        public TimeDisplayModeType TimeDisplayMode { get; set; }
        public double WindowSize { get; set; }
        public Mat Frame { get; set; }

        //
        public VideoWriter videowriter;
        private VideoCapture Capture;
        private Timer FpsTimer;
        private int FrameCount;

        // TODO: Sort variables
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
        #endregion


        public override string ToString()
        {
            return string.Format("[{0}]{1}", Index, Name);
        }
        public static string ToString(FilterType filter)
        {
            switch (filter)
            {
                case FilterType.NONE:
                    return string.Format("No Filter");
                case FilterType.GREYSCALE:
                    return string.Format("Greyscale");
                case FilterType.CANNY_EDGES:
                    return string.Format("Canny Edges");
                case FilterType.BRAE_EDGES:
                    return string.Format("Brae Edges");
                default:
                    return string.Format("Filter Text Error");
            }
        }
       
        // Camera
        public Camera()
        {
            Name = null;
            Index = 0;
            Status = StatusType.STOPPED;
            Filter = FilterType.NONE;

            // TODO: possible change
            WindowStatus = WindowStatusType.MAXIMISED;
            TimeDisplayMode = TimeDisplayModeType.CURRENT;

            FpsTimer = new Timer(1000);
            FpsTimer.AutoReset = true;
            FpsTimer.Elapsed += FpsTimer_Tick;
            FpsTimer.Enabled = true;
        }
        // Timers
        private void FpsTimer_Tick(object sender, EventArgs e)
        {
            FPS = FrameCount;
            FrameCount = 0;
        }
        // Methods
        private Mat ProcessFilter()
        {
            switch (Filter)
            {
                case FilterType.NONE:
                    return Frame;
                case FilterType.GREYSCALE:
                    CvInvoke.CvtColor(Frame, Frame, ColorConversion.Bgr2Gray);
                    break;
                case FilterType.CANNY_EDGES:
                    CvInvoke.CvtColor(Frame, Frame, ColorConversion.Bgr2Gray);
                    CvInvoke.PyrDown(Frame, Frame);
                    CvInvoke.PyrUp(Frame, Frame);
                    CvInvoke.Canny(Frame, Frame, 80, 40);
                    break;
                case FilterType.BRAE_EDGES:
                    CvInvoke.CvtColor(Frame, Frame, ColorConversion.Bgr2Gray);
                    CvInvoke.Threshold(Frame, Frame, LowerC, UpperC, ThresholdType.Binary);
                    CvInvoke.AdaptiveThreshold(Frame, Frame, UpperC, AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 3, 0);
                    //CvInvoke.FindContours(outputframe, mycontours, null, RetrType.External, ChainApproxMethod.ChainApproxNone, 0);
                    break;
                default:
                    break;
            }
            return Frame;
        }
        public void StartCapture()
        {
            //if the capture has perviously been used and not disposed of we should dispose of it now
            if (Capture != null)
            {
                Capture.Dispose();
            }
            //if the frame has perviously been used and not disposed of we should dispose of it now     
            // TODO: make a clean up method?
            if (Frame != null)
            {
                Frame.Dispose();
            }
            try
            {
                // update the capture object           
                Capture = new VideoCapture(Index);
                // create a new matrix to hold our image
                Frame = new Mat();
                // add event handler for our new capture  
                Capture.ImageGrabbed += ProcessFrame;               
                // update our status
                Status = StatusType.PLAYING;
                // start the capture       
                Capture.Start();                           
            }
            catch (NullReferenceException excpt)
            {
                MessageBox.Show(excpt.Message);
            }
        }
        public void StopCapture()
        {
            if (Status == StatusType.PLAYING)
            {
                try
                {
                    Capture.Stop();
                    Capture.Dispose();
                    Status = StatusType.STOPPED;
                }
                catch (Exception excpt)
                {
                    MessageBox.Show(excpt.Message);
                }
            }
            else
            {
                // TODO: we should provide an options/confirmation box
                MessageBox.Show("stop recording before stopping capture");  
            }
        }
        public void PauseCapture()
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
        public void ResumeCapture()
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

        public void StartReplaying(string path)
        {
            if (Capture != null)
            {
                Capture.Dispose();
            }
            Status = StatusType.REPLAY_ACTIVE;
            Capture = new VideoCapture(path);
            Capture.ImageGrabbed += ProcessFrame;
            Frame = new Mat();
            Capture.Start();
        }
        public void StartRecording(string path)
        {
            try
            {
                //recordframewidth = (int)_capture.GetCaptureProperty(CapProp.FrameWidth);
                //recordframeheight = (int)_capture.GetCaptureProperty(CapProp.FrameHeight);

                //recordsize.Width = recordframewidth;
                //recordsize.Height = recordframeheight;
                System.Drawing.Size recordsize = new System.Drawing.Size();
                recordsize.Width = 640;
                recordsize.Height = 480;
                try
                {
                    // TODO: Fix recording
                    videowriter = new VideoWriter(path, -1, 30, recordsize, true);
                }
                catch (Exception)
                {

                }
                Status = StatusType.RECORDING;
            }
            catch (NullReferenceException excpt)
            {
                MessageBox.Show(excpt.Message);
            }
        }
        public void StopRecording()
        {
            videowriter.Dispose();
            Status = StatusType.PLAYING;
        }


        public void ProcessFrame(object sender, EventArgs arg)
        {    
            if (Capture != null && Capture.Ptr != IntPtr.Zero)
            {
                Capture.Retrieve(Frame, 0);
                FrameCount++;
                if (FrameCount > 60)
                {
                    FrameCount = 0;
                    // Timer wasn't enabled
                    FpsTimer.Enabled = true;
                }
                ProcessFilter();
            }
        }

        public void FlipVertical()
        {
            if (Capture != null)
            {
                Capture.FlipVertical = !Capture.FlipVertical;
            }
        }
        public void FlipHorizontal()
        {
            if (Capture != null)
            {
                Capture.FlipHorizontal = !Capture.FlipHorizontal;
            }
        }

        public void OpenSettings()
        {
            //need try/catch or checks 
            if (Name != null)
            {
                try
                {
                    Capture.SetCaptureProperty(CapProp.Settings, 1);
                }
                catch (Exception excpt)
                {
                    MessageBox.Show(excpt.Message);
                }
            }
            else
            {
                MessageBox.Show("No Currently Connected Camera!");
            }
        }
    }
}