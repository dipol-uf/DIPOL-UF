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
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Threading.Tasks;
using System.IO;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Input;
using ANDOR_CS.Classes;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;
using ANDOR_CS.Exceptions;
using DynamicData;
using DynamicData.Binding;
using MathNet.Numerics;
using Microsoft.Xaml.Behaviors.Core;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using static DIPOL_UF.Validators.Validate;
using EnumConverter = ANDOR_CS.Classes.EnumConverter;
using MessageBox = System.Windows.MessageBox;
using Window = System.Windows.Window;

// ReSharper disable UnassignedGetOnlyAutoProperty

namespace DIPOL_UF.ViewModels
{
    internal sealed class AcquisitionSettingsViewModel : ReactiveViewModel<ReactiveWrapper<SettingsBase>>
    {
        public class DialogRequestedEventArgs : EventArgs
        {
            public FileDialogDescriptor Descriptor { get; }

            public DialogRequestedEventArgs(FileDialogDescriptor desc)
                => Descriptor = desc;
        }

        private static List<(PropertyInfo Property, string EquivalentName)> InteractiveSettings { get; }
            = typeof(AcquisitionSettingsViewModel)
              .GetProperties(BindingFlags.Public | BindingFlags.Instance)
              .Where(x => !(x.GetCustomAttribute<ReactiveAttribute>() is null))
              .Select(x => (Property: x,
                  EquivalentName: x.GetCustomAttribute<UnderlyingCameraSettingsAttribute>()?.AndorName
                                   .ToLowerInvariant()))
              .Where(x => !(x.EquivalentName is null))
              .ToList();


        public class SettingsAvailability : ReactiveObjectEx
        {
            public bool VsSpeed { [ObservableAsProperty] get; }
            public bool VsAmplitude { [ObservableAsProperty] get; }
            public bool AdcBitDepth { [ObservableAsProperty] get; }
            public bool Amplifier { [ObservableAsProperty] get; }
            public bool HsSpeed { [ObservableAsProperty] get; }
            public bool PreAmpGain { [ObservableAsProperty] get; }
            public bool AcquisitionMode { [ObservableAsProperty] get; }
            public bool ExposureTimeText { [ObservableAsProperty] get; }
            public bool FrameTransfer { [ObservableAsProperty] get; }
            public bool ReadMode { [ObservableAsProperty] get; }
            public bool TriggerMode { [ObservableAsProperty] get; }
            public bool EmCcdGainText { [ObservableAsProperty] get; }
            public bool ImageArea { [ObservableAsProperty] get; }
            public bool AccumulateCycleTime { [ObservableAsProperty] get; }
            public bool AccumulateCycleNumber { [ObservableAsProperty] get; }
            public bool KineticCycleTime { [ObservableAsProperty] get; }
            public bool KineticCycleNumber { [ObservableAsProperty] get; }
            public bool KineticCycleBlock { [ObservableAsProperty] get; }
        }

        private readonly SourceCache<(int Index, float Speed), int> _availableHsSpeeds
            = new SourceCache<(int Index, float Speed), int>(x => x.Index);

        private readonly  SourceCache<(int Index, string Name), int> _availablePreAmpGains
            = new SourceCache<(int Index, string Name), int>(x => x.Index);

        private readonly SourceCache<ReadMode, ReadMode> _availableReadModes
            = new SourceCache<ReadMode, ReadMode>(x => x);

        private string[] Group1Names { get; }
        private string[] Group2Names { get; }
        private string[] Group3Names { get; }

        public event EventHandler FileDialogRequested;

        public SettingsAvailability IsAvailable { get; }
            = new SettingsAvailability();
        public CameraBase Camera => Model.Object.Camera;

        public ICommand CancelCommand { get; private set; }
        public ReactiveCommand<Window, Window> SubmitCommand { get; private set; }
        public ReactiveCommand<Window, Unit> ViewLoadedCommand { get; private set; }
        public ReactiveCommand<Unit, FileDialogDescriptor> SaveButtonCommand { get; private set; }
        public ReactiveCommand<Unit, FileDialogDescriptor> LoadButtonCommand { get; private set; }
        public ReactiveCommand<string, Unit> SaveActionCommand { get; private set; }
        public ReactiveCommand<string, Unit> LoadActionCommand { get; private set; }

        /// <summary>
        /// Collection of supported by a given Camera settings.
        /// </summary>
        public HashSet<string> SupportedSettings { get; }
        /// <summary>
        /// Collection of settings that can be set now.
        /// </summary>
        public HashSet<string> AllowedSettings
        {
            get;
        }

        /// <summary>
        /// Supported acquisition modes.
        /// </summary>
        public AcquisitionMode[] AllowedAcquisitionModes =>
           Helper.EnumFlagsToArray<AcquisitionMode>(Model.Object.Camera.Capabilities.AcquisitionModes)
            .Where(item => item != ANDOR_CS.Enums.AcquisitionMode.FrameTransfer)
            .Where(EnumConverter.IsAcquisitionModeSupported)
            .ToArray();
        public TriggerMode[] AllowedTriggerModes =>
            Helper.EnumFlagsToArray<TriggerMode>(Model.Object.Camera.Capabilities.TriggerModes)
            .Where(EnumConverter.IsTriggerModeSupported)
            .ToArray();
        public IObservableCollection<(int Index, float Speed)> AvailableHsSpeeds { get; }
            = new ObservableCollectionExtended<(int Index, float Speed)>();
        public IObservableCollection<(int Index, string Name)> AvailablePreAmpGains { get; }
            = new ObservableCollectionExtended<(int Index, string Name)>();
        public IObservableCollection<ReadMode> AvailableReadModes { get; }
        = new ObservableCollectionExtended<ReadMode>();

        public string DetectorSize =>
            string.Format(Properties.Localization.AcquisitionSettings_DetectorSize_Format,
                Camera.Properties.DetectorSize.Horizontal,
                Camera.Properties.DetectorSize.Vertical);

        public bool Group1ContainsErrors { [ObservableAsProperty] get; }
        public bool Group2ContainsErrors { [ObservableAsProperty] get; }
        public bool Group3ContainsErrors { [ObservableAsProperty] get; }
        [Reactive]
        public string AllowedGain {  get; private set; }

        public AcquisitionSettingsViewModel(ReactiveWrapper<SettingsBase> model)
            : base(model)
        {
#if DEBUG
            Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                          x => Model.Object.PropertyChanged += x,
                          x => Model.Object.PropertyChanged -= x)
                      .Select(x =>
                      {
                          var name = x.EventArgs.PropertyName;
                          var val = x.Sender.GetType().GetProperty(name)?.GetValue(x.Sender);
                          return $"{name}\t{val?.ToString()}";
                      })
                      .LogObservable("SETTINGS", Subscriptions);
#endif
            SupportedSettings = Model.Object.SupportedSettings();
            AllowedSettings = Model.Object.AllowedSettings();

