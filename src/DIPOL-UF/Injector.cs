#nullable enable

using System;
using System.Collections.Generic;
using ANDOR_CS;
using ANDOR_CS.Classes;
using DIPOL_Remote;
using DIPOL_UF.Jobs;
using DIPOL_UF.Services.Contract;
using DIPOL_UF.Services.Implementation;
using DIPOL_UF.UserNotifications;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using StepMotor;

namespace DIPOL_UF
{
    internal static class Injector
    {
        public static IServiceProvider ServiceProvider { get; }

        static Injector()
        {
            ServiceProvider = new ServiceCollection()
                .AddSingleton<IUserNotifier, MessageBox.MessageBoxNotifier>()
                .AddTransient<IAsyncMotorFactory, StepMotorHandler.StepMotorFactory>()
                .AddTransient<IControlClientFactory, DipolClient.DipolClientFactory>()
                .AddTransient<IDeviceFactory, LocalCamera.LocalCameraFactory>()
#if DEBUG
                .AddTransient<IDebugDeviceFactory, DebugCamera.DebugCameraFactory>()
                .Decorate<IDeviceFactory, DebugLocalDeviceFactory>()
#endif
                .AddLogging(builder => builder.AddSerilog())
                .AddSingleton<ILogger>(Log.Logger)
                .AddTransient<App>()
                .AddSingleton<JobManager>()
                .AddSingleton<JobFactory>()
                .AddSingleton<IRemoteDeviceFactoryConstructor, RemoteDeviceFactoryConstructor>()
                .AddModels()
                .AddViewModels()
                .AddViews()
                .BuildServiceProvider();
        }

        [Obsolete("Use DI")]
        public static ILogger? GetLogger() => ServiceProvider.GetService<ILogger>();

        private static IServiceCollection AddViewModels(this IServiceCollection serviceCollection) =>
            serviceCollection
                .AddTransient<ViewModels.DipolMainWindowViewModel>();

        private static IServiceCollection AddModels(this IServiceCollection serviceCollection) =>
            serviceCollection
                .AddTransient<Models.DipolMainWindow>();

        private static IServiceCollection AddViews(this IServiceCollection serviceCollection) =>
            serviceCollection
                .AddTransient<Views.DipolMainWindow>();
    }
}
