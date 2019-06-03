﻿//    This file is part of Dipol-3 Camera Manager.

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
using System.Buffers;
using System.Collections.Concurrent;
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

        private static readonly int ResponseSizeInBytes = 9;

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

        private readonly TimeSpan _timeOut;

        private readonly ConcurrentQueue<TaskCompletionSource<Reply>> _responseWaitQueue 
            = new ConcurrentQueue<TaskCompletionSource<Reply>>();

        /// <summary>
        /// Used to suppress public events while performing WaitResponse.
        /// </summary>
        private volatile bool _suppressEvents;

        //public string PortName => _port.PortName;
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
        /// Default constructor
        /// </summary>
        /// <param name="port">COM port.</param>
        /// <param name="defaultTimeOut">Default response timeout.</param>
        /// <param name="address">Device address.</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public StepMotorHandler(SerialPort port, byte address = 1, TimeSpan defaultTimeOut = default)
        {
            _timeOut = defaultTimeOut == default
                ? TimeSpan.FromMilliseconds(300)
                : defaultTimeOut;

            Address = address;
            _port = port; //new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
            // Event listeners
            _port.DataReceived += OnPortDataReceived;
            _port.ErrorReceived += OnPortErrorReceived;
            _port.NewLine = "\r";

            if (!_port.IsOpen)
            {
                // Creates port
                _port.BaudRate = 9600;
                _port.Parity = Parity.None;
                _port.DataBits = 8;
                _port.StopBits = StopBits.One;
                _port.Open();
            }
        }


        private async Task<bool> PokeAddressInBinary(byte address)
        {
            var oldStatus = _suppressEvents;
            try
            {
                _suppressEvents = true;

                var result = await SendCommandAsync(
                    Command.GetAxisParameter, 
                    1, (byte) CommandType.Unused, 
                    address, 0, _timeOut);

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

        private async Task<bool> SwitchToBinary(byte address)
        {
            var oldStatus = _suppressEvents;

            try
            {
                _suppressEvents = true;
                _port.WriteLine("");
                await Task.Delay(_timeOut);

                var addrStr = ((char)(address - 1 + 'A')).ToString();

                var command = $"{addrStr} BIN";
                var taskSrc = new TaskCompletionSource<Reply>();
                _responseWaitQueue.Enqueue(taskSrc);
                _port.WriteLine(command);
                try
                {
                    await WaitTimeOut(taskSrc.Task, _timeOut);
                }
                catch (StepMotorException exept)
                {
                    if (exept.RawData != null && exept.RawData.Length > 0)
                    {
                        var result = Regex.Match(_port.Encoding.GetString(exept.RawData));

                        if (result.Groups.Count == 4
                            && result.Groups[1].Value == addrStr
                            && result.Groups[2].Value == "100")
                            return true;
                    }

                }
                catch (TimeoutException)
                {
                    taskSrc.SetCanceled();
                }

                return false;
            }
            finally
            {
                _suppressEvents = oldStatus;
            }

        }

       

        /// <summary>
        /// Handles internal COM port ErrorReceived.
        /// </summary>
        /// <param name="sender">COM port.</param>
        /// <param name="e">Event arguments.</param>
        protected void OnPortErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            // Reads last response
            byte[] buffer = null;

            if (_port.BytesToRead > 0)
                buffer = new byte[_port.BytesToRead];
            
            while(_responseWaitQueue.TryDequeue(out var taskSrc))
                taskSrc.SetException(new InvalidOperationException(@"Step motor returned an error."));

            OnErrorReceived(new StepMotorEventArgs(buffer ?? Array.Empty<byte>()));
        }

        /// <summary>
        /// Handles internal COM port DataReceived.
        /// </summary>
        /// <param name="sender">COM port.</param>
        /// <param name="e">Event arguments.</param>
        protected void OnPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_port.BytesToRead <= 0) return;

            var len = _port.BytesToRead;
            var pool = ArrayPool<byte>.Shared.Rent(Math.Max(ResponseSizeInBytes, len));
            TaskCompletionSource<Reply> taskSrc;

            while (_responseWaitQueue.TryDequeue(out taskSrc) &&
                   taskSrc.Task.IsCanceled)
            {
            }

            // Sometimes step motor writes checksum with a delay.
            // On trigger .BytesToRead == 8
            // Checksum is usually written immediately after, 
            // so small delay allows to capture it
            if (len < ResponseSizeInBytes)
            {
                Task.Delay(TimeSpan.FromMilliseconds(_timeOut.TotalMilliseconds/4)).GetAwaiter().GetResult();
                if (_port.BytesToRead % ResponseSizeInBytes == 0)
                    len = _port.BytesToRead;
            }

            try
            {
                _port.Read(pool, 0, len);
                
                if (len == ResponseSizeInBytes)
                {
                    var reply = new Reply(pool.AsSpan(0, ResponseSizeInBytes));
                    taskSrc?.SetResult(reply);
                    OnDataReceived(new StepMotorEventArgs(pool.AsSpan(0, ResponseSizeInBytes).ToArray()));
                }
                else if (len % ResponseSizeInBytes == 0)
                {
                    var reply = new Reply(pool.AsSpan(0, ResponseSizeInBytes));
                    taskSrc.SetResult(reply);
                    OnDataReceived(new StepMotorEventArgs(pool.AsSpan(0, ResponseSizeInBytes).ToArray()));

                    for (var i = 0; i < len / ResponseSizeInBytes; i++)
                    {
                        reply = new Reply(pool.AsSpan(0, ResponseSizeInBytes));
                        while (_responseWaitQueue.TryDequeue(out taskSrc) && taskSrc.Task.IsCanceled)
                        {
                        }

                        taskSrc?.SetResult(reply);
                        OnDataReceived(new StepMotorEventArgs(pool.AsSpan(0, ResponseSizeInBytes).ToArray()));
                    }
                }
                else
                {
                    taskSrc?.SetException(new StepMotorException("Step motor response is inconsistent", pool));
                    OnDataReceived(new StepMotorEventArgs(pool.ToArray()));
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(pool, true);
            }
        }

        protected async Task<Reply> SendCommandAsync(Command command, int argument,
            byte type,
            byte address,
            byte motorOrBank, TimeSpan timeOut)
        {

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

            byte sum = 0;
            unchecked
            {
                for (var i = 0; i < toSend.Length - 1; i++)
                    sum += toSend[i];
            }

            // Takes least significant byte
            toSend[8] = sum;

            var responseTaskSource = new TaskCompletionSource<Reply>();
            _responseWaitQueue.Enqueue(responseTaskSource);
            // Sends data to COM port
            _port.Write(toSend, 0, toSend.Length);

            // Wait for response
            return await WaitResponseAsync(responseTaskSource.Task, command, timeOut);
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
                _timeOut);

        /// <summary>
        /// Implements interface and frees resources
        /// </summary>
        public void Dispose()
        {
            while(_responseWaitQueue.TryDequeue(out var taskSrc) && taskSrc?.Task.IsCanceled == false)
                taskSrc.SetException(new ObjectDisposedException(nameof(StepMotorHandler)));
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
            throw new InvalidOperationException("Failed to retrieve value.");
        }

        public async Task<bool> IsTargetPositionReachedAsync(byte motorOrBank = 0)
        {
            var reply = await SendCommandAsync(Command.GetAxisParameter, 0, (CommandType)AxisParameter.TargetPositionReached,
                motorOrBank);
            if (reply.Status == ReturnStatus.Success)
                return reply.ReturnValue == 1;
            throw new InvalidOperationException("Failed to retrieve value.");
        }

        public async Task WaitForPositionReachedAsync(CancellationToken token = default, TimeSpan timeOut = default, byte motorOrBank = 0)
        {
            token.ThrowIfCancellationRequested();
            var startTime = DateTime.Now;

            if (!await IsTargetPositionReachedAsync(motorOrBank))
            {
                token.ThrowIfCancellationRequested();
                var status = await GetRotationStatusAsync(motorOrBank);
                var delayMs = Math.Max(
                    250 * Math.Abs(status[AxisParameter.TargetPosition] - status[AxisParameter.ActualPosition]) /
                    (SpeedFactor * status[AxisParameter.MaximumSpeed]),
                    500);

                while (!await IsTargetPositionReachedAsync(motorOrBank))
                {
                    token.ThrowIfCancellationRequested();
                    if(timeOut != default && (DateTime.Now - startTime) > timeOut)
                        throw new TimeoutException();
                    await Task.Delay(delayMs, token);
                }

            }
                
        }

        public async Task WaitForPositionReachedAsync(IProgress<(int Current, int Target)> progressReporter, CancellationToken token = default, TimeSpan timeOut = default, byte motorOrBank = 0)
        {
            token.ThrowIfCancellationRequested();
            var startTime = DateTime.Now;
            
            var target = 0;
            var reply = await SendCommandAsync(Command.GetAxisParameter, 0, (CommandType) AxisParameter.TargetPosition,
                motorOrBank);
            if(reply.Status != ReturnStatus.Success)
                throw new InvalidOperationException("Filed to query target position.");

            target = reply.ReturnValue;
            var current = await GetActualPositionAsync(motorOrBank);

            progressReporter?.Report((current, target));
            
            if (!await IsTargetPositionReachedAsync(motorOrBank))
            {
                token.ThrowIfCancellationRequested();
                var status = await GetRotationStatusAsync(motorOrBank);
                var delayMs = Math.Max(
                    125 * Math.Abs(status[AxisParameter.TargetPosition] - status[AxisParameter.ActualPosition]) /
                    (SpeedFactor * status[AxisParameter.MaximumSpeed]),
                    250);

                while (!await IsTargetPositionReachedAsync(motorOrBank))
                {
                    token.ThrowIfCancellationRequested();
                    if (timeOut != default && (DateTime.Now - startTime) > timeOut)
                        throw new TimeoutException();
                    await Task.Delay(delayMs, token);
                    current = await GetActualPositionAsync(motorOrBank);
                    progressReporter?.Report((current, target));
                }

            }

            current = await GetActualPositionAsync(motorOrBank);
            progressReporter?.Report((current, target));

        }

        public async Task<bool> IsInMotionAsync(byte motorOrBank = 0)
        {
            var reply = await SendCommandAsync(Command.GetAxisParameter, 0, (CommandType)AxisParameter.ActualSpeed,
                motorOrBank);
            if (reply.Status == ReturnStatus.Success)
                return reply.ReturnValue != 0;
            throw new InvalidOperationException("Failed to retrieve value.");
        }

        public async Task StopAsync(byte motorOrBank = 0)
        {
            var reply = await SendCommandAsync(Command.MotorStop, 0, motorOrBank: motorOrBank);
            if (reply.Status != ReturnStatus.Success)
                throw new InvalidOperationException("Failed to retrieve value.");
        }

        public async Task ReturnToOriginAsync(CancellationToken token = default, byte motorOrBank = 0)
        {
            token.ThrowIfCancellationRequested();

            var reply = await SendCommandAsync(Command.MoveToPosition, 0, CommandType.Absolute, motorOrBank);
            if (reply.Status != ReturnStatus.Success)
                throw new InvalidOperationException("Failed to return to the origin.");

            token.ThrowIfCancellationRequested();

            await WaitForPositionReachedAsync(token);
        }

        public async Task ReferenceReturnToOriginAsync(CancellationToken token = default, byte motorOrBank = 0)
        {
            var reply = await SendCommandAsync(Command.ReferenceSearch, 0, CommandType.Start, motorOrBank);
            if (reply.Status != ReturnStatus.Success)
                throw new InvalidOperationException("Failed to start reference search.");

            if (token.IsCancellationRequested)
            {
                await SendCommandAsync(Command.ReferenceSearch, 0, CommandType.Stop, motorOrBank);
                token.ThrowIfCancellationRequested();
            }
            var deltaMs = 200;

            while ((reply = await SendCommandAsync(Command.ReferenceSearch, 0, CommandType.Status, motorOrBank))
                   .Status == ReturnStatus.Success
                   && reply.ReturnValue != 0)
            {
                if (token.IsCancellationRequested)
                {
                    await SendCommandAsync(Command.ReferenceSearch, 0, CommandType.Stop, motorOrBank);
                    token.ThrowIfCancellationRequested();
                }
                await Task.Delay(deltaMs, token);
            }

        }

        /// <summary>
        /// Used to fire DataReceived event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnDataReceived(StepMotorEventArgs e)
        {
            if (!_suppressEvents)
                DataReceived?.Invoke(this, e);
        }
        /// <summary>
        /// Used to fire ErrorReceived event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnErrorReceived(StepMotorEventArgs e)
        {
            if (!_suppressEvents)
                ErrorReceived?.Invoke(this, e);
        }

        private static Task WaitTimeOut(Task task, TimeSpan timeOut = default, CancellationToken token = default)
        {
            if (timeOut == default)
                return task;
            var src = CancellationTokenSource.CreateLinkedTokenSource(token);

            var result = Task.WaitAll(
                new[]
                {
                    task.ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                            src.Cancel();
                    }, src.Token)
                }, (int)timeOut.TotalMilliseconds, src.Token);

            if (result)
                return task;

            if (src.IsCancellationRequested && task.IsFaulted)
                return task;
            if (src.IsCancellationRequested)
                return Task.FromCanceled(src.Token);
            return Task.FromException<TimeoutException>(new TimeoutException());



        }
        private static async Task<Reply> WaitResponseAsync(Task<Reply> source, Command command, TimeSpan timeOut = default)
        {
            if (source is null)
                throw new NullReferenceException(nameof(source));
            if (timeOut == default)
                return await source;

            //try
            //{
                await WaitTimeOut(source, timeOut);

                if (source.IsCompleted)
                {
                    var result = await source;
                    if (result.Command == command)
                        return result;
                    throw new InvalidOperationException(@"Sent/received command mismatch.");
                }
            //}
            //// WATCH : no exception handling
            //catch (Exception e)
            //{

            //}
            throw new TimeoutException();


        }

        public static async Task<ReadOnlyCollection<byte>> FindDevice(SerialPort port, byte startAddress = 1,
            byte endAddress = 16)
        {
           
            var result = new Collection<byte>();

            for (var address = startAddress; address <= endAddress; address++)
            {
                var motor = new StepMotorHandler(port, address);
                try
                {
                    if (await motor.PokeAddressInBinary(address))
                        result.Add(address);
                    else
                    {
                        await motor.SwitchToBinary(address);
                        if (await motor.PokeAddressInBinary(address))
                            result.Add(address);
                    }
                }
                catch (Exception)
                {
                    // Ignored
                }
                finally
                {
                    motor.Dispose();
                }
            }

            return new ReadOnlyCollection<byte>(result);
        }

        public static async Task<StepMotorHandler> TryCreateFromAddress(
            SerialPort port, byte address, TimeSpan defaultTimeOut = default)
        {
            if (port is null)
                throw new ArgumentNullException(nameof(port));

            var motor = new StepMotorHandler(port , address, defaultTimeOut);

            try
            {
                if (await motor.PokeAddressInBinary(address))
                    return motor;

                await motor.SwitchToBinary(address);
                if (await motor.PokeAddressInBinary(address))
                    return motor;

            }
            catch (Exception)
            {
                // Ignored
            }
            motor.Dispose();
            return null;
        }

        public static async Task<StepMotorHandler> TryCreateFirst(
            SerialPort port, byte startAddress = 1, byte endAddress = 16, TimeSpan defaultTimeOut = default)
        {
            if (port is null)
                throw new ArgumentNullException(nameof(port));
            if (startAddress > endAddress)
                throw new ArgumentOutOfRangeException(
                    $"[{nameof(startAddress)}] should be less than or equal to [{nameof(endAddress)}]");

            for (var address = startAddress; address <= endAddress; address++)
            {
                var motor = await TryCreateFromAddress(port, address, defaultTimeOut);
                if (motor != null)
                    return motor;
            }

            return null;
        }

        public static async Task<StepMotorHandler> CreateFirstOrFromAddress(
            SerialPort port, byte address,
            byte startAddress = 1, byte endAddress = 16,
            TimeSpan defaultTimeOut = default)
            => (await TryCreateFromAddress(port, address, defaultTimeOut)
                ?? await TryCreateFirst(port, startAddress, endAddress, defaultTimeOut))
               ?? throw new InvalidOperationException("Failed to connect to step motor.");
    }

}

