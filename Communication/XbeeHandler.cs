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

#endregion



namespace SwarmRoboticsGUI
{
	//**********************************************************************************************************************************************************

	public class XbeeAPI
	{
		public static class API_FRAME
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


		//MANSEL: make a seperate source and destination class
		public static class DESTINATION
		{
			public const UInt64 COORDINATOR = 0x0000000000000000;
			public const UInt64 BROADCAST = 0x000000000000FFFF;
			//public const UInt64 ROBOT_ONE = 0x0013A20041065FB3;

            public const UInt64 ROBOT_TWO = 0x0013A2004147F9DD;
            public const UInt64 ROBOT_THREE = 0x0013A2004152F256;
            public const UInt64 ROBOT_FOUR = 0x0013A2004147F9D8;
            //MANSEL: get robot xbee ID's

			public static string ToString(UInt64 location)
			{
				switch (location)
				{
					case COORDINATOR:
						return "PC/GUI";

					case BROADCAST:
						return "Broadcast Message";

					//case ROBOT_ONE:
						//return "Robot 1";

					/*
					case ROBOT_TWO:
						return "Robot 2";

					case ROBOT_THREE:
						return "Robot 3";

					case ROBOT_FOUR:
						return "Robot 4";
					*/

					default:
						return "Warning: Unknown Desstination";
				}
			}
		}



		MainWindow window = null;

		private const byte FRAME_DELIMITER = 0x7E;
		private const byte ESCAPE_BYTE = 0x7D;
		private const byte XON = 0x11;
		private const byte XOFF = 0x13;

		private static readonly IList<byte> BYTES_TO_ESCAPE = new List<byte> { 0x7E, 0x7D, 0x11, 0x13 }.AsReadOnly();

		public XbeeAPI(MainWindow main)
		{
			window = main;
		}


		Predicate<byte> isStartByte = (byte b) => { return b == 0x7E; };
		int _receiveState = ReceiveStates.START;
		int index = 0;
		int startIndex = -1;
		int length;
		bool escape;
		List<byte> frameAsList;

		static private class ReceiveStates
		{
			public const int START = 0;
			public const int LENGTH_MSB = 1;
			public const int LENGTH_LSB = 2;
			public const int Data = 3;
		}

		public byte[] FindFrames(List<byte> buffer)
		{
			if (startIndex == -1)
			{
				startIndex = buffer.FindIndex(isStartByte);

				if (startIndex != -1)
				{
					index = startIndex;
					frameAsList = new List<byte>();
				}
				else
				{
					buffer.Clear();
				}
			}

			if (startIndex != -1)
			{
				while (buffer.Count - index >= 1)
				{
					byte temp = buffer[index];

					if (temp == 0x7E && _receiveState != ReceiveStates.START)
					{
						startIndex = -1;
					}
					else if (temp == 0x7D)
					{
						escape = true;
					}
					else if (escape)
					{
						temp ^= 0x20;
						escape = false;
					}

					if (!escape)
					{
						frameAsList.Add(temp);

						switch (_receiveState)
						{
							case ReceiveStates.START:
								if (temp == 0x7E)
								{
									_receiveState = ReceiveStates.LENGTH_MSB;
								}
								break;

							case ReceiveStates.LENGTH_MSB:
								length = temp * 256;
								_receiveState = ReceiveStates.LENGTH_LSB;
								break;

							case ReceiveStates.LENGTH_LSB:
								length += temp;
								_receiveState = ReceiveStates.Data;
								break;

							case ReceiveStates.Data:
								if (frameAsList.Count == length + 4)
								{
									buffer.RemoveRange(0, index);
									startIndex = -1;
									_receiveState = ReceiveStates.START;
									return frameAsList.ToArray();
								}
								break;
						}
					}
					index++;
				}
			}
			return null;
		}




		public byte[] EscapeReceivedByteArray(byte[] byteArrayToEscape)
		{
			Queue<byte> temp = new Queue<byte>();

			for (int i = 0; i < byteArrayToEscape.Length; i++)
			{
				if (byteArrayToEscape[i] == 0x7D)
				{
                    //MANSEL: OUT OF RANGE ERROR (i think fixed by commenting out the below line)
					//i++;
					temp.Enqueue((byte)(byteArrayToEscape[i] ^ 0x20));
                    
				}
				else
				{
					temp.Enqueue(byteArrayToEscape[i]);
				}
			}
			return temp.ToArray();
		}