            Group1Names = new[]
            {
                nameof(VsSpeed),
                nameof(VsAmplitude),
                nameof(AdcBitDepth),
                nameof(Amplifier),
                nameof(HsSpeed),
                nameof(PreAmpGain),
                nameof(AcquisitionMode),
                nameof(ExposureTimeText),
                nameof(FrameTransfer),
                nameof(ReadMode),
                nameof(TriggerMode),
                nameof(EmCcdGainText)
            };

            Group2Names = new[]
            {
                nameof(ImageArea_X1),
                nameof(ImageArea_X2),
                nameof(ImageArea_Y1),
                nameof(ImageArea_Y2)
            };

            Group3Names = new[]
            {
                nameof(AccumulateCycleTime),
                nameof(AccumulateCycleNumber),
                nameof(KineticCycleTime),
                nameof(KineticCycleNumber)
            };


            InitializeCommands();

            WatchItemSources();
            HookObservables();
            HookValidators();

            _availableHsSpeeds.DisposeWith(Subscriptions);
            _availablePreAmpGains.DisposeWith(Subscriptions);
            _availableReadModes.DisposeWith(Subscriptions);

            if (Model.Object?.ADConverter.HasValue == true
                && Model.Object.OutputAmplifier.HasValue)
            {
                _availableHsSpeeds.Edit(context =>
                    context.Load(Model.Object.GetAvailableHSSpeeds(
                        Model.Object.ADConverter.Value.Index,
                        Model.Object.OutputAmplifier.Value.Index)));
            }

            if (Model.Object?.ADConverter.HasValue == true
                && Model.Object.OutputAmplifier.HasValue
                && Model.Object.HSSpeed.HasValue)
            {
                _availablePreAmpGains.Edit(context =>
                    context.Load(Model.Object.GetAvailablePreAmpGain(
                        Model.Object.ADConverter.Value.Index,
                        Model.Object.OutputAmplifier.Value.Index,
                        Model.Object.HSSpeed.Value.Index)));
            }

            if (Model.Object?.AcquisitionMode.HasValue == true)
            {
                var isFmt = Model.Object.AcquisitionMode.Value.HasFlag(ANDOR_CS.Enums.AcquisitionMode.FrameTransfer);
                _availableReadModes.Edit(context => context.Load(
                    Helper.EnumFlagsToArray<ReadMode>(isFmt ? Camera.Capabilities.FtReadModes : Camera.Capabilities.ReadModes)));
            }

            if (Model.Object?.OutputAmplifier.HasValue == true
                && Model.Object.OutputAmplifier.Value.OutputAmplifier == OutputAmplification.ElectronMultiplication)
            {

                var (low, high) = Model.Object.GetEmGainRange();
                AllowedGain = string.Format(Properties.Localization.AcquisitionSetttings_AvailableGainFormat,
                    low, high);
            }
        }

        private void HookObservables()
        {
            WatchAvailableSettings();
            AttachAccessors();

            _availableHsSpeeds
                .Connect()
                .ObserveOnUi()
                .Bind(AvailableHsSpeeds)
                .SubscribeDispose(Subscriptions);

            _availablePreAmpGains
                .Connect()
                .ObserveOnUi()
                .Bind(AvailablePreAmpGains)
                .SubscribeDispose(Subscriptions);

            _availableReadModes
                .Connect()
                .ObserveOnUi()
                .Bind(AvailableReadModes)
                .SubscribeDispose(Subscriptions);

            this.WhenAnyPropertyChanged(nameof(Amplifier))
                .Select(x => x.Amplifier)
                .DistinctUntilChanged()
                .Subscribe(_ => EmCcdGainText = null)
                .DisposeWith(Subscriptions);

            IsAvailable.WhenPropertyChanged(x => x.KineticCycleTime)
                       .Select(x => x.Value)
                       .CombineLatest(
                            IsAvailable.WhenPropertyChanged(y => y.KineticCycleNumber)
                                       .Select(y => y.Value),
                           (x, y) => x || y)
                       .ToPropertyEx(IsAvailable, x => x.KineticCycleBlock)
                       .DisposeWith(Subscriptions);

        }

        private void AttachAccessors()
        {
            AttachGetters();
            AttachSetters();
        }

