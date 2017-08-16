/**********************************************************************************************************************************************
*	File: SystemTest.cs
*
*	Developed By: Mansel Jeffares
*	First Build: 11 July 2017
*	Current Build:  11 July 2017
*
*	Description :
*		Methods required to implement the system test functionality
*		Built for x64, .NET 4.5.2
*		
*	Useage :
*		
*
*	Limitations :
*		Build for x64
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
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WPFCustomMessageBox;

#endregion



/**********************************************************************************************************************************************
* Structures and Classes
**********************************************************************************************************************************************/
#region


#endregion



namespace SwarmRoboticsGUI
{
	public partial class MainWindow : Window
	{
		public List<ToggleButton> togglebtnControls;
		public List<Button> btnControls;
		//bool communicationsTest = false;
		public bool testMode = false;
		bool fullSystemTest = false;
		int currentTestItem = -1;
		bool doublecommandlockout = false;
		public bool waitingForReply = false;
		public byte waitForReplyType = 0x00;
		//public communicated_message waitForReplyMessage;
		public UInt64 systemTestDesitination64;
		public UInt16 systemTestDesitination16;
		public List<UInt64> connectedRobots;
		public bool avoidConnected = false;
		public TaskCompletionSource<bool> Reply = new TaskCompletionSource<bool>();



		public void setupSystemTest()
		{
			connectedRobots = new List<UInt64>();

			togglebtnControls = new List<ToggleButton>()
			{
				 btnSysTestProxmityA,btnSysTestProxmityB, btnSysTestProxmityC, btnSysTestProxmityD, btnSysTestProxmityE, btnSysTestProxmityF, btnSysTestLightLHS, btnSysTestLightRHS, btnSysTestLineFL, btnSysTestLineCL, btnSysTestLineCR, btnSysTestLineFR, btnSysTestMouse, btnSysTestIMU
			};



			foreach (var toggleButton in togglebtnControls)
			{
				toggleButton.IsEnabled = false;
				toggleButton.Click += new RoutedEventHandler(btnSysTest_Click);
				toggleButton.Checked += new RoutedEventHandler(sysTestCheck);
				toggleButton.Unchecked += new RoutedEventHandler(sysTestCheck);
			}
		}


		public int MyHandler(object s, CommunicationManager.RequestedMessageReceivedArgs e)
		{
			MJLib.AutoClosingMessageBox.Close("Establishing Communications");

			if (e.msg != null)
			{
				//MJLib.AutoClosingMessageBox.Close("Establishing Communications");

				//systemTestDesitination16 = BitConverter.ToUInt16(msg.source16, 0);
				//systemTestDesitination64 = BitConverter.ToUInt64(msg.source64, 0);

				Application.Current.Dispatcher.Invoke((Action)delegate
				{
					MessageBoxResult sucessfullCommunicationsResult = CustomMessageBox.ShowYesNoCancel("Successfully communicated with: " + e.msg.SourceDisplay, "Communications established", "Connect to this robot", "Choose Another Robot", "Cancel");




					switch (sucessfullCommunicationsResult)
					{
						case MessageBoxResult.Yes:
							avoidConnected = false;
							connectedRobots.Clear();
							btnSysTestTestMode.IsEnabled = true;
							break;

						case MessageBoxResult.No:
							avoidConnected = true;
							connectedRobots.Add(systemTestDesitination64);
							//btnSysTestCommunications_Click(s, );
							CommunicationManager.WaitForMessage tada = new CommunicationManager.WaitForMessage(0xE1, 5000, MyHandler);
							break;

						case MessageBoxResult.Cancel:
							avoidConnected = false;
							connectedRobots.Clear();
							break;
					}
				});
			}
			else
			{
				//MJLib.AutoClosingMessageBox.Close("Establishing Communications");
				MessageBox.Show("TIMEOUT", "Communications Timed Out", MessageBoxButton.OK);
			}





			return 1;
		}


