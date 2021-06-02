#nullable enable

using System;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ANDOR_CS.Enums;

namespace DIPOL_UF.Jobs
{
    internal partial class JobManager
    {
        public class ShutterAction : JobAction
        {
            private static readonly Regex Regex =
                new Regex(@"^(?:shutter/)?(open|close|auto)\s*?(int(?:ernal)?|ext(?:ernal)?|all)?$",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);

            private ShutterMode? Internal { get; }
            private ShutterMode? External {get;}

            public ShutterAction(string command)
            {
                // Assuming regex produces exactly the amount of groups
                if (command is null)
                    throw new ArgumentNullException(nameof(command));

                var match = Regex.Match(command.ToLowerInvariant());
                if (!match.Success)
                    throw new ArgumentException(@"Shutter command is invalid.", nameof(command));

                ShutterMode action;
                switch (match.Groups[1].Value)
                {
                    case "open":
                        action = ShutterMode.PermanentlyOpen;
                        break;
                    case "close":
                        action = ShutterMode.PermanentlyClosed;
                        break;
                    default:
                        action = ShutterMode.FullyAuto;
                        break;
                }

                if (match.Groups[2].Value.StartsWith("int"))
                    Internal = action;
                else if (match.Groups[2].Value.StartsWith("ext"))
                    External = action;
                else if (match.Groups[2].Value == "all")
                {
                    Internal = action;
                    External = action;
                }
                else
                    throw new ArgumentException(@"Shutter command is invalid.", nameof(command));
            }

            public override async Task Execute(CancellationToken token)
            {
                foreach (var tab in Manager._jobControls)
                {
                    token.ThrowIfCancellationRequested();
                    await Task.Delay(TimeSpan.FromMilliseconds(50), token);

                    var(@internal, external, _, _, _) = tab.Camera.Shutter;

                    if (Internal is { } newInternal && newInternal != @internal)
                    {
                        if(!await tab.InternalShutterCommand.CanExecute.FirstAsync())
                        {
                            throw new InvalidOperationException(@"Cannot control internal shutter!");
                        }
                        await tab.InternalShutterCommand.Execute(newInternal);
                    }

                    if (
                        (tab.Camera.Capabilities.Features & SdkFeatures.ShutterEx) == SdkFeatures.ShutterEx &&
                        External is { } newExternal && 
                        newExternal != external)
                    {
                        if (!await tab.ExternalShutterCommand.CanExecute.FirstAsync())
                        {
                            throw new InvalidOperationException(@"Cannot control external shutter!");
                        }
                        await tab.ExternalShutterCommand.Execute(newExternal);
                    }
                }
            }
        }
    }
}