        private void AttachSetters()
        {
            void CreateSetter<TSrc, TTarget>(
                Expression<Func<AcquisitionSettingsViewModel, TSrc>> sourceAccessor,
                Func<TSrc, bool> condition,
                Func<TSrc, TTarget> selector,
                Action<TTarget> setter)
            {
                var name = (sourceAccessor.Body as MemberExpression)?.Member.Name
                           ?? throw new ArgumentException(
                               Properties.Localization.General_ShouldNotHappen,
                               nameof(sourceAccessor));

                this.WhenPropertyChanged(sourceAccessor)
                    .Where(x => condition(x.Value))
                    .Select(x => DoesNotThrow(setter, selector(x.Value)))
                    .ObserveOnUi()
                    .Subscribe(x => UpdateErrors(name, nameof(DoesNotThrow), x))
                    .DisposeWith(Subscriptions);
            }

            void CreateStringToIntSetter(
                Expression<Func<AcquisitionSettingsViewModel, string>> sourceAccessor,
                Action<int> setter,
                Expression<Func<SettingsAvailability, bool>> availability)
            {
                var name = (sourceAccessor.Body as MemberExpression)?.Member.Name
                           ?? throw new ArgumentException(
                               Properties.Localization.General_ShouldNotHappen,
                               nameof(sourceAccessor));

                var srcGetter = sourceAccessor.Compile();
                var avGetter = availability.Compile();

                this.WhenPropertyChanged(sourceAccessor).Select(_ => Unit.Default)
                    .Merge(
                        IsAvailable.WhenPropertyChanged(availability).Select(_ => Unit.Default))
                    .Subscribe(x =>
                    {
                        string test1 = null;
                        string test2 = null;
                        if (avGetter(IsAvailable))
                        {
                            test1 = CanBeParsed(srcGetter(this), out int result);

                            if (string.IsNullOrEmpty(test1))
                                test2 = DoesNotThrow(setter, result);
                        }

                        UpdateErrors(name, nameof(CanBeParsed), test1);
                        UpdateErrors(name, nameof(DoesNotThrow), test2);

                    }).DisposeWith(Subscriptions);
            }

            void CreateStringToFloatSetter(
                Expression<Func<AcquisitionSettingsViewModel, string>> sourceAccessor,
                Action<float> setter,
                Expression<Func<SettingsAvailability, bool>> availability)
            {
                var name = (sourceAccessor.Body as MemberExpression)?.Member.Name
                           ?? throw new ArgumentException(
                               Properties.Localization.General_ShouldNotHappen,
                               nameof(sourceAccessor));

                var srcGetter = sourceAccessor.Compile();
                var avGetter = availability.Compile();

                this.WhenPropertyChanged(sourceAccessor).Select(_ => Unit.Default)
                    .Merge(
                        IsAvailable.WhenPropertyChanged(availability).Select(_ => Unit.Default))
                    .Subscribe(x =>
                    {
                        string test1 = null;
                        string test2 = null;
                        if (avGetter(IsAvailable))
                        {
                            test1 = CanBeParsed(srcGetter(this), out float result);

                            if (string.IsNullOrEmpty(test1))
                                test2 = DoesNotThrow(setter, result);
                        }

                            BatchUpdateErrors(
                                (name, nameof(CanBeParsed), test1),
                                (name, nameof(DoesNotThrow), test2));
                    }).DisposeWith(Subscriptions);
            }

            // ReSharper disable PossibleInvalidOperationException
            CreateSetter(x => x.VsSpeed, y => y >= 0, z => z, Model.Object.SetVSSpeed);
            CreateSetter(x => x.VsAmplitude, y => y.HasValue, z => z.Value, Model.Object.SetVSAmplitude);
            CreateSetter(x => x.AdcBitDepth, y => y >= 0, z => z, Model.Object.SetADConverter);
            CreateSetter(x => x.Amplifier, y => y.HasValue, z => z.Value, Model.Object.SetOutputAmplifier);
            CreateSetter(x => x.HsSpeed, y => y >= 0 && y < AvailableHsSpeeds.Count,
                z => z, Model.Object.SetHSSpeed);
            CreateSetter(x => x.PreAmpGain, y => y >= 0 && y < AvailablePreAmpGains.Count,
                z => z, Model.Object.SetPreAmpGain);
            CreateSetter(x => x.TriggerMode, y => y.HasValue, z => z.Value, Model.Object.SetTriggerMode);
            CreateSetter(x => x.ReadMode, y => y.HasValue, z => z.Value, Model.Object.SetReadoutMode);

            CreateStringToFloatSetter(x => x.ExposureTimeText, Model.Object.SetExposureTime, 
                y => y.ExposureTimeText);
            CreateStringToIntSetter(x => x.EmCcdGainText, Model.Object.SetEmCcdGain, y => y.EmCcdGainText);

            this.NotifyWhenAnyPropertyChanged(
                    nameof(ImageArea_X1), nameof(ImageArea_X2),
                    nameof(ImageArea_Y1), nameof(ImageArea_Y2))
                .Merge(IsAvailable.NotifyWhenAnyPropertyChanged(nameof(IsAvailable.ImageArea)))
                .Subscribe(_ =>
                {
                    var firstTest = new string[] {null, null, null, null};
                    string secondTest = null;

                    if (IsAvailable.ImageArea)
                    {
                        firstTest[0] = CanBeParsed(ImageArea_X1, out int x1);
                        firstTest[1] = CanBeParsed(ImageArea_Y1, out int y1);
                        firstTest[2] = CanBeParsed(ImageArea_X2, out int x2);
                        firstTest[3] = CanBeParsed(ImageArea_Y2, out int y2);

                        if (firstTest.All(y => y is null)
                            && (secondTest =
                                DoesNotThrow(x => new Rectangle(x), (x1, y1, x2, y2), out var rect)) is null)
                            secondTest = DoesNotThrow(Model.Object.SetImageArea, rect);
                    }

                    BatchUpdateErrors(
                        (nameof(ImageArea_X1), nameof(CanBeParsed), firstTest[0]),
                        (nameof(ImageArea_Y1), nameof(CanBeParsed), firstTest[1]),
                        (nameof(ImageArea_X2), nameof(CanBeParsed), firstTest[2]),
                        (nameof(ImageArea_Y2), nameof(CanBeParsed), firstTest[3]),
                        (nameof(ImageArea_X1), nameof(DoesNotThrow), secondTest),
                        (nameof(ImageArea_Y1), nameof(DoesNotThrow), secondTest),
                        (nameof(ImageArea_X2), nameof(DoesNotThrow), secondTest),
                        (nameof(ImageArea_Y2), nameof(DoesNotThrow), secondTest));

                })
                .DisposeWith(Subscriptions);

            this.WhenAnyPropertyChanged(nameof(AcquisitionMode), nameof(FrameTransfer))
                .Where(x => x.AcquisitionMode.HasValue)
                .Select(x => x.FrameTransfer
                    ? x.AcquisitionMode.Value | ANDOR_CS.Enums.AcquisitionMode.FrameTransfer
                    : x.AcquisitionMode.Value)
                .Select(x => DoesNotThrow(Model.Object.SetAcquisitionMode, x))
                .ObserveOnUi()
                .Subscribe(x =>
                {
                    if (IsAvailable.AcquisitionMode)
                        UpdateErrors(nameof(AcquisitionMode), nameof(DoesNotThrow), x);
                    if (IsAvailable.FrameTransfer)
                        UpdateErrors(nameof(FrameTransfer), nameof(DoesNotThrow), x);
                })
                .DisposeWith(Subscriptions);

            this.NotifyWhenAnyPropertyChanged(
                    nameof(AccumulateCycleTime),
                    nameof(AccumulateCycleNumber))
                .Merge(IsAvailable.NotifyWhenAnyPropertyChanged(
                    nameof(IsAvailable.AccumulateCycleTime),
                    nameof(IsAvailable.AccumulateCycleNumber)))
                .Subscribe(_ =>
                {
                    if (!IsAvailable.AccumulateCycleTime && !IsAvailable.AccumulateCycleNumber)
                        BatchUpdateErrors(
                            (nameof(AccumulateCycleTime), nameof(CanBeParsed), null),
                            (nameof(AccumulateCycleNumber), nameof(CanBeParsed), null),
                            (nameof(AccumulateCycleTime), nameof(DoesNotThrow), null),
                            (nameof(AccumulateCycleNumber), nameof(DoesNotThrow), null));
                    else if (IsAvailable.AccumulateCycleTime && IsAvailable.AccumulateCycleNumber)
                    {
                        string secondTest = default;

                        var firstTestTime = CanBeParsed(AccumulateCycleTime, out float time);
                        var firstTestNumber = CanBeParsed(AccumulateCycleNumber, out int frames);

                        if (firstTestTime is null && firstTestNumber is null
                                                  && Model.Object.AccumulateCycle != (frames, time))
                            secondTest = DoesNotThrow(Model.Object.SetAccumulateCycle, frames, time);

                        BatchUpdateErrors(
                            (nameof(AccumulateCycleTime), nameof(CanBeParsed), firstTestTime),
                            (nameof(AccumulateCycleNumber), nameof(CanBeParsed), firstTestNumber),
                            (nameof(AccumulateCycleTime), nameof(DoesNotThrow), secondTest),
                            (nameof(AccumulateCycleNumber), nameof(DoesNotThrow), secondTest));
                    }
                    else if (IsAvailable.AccumulateCycleTime)
                    {
                        string secondTest = default;

                        var firstTest = CanBeParsed(AccumulateCycleTime, out float time);

                        if (firstTest is null
                            && Model.Object.AccumulateCycle?.Time.AlmostEqual(time) != true)
                            secondTest = DoesNotThrow(Model.Object.SetAccumulateCycle,
                                Model.Object.AccumulateCycle?.Frames ?? 0, time);

                        BatchUpdateErrors(
                            (nameof(AccumulateCycleTime), nameof(CanBeParsed), firstTest),
                            (nameof(AccumulateCycleTime), nameof(DoesNotThrow), secondTest));
                    }
                    else if (IsAvailable.AccumulateCycleNumber)
                    {
                        string secondTest = default;

                        var firstTest = CanBeParsed(AccumulateCycleNumber, out int frames);

                        if (firstTest is null
                            && Model.Object.AccumulateCycle?.Frames != frames)
                            secondTest = DoesNotThrow(Model.Object.SetAccumulateCycle,
                                frames, Model.Object.AccumulateCycle?.Time ?? 0f);

                        BatchUpdateErrors(
                            (nameof(AccumulateCycleNumber), nameof(CanBeParsed), firstTest),
                            (nameof(AccumulateCycleNumber), nameof(DoesNotThrow), secondTest));
                    }
                })
                .DisposeWith(Subscriptions);


            this.NotifyWhenAnyPropertyChanged(
                    nameof(KineticCycleTime),
                    nameof(KineticCycleNumber))
                .Merge(IsAvailable.NotifyWhenAnyPropertyChanged(
                    nameof(IsAvailable.KineticCycleTime),
                    nameof(IsAvailable.KineticCycleNumber)))
                .Subscribe(_ =>
                {
                    if (!IsAvailable.KineticCycleTime && !IsAvailable.KineticCycleNumber)
                        BatchUpdateErrors(
                            (nameof(KineticCycleTime), nameof(CanBeParsed), null),
                            (nameof(KineticCycleNumber), nameof(CanBeParsed), null),
                            (nameof(KineticCycleTime), nameof(DoesNotThrow), null),
                            (nameof(KineticCycleNumber), nameof(DoesNotThrow), null));
                    else if (IsAvailable.KineticCycleTime && IsAvailable.KineticCycleNumber)
                    {
                        string secondTest = default;

                        var firstTestTime = CanBeParsed(KineticCycleTime, out float time);
                        var firstTestNumber = CanBeParsed(KineticCycleNumber, out int frames);

                        if (firstTestTime is null && firstTestNumber is null
                                                  && Model.Object.KineticCycle != (frames, time))
                            secondTest = DoesNotThrow(Model.Object.SetKineticCycle, frames, time);

                        BatchUpdateErrors(
                            (nameof(KineticCycleTime), nameof(CanBeParsed), firstTestTime),
                            (nameof(KineticCycleNumber), nameof(CanBeParsed), firstTestNumber),
                            (nameof(KineticCycleTime), nameof(DoesNotThrow), secondTest),
                            (nameof(KineticCycleNumber), nameof(DoesNotThrow), secondTest));
                    }
                    else if (IsAvailable.KineticCycleTime)
                    {
                        string secondTest = default;

                        var firstTest = CanBeParsed(KineticCycleTime, out float time);

                        if (firstTest is null
                            && Model.Object.KineticCycle?.Time.AlmostEqual(time) != true)
                            secondTest = DoesNotThrow(Model.Object.SetKineticCycle,
                                Model.Object.KineticCycle?.Frames ?? 0, time);

                        BatchUpdateErrors(
                            (nameof(KineticCycleTime), nameof(CanBeParsed), firstTest),
                            (nameof(KineticCycleTime), nameof(DoesNotThrow), secondTest));
                    }
                    else if (IsAvailable.KineticCycleNumber)
                    {
                        string secondTest = default;

                        var firstTest = CanBeParsed(KineticCycleNumber, out int frames);

                        if (firstTest is null
                            && Model.Object.KineticCycle?.Frames != frames)
                            secondTest = DoesNotThrow(Model.Object.SetKineticCycle,
                                frames, Model.Object.KineticCycle?.Time ?? 0f);

                        BatchUpdateErrors(
                            (nameof(KineticCycleNumber), nameof(CanBeParsed), firstTest),
                            (nameof(KineticCycleNumber), nameof(DoesNotThrow), secondTest));
                    }
                })
                .DisposeWith(Subscriptions);
            // ReSharper restore PossibleInvalidOperationException
        }

