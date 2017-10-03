/**********************************************************************************************************************************************
*	File: SwarmRoboticsHandler.cs
*
*	Developed By: Mansel Jeffares
*	First Build: 31 July 2017
*	Current Build: 12 September 2017
*
*	Description :
*		Classes and methods to manage our full communication stack
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


/**********************************************************************************************************************************************
* Namespaces
**********************************************************************************************************************************************/
#region

using SwarmRoboticsCommunicationProtocolHandler.SwarmRoboticsCommunicationProtocolMessages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading;
using System.Threading.Tasks;
using XbeeHandler;
using XbeeHandler.XbeeFrames;
using System.Collections.ObjectModel;

#endregion



/**********************************************************************************************************************************************
* Structures and Classes
**********************************************************************************************************************************************/

namespace SwarmRoboticsGUI
{
	public class CommunicationManager
	{

        private SerialUARTCommunication primarySerialPort { get; set; }
        private XbeeAPI xbeeHandler { get; set; }
        public MainWindow window { get; set; }
        private ProtocolClass swarmRoboticsProtocolHandler { get; set; }
        public UInt64 currentTargetRobot { get; set; }

        public List<byte[]> rxMessageBuffer { get; set; }
        public ObservableCollection<XbeeAPIFrame> rxXbeeMessageBuffer { get; set; }

        public CommunicationManager(MainWindow main, SerialUARTCommunication mainSerialPort, XbeeAPI xbeeManager, ProtocolClass swarmManager)
        {
            window = main;
            primarySerialPort = mainSerialPort;
            xbeeHandler = xbeeManager;
            swarmRoboticsProtocolHandler = swarmManager;
            rxMessageBuffer = new List<byte[]>();
            rxXbeeMessageBuffer = new ObservableCollection<XbeeAPIFrame>();
            WaitForMessage.MessagesToWaitFor = new List<WaitForMessage>();

            primarySerialPort.DataReceived += new SerialUARTCommunication.SerialDataReceivedHandler(PrimarySerialPortDataReceived);

            //window.UpdateListViewBinding(rxXbeeMessageBuffer);
        }





		public class RequestedMessageReceivedArgs : EventArgs
		{
			public XbeeAPIFrame msg;

			public RequestedMessageReceivedArgs(XbeeAPIFrame m)
			{
				msg = m;
			}
		}		

		public class WaitForMessage
		{
			public static List<WaitForMessage> MessagesToWaitFor;

			public byte messageID;


			private delegate int RequestFinishedHandler(object sender, RequestedMessageReceivedArgs e);
			private event RequestFinishedHandler completedEvent;
			private Thread timeoutWorkerThread;
			private bool ran;

			public WaitForMessage(byte id, int timeout, Func<object,  RequestedMessageReceivedArgs, int> externalCompletionHandler)
			{
				messageID = id;
				completedEvent += new RequestFinishedHandler(externalCompletionHandler);
				
				MessagesToWaitFor.Add(this);

				if (timeout >= 0)
				{
					timeoutWorkerThread = new Thread(() =>
					{
						Thread.CurrentThread.Name = "Timeout Thread";
						Thread.CurrentThread.IsBackground = true;
						Thread.Sleep(timeout);
						RequestedMessageReceivedArgs args = new RequestedMessageReceivedArgs(null);
						OnCompletion(this, args);
					});

					timeoutWorkerThread.Start();
				}
			
			}

			private delegate void OnCompletionCallback(object sender, RequestedMessageReceivedArgs e);

			public void OnCompletion(object sender, RequestedMessageReceivedArgs e)
			{
				if (ran != true)
				{
					ran = true;
                    MessagesToWaitFor.Remove(this);
					completedEvent.Invoke(new OnCompletionCallback(CompletionHandler), e);
                    
				}
                else
                {
                    //THIS IS AN ERROR
                    MessagesToWaitFor.Remove(this);
                }
			}

			public virtual void CompletionHandler(object sender, RequestedMessageReceivedArgs e)
			{
                if (completedEvent != null)
                {
                    completedEvent.Invoke(this, e);
                }
				timeoutWorkerThread.Abort();
			}
		}



		/*	public List<communicated_message> communicatedMessages;
		private communicated_message newestMessage;

		public communicated_message NewestMessage
		{
			get { return newestMessage; }
			set { newestMessage = value; }
		}
		*/
		
		

		private void PrimarySerialPortDataReceived(object sender, EventArgs e)
		{
			byte[] frame = xbeeHandler.FindFrames(primarySerialPort.rxByteBuffer);

            if (frame != null)
            {
                rxMessageBuffer.Add(frame);

                var T = Task<XbeeAPIFrame>.Factory.StartNew(() => ReadFrame(frame));


				var message = T.Result;
				if (message != null)
				{
					Application.Current.Dispatcher.Invoke(() => rxXbeeMessageBuffer.Add(message));
				}

            }
		}

