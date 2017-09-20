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
        private VideoCapture videoCapture { get; set; }
        private Timer fpsTimer { get; set; }
        private DateTime recordingStartTime { get; set; }

        private UMat Frame { get; set; }
        #endregion

        public Camera()
        {
            Status = StatusType.STOPPED;
            Filter = FilterType.NONE;
            InitializeTimer();
            Frame = new UMat();
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
                videoCapture.Stop();
            }

        }

        public void StartCapture()
        {
            if (Status != StatusType.PLAYING)
            {
                videoCapture = new VideoCapture(Index);
                videoCapture.ImageGrabbed += GetFrame;

                videoCapture.SetCaptureProperty(CapProp.FourCC, VideoWriter.Fourcc('M', 'J', 'P', 'G'));
                videoCapture.SetCaptureProperty(CapProp.FrameHeight, 1080);
                videoCapture.SetCaptureProperty(CapProp.FrameWidth, 1920);
                videoCapture.SetCaptureProperty(CapProp.FrameCount, 30);
                videoCapture.SetCaptureProperty(CapProp.Exposure, -6);
                videoCapture.Start();

                Status = StatusType.PLAYING;
                fpsTimer.Start();
            }
        }

        public void StopCapture()
        {
            if (Status != StatusType.STOPPED)
            {
                videoCapture.Stop();
                videoCapture.ImageGrabbed -= GetFrame;
                Status = StatusType.STOPPED;
            }
        }

        public void CloseCapture()
        {
            if (Status == StatusType.PLAYING)
            {
                if (videoCapture.IsOpened)
                {
                    videoCapture.Stop();
                }
                videoCapture.ImageGrabbed -= GetFrame;
                fpsTimer.Stop();
                Status = StatusType.STOPPED;
            }
        }
        public void PauseCapture()
        {
            videoCapture.Pause();
            Status = StatusType.PAUSED;
        }
        public void ResumeCapture()
        {
            videoCapture.Start();
            Status = StatusType.PLAYING;          
        }
        // Video Methods
        // TODO: Recording methods for camera
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
            if (videoCapture != null)
            {
                videoCapture.FlipVertical = !videoCapture.FlipVertical;
            }
        }
        public void FlipHorizontal()
        {
            if (videoCapture != null)
            {
                videoCapture.FlipHorizontal = !videoCapture.FlipHorizontal;
            }
        }
        public void OpenSettings()
        {
            if (Name != null)
            {
                videoCapture.SetCaptureProperty(CapProp.Settings,0);
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
        
        private void GetFrame(object sender, EventArgs e)
        {
            frameCount++;
            videoCapture.Retrieve(Frame);
            Process(Frame, e);
        }

        public EventHandler Process { get; set; }
    }
}