using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PzemReader.Models
{
    public class PzemData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]  // 👈 auto increase
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }

        public float Voltage { get; set; }
        public float Current { get; set; }
        public float Power { get; set; }
        public float Energy { get; set; }
        public float Frequency { get; set; }
        public float PowerFactor { get; set; }

        public int Alarm { get; set; }
    }
}