		private void btnSysTestCommunications_Click(object sender, RoutedEventArgs e)
		{
			Reply = new TaskCompletionSource<bool>();

			byte[] data;
			data = new byte[2];
			data[0] = SYSTEM_TEST_MESSAGE.COMMUNICATION;
			data[1] = 0x01;
            
            //data[0] = 0xE4;
           // data[1] = 0x02;
            //data[2] = 0xFA;
                         

			xbee.SendTransmitRequest(XbeeAPI.DESTINATION.BROADCAST, data);

			new Thread(new ThreadStart(delegate
			{
				MessageBox.Show
				(
				  "Please wait while communications are tested.",
				  "Establishing Communications",
				  MessageBoxButton.OK,
				  MessageBoxImage.Warning

				);
			})).Start();



			CommunicationManager.WaitForMessage tada = new CommunicationManager.WaitForMessage(0xE1, 15000, MyHandler);

			

			//ProtocolClass.SwarmProtocolMessage msg = protocol.waitForMessage(0xE1, 5000).Result;

			/*
			if (msg != null)
			{
				MJLib.AutoClosingMessageBox.Close("Establishing Communications");

				//systemTestDesitination16 = BitConverter.ToUInt16(msg.source16, 0);
				//systemTestDesitination64 = BitConverter.ToUInt64(msg.source64, 0);

				MessageBoxResult sucessfullCommunicationsResult = CustomMessageBox.ShowYesNoCancel("Successfully communicated with: " + msg.SourceDisplay, "Communications established", "Connect to this robot", "Choose Another Robot", "Cancel");

				switch (sucessfullCommunicationsResult)
				{
					case MessageBoxResult.Yes:
						avoidConnected = false;
						connectedRobots.Clear();
						btnSysTestTestMode.IsEnabled = true;
						break;

					case MessageBoxResult.No:
						avoidConnected = true;
						connectedRobots.Add(systemTestDesitination64);
						btnSysTestCommunications_Click(sender, e);
						break;

					case MessageBoxResult.Cancel:
						avoidConnected = false;
						connectedRobots.Clear();
						break;
				}
			}
			else
			{
				MJLib.AutoClosingMessageBox.Close("Establishing Communications");
				MessageBox.Show("TIMEOUT", "Communications Timed Out", MessageBoxButton.OK);
			}
			*/

			//waitingForReply = true;
			//waitForReplyType = SYSTEM_TEST_MESSAGE.COMMUNICATION;


			//XXXX replace with custom messagebox without buttons
            /*
			new Thread(new ThreadStart(delegate
			{
				MessageBox.Show
				(
				  "Please wait while communications are tested.",
				  "Establishing Communications",
				  MessageBoxButton.OK,
				  MessageBoxImage.Warning
				);
			})).Start();
            */
             
			//if (await Task.WhenAny(Reply.Task, Task.Delay(15000)) == Reply.Task)
            /*
            if (await Reply.Task)
			{

				MJLib.AutoClosingMessageBox.Close("Establishing Communications");
				systemTestDesitination16 = BitConverter.ToUInt16(waitForReplyMessage.source16, 0);
				systemTestDesitination64 = BitConverter.ToUInt64(waitForReplyMessage.source64, 0);

				MessageBoxResult sucessfullCommunicationsResult = CustomMessageBox.ShowYesNoCancel("Successfully communicated with: " + waitForReplyMessage.SourceDisplay, "Communications established", "Connect to this robot", "Choose Another Robot", "Cancel");

				switch(sucessfullCommunicationsResult)
				{
					case MessageBoxResult.Yes:
						avoidConnected = false;
						connectedRobots.Clear();
						btnSysTestTestMode.IsEnabled = true;
						break;

					case MessageBoxResult.No:
						avoidConnected = true;
						connectedRobots.Add(systemTestDesitination64);
						btnSysTestCommunications_Click(sender, e);
						break;

					case MessageBoxResult.Cancel:
						avoidConnected = false;
						connectedRobots.Clear();
						break;
				}
			}
			else
			{
				MJLib.AutoClosingMessageBox.Close("Establishing Communications");
				MessageBox.Show("TIMEOUT", "Communications Timed Out", MessageBoxButton.OK);
			}			
			*/
             
		}

		

		private void btnSysTestTestMode_Click(object sender, RoutedEventArgs e)
		{
			if (testMode == false)
			{
				testMode = true;
				btnSysTestTestMode.Content = "Exit Test Mode";
				currentTestItem = 0;

				foreach (var toggleButton in togglebtnControls)
				{
					toggleButton.IsEnabled = true;
				}
				btnSysTestFullTest.IsEnabled = true;
			}
			else if(fullSystemTest == false)
			{
				testMode = false;
				btnSysTestTestMode.Content = "Enter Test Mode";

				foreach (var toggleButton in togglebtnControls)
				{
					toggleButton.IsEnabled = false;
				}
				btnSysTestFullTest.IsEnabled = false;
			}
			else if(fullSystemTest == true)
			{
				MessageBox.Show("Please finsh the full system test before exiting test mode");
			}
		}

		private async void btnSysTestFullTest_Click(object sender, RoutedEventArgs e)
		{
			if (fullSystemTest == false)
			{
				btnSysTestFullTest.Content = "End Full Systems Test";
				togglebtnControls[currentTestItem].IsChecked = false;
				//sysTestCheck(togglebtnControls[currentTestItem], e);
				await Task.Delay(40);
				fullSystemTest = true;
				btnSysTestNextTest.IsEnabled = true;
				btnSysTestPreviousTest.IsEnabled = true;
				currentTestItem = 0;
				togglebtnControls[currentTestItem].IsChecked = true;
				//sysTestCheck(togglebtnControls[currentTestItem], e);
				await Task.Delay(40);
			}
			else
			{
				btnSysTestFullTest.Content = "Begin Full Systems Test";
				fullSystemTest = false;
				btnSysTestNextTest.IsEnabled = false;
				btnSysTestPreviousTest.IsEnabled = false;
				togglebtnControls[currentTestItem].IsChecked = false;
				//sysTestCheck(togglebtnControls[currentTestItem], e);
				await Task.Delay(40);

			}
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

			if (fullSystemTest == true && currentTestItem == togglebtnControls.IndexOf(senderToggleButton))
			{
				togglebtnControls[currentTestItem].IsChecked = true;
				doublecommandlockout = true;

			}
			else if(currentTestItem != togglebtnControls.IndexOf(senderToggleButton))
			{
				togglebtnControls[currentTestItem].IsChecked = false;
				//stop streaming current data
				//string test = togglebtnControls[currentTestItem].Tag.ToString();
				//updateSystemsTest(test, REQUEST.STOP_STREAMING);

				//currentTestItem = togglebtnControls.IndexOf(senderToggleButton);
				//updateSystemsTest(togglebtnControls[currentTestItem].Tag.ToString(), REQUEST.START_STREAMING);
				//request new type of data
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
		private static class SYSTEM_TEST_MESSAGE
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
					data[1] = lineSensor[0];
					data[2] = request;
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

	}
}