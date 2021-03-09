﻿//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018 Ilia Kosenkov
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
using System.Collections.Generic;
using System.Collections.Immutable;
using ANDOR_CS;
using ANDOR_CS.Classes;
using DIPOL_Remote;
using Serilog;
using StepMotor;

namespace DIPOL_UF
{
    internal static class Injector
    {
        private static ILogger _loggerInst = null;

        public static void SetLogger(ILogger logger)
        {
            if (_loggerInst is {})
                throw new InvalidOperationException(@"Logger has been already set");
            _loggerInst = logger;
        }
        public static IAsyncMotorFactory NewStepMotorFactory() 
            => new StepMotorHandler.StepMotorFactory();

        public static IDeviceFactory NewLocalDeviceFactory()
            => new LocalCamera.LocalCameraFactory();
            
        #if  DEBUG
        public static IDeviceFactory NewDebugDeviceFactory()
            => new DebugCamera.DebugCameraFactory();
        #endif

        public static IDeviceFactory NewRemoteDeviceFactory(IControlClient client)
            => new RemoteCamera.RemoteCameraFactory(client);

        public static IControlClientFactory NewClientFactory()
            => new DipolClient.DipolClientFactory();


        public static ILogger GetLogger() => _loggerInst ?? Log.Logger;
//#if DEBUG
//        public static IDeviceFactory NewDebugDeviceFactory()
//            => new DebugCamera.DebugCameraFactory();
//#endif
    }
}
