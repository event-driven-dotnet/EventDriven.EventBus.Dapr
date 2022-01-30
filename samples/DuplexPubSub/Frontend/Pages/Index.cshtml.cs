using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Frontend.Pages
{
    public class IndexModel : PageModel
    {
        // ReSharper disable once NotAccessedField.Local
        private readonly ILogger<IndexModel> _logger;

        public WeatherForecast[] Forecasts { get; set; }

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public async Task OnPost([FromServices] WeatherClient client)
        {
            Forecasts = await client.GetWeatherAsync();
        }
    }
}
