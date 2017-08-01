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
            
            public static byte[] NORTHEAST = { 0x00, 0x2D };
            public static byte[] EAST = { 0x00, 0x5A };
            public static byte[] SOUTHEAST = { 0x00, 0x87 };
            public static byte[] SOUTH =  { 0x00, 0xB4 };
            public static byte[] SOUTHWEST = { 0x00, 0xE1 };
            public static byte[] WEST = { 0x01, 0x0E };
            public static byte[] NORTHWEST = { 0x01, 0x3B };

        }
        private static class MANUAL_MODE_MESSAGE
        {
            public const byte MoveStop = 0xD0;
            public const byte MoveDirection = 0xD1;
            public const byte MoveRotate = 0xD2;
        }




        private void ManualModeMouseEnter(object sender, MouseEventArgs e)
        {
            Button control = sender as Button;
            byte[] data = new byte[4];
            data[0] = MANUAL_MODE_MESSAGE.MoveDirection;

            switch(control.Name)
            {
                case "btManualModeNorth":
                    Array.Copy(DIRECTION.NORTH, 0, data, 1, 2);
                    break;

                case "btManualModeNorthEast":
                    Array.Copy(DIRECTION.NORTHEAST, 0, data, 1, 2);
                    break;

                case "btManualModeEast":
                    Array.Copy(DIRECTION.EAST, 0, data, 1, 2);
                    break;

                case "btManualModeSouthEast":
                    Array.Copy(DIRECTION.SOUTHEAST, 0, data, 1, 2);
                    break;

                case "btManualModeSouth":
                    Array.Copy(DIRECTION.SOUTH, 0, data, 1, 2);
                    break;

                case "btManualModeSouthWest":
                    Array.Copy(DIRECTION.SOUTHWEST, 0, data, 1, 2);
                    break;

                case "btManualModeWest":
                    Array.Copy(DIRECTION.WEST, 0, data, 1, 2);
                    break;

                case "btManualModeNorthWest":
                    Array.Copy(DIRECTION.NORTHWEST, 0, data, 1, 2);
                    break;
            }
            data[3] = (byte) Convert.ToInt16(tbManualModeSpeed.Text);
            xbee.SendTransmitRequest(XbeeAPI.DESTINATION.BROADCAST, data);
        }

        private void ManualModeMouseLeave(object sender, MouseEventArgs e)
        {
            byte[] data = new byte[1] { MANUAL_MODE_MESSAGE.MoveStop };
            xbee.SendTransmitRequest(XbeeAPI.DESTINATION.BROADCAST, data);
        }


        private void tbManualModeSpeed_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void tbManualModeSpeed_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //e.Handled = !IsTextAllowed(e.Text);
            int num = IsValid(((TextBox)sender).Text + e.Text);
            if (num != -1)
            {
                ((TextBox)sender).Text = num.ToString();
                e.Handled = true;
            }
            else
            {
                e.Handled = true;
            }

            //e.Handled = !IsValid(((TextBox)sender).Text + e.Text);
        }


        public static int IsValid(string str)
        {
            int num;
            if (int.TryParse(str, out num))
            {
                if (num >= 0)
                {
                    if (num <= 100)
                    {
                        return num;
                    }
                    else
                    {
                        return 100;
                    }
                }
                else
                {
                    return 0;
                }
            }
            return -1;

            //return int.TryParse(str, out i) && i >= 0 && i <= 100;
        }

        private static bool IsTextAllowed(string text)
        {
            /*
            int num;
            if(int.TryParse(text,out num))
            {
                if(num >= 0)
                {
                    if (num <= 100)
                    {
                        return true;
                    }
                }
            }
            return false;
             * */

           // Regex regex = new Regex("[^0-9.-]+"); //regex that matches disallowed text
            Regex regex = new Regex("(\b[0-1][0-9][0-9]\b)");
            return !regex.IsMatch(text);
        }


    }
}