        private void AttachGetters()
        {
            bool FloatIsNotEqualString(float? src, string tar)
                => src?.AlmostEqual(float.TryParse(tar, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var val)
                       ? val
                       : float.NaN) != true;

            bool IntIsNotEqualString(int? src, string tar)
                => src?.Equals(int.TryParse(tar, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var val)
                       ? val
                       : 0) != true;

            void CreateGetter<TSrc, TTarget>(
                Expression<Func<SettingsBase, TSrc>> sourceAccessor,
                Func<TSrc, TTarget> selector,
                Expression<Func<AcquisitionSettingsViewModel, TTarget>> targetAccessor,
                Func<TSrc, TTarget, bool> comparator = null)
                => Model.Object.WhenPropertyChanged(sourceAccessor)
                        .ModifyIf(!(comparator is null),
                            // ReSharper disable once PossibleNullReferenceException
                            x => x.Where(y => comparator(y.Value, targetAccessor.Compile()(this))))
                        .Select(x => selector(x.Value))
                        .DistinctUntilChanged()
                        .ObserveOnUi()
                        .BindTo(this, targetAccessor)
                        .DisposeWith(Subscriptions);

            CreateGetter(x => x.VSSpeed, y => y?.Index ?? -1, z => z.VsSpeed, (src, tar) => src?.Index != tar);
            CreateGetter(x => x.VSAmplitude, y => y, z => z.VsAmplitude, (src, tar) => src != tar);
            CreateGetter(x => x.ADConverter, y => y?.Index ?? -1, z => z.AdcBitDepth, (src, tar) => src?.Index != tar);
            CreateGetter(x => x.OutputAmplifier, y => y?.OutputAmplifier, 
                z => z.Amplifier, (src, tar) => src?.OutputAmplifier != tar);
            CreateGetter(x => x.HSSpeed, y => y?.Index ?? -1, z => z.HsSpeed, (src, tar) => src?.Index != tar);
            CreateGetter(x => x.PreAmpGain, y => y?.Index ?? -1, z => z.PreAmpGain, (src, tar) => src?.Index != tar);
            CreateGetter(x => x.TriggerMode, y => y, z => z.TriggerMode, (src, tar) => src != tar);
            CreateGetter(x => x.ReadoutMode, y => y, z => z.ReadMode, (src, tar) => src != tar);

            CreateGetter(x => x.ExposureTime,
                y => y?.ToString(Properties.Localization.General_ExposureFloatFormat),
                z => z.ExposureTimeText,
                FloatIsNotEqualString);

            CreateGetter(x => x.EMCCDGain, 
                y => y?.ToString(Properties.Localization.General_IntegerFormat),
                z => z.EmCcdGainText,
                IntIsNotEqualString);

            CreateGetter(x => x.ImageArea,
                y => y?.X1.ToString(Properties.Localization.General_IntegerFormat),
                z => z.ImageArea_X1,
                (src, tar) => IntIsNotEqualString(src?.X1, tar));
            CreateGetter(x => x.ImageArea,
                y => y?.Y1.ToString(Properties.Localization.General_IntegerFormat),
                z => z.ImageArea_Y1,
                (src, tar) => IntIsNotEqualString(src?.Y1, tar));
            CreateGetter(x => x.ImageArea,
                y => y?.X2.ToString(Properties.Localization.General_IntegerFormat),
                z => z.ImageArea_X2,
                (src, tar) => IntIsNotEqualString(src?.X2, tar));
            CreateGetter(x => x.ImageArea,
                y => y?.Y2.ToString(Properties.Localization.General_IntegerFormat),
                z => z.ImageArea_Y2,
                (src, tar) => IntIsNotEqualString(src?.Y2, tar));

            CreateGetter(x => x.AccumulateCycle,
                y => y?.Time.ToString(Properties.Localization.General_ExposureFloatFormat),
                z => z.AccumulateCycleTime,
                (src, tar) => FloatIsNotEqualString(src?.Time, tar));
            CreateGetter(x => x.AccumulateCycle,
                y => y?.Frames.ToString(Properties.Localization.General_IntegerFormat),
                z => z.AccumulateCycleNumber,
                (src, tar) => IntIsNotEqualString(src?.Frames, tar));

            CreateGetter(x => x.KineticCycle,
                y => y?.Time.ToString(Properties.Localization.General_ExposureFloatFormat),
                z => z.KineticCycleTime,
                (src, tar) => FloatIsNotEqualString(src?.Time, tar));
            CreateGetter(x => x.KineticCycle,
                y => y?.Frames.ToString(Properties.Localization.General_IntegerFormat),
                z => z.KineticCycleNumber,
                (src, tar) => IntIsNotEqualString(src?.Frames, tar));

            var acqModeObs =
                Model.Object.WhenPropertyChanged(x => x.AcquisitionMode)
                     .Select(x =>
                     {
                         var hasFt = x.Value?.HasFlag(ANDOR_CS.Enums.AcquisitionMode.FrameTransfer) ??
                                     false;

                         return (
                             Mode: hasFt
                                 ? x.Value ^ ANDOR_CS.Enums.AcquisitionMode.FrameTransfer
                                 : x.Value,
                             FrameTransfer: hasFt);
                     })
                     .DistinctUntilChanged();

            acqModeObs.Select(x => x.Mode)
                      .Where(x => x != AcquisitionMode)
                      .DistinctUntilChanged()
                      .ObserveOnUi()
                      .BindTo(this, x => x.AcquisitionMode)
                      .DisposeWith(Subscriptions);

            acqModeObs.Select(x => x.FrameTransfer)
                      .Where(x => x != FrameTransfer)
                      .DistinctUntilChanged()
                      .ObserveOnUi()
                      .BindTo(this, x => x.FrameTransfer)
                      .DisposeWith(Subscriptions);
        }

