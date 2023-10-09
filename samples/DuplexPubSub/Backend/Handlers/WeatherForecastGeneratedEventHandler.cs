using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Backend.Repositories;
using Common.Events;
using EventDriven.EventBus.Abstractions;

namespace Backend.Handlers
{
    public class WeatherForecastGeneratedEventHandler : IntegrationEventHandler<WeatherForecastGeneratedEvent>
    {
        // Set true to generate errors for retries
        private const bool GenerateErrorsForRetries = false;

        private readonly WeatherForecastRepository _weatherRepo;
        private readonly ILogger<WeatherForecastGeneratedEventHandler> _logger;
        private readonly List<string> _eventIds = new();

        public WeatherForecastGeneratedEventHandler(WeatherForecastRepository weatherRepo, ILogger<WeatherForecastGeneratedEventHandler> logger)
        {
            _weatherRepo = weatherRepo;
            _logger = logger;
        }

        public override Task HandleAsync(WeatherForecastGeneratedEvent @event)
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
