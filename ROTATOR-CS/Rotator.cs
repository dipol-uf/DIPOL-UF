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
        private byte address = 1;
        private byte motorOrBank = 0;

        public event SerialDataReceivedEventHandler DataRecieved;
        public event SerialErrorReceivedEventHandler ErrorRecieved;

        public byte[] LastRespond
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
            ErrorRecieved(sender, e);
        }

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            LastRespond = new byte[port.BytesToRead];
            port.Read(LastRespond, 0, LastRespond.Length);

            DataRecieved(sender, e);
        }

        public void SendCommand(Commands command, int argument, byte type = 0)
        {
            
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
    }

}