        private XbeeAPIFrame ReadFrame(byte[] frame)
        {
            byte[] data = xbeeHandler.EscapeReceivedByteArray(frame);

            if (xbeeHandler.ValidateChecksum(data) == 1)
            {
                //MANSEL: index out of range error
                primarySerialPort.rxByteBuffer.RemoveAt(0);
                return null;
            }
            XbeeAPIFrame message = xbeeHandler.ParseXbeeFrame(data);

            if (message is ZigbeeReceivePacket)
            {
                message = ProtocolClass.ParseSwarmProtocolMessage(message);
                swarmRoboticsProtocolHandler.InterperateSwarmRoboticsMessage(message as SwarmProtocolMessage);
            }
            //else
            //{
            //    InterperateXbeeFrame(message);
            //}
            return message;
        }
	}
    


	public class ProtocolClass
	{
		// variables
		private MainWindow window { get; set; }
        private List<RobotItem> RobotList;

		public static class MESSAGE_TYPES
		{
            //Debugging Messages 0x00 -> 0x0F
            public const byte DEBUG_STRING = 0x00;

            //Status Messages 0xA0 -> 0xAF
            public const byte ROBOT_POSITION = 0xA0;
            public const byte ROBOT_STATUS = 0xA1;

            //Robot Manual Control Messages 0xD0 -> 0xDF
            public const byte ROBOT_CONTROL_STOP = 0xD0;
            public const byte ROBOT_CONTROL_MOVE = 0xD1;
            public const byte ROBOT_CONTROL_ROTATE_CLOCKWISE = 0xD2;
            public const byte ROBOT_CONTROL_ROTATE_COUNTERCLOCKWISE = 0xD3;
            public const byte ROBOT_CONTROL_MOVE_RANDOMLY = 0xD4;
            public const byte ROBOT_CONTROL_RELEASE_DOCK = 0xD6;
            public const byte ROBOT_CONTROL_DOCK = 0xD7;
            public const byte ROBOT_CONTROL_STOP_OBSTACLE_AVOIDANCE = 0xD8;
            public const byte ROBOT_CONTROL_START_OBSTACLE_AVOIDANCE = 0xD9;
            public const byte ROBOT_CONTROL_FOLLOW_LIGHT = 0xDA;
            public const byte ROBOT_CONTROL_FOLLOW_LINE = 0xDB;
            public const byte ROBOT_CONTROL_ROTATE_TO_HEADING = 0xDC;
            public const byte ROBOT_CONTROL_MOVE_TO_POSITION = 0xDD;

            //Robot Systems Test Messages 0xE0 -> 0xEF
			public const byte SYSTEM_TEST_COMMUNICATION = 0xE1;
			public const byte SYSTEM_TEST_PROXIMITY_SENSORS = 0xE4;
			public const byte SYSTEM_TEST_LIGHT_SENSORS = 0xE5;
			public const byte SYSTEM_TEST_MOTORS = 0xE6;
			public const byte SYSTEM_TEST_MOUSE = 0xE7;
			public const byte SYSTEM_TEST_IMU = 0xE8;
			public const byte SYSTEM_TEST_LINE_FOLLOWERS = 0xE9;
			public const byte SYSTEM_TEST_FAST_CHARGE_CHIP = 0xEA;
			public const byte SYSTEM_TEST_TWI_MUX = 0xEB;
			public const byte SYSTEM_TEST_TWI_EXTERNAL = 0xEC;
			public const byte SYSTEM_TEST_CAMERA = 0xED;

            public const byte CHARGING_STATION_LIGHT_SENSORS = 0xF0;
            public const byte CHARGING_STATION_LEDS = 0xF1;
            public const byte CHARGING_STATION_DOCK_ENABLE = 0xF2;
            public const byte CHARGING_STATION_ROBOT_STATUS_REPORT = 0xF3;
            
		}

		// constructor
		public ProtocolClass(MainWindow main)
		{
			window = main;
            RobotList = window.ItemList.Where(R => R is RobotItem).Cast<RobotItem>().ToList<RobotItem>();
		}

		public TaskCompletionSource<SwarmProtocolMessage> MessageReceivedTask = new TaskCompletionSource<SwarmProtocolMessage>();
		
