using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DipolImage;

namespace DIPOL_UF.Models
{
    public class DipolImagePresenter : ObservableObject
    {
        private Image _sourceImage;
        private Image _displayedImage;
        private WriteableBitmap _bitmapSource;

        public WriteableBitmap BitmapSource
        {
            get => _bitmapSource;
            set
            {
                _bitmapSource = value;
                RaisePropertyChanged();
            }

        }

        public async Task LoadImage(Image image)
        {

            await CopyImage(image);
            await UpdateBitmap();
        }

        private async Task CopyImage(Image image)
        {
            switch (image.UnderlyingType)
            {
                case TypeCode.UInt16:
                    _sourceImage = await Task.Run(() => image.CastTo<ushort, float>(x => x));
                    _displayedImage = _sourceImage.Copy();
                    break;
                default:
                    throw  new Exception();
            }

            _displayedImage.Scale(0, 1);
        }

        private async Task UpdateBitmap()
        {
            if (_bitmapSource == null ||
                _bitmapSource.PixelWidth != _sourceImage.Width ||
                _bitmapSource.PixelHeight != _sourceImage.Height)
            {
                if (System.Threading.Thread.CurrentThread == Application.Current.Dispatcher.Thread)
                    _bitmapSource = new WriteableBitmap(_sourceImage.Width, _sourceImage.Height,
                        96, 96, PixelFormats.Gray32Float, null);
                else
                    _bitmapSource = Application.Current.Dispatcher.Invoke(() => new WriteableBitmap(_sourceImage.Width,
                        _sourceImage.Height,
                        96, 96, PixelFormats.Gray32Float, null));

            }

            var bytes = await Task.Run((Func<byte[]>)_displayedImage.GetBytes);

            try
            {
              Helper.ExecuteOnUI(_bitmapSource.Lock);
                Helper.ExecuteOnUI(() => System.Runtime.InteropServices.Marshal.Copy(
                    bytes, 0, _bitmapSource.BackBuffer, bytes.Length));
            }
            catch (Exception e)
            {

            }
            finally
            {
                Helper.ExecuteOnUI(() => _bitmapSource.AddDirtyRect(new Int32Rect(0, 0, _sourceImage.Width, _sourceImage.Height)));
                Helper.ExecuteOnUI(_bitmapSource.Unlock);
                Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(BitmapSource)));
            }
        }
    }
}
