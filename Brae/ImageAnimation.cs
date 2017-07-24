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
        private double FPS { get; set; }

        private Timer test { get; set; }

        public delegate void AnimationHandler(int Property, EventArgs e);
        public event AnimationHandler AnimationUpdate;

        public ImageAnimation(int Property, int Start, int End, double Duration)
        {
            //FPS = 60;
            FPS = 25;

            this.Duration = Duration;
            this.End = End;
            

            EndValue = (int)(Duration * FPS / 1000);
            StartValue = Start;

            InitializeTimer();
        }

        private void InitializeTimer()
        {
            //
            test = new Timer(40);
            test.AutoReset = true;
            test.Enabled = true;
        }

        private void Animation_Tick(object sender, EventArgs e)
        {
            if (Property < EndValue)
            {
                Property++;
                if (Property * End / EndValue > 100)
                {
                    var test = Property * End / EndValue;
                }
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
                IsRunning = true;
                //CompositionTarget.Rendering += Animation_Tick;               
                test.Elapsed += Animation_Tick;
            }           
            else
            {
                Property = StartValue;
            } 
        }
        public void Stop()
        {
            if (IsRunning)
            {
                Property = StartValue;
                IsRunning = false;
                //CompositionTarget.Rendering -= Animation_Tick;
                test.Elapsed -= Animation_Tick;
            }                    
        }
        public void Reset()
        {
            // TEMP:
            Stop();
        }
    }
}
