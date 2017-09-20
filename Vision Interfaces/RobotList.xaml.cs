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

        public static readonly DependencyProperty GroupsProperty =
            DependencyProperty.Register("Groups",
            typeof(List<RobotGroup>),
            typeof(RobotList),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public List<RobotItem> Items
        {
            get { return (List<RobotItem>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        public List<RobotGroup> Groups
        {
            get { return (List<RobotGroup>)GetValue(GroupsProperty); }
            set { SetValue(GroupsProperty, value); }
        }
        private SynchronizationContext uiContext { get; set; }
        private System.Timers.Timer InterfaceTimer { get; set; }

        public RobotList()
        {
            InitializeComponent();
            //RobotTree.ItemsSource = Groups;
            Groups = new List<RobotGroup>();

            Groups.Add(new RobotGroup("Roaming"));
            Groups.Add(new RobotGroup("Formation"));
            Groups.Add(new RobotGroup("Docked"));
            Groups.Add(new RobotGroup("Graveyard"));

            // Stores the UI context to be used to marshal 
            // code from other threads to the UI thread.
            uiContext = SynchronizationContext.Current;
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            // Create 100ms timer to drive interface changes
            InterfaceTimer = new System.Timers.Timer(300);
            InterfaceTimer.Elapsed += Interface_Tick;
            InterfaceTimer.Start();
        }

        private void Interface_Tick(object sender, ElapsedEventArgs e)
        {
            Update(uiContext);
        }

        private void Update(object state)
        {
            // Get the UI context from state
            SynchronizationContext uiContext = state as SynchronizationContext;
            uiContext.Post(UpdateList, null);
        }
        private void UpdateList(object data)
        {
            if (Items != null)
            {
                foreach (RobotItem Robot in Items)
                {
                    // Find the group the robot is currently in and remove the robot from this list
                    var Gprev = Groups.Where(g => g.Children.Where(r => r.Name == Robot.Name).Any()).FirstOrDefault();
                    if (Gprev != null)
                    {
                        var Rprev = Gprev.Children.Where(r => r.Name == Robot.Name).FirstOrDefault();
                        if (Rprev != null)
                            Groups.ElementAt(Groups.IndexOf(Gprev)).Children.Remove(Rprev);
                    }
                    // Find the group the robot is assigned to.
                    var G = Groups.Where(g => g.Name == Robot.Group).SingleOrDefault();
                    if (G != null)
                        Groups.ElementAt(Groups.IndexOf(G)).Children.Add(Robot);
                }
                RobotTree.ItemsSource = Groups;
            }          
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
                    for (int i = 0; i < Items.Count; i++)
                    {
                        if (i != index)
                            Items[i].IsSelected = false;
                    }

                    Items[index].IsSelected = true;
                }
            }
        }
    }
}
