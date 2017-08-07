


/**********************************************************************************************************************************************
* Namespaces
**********************************************************************************************************************************************/
#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading;
using System.Threading.Tasks;

#endregion



/**********************************************************************************************************************************************
* Structures and Classes
**********************************************************************************************************************************************/

namespace SwarmRoboticsGUI
{
	public class CommunicationManager
	{

		

		public class RequestedMessageReceivedArgs : EventArgs
		{
			public XbeeAPI.XbeeAPIFrame msg;

			public RequestedMessageReceivedArgs(XbeeAPI.XbeeAPIFrame m)
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
		
		private SerialUARTCommunication primarySerialPort;
		private XbeeAPI xbeeHandler;
		public MainWindow window;
		private ProtocolClass swarmRoboticsProtocolHandler;

		private List<byte[]> rxMessageBuffer;
		private List<XbeeAPI.XbeeAPIFrame> rxXbeeMessageBuffer;

		public CommunicationManager(MainWindow main, SerialUARTCommunication mainSerialPort, XbeeAPI xbeeManager, ProtocolClass swarmManager)
		{
			window = main;
			primarySerialPort = mainSerialPort;
			xbeeHandler = xbeeManager;
			swarmRoboticsProtocolHandler = swarmManager;
			rxMessageBuffer = new List<byte[]>();
			rxXbeeMessageBuffer = new List<XbeeAPI.XbeeAPIFrame>();
			//templist = new List<WaitForMessage.MyTempClass>();
			WaitForMessage.MessagesToWaitFor = new List<WaitForMessage>();

			primarySerialPort.DataReceived += new SerialUARTCommunication.SerialDataReceivedHandler(PrimarySerialPortDataReceived);

			window.UpdateListViewBinding(rxXbeeMessageBuffer);
		}

		private void PrimarySerialPortDataReceived(object sender, EventArgs e)
		{
			byte[] frame = xbeeHandler.FindFrames(primarySerialPort.rxByteBuffer);

			if (frame != null)
			{
				rxMessageBuffer.Add(frame);


				new Thread(() =>
				{
					Thread.CurrentThread.Name = "Received Frame Working Thread";
					Thread.CurrentThread.IsBackground = true;
					//Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
					MyThread(frame);
				}).Start();

				//t.Start();
			}
		}



		private uint MyThread(byte[] frame)
		{
			byte[] data = data = xbeeHandler.EscapeReceivedByteArray(frame);

			if (xbeeHandler.ValidateChecksum(data) == 1)
			{
				primarySerialPort.rxByteBuffer.RemoveAt(0);
				return 1;
			}

			XbeeAPI.XbeeAPIFrame message = xbeeHandler.ParseXbeeFrame(data);

			if(message is XbeeAPI.ZigbeeReceivePacket)
			{
				message = ProtocolClass.ParseSwarmProtocolMessage(message);
				//InterperateSwarmProtocolMessage(message);
				swarmRoboticsProtocolHandler.InterperateSwarmRoboticsMessage(message as ProtocolClass.SwarmProtocolMessage);
                
			}
			else
			{
				//InterperateXbeeFrame(message);
			}
			rxXbeeMessageBuffer.Add(message);

			window.RefreshListView();
			return 0;
		}
	}








