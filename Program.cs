using PzemReader.Models;
using PzemReader.Services;

namespace PzemReader
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddHttpClient("Api1", client =>
            {
                client.BaseAddress =
                    new Uri("http://localhost:5228/");

                client.Timeout =
                    TimeSpan.FromSeconds(10);
            });
            ;

            builder.Services.AddSingleton<ApiService>();

            builder.Services.Configure<ModbusOptions>(
            builder.Configuration.GetSection("Modbus"));

            builder.Services.AddHostedService<Worker>();
           

            var host = builder.Build();

           



            host.Run();
        }
    }
}