		public uint ValidateChecksum(byte[] frame)
		{
			int check = 0;

			for (int i = 3; i < frame.Length; i++)
			{
				check += frame[i];
			}

			if ((byte)check == 0xFF)
			{
				return 0;
			}
			return 1;
		}

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









		public XbeeAPIFrame ParseXbeeFrame(byte[] frame)
		{
			XbeeAPIFrame specificFrame;

			switch (frame[3])
			{

				case API_FRAME.AT_COMMAND_RESPONSE:
					//window.UpdateSerialReceivedTextBox("\rXBEE: AT Command Response Received (N/H)");
					specificFrame = null;
					break;

				case API_FRAME.MODEM_STATUS:
					//window.UpdateSerialReceivedTextBox("\rXBEE: Modem Status Received (N/H)");
					specificFrame = null;
					break;


				case API_FRAME.ZIGBEE_TRANSMIT_STATUS:
					//window.UpdateSerialReceivedTextBox("\rXBEE: Transmit Status Received (N/H)");
					specificFrame = new ZigbeeTransmitStatus(frame);
					break;


				case API_FRAME.ZIGBEE_RECEIVE_PACKET:
					//window.UpdateSerialReceivedTextBox("\rXBEE: Data Packet Received ");
					specificFrame = new ZigbeeReceivePacket(frame);
					break;


				case API_FRAME.ZIGBEE_EXPLICIT_RX_INDICATOR:
					//window.UpdateSerialReceivedTextBox("\rXBEE: Explicit Data Packet Received (N/H)");
					specificFrame = null;
					break;


				case API_FRAME.ZIGBEE_IO_DATA_SAMPLE_RX_INDICATOR:
					//window.UpdateSerialReceivedTextBox("\rXBEE: IO Sample Received (N/H)");		
					specificFrame = null;
					break;


				case API_FRAME.XBEE_SENSOR_READ_INDICATOR:
					//window.UpdateSerialReceivedTextBox("\rXBEE: Sensor Read Indicator Received (N/H)");	
					specificFrame = null;
					break;


				case API_FRAME.NODE_IDENTIFICATION_INDICATOR:
					//window.UpdateSerialReceivedTextBox("\rXBEE: Node Identification Indicator Received (N/H)");
					specificFrame = null;
					break;


				case API_FRAME.REMOTE_COMMAND_RESPONSE:
					//window.UpdateSerialReceivedTextBox("\rXBEE: Remote Command Response Received (N/H)");
					specificFrame = null;
					break;


				case API_FRAME.EXTENDED_MODEM_STATUS:
					//window.UpdateSerialReceivedTextBox("\rXBEE: Extended Modem Status Received (N/H)");
					specificFrame = null;
					break;


				case API_FRAME.OTA_FIRMWARE_UPDATE_STATUS:
					//window.UpdateSerialReceivedTextBox("\rXBEE: OTA Firmware Update Status Received (N/H)");
					specificFrame = null;
					break;


				case API_FRAME.ROUTE_RECORD_INDICATOR:
					//window.UpdateSerialReceivedTextBox("\rXBEE: Route Record Indicator Received (N/H)");
					specificFrame = null;
					break;


				case API_FRAME.MANY_TO_ONE_ROUTE_REQUEST_INDICATOR:
					//window.UpdateSerialReceivedTextBox("\rXBEE: Many To One Route Request Indicator Received (N/H)");
					specificFrame = null;
					break;

				default:
					//window.UpdateSerialReceivedTextBox("\rWARNING ERROR XBEE: unhandled message received");
					specificFrame = null;
					break;
			}
			return specificFrame;
			//window.serial.communicatedMessages.Add(window.serial.NewestMessage);
			//window.RefreshListView();
		}




		//***********************************************************************************************************************************************************************





		/*
		public enum robots : UInt64 {	COORDINATOR = 0x0000000000000000,
										BROADCAST = 0x000000000000FFFF,
										ROBOT_ONE = 0x0013A20041065FB3 }

		*/



