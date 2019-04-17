using System;

namespace StepMotor
{
    public sealed class StepMotorException : Exception
    {
        public byte[] RawData { get; }

        public StepMotorException(string message, Span<byte> data)
            :base(message)
        {
            if (data .IsEmpty)
                return;
            RawData = data.ToArray();
        }
    }
}
