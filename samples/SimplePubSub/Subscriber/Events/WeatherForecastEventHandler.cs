using Microsoft.Extensions.Logging;
using Subscriber.Models;
using System.Threading.Tasks;
using EventDriven.EventBus.Abstractions;

namespace Subscriber.Events
{
    public class WeatherForecastEventHandler : IntegrationEventHandler<WeatherForecastEvent>
    {
        private readonly WeatherForecastRepository _weatherRepo;
        private readonly ILogger<WeatherForecastEventHandler> _logger;

        public WeatherForecastEventHandler(WeatherForecastRepository weatherRepo, ILogger<WeatherForecastEventHandler> logger)
        {
            _weatherRepo = weatherRepo;
            _logger = logger;
        }

        public override Task HandleAsync(WeatherForecastEvent @event)
        {
            _logger.LogInformation("Weather posted");
            _weatherRepo.WeatherForecasts = @event.WeatherForecasts;
            return Task.CompletedTask;
        }
    }
}
