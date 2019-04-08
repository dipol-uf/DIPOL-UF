//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018-2019 Ilia Kosenkov
//     
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//     
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//     
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.

using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DIPOL_UF.Models;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Brush = System.Windows.Media.Brush;
using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

// ReSharper disable UnassignedGetOnlyAutoProperty

namespace DIPOL_UF.ViewModels
{
    internal sealed class DipolImagePresenterViewModel : ReactiveViewModel<DipolImagePresenter>
    {
        public static Brush[] ColorPickerColor { get; }
            = Application.Current?.Resources["ColorPickerColors"] as Brush[];

        [Reactive]
        public WriteableBitmap BitmapSource { get; private set; }

        public int ThumbScaleMin => Model.ThumbScaleMin;
        public int ThumbScaleMax => Model.ThumbScaleMax;


        [Reactive]
        public int ThumbLeft { get; set; }

        [Reactive]
        public int ThumbRight { get; set; }

        [Reactive]
        public int SamplerColorBrushIndex { get; set; }

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

        public Point AperturePos { [ObservableAsProperty] get; }
        public Point GapPos { [ObservableAsProperty] get; }
        public Point SamplerPos { [ObservableAsProperty] get; }
        public Point? SamplerCenterPosInPix { [ObservableAsProperty] get; }
        public Brush SamplerColor { [ObservableAsProperty] get; }
        public GeometryDescriptor ApertureGeometry { [ObservableAsProperty] get; }
        public GeometryDescriptor GapGeometry { [ObservableAsProperty] get; }
        public GeometryDescriptor SamplerGeometry { [ObservableAsProperty] get; }

        public bool IsImageLoaded { [ObservableAsProperty] get; }
        public bool IsGeometryDisplayed { [ObservableAsProperty] get; }
        public bool IsMouseOverImage { [ObservableAsProperty] get; }
        public bool IsSamplerFixed { [ObservableAsProperty] get; }
        public double MaxApertureWidth { [ObservableAsProperty] get; }
        public double MaxGapWidth { [ObservableAsProperty] get; }
        public double MaxAnnulusWidth { [ObservableAsProperty] get; }

        public DipolImagePresenter.ImageStatsCollection ImageStats { [ObservableAsProperty] get; }
        public double? PixValue { [ObservableAsProperty] get; }

        public ReactiveCommand<MouseEventArgs, MouseEventArgs> MouseHoverCommand { get; private set; }
        public ICommand SizeChangedCommand => Model.SizeChangedCommand;
        public ICommand ImageClickCommand => Model.ImageClickCommand;

        public ICollection<string> GeometryAliasCollection => DipolImagePresenter.GeometriesAliases;

        public double GeometrySliderTickFrequency => Model.GeometrySliderTickFrequency;
        public double MinGeometryWidth => Model.MinGeometryWidth;
        public double GeometryThicknessSliderTickFrequency => Model.GeometryThicknessSliderTickFrequency;
        public double MinGeometryThickness => Model.MinGeometryThickness;
        public double MaxGeometryThickness => Model.MaxGeometryThickness;


        public DipolImagePresenterViewModel(DipolImagePresenter model) : base(model)
        {
            ThumbRight = Model.ThumbScaleMax;
            SelectedGeometryIndex = 1;
            ImageApertureSize = 10.0;
            ImageGap = 10.0;
            ImageAnnulus = 10.0;
            ImageSamplerThickness = 2;
            InitializeCommands();
            HookObservables();
        }

        private void InitializeCommands()
        {
            MouseHoverCommand =
                ReactiveCommand.Create<MouseEventArgs, MouseEventArgs>(
                                   x => x,
                                   this.WhenPropertyChanged(x => x.IsImageLoaded).Select(x => x.Value))
                               .DisposeWith(Subscriptions);
        }

