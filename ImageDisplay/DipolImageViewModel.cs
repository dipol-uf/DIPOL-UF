using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageDisplayLib;
using System.Windows.Media.Imaging;


namespace FITS_CS
{
    public class DipolImageViewModel : DIPOL_UF.ViewModels.ViewModel<DipolImage>
    {
        public WriteableBitmap Bitmap => model.Bitmap;

        public DipolImageViewModel(DipolImage model) : base(model)
        {
        }
    }
}
