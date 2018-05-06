using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WPFCustomMessageBox;
using XbeeHandler;
using XbeeHandler.XbeeFrames;

namespace SwarmRoboticsGUI
{
    public static class ROBOT_CONTROL_MESSAGE
    {
        public const byte Stop = 0xD0;
        public const byte MoveDirection = 0xD1;
        public const byte RotateClockWise = 0xD2;
        public const byte RotateCounterClockWise = 0xD3;
        public const byte MoveRandomly = 0xD4;
        public const byte ReleaseDock = 0xD6;
        public const byte Dock = 0xD7;
        public const byte StopObstacleAvoidance = 0xD8;
        public const byte StartObstacleAvoidance = 0xD9;
        public const byte FollowLight = 0xDA;
        public const byte FollowLine = 0xDB;
        public const byte RotateToHeading = 0xDC;
        public const byte MoveToPosition = 0xDD;
        public const byte MoveAtHeading = 0xDE;
    }

    public partial class MainWindow : Window
    {
		#region Robot Camera

		public UInt16[] pixelData;
		public BitmapSource mybmpSource;


		public void TestImage()
		{
			double dpi = 96;
			int width = 310;
			int height = 240;
			pixelData = new UInt16[width * height];
			

			for (int y = 0; y < height; ++y)
			{
				int yIndex = y * width;
				for (int x = 0; x < width; ++x)
				{
					//pixelData[x + yIndex] = (UInt16)(x + y);
					pixelData[x + yIndex] = (UInt16)(0xfb96); //hot pink
				}
			}

			mybmpSource = BitmapSource.Create(width, height, dpi, dpi, PixelFormats.Bgr565, null, pixelData, width*2);

			testImage.Source = mybmpSource;
		}

		public void updatePixelData(UInt32 index, UInt16[] data)
		{
			Array.Copy(data, 0, pixelData, index, data.Length);
			double dpi = 96;
			int width = 310;
			int height = 240;
			BitmapSource bmpSource = BitmapSource.Create(width, height, dpi, dpi, PixelFormats.Bgr565, null, pixelData, width * 2);
			bmpSource.Freeze();
			

			//testImage.Source = bmpSource;
			UpdateImageSource(testImage, bmpSource);
		}

		
		public delegate void UpdateImageSourceCallback(Image img, BitmapSource source);
		public void UpdateImageSource(Image img, BitmapSource source)
		{
			//control.Text = TextAlignment;
			//lvCommunicatedMessages.Dispatcher.Invoke(new RefreshListViewCallback(this.Refresh));
			//control.Dispatcher.Invoke(new UpdateTextBoxCallback(this.UpdateText),new object { control, text } );

			img.Dispatcher.Invoke(new UpdateImageSourceCallback(this.UpdateSource), new object[] { img, source });
		}
		private void UpdateSource(Image img, BitmapSource source)
		{
			img.Source = source;
		}
		




		/*
		public delegate void UpdateTextBoxCallback(TextBox control, string text);
		public void UpdateTextBox(TextBox control, string text)
		{
			//control.Text = TextAlignment;
			//lvCommunicatedMessages.Dispatcher.Invoke(new RefreshListViewCallback(this.Refresh));
			//control.Dispatcher.Invoke(new UpdateTextBoxCallback(this.UpdateText),new object { control, text } );

			control.Dispatcher.Invoke(new UpdateTextBoxCallback(this.UpdateText), new object[] { control, text });
		}
		private void UpdateText(TextBox control, string text)
		{
			control.Text = text;
		}
		*/




		#endregion

		#region Tower Control

		public List<ToggleButton> dockLightControls;

        //Constructor
        public void TowerControlSetup()
        {
            dockLightControls = new List<ToggleButton>()
            {
                 btnDockLightA, btnDockLightB, btnDockLightC, btnDockLightD, btnDockLightE, btnDockLightF
            };

            foreach (var toggleButton in dockLightControls)
            {
                toggleButton.IsEnabled = true;
                toggleButton.Click += new RoutedEventHandler(btnDockLight_Click);
            }
        }

