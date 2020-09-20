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
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
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
            /// <summary>
            /// 3200
            /// </summary>
            private readonly int _angleInUnits = UiSettingsProvider.Settings.Get(@"StepMotorUnitsPerOneRotation", 3200);
            /// <summary>
            /// 3
            /// </summary>
            private readonly int _nRetries = UiSettingsProvider.Settings.Get(@"StepMotorNRetries", 3);
            /// <summary>
            /// 16
            /// </summary>
            private readonly int _nSteps = UiSettingsProvider.Settings.Get(@"StepMotorNSteps", 16);

            /// <summary>
            /// 1 &lt;&lt; 23 = 8388608
            /// </summary>
            private readonly int _stepMotorMaxPositionAbs =
                UiSettingsProvider.Settings.Get(@"StepMotorMaxPositionAbs", 1 << 23);

            private enum MotorActionType
            {
                Rotate,
                Reset
            }

            private float OneStepAngle => 360f / _nSteps;
            private int StepsPerFullRotation => _angleInUnits * _nSteps;

            private double Parameter { get; }

            private MotorActionType ActionType { get; }

           
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

            public override async Task Initialize(CancellationToken token)
            {
                await base.Initialize(token);
                var motor = Manager._windowRef.PolarimeterMotor;

                await motor.SetAxisParameter(
                    AxisParameter.ReferencingSwitchSpeed,
                    UiSettingsProvider.Settings.Get(@"StepMotorRFSSpeed", 95));

                for (var i = 0; i < _nRetries; i++)
                {
                    Helper.WriteLog($@"Reference search, step {i}");
                    await motor.ReferenceReturnToOriginAsync(token);
                    if (await motor.GetAxisParameter(AxisParameter.ReferenceSwitchStatus) == 0)
                        break;
                }

                await motor.MoveToPosition(250, CommandType.Relative);
                await motor.WaitForPositionReachedAsync(token);

                for (var i = 0; i < 3 * _nRetries; i++)
                {
                    Helper.WriteLog($@"Backtracking, step {i}");
                    if (await motor.GetAxisParameter(AxisParameter.ReferenceSwitchStatus) == 0)
                    {
                        await motor.MoveToPosition(-100, CommandType.Relative);
                        await motor.WaitForPositionReachedAsync(token);
                    }
                    else break;
                }

                var pos = await motor.GetTruePositionAsync();
                if (Math.Abs(pos) < 150)
                {
                    Helper.WriteLog($@"Found position is {pos}, marking as origin");
                    await motor.SetAxisParameter(AxisParameter.ActualPosition, 0);
                }
                else
                {
                    Helper.WriteLog($@"WARNING! Found position is {pos}, too far from 0, performing extra RFS with no origin reset.");
                    await motor.ReferenceReturnToOriginAsync(token);
                }
            }

            public override async Task Execute(CancellationToken token)
            {
                // TODO : move to resources
                if (Manager._windowRef.PolarimeterMotor is null)
                    throw new NotSupportedException(@"No step motor found");

                var pos = await RetryAction(() => Manager._windowRef.PolarimeterMotor.GetActualPositionAsync(), _nRetries);

                if (ActionType == MotorActionType.Reset)
                {
                    var zeroPos = StepsPerFullRotation * (int)Math.Ceiling(1.0 * pos / StepsPerFullRotation);
                    Helper.WriteLog(@$"Resetting motor: from {pos} to {zeroPos}");

                    if ((Math.Abs(zeroPos) + StepsPerFullRotation) >= _stepMotorMaxPositionAbs)
                        // Exceeding default ~327 rotations without reference search
                        // Step motor buffer overflow is predicted, forcing reference search
                        await Initialize(token);

                    await RetryAction(
                        () => Manager._windowRef.PolarimeterMotor.MoveToPosition(zeroPos), 
                        _nRetries);
                    await RetryAction(
                        () => Manager._windowRef.PolarimeterMotor.WaitForPositionReachedAsync(token),
                        _nRetries);
                }
                else
                {
                    // WATCH : Check that `Parameter` is 1
                    var newPos = (int)(pos + _angleInUnits * Parameter);
                    await RetryAction(
                        () => Manager._windowRef.PolarimeterMotor.MoveToPosition(
                            // ReSharper disable once RedundantArgumentDefaultValue
                            newPos, CommandType.Absolute),
                        _nRetries);


                    await RetryAction(
                        () => Manager._windowRef.PolarimeterMotor.WaitForPositionReachedAsync(token),
                        _nRetries);
                }

                pos = await RetryAction(() => Manager._windowRef.PolarimeterMotor.GetActualPositionAsync(), _nRetries);
                var actualPos = await RetryAction(() => Manager._windowRef.PolarimeterMotor.GetTruePositionAsync(), _nRetries);
                
                Manager.MotorPosition = OneStepAngle * (pos % StepsPerFullRotation) / _angleInUnits;
                Manager.ActualMotorPosition = OneStepAngle * (actualPos % StepsPerFullRotation) / _angleInUnits;

                Helper.WriteLog($@"Motor at position {pos} ({actualPos})");
            }
            private static async Task RetryAction(Func<Task> action, int retries)
            {
                var isFinished = false;
                Exception lastException = null;
                for (var i = 0; !isFinished && i < retries; i++)
                {
                    try
                    {
                        await action();
                        isFinished = true;
                    }
                    catch (TaskCanceledException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        lastException = e;
                        isFinished = false;
                    }
                }

                if (!isFinished)
                    throw lastException ??
                          new InvalidOperationException(Properties.Localization.General_ShouldNotHappen);
            }

            private static async Task<T> RetryAction<T>(Func<Task<T>> action, int retries)
            {
                var isFinished = false;
                Exception lastException = null;
                T result = default;
                for (var i = 0; !isFinished && i < retries; i++)
                {
                    try
                    {
                        result = await action();
                        isFinished = true;
                    }
                    catch (TaskCanceledException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        lastException = e;
                        isFinished = false;
                    }
                }

                if (!isFinished)
                    throw lastException ??
                          new InvalidOperationException(Properties.Localization.General_ShouldNotHappen);

                return result;
            }

        }
    }
}
