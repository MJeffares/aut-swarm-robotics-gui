/**********************************************************************************************************************************************
*	File: GeneralUtilities.cs
*
*	Developed By: Mansel Jeffares
*	First Build: 16 April 2017
*	Current Build:  17 April 2017
*
*	Description :
*		General Ulilities for use with WPF and C#
*		Built for x64, .NET 4.5.2
*
*	Limitations :
*		None, Unlimitied Power!
*   
*	Naming Conventions:
*		Variables, camelCase, start lower case, subsequent words also upper case, if another object goes by the same name, then also with an underscore
*		Methods, PascalCase, start upper case, subsequent words also upper case
*		Constants, all upper case, unscores for seperation
* 
**********************************************************************************************************************************************/

/**********************************************************************************************************************************************
* Namespaces
**********************************************************************************************************************************************/
#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

#endregion

/**********************************************************************************************************************************************
* Structures and Classes
**********************************************************************************************************************************************/
#region
public static class ExtensionMethod
{
	public static LinkedListNode<T> getNodeAt<T>(this LinkedList<T> _list, int position)
	{
		LinkedListNode<T> mark = _list.First;
		int i = 0;
		while (i < position)
		{
			mark = mark.Next;
			i++;
		}
		return mark;
	}
}

static class MJLib
{
	/// <summary>
	/// Converts a byte to hexidecimal string
	/// </summary>
	/// <param name="data">Sender object from handler</param>
	/// <param name="prefix">If true will prefix string with "0x"</param>
	/// <returns>A string</returns>
	public static string HexToString(byte data, bool prefix)
	{
		string messageString = null;    //temporary string to hold results

		//if 0x prefix is required
		if (prefix)
		{
			messageString += "0x";
		}

		//by default if byte is less than 0x10 the leading zero isnt added we amnmend this
		string temp = data.ToString("X");		
		if (data < 0x10)
		{
			messageString += "0";
			messageString += temp;
		}
		else
		{
			messageString += temp;
		}

		return messageString;
	}
	/// <summary>
	/// Converts a byte array to hexidecimal string
	/// </summary>
	/// <param name="data">Sender object from handler</param>
	/// <param name="index">Index of byte array to start at</param>
	/// <param name="length">Number of bytes from index to convert</param>
	/// <param name="prefix">If true will prefix string with "0x"</param>
	/// <returns>A string</returns>
	public static string HexToString(byte[] data, int index, int length, bool prefix)
	{
		//if there is nothing to convert we return null
		if (data == null)
		{
			return null;
		}
		else
		{
			string messageString = null;	//temporary string to hold results

			//if 0x prefix is required
			if(prefix)
			{
				messageString += "0x";
			}

			//loop does the conversion
			for (int i = index; i < index+length ; i++)
			{
				//by default if byte is less than 0x10 the leading zero isnt added we amnmend this
				string temp = data[i].ToString("X");				
				if (data[i] < 0x10)
				{
					messageString += "0";
					messageString += temp;
				}
				else
				{
					messageString += temp;
				}
			}
			return messageString;
		}
	}

	/// <summary>
	/// Converts a byte array to UInt64
	/// </summary>
	/// <param name="array">Array to get the bytes from</param>
	/// <param name="index">Index of byte array to start at</param>
	/// <param name="endianess">Endianess of the input, false for MSB first, true for LSB first</param>
	/// <returns>A string</returns>
	public static UInt64 ByteArrayToUInt64(byte[] array, int index, bool endianess = false)
	{
		UInt64 output = 0;

		if(endianess)
		{
			for (int i = 0; i < index + 8; i++)
			{
				output += array[index + i] * (UInt64)Math.Pow(16, i*2);
			}
		}
		else
		{
			
			for (int i = 0; i < index + 8; i++)
			{
				output += array[index + i] * (UInt64)Math.Pow(16, 14 - i*2);
			}
		}
		return output;
	}

