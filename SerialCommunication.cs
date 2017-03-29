/**********************************************************************************************************************************************
*	File: MJGuiSerialLib.cs
*
*	Developed By: Mansel Jeffares
*	First Build: 29 March 2017
*	Current Build:  29 March 2017
*
*	Description :
*		Serial communication methods and functions for Swarm Robotics Project
*		Built for x64, .NET 4.5.2
*
*	Limitations :
*		Build for x64
*   
*		Naming Conventions:
*			
*			Variables, camelCase, start lower case, subsequent words also upper case, if another object goes by the same name, then also with an underscore
*			Methods, PascalCase, start upper case, subsequent words also upper case
*			Constants, all upper case, unscores for seperation (camelCase ATM for this file)
* 
**********************************************************************************************************************************************/



/**********************************************************************************************************************************************
* Namespaces
**********************************************************************************************************************************************/
#region

using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
#endregion



/**********************************************************************************************************************************************
* Structures and Classes
**********************************************************************************************************************************************/
#region


namespace SwarmRoboticsGUI
{

	//cannot use precomplier defines therefore we use a static class
	static class SerialStatuses
	{
		public const int CLOSED = 0;
		public const int OPEN = 1;

	}



	public class SerialUARTCommunication
	{

		//
		static class MsgDir
		{
			public const int RX = 0;
			public const int TX = 1;
		}


		public SerialPort _serialPort;
		Queue<Message> txBuffer;
		Queue<Message> rxMessages;

		private Message txMessage;


		public struct Message
		{
			private byte headerByte1;
			private byte headerByte2;

			public byte robotID;
			public byte length;
			public byte[] message;

			public byte closeByte;


			public Message(byte id, byte[] msg, int dir)
			{
				if (dir == MsgDir.TX)
				{
					headerByte1 = 0xC4;     //magic "numbers" for heading, should be set by a const definition elsewhere
					headerByte2 = 0x3B;
					closeByte = 0xA5;
				}
				else
				{
					headerByte1 = 0;
					headerByte2 = 0;
					closeByte = 0;
				}

				robotID = id;

				length = (byte)msg.Length;

				message = msg;
			}




			//need to write
			public override string ToString()
			{
				return String.Format("NOT YET IMPLEMENTED");
			}

		}



		public SerialUARTCommunication()
		{
			txBuffer = new Queue<Message>();
			rxMessages = new Queue<Message>();

			_serialPort = new SerialPort();
			//_serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
		}

		public void sendInstruction(byte id, byte[] msg)
		{
			if (_serialPort.IsOpen)
			{
				txMessage = new Message(id, msg, MsgDir.TX);
				txBuffer.Enqueue(txMessage);
			}
		}

		public void sendInstruction(Message msg)        //overload from predefined messages (?)
		{

		}


	}
}


#endregion



namespace SwarmRoboticsGUI
{
	public partial class MainWindow : Window
	{
		/**********************************************************************************************************************************************
		* Constants
		**********************************************************************************************************************************************/
		#region

		// Tablet to Tower
		private const byte commsTestTablet2Tower = 0xE0;
		private const byte setTowerLEDs = 0xE1;
		private const byte setTowerLights = 0xE2;
		private const byte requestTowerSensors = 0xE3;


		// Tower to Tablet
		private const byte commsTestTower2Tablet = 0xF0;
		private const byte towerSensorData = 0xF3;


		// Tower to Robot 
		private const byte commsTestTower2Robot = 41;
		private const byte dockAtThisPort = 42;


		// Tablet to Robot (test mode)
		private const byte commsTestTablet2Robot = 0xC0;
		private const byte allTestToggle = 0xC1;
		private const byte lineTestToggle = 0xC2;
		private const byte proximityTestToggle = 0xC3;
		private const byte colourTestToggle = 0xC4;
		private const byte mouseTestToggle = 0xC5;
		private const byte batteryTestToggle = 0xC6;
		private const byte motor1TestPWM = 0xC7;
		private const byte motor2TestPWM = 0xC8;
		private const byte motor3TestPWM = 0xC9;
		private const byte moveRobot = 0xCA;
		private const byte robotMode = 0xCB;
		private const byte softReset = 0xCC;


		// Robot to Tower   
		private const byte commsTestRobot2Tower = 51;
		private const byte requestToDock = 52;