        protected override void HookValidators()
        {
            base.HookValidators();

            SetUpDefaultValueValidators();

            ObserveHasErrors
                .Throttle(UiSettingsProvider.UiThrottlingDelay)
                .Select(_ => Group1Names.Any(HasSpecificErrors))
                .ObserveOnUi()
                .ToPropertyEx(this, x => x.Group1ContainsErrors)
                .DisposeWith(Subscriptions);

            ObserveHasErrors
                .Throttle(UiSettingsProvider.UiThrottlingDelay)
                .Select(_ => Group2Names.Any(HasSpecificErrors))
                .ObserveOnUi()
                .ToPropertyEx(this, x => x.Group2ContainsErrors)
                .DisposeWith(Subscriptions);

            ObserveHasErrors
                .Throttle(UiSettingsProvider.UiThrottlingDelay)
                .Select(_ => Group3Names.Any(HasSpecificErrors))
                .ObserveOnUi()
                .ToPropertyEx(this, x => x.Group3ContainsErrors)
                .DisposeWith(Subscriptions);

        }

        private void SetUpDefaultValueValidators()
        {
            void DefaultValueValidator<TSrc>(
                Expression<Func<AcquisitionSettingsViewModel, TSrc>> accessor,
                TSrc comparisonValue,
                Expression<Func<SettingsAvailability, bool>> availability)
            {
                var name = (accessor.Body as MemberExpression)?.Member.Name
                           ?? throw new ArgumentException(
                               Properties.Localization.General_ShouldNotHappen,
                               nameof(accessor));

                CreateValidator(
                    this.WhenPropertyChanged(accessor)
                        .CombineLatest(IsAvailable.WhenPropertyChanged(availability),
                            (x, y) => (x.Value, IsAvailable: y.Value))
                        .Select(x => (
                            Type: nameof(CannotBeDefault),
                            Message: x.IsAvailable
                                ? CannotBeDefault(x.Value, comparisonValue)
                                : null)),
                    name);
            }

            void DefaultStringValueValidator(
                Expression<Func<AcquisitionSettingsViewModel, string>> accessor,
                Expression<Func<SettingsAvailability, bool>> availability)
            {
                var name = (accessor.Body as MemberExpression)?.Member.Name
                           ?? throw new ArgumentException(
                               Properties.Localization.General_ShouldNotHappen,
                               nameof(accessor));

                CreateValidator(
                    this.WhenPropertyChanged(accessor)
                        .CombineLatest(IsAvailable.WhenPropertyChanged(availability),
                            (x, y) => (x.Value, IsAvailable: y.Value))
                        .Select(x => (
                            Type: nameof(CannotBeDefault),
                            Message: x.IsAvailable
                                ? CannotBeDefault(x.Value)
                                : null))
                        .ObserveOnUi(),
                    name);
            }

            DefaultValueValidator(x => x.VsSpeed, -1, y => y.VsSpeed);
            DefaultValueValidator(x => x.VsAmplitude, null, y=> y.VsAmplitude);
            DefaultValueValidator(x => x.AdcBitDepth, -1, y => y.AdcBitDepth);
            DefaultValueValidator(x => x.Amplifier, null, y => y.Amplifier);
            DefaultValueValidator(x => x.HsSpeed, -1, y => y.HsSpeed);
            DefaultValueValidator(x => x.PreAmpGain, -1, y => y.PreAmpGain);
            DefaultValueValidator(x => x.AcquisitionMode, null, x => x.AcquisitionMode);
            DefaultValueValidator(x => x.TriggerMode, null, y => y.TriggerMode);
            DefaultValueValidator(x => x.ReadMode, null, y => y.ReadMode);
            
            DefaultStringValueValidator(x => x.ExposureTimeText, y => y.ExposureTimeText);
            DefaultStringValueValidator(x => x.EmCcdGainText, y => y.EmCcdGainText);
            DefaultStringValueValidator(x => x.ImageArea_X1, y => y.ImageArea);
            DefaultStringValueValidator(x => x.ImageArea_Y1, y => y.ImageArea);
            DefaultStringValueValidator(x => x.ImageArea_X2, y => y.ImageArea);
            DefaultStringValueValidator(x => x.ImageArea_Y2, y => y.ImageArea);

            DefaultStringValueValidator(x => x.AccumulateCycleTime, y => y.AccumulateCycleTime);
            DefaultStringValueValidator(x => x.AccumulateCycleNumber, y => y.AccumulateCycleNumber);
            DefaultStringValueValidator(x => x.KineticCycleTime, y => y.KineticCycleTime);
            DefaultStringValueValidator(x => x.KineticCycleNumber, y => y.KineticCycleNumber);

        }

