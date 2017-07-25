using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SwarmRoboticsGUI
{
    public class ListBoxItemStyleSelector : StyleSelector
    {
        public Style RobotStyle { get; set; }
        public Style GroupStyle { get; set; }

        public override Style SelectStyle(object Item, DependencyObject Container)
        {
            // Get the datatype
            var Type = Item.GetType();
            // Is it a robot
            bool IsRobotItem = (Type == typeof(RobotItem));
            // Return the style
            return IsRobotItem ? RobotStyle : GroupStyle;
        }
    }

    public class Item
    {
        public string Name { get; private set; }

        public Item(string Name)
        {
            this.Name = Name;
        }
    }
    public class RobotItem : Item
    {
        public int ID { get; private set; }

        public RobotItem(string Name, int ID) : base(Name)
        {
            this.ID = ID;
        }
    }
    public class RobotGroup : Item
    {
        public List<RobotItem> Children { get; private set; }
        public RobotGroup(string Name) : base(Name)
        {
            Children = new List<RobotItem>();
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
            int GroupIndex = _List.IndexOf(Group);
            _List.Insert(GroupIndex + 1, Item);
        }
        public void GroupRobot(RobotItem Item, string GroupName)
        {
            var Group = AddRobotGroup(GroupName);
            int GroupIndex = _List.IndexOf(Group);
            RemoveRobot(Item);
            _List.Insert(GroupIndex + 1, Item);
        }

        public void AddRobot(RobotItem Item)
        {
            foreach (Item I in _List)
            {
                if (I.Name == Item.Name)
                {
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
    }
}