		// data associated with the move command
		private const byte north = 0;
		private const byte northEast = 1;
		private const byte east = 2;
		private const byte southEast = 3;
		private const byte south = 4;
		private const byte southWest = 5;
		private const byte west = 6;
		private const byte northWest = 7;
		private const byte clockwise = 8;
		private const byte anticlockwise = 9;
		private const byte halt = 10;


		// data associated with the mode command
		private const byte manualMode = 0;
		private const byte lineFollowMode = 1;
		private const byte lightFollowMode = 2;
		private const byte objectAvoidanceMode = 3;
		private const byte dockMode = 4;
		private const byte undockMode = 5;
		private const byte haltMode = 6;


		// Robot to Tablet (test mode)
		private const byte commsTestRobot2Tablet = 0xD0;
		private const byte allTestData = 0xD1;
		private const byte lineTestData = 0xD2;
		private const byte proximityTestData = 0xD3;
		private const byte colourTestData = 0xD4;
		private const byte mouseTestData = 0xD5;
		private const byte batteryTestData = 0xD6;

		// misc data
		private const byte heartBeatRobot2Tablet = 33;
		private const byte iAmLinked = 34;


		// serial receive state defines
		private const byte RX_IDLE_STATE = 0;
		private const byte RX_HEADER_STATE = 1;
		private const byte RX_ROBOTID_STATE = 2;
		private const byte RX_COMMAND_STATE = 3;
		private const byte RX_NUM_OF_BYTES_STATE = 4;
		private const byte RX_DATA_STATE = 5;
		private const byte RX_END_PACKET_STATE = 6;

		//default serial port settings
		private const string DEFAULT_BAUD_RATE = "9600";
		private const string DEFAULT_STOP_BITS = "One";
		private const string DEFAULT_PARITY = "None";

		#endregion

		/**********************************************************************************************************************************************
		* Variables
		**********************************************************************************************************************************************/
		#region

		// flags
		private bool instructionReceivedFlag = false;
		private bool commsRobotTestFlag = false;
		private bool commsTowerTestFlag = false;
		private bool lineTestFlag = false;
		private bool colourTestFlag = false;
		private bool allTestFlag = false;
		private bool proximityTestFlag = false;
		private bool mouseTestFlag = false;
		private bool batteryTestFlag = false;


		// variables
		private byte robotId = 1;
		private byte[] rxBuffer = new byte[100];
		private byte rxState = RX_IDLE_STATE;
		private byte rxInstruction = 0;
		private byte rxChar;
		private byte rxCount;
		private byte rxNumberDataBytes;
		private byte[] txBuffer = new byte[100];

		private int commsTestTimer = 10;

		private byte[] rxTowerSensorData = new byte[6];
		private byte towerLedState;
		private byte towerLightState;
		private byte towerSensorVal0, towerSensorVal1, towerSensorVal2, towerSensorVal3, towerSensorVal4, towerSensorVal5;

		//private SerialPort _serialPort;
		private int _serialStatus = SerialStatuses.CLOSED;
		private static byte[] indata = new byte[100];
		//private Bitmap robot = null;

		#endregion

		


		//data binding would be a better way to do this
		public void PopulateSerialSettings()
		{
			string[] baudRates = new string[] { "4800", "9600", "19200", "38400", "57600", "115200", "230400", "460800", "921600" };

			menuCommunicationBaudList.Items.Clear();
			for (int i = 0; i < baudRates.Length; i++)
			{
				MenuItem item = new MenuItem { Header = baudRates[i] };
				item.Click += new RoutedEventHandler(menuCommunicationBaudRateListItem_Click);
				item.IsCheckable = true;

				if(baudRates[i] == DEFAULT_BAUD_RATE)
				{
					item.IsChecked = true;
				}
				
				menuCommunicationBaudList.Items.Add(item);
			}

			string[] stopBits = new string[] { "None", "One", "One Point Five", "Two" };

			menuCommunicationStopBitsList.Items.Clear();
			for (int i = 0; i < stopBits.Length; i++)
			{
				MenuItem item = new MenuItem { Header = stopBits[i] };
				item.Click += new RoutedEventHandler(menuCommunicationStopBitsListItem_Click);
				item.IsCheckable = true;


				if (stopBits[i] == DEFAULT_STOP_BITS)
				{
					item.IsChecked = true;
				}


				menuCommunicationStopBitsList.Items.Add(item);
			}

			string[] parity = new string[] { "None", "Odd", "Even", "Mark", "Space" };

			menuCommunicationParityList.Items.Clear();
			for (int i = 0; i < parity.Length; i++)
			{
				MenuItem item = new MenuItem { Header = parity[i] };
				item.Click += new RoutedEventHandler(menuCommunicationParityListItem_Click);
				item.IsCheckable = true;

				if (parity[i] == DEFAULT_PARITY)
				{
					item.IsChecked = true;
				}

				menuCommunicationParityList.Items.Add(item);
			}
		}



