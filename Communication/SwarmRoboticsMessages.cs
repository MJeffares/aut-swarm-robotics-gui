/**********************************************************************************************************************************************
*	File: SwarmRoboticsProtocol.cs
*
*	Developed By: Mansel Jeffares
*	First Build: 31 July 2017
*	Current Build:  12 September 2017
*
*	Description :
*		Swarm Robotics Project Custom Protocol Messages Definitions
*		Developed for a final year university project at Auckland University of Technology (AUT), New Zealand
*		
*	Useage :
*		Used ontop of Xbee protocol
*
*	Limitations :
*		Built for x64, .NET 4.5.2
*   
*	Naming Conventions:
*		Variables, camelCase, start lower case, subsequent words also upper case, if another object goes by the same name, then also with an underscore
*		Methods, PascalCase, start upper case, subsequent words also upper case
*		Constants, all upper case, unscores for seperation
* 
**********************************************************************************************************************************************/



using System;
using System.ComponentModel;
using XbeeHandler.XbeeFrames;
using SwarmRoboticsGUI;

namespace SwarmRoboticsCommunicationProtocolHandler.SwarmRoboticsCommunicationProtocolMessages
{

	public class SwarmProtocolMessage : ZigbeeReceivePacket
	{
		public byte messageID;
		public byte[] messageData;

		public SwarmProtocolMessage(byte[] frame) : base(frame)
		{
			messageID = receivedData[0];
			messageData = new byte[receivedData.Length - 1];
			Array.Copy(receivedData, 1, messageData, 0, receivedData.Length - 1);

            dispMessageType = "Swarm Message";
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

    public class RobotStatus : SwarmProtocolMessage
    {
        public UInt16 batteryVoltage;
        public byte task;
        public string disptask;

        public RobotStatus(byte[] frame) : base(frame)
        {
            task = messageData[0];
            batteryVoltage = (UInt16) (256 * messageData[1] + messageData[2]);

            disptask = EnumUtils<TaskType>.GetDescription((TaskType)(task));

            dispMessageType = "Robot Status";
            dispMessageData = "Task :" + disptask + ", Battery Voltage: " + batteryVoltage.ToString() + "mV";            
        }
    }

    public class DebugString : SwarmProtocolMessage
    {
        public string msg;

        public DebugString(byte[] frame) : base(frame)
        {
            msg = System.Text.Encoding.ASCII.GetString(messageData);
            dispMessageData = msg;

            dispMessageType = "Debug String";
            dispMessageData = msg;  

        }
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

		public CommunicationTest(byte[] frame) : base(frame)
		{
			communicationTestResult = testMessage[0];

            dispMessageType = "Communication Test";
            dispMessageData = "Successful";
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
		public static class Sensors
		{
			public const byte leftHandSide = 0xF9;
			public const byte rightHandSide = 0xF8;
		}

		public byte sensor;
		public byte[] sensorData = new byte[2];

		//public override string MessageDataDisplay;

		public LightSensorTestData(byte[] frame) : base(frame)
		{
			sensor = testMessage[0];
			sensorData[0] = testMessage[1];
			sensorData[1] = testMessage[2];

			//MessageDataDisplay = (256 * sensorData[0] + sensorData[1]).ToString();
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

	public class LineSensorTestData : SystemTestMessage
	{
		public static class Sensors
		{
			public const byte farLeft = 0x0D;
			public const byte centreLeft = 0x0F;
			public const byte centreRight = 0x00;
			public const byte farRight = 0x07;
		}


		public byte sensor;
		public byte[] sensorData = new byte[2];

		//public override string MessageDataDisplay;

		public LineSensorTestData(byte[] frame) : base(frame)
		{
			sensor = testMessage[0];
			sensorData[0] = testMessage[1];
			sensorData[1] = testMessage[2];

			//MessageDataDisplay = (256 * sensorData[0] + sensorData[1]).ToString();
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

	public class MouseSensorTestData : SystemTestMessage
	{
		public int dX;
		public int dY;

		public string dXDisplay;
		public string dYDisplay;
		//public override string MessageDataDisplay;

		public MouseSensorTestData(byte[] frame) : base(frame)
		{
			dX = 256 * testMessage[0] + testMessage[1];
			dY = 256 * testMessage[2] + testMessage[3];

			dXDisplay = dX.ToString();
			dYDisplay = dY.ToString();
			//MessageDataDisplay = "dX: " + dXDisplay + " dY: " + dYDisplay;
		}


		public override string MessageDataDisplay
		{
			get
			{
				//MANSEL: Improve this
				return "dX: " + dXDisplay + " dY: " + dYDisplay;
			}
		}

	}

	public class IMUSensorTestData : SystemTestMessage
	{
		public byte[] pitchArr = new byte[4];
		public byte[] rollArr = new byte[4];
		public byte[] yawArr = new byte[4];

		public float pitch;
		public float roll;
		public float yaw;

		public string pitchDisplay;
		public string rollDisplay;
		public string yawDisplay;

		//public override string MessageDataDisplay;

		public IMUSensorTestData(byte[] frame) : base(frame)
		{
			for (int i = 0; i < 4; i++)
			{
				pitchArr[3 - i] = testMessage[i];
			}
			pitch = BitConverter.ToSingle(pitchArr, 0);

			for (int i = 0; i < 4; i++)
			{
				rollArr[3 - i] = testMessage[i + 4];
			}
			roll = BitConverter.ToSingle(rollArr, 0);

			for (int i = 0; i < 4; i++)
			{
				yawArr[3 - i] = testMessage[i + 8];
			}
			yaw = BitConverter.ToSingle(yawArr, 0);

			pitchDisplay = pitch.ToString();
			rollDisplay = roll.ToString();
			yawDisplay = yaw.ToString();

			//MessageDataDisplay = "W: " + wDisplay + " X: " + xDisplay + " Y: " + yDisplay + " Z: " + zDisplay;
		}

		public override string MessageDataDisplay
		{
			get
			{
				//MANSEL: Improve this
				return "Pitch: " + pitchDisplay + " Roll: " + rollDisplay + " Yaw: " + yawDisplay;
			}
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

			if (testMessage[1] > 0x80)
			{
				motorDir = true;
				motorSpeed = testMessage[1] - 0x80;
			}
			else
			{
				motorDir = false;
				motorSpeed = testMessage[1];
			}

			//MANSEL: test motor conversion
		}
	}

	public class TWIMuxTestData : SystemTestMessage
	{
		public byte address;

		public TWIMuxTestData(byte[] frame) : base(frame)
		{
			address = testMessage[0];
		}

		public override string MessageDataDisplay
		{
			get
			{
				//MANSEL: Improve this
				return address.ToString();
			}
		}
	}

    public class TowerDockingLightSensorData : SystemTestMessage
    {
        public static class Sensors
        {
            public const byte A = 0x01;
            public const byte B = 0x02;
            public const byte C = 0x03;
            public const byte D = 0x04;
            public const byte E = 0x05;
            public const byte F = 0x06;
        }


        public byte sensor;
        //public byte[] sensorData = new byte[2];
        public byte sensorData;

        public TowerDockingLightSensorData(byte[] frame)
            : base(frame)
        {
            sensor = testMessage[0];
            sensorData = testMessage[1];
        }

        public override string MessageDataDisplay
        {
            get
            {
                //MANSEL: Improve this
                return sensorData.ToString();
            }
        }
    }
}