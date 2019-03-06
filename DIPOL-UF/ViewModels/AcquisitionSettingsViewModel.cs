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
using System.Linq;
using System.Windows;
using System.Threading.Tasks;
using System.IO;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using ANDOR_CS.Classes;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;
using DIPOL_UF.Commands;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using static DIPOL_UF.Validators.Validate;
using EnumConverter = ANDOR_CS.Classes.EnumConverter;

// ReSharper disable UnassignedGetOnlyAutoProperty

namespace DIPOL_UF.ViewModels
{
    internal sealed class AcquisitionSettingsViewModel : ReactiveViewModel<ReactiveWrapper<SettingsBase>>
    {
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


        public SettingsAvailability IsAvailable { get; }
            = new SettingsAvailability();

        public CameraBase Camera => Model.Object.Camera;

        public ReactiveCommand<string, Unit> GotFocusCommand { get; private set; }
        public ReactiveCommand<string, Unit> LostFocusCommand { get; private set; }


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
        public string AllowedGain { [ObservableAsProperty] get; }

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

            // TODO: Add property names here
            Group3Names = new[]
            {
                nameof(AccumulationCycleTime),
                nameof(AccumulationCycleNumber)
            };
            

            InitializeCommands();

            WatchItemSources();
            HookObservables();
            HookValidators();

            _availableHsSpeeds.DisposeWith(Subscriptions);
            _availablePreAmpGains.DisposeWith(Subscriptions);
            _availableReadModes.DisposeWith(Subscriptions);
            
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
                    .Subscribe(x => UpdateErrors(x, name, nameof(DoesNotThrow)))
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


            CreateStringToFloatSetter(x => x.ExposureTimeText, Model.Object.SetExposureTime, y => y.ExposureTimeText);
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

