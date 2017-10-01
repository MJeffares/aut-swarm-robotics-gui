/**********************************************************************************************************************************************
*	File: SerialCommunications.cs
*
*	Developed By: Mansel Jeffares
*	First Build: 20 July 2017
*	Current Build:  30 July 2017
*
*	Description :
*		Swrial communication classes methods 
*		Developed for a final year university project at Auckland University of Technology (AUT), New Zealand
*		
*	Useage :
*		
*
*	Limitations :
*		Built for x64, .NET 4.5.2
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
#region Namespaces

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



/**********************************************************************************************************************************************
* Structures and Classes
**********************************************************************************************************************************************/


namespace SwarmRoboticsGUI
{

	public class SerialUARTCommunication
	{


		#region Public Events

		public delegate void SerialDataReceivedHandler(object sender, EventArgs e);
		public event SerialDataReceivedHandler DataReceived;

		#endregion


		#region Public Properties

		public SerialPort _serialPort;
		public List<byte> rxByteBuffer;

        public bool IsConnected { get; private set; }

		#endregion


		#region Private Properties

		// supported serial port settings
		private string[] baudRateOptions = new string[] { "4800", "9600", "19200", "38400", "57600", "115200", "230400", "460800", "921600" };
		private string[] parityOptions = new string[] { "None", "Odd", "Even", "Mark", "Space" };
		private string[] dataBitOptions = new string[] { "8", "7", "6", "5" };
		private string[] stopBitOptions = new string[] { "None", "One", "One Point Five", "Two" };
		private string[] handshakingOptions = new string[] { "None", "XOnXOff", "RequestToSend", "RequestToSendXOnXOff" };

		// default serial port settings
		private const string DEFAULT_BAUD_RATE = "230400";
		private const string DEFAULT_PARITY = "None";
		private const string DEFAULT_DATA_BITS = "8";
		private const string DEFAULT_STOP_BITS = "One";
		private const string DEFAULT_HANDSHAKING = "None";

		// menu controls
		private MenuItem portList { get; set; }
		private MenuItem baudList { get; set; }
        private MenuItem parityList { get; set; }
        private MenuItem dataBitsList { get; set; }
        private MenuItem stopBitsList { get; set; }
        private MenuItem handshakingList { get; set; }
        private MenuItem connectButton { get; set; }

		// variables
		private MainWindow window { get; set; }
		public string currentlyConnectedPort { get; set; }
		#endregion


		//constructor
		public SerialUARTCommunication()
		{
			//window = main;

			rxByteBuffer = new List<byte>();

			_serialPort = new SerialPort();

			//window.RefreshListView();

			//PopulateSerialSettings(serialMenu);
			//PopulateSerialPorts();

			//Binding Event Handlers
			//portList.MouseEnter += new MouseEventHandler(main.menuPopulateSerialPorts);
			//connectButton.Click += new RoutedEventHandler(main.menuCommunicationConnect_Click);
			_serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

		}

