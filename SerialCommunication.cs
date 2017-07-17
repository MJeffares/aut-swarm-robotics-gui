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
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

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


	//variables
	MainWindow window = null;
	public SerialPort _serialPort;
	public Queue<byte> rxBuffer;
	private string currentlyConnectedPort = null;
	public List<communicated_message> communicatedMessages;
	private communicated_message newestMessage;

	public communicated_message NewestMessage
	{
		get { return newestMessage; }
		set { newestMessage = value; }
	}

	//constructor
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
		communicatedMessages = new List<communicated_message>();

		newestMessage = new communicated_message() { time_stamp = DateTime.Now };
		communicatedMessages.Add(newestMessage);
		window.UpdateListViewBinding(communicatedMessages);
		communicatedMessages.Clear();
		window.RefreshListView();

		PopulateSerialSettings();
		PopulateSerialPorts();

		portList.MouseEnter += new MouseEventHandler(PopulateSerialPorts);
		connectButton.Click += new RoutedEventHandler(menuCommunicationConnect_Click);
				
		_serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

	}

	
	public void SendByteArray(byte[] msg)
	{
		if (_serialPort.IsOpen)
		{
			_serialPort.Write(msg, 0, msg.Length);
		}
	}


	public void SendString(String msg)
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
		if (!_serialPort.IsOpen)
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
	}
	public void PopulateSerialPorts(object sender, MouseEventArgs e)
	{
		if (!_serialPort.IsOpen)
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
			str2 = Regex.Replace(stop.Header.ToString(), @"\s+", "");                               
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


		//avoid's threading error
		//XXXX this will print the raw data to the display
		//window.UpdateSerialReceivedTextBox(indata, bytes);

		newestMessage = new communicated_message() { time_stamp = DateTime.Now, raw_message = indata };
		communicatedMessages.Add(newestMessage);
		//window.lvCommunicatedMessages.ItemsSource = communicatedMessages;
		//window.UpdateListView(communicatedMessages);
		


		window.xbee.InterperateXbeeFrame();
		window.RefreshListView();
	}

}


#endregion



namespace SwarmRoboticsGUI
{
	public class communicated_message
	{
		public DateTime time_stamp { get; set; }

		public byte[] raw_message { get; set; }
		
		public int frame_length { get; set; }
		public byte frame_ID { get; set; }
		public byte[] frame_data { get; set; }

		public byte[] source16 { get; set; }
		public byte[] source64 { get; set; }

		public byte message_type { get; set; }
		public byte[] message_data { get; set; }

		public string TimeStampDisplay
		{
			get
			{
				return time_stamp.ToString("HH:mm:ss");
			}
		}

		public string  RawMessageDisplay
		{
			get
			{
				return MJLib.HexToString(raw_message, 0, frame_length + 4, true);
			}
		}

		public string FrameLengthDisplay
		{
			get
			{
				return MJLib.HexToString(BitConverter.GetBytes(frame_length), 0, 1, true) + " (" + frame_length.ToString() + ")";
			}
		}

		public string FrameIDDisplay
		{
			get
			{
				return XbeeHandler.GetXbeeFrameType(frame_ID) + " (" + MJLib.HexToString(frame_ID, true) + ")";
			}
		}

		public string FrameDataDisplay
		{
			get
			{
				return MJLib.HexToString(frame_data, 0, frame_length, true);
			}
		}

		public string SourceDisplay
		{
			get
			{
				return XbeeHandler.DESTINATION.ToString(BitConverter.ToUInt64(source64, 0)) + " (" + MJLib.HexToString(source64, 0, 8, true) + " , " + MJLib.HexToString(source16, 0, 2, true) + ")";
			}
		}

		public string MessageTypeDisplay
		{
			get
			{
				return ProtocolClass.GetMessageType(message_type) + " (" +  MJLib.HexToString(message_type, true) + ")";
			}
		}

		public string MessageDataDisplay
		{
			get
			{
				return ProtocolClass.GetMessageData(message_type, message_data) + " (" + MJLib.HexToString(message_data, 0, frame_length - 13, true) + ")";
			}
		}
	}



	public partial class MainWindow : Window
	{
		public delegate void RefreshListViewCallback();

		public void RefreshListView()
		{
			lvCommunicatedMessages.Dispatcher.Invoke(new RefreshListViewCallback(this.Refresh));
			
		}


