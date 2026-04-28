using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PzemReader.Models
{
    public class PzemData
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }

        public float Voltage { get; set; }
        public float Current { get; set; }
        public float Power { get; set; }
        public float Energy { get; set; }
        public float Frequency { get; set; }
        public float PowerFactor { get; set; }
    }
}
