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
using System.Runtime.Remoting;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ANDOR_CS.Classes;
using ANDOR_CS.Enums;
using DIPOL_UF.Models;
using DIPOL_UF.Properties;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI.Fody.Helpers;

namespace DIPOL_UF.Jobs
{
    internal sealed partial class JobManager : ReactiveObjectEx
    {
        private DipolMainWindow _windowRef;
        private byte[] _settingsRep;
        private List<CameraTab> _jobControls;
        private Dictionary<int, SettingsBase> _settingsCache;
        private Task _jobTask;
        private CancellationTokenSource _tokenSource;
        public static JobManager Manager { get; } = new JobManager();

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
        public int AcquisitionRuns { get; }
        // TODO : 1 is default while there is no UI for this
            = 1;

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
            try
            {
                _jobControls = _windowRef.CameraTabs.Items.Select(x => x.Tab).ToList();
                _settingsCache = _jobControls.ToDictionary(x => x.Camera.GetHashCode(),
                    y => y.Camera.CurrentSettings.MakeCopy());
                if (_jobControls.Any(x => x.Camera?.CurrentSettings is null))
                    throw new InvalidOperationException("At least one camera has no settings applied to it.");

                await Task.Run(async ()  =>
                {
                    var fileName = CurrentTarget.TargetName;
                    if (AcquisitionJob.ContainsActionOfType<CameraAction>())
                        foreach (var control in _jobControls)
                            control.Camera.StartImageSavingSequence(CurrentTarget.TargetName, fileName,
                                ImageFormat.SignedInt32);
                    for (var i = 0; i < AcquisitionRuns; i++)
                        await AcquisitionJob.Run(token);

                    if (AcquisitionJob.ContainsActionOfType<CameraAction>())
                        foreach (var control in _jobControls)
                            await control.Camera.FinishImageSavingSequenceAsync();


                    fileName = $"{CurrentTarget.TargetName}_bias";
                    if (BiasJob?.ContainsActionOfType<CameraAction>() == true)
                        foreach (var control in _jobControls)
                            control.Camera.StartImageSavingSequence(CurrentTarget.TargetName, fileName,
                                ImageFormat.SignedInt32);
                    await (BiasJob?.Run(token) ?? Task.CompletedTask);
                    if (BiasJob?.ContainsActionOfType<CameraAction>() == true)
                        foreach (var control in _jobControls)
                            await control.Camera.FinishImageSavingSequenceAsync();

                    fileName = $"{CurrentTarget.TargetName}_dark";
                    if (DarkJob?.ContainsActionOfType<CameraAction>() == true)
                        foreach (var control in _jobControls)
                            control.Camera.StartImageSavingSequence(CurrentTarget.TargetName, fileName,
                                ImageFormat.SignedInt32);
                    await (DarkJob?.Run(token) ?? Task.CompletedTask);
                    if (DarkJob?.ContainsActionOfType<CameraAction>() == true)
                        foreach (var control in _jobControls)
                            await control.Camera.FinishImageSavingSequenceAsync();
                }, token);
            }
            catch (Exception e)
            {
                // TODO : Handle various exceptions
            }
            finally
            {
                ReadyToRun = true;
                IsInProcess = false;
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
                BiasJob = CurrentTarget.BiasPath is null
                    ? null
                    : await ConstructJob(CurrentTarget.BiasPath);
                DarkJob = CurrentTarget.DarkPath is null
                    ? null
                    : await ConstructJob(CurrentTarget.DarkPath);
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
#if !DEBUG
            // TODO : Consider checking shutter support in advance
            if (AcquisitionJob.ContainsActionOfType<MotorAction>()
                && _windowRef.PolarimeterMotor is null)
                throw new InvalidOperationException("Cannot execute current control with no motor connected.");
#endif
            return control;
        }


        private JobManager() { }


    }
}
