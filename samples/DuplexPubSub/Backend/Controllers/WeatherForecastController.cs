using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Repositories;
using Common.Events;
using Common.Models;
using EventDriven.EventBus.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IEventBus _eventBus;
        private readonly WeatherForecastRepository _weatherRepo;
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(IEventBus eventBus, WeatherForecastRepository weatherRepo,
            ILogger<WeatherForecastController> logger)
        {
            _eventBus = eventBus;
            _weatherRepo = weatherRepo;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            // Publish event
            await _eventBus.PublishAsync(new WeatherForecastRequestedEvent(), null, "v1");
            _logger.LogInformation("Weather forecast requested");

            // Get notified when weather changes
            var weatherChanged = false;
            var weatherForecasts = Enumerable.Empty<WeatherForecast>();
            _weatherRepo.WeatherChangedEvent += (_, _) =>
            {
                weatherChanged = true;
                weatherForecasts = _weatherRepo.WeatherForecasts;
                _logger.LogInformation("Weather forecast generated");
            };

            // Wait in a loop for a response from the weather generator service.
            // NOTE: In a real-world scenario you would instead subscribe to events using SignalR,
            // so that the client HTTP request does not block.
            var elapsed = 0;
            var timeout = 10;
            while (!weatherChanged && elapsed < timeout)
            {
                await Task.Delay(1000);
                elapsed++;
            }
            return weatherForecasts;
        }
    }
}
