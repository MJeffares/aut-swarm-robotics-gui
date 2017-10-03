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
using XbeeHandler;
using System.Threading.Tasks;
//using SwarmRoboticsGUI;


namespace SwarmRoboticsGUI
{
    public class SwarmManager
    {
        private DispatcherTimer CheckupTimer;
        private DispatcherTimer PositioningTimer;
        private ChargingDockItem dock;
        private List<RobotItem> RobotList;
        private List<RobotItem> RegisteredRobots;
        private XbeeAPI xbee { get; set; }

        public SwarmManager(MainWindow mainWindow)
        {
            xbee = mainWindow.xbee;
            
            CheckupTimer = new DispatcherTimer();
            CheckupTimer.Tick += CheckupTimer_Tick;
            CheckupTimer.Interval = new System.TimeSpan(0, 1, 0);
            CheckupTimer.Start();

            PositioningTimer = new DispatcherTimer();
            PositioningTimer.Tick += PositioningTimer_Tick;
            PositioningTimer.Interval = new System.TimeSpan(0, 0, 1);
            PositioningTimer.Start();

            // RegisteredRobots = mainWindow.RobotList.Where(R => R is RobotItem && (R as RobotItem).IsTracked).Cast<RobotItem>().ToList<RobotItem>();
            RobotList = mainWindow.ItemList.Where(R => R is RobotItem).Cast<RobotItem>().ToList<RobotItem>();
            RegisteredRobots = RobotList.Where(R => (R as IObstacle).IsTracked).ToList<RobotItem>();

            dock = (ChargingDockItem)mainWindow.ItemList.First(D => D is ChargingDockItem);
            //MANSEL: Test this line
            //RegisteredRobots = mainWindow.ItemList.Where(R => (R is RobotItem) && ((R as IObstacle).IsTracked)).Cast<RobotItem>().ToList<RobotItem>();
        }


        private void CheckupTimer_Tick(object sender, EventArgs arg)
        {
            

            

        }

        private void PositioningTimer_Tick(object sender, EventArgs arg)
        {
            byte[] data;
			byte[] datatorobot;
            RegisteredRobots = RobotList.Where(R => (R as IObstacle).IsTracked).ToList<RobotItem>();

            foreach (RobotItem R in RegisteredRobots)
            {
                ICommunicates comms = R as ICommunicates;
                IObstacle obstacle = R as IObstacle;

                UInt16 positionX = (UInt16)obstacle.Location.X;
                UInt16 positionY = (UInt16)obstacle.Location.Y;
                UInt16 facing = (UInt16)R.FacingDeg;

                data = new byte[7];
                data[0] = 0xA0;
                data[1] = (byte)(positionX >> 0x8);
                data[2] = (byte)(positionX);
                data[3] = (byte)(positionY >> 0x8);
                data[4] = (byte)(positionY);
                data[5] = (byte)(facing>> 0x8);
                data[6] = (byte)(facing);

                xbee.SendTransmitRequest(comms.Address64, data);

				/*
				datatorobot = new byte[20];
				datatorobot[0] = ProtocolClass.MESSAGE_TYPES.CHARGING_STATION_ROBOT_STATUS_REPORT;
				datatorobot[1] = 0x00; //read

                UInt64 destination = comms.Address64;

				datatorobot[2] = BitConverter.GetBytes(destination)[7];
				datatorobot[3] = BitConverter.GetBytes(destination)[6];
				datatorobot[4] = BitConverter.GetBytes(destination)[5];
				datatorobot[5] = BitConverter.GetBytes(destination)[4];
				datatorobot[6] = BitConverter.GetBytes(destination)[3];
				datatorobot[7] = BitConverter.GetBytes(destination)[2];
				datatorobot[8] = BitConverter.GetBytes(destination)[1];
				datatorobot[9] = BitConverter.GetBytes(destination)[0];

				datatorobot[10] =  (byte)EnumUtils<TaskType>.FromDescription(R.Task);

				datatorobot[11] = (byte)(R.Battery >> 0x8);
				datatorobot[12] = (byte)(R.Battery);

				datatorobot[13] = (byte)(positionX >> 0x8);
				datatorobot[14] = (byte)(positionX);
				datatorobot[15] = (byte)(positionY >> 0x8);
				datatorobot[16] = (byte)(positionY);
				datatorobot[17] = (byte)(facing >> 0x8);
				datatorobot[18] = (byte)(facing);


                xbee.SendTransmitRequest(((ICommunicates)dock).Address64, datatorobot);
				*/
            }            
        }
    }
}
