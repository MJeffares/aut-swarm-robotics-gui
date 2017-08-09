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
            
            public static byte[] NORTHEAST = { 0x01, 0x3B };
            public static byte[] EAST = { 0x01, 0x0E };
            public static byte[] SOUTHEAST = { 0x00, 0xE1 };
            public static byte[] SOUTH =  { 0x00, 0xB4 };
            public static byte[] SOUTHWEST = { 0x00, 0x87 };
            public static byte[] WEST = { 0x00, 0x5A };
            public static byte[] NORTHWEST = { 0x00, 0x2D };
        }
        private static class MANUAL_MODE_MESSAGE
        {
            public const byte Stop = 0xD0;
            public const byte MoveDirection = 0xD1;
            public const byte RotateClockWise = 0xD2;
            public const byte RotateCounterClockWise = 0xD3;
        }

        
        private void ManualModeDirectionMouseEnter(object sender, MouseEventArgs e)
        {
            Button control = sender as Button;
            byte[] data = new byte[4];
            data[0] = MANUAL_MODE_MESSAGE.MoveDirection;

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
            xbee.SendTransmitRequest(XbeeAPI.DESTINATION.ROBOT_TWO, data);
        }

        private void ManualModeMouseLeave(object sender, MouseEventArgs e)
        {
            byte[] data = new byte[1] { MANUAL_MODE_MESSAGE.Stop };
            xbee.SendTransmitRequest(XbeeAPI.DESTINATION.ROBOT_TWO, data);
        }

        private void ManualModeRotateMouseEnter(object sender, MouseEventArgs e)
        {
            Button control = sender as Button;
            byte[] data = new byte[2];

            switch(control.Name)
            {
                case "btManualModeCW":
                    data[0] = MANUAL_MODE_MESSAGE.RotateClockWise;
                    break;

                case "btManualModeCCW":
                    data[0] = MANUAL_MODE_MESSAGE.RotateCounterClockWise;
                    break;
            }
            data[1] = (byte) Convert.ToInt16(tbManualModeSpeed.Text);
            xbee.SendTransmitRequest(XbeeAPI.DESTINATION.ROBOT_TWO, data);
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
    }
}
