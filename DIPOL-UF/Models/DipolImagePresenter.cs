using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Schema;
using DIPOL_UF.Commands;
using Image = DipolImage.Image;

namespace DIPOL_UF.Models
{
    public class DipolImagePresenter : ObservableObject
    {
        private enum GeometryLayer
        {
            Aperture,
            Gap,
            Annulus
        }

        private static List<Func<double, double, GeometryDescriptor>>  AvailableGeometries { get; }
        public static List<string> GeometriesAliases { get; }

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
        private DelegateCommand _imageDoubleClickCommand;
        private Point _samplerCenterPos;
        private bool _isMouseOverImage;
        private bool _isMouseOverUIControl;
        private bool _isSamplerFixed;
        private Size _lastKnownImageControlSize;
        private int _selectedGeometryIndex = 1;
        private double _imageSamplerScaleFactor = 1.0;
        private double _imageAnnulus = 25;
        private double _imageApertureSize = 30;
        private double _imageGap = 15;
        private double _imageSamplerThickness = 5.0;
        private Brush _samplerColor;
        private GeometryDescriptor _samplerGeometry;
        private GeometryDescriptor _apertureGeometry;
        private GeometryDescriptor _gapGeometry;
        private double _pixValue = 0;
        private double _maxApertureWidth = 100;
        private double _maxGapWidth = 100;
        private double _maxAnnulusWidth = 100;

        private double LeftScale => (_thumbLeft - _imgScaleMin) / (_imgScaleMax - _imgScaleMin);
        private double RightScale => (_thumbRight - _imgScaleMin) / (_imgScaleMax - _imgScaleMin);
      
        public Image DisplayedImage
        {
            get => _displayedImage;
            set
            {
                _displayedImage = value;
                RaisePropertyChanged();
            }
        }

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