		protected virtual void OnDataReceived(EventArgs e)
		{
			if (DataReceived != null)
			{
				DataReceived(this, e);
			}
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


		//private void PopulateSerialSettings(MenuItem mainMenu)
		//{
		//	portList = MJLib.CreateMenuItem("Communication Port");
		//	baudList = MJLib.CreateMenuItem("Baud Rate");
		//	parityList = MJLib.CreateMenuItem("Parity");
		//	dataBitsList = MJLib.CreateMenuItem("Data Bits");
		//	stopBitsList = MJLib.CreateMenuItem("Stop Bits");
		//	handshakingList = MJLib.CreateMenuItem("Handshaking");
		//	connectButton = MJLib.CreateMenuItem("Connect");

		//	connectButton.IsEnabled = false;
		//	connectButton.IsCheckable = true;

		//	mainMenu.Items.Add(portList);
		//	mainMenu.Items.Add(baudList);
		//	mainMenu.Items.Add(parityList);
		//	mainMenu.Items.Add(dataBitsList);
		//	mainMenu.Items.Add(stopBitsList);
		//	mainMenu.Items.Add(handshakingList);
		//	mainMenu.Items.Add(connectButton);

		//	MJLib.PopulateMenuItemList(baudList, baudRateOptions, DEFAULT_BAUD_RATE, MJLib.menuMutuallyExclusiveMenuItem_Click);
		//	MJLib.PopulateMenuItemList(parityList, parityOptions, DEFAULT_PARITY, MJLib.menuMutuallyExclusiveMenuItem_Click);
		//	MJLib.PopulateMenuItemList(dataBitsList, dataBitOptions, DEFAULT_DATA_BITS, MJLib.menuMutuallyExclusiveMenuItem_Click);
		//	MJLib.PopulateMenuItemList(stopBitsList, stopBitOptions, DEFAULT_STOP_BITS, MJLib.menuMutuallyExclusiveMenuItem_Click);
		//	MJLib.PopulateMenuItemList(handshakingList, handshakingOptions, DEFAULT_HANDSHAKING, MJLib.menuMutuallyExclusiveMenuItem_Click);
		//}

        public string[] GetOpenPorts()
        {
            string[] ports = null;

            //if (!_serialPort.IsOpen)
                ports = SerialPort.GetPortNames();

            return ports;
        }

		//public void PopulateSerialPorts()
		//{
		//	if (!_serialPort.IsOpen)
		//	{

		//		connectButton.IsEnabled = false;
		//		string[] ports = SerialPort.GetPortNames();
		//		portList.Items.Clear();

		//		for (int i = 0; i < ports.Length; i++)
		//		{
		//			MenuItem item = new MenuItem { Header = ports[i] };
		//			item.Click += new RoutedEventHandler(menuCommunicationPortListItem_Click);
		//			item.IsCheckable = true;

		//			portList.Items.Add(item);

		//			if (item.ToString() == currentlyConnectedPort)
		//			{
		//				item.IsChecked = true;
		//				connectButton.IsEnabled = true;
		//			}
		//		}

		//		if (portList.Items.Count == 0)
		//		{
		//			MenuItem nonefound = new MenuItem { Header = "No Com Ports Found" };
		//			portList.Items.Add(nonefound);
		//			nonefound.IsEnabled = false;
		//			connectButton.IsEnabled = false;
		//		}
		//	}
		//}

		//public void PopulateSerialPorts(object sender, MouseEventArgs e)
		//{
		//	if (!_serialPort.IsOpen)
		//	{
		//		connectButton.IsEnabled = false;
		//		string[] ports = SerialPort.GetPortNames();
		//		portList.Items.Clear();

		//		for (int i = 0; i < ports.Length; i++)
		//		{
		//			MenuItem item = new MenuItem { Header = ports[i] };
		//			item.Click += new RoutedEventHandler(menuCommunicationPortListItem_Click);
		//			item.IsCheckable = true;

		//			portList.Items.Add(item);

		//			if (item.ToString() == currentlyConnectedPort)
		//			{
		//				item.IsChecked = true;
		//				connectButton.IsEnabled = true;
		//			}
		//		}

		//		if (portList.Items.Count == 0)
		//		{
		//			MenuItem nonefound = new MenuItem { Header = "No Com Ports Found" };
		//			portList.Items.Add(nonefound);
		//			nonefound.IsEnabled = false;
		//			connectButton.IsEnabled = false;
		//		}
		//	}
		//}


		//private void menuCommunicationPortListItem_Click(object sender, RoutedEventArgs e)
		//{
		//	MJLib.menuMutuallyExclusiveMenuItem_Click(sender, e);
		//	connectButton.IsEnabled = true;
		//	currentlyConnectedPort = sender.ToString();
		//}

		public void Connect(string port, string baud, string parity, string data, string stop, string handshake)
		{

			if (!_serialPort.IsOpen)
			{
				//get all the serial port settings (we poll this once when we attempt to connect, this instead could be done by events when each button is pressed)
				//MenuItem port = MJLib.GetCheckedItemInList(portList, true);
				_serialPort.PortName = port;

				//MenuItem baud = MJLib.GetCheckedItemInList(baudList, true);
				_serialPort.BaudRate = int.Parse(baud);

				//MenuItem parity = MJLib.GetCheckedItemInList(parityList, true);
				_serialPort.Parity = (Parity)Enum.Parse(typeof(Parity), parity, true);

				//MenuItem data = MJLib.GetCheckedItemInList(dataBitsList, true);
				_serialPort.DataBits = Convert.ToInt32(data);

				//MenuItem stop = MJLib.GetCheckedItemInList(stopBitsList, true);
				string str1 = null;
				string str2 = null;
				str1 = stop;
				str2 = Regex.Replace(stop, @"\s+", "");
				_serialPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), str2, true);

				//MenuItem handshake = MJLib.GetCheckedItemInList(handshakingList, true);
				_serialPort.Handshake = (Handshake)Enum.Parse(typeof(Handshake), handshake, true);

				try
				{
					_serialPort.Open();
					//connectButton.Header = "Disconnect";

                    IsConnected = true;
                    //foreach (TabItem item in window.tcCenter.Items)
                    //{
                    //	item.IsEnabled = true;
                    //	item.Visibility = Visibility.Visible;
                    //}
                    //window.tcCenter.SelectedIndex++;
                    //window.nc.IsEnabled = false;
                    //window.nc.Visibility = Visibility.Collapsed;

                }
				catch (Exception excpt)
				{
					MessageBox.Show(excpt.ToString());
				}
			}
			

		}

