﻿using System;
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
            int N = 256;
            int M = 512;
            UInt16[] arr = new UInt16[N * M];

            for (int i = 0; i < N; i++)
                for (int j = 0; j < M; j++)
                    arr[M * i + j] = (UInt16)(30*(i+j) + 10000) ;

            var im = new ImageDisplayLib.Image(arr, M, N);

            var max = im.Max();
            

            ImageHandle.LoadImage(im.Clamp(15000, 30000).Scale(), ImageDisplayLib.ImageType.GrayScale16Int);

        }
    }
}
