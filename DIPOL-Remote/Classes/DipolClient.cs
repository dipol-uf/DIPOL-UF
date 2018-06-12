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

using System.ServiceModel;

using DIPOL_Remote.Interfaces;

namespace DIPOL_Remote.Classes
{
    public class DipolClient : IDisposable
    {
        private const string EndpointTemplate = @"net.tcp://{0}:400/DipolRemote"; //new Uri(@"net.tcp://localhost:400/DipolRemote");

        private readonly IRemoteControl _remote;
        private readonly InstanceContext _context = new InstanceContext(new RemoteCallbackHandler());

        private IRemoteControl Remote
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

            ((IDuplexContextChannel)_remote).OperationTimeout = operationTimeout;
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

        public void Dispose()
        {
            (_remote as ICommunicationObject)?.Close();
        }

        private static CommunicationException CommunicationException
            => new CommunicationException("Connection to service is not established yet.");
        
    }
}
