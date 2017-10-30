//    This file is part of Dipol-3 Camera Manager.

//    Dipol-3 Camera Manager is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

//    Dipol-3 Camera Manager is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with Dipol-3 Camera Manager.  If not, see<http://www.gnu.org/licenses/>.
//
//    Copyright 2017, Ilia Kosenkov, Tuorla Observatory, Finland

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO.Ports;

namespace ROTATOR_CS
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
        private SerialPort port = null;
        /// <summary>
        /// Indicates whether a command was sent and no response has been received yet.
        /// </summary>
        private volatile bool commandSent = false;
        /// <summary>
        /// Used to suppress public events while performing WaitResponse.
        /// </summary>
        private volatile bool suppressEvents = false;
        
        /// <summary>
        /// Fires when data has been received from COM port.
        /// </summary>
        public event StepMotorEventHandler DataRecieved;
        /// <summary>
        /// Fires when error data has been received from COM port.
        /// </summary>
        public event StepMotorEventHandler ErrorRecieved;

        /// <summary>
        /// Stores last raw response from the COM port.
        /// </summary>
        public byte[] LastResponse
        {
            get;
            private set;
        } = null;

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
            port = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
            // Event listeners
            port.DataReceived += Port_DataReceived;
            port.ErrorReceived += Port_ErrorReceived;
            // Opens port
            port.Open();
        }

        /// <summary>
        /// Handles internal COM port ErrorReceived.
        /// </summary>
        /// <param name="sender">COM port.</param>
        /// <param name="e">Event arguments.</param>
        private void Port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            // Reads last response
            LastResponse = new byte[port.BytesToRead];
            port.Read(LastResponse, 0, LastResponse.Length);
            // Indicates command received response
            commandSent = false;

            // IF events are not suppressed, fires respective public event
            if (!suppressEvents)
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
            if (port.BytesToRead > 0)
            {
                //Reads last response
                LastResponse = new byte[port.BytesToRead];
                port.Read(LastResponse, 0, LastResponse.Length);
                // Indicates command received response
                commandSent = false;

                // IF events are not suppressed, fires respective public event
                if (!suppressEvents)
                    OnDataReceived(new StepMotorEventArgs(LastResponse));
            }
        }

        /// <summary>
        /// Waits for response. Waits until commandSent if false, which is set in internal
        /// COM port event handlers. 
        /// Based on <see cref="System.Threading.SpinWait.SpinUntil(Func{bool}, TimeSpan)"/>.
        /// </summary>
        /// <param name="timeOutMS">Timeout to wait. -1 is not recommended.</param>
        /// <returns>An isntance of <see cref="Reply"/> generated from response byte array.</returns>
        private Reply WaitResponse(int timeOutMS = 200)
        {
            // Waits for response
            System.Threading.SpinWait.SpinUntil(() => !commandSent, timeOutMS);

            // If response still has not been received, throws
            if (commandSent)
            {
                commandSent = false;
                throw new InvalidOperationException("No response received");
            }

            return new Reply(LastResponse);
        }

        public Reply SendCommand(Command command, int argument, 
            byte type = (byte)CommandType.Unused, 
            byte address = 1, byte motorOrBank = 0, int waitResponseTimeMS = 200)
        {
            
            commandSent = true;

            byte[] val = BitConverter.GetBytes(argument);
            val = BitConverter.IsLittleEndian ? val.Reverse().ToArray() : val;


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
                0
            };

            int sum = toSend.Aggregate((sm, x) => sm += x);
            toSend[8] = Convert.ToByte(sum & 0x00FF);

            port.Write(toSend, 0, toSend.Length);

            return WaitResponse(waitResponseTimeMS);
        }

        /// <summary>
        /// Implements interface and frees resources
        /// </summary>
        public void Dispose()
        {
            port.Close();
            port.Dispose();
        }

        public Dictionary<AxisParameter, int> GetStatus(byte address = 1, byte motorOrBank = 0, bool suppressEvents = true)
        {
            var oldState = this.suppressEvents;
            this.suppressEvents = suppressEvents;

            try
            {
                Dictionary<AxisParameter, int> status = new Dictionary<AxisParameter, int>();

                for (byte i = 0; i < 14; i++)
                {
                    SendCommand(Command.GetAxisParameter, 0, i, address, motorOrBank);
                    WaitResponse();
                    Reply r = new Reply(LastResponse);
                    if (r.Status == ReturnStatus.Success)
                        status[(AxisParameter)i] = r.ReturnValue;

                }

                return status;
            }
            finally
            {
                this.suppressEvents = oldState;
            }

        }


        public void WaitPositionReached(byte address = 1, byte motorOrBank = 0, 
            bool suppressEvents = true, int timeOutMS = 10000, int checkIntervalMS = 200)
        {
            bool oldState = this.suppressEvents;
            this.suppressEvents = suppressEvents;
            try
            {
                SendCommand(Command.GetAxisParameter, 0, (byte)AxisParameter.TargetPoisitionReached, address, motorOrBank);
                WaitResponse();
                Reply r = new Reply(LastResponse);

                while (r.Status == ReturnStatus.Success && r.ReturnValue == 0)
                {
                    Task.Delay(checkIntervalMS);
                    SendCommand(Command.GetAxisParameter, 0, (byte)AxisParameter.TargetPoisitionReached, address, motorOrBank);
                    WaitResponse();
                    r = new Reply(LastResponse);
                }

            }
            finally
            {
                this.suppressEvents = oldState;
            }

        }

        /// <summary>
        /// Used to fire DataReceived event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnDataReceived(StepMotorEventArgs e)
            => DataRecieved?.Invoke(this, e);

        /// <summary>
        /// Used to fire ErrorReceived event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnErrorReceived(StepMotorEventArgs e)
            => ErrorRecieved?.Invoke(this, e);
    }

}

