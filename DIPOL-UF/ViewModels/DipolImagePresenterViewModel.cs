﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DIPOL_UF.Models;

namespace DIPOL_UF.ViewModels
{ 
    public class DipolImagePresenterViewModel :ViewModel<DipolImagePresenter>
    {
        public static Brush[] ColorPickerColor { get; } = Application.Current?.Resources["ColorPickerColors"] as Brush[];

        public WriteableBitmap BitmapSource { get; private set; }

        public double ImgScaleMin => model.ImgScaleMin;
        public double ImgScaleMax => model.ImgScaleMax;

        public double ThumbLeft
        {
            get => model.ThumbLeft;
            set => model.ThumbLeft = value;
        }
        public double ThumbRight
        {
            get => model.ThumbRight;
            set => model.ThumbRight = value;
        }

        public Point AperturePos =>
            new Point(
                model.SamplerCenterPos.X - ApertureGeometry.Center.X,
                model.SamplerCenterPos.Y - ApertureGeometry.Center.Y);
        public Point GapPos =>
            new Point(
                model.SamplerCenterPos.X - GapGeometry.Center.X,
                model.SamplerCenterPos.Y - GapGeometry.Center.Y);
        public Point SamplerPos => 
            new Point(
                model.SamplerCenterPos.X - SamplerGeometry.Center.X,
                model.SamplerCenterPos.Y - SamplerGeometry.Center.Y);

        public Point SamplerCenterPosInPix => model.SamplerCenterPosInPix;
        
        public int SelectedGeometryIndex
        {
            get => model.SelectedGeometryIndex;
            set => model.SelectedGeometryIndex = value;
        }
        public double ImageSamplerThickness
        {
            get => model.ImageSamplerThickness;
            set => model.ImageSamplerThickness = value;
        }
        public double ImageApertureSize
        {
            get => model.ImageApertureSize;
            set => model.ImageApertureSize = value;
        }
        public double ImageGapSize => model.ImageGapSize;
        public double ImageSamplerSize => model.ImageSamplerSize;
        public double ImageGap
        {
            get => model.ImageGap;
            set => model.ImageGap = value;
        }
        public double ImageAnnulus
        {
            get => model.ImageAnnulus;
            set => model.ImageAnnulus = value;
        }
        public double PixValue => model.PixValue;

        public ICommand ThumbValueChangedCommand => model.ThumbValueChangedCommand;
        public ICommand MouseHoverCommand => model.MouseHoverCommand;
        public ICommand SizeChangedCommand => model.SizeChangedCommand;
        public ICommand ImageDoubleClickCommand => model.ImageDoubleClickCommand;
        public ICommand UnloadImageCommand => model.UnloadImageCommand;

        public ICollection<string> GeometryAliasCollection => DipolImagePresenter.GeometriesAliases;

        public GeometryDescriptor ApertureGeometry => model.ApertureGeometry;
        public GeometryDescriptor GapGeometry => model.GapGeometry;
        public GeometryDescriptor SamplerGeometry => model.SamplerGeometry;
        public IDictionary<string, double> ImageStats => model.ImageStats;
        public int SamplerColorBrushIndex
        {
            get => model.SamplerColorBrushIndex;
            set => model.SamplerColorBrushIndex = value;
        }
        public Brush SamplerColor => ColorPickerColor[SamplerColorBrushIndex];
        public double MaxApertureWidth => model.MaxApertureWidth;
        public double MaxGapWidth => model.MaxGapWidth;
        public double MaxAnnulusWidth => model.MaxAnnulusWidth;
        public double MinGeometryWidth => model.MinGeometryWidth;
        public double GeometrySliderTickFrequency => model.GeometrySliderTickFrequency;
        public double GeometryThicknessSliderTickFrequency => model.GeometryThicknessSliderTickFrequency;
        public double MinGeometryThickness => model.MinGeometryThickness;
        public double MaxGeometryThickness => model.MaxGeometryThickness;

        public bool IsImageLoaded => model.DisplayedImage != null;
        public bool IsMouseOverUIControl => model.IsMouseOverUiControl;
        public bool IsSamplerFixed => model.IsSamplerFixed;
        public bool IsGeometryDisplayed => 
            IsImageLoaded && 
            (IsMouseOverUIControl || IsSamplerFixed);

        public DipolImagePresenterViewModel(DipolImagePresenter model) : base(model)
        { 
        }

        protected override async void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnModelPropertyChanged(sender, e);

            if (e.PropertyName == nameof(model.DisplayedImage))
            {
                await Helper.ExecuteOnUI(async () => await UpdateBitmapAsync());
                Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(IsImageLoaded)));
            }

            if (e.PropertyName == nameof(model.LeftScale) ||
                e.PropertyName == nameof(model.RightScale))
                await Helper.ExecuteOnUI(async () => await UpdateBitmapAsync());

            if (e.PropertyName == nameof(model.SamplerCenterPos))
            {
                Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(AperturePos)));
                Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(GapPos)));
                Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(SamplerPos)));
            }

            if (e.PropertyName == nameof(model.ApertureGeometry))
                Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(AperturePos)));

            if (e.PropertyName == nameof(model.GapGeometry))
                Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(GapPos)));

            if (e.PropertyName == nameof(model.SamplerGeometry))
                Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(SamplerPos)));

            if (e.PropertyName == nameof(model.IsMouseOverUiControl) ||
                e.PropertyName == nameof(model.IsSamplerFixed) ||
                e.PropertyName == nameof(model.DisplayedImage))
                Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(IsGeometryDisplayed)));

            if (e.PropertyName == nameof(model.SamplerColorBrushIndex))
                Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(SamplerColor)));
        }

        private async Task UpdateBitmapAsync()
        {
            byte[] bytes;

            if (model.DisplayedImage == null)
            {
                if (BitmapSource == null)
                {
                    RaisePropertyChanged(nameof(BitmapSource));
                    return;
                }

                bytes = new byte[BitmapSource.PixelWidth * BitmapSource.PixelHeight * 4];
            }
            else
            {
                bytes = await Task.Run(() =>
                {
                    var temp = model.DisplayedImage.Copy();
                    temp.Clamp(model.LeftScale, model.RightScale);
                    temp.Scale(0, 1);

                    return temp.GetBytes();
                });


                if (BitmapSource == null ||
                    BitmapSource.PixelWidth != model.DisplayedImage.Width ||
                    BitmapSource.PixelHeight != model.DisplayedImage.Height)
                {

                    BitmapSource = new WriteableBitmap(model.DisplayedImage.Width,
                        model.DisplayedImage.Height,
                        96, 96, PixelFormats.Gray32Float, null);

                }
            }


            try
            { 
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
                RaisePropertyChanged(nameof(BitmapSource));
            }

        }
    }
}
