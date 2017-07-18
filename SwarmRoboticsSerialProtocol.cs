/**********************************************************************************************************************************************
*	File: SwarmRoboticsProtocol.cs
*
*	Developed By: Mansel Jeffares
*	First Build: 28 April 2017
*	Current Build:  28 April 2017
*
*	Description :
*		Swarm Robotics Project Custom Protocol
*		Built for x64, .NET 4.5.2
*		
*	Useage :
*		Used ontop of Xbee protocol
*
*	Limitations :
*		Build for x64
*   
*		Naming Conventions:
*			
*			Variables, camelCase, start lower case, subsequent words also upper case, if another object goes by the same name, then also with an underscore
*			Methods, PascalCase, start upper case, subsequent words also upper case
*			Constants, all upper case, unscores for seperation
* 
**********************************************************************************************************************************************/

/**********************************************************************************************************************************************
* Namespaces
**********************************************************************************************************************************************/
#region

using SwarmRoboticsGUI;
using System.Windows;
using System.Windows.Controls;

#endregion

/**********************************************************************************************************************************************
* Structures and Classes
**********************************************************************************************************************************************/
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

	public void MessageReceived(byte[] message)
	{
		string[] tokens;

		if (window.testMode)
		{
			switch (message[0])
			{
				case MESSAGE_TYPES.SYSTEM_TEST_PROXIMITY_SENSORS:
					string proximityData = GetMessageData(message[0], message, false);
					tokens = proximityData.Split(',');

					switch (tokens[0])
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
					string lineData = GetMessageData(message[0], message, false);
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

				case MESSAGE_TYPES.SYSTEM_TEST_MOTORS:
					//return "System Test Motor";
					break;

				case MESSAGE_TYPES.SYSTEM_TEST_MOUSE:
					//return "System Test Mouse";
					break;

				case MESSAGE_TYPES.SYSTEM_TEST_IMU:
					//return "System Test IMU";
					break;

				case MESSAGE_TYPES.SYSTEM_TEST_LINE_FOLLOWERS:
					//return "System Test Line Followers";
					break;

				case MESSAGE_TYPES.SYSTEM_TEST_FAST_CHARGE_CHIP:
					//return "System Test Fast Charge Chip";
					break;

				case MESSAGE_TYPES.SYSTEM_TEST_TWI_MUX:
					//return "System Test TWI Mux";
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
			switch (message[0])
			{

				
				case MESSAGE_TYPES.COMMUNICATION_TEST:

					//window.UpdateSerialReceivedTextBox("\rCommunication Test Successful");
					//XXX
					//display the data here
					break;

				case MESSAGE_TYPES.BATTERY_VOLTAGE:
					float voltage = message[1] * 256 + message[2];
					voltage = voltage * 5 / 1000;
					//window.UpdateSerialReceivedTextBox("\rBattery Voltage:");
					//window.UpdateSerialReceivedTextBox(voltage.ToString());
					break;
					
			}
		}
	}


	public void SendMessage(byte type)
	{
		switch (type)
		{
			case MESSAGE_TYPES.COMMUNICATION_TEST:

				window.xbee.SendTransmitRequest(XbeeHandler.DESTINATION.BROADCAST, MESSAGE_TYPES.COMMUNICATION_TEST);

				break;

			case MESSAGE_TYPES.BATTERY_VOLTAGE:

				window.xbee.SendTransmitRequest(XbeeHandler.DESTINATION.BROADCAST, MESSAGE_TYPES.BATTERY_VOLTAGE);

				break;

		}
	}

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

			case MESSAGE_TYPES.BATTERY_VOLTAGE:
				float voltage = (float)(messageData[0] * 256 + messageData[1]) * 5 / 1000;
				return voltage.ToString() + " Volts";

			case MESSAGE_TYPES.SYSTEM_TEST_PROXIMITY_SENSORS:
				float distance = (float)(messageData[2] * 256 + messageData[3]);

				if (words == true)
				{
					switch (messageData[1])
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
					switch (messageData[1])
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
				float light = (float)(messageData[1] * 256 + messageData[2]);

				if (words == true)
				{
					switch (messageData[1])
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
					switch (messageData[1])
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
					returnMessage = dir.ToString() + "," + speed.ToString();
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

namespace SwarmRoboticsGUI
{
	public partial class MainWindow : Window
	{
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
	}
}
	


#region old_parts

/*
**********************************************************************************************************************************************
		* Constants
**********************************************************************************************************************************************
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

**********************************************************************************************************************************************
* Variables
**********************************************************************************************************************************************
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




static class SerialStatuses
{
	public const int CLOSED = 0;
	public const int OPEN = 1;

}



public class SerialUARTCommunication
{
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
*/
#endregion
