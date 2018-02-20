using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DIPOL_UF.Models;

namespace DIPOL_UF.ViewModels
{ 
    public class DipolImagePresnterViewModel :ViewModel<DipolImagePresenter>
    {
        public WriteableBitmap BitmapSource => model.BitmapSource;
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

        public ICollection<string> GeometryAliasCollection => DipolImagePresenter.GeometriesAliases;

        public GeometryDescriptor ApertureGeometry => model.ApertureGeometry;
        public GeometryDescriptor GapGeometry => model.GapGeometry;
        public GeometryDescriptor SamplerGeometry => model.SamplerGeometry;
        public List<double> ImageStats => model.ImageStats;
        public Brush SamplerColor
        {
            get => model.SamplerColor;
            set => model.SamplerColor = value;
        }
        //public bool IsReadyForInput => 
        //    model.BitmapSource != null &&
        //    model.IsMouseOverUIControl;
        public bool IsImageLoaded => model.BitmapSource != null;
        public bool IsMouseOverUIControl => model.IsMouseOverUIControl;
        public DipolImagePresnterViewModel(DipolImagePresenter model) : base(model)
        {

            
        }

        protected override void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnModelPropertyChanged(sender, e);

            switch (e.PropertyName)
            {
                case nameof(model.BitmapSource):
                    Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(IsImageLoaded)));
                    break;
                case nameof(model.SamplerCenterPos):
                    Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(AperturePos)));
                    Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(GapPos)));
                    Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(SamplerPos)));
                    break;
                case nameof(model.ApertureGeometry):
                    Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(AperturePos)));
                    break;
                case nameof(model.GapGeometry):
                    Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(GapPos)));
                    break;
                case nameof(model.SamplerGeometry):
                    Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(SamplerPos)));
                    break;
            }
        }
    }
}
