﻿using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Drawing;
using System.Windows;

namespace SwarmRoboticsGUI
{
    public class Item: INotifyPropertyChanged
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
        private string _Text { get; set; }
        public string Text
        {
            get { return _Text; }
            set
            {
                if (_Text != value)
                {
                    _Text = value;
                    NotifyPropertyChanged("Text");
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
                var Value = GetType().GetProperty(PropertyName).GetValue(this, null);
                var Child = Children.Where(f => f.Name == PropertyName).SingleOrDefault();

                if (Child != null)
                    Children.ElementAt(Children.IndexOf(Child)).Text = Value.ToString();
            }
        }
    }


    public interface IObstacle
    {
        System.Windows.Point Location { get; set; }
        System.Drawing.Point PixelLocation { get; set; }
        System.Drawing.Point[] Contour { get; set; }

        bool IsVisible { get; set; }
        bool IsTracked { get; set; }

        int Radius { get; set; }
        int Height { get; set; }
        int Width { get; set; }
    }

    public interface ICommunicates
    {
        UInt64 Address64 { get; set; }
        UInt16 Address16 { get; set; }
        bool IsCommunicating { get; set; }
    }




    public class RobotItem : Item, INotifyPropertyChanged, IObstacle, ICommunicates
    {
        #region Status Properties
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
        

        public System.Drawing.Point PreviousLocation { get; set; }


        #endregion

