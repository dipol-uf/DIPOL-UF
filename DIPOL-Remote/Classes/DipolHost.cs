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
//    Copyright 2017-2018, Ilia Kosenkov, Tuorla Observatory, Finland

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
        private static readonly Uri Endpoint = new Uri(@"net.tcp://localhost:400/DipolRemote");
        private const string LogName = @"Dipol Event Log";
        private const string SourceName = @"Dipol Remote Camera Service";
        private static readonly ConcurrentDictionary<int, DipolHost> _OpenedHosts =
            new ConcurrentDictionary<int, DipolHost>();

        private readonly ServiceHost _host;


        public static IReadOnlyDictionary<int, DipolHost> OpenedHosts
            => _OpenedHosts;

        public delegate void HostEventHandler(object source, string message);

        public event HostEventHandler EventReceived;

        public DipolHost()
        {
            if (!EventLog.SourceExists(SourceName))
                EventLog.CreateEventSource(SourceName, LogName);

            var bnd = new NetTcpBinding(SecurityMode.None)
            {
                OpenTimeout = TimeSpan.FromHours(24),
                CloseTimeout = TimeSpan.FromSeconds(15),
                SendTimeout = TimeSpan.FromHours(12),
                ReceiveTimeout = TimeSpan.FromHours(12)
            };


            _host = new ServiceHost(typeof(RemoteControl), Endpoint);

            _host.AddServiceEndpoint(typeof(IRemoteControl),bnd, "");

            _OpenedHosts.TryAdd(_host.BaseAddresses[0].GetHashCode(), this);

            EventReceived += (sender, message) =>
            {
                string senderString;
                if (sender is ANDOR_CS.Classes.CameraBase cam)
                    senderString = $"{cam.CameraModel}/{cam.SerialNumber}";
                else
                    senderString = sender.ToString();

                var logMessage = string.Format($"[{{0,23:yyyy/MM/dd HH-mm-ss.fff}}] @ {senderString}: {message}", DateTime.Now);

                EventLog.WriteEntry(SourceName, logMessage, EventLogEntryType.Information);

            };
        }
        
        public void Host() => _host?.Open();

      
        public void Dispose()
        {
            var baseAddress = _host.BaseAddresses[0];

            _host?.Close(TimeSpan.FromSeconds(15));

            _OpenedHosts.TryRemove(baseAddress.GetHashCode(), out _);
        }

        public virtual void OnEventReceived(object sender, string message)
            => EventReceived?.Invoke(sender, message);

    }
}
