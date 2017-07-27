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
        public string Value { get; private set; }
        public string Type { get; set; }
        public bool IsVisible { get; set; }

        public Item(string Name)
        {
            this.Name = Name;
            Type = "Item";
            IsVisible = false;
        }
        public Item(string Name, string Value)
        {
            this.Name = Name;
            this.Value = Value;
            Type = "Item";
            IsVisible = false;
        }
    }
    public class RobotItem : Item
    {
        public int ID { get; private set; }
        public List<Item> Children { get; private set; }
        public bool IsCollapsed { get; set; }

        public RobotItem(string Name, int ID) : base(Name)
        {
            this.ID = ID;
            Type = "RobotItem";
            IsCollapsed = true;
            IsVisible = true;

            // TEMP: Testing layout
            Children = new List<Item>();
            Children.Add(new Item("ID", "1"));
            Children.Add(new Item("Battery", "100%"));
            Children.Add(new Item("Task", "None"));
            Children.Add(new Item("Location", "X Y"));
            Children.Add(new Item("Heading", " 0 Deg"));
        }
    }
    public class RobotGroup : Item
    {
        public List<RobotItem> Children { get; private set; }
        public bool IsCollapsed { get; set; }

        public RobotGroup(string Name) : base(Name)
        {
            Type = "GroupItem";
            IsCollapsed = false;
            IsVisible = true;
            Children = new List<RobotItem>();
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
            return;
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
