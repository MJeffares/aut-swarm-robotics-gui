﻿#pragma checksum "..\..\CustomMessageBoxWindow.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "5FCA93F33630EFF3A00610E78DE9D6B4"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
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


namespace WPFCustomMessageBox {
    
    
    internal partial class CustomMessageBoxWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 20 "..\..\CustomMessageBoxWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Image Image_MessageBox;
        
        #line default
        #line hidden
        
        
        #line 21 "..\..\CustomMessageBoxWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock TextBlock_Message;
        
        #line default
        #line hidden
        
        
        #line 29 "..\..\CustomMessageBoxWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button Button_Cancel;
        
        #line default
        #line hidden
        
        
        #line 31 "..\..\CustomMessageBoxWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label Label_Cancel;
        
        #line default
        #line hidden
        
        
        #line 36 "..\..\CustomMessageBoxWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button Button_No;
        
        #line default
        #line hidden
        
        
        #line 38 "..\..\CustomMessageBoxWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label Label_No;
        
        #line default
        #line hidden
        
        
        #line 43 "..\..\CustomMessageBoxWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button Button_Yes;
        
        #line default
        #line hidden
        
        
        #line 45 "..\..\CustomMessageBoxWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label Label_Yes;
        
        #line default
        #line hidden
        
        
        #line 50 "..\..\CustomMessageBoxWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button Button_OK;
        
        #line default
        #line hidden
        
        
        #line 52 "..\..\CustomMessageBoxWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label Label_Ok;
        
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
            System.Uri resourceLocater = new System.Uri("/WPFCustomMessageBox;component/custommessageboxwindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\CustomMessageBoxWindow.xaml"
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
            this.Image_MessageBox = ((System.Windows.Controls.Image)(target));
            return;
            case 2:
            this.TextBlock_Message = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 3:
            this.Button_Cancel = ((System.Windows.Controls.Button)(target));
            
            #line 30 "..\..\CustomMessageBoxWindow.xaml"
            this.Button_Cancel.Click += new System.Windows.RoutedEventHandler(this.Button_Cancel_Click);
            
            #line default
            #line hidden
            return;
            case 4:
            this.Label_Cancel = ((System.Windows.Controls.Label)(target));
            return;
            case 5:
            this.Button_No = ((System.Windows.Controls.Button)(target));
            
            #line 37 "..\..\CustomMessageBoxWindow.xaml"
            this.Button_No.Click += new System.Windows.RoutedEventHandler(this.Button_No_Click);
            
            #line default
            #line hidden
            return;
            case 6:
            this.Label_No = ((System.Windows.Controls.Label)(target));
            return;
            case 7:
            this.Button_Yes = ((System.Windows.Controls.Button)(target));
            
            #line 44 "..\..\CustomMessageBoxWindow.xaml"
            this.Button_Yes.Click += new System.Windows.RoutedEventHandler(this.Button_Yes_Click);
            
            #line default
            #line hidden
            return;
            case 8:
            this.Label_Yes = ((System.Windows.Controls.Label)(target));
            return;
            case 9:
            this.Button_OK = ((System.Windows.Controls.Button)(target));
            
            #line 51 "..\..\CustomMessageBoxWindow.xaml"
            this.Button_OK.Click += new System.Windows.RoutedEventHandler(this.Button_OK_Click);
            
            #line default
            #line hidden
            return;
            case 10:
            this.Label_Ok = ((System.Windows.Controls.Label)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}
