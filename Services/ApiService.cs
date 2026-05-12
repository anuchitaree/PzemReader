using System.Text;
using System.Text.Json;

namespace PzemReader.Services
{
    public class ApiService
    {
        private readonly IHttpClientFactory _factory;

        public ApiService(IHttpClientFactory factory)
        {
            _factory = factory;
        }

        public async Task SendApi1(dynamic data)
        {
            var client = _factory.CreateClient("Api1");

            var json = JsonSerializer.Serialize(data);

            //using var form = new MultipartFormDataContent();

            //form.Add(
            //    new StringContent(json,
            //    Encoding.UTF8,
            //    "application/json"),
            //    "data");
            var content = new StringContent(
                            json,
                            Encoding.UTF8,
                            "application/json");

            var response =
                await client.PostAsync(
                    "api/v1/Database/post-pzemraw",
                    content);

            var result =
                await response.Content.ReadAsStringAsync();

            Console.WriteLine(result);
        }

        public async Task SendApi2()
        {
            var client = _factory.CreateClient("Api2");

            var response =
                await client.GetAsync("status");

            var result =
                await response.Content.ReadAsStringAsync();

            Console.WriteLine(result);
        }



    }
}
