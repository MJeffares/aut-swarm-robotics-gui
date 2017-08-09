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
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public ObservableCollection<RobotItem> Items
        {
            get { return (ObservableCollection<RobotItem>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        public Display()
        {
            InitializeComponent();
            //Items = new ObservableCollection<RobotItem>();
            //Arena.ItemsSource = Items;
        }

        #region Enumerations
        public enum OverlayType { NONE, DEBUG, PRETTY, INFO, GRID, TEST, NUM_OVERLAYS };
        public enum SourceType { NONE, CAMERA, CUTOUTS, NUM_SOURCES };
        #endregion
        public static string ToString(OverlayType Overlay)
        {
            switch (Overlay)
            {
                case OverlayType.NONE:
                    return string.Format("No Overlay");
                case OverlayType.DEBUG:
                    return string.Format("Debugging");
                case OverlayType.PRETTY:
                    return string.Format("Pretty");
                case OverlayType.INFO:
                    return string.Format("Information");
                case OverlayType.GRID:
                    return string.Format("Grid");
                case OverlayType.TEST:
                    return string.Format("Test Image");
                default:
                    return string.Format("Overlay Text Error");
            }
        }
        public static string ToString(SourceType Source)
        {
            switch (Source)
            {
                case SourceType.NONE:
                    return string.Format("No Source");
                case SourceType.CAMERA:
                    return string.Format("Camera");
                case SourceType.CUTOUTS:
                    return string.Format("Cutouts");
                default:
                    return string.Format("Source Text Error");
            }
        }

        private void Robot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var Target = sender as Grid;
            if (Target != null)
            {
                var Robot = Target.DataContext as RobotItem;
                if (Robot != null)
                {
                    for (int i = 0; i < Items.Count; i++)
                    {
                        Items[i].IsSelected = false;
                    }
                    int index = Items.IndexOf(Robot);
                    Items[index].IsSelected = true;
                }
            }
        }
    }
}
