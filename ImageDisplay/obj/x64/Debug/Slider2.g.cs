﻿#pragma checksum "..\..\..\Slider2.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "2A68CDC66AC72C664FC896D913298544"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using ImageDisplayLib;
using ImageDisplayLib.Converters;
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


namespace ImageDisplayLib {
    
    
    /// <summary>
    /// Slider2
    /// </summary>
    public partial class Slider2 : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 12 "..\..\..\Slider2.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Canvas UnderlyingCanvas;
        
        #line default
        #line hidden
        
        
        #line 13 "..\..\..\Slider2.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Shapes.Rectangle Track;
        
        #line default
        #line hidden
        
        
        #line 14 "..\..\..\Slider2.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Shapes.Rectangle LeftThumbSlider;
        
        #line default
        #line hidden
        
        
        #line 27 "..\..\..\Slider2.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Shapes.Rectangle RightThumbSlider;
        
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
            System.Uri resourceLocater = new System.Uri("/ImageDisplay;component/slider2.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\Slider2.xaml"
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
            this.UnderlyingCanvas = ((System.Windows.Controls.Canvas)(target));
            
            #line 12 "..\..\..\Slider2.xaml"
            this.UnderlyingCanvas.MouseMove += new System.Windows.Input.MouseEventHandler(this.UnderlyingCanvas_MouseMove);
            
            #line default
            #line hidden
            
            #line 12 "..\..\..\Slider2.xaml"
            this.UnderlyingCanvas.MouseUp += new System.Windows.Input.MouseButtonEventHandler(this.UnderlyingCanvas_MouseUp);
            
            #line default
            #line hidden
            
            #line 12 "..\..\..\Slider2.xaml"
            this.UnderlyingCanvas.MouseLeave += new System.Windows.Input.MouseEventHandler(this.UnderlyingCanvas_MouseLeave);
            
            #line default
            #line hidden
            return;
            case 2:
            this.Track = ((System.Windows.Shapes.Rectangle)(target));
            return;
            case 3:
            this.LeftThumbSlider = ((System.Windows.Shapes.Rectangle)(target));
            
            #line 14 "..\..\..\Slider2.xaml"
            this.LeftThumbSlider.MouseDown += new System.Windows.Input.MouseButtonEventHandler(this.ThumbSlider_MouseDown);
            
            #line default
            #line hidden
            
            #line 14 "..\..\..\Slider2.xaml"
            this.LeftThumbSlider.MouseUp += new System.Windows.Input.MouseButtonEventHandler(this.ThumbSlider_MouseUp);
            
            #line default
            #line hidden
            return;
            case 4:
            this.RightThumbSlider = ((System.Windows.Shapes.Rectangle)(target));
            
            #line 27 "..\..\..\Slider2.xaml"
            this.RightThumbSlider.MouseDown += new System.Windows.Input.MouseButtonEventHandler(this.ThumbSlider_MouseDown);
            
            #line default
            #line hidden
            
            #line 27 "..\..\..\Slider2.xaml"
            this.RightThumbSlider.MouseUp += new System.Windows.Input.MouseButtonEventHandler(this.ThumbSlider_MouseUp);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}
