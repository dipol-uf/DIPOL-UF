using System;
using System.Threading.Tasks;
using StepMotor;

namespace Tests
{
    public static class MainApp
    {
        public static async Task<int> Main()
        {
            using (var motor = new StepMotorHandler(@"COM1"))
            {
                var status = await motor.GetStatusAsync();

                await motor.SendCommandAsync(Command.MoveToPosition, 50000, CommandType.Absolute);
                await motor.WaitForPositionReachedAsync();

                status = await motor.GetRotationStatusAsync();

                await motor.ReturnToOriginAsyncEx();

                status = await motor.GetRotationStatusAsync();

            }
            return 0;
        }
    }
}
