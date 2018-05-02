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


			dispMessageType = "System Test Message";
			dispMessageData = "No Data";
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
		public UInt16 sensorValue;

		public ProximitySensorTestData(byte[] frame) : base(frame)
		{
			sensor = testMessage[0];
			sensorData[0] = testMessage[1];
			sensorData[1] = testMessage[2];

			sensorValue = (UInt16)(256 * sensorData[0] + sensorData[1]);
			dispMessageType = "Proximity Sensor Data";
			dispMessageData = "Sensor: " + MJLib.HexToString(testMessage, 0, 1, true) + " Value: " + sensorValue.ToString();
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
		public UInt16 sensorValue;

		public LightSensorTestData(byte[] frame) : base(frame)
		{
			sensor = testMessage[0];
			sensorData[0] = testMessage[1];
			sensorData[1] = testMessage[2];

			sensorValue = (UInt16)(256 * sensorData[0] + sensorData[1]);
			dispMessageType = "Light Sensor Data";
			dispMessageData = "Sensor: " + MJLib.HexToString(testMessage, 0, 1, true) + " Value: " + sensorValue.ToString();
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
		public UInt16 sensorValue;

		public LineSensorTestData(byte[] frame) : base(frame)
		{
			sensor = testMessage[0];
			sensorData[0] = testMessage[1];
			sensorData[1] = testMessage[2];

			sensorValue = (UInt16)(256 * sensorData[0] + sensorData[1]);
			dispMessageType = "Line Sensor Data";
			dispMessageData = "Sensor: " + MJLib.HexToString(testMessage, 0, 1, true) + " Value: " + sensorValue.ToString();
		}
	}

	public class MouseSensorTestData : SystemTestMessage
	{
		public int dX;
		public int dY;

		public string dXDisplay;
		public string dYDisplay;

		public MouseSensorTestData(byte[] frame) : base(frame)
		{
			dX = 256 * testMessage[0] + testMessage[1];
			dY = 256 * testMessage[2] + testMessage[3];

			dXDisplay = dX.ToString();
			dYDisplay = dY.ToString();

			dispMessageType = "Mouse Sensor Data";
			dispMessageData = "X: " + dXDisplay + " Y: " + dYDisplay;
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

			dispMessageType = "IMU Data";
			dispMessageData = "Pitch: " + pitchDisplay + " Roll: " + rollDisplay + " Yaw: " + yawDisplay;
		}
	}

	public class MotorTestData : SystemTestMessage
	{
		public byte motor;
		public int motorSpeed;
		public bool motorDir;
		public string motorDirection;

		public MotorTestData(byte[] frame) : base(frame)
		{
			motor = testMessage[0];

			if (testMessage[1] > 0x80)
			{
				motorDir = true;
				motorDirection = " Forwards";
				motorSpeed = testMessage[1] - 0x80;
			}
			else
			{
				motorDir = false;
				motorDirection = " Backwards";
				motorSpeed = testMessage[1];
			}

			dispMessageType = "Motor Test Data";
			dispMessageData = "Motor: " + motor.ToString() + " Speed:" + motorSpeed.ToString() + motorDirection;
		}
	}

	public class TWIMuxTestData : SystemTestMessage
	{
		public byte address;

		public TWIMuxTestData(byte[] frame) : base(frame)
		{
			address = testMessage[0];

			dispMessageType = "TWI Test";
			dispMessageData = "Address: " + address.ToString();
		}
	}

	public class CameraTestData : SwarmProtocolMessage
	{
		public UInt32 pixel_index;
		public UInt16[] rgb_pixel_data;
		public UInt16[] bgr_pixel_data;
		public uint request_type;

		//RGB 565 HEX masks
        public UInt16 red_mask = 0xF800;
        public UInt16 green_mask = 0x07E0;
        public UInt16 blue_mask = 0x001F;


        public CameraTestData(byte[] frame) : base(frame)
		{
			request_type = messageData[0];
			pixel_index = (UInt32)(messageData[1] << 24 | messageData[2] << 16 | messageData[3] << 8 | messageData[4]);
			rgb_pixel_data = new UInt16[(messageData.Length - 5) / 2];
			bgr_pixel_data = new UInt16[(messageData.Length - 5) / 2];

			for (int i = 5, j = 0; i < messageData.Length; i+=2, j++)
			{
				rgb_pixel_data[j] = BitConverter.ToUInt16(messageData, i);
			}

            for (int i = 0; i < rgb_pixel_data.Length; i++)
            {
                rgb_pixel_data[i] = (UInt16)((((rgb_pixel_data[i] & 0xFF00) >> 8) | ((rgb_pixel_data[i] & 0x00FF) << 8)));
            }


            //Array.Copy(messageData, 5, pixel_data, 0, messageData.Length - 5);
            dispMessageType = "Camera Image Data";

			

			for (int i = 0; i < rgb_pixel_data.Length; i++)
			{
				UInt16 red_pixel = (UInt16)((rgb_pixel_data[i] & red_mask) >> 11);
				UInt16 green_pixel = (UInt16)((rgb_pixel_data[i] & green_mask) >> 5);
				UInt16 blue_pixel = (UInt16)(rgb_pixel_data[i] & blue_mask);

				bgr_pixel_data[i] = (UInt16)((blue_pixel << 11) | (green_pixel << 5) | red_pixel);
			}
		}
	}

	public class CameraTestRequest : SwarmProtocolMessage
	{
		public CameraTestRequest(byte[] frame) : base(frame)
		{
			dispMessageType = "Camera Image Data";
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
        public byte sensorData;

        public TowerDockingLightSensorData(byte[] frame) : base(frame)
        {
            sensor = testMessage[0];
            sensorData = testMessage[1];

			dispMessageType = "Charging Station Light Sensors";
			dispMessageData = "Sensor: " + sensor.ToString() + " Value: " + sensorData.ToString();
		}

    }

	public class TowerRobotReport : SwarmProtocolMessage
	{
		public UInt64 robotrequested;

		public TowerRobotReport(byte[] frame) : base(frame)
		{
			robotrequested = MJLib.ByteArrayToUInt64(messageData, 2);
		}
	}

}