                        if (firstTest.All(y => y is null))
                            secondTest = DoesNotThrow(Model.Object.SetImageArea, new Rectangle(x1, y1, x2, y2));
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
                    nameof(AccumulationCycleTime),
                    nameof(AccumulationCycleNumber))
                .Merge(IsAvailable.NotifyWhenAnyPropertyChanged(
                    nameof(IsAvailable.AccumulateCycleTime),
                    nameof(IsAvailable.AccumulateCycleNumber)))
                .Subscribe(_ =>
                {
                    if (!IsAvailable.AccumulateCycleTime && !IsAvailable.AccumulateCycleNumber)
                        BatchUpdateErrors(
                            (nameof(AccumulationCycleTime), nameof(CanBeParsed), null),
                            (nameof(AccumulationCycleNumber), nameof(CanBeParsed), null),
                            (nameof(AccumulationCycleTime), nameof(DoesNotThrow), null),
                            (nameof(AccumulationCycleNumber), nameof(DoesNotThrow), null));
                    else if (IsAvailable.AccumulateCycleTime && IsAvailable.AccumulateCycleNumber)
                    {
                        string secondTest = default;

                        var firstTestTime = CanBeParsed(AccumulationCycleTime, out float time);
                        var firstTestNumber = CanBeParsed(AccumulationCycleNumber, out int frames);

                        if (firstTestTime is null && firstTestNumber is null
                                                  && Model.Object.AccumulateCycle != (frames, time))
                            secondTest = DoesNotThrow(Model.Object.SetAccumulationCycle, frames, time);

                        BatchUpdateErrors(
                            (nameof(AccumulationCycleTime), nameof(CanBeParsed), firstTestTime),
                            (nameof(AccumulationCycleNumber), nameof(CanBeParsed), firstTestNumber),
                            (nameof(AccumulationCycleTime), nameof(DoesNotThrow), secondTest),
                            (nameof(AccumulationCycleNumber), nameof(DoesNotThrow), secondTest));
                    }
                    else if (IsAvailable.AccumulateCycleTime)
                    {
                        string secondTest = default;

                        var firstTest = CanBeParsed(AccumulationCycleTime, out float time);

                        if (firstTest is null
                            && Model.Object.AccumulateCycle != (0, time))
                            secondTest = DoesNotThrow(Model.Object.SetAccumulationCycle,
                                0, time);

                        BatchUpdateErrors(
                            (nameof(AccumulationCycleTime), nameof(CanBeParsed), firstTest),
                            (nameof(AccumulationCycleTime), nameof(DoesNotThrow), secondTest));
                    }
                    else if (IsAvailable.AccumulateCycleNumber)
                    {
                        string secondTest = default;

                        var firstTest = CanBeParsed(AccumulationCycleNumber, out int frames);

                        if (firstTest is null
                            && Model.Object.AccumulateCycle != (frames, 0f))
                            secondTest = DoesNotThrow(Model.Object.SetAccumulationCycle,
                                frames, 0f);

                        BatchUpdateErrors(
                            (nameof(AccumulationCycleNumber), nameof(CanBeParsed), firstTest),
                            (nameof(AccumulationCycleNumber), nameof(DoesNotThrow), secondTest));
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
                            && Model.Object.KineticCycle != (0, time))
                            secondTest = DoesNotThrow(Model.Object.SetKineticCycle,
                                0, time);

                        BatchUpdateErrors(
                            (nameof(KineticCycleTime), nameof(CanBeParsed), firstTest),
                            (nameof(KineticCycleTime), nameof(DoesNotThrow), secondTest));
                    }
                    else if (IsAvailable.KineticCycleNumber)
                    {
                        string secondTest = default;

                        var firstTest = CanBeParsed(KineticCycleNumber, out int frames);

                        if (firstTest is null
                            && Model.Object.KineticCycle != (frames, 0f))
                            secondTest = DoesNotThrow(Model.Object.SetKineticCycle,
                                frames, 0f);

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
            void CreateGetter<TSrc, TTarget>(
                Expression<Func<SettingsBase, TSrc>> sourceAccessor,
                Func<TSrc, TTarget> selector,
                Expression<Func<AcquisitionSettingsViewModel, TTarget>> targetAccessor)
                => Model.Object.WhenPropertyChanged(sourceAccessor)
                        .Select(x => selector(x.Value))
                        .DistinctUntilChanged()
                        .ObserveOnUi()
                        .BindTo(this, targetAccessor)
                        .DisposeWith(Subscriptions);

            CreateGetter(x => x.VSSpeed, y => y?.Index ?? -1, z => z.VsSpeed);
            CreateGetter(x => x.VSAmplitude, y => y, z => z.VsAmplitude);
            CreateGetter(x => x.ADConverter, y => y?.Index ?? -1, z => z.AdcBitDepth);
            CreateGetter(x => x.OutputAmplifier, y => y?.OutputAmplifier, z => z.Amplifier);
            CreateGetter(x => x.HSSpeed, y => y?.Index ?? -1, z => z.HsSpeed);
            CreateGetter(x => x.PreAmpGain, y => y?.Index ?? -1, z => z.PreAmpGain);
            CreateGetter(x => x.ExposureTime, 
                y => y?.ToString(Properties.Localization.General_ExposureFloatFormat),
                z => z.ExposureTimeText);
            CreateGetter(x => x.TriggerMode, y => y, z => z.TriggerMode);
            CreateGetter(x => x.EMCCDGain, 
                y => y?.ToString(Properties.Localization.General_IntegerFormat),
                z => z.EmCcdGainText);

            CreateGetter(x => x.ImageArea,
                y => y?.X1.ToString(Properties.Localization.General_IntegerFormat),
                z => z.ImageArea_X1);
            CreateGetter(x => x.ImageArea,
                y => y?.Y1.ToString(Properties.Localization.General_IntegerFormat),
                z => z.ImageArea_Y1);
            CreateGetter(x => x.ImageArea,
                y => y?.X2.ToString(Properties.Localization.General_IntegerFormat),
                z => z.ImageArea_X2);
            CreateGetter(x => x.ImageArea,
                y => y?.Y2.ToString(Properties.Localization.General_IntegerFormat),
                z => z.ImageArea_Y2);

            CreateGetter(x => x.AccumulateCycle,
                y => y?.Time.ToString(Properties.Localization.General_ExposureFloatFormat),
                z => z.AccumulationCycleTime);
            CreateGetter(x => x.AccumulateCycle,
                y => y?.Frames.ToString(Properties.Localization.General_IntegerFormat),
                z => z.AccumulationCycleNumber);

            CreateGetter(x => x.KineticCycle,
                y => y?.Time.ToString(Properties.Localization.General_ExposureFloatFormat),
                z => z.KineticCycleTime);
            CreateGetter(x => x.KineticCycle,
                y => y?.Frames.ToString(Properties.Localization.General_IntegerFormat),
                z => z.KineticCycleNumber);

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
                      .DistinctUntilChanged()
                      .ObserveOnUi()
                      .BindTo(this, x => x.AcquisitionMode)
                      .DisposeWith(Subscriptions);

            acqModeObs.Select(x => x.FrameTransfer)
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

            DefaultStringValueValidator(x => x.AccumulationCycleTime, y => y.AccumulateCycleTime);
            DefaultStringValueValidator(x => x.AccumulationCycleNumber, y => y.AccumulateCycleNumber);
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
                 .ToPropertyEx(this, x => x.AllowedGain)
                 .DisposeWith(Subscriptions);

        }
        
        private void InitializeCommands()
        {
            GotFocusCommand =
                ReactiveCommand.Create<string>(RemoveAllErrors)
                               .DisposeWith(Subscriptions);
            LostFocusCommand =
                ReactiveCommand.Create<string>(this.RaisePropertyChanged)
                               .DisposeWith(Subscriptions);


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
                            MessageBox.Show(
                                $"An error occured while reading acquisition settings from {dialog.FileName}.\n" +
                                $"[{(e.Message.Length <= messSize ? e.Message : e.Message.Substring(0, messSize))}]",
                                "Unable to load file", MessageBoxButton.OK, MessageBoxImage.Information,
                                MessageBoxResult.OK);
                        });
                    }
                });

            }
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
        public string ExposureTimeText { get; set; }

        // -1 is the default selected index in the list, equivalent to
        // [SelectedItem] = null in case of nullable properties
        [Reactive] public int VsSpeed { get; set; } = -1;

        [Reactive]
        public VSAmplitude? VsAmplitude { get; set; }

        [Reactive] public int AdcBitDepth { get; set; } = -1;

        [Reactive]
        public OutputAmplification? Amplifier { get; set; }

        [Reactive] public int HsSpeed { get; set; } = -1;

        [Reactive]
        public int PreAmpGain { get; set; } = -1;

        [Reactive]
        public AcquisitionMode? AcquisitionMode { get; set; }

        [Reactive]
        public bool FrameTransfer { get; set; }

        [Reactive]
        public ReadMode? ReadMode { get; set; }

        [Reactive]
        public TriggerMode? TriggerMode { get; set; }

        [Reactive]
        public string EmCcdGainText { get; set; }

        [Reactive]
        // ReSharper disable once InconsistentNaming
        public string ImageArea_X1 { get; set; }
        [Reactive]
        // ReSharper disable once InconsistentNaming
        public string ImageArea_Y1 { get; set; }
        [Reactive]
        // ReSharper disable once InconsistentNaming
        public string ImageArea_X2 { get; set; }
        [Reactive]
        // ReSharper disable once InconsistentNaming
        public string ImageArea_Y2 { get; set; }

        [Reactive]
        public string AccumulationCycleTime { get; set; }
        [Reactive]
        public string AccumulationCycleNumber { get; set; }
        [Reactive]
        public string KineticCycleTime { get; set; }
        [Reactive]
        public string KineticCycleNumber { get; set; }

        #endregion
    }
}
