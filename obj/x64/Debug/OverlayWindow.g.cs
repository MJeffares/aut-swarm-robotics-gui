﻿#pragma checksum "..\..\..\OverlayWindow.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "6DC4A84630FC62ED95BE133034486872"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Emgu.CV.UI;
using SwarmRoboticsGUI;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace SwarmRoboticsGUI {
    
    
    /// <summary>
    /// OverlayWindow
    /// </summary>
    public partial class OverlayWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 2 "..\..\..\OverlayWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal SwarmRoboticsGUI.OverlayWindow Overlay;
        
        #line default
        #line hidden
        
        
        #line 13 "..\..\..\OverlayWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Forms.Integration.WindowsFormsHost host1;
        
        #line default
        #line hidden
        
        
        #line 14 "..\..\..\OverlayWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal Emgu.CV.UI.ImageBox OverlayImageBox;
        
        #line default
        #line hidden
        
        
        #line 40 "..\..\..\OverlayWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Slider slider;
        
        #line default
        #line hidden
        
        
        #line 41 "..\..\..\OverlayWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Slider slider1;
        
        #line default
        #line hidden
        
        
        #line 47 "..\..\..\OverlayWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Slider slider2;
        
        #line default
        #line hidden
        
        
        #line 48 "..\..\..\OverlayWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Slider slider3;
        
        #line default
        #line hidden
        
        
        #line 49 "..\..\..\OverlayWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Slider slider4;
        
        #line default
        #line hidden
        
        
        #line 54 "..\..\..\OverlayWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Slider slider5;
        
        #line default
        #line hidden
        
        
        #line 55 "..\..\..\OverlayWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Slider slider6;
        
        #line default
        #line hidden
        
        
        #line 60 "..\..\..\OverlayWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Slider slider7;
        
        #line default
        #line hidden
        
        
        #line 61 "..\..\..\OverlayWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Slider slider8;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/SwarmRoboticsGUI;component/overlaywindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\OverlayWindow.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.Overlay = ((SwarmRoboticsGUI.OverlayWindow)(target));
            
            #line 10 "..\..\..\OverlayWindow.xaml"
            this.Overlay.Closing += new System.ComponentModel.CancelEventHandler(this.Overlay_Closing);
            
            #line default
            #line hidden
            return;
            case 2:
            this.host1 = ((System.Windows.Forms.Integration.WindowsFormsHost)(target));
            return;
            case 3:
            this.OverlayImageBox = ((Emgu.CV.UI.ImageBox)(target));
            return;
            case 4:
            this.slider = ((System.Windows.Controls.Slider)(target));
            return;
            case 5:
            this.slider1 = ((System.Windows.Controls.Slider)(target));
            return;
            case 6:
            this.slider2 = ((System.Windows.Controls.Slider)(target));
            return;
            case 7:
            this.slider3 = ((System.Windows.Controls.Slider)(target));
            return;
            case 8:
            this.slider4 = ((System.Windows.Controls.Slider)(target));
            return;
            case 9:
            this.slider5 = ((System.Windows.Controls.Slider)(target));
            return;
            case 10:
            this.slider6 = ((System.Windows.Controls.Slider)(target));
            return;
            case 11:
            this.slider7 = ((System.Windows.Controls.Slider)(target));
            return;
            case 12:
            this.slider8 = ((System.Windows.Controls.Slider)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}

