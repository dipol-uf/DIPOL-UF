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
using System.Linq;
using System.IO.Ports;

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
        /// <summary>
        /// Indicates whether a command was sent and no response has been received yet.
        /// </summary>
        private volatile bool _commandSent;
        /// <summary>
        /// Used to suppress public events while performing WaitResponse.
        /// </summary>
        private volatile bool _suppressEvents;
        
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
            _port.DataReceived += Port_DataReceived;
            _port.ErrorReceived += Port_ErrorReceived;
            // Opens port
            _port.Open();
        }

        /// <summary>
        /// Handles internal COM port ErrorReceived.
        /// </summary>
        /// <param name="sender">COM port.</param>
        /// <param name="e">Event arguments.</param>
        private void Port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            // Reads last response
            LastResponse = new byte[_port.BytesToRead];
            _port.Read(LastResponse, 0, LastResponse.Length);
            // Indicates command received response
            _commandSent = false;

            // IF events are not suppressed, fires respective public event
            if (!_suppressEvents)
                OnErrorReceived(new StepMotorEventArgs(LastResponse));
        }

        /// <summary>
        /// Handles internal COM port DataReceived.
        /// </summary>
        /// <param name="sender">COM port.</param>
        /// <param name="e">Event arguments.</param>
        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // If port buffer is not empty
            if (_port.BytesToRead == 9)
            {
                //Reads last response
                LastResponse = new byte[_port.BytesToRead];
                _port.Read(LastResponse, 0, LastResponse.Length);
                // Indicates command received response
                _commandSent = false;

                // IF events are not suppressed, fires respective public event
                if (!_suppressEvents)
                    OnDataReceived(new StepMotorEventArgs(LastResponse));
            }
           
        }

        /// <summary>
        /// Waits for response. Waits until commandSent if false, which is set in internal
        /// COM port event handlers. 
        /// Based on <see cref="System.Threading.SpinWait.SpinUntil(Func{bool}, TimeSpan)"/>.
        /// </summary>
        /// <param name="timeOutMs">Timeout to wait. -1 is not recommended.</param>
        /// <returns>An instance of <see cref="Reply"/> generated from response byte array.</returns>
        private Reply WaitResponse(int timeOutMs = 200)
        {
            // Waits for response
            System.Threading.SpinWait.SpinUntil(() => !_commandSent, timeOutMs);

            // If response still has not been received, throws
            if (_commandSent)
            {
                _commandSent = false;
                throw new InvalidOperationException("No response received");
            }

            return new Reply(LastResponse);
        }

        /// <summary>
        /// Sends command and waits for a response.
        /// </summary>
        /// <param name="command">Command.</param>
        /// <param name="argument">Command value.</param>
        /// <param name="type">Type. Depends on the command.</param>
        /// <param name="address">Address.</param>
        /// <param name="motorOrBank">Motor or bank. Defaults to 0.</param>
        /// <param name="waitResponseTimeMs">Wait time out. -1 is not recommended.</param>
        /// <returns></returns>
        public Reply SendCommand(Command command, int argument, 
            byte type = (byte)CommandType.Unused, 
            byte address = 1, byte motorOrBank = 0, int waitResponseTimeMs = 200)
        {
            // Indicates command sending is in process and response have been received yet.
            _commandSent = true;

            // Converts Int32 into byte array. 
            // Motor accepts Most Significant Bit First, so for LittleEndian 
            // array should be reversed.
            byte[] val = BitConverter.GetBytes(argument);
            val = BitConverter.IsLittleEndian ? val.Reverse().ToArray() : val;

            // Constructs raw command array
            byte[] toSend = new byte[]
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

            int sum = 0;
            for (int i = 0; i < toSend.Length - 1; i++)
                sum += toSend[i];

            // Takes least significant byte
            toSend[8] = Convert.ToByte(sum & 0xFF);

            // Sends data to COM port
            _port.Write(toSend, 0, toSend.Length);

            // Wait for response
            return WaitResponse(waitResponseTimeMs);
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
        public Dictionary<AxisParameter, int> GetStatus(byte address = 1, byte motorOrBank = 0, bool suppressEvents = true)
        {
            // Stores old state
            var oldState = _suppressEvents;
            _suppressEvents = suppressEvents;

            var status = new Dictionary<AxisParameter, int>();

            // Ensures state is restored
            try
            {

                // For each basic Axis Parameter queries its value
                // Uses explicit conversion of byte to AxisParameter
                for (byte i = 0; i < 14; i++)
                {
                    SendCommand(Command.GetAxisParameter, 0, i, address, motorOrBank);
                    WaitResponse();
                    var r = new Reply(LastResponse);
                    if (r.Status == ReturnStatus.Success)
                        status[(AxisParameter)i] = r.ReturnValue;

                }

            }
            finally
            {
                // Restores state
                _suppressEvents = oldState;
            }

                // Returns query result
                return status;
        }

        /// <summary>
        /// Wait for position to be reached. Checks boolean TargetPositionReached parameter.
        /// </summary>
        /// <param name="address">Address.</param>
        /// <param name="motorOrBank">Motor or bank. Defaults to 0.</param>
        /// <param name="suppressEvents">If true, no standard events are thrown,
        /// but <see cref="StepMotorHandler.LastResponse"/> is updated anyway. 
        /// If false, events are fired for each parameter queried.</param>
        /// <param name="checkIntervalMs">TIme between subsequent checks of the status.</param>
        public void WaitPositionReached(byte address = 1, byte motorOrBank = 0, 
            bool suppressEvents = true, int checkIntervalMs = 200)
        {
            // Stores old state
            var oldState = _suppressEvents;
            _suppressEvents = suppressEvents;

            try
            {
                // Sends GetAxisParameter with TargetPositionReached as parameter.
                var r = SendCommand(
                    Command.GetAxisParameter, 
                    0, 
                    (byte)AxisParameter.TargetPoisitionReached, 
                    address, 
                    motorOrBank);
                

                // While status is Success and returned value if 0 (false), continue checks
                while (r.Status == ReturnStatus.Success && r.ReturnValue == 0)
                {
                    // Waits for small amount of time.
                    System.Threading.Thread.Sleep(checkIntervalMs);
                    r = SendCommand(Command.GetAxisParameter, 0, (byte)AxisParameter.TargetPoisitionReached, address, motorOrBank);
                }

            }
            finally
            {
                // Restores old state
                _suppressEvents = oldState;
            }

        }

        public void WaitReferencePositionReached(byte address = 1, byte motorOrBank = 0,
            bool suppressEvents = true, int checkIntervalMs = 200)
        {
            // Stores old state
            var oldState = _suppressEvents;
            _suppressEvents = suppressEvents;

            try
            {

                var r = SendCommand(Command.ReferenceSearch, 0, (byte)CommandType.Start, address, motorOrBank);
                if (r.Status != ReturnStatus.Success)
                    throw new Exception();
                r = SendCommand(Command.ReferenceSearch, 0, (byte)CommandType.Status, address, motorOrBank);
                // While status is Success and returned value if 0 (false), continue checks
                while (r.Status == ReturnStatus.Success && r.ReturnValue != 0)
                {
                    // Waits for small amount of time.
                    System.Threading.Thread.Sleep(checkIntervalMs);
                    r = SendCommand(Command.ReferenceSearch, 0, (byte)CommandType.Status, address, motorOrBank);
                }

            }
            finally
            {
                SendCommand(Command.ReferenceSearch, 0, (byte)CommandType.Stop, address, motorOrBank);
                // Restores old state
                _suppressEvents = oldState;
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
    }

}

