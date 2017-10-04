using System;
using System.Collections.Generic;
using System.IO;
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
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class ImageDisplay : UserControl
    {
        /// <summary>
        /// Initial image in any data format
        /// </summary>
        private Image initialImage;
        /// <summary>
        /// Image scaled to Int16 and displayed
        /// </summary>
        private Image displayedImage;

        /// <summary>
        /// Image Width dependency property.
        /// </summary>
        public static readonly DependencyProperty DisplayedImageWidthProperty =
            DependencyProperty.Register("DisplayedImageWidth", typeof(int), typeof(ImageDisplay), new PropertyMetadata(640, OnDisplayImageWidthChanged), value => (int)value > 0);
        /// <summary>
        /// Image Height dependency property
        /// </summary>
        public static readonly DependencyProperty DisplayedImageHeightProperty =
            DependencyProperty.Register("DisplayedImageHeight", typeof(int), typeof(ImageDisplay), new PropertyMetadata(480, OnDisplayImageHeightChanged), value => (int)value > 0);
        /// <summary>
        /// Image Name dependency property
        /// </summary>
        public static readonly DependencyProperty ImageNameProperty =
           DependencyProperty.Register("ImageName", typeof(string), typeof(ImageDisplay), new PropertyMetadata("", OnImageNameChanged));
        /// <summary>
        /// Dependencing property responsible for <see cref="bool"/> flag IsSamplingEnabled
        /// </summary>
        public static readonly DependencyProperty IsSamplingEnabledProperty =
            DependencyProperty.Register("IsSamplingEnabled", typeof(bool), typeof(ImageDisplay), new PropertyMetadata(false, OnIsSamplingEnabledChanged));

        /// <summary>
        /// Image width (pix).
        /// </summary>
        public int DisplayedImageWidth
        {
            get => (int)GetValue(DisplayedImageWidthProperty);
            set
            {
                SetValue(DisplayedImageWidthProperty, value);
                //PropertyChanged
            }
            
        }
        /// <summary>
        /// Image height (pix).
        /// </summary>
        public int DisplayedImageHeight
        {
            get => (int)GetValue(DisplayedImageHeightProperty);
            set => SetValue(DisplayedImageHeightProperty, value);
        }
        /// <summary>
        /// Displayed image name
        /// </summary>
        public string ImageName
        {
            get => (string)GetValue(ImageNameProperty);
            set => SetValue(ImageNameProperty, value);
        }
        /// <summary>
        /// If true, cursor can be used to peek pixel values of the initial image.
        /// </summary>
        public bool IsSamplingEnabled
        {
            get => (bool)GetValue(IsSamplingEnabledProperty);
            set => SetValue(IsSamplingEnabledProperty, value);
        }
        
        /// <summary>
        /// Fires on IsSamplingEnabled changed.
        /// </summary>
        public static event DependencyPropertyChangedEventHandler IsSamplingEnabledChanged;
        /// <summary>
        /// Fires on ImageName changed.
        /// </summary>
        public static event DependencyPropertyChangedEventHandler ImageNameChanged;
        /// <summary>
        /// Fires on ImageWidth changed.
        /// </summary>
        public static event DependencyPropertyChangedEventHandler DisplayImageWidthChanged;
        /// <summary>
        /// Fires on ImageHeight changed.
        /// </summary>
        public static event DependencyPropertyChangedEventHandler DisplayImageHeightChanged;

        /// <summary>
        /// Default constructor
        /// </summary>
        public ImageDisplay()
        {
            // Initializes components. WPF-related.
            InitializeComponent();
            // Sets data context to this; enables binding options.
            DataContext = this;

            // On IsSamlingEnabled changed clears text fields.
            IsSamplingEnabledChanged += ClearTextFields;
            
        }

        public void LoadImage(Image image)
        {
            
            if (initialImage == null)
                LoadImage(image, 0.00, 1.0);
            else
            {
                double low =  SliderTwo.LeftThumb;
                double high = SliderTwo.RightThumb;
                initialImage = image.Copy();
                DisplayedImageWidth = image.Width;
                DisplayedImageHeight = image.Height;
                

                dynamic imageMin = initialImage.Min();
                dynamic imageMax = initialImage.Max();

                SliderTwo.MinValue = 1.0 * imageMin;
                SliderTwo.MaxValue = 1.0 * imageMax;
                SliderTwo.LeftThumb = Math.Max(low, 1.0 * imageMin);
                SliderTwo.RightThumb = Math.Min(high, 1.0 * imageMax);
                SliderTwo.MinDifference = 0.025 * (SliderTwo.MaxValue - SliderTwo.MinValue);

                displayedImage = image.Copy();

                SliderTwo_IsThumbDraggingChanged(SliderTwo, new DependencyPropertyChangedEventArgs(Slider2.IsLeftThumbDraggingProperty, false, false));

            }
        }
        public void LoadImage(Image image, double low = 0.000, double high = 1)
        {

            initialImage = image.Copy();
            DisplayedImageWidth = image.Width;
            DisplayedImageHeight = image.Height;
            dynamic imageMin = initialImage.Min();
            dynamic imageMax = initialImage.Max();

            SliderTwo.MinValue = 1.0 * imageMin;
            SliderTwo.MaxValue = 1.0 * imageMax;

            SliderTwo.MinDifference = 0.025 * (SliderTwo.MaxValue - SliderTwo.MinValue);
            SliderTwo.RightThumb = SliderTwo.MaxValue;

            SliderTwo.LeftThumb = initialImage.Percentile(low);
            SliderTwo.RightThumb = Math.Max(initialImage.Percentile(high), SliderTwo.LeftThumb + SliderTwo.MinDifference);

            displayedImage = initialImage.Copy();

            SliderTwo_IsThumbDraggingChanged(SliderTwo, new DependencyPropertyChangedEventArgs(Slider2.IsLeftThumbDraggingProperty, false, false));

        }

        public void UpdateFrame(double left, double right)
        {
            PixelFormat pf = PixelFormats.Gray16;
            initialImage.CopyTo(displayedImage);
            displayedImage.Clamp(left, right);
            int stride = (displayedImage.Width * pf.BitsPerPixel + 7) / 8;

            switch (displayedImage.UnderlyingType)
            {
                case TypeCode.Int16:
                    displayedImage.Scale(Int16.MinValue, Int16.MaxValue);
                    displayedImage = displayedImage
                        .CastTo<Int16, UInt16>(x => (UInt16)(x - Int16.MinValue));
                    break;
                case TypeCode.Single:
                    dynamic locMax = displayedImage.Max();
                    dynamic locMin = displayedImage.Min();
                    double dLocMin = 1.0 * locMin;
                    double dLocMax = 1.0 * locMax;
                    displayedImage = displayedImage
                        .CastTo<Single, UInt16>(x => (UInt16)((UInt16.MaxValue)*(x - dLocMin)/(dLocMax - dLocMin)));
                    break;
            }
            ImageFrame.Source = BitmapSource.Create(displayedImage.Width, displayedImage.Height, 300, 300, pf, BitmapPalettes.Gray256,
                displayedImage.GetBytes(), stride);
    
           
        }

        private void ImageFrame_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsSamplingEnabled)
            {
                var pos = e.GetPosition(ImageFrame);

                int x = (int)Math.Round(DisplayedImageWidth * pos.X / ImageFrame.ActualWidth, 0);
                int y = (int)Math.Round(DisplayedImageHeight * pos.Y / ImageFrame.ActualHeight, 0);

                x = x < 0 ? 0 : x;
                y = y < 0 ? 0 : y;
                x = x >= DisplayedImageWidth ? DisplayedImageWidth - 1: x;
                y = y >= DisplayedImageHeight ? DisplayedImageHeight - 1: y;

                CoordinatesXText.Text = x.ToString();
                CoordinatesYText.Text = y.ToString();
                MeasuredValueText.Text = initialImage[y, x].ToString();
                                  
            }
        }

        private static void ClearTextFields(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is ImageDisplay imDis)
            {
                if ((bool)e.NewValue == false)
                {
                    imDis.CoordinatesXText.Text = "";
                    imDis.CoordinatesYText.Text = "";
                    imDis.MeasuredValueText.Text = "";
                }
            }
            else throw new Exception();
        }
        
        protected static void OnIsSamplingEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
            =>  IsSamplingEnabledChanged?.Invoke(sender, e);
        protected static void OnImageNameChanged(object sender, DependencyPropertyChangedEventArgs e)
            => ImageNameChanged?.Invoke(sender, e);
        protected static void OnDisplayImageWidthChanged(object sender, DependencyPropertyChangedEventArgs e)
            => DisplayImageWidthChanged?.Invoke(sender, e);
        protected static void OnDisplayImageHeightChanged(object sender, DependencyPropertyChangedEventArgs e)
        => DisplayImageHeightChanged?.Invoke(sender, e);

        private void SliderTwo_IsThumbDraggingChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == false & initialImage != null)
            {
                double left = SliderTwo.LeftThumb;
                double right = SliderTwo.RightThumb;


                UpdateFrame(left, right);

            }

        }

        private void SliderTwo_ThumbChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
          
        }
    }


}
