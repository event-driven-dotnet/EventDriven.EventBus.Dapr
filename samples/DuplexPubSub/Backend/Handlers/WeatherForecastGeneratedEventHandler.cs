﻿using Microsoft.Extensions.Logging;
using EventBus.Abstractions;
using System.Threading.Tasks;
using Common.Events;
using WeatherGenerator.Repositories;

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
            _logger.LogInformation($"Weather posted.");
            _weatherRepo.WeatherForecasts = @event.WeatherForecasts;
            return Task.CompletedTask;
        }
    }
}