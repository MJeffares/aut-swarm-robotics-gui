/**********************************************************************************************************************************************
*	File: SerialCommunication.cs
*
*	Developed By: Mansel Jeffares
*	First Build: 29 March 2017
*	Current Build:  27 April 2017
*
*	Description :
*		Serial communication methods and functions for Swarm Robotics Project
*		Built for x64, .NET 4.5.2
*		
*	Useage :
*		
*
*	Limitations :
*		Build for x64
*		1.5 Stopbits doesn't work unless also using 5 databits
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
using System.Windows.Threading;

#endregion



/**********************************************************************************************************************************************
* Structures and Classes
**********************************************************************************************************************************************/
#region


public class SerialUARTCommunication
{
	

	//supported serial port settings
	private string[] baudRateOptions = new string[] { "4800", "9600", "19200", "38400", "57600", "115200", "230400", "460800", "921600" };
	private string[] parityOptions = new string[] { "None", "Odd", "Even", "Mark", "Space" };
	private string[] dataBitOptions = new string[] { "8", "7", "6", "5" };
	private string[] stopBitOptions = new string[] { "None", "One", "One Point Five", "Two" };
	private string[] handshakingOptions = new string[] { "None", "XOnXOff", "RequestToSend", "RequestToSendXOnXOff" };


	//default serial port settings
	private const string DEFAULT_BAUD_RATE = "9600";
	private const string DEFAULT_PARITY = "None";
	private const string DEFAULT_DATA_BITS = "8";
	private const string DEFAULT_STOP_BITS = "One";
	private const string DEFAULT_HANDSHAKING = "None";


	//main window
	MainWindow window = null;

	//menu Items
	MenuItem portList = null;
	MenuItem baudList = null;
	MenuItem parityList = null;
	MenuItem dataBitsList = null;
	MenuItem stopBitsList = null;
	MenuItem handshakingList = null;
	MenuItem connectButton = null;

	public SerialPort _serialPort;
	public Queue<byte> rxBuffer;

	private string currentlyConnectedPort = null;

	private DispatcherTimer ReceiveTimer = new DispatcherTimer();   // one second timer to calculate and update the fps count


	public SerialUARTCommunication(MainWindow main)
	{
		window = main;

		rxBuffer = new Queue<byte>();

		_serialPort = new SerialPort();
		_serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

		//ReceiveTimer.Tick += ReceiveTimerTick;
		//ReceiveTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
		//ReceiveTimer.Start();

	}
	public SerialUARTCommunication(MainWindow main, MenuItem port, MenuItem baud, MenuItem parity, MenuItem data, MenuItem stop, MenuItem handshaking, MenuItem connect)
	{
		window = main;
		portList = port;
		baudList = baud;
		parityList = parity;
		dataBitsList = data;
		stopBitsList = stop;
		handshakingList = handshaking;
		connectButton = connect;

		rxBuffer = new Queue<byte>();
		_serialPort = new SerialPort();

		PopulateSerialSettings();
		PopulateSerialPorts();

		portList.MouseEnter += new MouseEventHandler(PopulateSerialPorts);
		connectButton.Click += new RoutedEventHandler(menuCommunicationConnect_Click);
				
		_serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

		//ReceiveTimer.Tick += ReceiveTimerTick;
		//ReceiveTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
		//ReceiveTimer.Start();
	}


	public void sendByteArray(byte[] msg)
	{
		if (_serialPort.IsOpen)
		{
			_serialPort.Write(msg, 0, msg.Length);
		}
	}

	public void sendString(String msg)
	{
		if (_serialPort.IsOpen)
		{
			_serialPort.Write(msg);
		}
	}

	private void PopulateSerialSettings()
	{
		MJLib.PopulateMenuItemList(baudList, baudRateOptions, DEFAULT_BAUD_RATE, MJLib.menuMutuallyExclusiveMenuItem_Click);
		MJLib.PopulateMenuItemList(parityList, parityOptions, DEFAULT_PARITY, MJLib.menuMutuallyExclusiveMenuItem_Click);
		MJLib.PopulateMenuItemList(dataBitsList, dataBitOptions, DEFAULT_DATA_BITS, MJLib.menuMutuallyExclusiveMenuItem_Click);
		MJLib.PopulateMenuItemList(stopBitsList, stopBitOptions, DEFAULT_STOP_BITS, MJLib.menuMutuallyExclusiveMenuItem_Click);
		MJLib.PopulateMenuItemList(handshakingList, handshakingOptions, DEFAULT_HANDSHAKING, MJLib.menuMutuallyExclusiveMenuItem_Click);
	}

