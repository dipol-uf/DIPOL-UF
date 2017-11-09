using System;
using System.Collections.Generic;
using System.IO;

namespace DIPOL_UF
{
    public static class DIPOL_UF_App
    {
        private static readonly string coreConfigPath = "core.dipolconfig";

        public static Dictionary<string, object> Settings
        {
            get;
            private set;
        }

        [STAThread]
        public static int Main(string[] args)
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

            //TestSettingsWriter();

            //System.Diagnostics.Debug.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(Console.Out));
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

        static DIPOL_UF_App()
        {
            try
            {
                if (File.Exists(coreConfigPath))
                    using (var str = new StreamReader(coreConfigPath))
                        Settings = SettingsManager.Read(str);
                else
                    Settings = null;
            }
            catch (Exception e)
            {
                Settings = null;
                Helper.WriteLog(e);
            }
        
        }

        public static void TestSettingsWriter()
        {
            using (var str = new System.IO.StreamWriter("test.dipolconfig"))
            {
                Dictionary<string, object> pars = new Dictionary<string, object>();

                pars.Add("Settings1", true);
                pars.Add("Settings2", 123);
                pars.Add("Settings3", 123.1);
                pars.Add("Settings4", "str");
                pars.Add("RemoteLocations", new object[] { true, "dipol-2", "dipol-3", 213, "1,\t 2," });
                SettingsManager.Write(str, pars);


            }
            using (var str = new System.IO.StreamReader("test.dipolconfig"))
            {
                var sets = SettingsManager.Read(str);
            }
        }
    }
}
