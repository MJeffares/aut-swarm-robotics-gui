using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Media;

namespace SwarmRoboticsGUI
{
    class ImageAnimation
    {
        public bool IsRunning { get; private set; }
        private int Property { get; set; }
        private int StartValue { get; set; }
        private int EndValue { get; set; }
        private int End { get; set; }
        private double Duration { get; set; }
        private int FPS { get; set; }

        public delegate void AnimationHandler(int Property, EventArgs e);
        public event AnimationHandler AnimationUpdate;

        public ImageAnimation(int Property, int Start, int End, double Duration)
        {
            FPS = 60;

            this.Duration = Duration;
            this.End = End;
            
            EndValue = (int)(Duration * FPS / 1000);
            StartValue = Start;
        }

        private void Animation_Tick(object sender, EventArgs e)
        {
            if (Property <= EndValue)
            {
                Property++;
                AnimationUpdate(Property * End / EndValue, e);
            }
            else
            {
                Stop();
            }
        }

        public void Start()
        {
            if (!IsRunning)
            {
                Property = StartValue;
                CompositionTarget.Rendering += Animation_Tick;               
                IsRunning = true;
            }            
        }
        public void Stop()
        {
            if (IsRunning)
            {
                Property = StartValue;
                CompositionTarget.Rendering -= Animation_Tick;
                IsRunning = false;                
            }                    
        }
        public void Reset()
        {
            // TEMP:
            Stop();
        }
    }
}
