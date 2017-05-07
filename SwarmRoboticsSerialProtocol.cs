﻿/**********************************************************************************************************************************************
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
	}


	// constructor
	public ProtocolClass(MainWindow main)
	{
		window = main;
	}
	


	public void MessageReceived(byte[] message)
	{
		switch(message[0])
		{
			case MESSAGE_TYPES.COMMUNICATION_TEST:

				window.UpdateSerialReceivedTextBox("\rCommunication Test Successful\r");
				//XXX
				//display the data here
				break;

		}
	}


	public void SendMessage(byte type)
	{
		switch (type)
		{
			case MESSAGE_TYPES.COMMUNICATION_TEST:

				window.xbee.SendTransmitRequest(XbeeHandler.DESTINATION.COORDINATOR, MESSAGE_TYPES.COMMUNICATION_TEST);

				break;

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
