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
using NUnit.Framework;
using StepMotor;

namespace Tests
{
    [TestFixture]
    public class RetractorMotorStaticTests
    {
        private StepMotorHandler _device;
        private const string TestPort = @"COM4";
        [TearDown]
        public void TearDown()
        {
            _device?.Dispose();
            _device = null;
        }

        [Theory]
        public async Task Test_TryCreateFromAddress()
        {
            var ports = SerialPort.GetPortNames();
            Assume.That(ports, Contains.Item(TestPort));
            Assume.That(_device, Is.Null);

            _device = await StepMotorHandler.TryCreateFromAddress(TestPort, 1);
            Assume.That(_device, Is.Not.Null);
            Assert.That(await _device.GetActualPositionAsync(), Is.EqualTo(0));

        }

        [Theory]
        public async Task Test_TryCreateFirst()
        {
            var ports = SerialPort.GetPortNames();
            Assume.That(ports, Contains.Item(TestPort));
            Assume.That(_device, Is.Null);

            _device = await StepMotorHandler.TryCreateFirst(TestPort);
            Assume.That(_device, Is.Not.Null);
            
            Assert.That(await _device.GetActualPositionAsync(), Is.EqualTo(0));

        }
    }
}
