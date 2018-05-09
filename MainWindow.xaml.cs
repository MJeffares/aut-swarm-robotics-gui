/**********************************************************************************************************************************************
*	File: MainWindow.xaml.cs
*
*	Developed By: Mansel Jeffares and Brae Hartley
*	First Build: 7 March 2017
*	Current Build: 12 September 2017
*
*	Description :
*		Does most of the important UI stuff
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

#region notestoself

// MANSEL: This is an example of a Mansel task
// BRAE: Use this to get Brae to do something for once
// TODO: This is for general things that need doing
// UNDONE: This is life

#endregion

#region Namespaces

using AForge.Video;
using AForge.Video.DirectShow;
using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.Util;
using folderHack;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using XbeeHandler;
using XbeeHandler.XbeeFrames;

#endregion


namespace SwarmRoboticsGUI
{
    public enum WindowStatusType { MAXIMISED, MINIMISED, POPPED_OUT };
    public enum TimeDisplayModeType { CURRENT, FROM_START, START };



	public partial class MainWindow : Window
	{
		// Declarations
		#region Public Properties
		// TODO: comment declarations
		public Camera camera1 { get; set; }
		public SerialUARTCommunication serial { get; set; }
		public XbeeAPI xbee { get; set; }
		public ProtocolClass protocol { get; set; }
		public CommunicationManager commManger { get; set; }
		public OverlayWindow Overlay { get; set; }
		public SwarmManager swarmManager { get; set; }

		public List<Item> ItemList { get; set; }
		public ObservableCollection<XbeeAPIFrame> XbeeMessages { get; set; }

		public WindowStatusType WindowStatus { get; set; }
		public TimeDisplayModeType TimeDisplayMode { get; set; }
		public double WindowSize { get; set; }
		#endregion

		#region Private Properties
		// one second timer to calculate and update the fps count
		private DispatcherTimer InterfaceTimer;
		//
		private OpenFileDialog openvideodialog = new OpenFileDialog();
		private SaveFileDialog savevideodialog = new SaveFileDialog();
		private FilterInfoCollection VideoDevices { get; set; }
		private VideoCaptureDevice VideoDevice { get; set; }
		#endregion

		// Constructor
		public MainWindow()
		{
			InitializeComponent();
			DataContext = this;
			//
			CvInvoke.UseOpenCL = true;
			//
			camera1 = new Camera();

			//
			PopulateFilters();
			PopulateOverlays();
			PopulateCameras();
			PopulateSources();
			PopulateRobots();
			//DEBUGGING_PopulateTestList();

			protocol = new ProtocolClass(this);
			serial = new SerialUARTCommunication();
			xbee = new XbeeAPI(this);

			commManger = new CommunicationManager(this, serial, xbee, protocol);
			XbeeMessages = commManger.rxXbeeMessageBuffer;
			swarmManager = new SwarmManager(this);

			PopulateSerialSettings();
			PopulateSerialPorts();

			portList.MouseEnter += new MouseEventHandler(menuPopulateSerialPorts);
			connectButton.Click += new RoutedEventHandler(menuCommunicationConnect_Click);

			Overlay = new OverlayWindow(this);
			Overlay.Show();
			// BRAE: Default setup for testing
			Overlay.display1.Source = SourceType.CAMERA;
			//
			openvideodialog.Filter = "Video Files|*.avi;*.mp4;*.mpg";
			savevideodialog.Filter = "Video Files|*.avi;*.mp4;*.mpg";
			savevideodialog.Title = "Record: Save As";
			//
			TimeDisplayMode = TimeDisplayModeType.CURRENT;
			WindowStatus = WindowStatusType.MAXIMISED;
			//
			SetupSystemTest();
			TowerControlSetup();
			//
			InitializeInterfaceTimer();

			TestImage();

		}

		#region Private Methods
		private void InitializeInterfaceTimer()
		{
			InterfaceTimer = new DispatcherTimer();
			InterfaceTimer.Tick += Interface_Tick;
			InterfaceTimer.Interval = new TimeSpan(0, 0, 1);
			InterfaceTimer.Start();
		}
		private void DEBUGGING_PopulateXbeeMessages()
		{
			//TestList = new List<XbeeAPIFrame>();
			for (int i = 0; i < 10; i++)
			{
				XbeeMessages.Add(new XbeeAPIFrame(new byte[] { 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01 }));
				XbeeMessages.Add(new XbeeAPIFrame(new byte[] { 0x02, 0x01, 0x01, 0x01, 0x02, 0x01, 0x02, 0x01, 0x02 }));
				XbeeMessages.Add(new XbeeAPIFrame(new byte[] { 0x03, 0x01, 0x03, 0x01, 0x03, 0x01, 0x01, 0x03, 0x01 }));
			}
		}
		private void PopulateCameras()
		{
			// we dont want to update this if we are connected to a camera
			if (camera1.Status != StatusType.PLAYING && camera1.Status != StatusType.RECORDING)
			{
				// gets currently connected devices
				VideoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
				// clears cameras from menu                                      
				menuCameraList.Items.Clear();
				menuCameraConnect.IsEnabled = false;
				menuCameraCapabilityList.IsEnabled = false;

				// loops through cameras and adds them to menu
				//for (int i = 0; i < VideoDevices.Count; i++)
				for (int i = 0; i < VideoDevices.Count; i++)
				{
					MenuItem item = new MenuItem { Header = VideoDevices[i].Name };
					item.Click += new RoutedEventHandler(menuCameraListItem_Click);
					item.IsCheckable = true;
					menuCameraList.Items.Add(item);

					// restores currently connect camera selection
					if (item.ToString() == camera1.Name)
					{
						item.IsEnabled = true;
						item.IsChecked = true;
						camera1.Index = menuCameraList.Items.IndexOf(item);
						menuCameraConnect.IsEnabled = true;
					}
				}

				// displays helpful message if no cameras found
				if (menuCameraList.Items.Count == 0)
				{
					MenuItem nonefound = new MenuItem { Header = "No Cameras Found" };
					menuCameraList.Items.Add(nonefound);
					nonefound.IsEnabled = false;
				}
			}
		}
		private void PopulateCameraCapabilities()
		{
			// we dont want to update this if we are connected to a camera
			if (camera1.Status == StatusType.STOPPED)
			{
				// clears cameras from menu                                      
				menuCameraCapabilityList.Items.Clear();
				// loops through cameras video options and adds them to menu
				foreach (VideoCapabilities capabilityInfo in VideoDevice.VideoCapabilities)
				{
					MenuItem item = new MenuItem
					{
						Header = string.Format("{0} by {1} @ {2} FPS",
						capabilityInfo.FrameSize.Width,
						capabilityInfo.FrameSize.Height,
						capabilityInfo.AverageFrameRate)
					};
					item.Click += new RoutedEventHandler(menuCameraCapabilityListItem_Click);
					item.IsCheckable = true;
					menuCameraCapabilityList.Items.Add(item);
				}

				// displays "helpful" message if no options are found
				if (menuCameraCapabilityList.Items.Count == 0)
				{
					MenuItem nonefound = new MenuItem { Header = "No" };
					menuCameraCapabilityList.Items.Add(nonefound);
					nonefound.IsEnabled = false;
				}
				else
					(menuCameraCapabilityList.Items[camera1.CapabilityIndex] as MenuItem).IsChecked = true;
			}
		}
		private void PopulateFilters()
		{
			//loops through our filters and adds them to our menu
			foreach (FilterType Filter in Enum.GetValues(typeof(FilterType)))
			{
				MenuItem item = new MenuItem { Header = EnumUtils<FilterType>.GetDescription(Filter) };
				item.Click += new RoutedEventHandler(menuFilterListItem_Click);
				item.IsCheckable = true;
				//by default select our first filter (no filter)
				if (Filter == FilterType.NONE)
				{
					item.IsChecked = true;
				}
				menuFilterList.Items.Add(item);
			}
			// add our seperator and settings menu items
			Separator sep = new Separator();
			menuFilterList.Items.Add(sep);

			MenuItem settingsmenuitem = new MenuItem { Header = "Settings" };
			menuFilterList.Items.Add(settingsmenuitem);
			settingsmenuitem.Click += menuPlaceHolder_Click;
		}
		private void PopulateOverlays()
		{
			//loops through our filters and adds them to our menu
			foreach (OverlayType Overlay in Enum.GetValues(typeof(OverlayType)))
			{
				MenuItem item = new MenuItem { Header = EnumUtils<OverlayType>.GetDescription(Overlay) };
				item.Click += new RoutedEventHandler(menuOverlayListItem_Click);
				item.IsCheckable = true;
				//by default select our first filter (no filter)
				if (Overlay == OverlayType.NONE)
				{
					item.IsChecked = true;
				}
				menuOverlayList.Items.Add(item);
			}
			// add our seperator and settings menu items
			Separator sep = new Separator();
			menuOverlayList.Items.Add(sep);

			MenuItem settingsmenuitem = new MenuItem { Header = "Settings" };
			menuOverlayList.Items.Add(settingsmenuitem);
			settingsmenuitem.Click += menuPlaceHolder_Click;
		}
		private void PopulateSources()
		{
			//loops through our filters and adds them to our menu
			foreach (SourceType Source in Enum.GetValues(typeof(SourceType)))
			{
				MenuItem item = new MenuItem { Header = EnumUtils<SourceType>.GetDescription(Source) };
				item.Click += new RoutedEventHandler(menuSourceListItem_Click);
				item.IsCheckable = true;
				//by default select our first source (no source)
				if (Source == SourceType.NONE)
				{
					item.IsChecked = true;
				}
				menuSourceList.Items.Add(item);
			}
			// add our seperator and settings menu items
			Separator sep = new Separator();
			menuSourceList.Items.Add(sep);

			MenuItem settingsmenuitem = new MenuItem { Header = "Settings" };
			menuSourceList.Items.Add(settingsmenuitem);
			settingsmenuitem.Click += menuPlaceHolder_Click;
		}
		private void PopulateRobots()
		{
			ItemList = new List<Item>();
			ItemList.Add(new RobotItem("Red Robot", 0x0013A2004147F9DD, "Red", 0));
			ItemList.Add(new RobotItem("Yellow Robot", 0x0013A200415B8C38, "Yellow", 1));
			ItemList.Add(new RobotItem("Purple Robot", 0x0013A200415B8BDD, "Purple", 2));
			ItemList.Add(new RobotItem("Light Blue Robot", 0x0013A2004152F256, "Cyan", 3));
			ItemList.Add(new RobotItem("Dark Blue Robot", 0x0013A200415B8C3A, "MidnightBlue", 4));
			ItemList.Add(new RobotItem("Brown Robot", 0x0013A20041065FB3, "SaddleBrown", 5));
			ItemList.Add(new RobotItem("Pink Robot", 0x0013A200415B8C18, "Plum", 6));
			ItemList.Add(new RobotItem("Orange Robot", 0x0013A200415B8BE5, "Orange", 7));

			ItemList.Add(new ChargingDockItem("Tower Base Station", 0x0013A200415B8C2A, "Lime"));
			ItemList.Add(new CommunicationItem("Broadcast", 0x000000000000FFFF, "White"));
		}
		private void DisplayTime()
		{
			switch (TimeDisplayMode)
			{
				case TimeDisplayModeType.CURRENT:
					statusTime.Text = DateTime.Now.ToString("t");
					statusTime.Text = DateTime.Now.ToString();
					statusTime.Text = String.Format("{0:d dd HH:mm:ss}", DateTime.Now);
					statusTime.Text = String.Format("{0:T}", DateTime.Now.ToString());
					break;

				case TimeDisplayModeType.FROM_START:
					if (camera1.Status == StatusType.RECORDING)
					{
						statusTime.Text = (camera1.RecordingTime).ToString(@"dd\.hh\:mm\:ss");
					}
					break;
			}
		}

		#endregion

		#region Time Events
		private void Interface_Tick(object sender, EventArgs arg)
		{
			//serial._serialPort.Write("Test");

			//commManger.rxXbeeMessageBuffer.Add(new XbeeAPIFrame(new byte[] { 0x02, 0x01, 0x01, 0x01, 0x02, 0x01, 0x02, 0x01, 0x02 }));

			/// Error message for zero frames
			///if (camera.Status == Camera.StatusType.PLAYING || camera.Status == Camera.StatusType.RECORDING)
			///{
			///	if(camera.Status == Camera.StatusType.PLAYING)
			///	{
			///		if (_fpscount == 0)
			///		{
			///			MessageBoxResult result = CustomMessageBox.ShowYesNo(
			///				"Warning Camera disconnected or significant failure, Please reconnect camera and press continue or stop capturing now",
			///				"Camera Error",
			///				"Continue",
			///				"Stop Capture",
			///				MessageBoxImage.Error
			///				);
			///			//check result do things based on it, add to seperate function
			///		}
			///	}
			///	else if (camera.Status == Camera.StatusType.RECORDING)
			///	{
			///		if (_fpscount == 0)
			///		{
			///			MessageBoxResult result = CustomMessageBox.ShowYesNo(
			///				"Warning Camera disconnected or significant failure, Please reconnect camera and press continue or end the recording now",
			///				"Camera Error",
			///				"Continue",
			///				"End Recording",
			///				MessageBoxImage.Error
			///				);
			///			//check result do things based on it, add to seperate function
			///		}
			///		//flashes recording dot red/black if recroding is in progress 
			///		if (statusRecordingDot.Foreground.Equals(Brushes.Red))
			///		{
			///			statusRecordingDot.Foreground = Brushes.Black;
			///		}
			///		else
			///		{
			///			statusRecordingDot.Foreground = Brushes.Red;
			///		}
			///	}
			///	//updates FPS counter
			///	statusFPS.Text = "FPS: " + _fpscount.ToString();
			///	_fpscount = 0;
			///}
			statusFPS.Text = camera1.Fps.ToString();

			DisplayTime();
		}
		#endregion

		#region Input Handlers
		//Display Menu
		private void menuFilterListItem_Click(object sender, RoutedEventArgs e)
		{
			MenuItem menusender = (MenuItem)sender;
			string menusenderstring = menusender.ToString();

			if (menusenderstring != EnumUtils<FilterType>.GetDescription(camera1.Filter))
			{
				MenuItem[] allitems = menuFilterList.Items.OfType<MenuItem>().ToArray();

				foreach (var item in allitems)
				{
					item.IsChecked = false;
				}
				menusender.IsChecked = true;
				camera1.Filter = (FilterType)menuFilterList.Items.IndexOf(menusender);
				//
				statusDisplayFilter.Text = EnumUtils<FilterType>.GetDescription(camera1.Filter);
			}
			else if (camera1.Filter != FilterType.NONE)
			{
				MenuItem[] allitems = menuFilterList.Items.OfType<MenuItem>().ToArray();

				foreach (var item in allitems)
				{
					item.IsChecked = false;
					item.IsEnabled = true;
				}
			}
		}

		private void menuOverlayListItem_Click(object sender, RoutedEventArgs e)
		{
			MenuItem menusender = (MenuItem)sender;
			string menusenderstring = menusender.ToString();

			if (menusenderstring != EnumUtils<OverlayType>.GetDescription(Overlay.display1.Overlay))
			{
				MenuItem[] allitems = menuOverlayList.Items.OfType<MenuItem>().ToArray();

				foreach (var item in allitems)
				{
					item.IsChecked = false;
				}
				menusender.IsChecked = true;
				Overlay.display1.Overlay = (OverlayType)menuOverlayList.Items.IndexOf(menusender);
				// TODO: Not sure where to display statusDisplayFilter right now
				//statusDisplayFilter.Text = ImageDisplay.ToString(overlayWindow.Display.Overlay);
			}
		}

		private void menuSourceListItem_Click(object sender, RoutedEventArgs e)
		{
			MenuItem menusender = (MenuItem)sender;
			string menusenderstring = menusender.ToString();

			if (menusenderstring != EnumUtils<SourceType>.GetDescription(Overlay.display1.Source))
			{
				MenuItem[] allitems = menuSourceList.Items.OfType<MenuItem>().ToArray();

				foreach (var item in allitems)
				{
					item.IsChecked = false;
				}
				menusender.IsChecked = true;
				Overlay.display1.Source = (SourceType)menuSourceList.Items.IndexOf(menusender);
			}
		}

		//Camera menu
		private void menuCameraListItem_Click(object sender, RoutedEventArgs e)
		{
			var menusender = sender as MenuItem;
			string menusenderstring = menusender.ToString();
			// Make sure a capture isn't running.
			if (camera1.Status == StatusType.STOPPED)
			{
				// If it isn't already selected
				if (camera1.Name != menusenderstring)
				{
					MenuItem[] allitems = menuCameraList.Items.OfType<MenuItem>().ToArray();

					foreach (var item in allitems) item.IsChecked = false;

					statusCameraName.Text = menusender.Header.ToString();
					// Capture can be started
					menuCameraConnect.IsEnabled = true;
					// Resolution can be selected
					menuCameraCapabilityList.IsEnabled = true;
					// Update camera
					camera1.Name = menusender.ToString();
					camera1.Index = menuCameraList.Items.IndexOf(menusender);
					camera1.MonikerString = VideoDevices[camera1.Index].MonikerString;

					VideoDevice = new VideoCaptureDevice(camera1.MonikerString);

					// Default capability to last in list
					camera1.CapabilityIndex = VideoDevice.VideoCapabilities.Length - 1;

					// Populate resolution options
					PopulateCameraCapabilities();

					// Display feedback to user
					menusender.IsChecked = true;
				}
			}
		}

		private void menuCameraCapabilityListItem_Click(object sender, RoutedEventArgs e)
		{
			var menusender = sender as MenuItem;

			if (camera1.Status == StatusType.STOPPED)
			{
				// If it isn't already selected
				if (camera1.CapabilityIndex != menuCameraCapabilityList.Items.IndexOf(menusender))
				{
					// Uncheck all options
					MenuItem[] allitems = menuCameraCapabilityList.Items.OfType<MenuItem>().ToArray();
					foreach (var item in allitems) item.IsChecked = false;
					// Display feedback to user
					menusender.IsChecked = true;
					// Set the camera resolution and framerate via capability index
					camera1.CapabilityIndex = menuCameraCapabilityList.Items.IndexOf(menusender);
				}
			}
		}

		private void menuCameraConnect_Click(object sender, RoutedEventArgs e)
		{
			if (camera1.Status == StatusType.PLAYING || camera1.Status == StatusType.PAUSED)
			{
				// Stop capturing
				camera1.StopCapture();
				//
				menuCameraConnect.Header = "Start Capture";
				menuCameraFreeze.Header = "Freeze";
				menuCameraFreeze.IsChecked = false;
				menuCameraFreeze.IsEnabled = false;
				menuRecordNew.IsEnabled = false;
				//
				MenuItem[] allitems = menuCameraList.Items.Cast<MenuItem>().ToArray();
				foreach (var item in allitems)
				{
					item.IsEnabled = true;
				}
				MenuItem[] allitems2 = menuCameraCapabilityList.Items.Cast<MenuItem>().ToArray();
				foreach (var item in allitems2)
				{
					item.IsEnabled = true;
				}
			}
			else if (camera1.Status == StatusType.STOPPED)
			{
				// Start capturing
				camera1.StartCapture();
				//
				menuCameraConnect.Header = "Stop Capture";          // Update the header on our connect/disconnect button
																	// TODO: What should the pause button say?
				menuCameraFreeze.Header = "Freeze";
				menuCameraFreeze.IsEnabled = true;                  // enable the freeze frame button
				menuRecordNew.IsEnabled = true;
				//
				MenuItem[] allitems = menuCameraList.Items.OfType<MenuItem>().ToArray();
				foreach (var item in allitems)
				{
					item.IsEnabled = false;
				}
				MenuItem[] allitems2 = menuCameraCapabilityList.Items.Cast<MenuItem>().ToArray();
				foreach (var item in allitems2)
				{
					item.IsEnabled = false;
				}
			}
		}

		private void menuCameraOptions_Click(object sender, RoutedEventArgs e)
		{
			camera1.OpenSettings();
		}

		private void menuCameraFreeze_Click(object sender, RoutedEventArgs e)
		{
			if (camera1.Status == StatusType.PLAYING)
			{
				camera1.PauseCapture();
			}
			else if (camera1.Status == StatusType.PAUSED)
			{
				camera1.ResumeCapture();
			}
		}

		private void menuFilterFlipVertical_Click(object sender, RoutedEventArgs e)
		{
			camera1.FlipVertical();
		}

		private void menuFilterFlipHorizontal_Click(object sender, RoutedEventArgs e)
		{
			camera1.FlipHorizontal();
		}

		// Replay menu
		private void menuReplayOpen_Click(object sender, RoutedEventArgs e)
		{
			if (camera1.Status == StatusType.STOPPED)
			{
				if (openvideodialog.ShowDialog() == true)
				{
					try
					{
						statusRecordingText.Text = "Replaying Video: " + openvideodialog.FileName;
						camera1.StartReplaying(openvideodialog.FileName);
					}
					catch (NullReferenceException excpt)
					{
						MessageBox.Show(excpt.Message);
					}
				}
			}
		}

		private void menuRecordNew_Click(object sender, RoutedEventArgs e)
		{
			if (camera1.Status == StatusType.PLAYING)
			{
				if (savevideodialog.ShowDialog() == true)
				{
					//
					camera1.StartRecording(savevideodialog.FileName);
					//
					statusRecordingText.Text = "Recording Video: " + savevideodialog.FileName;
					menuRecordStop.IsEnabled = true;
					menuCameraConnect.IsEnabled = false;
					menuCameraFreeze.IsEnabled = false;
				}
			}
		}

		private void menuRecordStop_Click(object sender, RoutedEventArgs e)
		{
			//
			camera1.StopRecording();
			//
			menuCameraConnect.IsEnabled = true;
			menuCameraFreeze.IsEnabled = true;
			menuRecordStop.IsEnabled = false;
			statusRecordingText.Text = "Not Recording";
			statusRecordingDot.Foreground = System.Windows.Media.Brushes.Black;
		}

		private void menuPlaceHolder_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("Sorry Placeholder");
		}

		private void btnBatteryVoltage_Click(object sender, RoutedEventArgs e)
		{
			//protocol.SendMessage(ProtocolClass.MESSAGE_TYPES.BATTERY_VOLTAGE);
		}


		// Items list
		private void ItemsList1_SelectedItemChanged(object sender, EventArgs e)
		{
			var itemList = sender as ItemList;
			if (itemList != null)
			{
				var commsItem = itemList.SelectedItem as ICommunicates;
				if (commsItem != null)
					commManger.currentTargetRobot = commsItem.Address64;
			}
		}


		// MANSEL: RotateToHeading, MoveToPosition events
		private void receivedDataClear_Click(object sender, RoutedEventArgs e)
		{
			DEBUGGING_PopulateXbeeMessages();
		}
		private void updnRotateToHeading_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			int value = (int)e.NewValue;
		}
		private void updnMoveToPositionX_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			int value = (int)e.NewValue;
		}
		private void updnMoveToPositionY_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			int value = (int)e.NewValue;
		}
		#endregion

		private void Window_Closing(object sender, CancelEventArgs e)
		{
			//camera1.CloseCapture();
		}

		private void btnCameraTest_Click(object sender, RoutedEventArgs e)
		{
			byte[] data;
			data = new byte[10];
			data[0] = SYSTEM_TEST_MESSAGE.CAMERA;
			data[1] = REQUEST.IMAGE;
			data[2] = 0x00;     //start pixel
			data[3] = 0x00;     //start pixel
			data[4] = 0x00;     //start pixel
			data[5] = 0x00;     //start pixel
			data[6] = 0x00;     //end pixel
			data[7] = 0x00;     //end pixel
			data[8] = 0x00;     //end pixel
			data[9] = 0x14;     //end pixel

			xbee.SendTransmitRequest(commManger.currentTargetRobot, data);

			//CommunicationManager.WaitForMessage tada = new CommunicationManager.WaitForMessage(0xE1, 15000, MyHandler);

			//EstablishingCommunicationsWindow = new ProgressWindow("Establishing Communications", "Please wait while communications are tested.");
			//EstablishingCommunicationsWindow.ShowDialog();
		}

		private void btnCameraTestClearFrame_Click(object sender, RoutedEventArgs e)
		{
			TestImage();
		}

		private void btnCameraTestSaveImage_Click(object sender, RoutedEventArgs e)
		{
			SaveFileDialog saveRobotImageDialog = new SaveFileDialog();
			saveRobotImageDialog.Filter = "Image files (*.png;*.jpeg)|*.png;*.jpeg|All files (*.*)|*.*";
			saveRobotImageDialog.FileName = "RobotImage.png";
			if (saveRobotImageDialog.ShowDialog() == true)
			{
				using (var fileStream = new FileStream(saveRobotImageDialog.FileName, FileMode.Create))
				{
					BitmapEncoder encoder = new PngBitmapEncoder();
					encoder.Frames.Add(BitmapFrame.Create(mybmpSource));
					encoder.Save(fileStream);
				}
			}
		}

		private void saveDebugMessages_Click(object sender, RoutedEventArgs e)
		{
			SaveFileDialog saveDebugMessages = new SaveFileDialog();
			saveDebugMessages.Filter = "Comma Seperate Values(*.csv)|*.csv";
			saveDebugMessages.FileName = "Debug Messages.csv";
			if (saveDebugMessages.ShowDialog() == true)
			{
				File.WriteAllText(saveDebugMessages.FileName, SwarmRoboticsCommunicationProtocolHandler.SwarmRoboticsCommunicationProtocolMessages.DebugString.csv.ToString());
				//SwarmRoboticsCommunicationProtocolHandler.SwarmRoboticsCommunicationProtocolMessages.DebugString.csv
				/*
				using (var fileStream = new FileStream(saveRobotImageDialog.FileName, FileMode.Create))
				{
					BitmapEncoder encoder = new PngBitmapEncoder();
					encoder.Frames.Add(BitmapFrame.Create(mybmpSource));
					encoder.Save(fileStream);
				}
				*/
			}


				
		}
	}
}
