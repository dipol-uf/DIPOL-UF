using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
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

        public ICommand ThumbValueChangedCommand => model.ThumbValueChangedCommand;

        public DipolImagePresnterViewModel(DipolImagePresenter model) : base(model)
        {
        }
    }
}
