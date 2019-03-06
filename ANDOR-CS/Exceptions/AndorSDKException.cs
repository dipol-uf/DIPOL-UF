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

        public string MethodName { get; }

        public AndorSdkException(string message, Exception innerException) 
            : base(message, innerException)
        { }

        public AndorSdkException(string message, uint errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
            MethodName = string.Empty;
        }

        public AndorSdkException(string message, uint errorCode, string methodName)
            : base(message)
        {
            ErrorCode = errorCode;
            MethodName = methodName;
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
                except = new AndorSdkException(
                    $"{name} returned error code.",
                    returnCode,
                    name);
                return true;
            }

            return false;
        }
    }
}
