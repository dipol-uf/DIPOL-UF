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

using ANDOR_CS.Classes;
using ANDOR_CS.Enums;
using DIPOL_Remote;
using DIPOL_UF.ViewModels;
using DIPOL_UF.Views;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DIPOL_UF.Jobs;
using DynamicData.Kernel;
using StepMotor;
using Exception = System.Exception;

namespace DIPOL_UF.Models
{
    internal sealed class DipolMainWindow : ReactiveObjectEx
    {
        private readonly string[] _remoteLocations
            = UiSettingsProvider.Settings.GetArray<string>("RemoteLocations")
              ?? new string[0];

        private DipolClient[] _remoteClients;

        private (string Port, ReadOnlyCollection<byte> Devices)[] _foundSerialDevices;

        private readonly SourceCache<(string Id, CameraBase Camera), string> _connectedCameras;

        private Task _serialPortScanningTask;

        [Reactive]
        private bool IsSerialPortTaskCompleted { get; set; }

        [Reactive]
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public StepMotorHandler PolarimeterMotor { get; private set; }

        public DescendantProvider ProgressBarProvider { get; private set; }
        public DescendantProvider AvailableCamerasProvider { get; private set; }

        public bool IsDisposing { get; private set; }

        // ReSharper disable UnassignedGetOnlyAutoProperty
        public bool CanConnect { [ObservableAsProperty] get; }
        // ReSharper restore UnassignedGetOnlyAutoProperty

        public SourceList<string> SelectedDevices { get; }
        public IObservableCache<(string Id, CameraBase Camera), string> ConnectedCameras { get; private set; }
        public IObservableCache<(string Id, CameraTab Tab), string> CameraTabs { get; private set; }

        public ReactiveCommand<Unit, Unit> WindowLoadedCommand { get; private set; }

        public ReactiveCommand<object, AvailableCamerasModel> ConnectButtonCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> DisconnectButtonCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> SelectAllCamerasCommand { get; private set; }
        public ReactiveCommand<string, Unit> SelectCameraCommand { get; private set; }
        public ReactiveCommand<string, Unit> ContextMenuCommand { get; private set; }

        public ReactiveCommand<Unit, Unit> PolarimeterMotorButtonCommand { get; private set; }

        public DipolMainWindow()
        {
            _serialPortScanningTask = CheckStepMotors();

            _connectedCameras = new SourceCache<(string Id, CameraBase Camera), string>(x => x.Id)
                .DisposeWith(Subscriptions);

            SelectedDevices = new SourceList<string>()
                .DisposeWith(Subscriptions);

            InitializeCommands();
            HookObservables();
            HookValidators();
            JobManager.Manager.AttachToMainWindow(this);
        }

        private Task CheckStepMotors()
        {
            return Task.Run(async () =>
            {
                Application.Current?.Dispatcher.InvokeAsync(() => IsSerialPortTaskCompleted = false);

                try
                {
                    var comPorts = SerialPort.GetPortNames();
                    _foundSerialDevices =
                        await Task.WhenAll(comPorts.Select(async x =>
                            (Port: x, Devices: await StepMotorHandler.FindDevice(x, 1, 4))));
                    var preferredPortName =
                        UiSettingsProvider.Settings.Get(@"PolarimeterMotorComPort", "COM1").ToUpperInvariant();

                    if (_foundSerialDevices.FirstOrOptional(x => x.Port.ToUpperInvariant() == preferredPortName) is
                            var ports
                        && ports.HasValue
                        && ports.Value.Devices.Count > 0)
                    {
                        var address = ports.Value.Devices.First();
                        PolarimeterMotor = new StepMotorHandler(preferredPortName, address).DisposeWith(Subscriptions);
                    }

                }
                catch (Exception)
                {
                    // TODO: maybe handle
                    // Ignored
                }
                finally
                {
                    Application.Current?.Dispatcher.InvokeAsync(() => IsSerialPortTaskCompleted = true);
                }
            });
        }