        #region Display Properties
        private bool _HasFacing { get; set; }
        public bool HasFacing
        {
            get { return _HasFacing; }
            set
            {
                if (_HasFacing != value)
                {
                    _HasFacing = value;
                    NotifyPropertyChanged("HasFacing");
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
        private string _Colour { get; set; }
        public string Colour
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
        #endregion

        #region Communication Properties
        private ulong _Address64 { get; set; }
        ulong ICommunicates.Address64
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
        private ushort _Address16 { get; set; }
        ushort ICommunicates.Address16
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
        private bool _IsCommunicating { get; set; }
        bool ICommunicates.IsCommunicating
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
        #endregion

        #region Obstacle Properties
        private System.Windows.Point _Location { get; set; }
        System.Windows.Point IObstacle.Location
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
        private System.Drawing.Point _PixelLocation { get; set; }
        System.Drawing.Point IObstacle.PixelLocation
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
        private System.Drawing.Point[] _Contour { get; set; }
        System.Drawing.Point[] IObstacle.Contour
        {
            get { return _Contour; }
            set
            {
                if (_Contour != value)
                {
                    _Contour = value;
                    NotifyPropertyChanged("Contour");
                }
            }
        }
        private bool _IsVisible { get; set; }
        bool IObstacle.IsVisible
        {
            get { return _IsVisible; }
            set
            {
                if (_IsVisible != value)
                {
                    _IsVisible = value;
                    NotifyPropertyChanged("IsVisible");
                }
            }
        }
        private bool _IsTracked { get; set; }
        bool IObstacle.IsTracked
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
        private int _Radius { get; set; }
        int IObstacle.Radius
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
        int IObstacle.Height
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
        int IObstacle.Width
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
        #endregion

        public RobotItem(string Name, ulong MAC_Address, string Colour, int ID) : base(Name)
        {
			this.Name = Name;
            this.ID = ID;
            this.Colour = Colour;
            this.Group = "Not Connected";
            this.HasFacing = false;

            ICommunicates comms = this;
            comms.Address16 = 0xFFFE;
            comms.Address64 = MAC_Address;

            IObstacle obstacle = this;
            // TEMP: Size of the displayed robots is fixed
            obstacle.Radius = 40;
            obstacle.Width = 2 * obstacle.Radius;
            obstacle.Height = (int)(Math.Sqrt(3) * obstacle.Radius);
            obstacle.IsVisible = false;
            
            // Create property labels
            Children.Add(new Item("ID"));
            Children.Add(new Item("Battery"));
            Children.Add(new Item("Task"));
            Children.Add(new Item("Location"));
            Children.Add(new Item("FacingDeg"));
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
        private System.Drawing.Point _Origin { get; set; }
        public System.Drawing.Point Origin
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
        public double _ScaleFactor { get; set; }
        public double ScaleFactor
        {
            get { return _ScaleFactor; }
            set
            {
                if (_ScaleFactor != value)
                {
                    _ScaleFactor = value;
                    NotifyPropertyChanged("ScaleFactor");
                }
            }
        }
        public System.Drawing.Point[] _Contour { get; set; }
        public System.Drawing.Point[] Contour
        {
            get { return _Contour; }
            set
            {
                if (_Contour != value)
                {
                    _Contour = value;
                    NotifyPropertyChanged("Contour");
                }
            }
        }

        public Arena() : base("Arena")
        {

        }
    }

    public class ChargingDockItem : Item, INotifyPropertyChanged, IObstacle, ICommunicates
    {
        #region Communication Properties
        private ulong _Address64 { get; set; }
        ulong ICommunicates.Address64
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
        private ushort _Address16 { get; set; }
        ushort ICommunicates.Address16
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
        private bool _IsCommunicating { get; set; }
        bool ICommunicates.IsCommunicating
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
        #endregion

        #region Obstacle Properties
        private System.Windows.Point _Location { get; set; }
        System.Windows.Point IObstacle.Location
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
        private System.Drawing.Point _PixelLocation { get; set; }
        System.Drawing.Point IObstacle.PixelLocation
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
        private System.Drawing.Point[] _Contour { get; set; }
        System.Drawing.Point[] IObstacle.Contour
        {
            get { return _Contour; }
            set
            {
                if (_Contour != value)
                {
                    _Contour = value;
                    NotifyPropertyChanged("Contour");
                }
            }
        }
        private bool _IsVisible { get; set; }
        bool IObstacle.IsVisible
        {
            get { return _IsVisible; }
            set
            {
                if (_IsVisible != value)
                {
                    _IsVisible = value;
                    NotifyPropertyChanged("IsVisible");
                }
            }
        }
        private bool _IsTracked { get; set; }
        bool IObstacle.IsTracked
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
        private int _Radius { get; set; }
        int IObstacle.Radius
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
        int IObstacle.Height
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
        int IObstacle.Width
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
        #endregion

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

        public ChargingDockItem(String Name, UInt64 MAC_Address, string Colour) : base(Name)
        {
            this.Name = Name;
            this.Group = "Charging Stations";
            this.Colour = "Green";
            this.FacingDeg = 30;

            ICommunicates comms = this;
            comms.Address16 = 0xFFFE;
            comms.Address64 = MAC_Address;

            IObstacle obstacle = this;
            // TEMP: Size of the displayed robots is fixed
            obstacle.Radius = 80;
            obstacle.Width = 2 * obstacle.Radius;
            obstacle.Height = (int)(Math.Sqrt(3) * obstacle.Radius);
            obstacle.IsVisible = true;
            double X = (2 * 1177 - obstacle.Width - 120) / 4;
            double Y = (2 * 1177 - obstacle.Height - 120) / 4;
            obstacle.Location = new System.Windows.Point(X, Y);
        }
    }

    public class CommunicationItem : Item, INotifyPropertyChanged, ICommunicates
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

        #region Communication Properties
        private ulong _Address64 { get; set; }
        ulong ICommunicates.Address64
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
        private ushort _Address16 { get; set; }
        ushort ICommunicates.Address16
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
        private bool _IsCommunicating { get; set; }
        bool ICommunicates.IsCommunicating
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
        #endregion

        public CommunicationItem(String Name, UInt64 MAC_Address, string Colour) : base(Name)
        {
            this.Name = Name;
            this.Group = "All";
            this.Colour = Colour;

            ICommunicates comms = this;
            comms.Address16 = 0xFFFE;
            comms.Address64 = MAC_Address;
        }
    }
}
