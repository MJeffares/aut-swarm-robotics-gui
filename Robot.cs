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
    public class Item
    {
        private string _Name { get; set; }
        public string Name
        {
            get { return _Name; }
            set
            {
                if (_Name != value)
                {
                    _Name = value;
                    NotifyPropertyChanged("Name");
                }
            }
        }
        private string _Text { get; set; }
        public string Text
        {
            get { return _Text; }
            set
            {
                if (_Text != value)
                {
                    _Text = value;
                    NotifyPropertyChanged("Value");
                }
            }
        }

        public Item(string Name)
        {
            this.Name = Name;
        }
        public Item(string Name, string Text)
        {
            this.Name = Name;
            this.Text = Text;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string PropertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(PropertyName));
            }
        }
    }
    public class RobotItem : Item, INotifyPropertyChanged
    {
        private int _Battery { get; set; }
        public int Battery
        {
            get { return _Battery; }
            set
            {
                if (_Battery != value)
                {
                    _Battery = value;
                    NotifyPropertyChanged("Battery");
                }
            }
        }
        private string _Group { get; set; }
        public string Group
        {
            get { return _Group; }
            set
            {
                if (_Group != value)
                {
                    _Group = value;
                    NotifyPropertyChanged("Group");
                }
            }
        }
        private int _ID { get; set; }
        public int ID
        {
            get { return _ID; }
            set
            {
                if (_ID != value)
                {
                    _ID = value;
                    NotifyPropertyChanged("ID");
                }
            }
        }
        private string _Task { get; set; }
        public string Task
        {
            get { return _Task; }
            set
            {
                if (_Task != value)
                {
                    _Task = value;
                    NotifyPropertyChanged("Task");
                }
            }
        }
        private int _Radius { get; set; }
        public int Radius
        {
            get { return _Radius; }
            set
            {
                if (_Radius != value)
                {
                    _Radius = value;
                    NotifyPropertyChanged("Radius");
                }
            }
        }
        private int _Height { get; set; }
        public int Height
        {
            get { return _Height; }
            set
            {
                if (_Height != value)
                {
                    _Height = value;
                    NotifyPropertyChanged("Height");
                }
            }
        }
        private int _Width { get; set; }
        public int Width
        {
            get { return _Width; }
            set
            {
                if (_Width != value)
                {
                    _Width = value;
                    NotifyPropertyChanged("Width");
                }
            }
        }
        private double _Heading { get; set; }
        public double Heading
        {
            get { return _Heading; }
            set
            {
                if (_Heading != value)
                {
                    _Heading = value;
                    NotifyPropertyChanged("Heading");
                }
            }
        }
        private double _HeadingDeg { get; set; }
        public double HeadingDeg
        {
            get { return _HeadingDeg; }
            set
            {
                if (_HeadingDeg != value)
                {
                    _HeadingDeg = value;
                    NotifyPropertyChanged("HeadingDeg");
                }
            }
        }

        private Point _Pixel { get; set; }
        public Point Pixel
        {
            get { return _Pixel; }
            set
            {
                if (_Pixel != value)
                {
                    _Pixel = value;
                    NotifyPropertyChanged("Pixel");
                }
            }
        }

        private System.Windows.Point _Location { get; set; }
        public System.Windows.Point Location
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

        private System.Windows.Point _DisplayLocation { get; set; }
        public System.Windows.Point DisplayLocation
        {
            get { return _DisplayLocation; }
            set
            {
                if (_DisplayLocation != value)
                {
                    _DisplayLocation = value;
                    NotifyPropertyChanged("DisplayLocation");
                }
            }
        }

        public Point PreviousLocation { get; set; }
        public Point Direction { get; set; }
        public Point[] Contour { get; set; }
        
        private bool _IsTracked { get; set; }
        public bool IsTracked
        {
            get { return _IsTracked; }
            set
            {
                if (_IsTracked != value)
                {
                    _IsTracked = value;
                    NotifyPropertyChanged("IsTracked");
                }
            }
        }
        private bool _IsExpanded { get; set; }
        public bool IsExpanded
        {
            get { return _IsExpanded; }
            set
            {
                if (_IsExpanded != value)
                {
                    _IsExpanded = value;
                    NotifyPropertyChanged("IsExpanded");
                }
            }
        }
        private bool _IsSelected { get; set; }
        public bool IsSelected
        {
            get { return _IsSelected; }
            set
            {
                if (_IsSelected != value)
                {
                    _IsSelected = value;
                    NotifyPropertyChanged("IsSelected");
                }
            }
        }
		private bool _IsCommunicating { get; set; }
		public bool IsCommunicating
		{
			get { return _IsCommunicating; }
			set
			{
				if (_IsCommunicating != value)
				{
					_IsCommunicating = value;
					NotifyPropertyChanged("IsCommunicating");
				}
			}
		}
		
		private ObservableCollection<Item> _Children { get; set; }
        public ObservableCollection<Item> Children
        {
            get { return _Children; }
            set
            {
                if (_Children != value)
                {
                    _Children = value;
                    NotifyPropertyChanged("Children");
                }
            }
        }

		private Brush _Colour { get; set; }
		public Brush Colour
		{
			get { return _Colour; }
			set
			{
				if (_Colour != value)
				{
					_Colour = value;
					NotifyPropertyChanged("Colour");
				}
			}
		}



		public RobotItem(string Name, int ID) : base(Name)
        {
            this.ID = ID;
            Group = "Unassigned";
            // TEMP: Size of the robots is fixed       
            Radius = 100;
            Width = 2 * Radius;
            Height = (int)(Math.Sqrt(3) * Radius);
            Children = new ObservableCollection<Item>();

            // TEMP: Testing layout
            Children.Add(new Item("ID", ""));
            Children.Add(new Item("Battery", ""));
            Children.Add(new Item("Task", ""));
            Children.Add(new Item("Location", ""));
            Children.Add(new Item("Heading", ""));
        }
    }
    public class RobotGroup : Item, INotifyPropertyChanged
    {
        private ObservableCollection<RobotItem> _Children { get; set; }
        public ObservableCollection<RobotItem> Children
        {
            get { return _Children; }
            set
            {
                if (_Children != value)
                {
                    _Children = value;
                    NotifyPropertyChanged("Children");
                }
            }
        }

        public RobotGroup(string Name) : base(Name)
        {
            Children = new ObservableCollection<RobotItem>();
        }
    }
}
