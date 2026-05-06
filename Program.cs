using Microsoft.EntityFrameworkCore;
using PzemReader.Data;
using PzemReader.Models;

namespace PzemReader
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
);

            builder.Services.Configure<ModbusOptions>(
            builder.Configuration.GetSection("Modbus"));

            builder.Services.AddHostedService<Worker>();
            builder.Services.AddHostedService<HouryWorker>();
            builder.Services.AddHostedService<DayWorker>();


            var host = builder.Build();

            using (var scope = host.Services.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    logger.LogInformation("Checking database...");

                    db.Database.Migrate(); // 🔥 สร้าง DB + Table ถ้ายังไม่มี

                    logger.LogInformation("Database ready");
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "Database init failed");

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ DATABASE ERROR");
                    Console.ResetColor();
                }
            }




            host.Run();
        }
    }
}