        private void InitializeCommands()
        {

            ContextMenuCommand =
                ReactiveCommand.CreateFromObservable<string, Unit>(
                                   x => Observable.FromAsync(_ => ContextMenuCommandExecuteAsync(x)),
                                   _connectedCameras.CountChanged.Select(x => x != 0)
                                                    .DistinctUntilChanged()
                                                    .ObserveOnUi())
                               .DisposeWith(Subscriptions);

            SelectCameraCommand =
                ReactiveCommand.Create<string>(
                                   SelectCameraCommandExecute,
                                   _connectedCameras.CountChanged.Select(x => x != 0)
                                                    .DistinctUntilChanged()
                                                    .ObserveOnUi())
                               .DisposeWith(Subscriptions);

            WindowLoadedCommand =
                ReactiveCommand.Create<Unit, Unit>(_ => Unit.Default)
                               .DisposeWith(Subscriptions);

            ConnectButtonCommand =
                ReactiveCommand.Create<object, AvailableCamerasModel>(
                                   _ =>
                                   {
                                       var camQueryModel = new AvailableCamerasModel(
                                           _remoteClients);

                                       return camQueryModel;
                                   },
                                   this.WhenAnyPropertyChanged(nameof(CanConnect))
                                       .Select(x => x.CanConnect)
                                       .DistinctUntilChanged()
                                       .ObserveOnUi())
                               .DisposeWith(Subscriptions);

            DisconnectButtonCommand =
                ReactiveCommand.Create(
                                   DisconnectButtonCommandExecute,
                                   SelectedDevices.CountChanged.Select(x => x != 0)
                                                  .DistinctUntilChanged()
                                                  .ObserveOnUi())
                               .DisposeWith(Subscriptions);

            SelectAllCamerasCommand =
                ReactiveCommand.Create(
                                   SelectAllCamerasCommandExecute,
                                   _connectedCameras.CountChanged.Select(x => x != 0)
                                                    .DistinctUntilChanged()
                                                    .ObserveOnUi())
                               .DisposeWith(Subscriptions);

            ProgressBarProvider = new DescendantProvider(
                    ReactiveCommand.CreateFromTask<object, ReactiveObjectEx>(
                        _ => Task.Run<ReactiveObjectEx>(() =>
                            new ProgressBar()
                            {
                                Minimum = 0,
                                Maximum = _remoteClients?.Length ?? 1,
                                Value = 0,
                                IsIndeterminate = true,
                                CanAbort = false,
                                BarTitle = Properties.Localization.MainWindow_ConnectingToRemoteLocations
                            })),
                    null,
                    null,
                    ReactiveCommand.Create<ReactiveObjectEx>(x => x.Dispose()))
                .DisposeWith(Subscriptions);

            AvailableCamerasProvider = new DescendantProvider(
                    ReactiveCommand.Create<object, ReactiveObjectEx>(x => (ReactiveObjectEx) x),
                    null,
                    null,
                    ReactiveCommand.CreateFromTask<ReactiveObjectEx>(async x =>
                    {
                        await ReceiveConnectedCameras((AvailableCamerasModel) x).ExpectCancellation();
                        x.Dispose();
                    }))
                .DisposeWith(Subscriptions);



            PolarimeterMotorButtonCommand =
                ReactiveCommand.CreateFromTask(CheckStepMotorStatus,
                                   this.WhenPropertyChanged(x => x.IsSerialPortTaskCompleted)
                                       .Select(x => x.Value))
                               .DisposeWith(Subscriptions);
        }

        private void HookObservables()
        {
            Observable.Merge(
                          ProgressBarProvider.ViewRequested.Select(_ => false),
                          ProgressBarProvider.ViewFinished.Select(_ => true),
                          AvailableCamerasProvider.ViewRequested.Select(_ => false),
                          AvailableCamerasProvider.ViewFinished.Select(_ => true))
                      .ToPropertyEx(this, x => x.CanConnect)
                      .DisposeWith(Subscriptions);


            _connectedCameras.Connect()
                             .DisposeManyEx(async x => await DisposeCamera(x.Camera))
                             .Subscribe()
                             .DisposeWith(Subscriptions);

            ConnectedCameras = _connectedCameras
                               .AsObservableCache()
                               .DisposeWith(Subscriptions);



            CameraTabs = _connectedCameras.Connect()
                                          .Transform(x => (x.Id, Tab: new CameraTab(x.Camera)))
                                          .DisposeManyEx(x => x.Tab?.Dispose())
                                          .AsObservableCache()
                                          .DisposeWith(Subscriptions);

            WindowLoadedCommand
                .InvokeCommand(ProgressBarProvider.ViewRequested)
                .DisposeWith(Subscriptions);

            ProgressBarProvider.ViewRequested.Select(x =>
                    Observable.FromAsync(async () => await InitializeRemoteSessionsAsync((ProgressBar) x)))
                .Merge()
                .CombineLatest(ProgressBarProvider.WindowShown,
                    (x, y) => x)
                .Delay(TimeSpan.Parse(UiSettingsProvider.Settings.Get("PopUpDelay", "00:00:00.750")))
                .Subscribe(async x =>
                {
                    await ProgressBarProvider.ClosingRequested.Execute();

                    foreach (var ex in x.Take(3))
                    {
                        Helper.ExecuteOnUi(() => MessageBox.Show(
                            ex.Message,
                            Properties.Localization.RemoteConnection_UnreachableHostTitle,
                            MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK));
                    }
                    if (x.Count > 3)
                        Helper.ExecuteOnUi(() => MessageBox.Show(
                            string.Format(Properties.Localization.MB_MoreLeft, x.Count - 3),
                            Properties.Localization.RemoteConnection_UnreachableHostTitle,
                            MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK));
                })
                .DisposeWith(Subscriptions);

            ConnectButtonCommand.InvokeCommand(AvailableCamerasProvider.ViewRequested as ICommand)
                                .DisposeWith(Subscriptions);

            AvailableCamerasProvider.WindowShown.WithLatestFrom(
                                        AvailableCamerasProvider.ViewRequested,
                                        (x, y) => y)
                                    .Subscribe(async x =>
                                        await QueryCamerasAsync((AvailableCamerasModel) x)
                                            .ExpectCancellation())
                                    .DisposeWith(Subscriptions);

        }