        //Button handlers
        private void btnDockLight_Click(object sender, RoutedEventArgs e)
        {
            var senderToggleButton = sender as ToggleButton;
            byte[] data;

            data = new byte[3];
            data[0] = ProtocolClass.MESSAGE_TYPES.CHARGING_STATION_LIGHT_SENSORS;
            byte[] lightsensor = MJLib.StringToByteArrayFastest(senderToggleButton.Tag.ToString());

            if (senderToggleButton.IsChecked == true)
            {
                data[1] = REQUEST.SINGLE_SAMPLE;

            }
            else
            {
                data[1] = REQUEST.STOP_STREAMING;
            }

            data[2] = lightsensor[0];

            ChargingDockItem Dock = (ChargingDockItem)ItemList.First(D => D is ChargingDockItem);
            xbee.SendTransmitRequest(((ICommunicates)Dock).Address64, data);
        }

        private void TowerDockingLights_Update(object sender, RoutedEventArgs e)
        {
            var senderCheckBox = sender as CheckBox;

            byte[] led = MJLib.StringToByteArrayFastest(senderCheckBox.Tag.ToString());

            ChargingDockItem Dock = (ChargingDockItem)ItemList.First(D => D is ChargingDockItem);

            if(senderCheckBox.IsChecked == true)
            {
                Dock.DockingLights &= (byte)~(byte)(1 << led[0]);
            }
            else
            {
                Dock.DockingLights |= (byte)(1 << led[0]);
            }

            byte[] data;
            data = new byte[3];
            data[0] = ProtocolClass.MESSAGE_TYPES.CHARGING_STATION_LEDS;
            data[1] = 0x01;
            data[2] = Dock.DockingLights;

            xbee.SendTransmitRequest(((ICommunicates)Dock).Address64, data);            
        }


        #endregion

        #region System Test
        public List<ToggleButton> togglebtnControls;
        ProgressWindow EstablishingCommunicationsWindow;
        public bool testMode = false;
        int currentTestItem = 0;
        bool doublecommandlockout = false;
        public Dictionary<string, byte> twiMuxAddresses;
        public void SetupSystemTest()
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
            data[1] = REQUEST.SINGLE_SAMPLE;

            xbee.SendTransmitRequest(commManger.currentTargetRobot, data);

            CommunicationManager.WaitForMessage tada = new CommunicationManager.WaitForMessage(0xE1, 15000, MyHandler);