        public Size LastKnownImageControlSize
        {
            get => _lastKnownImageControlSize;
            set
            {
                if (!_lastKnownImageControlSize.Equals(value))
                {
                    _lastKnownImageControlSize = value;
                    RaisePropertyChanged();
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
                    RaisePropertyChanged(nameof(SamplerCenterPosInPix));
                }
            }
        }
        public Point SamplerCenterPosInPix => GetPixelScale(SamplerCenterPos);
        public GeometryDescriptor SamplerGeometry
        {
            get => _samplerGeometry;
            set
            {
                _samplerGeometry = value;
                RaisePropertyChanged();
            }
        }
        public GeometryDescriptor ApertureGeometry
        {
            get => _apertureGeometry;
            set
            {
                _apertureGeometry = value;
                RaisePropertyChanged();
            }
        }
        public GeometryDescriptor GapGeometry
        {
            get => _gapGeometry;
            set
            {
                _gapGeometry = value;
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
        public double ImageApertureSize
        {
            get => _imageApertureSize;
            set
            {
                if (Math.Abs(value - _imageApertureSize) > double.Epsilon)
                {
                    _imageApertureSize = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(ImageGapSize));
                    RaisePropertyChanged(nameof(ImageSamplerSize));
                }
            }
        }
        public double ImageGapSize => _imageApertureSize + _imageGap;
        public double ImageSamplerSize => _imageApertureSize + _imageGap + _imageAnnulus;
        public double ImageGap
        {
            get => _imageGap;
            set
            {
                if (Math.Abs(value - _imageGap) > double.Epsilon)
                {
                    _imageGap = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(ImageGapSize));
                    RaisePropertyChanged(nameof(ImageSamplerSize));
                }
            }
        }
        public double ImageAnnulus
        {
            get => _imageAnnulus;
            set
            {
                if (Math.Abs(value - _imageAnnulus) > double.Epsilon)
                {
                    _imageAnnulus = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(ImageSamplerSize));
                }
            }

        }
        public double PixValue
        {
            get => _pixValue;
            set
            {
                if (Math.Abs(value - _pixValue) > double.Epsilon)
                {
                    _pixValue = value;
                    RaisePropertyChanged();
                }
            }
        }
        public double MinGeometryWidth => 5;
        public double MaxApertureWidth
        {
            get => _maxApertureWidth;
            set
            {
                if (Math.Abs(value - _maxApertureWidth) > double.Epsilon)
                {
                    _maxApertureWidth = value;
                    RaisePropertyChanged();
                }

            }
        }
        public double MaxGapWidth
        {
            get => _maxGapWidth;
            set
            {
                if (Math.Abs(value - _maxGapWidth) > double.Epsilon)
                {
                    _maxGapWidth = value;
                    RaisePropertyChanged();
                }

            }
        }
        public double MaxAnnulusWidth
        {
            get => _maxAnnulusWidth;
            set
            {
                if (Math.Abs(value - _maxAnnulusWidth) > double.Epsilon)
                {
                    _maxAnnulusWidth = value;
                    RaisePropertyChanged();
                }

            }
        }
        public double GeometrySliderTickFrequency => 5;
        public double GeometryThicknessSliderTickFrequency => 1;
        public double MinGeometryThickness => 1;
        public double MaxGeometryThickness => 10;

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
        public bool IsMouseOverUIControl
        {
            get => _isMouseOverUIControl;
            set
            {
                if (value != _isMouseOverUIControl)
                {
                    _isMouseOverUIControl = value;
                    RaisePropertyChanged();
                }
            }
        }
        public bool IsSamplerFixed
        {
            get => _isSamplerFixed;
            set
            {
                if (value != _isSamplerFixed)
                {
                    _isSamplerFixed = value;
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
        public DelegateCommand ImageDoubleClickCommand
        {
            get => _imageDoubleClickCommand;
            set
            {
                _imageDoubleClickCommand = value;
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
            InitializeSamplerGeometry();

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

            _thumbValueChangedTimer.Tick += OnThumbValueChangedTimer_TickAsync;
            _imageSamplerTimer.Tick += OnImageSamplerTimer_TickAsync;
        }
        static DipolImagePresenter()
        {
            (AvailableGeometries, GeometriesAliases) = InitializeAvailableGeometries();
        }

        public void LoadImage(Image image)
        {
            CopyImageAsync(image)
                .ContinueWith((t) => UpdateBitmapAsync())
                .ContinueWith((t) => UpdateGeometrySizeRanges());

        }
        public async Task LoadImageAsync(Image image)
        {
            await CopyImageAsync(image);
            await UpdateBitmapAsync();
            UpdateGeometrySizeRanges();
        }

        public Dictionary<string, double> ImageStats
        {
            get;
        } = new Dictionary<string, double>
        {
            {"Median", 0},
            {"Minimum", 0},
            {"Maximum", 0},
            {"ApertureAvg", 0},
            {"ApertureSd", 0},
            {"AnnulusAvg", 0},
            {"AnnulusSd", 0},
            {"SNR", 0},
            {"Intensity", 0}
        };

        private async Task CopyImageAsync(Image image)
        {
            switch (image.UnderlyingType)
            {
                case TypeCode.UInt16:
                    await Task.Run(() =>
                    {
                         _sourceImage = image.CastTo<ushort, float>(x => x);
                        DisplayedImage = _sourceImage.Copy();
                    });
                    break;
                case TypeCode.Single:
                    await Task.Run(() =>
                    {
                        _sourceImage = image.Copy();
                        DisplayedImage = _sourceImage.Copy();
                    });
                    break;
                default:
                    throw new Exception();
            }

            DisplayedImage.Scale(0, 1);
            SamplerCenterPos = new Point(DisplayedImage.Width/2.0, DisplayedImage.Height/2.0);
        }
        private async Task UpdateBitmapAsync()
        {
            if (DisplayedImage == null)
                return;

            if (_bitmapSource == null ||
                Helper.ExecuteOnUI(() => _bitmapSource.PixelWidth != _sourceImage.Width) ||
                Helper.ExecuteOnUI(() => _bitmapSource.PixelHeight != _sourceImage.Height))
            {

                _bitmapSource = Helper.ExecuteOnUI(() => new WriteableBitmap(_sourceImage.Width,
                    _sourceImage.Height,
                    96, 96, PixelFormats.Gray32Float, null));

            }

          
            var bytes = await Task.Run(() =>
            {
                var temp = DisplayedImage.Copy();
                temp.Clamp(LeftScale, RightScale);
                temp.Scale(0, 1);

                return temp.GetBytes();
            });

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

            ImageDoubleClickCommand = new DelegateCommand(
                ImageDoubleClickCommandExecute,
                DelegateCommand.CanExecuteAlways);
        }
        private void InitializeSamplerGeometry()
        {
            ApertureGeometry = AvailableGeometries[0](50, 1);
            GapGeometry = AvailableGeometries[0](50, 1);
            SamplerGeometry = AvailableGeometries[0](50,1);
            SamplerCenterPos = new Point(30, 30);
            LastKnownImageControlSize = Size.Empty;
        }
        private async Task CalculateStatisticsAsync()
        {
            if (!IsMouseOverImage)
                return;

            var annAvg = 0.0;
            var apAvg = 0.0;
            var min = 0.0;
            var max = 0.0;
            var med = 0.0;
            var annSd = 0.0;
            var apSd = 0.0;
            var snr = 1.0;
            var intens = 0.0;
            var n = 1;

            await Task.Run(async () =>
            {

                
                var aperture = GetPixelsInArea(GeometryLayer.Aperture);

                var annulus = GetPixelsInArea(GeometryLayer.Annulus);

             
                // DEBUG!
                //var newImg = new Image(new ushort[_sourceImage.Width * _sourceImage.Height],
                //    _sourceImage.Width, _sourceImage.Height);
                //foreach (var p in aperture)
                //    newImg.Set<ushort>(100, p.Y, p.X);
                //foreach (var p in annulus)
                //    newImg.Set<ushort>(10, p.Y, p.X);
                //await LoadImageAsync(newImg);
                // END DEBUG!

                var apData = aperture.Select(pix => _sourceImage.Get<float>(pix.Y, pix.X))
                                 .OrderBy(x => x)
                                 .ToList();

                var annData = annulus.Select(pix => _sourceImage.Get<float>(pix.Y, pix.X))
                                     .ToList();

                if (annData.Count > 0)
                {
                    annAvg = annData.Average();
                    annSd = Math.Sqrt(annData.Select(x => Math.Pow(x - annAvg, 2)).Sum() / annData.Count);
                }
                if (apData.Count > 0)
                {
                    apAvg = apData.Average();
                    min = apData.First();
                    max = apData.Last();
                    med = apData[apData.Count / 2];
                    var posPixels = apData.Where(x => x > annAvg).ToList();
                    n = posPixels.Count;
                    intens = posPixels.Sum() - n * annAvg;
                    apSd = Math.Sqrt(apData.Select(x => Math.Pow(x - apAvg, 2)).Sum() / apData.Count);
                }

                if (intens > 0 && n > 0 && annSd > 0)
                    snr = intens / annSd / Math.Sqrt(n);
                
            });

            ImageStats["ApertureAvg"] = apAvg;
            ImageStats["Minimum"] = min;
            ImageStats["Maximum"] = max;
            ImageStats["Median"] = med;
            ImageStats["Intensity"] = intens;
            ImageStats["ApertureSd"] = apSd;
            ImageStats["SNR"] = snr;
            ImageStats["AnnulusAvg"] = annAvg;
            ImageStats["AnnulusSd"] = annSd;

            RaisePropertyChanged(nameof(ImageStats));

        }
        private async Task UpdateGeometryAsync()
        {
            ApertureGeometry = AvailableGeometries[SelectedGeometryIndex](
                ImageSamplerScaleFactor * ImageApertureSize,
                ImageSamplerThickness * ImageSamplerScaleFactor);

            GapGeometry = AvailableGeometries[SelectedGeometryIndex](
                ImageSamplerScaleFactor * ImageGapSize,
                ImageSamplerThickness * ImageSamplerScaleFactor);

            SamplerGeometry = AvailableGeometries[SelectedGeometryIndex](
                ImageSamplerScaleFactor * ImageSamplerSize,
                ImageSamplerThickness * ImageSamplerScaleFactor);

            if (!LastKnownImageControlSize.IsEmpty)
                SamplerCenterPos = new Point(
                    SamplerCenterPos.X.Clamp(
                        SamplerGeometry.HalfSize.Width,
                        LastKnownImageControlSize.Width - SamplerGeometry.HalfSize.Width),
                    SamplerCenterPos.Y.Clamp(
                        SamplerGeometry.HalfSize.Height,
                        LastKnownImageControlSize.Height - SamplerGeometry.HalfSize.Height)
                );
            else
                SamplerCenterPos = DisplayedImage != null
                    ? new Point(DisplayedImage.Width / 2.0, DisplayedImage.Height / 2.0)
                    : new Point(0, 0);

            await CalculateStatisticsAsync();
        }

        private void UpdateGeometrySizeRanges()
        {
            if (_displayedImage == null)
                return;

            var size = Math.Min(_displayedImage.Width, _displayedImage.Height) - 3 * MaxGeometryThickness;
            var sizeFr = Math.Floor(size / 5.0);

            MaxApertureWidth = 2 * sizeFr;
            MaxGapWidth = sizeFr;
            MaxAnnulusWidth = 2 * sizeFr;
        }

        // Presenter to pixel scale transformations
        private double GetPixelScale(double x, bool horizontal = false)
            => x * (DisplayedImage != null && !LastKnownImageControlSize.IsEmpty
                   ? (horizontal
                       ? (DisplayedImage.Width / LastKnownImageControlSize.Width)
                       : (DisplayedImage.Height / LastKnownImageControlSize.Height))
                   : 1.0);
        private Point GetPixelScale(Point p)
            => DisplayedImage != null
                ? new Point(
                    GetPixelScale(p.X, true),
                    GetPixelScale(p.Y))
                : p;
        private Size GetPixelScale(Size s)
            => DisplayedImage != null
                ? new Size(
                    GetPixelScale(s.Width, true),
                    GetPixelScale(s.Height))
                : s;

        private List<(int X, int Y)> GetPixelsInArea(GeometryLayer layer)
        {
            if (!LastKnownImageControlSize.IsEmpty && DisplayedImage != null)
            {
               
                var centerPix = GetPixelScale(SamplerCenterPos);
                var halfSizePix = GetPixelScale(SamplerGeometry.HalfSize);
                var gapHalfSizePix = GetPixelScale(GapGeometry.HalfSize);
                var apertureHalfSizePix = GetPixelScale(ApertureGeometry.HalfSize);
                var thcknssPix = 0.5 *( GetPixelScale(SamplerGeometry.Thickness) + GetPixelScale(SamplerGeometry.Thickness, true));

                var pixelXLims = (Min: Convert.ToInt32(Math.Max(centerPix.X - halfSizePix.Width, 0)),
                                  Max: Convert.ToInt32(Math.Min(centerPix.X + halfSizePix.Width, DisplayedImage.Width - 1)));

                var pixelYLims = (Min: Convert.ToInt32(Math.Max(centerPix.Y - halfSizePix.Height, 0)),
                                  Max: Convert.ToInt32(Math.Min(centerPix.Y + halfSizePix.Height, DisplayedImage.Height - 1)));

                var aperturePixelXLims = (Min: Convert.ToInt32(Math.Max(centerPix.X - apertureHalfSizePix.Width, 0)),
                                          Max: Convert.ToInt32(Math.Min(centerPix.X + apertureHalfSizePix.Width, 
                                              DisplayedImage.Width - 1)));
                var aperturePixelYLims = (Min: Convert.ToInt32(Math.Max(centerPix.Y - apertureHalfSizePix.Height, 0)),
                                          Max: Convert.ToInt32(Math.Min(centerPix.Y + apertureHalfSizePix.Height,
                                              DisplayedImage.Width - 1)));

                var gapPixelXLims = (Min: Convert.ToInt32(Math.Max(centerPix.X - gapHalfSizePix.Width, 0)),
                                    Max: Convert.ToInt32(Math.Min(centerPix.X + gapHalfSizePix.Width,
                                        DisplayedImage.Width - 1)));
                var gapPixelYLims = (Min: Convert.ToInt32(Math.Max(centerPix.Y - gapHalfSizePix.Height, 0)),
                                     Max: Convert.ToInt32(Math.Min(centerPix.Y + gapHalfSizePix.Height,
                                        DisplayedImage.Width - 1)));

                switch (layer)
                {
                    case GeometryLayer.Annulus:
                        var annulusPixels =
                            new List<(int X, int Y)>(
                                (pixelXLims.Max - pixelXLims.Min) *
                                (pixelYLims.Max - pixelYLims.Min) / 8);

                        for (var xPix = pixelXLims.Min; xPix <= pixelXLims.Max; xPix++)
                            for (var yPix = pixelYLims.Min; yPix <= pixelYLims.Max; yPix++)
                            {
                                if ((SamplerGeometry?.IsInsideChecker(xPix, yPix, centerPix, halfSizePix,
                                        thcknssPix) ?? false) &&
                                    !(GapGeometry?.IsInsideChecker(xPix, yPix, centerPix, gapHalfSizePix,
                                        thcknssPix) ?? true))
                                    annulusPixels.Add((X: xPix, Y: yPix));
                            }

                        return annulusPixels;

                    case GeometryLayer.Gap:
                        var gapPixels =
                            new List<(int X, int Y)>(
                                (gapPixelXLims.Max - gapPixelXLims.Min) *
                                (gapPixelYLims.Max - gapPixelYLims.Min) / 8);

                        for (var xPix = gapPixelXLims.Min; xPix <= gapPixelXLims.Max; xPix++)
                            for (var yPix = gapPixelYLims.Min; yPix <= gapPixelYLims.Max; yPix++)
                            {
                                if ((GapGeometry?.IsInsideChecker(xPix, yPix, centerPix, halfSizePix,
                                         thcknssPix) ?? false) &&
                                    !(ApertureGeometry?.IsInsideChecker(xPix, yPix, centerPix, gapHalfSizePix,
                                          thcknssPix) ?? true))
                                    gapPixels.Add((X: xPix, Y: yPix));
                            }

                        return gapPixels;

                    case GeometryLayer.Aperture:
                        var aperturePixels =
                            new List<(int X, int Y)>(
                                (aperturePixelXLims.Max - aperturePixelXLims.Min) *
                                (aperturePixelYLims.Max - aperturePixelYLims.Min) / 8);

                        for (var xPix = aperturePixelXLims.Min; xPix <= aperturePixelXLims.Max; xPix++)
                            for (var yPix = aperturePixelYLims.Min; yPix <= aperturePixelYLims.Max; yPix++)
                            {
                                if (ApertureGeometry?.IsInsideChecker(xPix, yPix, centerPix, 
                                        apertureHalfSizePix, thcknssPix) ?? false)
                                    aperturePixels.Add((X: xPix, Y: yPix));
                            }

                        return aperturePixels;
                }
            }

            return new List<(int X, int Y)> {(0, 0)};
        }

        private async void OnThumbValueChangedTimer_TickAsync(object sender, object e)
        {
            _thumbValueChangedTimer.Stop();
            await UpdateBitmapAsync();
        }
        private async void OnImageSamplerTimer_TickAsync(object sender, object e)
        {
            await CalculateStatisticsAsync();
            _imageSamplerTimer.Stop();
        }

        private void MouseHoverCommandExecute(object parameter)
        {
            if (DisplayedImage == null ||
                IsSamplerFixed)
                return;
            if (parameter is CommandEventArgs<MouseEventArgs> eUI &&
                eUI.Sender is UserControl)
            {
                if (eUI.EventArgs.RoutedEvent.Name == nameof(UserControl.MouseEnter))
                    IsMouseOverUIControl = true;
                else if (eUI.EventArgs.RoutedEvent.Name == nameof(UserControl.MouseLeave))
                    IsMouseOverUIControl = false;
            }
            else if (parameter is CommandEventArgs<MouseEventArgs> e &&
                     e.Sender is FrameworkElement elem)
            {

                if (e.EventArgs.RoutedEvent.Name == nameof(FrameworkElement.MouseEnter))
                    IsMouseOverImage = true;
                if (e.EventArgs.RoutedEvent.Name == nameof(FrameworkElement.MouseLeave))
                {
                    IsMouseOverImage = false;
                    return;
                }

                if (e.EventArgs.RoutedEvent.Name == nameof(FrameworkElement.MouseMove) &&
                    !IsMouseOverImage)
                {
                    IsMouseOverImage = true;
                }

                if (!IsMouseOverUIControl)
                    IsMouseOverUIControl = true;

                var pos = e.EventArgs.GetPosition(elem);
                var posX = pos.X.Clamp(
                    SamplerGeometry.HalfSize.Width,
                    elem.ActualWidth - SamplerGeometry.HalfSize.Width);
                var posY = pos.Y.Clamp(
                    SamplerGeometry.HalfSize.Height,
                    elem.ActualHeight- SamplerGeometry.HalfSize.Height);
                SamplerCenterPos = new Point(posX, posY);
                LastKnownImageControlSize = new Size(elem.ActualWidth, elem.ActualHeight);
                PixValue = _sourceImage.Get<float>(
                    Convert.ToInt32(posY / elem.ActualHeight * DisplayedImage.Height),
                    Convert.ToInt32(posX / elem.ActualWidth * DisplayedImage.Width));

                if (!_imageSamplerTimer.IsEnabled)
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
            if (parameter is CommandEventArgs<SizeChangedEventArgs> args )
            {
                var oldPixPos = SamplerCenterPosInPix;
                LastKnownImageControlSize = args.EventArgs.NewSize;
                SamplerCenterPos = new Point(
                    oldPixPos.X * 
                        args.EventArgs.NewSize.Width / DisplayedImage.Width,
                    oldPixPos.Y * 
                        args.EventArgs.NewSize.Height / DisplayedImage.Height);
                ImageSamplerScaleFactor = Math.Min(args.EventArgs.NewSize.Width/DisplayedImage.Width, 
                                              args.EventArgs.NewSize.Height/DisplayedImage.Height);
            }
        }
        private void ImageDoubleClickCommandExecute(object parameter)
        {
            if (parameter is CommandEventArgs<MouseButtonEventArgs> args &&
                args.EventArgs.LeftButton == MouseButtonState.Pressed &&
                args.EventArgs.ClickCount == 2)
                IsSamplerFixed = !IsSamplerFixed;
        }

        protected override async void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);

            if (e.PropertyName == nameof(BitmapSource))
            {
                IsMouseOverImage = false;
                var keys = ImageStats.Keys.ToList();
                foreach (var key in keys)
                    ImageStats[key] = 0.0;

                RaisePropertyChanged(nameof(ImageStats));
                await UpdateGeometryAsync();
            }


            if (e.PropertyName == nameof(ImageSamplerScaleFactor) ||
                e.PropertyName == nameof(SelectedGeometryIndex) ||
                e.PropertyName == nameof(ImageSamplerThickness) ||
                e.PropertyName == nameof(ImageSamplerSize))
                await UpdateGeometryAsync();
                
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

                bool PixSelector(int x, int y, Point center, Size halfSize, double thcknss)
                    => Math.Abs(x - center.X) <= (halfSize.Width - thcknss) &&
                       Math.Abs(y - center.Y) <= (halfSize.Height - thcknss);

                return new GeometryDescriptor(
                    new Point(size / 2, size / 2), 
                    new Size(size, size), path, thickness,
                    PixSelector);
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


                bool PixSelector(int x, int y, Point center, Size halfSize, double thcknss)
                    => Math.Pow(x - center.X, 2) + Math.Pow(y - center.Y, 2) <=
                       Math.Pow(0.5 * (halfSize.Width + halfSize.Height) - thcknss, 2);
                

                return new GeometryDescriptor(
                    new Point(size / 2, size / 2), 
                    new Size(size, size), path, thickness, 
                    PixSelector);
            }

            return (new List<Func<double, double, GeometryDescriptor>> {CommonRectangle, CommonCircle}, 
                new List<string> {@"Rectangle", @"Circle"});

        }

        
    }
}
