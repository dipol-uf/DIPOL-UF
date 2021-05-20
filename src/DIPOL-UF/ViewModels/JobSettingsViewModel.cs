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

using System;
using System.Globalization;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DIPOL_UF.Jobs;
using DynamicData.Binding;
using Microsoft.Xaml.Behaviors.Core;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace DIPOL_UF.ViewModels
{
    [Obsolete]
    internal sealed class JobSettingsViewModel : ReactiveViewModel<ReactiveWrapper<Target>>
    {
        public event EventHandler FileDialogRequested;

        public event EventHandler BrowseDarkSettingsRequested;
        public event EventHandler BrowseBiasSettingsRequested;
        public event EventHandler BrowseJobSettingsRequested;
        public event EventHandler BrowseAcquisitionSettingsRequested;


        public ReactiveCommand<Unit, FileDialogDescriptor> SaveButtonCommand { get; private set; }
        public ReactiveCommand<Unit, FileDialogDescriptor> LoadButtonCommand { get; private set; }

        public ReactiveCommand<Window, Unit> SubmitCommand { get; private set; }
        public ReactiveCommand<string, Unit> SaveActionCommand { get; private set; }
        public ReactiveCommand<string, Unit> LoadActionCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        public ReactiveCommand<Unit, FileDialogDescriptor> BrowseAcquisitionCommand { get; private set; }
        public ReactiveCommand<Unit, FileDialogDescriptor> BrowseJobCommand { get; private set; }
        public ReactiveCommand<Unit, FileDialogDescriptor> BrowseBiasCommand { get; private set; }
        public ReactiveCommand<Unit, FileDialogDescriptor> BrowseDarkCommand { get; private set; }

        public ReactiveCommand<string, Unit> JobReceivedCommand { get; private set; }
        public ReactiveCommand<string, Unit> BiasReceivedCommand { get; private set; }
        public ReactiveCommand<string, Unit> DarkReceivedCommand { get; private set; }


        public ReactiveCommand<string, Unit> AcquisitionReceivedCommand { get; private set; }


        [Reactive]
        public string ObjectName { get; set; }

        [Reactive]
        public string JobPath { get; set; }
        [Reactive]
        public string BiasPath { get; set; }
        [Reactive]
        public string DarkPath { get; set; }
        [Reactive]
        public string SettingsPath { get; set; }
        [Reactive]
        public string Repeats { get; set; }

        public JobSettingsViewModel(ReactiveWrapper<Target> model) : base(model)
        {
            InitializeCommands();
            HookObservables();
            UpdateBindingsFromModel();
        }

        private void UpdateBindingsFromModel()
        {
            SettingsPath = Model.Object.SettingsPath;
            ObjectName = Model.Object.TargetName;
            JobPath = Model.Object.JobPath;
            BiasPath = Model.Object.BiasPath ?? string.Empty;
            DarkPath = Model.Object.DarkPath ?? string.Empty;
            Repeats = Model.Object.Repeats.ToString(Properties.Localization.General_IntegerFormat);
        }

        private void UpdateBindingsToModel()
        {
            Model.Object.SettingsPath = SettingsPath;
            Model.Object.TargetName = ObjectName;
            Model.Object.JobPath = JobPath;
            Model.Object.BiasPath = string.IsNullOrWhiteSpace(BiasPath) ? null : BiasPath;
            Model.Object.DarkPath = string.IsNullOrWhiteSpace(DarkPath) ? null : DarkPath;
            Model.Object.Repeats = int.TryParse(Repeats, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var rep)
                ? rep
                : 1;
        }

        private void InitializeCommands()
        {
            SubmitCommand =
                ReactiveCommand.CreateFromTask<Window>(
                                   Submit,
                                   ObserveHasErrors.Select(x => !x))
                               .DisposeWith(Subscriptions);

            CancelCommand = new ActionCommand(x =>
            {   
                (x as Window)?.Close();
            });

            SaveButtonCommand= ReactiveCommand.Create(() => new FileDialogDescriptor()
            {
                DefaultExtenstion = @".star",
                InitialDirectory = null,
                Mode = FileDialogDescriptor.DialogMode.Save,
                Title = Properties.Localization.JobSettings_Dialog_Save,
                FileName = string.Empty
            }).DisposeWith(Subscriptions);

            LoadButtonCommand = ReactiveCommand.Create(() => new FileDialogDescriptor()
            {
                DefaultExtenstion = @".star",
                InitialDirectory = null,
                Mode = FileDialogDescriptor.DialogMode.Load,
                Title = Properties.Localization.JobSettings_Dialog_Load,
                FileName = string.Empty
            }).DisposeWith(Subscriptions);

            BrowseJobCommand = ReactiveCommand.Create(() => new FileDialogDescriptor()
            {
                DefaultExtenstion = @".job",
                InitialDirectory = null,
                Mode = FileDialogDescriptor.DialogMode.Load,
                Title = Properties.Localization.JobSettings_Dialog_Job,
                FileName = string.Empty
            }).DisposeWith(Subscriptions);

            BrowseBiasCommand = ReactiveCommand.Create(() => new FileDialogDescriptor()
            {
                DefaultExtenstion = @".bias",
                InitialDirectory = null,
                Mode = FileDialogDescriptor.DialogMode.Load,
                Title = Properties.Localization.JobSettings_Dialog_Job,
                FileName = string.Empty
            }).DisposeWith(Subscriptions);

            BrowseDarkCommand = ReactiveCommand.Create(() => new FileDialogDescriptor()
            {
                DefaultExtenstion = @".dark",
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

            BiasReceivedCommand = ReactiveCommand.Create<string>(x =>
            {
                if (File.Exists(x))
                    BiasPath = Path.GetFullPath(x);
            }).DisposeWith(Subscriptions);

            DarkReceivedCommand = ReactiveCommand.Create<string>(x =>
            {
                if (File.Exists(x))
                    DarkPath = Path.GetFullPath(x);
            }).DisposeWith(Subscriptions);

            AcquisitionReceivedCommand = ReactiveCommand.Create<string>(x =>
            {
                if (File.Exists(x))
                    SettingsPath = Path.GetFullPath(x);
            }).DisposeWith(Subscriptions);

            SaveActionCommand = ReactiveCommand.CreateFromTask<string>(async x =>
            {
                if (string.IsNullOrEmpty(x))
                    return;
                UpdateBindingsToModel();
                // WATCH : Fixed overwriting issue
                using (var str = new FileStream(x, FileMode.Create, FileAccess.Write))
                    await Model.Object.Serialize(str);
            }).DisposeWith(Subscriptions);

            LoadActionCommand = ReactiveCommand.CreateFromTask<string>(async x =>
            {
                if (File.Exists(x))
                {
                    using (var str = new FileStream(x, FileMode.Open, FileAccess.Read))
                        await Model.Object.Deserialize(str);
                    Model.Object.Repeats = 0;
                    UpdateBindingsFromModel();
                }
            }).DisposeWith(Subscriptions);
        }

        private void HookObservables()
        {
            HandleInputOfRepeats();

            SaveButtonCommand.Subscribe(OnFileDialogRequested).DisposeWith(Subscriptions);
            LoadButtonCommand.Subscribe(OnFileDialogRequested).DisposeWith(Subscriptions);
            
            BrowseJobCommand.Subscribe(OnBrowseJobSettingsRequested).DisposeWith(Subscriptions);
            BrowseBiasCommand.Subscribe(OnBrowseBiasSettingsRequested).DisposeWith(Subscriptions);
            BrowseDarkCommand.Subscribe(OnBrowseDarkSettingsRequested).DisposeWith(Subscriptions);

            BrowseAcquisitionCommand.Subscribe(OnBrowseAcquisitionSettingsRequested).DisposeWith(Subscriptions);
        }

        private void HandleInputOfRepeats()
        {
            var source = this.WhenAnyPropertyChanged(nameof(Repeats)).Select(x => x.Repeats);

            CreateValidator(
                source.Select(x => (
                        Type: nameof(Validators.Validate.CannotBeDefault),
                        Message: Validators.Validate.CannotBeDefault(x))),
                nameof(Repeats));

            source.Subscribe(x =>
            {
                var canBeParsed = Validators.Validate.CanBeParsed(x, out int val);
                var isWithin = canBeParsed is null ? 
                    // WATCH : constant probably should be moved to settings
                    Validators.Validate.ShouldFallWithinRange(val, 1, 16)
                    : null;
                BatchUpdateErrors(
                    (nameof(Repeats), nameof(Validators.Validate.CanBeParsed), canBeParsed),
                    (nameof(Repeats), nameof(Validators.Validate.ShouldFallWithinRange), isWithin));
                
            }).DisposeWith(Subscriptions);
        }

        private async Task Submit(Window w)
        {
            try
            {
                UpdateBindingsToModel();
                await JobManager.Manager.SubmitNewTarget(Model.Object);
                Helper.ExecuteOnUi(() => w?.Close());
            }
            catch (FileNotFoundException fileExcept)
            {
                MessageBox.Show(
                    string.Format(Properties.Localization.JobManager_Error_FileNotFound, fileExcept.FileName),
                    Properties.Localization.JobManager_Error_Caption,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (InvalidOperationException invalidExcept)
            {
                MessageBox.Show(
                    string.Format(Properties.Localization.JobManager_Error_InvalidOp, invalidExcept.Message),
                    Properties.Localization.JobManager_Error_Caption,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    string.Format(Properties.Localization.JobManager_Error_Other, e.Message),
                    Properties.Localization.JobManager_Error_Caption,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OnBrowseJobSettingsRequested(FileDialogDescriptor e)
            => BrowseJobSettingsRequested?.Invoke(this, new DialogRequestedEventArgs(e));
        private void OnBrowseBiasSettingsRequested(FileDialogDescriptor e)
            => BrowseBiasSettingsRequested?.Invoke(this, new DialogRequestedEventArgs(e));
        private void OnBrowseDarkSettingsRequested(FileDialogDescriptor e)
            => BrowseDarkSettingsRequested?.Invoke(this, new DialogRequestedEventArgs(e));
        private void OnBrowseAcquisitionSettingsRequested(FileDialogDescriptor e)
            => BrowseAcquisitionSettingsRequested?.Invoke(this, new DialogRequestedEventArgs(e));

        private void OnFileDialogRequested(FileDialogDescriptor e) 
            => FileDialogRequested?.Invoke(this, new DialogRequestedEventArgs(e));
    }
}
