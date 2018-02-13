using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
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

        public double SelectionX => model.MousePos.X -50;
        public double SelectionY => model.MousePos.Y- 50;

        public ICommand ThumbValueChangedCommand => model.ThumbValueChangedCommand;
        public ICommand MouseHoverCommand => model.MouseHoverCommand;

        public object SamplerPath => model.SamplerGeometryRules;

        public DipolImagePresnterViewModel(DipolImagePresenter model) : base(model)
        {

            //Task.Run(() =>
            //{
            //    Task.Delay(1000).Wait();

            //    Helper.ExecuteOnUI(() =>
            //    {
                  
            //        var path = new StreamGeometry();
            //        using (var context = path.Open())
            //        {
            //            context.BeginFigure(new Point(0, 0), true, false);
            //            context.LineTo(new Point(100, 100), true, false);
            //        }
                   
            //        path.Freeze();

            //        //SelectionCurve = path.GetFlattenedPathGeometry();
            //        //RaisePropertyChanged(nameof(SelectionCurve));
            //    });
            //});
        }

        protected override void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnModelPropertyChanged(sender, e);
            if (e.PropertyName == nameof(model.SamplerGeometryRules)) 
                RaisePropertyChanged(nameof(SamplerPath));
            if (e.PropertyName == nameof(model.MousePos))
            {
                RaisePropertyChanged(nameof(SelectionX));
                RaisePropertyChanged(nameof(SelectionY));
            }
        }
    }
}