	/*

	public class communicated_message
	{
		public DateTime time_stamp { get; set; }

		public byte[] raw_message { get; set; }

		public int frame_length { get; set; }
		public byte frame_ID { get; set; }
		public byte[] frame_data { get; set; }

		public byte[] source16 { get; set; }
		public byte[] source64 { get; set; }

		public byte message_type { get; set; }
		public byte[] message_data { get; set; }

		public string TimeStampDisplay
		{
			get
			{
				return time_stamp.ToString("HH:mm:ss");
			}
		}

		public string RawMessageDisplay
		{
			get
			{
				return MJLib.HexToString(raw_message, 0, frame_length + 1, true);
			}
		}

		public string FrameLengthDisplay
		{
			get
			{
				return MJLib.HexToString(BitConverter.GetBytes(frame_length), 0, 1, true) + " (" + frame_length.ToString() + ")";
			}
		}

		public string FrameIDDisplay
		{
			get
			{
				return XbeeHandler.GetXbeeFrameType(frame_ID) + " (" + MJLib.HexToString(frame_ID, true) + ")";
			}
		}

		public string FrameDataDisplay
		{
			get
			{
				return MJLib.HexToString(frame_data, 0, frame_length, true);
			}
		}

		public string SourceDisplay
		{
			get
			{
				//return XbeeHandler.DESTINATION.ToString(BitConverter.ToUInt64(source64, 0)) + " (" + MJLib.HexToString(source64, 0, 8, true) + " , " + MJLib.HexToString(source16, 0, 2, true) + ")";
				return "FIX";
			}
		}

		public string MessageTypeDisplay
		{
			get
			{
				return ProtocolClass.GetMessageType(message_type) + " (" + MJLib.HexToString(message_type, true) + ")";
			}
		}

		public string MessageDataDisplay
		{
			get
			{
				return ProtocolClass.GetMessageData(message_type, message_data, true) + " (" + MJLib.HexToString(message_data, 0, frame_length - 13, true) + ")";
			}
		}
	}


	*/

	public class ProtocolClass
	{
		// variables
		private MainWindow window = null;
		public static class MESSAGE_TYPES
		{
			public const byte COMMUNICATION_TEST = 0x00;
			public const byte BATTERY_VOLTAGE = 0x01;

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
		}

		// constructor
		public ProtocolClass(MainWindow main)
		{
			window = main;
		}

		public TaskCompletionSource<SwarmProtocolMessage> MessageReceivedTask = new TaskCompletionSource<SwarmProtocolMessage>();
		
		//Mansel: To remove message received method
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

			if (window.testMode)
			{
				MJLib.TypeSwitch.Do
				(
					message.GetType(),

					MJLib.TypeSwitch.Case<ProximitySensorTestData>(()=> DisplayRoximityData(message as ProximitySensorTestData))

					//MJLib.TypeSwitch.Case<LineSensorTestData>()
				);


				/*
				switch(message.messageID)
				{
					case MESSAGE_TYPES.SYSTEM_TEST_PROXIMITY_SENSORS:
						string proximityData = GetMessageData(messageType, messageData, false);
						tokens = proximityData.Split(',');

						switch (message)
						{
							case "A":
								window.UpdateTextBox(window.tbSysTestProximityA, tokens[1]);
								break;

							case "B":
								window.UpdateTextBox(window.tbSysTestProximityB, tokens[1]);
								break;

							case "C":
								window.UpdateTextBox(window.tbSysTestProximityC, tokens[1]);
								break;

							case "D":
								window.UpdateTextBox(window.tbSysTestProximityD, tokens[1]);
								break;

							case "E":
								window.UpdateTextBox(window.tbSysTestProximityE, tokens[1]);
								break;

							case "F":
								window.UpdateTextBox(window.tbSysTestProximityF, tokens[1]);
								break;
						}
						break;

					case MESSAGE_TYPES.SYSTEM_TEST_LIGHT_SENSORS:
						string lineData = GetMessageData(messageType, messageData, false);
						tokens = lineData.Split(',');

						switch (tokens[0])
						{
							case "R":
								window.UpdateTextBox(window.tbSysTestLightSensorRHS, tokens[1]);

								break;

							case "L":
								window.UpdateTextBox(window.tbSysTestLightSensorLHS, tokens[1]);
								break;
						}
						break;

					case MESSAGE_TYPES.SYSTEM_TEST_LINE_FOLLOWERS:
						//return "System Test Line Followers";
						//XXXX neeed to know what kind of data
						break;

					case MESSAGE_TYPES.SYSTEM_TEST_MOUSE:
						string mouseData = GetMessageData(messageType, messageData, false);
						tokens = mouseData.Split(',');
						window.UpdateTextBox(window.tbSysTestMouseDX, tokens[0]);
						window.UpdateTextBox(window.tbSysTestMouseDY, tokens[1]);
						break;

					case MESSAGE_TYPES.SYSTEM_TEST_IMU:
						string imuData = GetMessageData(messageType, messageData, false);
						tokens = imuData.Split(',');
						window.UpdateTextBox(window.tbSysTestIMUW, tokens[0]);
						window.UpdateTextBox(window.tbSysTestIMUX, tokens[1]);
						window.UpdateTextBox(window.tbSysTestIMUY, tokens[2]);
						window.UpdateTextBox(window.tbSysTestIMUZ, tokens[3]);
						//return "System Test IMU";
						break;

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

					case MESSAGE_TYPES.SYSTEM_TEST_TWI_MUX:
						//return "System Test TWI Mux";
						break;

					case MESSAGE_TYPES.SYSTEM_TEST_FAST_CHARGE_CHIP:
						//return "System Test Fast Charge Chip";
						break;
					case MESSAGE_TYPES.SYSTEM_TEST_TWI_EXTERNAL:
						//return "System Test TWI External";
						break;

					case MESSAGE_TYPES.SYSTEM_TEST_CAMERA:
						//return "System Test Camera";
						break;
				}
			}
			else
			{
				switch (messageType)
				{


					case MESSAGE_TYPES.COMMUNICATION_TEST:

						//window.UpdateSerialReceivedTextBox("\rCommunication Test Successful");
						//XXX
						//display the data here
						break;

					case MESSAGE_TYPES.BATTERY_VOLTAGE:
						float voltage = messageData[1] * 256 + messageData[2];
						voltage = voltage * 5 / 1000;
						//window.UpdateSerialReceivedTextBox("\rBattery Voltage:");
						//window.UpdateSerialReceivedTextBox(voltage.ToString());
						break;

				}
				*/
			}

		}

