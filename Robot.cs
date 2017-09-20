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
using System.Reflection;

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
            Children = new List<Item>();
        }
        public Item(string Name, string Text)
        {
            this.Name = Name;
            this.Text = Text;
            Children = new List<Item>();
        }

        private List<Item> _Children { get; set; }
        public List<Item> Children
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

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string PropertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(PropertyName));
                var test = this.GetType().GetProperty(PropertyName).GetValue(this, null);
           
                var child = Children.Where(f => f.Name == PropertyName).SingleOrDefault();
                if (child != null)
                    Children.ElementAt(Children.IndexOf(child)).Text = test.ToString();
            }
        }
    }
    public class RobotItem : CommunicationItem, INotifyPropertyChanged
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
        private double _Facing { get; set; }
        public double Facing
        {
            get { return _Facing; }
            set
            {
                if (_Facing != value)
                {
                    _Facing = value;
                    NotifyPropertyChanged("Facing");
                }
            }
        }
        private double _FacingDeg { get; set; }
        public double FacingDeg
        {
            get { return _FacingDeg; }
            set
            {
                if (_FacingDeg != value)
                {
                    _FacingDeg = value;
                    NotifyPropertyChanged("FacingDeg");
                }
            }
        }

        private Point _PixelLocation { get; set; }
        public Point PixelLocation
        {
            get { return _PixelLocation; }
            set
            {
                if (_PixelLocation != value)
                {
                    _PixelLocation = value;
                    NotifyPropertyChanged("PixelLocation");
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

        public Point PreviousLocation { get; set; }

        public double _FacingMarker { get; set; }
        public double FacingMarker
        {
            get { return _FacingMarker; }
            set
            {
                if (_FacingMarker != value)
                {
                    _FacingMarker = value;
                    NotifyPropertyChanged("FacingMarker");
                }
            }
        }

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
		
		public RobotItem(string Name, UInt64 MAC_Address, string Colour, int ID) : base(Name, MAC_Address, Colour)
        {
			this.Name = Name;
            this.ID = ID;
			this.Colour = Colour;
			this.Address64 = MAC_Address;
            Group = "Roaming";
            // TEMP: Size of the robots is fixed       
            Radius = 30;
            Width = 2 * Radius;
            Height = (int)(Math.Sqrt(3) * Radius);

            // TEMP: Set position off the arena intially
            Location = new System.Windows.Point(-100, -100);
            
            // TEMP: Testing layout
            Children.Add(new Item("ID", ID.ToString()));
            Children.Add(new Item("Battery", Battery.ToString()));
            Children.Add(new Item("Task", "Task"));
            Children.Add(new Item("Location", Location.X.ToString() + ", " + Location.Y.ToString()));
            Children.Add(new Item("Facing", FacingDeg.ToString()));
        }
        
    }

    public class RobotGroup : Item, INotifyPropertyChanged
    {
        public RobotGroup(string Name) : base(Name)
        {

        }
    }

    public class Arena : Item, INotifyPropertyChanged
    {
        private Point _Origin { get; set; }
        public Point Origin
        {
            get { return _Origin; }
            set
            {
                if (_Origin != value)
                {
                    _Origin = value;
                    NotifyPropertyChanged("Origin");
                }
            }
        }

        private Point _Opposite { get; set; }
        public Point Opposite
        {
            get { return _Opposite; }
            set
            {
                if (_Opposite != value)
                {
                    _Opposite = value;
                    NotifyPropertyChanged("Opposite");
                }
            }
        }

        public double ScaleFactor { get; set; }


        public Point[] Contour { get; set; }

        public Arena() : base("Arena")
        {

        }
    }

    public class ChargingDockItem : CommunicationItem, INotifyPropertyChanged
    {
        public ChargingDockItem(String Name, UInt64 MAC_Address, string Colour) : base(Name, MAC_Address, Colour)
        {
            this.Name = Name;
            this.Address64 = MAC_Address;
            this.Colour = Colour;
        }
    }

    public class CommunicationItem : Item, INotifyPropertyChanged
    {
        private String _Colour { get; set; }
        public String Colour
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

        private UInt64 _Address64 { get; set; }
        public UInt64 Address64
        {
            get { return _Address64; }
            set
            {
                if (_Address64 != value)
                {
                    _Address64 = value;
                    NotifyPropertyChanged("Address64");
                }
            }
        }

        private UInt16 _Address16 { get; set; }
        public UInt16 Address16
        {
            get { return _Address16; }
            set
            {
                if (_Address16 != value)
                {
                    _Address16 = value;
                    NotifyPropertyChanged("Address16");
                }
            }
        }

        public CommunicationItem(String Name, UInt64 MAC_Address, string Colour) : base(Name)
        {
            this.Name = Name;
            this.Address16 = 0xFFFE;
            this.Address64 = MAC_Address;
            this.Colour = Colour;
        }
    }
}
