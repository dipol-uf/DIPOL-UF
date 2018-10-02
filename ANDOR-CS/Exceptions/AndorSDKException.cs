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

#if X86
using AndorSDK = ATMCD32CS.AndorSDK;
#endif
#if X64
using AndorSDK = ATMCD64CS.AndorSDK;
#endif


#pragma warning disable 1591
namespace ANDOR_CS.Exceptions
{
    /// <inheritdoc />
    public class AndorSdkException : Exception
    {
        
        public uint ErrorCode
        {
            get;
        }

        public AndorSdkException(string message, Exception innerException) 
            : base(message, innerException)
        { }

        public AndorSdkException(string message, uint errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public override string ToString()
        {
            return $"{Message} [{ErrorCode}]";
        }

        [Obsolete]
        public static void ThrowIfError(uint returnCode, string name)
        {
            if (returnCode != AndorSDK.DRV_SUCCESS 
                && returnCode != AndorSDK.DRV_NO_NEW_DATA)
                throw new AndorSdkException($"{name} returned error code.",
                    returnCode);
        }

        public static bool FailIfError(uint returnCode, string name, out Exception except)
        {
            except = null;
            if (returnCode != AndorSDK.DRV_SUCCESS
                && returnCode != AndorSDK.DRV_NO_NEW_DATA)
            {
                except = new AndorSdkException($"{name} returned error code.",
                    returnCode);
                return true;
            }

            return false;
        }
    }
}