		public void InterperateSwarmRoboticsMessage(SwarmProtocolMessage message)
		{
			//string[] tokens;

			if(CommunicationManager.WaitForMessage.MessagesToWaitFor.Any())
			{
				var matches = CommunicationManager.WaitForMessage.MessagesToWaitFor.Where(p => p.messageID == message.messageID);

				if (matches.Any())
				{
					CommunicationManager.WaitForMessage messageWaitedOn = matches.First();
					CommunicationManager.RequestedMessageReceivedArgs args = new CommunicationManager.RequestedMessageReceivedArgs(message);
                    CommunicationManager.WaitForMessage.MessagesToWaitFor.Remove(matches as CommunicationManager.WaitForMessage);
					messageWaitedOn.OnCompletion(this, args);
				}
			}
            

            if(message.messageID >= 0x00 && message.messageID <= 0x0F)
            {
                switch (message.messageID)
                {
                    case MESSAGE_TYPES.DEBUG_STRING:
                        //MANSEL: REMOVE THIS
                        break;
                }
            }
            else if(message.messageID >= 0xA0 && message.messageID <= 0xAF)
            {
                switch(message.messageID)
                {

                    case MESSAGE_TYPES.ROBOT_STATUS:
                        var robot = RobotList.Find(R => (R as ICommunicates).Address64 == message.sourceAddress64);
                        //robot.Task = (TaskType)((message as RobotStatus).task);
                        robot.Task = EnumUtils<TaskType>.GetDescription((TaskType)((message as RobotStatus).task));
                        robot.Battery = (message as RobotStatus).batteryVoltage;
                        break;
                }
            }
            else if(message.messageID > 0xDF && message.messageID < 0xF0)
            {
                switch(message.messageID)
                {
                    case MESSAGE_TYPES.SYSTEM_TEST_PROXIMITY_SENSORS:
                        DisplayProximityData(message as ProximitySensorTestData);
                        break;
                    
                    case MESSAGE_TYPES.SYSTEM_TEST_LIGHT_SENSORS:
                        DisplayLightSensorData(message as LightSensorTestData);
                        break;

                    case MESSAGE_TYPES.SYSTEM_TEST_LINE_FOLLOWERS:
                        DisplayLineSensorData(message as LineSensorTestData);
                        break;

                    case MESSAGE_TYPES.SYSTEM_TEST_MOUSE:
                        DisplayMouseSensorData(message as MouseSensorTestData);
                        break;

                    case MESSAGE_TYPES.SYSTEM_TEST_IMU:
                        DisplayIMUSensorData(message as IMUSensorTestData);
                        break;


                    case MESSAGE_TYPES.SYSTEM_TEST_TWI_MUX:
                        DisplayTWIMuxTestData(message as TWIMuxTestData);
                        break;

                    case MESSAGE_TYPES.SYSTEM_TEST_FAST_CHARGE_CHIP:

                        break;
                    case MESSAGE_TYPES.SYSTEM_TEST_TWI_EXTERNAL:
                           
                        break;

                    case MESSAGE_TYPES.SYSTEM_TEST_CAMERA:
                        
                        break;
                    
                }
            }
            else if(message.messageID > 0xEF && message.messageID <= 0xFF)
            {
                switch(message.messageID)
                {
                    case MESSAGE_TYPES.CHARGING_STATION_LIGHT_SENSORS:
                        DisplayTowerLightData(message as TowerDockingLightSensorData);
                        break;

                    case MESSAGE_TYPES.CHARGING_STATION_LEDS:
						ChargingDockItem dock = (ChargingDockItem)window.ItemList.First(D => D is ChargingDockItem);
						dock.DockingLights = message.messageData[2];
						break;

                    case MESSAGE_TYPES.CHARGING_STATION_DOCK_ENABLE:

                        break;

                    case MESSAGE_TYPES.CHARGING_STATION_ROBOT_STATUS_REPORT:

						byte[] datatodock;
						UInt64 destination = (message as TowerRobotReport).robotrequested;
						RobotItem robot = (RobotItem)window.ItemList.Find(R => (R is RobotItem) && ((R as ICommunicates).Address64 == message.sourceAddress64));
						ChargingDockItem chargingstation = (ChargingDockItem)window.ItemList.First(D => D is ChargingDockItem);


						datatodock = new byte[20];
						datatodock[0] = ProtocolClass.MESSAGE_TYPES.CHARGING_STATION_ROBOT_STATUS_REPORT;
						datatodock[1] = 0x00; //read			

						datatodock[2] = BitConverter.GetBytes(destination)[7];
						datatodock[3] = BitConverter.GetBytes(destination)[6];
						datatodock[4] = BitConverter.GetBytes(destination)[5];
						datatodock[5] = BitConverter.GetBytes(destination)[4];
						datatodock[6] = BitConverter.GetBytes(destination)[3];
						datatodock[7] = BitConverter.GetBytes(destination)[2];
						datatodock[8] = BitConverter.GetBytes(destination)[1];
						datatodock[9] = BitConverter.GetBytes(destination)[0];

						datatodock[10] = (byte)EnumUtils<TaskType>.FromDescription(robot.Task);

						datatodock[11] = (byte)(robot.Battery >> 0x8);
						datatodock[12] = (byte)(robot.Battery);

						datatodock[13] = (byte)((int)(robot as IObstacle).Location.X >> 0x8);
						datatodock[14] = (byte)((int)(robot as IObstacle).Location.X);
						datatodock[15] = (byte)((int)(robot as IObstacle).Location.Y >> 0x8);
						datatodock[16] = (byte)((int)(robot as IObstacle).Location.Y);
						datatodock[17] = (byte)((int)robot.Facing >> 0x8);
						datatodock[18] = (byte)((int)robot.Facing);


						window.xbee.SendTransmitRequest(((ICommunicates)chargingstation).Address64, datatodock);
						break;

                }
            }

            /*
				switch(message.messageID)
				{
				

					case MESSAGE_TYPES.SYSTEM_TEST_MOTORS:
						string motorData = GetMessageData(messageType, messageData, false);
						tokens = motorData.Split(',');

						switch (tokens[0])
						{
							case "1":
								window.UpdateTextBox(window.tbSysTestMotor1, tokens[1]);
								break;

							case "2":
								window.UpdateTextBox(window.tbSysTestMotor2, tokens[1]);
								break;


							case "3":
								window.UpdateTextBox(window.tbSysTestMotor3, tokens[1]);
								break;
						}

						//return "System Test Motor";
						break;
				}
            */

		}


