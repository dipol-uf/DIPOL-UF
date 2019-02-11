using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
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
    internal  sealed class DipolImagePresenterViewModel : ReactiveViewModel<DipolImagePresenter>
    {
        public static Brush[] ColorPickerColor { get; } 
            = Application.Current?.Resources["ColorPickerColors"] as Brush[];

        public WriteableBitmap BitmapSource { [ObservableAsProperty] get; }

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
        public Point SamplerCenterPosInPix { [ObservableAsProperty] get; }
        public Brush SamplerColor { [ObservableAsProperty] get; }
        public GeometryDescriptor ApertureGeometry { [ObservableAsProperty] get; }
        public GeometryDescriptor GapGeometry { [ObservableAsProperty] get; }
        public GeometryDescriptor SamplerGeometry { [ObservableAsProperty] get; }
        //public double ImageGapSize { [ObservableAsProperty] get; }
        //public double ImageSamplerSize { [ObservableAsProperty] get; }

        public bool IsImageLoaded { [ObservableAsProperty] get; }
        public bool IsGeometryDisplayed { [ObservableAsProperty] get; }
        public bool IsMouseOverImage { [ObservableAsProperty] get; }
        public bool IsSamplerFixed { [ObservableAsProperty] get; }

        public double PixValue => Model.PixValue;

        public ReactiveCommand<MouseEventArgs, MouseEventArgs> MouseHoverCommand { get; private set; }
        public ICommand SizeChangedCommand => Model.SizeChangedCommand;
        public ICommand ImageClickCommand => Model.ImageClickCommand;
        public ICommand UnloadImageCommand => Model.UnloadImageCommand;
        
        public ICollection<string> GeometryAliasCollection => DipolImagePresenter.GeometriesAliases;

        public IDictionary<string, double> ImageStats => Model.ImageStats;
        public double MaxApertureWidth => Model.MaxApertureWidth;
        public double MaxGapWidth => Model.MaxGapWidth;
        public double MaxAnnulusWidth => Model.MaxAnnulusWidth;
        public double MinGeometryWidth => Model.MinGeometryWidth;
        public double GeometrySliderTickFrequency => Model.GeometrySliderTickFrequency;
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
                               .DisposeWith(_subscriptions);
        }

        private void HookObservables()
        {
            this.WhenPropertyChanged(x => x.ThumbLeft)
                .Select(x => x.Value)
                .InvokeCommand(Model.LeftThumbChangedCommand)
                .DisposeWith(_subscriptions);

            this.WhenPropertyChanged(x => x.ThumbRight)
                .Select(x => x.Value)
                .InvokeCommand(Model.RightThumbChangedCommand)
                .DisposeWith(_subscriptions);

            this.WhenAnyPropertyChanged(
                    nameof(IsImageLoaded),
                    nameof(IsMouseOverImage),
                    nameof(IsSamplerFixed))
                .Select(x => x.IsImageLoaded && (x.IsMouseOverImage || x.IsSamplerFixed))
                .ToPropertyEx(this, x => x.IsGeometryDisplayed)
                .DisposeWith(_subscriptions);

            this.WhenPropertyChanged(x => x.SamplerColorBrushIndex)
                .Select(x => ColorPickerColor[x.Value])
                .ToPropertyEx(this, x => x.SamplerColor)
                .DisposeWith(_subscriptions);

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

            MouseHoverCommand
                .Where(x =>
                    x.Source is Image
                    && x.RoutedEvent.Name != nameof(Image.MouseMove))
                .Sample(TimeSpan.Parse(
                    UiSettingsProvider.Settings.Get("ImageSampleDelay", "00:00:00.050")))
                .Select(x => x.RoutedEvent.Name == nameof(Image.MouseEnter))
                .ObserveOnUi()
                .ToPropertyEx(this, x => x.IsMouseOverImage)
                .DisposeWith(_subscriptions);

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
                             .DisposeWith(_subscriptions);

            HookModelObservables();
        }

        private void HookModelObservables() { 

            Model.WhenPropertyChanged(x => x.ThumbRight)
                 .Select(x => x.Value)
                 .ObserveOnUi()
                 .BindTo(this, x => x.ThumbRight)
                 .DisposeWith(_subscriptions);

            Model.WhenPropertyChanged(x => x.ThumbLeft)
                 .Select(x => x.Value)
                 .ObserveOnUi()
                 .BindTo(this, x => x.ThumbLeft)
                 .DisposeWith(_subscriptions);


            Model.WhenAnyPropertyChanged(
                     nameof(Model.DisplayedImage), 
                     nameof(Model.LeftScale), 
                     nameof(Model.RightScale))
                 .Where(x => !(x.DisplayedImage is null))
                 .Select(x => Observable.FromAsync(async () => await UpdateBitmapAsync(x.DisplayedImage)))
                 .Merge()
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.BitmapSource)
                 .DisposeWith(_subscriptions);

            Model.WhenPropertyChanged(x => x.DisplayedImage)
                 .Select(x => !(x.Value is null))
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.IsImageLoaded)
                 .DisposeWith(_subscriptions);

            Model.WhenAnyPropertyChanged(
                     nameof(Model.SamplerCenterPos), 
                     nameof(Model.ApertureGeometry))
                 .Select(x => new Point(
                     x.SamplerCenterPos.X - x.ApertureGeometry.Center.X,
                     x.SamplerCenterPos.Y - x.ApertureGeometry.Center.Y))
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.AperturePos)
                 .DisposeWith(_subscriptions);

            Model.WhenAnyPropertyChanged(
                     nameof(Model.SamplerCenterPos),
                     nameof(Model.GapGeometry))
                 .Select(x => new Point(
                     x.SamplerCenterPos.X - x.GapGeometry.Center.X,
                     x.SamplerCenterPos.Y - x.GapGeometry.Center.Y))
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.GapPos)
                 .DisposeWith(_subscriptions);

            Model.WhenAnyPropertyChanged(
                     nameof(Model.SamplerCenterPos),
                     nameof(Model.SamplerGeometry))
                 .Select(x => new Point(
                     x.SamplerCenterPos.X - x.SamplerGeometry.Center.X,
                     x.SamplerCenterPos.Y - x.SamplerGeometry.Center.Y))
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.SamplerPos)
                 .DisposeWith(_subscriptions);

            Model.WhenPropertyChanged(x => x.SamplerCenterPosInPix)
                 .Select(x => x.Value)
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.SamplerCenterPosInPix)
                 .DisposeWith(_subscriptions);

            PropagateReadOnlyProperty(this, x => x.IsSamplerFixed, y => y.IsSamplerFixed);
            PropagateReadOnlyProperty(this, x => x.ApertureGeometry, y => y.ApertureGeometry);
            PropagateReadOnlyProperty(this, x => x.GapGeometry, y => y.GapGeometry);
            PropagateReadOnlyProperty(this, x => x.SamplerGeometry, y => y.SamplerGeometry);
            //PropagateReadOnlyProperty(this, x => x.ImageGapSize, y => y.ImageGapSize);
            //PropagateReadOnlyProperty(this, x => x.ImageSamplerSize, y => y.ImageSamplerSize);


        }

        //protected override async void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        //{
        //    base.OnModelPropertyChanged(sender, e);

        //    if (e.PropertyName == nameof(Model.DisplayedImage))
        //    {
        //        await Helper.ExecuteOnUI(async () => await UpdateBitmapAsync());
        //        Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(IsImageLoaded)));
        //    }

        //    if (e.PropertyName == nameof(Model.LeftScale) ||
        //        e.PropertyName == nameof(Model.RightScale))
        //        await Helper.ExecuteOnUI(async () => await UpdateBitmapAsync());

        //    if (e.PropertyName == nameof(Model.SamplerCenterPos))
        //    {
        //        Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(AperturePos)));
        //        Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(GapPos)));
        //        Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(SamplerPos)));
        //    }

        //    if (e.PropertyName == nameof(Model.ApertureGeometry))
        //        Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(AperturePos)));

        //    if (e.PropertyName == nameof(Model.GapGeometry))
        //        Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(GapPos)));

        //    if (e.PropertyName == nameof(Model.SamplerGeometry))
        //        Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(SamplerPos)));

        //    if (e.PropertyName == nameof(Model.IsMouseOverUiControl) ||
        //        e.PropertyName == nameof(Model.IsSamplerFixed) ||
        //        e.PropertyName == nameof(Model.DisplayedImage))
        //        Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(IsGeometryDisplayed)));

        //    if (e.PropertyName == nameof(Model.SamplerColorBrushIndex))
        //        Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(SamplerColor)));
        //}

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
