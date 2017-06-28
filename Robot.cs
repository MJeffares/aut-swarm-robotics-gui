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
        // TODO: Create task enum
        // TEMP: object type
        public object Task { get; set; }
        public Point Location { get; set; }
        public VectorOfPoint RobotContour1 { get; set; }
        public Point[] RobotContour { get; set; }
        public bool IsTracked { get; set; }
    }
}
