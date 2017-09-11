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

        private Image initialImage;
        private Image displayedImage;

        public static readonly DependencyProperty DisplayedImageWidthProperty =
            DependencyProperty.Register("DisplayedImageWidth", typeof(int), typeof(ImageDisplay));
        public static readonly DependencyProperty DisplayedImageHeightProperty =
            DependencyProperty.Register("DisplayedImageHeight", typeof(int), typeof(ImageDisplay));
        public static readonly DependencyProperty ImageNameProperty =
           DependencyProperty.Register("ImageName", typeof(string), typeof(ImageDisplay));

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


        public ImageDisplay()
        {
            InitializeComponent();
            DataContext = this;           
        }

        public void LoadImage(Image image, ImageType type)
        {
            displayedImage = image.Copy();
            initialImage = image.Copy();
            imageType = type;
            DisplayedImageWidth = image.Width;
            DisplayedImageHeight = image.Height;

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
            if (Debug_EnablePeek.IsChecked ?? false)
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
                this.MeasuredValueText.Text = initialImage[y, x].ToString();
                //CoordinatesText.Text = String.Format("[{0,-8:.00}:{1,8:.00}] : {2,16:e4}", x, y, ((Int16[,])rawAray)[y,x]);
                    
            }
        }

     
        
    }


}
