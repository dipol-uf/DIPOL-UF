﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using ANDOR_CS.Classes;
using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Exceptions;

using DIPOL_UF.Commands;

namespace DIPOL_UF.ViewModels
{
    class AcquisitionSettingsViewModel : ViewModel<SettingsBase>
    {
        private static readonly Regex PropNameTrimmer = new Regex("((Value)|(Index))+(Text)?");
        private static readonly List<(string, PropertyInfo)> PropertyList =
            typeof(AcquisitionSettingsViewModel)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(pi => pi.CanRead && pi.CanWrite)
            .Select(pi => (PropNameTrimmer.Replace(pi.Name, ""), pi))
            .ToList();

        private readonly CameraBase camera;

        private Dictionary<string, bool> _supportedSettings = null;

        private DelegateCommand saveCommand;
        private DelegateCommand loadCommand;
        private DelegateCommand submitCommand;
        private DelegateCommand cancelCommand;

        public DelegateCommand SubmitCommand => submitCommand;
        public DelegateCommand CancelCommand => cancelCommand;
        public DelegateCommand SaveCommand => saveCommand;
        public DelegateCommand LoadCommand => loadCommand;

        public (float ExposureTime, float AccumulationCycleTime, 
            float KineticCycleTime, int BufferSize) EstimatedTiming
        {
            get;
            private set;
        }

        /// <summary>
        /// Reference to Camera instance.
        /// </summary>
        public CameraBase Camera => camera;
        /// <summary>
        /// Collection of supported by a given Camera settings.
        /// </summary>
        public Dictionary<string, bool> SupportedSettings => _supportedSettings;

        /// <summary>
        /// Collection of settings that can be set now.
        /// </summary>
        public ObservableConcurrentDictionary<string, bool> AllowedSettings
        {
            get;
            set;
        }

        /// <summary>
        /// Supported acquisition modes.
        /// </summary>
        public AcquisitionMode[] AllowedAcquisitionModes =>
           Helper.EnumFlagsToArray(Camera.Capabilities.AcquisitionModes)
            .Where(item => item != AcquisitionMode.FrameTransfer)
            .Where(ANDOR_CS.Classes.EnumConverter.IsAcquisitionModeSupported)
            .ToArray();

        public ReadMode[] AllowedReadoutModes =>
            Helper.EnumFlagsToArray(Camera.Capabilities.ReadModes)
            .Where(ANDOR_CS.Classes.EnumConverter.IsReadModeSupported)
            .ToArray();

        public TriggerMode[] AllowedTriggerModes =>
            Helper.EnumFlagsToArray(Camera.Capabilities.TriggerModes)
            .Where(ANDOR_CS.Classes.EnumConverter.IsTriggerModeSupported)
            .ToArray();

        public (int Index, float Speed)[] AvailableHSSpeeds =>
            (ADConverterIndex < 0 || OutputAmplifierIndex < 0)
            ? null
            : model
            .GetAvailableHSSpeeds(ADConverterIndex, OutputAmplifierIndex)
            .ToArray();
        public (int Index, string Name)[] AvailablePreAmpGains =>
            (ADConverterIndex < 0 || OutputAmplifierIndex < 0 || HSSpeedIndex < 0)
            ? null
            : model
            .GetAvailablePreAmpGain(ADConverterIndex,
                OutputAmplifierIndex, HSSpeedIndex)
            .ToArray();

        public int[] AvailableEMCCDGains =>
            Enumerable.Range(Camera.Properties.EMCCDGainRange.Low, Camera.Properties.EMCCDGainRange.High)
            .ToArray();

