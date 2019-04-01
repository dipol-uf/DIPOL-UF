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
using System.Linq;
using System.IO.Ports;
using System.Threading.Tasks;

namespace StepMotor
{
    /// <summary>
    /// A COM-based interface to DIPOL's step-motor.
    /// </summary>
    public class StepMotorHandler : IDisposable
    {
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
        private volatile bool _suppressEvents = false;
        
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
        /// <exception cref="ArgumentOutOfRangeException"/>
        public StepMotorHandler(string portName)
        {
            // Checks if port name is legal
            if (!SerialPort.GetPortNames().Contains(portName))
                throw new ArgumentOutOfRangeException($"Provided {nameof(portName)} ({portName}) is either illegal or not present on the sstem.");

            // Creates port
            _port = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
            // Event listeners
            _port.DataReceived += OnPortDataReceived;
            _port.ErrorReceived += OnPortErrorReceived;
            // Opens port
            _port.Open();
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
            _portResponseTask.SetException(new InvalidOperationException("Serial device responded with an error."));

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

        public Task<Reply> SendCommandAsync(Command command, int argument,
            byte type = (byte)CommandType.Unused,
            byte address = 1, byte motorOrBank = 0, int waitResponseTimeMs = 200)
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
            return WaitResponseAsync(TimeSpan.FromMilliseconds(waitResponseTimeMs));
        }

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
        /// <param name="address">Address.</param>
        /// <param name="motorOrBank">Motor or bank, defaults to 0.</param>
        /// <param name="suppressEvents">If true, no standard events are thrown,
        /// but <see cref="StepMotorHandler.LastResponse"/> is updated anyway. 
        /// If false, events are fired for each parameter queried.</param>
        /// <returns>Retrieved values for each AxisParameter queried.</returns>
        //public Dictionary<AxisParameter, int> GetStatus(byte address = 1, byte motorOrBank = 0, bool suppressEvents = true)
        //{
        //    // Stores old state
        //    var oldState = _suppressEvents;
        //    _suppressEvents = suppressEvents;

        //    var status = new Dictionary<AxisParameter, int>();

        //    // Ensures state is restored
        //    try
        //    {

        //        // For each basic Axis Parameter queries its value
        //        // Uses explicit conversion of byte to AxisParameter
        //        for (byte i = 0; i < 14; i++)
        //        {
        //            SendCommand(Command.GetAxisParameter, 0, i, address, motorOrBank);
        //            WaitResponse();
        //            var r = new Reply(LastResponse);
        //            if (r.Status == ReturnStatus.Success)
        //                status[(AxisParameter)i] = r.ReturnValue;

        //        }

        //    }
        //    finally
        //    {
        //        // Restores state
        //        _suppressEvents = oldState;
        //    }

        //        // Returns query result
        //        return status;
        //}

        /// <summary>
        /// Wait for position to be reached. Checks boolean TargetPositionReached parameter.
        /// </summary>
        /// <param name="address">Address.</param>
        /// <param name="motorOrBank">Motor or bank. Defaults to 0.</param>
        /// <param name="suppressEvents">If true, no standard events are thrown,
        /// but <see cref="StepMotorHandler.LastResponse"/> is updated anyway. 
        /// If false, events are fired for each parameter queried.</param>
        /// <param name="checkIntervalMs">TIme between subsequent checks of the status.</param>
        //public void WaitPositionReached(byte address = 1, byte motorOrBank = 0, 
        //    bool suppressEvents = true, int checkIntervalMs = 200)
        //{
        //    // Stores old state
        //    var oldState = _suppressEvents;
        //    _suppressEvents = suppressEvents;

        //    try
        //    {
        //        // Sends GetAxisParameter with TargetPositionReached as parameter.
        //        var r = SendCommand(
        //            Command.GetAxisParameter, 
        //            0, 
        //            (byte)AxisParameter.TargetPositionReached, 
        //            address, 
        //            motorOrBank);
                

        //        // While status is Success and returned value if 0 (false), continue checks
        //        while (r.Status == ReturnStatus.Success && r.ReturnValue == 0)
        //        {
        //            // Waits for small amount of time.
        //            System.Threading.Thread.Sleep(checkIntervalMs);
        //            r = SendCommand(Command.GetAxisParameter, 0, (byte)AxisParameter.TargetPositionReached, address, motorOrBank);
        //        }

        //    }
        //    finally
        //    {
        //        // Restores old state
        //        _suppressEvents = oldState;
        //    }

        //}

        //public void WaitReferencePositionReached(byte address = 1, byte motorOrBank = 0,
        //    bool suppressEvents = true, int checkIntervalMs = 200)
        //{
        //    // Stores old state
        //    var oldState = _suppressEvents;
        //    _suppressEvents = suppressEvents;

        //    try
        //    {

        //        var r = SendCommand(Command.ReferenceSearch, 0, (byte)CommandType.Start, address, motorOrBank);
        //        if (r.Status != ReturnStatus.Success)
        //            throw new Exception();
        //        r = SendCommand(Command.ReferenceSearch, 0, (byte)CommandType.Status, address, motorOrBank);
        //        // While status is Success and returned value if 0 (false), continue checks
        //        while (r.Status == ReturnStatus.Success && r.ReturnValue != 0)
        //        {
        //            // Waits for small amount of time.
        //            System.Threading.Thread.Sleep(checkIntervalMs);
        //            r = SendCommand(Command.ReferenceSearch, 0, (byte)CommandType.Status, address, motorOrBank);
        //        }

        //    }
        //    finally
        //    {
        //        SendCommand(Command.ReferenceSearch, 0, (byte)CommandType.Stop, address, motorOrBank);
        //        // Restores old state
        //        _suppressEvents = oldState;
        //    }
        //}

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
    }

}