		private GridViewColumnHeader lvCommunicatedMessagesSortCol = null;
		private SortAdorner lvCommunicatedMessagesSortAdorner = null;


		private void Refresh()
		{
			if (lvCommunicatedMessagesSortAdorner != null && lvCommunicatedMessagesSortCol != null)
			{
				lvCommunicatedMessages.Items.SortDescriptions.Add(new SortDescription(lvCommunicatedMessagesSortCol.Tag.ToString(), lvCommunicatedMessagesSortAdorner.Direction));
			}
			
			lvCommunicatedMessages.Items.Refresh();
		}


		//this stuff is temporary XXXX
		
		private void receivedDataRemove_Click(object sender, RoutedEventArgs e)
		{
			gvCommunicatedMessages.Columns.Remove(gvcTimeStamp);
		}

		private void receivedDataAdd_Click(object sender, RoutedEventArgs e)
		{
			gvCommunicatedMessages.Columns.Add(gvcTimeStamp);
		}

		
		
		private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
		{
			GridViewColumnHeader column = (sender as GridViewColumnHeader);
			string sortBy = column.Tag.ToString();

			if(lvCommunicatedMessagesSortCol != null)
			{
				AdornerLayer.GetAdornerLayer(lvCommunicatedMessagesSortCol).Remove(lvCommunicatedMessagesSortAdorner);
				lvCommunicatedMessages.Items.SortDescriptions.Clear();
			}

			ListSortDirection newDir = ListSortDirection.Ascending;
			if (lvCommunicatedMessagesSortCol == column && lvCommunicatedMessagesSortAdorner.Direction == newDir)
				newDir = ListSortDirection.Descending;

			lvCommunicatedMessagesSortCol = column;
			lvCommunicatedMessagesSortAdorner = new SortAdorner(lvCommunicatedMessagesSortCol, newDir);
			AdornerLayer.GetAdornerLayer(lvCommunicatedMessagesSortCol).Add(lvCommunicatedMessagesSortAdorner);
			lvCommunicatedMessages.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
			//column.Width = column.ActualWidth + 10;
			lvCommunicatedMessages.Items.Refresh();
		}

		public class SortAdorner : Adorner
		{
			private static Geometry ascGeometry =
					Geometry.Parse("M 0 4 L 3.5 0 L 7 4 Z");

			private static Geometry descGeometry =
					Geometry.Parse("M 0 0 L 3.5 4 L 7 0 Z");

			public ListSortDirection Direction { get; private set; }

			public SortAdorner(UIElement element, ListSortDirection dir)
					: base(element)
			{
				this.Direction = dir;
			}

			protected override void OnRender(DrawingContext drawingContext)
			{
				base.OnRender(drawingContext);

				if (AdornedElement.RenderSize.Width < 20)
					return;

				TranslateTransform transform = new TranslateTransform
						(
								AdornedElement.RenderSize.Width - 15,
								(AdornedElement.RenderSize.Height - 5) / 2
						);
				drawingContext.PushTransform(transform);

				Geometry geometry = ascGeometry;
				if (this.Direction == ListSortDirection.Descending)
					geometry = descGeometry;
				drawingContext.DrawGeometry(Brushes.Black, null, geometry);

				drawingContext.Pop();
			}
		}
		





		public delegate void UpdateListViewBindingCallback(List<communicated_message> messages);

		public void UpdateListViewBinding(List<communicated_message> messages)
		{
			lvCommunicatedMessages.Dispatcher.Invoke(new UpdateListViewBindingCallback(this.UpdateBinding), new object[] { messages });
		}

		private void UpdateBinding(List<communicated_message> messages)
		{
			DataContext = this;
			lvCommunicatedMessages.ItemsSource = messages;
			lvCommunicatedMessages.Items.Refresh();
		}



		private void Button_Click(object sender, RoutedEventArgs e)
		{
			if (serial._serialPort.IsOpen)
			{
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



		/*
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
			}

			rtbSerialReceived.Dispatcher.Invoke(new UpdateTextCallback(this.UpdateText), new object[] { messageString });
			
		}
		
		private void UpdateText(string text)
		{
			rtbSerialReceived.AppendText(text);
			rtbSerialReceived.ScrollToEnd();
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
		*/
	}
}
