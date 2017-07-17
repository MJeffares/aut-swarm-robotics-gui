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
        #endregion

        // Camera Properties
        public string Name { get; set; }
        public int Index { get; set; }
        public StatusType Status { get; private set; }
        public UMat Frame { get; private set; }
        public int FPS { get; private set; }
        public int FrameCount { get; private set; }

        private VideoWriter videoWriter { get; set; }
        private VideoCapture videoCapture { get; set; }
        private Timer FpsTimer { get; set; }

        public delegate void FrameHandler(Camera cam, EventArgs e);
        public event FrameHandler FrameUpdate;

        public override string ToString()
        {
            return string.Format("[{0}]{1}", Index, Name);
        }
       
        // Camera
        public Camera()
        {
            Name = null;
            Index = 0;
            Status = StatusType.STOPPED;
            //
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
        public void StartCapture()
        {
            try
            {
                // update the capture object           
                videoCapture = new VideoCapture(Index);
                //videoCapture.SetCaptureProperty(CapProp.FrameHeight, 720);
                //videoCapture.SetCaptureProperty(CapProp.FrameWidth, 1280);
                // create a new matrix to hold our image
                Frame = new UMat();
                // add event handler for our new capture  
                videoCapture.ImageGrabbed += ProcessFrame;
                // update our status
                Status = StatusType.PLAYING;
                // start the capture       
                videoCapture.Start();                           
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
                    videoCapture.Stop();
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
                videoCapture.Pause();
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
                videoCapture.Start();
                Status = StatusType.PLAYING;
            }
            catch (Exception excpt)
            {
                MessageBox.Show(excpt.Message);
            }
        }

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
            try
            {
                //recordframewidth = (int)_capture.GetCaptureProperty(CapProp.FrameWidth);
                //recordframeheight = (int)_capture.GetCaptureProperty(CapProp.FrameHeight);

                //recordsize.Width = recordframewidth;
                //recordsize.Height = recordframeheight;
                System.Drawing.Size recordsize = new System.Drawing.Size();
                recordsize.Width = 1280;
                recordsize.Height = 720;

                // TODO: Fix recording
                videoWriter = new VideoWriter(path, -1, 30, recordsize, true);

                Status = StatusType.RECORDING;
            }
            catch (NullReferenceException excpt)
            {
                MessageBox.Show(excpt.Message);
            }
        }
        public void StopRecording()
        {
            videoWriter.Dispose();
            Status = StatusType.PLAYING;
        }

        public void ProcessFrame(object sender, EventArgs arg)
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
                try
                {
                    videoCapture.SetCaptureProperty(CapProp.Settings, 1);
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

        public void Dispose()
        {
            FpsTimer.Dispose();
            videoCapture.Dispose();
            Frame.Dispose();
        }
    }
}