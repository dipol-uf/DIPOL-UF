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
        private SerialPort port;
        private volatile bool commandSent = false;

        public event SerialDataReceivedEventHandler DataRecieved;
        public event SerialErrorReceivedEventHandler ErrorRecieved;

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
            OnErrorReceived(e);
        }

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (port.BytesToRead > 0)
            {
                commandSent = false;
                LastResponse = new byte[port.BytesToRead];
                port.Read(LastResponse, 0, LastResponse.Length);
                OnDataReceived(e);
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

        protected virtual void OnDataReceived(SerialDataReceivedEventArgs e)
            => DataRecieved?.Invoke(this, e);

        protected virtual void OnErrorReceived(SerialErrorReceivedEventArgs e)
            => ErrorRecieved?.Invoke(this, e);
    }

}
