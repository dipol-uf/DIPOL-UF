﻿//    This file is part of Dipol-3 Camera Manager.

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
using System.ServiceModel;

using ANDOR_CS.Events;

using DIPOL_Remote.Interfaces;

namespace DIPOL_Remote.Classes
{
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Multiple,
        IncludeExceptionDetailInFaults = true)]
    class RemoteCallbackHandler : IRemoteCallback
    {

        public RemoteCallbackHandler()
        {
            Console.WriteLine("Remote control handler created");
        }

        public void NotifyRemotePropertyChanged(int camIndex, string session, string property)
            => RemoteCamera.NotifyRemotePropertyChanged(camIndex, session, property);

        public void NotifyRemoteTemperatureStatusChecked(
            int camIndex, string session, TemperatureStatusEventArgs args)
            => RemoteCamera.NotifyRemoteTemperatureStatusChecked(camIndex, session, args);

    }
}