		public void DisplayRoximityData(ProximitySensorTestData message)
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
	

		public class SwarmProtocolMessage: XbeeAPI.ZigbeeReceivePacket
		{
			public byte messageID;
			public byte[] messageData;

			public SwarmProtocolMessage(byte[] frame) : base(frame)
			{
				messageID = receivedData[0];
				messageData = new byte[receivedData.Length - 1];
				Array.Copy(receivedData, 1, messageData, 0, receivedData.Length - 1);
			}
			//MANSEL: remove here and add to classes
			/*
			public override string MessageTypeDisplay
			{
				get
				{
					return ProtocolClass.GetMessageType(messageID) + " (" + MJLib.HexToString(messageID, true) + ")";
				}
			}
			*/
		}

		public class SystemTestMessage : SwarmProtocolMessage
		{
			protected byte testMode;
			protected byte[] testMessage;

			public SystemTestMessage(byte[] frame) : base(frame)
			{
				testMode = messageData[0];
				testMessage = new byte[messageData.Length - 1];
				Array.Copy(messageData, 1, testMessage, 0, messageData.Length - 1);
			}
		}

		public class CommunicationTest : SystemTestMessage
		{
			public byte communicationTestResult;

			public CommunicationTest (byte[] frame) : base(frame)
			{
				communicationTestResult = testMessage[0];
			}			
		}

		public class ProximitySensorTestData : SystemTestMessage
		{
			public static class Sensors
			{
				public const byte proximityA = 0xFA;
				public const byte proximityB = 0xFF;
				public const byte proximityC = 0xFE;
				public const byte proximityD = 0xFD;
				public const byte proximityE = 0xFC;
				public const byte proximityF = 0xFB;
			}


			public byte sensor;
			public byte[] sensorData = new byte[2];

			public ProximitySensorTestData(byte[] frame) : base(frame)
			{
				sensor = testMessage[0];
				sensorData[0] = testMessage[1];
				sensorData[1] = testMessage[2];
			}

			public override string MessageDataDisplay
			{
				get
				{
					//MANSEL: Improve this
					return (256 * sensorData[0] + sensorData[1]).ToString();
				}
			}

		}

		public class LightSensorTestData : SystemTestMessage
		{
			public byte sensor;
			public byte[] sensorData = new byte[2];

			public LightSensorTestData(byte[] frame) : base(frame)
			{
				sensor = testMessage[0];
				sensorData[0] = testMessage[1];
				sensorData[1] = testMessage[2];
			}
		}

