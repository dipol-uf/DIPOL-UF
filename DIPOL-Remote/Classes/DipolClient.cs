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
        private static readonly Uri endpoint = new Uri(@"net.tcp://localhost:400/DipolRemote");

        private IRemoteControl remote = null;
        private InstanceContext context = new InstanceContext(new RemoteCallbackHandler());

        private IRemoteControl Remote
            => remote ?? throw CommunicationException;

        public string SessionID
            => Remote.SessionID;

        public DipolClient()
        {
            remote = new DuplexChannelFactory<IRemoteControl>(
                context, 
                new NetTcpBinding(), 
                new EndpointAddress(endpoint)).CreateChannel();
            
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
            (remote as ICommunicationObject)?.Close();
        }

        private static CommunicationException CommunicationException
            => new CommunicationException("Connection to service is not established yet.");
        
    }
}
