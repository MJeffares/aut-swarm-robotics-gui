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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

#endregion



/**********************************************************************************************************************************************
* Structures and Classes
**********************************************************************************************************************************************/
#region



public class XbeeHandler
{

	private const byte FRAME_DELIMITER = 0x7E;
	private const byte ESCAPE_BYTE = 0x7D;
	private const byte XON = 0x11;
	private const byte XOFF = 0x13;

	private static readonly IList<byte> BYTES_TO_ESCAPE = new List<byte> { 0x7E, 0x7D, 0x11, 0x13 }.AsReadOnly();


	private static class API_FRAME
	{
		const byte AT_COMMAND = 0x08;
		const byte AT_COMMAND_QUEUE = 0x09;
		const byte ZIGBEE_TRANSMIT_REQUEST = 0x10;
		const byte EXPLICIT_ADDRESSING_ZIGBEE_COMMAND_FRAME = 0x11;
		const byte REMOTE_COMMAND_REQUEST = 0x17;
		const byte CREATE_SOURCE_ROUTE = 0x21;
		const byte AT_COMMAND_RESPONSE = 0x88;
		const byte MODEM_STATUS = 0x8A;
		const byte ZIGBEE_TRANSMIT_STATUS = 0x8B;
		const byte ZIGBEE_RECEIVE_PACKET = 0x90;
		const byte ZIGBEE_EXPLICIT_RX_INDICATOR = 0x91;
		const byte ZIGBEE_IO_DATA_SAMPLE_RX_INDICATOR = 0x92;
		const byte XBEE_SENSOR_READ_INDICATOR = 0x94;
		const byte NODE_IDENTIFICATION_INDICATOR = 0x95;
		const byte REMOTE_COMMAND_RESPONSE = 0x97;
		const byte EXTENDED_MODEM_STATUS = 0x98;
		const byte OTA_FIRMWARE_UPDATE_STATUS = 0xA0;
		const byte ROUTE_RECORD_INDICATOR = 0xA1;
		const byte MANY_TO_ONE_ROUTE_REQUEST_INDICATOR = 0xA3;
	}

	public static class FRAME_STATUS
	{
		const byte CHECKSUM_ERROR = 1;
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
				list.AddAfter(mark, (byte)(value^0x20));
				list.Remove(mark);
			}
		}

		return list.ToArray();
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
		checksum = (byte)(checksum_int & 0xFF);


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



	/*
	public byte CalculateChecksum()
	{
		int checksum_int = api_identifier;

		for(int a = 0; a < data.Length; a++)
		{
			checksum_int += data[a];
		}

		return (byte)(checksum_int & 0xFF);
	}
	*/




}






#endregion
