using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace StepMotor
{
    public interface IAsyncMotor : IDisposable
    {
        byte Address { get; }

        Task ReturnToOriginAsync(CancellationToken token = default, byte motorOrBank = 0);
        Task ReferenceReturnToOriginAsync(CancellationToken token = default, byte motorOrBank = 0);

        Task WaitForPositionReachedAsync(CancellationToken token = default, TimeSpan timeOut = default,
            byte motorOrBank = 0);

        Task WaitForPositionReachedAsync(IProgress<(int Current, int Target)> progressReporter,
            CancellationToken token = default, TimeSpan timeOut = default, byte motorOrBank = 0);

        Task<bool> IsTargetPositionReachedAsync(byte motorOrBank = 0);

        Task<int> GetActualPositionAsync(byte motorOrBank = 0);

        Task<ImmutableDictionary<AxisParameter, int>> GetRotationStatusAsync(byte motorOrBank = 0);

        Task<ImmutableDictionary<AxisParameter, int>> GetStatusAsync(byte motorOrBank = 0);
    }
}