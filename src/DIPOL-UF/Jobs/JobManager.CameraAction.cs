#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ANDOR_CS.AcquisitionMetadata;
using ANDOR_CS.Events;
using DIPOL_UF.Models;
using FITS_CS;

namespace DIPOL_UF.Jobs
{
    internal sealed partial class JobManager
    {
        private class CameraAction : JobAction
        {
            private static readonly Regex Regex =
                new(@"^(?:camera/)?(expose)\s*((?:\s*[0-9]+,?)+)?$",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);

            // private List<int> SpecificCameras { get; }


            public CameraAction(string command)
            {

                if (command is null)
                    throw new ArgumentNullException(nameof(command));

                var match = Regex.Match(command.ToLowerInvariant());
                if (!match.Success)
                {
                    throw new ArgumentException(@"Motor command is invalid.", nameof(command));
                }

                // SpecificCameras = match.Groups[2].Value.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                //                        .Select(x =>
                //                            int.TryParse(x, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var y)
                //                                ? new int?(y)
                //                                : null)
                //                        .Where(x => x is not null)
                //                        .Select(x => x!.Value)
                //                        .ToList();
            }

            public override async Task Execute(CancellationToken token)
            {
                var sharedKeys = new List<FitsKey>();
                if (Manager.MotorPosition is { } motorPosition)
                {
                    sharedKeys.Add(new FitsKey("ANGLE", FitsKeywordType.Float, motorPosition, "Plate position in deg"));
                }

                if (Manager.ActualMotorPosition is { } actualPosition)
                {
                    sharedKeys.Add(
                        new FitsKey("RAWANGLE", FitsKeywordType.Float, actualPosition, "Actual position in deg")
                    );
                }

                // List<Task> tasks = Manager._jobControls.Select(async x => {
                //     // In order to capture extremely fast acquisitions, first capture a task from observable,
                //     // then start acquisition through the command interface
                //     // and then await initial task
                //     ConfiguredTaskAwaitable<AcquisitionStatusEventArgs> task = x.WhenAcquisitionFinished.FirstAsync()
                //         .ToTask(token).ConfigureAwait(false);
                //     await Task.Delay(TimeSpan.FromMilliseconds(0.001), token);
                //     x.StartAcquisition(Manager._requestMap[x.Camera.GetHashCode()].WithNewKeywords(sharedKeys), token);
                //     await task;
                // }).ToList();

                List<Task<AcquisitionStatusEventArgs>> tasks = Manager._jobControls.Select(
                    async x => await x.WhenAcquisitionFinished.FirstAsync().ToTask(token).ConfigureAwait(false)
                ).ToList();

                List<(CameraTab x, Request)> requests =
                    Manager._jobControls
                           .Select(
                               x => (x, Manager._requestMap[x.Camera.GetHashCode()]
                                               .WithNewKeywords(sharedKeys))
                           ).ToList();

                foreach (var (tab, request) in requests)
                {
                    tab.StartAcquisition(request, token);
                }
                
                await Task.WhenAll(tasks);
                Manager.Progress++;
                Manager.CumulativeProgress++;
            }
        }
    }
}