		public void PopulateSerialPorts()
		{
			string[] ports = SerialPort.GetPortNames();

			for (int i = 0; i < ports.Length; i++)
			{
				MenuItem item = new MenuItem { Header = ports[i] };
				item.Click += new RoutedEventHandler(menuCommunicationPortListItem_Click);
				item.IsCheckable = true;

				menuCommunicationPortList.Items.Add(item);

				if (menuCommunicationPortList.Items.Count == 0)
				{
					MenuItem nonefound = new MenuItem { Header = "No Com Ports Found" };
					menuCommunicationPortList.Items.Add(nonefound);
					nonefound.IsEnabled = false;
					menuCommunicationConnect.IsEnabled = false;
				}

			}
		}



		private void menuCommunicationBaudRateListItem_Click(object sender, RoutedEventArgs e)
		{
			MenuItem menusender = (MenuItem)sender;
			String menusenderstring = menusender.ToString();

			if (_serialStatus == SerialStatuses.CLOSED)
			{

				var allitems = menuCommunicationBaudList.Items.OfType<MenuItem>().ToArray();

				foreach (var item in allitems)
				{
					item.IsChecked = false;
				}

				menusender.IsChecked = true;

			}
		}



		private void menuCommunicationStopBitsListItem_Click(object sender, RoutedEventArgs e)
		{
			MenuItem menusender = (MenuItem)sender;
			String menusenderstring = menusender.ToString();

			if (_serialStatus == SerialStatuses.CLOSED)
			{

				var allitems = menuCommunicationStopBitsList.Items.OfType<MenuItem>().ToArray();

				foreach (var item in allitems)
				{
					item.IsChecked = false;
				}

				menusender.IsChecked = true;

			}
		}



		private void menuCommunicationParityListItem_Click(object sender, RoutedEventArgs e)
		{
			MenuItem menusender = (MenuItem)sender;
			String menusenderstring = menusender.ToString();

			if (_serialStatus == SerialStatuses.CLOSED)
			{

				var allitems = menuCommunicationParityList.Items.OfType<MenuItem>().ToArray();

				foreach (var item in allitems)
				{
					item.IsChecked = false;
				}

				menusender.IsChecked = true;

			}
		}



		private void menuCommunicationPortListItem_Click(object sender, RoutedEventArgs e)
		{
			MenuItem menusender = (MenuItem)sender;
			String menusenderstring = menusender.ToString();

			if (_serialStatus == SerialStatuses.CLOSED)
			{

				var allitems = menuCommunicationPortList.Items.OfType<MenuItem>().ToArray();

				foreach (var item in allitems)
				{
					item.IsChecked = false;
				}

				menusender.IsChecked = true;
				menuCommunicationConnect.IsEnabled = true;

			}
		}



