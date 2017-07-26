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

using System.Runtime.Serialization;
using System.ServiceModel;

namespace DIPOL_Remote.Faults
{
    [DataContract(IsReference = false)]
    public class ServiceException
    {
        public static readonly FaultReason CameraCommunicationReason
            = new FaultReason($"Error occured while communicating with {nameof(ANDOR_CS.Classes.Camera)} object.");

        public static readonly FaultReason GeneralServiceErrorReason
            = new FaultReason("General error occured while communicating with service.");

        public static readonly FaultReason IllegalSessionReason
            = new FaultReason("Accessed camera belongs to another remote session.");

        public static FaultException<ServiceException> IllegalSessionFaultException()
            => new FaultException<ServiceException>(
                        new ServiceException()
                        {
                            Message = "Cannot remove camera used in our session.",
                            Details = "Specified camera is used in anoter session and therefore cannot be controlled from current session.",
                            MethodName = ""
                        },
                        IllegalSessionReason);

        [DataMember(IsRequired = true, Order = 0)]
        public string Message;
        [DataMember(IsRequired = true, Order = 1)]
        public string Details;
        [DataMember(IsRequired = true, Order = 2)]
        public string MethodName;
    }
}
