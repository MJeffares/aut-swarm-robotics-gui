
using System.Windows;

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
	}
}