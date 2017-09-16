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
    public enum OverlayType
    {
        [Description("No Overlay")]
        NONE,
        [Description("Debugging")]
        DEBUG,
        [Description("Pretty")]
        PRETTY,
        [Description("Information")]
        INFO,
        [Description("Grid")]
        GRID,
        [Description("Test Image")]
        TEST,
    };
    public enum SourceType
    {
        [Description("No Source")]
        NONE,
        [Description("Camera")]
        CAMERA,
        [Description("Cutouts")]
        CUTOUTS,
    };


    /// <summary>
    /// Interaction logic for Display.xaml
    /// </summary>
    public partial class Display : UserControl
    {
        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register("Items",
            typeof(List<RobotItem>),
            typeof(Display),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public List<RobotItem> Items
        {
            get { return (List<RobotItem>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        public OverlayType Overlay { get; set; }
        public SourceType Source { get; set; }

        public Display()
        {
            InitializeComponent();
            //Items = new ObservableCollection<RobotItem>();
            //Arena.ItemsSource = Items;
        }


        private void Robot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var Target = sender as Polygon;
            if (Target != null)
            {
                var Robot = Target.DataContext as RobotItem;
                if (Robot != null)
                {
                    int index = Items.IndexOf(Robot);
                    for (int i = 0; i < Items.Count; i++)
                    {
                        Items[i].IsSelected = false;
                    }
                    Items[index].IsSelected = true;
                }
            }
        }
    }
}
