﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SwarmRoboticsGUI
{
    /// <summary>
    /// Interaction logic for IntegerUpDown.xaml
    /// </summary>
    public partial class IntegerUpDown : UserControl
    {
        public IntegerUpDown()
        {
            InitializeComponent();
        }

        public int Maximum
        {
            get { return (int)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }
        public readonly static DependencyProperty MaximumProperty = DependencyProperty.Register(
            "Maximum", typeof(int), typeof(IntegerUpDown), new UIPropertyMetadata(int.MaxValue));

        public int Minimum
        {
            get { return (int)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }
        public readonly static DependencyProperty MinimumProperty = DependencyProperty.Register(
            "Minimum", typeof(int), typeof(IntegerUpDown), new UIPropertyMetadata(int.MinValue));


        public int Interval
        {
            get { return (int)GetValue(IntervalProperty); }
            set { SetValue(IntervalProperty, value); }
        }
        public readonly static DependencyProperty IntervalProperty = DependencyProperty.Register(
            "Interval", typeof(int), typeof(IntegerUpDown), new UIPropertyMetadata(33));

        public int Delay
        {
            get { return (int)GetValue(DelayProperty); }
            set { SetValue(DelayProperty, value); }
        }
        public readonly static DependencyProperty DelayProperty = DependencyProperty.Register(
            "Delay", typeof(int), typeof(IntegerUpDown), new UIPropertyMetadata(500));


        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetCurrentValue(TextProperty, value); }
        }
        public readonly static DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(string), typeof(IntegerUpDown), new UIPropertyMetadata(string.Empty));

        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetCurrentValue(ValueProperty, value); }
        }
        public readonly static DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(int), typeof(IntegerUpDown), new UIPropertyMetadata(0, (o, e) =>
            {
                IntegerUpDown tb = (IntegerUpDown)o;
                tb.RaiseValueChangedEvent(e);
            }));

        public event EventHandler<DependencyPropertyChangedEventArgs> ValueChanged;
        private void RaiseValueChangedEvent(DependencyPropertyChangedEventArgs e)
        {
            if (ValueChanged != null)
                ValueChanged(this, e);
        }


        public int Step
        {
            get { return (int)GetValue(StepProperty); }
            set { SetValue(StepProperty, value); }
        }
        public readonly static DependencyProperty StepProperty = DependencyProperty.Register(
            "Step", typeof(int), typeof(IntegerUpDown), new UIPropertyMetadata(1));



        RepeatButton _UpButton;
        RepeatButton _DownButton;
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _UpButton = Template.FindName("PART_UpButton", this) as RepeatButton;
            _DownButton = Template.FindName("PART_DownButton", this) as RepeatButton;
            if (_UpButton != null)
                _UpButton.Click += btup_Click;
            if (_DownButton != null)
                _DownButton.Click += btdown_Click;
        }


        private void btup_Click(object sender, RoutedEventArgs e)
        {
            if (Value < Maximum)
            {
                Value += Step;
                if (Value > Maximum)
                    Value = Maximum;
            }
        }

        private void btdown_Click(object sender, RoutedEventArgs e)
        {
            if (Value > Minimum)
            {
                Value -= Step;
                if (Value < Minimum)
                    Value = Minimum;
            }
        }

    }
}
