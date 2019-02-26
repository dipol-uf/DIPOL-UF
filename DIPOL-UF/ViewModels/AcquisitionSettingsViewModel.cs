﻿//    This file is part of Dipol-3 Camera Manager.

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
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using ANDOR_CS.Classes;
using ANDOR_CS.Enums;
using DIPOL_UF.Commands;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace DIPOL_UF.ViewModels
{
    internal sealed class AcquisitionSettingsViewModel : ReactiveViewModel<ReactiveWrapper<SettingsBase>>
    {
        public class SettingsAvailability : ReactiveObjectEx
        {
            public bool VsSpeed { [ObservableAsProperty] get; }
            public bool VsAmplitude { [ObservableAsProperty] get; }
            public bool AdcBitDepth { [ObservableAsProperty] get; }

        }

        private static readonly Regex PropNameTrimmer = new Regex("(((Value)|(Index))+(Text)?)|(_.{2})");
        private static readonly List<(string, PropertyInfo)> PropertyList =
            typeof(AcquisitionSettingsViewModel)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(pi => pi.CanRead && pi.CanWrite)
            .Select(pi => (PropNameTrimmer.Replace(pi.Name, ""), pi))
            .ToList();


        public SettingsAvailability IsAvailable { get; }
            = new SettingsAvailability();

        public CameraBase Camera => Model.Object.Camera;

        public DelegateCommand SubmitCommand { get; private set; }

        public DelegateCommand CancelCommand { get; private set; }
        public DelegateCommand SaveCommand { get; private set; }

        public DelegateCommand LoadCommand { get; private set; }
        public (float ExposureTime, float AccumulationCycleTime, 
            float KineticCycleTime) EstimatedTiming
        {
            get;
            private set;
        }

       /// <summary>
        /// Collection of supported by a given Camera settings.
        /// </summary>
        public HashSet<string> SupportedSettings { get; private set; }

        /// <summary>
        /// Collection of settings that can be set now.
        /// </summary>
        public HashSet<string> AllowedSettings
        {
            get;
            private set;
        }

        /// <summary>
        /// Supported acquisition modes.
        /// </summary>
        public AcquisitionMode[] AllowedAcquisitionModes =>
           Helper.EnumFlagsToArray<AcquisitionMode>(Model.Object.Camera.Capabilities.AcquisitionModes)
            .Where(item => item != ANDOR_CS.Enums.AcquisitionMode.FrameTransfer)
            .Where(ANDOR_CS.Classes.EnumConverter.IsAcquisitionModeSupported)
            .ToArray();

        public ReadMode[] AllowedReadoutModes =>
            Helper.EnumFlagsToArray<ReadMode>(Model.Object.Camera.Capabilities.ReadModes)
            .Where(ANDOR_CS.Classes.EnumConverter.IsReadModeSupported)
            .ToArray();

        public TriggerMode[] AllowedTriggerModes =>
            Helper.EnumFlagsToArray<TriggerMode>(Model.Object.Camera.Capabilities.TriggerModes)
            .Where(ANDOR_CS.Classes.EnumConverter.IsTriggerModeSupported)
            .ToArray();

        public (int Index, float Speed)[] AvailableHSSpeeds { [ObservableAsProperty] get; }

        public (int Index, string Name)[] AvailablePreAmpGains { [ObservableAsProperty] get; }
        //(ADConverterIndex < 0 || OutputAmplifierIndex < 0)
            //    ? null
            //    : Model.Object.GetAvailableHSSpeeds(ADConverterIndex, OutputAmplifierIndex)
            //           .ToArray();

        //public (int Index, string Name)[] AvailablePreAmpGains =>
        //    (ADConverterIndex < 0 || OutputAmplifierIndex < 0 || HSSpeedIndex < 0)
        //        ? null
        //        : Model.Object.GetAvailablePreAmpGain(ADConverterIndex,
        //                   OutputAmplifierIndex, HSSpeedIndex)
        //               .ToArray();

        // Todo: Use (Min, Max)
        public int[] AvailableEMCCDGains =>
            Enumerable.Range(
                          Model.Object.Camera.Properties.EMCCDGainRange.Low, 
                          Model.Object.Camera.Properties.EMCCDGainRange.High)
            .ToArray();

        ///// <summary>
        ///// Index of VS Speed.
        ///// </summary>
        //public int VsSpeed
        //{
        //    get => model.VSSpeed?.Index ?? -1;
        //    set
        //    {
        //        try
        //        {
        //            model.SetVSSpeed(value < 0 ? 0 : value);
        //            ValidateProperty(null);
        //            //RaisePropertyChanged();
        //        }
        //        catch (Exception e)
        //        {
        //            ValidateProperty(e);
        //        }

        //    }
        //}
        ///// <summary>
        ///// VS Amplitude.
        ///// </summary>
        //public VSAmplitude? VSAmplitudeValue
        //{
        //    get => model.VSAmplitude;
        //    set
        //    {
        //        try
        //        {
        //            model.SetVSAmplitude(value ?? VSAmplitude.Normal);
        //            ValidateProperty(null);
        //            //RaisePropertyChanged();
        //        }
        //        catch (Exception e)
        //        {
        //            ValidateProperty(e);
        //        }
        //    }
        //}
        ///// <summary>
        ///// Analog-Digital COnverter index.
        ///// </summary>
        //public int ADConverterIndex
        //{
        //    get => model.ADConverter?.Index ?? -1;
        //    set
        //    {
        //        try
        //        {
        //            model.SetADConverter(value < 0 ? 0 : value);
        //            ValidateProperty(null);
        //            //RaisePropertyChanged();
        //        }
        //        catch (Exception e)
        //        {
        //            ValidateProperty(e);
        //        }
        //    }
        //}
        ///// <summary>
        ///// Output OutputAmplifier index.
        ///// </summary>
        //public int OutputAmplifierIndex
        //{
        //    get => model.OutputAmplifier?.Index ?? -1;
        //    set
        //    {
        //        try
        //        {
        //            model.SetOutputAmplifier(Camera.Properties.OutputAmplifiers[value < 0 ? 0 : value].OutputAmplifier);
        //            ValidateProperty(null);
        //            //RaisePropertyChanged();
        //        }
        //        catch (Exception e)
        //        {
        //            ValidateProperty(e);
        //        }
        //    }
        //}
        ///// <summary>
        ///// HS Speed.
        ///// </summary>
        //public int HSSpeedIndex
        //{
        //    get => model.HSSpeed?.Index ?? -1;
        //    set
        //    {

        //        try
        //        {
        //            model.SetHSSpeed(value < 0 ? 0 : value);
        //            ValidateProperty(null);
        //            //RaisePropertyChanged();
        //        }
        //        catch (Exception e)
        //        {
        //            ValidateProperty(e);
        //        }

        //    }
        //}
        ///// <summary>
        ///// Index of Pre OutputAmplifier Gain.
        ///// </summary>
        //public int PreAmpGainIndex
        //{
        //    get => model.PreAmpGain?.Index ?? -1;
        //    set
        //    {
        //        try
        //        {
        //            model.SetPreAmpGain(value < 0 ? 0 : value);
        //            ValidateProperty(null);
        //            //RaisePropertyChanged();

        //        }
        //        catch (Exception e)
        //        {
        //            ValidateProperty(e);
        //        }
        //    }
        //}
        ///// <summary>
        ///// Acquisition mode value.
        ///// </summary>
        //public AcquisitionMode? AcquisitionModeValue
        //{
        //    get
        //    {
        //        if (model.AcquisitionMode?.HasFlag(AcquisitionMode.FrameTransfer) ?? false)
        //            return model.AcquisitionMode ^ AcquisitionMode.FrameTransfer;
        //        else
        //            return model.AcquisitionMode;
        //    }
        //    set
        //    {
        //        try
        //        {
        //            model.SetAcquisitionMode(value ?? AcquisitionMode.SingleScan);
        //            ValidateProperty(null);
        //            //RaisePropertyChanged();
        //        }
        //        catch (Exception e)
        //        {
        //            ValidateProperty(e);
        //        }
        //    }
        //}
        ///// <summary>
        ///// Frame transfer flag; applied to acquisition mode
        ///// </summary>
        //public bool FrameTransferValue
        //{
        //    get => model.AcquisitionMode?.HasFlag(AcquisitionMode.FrameTransfer) ?? false;
        //    set
        //    {
        //        try
        //        {
        //            if (value)
        //            {
        //                if (!model.AcquisitionMode?.HasFlag(AcquisitionMode.FrameTransfer) ?? false)
        //                {
        //                    model.SetAcquisitionMode((model.AcquisitionMode | AcquisitionMode.FrameTransfer) ?? AcquisitionMode.FrameTransfer);
        //                    ValidateProperty(null);
        //                    //RaisePropertyChanged();
        //                }
        //            }
        //            else
        //            {
        //                if (model.AcquisitionMode?.HasFlag(AcquisitionMode.FrameTransfer) ?? false)
        //                {
        //                    model.SetAcquisitionMode((model.AcquisitionMode ^ AcquisitionMode.FrameTransfer) ?? AcquisitionMode.SingleScan);
        //                    ValidateProperty(null);
        //                    //RaisePropertyChanged();
        //                }
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            ValidateProperty(e);
        //        }

        //    }
        //}
        ///// <summary>
        ///// Read mode value
        ///// </summary>
        //public ReadMode? ReadoutModeValue
        //{
        //    get => model.ReadoutMode;
        //    set
        //    {
        //        try
        //        {
        //            model.SetReadoutMode(value ?? ReadMode.FullImage);
        //            ValidateProperty(null);
        //            //RaisePropertyChanged();
        //        }
        //        catch (Exception e)
        //        {
        //            ValidateProperty(e);
        //        }
        //    }
        //}
        ///// <summary>
        ///// Trigger mode value
        ///// </summary>
        //public TriggerMode? TriggerModeValue
        //{
        //    get => model.TriggerMode;
        //    set
        //    {
        //        try
        //        {
        //            model.SetTriggerMode(value ?? TriggerMode.Internal);
        //            ValidateProperty(null);
        //            //RaisePropertyChanged();
        //        }
        //        catch (Exception e)
        //        {
        //            ValidateProperty(e);
        //        }
        //    }
        //}
        ///// <summary>
        ///// Exposure time; text field
        ///// </summary>
        //public string ExposureTimeValueText
        //{
        //    get => model.ExposureTime?.ToString();
        //    set
        //    {
        //        try
        //        {
        //            if (string.IsNullOrWhiteSpace(value))
        //            {
        //                model.SetExposureTime(0f);
        //                ValidateProperty(null);
        //                //RaisePropertyChanged();
        //            }
        //            else if (float.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out float flVal))
        //            {
        //                model.SetExposureTime(flVal);
        //                ValidateProperty(null);
        //                //RaisePropertyChanged();
        //            }
        //            else
        //                ValidateProperty(new ArgumentException("Provided value is not a number."));

        //        }
        //        catch (Exception e)
        //        {
        //            ValidateProperty(e);
        //        }
        //    }

        //}
        ///// <summary>
        ///// EM CCD gain; text field
        ///// </summary>
        //public string EMCCDGainValueText
        //{
        //    get => model.EMCCDGain?.ToString();
        //    set
        //    {
        //        try
        //        {
        //            if (string.IsNullOrWhiteSpace(value))
        //            {
        //                model.SetEMCCDGain(Camera.Properties.EMCCDGainRange.Low);
        //                ValidateProperty();
        //                //RaisePropertyChanged();
        //            }
        //            else if (int.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out int intVal))
        //            {
        //                model.SetEMCCDGain(intVal);
        //                ValidateProperty();
        //                //RaisePropertyChanged();
        //            }
        //            else
        //                ValidateProperty(new ArgumentException("Provided value is not a number."));
        //        }
        //        catch (Exception e)
        //        {
        //            ValidateProperty(e);
        //        }
        //        finally
        //        {
        //            //RaisePropertyChanged();
        //        }
        //    }
        //}

        //public string ImageArea_X1
        //{
        //    get => model.ImageArea?.X1.ToString();
        //    set {
        //        try
        //        {
        //            if (int.TryParse(value,
        //                System.Globalization.NumberStyles.Any,
        //                System.Globalization.NumberFormatInfo.InvariantInfo, out var intVal))
        //            {
        //                var rect = new Rectangle(
        //                    intVal,
        //                    model.ImageArea?.Y1 ?? 1,
        //                    model.ImageArea?.X2 > intVal ? model.ImageArea.Value.X2 : intVal + 1,
        //                    model.ImageArea?.Y2 ?? 2);
        //                model.SetImageArea(rect);
        //                ValidateProperty(null);
        //            }
        //            else
        //                ValidateProperty(new ArgumentException("Provided value is not a number"));
        //        }
        //        catch (Exception e)
        //        {
        //            ValidateProperty(e);
        //        }

        //    }
        //}
        //public string ImageArea_X2
        //{
        //    get => model.ImageArea?.X2.ToString();
        //    set
        //    {
        //        try
        //        {
        //            if (int.TryParse(value,
        //                System.Globalization.NumberStyles.Any,
        //                System.Globalization.NumberFormatInfo.InvariantInfo, out var intVal))
        //            {
        //                var rect = new Rectangle(
        //                    model.ImageArea?.X1 < intVal ? model.ImageArea.Value.X1 : intVal - 1,
        //                    model.ImageArea?.Y1 ?? 1,
        //                    intVal,
        //                    model.ImageArea?.Y2 ?? 2);
        //                model.SetImageArea(rect);
        //                ValidateProperty(null);
        //            }
        //            else
        //                ValidateProperty(new ArgumentException("Provided value is not a number"));
        //        }
        //        catch (Exception e)
        //        {
        //            ValidateProperty(e);
        //        }

        //    }
        //}
        //public string ImageArea_Y1
        //{
        //    get => model.ImageArea?.Y1.ToString();
        //    set
        //    {
        //        try
        //        {
        //            if (int.TryParse(value,
        //                System.Globalization.NumberStyles.Any,
        //                System.Globalization.NumberFormatInfo.InvariantInfo, out var intVal))
        //            {
        //                var rect = new Rectangle(
        //                    model.ImageArea?.X1 ?? 1,
        //                    intVal,
        //                    model.ImageArea?.X2 ?? 2,
        //                    model.ImageArea?.Y2 > intVal ? model.ImageArea.Value.Y2 : intVal + 1);
        //                model.SetImageArea(rect);
        //                ValidateProperty(null);
        //            }
        //            else
        //                ValidateProperty(new ArgumentException("Provided value is not a number"));
        //        }
        //        catch (Exception e)
        //        {
        //            ValidateProperty(e);
        //        }

        //    }
        //}
        //public string ImageArea_Y2
        //{
        //    get => model.ImageArea?.Y2.ToString();
        //    set
        //    {
        //        try
        //        {
        //            if (int.TryParse(value,
        //                System.Globalization.NumberStyles.Any,
        //                System.Globalization.NumberFormatInfo.InvariantInfo, out var intVal))
        //            {
        //                var rect = new Rectangle(
        //                    model.ImageArea?.X1 ?? 1,
        //                    model.ImageArea?.Y1 < intVal ? model.ImageArea.Value.Y1 : intVal - 1,
        //                    model.ImageArea?.X2 ?? 2,
        //                    intVal);
        //                model.SetImageArea(rect);
        //                ValidateProperty(null);
        //            }
        //            else
        //                ValidateProperty(new ArgumentException("Provided value is not a number"));
        //        }
        //        catch (Exception e)
        //        {
        //            ValidateProperty(e);
        //        }

        //    }
        //}

        public AcquisitionSettingsViewModel(ReactiveWrapper<SettingsBase> model)
            :base(model)
        {
            SupportedSettings = Model.Object.SupportedSettings();
            InitializeAllowedSettings();


            InitializeCommands();

            HookObservables();
            HookValidators();

            WatchAvailableSettings();
        }

        private void HookObservables()
        {
            AttachGetters();
            AttachSetters();
         
            
        }

        private void AttachGetters()
        {
            Model.Object.WhenPropertyChanged(x => x.VSSpeed)
                 .Select(x => x.Value?.Index ?? -1)
                 .BindTo(this, x => x.VsSpeed)
                 .DisposeWith(_subscriptions);
        }

        private void AttachSetters()
        {

        }

        private void WatchAvailableSettings()
        {
            Observable.Return(
                    AllowedSettings.Contains(nameof(Model.Object.VSSpeed).ToLowerInvariant())
                    && SupportedSettings.Contains(nameof(Model.Object.VSSpeed).ToLowerInvariant()))
                .ToPropertyEx(IsAvailable, x => x.VsSpeed);

            Observable.Return(
                    AllowedSettings.Contains(nameof(Model.Object.VSAmplitude).ToLowerInvariant())
                    && SupportedSettings.Contains(nameof(Model.Object.VSAmplitude).ToLowerInvariant()))
                .ToPropertyEx(IsAvailable, x => x.VsAmplitude);

            Observable.Return(
                          AllowedSettings.Contains(nameof(Model.Object.ADConverter).ToLowerInvariant())
                          && SupportedSettings.Contains(nameof(Model.Object.ADConverter).ToLowerInvariant()))
                      .ToPropertyEx(IsAvailable, x => x.AdcBitDepth);
        }

        protected override void HookValidators()
        {
            base.HookValidators();

            CreateValidator(
                this.WhenPropertyChanged(x => x.VsSpeed)
                    .Select(x =>
                        (Type: nameof(Validators.Validate.CannotBeDefault),
                            Message: Validators.Validate.CannotBeDefault(x.Value, -1))),
                nameof(VsSpeed));

            CreateValidator(
                this.WhenPropertyChanged(x => x.VsAmplitude)
                    .Select(x =>
                        (Type: nameof(Validators.Validate.CannotBeDefault),
                            Message: Validators.Validate.CannotBeDefault((object)x.Value, null))),
                nameof(VsSpeed));


            CreateValidator(
                this.WhenPropertyChanged(x => x.VsSpeed)
                    .Where(x => x.Value >= 0)
                    .Select(x =>
                        (Type: nameof(Validators.Validate.DoesNotThrow),
                            Message: Validators.Validate.DoesNotThrow(Model.Object.SetVSSpeed, x.Value))), nameof(VsSpeed));

            // ReSharper disable once PossibleInvalidOperationException
            CreateValidator(
                this.WhenPropertyChanged(x => x.VsAmplitude)
                    .Where(x => x.Value.HasValue)
                    .Select(x =>
                        (Type: nameof(Validators.Validate.DoesNotThrow),
                            Message: Validators.Validate.DoesNotThrow(Model.Object.SetVSAmplitude, x.Value.Value))),
                nameof(VsAmplitude));
        }
        
        private void InitializeAllowedSettings()
        {
            AllowedSettings = Model.Object.AllowedSettings();
            

            //new KeyValuePair<string, bool>(nameof(model.ADConverter), true),
            //new KeyValuePair<string, bool>(nameof(model.OutputAmplifier), true),
            //new KeyValuePair<string, bool>(nameof(model.HSSpeed),
            //    ADConverterIndex >= 0
            //    && OutputAmplifierIndex >= 0),
            //new KeyValuePair<string, bool>(nameof(model.PreAmpGain),
            //    ADConverterIndex >= 0
            //    && OutputAmplifierIndex >= 0
            //    && HSSpeedIndex >= 0),
            //new KeyValuePair<string, bool>(nameof(model.AcquisitionMode), true),
            //new KeyValuePair<string, bool>(nameof(FrameTransferValue), false),
            //new KeyValuePair<string, bool>(nameof(model.ReadoutMode), true),
            //new KeyValuePair<string, bool>(nameof(model.TriggerMode), true),
            //new KeyValuePair<string, bool>(nameof(model.ExposureTime), true),
            //new KeyValuePair<string, bool>(nameof(model.EMCCDGain),
            //    (OutputAmplifierIndex >= 0)
            //    && Camera.Properties.OutputAmplifiers[OutputAmplifierIndex].OutputAmplifier == OutputAmplification.Conventional),
            //new KeyValuePair<string, bool>(nameof(model.ImageArea), true)
            //};

        }

        private void InitializeCommands()
        {
            SubmitCommand = new DelegateCommand(
                (param) => CloseView(param, false),
                CanSubmit
                );

            CancelCommand = new DelegateCommand(
                (param) => CloseView(param, true),
                DelegateCommand.CanExecuteAlways
                );

            SaveCommand = new DelegateCommand(
                SaveTo,
                DelegateCommand.CanExecuteAlways
                );

            LoadCommand = new DelegateCommand(
                LoadFrom,
                DelegateCommand.CanExecuteAlways);
        }

        private void SaveTo(object parameter)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                AddExtension = true,
                CheckPathExists = true,
                DefaultExt = ".acq",
                FileName = Camera.ToString(),
                FilterIndex = 0,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Title = "Save current acquisition settings"
            };
            dialog.Filter = $@"Acquisition settings (*{dialog.DefaultExt})|*{dialog.DefaultExt}|All files (*.*)|*.*";
            

            if (dialog.ShowDialog() == true)
                try
                {
                    using (var fl = File.Open(dialog.FileName, FileMode.Create, FileAccess.Write, FileShare.Read))
                        Model.Object.Serialize(fl);
                }
                catch (Exception e)
                {
                    var messSize = UiSettingsProvider.Settings.Get("ExceptionStringLimit", 80);
                    MessageBox.Show($"An error occured while saving acquisition settings to {dialog.FileName}.\n" +
                                    $"[{(e.Message.Length <= messSize ? e.Message : e.Message.Substring(0, messSize))}]", 
                        "Unable to save file", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                }
        }

        private void LoadFrom(object parameter)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog()
            {
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = ".acq",
                FileName = Camera.ToString(),
                FilterIndex = 0,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Title = "Load acquisition settings from file"
            };
            dialog.Filter = $@"Acquisition settings (*{dialog.DefaultExt})|*{dialog.DefaultExt}|All files (*.*)|*.*";

            //var temp = AllowedSettings;
            //AllowedSettings =
            //    new Dictionary<string, bool>(temp.Select(item => new KeyValuePair<string, bool>(item.Key, false)));
            ////RaisePropertyChanged(nameof(AllowedSettings));
            //AllowedSettings = temp;
            if (dialog.ShowDialog() == true)
            {
                Task.Run(() =>
                {
                    try
                    {
                        Task.Delay(2000).Wait();
                        using (var fl = File.Open(dialog.FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                            Model.Object.Deserialize(fl);
                    }
                    catch (Exception e)
                    {
                        var messSize = UiSettingsProvider.Settings.Get("ExceptionStringLimit", 80);
                        Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            MessageBox.Show($"An error occured while reading acquisition settings from {dialog.FileName}.\n" +
                                            $"[{(e.Message.Length <= messSize ? e.Message : e.Message.Substring(0, messSize))}]",
                                "Unable to load file", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                        });
                    }
                    //Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(AllowedSettings)));
                });

            }
            //else
            //    RaisePropertyChanged(nameof(AllowedSettings));
        }

        
        //protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        //{
        //    base.OnPropertyChanged(sender, e);

        //    if ((e.PropertyName == nameof(OutputAmplifierIndex) ||
        //         e.PropertyName == nameof(ADConverterIndex)) &&
        //        (AllowedSettings[nameof(model.HSSpeed)]
        //            = ADConverterIndex >= 0 &&
        //              OutputAmplifierIndex >= 0))
        //    {
        //        RaisePropertyChanged(nameof(PreAmpGainIndex));
        //        RaisePropertyChanged(nameof(HSSpeedIndex));
        //        RaisePropertyChanged(nameof(AvailableHSSpeeds));
        //    }

        //    if ((e.PropertyName == nameof(OutputAmplifierIndex) ||
        //         e.PropertyName == nameof(ADConverterIndex) ||
        //         e.PropertyName == nameof(HSSpeedIndex)) &&
        //        (AllowedSettings[nameof(model.PreAmpGain)]
        //            = OutputAmplifierIndex >= 0 &&
        //              ADConverterIndex >= 0 &&
        //              HSSpeedIndex >= 0))
        //    {
        //        RaisePropertyChanged(nameof(PreAmpGainIndex));
        //        RaisePropertyChanged(nameof(AvailablePreAmpGains));
        //    }

        //    if (e.PropertyName == nameof(AcquisitionModeValue) &&
        //        AcquisitionModeValue.HasValue)
        //    {
        //        AllowedSettings[nameof(FrameTransferValue)] =
        //            AcquisitionModeValue != AcquisitionMode.SingleScan &&
        //            AcquisitionModeValue != AcquisitionMode.FastKinetics;
        //        if (!AllowedSettings[nameof(FrameTransferValue)])
        //        {
        //            FrameTransferValue = false;
        //            //RaisePropertyChanged(nameof(FrameTransferValue));
        //        }
        //    }
        //    if (e.PropertyName == nameof(OutputAmplifierIndex))
        //    {
        //        AllowedSettings[nameof(model.EMCCDGain)] = (OutputAmplifierIndex >= 0) &&
        //            (Camera.Properties.OutputAmplifiers[OutputAmplifierIndex].OutputAmplifier 
        //            == OutputAmplification.Conventional);
        //        RaisePropertyChanged(nameof(EMCCDGainValueText));
        //    }

        //    SubmitCommand?.OnCanExecuteChanged();

        //}

        //protected override void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        //{
        //    // Model can raise PropertyChange from non-UI thread,
        //    // therefore ModelView should dispatch respective events on UI thread

        //    base.OnModelPropertyChanged(sender, e);
        //    var prop = PropertyList
        //        .FirstOrDefault(item => item.Item1 == e.PropertyName);

        //    if (prop.Item2 != null)
        //       Helper.ExecuteOnUI(() => RaisePropertyChanged(prop.Item2.Name));

        //    if (e.PropertyName == nameof(model.AcquisitionMode))
        //        Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(FrameTransferValue)));

        //    if (e.PropertyName == nameof(model.ImageArea))
        //    {
        //        Helper.ExecuteOnUI(() => RaiseErrorChanged(nameof(ImageArea_X1)));
        //        Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(ImageArea_Y1)));
        //        Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(ImageArea_X2)));
        //        Helper.ExecuteOnUI(() => RaisePropertyChanged(nameof(ImageArea_Y2)));
        //    }
        //}

        private void CloseView(object parameter, bool isCanceled)
        {
            if (parameter is DependencyObject elem)
            {
                var window = Helper.FindParentOfType<Window>(elem);
                if (window != null && Helper.IsDialogWindow(window))
                {
                    if (!isCanceled)
                    {
                        try
                        {
                            try
                            {
                                Model.Object.Camera.ApplySettings(Model.Object);
                            }
                            catch(Exception e)
                            { 
                                var messSize = UiSettingsProvider.Settings.Get("ExceptionStringLimit", 80);
                                var listSize = UiSettingsProvider.Settings.Get("ExceptionStringLimit", 5);
                                var sb = new StringBuilder(messSize * listSize);

                                sb.AppendLine("Some of the settings were applied unsuccessfully:");
                                sb.AppendLine(e.Message);

                                MessageBox.Show(sb.ToString(),
                                    "Partially unsuccessful application of settings", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);

                                //foreach (var prop in PropertyList.Join(failed, x => x.Item1, y => y.Option, (x, y) =>
                                //    new {
                                //        Name = x.Item1,
                                //        Error = y.ReturnCode
                                //    }))
                                //    ValidateProperty(new AndorSdkException(prop.Name, prop.Error), prop.Name);

                                return;
                            }
                            EstimatedTiming = Model.Object.Camera.Timings;
                        }
                        catch (Exception e)
                        {
                            var messSize = UiSettingsProvider.Settings.Get("ExceptionStringLimit", 80);
                            MessageBox.Show("Failed to apply current settings to target camera.\n" +
                                            $"[{(e.Message.Length <= messSize ? e.Message : e.Message.Substring(0, messSize))}]",
                                "Incompatible settings", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                            return;
                        }
                    }


                    window.DialogResult = !isCanceled;
                    window.Close();
                }

             
            }
        }

        /// <summary>
        /// Checks if Acquisiotn Settings form can be submitted.
        /// </summary>
        /// <param name="parameter">Unused parameter for compatibility with <see cref="Commands.DelegateCommand"/>.</param>
        /// <returns>True if all required fields are set.</returns>
        private bool CanSubmit(object parameter)
        {
            //// Helper function, checks if value is set.
            //bool ValueIsSet(PropertyInfo p)
            //{
            //    if (Nullable.GetUnderlyingType(p.PropertyType) != null)
            //        return p.GetValue(this) != null;
            //    if (p.PropertyType == typeof(int))
            //        return (int)p.GetValue(this) != -1;
            //    if (p.PropertyType == typeof(string))
            //        return !string.IsNullOrWhiteSpace((string)p.GetValue(this));
            //    return false;
            //}

            //// Query that joins pulic Properties to Allowed settings with true value.
            //// As a result, propsQuery stores all Proprties that should have values set.
            //var propsQuery =
            //    from prop in PropertyList
            //    join allowedProp in AllowedSettings 
            //    on prop.Item1 equals allowedProp.Key
            //    where allowedProp.Value
            //    select prop.Item2;

            //// Runs check of values on all selected properties.
            //return propsQuery.All(ValueIsSet) && propsQuery.Any();
            return false;
        }


        #region V2

        [Reactive]
        public int VsSpeed { get; set; }

        [Reactive]
        public VSAmplitude? VsAmplitude { get; set; } 

        [Reactive]
        public int AdcBitDepth { get; set; }

        [Reactive]
        public int Amplifier { get; set; }

        [Reactive]
        public int HsSpeedIndex { get; set; }

        [Reactive]
        public int PreAmpGain { get; set; }

        [Reactive]
        public int AcquisitionMode { get; set; }

        #endregion
    }
}
