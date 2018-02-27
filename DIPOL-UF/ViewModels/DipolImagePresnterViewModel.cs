using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DIPOL_UF.Models;

namespace DIPOL_UF.ViewModels
{ 
    public class DipolImagePresnterViewModel :ViewModel<DipolImagePresenter>
    {
        private WriteableBitmap _bitmapSource;

        public WriteableBitmap BitmapSource => _bitmapSource;
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
        public Brush SamplerColor
        {
            get => model.SamplerColor;
            set => model.SamplerColor = value;
        }
        public double MaxApertureWidth => model.MaxApertureWidth;
        public double MaxGapWidth => model.MaxGapWidth;
        public double MaxAnnulusWidth => model.MaxAnnulusWidth;
        public double MinGeometryWidth => model.MinGeometryWidth;
        public double GeometrySliderTickFrequency => model.GeometrySliderTickFrequency;
        public double GeometryThicknessSliderTickFrequency => model.GeometryThicknessSliderTickFrequency;
        public double MinGeometryThickness => model.MinGeometryThickness;
        public double MaxGeometryThickness => model.MaxGeometryThickness;

        public bool IsImageLoaded => model.DisplayedImage != null;
        public bool IsMouseOverUIControl => model.IsMouseOverUIControl;
        public bool IsSamplerFixed => model.IsSamplerFixed;
        public bool IsGeometryDisplayed => 
            IsImageLoaded && 
            (IsMouseOverUIControl || IsSamplerFixed);

        public DipolImagePresnterViewModel(DipolImagePresenter model) : base(model)
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
            if (e.PropertyName == nameof(IsMouseOverUIControl) ||
                e.PropertyName == nameof(IsSamplerFixed) ||
                e.PropertyName == nameof(IsImageLoaded))
                Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(IsGeometryDisplayed)));
        }

        private async Task UpdateBitmapAsync()
        {
            if (model.DisplayedImage == null)
            {
                _bitmapSource = null;
                RaisePropertyChanged(nameof(BitmapSource));
                return;
            }

            if (_bitmapSource == null ||
                 _bitmapSource.PixelWidth != model.DisplayedImage.Width ||
                 _bitmapSource.PixelHeight != model.DisplayedImage.Height)
            {

                _bitmapSource =new WriteableBitmap(model.DisplayedImage.Width,
                    model.DisplayedImage.Height,
                    96, 96, PixelFormats.Gray32Float, null);

            }


            var bytes = await Task.Run(() =>
            {
                var temp = model.DisplayedImage.Copy();
                temp.Clamp(model.LeftScale, model.RightScale);
                temp.Scale(0, 1);

                return temp.GetBytes();
            });

            try
            {
                _bitmapSource.Lock();
                System.Runtime.InteropServices.Marshal.Copy(
                    bytes, 0, _bitmapSource.BackBuffer, bytes.Length);
            }
            finally
            {
                _bitmapSource.AddDirtyRect(
                    new Int32Rect(0, 0, model.DisplayedImage.Width, model.DisplayedImage.Height));
                _bitmapSource.Unlock();
                RaisePropertyChanged(nameof(BitmapSource));
            }

        }
    }
}
