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
using System.Threading.Tasks;
using ANDOR_CS.Classes;
using DIPOL_UF.Models;
using ReactiveUI;

namespace DIPOL_UF.Jobs
{
    internal sealed class JobManager : ReactiveObject
    {
        private DipolMainWindow _windowRef;

        public static JobManager Manager { get; } = new JobManager();

        // TODO : return a copy of a target
        public Target CurrentTarget { get; private set; } = new Target();
        public Job AcquisitionJob { get; private set; }
        public void AttachToMainWindow(DipolMainWindow window)
            => _windowRef = window ?? throw new ArgumentNullException(nameof(window));

        public Task SubmitNewTarget(Target target)
        {
            CurrentTarget = target ?? throw new ArgumentNullException(nameof(target));
            return SetupNewTarget();
        }

        private async Task SetupNewTarget()
        {
            try
            {
                await ApplySettingsTemplate();
                await ConstructJob();
            }
            catch (Exception)
            {
                CurrentTarget = new Target();
                AcquisitionJob = null;
                throw;
            }
        }

        private async Task ApplySettingsTemplate()
        {
            if(!File.Exists(CurrentTarget.SettingsPath))
                throw new FileNotFoundException("Settings file is not found", CurrentTarget.SettingsPath);

            if(_windowRef.ConnectedCameras.Count < 1)
                throw new InvalidOperationException("No connected cameras to work with.");


            byte[] settingsByteRep;

            using (var str = new FileStream(CurrentTarget.SettingsPath, FileMode.Open, FileAccess.Read))
            {
                settingsByteRep = new byte[str.Length];
                await str.ReadAsync(settingsByteRep, 0, settingsByteRep.Length);
            }

            var cams = _windowRef.ConnectedCameras.Items.Select(x => x.Camera).ToList();

            List<SettingsBase> setts;
            using (var memory = new MemoryStream(settingsByteRep, false))
                setts = cams.Select(x =>
                {
                    memory.Position = 0;
                    var template = x.GetAcquisitionSettingsTemplate();
                    template.Deserialize(memory);
                    return template;
                }).ToList();

            // TODO : here, update settings of each camera individually

            for (var i = 0; i < cams.Count; i++)
            {
                cams[i].ApplySettings(setts[i]);
            }
        }

        private async Task ConstructJob()
        {
            if (!File.Exists(CurrentTarget.JobPath))
                throw new FileNotFoundException("Job file is not found", CurrentTarget.JobPath);

            using (var str = new FileStream(CurrentTarget.JobPath, FileMode.Open, FileAccess.Read))
                AcquisitionJob = Job.Create(str);

            if (AcquisitionJob.ContainsActionOfType<MotorAction>()
                && _windowRef.PolarimeterMotor is null)
                throw new InvalidOperationException("Cannot execute current job with no motor connected.");
        }

        private JobManager() { }


    }
}
