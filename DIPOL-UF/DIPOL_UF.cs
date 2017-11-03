using System;


namespace DIPOL_UF
{
    public static class DIPOL_UF_App
    {
        [STAThread]
        public static int Main(string[] args)
        {
            System.Diagnostics.Debug.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(Console.Out));
            System.Diagnostics.Debug.AutoFlush = true;

            App applicationInstance = new App();
            applicationInstance.InitializeComponent();


            using (var mainModel = new Models.DipolMainWindow())
            {
                var view = new ViewModels.DipolMainWindowViewModel(mainModel);

                applicationInstance.Run(new Views.DipolMainWindow(view));
            }
            
            return 0;
        }
    }
}
