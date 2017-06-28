/**********************************************************************************************************************************************
*	File: XbeeCommunication.cs
*
*	Developed By: Mansel Jeffares
*	First Build: 27 April 2017
*	Current Build:  27 April 2017
*
*	Description :
*		Xbee communication methods and functions for Swarm Robotics Project
*		Built for x64, .NET 4.5.2
*		
*	Useage :
*		
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
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

#endregion



/***************************************************************************************************************************************
* Structures and Classes
**********************************************************************************************************************************************/
#region



public class XbeeHandler
{
	MainWindow window = null;


	private const byte FRAME_DELIMITER = 0x7E;
	private const byte ESCAPE_BYTE = 0x7D;
	private const byte XON = 0x11;
	private const byte XOFF = 0x13;

	private static readonly IList<byte> BYTES_TO_ESCAPE = new List<byte> { 0x7E, 0x7D, 0x11, 0x13 }.AsReadOnly();

	public XbeeHandler(MainWindow main)
	{
		window = main;
	}


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


	public static class DESTINATION
	{
		private const UInt32 SERIAL_NUMBER_HIGH = 0x0013A200;


		public const UInt64 COORDINATOR = 0x0000000000000000;
		public const UInt64 BROADCAST = 0x000000000000FFFF;

		public const UInt64 ROBOT_ONE = SERIAL_NUMBER_HIGH * 2 ^ 32 + 0x41065FB3;
	}

	public static class FRAME_STATUS
	{
		const byte CHECKSUM_ERROR = 1;
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
			else if(escapeCarryOver)
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

		byte[] destination_address_64 = BitConverter.GetBytes(destination);

		destination_address_64.CopyTo(frame_data, 2);

		byte[] destination_address_16 = { 0xFF, 0xFE };

		destination_address_16.CopyTo(frame_data, 10);

		frame_data[12] = 0x00;  //broadcast options

		frame_data[13] = 0x00;  //options

		data.CopyTo(frame_data, 14);

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



	int _receiveState = ReceiveStates.START;

	static private class ReceiveStates
	{
		public const int START = 0;
		public const int LENGTH_MSB = 1;
		public const int LENGTH_LSB = 2;
		public const int WAIT = 3;
		public const int DATA = 4;

	}

	int length;
	byte[] data;
	int check;

	//public async Task<byte[]> ReceiveMessage()
	public byte[] ReceiveMessage()
	{

		while (window.serial.rxBuffer.Count >= 1)
		{
			if (window.serial.rxBuffer.Peek() == FRAME_DELIMITER && _receiveState != ReceiveStates.START)
			{
				_receiveState = ReceiveStates.START;

				//XXXX error in last message received as not fully received
				//check if state isnt currently start. If its not we got a fragmented message
			}


			switch (_receiveState)
			{
				case ReceiveStates.START:
					if (window.serial.rxBuffer.Dequeue() == FRAME_DELIMITER)
					{
						_receiveState = ReceiveStates.LENGTH_MSB;
					}
					break;

				case ReceiveStates.LENGTH_MSB:
					length = 0;
					length = window.serial.rxBuffer.Dequeue() * 256;
					_receiveState = ReceiveStates.LENGTH_LSB;
					break;

				case ReceiveStates.LENGTH_LSB:
					length +=  window.serial.rxBuffer.Dequeue();
					data = new byte[length];
					check = 0;
					_receiveState = ReceiveStates.WAIT;
					break;

				case ReceiveStates.WAIT:
					if (window.serial.rxBuffer.Count >= length + 1)
					{
						_receiveState = ReceiveStates.DATA;
					}
					else
					{
						return null;
					}
					break;

				case ReceiveStates.DATA:
					for (int i = 0; i < length; i++)
					{
						data[i] = window.serial.rxBuffer.Dequeue();
						check += data[i];
					}


					check += window.serial.rxBuffer.Dequeue();

					if ((byte)(check) == 0xFF)
					{
						//window.UpdateSerialReceivedTextBox("\rXbee Message Received\r");
						_receiveState = ReceiveStates.START;
						return data;

					}

					break;
			}


		}

		return null;

		/*
		while (window.serial.rxBuffer.Dequeue() != FRAME_DELIMITER)
		{
			//await Task.Delay(10);
		}


		while (window.serial.rxBuffer.Count < 3)
		{
			//await Task.Delay(10);
		}

		int length = window.serial.rxBuffer.Dequeue() * 256 + window.serial.rxBuffer.Dequeue();
		byte[] data = new byte[length];
		int check = 0;

		while (window.serial.rxBuffer.Count < length + 1)
		{
			//await Task.Delay(10);
		}

		for (int i = 0; i < length; i++)
		{
			data[i] = window.serial.rxBuffer.Dequeue();
			check += data[i];
		}

		byte checksum = window.serial.rxBuffer.Dequeue();
		check += checksum;

		if ((byte)(check) == 0xFF)
		{
			window.UpdateSerialReceivedTextBox("\rXbee Message Received\r");

			return data;

		}
		return null;
		*/

	}







