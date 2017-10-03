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
            typeof(List<IObstacle>),
            typeof(Display),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public List<IObstacle> Items
        {
            get { return (List<IObstacle>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        public OverlayType Overlay { get; set; }
        public SourceType Source { get; set; }

        private System.Windows.Threading.DispatcherTimer TimeoutTimer = new System.Windows.Threading.DispatcherTimer();


        public Point Target
        {
            get { return (Point)GetValue(TargetProperty); }
            set { SetCurrentValue(TargetProperty, value); }
        }
        public readonly static DependencyProperty TargetProperty = DependencyProperty.Register(
            "Target", typeof(Point), typeof(Display), new UIPropertyMetadata(new Point(), (o, e) =>
            {
                Display disp = (Display)o;
                disp.RaiseTargetChangedEvent(e);
            }));

        public event EventHandler<DependencyPropertyChangedEventArgs> TargetChanged;
        private void RaiseTargetChangedEvent(DependencyPropertyChangedEventArgs e)
        {
            if (TargetChanged != null)
                TargetChanged(this, e);
        }



        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem",
            typeof(IObstacle),
            typeof(Display),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public IObstacle SelectedItem
        {
            get { return (IObstacle)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public event EventHandler SelectedItemChanged;
        protected void SelectedItem_Changed(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //bubble the event up to the parent
            if (this.SelectedItemChanged != null)
                this.SelectedItemChanged(this, e);
        }

        public Display()
        {
            InitializeComponent();
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            // Create 100ms timer to drive interface changes
            //TimeoutTimer = new System.Windows.Threading.DispatcherTimer();
            TimeoutTimer.Interval = new TimeSpan(0,0,1);
            TimeoutTimer.Tick += Timeout_Tick;
            TimeoutTimer.Start();
        }

        private void Timeout_Tick(object sender, EventArgs e)
        {
            if (Items != null)
            {
                foreach (IObstacle Obstacle in Items.Where(O => O is IObstacle))
                {
                    if (Obstacle.LastVisible != null)
                    {
                        TimeSpan Delta = DateTime.Now - Obstacle.LastVisible;
                        if (Delta.TotalSeconds > 5)
                            Obstacle.IsVisible = false;
                    }
                }
            }
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
                    foreach (RobotItem R in Items.Where(R => R is RobotItem))
                    {
                        R.IsSelected = false;
                        R.IsExpanded = false;
                    }
                    Robot.IsSelected = true;
                    Robot.IsExpanded = true;

                    SelectedItem = Robot;
                }
            }
            
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Target = e.GetPosition(sender as Canvas);
            
        }
    }
}
