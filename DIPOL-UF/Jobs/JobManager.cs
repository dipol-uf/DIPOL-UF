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
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ANDOR_CS.Classes;
using ANDOR_CS.Enums;
using DIPOL_UF.Models;
using DynamicData;
using DynamicData.Binding;
using FITS_CS;
using ReactiveUI.Fody.Helpers;
using Localization = DIPOL_UF.Properties.Localization;

namespace DIPOL_UF.Jobs
{
    internal sealed partial class JobManager : ReactiveObjectEx
    {
        // These regimes might produce data that will overflow uint16 format,
        // therefore images should be save as int32
        private static readonly AcquisitionMode[] LongFormatModes =
        {
            AcquisitionMode.Accumulation,
            AcquisitionMode.Kinetic,
            AcquisitionMode.FastKinetics
        };


        private DipolMainWindow _windowRef;
        private byte[] _settingsRep;
        private List<CameraTab> _jobControls;
        private Dictionary<int, SettingsBase> _settingsCache;
        private Task _jobTask;
        private CancellationTokenSource _tokenSource;
        private Dictionary<int, ImageFormat> _imageFormatMap;

        public int AcquisitionActionCount { get; private set; }
        public int TotalAcquisitionActionCount { get; private set; }
        public int BiasActionCount { get; private set; }
        public int DarkActionCount { get; private set; }


        public static JobManager Manager { get; } = new JobManager();

        [Reactive]
        public float? MotorPosition { get; private set; }
        [Reactive]
        public int Progress { get; private set; }
        [Reactive]
        public int CumulativeProgress { get; private set; }
        [Reactive]
        public int Total { get; private set; }
        [Reactive]
        public string CurrentJobName { get; private set; }
        [Reactive]
        public bool IsInProcess { get; private set; }
        [Reactive]
        public bool ReadyToRun { get; private set; }
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public bool AnyCameraIsAcquiring { [ObservableAsProperty] get; }

        // TODO : return a copy of a target
        public Target CurrentTarget { get; private set; } = new Target();
        public Job AcquisitionJob { get; private set; }
        public Job BiasJob { get; private set; }
        public Job DarkJob { get; private set; }
        public int AcquisitionRuns { get; private set; }

        public void AttachToMainWindow(DipolMainWindow window)
        {
            if(_windowRef is null)
                _windowRef = window ?? throw new ArgumentNullException(nameof(window));
            else
                throw new InvalidOperationException(Localization.General_ShouldNotHappen);

            _windowRef.ConnectedCameras.Connect()
                .Transform(x => x.Camera)
                .TrueForAny(x => x.WhenPropertyChanged(y => y.IsAcquiring).Select(y => y.Value),
                    z => z)
                .ToPropertyEx(this, x => x.AnyCameraIsAcquiring)
                .DisposeWith(Subscriptions);
        }

        public void StartJob()
        {
            CumulativeProgress = 0;
            IsInProcess = true;
            ReadyToRun = false;
            _tokenSource?.Dispose();
            _tokenSource = new CancellationTokenSource();
            _jobTask = StartJobAsync(_tokenSource.Token);
        }

        public void StopJob()
        {
            if (_jobTask is null)
                return;
            if(!_jobTask.IsCompleted)
                _tokenSource?.Cancel();
        }

        public Task SubmitNewTarget(Target target)
        {
            ReadyToRun = false;
            CurrentTarget = target ?? throw new ArgumentNullException(
                                Localization.General_ShouldNotHappen,
                                nameof(target));
            return SetupNewTarget();
        }

