using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ImageDisplayLib
{
    /// <summary>
    /// Interaction logic for Slider2.xaml
    /// </summary>
    public partial class Slider2 : UserControl
    {
        public static DependencyProperty MinValueProperty = DependencyProperty.Register("MinValue", typeof(double), typeof(Slider2), new PropertyMetadata(0.0));
        public static DependencyProperty MaxValueProperty = DependencyProperty.Register("MaxValue", typeof(double), typeof(Slider2), new PropertyMetadata(100.0));
        public static DependencyProperty LeftThumbProperty = DependencyProperty.Register("LeftThumb", typeof(double), typeof(Slider2),
            new PropertyMetadata(1.0, OnLeftThumbPropertyChanged, CoerceLeftThumb));
        public static DependencyProperty RightThumbProperty = DependencyProperty.Register("RightThumb", typeof(double), typeof(Slider2),
            new PropertyMetadata(50.0, OnRightThumbPropertyChanged, CoerceRightThumb));
        public static DependencyProperty MinDifferenceProperty = DependencyProperty.Register("MinDifference", typeof(double), typeof(Slider2), new PropertyMetadata(1.0));
        public static DependencyProperty TrackBrushProperty = DependencyProperty.Register("TrackBrush", typeof(Brush), typeof(Slider2), new PropertyMetadata(Brushes.DarkGray));
        public static DependencyProperty LeftThumbBrushProperty = DependencyProperty.Register("LeftThumbBrush", typeof(Brush), typeof(Slider2), new PropertyMetadata(Brushes.DarkGreen));
        public static DependencyProperty RightThumbBrushProperty = DependencyProperty.Register("RightThumbBrush", typeof(Brush), typeof(Slider2), new PropertyMetadata(Brushes.DarkBlue));
        public static DependencyProperty IsLeftThumbDraggingProperty = DependencyProperty.Register("IsLeftThumbDragging", typeof(bool), typeof(Slider2), new PropertyMetadata(false, OnIsLeftThumbDraggingPropertyChanged));
        public static DependencyProperty IsRightThumbDraggingProperty = DependencyProperty.Register("IsRightThumbDragging", typeof(bool), typeof(Slider2), new PropertyMetadata(false, OnIsRightThumbDraggingPropertyChanged));

        public bool IsLeftThumbDragging
        {
            get => (bool)GetValue(IsLeftThumbDraggingProperty);
            set => SetValue(IsLeftThumbDraggingProperty, value);
        }
        public bool IsRightThumbDragging
        {
            get => (bool)GetValue(IsRightThumbDraggingProperty);
            set => SetValue(IsRightThumbDraggingProperty, value);
        }
        public double MinValue
        {
            get => (double)GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }
        public double MaxValue
        {
            get => (double)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }
        public double LeftThumb
        {
            get => (double)GetValue(LeftThumbProperty);
            set => SetValue(LeftThumbProperty, value);
        }
        public double RightThumb
        {
            get => (double)GetValue(RightThumbProperty);
            set => SetValue(RightThumbProperty, value);
        }
        public double MinDifference
        {
            get => (double)GetValue(MinDifferenceProperty);
            set => SetValue(MinDifferenceProperty, value);
        }
        public Brush TrackBrush
        {
            get => (Brush)GetValue(TrackBrushProperty);
            set => SetValue(TrackBrushProperty, value);
        }
        public Brush LeftThumbBrush
        {
            get => (Brush)GetValue(LeftThumbBrushProperty);
            set => SetValue(LeftThumbBrushProperty, value);
        }
        public Brush RightThumbBrush
        {
            get => (Brush)GetValue(RightThumbBrushProperty);
            set => SetValue(RightThumbBrushProperty, value);
        }

        public event DependencyPropertyChangedEventHandler LeftThumbChanged;
        public event DependencyPropertyChangedEventHandler RightThumbChanged;
        public event DependencyPropertyChangedEventHandler IsLeftThumbDraggingChanged;
        public event DependencyPropertyChangedEventHandler IsRightThumbDraggingChanged;

        public Slider2()
        {
            InitializeComponent();           
        }

        protected virtual void OnLeftThumbChanged(object sender, DependencyPropertyChangedEventArgs e)
            => LeftThumbChanged?.Invoke(sender, e);
        protected virtual void OnRightThumbChanged(object sender, DependencyPropertyChangedEventArgs e)
            => RightThumbChanged?.Invoke(sender, e);
        protected virtual void OnIsLeftThumbDraggingChanged(object sender, DependencyPropertyChangedEventArgs e)
            => IsLeftThumbDraggingChanged?.Invoke(sender, e);
        protected virtual void OnIsRightThumbDraggingChanged(object sender, DependencyPropertyChangedEventArgs e)
            => IsRightThumbDraggingChanged?.Invoke(sender, e);

        protected static void OnLeftThumbPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Slider2 slider)
                slider.OnLeftThumbChanged(sender, e);
            else throw new Exception();
        }
        protected static void OnRightThumbPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Slider2 slider)
                slider.OnRightThumbChanged(sender, e);
            else throw new Exception();
        }
        protected static void OnIsLeftThumbDraggingPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Slider2 slider)
                slider.OnIsLeftThumbDraggingChanged(sender, e);
            else throw new Exception();
        }
        protected static void OnIsRightThumbDraggingPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Slider2 slider)
                slider.OnIsRightThumbDraggingChanged(sender, e);
            else throw new Exception();
        }

        private static object CoerceLeftThumb(DependencyObject d, object value)
        {
            double val = (double)value;

            if (val < (double)d.GetValue(MinValueProperty))
                return (double)d.GetValue(MinValueProperty);
            else if (((double)d.GetValue(RightThumbProperty) - val) < (double)d.GetValue(MinDifferenceProperty))
                return (double)d.GetValue(RightThumbProperty) - (double)d.GetValue(MinDifferenceProperty);
            else return val;
        }

        private static object CoerceRightThumb(DependencyObject d, object value)
        {
            double val = (double)value;

            if ((val - (double)d.GetValue(LeftThumbProperty)) < (double)d.GetValue(MinDifferenceProperty))
                return (double)d.GetValue(LeftThumbProperty) + (double)d.GetValue(MinDifferenceProperty);
            else
            if (val > (double)d.GetValue(MaxValueProperty))
                return (double)d.GetValue(MaxValueProperty);
            else return val;

        }

        private void ThumbSlider_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement).Name == "LeftThumbSlider")
            {
                IsLeftThumbDragging = e.LeftButton == MouseButtonState.Pressed;
                IsRightThumbDragging &= !IsLeftThumbDragging;
            }
            else if ((sender as FrameworkElement).Name == "RightThumbSlider")
            {
                IsRightThumbDragging = e.LeftButton == MouseButtonState.Pressed;
                IsLeftThumbDragging &= !IsRightThumbDragging;
            }

        }

        private void ThumbSlider_MouseUp(object sender, MouseButtonEventArgs e)
        {

            if ((sender as FrameworkElement).Name == "LeftThumbSlider")
            {
                IsLeftThumbDragging = e.LeftButton == MouseButtonState.Pressed;
                
            }
            else if ((sender as FrameworkElement).Name == "RightThumbSlider")
            { 
                IsRightThumbDragging = e.LeftButton == MouseButtonState.Pressed;
                
            }
            
        }

        
        private void UnderlyingCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsLeftThumbDragging | IsRightThumbDragging)
            {
                double xPos = e.GetPosition(UnderlyingCanvas).X;

                double offset = xPos - 0.5 * (UnderlyingCanvas.Width - Track.Width);

                double val = MinValue + offset * (MaxValue - MinValue) / Track.Width;

                if (IsLeftThumbDragging)
                    LeftThumb = val;
                else
                    RightThumb = val;
            }

            
        }

        private void UnderlyingCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
          
            if (IsLeftThumbDragging && e.LeftButton == MouseButtonState.Released)
                IsLeftThumbDragging = false;
            if (IsRightThumbDragging && e.LeftButton == MouseButtonState.Released)
                IsRightThumbDragging = false;
        }

        private void UnderlyingCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (IsLeftThumbDragging)
                IsLeftThumbDragging = false;
            if (IsRightThumbDragging)
                IsRightThumbDragging = false;
        }

        
    }
}