        /// <summary>
        /// Index of VS Speed.
        /// </summary>
        public int VSSpeedIndex
        {
            get => model.VSSpeed?.Index ?? -1;
            set
            {
                try
                {
                    model.SetVSSpeed(value < 0 ? 0 : value);
                    ValidateProperty(null);
                    //RaisePropertyChanged();
                }
                catch (Exception e)
                {
                    ValidateProperty(e);
                }

            }
        }
        /// <summary>
        /// VS Amplitude.
        /// </summary>
        public VSAmplitude? VSAmplitudeValue
        {
            get => model.VSAmplitude;
            set
            {
                try
                {
                    model.SetVSAmplitude(value ?? VSAmplitude.Normal);
                    ValidateProperty(null);
                    //RaisePropertyChanged();
                }
                catch (Exception e)
                {
                    ValidateProperty(e);
                }
            }
        }
        /// <summary>
        /// Analog-Digital COnverter index.
        /// </summary>
        public int ADConverterIndex
        {
            get => model.ADConverter?.Index ?? -1;
            set
            {
                try
                {
                    model.SetADConverter(value < 0 ? 0 : value);
                    ValidateProperty(null);
                    //RaisePropertyChanged();
                }
                catch (Exception e)
                {
                    ValidateProperty(e);
                }
            }
        }
        /// <summary>
        /// Output OutputAmplifier index.
        /// </summary>
        public int OutputAmplifierIndex
        {
            get => model.OutputAmplifier?.Index ?? -1;
            set
            {
                try
                {
                    model.SetOutputAmplifier(camera.Properties.OutputAmplifiers[value < 0 ? 0 : value].OutputAmplifier);
                    ValidateProperty(null);
                    //RaisePropertyChanged();
                }
                catch (Exception e)
                {
                    ValidateProperty(e);
                }
            }
        }
        /// <summary>
        /// HS Speed.
        /// </summary>
        public int HSSpeedIndex
        {
            get => model.HSSpeed?.Index ?? -1;
            set
            {

                try
                {
                    model.SetHSSpeed(value < 0 ? 0 : value);
                    ValidateProperty(null);
                    //RaisePropertyChanged();
                }
                catch (Exception e)
                {
                    ValidateProperty(e);
                }

            }
        }
        /// <summary>
        /// Index of Pre OutputAmplifier Gain.
        /// </summary>
        public int PreAmpGainIndex
        {
            get => model.PreAmpGain?.Index ?? -1;
            set
            {
                try
                {
                    model.SetPreAmpGain(value < 0 ? 0 : value);
                    ValidateProperty(null);
                    //RaisePropertyChanged();

                }
                catch (Exception e)
                {
                    ValidateProperty(e);
                }
            }
        }
        /// <summary>
        /// Acquisition mode value.
        /// </summary>
        public AcquisitionMode? AcquisitionModeValue
        {
            get
            {
                if (model.AcquisitionMode?.HasFlag(AcquisitionMode.FrameTransfer) ?? false)
                    return model.AcquisitionMode ^ AcquisitionMode.FrameTransfer;
                else
                    return model.AcquisitionMode;
            }
            set
            {
                try
                {
                    model.SetAcquisitionMode(value ?? AcquisitionMode.SingleScan);
                    ValidateProperty(null);
                    //RaisePropertyChanged();
                }
                catch (Exception e)
                {
                    ValidateProperty(e);
                }
            }
        }
        /// <summary>
        /// Frame transfer flag; applied to acquisition mode
        /// </summary>
        public bool FrameTransferValue
        {
            get => model.AcquisitionMode?.HasFlag(AcquisitionMode.FrameTransfer) ?? false;
            set
            {
                try
                {
                    if (value)
                    {
                        if (!model.AcquisitionMode?.HasFlag(AcquisitionMode.FrameTransfer) ?? false)
                        {
                            model.SetAcquisitionMode((model.AcquisitionMode | AcquisitionMode.FrameTransfer) ?? AcquisitionMode.FrameTransfer);
                            ValidateProperty(null);
                            //RaisePropertyChanged();
                        }
                    }
                    else
                    {
                        if (model.AcquisitionMode?.HasFlag(AcquisitionMode.FrameTransfer) ?? false)
                        {
                            model.SetAcquisitionMode((model.AcquisitionMode ^ AcquisitionMode.FrameTransfer) ?? AcquisitionMode.SingleScan);
                            ValidateProperty(null);
                            //RaisePropertyChanged();
                        }
                    }
                }
                catch (Exception e)
                {
                    ValidateProperty(e);
                }

            }
        }
        /// <summary>
        /// Read mode value
        /// </summary>
        public ReadMode? ReadoutModeValue
        {
            get => model.ReadoutMode;
            set
            {
                try
                {
                    model.SetReadoutMode(value ?? ReadMode.FullImage);
                    ValidateProperty(null);
                    //RaisePropertyChanged();
                }
                catch (Exception e)
                {
                    ValidateProperty(e);
                }
            }
        }
        /// <summary>
        /// Trigger mode value
        /// </summary>
        public TriggerMode? TriggerModeValue
        {
            get => model.TriggerMode;
            set
            {
                try
                {
                    model.SetTriggerMode(value ?? TriggerMode.Internal);
                    ValidateProperty(null);
                    //RaisePropertyChanged();
                }
                catch (Exception e)
                {
                    ValidateProperty(e);
                }
            }
        }
        /// <summary>
        /// Exposure time; text field
        /// </summary>
        public string ExposureTimeValueText
        {
            get => model?.ExposureTime?.ToString();
            set
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        model.SetExposureTime(0f);
                        ValidateProperty(null);
                        //RaisePropertyChanged();
                    }
                    else if (float.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out float flVal))
                    {
                        model.SetExposureTime(flVal);
                        ValidateProperty(null);
                        //RaisePropertyChanged();
                    }
                    else
                        ValidateProperty(new ArgumentException("Provided value is not a number."));

                }
                catch (Exception e)
                {
                    ValidateProperty(e);
                }
            }

        }
        /// <summary>
        /// EM CCD gain; text field
        /// </summary>
        public string EMCCDGainValueText
        {
            get => model.EMCCDGain.ToString();
            set
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        model.SetEMCCDGain(Camera.Properties.EMCCDGainRange.Low);
                        ValidateProperty();
                        //RaisePropertyChanged();
                    }
                    else if (int.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out int intVal))
                    {
                        model.SetEMCCDGain(intVal);
                        ValidateProperty();
                        //RaisePropertyChanged();
                    }
                    else
                        ValidateProperty(new ArgumentException("Provided value is not a number."));
                }
                catch (Exception e)
                {
                    ValidateProperty(e);
                }
                finally
                {
                    //RaisePropertyChanged();
                }
            }
        }

        public AcquisitionSettingsViewModel(SettingsBase model, CameraBase camera)
            : base(model)
        {
            this.model = model;
            this.camera = camera;


            CheckSupportedFeatures();
            InitializeAllowedSettings();
            InitializeCommands();

        }

        private void CheckSupportedFeatures()
        {
            _supportedSettings = new Dictionary<string, bool>()
            {
                { nameof(model.VSSpeed), camera.Capabilities.SetFunctions.HasFlag(SetFunction.VerticalReadoutSpeed)},
                { nameof(model.VSAmplitude), camera.Capabilities.SetFunctions.HasFlag(SetFunction.VerticalClockVoltage) },
                { nameof(model.ADConverter), true },
                { nameof(model.OutputAmplifier), true },
                { nameof(model.HSSpeed), camera.Capabilities.SetFunctions.HasFlag(SetFunction.HorizontalReadoutSpeed) },
                { nameof(model.PreAmpGain), camera.Capabilities.SetFunctions.HasFlag(SetFunction.PreAmpGain) },
                { nameof(model.AcquisitionMode), true },
                { nameof(FrameTransferValue), true},
                { nameof(model.ReadoutMode), true },
                { nameof(model.TriggerMode), true },
                { nameof(model.ExposureTime), true },
                { nameof(model.EMCCDGain), camera.Capabilities.SetFunctions.HasFlag(SetFunction.EMCCDGain) }
            };
        }

        private void InitializeAllowedSettings()
        {
            AllowedSettings = new ObservableConcurrentDictionary<string, bool>(
                new KeyValuePair<string, bool>[]
                {
                    new KeyValuePair<string, bool>(nameof(model.VSSpeed), true),
                    new KeyValuePair<string, bool>(nameof(model.VSAmplitude), true),
                    new KeyValuePair<string, bool>(nameof(model.ADConverter), true),
                    new KeyValuePair<string, bool>(nameof(model.OutputAmplifier), true),
                    new KeyValuePair<string, bool>(nameof(model.HSSpeed),
                        ADConverterIndex >= 0
                        && OutputAmplifierIndex >= 0),
                    new KeyValuePair<string, bool>(nameof(model.PreAmpGain),
                        ADConverterIndex >= 0
                        && OutputAmplifierIndex >= 0
                        && HSSpeedIndex >= 0),
                    new KeyValuePair<string, bool>(nameof(model.AcquisitionMode), true),
                    new KeyValuePair<string, bool>(nameof(FrameTransferValue), false),
                    new KeyValuePair<string, bool>(nameof(model.ReadoutMode), true),
                    new KeyValuePair<string, bool>(nameof(model.TriggerMode), true),
                    new KeyValuePair<string, bool>(nameof(model.ExposureTime), true),
                    new KeyValuePair<string, bool>(nameof(model.EMCCDGain),
                        (OutputAmplifierIndex >= 0)
                        && Camera.Properties.OutputAmplifiers[OutputAmplifierIndex].OutputAmplifier == OutputAmplification.Conventional)
                }
                );
        }

        private void InitializeCommands()
        {
            submitCommand = new DelegateCommand(
                (param) => CloseView(param, false),
                CanSubmit
                );

            cancelCommand = new DelegateCommand(
                (param) => CloseView(param, true),
                DelegateCommand.CanExecuteAlways
                );

            saveCommand = new DelegateCommand(
                SaveTo,
                DelegateCommand.CanExecuteAlways
                );

            loadCommand = new DelegateCommand(
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
                FileName = camera.ToString(),
                FilterIndex = 0,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Title = "Save current acquisition settings"
            };
            dialog.Filter = $@"Acquisition settings (*{dialog.DefaultExt})|*{dialog.DefaultExt}|All files (*.*)|*.*";
            

            if (dialog.ShowDialog() == true)
                try
                {
                    using (var fl = File.Open(dialog.FileName, FileMode.Create, FileAccess.Write, FileShare.Read))
                        model.Serialize(fl);
                }
                catch (Exception e)
                {
                    var messSize = DIPOL_UF_App.Settings.GetValueOrNullSafe("ExceptionStringLimit", 80);
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

            if (dialog.ShowDialog() == true)
                Task.Run(() =>
                {
                    try
                    {
                        using (var fl = File.Open(dialog.FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                            model.Deserialize(fl);
                    }
                    catch (Exception e)
                    {
                        var messSize = DIPOL_UF_App.Settings.GetValueOrNullSafe("ExceptionStringLimit", 80);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show($"An error occured while reading acquisition settings from {dialog.FileName}.\n" +
                                            $"[{(e.Message.Length <= messSize ? e.Message : e.Message.Substring(0, messSize))}]",
                                "Unable to load file", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                        });
                    }
                });
        }

        private void ValidateProperty(Exception e = null,
            [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                if (e != null)
                    AddError(new ValidationErrorInstance("DefaultError", e.Message),
                       ErrorPriority.High,
                       propertyName);
                else
                    RemoveError(new ValidationErrorInstance("DefaultError", ""),
                        propertyName);

            }
        }

        protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);

            if ((e.PropertyName == nameof(OutputAmplifierIndex) ||
                 e.PropertyName == nameof(ADConverterIndex)) &&
                (AllowedSettings[nameof(model.HSSpeed)]
                    = ADConverterIndex >= 0 &&
                      OutputAmplifierIndex >= 0))
            {
                RaisePropertyChanged(nameof(PreAmpGainIndex));
                RaisePropertyChanged(nameof(HSSpeedIndex));
                RaisePropertyChanged(nameof(AvailableHSSpeeds));
            }

            if ((e.PropertyName == nameof(OutputAmplifierIndex) ||
                 e.PropertyName == nameof(ADConverterIndex) ||
                 e.PropertyName == nameof(HSSpeedIndex)) &&
                (AllowedSettings[nameof(model.PreAmpGain)]
                    = OutputAmplifierIndex >= 0 &&
                      ADConverterIndex >= 0 &&
                      HSSpeedIndex >= 0))
            {
                RaisePropertyChanged(nameof(PreAmpGainIndex));
                RaisePropertyChanged(nameof(AvailablePreAmpGains));
            }

            if (e.PropertyName == nameof(AcquisitionModeValue) &&
                AcquisitionModeValue.HasValue)
            {
                AllowedSettings[nameof(FrameTransferValue)] =
                    AcquisitionModeValue != AcquisitionMode.SingleScan &&
                    AcquisitionModeValue != AcquisitionMode.FastKinetics;
                if (!AllowedSettings[nameof(FrameTransferValue)])
                {
                    FrameTransferValue = false;
                    //RaisePropertyChanged(nameof(FrameTransferValue));
                }
            }
            if (e.PropertyName == nameof(OutputAmplifierIndex))
            {
                AllowedSettings[nameof(model.EMCCDGain)] = (OutputAmplifierIndex >= 0) &&
                    (Camera.Properties.OutputAmplifiers[OutputAmplifierIndex].OutputAmplifier 
                    == OutputAmplification.Conventional);
                RaisePropertyChanged(nameof(EMCCDGainValueText));
            }

            SubmitCommand?.OnCanExecuteChanged();

        }

        protected override void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnModelPropertyChanged(sender, e);
            var prop = PropertyList
                .FirstOrDefault(item => item.Item1 == e.PropertyName);

            if (prop.Item2 != null)
               Application.Current.Dispatcher
                    .Invoke(() => RaisePropertyChanged(prop.Item2.Name));

            if (e.PropertyName == nameof(model.AcquisitionMode))
                Application.Current.Dispatcher
                   .Invoke(() => RaisePropertyChanged(nameof(FrameTransferValue)));
        }

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
                            var applicationResult = model.ApplySettings(out var timing);
                            var failed = applicationResult.Where(item => !item.Success).ToList();
                            if (failed.Count > 0)
                            {
                                var messSize = DIPOL_UF_App.Settings.GetValueOrNullSafe("ExceptionStringLimit", 80);
                                var listSize = DIPOL_UF_App.Settings.GetValueOrNullSafe("ExceptionStringLimit", 5);
                                var sb = new StringBuilder(messSize * listSize);

                                sb.AppendLine("Some of the settings were applied unsuccessfully:");
                                foreach (var fl in failed.Take(listSize))
                                    sb.AppendLine($"[{(fl.Option.Length < messSize ? fl.Option : fl.Option.Substring(0, messSize))}]");
                                MessageBox.Show(sb.ToString(),
                                    "Partially unsuccessfull application of settings", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);

                                foreach (var prop in PropertyList.Join(failed, x => x.Item1, y => y.Option, (x, y) =>
                                    new {
                                        Name = x.Item1,
                                        Error = y.ReturnCode
                                    }))
                                    ValidateProperty(new AndorSdkException(prop.Name, prop.Error), prop.Name);

                                return;
                            }
                            EstimatedTiming = timing;
                        }
                        catch (Exception e)
                        {
                            var messSize = DIPOL_UF_App.Settings.GetValueOrNullSafe("ExceptionStringLimit", 80);
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
            // Helper function, checks if value is set.
            bool ValueIsSet(PropertyInfo p)
            {
                if (Nullable.GetUnderlyingType(p.PropertyType) != null)
                    return p.GetValue(this) != null;
                if (p.PropertyType == typeof(int))
                    return (int)p.GetValue(this) != -1;
                if (p.PropertyType == typeof(string))
                    return !string.IsNullOrWhiteSpace((string)p.GetValue(this));
                return false;
            }

            // Query that joins pulic Properties to Allowed settings with true value.
            // As a result, propsQuery stores all Proprties that should have values set.
            var propsQuery =
                from prop in PropertyList
                join allowedProp in AllowedSettings 
                on prop.Item1 equals allowedProp.Key
                where allowedProp.Value
                select prop.Item2;

            // Runs check of values on all selected properties.
            return propsQuery.All(ValueIsSet);
        }
    }
}
