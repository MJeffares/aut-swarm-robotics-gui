using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.Util;
using System;
using System.Timers;
using System.Text;
using System.Drawing;

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
        public UMat Frame { get; private set; }
        public Size Resolution { get; private set; }
        public int FPS { get; private set; }
        #endregion

        #region Private Properties
        // Capture Properties
        private int FrameCount { get; set; }
        private VideoWriter videoWriter { get; set; }
        private VideoCapture videoCapture { get; set; }
        private Timer FpsTimer { get; set; }
        #endregion

        #region Public Events
        public delegate void FrameHandler(Camera cam, EventArgs e);
        public event FrameHandler FrameUpdate;
        #endregion
        public Camera(int Width = 640, int Height = 480)
        {
            Status = StatusType.STOPPED;
            Resolution = new Size(Width, Height);
            InitializeTimer();
        }
        public Camera(Size Resolution)
        {
            Status = StatusType.STOPPED;
            this.Resolution = Resolution;
            InitializeTimer();
        }

        #region Public Methods
        // ToString Override
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
            // update the capture object           
            videoCapture = new VideoCapture(Index);
            videoCapture.SetCaptureProperty(CapProp.FrameWidth, Resolution.Width);
            videoCapture.SetCaptureProperty(CapProp.FrameHeight, Resolution.Height);
            // create a new matrix to hold our image
            Frame = new UMat();
            // add event handler for our new capture  
            videoCapture.ImageGrabbed += ProcessFrame;
            // update our status
            Status = StatusType.PLAYING;
            // start the capture       
            videoCapture.Start();
        }
        public void StopCapture()
        {
            if (Status == StatusType.PLAYING)
            {
                videoCapture.Stop();
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
        public void StartReplaying(string path)
        {
            if (videoCapture != null)
            {
                videoCapture.Dispose();
            }
            videoCapture = new VideoCapture(path);
            Frame = new UMat();
            videoCapture.ImageGrabbed += ProcessFrame;
            Status = StatusType.REPLAY_ACTIVE;
            videoCapture.Start();
        }
        public void StartRecording(string path)
        {
            //recordframewidth = (int)_capture.GetCaptureProperty(CapProp.FrameWidth);
            //recordframeheight = (int)_capture.GetCaptureProperty(CapProp.FrameHeight);

            //recordsize.Width = recordframewidth;
            //recordsize.Height = recordframeheight;
            Size recordsize = Resolution;

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
            //need try/catch or checks 
            if (Name != null)
            {
                videoCapture.SetCaptureProperty(CapProp.Settings, 1);
            }
        }
        public void Dispose()
        {
            FpsTimer.Dispose();
            videoCapture.Dispose();
            Frame.Dispose();
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
        private void ProcessFrame(object sender, EventArgs arg)
        {
            // Check capture exists
            if (videoCapture != null && videoCapture.Ptr != IntPtr.Zero)
            {
                // Get the new frame
                videoCapture.Retrieve(Frame, 0);
                FrameUpdate(this, arg);
                // 
                FrameCount++;
            }
        }
        #endregion
    }
}