using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.Util;
using Emgu.CV.Cuda;
using System;
using System.Timers;
using System.Text;
using System.Drawing;
using AForge.Video.DirectShow;
using AForge.Video;

namespace SwarmRoboticsGUI
{
    public class Camera
    {
        #region Enumerations
        public enum StatusType { PLAYING, PAUSED, STOPPED, REPLAY_ACTIVE, REPLAY_PAUSED, RECORDING };
        #endregion 

        #region Public Properties
        // Camera Properties
        public string Name { get; set; }
        public int Index { get; set; }
        public StatusType Status { get; private set; }
        public int CapabilityIndex { get; set; }
        public int FPS { get; private set; }
        #endregion

        #region Private Properties
        // Capture Properties
        private int FrameCount { get; set; }
        private VideoWriter videoWriter { get; set; }
        private VideoCaptureDevice videoSource { get; set; }
        private Timer FpsTimer { get; set; }
        #endregion

        #region Public Events
        public delegate void FrameHandler(Bitmap Frame, NewFrameEventArgs e);
        public event FrameHandler FrameUpdate;
        #endregion
        public Camera()
        {
            Status = StatusType.STOPPED;
            InitializeTimer();
        }

        #region Public Methods
        // ToString Overrides
        public override string ToString()
        {
            return string.Format("[{0}]{1}", Index, Name);
        }
        //Capture Methods
        public void Resize(Size Size)
        {
            if (Status == StatusType.PLAYING)
            {
                //videoCapture.Stop();
                videoSource.Stop();
            }

        }

        public void StartCapture()
        {
            // gets currently connected devices
            var VideoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            // create video source
            videoSource = new VideoCaptureDevice(VideoDevices[Index].MonikerString);

            videoSource.VideoResolution = videoSource.VideoCapabilities[CapabilityIndex];

            // set NewFrame event handler
            videoSource.NewFrame += new NewFrameEventHandler(ProcessFrame);
            // start the video source
            videoSource.Start();

            Status = StatusType.PLAYING;
        }
        public void StopCapture()
        {
            if (Status == StatusType.PLAYING)
            {
                videoSource.SignalToStop();
                Status = StatusType.STOPPED;
            }
        }
        public void PauseCapture()
        {
            videoSource.SignalToStop();
            Status = StatusType.PAUSED;
        }
        public void ResumeCapture()
        {
            Status = StatusType.PLAYING;
            videoSource.Start();
        }
        // Video Methods
        public void StartReplaying(string path)
        {
            //if (videoCapture != null)
            //{
            //    videoCapture.Dispose();
            //}
            //videoCapture = new VideoCapture(path);
            //videoCapture.ImageGrabbed += ProcessFrame;
            //Status = StatusType.REPLAY_ACTIVE;
            //videoCapture.Start();
        }
        public void StartRecording(string path)
        {
            //recordframewidth = (int)_capture.GetCaptureProperty(CapProp.FrameWidth);
            //recordframeheight = (int)_capture.GetCaptureProperty(CapProp.FrameHeight);

            //recordsize.Width = recordframewidth;
            //recordsize.Height = recordframeheight;
            // BRAE: The recording size is set to 0,0... what on earth does that solve.
            Size recordsize = new Size(0,0);

            // TODO: Fix recording
            videoWriter = new VideoWriter(path, -1, 30, recordsize, true);

            Status = StatusType.RECORDING;
        }
        public void StopRecording()
        {
            videoWriter.Dispose();
            Status = StatusType.PLAYING;
        }
        // Other Methods
        public void FlipVertical()
        {
            //if (videoCapture != null)
            //{
            //    videoCapture.FlipVertical = !videoCapture.FlipVertical;
            //}
        }
        public void FlipHorizontal()
        {
            //if (videoCapture != null)
            //{
            //    videoCapture.FlipHorizontal = !videoCapture.FlipHorizontal;
            //}
        }
        public void OpenSettings()
        {
            ////need try/catch or checks 
            //if (Name != null)
            //{
            //    videoCapture.SetCaptureProperty(CapProp.Settings, 1);
            //}
        }
        public void Dispose()
        {
            FpsTimer.Dispose();
        }
        #endregion

        #region Private Methods
        private void InitializeTimer()
        {
            //
            FpsTimer = new Timer(1000);
            FpsTimer.AutoReset = true;
            FpsTimer.Elapsed += FpsTimer_Tick;
            FpsTimer.Enabled = true;
        }
        private void FpsTimer_Tick(object sender, EventArgs e)
        {
            FPS = FrameCount;
            FrameCount = 0;
        }
        private void ProcessFrame(object sender, NewFrameEventArgs e)
        {
            FrameUpdate(e.Frame, e);
            FrameCount++;
        }
        #endregion
    }
}