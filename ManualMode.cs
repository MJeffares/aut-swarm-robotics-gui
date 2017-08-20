using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace SwarmRoboticsGUI
{
	public partial class MainWindow : Window
	{

        public static class DIRECTION
        {            
            public static byte[] NORTH = { 0x00, 0x00 };

            public static byte[] NORTHEAST = { 0x00, 0x3B };
            public static byte[] EAST = { 0x00, 0x5A };
            public static byte[] SOUTHEAST = { 0x00, 0x87 };
            public static byte[] SOUTH =  { 0x00, 0xB4 };
            public static byte[] SOUTHWEST = { 0x00, 0xE1  };
            public static byte[] WEST = { 0x01, 0x0E };
            public static byte[] NORTHWEST = { 0x01, 0x2D };
        }

        private static class ROBOT_CONTROL_MESSAGE
        {
            public const byte Stop = 0xD0;
            public const byte MoveDirection = 0xD1;
            public const byte RotateClockWise = 0xD2;
            public const byte RotateCounterClockWise = 0xD3;
            public const byte MoveRandomly = 0xD4;
            public const byte DockViaLight = 0xD5;
            public const byte DockViaLine = 0xD6;
            public const byte Dock = 0xD7;
            public const byte StopObstacleAvoidance = 0xD8;
            public const byte StartObstacleAvoidance = 0xD9;
            public const byte FollowLight = 0xDA;
            public const byte FollowLine = 0xDB;
            public const byte RotateToHeading = 0xDC;
            public const byte MoveToPosition = 0xDD;
            public const byte MoveAtHeading = 0xDE;
        }
		
        private void ManualModeDirectionMouseEnter(object sender, MouseEventArgs e)
        {
            Button control = sender as Button;
            byte[] data = new byte[4];
            data[0] = ROBOT_CONTROL_MESSAGE.MoveDirection;

            switch(control.Name)
            {
                case "btManualModeN":
                    Array.Copy(DIRECTION.NORTH, 0, data, 1, 2);
                    break;

                case "btManualModeNE":
                    Array.Copy(DIRECTION.NORTHEAST, 0, data, 1, 2);
                    break;

                case "btManualModeE":
                    Array.Copy(DIRECTION.EAST, 0, data, 1, 2);
                    break;

                case "btManualModeSE":
                    Array.Copy(DIRECTION.SOUTHEAST, 0, data, 1, 2);
                    break;

                case "btManualModeS":
                    Array.Copy(DIRECTION.SOUTH, 0, data, 1, 2);
                    break;

                case "btManualModeSW":
                    Array.Copy(DIRECTION.SOUTHWEST, 0, data, 1, 2);
                    break;

                case "btManualModeW":
                    Array.Copy(DIRECTION.WEST, 0, data, 1, 2);
                    break;

                case "btManualModeNW":
                    Array.Copy(DIRECTION.NORTHWEST, 0, data, 1, 2);
                    break;  
            }
            data[3] = (byte) Convert.ToInt16(tbManualModeSpeed.Text);
            xbee.SendTransmitRequest(commManger.currentTargetRobot, data);
        }

        private void ManualModeMouseLeave(object sender, MouseEventArgs e)
        {
            byte[] data = new byte[1] { ROBOT_CONTROL_MESSAGE.Stop };
            xbee.SendTransmitRequest(commManger.currentTargetRobot, data);
        }

        private void ManualModeRotateMouseEnter(object sender, MouseEventArgs e)
        {
            Button control = sender as Button;
            byte[] data = new byte[2];

            switch(control.Name)
            {
                case "btManualModeCW":
                    data[0] = ROBOT_CONTROL_MESSAGE.RotateClockWise;
                    break;

                case "btManualModeCCW":
                    data[0] = ROBOT_CONTROL_MESSAGE.RotateCounterClockWise;
                    break;
            }
            data[1] = (byte) Convert.ToInt16(tbManualModeSpeed.Text);
            xbee.SendTransmitRequest(commManger.currentTargetRobot, data);
        }

        private void tbManualModeSpeed_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int num;

            if (int.TryParse(((TextBox)sender).Text + e.Text, out num))
            {
                if (num >= 0)
                {
                    if (num > 100)
                    {
                        num = 100;
                    }
                    ((TextBox)sender).Text = num.ToString();   
                }
                e.Handled = true;
            }
        }
		


		private void robotTaskObstacleAvoidance_Click(object sender, RoutedEventArgs e)
		{
			CheckBox checkboxsender = sender as CheckBox;

            byte[] data;
            data = new byte[1];
           
			if(checkboxsender.IsChecked == true)
			{
                data[0] = ROBOT_CONTROL_MESSAGE.StartObstacleAvoidance;
			}
			else
			{
                data[0] = ROBOT_CONTROL_MESSAGE.StopObstacleAvoidance;
			}
            xbee.SendTransmitRequest(commManger.currentTargetRobot, data);
		}

        private void robotTaskStopMoving_Click(object sender, RoutedEventArgs e)
        {
            byte[] data;
            data = new byte[1];
            data[0] = ROBOT_CONTROL_MESSAGE.Stop;

            xbee.SendTransmitRequest(commManger.currentTargetRobot, data);
        }

		private void robotTaskMoveRandomly_Click(object sender, RoutedEventArgs e)
		{
            byte[] data;
            data = new byte[1];
            data[0] = ROBOT_CONTROL_MESSAGE.MoveRandomly;

            xbee.SendTransmitRequest(commManger.currentTargetRobot, data);
		}

        private void robotTaskDock_Click(object sender, RoutedEventArgs e)
        {
            byte[] data;
            data = new byte[1];
            data[0] = ROBOT_CONTROL_MESSAGE.Dock;

            xbee.SendTransmitRequest(commManger.currentTargetRobot, data);
        }

		private void robotTaskDockViaLight_Click(object sender, RoutedEventArgs e)
		{
            byte[] data;
            data = new byte[1];
            data[0] = ROBOT_CONTROL_MESSAGE.DockViaLight;

            xbee.SendTransmitRequest(commManger.currentTargetRobot, data);
		}

		private void robotTaskDockViaLine_Click(object sender, RoutedEventArgs e)
		{
            byte[] data;
            data = new byte[1];
            data[0] = ROBOT_CONTROL_MESSAGE.DockViaLine;

            xbee.SendTransmitRequest(commManger.currentTargetRobot, data);
		}

		private void robotTaskMoveTowardsLight_Click(object sender, RoutedEventArgs e)
		{
            byte[] data;
            data = new byte[1];
            data[0] = ROBOT_CONTROL_MESSAGE.FollowLight;

            xbee.SendTransmitRequest(commManger.currentTargetRobot, data);
		}

		private void robotTaskFollowLine_Click(object sender, RoutedEventArgs e)
		{
            byte[] data;
            data = new byte[1];
            data[0] = ROBOT_CONTROL_MESSAGE.FollowLine;

            xbee.SendTransmitRequest(commManger.currentTargetRobot, data);
		}

	}
}
