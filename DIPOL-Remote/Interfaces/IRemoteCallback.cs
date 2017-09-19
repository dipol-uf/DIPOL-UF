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

using System.ServiceModel;

using DIPOL_Remote.Enums;

using ANDOR_CS.Events;

namespace DIPOL_Remote.Interfaces
{
    public interface IRemoteCallback
    {
        [OperationContract(IsOneWay = true)]
        void NotifyRemotePropertyChanged(int camIndex, string session, string proeprty);

        [OperationContract(IsOneWay = true)]
        void NotifyRemoteTemperatureStatusChecked(
            int camIndex, string session, TemperatureStatusEventArgs args);

        [OperationContract(IsOneWay = true)]
        void NotifyRemoteAcquisitionEventHappened(
            int camIndex, string session, AcquisitionEventType type, AcquisitionStatusEventArgs args);

        [OperationContract(IsOneWay = true)]
        void NotifyRemoteNewImageReceivedEventHappened(int camIndex, string session, NewImageReceivedEventArgs e);

    }
}
