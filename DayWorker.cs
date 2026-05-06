using PzemReader.Data;
using PzemReader.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;



namespace PzemReader
{
    public class DayWorker : BackgroundService
    {
        private readonly ILogger<DayWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan _period = TimeSpan.FromDays(1);
        public DayWorker(ILogger<DayWorker> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Hourly Worker started at: {time}", DateTimeOffset.Now);

            // ใช้ PeriodicTimer แทน Timer แบบเดิมใน .NET 8
            using PeriodicTimer timer = new PeriodicTimer(_period);

            // ทำงานทันที 1 ครั้งเมื่อ Start (ถ้าต้องการ)
            await DoWorkAsync(DateTime.UtcNow);

            // รอจนกว่าจะครบ 1 ชั่วโมง แล้วทำงานต่อใน Loop
            while (!stoppingToken.IsCancellationRequested &&
                   await timer.WaitForNextTickAsync(stoppingToken))
            {
                await DoWorkAsync(DateTime.UtcNow);
            }
        }

        private async Task DoWorkAsync(DateTime dateTime)
        {

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var lastDay = dateTime.AddDays(-1);

                (DateTime from, DateTime to) = GetHourFromtoUtc(lastDay);
                var daysKey = GetDayUtc(lastDay);
                var lasthourTable = await db.EnergyHours
                                        .Where(d => d.Hour >= from && d.Hour <= to).ToListAsync();
                if(lasthourTable.Count == 0)
                {
                    _logger.LogInformation("No data found for the last day: {day}", daysKey);
                    return;
                }

                var sumEnergy = lasthourTable.Sum(d => d.EnergyKwh);
                var maxPower = lasthourTable.Max(d => d.MaxPower);


                var checklastupdate = await db.EnergyDays
                        .Where(d => d.Day == daysKey)
                        .FirstOrDefaultAsync();

                if (checklastupdate == null)
                {
                    var newDayData = new EnergyDay
                    {
                        Day = daysKey,
                        EnergyKwh = sumEnergy,
                        MaxPower = maxPower
                    };
                    db.EnergyDays.Add(newDayData);
                    await db.SaveChangesAsync();
                }
                else
                {
                    checklastupdate.EnergyKwh = sumEnergy;
                    checklastupdate.MaxPower = maxPower;
                    db.EnergyDays.Update(checklastupdate);
                    await db.SaveChangesAsync();
                }





                // ใส่ Logic ของคุณที่นี่
                await Task.Delay(1000); // จำลองการทำงาน
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing work.");
            }

        }

        private (DateTime, DateTime) GetHourFromtoUtc(DateTime dt)
        {
            return (new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(dt.Year, dt.Month, dt.Day, 23, 59, 0, DateTimeKind.Utc));
        }

        private DateTime GetDayUtc(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, DateTimeKind.Utc);
        }

    }
}