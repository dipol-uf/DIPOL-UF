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
