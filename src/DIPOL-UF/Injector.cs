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
        
        private static readonly Dictionary<Type, Func<object?>> ServiceLocator = new();

        private static IUserNotifier Notifier => _notifier ??= new MessageBox.MessageBoxNotifier();

        private static ILogger? Logger { get; set; }

        public static T Locate<T>() => ServiceLocator.TryGetValue(typeof(T), out Func<object?> init)
            ? init() is T result 
                ? result 
                : throw new InvalidCastException()
            : throw new InvalidOperationException();
        
        public static T? LocateOrDefault<T>() => ServiceLocator.TryGetValue(typeof(T), out Func<object?> init)
            ? init() is T result 
                ? result 
                : default
            : throw new InvalidOperationException();

        static Injector()
        {
            // These are singletons
            ServiceLocator.Add(typeof(IUserNotifier), () => Notifier);
            ServiceLocator.Add(typeof(ILogger), () => Logger);
            
            // These are transients
            ServiceLocator.Add(typeof(IAsyncMotorFactory), () => new StepMotorHandler.StepMotorFactory());
            ServiceLocator.Add(typeof(IControlClientFactory), () => new DipolClient.DipolClientFactory());
        }
        
        public static void SetLogger(ILogger logger)
        {
            if (Logger is {})
                throw new InvalidOperationException(@"Logger has been already set");
            Logger = logger;
        }
        public static IAsyncMotorFactory NewStepMotorFactory() => Locate<IAsyncMotorFactory>();

        public static IDeviceFactory NewLocalDeviceFactory()
            => new LocalCamera.LocalCameraFactory();
            
        #if  DEBUG
        public static IDeviceFactory NewDebugDeviceFactory()
            => new DebugCamera.DebugCameraFactory();
        #endif

        public static IDeviceFactory NewRemoteDeviceFactory(IControlClient client)
            => new RemoteCamera.RemoteCameraFactory(client);

        public static IControlClientFactory NewClientFactory() => Locate<IControlClientFactory>();


        public static ILogger? GetLogger() => LocateOrDefault<ILogger>();

        public static IUserNotifier GetNotifier() => Locate<IUserNotifier>();
    }
}
