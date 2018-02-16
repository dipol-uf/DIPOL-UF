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

        public Point SamplerCenterPos => 
            new Point(
                model.SamplerCenterPos.X - SamplerGeometry.Center.X,
                model.SamplerCenterPos.Y - SamplerGeometry.Center.Y);
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
        public double ImageSamplerSize
        {
            get => model.ImageSamplerSize;
            set => model.ImageSamplerSize = value;
        }

        public ICommand ThumbValueChangedCommand => model.ThumbValueChangedCommand;
        public ICommand MouseHoverCommand => model.MouseHoverCommand;
        public ICommand SizeChangedCommand => model.SizeChangedCommand;

        public ICollection<string> GeometryAliasCollection => DipolImagePresenter.GeometriesAliases;

        public GeometryDescriptor SamplerGeometry => model.SamplerGeometry;

        public List<double> ImageStats => model.ImageStats;
        public Brush SamplerColor
        {
            get => model.SamplerColor;
            set => model.SamplerColor = value;
        }
        public bool IsReadyForInput => 
            model.BitmapSource != null &&
            model.IsMouseOverUIControl;
        public bool IsImageLoaded => model.BitmapSource != null;

        public DipolImagePresnterViewModel(DipolImagePresenter model) : base(model)
        {

            
        }

        protected override void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnModelPropertyChanged(sender, e);

            if(e.PropertyName == nameof(BitmapSource) ||
               e.PropertyName == nameof(model.IsMouseOverUIControl))
                Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(IsReadyForInput)));

            if(e.PropertyName == nameof(BitmapSource))
                Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(IsImageLoaded)));
        }
    }
}
