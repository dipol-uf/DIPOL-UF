#nullable enable

using System;
using System.Collections.Generic;
using ANDOR_CS;
using ANDOR_CS.Classes;
using DIPOL_Remote;
using DIPOL_UF.UserNotifications;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using StepMotor;

namespace DIPOL_UF
{
    internal static class Injector
    {
        
        private static readonly Dictionary<Type, Func<object[], object?>> ServiceLocator = new();

        public static IServiceProvider ServiceProvider { get; }

        [Obsolete("Use DI")]
        public static T Locate<T>(params object[] args) =>
            ServiceLocator.TryGetValue(typeof(T), out Func<object[], object?> init)
                ? init(args) is T result
                    ? result
                    : throw new InvalidCastException()
                : throw new InvalidOperationException();

        [Obsolete("Use DI")]
        public static T? LocateOrDefault<T>(params object[] args) =>
            ServiceLocator.TryGetValue(typeof(T), out Func<object[], object?> init)
                ? init(args) is T result
                    ? result
                    : default
                : throw new InvalidOperationException();

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
                .AddTransient<App>()
                .AddModels()
                .AddViewModels()
                .AddViews()
                .BuildServiceProvider();

            // These are singletons
            ServiceLocator.Add(typeof(IUserNotifier), _ => ServiceProvider.GetRequiredService<IUserNotifier>());
            ServiceLocator.Add(typeof(ILogger), _ => Log.Logger);
            
            // These are transients
            ServiceLocator.Add(typeof(IAsyncMotorFactory), _ => new StepMotorHandler.StepMotorFactory());
            ServiceLocator.Add(typeof(IControlClientFactory), _ => new DipolClient.DipolClientFactory());
            
            #if DEBUG
            ServiceLocator.Add(
                typeof(IDeviceFactory),
                _ => new DebugLocalDeviceFactory(
                    new LocalCamera.LocalCameraFactory(), 
                    new DebugCamera.DebugCameraFactory()
                )
            );
            #else
            ServiceLocator.Add(typeof(IDeviceFactory), _ => new LocalCamera.LocalCameraFactory());
            #endif
            // This is not yet DI'ed
            ServiceLocator.Add(
                typeof(IRemoteDeviceFactory), args =>
                    args is {Length: 1} && args[0] is IControlClient client
                        ? new RemoteCamera.RemoteCameraFactory(client)
                        : null
            );
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