        private void WatchAvailableSettings()
        {
            void ImmutableAvailability(string srcProperty,
                Expression<Func<SettingsAvailability, bool>> accessor)
            {
                var lwrName = srcProperty.ToLowerInvariant();
                Observable.Return(AllowedSettings.Contains(lwrName) && SupportedSettings.Contains(lwrName))
                          .ToPropertyEx(IsAvailable, accessor)
                          .DisposeWith(Subscriptions);
            }
            
            ImmutableAvailability(nameof(Model.Object.VSSpeed), x => x.VsSpeed);
            ImmutableAvailability(nameof(Model.Object.VSAmplitude), x => x.VsAmplitude);
            ImmutableAvailability(nameof(Model.Object.ADConverter), x => x.AdcBitDepth);
            ImmutableAvailability(nameof(Model.Object.OutputAmplifier), x=> x.Amplifier);
            ImmutableAvailability(nameof(Model.Object.AcquisitionMode), x => x.AcquisitionMode);
            ImmutableAvailability(nameof(Model.Object.ExposureTime), x => x.ExposureTimeText);
            ImmutableAvailability(nameof(FrameTransfer), x => x.FrameTransfer); // This is correct
            ImmutableAvailability(nameof(Model.Object.TriggerMode), x => x.TriggerMode);
            ImmutableAvailability(nameof(Model.Object.ImageArea), x => x.ImageArea);

            this.WhenAnyPropertyChanged(nameof(AdcBitDepth), nameof(Amplifier))
                .Select(x =>
                {
                    var name = nameof(Model.Object.HSSpeed).ToLowerInvariant();
                    return AllowedSettings.Contains(name)
                           && SupportedSettings.Contains(name)
                           && x.AdcBitDepth >= 0
                           && Amplifier.HasValue;

                })
                .ToPropertyEx(IsAvailable, x => x.HsSpeed)
                .DisposeWith(Subscriptions);

            this.WhenAnyPropertyChanged(nameof(AdcBitDepth), nameof(Amplifier), nameof(HsSpeed))
                .Select(x =>
                {
                    var name = nameof(Model.Object.PreAmpGain).ToLowerInvariant();
                    return AllowedSettings.Contains(name)
                           && SupportedSettings.Contains(name)
                           && x.AdcBitDepth >= 0
                           && Amplifier.HasValue
                           && HsSpeed >= 0;

                })
                .ToPropertyEx(IsAvailable, x => x.PreAmpGain)
                .DisposeWith(Subscriptions);

            this.WhenAnyPropertyChanged(nameof(Amplifier))
                .Select(x =>
                {
                    var name = nameof(Model.Object.EMCCDGain).ToLowerInvariant();
                    return AllowedSettings.Contains(name)
                           && SupportedSettings.Contains(name)
                           && Amplifier.HasValue
                           && Amplifier.Value == OutputAmplification.ElectronMultiplication;
                })
                .ToPropertyEx(IsAvailable, x => x.EmCcdGainText)
                .DisposeWith(Subscriptions);

            this.WhenAnyPropertyChanged(nameof(AcquisitionMode))
                .Select(x =>
                {
                    var name = nameof(Model.Object.ReadoutMode).ToLowerInvariant();
                    return AllowedSettings.Contains(name)
                           && SupportedSettings.Contains(name)
                           && AcquisitionMode.HasValue;
                })
                .ToPropertyEx(IsAvailable, x => x.ReadMode)
                .DisposeWith(Subscriptions);

            this.WhenAnyPropertyChanged(nameof(AcquisitionMode), nameof(FrameTransfer))
                .Select(x =>
                {
                    var name = nameof(Model.Object.AccumulateCycle).ToLowerInvariant();
                    return AcquisitionMode is AcquisitionMode mode
                           && AllowedSettings.Contains(name)
                           && SupportedSettings.Contains(name)
                           && (mode.HasFlag(ANDOR_CS.Enums.AcquisitionMode.Accumulation)
                               || mode.HasFlag(ANDOR_CS.Enums.AcquisitionMode.Kinetic)
                               || mode.HasFlag(ANDOR_CS.Enums.AcquisitionMode.FastKinetics));
                })
                .ToPropertyEx(IsAvailable, x => x.AccumulateCycleTime)
                .DisposeWith(Subscriptions);

            this.WhenAnyPropertyChanged(nameof(AcquisitionMode), nameof(FrameTransfer))
                .Select(x =>
                {
                    var name = nameof(Model.Object.AccumulateCycle).ToLowerInvariant();
                    return AcquisitionMode is AcquisitionMode mode
                           && AllowedSettings.Contains(name)
                           && SupportedSettings.Contains(name)
                           && (mode.HasFlag(ANDOR_CS.Enums.AcquisitionMode.Accumulation)
                               || mode.HasFlag(ANDOR_CS.Enums.AcquisitionMode.Kinetic));
                })
                .ToPropertyEx(IsAvailable, x => x.AccumulateCycleNumber)
                .DisposeWith(Subscriptions);

            this.WhenAnyPropertyChanged(nameof(AcquisitionMode), nameof(FrameTransfer))
                .Select(x =>
                {
                    var name = nameof(Model.Object.KineticCycle).ToLowerInvariant();
                    return AcquisitionMode is AcquisitionMode mode
                           && AllowedSettings.Contains(name)
                           && SupportedSettings.Contains(name)
                           && (mode.HasFlag(ANDOR_CS.Enums.AcquisitionMode.Kinetic)
                               || mode.HasFlag(ANDOR_CS.Enums.AcquisitionMode.RunTillAbort));
                })
                .ToPropertyEx(IsAvailable, x => x.KineticCycleTime)
                .DisposeWith(Subscriptions);

            this.WhenAnyPropertyChanged(nameof(AcquisitionMode), nameof(FrameTransfer))
                .Select(x =>
                {
                    var name = nameof(Model.Object.KineticCycle).ToLowerInvariant();
                    return AcquisitionMode is AcquisitionMode mode
                           && AllowedSettings.Contains(name)
                           && SupportedSettings.Contains(name)
                           && (mode.HasFlag(ANDOR_CS.Enums.AcquisitionMode.Kinetic)
                               || mode.HasFlag(ANDOR_CS.Enums.AcquisitionMode.FastKinetics));
                })
                .ToPropertyEx(IsAvailable, x => x.KineticCycleNumber)
                .DisposeWith(Subscriptions);
        }

