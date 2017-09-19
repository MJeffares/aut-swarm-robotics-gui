using System.Linq;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
//using SwarmRoboticsGUI;


namespace SwarmRoboticsGUI
{
    public class SwarmManager
    {
        private DispatcherTimer CheckupTimer;
        private DispatcherTimer PositioningTimer;
        private List<RobotItem> RobotList;
        private List<RobotItem> RegisteredRobots;


        public SwarmManager(MainWindow mainWindow)
        {
            
            CheckupTimer = new DispatcherTimer();
            CheckupTimer.Tick += CheckupTimer_Tick;
            CheckupTimer.Interval = new System.TimeSpan(0, 1, 0);
            CheckupTimer.Start();

            PositioningTimer = new DispatcherTimer();
            PositioningTimer.Tick += PositioningTimer_Tick;
            PositioningTimer.Interval = new System.TimeSpan(0, 0, 10);
            PositioningTimer.Start();

           // RegisteredRobots = mainWindow.RobotList.Where(R => R is RobotItem && (R as RobotItem).IsTracked).Cast<RobotItem>().ToList<RobotItem>();
            RobotList = mainWindow.ItemList.Where(R => R is RobotItem).Cast<RobotItem>().ToList<RobotItem>();
            RegisteredRobots = RobotList.Where(R => R.IsTracked).ToList<RobotItem>();
        }


        private void CheckupTimer_Tick(object sender, EventArgs arg)
        {
            RegisteredRobots = RobotList.Where(R => R.IsTracked).ToList<RobotItem>();
        }

        private void PositioningTimer_Tick(object sender, EventArgs arg)
        {
            //RegisteredRobots = RobotList.Where(R => R.IsTracked).ToList<RobotItem>();
        }
    }
}
