using System;
using System.Collections.Generic;
using System.ServiceModel.Configuration;
using System.Threading.Tasks;
using DIPOL_UF.Jobs;

namespace DIPOL_UF
{
    // WATCH : partial class that hides real [Main]
    public static partial class DipolUfApp
    {
        public static async Task Main()
        {
            var job = new Jobs.Job
            {
                _actions = new List<JobAction>
                {
                    new MotorAction(),
                    new RepeatAction(
                        new List<JobAction>
                        {
                            new CameraAction(@"camera/expose 1,3"),
                            new MotorAction(@"motor/rotate -3.5")
                        }, 4),
                    new MotorAction()
                }
            };

            await job.Run();

            Console.ReadKey();
        }
    }
}
