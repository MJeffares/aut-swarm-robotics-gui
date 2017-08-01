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
    public class RobotItem : Item, INotifyPropertyChanged
    {
        public int ID { get; private set; }
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
        public Point Direction { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public double Heading { get; set; }
        public double HeadingDeg { get; set; }
        public bool IsCollapsed { get; set; }
        public ObservableCollection<Item> Children { get; set; }
        public RobotItem(string Name, int ID) : base(Name)
        {
            InitializeRobotItem();
            this.ID = ID;

            // TEMP: Testing layout
            Children.Add(new Item("ID", "1"));
            Children.Add(new Item("Battery", "100%"));
            Children.Add(new Item("Task", "None"));
            Children.Add(new Item("Location", "X Y"));
            Children.Add(new Item("Heading", " 90 Deg"));
        }
        public RobotItem(Robot Robot):base("Robot " + Robot.ID.ToString())
        {
            InitializeRobotItem();

            ID = Robot.ID;
            Location = Robot.Location;
            Heading = Robot.Heading;
            HeadingDeg = Robot.Heading * 180/Math.PI;
            // TEMP: Size of the robots is fixed
            Direction = new Point((int)(80 * Math.Cos(Robot.Heading)),
                                  (int)(80 * Math.Sin(Robot.Heading)));
            // Property list
            Children.Add(new Item("ID", Robot.ID.ToString()));
            Children.Add(new Item("Battery", Robot.Battery.ToString()));
            Children.Add(new Item("Task", Robot.Task));
            Children.Add(new Item("Location", Robot.Location.ToString()));
            Children.Add(new Item("Heading", Robot.Heading.ToString()));
        }
        private void InitializeRobotItem()
        {
            Type = "RobotItem";
            IsCollapsed = true;
            IsVisible = true;
            // TEMP: Size of the robots is fixed
            Height = 200;
            Width = 200;
            Children = new ObservableCollection<Item>();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propName)
        {
            //MANSEL: Uncomment this
            //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
            //PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
    public class RobotGroup : Item
    {
        public ObservableCollection<Item> Children { get; set; }
        public bool IsCollapsed { get; set; }

        public RobotGroup(string Name) : base(Name)
        {
            Type = "GroupItem";
            IsCollapsed = false;
            IsVisible = true;
            Children = new ObservableCollection<Item>();
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
}
