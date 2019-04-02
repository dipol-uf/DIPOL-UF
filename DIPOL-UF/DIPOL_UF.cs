//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018-2019 Ilia Kosenkov
//     
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//     
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//     
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.

#define HOST_SERVER
#define IN_PROCESS

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using DIPOL_Remote;
using DIPOL_UF.Models;
using DIPOL_UF.ViewModels;


namespace DIPOL_UF
{
    public static class DipolUfApp
    {
        [STAThread]
        private static int Main()
        {
#if DEBUG && HOST_SERVER
#if IN_PROCESS

            var connStr = UiSettingsProvider.Settings.GetArray<string>("RemoteLocations")?.FirstOrDefault();
            var host = connStr is null ? null : new DipolHost(new Uri(connStr));
            host?.Open();
#else
            var connStr = UiSettingsProvider.Settings.GetArray<string>("RemoteLocations")?.FirstOrDefault();

            var pInfo = new ProcessStartInfo()
            {
                FileName = Path.Combine("../../../../Host/bin/x86/Debug/Host.exe"),
                Arguments = connStr,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true
            };
            var process = connStr is null ? null : Process.Start(pInfo);
#endif

#endif
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Debug.AutoFlush = true;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");



            var applicationInstance = new App();
            applicationInstance.InitializeComponent();


            using (var mainModel = new DipolMainWindow())
                using (var view = new DipolMainWindowViewModel(mainModel))
                    applicationInstance.Run(new Views.DipolMainWindow().WithDataContext(view));


#if DEBUG && HOST_SERVER
           
#if IN_PROCESS
            host?.Close();
            host?.Dispose();
#else
            process?.StandardInput.WriteLine("exit");
            process?.WaitForExit(500);
            process?.Kill();
            process?.Dispose();
#endif 
    
#endif

            return 0;
        }
   }
}
