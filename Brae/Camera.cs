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
        public string MonikerString { get; set; }
        public int CapabilityIndex { get; set; }
        public FilterType Filter { get; set; }
        public StatusType Status { get; private set; }       
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

        public EventHandler Process { get; set; }

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

        // Capture
        public void StartCapture()
        {
            if (Status != StatusType.PLAYING)
            {
                // Get the user-selected camera device
                videoCapture = new VideoCapture(Index);
                videoCapture.ImageGrabbed += GetFrame;
                var videoDevice = new VideoCaptureDevice(MonikerString);
                // Set the user selected capability
                var videoCapability = videoDevice.VideoCapabilities[CapabilityIndex];
                // Enable hardware encoding if the camera supports it
                videoCapture.SetCaptureProperty(CapProp.FourCC, VideoWriter.Fourcc('M', 'J', 'P', 'G'));
                // Set the camera frame size and framerate
                videoCapture.SetCaptureProperty(CapProp.FrameHeight, videoCapability.FrameSize.Height);
                videoCapture.SetCaptureProperty(CapProp.FrameWidth, videoCapability.FrameSize.Width);
                videoCapture.SetCaptureProperty(CapProp.FrameCount, videoCapability.AverageFrameRate);
                //videoCapture.SetCaptureProperty(CapProp.Exposure, -6);

                // Start capturing and recording fps
                videoCapture.Start();
                fpsTimer.Start();

                // Update the camera status
                Status = StatusType.PLAYING;
            }
        }
        public void StopCapture()
        {
            if (Status != StatusType.STOPPED)
            {
                videoCapture.Stop();
                videoCapture.ImageGrabbed -= GetFrame;
                videoCapture.Dispose();
                fpsTimer.Stop();
                Status = StatusType.STOPPED;
            }
        }
        public void PauseCapture()
        {
            if (Status == StatusType.PLAYING)
            {
                videoCapture.Pause();
                Status = StatusType.PAUSED;
            }
        }
        public void ResumeCapture()
        {
            if (Status == StatusType.PAUSED)
            {
                videoCapture.Start();
                Status = StatusType.PLAYING;
            }    
        }     

        // Video
        public void StartReplaying(string path)
        {
            // TODO: Recording methods for camera
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

        // Controls
        public void FlipHorizontal()
        {
            if (videoCapture != null)
            {
                videoCapture.FlipHorizontal = !videoCapture.FlipHorizontal;
            }
        }
        public void FlipVertical()
        {
            if (videoCapture != null)
            {
                videoCapture.FlipVertical = !videoCapture.FlipVertical;
            }
        }      
        public void OpenSettings()
        {
            if (videoCapture != null)
            {
                videoCapture.SetCaptureProperty(CapProp.Settings,0);
            }
        }
        public void SetWhiteBalance(int value)
        {
            if (videoCapture != null && Status == StatusType.PLAYING)
            {
                videoCapture.SetCaptureProperty(CapProp.WhiteBalanceBlueU, value);
                var check = videoCapture.GetCaptureProperty(CapProp.WhiteBalanceBlueU);
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
        private void GetFrame(object sender, EventArgs e)
        {
            frameCount++;
            videoCapture.Retrieve(Frame);
            Process(Frame, e);
        }
        #endregion
    }
}