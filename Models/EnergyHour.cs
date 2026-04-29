using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PzemReader.Models
{
    public class EnergyHour
    {
        public DateTime Hour { get; set; }
        public float EnergyKwh { get; set; }
        public float MaxPower { get; set; }
    }
}