	public void PopulateSerialPorts()
	{
		connectButton.IsEnabled = false;
		string[] ports = SerialPort.GetPortNames();
		portList.Items.Clear();

		for (int i = 0; i < ports.Length; i++)
		{
			MenuItem item = new MenuItem { Header = ports[i] };
			item.Click += new RoutedEventHandler(menuCommunicationPortListItem_Click);
			item.IsCheckable = true;

			portList.Items.Add(item);

			if(item.ToString() == currentlyConnectedPort)
			{
				item.IsChecked = true;
				connectButton.IsEnabled = true;
			}
		}

		if (portList.Items.Count == 0)
		{
			MenuItem nonefound = new MenuItem { Header = "No Com Ports Found" };
			portList.Items.Add(nonefound);
			nonefound.IsEnabled = false;
			connectButton.IsEnabled = false;
		}
	}
	public void PopulateSerialPorts(object sender, MouseEventArgs e)
	{
		connectButton.IsEnabled = false;
		string[] ports = SerialPort.GetPortNames();
		portList.Items.Clear();

		for (int i = 0; i < ports.Length; i++)
		{
			MenuItem item = new MenuItem { Header = ports[i] };
			item.Click += new RoutedEventHandler(menuCommunicationPortListItem_Click);
			item.IsCheckable = true;

			portList.Items.Add(item);

			if (item.ToString() == currentlyConnectedPort)
			{
				item.IsChecked = true;
				connectButton.IsEnabled = true;
			}
		}

		if (portList.Items.Count == 0)
		{
			MenuItem nonefound = new MenuItem { Header = "No Com Ports Found" };
			portList.Items.Add(nonefound);
			nonefound.IsEnabled = false;
			connectButton.IsEnabled = false;
		}
	}



	private void menuCommunicationPortListItem_Click(object sender, RoutedEventArgs e)
	{
		MJLib.menuMutuallyExclusiveMenuItem_Click(sender, e);
		connectButton.IsEnabled = true;
		currentlyConnectedPort = sender.ToString();
	}



	public void menuCommunicationConnect_Click(object sender, RoutedEventArgs e)
	{

		if (!_serialPort.IsOpen)
		{
			//get all the serial port settings (we poll this once when we attempt to connect, this instead could be done by events when each button is pressed)
			MenuItem port = MJLib.GetCheckedItemInList(portList, true);
			_serialPort.PortName = port.Header.ToString();

			MenuItem baud = MJLib.GetCheckedItemInList(baudList, true);
			_serialPort.BaudRate = int.Parse(baud.Header.ToString());

			MenuItem parity = MJLib.GetCheckedItemInList(parityList, true);
			_serialPort.Parity = (Parity)Enum.Parse(typeof(Parity), parity.Header.ToString(), true);

			MenuItem data = MJLib.GetCheckedItemInList(dataBitsList, true);
			_serialPort.DataBits = Convert.ToInt32(data.Header.ToString());

			MenuItem stop = MJLib.GetCheckedItemInList(stopBitsList, true);
			string str1 = null;
			string str2 = null;
			str1 = stop.Header.ToString();
			str2 = Regex.Replace(stop.Header.ToString(), @"\s+", "");                               //we need to remove spaces
			_serialPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), str2, true);

			MenuItem handshake = MJLib.GetCheckedItemInList(handshakingList, true);
			_serialPort.Handshake = (Handshake)Enum.Parse(typeof(Handshake), handshake.Header.ToString(), true);