        public void DisplayTowerLightData(TowerDockingLightSensorData message)
        {
            switch (message.sensor)
            {
                case TowerDockingLightSensorData.Sensors.A:
                    window.UpdateTextBox(window.tbDockLightA, message.MessageDataDisplay);
                    break;

                case TowerDockingLightSensorData.Sensors.B:
                    window.UpdateTextBox(window.tbDockLightB, message.MessageDataDisplay);
                    break;

                case TowerDockingLightSensorData.Sensors.C:
                    window.UpdateTextBox(window.tbDockLightC, message.MessageDataDisplay);
                    break;

                case TowerDockingLightSensorData.Sensors.D:
                    window.UpdateTextBox(window.tbDockLightD, message.MessageDataDisplay);
                    break;

                case TowerDockingLightSensorData.Sensors.E:
                    window.UpdateTextBox(window.tbDockLightE, message.MessageDataDisplay);
                    break;

                case TowerDockingLightSensorData.Sensors.F:
                    window.UpdateTextBox(window.tbDockLightF, message.MessageDataDisplay);
                    break;
            }
        }




		public void DisplayProximityData(ProximitySensorTestData message)
		{
			switch(message.sensor)
			{
				case ProximitySensorTestData.Sensors.proximityA:
					window.UpdateTextBox(window.tbSysTestProximityA, message.MessageDataDisplay);
					break;

				case ProximitySensorTestData.Sensors.proximityB:
					window.UpdateTextBox(window.tbSysTestProximityB, message.MessageDataDisplay);
					break;

				case ProximitySensorTestData.Sensors.proximityC:
					window.UpdateTextBox(window.tbSysTestProximityC, message.MessageDataDisplay);
					break;

				case ProximitySensorTestData.Sensors.proximityD:
					window.UpdateTextBox(window.tbSysTestProximityD, message.MessageDataDisplay);
					break;

				case ProximitySensorTestData.Sensors.proximityE:
					window.UpdateTextBox(window.tbSysTestProximityE, message.MessageDataDisplay);
					break;

				case ProximitySensorTestData.Sensors.proximityF:
					window.UpdateTextBox(window.tbSysTestProximityF, message.MessageDataDisplay);
					break;
			}
		}

        public void DisplayLightSensorData(LightSensorTestData message)
        {
            switch(message.sensor)
            {	
                case LightSensorTestData.Sensors.leftHandSide:
                    window.UpdateTextBox(window.tbSysTestLightSensorLHS, message.MessageDataDisplay);
                    break;

                case LightSensorTestData.Sensors.rightHandSide:
                    window.UpdateTextBox(window.tbSysTestLightSensorRHS, message.MessageDataDisplay);
                    break;
            }
        }

        public void DisplayMouseSensorData(MouseSensorTestData message)
        {
            window.UpdateTextBox(window.tbSysTestMouseDX, message.dXDisplay);
            window.UpdateTextBox(window.tbSysTestMouseDY, message.dYDisplay);
        }

        public void DisplayIMUSensorData(IMUSensorTestData message)
        {
           window.UpdateTextBox(window.tbSysTestIMUPitch, message.pitchDisplay);
           window.UpdateTextBox(window.tbSysTestIMURoll, message.rollDisplay);
           window.UpdateTextBox(window.tbSysTestIMUYaw, message.yawDisplay);
        }
	
        public void DisplayLineSensorData(LineSensorTestData message)
        {
            switch(message.sensor)
            {
                case LineSensorTestData.Sensors.farLeft:
                    window.UpdateTextBox(window.tbSysTestLineFollowerFarLeft, message.MessageDataDisplay);
                    break;

                case LineSensorTestData.Sensors.centreLeft:
                    window.UpdateTextBox(window.tbSysTestLineFollowerCentreLeft, message.MessageDataDisplay);
                    break;

                case LineSensorTestData.Sensors.centreRight:
                    window.UpdateTextBox(window.tbSysTestLineFollowerCentreRight, message.MessageDataDisplay);
                    break;

                case LineSensorTestData.Sensors.farRight:
                    window.UpdateTextBox(window.tbSysTestLineFollowerFarRight, message.MessageDataDisplay);
                    break;
            }
        }

