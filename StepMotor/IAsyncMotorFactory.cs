using System;
using System.Collections.Immutable;
using System.IO.Ports;
using System.Threading.Tasks;

namespace StepMotor
{
    public interface IAsyncMotorFactory
    {
        Task<ImmutableList<byte>> FindDevice(SerialPort port, byte startAddress = 1, byte endAddress = 16);

        Task<IAsyncMotor> TryCreateFromAddress(
            SerialPort port, byte address, TimeSpan defaultTimeOut = default);

        Task<IAsyncMotor> TryCreateFirst(
            SerialPort port, byte startAddress = 1, byte endAddress = 16, TimeSpan defaultTimeOut = default);

        Task<IAsyncMotor> CreateFirstOrFromAddress(
            SerialPort port, byte address,
            byte startAddress = 1, byte endAddress = 16,
            TimeSpan defaultTimeOut = default);
    }
}