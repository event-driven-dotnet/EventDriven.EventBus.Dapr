using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Subscriber.Models;
using System.Collections.Generic;

namespace Subscriber.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly WeatherForecastRepository _weatherRepo;
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(WeatherForecastRepository weatherRepo, ILogger<WeatherForecastController> logger)
        {
            _weatherRepo = weatherRepo;
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            _logger.LogInformation("Weather forecast requested");
            return _weatherRepo.WeatherForecasts;
        }
    }
}