        public void DisplayTWIMuxTestData(TWIMuxTestData message)
        {

			window.UpdateTextBox(window.tbSysTestTWIRead, window.twiMuxAddresses.FirstOrDefault(x => x.Value == message.address).Key);
        }







		public static SwarmProtocolMessage ParseSwarmProtocolMessage(XbeeAPIFrame receivedPacket)
		{
			SwarmProtocolMessage swarmMessage = new SwarmProtocolMessage(receivedPacket.RawMessage);

			switch(swarmMessage.messageID)
			{
                case MESSAGE_TYPES.DEBUG_STRING:
                    swarmMessage = new DebugString(swarmMessage.RawMessage);
                    break;

                case MESSAGE_TYPES.ROBOT_STATUS:
                    swarmMessage = new RobotStatus(swarmMessage.RawMessage);
                    break;

				case MESSAGE_TYPES.SYSTEM_TEST_PROXIMITY_SENSORS:
					swarmMessage = new ProximitySensorTestData(swarmMessage.RawMessage);
					break;

				case MESSAGE_TYPES.SYSTEM_TEST_LIGHT_SENSORS:
                    swarmMessage = new LightSensorTestData(swarmMessage.RawMessage);
					break;

				case MESSAGE_TYPES.SYSTEM_TEST_LINE_FOLLOWERS:
                    swarmMessage = new LineSensorTestData(swarmMessage.RawMessage);
					break;

                case MESSAGE_TYPES.SYSTEM_TEST_MOUSE:
                    swarmMessage = new MouseSensorTestData(swarmMessage.RawMessage);
                    break;

                case MESSAGE_TYPES.SYSTEM_TEST_IMU:
                    swarmMessage = new IMUSensorTestData(swarmMessage.RawMessage);
                    break;


                case MESSAGE_TYPES.SYSTEM_TEST_TWI_MUX:
                    swarmMessage = new TWIMuxTestData(swarmMessage.RawMessage);
                    break;

                    //MANSEL: Common issue with new receive

                case MESSAGE_TYPES.CHARGING_STATION_LIGHT_SENSORS:
                    swarmMessage = new TowerDockingLightSensorData(swarmMessage.RawMessage);
                    break;

				default:

					break;
			}
			return swarmMessage;
		}

        

		/*
		 * //MANSEL: remove this add to classes
		public static string GetMessageType(byte messageType)
		{
			switch (messageType)
			{
				case MESSAGE_TYPES.COMMUNICATION_TEST:
					return "Communication Test";

				case MESSAGE_TYPES.BATTERY_VOLTAGE:
					return "Battery Voltage";

				case MESSAGE_TYPES.SYSTEM_TEST_COMMUNICATION:
					return "System Test Communication";

				case MESSAGE_TYPES.SYSTEM_TEST_PROXIMITY_SENSORS:
					return "System Test Proxmity Sensors";

				case MESSAGE_TYPES.SYSTEM_TEST_LIGHT_SENSORS:
					return "System Test Light Sensors";

				case MESSAGE_TYPES.SYSTEM_TEST_MOTORS:
					return "System Test Motor";

				case MESSAGE_TYPES.SYSTEM_TEST_MOUSE:
					return "System Test Mouse";

				case MESSAGE_TYPES.SYSTEM_TEST_IMU:
					return "System Test IMU";

				case MESSAGE_TYPES.SYSTEM_TEST_LINE_FOLLOWERS:
					return "System Test Line Followers";

				case MESSAGE_TYPES.SYSTEM_TEST_FAST_CHARGE_CHIP:
					return "System Test Fast Charge Chip";

				case MESSAGE_TYPES.SYSTEM_TEST_TWI_MUX:
					return "System Test TWI Mux";

				case MESSAGE_TYPES.SYSTEM_TEST_TWI_EXTERNAL:
					return "System Test TWI External";

				case MESSAGE_TYPES.SYSTEM_TEST_CAMERA:
					return "System Test Camera";


				default:
					return "WARNING: Message Type Unhandled";
			}
		}
		*/



