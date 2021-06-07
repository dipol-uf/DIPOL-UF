
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ANDOR_CS;
using ANDOR_CS.Enums;
using ANDOR_CS.Events;
using DIPOL_UF.Annotations;
using DIPOL_UF.Converters;
using DIPOL_UF.Jobs;
using DIPOL_UF.Models;
using DIPOL_UF.Properties;
using DynamicData.Binding;
using FITS_CS;
using MathNet.Numerics;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace DIPOL_UF.ViewModels
{
    internal sealed class CameraTabViewModel : ReactiveViewModel<CameraTab>
    {
        [CanBeNull]
        private IAcquisitionSettings _previousSettings = null;

        public event EventHandler FileDialogRequested;

        public float MinimumAllowedTemperature => Model.TemperatureRange.Minimum;
        public float MaximumAllowedTemperature => Model.TemperatureRange.Maximum;
        public bool CanControlTemperature => Model.CanControlTemperature;
        public bool CanQueryTemperature => Model.CanQueryTemperature;
        public bool CanControlFan => Model.CanControlFan;
        public bool CanControlInternalShutter => Model.CanControlShutter.Internal;
        public bool CanControlExternalShutter => Model.CanControlShutter.External;

        public int FanTickFrequency => Model.IsThreeStateFan ? 1 : 2;
        // ReSharper disable once UnusedMember.Global
        public string TabHeader => Model.Alias;

        public DipolImagePresenterViewModel DipolImagePresenter { get; }
        public DescendantProxy AcquisitionSettingsWindow { get; private set; }
        public DescendantProxy JobSettingsWindow { get; private set; }
        public DescendantProxy CycleConfigWindow { get; private set; }

        public float ExposureTime { [ObservableAsProperty] get; }
        public int AcquisitionPbMax { [ObservableAsProperty] get; }
        public int AcquisitionPbMin { [ObservableAsProperty] get; }
        public double AcquisitionPbVal { [ObservableAsProperty] get; }


        [Reactive]
        public float TargetTemperature { get; set; }
        [Reactive]
        public string TargetTemperatureText { get; set; }
        [Reactive]
        public int FanMode { get; set; }

        [Reactive]
        public ShutterMode InternalShutterState { get; set; }
        [Reactive]
        public ShutterMode? ExternalShutterMode { get; set; }

        public bool IsJobInProgress { [ObservableAsProperty] get; }
        public bool IsAcquiring { [ObservableAsProperty]get; }
        public bool IsAnyCameraAcquiring { [ObservableAsProperty] get; }
        public float CurrentTemperature { [ObservableAsProperty] get; }
        public Switch CoolerMode { [ObservableAsProperty] get; }
        public string JobName { [ObservableAsProperty] get; }
        public string JobProgressString { [ObservableAsProperty] get; }
        public int JobCumulativeTotal { [ObservableAsProperty] get; }
        public int JobCumulativeCurrent { [ObservableAsProperty] get; }
        public string JobMotorProgress { [ObservableAsProperty] get; }
        public bool IsPolarimetryJob { [ObservableAsProperty] get; }
        public string LastSavedFilePath { [ObservableAsProperty] get; }

        public RenderTargetBitmap PolarizationSymbolImage { get; }

        public ReactiveCommand<Unit, FileDialogDescriptor> SaveButtonCommand { get; private set; }
        public ReactiveCommand<string, Unit> SaveActionCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> CoolerCommand { get; private set; }
        public ICommand SetUpAcquisitionCommand => Model.SetUpAcquisitionCommand;
        public ICommand StartAcquisitionCommand => Model.StartAcquisitionCommand;
        public ICommand StartJobCommand => Model.StartJobCommand;
        public ICommand SetUpJobCommand => Model.SetUpJobCommand;

        public ICommand StartAllAcquisitionsCommand => Model.StartAllAcquisitionsCommand;
        public IReactiveCommand StartQuickVideo { get; private set; }


        public CameraTabViewModel(CameraTab model) : base(model)
        {
            PolarizationSymbolImage = new RenderTargetBitmap(256, 256, 96, 96, PixelFormats.Pbgra32);
            DipolImagePresenter = new DipolImagePresenterViewModel(Model.ImagePresenter);
            //AcquisitionSettingsWindow = new DescendantProxy(Model.AcquisitionSettingsWindow, null);

            HookValidators();
            HookObservables();
            InitializeCommands();

            TargetTemperatureText = "0";
        }

        private void InitializeCommands()
        {
            IObservable<bool> canUseCooler =
                this.WhenAnyPropertyChanged(nameof(IsJobInProgress), nameof(IsAcquiring))
                    .Select(x => !x.IsAcquiring && !x.IsJobInProgress)
                    .StartWith(true)
                    .CombineLatest(ObserveSpecificErrors(nameof(TargetTemperatureText)),
                        Model.CoolerCommand.CanExecute,
                        (x, y, z) => x && !y && z);

            CoolerCommand =
                ReactiveCommand.Create(() => Unit.Default,
                                  canUseCooler)
                               .DisposeWith(Subscriptions);

            CoolerCommand.Select(_ => (int)TargetTemperature).InvokeCommand(Model.CoolerCommand).DisposeWith(Subscriptions);

            AcquisitionSettingsWindow =
                new DescendantProxy(Model.AcquisitionSettingsWindow,
                        x => new AcquisitionSettingsViewModel((ReactiveWrapper<IAcquisitionSettings>) x))
                    .DisposeWith(Subscriptions);
            
            JobSettingsWindow
                = new DescendantProxy(Model.JobSettingsWindow,
                    //x => new JobSettingsViewModel((ReactiveWrapper<Target>)x))
                    x => new JobSettingsViewModel1((ReactiveWrapper<Target1>)x))
                    .DisposeWith(Subscriptions);

            CycleConfigWindow = new DescendantProxy(Model.CycleConfigWindow,
                    x => new CycleConfigViewModel((ReactiveWrapper<int?>) x))
                .DisposeWith(Subscriptions);

            if (CanControlExternalShutter)
                ExternalShutterMode = ShutterMode.PermanentlyOpen;
            if (CanControlInternalShutter)
                InternalShutterState = ShutterMode.PermanentlyOpen;

            SaveButtonCommand =
                ReactiveCommand.Create<Unit, FileDialogDescriptor>(
                                    _ => CreateSaveFileDescriptor(),
                                    DipolImagePresenter.WhenPropertyChanged(x => x.BitmapSource)
                                                       .Select(x => x.Value is { })
                                )
                               .DisposeWith(Subscriptions);
            
            SaveActionCommand = ReactiveCommand.CreateFromTask<string>(WriteTempFileAsync).DisposeWith(Subscriptions);

            StartQuickVideo = ReactiveCommand.Create(ExecuteStartQuickVideo).DisposeWith(Subscriptions);

            // Requests SaveFile window
            SaveButtonCommand.Subscribe(OnFileDialogRequested).DisposeWith(Subscriptions);

        }

        private void HookObservables()
        {
            JobManager.Manager.WhenPropertyChanged(x => x.MotorPosition).Select(x => x.Value.HasValue)
                .DistinctUntilChanged()
                .ObserveOnUi()
                .ToPropertyEx(this, x => x.IsPolarimetryJob)
                .DisposeWith(Subscriptions);

            JobManager.Manager.WhenPropertyChanged(x => x.MotorPosition).Select(x => x.Value ?? 0f)
                      .Select(x => string.Format(Localization.CameraTab_StepMotorAngleFormat, x))
                .ObserveOnUi()
                .ToPropertyEx(this, x => x.JobMotorProgress)
                .DisposeWith(Subscriptions);

            JobManager.Manager.WhenAnyPropertyChanged(nameof(JobManager.Progress), nameof(JobManager.Total))
                      .Select(x => string.Format(Localization.CameraTab_JobProgressFormat, x.Progress, x.Total))
                      .ObserveOnUi()
                      .ToPropertyEx(this, x => x.JobProgressString)
                      .DisposeWith(Subscriptions);

            JobManager.Manager.WhenPropertyChanged(x => x.CurrentJobName).Select(x => x.Value)
                      .Sample(UiSettingsProvider.UiThrottlingDelay)
                      .ObserveOnUi()
                      .ToPropertyEx(this, x => x.JobName)
                      .DisposeWith(Subscriptions);

            JobManager.Manager.WhenAnyPropertyChanged(nameof(JobManager.IsInProcess))
                      .Sample(UiSettingsProvider.UiThrottlingDelay)
                      .Select(x => x.IsInProcess
                          ? x.TotalAcquisitionActionCount + x.BiasActionCount + x.DarkActionCount
                          : 0)
                      .ObserveOnUi()
                      .ToPropertyEx(this, x => x.JobCumulativeTotal)
                      .DisposeWith(Subscriptions);
                      
            JobManager.Manager.WhenPropertyChanged(x => x.CumulativeProgress)
                      .Select(x => x.Value)
                      .Sample(UiSettingsProvider.UiThrottlingDelay)
                      .ObserveOnUi()
                      .ToPropertyEx(this, x => x.JobCumulativeCurrent)
                      .DisposeWith(Subscriptions);

            JobManager.Manager.WhenPropertyChanged(x => x.AnyCameraIsAcquiring)
                      .Select(x => x.Value)
                      .ObserveOnUi()
                      .ToPropertyEx(this, x => x.IsAnyCameraAcquiring)
                      .DisposeWith(Subscriptions);

            JobManager.Manager.WhenPropertyChanged(x => x.CurrentCycleType)
                      .ObserveOnUi()
                      .Subscribe(x => ImageProvider.UpdateBitmap(PolarizationSymbolImage, x.Value))
                      .DisposeWith(Subscriptions);
            
            Observable.FromEventPattern<ImageSavedHandler, ImageSavedEventArgs>(
                          x => Model.Camera.ImageSaved += x,
                          x => Model.Camera.ImageSaved -= x)
                      .Select(x =>
                          string.Format(Localization.CameraTab_LastSavedFile, x.EventArgs.Path, x.EventArgs.Index))
                      .ObserveOnUi()
                      .ToPropertyEx(this, x => x.LastSavedFilePath)
                      .DisposeWith(Subscriptions);

            Model.Camera.WhenAnyPropertyChanged(nameof(Model.Camera.IsAcquiring))
                 .Select(x => x.IsAcquiring)
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.IsAcquiring)
                 .DisposeWith(Subscriptions);

            Model.Camera.WhenAnyPropertyChanged(nameof(Model.Camera.CoolerMode))
                 .Select(x => x.CoolerMode)
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.CoolerMode)
                 .DisposeWith(Subscriptions);

            Model.WhenTemperatureChecked
                 .Select(x => x.Temperature)
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.CurrentTemperature)
                 .DisposeWith(Subscriptions);

            // Handles temperature changes

            this.WhenPropertyChanged(x => x.TargetTemperatureText)
                .Subscribe(x =>
                {
                    var canParseMessage = Validators.Validate.CanBeParsed(x.Value, out float temp);
                    string inRangeMessage = null;
                    if (canParseMessage is null)
                        inRangeMessage = Validators.Validate.ShouldFallWithinRange(temp,
                            x.Sender.MinimumAllowedTemperature,
                            x.Sender.MaximumAllowedTemperature);

                    BatchUpdateErrors(
                        (nameof(TargetTemperatureText), nameof(Validators.Validate.CanBeParsed), canParseMessage),
                        (nameof(TargetTemperatureText), nameof(Validators.Validate.ShouldFallWithinRange),
                            inRangeMessage));

                    if (canParseMessage is null && inRangeMessage is null)
                        TargetTemperature = temp;
                })
                .DisposeWith(Subscriptions);

            this.WhenPropertyChanged(x => x.TargetTemperature)
                .Where(x => !x.Value.AlmostEqual(float.TryParse(
                    x.Sender.TargetTemperatureText, NumberStyles.Any, NumberFormatInfo.InvariantInfo,
                    out var temp)
                    ? temp
                    : float.NaN))
                .Select(x => x.Value.ToString(Localization.General_FloatTempFormat))
                .BindTo(this, x => x.TargetTemperatureText)
                .DisposeWith(Subscriptions);

            // End of temperature changes


            Model.Camera.WhenAnyPropertyChanged(nameof(Model.Camera.FanMode))
                 .Select(x => 2 - (uint) x.FanMode)
                 .DistinctUntilChanged()
                 .ObserveOnUi()
                 .BindTo(this, x => x.FanMode)
                 .DisposeWith(Subscriptions);

            this.WhenPropertyChanged(x => x.FanMode)
                .Select(x => (FanMode) (2 - x.Value))
                .InvokeCommand(Model.FanCommand)
                .DisposeWith(Subscriptions);


            var shutterSrc =
                Model.Camera.WhenAnyPropertyChanged(nameof(Model.Camera.Shutter))
                     .Select(x => x.Shutter)
                     .DistinctUntilChanged()
                     .ObserveOnUi();

            shutterSrc.Select(x => x.Internal)
                      .DistinctUntilChanged()
                      .BindTo(this, x => x.InternalShutterState)
                      .DisposeWith(Subscriptions);

            this.WhenPropertyChanged(x => x.InternalShutterState)
                .Select(x => x.Value)
                .DistinctUntilChanged()
                .InvokeCommand(Model.InternalShutterCommand)
                .DisposeWith(Subscriptions);

            if (CanControlExternalShutter)
            {
                shutterSrc.Select(x => x.External)
                          .DistinctUntilChanged()
                          .BindTo(this, x => x.ExternalShutterMode)
                          .DisposeWith(Subscriptions);

                this.WhenPropertyChanged(x => x.ExternalShutterMode)
                    .Select(x => x.Value)
                    .DistinctUntilChanged()
                    .InvokeCommand(Model.ExternalShutterCommand)
                    .DisposeWith(Subscriptions);

            }

            Model.WhenTimingCalculated.Select(x => x.Kinetic)
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.ExposureTime)
                 .DisposeWith(Subscriptions);

            Model.WhenPropertyChanged(x => x.AcquisitionProgress)
                 .Select(x => x.Value)
                 //.Sample(UiSettingsProvider.UiThrottlingDelay)
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.AcquisitionPbVal)
                 .DisposeWith(Subscriptions);

            Model.WhenPropertyChanged(x => x.AcquisitionProgressRange)
                 .Select(x => x.Value.Min)
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.AcquisitionPbMin)
                 .DisposeWith(Subscriptions);

            Model.WhenPropertyChanged(x => x.AcquisitionProgressRange)
                 .Select(x => x.Value.Max)
                 .ObserveOnUi()
                 .ToPropertyEx(this, x => x.AcquisitionPbMax)
                 .DisposeWith(Subscriptions);

            this.WhenPropertyChanged(x => x.IsAcquiring)
                .Where(x => x.Value is false)
                .Subscribe(_ => FinishQuickVideo())
                .DisposeWith(Subscriptions);

            PropagateReadOnlyProperty(this, x => x.IsJobInProgress, y => y.IsJobInProgress);

        }

        private void OnFileDialogRequested(FileDialogDescriptor desc) =>
            FileDialogRequested?.Invoke(this, new DialogRequestedEventArgs(desc));
        
        private FileDialogDescriptor CreateSaveFileDescriptor() =>
            new FileDialogDescriptor
            {
                Mode = FileDialogDescriptor.DialogMode.Save,
                DefaultExtenstion = ".fits",
                Title = "Save temporary image",
                FileName = GetTempFileName()
            };

        private async Task WriteTempFileAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            var image = Model.ImagePresenter.SourceImage?.Copy();
            if (image is null)
            {
                return;
            }

            try
            {
                var type = image.UnderlyingType switch
                {
                    TypeCode.SByte  => FitsImageType.UInt8,
                    TypeCode.Byte   => FitsImageType.UInt8,
                    TypeCode.Int16  => FitsImageType.Int16,
                    TypeCode.UInt16 => FitsImageType.Int16,
                    TypeCode.Int32  => FitsImageType.Int32,
                    TypeCode.UInt32 => FitsImageType.Int32,
                    TypeCode.Single => FitsImageType.Single,
                    _               => FitsImageType.Double,
                };

                List<FitsKey> keys = Model.Camera.CurrentSettings?.ConvertToFitsKeys() ?? new List<FitsKey>(4);
                keys.AddRange(
                    new[]
                    {
                        new FitsKey(
                            "CAMERA", FitsKeywordType.String,
                            ConverterImplementations.CameraToStringAliasConversion(Model.Camera)
                        ),
                        new FitsKey(
                            "FILTER", FitsKeywordType.String,
                            ConverterImplementations.CameraToFilterConversion(Model.Camera)
                        )
                    }
                );

                await FitsStream.WriteImageAsync(
                    image, type, path, keys
                );

                if (Injector.GetLogger() is { } logger)
                {
                    logger.Information(
                        "Saved current image from camera {Camera} to {Path}.",
                        ConverterImplementations.CameraToStringAliasConversion(Model.Camera), path
                    );
                }
            }
            catch (Exception e)
            {
                if (Injector.GetLogger() is { } logger)
                {
                    logger.Error(
                        e, "Failed to save current image from camera {Camera} to {Path}.",
                        ConverterImplementations.CameraToStringAliasConversion(Model.Camera), path
                    );
                }
            }
        }

        private string GetTempFileName()
        {
            var timeStamp = DateTimeOffset.UtcNow;
            var name = JobManager.Manager.CurrentTarget1?.StarName is { } starName ? starName : "temp";
            var filter = ConverterImplementations.CameraToFilterConversion(Model.Camera);
            // As a precaution, sanitize default name
            return $"{name}_{filter}_{timeStamp:yyyy-MM-ddTHH-mm-ss}.fits".AsSpan().SanitizePath();
            
        }

        private void ExecuteStartQuickVideo()
        {
            // TODO: Verify Video mode is supported
            // TODO: Verify settings already present

            _previousSettings = Model.Camera.CurrentSettings;
            var newSettings = _previousSettings.MakeCopy();
            newSettings.SetAcquisitionMode(AcquisitionMode.RunTillAbort);
            newSettings.SetKineticCycle(0, 0f);
            Model.Camera.ApplySettings(newSettings);
            StartAcquisitionCommand.Execute(null);
        }

        private void FinishQuickVideo()
        {

            var videoSettings = Model.Camera.CurrentSettings;
            if (_previousSettings is null || ReferenceEquals(_previousSettings, Model.Camera.CurrentSettings))
            {
                return;
            }

            Model.Camera.ApplySettings(_previousSettings);
            videoSettings.Dispose();
            _previousSettings = null;
        }


    }
}