			try
			{
				_serialPort.Open();
				connectButton.Header = "Disconnect";
			}
			catch (Exception excpt)
			{
				MessageBox.Show(excpt.ToString());
			}
		}
		else    //if the serial port is currently open
		{
			try
			{
				_serialPort.Close();
			}
			catch (Exception excpt)
			{
				MessageBox.Show(excpt.ToString());
			}


			//creates a list with all settings in it
			MenuItem[] ports = portList.Items.OfType<MenuItem>().ToArray();
			MenuItem[] bauds =baudList.Items.OfType<MenuItem>().ToArray();
			MenuItem[] parity = parityList.Items.OfType<MenuItem>().ToArray();
			MenuItem[] data = dataBitsList.Items.OfType<MenuItem>().ToArray();
			MenuItem[] stops = stopBitsList.Items.OfType<MenuItem>().ToArray();
			MenuItem[] handshakes = handshakingList.Items.OfType<MenuItem>().ToArray();

			List<MenuItem> itemList = new List<MenuItem>(ports.Concat<MenuItem>(ports));
			itemList.AddRange(bauds);
			itemList.AddRange(parity);
			itemList.AddRange(data);
			itemList.AddRange(stops);
			itemList.AddRange(handshakes);

			MenuItem[] finalArray = itemList.ToArray();

			//re-enables all settings
			foreach (var item in finalArray)
			{
				item.IsEnabled = true;
			}

			//updates connect button
			connectButton.Header = "Connect";
			connectButton.IsChecked = false;
		}

	}

	public void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
	{
		
		SerialPort sp = (SerialPort)sender;
		int bytes = sp.BytesToRead;

		byte[] indata = new byte[bytes];

		sp.Read(indata, 0, bytes);


		indata = window.xbee.DeEscape(indata);
		bytes = indata.Length;


		for(int i = 0; i < indata.Length; i++)
		{
			rxBuffer.Enqueue(indata[i]);
		}

		/*
		byte[] indata = new byte[bytes*2];	//for worst case with all escaped character
		
		sp.Read(indata, 0, bytes);

		for(int i = 0; i < indata.Length; i++)
		{
			if (indata[i] == 0x7D)
			{
				i++;
				rxBuffer.Enqueue((byte)(indata[i] ^ 0x20));
			}
			else
			{
				rxBuffer.Enqueue(indata[i]);
			}
		}
		*/
		
		//avoid's threading error
		//rtbSerialReceived.Dispatcher.Invoke(new UpdateTextCallback(this.UpdateText), new object[] { indata, bytes });
		window.UpdateSerialReceivedTextBox(indata, bytes);

		window.xbee.InterperateXbeeFrame();


		//start thread to process on the receive side
		//XXXX

		//ReceiveTimer.Stop();
		//ReceiveTimer.Start();
		/*
		for(int i = 0; i < 100; i++)
		{
			if (window.xbee.ReceiveMessage() != null)
			{
				window.UpdateSerialReceivedTextBox("\rXbee Message Received\r");
				i = 100;
			}
			else
			{

			}
		}
		
		while(window.xbee.ReceiveMessage() == null)
		{

		}
		*/

	}


	
	/*

	private int receiveMaxCount;

	private void ReceiveTimerTick(object sender, EventArgs arg)
	{
		if(receiveMaxCount > 1000)
		{
			ReceiveTimer.Stop();
			receiveMaxCount = 0;
			window.UpdateSerialReceivedTextBox("\rMessage Receive Attempt Timeout\r");
		}

		byte[] xbeeData = window.xbee.ReceiveMessage();


		if (xbeeData != null)
		{
			

			if(xbeeData[0] == 0x90) //Received message
			{
				window.UpdateSerialReceivedTextBox("\rXbee Message Received (Receive Packet Frame)\r");

				byte[] rawMessage = new byte[xbeeData.Length - 12];

				Array.Copy(xbeeData, 12, rawMessage, 0, xbeeData.Length - 12);

				//XXXX
				//call swarm serial function passing "xbeeData" too it
				window.protocol.MessageReceived(rawMessage);
				//window.protocol.MessageReceived(xbeeData);

			}
			else
			{
				window.UpdateSerialReceivedTextBox("\rXbee Not Implemented Message Received\r");
			}

			


			ReceiveTimer.Stop();
			receiveMaxCount = 0;
		}


		receiveMaxCount++;
	}
	*/
}


#endregion



namespace SwarmRoboticsGUI
{
	public partial class MainWindow : Window
	{
	

		public delegate void UpdateTextCallback(string text);

		public void UpdateSerialReceivedTextBox(string text)
		{
			rtbSerialReceived.Dispatcher.Invoke(new UpdateTextCallback(this.UpdateText), new object[] { text });
		}

		public void UpdateSerialReceivedTextBox(byte[] message, int number)
		{
			string messageString = null;

			for (int i = 0; i < number; i++)
			{
				//messageString += Encoding.ASCII.GetString(message,0,1);	

				string temp = message[i].ToString("X");

				if (temp == "7E")
				{
					messageString += "\r";
					messageString += temp;
				}
				else if (message[i] < 0x10)
				{
					messageString += "0";
					messageString += temp;
				}
				else
				{
					messageString += temp;
				}


				

				
				/*
				if ((i % 2) == 0)
				{
					messageString += "-";
				}
				*/


			}
			rtbSerialReceived.Dispatcher.Invoke(new UpdateTextCallback(this.UpdateText), new object[] { messageString });
			
		}
		
		private void UpdateText(string text)
		{
			rtbSerialReceived.AppendText(text);
			rtbSerialReceived.ScrollToEnd();
		}

		

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			if (serial._serialPort.IsOpen)
			{
				byte test = 0 ; //


				rtbSendBuffer.SelectAll();
				string text = rtbSendBuffer.Selection.Text.ToString();
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
					

					serial._serialPort.Write(bytes, 0, bytes.Length);
				}
				catch (Exception excpt)
				{
					MessageBox.Show(excpt.Message);
				}


				rtbSerialSent.AppendText(text);
				//rtbSerialSent.AppendText(test.ToString()); //
				rtbSendBuffer.Document.Blocks.Clear();
				rtbSerialSent.ScrollToEnd();
			}
			else
			{
				MessageBox.Show("Port not open");
			}
		}


		private void receivedDataNewline_Click(object sender, RoutedEventArgs e)
		{
			rtbSerialReceived.AppendText("\r");
			rtbSerialReceived.ScrollToEnd(); ;
		}


		private void receivedDataClear_Click(object sender, RoutedEventArgs e)
		{
			rtbSerialReceived.Document.Blocks.Clear();
			rtbSerialReceived.ScrollToEnd(); ;
		}

	}
}