		public byte[] XbeeFrame(byte[] msg)
		{
			LinkedList<byte> messageList = new LinkedList<byte>();  //use a list over queue for added functionality
																	//Queue<byte> messageQueue = new Queue<byte>();

			byte length_msb;
			byte length_lsb;
			byte checksum;

			//calculate length
			length_msb = (byte)((msg.Length >> 7) & 0xFF);
			length_lsb = (byte)(msg.Length & 0xFF);


			//calculate checksum
			int checksum_int = 0;
			for (int i = 0; i < msg.Length; i++)
			{
				checksum_int += msg[i];
			}
			checksum = (byte)(0xFF - checksum_int);


			messageList.AddLast(FRAME_DELIMITER);


			if (BYTES_TO_ESCAPE.Contains(length_msb))
			{
				messageList.AddLast(ESCAPE_BYTE);
				messageList.AddLast((byte)(length_msb ^ 0x20));
			}
			else
			{
				messageList.AddLast(length_msb);
			}


			if (BYTES_TO_ESCAPE.Contains(length_lsb))
			{
				messageList.AddLast(ESCAPE_BYTE);
				messageList.AddLast((byte)(length_lsb ^ 0x20));
			}
			else
			{
				messageList.AddLast(length_lsb);
			}

			for (int i = 0; i < msg.Length; i++)
			{
				if (BYTES_TO_ESCAPE.Contains(msg[i]))
				{
					messageList.AddLast(ESCAPE_BYTE);
					messageList.AddLast((byte)(msg[i] ^ 0x20));
				}
				else
				{
					messageList.AddLast(msg[i]);
				}
			}


			if (BYTES_TO_ESCAPE.Contains(checksum))
			{
				messageList.AddLast(ESCAPE_BYTE);
				messageList.AddLast((byte)(checksum ^ 0x20));
			}
			else
			{
				messageList.AddLast(checksum);
			}

			return messageList.ToArray<byte>();
		}
		public byte[] XbeeFrame(byte api, byte[] msg)
		{
			LinkedList<byte> messageList = new LinkedList<byte>();  //use a list over queue for added functionality
																	//Queue<byte> messageQueue = new Queue<byte>();

			byte length_msb;
			byte length_lsb;
			byte checksum;

			//calculate length
			length_msb = (byte)((msg.Length >> 7) & 0xFF);
			length_lsb = (byte)(msg.Length & 0xFF);


			//calculate checksum
			int checksum_int = api;
			for (int i = 0; i < msg.Length; i++)
			{
				checksum_int += msg[i];
			}
			checksum = (byte)(0xFF - checksum_int);


			messageList.AddLast(FRAME_DELIMITER);


			if (BYTES_TO_ESCAPE.Contains(length_msb))
			{
				messageList.AddLast(ESCAPE_BYTE);
				messageList.AddLast((byte)(length_msb ^ 0x20));
			}
			else
			{
				messageList.AddLast(length_msb);
			}


			if (BYTES_TO_ESCAPE.Contains(length_lsb))
			{
				messageList.AddLast(ESCAPE_BYTE);
				messageList.AddLast((byte)(length_lsb ^ 0x20));
			}
			else
			{
				messageList.AddLast(length_lsb);
			}


			if (BYTES_TO_ESCAPE.Contains(api))
			{
				messageList.AddLast(ESCAPE_BYTE);
				messageList.AddLast((byte)(api ^ 0x20));
			}
			else
			{
				messageList.AddLast(api);
			}


			for (int i = 0; i < msg.Length; i++)
			{
				if (BYTES_TO_ESCAPE.Contains(msg[i]))
				{
					messageList.AddLast(ESCAPE_BYTE);
					messageList.AddLast((byte)(msg[i] ^ 0x20));
				}
				else
				{
					messageList.AddLast(msg[i]);
				}
			}


			if (BYTES_TO_ESCAPE.Contains(checksum))
			{
				messageList.AddLast(ESCAPE_BYTE);
				messageList.AddLast((byte)(checksum ^ 0x20));
			}
			else
			{
				messageList.AddLast(checksum);
			}

			return messageList.ToArray<byte>();
		}


		public byte[] Escape(byte[] Frame_To_Escape)
		{
			//List<byte> list = Frame_To_Escape.ToList<byte>();

			LinkedList<byte> list = new LinkedList<byte>(Frame_To_Escape);

			for (int i = 1; i < list.Count; i++)    //skip the first index to avoid the start
			{
				if (BYTES_TO_ESCAPE.Contains(list.ElementAt(i)))
				{
					LinkedListNode<byte> mark = list.getNodeAt(i);
					byte value = list.ElementAt(i);

					list.AddBefore(mark, ESCAPE_BYTE);
					list.AddAfter(mark, (byte)(value ^ 0x20));
					list.Remove(mark);
				}
			}

			return list.ToArray();
		}

