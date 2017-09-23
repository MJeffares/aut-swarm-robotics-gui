/**********************************************************************************************************************************************
*	File: SwarmManager.cs
*
*	Developed By: Mansel Jeffares
*	First Build: 19 October 2017
*	Current Build: 19 October 2017
*
*	Description :
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



using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;




namespace SwarmRoboticsGUI
{
 //   public partial class MainWindow : Window
	//{

 //       public List<ToggleButton> dockLightControls;

 //       public void TowerControlSetup()
 //       {


 //           dockLightControls = new List<ToggleButton>()
	//		{
	//			 btnDockLightA, btnDockLightB, btnDockLightC, btnDockLightD, btnDockLightE, btnDockLightF 
	//		};

 //           foreach (var toggleButton in dockLightControls)
 //           {
 //               toggleButton.IsEnabled = true;
 //               toggleButton.Click += new RoutedEventHandler(btnDockLight_Click);
 //           }
 //       }

 //       private void btnDockLight_Click(object sender, RoutedEventArgs e)
 //       {
 //           var senderToggleButton = sender as ToggleButton;
 //           byte[] data;

 //           data = new byte[3];
 //           data[0] = ProtocolClass.MESSAGE_TYPES.TOWER_LIGHT_SENSORS;
 //           byte[] lightsensor = MJLib.StringToByteArrayFastest(senderToggleButton.Tag.ToString());

 //           if (senderToggleButton.IsChecked == true)
 //           {
 //               data[1] = REQUEST.SINGLE_SAMPLE;
                
 //           }
 //           else
 //           {
 //               data[1] = REQUEST.STOP_STREAMING;
 //           }

 //           data[2] = lightsensor[0];
            
 //           ChargingDockItem Dock = (ChargingDockItem) ItemList.First(D => D is ChargingDockItem);
 //           xbee.SendTransmitRequest(Dock.Address64, data);
 //       }
 //   }
}