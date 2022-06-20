using System;
using System.Linq;
using ANDOR_CS.Classes;
using DIPOL_UF.Jobs;
using DIPOL_UF.Services.Contract;

namespace DIPOL_UF.Services.Implementation
{
    internal sealed class AcquisitionTimer : IAcquisitionTimer
    {
        private readonly JobManager _jobManager;

        public AcquisitionTimer(JobManager jobManager)
        {
            _jobManager = jobManager;

        }

        public void StartReporting()
        {
            throw new NotImplementedException();
        }
    }
}
