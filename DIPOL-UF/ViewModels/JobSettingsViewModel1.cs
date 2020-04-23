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
#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using ANDOR_CS;
using ANDOR_CS.Classes;
using ANDOR_CS.Enums;
using DIPOL_UF.Converters;
using DIPOL_UF.Enums;
using DIPOL_UF.Jobs;
using DIPOL_UF.Models;
using DynamicData;
using DynamicData.Binding;
using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace DIPOL_UF.ViewModels
{
    internal sealed class JobSettingsViewModel1 : ReactiveViewModel<ReactiveWrapper<Target1>>
    {
        public sealed class PerCameraSettingItem : INotifyPropertyChanged, INotifyDataErrorInfo, IDisposable
        { 
            private readonly Dictionary<string, string> _valueErrors = new Dictionary<string, string>();
            private readonly CompositeDisposable _subscriptions = new CompositeDisposable();
            private string _value = string.Empty;
            public string CameraName { get; }

            public IObservable<bool> ObserveHasErrors { get; } 
            public bool HasErrors => _valueErrors.Any();
            public string Value
            {
                get => _value;
                set
                {
                    value ??= string.Empty;
                    if (value == _value) return;
                    _value = value;
                    RaisePropertyChanged();
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

            public PerCameraSettingItem(string camera, string value)
            {
                (CameraName, Value) = (camera, value);
                this.WhenPropertyChanged(x => x.Value)
                    .Select(x => x.Value)
                    .Subscribe(ValidateValue)
                    .DisposeWith(_subscriptions);

                ObserveHasErrors = Observable
                    .FromEventPattern<DataErrorsChangedEventArgs>(x => ErrorsChanged += x, x => ErrorsChanged -= x)
                    .Select(_ => HasErrors);

                RaiseErrorsChanged(nameof(Value));
            }

            private void ValidateValue(string s)
            {
                if (string.IsNullOrWhiteSpace(s))
                {
                    _valueErrors.Remove(nameof(Validators.Validate.CanBeParsed));
                    _valueErrors.Remove(nameof(Validators.Validate.CannotBeLessThan));

                }
                else
                {
                    var m1 = Validators.Validate.CanBeParsed(s, out float f);
                    if (m1 is { })
                    {
                        _valueErrors[nameof(Validators.Validate.CanBeParsed)] = m1;
                        _valueErrors.Remove(nameof(Validators.Validate.CannotBeLessThan));
                    }
                    else
                    {
                        var m2 = Validators.Validate.CannotBeLessThan(f, 0f);
                        _valueErrors.Remove(nameof(Validators.Validate.CanBeParsed));
                        if (m2 is { })
                            _valueErrors[nameof(Validators.Validate.CannotBeLessThan)] = m2;
                        else
                            _valueErrors.Remove(nameof(Validators.Validate.CannotBeLessThan));
                    }
                }
                RaiseErrorsChanged(nameof(Value));
            }

            private void RaisePropertyChanged([CallerMemberName] string? propertyName = null) => 
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            private void RaiseErrorsChanged([CallerMemberName] string? propertyName = null) =>
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));

            public IEnumerable GetErrors(string propertyName) =>
                propertyName switch
                {
                    nameof(Value) => _valueErrors.Values,
                    _ => Enumerable.Empty<object>()
                };

            public void Dispose() 
                => _subscriptions?.Dispose();
        }

        public sealed class CollectionItem
        {
            public string SettingsName { get; }
            public string? Value { get; }

            public bool IsOverriden { get; }
            public bool IsNotSpecified => Value is null && !IsOverriden;
            public CollectionItem(string name, string? value, bool isOverriden = false)
                => (SettingsName, Value, IsOverriden) = (name, value, isOverriden);
        }


        private bool _closedUsingButton;
        private DescendantProvider? _acqSettsProvider;
        private readonly IDevice _firstCamera;
        private readonly ISourceList<CollectionItem> _propList;
        private readonly ISourceCache<PerCameraSettingItem, string> _exposureList;

        public event EventHandler? FileDialogRequested;

        public DescendantProxy AcquisitionSettingsProxy { get; }

        public ReactiveCommand<Window, Unit> CancelButtonCommand { get; private set; }
        public ReactiveCommand<Window, Window>? SaveAndSubmitButtonCommand { get; private set; }
        public ReactiveCommand<Unit, Unit>? LoadButtonCommand { get; private set; }

        public ReactiveCommand<Unit, Unit>? CreateNewButtonCommand { get; private set; }

        public ReactiveCommand<string, bool>? SaveActionCommand { get; private set; }
        public ReactiveCommand<string, Unit>? LoadActionCommand { get; private set; }

        public ReactiveCommand<object, Unit> WindowClosingCommand { get; private set; }

        public ReadOnlyObservableCollection<CollectionItem> SharedSettingsView { get; }
        public ReadOnlyObservableCollection<PerCameraSettingItem> PerCameraSettingsView { get; }

        [Reactive]
        public string? ObjectName { get; set; }

        [Reactive]
        public string? Description { get; set; }

        [Reactive]
        public CycleType CycleType { get; set; }

        public JobSettingsViewModel1(ReactiveWrapper<Target1> model) : base(model)
        {
            _firstCamera = JobManager.Manager.ConnectedCameras.Items.First().Item2;

            _propList = new SourceList<CollectionItem>();
            _propList.Connect().ObserveOnDispatcher().Bind(out var collection, 2).SubscribeDispose(Subscriptions);

            _exposureList = new SourceCache<PerCameraSettingItem, string>(x => x.CameraName);
            _exposureList.Connect().ObserveOnDispatcher().Bind(out var expList, 2).DisposeMany().SubscribeDispose(Subscriptions);

            PerCameraSettingsView = expList;
            SharedSettingsView = collection;
            var camNames = JobManager.Manager.GetCameras().Keys.ToList();

            _exposureList.Edit(x =>
            {
                x.Load(camNames.Select(y => new PerCameraSettingItem(y, string.Empty)));
            });


            InitializeCommands();
            HookObservables();
            LoadValues();

            AcquisitionSettingsProxy = new DescendantProxy(_acqSettsProvider, x => new AcquisitionSettingsViewModel((ReactiveWrapper<IAcquisitionSettings>)x, false)).DisposeWith(Subscriptions);


            CreateValidators();

        }

        private void CreateValidators()
        {
            CreateValidator(this.WhenPropertyChanged(x => x.ObjectName)
                .Select(x => (nameof(Validators.Validate.CannotBeDefault), Validators.Validate.CannotBeDefault(x.Value))), nameof(ObjectName));

            CreateValidator(this.WhenPropertyChanged(x => x.ObjectName)
                .Select(x => (nameof(Validators.Validate.ShouldBeSimpleString), Validators.Validate.ShouldBeSimpleString(x.Value))), nameof(ObjectName));
        }

        private void PushValues()
        {

            // BUG : Incorrectly generated
            Model.Object.StarName = ObjectName;
            Model.Object.Description = Description;
            Model.Object.CycleType = CycleType;

            var sharedExposure = Model.Object.SharedParameters?.ExposureTime ?? 0f;

            Model.Object.PerCameraParameters ??= new Dictionary<string, Dictionary<string, object?>>();

            if (!Model.Object.PerCameraParameters.ContainsKey(nameof(IAcquisitionSettings.ExposureTime)))
                Model.Object.PerCameraParameters[nameof(IAcquisitionSettings.ExposureTime)] =
                    new Dictionary<string, object?>();

            foreach (var (camName, valueContainer) in _exposureList.KeyValues)
            {
                if (!valueContainer.HasErrors && float.TryParse(valueContainer.Value, NumberStyles.Any,
                    NumberFormatInfo.InvariantInfo, out var expTime))
                    Model.Object.PerCameraParameters[nameof(IAcquisitionSettings.ExposureTime)][camName] = expTime;
                else if(!Model.Object.PerCameraParameters.ContainsKey(camName))
                    Model.Object.PerCameraParameters[nameof(IAcquisitionSettings.ExposureTime)][camName] = sharedExposure;
            }

        }
        private void LoadValues()
        {
            ObjectName = Model.Object.StarName ?? $"star_{DateTimeOffset.UtcNow:yyMMddHHmmss}";
            Description = Model.Object.Description;
            CycleType = Model.Object.CycleType;

            var camNames = JobManager.Manager.GetCameras().Keys.ToList();

            
            if (Model.Object.SharedParameters is { } @params)
            {

                var perCamSetts = Model.Object.PerCameraParameters?.Where(x => x.Value.Keys.Join(camNames, y => y, z => z, (y, z) => Unit.Default).Any())
                    .Select(x => x.Key).ToList();

                var dataToLoad = @params.AsDictionary(true)
                    .Select(x => (x.Value, perCamSetts?.Contains(x.Key) == true) switch
                    {
                        (float f, bool ovr) => new CollectionItem(x.Key, f.ToString("F"), ovr),
                        (Enum @enum, bool ovr) => new CollectionItem(x.Key,
                            ConverterImplementations.EnumToDescriptionConversion(@enum), ovr),
                        ({ } val, bool ovr) => new CollectionItem(x.Key, val.ToString(), ovr),
                        (null, true) => new CollectionItem(x.Key, @"Overriden", true), 
                        (null, false) => new CollectionItem(x.Key, null, false),
                    })
                    .Where(FilterEssential);
                _propList.Edit(x =>
                {
                    x.Clear();
                    x.AddRange(dataToLoad);
                });
            }

            if (Model.Object.PerCameraParameters?.ContainsKey(nameof(IAcquisitionSettings.ExposureTime)) == true)
            {
                var exposures = Model.Object.PerCameraParameters[nameof(IAcquisitionSettings.ExposureTime)]
                    .ToDictionary(x => x.Key,
                        x => x.Value switch
                        {
                            float f => f.ToString("F"),
                            double d => d.ToString("F"),
                            _ => ""
                        });

                _exposureList.Edit(x =>
                {
                    foreach (var (camName, val) in exposures)
                    {
                        if (x.Lookup(camName) is var lookup && lookup.HasValue)
                        {
                            lookup.Value.Value = val;
                            x.AddOrUpdate(lookup.Value);
                        }
                        else
                            x.AddOrUpdate(new PerCameraSettingItem(camName, val));
                    }
                });
            }
            else
                _exposureList.Edit(x =>
                {
                    foreach (var item in x.Items)
                        item.Value = string.Empty;
                });
        }


        private bool FilterEssential(CollectionItem item)
        {
            if (item.IsNotSpecified)
            {
                return item.SettingsName switch
                {
                    nameof(IAcquisitionSettings.EMCCDGain) when Model.Object?.SharedParameters.OutputAmplifier !=
                                                                OutputAmplification.ElectronMultiplication => false,
                    nameof(IAcquisitionSettings.AccumulateCycle) when
                    Model.Object?.SharedParameters.AcquisitionMode is { } mode
                    && (mode == AcquisitionMode.SingleScan || mode == AcquisitionMode.RunTillAbort) => false,
                    nameof(IAcquisitionSettings.KineticCycle) when
                    Model.Object?.SharedParameters.AcquisitionMode is { } mode
                    && (mode == AcquisitionMode.SingleScan || mode == AcquisitionMode.Accumulation) => false,
                    _ => true
                };
            }

            return true;
        }

        private void InitializeCommands()
        {
            CancelButtonCommand = ReactiveCommand.Create<Window>(w =>
            {
                _closedUsingButton = true;
                // Passing `null` to indicate that the operation is cancelled
                Model.Object = null;
                w?.Close();
            });
            //.DisposeWith(Subscriptions);
            
            var canSubmit = _propList.Connect().Select(_ => _propList.Items.Any(x => x.IsNotSpecified))
                .CombineLatest(
                    ObserveHasErrors,
                    _exposureList.Connect().TrueForAny(x => x.ObserveHasErrors, (x, y) => y).Prepend(false),
                    (x, y, z) => !x && !y && !z);

            SaveAndSubmitButtonCommand = ReactiveCommand.Create<Window, Window>(x => x, canSubmit)
                .DisposeWith(Subscriptions);

            LoadButtonCommand = ReactiveCommand.Create<Unit, Unit>(x => x)
                .DisposeWith(Subscriptions);

            SaveActionCommand = ReactiveCommand.CreateFromTask<string, bool>(WriteTargetFile).DisposeWith(Subscriptions);
            LoadActionCommand = ReactiveCommand.CreateFromTask<string>(ReadTargetFile).DisposeWith(Subscriptions);

            CreateNewButtonCommand = ReactiveCommand.Create(() => Unit.Default).DisposeWith(Subscriptions);

            _acqSettsProvider = new DescendantProvider(
                ReactiveCommand.Create<object, ReactiveObjectEx>(_ => new ReactiveWrapper<IAcquisitionSettings>(GetNewSettingsTemplate())),
                null, null,
                ReactiveCommand.Create<ReactiveObjectEx>(x =>
                {
                    if (x is ReactiveWrapper<IAcquisitionSettings> wrapper)
                    {
                        Model.Object.SharedParameters = new SharedSettingsContainer(wrapper.Object);
                        Model.Object.PerCameraParameters = new Dictionary<string, Dictionary<string, object?>>();
                        // If the settings are applied to the camera, do not dispose it 
                        if (ReferenceEquals(_firstCamera.CurrentSettings, wrapper.Object))
                            wrapper.Object = null!;
                        LoadValues();
                    }

                    x.Dispose();
                })).DisposeWith(Subscriptions);

            WindowClosingCommand = ReactiveCommand.Create<object>(x =>
            {
                if (!_closedUsingButton)
                    Model.Object = null!;
            });
        }

        private IAcquisitionSettings GetNewSettingsTemplate()
        {
            var template = _firstCamera.CurrentSettings?.MakeCopy() ?? _firstCamera.GetAcquisitionSettingsTemplate();
            if (Model.Object.SharedParameters is { } shPar)
                template.Load1(shPar.AsDictionary());
            return template;
        }

        private FileDialogDescriptor GenerateSaveDialogDescriptor() =>
            new FileDialogDescriptor
            {
                Mode = FileDialogDescriptor.DialogMode.Save,
                DefaultExtenstion = "*.star",
                FileName = ObjectName,
                Title = Properties.Localization.JobSettings_Dialog_Save
            };


        private FileDialogDescriptor GenerateLoadDialogDescriptor() =>
            new FileDialogDescriptor
            {
                Mode = FileDialogDescriptor.DialogMode.Load,
                DefaultExtenstion = "*.star",
                FileName = ObjectName,
                Title = Properties.Localization.JobSettings_Dialog_Load
            };

        private void HookObservables()
        {
            // On save button click, generate file dialog request
            SaveAndSubmitButtonCommand
                .Select(_ => GenerateSaveDialogDescriptor())
                .Subscribe(OnFileDialogRequested)
                .DisposeWith(Subscriptions);
            
            // When both save button was clicked and dialog finished (one way or another),
            // Close window
            SaveAndSubmitButtonCommand.Zip(SaveActionCommand, 
                (x, y) => (Window:x, WasSaved:y))
                .Subscribe(x =>
                {
                    if (x.WasSaved && x.Window is {} window)
                    {
                        _closedUsingButton = true;
                        window.Close();
                    }

                }).DisposeWith(Subscriptions);

            LoadButtonCommand
                .Select(_ => GenerateLoadDialogDescriptor())
                .Subscribe(OnFileDialogRequested)
                .DisposeWith(Subscriptions);

            CreateNewButtonCommand.InvokeCommand(_acqSettsProvider?.ViewRequested).DisposeWith(Subscriptions);
        }


        private async Task<bool> WriteTargetFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;
            PushValues();
            try
            {
                using var fStr = new FileStream(path, FileMode.Create, FileAccess.Write);
                using var writer = new StreamWriter(fStr);
                var line = JsonConvert.SerializeObject(Model.Object, Formatting.Indented);
                await writer.WriteAsync(line);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task ReadTargetFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;
            try
            {
                using var fStr = new FileStream(path, FileMode.Open, FileAccess.Read);
                using var reader = new StreamReader(fStr);
                var line = await reader.ReadToEndAsync();
                var target = JsonConvert.DeserializeObject<Target1>(line);
                if (target is { })
                {
                    Model.Object = target;
                    LoadValues();
                }

            }
            catch (Exception e)
            {
                // TODO : Show message box
            }
        }


        private void OnFileDialogRequested(FileDialogDescriptor e) 
            => FileDialogRequested?.Invoke(this, new DialogRequestedEventArgs(e));
    }
}
