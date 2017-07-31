using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SwarmRoboticsGUI
{
    /// <summary>
    /// Interaction logic for Display.xaml
    /// </summary>
    public partial class Display : UserControl
    {
        public static readonly DependencyProperty ItemsProperty =
                 DependencyProperty.Register("Items",
                     typeof(ObservableCollection<RobotItem>),
                     typeof(Display),
                     new PropertyMetadata(new ObservableCollection<RobotItem>(), OnChanged));
        public ObservableCollection<RobotItem> Items
        {
            get { return (ObservableCollection<RobotItem>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        public Display()
        {
            InitializeComponent();
            Items = new ObservableCollection<RobotItem>();
            Arena.ItemsSource = Items;
        }

        static void OnChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as Display).OnChanged();
        }
        void OnChanged()
        {
            Arena.ItemsSource = Items;
        }
    }
}