		public void menuCommunicationConnect_Click(object sender, RoutedEventArgs e)
		{
			MenuItem menusender = (MenuItem)sender;
			String menusenderstring = menusender.ToString();

			
			//if (_capturestatus == CaptureStatuses.STOPPED && currentlyconnectedcamera != menusenderstring) //also check if the same menu option is clicked twice
			if (_serialStatus == SerialStatuses.CLOSED )
			{
				

				//repeated code split into function
				var portItems = menuCommunicationPortList.Items.OfType<MenuItem>().ToArray();

				foreach (var item in portItems)
				{
					if (item.IsChecked)
					{
						item.IsEnabled = false;
						serial._serialPort.PortName = item.Header.ToString();
					}
					else
					{
						item.IsChecked = false;
						item.IsEnabled = false;
					}
				}


				var baudItems = menuCommunicationBaudList.Items.OfType<MenuItem>().ToArray();

				

				foreach (var item in baudItems)
				{
					if (item.IsChecked)
					{
						//string name = item.Header.ToString();
						item.IsEnabled = false;
						serial._serialPort.BaudRate = int.Parse(item.Header.ToString());
					}
					else
					{
						item.IsChecked = false;
						item.IsEnabled = false;
					}
				}


				var parityItems = menuCommunicationParityList.Items.OfType<MenuItem>().ToArray();
				//string methodName = null;

				foreach (var item in parityItems)
				{
					if (item.IsChecked)
					{
						item.IsEnabled = false;

						serial._serialPort.Parity = (Parity)Enum.Parse(typeof(Parity), item.Header.ToString(), true);
					}
					else
					{
						item.IsChecked = false;
						item.IsEnabled = false;
					}
				}

				//default value always
				serial._serialPort.DataBits = 8;


				var stopBitsItems = menuCommunicationStopBitsList.Items.OfType<MenuItem>().ToArray();

				foreach (var item in stopBitsItems)
				{
					if (item.IsChecked)
					{
						item.IsEnabled = false;
						string str1 = null;
						string str2 = null;
						str1 = item.Header.ToString();
						str2 = Regex.Replace(item.Header.ToString(), @"\s+", "");
						//_serialPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), Regex.Replace(item.Header.ToString(), @"\s+", ""), true);
						serial._serialPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), str2, true);      //might need to remve spaces for "one point five" to work
																										//1.5 stop bits causes an error unless data bits is 5
					}
					else
					{
						item.IsChecked = false;
						item.IsEnabled = false;
					}


					//default value always (no menu yet)
					serial._serialPort.Handshake = Handshake.None;

					if(serial._serialPort.IsOpen)
					{
						try
						{
							serial._serialPort.Close();
						}
						catch (Exception excpt)
						{
							MessageBox.Show(excpt.ToString());
						}
					}


					try
					{
						serial._serialPort.Open();

						if (serial._serialPort.IsOpen)
						{
							_serialStatus = SerialStatuses.OPEN;
						}
					}
					catch (Exception excpt)
					{
						MessageBox.Show(excpt.ToString());
					}
				}
			}
			else
			{
				try
				{
					serial._serialPort.Close();
				}
				catch (Exception excpt)
				{
					MessageBox.Show(excpt.ToString());
				}

				_serialStatus = SerialStatuses.CLOSED;

				MenuItem[] ports = menuCommunicationPortList.Items.OfType<MenuItem>().ToArray();
				MenuItem[] bauds = menuCommunicationBaudList.Items.OfType<MenuItem>().ToArray();
				MenuItem[] stops = menuCommunicationStopBitsList.Items.OfType<MenuItem>().ToArray();
				MenuItem[] parity = menuCommunicationParityList.Items.OfType<MenuItem>().ToArray();

				List<MenuItem> itemList = new List<MenuItem>(ports.Concat<MenuItem>(bauds));
				//allItems = allItems + List<MenuItem>(stops.Concat<MenuItem>(parity));
				itemList.AddRange(stops);
				itemList.AddRange(parity);

				MenuItem[] finalArray = itemList.ToArray();

				//var items = menuCommunicationPortList.Items.OfType<MenuItem>().ToArray();

				foreach (var item in finalArray)
				{
					item.IsEnabled = true;
					menuCommunicationConnect.IsChecked = false;
				}
			}
		}

		public  void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
		{
			SerialPort sp = (SerialPort)sender;
			int bytes = sp.BytesToRead;
			//string indata = sp.Read()
			
			sp.Read(indata, 0, bytes);

			for(int i = 0; i < bytes; i++)
			{
				//rtbSerial.AppendText(indata[i].ToString());	//threading error

				//avoid's threading error
				rtbSerial.Dispatcher.Invoke(new UpdateTextCallback(this.UpdateText), new object[] { indata, bytes });
			}
			
		}



		public delegate void UpdateTextCallback(byte[] message, int number);

		private void UpdateText(byte[] message, int number)
		{
			for (int i = 0; i < number; i++)
			{
				rtbSerial.AppendText(message[i].ToString()); 
			}
			rtbSerial.AppendText(Environment.NewLine);
		}

	}
}