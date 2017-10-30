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
    public class Rotator : IDisposable
    {
        public delegate void RotatorEventHandler(object sender, RotatorEventArgs e);

        private SerialPort port;
        private volatile bool commandSent = false;
        private volatile bool suppressEvents = false;

        public event RotatorEventHandler DataRecieved;
        public event RotatorEventHandler ErrorRecieved;

        public byte[] LastResponse
        {
            get;
            private set;
        } = null;

        public Rotator(string portName)
        {
            port = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
            port.DataReceived += Port_DataReceived;
            port.ErrorReceived += Port_ErrorReceived;
            port.Open();
        }

        private void Port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            LastResponse = new byte[port.BytesToRead];
            port.Read(LastResponse, 0, LastResponse.Length);
            commandSent = false;

            if (!suppressEvents)
                OnErrorReceived(new RotatorEventArgs(LastResponse));
        }

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (port.BytesToRead > 0)
            {
                LastResponse = new byte[port.BytesToRead];
                port.Read(LastResponse, 0, LastResponse.Length);
                commandSent = false;
                if(!suppressEvents)
                    OnDataReceived(new RotatorEventArgs(LastResponse));
            }
        }

        public void SendCommand(Command command, int argument, 
            byte type = (byte)CommandType.Unused, 
            byte address = 1, byte motorOrBank = 0)
        {
            if (commandSent)
                System.Threading.SpinWait.SpinUntil(() => !commandSent, 100 * 5);

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
        }

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

        public Reply WaitResponse(int timeOutMS = 1000)
        {
            System.Threading.SpinWait.SpinUntil(() => !commandSent, timeOutMS);
            return new Reply(LastResponse);
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

        protected virtual void OnDataReceived(RotatorEventArgs e)
            => DataRecieved?.Invoke(this, e);

        protected virtual void OnErrorReceived(RotatorEventArgs e)
            => ErrorRecieved?.Invoke(this, e);
    }

}
