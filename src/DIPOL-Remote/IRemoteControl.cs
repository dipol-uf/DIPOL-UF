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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ServiceModel;

namespace DIPOL_Remote
{
    [ServiceContract(SessionMode = SessionMode.Required,
        CallbackContract = typeof(IRemoteCallback))]
    public interface IRemoteControl
    {
        string SessionID
        {
            [OperationContract(IsOneWay = false)]
            get;
        }

        [OperationContract(IsInitiating = true, IsOneWay = false)]
        void Connect();

        [OperationContract(IsTerminating = true, IsOneWay = false)]
        void Disconnect();

        [OperationContract(IsOneWay = false)]
        int GetNumberOfCameras();
        
    }
}
