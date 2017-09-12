/**********************************************************************************************************************************************
*	File: SystemTest.cs
*
*	Developed By: Mansel Jeffares
*	First Build: 11 July 2017
*	Current Build:  11 July 2017
*
*	Description :
*		Methods required to implement the system test functionality
*		Developed for a final year university project at Auckland University of Technology (AUT), New Zealand
*		
*	Useage :
*		
*
*	Limitations :
*		Built for x64, .NET 4.5.2
*   
*	Naming Conventions:
*		Variables, camelCase, start lower case, subsequent words also upper case
*		Methods, PascalCase, start upper case, subsequent words also upper case
*		Constants, all upper case, unscores for seperation
* 
**********************************************************************************************************************************************/



/**********************************************************************************************************************************************
* Namespaces
**********************************************************************************************************************************************/
#region

using SwarmRoboticsGUI;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using WPFCustomMessageBox;

#endregion


namespace SwarmRoboticsGUI
{
	public partial class MainWindow : Window
	{
		public List<ToggleButton> togglebtnControls;
		ProgressWindow EstablishingCommunicationsWindow;
		public bool testMode = false;
		int currentTestItem = 0;
		bool doublecommandlockout = false;
		public Dictionary<string, byte> twiMuxAddresses;
		
		public void setupSystemTest()
		{
			EstablishingCommunicationsWindow = new ProgressWindow("Establishing Communications", "Please wait while communications are tested.");

			twiMuxAddresses = new Dictionary<string, byte>()
			{
				{"Proximity Front", 0xFA},
				{"Proximity Front Right", 0xFF},
				{"Proximity Rear Right", 0xFE},
				{"Proximity Rear", 0xFD},
				{"Proximity Rear Left", 0xFC},
				{"Proximity Front Left", 0xFB},
				{"Light Sensor Left Hand Side", 0xF8},
				{"Light Sensor Right Hand Side", 0xF9}
			};

			this.DataContext = this;

			cbSysTestTWISet.ItemsSource = twiMuxAddresses;

			togglebtnControls = new List<ToggleButton>()
			{
				 btnSysTestProxmityA,btnSysTestProxmityB, btnSysTestProxmityC, btnSysTestProxmityD, btnSysTestProxmityE, btnSysTestProxmityF, btnSysTestLightLHS, btnSysTestLightRHS, btnSysTestLineFL, btnSysTestLineCL, btnSysTestLineCR, btnSysTestLineFR, btnSysTestMouse, btnSysTestIMU
			};
			
			foreach (var toggleButton in togglebtnControls)
			{
				toggleButton.IsEnabled = true;
				toggleButton.Click += new RoutedEventHandler(btnSysTest_Click);
				toggleButton.Checked += new RoutedEventHandler(sysTestCheck);
				toggleButton.Unchecked += new RoutedEventHandler(sysTestCheck);
			}
		}


		public int MyHandler(object s, CommunicationManager.RequestedMessageReceivedArgs e)
		{
			EstablishingCommunicationsWindow.CloseWindow();

			if (e.msg != null)
			{
				Application.Current.Dispatcher.Invoke((Action)delegate
				{
					CustomMessageBox.ShowOK("Successfully communicated with: " + e.msg.SourceDisplay, "Communications established", "Continue");
					
				});
			}
			else
			{
				MessageBox.Show("Communications Timed Out", "TIMEOUT", MessageBoxButton.OK, MessageBoxImage.Warning);
			}

			return 1;
		}


		private void btnSysTestCommunications_Click(object sender, RoutedEventArgs e)
		{

			byte[] data;
			data = new byte[2];
			data[0] = SYSTEM_TEST_MESSAGE.COMMUNICATION;
			data[1] = 0x01;

			xbee.SendTransmitRequest(commManger.currentTargetRobot, data);

			CommunicationManager.WaitForMessage tada = new CommunicationManager.WaitForMessage(0xE1, 15000, MyHandler);

			EstablishingCommunicationsWindow.ShowDialog();
		}


		private async void btnSysTestPreviousTest_Click(object sender, RoutedEventArgs e)
		{
			togglebtnControls[currentTestItem].IsChecked = false;
			await Task.Delay(40);
			currentTestItem--;

			if (currentTestItem < 0)
			{
				currentTestItem = togglebtnControls.Count-1;
			}

			togglebtnControls[currentTestItem].IsChecked = true;
			await Task.Delay(40);
		}

		private async void btnSysTestNextTest_Click(object sender, RoutedEventArgs e)
		{
			togglebtnControls[currentTestItem].IsChecked = false;
			await Task.Delay(40);
			currentTestItem++;

			if(currentTestItem == togglebtnControls.Count)
			{
				currentTestItem = 0;
			}

			togglebtnControls[currentTestItem].IsChecked = true;
			await Task.Delay(40);
		}

