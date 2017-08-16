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
using System.Collections.Concurrent;

using System.ServiceModel;

using DIPOL_Remote.Interfaces;

namespace DIPOL_Remote.Classes
{
    public class DipolHost : IDisposable
    {
        private static readonly Uri endpoint = new Uri(@"net.tcp://localhost:400/DipolRemote");
        private static ConcurrentDictionary<int, DipolHost> _OpenedHosts;

        private ServiceHost host = null;


        public static IReadOnlyDictionary<int, DipolHost> OpenedHosts
            => _OpenedHosts as IReadOnlyDictionary<int, DipolHost>;


        public DipolHost()
        {
            host = new ServiceHost(typeof(RemoteControl), endpoint);

            host.AddServiceEndpoint(typeof(IRemoteControl), new NetTcpBinding(SecurityMode.None), "");

            _OpenedHosts.TryAdd(host.BaseAddresses[0].GetHashCode(), this);
        }

        public void Host() => host?.Open();

      
        public void Dispose()
        {
            var baseAddress = host.BaseAddresses[0];

            host?.Close();

            _OpenedHosts.TryRemove(baseAddress.GetHashCode(), out _);
        }


    }
}