        private void SelectAllCamerasCommandExecute()
        {
            if (SelectedDevices.Count < ConnectedCameras.Count)
                SelectedDevices.Edit(context =>
                {
                    context.Clear();
                    context.AddRange(ConnectedCameras.Keys);
                });
            else
                SelectedDevices.Clear();
        }

        private void SelectCameraCommandExecute(string param)
        {
            SelectedDevices.Edit(context =>
            {
                if (context.Contains(param))
                    context.Remove(param);
                else
                    context.Add(param);
            });
        }

        private async Task ContextMenuCommandExecuteAsync(string param)
        {
            var result = await Helper.RunNoMarshall(() => ConnectedCameras.Lookup(param));
            if (result.HasValue)
            {
                var vm = await Helper.RunNoMarshall(() => new CameraPropertiesViewModel(result.Value.Camera));
                var view = Helper.ExecuteOnUi(() => new CameraPropertiesView()
                        {WindowStartupLocation = WindowStartupLocation.CenterScreen}
                    .WithDataContext(vm));

                Helper.ExecuteOnUi(view.ShowDialog);

                await Helper.RunNoMarshall(vm.Dispose);
            }
        }

        private async Task<List<Exception>> InitializeRemoteSessionsAsync(ProgressBar pb)
        {
            var tasks = _remoteLocations.Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => Task.Run(() =>
                {
                    try
                    {
                        var client = DipolClient.Create(new Uri(x),
                            TimeSpan.Parse(UiSettingsProvider.Settings.Get("RemoteOpenTimeout", "00:00:30")),
                            TimeSpan.Parse(UiSettingsProvider.Settings.Get("RemoteSendTimeout", "00:00:30")),
                            TimeSpan.Parse(UiSettingsProvider.Settings.Get("RemoteCloseTimeout", "00:00:30")));
                        client.Connect();

                        pb.TryIncrement();
                        pb.BarComment = string.Format(Properties.Localization.MainWindow_RemoteConnection_ClientCount,
                            pb.Value, _remoteLocations.Length);

                        return client;
                    }
                    //catch (System.ServiceModel.EndpointNotFoundException endpointException)
                    //{
                    //    Helper.WriteLog(endpointException.Message);
                    //    Helper.ExecuteOnUi(() => MessageBox.Show(
                    //        endpointException.Message,
                    //        Properties.Localization.RemoteConnection_UnreachableHostTitle,
                    //        MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK));
                    //}
                    catch (Exception e)
                    {
                        Helper.WriteLog(e.Message);
                        //Helper.ExecuteOnUi(() => MessageBox.Show(
                        //    e.Message,
                        //    Properties.Localization.RemoteConnection_UnreachableHostTitle,
                        //    MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK));
                        return (object)e;
                    }

                }));


            var result = await Task.WhenAll(tasks).ConfigureAwait(false);
            _remoteClients = result.OfType<DipolClient>().ToArray();

            var exceptions = result.OfType<Exception>().ToList();

             pb.BarComment = string.Format(Properties.Localization.MainWindow_RemoteConnection_ClientCount,
                _remoteClients.Length, _remoteLocations.Length);

             return exceptions;
        }

        private async Task ReceiveConnectedCameras(AvailableCamerasModel model)
        {
            var cams = model.RetrieveSelectedDevices();

            if (cams.Count > 0)
                _connectedCameras.Edit(context => { context.AddOrUpdate(cams); });

            await PrepareCamerasAsync(cams.Select(x => x.Camera));
        }

