
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

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
			//KeyValuePair<string, UInt64> selected = (KeyValuePair<string, UInt64>)CBsender.SelectedItem;
			//commManger.currentTargetRobot = selected.Value;

			TempRobotClass selected = (TempRobotClass)CBsender.SelectedItem;
			commManger.currentTargetRobot = selected.ID;
        }
	}
}