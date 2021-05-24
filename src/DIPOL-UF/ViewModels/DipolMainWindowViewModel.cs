using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using DIPOL_UF.Annotations;
using DIPOL_UF.Enums;
using DIPOL_UF.Jobs;
using DIPOL_UF.Models;
using DIPOL_UF.UserNotifications;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using MessageBox = System.Windows.MessageBox;

// ReSharper disable UnassignedGetOnlyAutoProperty

namespace DIPOL_UF.ViewModels
{
    internal sealed class DipolMainWindowViewModel : ReactiveViewModel<DipolMainWindow>
    {

        public IObservableCollection<MainWindowTreeViewModel> CameraPanel { get; }
            = new ObservableCollectionExtended<MainWindowTreeViewModel>();

        public IObservableCollection<CameraTabViewModel> CameraTabs { get; }
            = new ObservableCollectionExtended<CameraTabViewModel>();

        public bool HasPolarimeterMotor { [ObservableAsProperty] get; }
        public bool HasRetractorMotor { [ObservableAsProperty] get; }


        public bool AnyCameraConnected { [ObservableAsProperty] get; }
        public bool? AllCamerasSelected { [ObservableAsProperty] get; }
        public ICommand SelectAllCamerasCommand => Model.SelectAllCamerasCommand;
        public ICommand ConnectButtonCommand => Model.ConnectButtonCommand;
        public ICommand DisconnectButtonCommand => Model.DisconnectButtonCommand;
        public ICommand WindowLoadedCommand => Model.WindowLoadedCommand;
        public ICommand PolarimeterMotorButtonCommand => Model.PolarimeterMotorButtonCommand;
        public ICommand RetractorMotorButtonCommand => Model.RetractorMotorButtonCommand;
        
        public ICommand WindowClosingCommand { get; private set; }
        public bool CanSwitchRegime { [ObservableAsProperty] get; }
        public InstrumentRegime ActualRegime { [ObservableAsProperty] get; }
        
        [Reactive] public int Regime { get; set; }

        public string ApplicationName 
        {
            get
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                var version = asm.GetName().Version;
                return $"{Properties.Localization.ApplicationName} v{version.Major}.{version.Minor}";
            }
        }



        public DescendantProxy ProgressBarProxy { get; }
        public DescendantProxy AvailableCamerasProxy { get; }
        public DescendantProxy RegimeSwitchProxy { get; }

    public DipolMainWindowViewModel(DipolMainWindow model) : base(model)
        {
            ProgressBarProxy = new DescendantProxy(
                Model.ProgressBarProvider,
                x => new ProgressBarViewModel((ProgressBar)x))
                .DisposeWith(Subscriptions);
                
            AvailableCamerasProxy = new DescendantProxy(
                Model.AvailableCamerasProvider,
                x => new AvailableCamerasViewModel((AvailableCamerasModel)x))
                .DisposeWith(Subscriptions);

            RegimeSwitchProxy = new DescendantProxy(Model.RegimeSwitchProvider,
                    x => new ProgressBarViewModel((ProgressBar) x))
                .DisposeWith(Subscriptions);

            WindowClosingCommand =
                ReactiveCommand.Create<(object Sender, object Args)>(
                                   x => OverrideWindowClosing(x.Sender, x.Args as CancelEventArgs)
                               )
                               .DisposeWith(Subscriptions);

            HookObservables();
            HookValidators();
        }

        private void HookObservables()
        {
            this.WhenPropertyChanged(x => x.Regime, false)
                .Select(x => x.Value == 1 ? InstrumentRegime.Polarimeter : InstrumentRegime.Photometer)
                .Where(x => x != Model.Regime && CanSwitchRegime)
                .InvokeCommand(Model.ChangeRegimeCommand)
                .DisposeWith(Subscriptions);


            Model.WhenPropertyChanged(x => x.PolarimeterMotor)
                 .Select(x => x.Value != null)
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.HasPolarimeterMotor);

            Model.WhenPropertyChanged(x => x.RetractorMotor)
                .Select(x => x.Value != null)
                .ObserveOnUi()
                .ToPropertyEx(this, x => x.HasRetractorMotor);

            Model.ConnectedCameras.CountChanged
                 .Select(x => x != 0)
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.AnyCameraConnected)
                 .DisposeWith(Subscriptions);

            Model.SelectedDevices.CountChanged
                 .CombineLatest(
                     Model.ConnectedCameras.CountChanged,
                     (x, y) =>
                         x == 0
                             ? false
                             : x < y
                                 ? null
                                 : new bool?(true))
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.AllCamerasSelected)
                 .DisposeWith(Subscriptions);


            Model.ConnectedCameras.Connect()
                 .Group(x => Helper.GetCameraHostName(x.Id))
                 .ObserveOnUi()
                 .Transform(x =>
                     new MainWindowTreeViewModel(x.Key, x.Cache,
                         Model.SelectedDevices,
                         Model.SelectCameraCommand,
                         Model.ContextMenuCommand))
                 .Bind(CameraPanel)
                 .DisposeMany()
                 .Subscribe()
                 .DisposeWith(Subscriptions);

            Model.CameraTabs.Connect()
                 .ObserveOnUi()
                 .Transform(x => new CameraTabViewModel(x.Tab))
                 .Bind(CameraTabs)
                 .DisposeMany()
                 .Subscribe()
                 .DisposeWith(Subscriptions);

            Model.WhenPropertyChanged(x => x.Regime)
                .Select(x => x.Value != InstrumentRegime.Unknown)
                .CombineLatest(
                    JobManager.Manager.WhenPropertyChanged(y => y.AnyCameraIsAcquiring).Select(y => !y.Value), 
                    JobManager.Manager.WhenPropertyChanged(z => z.IsInProcess).Select(z => !z.Value),
                    (x, y, z) => x && y && z)
                .ObserveOnUi()
                .ToPropertyEx(this, x => x.CanSwitchRegime)
                .DisposeWith(Subscriptions);


            Model.WhenPropertyChanged(x => x.Regime)
                .Where(x => x.Value != InstrumentRegime.Unknown)
                .Select(x => x.Value == InstrumentRegime.Polarimeter ? 1 : 2)
                .ObserveOnUi()
                .BindTo(this, x => x.Regime)
                .DisposeWith(Subscriptions);

            PropagateReadOnlyProperty(this, x => x.Regime, y => y.ActualRegime);

        }


        private static void OverrideWindowClosing([CanBeNull] object source, [CanBeNull] CancelEventArgs args)
        {
            var caption = "Sample header";
            var message = "Sample message";

            if (args is null)
            {
                return;
            }

            var notifier = Injector.Locate<IUserNotifier>();
            
            if (notifier.YesNo(message, caption) is not YesNoResult.Yes)
            {
                args.Cancel = true;
            }
        }
    }
}
