//    This file is part of Dipol-3 Camera Manager.

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
using System.IO.Ports;
using System.Threading.Tasks;
using StepMotor;

namespace Tests
{
    public static class MainApp
    {
        public static async Task<int> Main()
        {
            using (var port = new SerialPort(StaticConfigurationProvider.RetractorMotorPort))
            using (var motor = new StepMotorHandler(port, defaultTimeOut: TimeSpan.FromMilliseconds(50)))
            {
                //var status = await motor.GetStatusAsync();

                //await motor.SendCommandAsync(Command.MoveToPosition, 50000, CommandType.Absolute);
                //await motor.WaitForPositionReachedAsync();

                //status = await motor.GetRotationStatusAsync();

                //await motor.ReferenceReturnToOriginAsync();

                //status = await motor.GetRotationStatusAsync();
                await Task.Yield();
                try
                {
                    var pos_1 = await motor.GetActualPositionAsync();
                    Console.WriteLine(pos_1);
                    var pos_2 = await motor.GetActualPositionAsync();
                    Console.WriteLine(pos_2);
                    Console.WriteLine((pos_1, pos_2));
                }
                catch(Exception e)
                {
                    Console.WriteLine($"Exception {e.Message}");
                    throw;
                }
            }
            Console.ReadKey();
            return 0;
        }
    }
}
