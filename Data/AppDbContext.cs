using Microsoft.EntityFrameworkCore;
using PzemReader.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PzemReader.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<PzemData> PzemDatas { get; set; }
        public DbSet<EnergyDay> EnergyDays { get; set; }
        public DbSet<EnergyHour> EnergyHours { get; set; }
        public DbSet<EnergyMinute> EnergyMinutes { get; set; }


        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
    }
}
