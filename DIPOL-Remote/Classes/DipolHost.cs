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
using System.Diagnostics;
using System.ServiceModel;

using DIPOL_Remote.Interfaces;

namespace DIPOL_Remote.Classes
{
    public class DipolHost : IDisposable
    {
        private static readonly Uri endpoint = new Uri(@"net.tcp://localhost:400/DipolRemote");
        private static readonly string logName = @"Dipol Event Log";
        private static readonly string sourceName = @"Dipol Remote Camera Service";
        private static ConcurrentDictionary<int, DipolHost> _OpenedHosts = new ConcurrentDictionary<int, DipolHost>();

        private ServiceHost host = null;


        public static IReadOnlyDictionary<int, DipolHost> OpenedHosts
            => _OpenedHosts as IReadOnlyDictionary<int, DipolHost>;

        public delegate void HostEventHandler(object source, string message);

        public event HostEventHandler EventReceived;

        public DipolHost()
        {
            if (!EventLog.SourceExists(sourceName))
                EventLog.CreateEventSource(sourceName, logName);

            var bnd = new NetTcpBinding(SecurityMode.None);

            bnd.OpenTimeout = TimeSpan.FromHours(24);
            bnd.CloseTimeout = TimeSpan.FromSeconds(15);
            bnd.SendTimeout = TimeSpan.FromHours(12);
            bnd.ReceiveTimeout = TimeSpan.FromHours(12);

            host = new ServiceHost(typeof(RemoteControl), endpoint);

            host.AddServiceEndpoint(typeof(IRemoteControl),bnd, "");

            _OpenedHosts.TryAdd(host.BaseAddresses[0].GetHashCode(), this);

            EventReceived += (sender, message)
                =>
            {
                string senderString = "";
                if (sender is ANDOR_CS.Classes.CameraBase cam)
                    senderString = $"{cam.CameraModel}/{cam.SerialNumber}";
                else
                    senderString = sender.ToString();

                var logMessage = String.Format($"[{{0,23:yyyy/MM/dd HH-mm-ss.fff}}] @ {senderString}: {message}", DateTime.Now);

                EventLog.WriteEntry(sourceName, logMessage, EventLogEntryType.Information);

            };
        }
        
        public void Host() => host?.Open();

      
        public void Dispose()
        {
            var baseAddress = host.BaseAddresses[0];

            host?.Close(TimeSpan.FromSeconds(15));

            _OpenedHosts.TryRemove(baseAddress.GetHashCode(), out _);
        }

        public virtual void OnEventReceived(object sender, string message)
            => EventReceived?.Invoke(sender, message);

    }
}
