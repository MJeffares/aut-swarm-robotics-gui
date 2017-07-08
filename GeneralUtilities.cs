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
