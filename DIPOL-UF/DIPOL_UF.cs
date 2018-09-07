using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using System.Linq;

using System.Web.Script.Serialization;


namespace DIPOL_UF
{
    public static class DIPOL_UF_App
    {
        private const string CoreConfigPath = "core.dipolconfig.json";

        public static Dictionary<string, object> Settings
        {
            get;
        }

       
        [STAThread]
        static int Main(string[] args)
        {
            System.Diagnostics.Debug.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(Console.Out));
            System.Diagnostics.Debug.AutoFlush = true;
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

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
            //System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            //System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

            try
            {
                if (File.Exists(CoreConfigPath))
                    using (var str = new StreamReader(CoreConfigPath))
                        Settings = SettingsManager.Read(str);
                else
                    Settings = new Dictionary<string, object>();
            }
            catch (Exception e)
            {
                Settings = new Dictionary<string, object>();
                Helper.WriteLog(e);
            }
        
        }

        
    }
}