        private void WatchItemSources()
        {
            // Watches ADConverter & Amplifier and updates HsSpeeds
            Model.Object.WhenAnyPropertyChanged(
                     nameof(Model.Object.ADConverter),
                     nameof(Model.Object.OutputAmplifier))
                 .Where(x => x.ADConverter.HasValue && x.OutputAmplifier.HasValue)
                 .Select(x => x.GetAvailableHSSpeeds(x.ADConverter?.Index ?? -1, x.OutputAmplifier?.Index ?? -1))
                 .ObserveOnUi()
                 .Subscribe(x =>
                 {
                     HsSpeed = -1;
                     _availableHsSpeeds.Edit(context => context.Load(x));
                 })
                 .DisposeWith(Subscriptions);

            Model.Object.WhenAnyPropertyChanged(
                     nameof(Model.Object.ADConverter),
                     nameof(Model.Object.OutputAmplifier),
                     nameof(Model.Object.HSSpeed))
                 .Where(x =>
                     x.ADConverter.HasValue
                     && x.OutputAmplifier.HasValue
                     && x.HSSpeed.HasValue)
                 .Select(x =>
                     x.GetAvailablePreAmpGain(
                         x.ADConverter?.Index ?? -1,
                         x.OutputAmplifier?.Index ?? -1,
                         x.HSSpeed?.Index ?? -1))
                 .ObserveOnUi()
                 .Subscribe(x =>
                 {
                     PreAmpGain = -1;
                     _availablePreAmpGains.Edit(context => context.Load(x));
                 })
                 .DisposeWith(Subscriptions);

            // Read mode changes if FrameTransfer is enabled
            Model.Object.WhenAnyPropertyChanged(nameof(Model.Object.AcquisitionMode))
                 .Select(x => x.AcquisitionMode?.HasFlag(ANDOR_CS.Enums.AcquisitionMode.FrameTransfer) ?? false)
                 .DistinctUntilChanged()
                 .Select(x => Helper.EnumFlagsToArray<ReadMode>(x
                                        ? Camera.Capabilities.FtReadModes
                                        : Camera.Capabilities.ReadModes)
                                    .Where(EnumConverter.IsReadModeSupported))
                 .ObserveOnUi()
                 .Subscribe(x =>
                 {
                     ReadMode = null;
                     _availableReadModes.Edit(context => context.Load(x));
                 })
                 .DisposeWith(Subscriptions);

            Model.Object.WhenPropertyChanged(x => x.OutputAmplifier)
                 .Select(x =>
                 {
                     var isEm = x.Value?.OutputAmplifier == OutputAmplification.ElectronMultiplication;
                     if (!isEm) return null;
                     var (low, high) = Model.Object.GetEmGainRange();
                     return string.Format(Properties.Localization.AcquisitionSetttings_AvailableGainFormat,
                         low, high);
                 })
                 .BindTo(this, x => x.AllowedGain)
                 .DisposeWith(Subscriptions);

        }
        
        private void InitializeCommands()
        {
            ViewLoadedCommand = 
                    ReactiveCommand.Create<Window>(x =>
                    {
                        foreach (var name in Group1Names)
                            OnErrorsChanged(new DataErrorsChangedEventArgs(name));
                    });

            // Sacrificing reactivity in order to avoid command disposal and memory leaks
            CancelCommand = new ActionCommand(x => (x as Window)?.Close());

            var isValid = this.WhenAnyPropertyChanged(
                                  nameof(Group1ContainsErrors),
                                  nameof(Group2ContainsErrors),
                                  nameof(Group3ContainsErrors))
                              .Select(x => !x.Group1ContainsErrors
                                           && !x.Group2ContainsErrors
                                           && !x.Group3ContainsErrors)
                              .DistinctUntilChanged();

            SubmitCommand =
                ReactiveCommand.Create<Window, Window>(
                                   x => x,
                                   isValid)
                               .DisposeWith(Subscriptions);

            SubmitCommand.Subscribe(Submit).DisposeWith(Subscriptions);

            SaveButtonCommand =
                ReactiveCommand.Create<Unit, FileDialogDescriptor>(
                                   _ => new FileDialogDescriptor()
                                   {
                                       Mode = FileDialogDescriptor.DialogMode.Save,
                                       Title = Properties.Localization.AcquisitionSettings_Dialog_Save_Title,
                                       InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                       FileName = Camera.ToString(),
                                       DefaultExtenstion = ".acq"
                                   }, isValid)
                               .DisposeWith(Subscriptions);

            LoadButtonCommand =
                ReactiveCommand.Create<Unit, FileDialogDescriptor>(
                                   _ => new FileDialogDescriptor()
                                   {
                                       Mode = FileDialogDescriptor.DialogMode.Load,
                                       Title = Properties.Localization.AcquisitionSettings_Dialog_Load_Title,
                                       InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                       FileName = Camera.ToString(),
                                       DefaultExtenstion = ".acq"
                                   })
                               .DisposeWith(Subscriptions);

            SaveButtonCommand.Subscribe(OnFileDialogRequested).DisposeWith(Subscriptions);
            LoadButtonCommand.Subscribe(OnFileDialogRequested).DisposeWith(Subscriptions);

            SaveActionCommand =
                ReactiveCommand.CreateFromTask<string>(
                                   async (x, token) =>
                                   {
                                       if (x is null)
                                           return;
                                       await SaveTo(x, token);
                                   })
                               .DisposeWith(Subscriptions);

            LoadActionCommand =
                ReactiveCommand.CreateFromTask<string>(
                                   async (x, token) =>
                                   {
                                       if (x is null)
                                           return;
                                       await LoadFrom(x, token);
                                   })
                               .DisposeWith(Subscriptions);

        }

