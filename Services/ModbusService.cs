using HslCommunication.ModBus;
using PzemReader.Models;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace PzemReader.Services
{
    public class ModbusService
    {

        private readonly ModbusRtu _modbus;
        private readonly byte _slaveId;

        public ModbusService(string port, int baudRate,int databit,int stopbit,string parityStr, byte slaveId)
        {
            _slaveId = slaveId;

            _modbus = new ModbusRtu();
            _modbus.Station = slaveId;

            _modbus.SerialPortInni(sp =>
            {
                sp.PortName = port;
                sp.BaudRate = baudRate;
                sp.DataBits = databit; //8;
                sp.StopBits = (StopBits)stopbit; //  System.IO.Ports.StopBits.One;
                sp.Parity = (Parity)Enum.Parse(typeof(Parity), parityStr, true); //  System.IO.Ports.Parity.None;
            });

            _modbus.Open();
        }

        public PzemData ReadData()
        {
            // อ่าน 9 registers
            var result = _modbus.ReadUInt16("x=3;0", 10);

            if (!result.IsSuccess)
                throw new Exception(result.Message);

            var r = result.Content;

            float voltage = r[0] / 10.0f;

            uint currentRaw = ((uint)r[2] << 16) | r[1];
            float current = currentRaw / 1000.0f;

            uint powerRaw = ((uint)r[4] << 16) | r[3];
            float power = powerRaw / 10.0f;

            uint energyRaw = ((uint)r[6] << 16) | r[5];
            float energy = energyRaw/1000.0f;

            float frequency = r[7] / 10.0f;
            float pf = r[8] / 100.0f;

            int alarm = r[9];

            return new PzemData
            {
                Timestamp = DateTime.UtcNow,
                Voltage = voltage,
                Current = current,
                Power = power,
                Energy = energy,
                Frequency = frequency,
                PowerFactor = pf,
                Alarm = alarm,
            };
        }

    }
}
