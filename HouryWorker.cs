using Microsoft.EntityFrameworkCore;
using PzemReader.Data;
using PzemReader.Models;

namespace PzemReader
{
    public class HouryWorker : BackgroundService
    {
        private readonly ILogger<HouryWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan _period = TimeSpan.FromHours(1);

        public HouryWorker(ILogger<HouryWorker> logger,
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
                await Task.Delay(5000);
                await DoWorkAsync(DateTime.UtcNow);
            }
        }

        private async Task DoWorkAsync(DateTime dateTime)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();



                var nowHour = dateTime;

                (DateTime from0, DateTime to0) = GetHourFromtoUtc(nowHour);
                var nowdayKey = GetDayUtc(nowHour);

                var todayTable = await db.EnergyHours
                                        .Where(d => d.Hour >= from0 && d.Hour <= to0).ToListAsync();
                if (todayTable.Count == 0)
                {
                    _logger.LogInformation("No data found for today: {nowhourKey}", nowdayKey);
                    return;
                }
                var sumEnergyToday = todayTable.Sum(d => d.EnergyKwh);  
                var maxPowerToday = todayTable.Max(d => d.MaxPower);
               var checktodayupdate = await db.EnergyDays
                        .Where(d => d.Day == nowdayKey)
                        .FirstOrDefaultAsync();
                if (checktodayupdate == null)
                {
                    var newDayData = new EnergyDay
                    {
                        Day = nowdayKey,
                        EnergyKwh = sumEnergyToday,
                        MaxPower = maxPowerToday
                    };
                    db.EnergyDays.Add(newDayData);
                    await db.SaveChangesAsync();
                }
                else
                {
                    checktodayupdate.EnergyKwh = sumEnergyToday;
                    checktodayupdate.MaxPower = maxPowerToday;
                    db.EnergyDays.Update(checktodayupdate);
                    await db.SaveChangesAsync();
                }






                var lastHour = dateTime.AddHours(-1);
                (DateTime from, DateTime to) = GetMinuteFromtoUtc(lastHour);
                var hourKey = GetHourUtc(lastHour);
                var lasthourTable = await db.EnergyMinutes
                                        .Where(d => d.Minute >= from && d.Minute <= to).ToListAsync();

                if (lasthourTable.Count == 0)
                {
                    _logger.LogInformation("No data found for the last hour: {hourKey}", hourKey);
                    return;
                }

                var sumEnergy = lasthourTable.Sum(d => d.EnergyKwh);
                var maxPower = lasthourTable.Max(d => d.MaxPower);

                var checklastupdate = await db.EnergyHours
                        .Where(d => d.Hour == hourKey)
                        .FirstOrDefaultAsync();

                if (checklastupdate == null)
                {
                    var newHourData = new EnergyHour
                    {
                        Hour = hourKey,
                        EnergyKwh = sumEnergy,
                        MaxPower = maxPower
                    };
                    db.EnergyHours.Add(newHourData);
                    await db.SaveChangesAsync();
                }
                else
                {
                    checklastupdate.EnergyKwh = sumEnergy;
                    checklastupdate.MaxPower = maxPower;
                    db.EnergyHours.Update(checklastupdate);
                    await db.SaveChangesAsync();
                }

                var lasthourQ1 = new Energy15Minute
                {
                    Minute = GetMinuteQ1(lastHour).Item3,
                    EnergyKwh = lasthourTable
                        .Where(d => d.Minute >= GetMinuteQ1(lastHour).Item1 && d.Minute <= GetMinuteQ1(lastHour).Item2)
                        .Sum(d => d.EnergyKwh),
                    MaxPower = lasthourTable
                        .Where(d => d.Minute >= GetMinuteQ1(lastHour).Item1 && d.Minute <= GetMinuteQ1(lastHour).Item2)
                        .Max(d => d.MaxPower)
                };
                var getlast15minQ1 = await db.Energy15Minutes
                        .Where(d => d.Minute == lasthourQ1.Minute)
                        .FirstOrDefaultAsync();
                if (getlast15minQ1 == null)
                {
                    await db.Energy15Minutes.AddAsync(lasthourQ1);
                }
                else
                {
                    getlast15minQ1.EnergyKwh = lasthourQ1.EnergyKwh;
                    getlast15minQ1.MaxPower = lasthourQ1.MaxPower;
                }



