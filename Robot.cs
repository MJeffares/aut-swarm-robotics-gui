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
    public class Robot
    {
        public Robot()
        {
            Battery = 0;
            Location = new Point(0, 0);
            DirectionMarker = new Point(0, 0);
            IsTracked = false;
        }

        public int Battery { get; set; }
        // TODO: Create task enum
        // TEMP: object type
        public object Task { get; set; }
        public Point Location { get; set; }
        public Point DirectionMarker { get; set; }
        public double Heading { get; set; }
        public Point[] Contour { get; set; }
        public bool IsTracked { get; set; }
    }
}
