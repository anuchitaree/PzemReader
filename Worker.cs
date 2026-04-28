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
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var data = _modbus.ReadData();

                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    db.PzemDatas.Add(data);
                    await db.SaveChangesAsync();

                    _logger.LogInformation(
                        $"V={data.Voltage}V I={data.Current}A P={data.Power}W"
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading Modbus");
                }

                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
