using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Subscriber.Models;
using System.Threading.Tasks;
using EventDriven.EventBus.Abstractions;

namespace Subscriber.Events
{
    public class WeatherForecastEventHandler : IntegrationEventHandler<WeatherForecastEvent>
    {
        // Set true to generate errors for retries
        private const bool GenerateErrorsForRetries = false;

        private readonly WeatherForecastRepository _weatherRepo;
        private readonly ILogger<WeatherForecastEventHandler> _logger;
        private readonly List<string> _eventIds = new();

        public WeatherForecastEventHandler(WeatherForecastRepository weatherRepo, ILogger<WeatherForecastEventHandler> logger)
        {
            _weatherRepo = weatherRepo;
            _logger = logger;
        }

        public override Task HandleAsync(WeatherForecastEvent @event)
        {
            _logger.LogInformation("Weather posted");

            // Throw exception the first time we process this event
            if (GenerateErrorsForRetries && !_eventIds.Contains(@event.Id))
            {
                _eventIds.Add(@event.Id);
                throw new Exception("Weather processing exception. Retry pending.");
            }

            _weatherRepo.WeatherForecasts = @event.WeatherForecasts;
            return Task.CompletedTask;
        }
    }
}