		public class LineSensorTestData : SystemTestMessage
		{
			public byte sensor;
			//MANSEL: need to findout what data comes from line sensors
			//protected byte[] sensorData = new byte[2];

			public LineSensorTestData(byte[] frame) : base(frame)
			{
				sensor = testMessage[0];
				//sensorData[0] = testMessage[1];
				//sensorData[1] = testMessage[2];
			}
		}
		
		public class MouseSensorTestData : SystemTestMessage
		{
			public int dX;
			public int dY;

			public MouseSensorTestData(byte[] frame) : base(frame)
			{
				dX = 256*testMessage[0] + testMessage[1];
				dY = 256 * testMessage[2] + testMessage[3];
			}
		}

		public class IMUSensorTestData : SystemTestMessage
		{
			public byte w;
			public byte x;
			public byte y;
			public byte z;

			public IMUSensorTestData(byte[] frame) : base(frame)
			{
				w = testMessage[0];
				x = testMessage[1];
				y = testMessage[2];
				z = testMessage[3];
			}
		}


		public class MotorTestData : SystemTestMessage
		{
			public byte motor;
			public int motorSpeed;
			public bool motorDir;

			public MotorTestData(byte[] frame) : base(frame)
			{
				motor = testMessage[0];
				//MANSEL: add motor conversion here
				//sensorData[0] = testMessage[1];
				//sensorData[1] = testMessage[2];
			}
		}


		public static SwarmProtocolMessage ParseSwarmProtocolMessage(XbeeAPI.XbeeAPIFrame receivedPacket)
		{
			SwarmProtocolMessage swarmMessage = new SwarmProtocolMessage(receivedPacket.rawMessage);

			switch(swarmMessage.messageID)
			{
				case MESSAGE_TYPES.SYSTEM_TEST_PROXIMITY_SENSORS:
					swarmMessage = new ProximitySensorTestData(swarmMessage.rawMessage);
					break;

				case MESSAGE_TYPES.SYSTEM_TEST_LIGHT_SENSORS:

					break;

				case MESSAGE_TYPES.SYSTEM_TEST_LINE_FOLLOWERS:

					break;

				default:

					break;
			}
			return swarmMessage;
		}



		