        private void Submit(Window w)
        {
            try
            {
                Camera.ApplySettings(Model.Object);
                Helper.ExecuteOnUi(() => CancelCommand.Execute(w));
            }
            catch (AndorSdkException andorExcept)
            {
                if (andorExcept.MethodName?.ToLowerInvariant() is string methodName)
                {
                    var errors = new List<(string, string, string)>();
                    var errorStrings= new List<string>();

                    foreach (var (prop, _) in InteractiveSettings.Where(x => x.EquivalentName == methodName))
                    {
                        var isAvailableName = prop.GetCustomAttribute<MappedNameAttribute>()?.MappedName ?? prop.Name;
                        var isAvailable = typeof(SettingsAvailability).GetProperty(isAvailableName,
                                              BindingFlags.Public | BindingFlags.Instance)?.GetValue(IsAvailable) is bool b && b;

                        if (isAvailable)
                            errors.Add((prop.Name, nameof(DoesNotThrow), andorExcept.Message));
                        else
                            errorStrings.Add(prop.Name);
                        
                    }

                    if(errors.Count > 0)
                        BatchUpdateErrors(errors);

                    if (errorStrings.Count > 0)
                        Helper.ExecuteOnUi(() => MessageBox.Show(
                            string.Format(Properties.Localization.AcquisitionSettings_ApplicationFailed_Message,
                                andorExcept.Message,
                                errorStrings.EnumerableToString(",\r\n") is var message
                                && UiSettingsProvider.Settings.Get(@"MessageBoxMessageMaxLength", 200) is var maxLength
                                && message.Length > maxLength
                                    ? message.Substring(0, maxLength) + "..."
                                    : message),
                            Properties.Localization.AcquisitionSettings_ApplicationFailed,
                            MessageBoxButton.OK,
                            MessageBoxImage.Error));
                }
                else
                    Helper.ExecuteOnUi(() => MessageBox.Show(
                        string.Format(Properties.Localization.AcquisitionSettings_ApplicationFailed_Message,
                            andorExcept.Message,
                            Properties.Localization.AcquisitionSettings_UnknownSetting),
                        Properties.Localization.AcquisitionSettings_ApplicationFailed,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error));
            }
            catch (Exception except)
            {
                Helper.ExecuteOnUi(() => MessageBox.Show(
                    string.Format(
                        Properties.Localization.AcquisitionSettings_ApplicationFailedUnrecoverable_Message,
                        except.Message),
                    Properties.Localization.AcquisitionSettings_ApplicationFailed,
                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        private async Task SaveTo(string fileName, CancellationToken token)
        {
            try
            {
                using (var fl = File.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.Read))
                    await Model.Object.SerializeAsync(fl, Encoding.ASCII, token)
                               .ExpectCancellationAsync()
                               .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                var messSize = UiSettingsProvider.Settings.Get("MessageBoxMessageMaxLength", 100);
                var message = string.Format(Properties.Localization.AcquisitionSettings_SerializationFailed_Message,
                    Path.GetFileName(fileName),
                    e.Message);
                message = message.Length > messSize ? message.Substring(0, messSize) : message;
                MessageBox.Show(message,
                    Properties.Localization.AcquisitionSettings_SerializationFailed_Title,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadFrom(string fileName, CancellationToken token)
        {
            try
            {
                using (var fl = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                    // ReSharper disable once AccessToDisposedClosure
                    await Task.Run(() => Model.Object.Deserialize(fl), token)
                              .ExpectCancellationAsync()
                              .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                var messSize = UiSettingsProvider.Settings.Get("MessageBoxMessageMaxLength", 100);
                var message = string.Format(Properties.Localization.AcquisitionSettings_DeserializationFailed_Message,
                    Path.GetFileName(fileName),
                    e.Message);
                message = message.Length > messSize ? message.Substring(0, messSize) : message;
                MessageBox.Show(message,
                    Properties.Localization.AcquisitionSettings_DeserializationFailed_Title,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void OnFileDialogRequested(FileDialogDescriptor e)
            => FileDialogRequested?.Invoke(this, new DialogRequestedEventArgs(e));


        #region V2

        [Reactive]
        [UnderlyingCameraSettings(@"SetExposureTime")]
        public string ExposureTimeText { get; set; }

        // -1 is the default selected index in the list, equivalent to
        // [SelectedItem] = null in case of nullable properties
        [Reactive]
        [UnderlyingCameraSettings(@"SetVSSpeed")]
        public int VsSpeed { get; set; } = -1;

        [Reactive]
        [UnderlyingCameraSettings(@"SetVSAmplitude")]
        public VSAmplitude? VsAmplitude { get; set; }

        [Reactive]
        [UnderlyingCameraSettings(@"SetADChannel")]
        public int AdcBitDepth { get; set; } = -1;

        [Reactive]
        [UnderlyingCameraSettings(@"SetOutputAmplifier")]
        public OutputAmplification? Amplifier { get; set; }

        [Reactive]
        [UnderlyingCameraSettings(@"SetHSSpeed")]
        public int HsSpeed { get; set; } = -1;

        [Reactive]
        [UnderlyingCameraSettings(@"SetPreAmpGain")]
        public int PreAmpGain { get; set; } = -1;

        [Reactive]
        [UnderlyingCameraSettings(@"SetAcquisitionMode")]
        public AcquisitionMode? AcquisitionMode { get; set; }

        [Reactive]
        [UnderlyingCameraSettings(@"SetFrameTransferMode")]
        public bool FrameTransfer { get; set; }

        [Reactive]
        [UnderlyingCameraSettings(@"SetReadMode")]
        public ReadMode? ReadMode { get; set; }

        [Reactive]
        [UnderlyingCameraSettings(@"SetTriggerMode")]
        public TriggerMode? TriggerMode { get; set; }

        [Reactive]
        [UnderlyingCameraSettings(@"SetEMCCDGain")]
        public string EmCcdGainText { get; set; }

        // ReSharper disable InconsistentNaming
        [Reactive]
        [UnderlyingCameraSettings(@"SetImage")]
        [MappedName(nameof(SettingsAvailability.ImageArea))]
        public string ImageArea_X1 { get; set; }
        [Reactive]
        [UnderlyingCameraSettings(@"SetImage")]
        [MappedName(nameof(SettingsAvailability.ImageArea))]
        public string ImageArea_Y1 { get; set; }
        [Reactive]
        [UnderlyingCameraSettings(@"SetImage")]
        [MappedName(nameof(SettingsAvailability.ImageArea))]
        public string ImageArea_X2 { get; set; }
        [Reactive]
        [UnderlyingCameraSettings(@"SetImage")]
        [MappedName(nameof(SettingsAvailability.ImageArea))]
        public string ImageArea_Y2 { get; set; }
        // ReSharper restore InconsistentNaming

        [Reactive]
        [UnderlyingCameraSettings(@"SetAccumulationCycleTime")]
        public string AccumulateCycleTime { get; set; }
        [Reactive]
        [UnderlyingCameraSettings(@"SetNumberAccumulations")]
        public string AccumulateCycleNumber { get; set; }
        [Reactive]
        [UnderlyingCameraSettings(@"SetKineticCycleTime")]
        public string KineticCycleTime { get; set; }
        [Reactive]
        [UnderlyingCameraSettings(@"SetNumberKinetics")]
        public string KineticCycleNumber { get; set; }

        #endregion

       
    }
}
