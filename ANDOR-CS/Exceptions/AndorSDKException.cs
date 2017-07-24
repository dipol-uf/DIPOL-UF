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

namespace ANDOR_CS.Exceptions
{
   
    public class AndorSDKException : Exception
    {
        
        public uint ErrorCode
        {
            get;
            private set;
        } = 0;

        public AndorSDKException(string message, Exception innerException) 
            : base(message, innerException)
        { }

        public AndorSDKException(string message, uint errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public override string ToString()
        {
            return string.Format("{0} [{1}]", base.Message, ErrorCode);
        }

        public static void ThrowIfError(uint returnCode, string name)
        {
            if (returnCode != ATMCD64CS.AndorSDK.DRV_SUCCESS)
                throw new AndorSDKException($"{name} returned error code.",
                    returnCode);
        }
    }
}
