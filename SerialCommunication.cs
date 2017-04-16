/**********************************************************************************************************************************************
*	File: SerialCommunication.cs
*
*	Developed By: Mansel Jeffares
*	First Build: 29 March 2017
*	Current Build:  16 April 2017
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

	public SerialUARTCommunication()
	{
		rxBuffer = new Queue<byte>();

		_serialPort = new SerialPort();
		//_serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
	}
	public SerialUARTCommunication(MenuItem port, MenuItem baud, MenuItem parity, MenuItem data, MenuItem stop, MenuItem handshaking, MenuItem connect)
	{
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
				
		//_serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
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
}


#endregion



namespace SwarmRoboticsGUI
{
	public partial class MainWindow : Window
	{
		private static byte[] indata = new byte[100];
		
		public void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
		{
			SerialPort sp = (SerialPort)sender;
			int bytes = sp.BytesToRead;
			//string indata = sp.Read()

			sp.Read(indata, 0, bytes);

			for (int i = 0; i < bytes; i++)
			{
				//rtbSerial.AppendText(indata[i].ToString());	//threading error

				//avoid's threading error
				rtbSerialReceived.Dispatcher.Invoke(new UpdateTextCallback(this.UpdateText), new object[] { indata, bytes });
			}

		}
		
		public delegate void UpdateTextCallback(byte[] message, int number);

		private void UpdateText(byte[] message, int number)
		{
			for (int i = 0; i < number; i++)
			{
				rtbSerialReceived.AppendText(message[i].ToString());
			}
			//rtbSerialReceived.AppendText(Environment.NewLine);
		}



		private void Button_Click(object sender, RoutedEventArgs e)
		{
			rtbSendBuffer.SelectAll();
			string text = rtbSendBuffer.Selection.Text.ToString();
			byte[] bytes = Encoding.ASCII.GetBytes(text);

			serial._serialPort.Write(bytes, 0, bytes.Length);

			rtbSerialSent.AppendText(text);
			rtbSendBuffer.Document.Blocks.Clear();
		}
	}
}
