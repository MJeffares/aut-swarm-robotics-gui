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
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace SwarmRoboticsGUI
{
    public enum StatusType { PLAYING, PAUSED, STOPPED, REPLAY_ACTIVE, REPLAY_PAUSED, RECORDING };
    public class Camera
    {
        #region Public Properties
        // Camera Properties
        public string Name { get; set; }
        public int Index { get; set; }
        public StatusType Status { get; private set; }
        public FilterType Filter { get; set; }
        public int CapabilityIndex { get; set; }
        public int Fps { get; private set; }
        public TimeSpan RecordingTime { get; private set; }
        #endregion

        #region Private Properties
        // Capture Properties
        private int frameCount { get; set; }
        private VideoWriter videoWriter { get; set; }
        private VideoCaptureDevice videoSource { get; set; }
        private AsyncVideoSource asyncVideoSource { get; set; }
        private Timer fpsTimer { get; set; }
        private DateTime recordingStartTime { get; set; }
        #endregion

        #region Public Events
        public delegate void FrameHandler(object sender, NewFrameEventArgs e);
        public event FrameHandler FrameUpdate;
        #endregion
        public Camera()
        {
            Status = StatusType.STOPPED;
            Filter = FilterType.NONE;
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
            if (Status != StatusType.PLAYING)
            {
                // gets currently connected devices
                var VideoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                // create video source
                videoSource = new VideoCaptureDevice(VideoDevices[Index].MonikerString);
                videoSource.VideoResolution = videoSource.VideoCapabilities[CapabilityIndex];

                asyncVideoSource = new AsyncVideoSource(videoSource, true);
                // set NewFrame event handler
                asyncVideoSource.NewFrame += new NewFrameEventHandler(FrameUpdate);
                // start the video source
                asyncVideoSource.Start();
                Status = StatusType.PLAYING;
                fpsTimer.Start();
            }
        }
        public void StopCapture()
        {
            if (Status == StatusType.PLAYING)
            {
                //videoSource.SignalToStop();                
                //videoSource.WaitForStop();
                asyncVideoSource.SignalToStop();
                asyncVideoSource.WaitForStop();
                if (FrameUpdate != null)
                asyncVideoSource.NewFrame -= new NewFrameEventHandler(FrameUpdate);
                fpsTimer.Stop();
                Status = StatusType.STOPPED;
            }
        }
        public void PauseCapture()
        {
            asyncVideoSource.SignalToStop();
            Status = StatusType.PAUSED;
        }
        public void ResumeCapture()
        {
            Status = StatusType.PLAYING;
            asyncVideoSource.Start();
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
            if (Name != null)
            {
                // gets currently connected devices
                var VideoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                // create video source
                videoSource = new VideoCaptureDevice(VideoDevices[Index].MonikerString);
                videoSource.DisplayPropertyPage(new IntPtr());
            }
        }
        #endregion

        #region Private Methods
        private void InitializeTimer()
        {
            //
            fpsTimer = new Timer(1000);
            fpsTimer.AutoReset = true;
            fpsTimer.Elapsed += FpsTimer_Tick;
        }
        private void FpsTimer_Tick(object sender, EventArgs e)
        {
            if (Status == StatusType.RECORDING)
            {
                RecordingTime = DateTime.Now - recordingStartTime;
            }

            Fps = frameCount;
            frameCount = 0;
        }
        #endregion
    }
}