		/// <summary>
		/// Calculates and returns a string representation of message data
		/// </summary>
		/// <param name="messageType">The type of message to get the data of</param>
		/// <param name="messageData">A byte array with the message data</param>
		/// <param name="words">If true will preface with additional information (e.g. what sensor the data came from)</param>
		/// <returns>A string</returns>
		public static string GetMessageData(byte messageType, byte[] messageData, bool words)
		{
			string returnMessage = null;
			switch (messageType)
			{
				/*
				case MESSAGE_TYPES.BATTERY_VOLTAGE:
					float voltage = (float)(messageData[0] * 256 + messageData[1]) * 5 / 1000;
					return voltage.ToString() + " Volts";
					*/

				case MESSAGE_TYPES.SYSTEM_TEST_PROXIMITY_SENSORS:
					float distance = (float)(messageData[1] * 256 + messageData[2]);

					if (words == true)
					{
						switch (messageType)
						{
							case 0xFA:
								returnMessage = "Sensor A: ";
								break;

							case 0xFF:
								returnMessage = "Sensor B: ";
								break;

							case 0xFE:
								returnMessage = "Sensor C: ";
								break;

							case 0xFD:
								returnMessage = "Sensor D: ";
								break;

							case 0xFC:
								returnMessage = "Sensor E: ";
								break;

							case 0xFB:
								returnMessage = "Sensor F: ";
								break;
						}
					}
					else
					{
						switch (messageType)
						{
							case 0xFA:
								returnMessage = "A,";
								break;

							case 0xFF:
								returnMessage = "B,";
								break;

							case 0xFE:
								returnMessage = "C,";
								break;

							case 0xFD:
								returnMessage = "D,";
								break;

							case 0xFC:
								returnMessage = "E, ";
								break;

							case 0xFB:
								returnMessage = "F,";
								break;
						}
					}


					returnMessage += distance.ToString();
					return returnMessage;

				case MESSAGE_TYPES.SYSTEM_TEST_LIGHT_SENSORS:
					float light = (float)(messageData[2] * 256 + messageData[3]);

					if (words == true)
					{
						switch (messageType)
						{
							case 0xF8:
								returnMessage = "Right Hand Side Sensor";
								break;

							case 0xF9:
								returnMessage = "Left Hand Side Sensor";
								break;
						}
					}
					else
					{
						switch (messageType)
						{
							case 0xF8:
								returnMessage = "R,";
								break;

							case 0xF9:
								returnMessage = "L,";
								break;
						}
					}

					returnMessage += light.ToString();
					return returnMessage;

				case MESSAGE_TYPES.SYSTEM_TEST_MOTORS:
					int dir;    //1 for forward, 0 for reverse
					int speed;
					int motor = messageData[1];

					speed = messageData[2] + messageData[3] + messageData[4];

					if (messageData[3] > 128)
					{
						dir = 1;
					}
					else
					{
						dir = 0;
					}

					speed = messageData[3] & (~(1 << 7));

					if (words)
					{
						switch (messageData[1])
						{
							case 1:
								returnMessage = "Motor 1 ";
								break;

							case 2:
								returnMessage = "Motor 2 ";
								break;

							case 3:
								returnMessage = "Motor 3 ";
								break;
						}


						if (dir == 1)
						{
							returnMessage += "Fowards " + speed.ToString() + "%";
						}
						else
						{
							returnMessage += "Reverse " + speed.ToString() + "%";
						}
					}
					else
					{
						switch (messageData[1])
						{
							case 1:
								returnMessage = "1,";
								break;

							case 2:
								returnMessage = "2,";
								break;

							case 3:
								returnMessage = "3,";
								break;
						}

						if (dir == 0)
						{
							returnMessage += "-";
						}
						returnMessage += speed.ToString() + "%";
					}

					return returnMessage;

				case MESSAGE_TYPES.SYSTEM_TEST_MOUSE:
					float dx = (float)(messageData[1] * 256 + messageData[2]);
					float dy = (float)(messageData[3] * 256 + messageData[4]);

					if (words == true)
					{
						returnMessage = "dX: " + dx.ToString() + "dY: " + dy.ToString();
					}
					else
					{
						returnMessage = dx.ToString() + "," + dy.ToString();
					}
					return returnMessage;

				case MESSAGE_TYPES.SYSTEM_TEST_IMU:
					float w = (float)(messageData[2]);
					float x = (float)(messageData[3]);
					float y = (float)(messageData[4]);
					float z = (float)(messageData[5]);

					if (words == true)
					{
						returnMessage = "W: " + w.ToString() + "X: " + x.ToString() + "Y: " + y.ToString() + "Z: " + z.ToString();
					}
					else
					{
						returnMessage = w.ToString() + "," + x.ToString() + "," + y.ToString() + "," + z.ToString();
					}
					return returnMessage;

				case MESSAGE_TYPES.SYSTEM_TEST_LINE_FOLLOWERS:
					return "NOT YET COMPLETED";

				case MESSAGE_TYPES.SYSTEM_TEST_FAST_CHARGE_CHIP:
					return "NOT YET COMPLETED";

				case MESSAGE_TYPES.SYSTEM_TEST_TWI_MUX:
					return "NOT YET COMPLETED";

				case MESSAGE_TYPES.SYSTEM_TEST_TWI_EXTERNAL:
					return "NOT YET COMPLETED";

				case MESSAGE_TYPES.SYSTEM_TEST_CAMERA:
					return "NOT YET COMPLETED";

				default:
					return "WARNING: Unknown data";
			}
		}

	}


	//public partial class MainWindow : Window
	//{
	//	public delegate void RefreshListViewCallback();

	//	public void RefreshListView()
	//	{
	//		lvCommunicatedMessages.Dispatcher.Invoke(new RefreshListViewCallback(this.Refresh));

	//	}

	//	private GridViewColumnHeader lvCommunicatedMessagesSortCol = null;
	//	private SortAdorner lvCommunicatedMessagesSortAdorner = null;

