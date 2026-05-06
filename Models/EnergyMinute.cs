using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PzemReader.Models
{
    public class EnergyMinute
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]  // 👈 auto increase
        public int Id { get; set; }

        public DateTime Minute { get; set; }
        public float EnergyKwh { get; set; }
        public float MaxPower { get; set; } // 🔥 peak ใน 1 นาที
    }
}
