/**********************************************************************************************************************************************
*	File: XbeeFrames.cs
*
*	Developed By: Mansel Jeffares
*	First Build: 12 September 2017
*	Current Build: 12 September 2017
*
*	Description :
*		Definitions for Xbee Frames 
*		Developed for a university Swarm Robotics Project
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



using System;

namespace XbeeHandler.XbeeFrames
{
	public class XbeeAPIFrame
	{
		public DateTime timeStamp;
		public byte[] rawMessage;
		public int length;
		public byte frameCommand;
		public byte[] frameData;
		public int checksum;

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