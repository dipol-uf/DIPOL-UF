﻿#nullable enable
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ANDOR_CS;
using ANDOR_CS.Enums;
using DipolImage;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Optimization;
using Microsoft.Toolkit.HighPerformance;
using Image = DipolImage.Image;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Size = System.Windows.Size;

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

        public struct GaussianFitResults : IEquatable<GaussianFitResults>
        {
            public double Center { get; }
            public double FWHM { get; }
            public int Origin { get; set; }

            public GaussianFitResults(double center, double fwhm) =>
                (Center, FWHM, Origin) = (center, fwhm, 0);

            public override bool Equals(object? obj) =>
                obj is GaussianFitResults other && Equals(other);


            public bool Equals(GaussianFitResults other) =>
                (Center, FWHM, Origin) == (other.Center, other.FWHM, other.Origin);

            public override int GetHashCode() =>
                HashCode.Combine(Center, FWHM, Origin);

            public static bool operator ==(GaussianFitResults left, GaussianFitResults right) =>
                left.Equals(right);

            public static bool operator !=(GaussianFitResults left, GaussianFitResults right) =>
                !left.Equals(right);
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
        private IDevice? _device;

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

        public Image? SourceImage => _sourceImage;

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

        public IObservable<(GaussianFitResults Row, GaussianFitResults Column)> FWHMEstimates { get; private set; }
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

        public ReactiveCommand<MouseButtonEventArgs, MouseButtonEventArgs>? ImageRightClickCommand { get; private set; }
        public ReactiveCommand<Unit, Unit>? UnloadImageCommand { get; private set; }

        public DipolImagePresenter(DeviceSettingsDescriptor? desc = null, IDevice? device = null)
        {
            _deviceSettings = desc;
            _device = device;
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

            ImageRightClickCommand =
                ReactiveCommand.Create<MouseButtonEventArgs, MouseButtonEventArgs>(
                                    x => x,
                                    this.WhenPropertyChanged(x => x.DisplayedImage)
                                        .Select(x => x.Value is {})
                                )
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

            FWHMEstimates =
                ImageRightClickCommand!
                   .Where(x => x.RightButton == MouseButtonState.Pressed && !IsSamplerFixed)
                   .Select(ImageRightClickCommandExecute);


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
                           .Select<DipolImagePresenter, ImageStatsCollection>(x => null)
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
            // The EM reflection takes precedence.
            // 1. Reflect amplifier (in-camera effect)
            // 2. Rotate (camera position effect)
            // 3. Reflect (camera position effect)
            if (
                _deviceSettings is {ReflectionEMDirection: var reflectEm and not ReflectionDirection.NoReflection}
             && _device?.CurrentSettings?.OutputAmplifier is (OutputAmplification.ElectronMultiplication, _, _)
            )
            {
                if ((reflectEm & ReflectionDirection.Horizontal) == ReflectionDirection.Horizontal)
                {
                    image = image.Reflect(ReflectionDirection.Horizontal);
                }

                if ((reflectEm & ReflectionDirection.Vertical) == ReflectionDirection.Vertical)
                {
                    image = image.Reflect(ReflectionDirection.Vertical);
                }
            }

            if (_deviceSettings is { RotateImageBy: var rotateBy and not RotateBy.Deg0, RotateImageDirection: var rotationDirection})
            {
                image = image.Rotate(rotateBy, rotationDirection);
            }

            if (
                _deviceSettings is {ReflectionDirection: var reflectDir and not ReflectionDirection.NoReflection}
            )
            {
                if ((reflectDir & ReflectionDirection.Horizontal) == ReflectionDirection.Horizontal)
                {
                    image = image.Reflect(ReflectionDirection.Horizontal);
                }
                if((reflectDir & ReflectionDirection.Vertical) == ReflectionDirection.Vertical)
                {
                    image = image.Reflect(ReflectionDirection.Vertical);
                }
            }


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
                case TypeCode.Int16:
                    await Helper.RunNoMarshall(() =>
                    {
                        _sourceImage = image.CastTo<short, float>(x => x);
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

            SamplerCenterPosInPix = isFirstLoad
                // ReSharper disable PossibleLossOfFraction
                ? new Point(temp.Width / 2, temp.Height / 2)
                // ReSharper restore PossibleLossOfFraction
                : SamplerCenterPosInPix;

            DisplayedImage = temp;
        }

        private async Task CalculateStatisticsAsync()
        {

            var stats = new ImageStatsCollection();

            await Helper.RunNoMarshall(() =>
            {
                var n = 0;

                var aperture = GetPixelsInArea(GeometryLayer.Aperture);

                var annulus = GetPixelsInArea(GeometryLayer.Annulus);

                var apData = aperture.Select(pix => _sourceImage.Get<float>(pix.Y, pix.X))
                                     .OrderBy(x => x)
                                     .ToList();

                var annData = annulus.Select(pix => _sourceImage.Get<float>(pix.Y, pix.X))
                                     .ToList();

                if (annData.Count > 0)
                {
                    stats.AnnulusAvg = annData.Average();
                    stats.AnnulusSd =
                        Math.Sqrt(annData.Select(x => Math.Pow(x - stats.AnnulusAvg, 2)).Sum() / annData.Count);
                }

                if (apData.Count > 0)
                {
                    stats.ApertureAvg = apData.Average();
                    stats.Minimum = apData.First();
                    stats.Maximum = apData.Last();
                    stats.Median = apData[apData.Count / 2];
                    var posPixels = apData.Where(x => x > stats.AnnulusAvg).ToList();
                    n = posPixels.Count;
                    stats.Intensity = posPixels.Sum() - n * stats.AnnulusAvg;
                    stats.ApertureSd =
                        Math.Sqrt(apData.Select(x => Math.Pow(x - stats.ApertureAvg, 2)).Sum() / apData.Count);
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

        private (GaussianFitResults Row, GaussianFitResults Column) GetImageStatistics()
        {
            if (LastKnownImageControlSize.IsEmpty || DisplayedImage == null)
            {
                return default;
            }

            var centerPix = GetPixelScale(SamplerCenterPos);
            var sizePix = GetPixelScale(SamplerGeometry!.Size);
            var halfSizePix = GetPixelScale(SamplerGeometry!.HalfSize);
            var image = _sourceImage!;
            
            var rowStart = Math.Max(0, (int) (centerPix.Y - halfSizePix.Height));
            var colStart = Math.Max(0, (int) (centerPix.X - halfSizePix.Width));
            var width = Math.Min((int) sizePix.Width, image.Width - colStart);
            var height = Math.Min((int) sizePix.Height, image.Height - rowStart);

            double[]? buffer = null;
            try
            {
                buffer = ArrayPool<double>.Shared.Rent(width * height);
                var tempView = new Span2D<double>(buffer, height, width);
                CastViewToDouble(image, colStart, rowStart, width, height, tempView);

                var backGround = Helper.Percentile(buffer.AsSpan(0, width * height), 0.025);
                
                var stats = ComputeFullWidthHalfMax(tempView, backGround);
                stats.Column.Origin = colStart;
                stats.Row.Origin = rowStart;
                return stats;
            }
            finally
            {
                if (buffer is { })
                {
                    ArrayPool<double>.Shared.Return(buffer);
                }

                
            }
        }

        private IReadOnlyList<(int X, int Y)> GetPixelsInArea(GeometryLayer layer)
        {
            if (
                !LastKnownImageControlSize.IsEmpty && 
                DisplayedImage is not null
            )
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
                        if (SamplerGeometry is null || GapGeometry is null)
                        {
                            return new List<(int X, int Y)> { (0, 0) };
                        }
                        var annulusPixels =
                            new List<(int X, int Y)>(
                                (pixelXLims.Max - pixelXLims.Min) *
                                (pixelYLims.Max - pixelYLims.Min) / 8);

                        for (var xPix = pixelXLims.Min; xPix <= pixelXLims.Max; xPix++)
                            for (var yPix = pixelYLims.Min; yPix <= pixelYLims.Max; yPix++)
                            {
                                if (SamplerGeometry.IsInsideChecker(
                                        xPix, yPix, centerPix, halfSizePix,
                                        thcknssPix
                                    ) &&
                                    !GapGeometry.IsInsideChecker(
                                        xPix, yPix, centerPix, gapHalfSizePix,
                                        thcknssPix
                                    ))
                                {
                                    annulusPixels.Add((X: xPix, Y: yPix));
                                }
                            }

                        return annulusPixels;

                    case GeometryLayer.Gap:
                        if (ApertureGeometry is null || GapGeometry is null)
                        {
                            return new List<(int X, int Y)> { (0, 0) };
                        }
                        var gapPixels =
                            new List<(int X, int Y)>(
                                (gapPixelXLims.Max - gapPixelXLims.Min) *
                                (gapPixelYLims.Max - gapPixelYLims.Min) / 8);

                        for (var xPix = gapPixelXLims.Min; xPix <= gapPixelXLims.Max; xPix++)
                            for (var yPix = gapPixelYLims.Min; yPix <= gapPixelYLims.Max; yPix++)
                            {
                                if (GapGeometry.IsInsideChecker(
                                        xPix, yPix, centerPix, halfSizePix,
                                        thcknssPix
                                    ) &&
                                    !ApertureGeometry.IsInsideChecker(
                                        xPix, yPix, centerPix, gapHalfSizePix,
                                        thcknssPix
                                    ))
                                {
                                    gapPixels.Add((X: xPix, Y: yPix));
                                }
                            }

                        return gapPixels;

                    case GeometryLayer.Aperture:
                        if (ApertureGeometry is null)
                        {
                            return new List<(int X, int Y)> { (0, 0) };
                        }
                        var aperturePixels =
                            new List<(int X, int Y)>(
                                (aperturePixelXLims.Max - aperturePixelXLims.Min) *
                                (aperturePixelYLims.Max - aperturePixelYLims.Min) / 8);

                        for (var xPix = aperturePixelXLims.Min; xPix <= aperturePixelXLims.Max; xPix++)
                            for (var yPix = aperturePixelYLims.Min; yPix <= aperturePixelYLims.Max; yPix++)
                            {
                                if (ApertureGeometry.IsInsideChecker(
                                    xPix, yPix, centerPix,
                                    apertureHalfSizePix, thcknssPix
                                ))
                                {
                                    aperturePixels.Add((X: xPix, Y: yPix));
                                }
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

        private (GaussianFitResults Row, GaussianFitResults Column) ImageRightClickCommandExecute(MouseEventArgs args)
        {
            if (args.Source is not FrameworkElement elem)
            {
                return default;
            }

            var pos = args.GetPosition(elem);
#if DEBUG
            if (Injector.GetLogger() is { } logger1)
            {
                logger1.Information("Right-clicked on image at {X}, {Y}.", pos.X, pos.Y);
            }
#endif
            try
            {
                return GetImageStatistics();
            }
            catch (Exception e)
            {
                if (Injector.GetLogger() is { } logger2)
                {
                    logger2.Error(e, "Failed to fit gaussian at {X}, {Y}.", pos.X, pos.Y);
                }

                return default;
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
                        new Point(0, 0), (_, _) => { }),
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

        private static void CastViewToDouble(Image image, int col, int row, int width, int height, Span2D<double> target)
        {
            if (image.UnderlyingType is TypeCode.SByte)
            {
                ReadOnlySpan2D<sbyte> srcView = image.TypedView2D<sbyte>().Slice(row, col, width, height);
                for (var i = 0; i < height; i++)
                {
                    for (var j = 0; j < width; j++)
                    {
                        target[i, j] = srcView[i, j];
                    }
                }
            }
            else if (image.UnderlyingType is TypeCode.Byte)
            {
                ReadOnlySpan2D<byte> srcView = image.TypedView2D<byte>().Slice(row, col, width, height);
                for (var i = 0; i < height; i++)
                {
                    for (var j = 0; j < width; j++)
                    {
                        target[i, j] = srcView[i, j];
                    }
                }
            }
            else if (image.UnderlyingType is TypeCode.Int16)
            {
                ReadOnlySpan2D<short> srcView = image.TypedView2D<short>().Slice(row, col, width, height);
                for (var i = 0; i < height; i++)
                {
                    for (var j = 0; j < width; j++)
                    {
                        target[i, j] = srcView[i, j];
                    }
                }
            }
            else if (image.UnderlyingType is TypeCode.UInt16)
            {
                ReadOnlySpan2D<ushort> srcView = image.TypedView2D<ushort>().Slice(row, col, width, height);
                for (var i = 0; i < height; i++)
                {
                    for (var j = 0; j < width; j++)
                    {
                        target[i, j] = srcView[i, j];
                    }
                }
            }
            else if (image.UnderlyingType is TypeCode.Int32)
            {
                ReadOnlySpan2D<int> srcView = image.TypedView2D<int>().Slice(row, col, width, height);
                for (var i = 0; i < height; i++)
                {
                    for (var j = 0; j < width; j++)
                    {
                        target[i, j] = srcView[i, j];
                    }
                }
            }
            else if (image.UnderlyingType is TypeCode.UInt32)
            {
                ReadOnlySpan2D<uint> srcView = image.TypedView2D<uint>().Slice(row, col, width, height);
                for (var i = 0; i < height; i++)
                {
                    for (var j = 0; j < width; j++)
                    {
                        target[i, j] = srcView[i, j];
                    }
                }
            }
            else if (image.UnderlyingType is TypeCode.Int64)
            {
                ReadOnlySpan2D<long> srcView = image.TypedView2D<long>().Slice(row, col, width, height);
                for (var i = 0; i < height; i++)
                {
                    for (var j = 0; j < width; j++)
                    {
                        target[i, j] = srcView[i, j];
                    }
                }
            }
            else if (image.UnderlyingType is TypeCode.UInt64)
            {
                ReadOnlySpan2D<ulong> srcView = image.TypedView2D<ulong>().Slice(row, col, width, height);
                for (var i = 0; i < height; i++)
                {
                    for (var j = 0; j < width; j++)
                    {
                        target[i, j] = srcView[i, j];
                    }
                }
            }
            else if (image.UnderlyingType is TypeCode.Single)
            {
                ReadOnlySpan2D<float> srcView = image.TypedView2D<float>().Slice(row, col, width, height);
                for (var i = 0; i < height; i++)
                {
                    for (var j = 0; j < width; j++)
                    {
                        target[i, j] = srcView[i, j];
                    }
                }
            }
            else if (image.UnderlyingType is TypeCode.Double)
            {
                image.TypedView2D<double>().Slice(row, col, width, height).CopyTo(target);
            }

        }

        private static (GaussianFitResults Row, GaussianFitResults Column) ComputeFullWidthHalfMax(ReadOnlySpan2D<double> data, double bkg)
        {
            var center = FindBrightestPixel(data);
            ReadOnlySpan<double> row = IK.ILSpanCasts.SpanExtensions.GetRow(data, center.Row);


            Span<double> rowView = data.Width > 128
                ? new double[data.Width]
                : stackalloc double[data.Width];

            Span<double> colView = data.Height > 128
                ? new double[data.Height]
                : stackalloc double[data.Height];

            ReadOnlySpan2D<double> column = data.Slice(0, center.Column, width: 1, height: data.Height);

            row.CopyTo(rowView);
            
            for (var i = 0; i < colView.Length; i++)
            {
                colView[i] = column[i, 0];
            }


            var rowStats =
                new GaussianFitResults(center.Row, ComputeFullWidthHalfMax(rowView, center.Column, bkg));
            var colStats =
                new GaussianFitResults(center.Column, ComputeFullWidthHalfMax(colView, center.Row, bkg));
            return (rowStats, colStats);
        }

        private static double ComputeFullWidthHalfMax(ReadOnlySpan<double> data, int pos, double bkg)
        {
            if (pos > data.Length || pos < 0)
            {
                return default;
            }

            var max = data[pos] - bkg;

            (double left, double right) = (0, data.Length - 1);
            
            for(var i = pos; i >= 0; i--) 
            {
                if (data[i] - bkg < 0.5 * max)
                {
                    left = Helper.Interpolate(data[i] - bkg, data[i + 1] - bkg, i, i + 1, 0.5 * max);
                    break;
                }
            }

            for (var i = pos; i < data.Length; i++)
            {
                if (data[i] - bkg < 0.5 * max)
                {
                    right = Helper.Interpolate(data[i - 1] - bkg, data[i] - bkg, i - 1, i, 0.5 * max);
                    break;
                }
            }

            return right - left;
        }
        
        private static (int Row, int Column) FindBrightestPixel(ReadOnlySpan2D<double> data)
        {
            var result = (Row: data.Height / 2, Column: data.Width / 2);
            var maxVal = data[result.Row, result.Column];

            for (var i = 0; i < data.Height; i++)
            {
                for (var j = 0; j < data.Width; j++)
                {
                    var current = data[i, j];
                    if (current <= maxVal)
                    {
                        continue;
                    }

                    maxVal = current;
                    result = (i, j);
                }
            }

#if DEBUG
            if (Injector.GetLogger() is { } logger)
            {
                logger.Information("Found maximum within aperture at ({X}, {Y}) with value {MaxValue}.", result.Row, result.Column, maxVal);
            }
#endif
            return result;
        }
       
    }
}
