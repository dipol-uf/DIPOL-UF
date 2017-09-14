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

namespace ImageTest
{
    /// <summary>
    /// Interaction logic for TestWindow.xaml
    /// </summary>
    public partial class TestWindow : Window
    {
        public TestWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            int N = 4096;
            int M = 8192;
            UInt16[] arr = new UInt16[N * M];

            for (int i = 0; i < N; i++)
                for (int j = 0; j < M; j++)
                    arr[M * i + j] = (UInt16)(60*(i+j) + 1000) ;

            var im = new ImageDisplayLib.Image(arr, M, N);

            var max = im.Max();            

            ImageHandle.LoadImage(im, ImageDisplayLib.ImageType.GrayScale16Int);

        }
    }
}