        public void Disconnect()
        {
            if (_serialPort.IsOpen)    //if the serial port is currently open
            {
                try
                {
					IsConnected = false;
					_serialPort.Close();
                }
                catch (Exception excpt)
                {
                    MessageBox.Show(excpt.ToString());
                }


                ////creates a list with all settings in it
                //MenuItem[] ports = portList.Items.OfType<MenuItem>().ToArray();
                //MenuItem[] bauds = baudList.Items.OfType<MenuItem>().ToArray();
                //MenuItem[] paritys = parityList.Items.OfType<MenuItem>().ToArray();
                //MenuItem[] datas = dataBitsList.Items.OfType<MenuItem>().ToArray();
                //MenuItem[] stops = stopBitsList.Items.OfType<MenuItem>().ToArray();
                //MenuItem[] handshakes = handshakingList.Items.OfType<MenuItem>().ToArray();

                //List<MenuItem> itemList = new List<MenuItem>(ports.Concat<MenuItem>(ports));
                //itemList.AddRange(bauds);
                //itemList.AddRange(paritys);
                //itemList.AddRange(datas);
                //itemList.AddRange(stops);
                //itemList.AddRange(handshakes);

                //MenuItem[] finalArray = itemList.ToArray();

                ////re-enables all settings
                //foreach (var item in finalArray)
                //{
                //	item.IsEnabled = true;
                //}

                //updates connect button
                //connectButton.Header = "Connect";
                //connectButton.IsChecked = false;

                //foreach (TabItem item in window.tcCenter.Items)
                //{
                //	item.IsEnabled = false;
                //	item.Visibility = Visibility.Hidden;
                //}
                //window.nc.IsEnabled = true;
                //window.nc.Visibility = Visibility.Visible;
                //window.nc.IsSelected = true;
            }
        }

		public void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
		{
			SerialPort sp = (SerialPort)sender;
			int bytes = sp.BytesToRead;
			byte[] indata = new byte[bytes];

			sp.Read(indata, 0, bytes);
			bytes = indata.Length;

			rxByteBuffer.AddRange(indata);

			OnDataReceived(EventArgs.Empty);

			//avoid's threading error
			//XXXX this will print the raw data to the display
			//window.UpdateSerialReceivedTextBox(indata, bytes);

			//window.lvCommunicatedMessages.ItemsSource = communicatedMessages;
			//window.UpdateListView(communicatedMessages); //XXXXXXXXXXXXXXXXXXXXXXXX

			//newestMessage = new communicated_message() { time_stamp = DateTime.Now, raw_message = indata };

			//window.xbee.InterperateXbeeFrame();
			//communicatedMessages.Add(newestMessage);
			//window.RefreshListView();
		}
	}
}