		public void SendMessage(byte type)
		{
			switch (type)
			{
				case MESSAGE_TYPES.COMMUNICATION_TEST:

					window.xbee.SendTransmitRequest(XbeeAPI.DESTINATION.BROADCAST, MESSAGE_TYPES.COMMUNICATION_TEST);

					break;

				case MESSAGE_TYPES.BATTERY_VOLTAGE:

					window.xbee.SendTransmitRequest(XbeeAPI.DESTINATION.BROADCAST, MESSAGE_TYPES.BATTERY_VOLTAGE);

					break;

			}
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
				case MESSAGE_TYPES.COMMUNICATION_TEST:
					return "Sucessful";

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


	public partial class MainWindow : Window
	{
		public delegate void RefreshListViewCallback();

		public void RefreshListView()
		{
			lvCommunicatedMessages.Dispatcher.Invoke(new RefreshListViewCallback(this.Refresh));

		}

		private GridViewColumnHeader lvCommunicatedMessagesSortCol = null;
		private SortAdorner lvCommunicatedMessagesSortAdorner = null;

		private void Refresh()
		{
			if (lvCommunicatedMessagesSortAdorner != null && lvCommunicatedMessagesSortCol != null)
			{
				lvCommunicatedMessages.Items.SortDescriptions.Add(new SortDescription(lvCommunicatedMessagesSortCol.Tag.ToString(), lvCommunicatedMessagesSortAdorner.Direction));
			}

			lvCommunicatedMessages.Items.Refresh();
		}


		//this stuff is temporary XXXX

		private void receivedDataRemove_Click(object sender, RoutedEventArgs e)
		{
			gvCommunicatedMessages.Columns.Remove(gvcTimeStamp);
		}

		private void receivedDataAdd_Click(object sender, RoutedEventArgs e)
		{
			gvCommunicatedMessages.Columns.Add(gvcTimeStamp);
		}



		private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
		{
			GridViewColumnHeader column = (sender as GridViewColumnHeader);
			string sortBy = column.Tag.ToString();

			if (lvCommunicatedMessagesSortCol != null)
			{
				AdornerLayer.GetAdornerLayer(lvCommunicatedMessagesSortCol).Remove(lvCommunicatedMessagesSortAdorner);
				lvCommunicatedMessages.Items.SortDescriptions.Clear();
			}

			ListSortDirection newDir = ListSortDirection.Ascending;
			if (lvCommunicatedMessagesSortCol == column && lvCommunicatedMessagesSortAdorner.Direction == newDir)
				newDir = ListSortDirection.Descending;

			lvCommunicatedMessagesSortCol = column;
			lvCommunicatedMessagesSortAdorner = new SortAdorner(lvCommunicatedMessagesSortCol, newDir);
			AdornerLayer.GetAdornerLayer(lvCommunicatedMessagesSortCol).Add(lvCommunicatedMessagesSortAdorner);
			lvCommunicatedMessages.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
			//column.Width = column.ActualWidth + 10;
			lvCommunicatedMessages.Items.Refresh();
		}

		public class SortAdorner : Adorner
		{
			private static Geometry ascGeometry =
					Geometry.Parse("M 0 4 L 3.5 0 L 7 4 Z");

			private static Geometry descGeometry =
					Geometry.Parse("M 0 0 L 3.5 4 L 7 0 Z");

			public ListSortDirection Direction { get; private set; }

			public SortAdorner(UIElement element, ListSortDirection dir)
					: base(element)
			{
				this.Direction = dir;
			}

			protected override void OnRender(DrawingContext drawingContext)
			{
				base.OnRender(drawingContext);

				if (AdornedElement.RenderSize.Width < 20)
					return;

				TranslateTransform transform = new TranslateTransform
						(
								AdornedElement.RenderSize.Width - 15,
								(AdornedElement.RenderSize.Height - 5) / 2
						);
				drawingContext.PushTransform(transform);

				Geometry geometry = ascGeometry;
				if (this.Direction == ListSortDirection.Descending)
					geometry = descGeometry;
				drawingContext.DrawGeometry(Brushes.Black, null, geometry);

				drawingContext.Pop();
			}
		}






		public delegate void UpdateListViewBindingCallback(List<XbeeAPI.XbeeAPIFrame> messages);

		public void UpdateListViewBinding(List<XbeeAPI.XbeeAPIFrame> messages)
		{
			lvCommunicatedMessages.Dispatcher.Invoke(new UpdateListViewBindingCallback(this.UpdateBinding), new object[] { messages });
		}

		private void UpdateBinding(List<XbeeAPI.XbeeAPIFrame> messages)
		{
			DataContext = this;
			lvCommunicatedMessages.ItemsSource = messages;
			lvCommunicatedMessages.Items.Refresh();
		}



		private void Button_Click(object sender, RoutedEventArgs e)
		{
			if (serial._serialPort.IsOpen)
			{
				rtbSendBuffer.SelectAll();
				string text = rtbSendBuffer.Selection.Text.ToString();
				string textToSend = text;

				textToSend = textToSend.Replace("\r", string.Empty);
				textToSend = textToSend.Replace("\n", string.Empty);
				textToSend = textToSend.Replace(" ", string.Empty);
				textToSend = textToSend.Replace("-", string.Empty);
				textToSend = textToSend.Replace("0x", string.Empty);

				text = text.Replace("\n", string.Empty);
				text = text.Replace(" ", "-");

				try
				{
					byte[] bytes = bytes = Enumerable.Range(0, textToSend.Length).Where(x => x % 2 == 0).Select(x => Convert.ToByte(textToSend.Substring(x, 2), 16)).ToArray();


					//test = xbee.CalculateChecksum(bytes); //			

					//bytes = xbee.Escape(bytes); //escapes bytes


					serial._serialPort.Write(bytes, 0, bytes.Length);
				}
				catch (Exception excpt)
				{
					MessageBox.Show(excpt.Message);
				}


				rtbSerialSent.AppendText(text);
				//rtbSerialSent.AppendText(test.ToString()); //
				rtbSendBuffer.Document.Blocks.Clear();
				rtbSerialSent.ScrollToEnd();
			}
			else
			{
				MessageBox.Show("Port not open");
			}
		}


		public bool messageReceived = false;

		private void btnXbeeSend_Click(object sender, RoutedEventArgs e)
		{
			if (serial._serialPort.IsOpen)
			{
				//byte test = 0; //

				rtbXbeeSendBuffer.SelectAll();
				string text = rtbXbeeSendBuffer.Selection.Text.ToString();
				string textToSend = text;

				textToSend = textToSend.Replace("\r", string.Empty);
				textToSend = textToSend.Replace("\n", string.Empty);
				textToSend = textToSend.Replace(" ", string.Empty);
				textToSend = textToSend.Replace("-", string.Empty);
				textToSend = textToSend.Replace("0x", string.Empty);

				text = text.Replace("\n", string.Empty);
				text = text.Replace(" ", "-");

				try
				{
					byte[] bytes = bytes = Enumerable.Range(0, textToSend.Length).Where(x => x % 2 == 0).Select(x => Convert.ToByte(textToSend.Substring(x, 2), 16)).ToArray();


					//test = xbee.CalculateChecksum(bytes); //



					//bytes = xbee.Escape(bytes); //escapes bytes

					xbee.SendTransmitRequest(XbeeAPI.DESTINATION.COORDINATOR, bytes);

					//serial._serialPort.Write(bytes, 0, bytes.Length);
				}
				catch (Exception excpt)
				{
					MessageBox.Show(excpt.Message);
				}


				rtbXbeeSent.AppendText(text);
				//rtbSerialSent.AppendText(test.ToString()); 
				rtbXbeeSendBuffer.Document.Blocks.Clear();
				rtbXbeeSent.ScrollToEnd();
			}
			else
			{
				MessageBox.Show("Port not open");
			}
		}


		public delegate void UpdateTextBoxCallback(TextBox control, string text);

		public void UpdateTextBox(TextBox control, string text)
		{
			//control.Text = TextAlignment;
			//lvCommunicatedMessages.Dispatcher.Invoke(new RefreshListViewCallback(this.Refresh));
			//control.Dispatcher.Invoke(new UpdateTextBoxCallback(this.UpdateText),new object { control, text } );
			control.Dispatcher.Invoke(new UpdateTextBoxCallback(this.UpdateText), new object[] { control, text });
		}


		private void UpdateText(TextBox control, string text)
		{
			control.Text = text;
		}



		/*
		public delegate void UpdateTextCallback(string text);

		public void UpdateSerialReceivedTextBox(string text)
		{
			rtbSerialReceived.Dispatcher.Invoke(new UpdateTextCallback(this.UpdateText), new object[] { text });
		}

		public void UpdateSerialReceivedTextBox(byte[] message, int number)
		{
			string messageString = null;

			for (int i = 0; i < number; i++)
			{

				string temp = message[i].ToString("X");

				if (temp == "7E")
				{
					messageString += "\r";
					messageString += temp;
				}
				else if (message[i] < 0x10)
				{
					messageString += "0";
					messageString += temp;
				}
				else
				{
					messageString += temp;
				}
			}

			rtbSerialReceived.Dispatcher.Invoke(new UpdateTextCallback(this.UpdateText), new object[] { messageString });
			
		}
		
		private void UpdateText(string text)
		{
			rtbSerialReceived.AppendText(text);
			rtbSerialReceived.ScrollToEnd();
		}

		private void receivedDataNewline_Click(object sender, RoutedEventArgs e)
		{
			rtbSerialReceived.AppendText("\r");
			rtbSerialReceived.ScrollToEnd(); ;
		}


		private void receivedDataClear_Click(object sender, RoutedEventArgs e)
		{
			rtbSerialReceived.Document.Blocks.Clear();
			rtbSerialReceived.ScrollToEnd(); ;
		}
		*/
	}
}


