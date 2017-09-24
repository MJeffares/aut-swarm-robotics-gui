/**********************************************************************************************************************************************
*	File: XbeeFrames.cs
*
*	Developed By: Mansel Jeffares
*	First Build: 31 July
*	Current Build: 12 September 2017
*
*	Description :
*		Definitions for Xbee Frames 
*		Developed for a final year university project at Auckland University of Technology (AUT), New Zealand
*		
*	Useage :
*		
*
*	Limitations :
*		Build for x64, .NET 4.5.2
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

using System;

#endregion



namespace XbeeHandler.XbeeFrames
{
	public static class API_FRAME_TYPES
	{
		public const byte AT_COMMAND = 0x08;
		public const byte AT_COMMAND_QUEUE = 0x09;
		public const byte ZIGBEE_TRANSMIT_REQUEST = 0x10;
		public const byte EXPLICIT_ADDRESSING_ZIGBEE_COMMAND_FRAME = 0x11;
		public const byte REMOTE_COMMAND_REQUEST = 0x17;
		public const byte CREATE_SOURCE_ROUTE = 0x21;
		public const byte AT_COMMAND_RESPONSE = 0x88;
		public const byte MODEM_STATUS = 0x8A;
		public const byte ZIGBEE_TRANSMIT_STATUS = 0x8B;
		public const byte ZIGBEE_RECEIVE_PACKET = 0x90;
		public const byte ZIGBEE_EXPLICIT_RX_INDICATOR = 0x91;
		public const byte ZIGBEE_IO_DATA_SAMPLE_RX_INDICATOR = 0x92;
		public const byte XBEE_SENSOR_READ_INDICATOR = 0x94;
		public const byte NODE_IDENTIFICATION_INDICATOR = 0x95;
		public const byte REMOTE_COMMAND_RESPONSE = 0x97;
		public const byte EXTENDED_MODEM_STATUS = 0x98;
		public const byte OTA_FIRMWARE_UPDATE_STATUS = 0xA0;
		public const byte ROUTE_RECORD_INDICATOR = 0xA1;
		public const byte MANY_TO_ONE_ROUTE_REQUEST_INDICATOR = 0xA3;
	}



	public class XbeeAPIFrame
	{
		public DateTime timeStamp { get; set; }
		public byte[] rawMessage { get; set; }
        public int length { get; set; }
        public byte frameCommand { get; set; }
        public byte[] frameData { get; set; }
        public int checksum { get; set; }

        public XbeeAPIFrame(byte[] frame)
		{
			timeStamp = DateTime.Now;
			rawMessage = new byte[frame.Length];
			Array.Copy(frame, rawMessage, frame.Length);
			length = 256 * frame[1] + frame[2];
			frameCommand = frame[3];
			frameData = new byte[frame.Length - 5];
			Array.Copy(frame, 4, frameData, 0, frame.Length - 5);
			checksum = frame[frame.Length - 1];
		}


		public string TimeStampDisplay
		{
			get
			{
				return timeStamp.ToString("HH:mm:ss:fff");
			}
		}

		public string RawMessageDisplay
		{
			get
			{
				return MJLib.HexToString(rawMessage, 0, rawMessage.Length, true);
			}
		}

		public string FrameLengthDisplay
		{
			get
			{
				return MJLib.HexToString(BitConverter.GetBytes(length), 0, 1, true) + " (" + length.ToString() + ")";
			}
		}

		public virtual string FrameCommandDisplay
		{
			get
			{
				return "WARNING: unhandled xbee frame received";
			}
		}

		public virtual string FrameDataDisplay
		{
			get
			{
				return MJLib.HexToString(frameData, 0, length, true);
			}
		}

		public virtual string SourceDisplay
		{
			get
			{
				return "";
			}
		}

		public virtual string MessageTypeDisplay
		{
			get
			{
				return "";
			}
		}

		public virtual string MessageDataDisplay
		{
			get
			{
				return "";
			}
		}

	}
	/*

	class ATCommand : XbeeAPIFrame
	{
		//MANSEL: add this class
		public override string FrameCommandDisplay
		{
			get
			{
				return "Xbee: Many To One Route Request Indicator Received (N/H)";
			}
		}
	}

	class ATCommandQueue : XbeeAPIFrame
	{
		//MANSEL: add this class
		public override string FrameCommandDisplay
		{
			get
			{
				return "Xbee: Many To One Route Request Indicator Received (N/H)";
			}
		}
	}

	class ZigbeeTransmitRequest : XbeeAPIFrame
	{
		//MANSEL: add this class
		public override string FrameCommandDisplay
		{
			get
			{
				return "Xbee: Many To One Route Request Indicator Received (N/H)";
			}
		}
	}

	class ZigbeeExplicitAddressingCommandFrame : XbeeAPIFrame
	{
		public override string FrameCommandDisplay
		{
			get
			{
				return "Xbee: Many To One Route Request Indicator Received (N/H)";
			}
		}
	}

	class RemoteCommandRequest : XbeeAPIFrame
	{
		//MANSEL: add this class
		public override string FrameCommandDisplay
		{
			get
			{
				return "Xbee: Many To One Route Request Indicator Received (N/H)";
			}
		}
	}

	class CreateSourceRoute : XbeeAPIFrame
	{
		//MANSEL: add this class
		public override string FrameCommandDisplay
		{
			get
			{
				return "Xbee: Many To One Route Request Indicator Received (N/H)";
			}
		}
	}

	class ATCommandResponse : XbeeAPIFrame
	{
		//MANSEL: add this class
		public override string FrameCommandDisplay
		{
			get
			{
				return "Xbee: AT Command Response Received (N/H)";
			}
		}
	}

	class ModemStatus : XbeeAPIFrame
	{
		//MANSEL: add this class
		public override string FrameCommandDisplay
		{
			get
			{
				return "Xbee: Modem Status Received (N/H)";				
			}
		}
	}
	*/

	class ZigbeeTransmitStatus : XbeeAPIFrame
	{
		public byte frameID;
		public UInt16 destinationAddress16;
		public byte transmitRetryCount;
		public byte deliveryStatus;
		public byte discoveryStatus;

		public ZigbeeTransmitStatus(byte[] frame) : base(frame)
		{
			frameID = frameData[0];
			destinationAddress16 = MJLib.ByteArrayToUInt16(frameData, 1);
			transmitRetryCount = frameData[3];
			deliveryStatus = frameData[4];
			discoveryStatus = frameData[5];
		}

		public override string FrameCommandDisplay
		{
			get
			{
				return "WARNING: unhandled swarm message received";
			}
		}

		public override string FrameDataDisplay
		{
			get
			{
				return MJLib.HexToString(frameData, 0, length, true);
			}
		}

	}


	public class ZigbeeReceivePacket : XbeeAPIFrame
	{
		public UInt64 sourceAddress64;
		public UInt16 sourceAddress16;
		public byte receiveOptions;
		public byte[] receivedData;


		public ZigbeeReceivePacket(byte[] frame) : base(frame)
		{
			sourceAddress64 = MJLib.ByteArrayToUInt64(frameData, 0);
			sourceAddress16 = MJLib.ByteArrayToUInt16(frameData, 8);
			receiveOptions = frameData[10];
			receivedData = new byte[frameData.Length - 11];
			Array.Copy(frameData, 11, receivedData, 0, frameData.Length - 11);
		}

		public override string SourceDisplay
		{
			get
			{
				//MANSEL: need to add UInt64 -> byte array
				//return XbeeHandler.DESTINATION.ToString(sourceAddress64) + " (" + MJLib.HexToString(source64, 0, 8, true) + " , " + MJLib.HexToString(source16, 0, 2, true) + ")";

				return XbeeAPI.DESTINATION.ToString(sourceAddress64) + " (" + sourceAddress64.ToString() + ")";
			}
		}
	}


	/*
	class ZigbeeExplicitRXIndicator : XbeeAPIFrame
	{
		public override string FrameCommandDisplay
		{
			get
			{
				return "Xbee: Explicit Data Packet Received (N/H)";				
			}
		}
	}

	class ZigbeeIODataSampleRXIndicator : XbeeAPIFrame
	{
		public override string FrameCommandDisplay
		{
			get
			{
				return "Xbee: IO Sample Received (N/H)";
			}
		}
	}

	class XbeeSensorReadIndicator : XbeeAPIFrame
	{
		public override string FrameCommandDisplay
		{
			get
			{
				return "Xbee: Sensor Read Indicator Received (N/H)";				
			}
		}
	}

	class NodeIdentificationIndicator : XbeeAPIFrame
	{
		//MANSEL: add this class
		public override string FrameCommandDisplay
		{
			get
			{
				return "Xbee: Node Identification Indicator Received (N/H)";				
			}
		}
	}

	class RemoteCommandResponse : XbeeAPIFrame
	{
		//MANSEL: add this class
		public override string FrameCommandDisplay
		{
			get
			{
				return "Xbee: Remote Command Response Received (N/H)";				
			}
		}
	}

	class ExtendedModemStatus : XbeeAPIFrame
	{
		//MANSEL: add this class
		public override string FrameCommandDisplay
		{
			get
			{
				return "Xbee: Extended Modem Status Received (N/H)";
			}
		}
	}

	class OverTheAirFirmwareUpdateStatus : XbeeAPIFrame
	{
		public override string FrameCommandDisplay
		{
			get
			{
				return "Xbee: OTA Firmware Update Status Received (N/H)";
			}
		}
	}

	class RouteRecordIndicator : XbeeAPIFrame
	{
		//MANSEL: add this class

		public override string FrameCommandDisplay
		{
			get
			{
				return "Xbee: Route Record Indicator Received (N/H)";
			}
		}
	}

	class ManyToOneRouteRequestIndicator : XbeeAPIFrame
	{
		//MANSEL: add this class

		public override string FrameCommandDisplay
		{
			get
			{
				return "Xbee: Many To One Route Request Indicator Received (N/H)";
			}
		}

	}
	*/
}