	//	private void Refresh()
	//	{
	//		if (lvCommunicatedMessagesSortAdorner != null && lvCommunicatedMessagesSortCol != null)
	//		{
	//			lvCommunicatedMessages.Items.SortDescriptions.Add(new SortDescription(lvCommunicatedMessagesSortCol.Tag.ToString(), lvCommunicatedMessagesSortAdorner.Direction));
	//		}

            
	//		lvCommunicatedMessages.Items.Refresh();
	//	}


	//	//this stuff is temporary XXXX

	//	private void receivedDataRemove_Click(object sender, RoutedEventArgs e)
	//	{
	//		gvCommunicatedMessages.Columns.Remove(gvcTimeStamp);
	//	}

	//	private void receivedDataAdd_Click(object sender, RoutedEventArgs e)
	//	{
	//		gvCommunicatedMessages.Columns.Add(gvcTimeStamp);
	//	}



	//	private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
	//	{
	//		GridViewColumnHeader column = (sender as GridViewColumnHeader);
	//		string sortBy = column.Tag.ToString();

	//		if (lvCommunicatedMessagesSortCol != null)
	//		{
	//			AdornerLayer.GetAdornerLayer(lvCommunicatedMessagesSortCol).Remove(lvCommunicatedMessagesSortAdorner);
	//			lvCommunicatedMessages.Items.SortDescriptions.Clear();
	//		}

	//		ListSortDirection newDir = ListSortDirection.Ascending;
	//		if (lvCommunicatedMessagesSortCol == column && lvCommunicatedMessagesSortAdorner.Direction == newDir)
	//			newDir = ListSortDirection.Descending;

	//		lvCommunicatedMessagesSortCol = column;
	//		lvCommunicatedMessagesSortAdorner = new SortAdorner(lvCommunicatedMessagesSortCol, newDir);
	//		AdornerLayer.GetAdornerLayer(lvCommunicatedMessagesSortCol).Add(lvCommunicatedMessagesSortAdorner);
	//		lvCommunicatedMessages.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
	//		//column.Width = column.ActualWidth + 10;
	//		lvCommunicatedMessages.Items.Refresh();
	//	}

	//	public class SortAdorner : Adorner
	//	{
	//		private static Geometry ascGeometry =
	//				Geometry.Parse("M 0 4 L 3.5 0 L 7 4 Z");

	//		private static Geometry descGeometry =
	//				Geometry.Parse("M 0 0 L 3.5 4 L 7 0 Z");

	//		public ListSortDirection Direction { get; private set; }

	//		public SortAdorner(UIElement element, ListSortDirection dir)
	//				: base(element)
	//		{
	//			this.Direction = dir;
	//		}

	//		protected override void OnRender(DrawingContext drawingContext)
	//		{
	//			base.OnRender(drawingContext);

	//			if (AdornedElement.RenderSize.Width < 20)
	//				return;

	//			TranslateTransform transform = new TranslateTransform
	//					(
	//							AdornedElement.RenderSize.Width - 15,
	//							(AdornedElement.RenderSize.Height - 5) / 2
	//					);
	//			drawingContext.PushTransform(transform);

	//			Geometry geometry = ascGeometry;
	//			if (this.Direction == ListSortDirection.Descending)
	//				geometry = descGeometry;
	//			drawingContext.DrawGeometry(Brushes.Black, null, geometry);

	//			drawingContext.Pop();
	//		}
	//	}






	//	public delegate void UpdateListViewBindingCallback(List<XbeeAPIFrame> messages);

	//	public void UpdateListViewBinding(List<XbeeAPIFrame> messages)
	//	{
	//		lvCommunicatedMessages.Dispatcher.Invoke(new UpdateListViewBindingCallback(this.UpdateBinding), new object[] { messages });
	//	}

	//	private void UpdateBinding(List<XbeeAPIFrame> messages)
	//	{
	//		DataContext = this;
	//		lvCommunicatedMessages.ItemsSource = messages;
	//		lvCommunicatedMessages.Items.Refresh();
	//	}



	//	private void Button_Click(object sender, RoutedEventArgs e)
	//	{
	//		if (serial._serialPort.IsOpen)
	//		{
	//			rtbSendBuffer.SelectAll();
	//			string text = rtbSendBuffer.Selection.Text.ToString();
	//			string textToSend = text;

	//			textToSend = textToSend.Replace("\r", string.Empty);
	//			textToSend = textToSend.Replace("\n", string.Empty);
	//			textToSend = textToSend.Replace(" ", string.Empty);
	//			textToSend = textToSend.Replace("-", string.Empty);
	//			textToSend = textToSend.Replace("0x", string.Empty);

	//			text = text.Replace("\n", string.Empty);
	//			text = text.Replace(" ", "-");

	//			try
	//			{
	//				byte[] bytes = bytes = Enumerable.Range(0, textToSend.Length).Where(x => x % 2 == 0).Select(x => Convert.ToByte(textToSend.Substring(x, 2), 16)).ToArray();


