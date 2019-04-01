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
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using StepMotor;

namespace Tests
{
    [TestFixture]
    public class StepMotorTests
    {
        [Test]
        public async Task Test_SimpleCommand()
        {
            var ports = SerialPort.GetPortNames();

            var devices = await Task.WhenAll(ports.Select(async x => (Port: x, Addresses: await StepMotorHandler.FindDevice(x))));

            Assert.IsTrue(devices.Any(x => x.Addresses.Count > 0));

        }

        [Theory]
        public async Task Test_Rotation()
        {
            var deviceAddresses = await StepMotorHandler.FindDevice("COM1");
            Assume.That(deviceAddresses.Count, Is.EqualTo(1));

            using (var motor = new StepMotorHandler("COM1", deviceAddresses[0]))
            {
                var reply = await motor.SendCommandAsync(Command.MoveToPosition, 10000, CommandType.Absolute);
                Assert.AreEqual(ReturnStatus.Success, reply.Status);

                await Task.Delay(TimeSpan.FromMilliseconds(500));

                reply = await motor.SendCommandAsync(Command.GetAxisParameter, 1);
                Assert.AreEqual(ReturnStatus.Success, reply.Status);
                Assert.AreEqual(10000, reply.ReturnValue);

                reply = await motor.SendCommandAsync(Command.MoveToPosition, 0, CommandType.Absolute);
                Assert.AreEqual(ReturnStatus.Success, reply.Status);

                await Task.Delay(TimeSpan.FromMilliseconds(500));


                reply = await motor.SendCommandAsync(Command.GetAxisParameter, 1);
                Assert.AreEqual(ReturnStatus.Success, reply.Status);
                Assert.AreEqual(0, reply.ReturnValue);


            }
        }

    }
}
