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
    public class Camera : IDisposable
    {
        #region Public Properties
        // Camera Properties
        public string Name { get; set; }
        public int Index { get; set; }
        public StatusType Status { get; private set; }
        public FilterType Filter { get; set; }
        public int CapabilityIndex { get; set; }
        public int FPS { get; private set; }
        #endregion

        #region Private Properties
        // Capture Properties
        private int FrameCount { get; set; }
        private VideoWriter videoWriter { get; set; }
        private VideoCaptureDevice videoSource { get; set; }
        private AsyncVideoSource AsyncVS { get; set; }
        private Timer FpsTimer { get; set; }
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
            // gets currently connected devices
            var VideoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            // create video source
            videoSource = new VideoCaptureDevice(VideoDevices[Index].MonikerString);
            videoSource.VideoResolution = videoSource.VideoCapabilities[CapabilityIndex];

            AsyncVS = new AsyncVideoSource(videoSource);
            // set NewFrame event handler
            AsyncVS.NewFrame += new NewFrameEventHandler(FrameUpdate);
            // start the video source
            AsyncVS.Start();
            Status = StatusType.PLAYING;
        }
        public void StopCapture()
        {
            if (Status == StatusType.PLAYING)
            {
                AsyncVS.NewFrame -= new NewFrameEventHandler(FrameUpdate);
                AsyncVS.SignalToStop();
                Status = StatusType.STOPPED;
            }
        }
        public void PauseCapture()
        {
            AsyncVS.SignalToStop();
            Status = StatusType.PAUSED;
        }
        public void ResumeCapture()
        {
            Status = StatusType.PLAYING;
            AsyncVS.Start();
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

        private bool disposed = false;
        private SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);
        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);

        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            if (disposing)
                handle.Dispose();

            if (FpsTimer != null)
            {
                FpsTimer.Stop();
                FpsTimer.Dispose();
            }
            
            if (AsyncVS != null)
            {
                AsyncVS.Stop();
            }
            
            if (videoSource != null)
            {
                videoSource.Stop();
            }

            disposed = true;
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
        #endregion
    }
}