		private void btnSysTest_Click(object sender, RoutedEventArgs e)
		{
			var senderToggleButton = sender as ToggleButton;

			if(currentTestItem != togglebtnControls.IndexOf(senderToggleButton))
			{
				togglebtnControls[currentTestItem].IsChecked = false;
			}			
		}

		private async void sysTestCheck(object sender, RoutedEventArgs e)
		{

			var senderToggleButton = sender as ToggleButton;

			if (senderToggleButton.IsChecked == true)
			{
				await Task.Delay(30);
				currentTestItem = togglebtnControls.IndexOf(senderToggleButton);
				if (doublecommandlockout == false)
				{
					updateSystemsTest(togglebtnControls[currentTestItem].Tag.ToString(), REQUEST.START_STREAMING);
				}
				else
				{
					doublecommandlockout = false;
				}
			}
			else
			{
				await Task.Delay(10);

				if (senderToggleButton.IsChecked != true)
				{
					updateSystemsTest(togglebtnControls[currentTestItem].Tag.ToString(), REQUEST.STOP_STREAMING);
				}
			}
		}


		public static class REQUEST
		{
			public const byte DATA = 0x00;
			public const byte SINGLE_SAMPLE = 0x01;
			public const byte START_STREAMING = 0x02;
			public const byte STOP_STREAMING = 0xFF;
		}

		public static class SYSTEM_TEST_MESSAGE
		{
			public const byte MODE = 0xE0;
			public const byte COMMUNICATION = 0xE1;
			public const byte PROXIMITY = 0xE4;
			public const byte LIGHT = 0xE5;
			public const byte MOTORS = 0xE6;
			public const byte MOUSE = 0xE7;
			public const byte IMU = 0xE8;
			public const byte LINE_FOLLOWERS = 0xE9;
			public const byte FAST_CHARGE = 0xEA;
			public const byte TWI_MUX = 0xEB;
			public const byte TWI_EXT = 0xEC;
			public const byte CAMERA = 0xED;
		}



		private void updateSystemsTest(string test, byte request)
		{
			string[] tokens = test.Split(',');
			byte[] data;

			switch (tokens[0])
			{
				case "Proximity":
					byte [] proximitySensor = MJLib.StringToByteArrayFastest(tokens[1]);
					data = new byte[3];
					data[0] = 0xE4;
                    data[1] = request;
					data[2] = proximitySensor[0];

                    xbee.SendTransmitRequest(commManger.currentTargetRobot, data);
					break;

				case "Light":
					byte[] lightSensor = MJLib.StringToByteArrayFastest(tokens[1]);
					data = new byte[3];
					data[0] = 0xE5;
                    data[1] = request;
					data[2] = lightSensor[0];

                    xbee.SendTransmitRequest(commManger.currentTargetRobot, data);
					break;

				case "Mouse":
					data = new byte[2];
					data[0] = 0xE7;
					data[1] = request;
                    xbee.SendTransmitRequest(commManger.currentTargetRobot, data);
					break;

				case "IMU":
					data = new byte[2];
					data[0] = 0xE8;
					data[1] = request;
                    xbee.SendTransmitRequest(commManger.currentTargetRobot, data);
					break;

				case "Line":
					byte[] lineSensor = MJLib.StringToByteArrayFastest(tokens[1]);
					data = new byte[3];
					data[0] = 0xE9;
					data[1] = request;
					data[2] = lineSensor[0];
                    xbee.SendTransmitRequest(commManger.currentTargetRobot, data);
					break;

				case "Charge":

					break;

				case "TWI":

					break;

				case "Camera":

					break;
            }
		}



		private void slMotor_DragCompleted(object sender, DragCompletedEventArgs e)
		{
			Slider slider = sender as Slider;
			slider.Value = 0;

			byte[] motor = MJLib.StringToByteArrayFastest(slider.Tag.ToString());

			byte[] data = new byte[3];
			data[0] = 0xE6;
			data[1] = motor[0];
			data[2] += (byte)Math.Abs(slider.Value);

			xbee.SendTransmitRequest(commManger.currentTargetRobot, data);
		}

		private void slMotor_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			Slider slider = sender as Slider;
			byte[] motor = MJLib.StringToByteArrayFastest(slider.Tag.ToString());

			byte[] data = new byte[3];
			data[0] = 0xE6;
			data[1] = motor[0];

			if (slider.Value > 0)
			{
				data[2] = 0x80;
			}

			data[2] += (byte)Math.Abs(slider.Value);

			xbee.SendTransmitRequest(commManger.currentTargetRobot, data);
		}

		private void btnSysTestTWISet_Click(object sender, RoutedEventArgs e)
		{
			KeyValuePair<string, byte> selected = (KeyValuePair<string, byte>)cbSysTestTWISet.SelectedItem;

			byte[] data = new byte[3];
			data[0] = 0xEB;
			data[1] = 0x01;
			data[2] = selected.Value;

			xbee.SendTransmitRequest(commManger.currentTargetRobot, data);
		}
	}
}