	/// <summary>
	/// Converts a byte array to UInt16
	/// </summary>
	/// <param name="array">Array to get the bytes from</param>
	/// <param name="index">Index of byte array to start at</param>
	/// <param name="endianess">Endianess of the input, false for MSB first, true for LSB first</param>
	/// <returns>A string</returns>
	public static UInt16 ByteArrayToUInt16(byte[] array, int index, bool endianess = false)
	{
		UInt16 output = 0;

		if (endianess)
		{
			output += (UInt16)(array[index + 0] * (UInt16)Math.Pow(16, 0));
			output += (UInt16)(array[index + 1] * (UInt16)Math.Pow(16, 2));
		}
		else
		{
			output += (UInt16)(array[index + 0] * (UInt16)Math.Pow(16, 2));
			output += (UInt16)(array[index + 1] * (UInt16)Math.Pow(16, 0));
		}
		return output;
	}


	//MANSEL: document this
	public static class TypeSwitch
	{
		public class CaseInfo
		{
			public bool IsDefault { get; set; }
			public Type Target { get; set; }
			public Action<object> Action { get; set; }
		}

		public static void Do(object source, params CaseInfo[] cases)
		{
			var type = source.GetType();
			foreach (var entry in cases)
			{
				if (entry.IsDefault || entry.Target.IsAssignableFrom(type))
				{
					entry.Action(source);
					break;
				}
			}
		}

		public static CaseInfo Case<T>(Action action)
		{
			return new CaseInfo()
			{
				Action = x => action(),
				Target = typeof(T)
			};
		}

		public static CaseInfo Case<T>(Action<T> action)
		{
			return new CaseInfo()
			{
				Action = (x) => action((T)x),
				Target = typeof(T)
			};
		}

		public static CaseInfo Default(Action action)
		{
			return new CaseInfo()
			{
				Action = x => action(),
				IsDefault = true
			};
		}
	}


	//MANSEL: document this
	public static byte[] StringToByteArrayFastest(string hex)
    {
		if (hex.Length % 2 == 1)
           throw new Exception("The binary key cannot have an odd number of digits");

       byte[] arr = new byte[hex.Length >> 1];

        for (int i = 0; i < (hex.Length >> 1); ++i)
        {
            arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
        }

        return arr;
    }

    public static int GetHexVal(char hex)
    {
        int val = (int)hex;
        //For uppercase A-F letters:
        return val - (val < 58 ? 48 : 55);
        //For lowercase a-f letters:
        //return val - (val < 58 ? 48 : 87);
        //Or the two combined, but a bit slower:
        //return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
    }

	//MANSEL: document this
	public class AutoClosingMessageBox
	{
		System.Threading.Timer _timeoutTimer;
		string _caption;

		AutoClosingMessageBox(string text, string caption, int timeout)
		{
			_caption = caption;
			_timeoutTimer = new System.Threading.Timer(OnTimerElapsed,
				null, timeout, System.Threading.Timeout.Infinite);
			MessageBox.Show(text, caption);
		}

		public static void Show(string text, string caption, int timeout)
		{
			new AutoClosingMessageBox(text, caption, timeout);
		}

