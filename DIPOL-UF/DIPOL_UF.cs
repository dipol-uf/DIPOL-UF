using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIPOL_UF
{
    public static class DIPOL_UF_App
    {
        [STAThread]
        public static int Main(string[] args)
        {
            App applicationInstance = new App();
            applicationInstance.InitializeComponent();
            applicationInstance.Run(new Views.DipolMainWindow());

            return 0;
        }
    }
}
