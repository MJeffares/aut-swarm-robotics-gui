/**********************************************************************************************************************************************
*	File: Communications.cs
*
*	Developed By: Mansel Jeffares
*	First Build: 31 July 2017
*	Current Build: 12 September 2017
*
*	Description :
*		Classes and methods to manage our full communication stack, mainly UI related functions
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

//MANSEL: Merge this file with commmunication manger

/**********************************************************************************************************************************************
* Namespaces
**********************************************************************************************************************************************/
#region

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

#endregion




namespace SwarmRoboticsGUI
{
	public partial class MainWindow : Window
	{
		private void dispSelectBtnPrevious_Click(object sender, RoutedEventArgs e)
		{
			if (dispSelectRobot.SelectedIndex > 0)
			{
				dispSelectRobot.SelectedIndex--;
			}
			else if (dispSelectRobot.SelectedIndex == 0)
			{
				dispSelectRobot.SelectedIndex = dispSelectRobot.Items.Count - 1;
			}
		}

		private void dispSelectBtnNext_Click(object sender, RoutedEventArgs e)
		{
			if (dispSelectRobot.SelectedIndex < dispSelectRobot.Items.Count - 1)
			{
				dispSelectRobot.SelectedIndex++;
			}
			else if (dispSelectRobot.SelectedIndex == dispSelectRobot.Items.Count - 1)
			{
				dispSelectRobot.SelectedIndex = 0;
			}
		}

        private void dispSelectRobot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox CBsender = sender as ComboBox;
			TempRobotClass selected = (TempRobotClass)CBsender.SelectedItem;
			commManger.currentTargetRobot = selected.ID;
        }
	}
}