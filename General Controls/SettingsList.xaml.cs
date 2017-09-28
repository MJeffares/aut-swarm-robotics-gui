using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;


namespace SwarmRoboticsGUI.Settings
{

    public class SettingsGroup : Item, INotifyPropertyChanged
    {
        public SettingsGroup(string Name) : base(Name)
        {
            Children = new List<Item>();
        }
    }

    public class SettingsRange : Item, INotifyPropertyChanged
    {
        private double _Lower { get; set; }
        public double Lower
        {
            get { return _Lower; }
            set
            {
                if (_Lower != value)
                {
                    _Lower = value;
                    NotifyPropertyChanged("Lower");
                }
            }
        }
        private double _Upper { get; set; }
        public double Upper
        {
            get { return _Upper; }
            set
            {
                if (_Upper != value)
                {
                    _Upper = value;
                    NotifyPropertyChanged("Upper");
                }
            }
        }

        private double _Minimum { get; set; }
        public double Minimum
        {
            get { return _Minimum; }
            set
            {
                if (_Minimum != value)
                {
                    _Minimum = value;
                }
            }
        }

        private double _Maximum { get; set; }
        public double Maximum
        {
            get { return _Maximum; }
            set
            {
                if (_Maximum != value)
                {
                    _Maximum = value;
                }
            }
        }

        public SettingsRange(string Name, double Lower, double Upper, double Minimum, double Maximum) : base(Name)
        {
            this.Lower = Lower;
            this.Upper = Upper;
            this.Minimum = Minimum;
            this.Maximum = Maximum;
        }


    }

    public class SettingsValue : Item, INotifyPropertyChanged
    {
        private double _Value { get; set; }
        public double Value
        {
            get { return _Value; }
            set
            {
                if (_Value != value)
                {
                    _Value = value;
                    NotifyPropertyChanged("Value");
                }
            }
        }
        private double _Minimum { get; set; }
        public double Minimum
        {
            get { return _Minimum; }
            set
            {
                if (_Minimum != value)
                {
                    _Minimum = value;
                }
            }
        }

        private double _Maximum { get; set; }
        public double Maximum
        {
            get { return _Maximum; }
            set
            {
                if (_Maximum != value)
                {
                    _Maximum = value;
                }
            }
        }

        public SettingsValue(string Name, double Value, double Minimum, double Maximum) : base(Name)
        {
            this.Value = Value;
            this.Minimum = Minimum;
            this.Maximum = Maximum;
        }
    }


    /// <summary>
    /// Interaction logic for ImageProcessSettings.xaml
    /// </summary>
    public partial class SettingsList : UserControl
    {
        public SettingsList()
        {
            InitializeComponent();
        }
    }
}