        private async Task CheckStepMotorStatus()
        {
            try
            {
                await (PolarimeterMotor?.GetActualPositionAsync() ?? Task.FromResult(0));
            }
            catch (Exception)
            {
                PolarimeterMotor?.Dispose();
                PolarimeterMotor = null;
            }
            if (!(PolarimeterMotor is null))
            {
                MessageBox.Show(
                    string.Format(Properties.Localization.MainWindow_MB_PolarimeterMotorOK_Text,
                        PolarimeterMotor.PortName,
                        PolarimeterMotor.Address),
                    Properties.Localization.MainWindow_MB_PolarimeterMotorOK_Caption,
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                if (!(_serialPortScanningTask is null)
                    && !_serialPortScanningTask.IsFaulted
                    && (_foundSerialDevices?.Any() ?? false))
                {
                    var response = MessageBox.Show(
                        string.Format(Properties.Localization.MainWindow_MB_PolarimeterMotorNotFound_Text,
                            UiSettingsProvider.Settings.Get(@"PolarimeterMotorComPort", "COM1").ToUpperInvariant()),
                        Properties.Localization.MainWindow_MB_PolarimeterMotorNotFound_Caption,
                        MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (response == MessageBoxResult.Yes)
                        _serialPortScanningTask = CheckStepMotors();
                }
                else
                {
                    MessageBox.Show(
                        Properties.Localization.MainWindow_MB_PolarimeterMotorFailure_Text,
                        Properties.Localization.MainWindow_MB_PolarimeterMotorFailure_Caption,
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DisconnectButtonCommandExecute()
        {
            foreach (var id in SelectedDevices.Items.ToList())
            {
                SelectedDevices.Remove(id);
                _connectedCameras.RemoveKey(id);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed && !IsDisposing)
                if (disposing)
                {
                    // This one overrides on [Disconnect] disposal,
                    // as it is "fire & forget", and time-consuming.
                    // When window is closed, the unobserved disposals take some time
                    // and can use client infrastructure when client has been disconnected.
                    // [IsDisposing] overrides that behaviour, and each camera is removed & disposed
                    // individually and synchronously.
                    IsDisposing = true;
                    var keys = _connectedCameras.Keys.ToList();
                    foreach (var key in keys)
                    {
                        var val = _connectedCameras.Lookup(key);
                        if (val.HasValue)
                        {
                            _connectedCameras.Remove(val.Value.Id);
                            if(!val.Value.Camera.IsDisposed)
                                val.Value.Camera.Dispose();
                        }
                    }

                    if (!(_remoteClients is null))
                        Parallel.ForEach(_remoteClients, (client) =>
                        {
                            try
                            {
                                client?.Disconnect();
                                client?.Dispose();
                            }
                            catch (Exception)
                            {
                                // TODO : may be important
                                // Ignored
                            }
                        });
                    IsDisposing = false;
                    base.Dispose(true);
                }
        }

        private static async Task<AvailableCamerasModel> QueryCamerasAsync(AvailableCamerasModel model)
        {
            (await Helper.RunNoMarshall(() => model
                                              .QueryCamerasCommand.Execute()))
                .Subscribe(_ => { }, () => { });

            return model;
        }

        private static async Task PrepareCamerasAsync(IEnumerable<CameraBase> cams)
        {
            await Helper.RunNoMarshall(() =>
            {
                foreach (var cam in cams)
                {
                    if (cam.Capabilities.GetFunctions.HasFlag(GetFunction.Temperature))
                        cam.TemperatureMonitor(Switch.Enabled,
                            (int) (TimeSpan.Parse(UiSettingsProvider.Settings.Get("UICamStatusUpdateDelay", "00:00:01"))
                                           .TotalMilliseconds));

                    if(cam.Capabilities.Features.HasFlag(SdkFeatures.ShutterEx) ||
                       cam.Capabilities.Features.HasFlag(SdkFeatures.Shutter))
                        cam.ShutterControl(ShutterMode.PermanentlyOpen, ShutterMode.PermanentlyOpen);
                }
            });
        }

        private async Task DisposeCamera(CameraBase cam)
            => await Helper.RunNoMarshall(() =>
            {
                // This one only works if a camera is disposed through 
                // removal from collection - as in "user pressed Disconnect"
                if (cam?.IsDisposed == false && !IsDisposing)
                {
                    cam.CoolerControl(Switch.Disabled);
                    cam.TemperatureMonitor(Switch.Disabled);
                    cam.Dispose();
                }
            });
    }
}