        private async Task StartJobAsync(CancellationToken token)
        {
            async Task DoCameraJobAsync(Job job, string file)
            {
                if (job is null)
                    return;
                try
                {
                    if (job.ContainsActionOfType<CameraAction>())
                        foreach (var control in _jobControls)
                            control.Camera.StartImageSavingSequence(CurrentTarget.TargetName, file,
                                _imageFormatMap[control.Camera.GetHashCode()],
                                new[] { FitsKey.CreateDate("STDATE", DateTimeOffset.Now.UtcDateTime) });
                    MotorPosition = job.ContainsActionOfType<MotorAction>()
                        ? new float?(0)
                        : null;
                    await job.Run(token);
                }
                finally
                {
                    if (job.ContainsActionOfType<CameraAction>())
                        foreach (var control in _jobControls)
                            await control.Camera.FinishImageSavingSequenceAsync();

                    MotorPosition = null;
                }
            }

            try
            {
                _jobControls = _windowRef.CameraTabs.Items.Select(x => x.Tab).ToList();
                _settingsCache = _jobControls.ToDictionary(x => x.Camera.GetHashCode(),
                    y => y.Camera.CurrentSettings.MakeCopy());
                if (_jobControls.Any(x => x.Camera?.CurrentSettings is null))
                    throw new InvalidOperationException("At least one camera has no settings applied to it.");

                _imageFormatMap = _jobControls.ToDictionary(
                    x => x.Camera.GetHashCode(),
                    y => LongFormatModes.Any(z => y.Camera?.CurrentSettings.AcquisitionMode?.HasFlag(z) == true)
                        ? ImageFormat.SignedInt32
                        : ImageFormat.UnsignedInt16);

                await Task.Run(async ()  =>
                {
                    var fileName = CurrentTarget.TargetName;
                    Progress = 0;
                    Total = TotalAcquisitionActionCount;
                    CurrentJobName = Localization.JobManager_AcquisitionJobName;
                    try
                    {
                        if (AcquisitionJob.ContainsActionOfType<CameraAction>())
                            foreach (var control in _jobControls)
                                control.Camera.StartImageSavingSequence(
                                    CurrentTarget.TargetName, fileName,
                                    _imageFormatMap[control.Camera.GetHashCode()],
                                    new [] {  FitsKey.CreateDate("STDATE", DateTimeOffset.Now.UtcDateTime) });

                        MotorPosition = AcquisitionJob.ContainsActionOfType<MotorAction>()
                            ? new float?(0)
                            : null;
                        for (var i = 0; i < AcquisitionRuns; i++)
                            await AcquisitionJob.Run(token);
                    }
                    finally
                    {
                        if (AcquisitionJob.ContainsActionOfType<CameraAction>())
                            foreach (var control in _jobControls)
                                await control.Camera.FinishImageSavingSequenceAsync();
                        MotorPosition = null;
                    }

                    // TODO : Remove logging
                    if (!(BiasJob is null))
                    {
                        Progress = 0;
                        Total = BiasActionCount;
                        CurrentJobName = Localization.JobManager_BiasJobName;
                        await DoCameraJobAsync(BiasJob, $"{CurrentTarget.TargetName}_bias");
                    }

                    if (!(DarkJob is null))
                    {
                        Progress = 0;
                        Total = DarkActionCount;
                        CurrentJobName = Localization.JobManager_DarkJobName;
                        await DoCameraJobAsync(DarkJob, $"{CurrentTarget.TargetName}_dark");
                    }

                }, token);

                var report = $"{Environment.NewLine}{TotalAcquisitionActionCount} {Localization.JobManager_AcquisitionJobName}";
                if (!(BiasJob is null))
                    report += $",{Environment.NewLine}{BiasActionCount} {Localization.JobManager_BiasJobName}";

                if (!(DarkJob is null))
                    report += $",{Environment.NewLine}{BiasActionCount} {Localization.JobManager_DarkJobName}";
                
                MessageBox.Show(
                    string.Format(Localization.JobManager_MB_Finished_Text, report),
                    Localization.JobManager_MB_Finished_Header,
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    string.Format(Localization.JobManager_MB_Failed_Text, e.Message),
                    Localization.JobManager_MB_Failed_Header,
                    MessageBoxButton.OK, MessageBoxImage.Error);

                // TODO : Deal with the failed state if possible
            }
            finally
            {
                ReadyToRun = true;
                IsInProcess = false;
                Progress = 0;
                CurrentJobName = string.Empty;
                Total = 0;
                _jobControls.Clear();
                _jobControls = null;
                foreach( var sett in _settingsCache)
                    sett.Value.Dispose();
                _settingsCache.Clear();
                _settingsCache = null;
            }
        }

        private async Task SetupNewTarget()
        {
            try
            {
                AcquisitionJob = await ConstructJob(CurrentTarget.JobPath);
                AcquisitionRuns = CurrentTarget.Repeats > 0 
                    ? CurrentTarget.Repeats
                    : throw new InvalidOperationException(Localization.General_ShouldNotHappen);
                AcquisitionActionCount = AcquisitionJob.NumberOfActions<CameraAction>();
                TotalAcquisitionActionCount = AcquisitionActionCount * AcquisitionRuns;

                // TODO : Consider checking shutter support in advance
                if (AcquisitionJob.ContainsActionOfType<MotorAction>()
                    && _windowRef.PolarimeterMotor is null)
                    throw new InvalidOperationException("Cannot execute current control with no motor connected.");

                BiasJob = CurrentTarget.BiasPath is null
                    ? null
                    : await ConstructJob(CurrentTarget.BiasPath);
                BiasActionCount = BiasJob?.NumberOfActions<CameraAction>() ?? 0;  

                DarkJob = CurrentTarget.DarkPath is null
                    ? null
                    : await ConstructJob(CurrentTarget.DarkPath);
                DarkActionCount = DarkJob?.NumberOfActions<CameraAction>() ?? 0;


                await LoadSettingsTemplate();
                await ApplySettingsTemplate();
                ReadyToRun = true;
            }
            catch (Exception)
            {
                CurrentTarget = new Target();
                AcquisitionJob = null;
                BiasJob = null;
                DarkJob = null;
                _settingsRep = null;
                throw;
            }
        }

        private async Task LoadSettingsTemplate()
        {
            if(!File.Exists(CurrentTarget.SettingsPath))
                throw new FileNotFoundException("Settings file is not found", CurrentTarget.SettingsPath);

            if(_windowRef.ConnectedCameras.Count < 1)
                throw new InvalidOperationException("No connected cameras to work with.");


            using (var str = new FileStream(CurrentTarget.SettingsPath, FileMode.Open, FileAccess.Read))
            {
                _settingsRep = new byte[str.Length];
                await str.ReadAsync(_settingsRep, 0, _settingsRep.Length);
            }
        }

        private async Task ApplySettingsTemplate()
        {
            if (_settingsRep is null)
                throw new InvalidOperationException(Localization.General_ShouldNotHappen);

            var cameras = _windowRef.ConnectedCameras.Items.Select(x => x.Camera).ToList();

            if (cameras.Count < 1)
                throw new InvalidOperationException("No connected cameras to work with.");

            using (var memory = new MemoryStream(_settingsRep, false))
            foreach (var cam in cameras)
            {
                memory.Position = 0;
                var template = cam.GetAcquisitionSettingsTemplate();
                await template.DeserializeAsync(memory, Encoding.ASCII, CancellationToken.None);

                // TODO : Modify individual settings here

                cam.ApplySettings(template);
            }
        }

        private async Task<Job> ConstructJob(string path)
        {
            Job control;
            if (!File.Exists(path))
                throw new FileNotFoundException("Job file is not found", path);

            using (var str = new FileStream(path, FileMode.Open, FileAccess.Read))
                control = await Job.CreateAsync(str);

            // TODO : Enable for alpha tests
            // INFO : Disabled to test on local environment
           
            return control;
        }


        private JobManager() { }


    }
}