	//				//test = xbee.CalculateChecksum(bytes); //			

	//				//bytes = xbee.Escape(bytes); //escapes bytes


	//				serial._serialPort.Write(bytes, 0, bytes.Length);
	//			}
	//			catch (Exception excpt)
	//			{
	//				MessageBox.Show(excpt.Message);
	//			}


	//			rtbSerialSent.AppendText(text);
	//			//rtbSerialSent.AppendText(test.ToString()); //
	//			rtbSendBuffer.Document.Blocks.Clear();
	//			rtbSerialSent.ScrollToEnd();
	//		}
	//		else
	//		{
	//			MessageBox.Show("Port not open");
	//		}
	//	}


	//	public bool messageReceived = false;

	//	private void btnXbeeSend_Click(object sender, RoutedEventArgs e)
	//	{
	//		if (serial._serialPort.IsOpen)
	//		{
	//			//byte test = 0; //

	//			rtbXbeeSendBuffer.SelectAll();
	//			string text = rtbXbeeSendBuffer.Selection.Text.ToString();
	//			string textToSend = text;

	//			textToSend = textToSend.Replace("\r", string.Empty);
	//			textToSend = textToSend.Replace("\n", string.Empty);
	//			textToSend = textToSend.Replace(" ", string.Empty);
	//			textToSend = textToSend.Replace("-", string.Empty);
	//			textToSend = textToSend.Replace("0x", string.Empty);

	//			text = text.Replace("\n", string.Empty);
	//			text = text.Replace(" ", "-");

	//			try
	//			{
	//				byte[] bytes = bytes = Enumerable.Range(0, textToSend.Length).Where(x => x % 2 == 0).Select(x => Convert.ToByte(textToSend.Substring(x, 2), 16)).ToArray();


	//				//test = xbee.CalculateChecksum(bytes); //



	//				//bytes = xbee.Escape(bytes); //escapes bytes

	//				xbee.SendTransmitRequest(XbeeAPI.DESTINATION.COORDINATOR, bytes);

	//				//serial._serialPort.Write(bytes, 0, bytes.Length);
	//			}
	//			catch (Exception excpt)
	//			{
	//				MessageBox.Show(excpt.Message);
	//			}


	//			rtbXbeeSent.AppendText(text);
	//			//rtbSerialSent.AppendText(test.ToString()); 
	//			rtbXbeeSendBuffer.Document.Blocks.Clear();
	//			rtbXbeeSent.ScrollToEnd();
	//		}
	//		else
	//		{
	//			MessageBox.Show("Port not open");
	//		}
	//	}


	//	public delegate void UpdateTextBoxCallback(TextBox control, string text);

	//	public void UpdateTextBox(TextBox control, string text)
	//	{
	//		//control.Text = TextAlignment;
	//		//lvCommunicatedMessages.Dispatcher.Invoke(new RefreshListViewCallback(this.Refresh));
	//		//control.Dispatcher.Invoke(new UpdateTextBoxCallback(this.UpdateText),new object { control, text } );
	//		control.Dispatcher.Invoke(new UpdateTextBoxCallback(this.UpdateText), new object[] { control, text });
	//	}


	//	private void UpdateText(TextBox control, string text)
	//	{
	//		control.Text = text;
	//	}



	//	/*
	//	public delegate void UpdateTextCallback(string text);

	//	public void UpdateSerialReceivedTextBox(string text)
	//	{
	//		rtbSerialReceived.Dispatcher.Invoke(new UpdateTextCallback(this.UpdateText), new object[] { text });
	//	}

	//	public void UpdateSerialReceivedTextBox(byte[] message, int number)
	//	{
	//		string messageString = null;

	//		for (int i = 0; i < number; i++)
	//		{

	//			string temp = message[i].ToString("X");

	//			if (temp == "7E")
	//			{
	//				messageString += "\r";
	//				messageString += temp;
	//			}
	//			else if (message[i] < 0x10)
	//			{
	//				messageString += "0";
	//				messageString += temp;
	//			}
	//			else
	//			{
	//				messageString += temp;
	//			}
	//		}

	//		rtbSerialReceived.Dispatcher.Invoke(new UpdateTextCallback(this.UpdateText), new object[] { messageString });
			
	//	}
		
	//	private void UpdateText(string text)
	//	{
	//		rtbSerialReceived.AppendText(text);
	//		rtbSerialReceived.ScrollToEnd();
	//	}

	//	private void receivedDataNewline_Click(object sender, RoutedEventArgs e)
	//	{
	//		rtbSerialReceived.AppendText("\r");
	//		rtbSerialReceived.ScrollToEnd(); ;
	//	}


	//	private void receivedDataClear_Click(object sender, RoutedEventArgs e)
	//	{
	//		rtbSerialReceived.Document.Blocks.Clear();
	//		rtbSerialReceived.ScrollToEnd(); ;
	//	}
	//	*/
	//}
}


