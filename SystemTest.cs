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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

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
		public bool testMode = false;
		bool fullSystemTest = false;
		int currentTestItem = -1;
		bool doublecommandlockout = false;

		public void setupSystemTest()
		{
			togglebtnControls = new List<ToggleButton>()
			{
				 btnSysTestProxmityA,btnSysTestProxmityB, btnSysTestProxmityC, btnSysTestProxmityD, btnSysTestProxmityE, btnSysTestProxmityF, btnSysTestLightLHS, btnSysTestLightRHS, btnSysTestMouse, btnSysTestIMU, btnSysTestLineFL, btnSysTestLineCL, btnSysTestLineCR, btnSysTestLineFR, btnSysTestFastCharge, btnSysTestTWIMux, btnSysTestCamera
			};



			foreach (var toggleButton in togglebtnControls)
			{
				toggleButton.IsEnabled = false;
				toggleButton.Click += new RoutedEventHandler(btnSysTest_Click);
				toggleButton.Checked += new RoutedEventHandler(sysTestCheck);
				toggleButton.Unchecked += new RoutedEventHandler(sysTestCheck);
			}

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
					data[1] = proximitySensor[0];
					data[2] = request;
					xbee.SendTransmitRequest(XbeeHandler.DESTINATION.BROADCAST, data);
					break;

				case "Light":
					byte[] lightSensor = MJLib.StringToByteArrayFastest(tokens[1]);
					data = new byte[3];
					data[0] = 0xE5;
					data[1] = lightSensor[0];
					data[2] = request;
					xbee.SendTransmitRequest(XbeeHandler.DESTINATION.BROADCAST, data);
					break;

				case "Mouse":
					data = new byte[2];
					data[0] = 0xE7;
					data[1] = request;
					xbee.SendTransmitRequest(XbeeHandler.DESTINATION.BROADCAST, data);
					break;

				case "IMU":
					data = new byte[2];
					data[0] = 0xE8;
					data[1] = request;
					xbee.SendTransmitRequest(XbeeHandler.DESTINATION.BROADCAST, data);
					break;

				case "Line":
					byte[] lineSensor = MJLib.StringToByteArrayFastest(tokens[1]);
					data = new byte[3];
					data[0] = 0xE9;
					data[1] = lineSensor[0];
					data[2] = request;
					xbee.SendTransmitRequest(XbeeHandler.DESTINATION.BROADCAST, data);
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