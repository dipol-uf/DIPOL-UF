#nullable enable
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DIPOL_UF.Converters;
using DipolImage;
using Image = DipolImage.Image;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace DIPOL_UF.Models
{
    public class DipolImagePresenter : ReactiveObjectEx
    {
        private enum GeometryLayer
        {
            Aperture,
            Gap,
            Annulus
        }

        public class ImageStatsCollection
        {
            public double Median { get; set; }
            public double Minimum { get; set; }
            public double Maximum { get; set; }
            public double ApertureAvg { get; set; }
            public double Intensity { get; set; }
            public double ApertureSd { get; set; }
            public double AnnulusAvg { get; set; }
            public double Snr { get; set; }
            public double AnnulusSd { get; set; }
        }

        private static List<Func<double, double, GeometryDescriptor>> AvailableGeometries { get; }
        public static List<string> GeometriesAliases { get; }

        private DeviceSettingsDescriptor? _deviceSettings;

        private Image? _sourceImage;

        private readonly int ThumbDelta = 1;
        public int ThumbScaleMin => 0;
        public int ThumbScaleMax => 1000;
        public double ImageScaleMin => 0.0;
        public double ImageScaleMax => 1.0;
        public double GeometrySliderTickFrequency => 5;
        public double GeometryThicknessSliderTickFrequency => 1;
        public double MinGeometryThickness => 1;
        public double MaxGeometryThickness => 10;
        public double MinGeometryWidth => 5;

        [Reactive]
        public Image? DisplayedImage { get; set; }

        [Reactive]
        public double LeftScale { get; set; }

        [Reactive]
        public double RightScale { get; set; }

        [Reactive]
        public int ThumbLeft { get; set; }

        [Reactive]
        public int ThumbRight { get; set; }

        [Reactive]
        public Size LastKnownImageControlSize { get; set; }

        [Reactive]
        public Point SamplerCenterPosInPix { get; set; }

        [Reactive]
        public GeometryDescriptor? SamplerGeometry { get; set; }

        [Reactive]
        public GeometryDescriptor? ApertureGeometry { get; set; }

        [Reactive]
        public GeometryDescriptor? GapGeometry { get; set; }

        [Reactive]
        public double ImageSamplerScaleFactor { get; set; }

        [Reactive]
        public int SelectedGeometryIndex { get; set; }

        [Reactive]
        public double ImageSamplerThickness { get; set; }

        [Reactive]
        public double ImageApertureSize { get; set; }

        [Reactive]
        public double ImageGap { get; set; }

        [Reactive]
        public double ImageAnnulus { get; set; }

        [Reactive]
        public double MaxApertureWidth { get; set; }

        [Reactive]
        public double MaxGapWidth { get; set; }

        [Reactive]
        public double MaxAnnulusWidth { get; set; }

        [Reactive]
        public bool IsSamplerFixed { get; set; }

        [Reactive]
        public ImageStatsCollection? ImageStats { get; set; }

        [Reactive]
        public bool IsMouseOverImage { get; set; }

        public Point SamplerCenterPos { [ObservableAsProperty] get; }
        public double ImageGapSize { [ObservableAsProperty] get; }
        public double ImageSamplerSize { [ObservableAsProperty] get; }
        public double PixValue { [ObservableAsProperty] get; }

        public ReactiveCommand<Image, Image>? LoadImageCommand { get; private set; }
        public ReactiveCommand<int, int>? LeftThumbChangedCommand { get; private set; }
        public ReactiveCommand<int, int>? RightThumbChangedCommand { get; private set; }
        public ReactiveCommand<(Size Size, Point Pos), (Size Size, Point Pos)>? MouseHoverCommand { get; private set; }
        public ReactiveCommand<SizeChangedEventArgs, SizeChangedEventArgs>? SizeChangedCommand { get; private set; }
        public ReactiveCommand<MouseButtonEventArgs, MouseButtonEventArgs>? ImageClickCommand { get; private set; }
        public ReactiveCommand<Unit, Unit>? UnloadImageCommand { get; private set; }

        public DipolImagePresenter(DeviceSettingsDescriptor? desc = null)
        {
            _deviceSettings = desc;
            ThumbRight = ThumbScaleMax;
            SelectedGeometryIndex = 1;
            ImageSamplerScaleFactor = 1.0;
            MaxApertureWidth = 100;
            MaxGapWidth = 100;
            MaxAnnulusWidth = 100;
            ImageStats = new ImageStatsCollection();


            InitializeCommands();
            InitializeSamplerGeometry();
            HookObservables();

        }

        private void InitializeCommands()
        {
            LeftThumbChangedCommand =
                ReactiveCommand.Create<int, int>(
                                   x =>
                                   {
                                       if (x < ThumbScaleMin)
                                           return ThumbScaleMin;
                                       if (x >= ThumbScaleMax)
                                           return ThumbScaleMax - ThumbDelta;
                                       return x;
                                   })
                               .DisposeWith(Subscriptions);

            RightThumbChangedCommand =
                ReactiveCommand.Create<int, int>(
                                   x =>
                                   {
                                       if (x <= ThumbScaleMin)
                                           return ThumbScaleMin + ThumbDelta;
                                       if (x > ThumbScaleMax)
                                           return ThumbScaleMax;
                                       return x;
                                   })
                               .DisposeWith(Subscriptions);

            LoadImageCommand =
                ReactiveCommand.CreateFromTask<Image, Image>(
                                   async x =>
                                   {
                                       await LoadImageAsync(x);
                                       return DisplayedImage!;
                                   })
                               .DisposeWith(Subscriptions);

            ImageClickCommand =
                ReactiveCommand.Create<MouseButtonEventArgs, MouseButtonEventArgs>(
                                   x => x,
                                   this.WhenPropertyChanged(x => x.DisplayedImage)
                                       .Select(x => !(x.Value is null)))
                               .DisposeWith(Subscriptions);

            MouseHoverCommand =
                ReactiveCommand.Create<(Size Size, Point Pos), (Size Size, Point Pos)>(
                                   x => x,
                                   this.WhenPropertyChanged(x => x.DisplayedImage)
                                       .Select(x => !(x.Value is null)))
                               .DisposeWith(Subscriptions);


            SizeChangedCommand =
                ReactiveCommand.Create<SizeChangedEventArgs, SizeChangedEventArgs>(
                                   x => x,
                                   this.WhenPropertyChanged(x => x.DisplayedImage)
                                       .Select(x => !(x.Value is null)))
                               .DisposeWith(Subscriptions);

            UnloadImageCommand =
                ReactiveCommand.Create<Unit>(
                                   _ => UnloadImageCommandExecute(),
                                   this.WhenPropertyChanged(x => x.DisplayedImage)
                                       .Select(x => !(x.Value is null)))
                               .DisposeWith(Subscriptions);

        }

        private void InitializeSamplerGeometry()
        {
            ApertureGeometry = AvailableGeometries[0](20, 3);
            GapGeometry = AvailableGeometries[0](40, 3);
            SamplerGeometry = AvailableGeometries[0](60, 3);
            SamplerCenterPosInPix = new Point(0, 0);
            LastKnownImageControlSize = Size.Empty;
        }

        private void HookObservables()
        {
            LeftThumbChangedCommand.BindTo(this, x => x.ThumbLeft)
                                   .DisposeWith(Subscriptions);
            RightThumbChangedCommand.BindTo(this, x => x.ThumbRight)
                                    .DisposeWith(Subscriptions);

            LeftThumbChangedCommand!
                .Where(x => x >= ThumbRight)
                .Select(x => x + ThumbDelta)
                .BindTo(this, x => x.ThumbRight)
                .DisposeWith(Subscriptions);
            RightThumbChangedCommand!
                .Where(x => x <= ThumbLeft)
                .Select(x => x - ThumbDelta)
                .BindTo(this, x => x.ThumbLeft)
                .DisposeWith(Subscriptions);

            var leftThumbObs = this.WhenPropertyChanged(x => x.ThumbLeft)
                                   .Select(x => x.Value)
                                   .DistinctUntilChanged();

            var rightThumbObs = this.WhenPropertyChanged(x => x.ThumbRight)
                                    .Select(x => x.Value)
                                    .DistinctUntilChanged();

            var thumbObs = leftThumbObs.CombineLatest(rightThumbObs, (x, y) => (Left: x, Right: y))
                                       .Sample(TimeSpan.Parse(
                                           UiSettingsProvider.Settings.Get("ImageRedrawDelay", "00:00:00.5")));

            thumbObs.Select(x => 1.0 * (x.Left - ThumbScaleMin) /
                                 (ThumbScaleMax - ThumbScaleMin) *
                                 (ImageScaleMax - ImageScaleMin))
                    .BindTo(this, x => x.LeftScale).DisposeWith(Subscriptions);

            thumbObs.Select(x => 1.0 * (x.Right - ThumbScaleMin) /
                                 (ThumbScaleMax - ThumbScaleMin) *
                                 (ImageScaleMax - ImageScaleMin))
                    .BindTo(this, x => x.RightScale).DisposeWith(Subscriptions);

            this.WhenPropertyChanged(x => x.SamplerCenterPosInPix)
                .Select(x => GetImageScale(x.Value))
                .ToPropertyEx(this, x => x.SamplerCenterPos)
                .DisposeWith(Subscriptions);

            this.WhenPropertyChanged(x => DisplayedImage)
                .Subscribe(_ => UpdateGeometrySizeRanges())
                .DisposeWith(Subscriptions);

            ImageClickCommand!
                .Where(x => x.LeftButton == MouseButtonState.Pressed && x.ClickCount == 2)
                .Subscribe(ImageDoubleClickCommandExecute)
                .DisposeWith(Subscriptions);


            MouseHoverCommand!
                .Where(x => !IsSamplerFixed)
                .Subscribe(x => UpdateSamplerPosition(x.Size, x.Pos))
                .DisposeWith(Subscriptions);


            // Geometry updates
            this.WhenAnyPropertyChanged(nameof(ImageApertureSize), nameof(ImageGap))
                .Select(x => x.ImageGap + x.ImageApertureSize)
                .ToPropertyEx(this, x => x.ImageGapSize)
                .DisposeWith(Subscriptions);

            this.WhenAnyPropertyChanged(nameof(ImageApertureSize), nameof(ImageGap), nameof(ImageAnnulus))
                .Select(x => x.ImageGap + x.ImageApertureSize + x.ImageAnnulus)
                .ToPropertyEx(this, x => x.ImageSamplerSize)
                .DisposeWith(Subscriptions);

            this.WhenAnyPropertyChanged(
                    nameof(SelectedGeometryIndex),
                    nameof(ImageSamplerScaleFactor),
                    nameof(ImageApertureSize),
                    nameof(ImageSamplerThickness))
                .Select(x => AvailableGeometries[x.SelectedGeometryIndex](
                    x.ImageSamplerScaleFactor * x.ImageApertureSize,
                    x.ImageSamplerScaleFactor * x.ImageSamplerThickness))
                .BindTo(this, x => x.ApertureGeometry)
                .DisposeWith(Subscriptions);

            this.WhenAnyPropertyChanged(
                    nameof(SelectedGeometryIndex),
                    nameof(ImageSamplerScaleFactor),
                    nameof(ImageGapSize),
                    nameof(ImageSamplerThickness))
                .Select(x => AvailableGeometries[x.SelectedGeometryIndex](
                    x.ImageSamplerScaleFactor * x.ImageGapSize,
                    x.ImageSamplerScaleFactor * x.ImageSamplerThickness))
                .BindTo(this, x => x.GapGeometry)
                .DisposeWith(Subscriptions);

            this.WhenAnyPropertyChanged(
                    nameof(SelectedGeometryIndex),
                    nameof(ImageSamplerScaleFactor),
                    nameof(ImageSamplerSize),
                    nameof(ImageSamplerThickness))
                .Select(x => AvailableGeometries[x.SelectedGeometryIndex](
                    x.ImageSamplerScaleFactor * x.ImageSamplerSize,
                    x.ImageSamplerScaleFactor * x.ImageSamplerThickness))
                .BindTo(this, x => x.SamplerGeometry)
                .DisposeWith(Subscriptions);

            // Watch : solves issue by subscribing to [DisplayedImage] and clamping sampler position
            this.WhenAnyPropertyChanged(
                    nameof(DisplayedImage),
                    nameof(ApertureGeometry),
                    nameof(GapGeometry),
                    nameof(SamplerGeometry))
                .Subscribe(_ => UpdateSamplerPosition(LastKnownImageControlSize, SamplerCenterPos))
                .DisposeWith(Subscriptions);

            // Watch bug fix : throws because is triggered by [DisplayedImage] assignment
            this.WhenAnyPropertyChanged(nameof(SamplerCenterPos), nameof(DisplayedImage))
                .Where(x =>
                    !LastKnownImageControlSize.IsEmpty
                    && !(_sourceImage is null))
                // Watch bug fix: may throw if image size changes
                .Select(x => 1.0 * _sourceImage!.Get<float>(
                                 Convert.ToInt32(x.SamplerCenterPosInPix.Y),
                                 Convert.ToInt32(x.SamplerCenterPosInPix.X)))
                .ToPropertyEx(this, x => x.PixValue)
                .DisposeWith(Subscriptions);

            var statsObservable =
                this.WhenAnyPropertyChanged(
                        nameof(SamplerCenterPosInPix),
                        nameof(DisplayedImage),
                        nameof(ApertureGeometry), nameof(GapGeometry), nameof(SamplerGeometry))
                    .Where(x =>
                        !(x.DisplayedImage is null))
                    .Sample(TimeSpan.Parse(
                        UiSettingsProvider.Settings.Get("ImageStatsDelay", "00:00:00.250")));

            statsObservable.Where(x => !x.IsMouseOverImage && !IsSamplerFixed)
                           .Select<DipolImagePresenter, ImageStatsCollection?>(x => null)
                           .BindTo(this, x => x.ImageStats)
                           .DisposeWith(Subscriptions);
            statsObservable.Where(x => x.IsSamplerFixed || x.IsMouseOverImage)
                           .Select(_ => Observable.FromAsync(CalculateStatisticsAsync))
                           .Merge()
                           .SubscribeDispose(Subscriptions);

            SizeChangedCommand!
                //.Sample(TimeSpan.Parse(
                //                  UiSettingsProvider.Settings.Get("ImageRedrawDelay", "00:00:00.5")))
                .Subscribe(SizeChangedCommandExecute)
                .DisposeWith(Subscriptions);
        }

        public void LoadImage(Image image)
        {
            // WATCH : Modified this call
            Helper.RunNoMarshall(async () => await CopyImageAsync(image));

            //CopyImageAsync(image).ConfigureAwait(true).GetAwaiter().GetResult();
        }

        public async Task LoadImageAsync(Image image)
        {
            await CopyImageAsync(image);
        }

        private async Task CopyImageAsync(Image image)
        {
            var isFirstLoad = DisplayedImage is null;
#if DEBUG
            Random r = new Random();
            // WATCH: Debugging
            const int debugWidth = 32;
            const int debugHeight = 28;
            var dataArray = new int[debugWidth * debugHeight];
            for (var i = 0; i < debugWidth * debugHeight; i++)
            {
                dataArray[i] = r.Next() % 127;
            }
            // Top-left
            dataArray[0] = 100;

            // Top-right
            dataArray[debugWidth - 1] = 200;
            dataArray[debugWidth - 2] = 200;

            // Bottom-left
            dataArray[(debugHeight - 1) * debugWidth] = 400;
            dataArray[(debugHeight - 1) * debugWidth + 1] = 400;
            dataArray[(debugHeight - 1) * debugWidth + 2] = 400;

            // Bottom-right
            dataArray[debugWidth * debugHeight - 1] = 800;
            dataArray[debugWidth * debugHeight - 2] = 800;
            dataArray[debugWidth * debugHeight - 3] = 800;
            dataArray[debugWidth * debugHeight - 4] = 800;

            var fakeImage = new Image(dataArray, debugWidth, debugHeight, copy:false);

            

            image = fakeImage;
#endif
            Image? temp = null;
            switch (image.UnderlyingType)
            {
                case TypeCode.UInt16:
                    await Helper.RunNoMarshall(() =>
                    {
                        _sourceImage = image.CastTo<ushort, float>(x => x);
                        temp = _sourceImage.Copy();
                    });
                    break;
                case TypeCode.Single:
                    await Helper.RunNoMarshall(() =>
                    {
                        _sourceImage = image.Copy();
                        temp = _sourceImage.Copy();
                    });
                    break;
                case TypeCode.Int32:
                {
                    await Helper.RunNoMarshall(() =>
                    {
                        _sourceImage = image.CastTo<int, float>(x => x);
                        temp = _sourceImage.Copy();
                    });
                    break;
                }
                default:
                    // TODO : Implement more image types. (Even though sdk never generates other)
                    throw new NotSupportedException($"Image type {image.UnderlyingType} is not supported.");
            }
            
            if (temp is null)
                throw new NullReferenceException("Image cannot be null.");

            temp.Scale(ImageScaleMin, ImageScaleMax);
            if (_deviceSettings is {} && _deviceSettings.RotateImageBy != RotateBy.Deg0)
            {
                temp = temp.Rotate(_deviceSettings.RotateImageBy, _deviceSettings.RotateImageDirection);
            }

            if (
                _deviceSettings is {} &&
                _deviceSettings.ReflectionDirection is var reflectDir &&
                reflectDir != ReflectionDirection.NoReflection
            )
            {
                if ((reflectDir & ReflectionDirection.Horizontal) == ReflectionDirection.Horizontal)
                {
                    temp = temp.Reflect(ReflectionDirection.Horizontal);
                }
                if((reflectDir & ReflectionDirection.Vertical) == ReflectionDirection.Vertical)
                {
                    temp = temp.Reflect(ReflectionDirection.Vertical);
                }
            }

            SamplerCenterPosInPix = isFirstLoad
                // ReSharper disable PossibleLossOfFraction
                ? new Point(temp.Width / 2, temp.Height / 2)
                // ReSharper restore PossibleLossOfFraction
                : SamplerCenterPosInPix;

            DisplayedImage = temp;
        }

        private async Task CalculateStatisticsAsync()
        {
            if (_sourceImage is null)
            {
                return;
            }
            var stats = new ImageStatsCollection();

            await Helper.RunNoMarshall(() =>
            {
                var n = 0;

                IReadOnlyList<(int X, int Y)> aperture = GetPixelsInArea(GeometryLayer.Aperture);

                IReadOnlyList<(int X, int Y)> annulus = GetPixelsInArea(GeometryLayer.Annulus);

                // var apData = aperture.Select(pix => _sourceImage.Get<float>(pix.Y, pix.X))
                //                      .OrderBy(x => x)
                //                      .ToList();
                float[] apData = PreSelectPixels(aperture, _sourceImage);

                // var annData = annulus.Select(pix => _sourceImage.Get<float>(pix.Y, pix.X))
                //                      .ToList();

                float[] annData = PreSelectPixels(annulus, _sourceImage);

                if (annData.Length > 0)
                {
                    (stats.AnnulusAvg, stats.AnnulusSd) = ComputeAverageAndSd(annData);
                    // stats.AnnulusAvg = annData.Average();
                    // stats.AnnulusSd =
                    //     Math.Sqrt(annData.Select(x => Math.Pow(x - stats.AnnulusAvg, 2)).Sum() / annData.Length);
                }

                if (apData.Length > 0)
                {
                    (stats.ApertureAvg, stats.ApertureSd) = ComputeAverageAndSd(apData);
                    // stats.ApertureAvg = apData.Average();
                    // stats.ApertureSd =
                    //     Math.Sqrt(apData.Select(x => Math.Pow(x - stats.ApertureAvg, 2)).Sum() / apData.Length);
                    stats.Minimum = apData[0];
                    stats.Maximum = apData[apData.Length - 1];
                    stats.Median = apData[apData.Length / 2];
                    stats.Intensity = ComputeIntensity(apData, stats.AnnulusAvg, out n);
                    // var posPixels = apData.Where(x => x > stats.AnnulusAvg).ToList();
                    // n = posPixels.Count;
                    // stats.Intensity = posPixels.Sum() - n * stats.AnnulusAvg;
                }

                if (stats.Intensity > 0 && n > 0 && stats.AnnulusSd > 0)
                    stats.Snr = stats.Intensity / stats.AnnulusSd / Math.Sqrt(n);

            });

            ImageStats = stats;

        }

        private void UpdateGeometrySizeRanges()
        {
            if (DisplayedImage == null)
                return;

            var size = Math.Min(DisplayedImage.Width, DisplayedImage.Height) - 3 * MaxGeometryThickness;
            var sizeFr = Math.Floor(size / 5.0);

            MaxApertureWidth = 2 * sizeFr;
            MaxGapWidth = sizeFr;
            MaxAnnulusWidth = 2 * sizeFr;
        }

        private void UpdateSamplerPosition(Size elemSize, Point pos)
        {
            if (elemSize.IsEmpty)
                return;

            var posX = pos.X.Clamp(
                SamplerGeometry!.HalfSize.Width,
                elemSize.Width - SamplerGeometry.HalfSize.Width);
            var posY = pos.Y.Clamp(
                SamplerGeometry.HalfSize.Height,
                elemSize.Height - SamplerGeometry.HalfSize.Height);
            LastKnownImageControlSize = new Size(elemSize.Width, elemSize.Height);
            SamplerCenterPosInPix = GetPixelScale(new Point(posX, posY));
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

        private double GetImageScale(double x, bool horizontal = false)
            => x * (DisplayedImage != null && !LastKnownImageControlSize.IsEmpty
                   ? (horizontal
                       ? (LastKnownImageControlSize.Width / DisplayedImage.Width)
                       : (LastKnownImageControlSize.Height / DisplayedImage.Height))
                   : 1.0);

        private Point GetImageScale(Point p)
            => DisplayedImage != null
                ? new Point(
                    GetImageScale(p.X, true),
                    GetImageScale(p.Y))
                : p;

        private List<(int X, int Y)> GetPixelsInArea(GeometryLayer layer)
        {
            if (!LastKnownImageControlSize.IsEmpty && DisplayedImage != null)
            {

                var centerPix = GetPixelScale(SamplerCenterPos);
                var halfSizePix = GetPixelScale(SamplerGeometry!.HalfSize);
                var gapHalfSizePix = GetPixelScale(GapGeometry!.HalfSize);
                var apertureHalfSizePix = GetPixelScale(ApertureGeometry!.HalfSize);
                var thcknssPix = 0.5 * (GetPixelScale(SamplerGeometry.Thickness) +
                                        GetPixelScale(SamplerGeometry.Thickness, true));

                var pixelXLims = (Min: Convert.ToInt32(Math.Max(centerPix.X - halfSizePix.Width, 0)),
                    Max: Convert.ToInt32(Math.Min(centerPix.X + halfSizePix.Width, DisplayedImage.Width - 1)));

                var pixelYLims = (Min: Convert.ToInt32(Math.Max(centerPix.Y - halfSizePix.Height, 0)),
                    Max: Convert.ToInt32(Math.Min(centerPix.Y + halfSizePix.Height, DisplayedImage.Height - 1)));

                var aperturePixelXLims = (Min: Convert.ToInt32(Math.Max(centerPix.X - apertureHalfSizePix.Width, 0)),
                    Max: Convert.ToInt32(Math.Min(centerPix.X + apertureHalfSizePix.Width,
                        DisplayedImage.Width - 1)));
                var aperturePixelYLims = (Min: Convert.ToInt32(Math.Max(centerPix.Y - apertureHalfSizePix.Height, 0)),
                    Max: Convert.ToInt32(Math.Min(centerPix.Y + apertureHalfSizePix.Height,
                        DisplayedImage.Height - 1)));

                var gapPixelXLims = (Min: Convert.ToInt32(Math.Max(centerPix.X - gapHalfSizePix.Width, 0)),
                    Max: Convert.ToInt32(Math.Min(centerPix.X + gapHalfSizePix.Width,
                        DisplayedImage.Width - 1)));
                var gapPixelYLims = (Min: Convert.ToInt32(Math.Max(centerPix.Y - gapHalfSizePix.Height, 0)),
                    Max: Convert.ToInt32(Math.Min(centerPix.Y + gapHalfSizePix.Height,
                        DisplayedImage.Height - 1)));

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

        private void SizeChangedCommandExecute(SizeChangedEventArgs args)
        {
            var oldPos = SamplerCenterPosInPix;
            LastKnownImageControlSize = args.NewSize;
            ImageSamplerScaleFactor = Math.Min(args.NewSize.Width / DisplayedImage!.Width,
                args.NewSize.Height / DisplayedImage.Height);

            UpdateSamplerPosition(LastKnownImageControlSize, GetImageScale(oldPos));
        }

        private void ImageDoubleClickCommandExecute(MouseEventArgs args)
        {
            if (args.Source is FrameworkElement elem)
            {
                IsSamplerFixed = !IsSamplerFixed;

                if (!IsSamplerFixed)
                {
                    var pos = args.GetPosition(elem);
                    UpdateSamplerPosition(new Size(elem.ActualWidth, elem.ActualHeight), pos);
                }
            }
        }

        private void UnloadImageCommandExecute()
        {
            _sourceImage = null;
            DisplayedImage = null;
        }

        static DipolImagePresenter()
        {
            (AvailableGeometries, GeometriesAliases) = InitializeAvailableGeometries();
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
                        new Point(size / 2, 0), null),
                    Tuple.Create<Point, Action<StreamGeometryContext, Point>>(
                        new Point(size, size / 2), (cont, pt)
                            => cont.ArcTo(pt, new Size(size / 2, size / 2),
                                90, false, SweepDirection.Clockwise, true, false)),
                    Tuple.Create<Point, Action<StreamGeometryContext, Point>>(
                        new Point(size / 2, size), (cont, pt)
                            => cont.ArcTo(pt, new Size(size / 2, size / 2),
                                90, false, SweepDirection.Clockwise, true, false)),
                    Tuple.Create<Point, Action<StreamGeometryContext, Point>>(
                        new Point(0, size / 2), (cont, pt)
                            => cont.ArcTo(pt, new Size(size / 2, size / 2),
                                90, false, SweepDirection.Clockwise, true, false)),
                    Tuple.Create<Point, Action<StreamGeometryContext, Point>>(
                        new Point(size / 2, 0), (cont, pt)
                            => cont.ArcTo(pt, new Size(size / 2, size / 2),
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


        private static float[] PreSelectPixels(
            IReadOnlyList<(int X, int Y)> coords,
            Image image
        )
        {
            var buffer = new float[coords.Count];
            var view = image.TypedView<float>();
            var width = image.Width;
            for (var i = 0; i < coords.Count; i++)
            {
                var (x, y) = coords[i];
                buffer[i] = view[y * width + x];
            }
            Array.Sort(buffer, 0, buffer.Length);
            return buffer;
        }

        private static (double Average, double Sd) ComputeAverageAndSd(ReadOnlySpan<float> data)
        {
            if (data.IsEmpty)
            {
                return (double.NaN, double.NaN);
            }

            if (data.Length == 1)
            {
                return (data[0], double.NaN);
            }
            
            var sd = 0.0;
            var avg = 0.0;
            for (var i = 0; i < data.Length; i++)
            {
                avg += data[i];
            }

            avg /= data.Length;
            for (var i = 0; i < data.Length; i++)
            {
                var item = data[i];
                sd += (item - avg) * (item - avg);
            }

            sd = Math.Sqrt(sd / (data.Length - 1));

            return (avg, sd);
        }

        private static double ComputeIntensity(ReadOnlySpan<float> data, double annAvg, out int n)
        {
            n = 0;
            if (data.IsEmpty)
            {
                return 0.0;
            }

            var sum = 0.0;
            for (var i = 0; i < data.Length; i++)
            {
                var item = data[i];
                if (item > annAvg)
                {
                    sum += annAvg;
                    n++;
                }
            }

            return n == 0 ? 0 : sum - n * annAvg;
        }
    }
}
