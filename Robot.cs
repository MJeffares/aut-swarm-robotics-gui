using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Emgu.CV;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SwarmRoboticsGUI
{
    public class Robot
    {
        #region Public Properties
        public int ID { get; set; }
        public int Battery { get; set; }
        public string Task { get; set; }
        public Point Location { get; set; }
        public Point PreviousLocation { get; set; }
        public double Heading { get; set; }
        public Point[] Contour { get; set; }
        public bool IsTracked { get; set; }
        public bool IsSelected { get; set; }
        #endregion

        public Robot()
        {
            Battery = 0;
            Heading = 0;
            Location = new Point(0, 0);
            IsTracked = false;
            IsSelected = false;
        }
    }
    public class Item
    {
        public string Name { get; private set; }
        public int ID { get; private set; }
        public string Value { get; private set; }
        public string Type { get; set; }

        public Item(string Name)
        {
            this.Name = Name;
            Type = "Item";
        }
        public Item(string Name, string Value)
        {
            this.Name = Name;
            this.Value = Value;
            Type = "Item";
        }
        public Item(string Name, int ID)
        {
            this.Name = Name;
            this.ID = ID;
            Type = "Item";
        }
    }
    public class RobotItem : Item, INotifyPropertyChanged
    {
        public int Battery { get; set; }
        public string Group { get; set; }
        public string Task { get; set; }
        private Point _Location { get; set; }
        public Point Location
        {
            get { return _Location; }
            set
            {
                if (_Location != value)
                {
                    _Location = value;
                    NotifyPropertyChanged("Location");
                }
            }
        }
        public Point PreviousLocation { get; set; }
        public Point Direction { get; set; }
        public Point[] Contour { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public double Heading { get; set; }
        public double HeadingDeg { get; set; }
        public bool IsTracked { get; set; }

        public bool IsSelected { get; set; }
        public ObservableCollection<Item> Children { get; set; }
        public RobotItem(string Name, int ID) : base(Name, ID)
        {
            InitializeRobotItem();

            // TEMP: Testing layout
            Children.Add(new Item("ID", "1"));
            Children.Add(new Item("Battery", "100%"));
            Children.Add(new Item("Task", "None"));
            Children.Add(new Item("Location", "X Y"));
            Children.Add(new Item("Heading", "90 Deg"));
        }

        private void InitializeRobotItem()
        {
            Type = "RobotItem";
            Group = "Unassigned";
            IsSelected = false;
            IsTracked = false;
            // TEMP: Size of the robots is fixed
            Height = 200;
            Width = 200;
            Children = new ObservableCollection<Item>();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
    }
    public class RobotGroup : Item
    {
        public ObservableCollection<RobotItem> Children { get; set; }
        public bool IsCollapsed { get; set; }

        public RobotGroup(string Name) : base(Name)
        {
            Type = "GroupItem";
            IsCollapsed = false;
            Children = new ObservableCollection<RobotItem>();
        }

        public void AddRobot(RobotItem Item)
        {
            foreach (RobotItem I in Children)
            {
                if (I.Name == Item.Name)
                {
                    return;
                }
            }
            Children.Add(Item);
        }
    }
}