        private void HookObservables()
        {
            this.WhenPropertyChanged(x => x.ThumbLeft)
                .Select(x => x.Value)
                .InvokeCommand(Model.LeftThumbChangedCommand)
                .DisposeWith(Subscriptions);

            this.WhenPropertyChanged(x => x.ThumbRight)
                .Select(x => x.Value)
                .InvokeCommand(Model.RightThumbChangedCommand)
                .DisposeWith(Subscriptions);

            this.WhenAnyPropertyChanged(
                    nameof(IsImageLoaded),
                    nameof(IsMouseOverImage),
                    nameof(IsSamplerFixed))
                .Select(x => x.IsImageLoaded && (x.IsMouseOverImage || x.IsSamplerFixed))
                .ToPropertyEx(this, x => x.IsGeometryDisplayed)
                .DisposeWith(Subscriptions);

            this.WhenPropertyChanged(x => x.SamplerColorBrushIndex)
                .Select(x => ColorPickerColor[x.Value])
                .ToPropertyEx(this, x => x.SamplerColor)
                .DisposeWith(Subscriptions);

            BindTo(this, x => x.SelectedGeometryIndex,
                Model, y => y.SelectedGeometryIndex);
            BindTo(this, x => x.ImageSamplerThickness,
                Model, y => y.ImageSamplerThickness);
            BindTo(this, x => x.ImageApertureSize,
                Model, y => y.ImageApertureSize);
            BindTo(this, x => x.ImageGap,
                Model, y => y.ImageGap);
            BindTo(this, x => x.ImageAnnulus,
                Model, y => y.ImageAnnulus);
            BindTo(this, x => x.IsMouseOverImage,
                Model, y => y.IsMouseOverImage);

            MouseHoverCommand
                .Where(x =>
                    x.Source is Image
                    && x.RoutedEvent.Name != nameof(Image.MouseMove))
                .Sample(TimeSpan.Parse(
                    UiSettingsProvider.Settings.Get("ImageSampleDelay", "00:00:00.050")))
                .Select(x => x.RoutedEvent.Name == nameof(Image.MouseEnter))
                .ObserveOnUi()
                .ToPropertyEx(this, x => x.IsMouseOverImage)
                .DisposeWith(Subscriptions);

            MouseHoverCommand.Where(x => x.Source is Image)
                             .Sample(TimeSpan.Parse(
                                 UiSettingsProvider.Settings.Get("ImageSampleDelay", "00:00:00.050")))
                             .ObserveOnUi()
                             .Select(x =>
                             {
                                 var src = x.Source as FrameworkElement;
                                 // ReSharper disable once PossibleNullReferenceException
                                 return (Size: new Size(src.ActualWidth, src.ActualHeight),
                                     SamplerCenterPosInPix: x.GetPosition(src));
                             }).InvokeCommand(Model.MouseHoverCommand)
                             .DisposeWith(Subscriptions);

            HookModelObservables();
        }

        private void HookModelObservables()
        {

            Model.WhenPropertyChanged(x => x.ThumbRight)
                 .Select(x => x.Value)
                 .ObserveOnUi()
                 .BindTo(this, x => x.ThumbRight)
                 .DisposeWith(Subscriptions);

            Model.WhenPropertyChanged(x => x.ThumbLeft)
                 .Select(x => x.Value)
                 .ObserveOnUi()
                 .BindTo(this, x => x.ThumbLeft)
                 .DisposeWith(Subscriptions);


            Model.WhenAnyPropertyChanged(
                     nameof(Model.DisplayedImage),
                     nameof(Model.LeftScale),
                     nameof(Model.RightScale))
                 .Where(x => !(x.DisplayedImage is null))
                 .LogTask("BEFORE UI")
                 .ObserveOnUi()
                 .LogTask("AFTER UI")
                 .Subscribe(x => UpdateBitmap(x.DisplayedImage))
                 .DisposeWith(Subscriptions);
            
            //.Select(x => Observable.FromAsync(() => UpdateBitmapAsync(x.DisplayedImage)))
                 //.ToPropertyEx(this, x => x.BitmapSource)
                 //.DisposeWith(Subscriptions);

            Model.WhenPropertyChanged(x => x.DisplayedImage)
                 .Select(x => !(x.Value is null))
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.IsImageLoaded)
                 .DisposeWith(Subscriptions);

            Model.WhenAnyPropertyChanged(
                     nameof(Model.SamplerCenterPos),
                     nameof(Model.ApertureGeometry))
                 .Select(x => new Point(
                     x.SamplerCenterPos.X - x.ApertureGeometry.Center.X,
                     x.SamplerCenterPos.Y - x.ApertureGeometry.Center.Y))
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.AperturePos)
                 .DisposeWith(Subscriptions);

