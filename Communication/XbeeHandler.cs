/**********************************************************************************************************************************************
*	File: XbeeHandler.cs
*
*	Developed By: Mansel Jeffares
*	First Build: 31 July
*	Current Build: 12 September 2017
*
*	Description :
*		Xbee communication classes and methods 
*		Developed for a final year university project at Auckland University of Technology (AUT), New Zealand
*		
*	Useage :
*		
*
*	Limitations :
*		Build for x64, .NET 4.5.2
*   
*		Naming Conventions:
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
using XbeeHandler.XbeeFrames;


#endregion



namespace XbeeHandler
{
	public class XbeeAPI
	{
		/**********************************************************************************************************************************************
		* Class private fields
		**********************************************************************************************************************************************/
		#region 

		private const byte FRAME_DELIMITER = 0x7E;
		private const byte ESCAPE_BYTE = 0x7D;
		private const byte XON = 0x11;
		private const byte XOFF = 0x13;

		Predicate<byte> isStartByte = (byte b) => { return b == 0x7E; };
		int _receiveState = ReceiveStates.START;
		int index = 0;
		int startIndex = -1;
		int length;
		bool escape;
		List<byte> frameAsList;
		MainWindow window = null;

		private static readonly IList<byte> BYTES_TO_ESCAPE = new List<byte> { FRAME_DELIMITER, ESCAPE_BYTE, XON, XOFF }.AsReadOnly();

		bool escapeCarryOver = false;

		#endregion

		/**********************************************************************************************************************************************
		* Constructor
		**********************************************************************************************************************************************/
		public XbeeAPI(MainWindow main)
		{
			window = main;
		}

		/**********************************************************************************************************************************************
		* Child Classes
		**********************************************************************************************************************************************/
		#region

		public static class DESTINATION
		{
			public const UInt64 COORDINATOR = 0x0000000000000000;
			public const UInt64 BROADCAST = 0x000000000000FFFF;

			public const UInt64 BROWN_ROBOT = 0x0013A20041065FB3;
			public const UInt64 DARK_BLUE_ROBOT = 0x0013A200415B8C3A;
			public const UInt64 GREEN_TOWER = 0x0013A200415B8C2A;
			public const UInt64 LIGHT_BLUE_ROBOT = 0x0013A2004152F256;
			public const UInt64 ORANGE_ROBOT = 0x0013A200415B8BE5;
			public const UInt64 PINK_ROBOT = 0x0013A200415B8C18;
			public const UInt64 PURPLE_ROBOT = 0x0013A200415B8BDD;
			public const UInt64 RED_ROBOT = 0x0013A2004147F9DD;
			public const UInt64 YELLOW_ROBOT = 0x0013A200415B8C38;


			public static string ToString(UInt64 location)
			{
				switch (location)
				{
					case COORDINATOR:
						return "PC/GUI";

					case BROADCAST:
						return "Broadcast Message";

					case BROWN_ROBOT:
						return "Brown Robot";

					case DARK_BLUE_ROBOT:
						return "Dark Blue Robot";

					case GREEN_TOWER:
						return "Tower Base Station";

					case LIGHT_BLUE_ROBOT:
						return "Light Blue Robot";

					case ORANGE_ROBOT:
						return "Orange Robot";

					case PINK_ROBOT:
						return "Pink Robot";

					case PURPLE_ROBOT:
						return "Purple Robot";

					case RED_ROBOT:
						return "Red Robot";

					case YELLOW_ROBOT:
						return "Yellow Robot";

					default:
						return "Warning: Unknown Desstination";
				}
			}
		}

		static private class ReceiveStates
		{
			public const int START = 0;
			public const int LENGTH_MSB = 1;
			public const int LENGTH_LSB = 2;
			public const int Data = 3;
		}

		#endregion

		/**********************************************************************************************************************************************
		* Methods
		**********************************************************************************************************************************************/
		#region

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

		public XbeeAPIFrame ParseXbeeFrame(byte[] frame)
		{
			XbeeAPIFrame specificFrame;

			switch (frame[3])
			{

				case API_FRAME_TYPES.AT_COMMAND_RESPONSE:
					//window.UpdateSerialReceivedTextBox("\rXBEE: AT Command Response Received (N/H)");
					specificFrame = null;
					break;

				case API_FRAME_TYPES.MODEM_STATUS:
					//window.UpdateSerialReceivedTextBox("\rXBEE: Modem Status Received (N/H)");
					specificFrame = null;
					break;


				case API_FRAME_TYPES.ZIGBEE_TRANSMIT_STATUS:
					//window.UpdateSerialReceivedTextBox("\rXBEE: Transmit Status Received (N/H)");
					specificFrame = new ZigbeeTransmitStatus(frame);
					break;


				case API_FRAME_TYPES.ZIGBEE_RECEIVE_PACKET:
					//window.UpdateSerialReceivedTextBox("\rXBEE: Data Packet Received ");
					specificFrame = new ZigbeeReceivePacket(frame);
					break;


				case API_FRAME_TYPES.ZIGBEE_EXPLICIT_RX_INDICATOR:
					//window.UpdateSerialReceivedTextBox("\rXBEE: Explicit Data Packet Received (N/H)");
					specificFrame = null;
					break;


				case API_FRAME_TYPES.ZIGBEE_IO_DATA_SAMPLE_RX_INDICATOR:
					//window.UpdateSerialReceivedTextBox("\rXBEE: IO Sample Received (N/H)");		
					specificFrame = null;
					break;


				case API_FRAME_TYPES.XBEE_SENSOR_READ_INDICATOR:
					//window.UpdateSerialReceivedTextBox("\rXBEE: Sensor Read Indicator Received (N/H)");	
					specificFrame = null;
					break;


				case API_FRAME_TYPES.NODE_IDENTIFICATION_INDICATOR:
					//window.UpdateSerialReceivedTextBox("\rXBEE: Node Identification Indicator Received (N/H)");
					specificFrame = null;
					break;


				case API_FRAME_TYPES.REMOTE_COMMAND_RESPONSE:
					//window.UpdateSerialReceivedTextBox("\rXBEE: Remote Command Response Received (N/H)");
					specificFrame = null;
					break;


				case API_FRAME_TYPES.EXTENDED_MODEM_STATUS:
					//window.UpdateSerialReceivedTextBox("\rXBEE: Extended Modem Status Received (N/H)");
					specificFrame = null;
					break;


				case API_FRAME_TYPES.OTA_FIRMWARE_UPDATE_STATUS:
					//window.UpdateSerialReceivedTextBox("\rXBEE: OTA Firmware Update Status Received (N/H)");
					specificFrame = null;
					break;


				case API_FRAME_TYPES.ROUTE_RECORD_INDICATOR:
					//window.UpdateSerialReceivedTextBox("\rXBEE: Route Record Indicator Received (N/H)");
					specificFrame = null;
					break;


				case API_FRAME_TYPES.MANY_TO_ONE_ROUTE_REQUEST_INDICATOR:
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
			//MANSEL: Why is this empty
		}


		public void SendTransmitRequest(UInt64 destination, byte[] data)
		{
			byte[] frame_data = new byte[data.Length + 14];

			frame_data[0] = API_FRAME_TYPES.ZIGBEE_TRANSMIT_REQUEST;
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

		#endregion

	}
}