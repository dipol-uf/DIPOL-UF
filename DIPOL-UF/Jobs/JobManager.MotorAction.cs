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
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using StepMotor;

namespace DIPOL_UF.Jobs
{
    internal sealed partial class JobManager
    {
        private class MotorAction : JobAction
        {
            private static readonly Regex Regex =
                new Regex(@"^(?:motor/)?(rotate|reset)\s*?([+-]?[0-9]+\.?[0-9]*)?$",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);

            private enum MotorActionType
            {
                Rotate,
                Reset
            }

            private double Parameter { get; }

            private MotorActionType ActionType { get; }

            public MotorAction()
            {
                ActionType = MotorActionType.Reset;
                Parameter = 0;
            }

            public MotorAction(string command)
            {
                // Assuming regex produces exactly the amount of groups
                if (command is null)
                    throw new ArgumentNullException(nameof(command));

                var match = Regex.Match(command.ToLowerInvariant());
                if (!match.Success)
                    throw new ArgumentException(@"Motor command is invalid.", nameof(command));

                ActionType =
                    match.Groups[1].Value == "reset"
                        ? MotorActionType.Reset
                        : MotorActionType.Rotate;

                if (double.TryParse(
                    match.Groups[2].Value,
                    NumberStyles.Any, NumberFormatInfo.InvariantInfo,
                    out var param))
                    Parameter = param;
                else
                    Parameter =
                        ActionType == MotorActionType.Rotate
                            ? 1
                            : 0;
            }

            public override async Task Execute()
            {
                // TODO : move to settings
                const int angle = 3200;

                //Console.WriteLine($@"{DateTime.Now:HH:mm:ss.fff} Motor starts {ActionType} with parameter {Parameter}");

                if(Manager._windowRef.PolarimeterMotor is null)
                    await Task.Delay(TimeSpan.FromMilliseconds(1000));
                else
                {
                    // TODO : Add cancellation support
                    if (ActionType == MotorActionType.Reset)
                        await Manager._windowRef.PolarimeterMotor.ReturnToOriginAsync(default);
                    else
                    {
                        await Manager._windowRef.PolarimeterMotor.SendCommandAsync(Command.MoveToPosition,
                            (int) (angle * Parameter), CommandType.Absolute);
                        await Manager._windowRef.PolarimeterMotor.WaitForPositionReachedAsync(default);
                    }
                }
            
                //Console.WriteLine(
                //    $@"{DateTime.Now:HH:mm:ss.fff} Motor finishes {ActionType}  with parameter {Parameter}");
            }

            public override bool ContainsActionOfType<T>()
                => this is T;
        }
    }
}
