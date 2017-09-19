/**********************************************************************************************************************************************
*	File: SwarmManager.cs
*
*	Developed By: Mansel Jeffares
*	First Build: 19 October 2017
*	Current Build: 19 October 2017
*
*	Description :
*		Developed for a final year university project at Auckland University of Technology (AUT), New Zealand
*		
*	Useage :
*		
*
*	Limitations :
*		Build for x64, .NET 4.5.2
*   
*		Naming Conventions:
*			Variables, camelCase, start lower case, subsequent words also upper case, if another object goes by the same name, then also with an underscore
*			Methods, PascalCase, start upper case, subsequent words also upper case
*			Constants, all upper case, unscores for seperation
* 
**********************************************************************************************************************************************/


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

        MainWindow mainwindow;

        public SwarmManager(MainWindow mainWindow)
        {
            mainwindow = mainWindow;
            
            CheckupTimer = new DispatcherTimer();
            CheckupTimer.Tick += CheckupTimer_Tick;
            CheckupTimer.Interval = new System.TimeSpan(0, 1, 0);
            CheckupTimer.Start();

            PositioningTimer = new DispatcherTimer();
            PositioningTimer.Tick += PositioningTimer_Tick;
            PositioningTimer.Interval = new System.TimeSpan(0, 0, 5);
            PositioningTimer.Start();

           // RegisteredRobots = mainWindow.RobotList.Where(R => R is RobotItem && (R as RobotItem).IsTracked).Cast<RobotItem>().ToList<RobotItem>();
            RobotList = mainWindow.ItemList.Where(R => R is RobotItem).Cast<RobotItem>().ToList<RobotItem>();
            RegisteredRobots = RobotList.Where(R => R.IsTracked).ToList<RobotItem>();
        }


        private void CheckupTimer_Tick(object sender, EventArgs arg)
        {
            

            

        }

        private void PositioningTimer_Tick(object sender, EventArgs arg)
        {
            RegisteredRobots = RobotList.Where(R => R.IsTracked).ToList<RobotItem>();

            foreach (RobotItem R in RegisteredRobots)
            {
                byte[] data;
                UInt16 num;
                data = new byte[7];

                data[0] = 0xA0;

                num = (UInt16)R.Location.X;

                data[1] = (byte)(num >> 0x8);
                data[2] = (byte)(num);

                num = (UInt16)R.Location.Y;

                data[3] = (byte)(num >> 0x8);
                data[4] = (byte)(num);


                num = (UInt16)R.FacingDeg;

                data[5] = (byte)(num >> 0x8);
                data[6] = (byte)(num);

                mainwindow.xbee.SendTransmitRequest(R.Address64, data);
            }
        }
    }
}
