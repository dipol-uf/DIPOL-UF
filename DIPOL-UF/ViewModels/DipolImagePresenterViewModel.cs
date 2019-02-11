using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace DIPOL_UF.ViewModels
{ 
    internal  sealed class DipolImagePresenterViewModel : ReactiveViewModel<DipolImagePresenter>
    {
        public static Brush[] ColorPickerColor { get; } = Application.Current?.Resources["ColorPickerColors"] as Brush[];

        public WriteableBitmap BitmapSource { get; private set; }

        public double ImgScaleMin => Model.ImgScaleMin;
        public double ImgScaleMax => Model.ImgScaleMax;

        [Reactive]
        public double ThumbLeft { get; set; }
        [Reactive]
        public double ThumbRight { get; set; }

        public Point AperturePos =>
            new Point(
                Model.SamplerCenterPos.X - ApertureGeometry.Center.X,
                Model.SamplerCenterPos.Y - ApertureGeometry.Center.Y);
        public Point GapPos =>
            new Point(
                Model.SamplerCenterPos.X - GapGeometry.Center.X,
                Model.SamplerCenterPos.Y - GapGeometry.Center.Y);
        public Point SamplerPos => 
            new Point(
                Model.SamplerCenterPos.X - SamplerGeometry.Center.X,
                Model.SamplerCenterPos.Y - SamplerGeometry.Center.Y);

        public Point SamplerCenterPosInPix => Model.SamplerCenterPosInPix;
        
        public int SelectedGeometryIndex
        {
            get => Model.SelectedGeometryIndex;
            set => Model.SelectedGeometryIndex = value;
        }
        public double ImageSamplerThickness
        {
            get => Model.ImageSamplerThickness;
            set => Model.ImageSamplerThickness = value;
        }
        public double ImageApertureSize
        {
            get => Model.ImageApertureSize;
            set => Model.ImageApertureSize = value;
        }
        public double ImageGapSize => Model.ImageGapSize;
        public double ImageSamplerSize => Model.ImageSamplerSize;
        public double ImageGap
        {
            get => Model.ImageGap;
            set => Model.ImageGap = value;
        }
        public double ImageAnnulus
        {
            get => Model.ImageAnnulus;
            set => Model.ImageAnnulus = value;
        }
        public double PixValue => Model.PixValue;

        public ICommand MouseHoverCommand => Model.MouseHoverCommand;
        public ICommand SizeChangedCommand => Model.SizeChangedCommand;
        public ICommand ImageDoubleClickCommand => Model.ImageDoubleClickCommand;
        public ICommand UnloadImageCommand => Model.UnloadImageCommand;

        public ICollection<string> GeometryAliasCollection => DipolImagePresenter.GeometriesAliases;

        public GeometryDescriptor ApertureGeometry => Model.ApertureGeometry;
        public GeometryDescriptor GapGeometry => Model.GapGeometry;
        public GeometryDescriptor SamplerGeometry => Model.SamplerGeometry;
        public IDictionary<string, double> ImageStats => Model.ImageStats;
        public int SamplerColorBrushIndex
        {
            get => Model.SamplerColorBrushIndex;
            set => Model.SamplerColorBrushIndex = value;
        }
        public Brush SamplerColor => ColorPickerColor[SamplerColorBrushIndex];
        public double MaxApertureWidth => Model.MaxApertureWidth;
        public double MaxGapWidth => Model.MaxGapWidth;
        public double MaxAnnulusWidth => Model.MaxAnnulusWidth;
        public double MinGeometryWidth => Model.MinGeometryWidth;
        public double GeometrySliderTickFrequency => Model.GeometrySliderTickFrequency;
        public double GeometryThicknessSliderTickFrequency => Model.GeometryThicknessSliderTickFrequency;
        public double MinGeometryThickness => Model.MinGeometryThickness;
        public double MaxGeometryThickness => Model.MaxGeometryThickness;

        public bool IsImageLoaded => true;//Model.DisplayedImage != null;
        public bool IsMouseOverUIControl => Model.IsMouseOverUiControl;
        public bool IsSamplerFixed => Model.IsSamplerFixed;
        public bool IsGeometryDisplayed => 
            IsImageLoaded && 
            (IsMouseOverUIControl || IsSamplerFixed);

        public DipolImagePresenterViewModel(DipolImagePresenter model) : base(model)
        { 
            HookObservables();
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

            Model.WhenPropertyChanged(x => x.ThumbRight)
                 .Select(x => x.Value)
                 .BindTo(this, x => x.ThumbRight)
                 .DisposeWith(_subscriptions);

            Model.WhenPropertyChanged(x => x.ThumbLeft)
                 .Select(x => x.Value)
                 .BindTo(this, x => x.ThumbLeft)
                 .DisposeWith(_subscriptions);
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

        private async Task UpdateBitmapAsync()
        {
            //byte[] bytes;

            //if (Model.DisplayedImage == null)
            //{
            //    if (BitmapSource == null)
            //    {
            //        RaisePropertyChanged(nameof(BitmapSource));
            //        return;
            //    }

            //    bytes = new byte[BitmapSource.PixelWidth * BitmapSource.PixelHeight * 4];
            //}
            //else
            //{
            //    bytes = await Task.Run(() =>
            //    {
            //        var temp = Model.DisplayedImage.Copy();
            //        temp.Clamp(Model.LeftScale, Model.RightScale);
            //        temp.Scale(0, 1);

            //        return temp.GetBytes();
            //    });


            //    if (BitmapSource == null ||
            //        BitmapSource.PixelWidth != Model.DisplayedImage.Width ||
            //        BitmapSource.PixelHeight != Model.DisplayedImage.Height)
            //    {

            //        BitmapSource = new WriteableBitmap(Model.DisplayedImage.Width,
            //            Model.DisplayedImage.Height,
            //            96, 96, PixelFormats.Gray32Float, null);

            //    }
            //}


            //try
            //{ 
            //    BitmapSource.Lock();
            //    System.Runtime.InteropServices.Marshal.Copy(
            //        bytes, 0, BitmapSource.BackBuffer, bytes.Length);
            //}
            //finally
            //{
            //    BitmapSource.AddDirtyRect(
            //        new Int32Rect(0, 0,
            //            BitmapSource.PixelWidth,
            //            BitmapSource.PixelHeight));
            //    BitmapSource.Unlock();
            //    RaisePropertyChanged(nameof(BitmapSource));
            //}

        }
    }
}
