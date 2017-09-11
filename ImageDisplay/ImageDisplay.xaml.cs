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

        private ImageType imageType;

        private double oldSliderValue = double.MinValue;

        private Image initialImage;
        private Image displayedImage;

        public static readonly DependencyProperty DisplayedImageWidthProperty =
            DependencyProperty.Register("DisplayedImageWidth", typeof(int), typeof(ImageDisplay), new PropertyMetadata(640, OnDisplayImageWidthChanged), value => (int)value > 0);
        public static readonly DependencyProperty DisplayedImageHeightProperty =
            DependencyProperty.Register("DisplayedImageHeight", typeof(int), typeof(ImageDisplay), new PropertyMetadata(480, OnDisplayImageHeightChanged), value => (int)value > 0);
        public static readonly DependencyProperty ImageNameProperty =
           DependencyProperty.Register("ImageName", typeof(string), typeof(ImageDisplay), new PropertyMetadata("", OnImageNameChanged));
        public static readonly DependencyProperty IsSamplingEnabledProperty =
            DependencyProperty.Register("IsSamplingEnabled", typeof(bool), typeof(ImageDisplay), new PropertyMetadata(false, OnIsSamplingEnabledChanged));


        public int DisplayedImageWidth
        {
            get => (int)GetValue(DisplayedImageWidthProperty);
            set
            {
                SetValue(DisplayedImageWidthProperty, value);
                //PropertyChanged
            }
            
        }
        public int DisplayedImageHeight
        {
            get => (int)GetValue(DisplayedImageHeightProperty);
            set => SetValue(DisplayedImageHeightProperty, value);
        }
        public string ImageName
        {
            get => (string)GetValue(ImageNameProperty);
            set => SetValue(ImageNameProperty, value);
        }
        public bool IsSamplingEnabled
        {
            get => (bool)GetValue(IsSamplingEnabledProperty);
            set => SetValue(IsSamplingEnabledProperty, value);
        }

        public static event DependencyPropertyChangedEventHandler IsSamplingEnabledChanged;
        public static event DependencyPropertyChangedEventHandler ImageNameChanged;
        public static event DependencyPropertyChangedEventHandler DisplayImageWidthChanged;
        public static event DependencyPropertyChangedEventHandler DisplayImageHeightChanged;

        public ImageDisplay()
        {
            InitializeComponent();
            DataContext = this;

            IsSamplingEnabledChanged += ClearTextFields;
            SliderTwo.LeftThumbChanged += Slider2_ThumbChanged;
            SliderTwo.RightThumbChanged += Slider2_ThumbChanged;
        }

        private void Slider2_ThumbChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

            if (Math.Abs((double)e.NewValue - oldSliderValue) > 0.05 * (SliderTwo.MaxValue - SliderTwo.MinValue))
            {
                oldSliderValue = (double)e.NewValue;
                displayedImage = initialImage.Clamp(SliderTwo.LeftThumb, SliderTwo.RightThumb).Scale();

                UpdateFrame();
            }
            
        }

        public void LoadImage(Image image, ImageType type)
        {
            displayedImage = image.Copy();
            initialImage = image.Copy();
            imageType = type;
            DisplayedImageWidth = image.Width;
            DisplayedImageHeight = image.Height;
            dynamic imageMin = initialImage.Min();
            dynamic imageMax = initialImage.Max();

            SliderTwo.MinValue = 1.0 * imageMin;
            SliderTwo.MaxValue = 1.0 * imageMax;
            SliderTwo.RightThumb = SliderTwo.MaxValue;
            SliderTwo.LeftThumb = SliderTwo.MinValue;
            SliderTwo.MinDifference = 0.025*(SliderTwo.MaxValue - SliderTwo.MinValue);
            
            UpdateFrame();
        }

        public void UpdateFrame()
        {
            PixelFormat pf;
            int stride;
            switch (imageType)
            {
                case ImageType.GrayScale16Int:
                    pf = PixelFormats.Gray16;
                    stride = 2 * displayedImage.Width;
                    break;
                case ImageType.GrayScale32Float:
                    pf = PixelFormats.Gray32Float;
                    stride = 4 * displayedImage.Width;
                    break;
                default:
                    throw new Exception();
            }

            ImageFrame.Source = BitmapSource.Create(displayedImage.Width, displayedImage.Height, 300, 300, pf, BitmapPalettes.Gray256, displayedImage.GetBytes(), stride);
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
        
    }


}
