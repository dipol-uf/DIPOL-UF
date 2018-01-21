using System;
using System.Collections.Generic;
using System.IO;

using System.Linq;

using System.Web.Script.Serialization;


namespace DIPOL_UF
{
    public static class DIPOL_UF_App
    {

        private static readonly string coreConfigPath = "core.dipolconfig.json";

        public static Dictionary<string, object> Settings
        {
            get;
            private set;
        }

       
        [STAThread]
        static int Main(string[] args)
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

        static DIPOL_UF_App()
        {
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

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

        
    }
}