            Model.WhenAnyPropertyChanged(
                     nameof(Model.SamplerCenterPos),
                     nameof(Model.GapGeometry))
                 .Select(x => new Point(
                     x.SamplerCenterPos.X - x.GapGeometry.Center.X,
                     x.SamplerCenterPos.Y - x.GapGeometry.Center.Y))
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.GapPos)
                 .DisposeWith(Subscriptions);

            Model.WhenAnyPropertyChanged(
                     nameof(Model.SamplerCenterPos),
                     nameof(Model.SamplerGeometry))
                 .Select(x => new Point(
                     x.SamplerCenterPos.X - x.SamplerGeometry.Center.X,
                     x.SamplerCenterPos.Y - x.SamplerGeometry.Center.Y))
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.SamplerPos)
                 .DisposeWith(Subscriptions);


            PropagateReadOnlyProperty(this, x => x.IsSamplerFixed, y => y.IsSamplerFixed);
            PropagateReadOnlyProperty(this, x => x.ApertureGeometry, y => y.ApertureGeometry);
            PropagateReadOnlyProperty(this, x => x.GapGeometry, y => y.GapGeometry);
            PropagateReadOnlyProperty(this, x => x.SamplerGeometry, y => y.SamplerGeometry);
            PropagateReadOnlyProperty(this, x => x.MaxApertureWidth, y => y.MaxApertureWidth);
            PropagateReadOnlyProperty(this, x => x.MaxGapWidth, y => y.MaxGapWidth);
            PropagateReadOnlyProperty(this, x => x.MaxAnnulusWidth, y => y.MaxAnnulusWidth);
            PropagateReadOnlyProperty(this, x => x.ImageStats, y => y.ImageStats);

            Model.WhenPropertyChanged(x => x.SamplerCenterPosInPix)
                 .Select(x => (Point?)x.Value)
                 .Merge(Model.WhenPropertyChanged(x => x.ImageStats).Where(x => x.Value is null)
                             .Select(_ => (Point?)null))
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.SamplerCenterPosInPix)
                 .DisposeWith(Subscriptions);
            Model.WhenPropertyChanged(x => x.PixValue)
                 .Select(x => new double?(x.Value))
                 .Merge(Model.WhenPropertyChanged(x => x.ImageStats).Where(x => x.Value is null)
                             .Select(_ => (double?) null))
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.PixValue)
                 .DisposeWith(Subscriptions);

        }

        private void UpdateBitmap(DipolImage.Image image)
        {
            DebugHelper.LogTask("In updater");
            var temp = image.Copy();
            temp.Clamp(Model.LeftScale, Model.RightScale);
            temp.Scale(Model.ImageScaleMin, Model.ImageScaleMax);

            var bytes = temp.GetBytes();
            if (BitmapSource is null ||
                BitmapSource.PixelWidth != Model.DisplayedImage.Width ||
                BitmapSource.PixelHeight != Model.DisplayedImage.Height)
                BitmapSource = new WriteableBitmap(image.Width,
                    image.Height,
                    96, 96, PixelFormats.Gray32Float, null);

            try
            {
                DebugHelper.LogTask($"Before is downloading");
                BitmapSource.Lock();
                System.Runtime.InteropServices.Marshal.Copy(
                    bytes, 0, BitmapSource.BackBuffer, bytes.Length);
            }
            finally
            {
                BitmapSource.AddDirtyRect(
                    new Int32Rect(0, 0,
                        BitmapSource.PixelWidth,
                        BitmapSource.PixelHeight));
                BitmapSource.Unlock();
                DebugHelper.LogTask($"After is downloading");

            }

        }

        private async Task<WriteableBitmap> UpdateBitmapAsync(DipolImage.Image image)
        {
            var bytes = await Helper.RunNoMarshall(() =>
            {
                var temp = image.Copy();
                temp.Clamp(Model.LeftScale, Model.RightScale);
                temp.Scale(Model.ImageScaleMin, Model.ImageScaleMax);

                return temp.GetBytes();
            });

            var bitmap = BitmapSource;
            Helper.ExecuteOnUi(() =>
            {
                if (bitmap is null ||
                    bitmap.PixelWidth != Model.DisplayedImage.Width ||
                    bitmap.PixelHeight != Model.DisplayedImage.Height)
                    bitmap = new WriteableBitmap(image.Width,
                        image.Height,
                        96, 96, PixelFormats.Gray32Float, null);



                try
                {
                    bitmap.Lock();
                    System.Runtime.InteropServices.Marshal.Copy(
                        bytes, 0, bitmap.BackBuffer, bytes.Length);
                }
                finally
                {
                    bitmap.AddDirtyRect(
                        new Int32Rect(0, 0,
                            bitmap.PixelWidth,
                            bitmap.PixelHeight));
                    bitmap.Unlock();

                }
            });

            return bitmap;
        }
    }
}