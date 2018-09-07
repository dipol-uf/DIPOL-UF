//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018 Ilia Kosenkov
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
using System.Collections.Concurrent;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using DIPOL_Remote.Interfaces;

namespace DIPOL_Remote.Classes
{
    public class DipolClient : IDisposable
    {
        private const string EndpointTemplate = @"net.tcp://{0}:400/DipolRemote"; //new Uri(@"net.tcp://localhost:400/DipolRemote");

        private readonly InstanceContext _context = new InstanceContext(new RemoteCallbackHandler());
        private readonly IRemoteControl _remote;

        internal static ConcurrentDictionary<(string sessionID, int camIndex), (ManualResetEvent Event, bool Success)>
            CameraCreatedEvents { get; } =
            new ConcurrentDictionary<(string sessionID, int camIndex), (ManualResetEvent, bool)>();

        public IRemoteControl Remote
            => _remote ?? throw CommunicationException;

        public string HostAddress
        {
            get;
        }

        public string SessionID
            => Remote.SessionID;

        public DipolClient(string host)
        {
            HostAddress = host;
            var bnd = new NetTcpBinding(SecurityMode.None)
            {
                MaxReceivedMessageSize = 512 * 512 * 8 * 2
            };
            // IMPORTANT! Limits the size of SOAP message. For larger images requires another implementation
            var endpoint = new Uri(string.Format(EndpointTemplate, host));
            _remote = new DuplexChannelFactory<IRemoteControl>(
                _context, 
                bnd, 
                new EndpointAddress(endpoint)).CreateChannel();

            
        }

        public DipolClient(string host, TimeSpan openTimeout, TimeSpan sendTimeout, TimeSpan operationTimeout, TimeSpan closeTimeout)
        {
            HostAddress = host;
            var bnd = new NetTcpBinding(SecurityMode.None)
            {
                MaxReceivedMessageSize = 512 * 512 * 8 * 2,
                OpenTimeout = openTimeout,
                SendTimeout = sendTimeout,
                CloseTimeout = closeTimeout
            };
            // IMPORTANT! Limits the size of SOAP message. For larger images requires another implementation


            var endpoint = new Uri(string.Format(EndpointTemplate, host));
            _remote = new DuplexChannelFactory<IRemoteControl>(
                _context,
                bnd,
                new EndpointAddress(endpoint)).CreateChannel();

            // ReSharper disable once SuspiciousTypeConversion.Global
            ((IDuplexContextChannel) _remote).OperationTimeout = operationTimeout;
        }


        public void Connect()
            => Remote.Connect();

        public void Disconnect()
            => Remote.Disconnect();

       
        public int GetNumberOfCameras()
            => Remote.GetNumberOfCameras();

        public int[] ActiveRemoteCameras()
            => Remote.GetCamerasInUse();

        public RemoteCamera CreateRemoteCamera(int camIndex = 0)
        {
            if (Remote.GetCamerasInUse().Contains(camIndex))
                throw new ArgumentException($"Camera with index {camIndex} is already in use.");
            Remote.CreateCamera(camIndex);
            return new RemoteCamera(Remote, camIndex);
        }

        public void RequestCreateRemoteCamera(int camIndex = 0)
        {
            if (Remote.GetCamerasInUse().Contains(camIndex))
                throw new ArgumentException($"Camera with index {camIndex} is already in use.");
            Remote.RequestCreateCamera(camIndex);
        }

        public void Dispose()
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            (_remote as ICommunicationObject)?.Close();
        }

        private static CommunicationException CommunicationException
            => new CommunicationException("Connection to service is not established yet.");
        
    }
}