	public void InterperateXbeeFrame()
	{
		byte[] xbeeData = ReceiveMessage();
		
		if (xbeeData != null)
		{

			switch (xbeeData[0])
			{

				case API_FRAME.AT_COMMAND_RESPONSE:
					window.UpdateSerialReceivedTextBox("\rXBEE: AT Command Response Received (N/H)");
					break;


				case API_FRAME.MODEM_STATUS:
					window.UpdateSerialReceivedTextBox("\rXBEE: Modem Status Received (N/H)");
					break;


				case API_FRAME.ZIGBEE_TRANSMIT_STATUS:
					window.UpdateSerialReceivedTextBox("\rXBEE: Transmit Status Received (N/H)");
					break;


				case API_FRAME.ZIGBEE_RECEIVE_PACKET:

					window.UpdateSerialReceivedTextBox("\rXBEE: Data Packet Received ");

					byte[] rawMessage = new byte[xbeeData.Length - 12];

					Array.Copy(xbeeData, 12, rawMessage, 0, xbeeData.Length - 12);

					//XXXX
					//call swarm serial function passing "xbeeData" too it
					window.protocol.MessageReceived(rawMessage);
					//window.protocol.MessageReceived(xbeeData);

					break;


				case API_FRAME.ZIGBEE_EXPLICIT_RX_INDICATOR:
					window.UpdateSerialReceivedTextBox("\rXBEE: Explicit Data Packet Received (N/H)");
					break;


				case API_FRAME.ZIGBEE_IO_DATA_SAMPLE_RX_INDICATOR:
					window.UpdateSerialReceivedTextBox("\rXBEE: IO Sample Received (N/H)");		
					break;


				case API_FRAME.XBEE_SENSOR_READ_INDICATOR:
					window.UpdateSerialReceivedTextBox("\rXBEE: Sensor Read Indicator Received (N/H)");		
					break;


				case API_FRAME.NODE_IDENTIFICATION_INDICATOR:
					window.UpdateSerialReceivedTextBox("\rXBEE: Node Identification Indicator Received (N/H)");
					break;


				case API_FRAME.REMOTE_COMMAND_RESPONSE:
					window.UpdateSerialReceivedTextBox("\rXBEE: Remote Command Response Received (N/H)");
					break;


				case API_FRAME.EXTENDED_MODEM_STATUS:
					window.UpdateSerialReceivedTextBox("\rXBEE: Extended Modem Status Received (N/H)");
					break;


				case API_FRAME.OTA_FIRMWARE_UPDATE_STATUS:
					window.UpdateSerialReceivedTextBox("\rXBEE: OTA Firmware Update Status Received (N/H)");
					break;


				case API_FRAME.ROUTE_RECORD_INDICATOR:
					window.UpdateSerialReceivedTextBox("\rXBEE: Route Record Indicator Received (N/H)");
					break;


				case API_FRAME.MANY_TO_ONE_ROUTE_REQUEST_INDICATOR:
					window.UpdateSerialReceivedTextBox("\rXBEE: Many To One Route Request Indicator Received (N/H)");
					break;

				default:
					window.UpdateSerialReceivedTextBox("\rWARNING ERROR XBEE: unhandled message received");
					break;
			}

		}
	}
}




namespace SwarmRoboticsGUI
{
	public partial class MainWindow : Window
	{

		private void btnXbeeSend_Click(object sender, RoutedEventArgs e)
		{
			if (serial._serialPort.IsOpen)
			{
				//byte test = 0; //



				rtbXbeeSendBuffer.SelectAll();
				string text = rtbXbeeSendBuffer.Selection.Text.ToString();
				string textToSend = text;

				textToSend = textToSend.Replace("\r", string.Empty);
				textToSend = textToSend.Replace("\n", string.Empty);
				textToSend = textToSend.Replace(" ", string.Empty);
				textToSend = textToSend.Replace("-", string.Empty);
				textToSend = textToSend.Replace("0x", string.Empty);

				text = text.Replace("\n", string.Empty);
				text = text.Replace(" ", "-");

				try
				{
					byte[] bytes = bytes = Enumerable.Range(0, textToSend.Length).Where(x => x % 2 == 0).Select(x => Convert.ToByte(textToSend.Substring(x, 2), 16)).ToArray();


					//test = xbee.CalculateChecksum(bytes); //



					//bytes = xbee.Escape(bytes); //escapes bytes

					xbee.SendTransmitRequest(XbeeHandler.DESTINATION.COORDINATOR, bytes);

					//serial._serialPort.Write(bytes, 0, bytes.Length);
				}
				catch (Exception excpt)
				{
					MessageBox.Show(excpt.Message);
				}


				rtbXbeeSent.AppendText(text);
				//rtbSerialSent.AppendText(test.ToString()); 
				rtbXbeeSendBuffer.Document.Blocks.Clear();
				rtbXbeeSent.ScrollToEnd();
			}
			else
			{
				MessageBox.Show("Port not open");
			}
		}

	}
}


#endregion
