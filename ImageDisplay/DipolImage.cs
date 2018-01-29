using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageDisplayLib
{
    public class DipolImage : DIPOL_UF.ObservableObject
    {
        private static readonly BitmapPalette GrayScalePalette =
            new BitmapPalette(
                Enumerable
                    .Range(1000, 0)
                    .Select(i => Color.FromScRgb(1.0f, 1.0f/i, 1.0f/i, 1.0f/i))
                    .ToList());
        private WriteableBitmap _bitmap;

        public WriteableBitmap Bitmap
        {
            get => _bitmap;
            private set
            {
                if (!Equals(value, _bitmap))
                {
                    _bitmap = value;
                    RaisePropertyChanged();
                }
            }

        }

        public void LoadImage(Image im)
        {
            //var pallete = new BitmapPalette();
            //Bitmap = new WriteableBitmap(im.Width, im.Height, 96, 96, PixelFormats.Gray32Float, BitmapPalettes.);
        }
    }
}
