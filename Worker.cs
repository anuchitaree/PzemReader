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

        private readonly List<float> _energerTotal = new();
        private readonly List<float> _powerTotal = new();

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
                opt.DataBits,
                opt.StopBits,
                opt.Parity,
                opt.SlaveId
            );
            _currentMinute = GetMinuteKey(DateTime.Now);


        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var nowUtc = DateTime.UtcNow;
                var minuteKey = GetMinuteKey(DateTime.Now);
               

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
                    await SaveRaw(data, nowUtc);  // every 5 วินาที


                   

                    // 👉 สะสมรายนาที
                    if (!_minuteBuffer.ContainsKey(minuteKey))
                    {
                        _minuteBuffer[minuteKey] = 0;
                        
                    }

                    _energerTotal.Add(delta);
                    _powerTotal.Add(data.Power);

                    _minuteBuffer[minuteKey] += delta;

                    // 👉 ถ้านาทีเปลี่ยน → flush
                    if (minuteKey != _currentMinute)
                    {

                        var totalEnergy = _energerTotal.Sum();
                        var PeakPower = _powerTotal.Count > 0 ? _powerTotal.Max() : 0;

                        await FlushMinute(_currentMinute, totalEnergy, PeakPower);

                        _energerTotal.Clear();
                        _powerTotal.Clear();
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
                Energy = data.Energy,
                Frequency = data.Frequency,
                PowerFactor = data.PowerFactor,
                Alarm = data.Alarm,
            });

            await db.SaveChangesAsync();
        }

        // 🔹 flush นาที
        private async Task FlushMinute(DateTime minute, float totalEnergy, float peakPower)
        {
            if (!_minuteBuffer.ContainsKey(minute))
                return;
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var energy = _minuteBuffer[minute];

                db.EnergyMinutes.Add(new EnergyMinute
                {
                    Minute = minute.ToUniversalTime(),
                    EnergyKwh = totalEnergy,
                    MaxPower = peakPower
                });

                await db.SaveChangesAsync();

                _logger.LogInformation($"Saved minute {minute}: {energy} kWh : {peakPower} kW");
            }
            catch (Exception ex)
            {
              _logger.LogError(ex, "Error saving minute data");
             
            }
            
        }
    }
}
