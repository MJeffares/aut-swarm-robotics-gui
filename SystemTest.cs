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
using System.Text.RegularExpressions;
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
		bool testMode = false;
		bool fullSystemTest = false;
		int currentTestItem = 0;

		public void setupSystemTest()
		{
			togglebtnControls = new List<ToggleButton>()
			{
				 btnSysTestProxmityA,btnSysTestProxmityB, btnSysTestProxmityC, btnSysTestProxmityD, btnSysTestProxmityE, btnSysTestProxmityF
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

		private void btnSysTestFullTest_Click(object sender, RoutedEventArgs e)
		{
			if (fullSystemTest == false)
			{
				btnSysTestFullTest.Content = "End Full Systems Test";
				togglebtnControls[currentTestItem].IsChecked = false;
				fullSystemTest = true;
				btnSysTestNextTest.IsEnabled = true;
				btnSysTestPreviousTest.IsEnabled = true;
				currentTestItem = 0;
				togglebtnControls[currentTestItem].IsChecked = true;
			}
			else
			{
				btnSysTestFullTest.Content = "Begin Full Systems Test";
				fullSystemTest = false;
				btnSysTestNextTest.IsEnabled = false;
				btnSysTestPreviousTest.IsEnabled = false;
				togglebtnControls[currentTestItem].IsChecked = false;


			}
		}

		private void btnSysTestPreviousTest_Click(object sender, RoutedEventArgs e)
		{
			togglebtnControls[currentTestItem].IsChecked = false;
			currentTestItem--;

			if (currentTestItem < 0)
			{
				currentTestItem = togglebtnControls.Count-1;
			}

			togglebtnControls[currentTestItem].IsChecked = true;
		}

		private void btnSysTestNextTest_Click(object sender, RoutedEventArgs e)
		{
			togglebtnControls[currentTestItem].IsChecked = false;
			currentTestItem++;

			if(currentTestItem == togglebtnControls.Count)
			{
				currentTestItem = 0;
			}

			togglebtnControls[currentTestItem].IsChecked = true;
		}

		private void btnSysTest_Click(object sender, RoutedEventArgs e)
		{
			var senderToggleButton = sender as ToggleButton;

			if (fullSystemTest == true && currentTestItem == togglebtnControls.IndexOf(senderToggleButton))
			{
				togglebtnControls[currentTestItem].IsChecked = true;
			}
			else if(currentTestItem != togglebtnControls.IndexOf(senderToggleButton))
			{
				togglebtnControls[currentTestItem].IsChecked = false;
				currentTestItem = togglebtnControls.IndexOf(senderToggleButton);
			}			
		}

		private void sysTestCheck(object sender, RoutedEventArgs e)
		{

		}
	}
}