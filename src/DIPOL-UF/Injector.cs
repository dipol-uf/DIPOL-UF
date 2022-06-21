#nullable enable

using System;
using ANDOR_CS;
using ANDOR_CS.Classes;
using DIPOL_Remote;
using DIPOL_UF.Jobs;
using DIPOL_UF.Services.Contract;
using DIPOL_UF.Services.Implementation;
using DIPOL_UF.UiComponents.Contract;
using DIPOL_UF.UiComponents.Implementation;
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
                .AddSingleton(Log.Logger)
                .AddTransient<App>()
                .AddSingleton<JobManager>()
                .AddSingleton<JobFactory>()
                .AddSingleton<IRemoteDeviceFactoryConstructor, RemoteDeviceFactoryConstructor>()
                // These are singletons, single per app
                .AddSingleton<CycleTimerManager>()
                .AddSingleton<ICycleTimerManager>(provider => provider.GetRequiredService<CycleTimerManager>())
                .AddSingleton<ICycleTimerSource>(provider => provider.GetRequiredService<CycleTimerManager>())
                // These are scoped to each camera                
                .AddScoped<AcquisitionTimerManger>()
                .AddScoped<IAcquisitionTimerManager>(provider => provider.GetRequiredService<AcquisitionTimerManger>())
                .AddScoped<IAcquisitionTimerSource>(provider => provider.GetRequiredService<AcquisitionTimerManger>())
                
                .AddScoped<IJobTimerSource, JobTimerSource>()
                .AddScoped<IJobProgressSource, JobProgressSource>()
                .AddModels()
                .AddViewModels()
                .AddViews()
                .AddLogging(builder => builder.AddSerilog())
                .AddMemoryCache()
                .AddOptions()
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
