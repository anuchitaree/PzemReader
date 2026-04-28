using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PzemReader.Models
{
    public class ModbusOptions
    {
        public string Port { get; set; } = default!;
        public int BaudRate { get; set; }
        public byte SlaveId { get; set; }
    }
}
