

#define HOST_SERVER
#define IN_PROCESS

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using DIPOL_Remote;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using DipolMainWindow = DIPOL_UF.Views.DipolMainWindow;


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
#if X86
                FileName = Path.GetFullPath("../../../../Host/bin/x86/Debug/Host.exe"),
#else
                FileName = Path.GetFullPath("../../../../Host/bin/x64/Debug/Host.exe"),
#endif
                Arguments = connStr,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true
            };
            var process = connStr is null ? null : Process.Start(pInfo);
#endif

#endif
                var dirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Dipol-UF");
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            var filePath = Path.Combine(dirPath, $"{DateTime.UtcNow:yyyyMMdd}.log");
            //using var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write);
            //using var fileWriter = new StreamWriter(fileStream);
            //Debug.Listeners.Add(new TextWriterTraceListener(fileWriter));
            //Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            //Debug.AutoFlush = true;
            
            using var logger = 
                new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(filePath)
                .WriteTo.Console()
                .CreateLogger();
            Log.Logger = logger;    

            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

            var applicationInstance = Injector.ServiceProvider.GetRequiredService<App>();
            var exitCode = applicationInstance.Run(Injector.ServiceProvider.GetRequiredService<DipolMainWindow>());

            
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
