using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Frontend
{
    public class WeatherClient
    {
        private readonly JsonSerializerOptions _options = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly HttpClient _client;

        public WeatherClient(IHttpClientFactory clientFactory)
        {
            _client = clientFactory.CreateClient("backend");
        }

        public async Task<WeatherForecast[]> GetWeatherAsync()
        {
            var responseMessage = await _client.GetAsync("/weatherforecast");
            responseMessage.EnsureSuccessStatusCode();
            var stream = await responseMessage.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<WeatherForecast[]>(stream, _options);
        }
    }
}