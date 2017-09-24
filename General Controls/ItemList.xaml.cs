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
    public partial class ItemList : UserControl
    {
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem",
            typeof(Item),
            typeof(ItemList),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public Item SelectedItem
        {
            get { return (Item)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register("Items",
            typeof(List<Item>),
            typeof(ItemList),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public List<Item> Items
        {
            get { return (List<Item>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }


        public event EventHandler SelectedItemChanged;
        protected void SelectedItem_Changed(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //bubble the event up to the parent
            if (this.SelectedItemChanged != null)
                this.SelectedItemChanged(this, e);
        }




        public ItemList()
        {
            InitializeComponent();
        }

        private void TV_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var Target = sender as TreeViewItem;

            if (Target != null)
            {
                var Item = Target.DataContext as Item;
                if (Item != null)
                {
                    int index = Items.IndexOf(Item);
                    foreach (Item I in Items)
                    {
                        I.IsSelected = false;
                    }
                    Items[index].IsSelected = true;
                    SelectedItem = Items[index];
                    SelectedItem_Changed(this, e);
                }
            }
        }
    }
}
