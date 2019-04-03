using System;
using System.IO;
using System.Net;
using System.Reactive;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Input;
using DIPOL_UF.Jobs;
using Microsoft.Xaml.Behaviors.Core;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace DIPOL_UF.ViewModels
{
    internal sealed class JobSettingsViewModel : ReactiveViewModel<ReactiveWrapper<Target>>
    {
        public event EventHandler FileDialogRequested;

        public event EventHandler BrowseJobSettingsRequested;
        public event EventHandler BrowseAcquisitionSettingsRequested;


        public ReactiveCommand<string, Unit> SaveActionCommand { get; private set; }
        public ReactiveCommand<string, Unit> LoadActionCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        public ReactiveCommand<Unit, FileDialogDescriptor> BrowseAcquisitionCommand { get; private set; }
        public ReactiveCommand<Unit, FileDialogDescriptor> BrowseJobCommand { get; private set; }
        public ReactiveCommand<string, Unit> JobReceivedCommand { get; private set; }
        public ReactiveCommand<string, Unit> AcquisitionReceivedCommand { get; private set; }


        [Reactive]
        public string ObjectName { get; set; }

        [Reactive]
        public string JobPath { get; set; }

        [Reactive]
        public string SettingsPath { get; set; }

        public JobSettingsViewModel(ReactiveWrapper<Target> model) : base(model)
        {
            InitializeCommands();
            HookObservables();
        }

        private void InitializeCommands()
        {
            CancelCommand = new ActionCommand(x => (x as Window)?.Close());

            BrowseJobCommand = ReactiveCommand.Create(() => new FileDialogDescriptor()
            {
                DefaultExtenstion = @".job",
                InitialDirectory = null,
                Mode = FileDialogDescriptor.DialogMode.Load,
                Title = Properties.Localization.JobSettings_Dialog_Job,
                FileName = string.Empty
            }).DisposeWith(Subscriptions);

            BrowseAcquisitionCommand = ReactiveCommand.Create(() => new FileDialogDescriptor()
            {
                DefaultExtenstion = @".acq",
                InitialDirectory = null,
                Mode = FileDialogDescriptor.DialogMode.Load,
                Title = Properties.Localization.JobSettings_Dialog_Acquisition,
                FileName = string.Empty
            }).DisposeWith(Subscriptions);


            JobReceivedCommand = ReactiveCommand.Create<string>(x =>
            {
                if (File.Exists(x))
                    JobPath = Path.GetFullPath(x);
            }).DisposeWith(Subscriptions);

            AcquisitionReceivedCommand = ReactiveCommand.Create<string>(x =>
            {
                if (File.Exists(x))
                    SettingsPath = Path.GetFullPath(x);
            }).DisposeWith(Subscriptions);


        }

        private void HookObservables()
        {
            BrowseJobCommand.Subscribe(OnBrowseJobSettingsRequested).DisposeWith(Subscriptions);
            BrowseAcquisitionCommand.Subscribe(OnBrowseAcquisitionSettingsRequested).DisposeWith(Subscriptions);
        }

        private void OnBrowseJobSettingsRequested(FileDialogDescriptor e)
            => BrowseJobSettingsRequested?.Invoke(this, new DialogRequestedEventArgs(e));

        private void OnBrowseAcquisitionSettingsRequested(FileDialogDescriptor e)
            => BrowseAcquisitionSettingsRequested?.Invoke(this, new DialogRequestedEventArgs(e));
    }
}
