using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DipolImage;
using DIPOL_UF.Commands;

namespace DIPOL_UF.Models
{
    public class DipolImagePresenter : ObservableObject
    {
        private Image _sourceImage;
        private Image _displayedImage;
        private WriteableBitmap _bitmapSource;
        private double _imgScaleMax = 1000;
        private double _imgScaleMin = 0;
        private double _thumbLeft = 0;
        private double _thumbRight = 1000;
        private DelegateCommand _thumbValueChangedCommand;
        private DelegateCommand _mouseHoverCommand;
        private readonly DispatcherTimer _thumbValueChangedTimer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromMilliseconds(250),
            IsEnabled = false
        };
        private List<Tuple<Point, Action<StreamGeometryContext, Point>>> _samplerGeometryRules 
            = new List<Tuple<Point, Action<StreamGeometryContext, Point>>>(4);

        private double LeftScale => (_thumbLeft - _imgScaleMin) / (_imgScaleMax - _imgScaleMin);
        private double RightScale => (_thumbRight - _imgScaleMin) / (_imgScaleMax - _imgScaleMin);

        public double ImgScaleMax
        {
            get => _imgScaleMax;
            set
            {
                if (Math.Abs(value - _imgScaleMax) > double.Epsilon)
                {
                    _imgScaleMax = value;
                    RaisePropertyChanged();
                }

            }

        }
        public double ImgScaleMin
        {
            get => _imgScaleMin;
            set
            {
                if (Math.Abs(value - _imgScaleMin) > double.Epsilon)
                {
                    _imgScaleMin = value;
                    RaisePropertyChanged();
                }
            }

        }

        public double ThumbLeft
        {
            get => _thumbLeft;
            set
            {
                if (Math.Abs(value - _thumbLeft) > double.Epsilon)
                {
                    if (value < _imgScaleMin)
                        _thumbLeft = _imgScaleMin;
                    else if (_imgScaleMax - value < 1)
                        _thumbLeft = _imgScaleMax - 1;
                    else
                        _thumbLeft = value;
                    
                    RaisePropertyChanged();

                    if (_thumbRight <= _thumbLeft)
                    {
                        _thumbRight = _thumbLeft + 1;
                        RaisePropertyChanged(nameof(ThumbRight));
                    }

                }

            }

        }
        public double ThumbRight
        {
            get => _thumbRight;
            set
            {
                if (Math.Abs(value - _thumbRight) > double.Epsilon)
                {
                    if (value > _imgScaleMax)
                        _thumbRight = _imgScaleMax;
                    else if (value - _imgScaleMin < 1)
                        _thumbRight = _imgScaleMin + 1;
                    else
                        _thumbRight = value;
                    RaisePropertyChanged();

                    if (_thumbLeft >= _thumbRight)
                    {
                        _thumbLeft = _thumbRight - 1;
                        RaisePropertyChanged(nameof(ThumbLeft));
                    }
                }

            }

        }

        public Point MousePos { get; private set; }

        public List<Tuple<Point, Action<StreamGeometryContext, Point>>> SamplerGeometryRules
        {
            get => _samplerGeometryRules;
            set
            {
                _samplerGeometryRules = value;
                RaisePropertyChanged();
            }

        }

        public DelegateCommand ThumbValueChangedCommand
        {
            get => _thumbValueChangedCommand;
            set
            {
                _thumbValueChangedCommand = value;
                RaisePropertyChanged();
            }
        }

        public DelegateCommand MouseHoverCommand
        {
            get => _mouseHoverCommand;
            set
            {
                _mouseHoverCommand = value;
                RaisePropertyChanged();
            }

        }

        public WriteableBitmap BitmapSource
        {
            get => _bitmapSource;
            set
            {
                _bitmapSource = value;
                RaisePropertyChanged();
            }

        }

        public void LoadImage(Image image)
        {

            CopyImage(image);
            UpdateBitmap();
        }

