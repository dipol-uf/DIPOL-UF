using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DipolImage;
using DIPOL_UF.Commands;

namespace DIPOL_UF.Models
{
    public class DipolImagePresenter : ObservableObject
    {
        private static List<Func<double, double, GeometryDescriptor>>  AvailableGeometries { get; }
        public static List<string> GeometriesAliases { get; }

        private Image _sourceImage;
        private Image _displayedImage;
        private WriteableBitmap _bitmapSource;
        private double _imgScaleMax = 1000;
        private double _imgScaleMin;
        private double _thumbLeft;
        private double _thumbRight = 1000;
        private DelegateCommand _thumbValueChangedCommand;
        private DelegateCommand _mouseHoverCommand;
        private DelegateCommand _sizeChangedCommand;
        private Point _samplerCenterPos;
        private bool _isMouseOverImage;
        private Size _lastKnownImageControlSize;
        private int _selectedGeometryIndex = 1;
        private double _imageSamplerScaleFactor = 1.0;
        private double _imageSamplerSize = 200;
        private double _imageSamplerThickness = 4.0;
        private Brush _samplerColor;

        private readonly DispatcherTimer _thumbValueChangedTimer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromMilliseconds(250),
            IsEnabled = false
        };

        private readonly DispatcherTimer _imageSamplerTimer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromMilliseconds(100),
            IsEnabled = false
        };

        private GeometryDescriptor _samplerGeometry;

        private List<double> _imageStats = new List<double>(3)
            {0, 0, 0};
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

        public Point SamplerCenterPos
        {
            get => _samplerCenterPos;
            set
            {
                if (!value.Equals(_samplerCenterPos))
                {
                    _samplerCenterPos = value;
                    RaisePropertyChanged();
                }
            }
        }
        public GeometryDescriptor SamplerGeometry
        {
            get => _samplerGeometry;
            set
            {
                _samplerGeometry = value;
                RaisePropertyChanged();
            }
        }
        public double ImageSamplerScaleFactor
        {
            get => _imageSamplerScaleFactor;
            set
            {
                if (Math.Abs(value - _imageSamplerScaleFactor) > double.Epsilon)
                {
                    _imageSamplerScaleFactor = value;
                    RaisePropertyChanged();
                }

            }
        }
        public int SelectedGeometryIndex
        {
            get => _selectedGeometryIndex;
            set
            {
                if (value != _selectedGeometryIndex)
                {
                    _selectedGeometryIndex = value;
                    RaisePropertyChanged();
                }
            }
        }
        public double ImageSamplerThickness
        {
            get => _imageSamplerThickness;
            set
            {
                if (Math.Abs(value - _imageSamplerThickness) > double.Epsilon)
                {
                    _imageSamplerThickness = value;
                    RaisePropertyChanged();
                }

            }
        }
        public double ImageSamplerSize
        {
            get => _imageSamplerSize;
            set
            {
                if (Math.Abs(value - _imageSamplerSize) > double.Epsilon)
                {
                    _imageSamplerSize = value;
                    RaisePropertyChanged();
                } 
            }

        }
        public Brush SamplerColor
        {
            get => _samplerColor;
            set
            {
                _samplerColor = value;
                RaisePropertyChanged();
            }
        }

        public bool IsMouseOverImage
        {
            get => _isMouseOverImage;

            set
            {
                if (value != _isMouseOverImage)
                {
                    _isMouseOverImage = value;
                    RaisePropertyChanged();
                }
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
        public DelegateCommand SizeChangedCommand
        {
            get => _sizeChangedCommand;
            set
            {
                _sizeChangedCommand = value;
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


        public DipolImagePresenter()
        {
            InitializeCommands();
            SetSamplerGeometry();

            var props = typeof(DipolImagePresenter)
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(pi => pi.CanRead)
                        .ToList();
            PropertyChanged += (sender, e) => Helper.ExecuteOnUI(() =>
            {
                var val = props
                          .FirstOrDefault(pi => pi.Name == e.PropertyName)
                          ?.GetValue(this);
                Console.WriteLine($@"{e.PropertyName}: " +
                                  $@"{val}");
            });

            _thumbValueChangedTimer.Tick += OnThumbValueChangedTimer_Tick;
            _imageSamplerTimer.Tick += OnImageSamplerTimer_Tick;
        }

        static DipolImagePresenter()
        {
            (AvailableGeometries, GeometriesAliases) = InitializeAvailableGeometries();
        }

        public void LoadImage(Image image)
        {

            CopyImage(image);
            UpdateBitmap();
        }

        public List<double> ImageStats
        {
            get => _imageStats;
            set
            {
                if (!value.Equals(_imageStats))
                {
                    _imageStats = value;
                    RaisePropertyChanged();
                }
            }

        }

        private void CopyImage(Image image)
        {
            switch (image.UnderlyingType)
            {
                case TypeCode.UInt16:
                    _sourceImage = image.CastTo<ushort, float>(x => x);
                    _displayedImage = _sourceImage.Copy();
                    break;
                case TypeCode.Single:
                    _sourceImage = image.Copy();
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
                Helper.ExecuteOnUI(() => _bitmapSource.PixelHeight != _sourceImage.Height))
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
            finally
            {
                Helper.ExecuteOnUI(() =>
                    _bitmapSource.AddDirtyRect(new Int32Rect(0, 0, _sourceImage.Width, _sourceImage.Height)));
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

            SizeChangedCommand = new DelegateCommand(
                SizeChangedCommandExecute,
                DelegateCommand.CanExecuteAlways);
            
        }

        private void SetSamplerGeometry()
        {
            SamplerGeometry = AvailableGeometries[1](100, 3);
            SamplerCenterPos = SamplerGeometry.Center;
        }

        private void CalculateStatistics()
        {
            //if (!IsMouseOverImage)
            //    return;
            Task.Run(() =>
            {

                var pixels = SamplerGeometry.PixelsInsideGeometry(SamplerCenterPos,
                    _displayedImage.Width-1, _displayedImage.Height-1,
                    _lastKnownImageControlSize.Width, _lastKnownImageControlSize.Height);

                // DEBUG!
                var newImg = new Image(new ushort[_sourceImage.Width * _sourceImage.Height],
                    _sourceImage.Width, _sourceImage.Height);
                foreach (var p in pixels)
                    newImg.Set<ushort>(100, p.Y, p.X);
                LoadImage(newImg);
                // END DEBUG!

                var data = pixels.Select(pix => _sourceImage.Get<float>(pix.Y, pix.X)).ToList();

                double avg = data.Average();
                double min = data.Min();
                double max = data.Max();


                Helper.ExecuteOnUI(() =>
                {
                    _imageStats[0] = avg;
                    _imageStats[1] = min;
                    _imageStats[2] = max;

                });

                RaisePropertyChanged(nameof(ImageStats));
            });

        }

        private void UpdateGeometry()
        {
            SamplerGeometry = AvailableGeometries[SelectedGeometryIndex](
                ImageSamplerScaleFactor * ImageSamplerSize, ImageSamplerThickness * ImageSamplerScaleFactor);

            SamplerCenterPos = new Point(
                SamplerCenterPos.X.Clamp(
                    SamplerGeometry.HalfSize.Width, 
                    _lastKnownImageControlSize.Width - SamplerGeometry.HalfSize.Width),
                SamplerCenterPos.Y.Clamp(
                    SamplerGeometry.HalfSize.Height, 
                    _lastKnownImageControlSize.Height - SamplerGeometry.HalfSize.Height)
                );
        }

        private void OnThumbValueChangedTimer_Tick(object sender, object e)
        {
            _thumbValueChangedTimer.Stop();
            UpdateBitmap();
        }
        private void OnImageSamplerTimer_Tick(object sender, object e)
        {
            CalculateStatistics();
            _imageSamplerTimer.Stop();
        }

        private void MouseHoverCommandExecute(object parameter)
        {
            if (_displayedImage != null && 
                parameter is CommandEventArgs<MouseEventArgs> e &&
                e.Sender is FrameworkElement elem)
            {

                if (e.EventArgs.RoutedEvent.Name == @"MouseEnter")
                    IsMouseOverImage = true;
                if (e.EventArgs.RoutedEvent.Name == @"MouseLeave")
                {
                    IsMouseOverImage = false;
                    return;
                }

                if (e.EventArgs.RoutedEvent.Name == @"MouseMove" && !IsMouseOverImage)
                    IsMouseOverImage = true;

                var pos = e.EventArgs.GetPosition(elem);
                var posX = pos.X.Clamp(
                    SamplerGeometry.HalfSize.Width,
                    elem.ActualWidth - SamplerGeometry.HalfSize.Width);
                var posY = pos.Y.Clamp(
                    SamplerGeometry.HalfSize.Height,
                    elem.ActualHeight- SamplerGeometry.HalfSize.Height);
                SamplerCenterPos = new Point(posX, posY);
                _lastKnownImageControlSize = new Size(elem.ActualWidth, elem.ActualHeight);
                //RaisePropertyChanged(nameof(SamplerCenterPos));
                if(!_imageSamplerTimer.IsEnabled)
                    _imageSamplerTimer.Start();
            }
        }
        private void ThumbValueChangedCommandExecute(object parameter)
        {
            if (parameter is
                CommandEventArgs<RoutedPropertyChangedEventArgs<double>>)
            {
                _thumbValueChangedTimer.Start();
            }
        }
        private void SizeChangedCommandExecute(object parameter)
        {
            if (parameter is CommandEventArgs<SizeChangedEventArgs> args)
            {
                ImageSamplerScaleFactor = Math.Min(args.EventArgs.NewSize.Width, args.EventArgs.NewSize.Height) / 1000.0;
            }
        }

        protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);

            if (e.PropertyName == nameof(BitmapSource))
            {
                IsMouseOverImage = false;
                _imageStats[0] = 0;
                _imageStats[0] = 0;
                _imageStats[0] = 0;
                RaisePropertyChanged(nameof(ImageStats));
            }


            if (e.PropertyName == nameof(ImageSamplerScaleFactor) ||
                e.PropertyName == nameof(SelectedGeometryIndex) ||
                e.PropertyName == nameof(ImageSamplerThickness) ||
                e.PropertyName == nameof(ImageSamplerSize))
                UpdateGeometry();
                

            //if(e.PropertyName == nameof(SelectedGeometryIndex))
            //    SamplerGeometry = AvailableGeometries[SelectedGeometryIndex](
            //        ImageSamplerScaleFactor * _imageSamplerSize, _imageSamplerThickness * ImageSamplerScaleFactor);
        }

        private static (List<Func<double, double, GeometryDescriptor>>, List<string>) InitializeAvailableGeometries()
        {
            GeometryDescriptor CommonRectangle(double size, double thickness)
            {
                var path = new List<Tuple<Point, Action<StreamGeometryContext, Point>>>(4)
                {
                    Tuple.Create<Point, Action<StreamGeometryContext, Point>>(
                        new Point(0, 0), null),
                    Tuple.Create<Point, Action<StreamGeometryContext, Point>>(
                        new Point(size, 0), (cont, pt) => cont.LineTo(pt, true, false)),
                    Tuple.Create<Point, Action<StreamGeometryContext, Point>>(
                        new Point(size, size), (cont, pt) => cont.LineTo(pt, true, false)),
                    Tuple.Create<Point, Action<StreamGeometryContext, Point>>(
                        new Point(0, size), (cont, pt) => cont.LineTo(pt, true, false)),
                    Tuple.Create<Point, Action<StreamGeometryContext, Point>>(
                        new Point(0, 0), (cont, pt) => cont.LineTo(pt, true, false))
                };

                return new GeometryDescriptor(
                    new Point(size / 2, size / 2), 
                    new Size(size, size), path, thickness, 
                    (i, j, p) => true);
            }

            GeometryDescriptor CommonCircle(double size, double thickness)
            {
                var path = new List<Tuple<Point, Action<StreamGeometryContext, Point>>>(4)
                {
                    Tuple.Create<Point, Action<StreamGeometryContext, Point>>(
                        new Point(size/2, 0), null),
                    Tuple.Create<Point, Action<StreamGeometryContext, Point>>(
                       new Point(size, size/2), (cont, pt) 
                           => cont.ArcTo(pt, new Size(size/2, size/2), 
                               90, false, SweepDirection.Clockwise, true, false)),
                    Tuple.Create<Point, Action<StreamGeometryContext, Point>>(
                        new Point(size/2, size), (cont, pt) 
                            => cont.ArcTo(pt, new Size(size/2, size/2), 
                                90, false, SweepDirection.Clockwise, true, false)),
                    Tuple.Create<Point, Action<StreamGeometryContext, Point>>(
                        new Point(0, size/2), (cont, pt) 
                            => cont.ArcTo(pt, new Size(size/2, size/2), 
                                90, false, SweepDirection.Clockwise, true, false)),
                    Tuple.Create<Point, Action<StreamGeometryContext, Point>>(
                        new Point(size/2, 0), (cont, pt) 
                            => cont.ArcTo(pt, new Size(size/2, size/2),
                                90, false, SweepDirection.Clockwise, true, false))
                };

                bool PixSelector(double px, double py, GeometryDescriptor desc)
                    => (Math.Pow(px - desc.Thickness, 2) + Math.Pow(py - desc.Thickness, 2)) <= 
                           Math.Pow(0.5 * (desc.HalfSize.Width + desc.HalfSize.Height) - desc.Thickness, 2);

                return new GeometryDescriptor(
                    new Point(size / 2, size / 2), 
                    new Size(size, size), path, thickness, 
                    PixSelector);
            }

            return (new List<Func<double, double, GeometryDescriptor>>(3) {CommonRectangle, CommonCircle}, 
                new List<string>() {@"Rectangle", @"Circle"});

        }
    }
}
