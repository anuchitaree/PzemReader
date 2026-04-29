using Microsoft.Extensions.Options;
using PzemReader.Data;
using PzemReader.Models;
using PzemReader.Services;

namespace PzemReader
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ModbusService _modbus;

        private float? _lastEnergy = null;

        private readonly Dictionary<DateTime, float> _minuteBuffer = new();
        private DateTime _currentMinute;

        public Worker(ILogger<Worker> logger,
            IServiceScopeFactory scopeFactory,
            IOptions<ModbusOptions> options)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;

            var opt = options.Value;

            _modbus = new ModbusService(
                opt.Port,
                opt.BaudRate,
                opt.SlaveId
            );
            _currentMinute = GetMinuteKey(DateTime.UtcNow);

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var minuteKey = GetMinuteKey(now);


                try
                {
                    var data = _modbus.ReadData(); // ต้องมี EnergyTotal

                    float delta = 0;

                    if (_lastEnergy.HasValue)
                    {
                        delta = data.Energy - _lastEnergy.Value;

                        // กัน reset / overflow
                        if (delta < 0 || delta > 5)
                        {
                            _logger.LogWarning("Energy reset detected");
                            delta = 0;
                        }
                    }
                    _lastEnergy = data.Energy;

                    // 👉 เก็บ RAW
                    await SaveRaw(data, now);

                    // 👉 สะสมรายนาที
                    if (!_minuteBuffer.ContainsKey(minuteKey))
                        _minuteBuffer[minuteKey] = 0;

                    _minuteBuffer[minuteKey] += delta;

                    // 👉 ถ้านาทีเปลี่ยน → flush
                    if (minuteKey != _currentMinute)
                    {
                        await FlushMinute(_currentMinute);

                        _minuteBuffer.Remove(_currentMinute);
                        _currentMinute = minuteKey;
                    }


                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading Modbus");
                }

                await Task.Delay(5000, stoppingToken);
            }
        }
        private DateTime GetMinuteKey(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0);
        }
        // 🔹 save raw
        private async Task SaveRaw(dynamic data, DateTime now)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.PzemDatas.Add(new PzemData
            {
                Timestamp = now,
                Voltage = data.Voltage,
                Current = data.Current,
                Power = data.Power,
                Energy = data.EnergyTotal,
                Frequency = data.Frequency,
                PowerFactor = data.PowerFactor,
            });

            await db.SaveChangesAsync();
        }

        // 🔹 flush นาที
        private async Task FlushMinute(DateTime minute)
        {
            if (!_minuteBuffer.ContainsKey(minute))
                return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var energy = _minuteBuffer[minute];

            db.EnergyMinutes.Add(new EnergyMinute
            {
                Minute = minute,
                EnergyKwh = energy
            });

            await db.SaveChangesAsync();

            _logger.LogInformation($"Saved minute {minute}: {energy} kWh");
        }
    }
}