        public DipolImagePresenter()
        {
            InitializeCommands();
            SetSamplerGeometry();

            var props = typeof(DipolImagePresenter)
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(pi => pi.CanRead)
                        .ToList();
            PropertyChanged += (sender, e) =>
            {
                var val = props
                          .FirstOrDefault(pi => pi.Name == e.PropertyName)
                          ?.GetValue(this);
                Console.WriteLine($@"{e.PropertyName}: " +
                                  $@"{val}");
            };

            _thumbValueChangedTimer.Tick += OnThumbValueChangedTimer_Tick;
        }

        private void CopyImage(Image image)
        {
            switch (image.UnderlyingType)
            {
                case TypeCode.UInt16:
                    _sourceImage = image.CastTo<ushort, float>(x => x);
                    _displayedImage = _sourceImage.Copy();
                    break;
                default:
                    throw new Exception();
            }

            _displayedImage.Scale(0, 1);
        }

        private void UpdateBitmap()
        {
            if (_displayedImage == null)
                return;

            if (_bitmapSource == null ||
                Helper.ExecuteOnUI(() => _bitmapSource.PixelWidth != _sourceImage.Width) ||
                Helper.ExecuteOnUI(() =>_bitmapSource.PixelHeight != _sourceImage.Height))
            {

                _bitmapSource = Helper.ExecuteOnUI(() => new WriteableBitmap(_sourceImage.Width,
                    _sourceImage.Height,
                    96, 96, PixelFormats.Gray32Float, null));

            }

            var temp = _displayedImage.Copy();
            temp.Clamp(LeftScale, RightScale);
            temp.Scale(0, 1);

            var bytes = temp.GetBytes();

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

        private void InitializeCommands()
        {
            ThumbValueChangedCommand = new DelegateCommand(
                ThumbValueChangedCommandExecute,
                DelegateCommand.CanExecuteAlways);

            MouseHoverCommand = new DelegateCommand(
                MouseHoverCommandExecute,
                DelegateCommand.CanExecuteAlways);
        }

        private void SetSamplerGeometry()
        {
            _samplerGeometryRules.Clear();
            _samplerGeometryRules.Add(Tuple.Create<Point, Action<StreamGeometryContext, Point>>(
                new Point(0, 0),
                (cont, pt) => cont.LineTo(pt, true, false)
            ));
            _samplerGeometryRules.Add(Tuple.Create<Point, Action<StreamGeometryContext, Point>>(
                new Point(100, 0),
                (cont, pt) => cont.LineTo(pt, true, false)
            ));
            _samplerGeometryRules.Add(Tuple.Create<Point, Action<StreamGeometryContext, Point>>(
                new Point(100, 100),
                (cont, pt) => cont.LineTo(pt, true, false)
            ));
            _samplerGeometryRules.Add(Tuple.Create<Point, Action<StreamGeometryContext, Point>>(
                new Point(0, 100),
                (cont, pt) => cont.LineTo(pt, true, false)
            ));

            RaisePropertyChanged(nameof(SamplerGeometryRules));
        }


        private void OnThumbValueChangedTimer_Tick(object sender, object e)
        {
           _thumbValueChangedTimer.Stop();
            UpdateBitmap();
        }

        private void MouseHoverCommandExecute(object parameter)
        {
            if (parameter is CommandEventArgs<MouseEventArgs> e &&
                e.Sender is FrameworkElement)
            {
                var pos = e.EventArgs.GetPosition((FrameworkElement) e.Sender);
                Console.WriteLine(pos);
                MousePos = pos;
                RaisePropertyChanged(nameof(MousePos));
            }
        }

        private void ThumbValueChangedCommandExecute(object parameter)
        {
            if (parameter is
                CommandEventArgs<RoutedPropertyChangedEventArgs<double>> evnt)
            {
                _thumbValueChangedTimer.Start();
            }
        }


    }
}
