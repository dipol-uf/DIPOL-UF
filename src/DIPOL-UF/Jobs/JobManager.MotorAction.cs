﻿using System;
using System.Collections.Generic;
using System.Globalization;
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

            private int NSteps { get; }

            /// <summary>
            /// 1 &lt;&lt; 23 = 8388608
            /// </summary>
            private readonly int _stepMotorMaxPositionAbs =
                UiSettingsProvider.Settings.Get(@"StepMotorMaxPositionAbs", 1 << 23);

            /// <summary>
            /// 40
            /// </summary>
            private readonly int _referenceSearchMaxAttempts =
                UiSettingsProvider.Settings.Get(@"StepMotorMaxRFSAttempts", 40);

            private enum MotorActionType
            {
                Rotate,
                Reset
            }

            private float OneStepAngle => 360f / NSteps;
            
            private int Step => (int) (_angleInUnits * Parameter);
            private int StepsPerFullRotation => Step * NSteps;
            
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

                NSteps = 16;
            }

            public MotorAction(IReadOnlyDictionary<string, object> props)
            {
                // Action type
                ActionType = props.TryGetValue("Type", out var actionType)
                             && actionType is string typeStr && typeStr.ToLowerInvariant() == "reset"
                    ? MotorActionType.Reset
                    : MotorActionType.Rotate;

                // Rotation scaling factor. 
                // `1` corresponds to standard HWP linear polarization rotation of 22.5 deg,
                // measured in units of `StepMotorUnitsPerOneRotation` configuration (default is 3200)
                // `4` corresponds to QWP circular polarization rotation of 90 deg 
                // `0` can be used for `no rotation`
                Parameter =
                    props.TryGetValue("Parameter", out var param) && ObjectAsDouble(param) is { } dParam
                        ? dParam
                        : ActionType switch
                        {
                            MotorActionType.Rotate => 1,
                            _ => 0
                        };

                NSteps = props.TryGetValue("NSteps", out var nSteps) && ObjectToInt(nSteps) is { } n ? n : 16;
            }

            public override async Task Initialize(CancellationToken token)
            {
                await base.Initialize(token);
                var motor = Manager._windowRef.PolarimeterMotor;

                if(!UiSettingsProvider.Settings.Get(@"StepMotorBacktrackingRFS", true))
                {
                    Helper.WriteLog(Serilog.Events.LogEventLevel.Information, "Using simple RFS to calibrate origin");
                    await motor.ReferenceReturnToOriginAsync(token);
                    return;
                }

                await motor.SetAxisParameter(
                    AxisParameter.ReferencingSwitchSpeed,
                    UiSettingsProvider.Settings.Get(@"StepMotorRFSSpeed", 95));
                var i = 0;
                if (ObjectToInt(UiSettingsProvider.Settings.Get(@"StepMotorRFSSwitchStatus")) is { } switchStatus)
                {

                    for (; i < _referenceSearchMaxAttempts; i++)
                    {
                        Helper.WriteLog(Serilog.Events.LogEventLevel.Information, @"Reference search, step {i}", i + 1);
                        await motor.ReferenceReturnToOriginAsync(token);
                        if (await motor.GetAxisParameter(AxisParameter.ReferenceSwitchStatus) == switchStatus)
                            break;
                    }
                }
                else
                {
                    Helper.WriteLog(
                        Serilog.Events.LogEventLevel.Warning,
                        @"{Config} is not set in the configuration file. Reference search is unreliable.",
                        @"StepMotorRFSSwitchStatus"
                    );
                    await motor.ReferenceReturnToOriginAsync(token);
                }

                Helper.WriteLog(
                        Serilog.Events.LogEventLevel.Information,
                        @"RFS finished in {N} step(s), new position is {pos} ({actualPos})",
                        i + 1,
                        await motor.GetActualPositionAsync(),
                        await motor.GetTruePositionAsync());

            }

            public override async Task Execute(CancellationToken token)
            {
                if (Manager._windowRef.PolarimeterMotor is null)
                    throw new NotSupportedException(@"No step motor found");

                var pos = await RetryAction(() => Manager._windowRef.PolarimeterMotor.GetActualPositionAsync(), _nRetries);

                if (ActionType == MotorActionType.Reset)
                {
                    var zeroPos = StepsPerFullRotation * (int)Math.Ceiling(1.0 * pos / StepsPerFullRotation);
                    if (pos != zeroPos)
                    {
                        Helper.WriteLog(Serilog.Events.LogEventLevel.Information, @"Resetting motor: from {pos} to {zeroPos}", pos, zeroPos);

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
                        Helper.WriteLog(Serilog.Events.LogEventLevel.Information, "Motor is at correct position of {zeroPos}", zeroPos);
                    }

                }
                else
                {
                    // WATCH : Check that `Parameter` is 1
                    var newPos = pos + Step;
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
                    
                Manager.MotorPosition = OneStepAngle * (pos % StepsPerFullRotation) / Step;
                Manager.ActualMotorPosition = OneStepAngle * (actualPos % StepsPerFullRotation) / Step;
                if (ActionType == MotorActionType.Rotate)
                {
                    Helper.WriteLog(
                        Serilog.Events.LogEventLevel.Information, @"Motor at position {pos} ({actualPos})", pos,
                        actualPos
                    );
                }

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

            private static int? ObjectToInt(object value) =>
                value switch
                {
                    int iValue => iValue,
                    long lValue => (int)lValue,
                    _ => null
                };
            
            private static double? ObjectAsDouble(object value) =>
                value switch
                {
                    int iValue => iValue,
                    long lValue => lValue,
                    float fValue => fValue,
                    double dValue => dValue,
                    _ => null
                };
        }
    }
}
