using NUnit.Framework;
using StepMotor;
using System;
using System.IO.Ports;
using System.Threading.Tasks;

namespace Tests
{
    [TestFixture]
    public class MotorDebugTests
    {
        private StepMotorHandler _motor;
        private SerialPort _port;
        [SetUp]
        public void SetUp()
        {
            _port = new SerialPort("COM1");
            _motor = new StepMotorHandler(_port);
            //_motor.ReturnToOriginAsync().GetAwaiter().GetResult();
        }

        [TearDown]
        public void TearDown()
        {
            _motor?.Dispose();
            _port?.Dispose();
        }
        [Test]
        public async Task Test_1()
        {
            _motor.DataReceived += (sender, e) => { };
            _motor.ErrorReceived += (sender, e) => { };

            var pos_1 = await _motor.GetActualPositionAsync();
            var pos_2 = await _motor.GetActualPositionAsync();
            Assert.AreEqual(pos_1, pos_2);
        }
    }

}

