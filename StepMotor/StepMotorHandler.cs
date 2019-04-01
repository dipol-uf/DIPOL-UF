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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace StepMotor
{
    /// <summary>
    /// A COM-based interface to DIPOL's step-motor.
    /// </summary>
    public class StepMotorHandler : IDisposable
    {
        private static readonly Regex Regex = new Regex(@"[a-z]([a-z])\s*(\d{1,3})\s*(.*)\r",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly int SpeedFactor = 30;

        /// <summary>
        /// Delegate that handles Data/Error received events.
        /// </summary>
        /// <param name="sender">Sender is <see cref="StepMotorHandler"/>.</param>
        /// <param name="e">Event arguments.</param>
        public delegate void StepMotorEventHandler(object sender, StepMotorEventArgs e);

        /// <summary>
        /// Backend serial port 
        /// </summary>
        private readonly SerialPort _port;

        private TaskCompletionSource<Reply> _portResponseTask;

        /// <summary>
        /// Used to suppress public events while performing WaitResponse.
        /// </summary>
        private volatile bool _suppressEvents;

        public byte Address { get; }

        /// <summary>
        /// Fires when data has been received from COM port.
        /// </summary>
        public event StepMotorEventHandler DataReceived;
        /// <summary>
        /// Fires when error data has been received from COM port.
        /// </summary>
        public event StepMotorEventHandler ErrorReceived;

        /// <summary>
        /// Stores last raw response from the COM port.
        /// </summary>
        public byte[] LastResponse
        {
            get;
            private set;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="portName">COM port name.</param>
        /// <param name="address">Device address.</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public StepMotorHandler(string portName, byte address = 1)
        {
            Address = address;
            // Checks if port name is legal
            if (!SerialPort.GetPortNames().Contains(portName))
                throw new ArgumentOutOfRangeException($"Provided {nameof(portName)} ({portName}) is either illegal or not present on the sstem.");

            // Creates port
            _port = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
            // Event listeners
            _port.DataReceived += OnPortDataReceived;
            _port.ErrorReceived += OnPortErrorReceived;
            _port.NewLine = "\r";
            // Opens port
            _port.Open();
        }

        private async Task<bool> PokeAddressInBinary(byte address)
        {
            var oldStatus = _suppressEvents;
            try
            {
                _suppressEvents = true;

                var result = await SendCommandAsync(Command.GetAxisParameter, 1, (byte) CommandType.Unused, address, 0,
                    TimeSpan.FromMilliseconds(100));

                return result.Status == ReturnStatus.Success;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                _suppressEvents = oldStatus;
            }

        }

        private async Task SwitchToBinary(byte address)
        {
            var oldStatus = _suppressEvents;

            try
            {
                _suppressEvents = true;
                _portResponseTask = null;
                _port.WriteLine("");
                await Task.Delay(TimeSpan.FromMilliseconds(100));

                var addrStr = ((char) (address - 1 + 'A')).ToString();

                var command = $"{addrStr} BIN";
                _portResponseTask = new TaskCompletionSource<Reply>();
                _port.WriteLine(command);
                await Task.WhenAny(_portResponseTask.Task, Task.Delay(TimeSpan.FromMilliseconds(100)));
                if (_port.BytesToRead != 0)
                {
                    var buffer = new byte[_port.BytesToRead];
                    _port.Read(buffer, 0, buffer.Length);
                    var result = Regex.Match(_port.Encoding.GetString(buffer));

                    if (result.Groups.Count == 4
                        && result.Groups[1].Value == addrStr
                        && result.Groups[2].Value == "100")
                        return;

                }
                throw new InvalidOperationException("Failed to switch from ASCII to Binary.");
            }
            finally
            {
                _suppressEvents = oldStatus;
            }

        }

        /// <summary>
        /// Waits for response. Waits until commandSent if false, which is set in internal
        /// COM port event handlers. 
        /// </summary>
        /// <param name="timeOut">Wait timeout. If there is no response within this interval, <see cref="TimeoutException"/> is thrown.</param>
        /// <returns>An instance of <see cref="Reply"/> generated from response byte array.</returns>
        private async Task<Reply> WaitResponseAsync(TimeSpan timeOut)
        {
            if(_portResponseTask is null)
                throw new NullReferenceException("The task source object is null.");

            var result = await Task.WhenAny(_portResponseTask.Task, Task.Delay(timeOut));
            if (result is Task<Reply> reply)
                return await reply;
            throw new TimeoutException("Serial device did not communicate back within allotted time interval.");
        }

        /// <summary>
        /// Handles internal COM port ErrorReceived.
        /// </summary>
        /// <param name="sender">COM port.</param>
        /// <param name="e">Event arguments.</param>
        protected void OnPortErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            // Reads last response
            LastResponse = new byte[_port.BytesToRead];
            _port.Read(LastResponse, 0, LastResponse.Length);
            // Indicates command received response
            _portResponseTask?.SetException(new InvalidOperationException("Serial device responded with an error."));

            // IF events are not suppressed, fires respective public event
            if (!_suppressEvents)
                OnErrorReceived(new StepMotorEventArgs(LastResponse));
        }

        /// <summary>
        /// Handles internal COM port DataReceived.
        /// </summary>
        /// <param name="sender">COM port.</param>
        /// <param name="e">Event arguments.</param>
        protected void OnPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // If port buffer is not empty
            if (_port.BytesToRead == 9)
            {
                //Reads last response
                LastResponse = new byte[_port.BytesToRead];
                _port.Read(LastResponse, 0, LastResponse.Length);
                // Indicates command received response
                _portResponseTask?.SetResult(new Reply(LastResponse));

                // IF events are not suppressed, fires respective public event
                if (!_suppressEvents)
                    OnDataReceived(new StepMotorEventArgs(LastResponse));
            }
           
        }

        protected Task<Reply> SendCommandAsync(Command command, int argument,
            byte type,
            byte address,
            byte motorOrBank, TimeSpan timeOut)
        {
            // Indicates command sending is in process and response have been received yet.
            _portResponseTask = new TaskCompletionSource<Reply>();

            // Converts Int32 into byte array. 
            // Motor accepts Most Significant Bit First, so for LittleEndian 
            // array should be reversed.
            var val = BitConverter.GetBytes(argument);
            // Constructs raw command array
            byte[] toSend;
            if (BitConverter.IsLittleEndian)
            {
                toSend = new byte[]
                {
                    address,
                    (byte) command,
                    type,
                    motorOrBank,
                    val[3],
                    val[2],
                    val[1],
                    val[0],
                    0 // Reserved for checksum
                };
            }
            else
            {
                toSend = new byte[]
                {
                    address,
                    (byte) command,
                    type,
                    motorOrBank,
                    val[0],
                    val[1],
                    val[2],
                    val[3],
                    0 // Reserved for checksum
                };
            }

            var sum = 0;
            for (var i = 0; i < toSend.Length - 1; i++)
                sum += toSend[i];

            // Takes least significant byte
            toSend[8] = unchecked((byte)sum);

            // Sends data to COM port
            _port.Write(toSend, 0, toSend.Length);

            // Wait for response
            return WaitResponseAsync(timeOut);
        }

        public Task<Reply> SendCommandAsync(
            Command command, int argument,
            CommandType type = CommandType.Unused,
            byte motorOrBank = 0)
            => SendCommandAsync(
                command, 
                argument, 
                (byte) type,
                Address,
                motorOrBank, 
                TimeSpan.FromMilliseconds(100));

        /// <summary>
        /// Implements interface and frees resources
        /// </summary>
        public void Dispose()
        {
            _port.Close();
            _port.Dispose();
        }

        /// <summary>
        /// Queries status of all Axis parameters.
        /// </summary>
        /// <param name="motorOrBank">Motor or bank, defaults to 0.</param>
        /// <returns>Retrieved values for each AxisParameter queried.</returns>
        public async Task<ReadOnlyDictionary<AxisParameter, int>> GetStatusAsync(
            byte motorOrBank = 0)
        {
            // Stores old state
            var oldState = _suppressEvents;
            _suppressEvents = true;

            var status = new Dictionary<AxisParameter, int>();

            // Ensures state is restored
            try
            {
                
                // For each basic Axis Parameter queries its value
                // Uses explicit conversion of byte to AxisParameter
                for (byte i = 0; i < 14; i++)
                {
                    var reply = await SendCommandAsync(Command.GetAxisParameter, 0, (CommandType) i);
                    if(reply.Status == ReturnStatus.Success)
                        status.Add((AxisParameter)i, reply.ReturnValue);
                }
                
            }
            finally
            {
                // Restores state
                _suppressEvents = oldState;
            }

            // Returns query result
            return new ReadOnlyDictionary<AxisParameter, int>(status);
        }


        /// <summary>
        /// Queries status of essential Axis parameters.
        /// </summary>
        /// <param name="motorOrBank">Motor or bank, defaults to 0.</param>
        /// <returns>Retrieved values for each AxisParameter queried.</returns>
        public async Task<ReadOnlyDictionary<AxisParameter, int>> GetRotationStatusAsync(
            byte motorOrBank = 0)
        {
            // Stores old state
            var oldState = _suppressEvents;
            _suppressEvents = true;

            var status = new Dictionary<AxisParameter, int>();

            // Ensures state is restored
            try
            {

                // For each basic Axis Parameter queries its value
                // Uses explicit conversion of byte to AxisParameter
                for (byte i = 0; i < 6; i++)
                {
                    var reply = await SendCommandAsync(Command.GetAxisParameter, 0, (CommandType)i);
                    if (reply.Status == ReturnStatus.Success)
                        status.Add((AxisParameter)i, reply.ReturnValue);
                }

            }
            finally
            {
                // Restores state
                _suppressEvents = oldState;
            }

            // Returns query result
            return new ReadOnlyDictionary<AxisParameter, int>(status);
        }

        public async Task<int> GetActualPositionAsync(byte motorOrBank = 0)
        {
            var reply = await SendCommandAsync(Command.GetAxisParameter, 0, (CommandType) AxisParameter.ActualPosition,
                motorOrBank);
            if (reply.Status == ReturnStatus.Success)
                return reply.ReturnValue;
            throw new InvalidOperationException("Failed to retrieve position.");
        }

        public async Task<bool> IsTargetPositionReachedAsync(byte motorOrBank = 0)
        {
            var reply = await SendCommandAsync(Command.GetAxisParameter, 0, (CommandType)AxisParameter.TargetPositionReached,
                motorOrBank);
            if (reply.Status == ReturnStatus.Success)
                return reply.ReturnValue == 1;
            throw new InvalidOperationException("Failed to retrieve position.");
        }

        public async Task WaitForPositionReachedAsync(CancellationToken token, byte motorOrBank = 0)
        {
            if(!await IsTargetPositionReachedAsync(motorOrBank))
            {
                token.ThrowIfCancellationRequested();
                var status = await GetRotationStatusAsync(motorOrBank);
                var timeInSec = 0.25 * (status[AxisParameter.TargetPosition] - status[AxisParameter.ActualPosition]) /
                                (SpeedFactor * status[AxisParameter.MaximumSpeed]);
                var timeOut = TimeSpan.FromSeconds(Math.Max(Math.Abs(timeInSec), 0.025));

                token.ThrowIfCancellationRequested();
                while (!await IsTargetPositionReachedAsync(motorOrBank))
                {
                    await Task.Delay(timeOut, token);
                    token.ThrowIfCancellationRequested();
                }
            }
        }

        /// <summary>
        /// Used to fire DataReceived event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnDataReceived(StepMotorEventArgs e)
            => DataReceived?.Invoke(this, e);

        /// <summary>
        /// Used to fire ErrorReceived event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnErrorReceived(StepMotorEventArgs e)
            => ErrorReceived?.Invoke(this, e);

        public static async Task<ReadOnlyCollection<byte>> FindDevice(string port, byte startAddress = 1, byte endAddress = 16)
        {
            var result = new Collection<byte>();
            using (var motor = new StepMotorHandler(port))
            {
                for (var i = startAddress; i <= endAddress; i++)
                {
                    try
                    {
                        if (await motor.PokeAddressInBinary(i))
                            result.Add(i);
                        else
                        {
                            await motor.SwitchToBinary(i);
                            if(await motor.PokeAddressInBinary(i))
                                result.Add(i);
                        }
                    }
                    catch (Exception)
                    {
                        // Ignored
                    }
                }
            }

            return new ReadOnlyCollection<byte>(result);
        }
    }

}

