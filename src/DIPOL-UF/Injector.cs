#nullable enable

using System;
using System.Collections.Generic;
using ANDOR_CS;
using ANDOR_CS.Classes;
using DIPOL_Remote;
using DIPOL_UF.UserNotifications;
using Serilog;
using StepMotor;

namespace DIPOL_UF
{
    internal static class Injector
    {
        // Basically singleton lifetimes
        private static IUserNotifier? _notifier;
        
        private static readonly Dictionary<Type, Func<object[], object?>> ServiceLocator = new();
        
        private static IUserNotifier Notifier => _notifier ??= new MessageBox.MessageBoxNotifier();

        private static ILogger? Logger { get; set; }

        public static T Locate<T>(params object[] args) =>
            ServiceLocator.TryGetValue(typeof(T), out Func<object[], object?> init)
                ? init(args) is T result
                    ? result
                    : throw new InvalidCastException()
                : throw new InvalidOperationException();

        public static T? LocateOrDefault<T>(params object[] args) =>
            ServiceLocator.TryGetValue(typeof(T), out Func<object[], object?> init)
                ? init(args) is T result
                    ? result
                    : default
                : throw new InvalidOperationException();

        static Injector()
        {
            // These are singletons
            ServiceLocator.Add(typeof(IUserNotifier), _ => Notifier);
            ServiceLocator.Add(typeof(ILogger), _ => Logger);
            
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
            ServiceLocator.Add(
                typeof(IRemoteDeviceFactory), args =>
                    args is {Length: 1} && args[0] is IControlClient client
                        ? new RemoteCamera.RemoteCameraFactory(client)
                        : null
            );
        }
        
        public static void SetLogger(ILogger logger)
        {
            if (Logger is {})
                throw new InvalidOperationException(@"Logger has been already set");
            Logger = logger;
        }
        public static IAsyncMotorFactory NewStepMotorFactory() => Locate<IAsyncMotorFactory>();

        public static IDeviceFactory NewLocalDeviceFactory() => Locate<IDeviceFactory>();
            
        public static IRemoteDeviceFactory NewRemoteDeviceFactory(IControlClient client) =>
            Locate<IRemoteDeviceFactory>(client);
        public static IControlClientFactory NewClientFactory() => Locate<IControlClientFactory>();


        public static ILogger? GetLogger() => LocateOrDefault<ILogger>();

        public static IUserNotifier GetNotifier() => Locate<IUserNotifier>();
    }
}