                var lasthourQ2 = new Energy15Minute
                {
                    Minute = GetMinuteQ2(lastHour).Item3,
                    EnergyKwh = lasthourTable
                        .Where(d => d.Minute >= GetMinuteQ2(lastHour).Item1 && d.Minute <= GetMinuteQ2(lastHour).Item2)
                        .Sum(d => d.EnergyKwh),
                    MaxPower = lasthourTable
                        .Where(d => d.Minute >= GetMinuteQ2(lastHour).Item1 && d.Minute <= GetMinuteQ2(lastHour).Item2)
                        .Max(d => d.MaxPower)
                };
                var getlast15minQ2 = await db.Energy15Minutes
                       .Where(d => d.Minute == lasthourQ2.Minute)
                       .FirstOrDefaultAsync();
                if (getlast15minQ2 == null)
                {
                    await db.Energy15Minutes.AddAsync(lasthourQ2);
                }
                else
                {
                    getlast15minQ2.EnergyKwh = lasthourQ2.EnergyKwh;
                    getlast15minQ2.MaxPower = lasthourQ2.MaxPower;
                }


                var lasthourQ3 = new Energy15Minute
                {
                    Minute = GetMinuteQ3(lastHour).Item3,
                    EnergyKwh = lasthourTable
                        .Where(d => d.Minute >= GetMinuteQ3(lastHour).Item1 && d.Minute <= GetMinuteQ3(lastHour).Item2)
                        .Sum(d => d.EnergyKwh),
                    MaxPower = lasthourTable
                        .Where(d => d.Minute >= GetMinuteQ3(lastHour).Item1 && d.Minute <= GetMinuteQ3(lastHour).Item2)
                        .Max(d => d.MaxPower)
                };
                var getlast15minQ3 = await db.Energy15Minutes
                       .Where(d => d.Minute == lasthourQ3.Minute)
                       .FirstOrDefaultAsync();
                if (getlast15minQ3 == null)
                {
                    await db.Energy15Minutes.AddAsync(lasthourQ3);
                }
                else
                {
                    getlast15minQ3.EnergyKwh = lasthourQ3.EnergyKwh;
                    getlast15minQ3.MaxPower = lasthourQ3.MaxPower;
                }

                var lasthourQ4 = new Energy15Minute
                {
                    Minute = GetMinuteQ4(lastHour).Item3,
                    EnergyKwh = lasthourTable
                        .Where(d => d.Minute >= GetMinuteQ4(lastHour).Item1 && d.Minute <= GetMinuteQ4(lastHour).Item2)
                        .Sum(d => d.EnergyKwh),
                    MaxPower = lasthourTable
                        .Where(d => d.Minute >= GetMinuteQ4(lastHour).Item1 && d.Minute <= GetMinuteQ4(lastHour).Item2)
                        .Max(d => d.MaxPower)
                };
                var getlast15minQ4 = await db.Energy15Minutes
                       .Where(d => d.Minute == lasthourQ4.Minute)
                       .FirstOrDefaultAsync();
                if (getlast15minQ4 == null)
                {
                    await db.Energy15Minutes.AddAsync(lasthourQ4);
                }
                else
                {
                    getlast15minQ4.EnergyKwh = lasthourQ4.EnergyKwh;
                    getlast15minQ4.MaxPower = lasthourQ4.MaxPower;
                }
                await db.SaveChangesAsync();



                // ใส่ Logic ของคุณที่นี่
                await Task.Delay(1000); // จำลองการทำงาน
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing work.");
            }
        }
        private (DateTime, DateTime) GetMinuteFromtoUtc(DateTime dt)
        {
            return (new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0, DateTimeKind.Utc),
                    new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 59, 0, DateTimeKind.Utc));
        }

        private DateTime GetHourUtc(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0, DateTimeKind.Utc);
        }


        private (DateTime, DateTime, DateTime) GetMinuteQ1(DateTime dt)
        {
            return (new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0, DateTimeKind.Utc),
                    new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 14, 0, DateTimeKind.Utc),
                    new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0, DateTimeKind.Utc));
        }
        private (DateTime, DateTime, DateTime) GetMinuteQ2(DateTime dt)
        {
            return (new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 15, 0, DateTimeKind.Utc),
                    new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 29, 0, DateTimeKind.Utc),
                    new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 15, 0, DateTimeKind.Utc));
        }
        private (DateTime, DateTime, DateTime) GetMinuteQ3(DateTime dt)
        {
            return (new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 30, 0, DateTimeKind.Utc),
                    new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 44, 0, DateTimeKind.Utc),
                    new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 30, 0, DateTimeKind.Utc));
        }
        private (DateTime, DateTime, DateTime) GetMinuteQ4(DateTime dt)
        {
            return (new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 45, 0, DateTimeKind.Utc),
                    new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 59, 0, DateTimeKind.Utc),
                    new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 45, 0, DateTimeKind.Utc));
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
