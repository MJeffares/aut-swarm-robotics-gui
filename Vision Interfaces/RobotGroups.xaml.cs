using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SwarmRoboticsGUI
{
    public class Item
    {
        public string Name { get; private set; }
        public string Type { get; set; }
        public bool IsVisible { get; set; }

        public Item(string Name)
        {
            this.Name = Name;
            Type = "Item";
            IsVisible = true;
        }
    }
    public class RobotItem : Item
    {
        public int ID { get; private set; }
        public List<Item> Children { get; private set; }

        public RobotItem(string Name, int ID) : base(Name)
        {
            this.ID = ID;
            Type = "RobotItem";

        }
    }
    public class RobotGroup : Item
    {
        public List<RobotItem> Children { get; private set; }
        public RobotGroup(string Name) : base(Name)
        {
            Children = new List<RobotItem>();
            Type = "GroupItem";

        }
        public void AddRobot(RobotItem Item)
        {
            foreach (Item I in Children)
            {
                if (I.Name == Item.Name)
                {
                    return;
                }
            }
            Children.Add(Item);
        }
    }

    public partial class RobotGroups : UserControl
    {
        private IList<Item> _List = new ObservableCollection<Item>();

        public RobotGroups()
        {
            InitializeComponent();
            GroupRobot(new RobotItem("Robot 1", 1), "Tower");
            GroupRobot(new RobotItem("Robot 2", 2), "Tower");
            GroupRobot(new RobotItem("Robot 3", 3), "Formation");
            GroupRobot(new RobotItem("Robot 4", 4), "Formation");
            GroupRobot(new RobotItem("Robot 5", 5), "Tower");
            GroupRobot(new RobotItem("Robot 6", 6), "Unassigned");

            RobotList.DisplayMemberPath = "Name";
            RobotList.ItemsSource = _List;
        }

        public void GroupRobot(RobotItem Item, RobotGroup Group)
        {
            AddRobot(Item);
            AddRobotGroup(Group);
            Group.AddRobot(Item);
            int GroupIndex = _List.IndexOf(Group);
            RemoveRobot(Item);
            _List.Insert(GroupIndex + 1, Item);
        }
        public void GroupRobot(RobotItem Item, string GroupName)
        {
            var Group = AddRobotGroup(GroupName);
            GroupRobot(Item, Group);
        }

        public void AddRobot(RobotItem Item)
        {
            foreach (Item I in _List)
            {
                if (I.Name == Item.Name)
                {
                    var index = _List.IndexOf(I);
                    _List.RemoveAt(index);
                    _List.Insert(index, Item);
                    return;
                }
            }
            _List.Add(Item);
        }
        public void AddRobot(string Name, int ID)
        {
            var Item = new RobotItem(Name, ID);
            AddRobot(Item);
        }
        public void RemoveRobot(RobotItem Item)
        {
            foreach (Item I in _List)
            {
                if (I.Name == Item.Name)
                {
                    _List.Remove(Item);
                    return;
                }
            }
        }
        public void RemoveRobot(string Name, int ID)
        {
            var Item = new RobotItem(Name, ID);
            if (_List.Contains(Item))
            {
                _List.Remove(Item);
            }
        }
        public RobotGroup AddRobotGroup(RobotGroup Group)
        {
            foreach (Item I in _List)
            {
                if (I.Name == Group.Name)
                {
                    return (RobotGroup)I;
                }
            }
            _List.Add(Group);
            return Group;
        }
        public RobotGroup AddRobotGroup(string Name)
        {
            var Group = new RobotGroup(Name);
            return AddRobotGroup(Group);
        }
        public void RemoveRobotGroup(RobotGroup Group)
        {
            foreach (Item I in _List)
            {
                if (I.Name == Group.Name)
                {
                    _List.Remove(Group);
                    return;
                }
            }
        }
        public void RemoveRobotGroup(string Name)
        {
            var Group = new RobotGroup(Name);
            if (_List.Contains(Group))
            {
                _List.Remove(Group);
            }
        }

        private void ToggleGroup(object sender, EventArgs e)
        {
            var ListItem = sender as ListBoxItem;
            var Item = ListItem.DataContext;
            var type = Item.GetType();
            if (type == typeof(RobotGroup))
            {
                var GroupItem = (RobotGroup)Item;
                foreach (Item I in GroupItem.Children)
                {
                    if (_List.Contains(I))
                    {
                        I.IsVisible = I.IsVisible ? false : true;
                        AddRobot((RobotItem)I);
                    }
                }
            }
        }
    }
}
