//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018-2019 Ilia Kosenkov
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
using System.Diagnostics;
using System.ServiceModel;
using DIPOL_Remote.Remote;

namespace DIPOL_Remote
{
    public sealed class DipolHost : ServiceHost, IDisposable
    {
        //private const string LogName = @"Dipol Event Log";
        // TODO: To resources
        private const string SourceName = @"Dipol Remote Camera Service";

        //public static IReadOnlyDictionary<int, DipolHost> OpenedHosts
        //    => _OpenedHosts;

        public delegate void HostEventHandler(object source, string message);

        public event HostEventHandler EventReceived;

        public DipolHost(Uri serviceUri)
        {
            var endpoint = serviceUri ?? throw new ArgumentNullException(nameof(serviceUri));
            //if (!EventLog.SourceExists(SourceName))
            //    EventLog.CreateEventSource(SourceName, LogName);

            var bnd = new NetTcpBinding(SecurityMode.None)
            {
                OpenTimeout = TimeSpan.FromHours(24),
                CloseTimeout = TimeSpan.FromSeconds(15),
                SendTimeout = TimeSpan.FromHours(12),
                ReceiveTimeout = TimeSpan.FromHours(12)
            };
           
            InitializeDescription(typeof(RemoteControl), new UriSchemeKeyedCollection(endpoint));

            AddServiceEndpoint(typeof(IRemoteControl),bnd, "");

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

        public void Dispose()
        {
            Close();
        }

        public void OnEventReceived(object sender, string message)
            => EventReceived?.Invoke(sender, message);

    }
}
