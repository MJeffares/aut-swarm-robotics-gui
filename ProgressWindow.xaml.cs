using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SwarmRoboticsGUI
{
	/// <summary>
	/// Interaction logic for ProgressWindow.xaml
	/// </summary>
	public partial class ProgressWindow : Window
	{
		private bool canClose;

		private const int GWL_STYLE = -16;
		private const int WS_SYSMENU = 0x80000;
		[DllImport("user32.dll", SetLastError = true)]
		private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
		[DllImport("user32.dll")]
		private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		public ProgressWindow(string title, string text)
		{			
			InitializeComponent();			

			this.Title = title;
			DisplayText.Text = text;
			canClose = false;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			var hwnd = new WindowInteropHelper(this).Handle;
			SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (!canClose)
			{
				base.OnClosing(e);
				e.Cancel = true;
			}

		}

		public delegate void CloseWindowCallback();

		public void CloseWindow()
		{
			Dispatcher.Invoke(new CloseWindowCallback(ForceCloseWindow));
		}

		private void ForceCloseWindow()
		{
			canClose = true;
			Close();
		}
	}
}