		public static void Close(string title)
		{
			IntPtr mbWnd = FindWindow(null, title);
			if (mbWnd != IntPtr.Zero)
				SendMessage(mbWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
		}

		public void OnTimerElapsed(object state)
		{
			IntPtr mbWnd = FindWindow(null, _caption);
			if (mbWnd != IntPtr.Zero)
				SendMessage(mbWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
			_timeoutTimer.Dispose();
		}

		const int WM_CLOSE = 0x0010;
		[System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
		static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
		[System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
		static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
	}




	/// <summary>
	/// Handles exclusively checking a menu item within a list
	/// </summary>
	/// <param name="sender">Sender object from handler</param>
	/// <param name="e">Routed Event Arugments from handler</param>
	/// <returns>nothing</returns>
	public static void menuMutuallyExclusiveMenuItem_Click(object sender, RoutedEventArgs e)
	{
		//gets the sender and parent as local addressable variables
		MenuItem menusender = (MenuItem)sender;
		MenuItem parent = (MenuItem)menusender.Parent;

		//creates a list of all items under the menu
		var allitems = parent.Items.OfType<MenuItem>().ToArray();

		//unchecks all items in the list
		foreach (var item in allitems)
		{
			item.IsChecked = false;
		}

		//re-checks only the item that has been clicked
		menusender.IsChecked = true;
	}
	/// <summary>
	/// Handles exclusively checking a menu item within a list, only makes a change if a condition is met (check == condition)
	/// </summary>
	/// <param name="sender">Sender object from handler</param>
	/// <param name="e">Routed Event Arugments from handler</param>
	/// <param name="check">What parameter to check</param>
	/// <param name="condition">What the parameter is required to meet</param>
	/// <returns>nothing</returns>
	public static void menuMutuallyExclusiveMenuItem_Click(object sender, RoutedEventArgs e, int check, int condition)
	{
		//gets the sender and parent as local addressable variables
		MenuItem menusender = (MenuItem)sender;
		MenuItem parent = (MenuItem)menusender.Parent;

		//checks the condition
		if (check == condition)
		{
			//creates a list of all items under the menu
			var allitems = parent.Items.OfType<MenuItem>().ToArray();

			//unchecks all items in the list
			foreach (var item in allitems)
			{
				item.IsChecked = false;
			}

			//re-checks only the item that has been clicked
			menusender.IsChecked = true;
		}
	}

	/// <summary>
	/// Populates a menuItem Sub heading with a list from an array of strings
	/// </summary>
	/// <param name="list">The parent menuItem to form a list under</param>
	/// <param name="items">The items to populate the list from</param>
	/// <param name="defaultCheckedItem">The default Item to be checked</param>
	/// <param name="handler">The handler for all items on the list click event</param>
	/// <returns>nothing</returns>
	public static void PopulateMenuItemList(MenuItem list, String[] items, String defaultCheckedItem, RoutedEventHandler handler)
	{
		//clears the list to begin with
		list.Items.Clear();

		//loops through all items
		for (int i = 0; i < items.Length; i++)
		{
			//creates a new menuItem for each item and binds it to the event handler
			MenuItem item = new MenuItem { Header = items[i] };
			item.Click += new RoutedEventHandler(handler);
			item.IsCheckable = true;

			//checks the default checked item
			if (items[i] == defaultCheckedItem)
			{
				item.IsChecked = true;
			}

			//adds the item to the list
			list.Items.Add(item);
		}
	}

	/// <summary>
	/// Finds the first checked item in a MenuItem list
	/// </summary>
	/// <param name="list">The MenuItem list</param>
	/// <returns>The first checked MenuItem, null if none found</returns>
	public static MenuItem GetCheckedItemInList(MenuItem list)
	{
		//gets the list as an array
		var Items = list.Items.OfType<MenuItem>().ToArray();
		
		//loops through list and 
		foreach (var item in Items)
		{
			if (item.IsChecked)
			{
				return item;
			}
		}

		return null;
	}
	/// <summary>
	/// Finds the first checked item in a MenuItem list
	/// </summary>
	/// <param name="list">The MenuItem list</param>
	/// <param name="disable">If true disables each item in the list</param>
	/// <returns>The first checked MenuItem, null if none found</returns>
	public static MenuItem GetCheckedItemInList(MenuItem list, bool disable)
	{
		MenuItem checkedItem = null;

		//gets the list as an array
		var Items = list.Items.OfType<MenuItem>().ToArray();

		//loops through list and if a checked item is found remembers it
		foreach (var item in Items)
		{
			//if we want to disable the items as we loop through we do so
			if (disable)
			{
				item.IsEnabled = false;
			}

			//we only look for the first checked item
			if (item.IsChecked == true && checkedItem == null)
			{
				checkedItem = item;
			}
		}
		return checkedItem;
	}
}
#endregion
