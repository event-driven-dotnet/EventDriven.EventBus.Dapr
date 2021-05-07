using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Backend.Repositories;
using Common.Events;
using EventDriven.EventBus.Abstractions;

namespace Backend.Handlers
{
    public class WeatherForecastGeneratedEventHandler : IntegrationEventHandler<WeatherForecastGeneratedEvent>
    {
        private readonly WeatherForecastRepository _weatherRepo;
        private readonly ILogger<WeatherForecastGeneratedEventHandler> _logger;

        public WeatherForecastGeneratedEventHandler(WeatherForecastRepository weatherRepo, ILogger<WeatherForecastGeneratedEventHandler> logger)
        {
            _weatherRepo = weatherRepo;
            _logger = logger;
        }

        public override Task HandleAsync(WeatherForecastGeneratedEvent @event)
        {
            _logger.LogInformation("Weather posted");
            _weatherRepo.WeatherForecasts = @event.WeatherForecasts;
            return Task.CompletedTask;
        }
    }
}
