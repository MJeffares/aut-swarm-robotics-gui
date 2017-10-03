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
		public DateTime TimeStamp { get; set; }
		public byte[] RawMessage { get; set; }
        public int Length { get; set; }
        public byte FrameCommand { get; set; }
        public byte[] FrameData { get; set; }
        public int Checksum { get; set; }

        //Displayed attributes
        protected string dispTimeStamp;
        protected string dispSource;
        protected string dispMessageType;
        protected string dispMessageData;
        protected string dispRawMessage;
        

        public XbeeAPIFrame(byte[] frame)
		{
			TimeStamp = DateTime.Now;
			RawMessage = new byte[frame.Length];
			Array.Copy(frame, RawMessage, frame.Length);
			Length = 256 * frame[1] + frame[2];
			FrameCommand = frame[3];
			FrameData = new byte[frame.Length - 5];
			Array.Copy(frame, 4, FrameData, 0, frame.Length - 5);
			Checksum = frame[frame.Length - 1];

            dispTimeStamp = TimeStamp.ToString("HH:mm:ss:fff");
            dispRawMessage = MJLib.HexToString(RawMessage, 0, RawMessage.Length, true);
            dispMessageType = "XBEE";
        }
        
		public string TimeStampDisplay
		{
			get
			{
                return dispRawMessage;
			}
		}

        public virtual string SourceDisplay
        {
            get
            {
                return dispSource;
            }
        }

        public virtual string MessageTypeDisplay
        {
            get
            {
                return dispMessageType;
            }
        }

        public virtual string MessageDataDisplay
        {
            get
            {
                return dispMessageData;
            }
        }

		public string RawMessageDisplay
		{
			get
			{
                return dispRawMessage;
			}
		}

	}
    /*

	class ATCommand : XbeeAPIFrame
	{
		//MANSEL: add the ATCommand class
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
		//MANSEL: add the ATCommandQueue class
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
		//MANSEL: add the ZigbeeTransmitRequest class
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
		//MANSEL: add the RemoteCommandRequest class
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
		//MANSEL: add the CreateSourceRoute class
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
		//MANSEL: add the ATCommandResponse class
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
		//MANSEL: add the ModemStatus class
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
			frameID = FrameData[0];
			destinationAddress16 = MJLib.ByteArrayToUInt16(FrameData, 1);
			transmitRetryCount = FrameData[3];
			deliveryStatus = FrameData[4];
			discoveryStatus = FrameData[5];

            dispMessageType = "Xbee Transmit Status";

            if(deliveryStatus == 0x00)
            {
                dispMessageData = "Success";
            }
            else if(deliveryStatus == 0x24)
            {
                dispMessageData = "Address Not Found";
            }
            else
            {
                dispMessageData = "Failure";
            }

            dispMessageData += " Retries:" + transmitRetryCount.ToString();
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
			sourceAddress64 = MJLib.ByteArrayToUInt64(FrameData, 0);
			sourceAddress16 = MJLib.ByteArrayToUInt16(FrameData, 8);
			receiveOptions = FrameData[10];
			receivedData = new byte[FrameData.Length - 11];
			Array.Copy(FrameData, 11, receivedData, 0, FrameData.Length - 11);

            //dispSource = XbeeAPI.DESTINATION.ToString(sourceAddress64) + " (" + sourceAddress64.ToString() + ")";
            dispSource = XbeeAPI.DESTINATION.ToString(sourceAddress64);

			dispMessageType = "Xbee Received Message";
			dispMessageData = MJLib.HexToString(receivedData, 0, receivedData.Length, true);
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
		//MANSEL: add the NodeIdentificationIndicator class
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
		//MANSEL: add the RemoteCommandResponse class
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
		//MANSEL: add the ExtendedModemStatus class
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
		//MANSEL: add the RouteRecordIndicator class

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
		//MANSEL: add the ManyToOneRouteRequestIndicator class

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
