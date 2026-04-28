using HslCommunication.ModBus;
using PzemReader.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PzemReader.Services
{
    public class ModbusService
    {

        private readonly ModbusRtu _modbus;
        private readonly byte _slaveId;

        public ModbusService(string port, int baudRate, byte slaveId)
        {
            _slaveId = slaveId;

            _modbus = new ModbusRtu();
            _modbus.SerialPortInni(sp =>
            {
                sp.PortName = port;
                sp.BaudRate = baudRate;
                sp.DataBits = 8;
                sp.StopBits = System.IO.Ports.StopBits.One;
                sp.Parity = System.IO.Ports.Parity.None;
            });

            _modbus.Open();
        }

        public PzemData ReadData()
        {
            // อ่าน 9 registers
            var result = _modbus.ReadUInt16("x=4;0", 9);

            if (!result.IsSuccess)
                throw new Exception(result.Message);

            var r = result.Content;

            float voltage = r[0] / 10.0f;

            uint currentRaw = ((uint)r[1] << 16) | r[2];
            float current = currentRaw / 1000.0f;

            uint powerRaw = ((uint)r[3] << 16) | r[4];
            float power = powerRaw / 10.0f;

            uint energyRaw = ((uint)r[5] << 16) | r[6];
            float energy = energyRaw;

            float frequency = r[7] / 10.0f;
            float pf = r[8] / 100.0f;

            return new PzemData
            {
                Timestamp = DateTime.UtcNow,
                Voltage = voltage,
                Current = current,
                Power = power,
                Energy = energy,
                Frequency = frequency,
                PowerFactor = pf
            };
        }

    }
}
