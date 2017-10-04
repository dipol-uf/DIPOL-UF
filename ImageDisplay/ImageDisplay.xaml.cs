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

        /// <summary>
        /// Loads image to the frame.
        /// </summary>
        /// <param name="image">Image to load</param>
        public void LoadImage(Image image)
        {
            // If there is no initial image (first time sonething is loaded)
            if (initialImage == null)
                LoadImage(image, 0.00, 1.0);
            // Else, keep selected range of values (if possible).
            // Works OK when a series of similar images is loaded with similar dynamic range
            else
            {
                // Preserves current settings; lower and upper limits
                double low =  SliderTwo.LeftThumb;
                double high = SliderTwo.RightThumb;
                // Loads provided image into control's memory
                initialImage = image.Copy();
                // Assigns Width/Height properties
                DisplayedImageWidth = image.Width;
                DisplayedImageHeight = image.Height;
                
                // Determines image dynamic range
                dynamic imageMin = initialImage.Min();
                dynamic imageMax = initialImage.Max();

                // Min/max slider settings
                SliderTwo.MinValue = 1.0 * imageMin;
                SliderTwo.MaxValue = 1.0 * imageMax;
                // Restores slider settings (if possible)
                SliderTwo.LeftThumb = Math.Max(low, 1.0 * imageMin);
                SliderTwo.RightThumb = Math.Min(high, 1.0 * imageMax);
                SliderTwo.MinDifference = 0.025 * (SliderTwo.MaxValue - SliderTwo.MinValue);


                // Manually fires SliderChange event to update content
                SliderTwo_IsThumbDraggingChanged(SliderTwo, new DependencyPropertyChangedEventArgs(Slider2.IsLeftThumbDraggingProperty, false, false));

            }
        }

        /// <summary>
        /// Loads image to the frame. Resets dynamic range selection.
        /// </summary>
        /// <param name="image">Image to load.</param>
        /// <param name="low">Lower percentile to clamp.</param>
        /// <param name="high">Upper percentile to clamp.</param>
        public void LoadImage(Image image, double low = 0.000, double high = 1)
        {
            // Overwrites existing image with a new one.
            initialImage = image.Copy();
            // Assigns Height/Width properties
            DisplayedImageWidth = image.Width;
            DisplayedImageHeight = image.Height;

            // Determines dynamic ranges
            dynamic imageMin = initialImage.Min();
            dynamic imageMax = initialImage.Max();

            // Sets slider limits
            SliderTwo.MinValue = 1.0 * imageMin;
            SliderTwo.MaxValue = 1.0 * imageMax;

            SliderTwo.MinDifference = 0.025 * (SliderTwo.MaxValue - SliderTwo.MinValue);

            // First, sets right thumb to rightmost value.
            SliderTwo.RightThumb = SliderTwo.MaxValue;
            // Then, sets left thumb.
            SliderTwo.LeftThumb = initialImage.Percentile(low);
            // Finally, changes position of right thumb. Avoids locks and collisions of thumbs
            SliderTwo.RightThumb = Math.Max(initialImage.Percentile(high), SliderTwo.LeftThumb + SliderTwo.MinDifference);

            // Manually invokes event to update control
            SliderTwo_IsThumbDraggingChanged(SliderTwo, new DependencyPropertyChangedEventArgs(Slider2.IsLeftThumbDraggingProperty, false, false));

        }

        public async Task UpdateFrame(double left, double right)
        {
            PixelFormat pf = PixelFormats.Gray16;
            var task = Task.Run<Image>(() =>
            {
                var locImage = initialImage.Copy();
                locImage.Clamp(left, right);
                switch (locImage.UnderlyingType)
                {
                    case TypeCode.Int16:
                        locImage.Scale(Int16.MinValue, Int16.MaxValue);
                        locImage = locImage
                            .CastTo<Int16, UInt16>(x => (UInt16)(x - Int16.MinValue));
                        break;
                    case TypeCode.Single:
                        dynamic locMax = locImage.Max();
                        dynamic locMin = locImage.Min();
                        double dLocMin = 1.0 * locMin;
                        double dLocMax = 1.0 * locMax;
                        locImage = locImage
                            .CastTo<Single, UInt16>(x => (UInt16)((UInt16.MaxValue) * (x - dLocMin) / (dLocMax - dLocMin)));
                        break;
                }

                return locImage;
            });
            Image displayedImage = await task;

            int stride = (displayedImage.Width * pf.BitsPerPixel + 7) / 8;
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

        private async void SliderTwo_IsThumbDraggingChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == false & initialImage != null)
            {
                double left = SliderTwo.LeftThumb;
                double right = SliderTwo.RightThumb;


                await UpdateFrame(left, right);

            }

        }

        private void SliderTwo_ThumbChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
          
        }
    }


}
