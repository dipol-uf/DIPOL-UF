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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using ANDOR_CS.Classes;
using DIPOL_UF.Converters;
using DIPOL_UF.Enums;
using DIPOL_UF.Jobs;
using DIPOL_UF.Models;
using DynamicData;
using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SettingsBase = ANDOR_CS.Classes.SettingsBase;

namespace DIPOL_UF.ViewModels
{
    internal sealed class JobSettingsViewModel1 : ReactiveViewModel<ReactiveWrapper<Target1>>
    {
        public class CollectionItem
        {
            public string SettingsName { get; }
            public string? Value { get; }

            public bool IsOverriden { get; }

            public CollectionItem(string name, string? value, bool isOverriden = false)
                => (SettingsName, Value, IsOverriden) = (name, value, isOverriden);
        }
        private DescendantProvider? _acqSettsProvider;
        private readonly CameraBase _firstCamera;
        private readonly ISourceList<CollectionItem> _propList;
        
        public event EventHandler? FileDialogRequested;

        public DescendantProxy AcquisitionSettingsProxy { get; }

        public ReactiveCommand<Window, Unit> CancelButtonCommand { get; private set; }
        public ReactiveCommand<Window, Window>? SaveAndSubmitButtonCommand { get; private set; }
        public ReactiveCommand<Unit, Unit>? LoadButtonCommand { get; private set; }

        public ReactiveCommand<Unit, Unit>? CreateNewButtonCommand { get; private set; }

        public ReactiveCommand<string, bool>? SaveActionCommand { get; private set; }
        public ReactiveCommand<string, Unit>? LoadActionCommand { get; private set; }

        public ReadOnlyObservableCollection<CollectionItem> SharedSettingsView { get; }

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
            SharedSettingsView = collection;
            
            LoadValues();

            InitializeCommands();
            HookObservables();

            AcquisitionSettingsProxy = new DescendantProxy(_acqSettsProvider, x => new AcquisitionSettingsViewModel((ReactiveWrapper<SettingsBase>)x, false)).DisposeWith(Subscriptions);
        }

        private void PushValues()
        {
            Model.Object.StarName = ObjectName;
            Model.Object.Description = Description;
            Model.Object.CycleType = CycleType;
        }
        private void LoadValues()
        {
            ObjectName = Model.Object.StarName ?? $"star_{DateTimeOffset.UtcNow:yyMMddHHmmss}";
            Description = Model.Object.Description;
            CycleType = Model.Object.CycleType;
            //SharedSettingsRepresentation.Clear();
            if (Model.Object.SharedParameters is { } @params)
            {
                var perCamSetts = Model.Object.PerCameraParameters?.Select(x => x.Key).ToList();

                var dataToLoad = @params.AsDictionary(true)
                    .Select(x => new CollectionItem(x.Key, x.Value switch
                        {
                            float f => f.ToString("F"),
                            Enum @enum => ConverterImplementations.EnumToDescriptionConversion(@enum),
                            { } val => val.ToString(),
                            _ => "Overriden"
                        },
                        perCamSetts?.Contains(x.Key) == true))
                    .Where(x => x.Value != "Overriden" || x.IsOverriden);
                _propList.Edit(x =>
                {
                    x.Clear();
                    x.AddRange(dataToLoad);
                });
            }
        }

        private void InitializeCommands()
        {
            CancelButtonCommand = ReactiveCommand.Create<Window>(w =>
            {
                // Passing `null` to indicate that the operation is cancelled
                Model.Object = null;
                w?.Close();
            });
                //.DisposeWith(Subscriptions);

            SaveAndSubmitButtonCommand = ReactiveCommand.Create<Window, Window>(x => x)
                .DisposeWith(Subscriptions);

            LoadButtonCommand = ReactiveCommand.Create<Unit, Unit>(x => x)
                .DisposeWith(Subscriptions);

            SaveActionCommand = ReactiveCommand.CreateFromTask<string, bool>(WriteTargetFile).DisposeWith(Subscriptions);
            LoadActionCommand = ReactiveCommand.CreateFromTask<string>(ReadTargetFile).DisposeWith(Subscriptions);

            CreateNewButtonCommand = ReactiveCommand.Create(() => Unit.Default).DisposeWith(Subscriptions);

            _acqSettsProvider = new DescendantProvider(
                ReactiveCommand.Create<object, ReactiveObjectEx>(_ => new ReactiveWrapper<SettingsBase>(GetNewSettingsTemplate())),
                null, null,
                ReactiveCommand.Create<ReactiveObjectEx>(x =>
                {
                    if (x is ReactiveWrapper<SettingsBase> wrapper)
                    {
                        Model.Object.SharedParameters = new SharedSettingsContainer(wrapper.Object);
                        
                        // If the settings are applied to the camera, do not dispose it 
                        if (ReferenceEquals(_firstCamera.CurrentSettings, wrapper.Object))
                            wrapper.Object = null!;
                        LoadValues();
                    }

                    x.Dispose();
                })).DisposeWith(Subscriptions);
        }

        private SettingsBase GetNewSettingsTemplate()
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
                    if(x.WasSaved && x.Window is {} window)
                        window.Close();

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
            catch (Exception e)
            {
                // TODO : Show message box
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
