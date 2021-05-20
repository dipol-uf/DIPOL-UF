using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;
using DIPOL_Remote;

namespace Host
{
    internal static class Host
    {
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        private sealed class Options
        {
            private static List<(PropertyInfo Property, object Default)> props =
                typeof(Options).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                               .Where(x => x.CanWrite &&
                                           (x.GetCustomAttribute<OptionAttribute>()?.Default != null
                                            || x.GetCustomAttribute<ValueAttribute>()?.Default != null))
                               .Select(x => (Property: x,
                                   Default: x.GetCustomAttribute<OptionAttribute>()?.Default ??
                                            x.GetCustomAttribute<ValueAttribute>()?.Default))
                               .ToList();

            [Value(0, HelpText = @"Service connection string", Required = true)]
            public string Uri { get; set; }

            [Option("console-width", Default = 120, HelpText = @"Width of the console window")]
            public int ConsoleWidth { get; set; }

            [Option("console-height", Default = 80, HelpText = @"Height of the console window")]
            public int ConsoleHeight { get; set; }

            [Option('l', "log", Default = false, HelpText = @"Enable logging")]
            public bool Log { get; set; }

            public static Options MakeDefault()
            {
                var opt = new Options();

                foreach (var (property, @default) in props)
                    property.SetValue(opt, @default);

                return opt;
            }
        }

        private static TextWriter Output { get; } = Console.Out;

        private static Options HandleArgs(IEnumerable<string> args)
        {
            if (args is null)
                return Options.MakeDefault();

            using (var parser = new Parser(settings =>
            {
                settings.AutoHelp = true;
                settings.AutoVersion = true;
                settings.CaseInsensitiveEnumValues = true;
                settings.HelpWriter = Output;
                settings.IgnoreUnknownArguments = true;
            }))
            {
                var arguments = parser.ParseArguments<Options>(args);

                return arguments.MapResult(x => x, y => Options.MakeDefault());
            }
        }

        private static string MessageTemplate => $"[{DateTime.Now:yyyy/MM/dd\t HH:mm:ss.fff}] > ";

        private static int Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo(@"en-US");
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo(@"en-US");

            var options = HandleArgs(args);

            if (options.Uri is null || !Uri.TryCreate(options.Uri, UriKind.RelativeOrAbsolute, out var uri))
            {
                Console.ReadKey();
                return 13;
            }

            if (options.ConsoleWidth < Console.LargestWindowWidth)
                Console.WindowWidth = options.ConsoleWidth;

            if (options.ConsoleHeight < Console.LargestWindowHeight)
                Console.WindowHeight = options.ConsoleHeight;

            using (var host = new DipolHost(uri))
            {

                if (options.Log)
                {
                    host.Opening += (sender, e) => OnHostOpenFired("opening");
                    host.Opened += (sender, e) => OnHostOpenFired("opened");

                    host.Closing += (sender, e) => OnHostCloseFired("closing");
                    host.Closed += (sender, e) => OnHostCloseFired("closed");

                    host.Faulted += (sender, e) => OnHostFaultingFired("faulted");
                    host.UnknownMessageReceived += (sender, e) => OnHostFaultingFired(e.Message.ToString());

                    host.EventReceived += OnServiceMessageFired;

                }

                host.Open();
                
//#if !DEBUG
//                while (Console.ReadLine() != "exit")
//                {
//                }
//#else
                try
                {

                    while (Console.ReadKey().Key is not ConsoleKey.Escape and not ConsoleKey.Q) { }
                }
                catch (InvalidOperationException)
                {
                    while (Console.ReadLine().ToLowerInvariant() != "exit") { }
                }
//#endif
            }

            return 0;
        }

        private static async void OnHostOpenFired(string message)
        {
            if(Output is null)
                return;

            var str = $"{MessageTemplate} Initialization: {message}";
            await Output.WriteLineAsync(str);
            await Output.FlushAsync();
        }

        private static async void OnHostCloseFired(string message)
        {
            if (Output is null)
                return;

            var str = $"{MessageTemplate} Finalization: {message}";
            await Output.WriteLineAsync(str);
            await Output.FlushAsync();
        }

        private static async void OnHostFaultingFired(string message)
        {
            if (Output is null)
                return;

            var str = $"{MessageTemplate} service failing: {message}";
            await Output.WriteLineAsync(str);
            await Output.FlushAsync();
        }

        private static async void OnServiceMessageFired(object sender, string message)
        {
            if (Output is null)
                return;

            var str = $"{MessageTemplate} [{sender}]: {message}";
            await Output.WriteLineAsync(str);
            await Output.FlushAsync();
        }
    }
}