		bool escapeCarryOver = false;

		//XXXX rename
		public byte[] DeEscape(byte[] Frame_To_DeEscape)
		{
			Queue<byte> temp = new Queue<byte>();
			byte[] data = new byte[Frame_To_DeEscape.Length * 2];


			for (int i = 0; i < Frame_To_DeEscape.Length; i++)
			{
				if (Frame_To_DeEscape[i] == 0x7D)
				{
					i++;

					if (i >= Frame_To_DeEscape.Length)
					{
						escapeCarryOver = true;
					}
					else
					{
						temp.Enqueue((byte)(Frame_To_DeEscape[i] ^ 0x20));
					}
				}
				else if (escapeCarryOver)
				{
					temp.Enqueue((byte)(Frame_To_DeEscape[i] ^ 0x20));
					escapeCarryOver = false;
				}
				else
				{
					temp.Enqueue(Frame_To_DeEscape[i]);
				}
			}

			return temp.ToArray();
		}




		public byte CalculateChecksum(byte api, byte[] data)
		{
			int checksum_int = api;

			for (int a = 0; a < data.Length; a++)
			{
				checksum_int += data[a];
			}

			return (byte)(0xFF - checksum_int);
		}
		public byte CalculateChecksum(byte[] data)  //only for data of msg
		{
			int checksum_int = 0;

			for (int a = 3; a < data.Length; a++)
			{
				checksum_int += data[a];
			}

			return (byte)(0xFF - checksum_int);
		}


		public void SendFrame(byte api_frame, byte[] data)
		{

		}


		public void SendTransmitRequest(UInt64 destination, byte[] data)
		{
			byte[] frame_data = new byte[data.Length + 14];

			frame_data[0] = API_FRAME.ZIGBEE_TRANSMIT_REQUEST;
			frame_data[1] = 200;

			//byte[] destination_address_64 = BitConverter.GetBytes(destination);
            byte[] destination_address_64 = new byte[8];

            destination_address_64[0] = BitConverter.GetBytes(destination)[7];
            destination_address_64[1] = BitConverter.GetBytes(destination)[6];
            destination_address_64[2] = BitConverter.GetBytes(destination)[5];
            destination_address_64[3] = BitConverter.GetBytes(destination)[4];
            destination_address_64[4] = BitConverter.GetBytes(destination)[3];
            destination_address_64[5] = BitConverter.GetBytes(destination)[2];
            destination_address_64[6] = BitConverter.GetBytes(destination)[1];
            destination_address_64[7] = BitConverter.GetBytes(destination)[0]; 


            //MANSEL: YOU TWAT FIX THIS
			//byte[] destination_address_64 = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF };
			destination_address_64.CopyTo(frame_data, 2);
			byte[] destination_address_16 = { 0xFF, 0xFE };
			destination_address_16.CopyTo(frame_data, 10);

			frame_data[12] = 0x00;  //broadcast options

			frame_data[13] = 0x00;  //options

			data.CopyTo(frame_data, 14);

			//frame_data[data.Length + 14] = CalculateChecksum(frame_data);


			//serial._serial XbeeFrame(frame_data);
			//SerialUARTCommunication serial
			try
			{
				window.serial.SendByteArray(XbeeFrame(frame_data));
			}
			catch (Exception excpt)
			{
				MessageBox.Show(excpt.Message);
			}
		}
		public void SendTransmitRequest(UInt64 destination, byte data)
		{
			byte[] frame_data = new byte[15];

			frame_data[0] = API_FRAME.ZIGBEE_TRANSMIT_REQUEST;
			frame_data[1] = 200;

			//byte[] destination_address_64 = BitConverter.GetBytes(destination);

			//destination_address_64.CopyTo(frame_data, 2);

			frame_data[8] = 0xFF;
			frame_data[9] = 0xFF;

			byte[] destination_address_16 = { 0xFF, 0xFE };

			destination_address_16.CopyTo(frame_data, 10);

			frame_data[12] = 0x00;  //broadcast options

			frame_data[13] = 0x00;  //options

			frame_data[14] = data;

			//serial._serial XbeeFrame(frame_data);
			//SerialUARTCommunication serial
			try
			{
				window.serial.SendByteArray(XbeeFrame(frame_data));
			}
			catch (Exception excpt)
			{
				MessageBox.Show(excpt.Message);
			}

		}
	}
}