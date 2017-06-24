using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Emgu.CV;

namespace SwarmRoboticsGUI
{
    class Robot
    {
        public int Battery { get; set; }
        public bool hasImage { get; set; }
        // TODO: Create task enum
        // TEMP: object type
        public object Task { get; set; }
        public Point Location { get; set; }
        public Mat RobotImage { get; set; }
        public bool Tracked { get; set; }
    }
}
