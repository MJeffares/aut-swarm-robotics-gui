using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;

namespace SwarmRoboticsGUI
{
    public partial class RobotList : UserControl
    {
        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register("Items",
            typeof(List<RobotItem>),
            typeof(RobotList),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public List<RobotItem> Items
        {
            get { return (List<RobotItem>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        public RobotList()
        {
            InitializeComponent();
        }

        private void TV_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var Target = sender as TreeViewItem;

            if (Target != null)
            {
                var Robot = Target.DataContext as RobotItem;
                if (Robot != null)
                {
                    int index = Items.IndexOf(Robot);
                    foreach (RobotItem R in Items)
                    {
                        R.IsSelected = false;
                    }
                    Items[index].IsSelected = true;
                }
            }
        }
    }
}
