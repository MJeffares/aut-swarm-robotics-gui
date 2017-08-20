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
            typeof(ObservableCollection<RobotItem>),
            typeof(RobotList),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty GroupsProperty =
            DependencyProperty.Register("Groups",
            typeof(ObservableCollection<RobotGroup>),
            typeof(RobotList),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public ObservableCollection<RobotItem> Items
        {
            get { return (ObservableCollection<RobotItem>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        public ObservableCollection<RobotGroup> Groups
        {
            get { return (ObservableCollection<RobotGroup>)GetValue(GroupsProperty); }
            set { SetValue(GroupsProperty, value); }
        }
        private SynchronizationContext uiContext { get; set; }
        private System.Timers.Timer InterfaceTimer { get; set; }

        public RobotList()
        {
            InitializeComponent();
            //RobotTree.ItemsSource = Groups;
            Groups = new ObservableCollection<RobotGroup>();
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
                foreach (RobotItem R in Items)
                {
                    // Find the group the robot is assigned to.
                    var G = Groups.Where(f => f.Name == R.Group).FirstOrDefault();
                    // Find the group the robot is currently in
                    var Gprev = Groups.Where(f => f.Children.Where(g => g.Name == R.Name).Any()).FirstOrDefault();
                    // If the group exists
                    if (G != null)
                    {
                        // Get the group index inside the group collection
                        int GroupIndex = Groups.IndexOf(G);

                        // Find the robot inside the group
                        var Robot = G.Children.Where(f => f.ID == R.ID).FirstOrDefault();
                        // Get the robot index inside the group
                        int RobotIndex = G.Children.IndexOf(Robot);
                        // Robot was found in the group
                        if (RobotIndex != -1)
                        {
                            // Replace the robot
                            Groups[GroupIndex].Children.RemoveAt(RobotIndex);
                            Groups[GroupIndex].Children.Insert(RobotIndex, R);
                        }
                        else
                        {
                            // Add the robot
                            Groups[GroupIndex].Children.Add(R);
                        }

                        // The assigned group is different to the group the robot was in
                        if (G != Gprev)
                        {
                            // Remove the robot from the previous group
                            if (Gprev != null)
                            {
                                // Find previous group index
                                GroupIndex = Groups.IndexOf(Gprev);
                                // Find the robot inside the group
                                Robot = Gprev.Children.Where(f => f.ID == R.ID).FirstOrDefault();
                                // Get the robot index inside the group
                                RobotIndex = Gprev.Children.IndexOf(Robot);
                                // Replace the robot
                                Groups[GroupIndex].Children.RemoveAt(RobotIndex);
                                // Group is empty
                                if (Groups[GroupIndex].Children.Count == 0)
                                {
                                    // Remove the group
                                    Groups.RemoveAt(GroupIndex);
                                }
                            }
                        }
                    }
                    // The group doesn't exist
                    else
                    {
                        // Create a new group
                        var NewGroup = new RobotGroup(R.Group.ToString());
                        NewGroup.Children.Add(R);
                        Groups.Add(NewGroup);
                    }
                }
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
