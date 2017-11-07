using System;


namespace DIPOL_UF
{
    public static class DIPOL_UF_App
    {
        [STAThread]
        public static int Main(string[] args)
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

            //TestSettingsWriter();

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

        public static void TestSettingsWriter()
        {
            using (var str = new System.IO.StreamWriter("test.dipolconfig"))
            {
                System.Collections.Generic.Dictionary<string, object> pars = new System.Collections.Generic.Dictionary<string, object>();

                pars.Add("Settings1", true);
                pars.Add("Settings2", 123);
                pars.Add("Settings3", 123.1);
                pars.Add("Settings4", "str");

                SettingsManager.Write(str, pars);


            }
            using (var str = new System.IO.StreamReader("test.dipolconfig"))
            {
                var sets = SettingsManager.Read(str);
            }
        }
    }
}