            EstablishingCommunicationsWindow = new ProgressWindow("Establishing Communications", "Please wait while communications are tested.");
            EstablishingCommunicationsWindow.ShowDialog();
        }
        private async void btnSysTestPreviousTest_Click(object sender, RoutedEventArgs e)
        {
            togglebtnControls[currentTestItem].IsChecked = false;
            await Task.Delay(40);
            currentTestItem--;

            if (currentTestItem < 0)
            {
                currentTestItem = togglebtnControls.Count - 1;
            }

            togglebtnControls[currentTestItem].IsChecked = true;
            await Task.Delay(40);
        }
        private async void btnSysTestNextTest_Click(object sender, RoutedEventArgs e)
        {
            togglebtnControls[currentTestItem].IsChecked = false;
            await Task.Delay(40);
            currentTestItem++;

            if (currentTestItem == togglebtnControls.Count)
            {
                currentTestItem = 0;
            }

            togglebtnControls[currentTestItem].IsChecked = true;
            await Task.Delay(40);
        }
        private void btnSysTest_Click(object sender, RoutedEventArgs e)
        {
            var senderToggleButton = sender as ToggleButton;

            if (currentTestItem != togglebtnControls.IndexOf(senderToggleButton))
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
			public const byte IMAGE = 0x03;
            public const byte STOP_STREAMING = 0xFF;
        }
        public static class SYSTEM_TEST_MESSAGE
        {
            public const byte MODE = 0xE0;				//???
            public const byte COMMUNICATION = 0xE1;
			//0xE2  reserved
			//0xE3  reserved
			public const byte PROXIMITY = 0xE4;
            public const byte LIGHT = 0xE5;
            public const byte MOTORS = 0xE6;
            public const byte MOUSE = 0xE7;
            public const byte IMU = 0xE8;
            public const byte LINE_FOLLOWERS = 0xE9;
			//0xEA  reserved
			public const byte TWI_MUX = 0xEB;
			//0xEC  reserved
			public const byte CAMERA = 0xED;
			//0xEE  reserved
			//0xEF  reserved
		}
		private void updateSystemsTest(string test, byte request)
        {
            string[] tokens = test.Split(',');
            byte[] data;

            switch (tokens[0])
            {
                case "Proximity":
                    byte[] proximitySensor = MJLib.StringToByteArrayFastest(tokens[1]);
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

            byte[] data = new byte[4];
            data[0] = 0xE6;
			data[1] = 0x00;
            data[2] = motor[0];
			data[3] = 0x00;

			xbee.SendTransmitRequest(commManger.currentTargetRobot, data);
        }
        private void slMotor_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
			
            Slider slider = sender as Slider;
            byte[] motor = MJLib.StringToByteArrayFastest(slider.Tag.ToString());

            byte[] data = new byte[4];
            data[0] = 0xE6;
			data[1] = 0x02;
            data[2] = motor[0];

            if (slider.Value > 2)
            {
                data[3] = 0x80;
				data[3] += (byte)Math.Abs(slider.Value);
			}
			else if (slider.Value < -2)
			{
				data[3] += (byte)Math.Abs(slider.Value);
			}
			else
			{
				data[1] = 0x00;
				data[3] = 0x00;
			}
			
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
        #endregion

        #region Manual Mode
        public static class DIRECTION
        {
            public static byte[] NORTH = { 0x00, 0x00 };

            public static byte[] NORTHEAST = { 0x00, 0x2D };
            public static byte[] EAST = { 0x00, 0x5A };
            public static byte[] SOUTHEAST = { 0x00, 0x87 };
            public static byte[] SOUTH = { 0x00, 0xB4 };
            public static byte[] SOUTHWEST = { 0x00, 0xE1 };
            public static byte[] WEST = { 0x01, 0x0E };
            public static byte[] NORTHWEST = { 0x01, 0x2D };
        }
        //private static class ROBOT_CONTROL_MESSAGE
        //{
        //    public const byte Stop = 0xD0;
        //    public const byte MoveDirection = 0xD1;
        //    public const byte RotateClockWise = 0xD2;
        //    public const byte RotateCounterClockWise = 0xD3;
        //    public const byte MoveRandomly = 0xD4;
        //    public const byte ReleaseDock = 0xD6;
        //    public const byte Dock = 0xD7;
        //    public const byte StopObstacleAvoidance = 0xD8;
        //    public const byte StartObstacleAvoidance = 0xD9;
        //    public const byte FollowLight = 0xDA;
        //    public const byte FollowLine = 0xDB;
        //    public const byte RotateToHeading = 0xDC;
        //    public const byte MoveToPosition = 0xDD;
        //    public const byte MoveAtHeading = 0xDE;
        //}
        private void ManualModeDirectionMouseEnter(object sender, MouseEventArgs e)
        {
            Button control = sender as Button;
            byte[] data = new byte[4];
            data[0] = ROBOT_CONTROL_MESSAGE.MoveDirection;

            switch (control.Name)
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
            data[3] = (byte)Convert.ToInt16(tbManualModeSpeed.Text);
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

            switch (control.Name)
            {
                case "btManualModeCW":
                    data[0] = ROBOT_CONTROL_MESSAGE.RotateClockWise;
                    break;

                case "btManualModeCCW":
                    data[0] = ROBOT_CONTROL_MESSAGE.RotateCounterClockWise;
                    break;
            }
            data[1] = (byte)Convert.ToInt16(tbManualModeSpeed.Text);
            xbee.SendTransmitRequest(commManger.currentTargetRobot, data);
        }

        private void tbManualModeSpeed_TextChanged(object sender, TextChangedEventArgs e)
        {
            int num;

            if (int.TryParse(((TextBox)sender).Text, out num))
            {
                if (num >= 0)
                {
                    if (num > 100)
                    {
                        num = 100;
                    }
                    ((TextBox)sender).Text = num.ToString();
                }
            }
        }


        /*
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
         * */
        private void robotTaskObstacleAvoidance_Click(object sender, RoutedEventArgs e)
        {
            Button checkboxsender = sender as Button;

            byte[] data;
            data = new byte[1];

            data[0] = ROBOT_CONTROL_MESSAGE.StartObstacleAvoidance;

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
        private void robotTaskReleaseDock_Click(object sender, RoutedEventArgs e)
        {
            byte[] data;
            data = new byte[1];
            data[0] = ROBOT_CONTROL_MESSAGE.ReleaseDock;

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
        private void robotTaskRotateToHeading_Click(object sender, RoutedEventArgs e)
        {
            byte[] data;
            data = new byte[3];
            data[0] = ROBOT_CONTROL_MESSAGE.RotateToHeading;
            data[1] = (byte)(UDrobotTaskRotateToHeading.Value >> 8);
            data[2] = (byte)(UDrobotTaskRotateToHeading.Value);

            xbee.SendTransmitRequest(commManger.currentTargetRobot, data);
        }

        private void robotTaskMoveToPosition_Click(object sender, RoutedEventArgs e)
        {
            byte[] data;
            data = new byte[5];
            data[0] = ROBOT_CONTROL_MESSAGE.MoveToPosition;
            data[1] = (byte)(UBrobotTaskMoveToPositionX.Value >> 8);
            data[2] = (byte)(UBrobotTaskMoveToPositionX.Value);
            data[3] = (byte)(UBrobotTaskMoveToPositionY.Value >> 8);
            data[4] = (byte)(UBrobotTaskMoveToPositionY.Value);

            xbee.SendTransmitRequest(commManger.currentTargetRobot, data);
        }

        //private void tbRotateToHeading_PreviewTextInput(object sender, TextCompositionEventArgs e)
        //{
        //    int num;

        //    if (int.TryParse(((TextBox)sender).Text + e.Text, out num))
        //    {
        //        if (num >= 0)
        //        {
        //            if (num > 360)
        //            {
        //                num = 360;
        //            }
        //            ((TextBox)sender).Text = num.ToString();
        //        }
        //        e.Handled = true;
        //    }
        //}

        /*
        private void btnRotateToHeadingUp_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Button senderbutton = (Button) sender as Button;

            UInt16 num;
            int acc = 10;
            Timer t = new Timer();
            t.Interval = 50;

            UInt16.TryParse(tbRotateToHeading.Text, out num);

            while(senderbutton.IsPressed)
            {
                

                if (num == 360)
                {
                    num = 0;
                }
                else
                {
                    num = (UInt16)(num + (acc/10));
                    acc++;
                }
            }

            tbRotateToHeading.Text = num.ToString();
        }
        */

        //private void btnRotateToHeadingUp_Click(object sender, RoutedEventArgs e)
        //{
        //    int num;

        //    int.TryParse(tbRotateToHeading.Text, out num);

        //    if (num == 360)
        //    {
        //        num = 0;
        //    }
        //    else
        //    {
        //        num++;
        //    }

        //    tbRotateToHeading.Text = num.ToString();
        //}
        //private void btnRotateToHeadingDown_Click(object sender, RoutedEventArgs e)
        //{
        //    int num;

        //    int.TryParse(tbRotateToHeading.Text, out num);

        //    if (num == 0)
        //    {
        //        num = 360;
        //    }
        //    else
        //    {
        //        num--;
        //    }

        //    tbRotateToHeading.Text = num.ToString();
        //}
        #endregion

        // DONE
        #region Communications
        //private void dispSelectBtnPrevious_Click(object sender, RoutedEventArgs e)
        //{
        //    if (dispSelectRobot.SelectedIndex > 0)
        //    {
        //        dispSelectRobot.SelectedIndex--;
        //    }
        //    else if (dispSelectRobot.SelectedIndex == 0)
        //    {
        //        dispSelectRobot.SelectedIndex = dispSelectRobot.Items.Count - 1;
        //    }
        //}
        //private void dispSelectBtnNext_Click(object sender, RoutedEventArgs e)
        //{
        //    if (dispSelectRobot.SelectedIndex < dispSelectRobot.Items.Count - 1)
        //    {
        //        dispSelectRobot.SelectedIndex++;
        //    }
        //    else if (dispSelectRobot.SelectedIndex == dispSelectRobot.Items.Count - 1)
        //    {
        //        dispSelectRobot.SelectedIndex = 0;
        //    }
        //}
        //private void dispSelectRobot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    ComboBox CBsender = sender as ComboBox;
        //    CommunicationItem selected = (CommunicationItem)CBsender.SelectedItem;
        //    commManger.currentTargetRobot = selected.Address64;
        //    var hello = ItemsList1.SelectedItem;
        //}
        #endregion

        // WIP
        #region Communications Manager    
        //public delegate void RefreshListViewCallback();
        //public void RefreshListView()
        //{
        //    lvCommunicatedMessages.Dispatcher.Invoke(new RefreshListViewCallback(this.Refresh));

        //}
        //private GridViewColumnHeader lvCommunicatedMessagesSortCol { get; set; }
        //private SortAdorner lvCommunicatedMessagesSortAdorner { get; set; }
        //private void Refresh()
        //{
        //    if (lvCommunicatedMessagesSortAdorner != null && lvCommunicatedMessagesSortCol != null)
        //    {
        //        lvCommunicatedMessages.Items.SortDescriptions.Add(new SortDescription(lvCommunicatedMessagesSortCol.Tag.ToString(), lvCommunicatedMessagesSortAdorner.Direction));
        //    }


        //    lvCommunicatedMessages.Items.Refresh();
        //}
        //this stuff is temporary XXXX
        //private void receivedDataRemove_Click(object sender, RoutedEventArgs e)
        //{
        //    gvCommunicatedMessages.Columns.Remove(gvcTimeStamp);
        //}
        //private void receivedDataAdd_Click(object sender, RoutedEventArgs e)
        //{
        //    gvCommunicatedMessages.Columns.Add(gvcTimeStamp);
        //}
        //private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        //{
        //    GridViewColumnHeader column = (sender as GridViewColumnHeader);
        //    string sortBy = column.Tag.ToString();

        //    if (lvCommunicatedMessagesSortCol != null)
        //    {
        //        AdornerLayer.GetAdornerLayer(lvCommunicatedMessagesSortCol).Remove(lvCommunicatedMessagesSortAdorner);
        //        lvCommunicatedMessages.Items.SortDescriptions.Clear();
        //    }

        //    // Swap sorting direction if previous sorting was this column
        //    ListSortDirection newDir = ListSortDirection.Ascending;
        //    if (lvCommunicatedMessagesSortCol == column && lvCommunicatedMessagesSortAdorner.Direction == newDir)
        //        newDir = ListSortDirection.Descending;

        //    lvCommunicatedMessagesSortCol = column;
        //    lvCommunicatedMessagesSortAdorner = new SortAdorner(lvCommunicatedMessagesSortCol, newDir);
        //    AdornerLayer.GetAdornerLayer(lvCommunicatedMessagesSortCol).Add(lvCommunicatedMessagesSortAdorner);
        //    lvCommunicatedMessages.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
        //    //column.Width = column.ActualWidth + 10;
        //    lvCommunicatedMessages.Items.Refresh();
        //}
        //public class SortAdorner : Adorner
        //{
        //    private static Geometry ascGeometry =
        //            Geometry.Parse("M 0 4 L 3.5 0 L 7 4 Z");

        //    private static Geometry descGeometry =
        //            Geometry.Parse("M 0 0 L 3.5 4 L 7 0 Z");

        //    public ListSortDirection Direction { get; private set; }

        //    public SortAdorner(UIElement element, ListSortDirection dir)
        //            : base(element)
        //    {
        //        this.Direction = dir;
        //    }

        //    protected override void OnRender(DrawingContext drawingContext)
        //    {
        //        base.OnRender(drawingContext);

        //        if (AdornedElement.RenderSize.Width < 20)
        //            return;

        //        TranslateTransform transform = new TranslateTransform
        //                (
        //                        AdornedElement.RenderSize.Width - 15,
        //                        (AdornedElement.RenderSize.Height - 5) / 2
        //                );
        //        drawingContext.PushTransform(transform);

        //        Geometry geometry = ascGeometry;
        //        if (this.Direction == ListSortDirection.Descending)
        //            geometry = descGeometry;
        //        drawingContext.DrawGeometry(Brushes.Black, null, geometry);

        //        drawingContext.Pop();
        //    }
        //}
        //public delegate void UpdateListViewBindingCallback(List<XbeeAPIFrame> messages);
        //public void UpdateListViewBinding(List<XbeeAPIFrame> messages)
        //{
        //    lvCommunicatedMessages.Dispatcher.Invoke(new UpdateListViewBindingCallback(this.UpdateBinding), new object[] { messages });
        //}
        //private void UpdateBinding(List<XbeeAPIFrame> messages)
        //{
        //    DataContext = this;
        //    lvCommunicatedMessages.ItemsSource = messages;
        //    lvCommunicatedMessages.Items.Refresh();
        //}

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (serial._serialPort.IsOpen)
            {
                rtbSendBuffer.SelectAll();
                string text = rtbSendBuffer.Selection.Text.ToString();
                string textToSend = text;

                textToSend = textToSend.Replace("\r", string.Empty);
                textToSend = textToSend.Replace("\n", string.Empty);
                textToSend = textToSend.Replace(" ", string.Empty);
                textToSend = textToSend.Replace("-", string.Empty);
                textToSend = textToSend.Replace("0x", string.Empty);

                text = text.Replace("\n", string.Empty);
                text = text.Replace(" ", "-");

                try
                {
                    byte[] bytes = bytes = Enumerable.Range(0, textToSend.Length).Where(x => x % 2 == 0).Select(x => Convert.ToByte(textToSend.Substring(x, 2), 16)).ToArray();


                    //test = xbee.CalculateChecksum(bytes); //			

                    //bytes = xbee.Escape(bytes); //escapes bytes


                    serial._serialPort.Write(bytes, 0, bytes.Length);
                }
                catch (Exception excpt)
                {
                    MessageBox.Show(excpt.Message);
                }


                rtbSerialSent.AppendText(text);
                //rtbSerialSent.AppendText(test.ToString()); //
                rtbSendBuffer.Document.Blocks.Clear();
                rtbSerialSent.ScrollToEnd();
            }
            else
            {
                MessageBox.Show("Port not open");
            }
        }
        public bool messageReceived = false;
        private void btnXbeeSend_Click(object sender, RoutedEventArgs e)
        {
            if (serial._serialPort.IsOpen)
            {
                //byte test = 0; //

                rtbXbeeSendBuffer.SelectAll();
                string text = rtbXbeeSendBuffer.Selection.Text.ToString();
                string textToSend = text;

                textToSend = textToSend.Replace("\r", string.Empty);
                textToSend = textToSend.Replace("\n", string.Empty);
                textToSend = textToSend.Replace(" ", string.Empty);
                textToSend = textToSend.Replace("-", string.Empty);
                textToSend = textToSend.Replace("0x", string.Empty);

                text = text.Replace("\n", string.Empty);
                text = text.Replace(" ", "-");

                try
                {
                    byte[] bytes = bytes = Enumerable.Range(0, textToSend.Length).Where(x => x % 2 == 0).Select(x => Convert.ToByte(textToSend.Substring(x, 2), 16)).ToArray();


                    //test = xbee.CalculateChecksum(bytes); //



                    //bytes = xbee.Escape(bytes); //escapes bytes

                    xbee.SendTransmitRequest(XbeeAPI.DESTINATION.COORDINATOR, bytes);

                    //serial._serialPort.Write(bytes, 0, bytes.Length);
                }
                catch (Exception excpt)
                {
                    MessageBox.Show(excpt.Message);
                }


                rtbXbeeSent.AppendText(text);
                //rtbSerialSent.AppendText(test.ToString()); 
                rtbXbeeSendBuffer.Document.Blocks.Clear();
                rtbXbeeSent.ScrollToEnd();
            }
            else
            {
                MessageBox.Show("Port not open");
            }
        }
        public delegate void UpdateTextBoxCallback(TextBox control, string text);
        public void UpdateTextBox(TextBox control, string text)
        {
            //control.Text = TextAlignment;
            //lvCommunicatedMessages.Dispatcher.Invoke(new RefreshListViewCallback(this.Refresh));
            //control.Dispatcher.Invoke(new UpdateTextBoxCallback(this.UpdateText),new object { control, text } );

            control.Dispatcher.Invoke(new UpdateTextBoxCallback(this.UpdateText), new object[] { control, text });
        }
        private void UpdateText(TextBox control, string text)
        {
            control.Text = text;
        }


        /*
		public delegate void UpdateTextCallback(string text);

		public void UpdateSerialReceivedTextBox(string text)
		{
			rtbSerialReceived.Dispatcher.Invoke(new UpdateTextCallback(this.UpdateText), new object[] { text });
		}

		public void UpdateSerialReceivedTextBox(byte[] message, int number)
		{
			string messageString = null;

			for (int i = 0; i < number; i++)
			{

				string temp = message[i].ToString("X");

				if (temp == "7E")
				{
					messageString += "\r";
					messageString += temp;
				}
				else if (message[i] < 0x10)
				{
					messageString += "0";
					messageString += temp;
				}
				else
				{
					messageString += temp;
				}
			}

			rtbSerialReceived.Dispatcher.Invoke(new UpdateTextCallback(this.UpdateText), new object[] { messageString });
			
		}
		
		private void UpdateText(string text)
		{
			rtbSerialReceived.AppendText(text);
			rtbSerialReceived.ScrollToEnd();
		}

		private void receivedDataNewline_Click(object sender, RoutedEventArgs e)
		{
			rtbSerialReceived.AppendText("\r");
			rtbSerialReceived.ScrollToEnd(); ;
		}


		private void receivedDataClear_Click(object sender, RoutedEventArgs e)
		{
			rtbSerialReceived.Document.Blocks.Clear();
			rtbSerialReceived.ScrollToEnd(); ;
		}
		*/
        #endregion

        #region Serial Communication Converted
        // supported serial port settings
        private string[] baudRateOptions = new string[] { "4800", "9600", "19200", "38400", "57600", "115200", "230400", "460800", "921600" };
        private string[] parityOptions = new string[] { "None", "Odd", "Even", "Mark", "Space" };
        private string[] dataBitOptions = new string[] { "8", "7", "6", "5" };
        private string[] stopBitOptions = new string[] { "None", "One", "One Point Five", "Two" };
        private string[] handshakingOptions = new string[] { "None", "XOnXOff", "RequestToSend", "RequestToSendXOnXOff" };

        // default serial port settings
        private const string DEFAULT_BAUD_RATE = "230400";
        private const string DEFAULT_PARITY = "None";
        private const string DEFAULT_DATA_BITS = "8";
        private const string DEFAULT_STOP_BITS = "One";
        private const string DEFAULT_HANDSHAKING = "None";

        private MenuItem portList = null;
        private MenuItem baudList = null;
        private MenuItem parityList = null;
        private MenuItem dataBitsList = null;
        private MenuItem stopBitsList = null;
        private MenuItem handshakingList { get; set; }
        private MenuItem connectButton { get; set; }

        private void PopulateSerialSettings()
        {
            portList = MJLib.CreateMenuItem("Communication Port");
            baudList = MJLib.CreateMenuItem("Baud Rate");
            parityList = MJLib.CreateMenuItem("Parity");
            dataBitsList = MJLib.CreateMenuItem("Data Bits");
            stopBitsList = MJLib.CreateMenuItem("Stop Bits");
            handshakingList = MJLib.CreateMenuItem("Handshaking");
            connectButton = MJLib.CreateMenuItem("Connect");

            connectButton.IsEnabled = false;
            connectButton.IsCheckable = true;

            menuCommunication.Items.Add(portList);
            menuCommunication.Items.Add(baudList);
            menuCommunication.Items.Add(parityList);
            menuCommunication.Items.Add(dataBitsList);
            menuCommunication.Items.Add(stopBitsList);
            menuCommunication.Items.Add(handshakingList);
            menuCommunication.Items.Add(connectButton);

            MJLib.PopulateMenuItemList(baudList, baudRateOptions, DEFAULT_BAUD_RATE, MJLib.menuMutuallyExclusiveMenuItem_Click);
            MJLib.PopulateMenuItemList(parityList, parityOptions, DEFAULT_PARITY, MJLib.menuMutuallyExclusiveMenuItem_Click);
            MJLib.PopulateMenuItemList(dataBitsList, dataBitOptions, DEFAULT_DATA_BITS, MJLib.menuMutuallyExclusiveMenuItem_Click);
            MJLib.PopulateMenuItemList(stopBitsList, stopBitOptions, DEFAULT_STOP_BITS, MJLib.menuMutuallyExclusiveMenuItem_Click);
            MJLib.PopulateMenuItemList(handshakingList, handshakingOptions, DEFAULT_HANDSHAKING, MJLib.menuMutuallyExclusiveMenuItem_Click);
        }

        private void PopulateSerialPorts()
        {
            connectButton.IsEnabled = false;        

            string[] ports = serial.GetOpenPorts();

            if (ports != null)
            {
                portList.Items.Clear();

                foreach (string port in ports)
                {
                    MenuItem item = new MenuItem { Header = port };
                    item.Click += new RoutedEventHandler(menuCommunicationPortListItem_Click);
                    item.IsCheckable = true;

                    portList.Items.Add(item);

                    if (item.ToString() == serial.currentlyConnectedPort)
                    {
                        item.IsChecked = true;
                        connectButton.IsEnabled = true;
                    }

					if(serial.IsConnected)
					{
						item.IsEnabled = false;
					}
                }

				if (ports.Length == 0)
				{
					MenuItem nonefound = new MenuItem { Header = "No Com Ports Found" };
					portList.Items.Add(nonefound);
					nonefound.IsEnabled = false;
					connectButton.IsEnabled = false;
				}
			}
		}

        public void menuPopulateSerialPorts(object sender, MouseEventArgs e)
        {
            PopulateSerialPorts();
        }

        public void menuCommunicationPortListItem_Click(object sender, RoutedEventArgs e)
        {
            MJLib.menuMutuallyExclusiveMenuItem_Click(sender, e);
            connectButton.IsEnabled = true;
            serial.currentlyConnectedPort = sender.ToString();
        }


        public void menuCommunicationConnect_Click(object sender, RoutedEventArgs e)
        {
            var port = MJLib.GetCheckedItemInList(portList, true).Header.ToString();
            var baud = MJLib.GetCheckedItemInList(baudList, true).Header.ToString();
            var parity = MJLib.GetCheckedItemInList(parityList, true).Header.ToString();
            var data = MJLib.GetCheckedItemInList(dataBitsList, true).Header.ToString();
            var stop = MJLib.GetCheckedItemInList(stopBitsList, true).Header.ToString();
            var handshake = MJLib.GetCheckedItemInList(handshakingList, true).Header.ToString();

            if (!serial.IsConnected)
                serial.Connect(port, baud, parity, data, stop, handshake);
            else
            {
                serial.Disconnect();

				////creates a list with all settings in it
				MenuItem[] ports = portList.Items.OfType<MenuItem>().ToArray();
				MenuItem[] bauds = baudList.Items.OfType<MenuItem>().ToArray();
				MenuItem[] paritys = parityList.Items.OfType<MenuItem>().ToArray();
				MenuItem[] datas = dataBitsList.Items.OfType<MenuItem>().ToArray();
				MenuItem[] stops = stopBitsList.Items.OfType<MenuItem>().ToArray();
				MenuItem[] handshakes = handshakingList.Items.OfType<MenuItem>().ToArray();

				List<MenuItem> itemList = new List<MenuItem>(ports.Concat<MenuItem>(ports));
				itemList.AddRange(bauds);
				itemList.AddRange(paritys);
				itemList.AddRange(datas);
				itemList.AddRange(stops);
				itemList.AddRange(handshakes);

				MenuItem[] finalArray = itemList.ToArray();

				//re-enables all settings
				foreach (var item in finalArray)
				{
					item.IsEnabled = true;
				}

				connectButton.Header = "Connect";
            }
            connectButton.IsChecked = false;

            if (serial.IsConnected)
            {
                connectButton.Header = "Disconnect";

                foreach (TabItem item in tcCenter.Items)
                {
                    item.IsEnabled = true;
                    item.Visibility = Visibility.Visible;
                }
                tcCenter.SelectedIndex++;
                nc.IsEnabled = false;
                nc.Visibility = Visibility.Collapsed;
            }
        }
        #endregion
    }
}
