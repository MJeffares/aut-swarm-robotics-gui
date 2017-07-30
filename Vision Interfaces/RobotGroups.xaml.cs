using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SwarmRoboticsGUI
{
    public partial class RobotGroups : UserControl
    {
        private IList<Item> _List = new ObservableCollection<Item>();
        public RobotGroups()
        {
            InitializeComponent();

            // TEMP: Create groups and robots manually
            var Tower = new RobotGroup("Tower");
            Tower.AddRobot(new RobotItem("Robot 1", 1));
            Tower.AddRobot(new RobotItem("Robot 2", 2));
            var Formation = new RobotGroup("Formation");
            Formation.AddRobot(new RobotItem("Robot 3", 3));
            Formation.AddRobot(new RobotItem("Robot 4", 4));
            Formation.AddRobot(new RobotItem("Robot 5", 5));
            var Unassigned = new RobotGroup("Unassigned");
            Unassigned.AddRobot(new RobotItem("Robot 6", 6));

            AddRobotGroup(Tower);
            AddRobotGroup(Formation);
            AddRobotGroup(Unassigned);

            RobotList.DisplayMemberPath = "Name";
            RobotList.ItemsSource = _List;
        }
        public void AddRobotGroup(RobotGroup Group)
        {
            _List.Add(Group);
            foreach (RobotItem I in Group.Children)
            {
                _List.Add(I);
                foreach (Item S in I.Children)
                {
                    _List.Add(S);
                }
            }
        }

        private void ToggleListBoxItem(object sender, EventArgs e)
        {
            var ListItem = sender as ListBoxItem;
            var Item = ListItem.DataContext;
            var type = Item.GetType();
            if (type == typeof(RobotGroup))
            {
                ToggleGroupItem((RobotGroup)Item);
            }
            else if (type == typeof(RobotItem))
            {
                ToggleRobotItem((RobotItem)Item);
            }
        }
        private void ToggleRobotItem(RobotItem RobotItem)
        {
            var RobotIndex = _List.IndexOf(RobotItem);
            RobotItem.IsCollapsed = RobotItem.IsCollapsed ? false : true;
            _List.RemoveAt(RobotIndex);
            _List.Insert(RobotIndex, RobotItem);

            foreach (Item I in RobotItem.Children)
            {
                var ItemIndex = _List.IndexOf(I);
                I.IsVisible = RobotItem.IsCollapsed ? false : true;
                _List.RemoveAt(ItemIndex);
                _List.Insert(ItemIndex, I);
            }
            
        }
        private void ToggleGroupItem(RobotGroup RobotGroup)
        {
            var GroupIndex = _List.IndexOf(RobotGroup);
            RobotGroup.IsCollapsed = RobotGroup.IsCollapsed ? false : true;
            _List.RemoveAt(GroupIndex);
            _List.Insert(GroupIndex, RobotGroup);
            foreach (RobotItem I in RobotGroup.Children)
            {
                if (_List.Contains(I))
                {
                    var RobotIndex = _List.IndexOf(I);
                    I.IsVisible = I.IsVisible ? false : true;
                    _List.RemoveAt(RobotIndex);
                    _List.Insert(RobotIndex, I);
                    foreach(Item S in I.Children)
                    {
                        var ItemIndex = _List.IndexOf(S);
                        S.IsVisible = (I.IsVisible && !I.IsCollapsed && !RobotGroup.IsCollapsed) ? true : false;
                        _List.RemoveAt(ItemIndex);
                        _List.Insert(ItemIndex, S);
                    }
                }
            }  
        }
    }
}
