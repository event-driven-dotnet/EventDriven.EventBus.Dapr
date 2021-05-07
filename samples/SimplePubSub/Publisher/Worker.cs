using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Publisher.Events;
using Publisher.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using EventDriven.EventBus.Abstractions;

namespace Publisher
{
    public class Worker : BackgroundService
    {
        private readonly WeatherFactory _factory;
        private readonly IEventBus _eventBus;
        private readonly ILogger<Worker> _logger;

        public Worker(WeatherFactory factory, IEventBus eventBus, ILogger<Worker> logger)
        {
            _factory = factory;
            _eventBus = eventBus;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Publishing event at: {Time}", DateTimeOffset.Now);

                // Create weather forecasts
                var weathers = _factory.CreateWeather();

                // Publish event
                await _eventBus.PublishAsync(new WeatherForecastEvent(weathers), null, "v1");

                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
