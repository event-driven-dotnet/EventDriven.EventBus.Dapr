using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Common.Events;
using EventDriven.EventBus.Abstractions;
using WeatherGenerator.Factories;

namespace WeatherGenerator.Handlers
{
    public class WeatherForecastRequestedEventHandler : IntegrationEventHandler<WeatherForecastRequestedEvent>
    {
        private readonly IEventBus _eventBus;
        private readonly WeatherFactory _factory;
        private readonly ILogger<WeatherForecastRequestedEventHandler> _logger;

        public WeatherForecastRequestedEventHandler(IEventBus eventBus, WeatherFactory factory,
            ILogger<WeatherForecastRequestedEventHandler> logger)
        {
            _eventBus = eventBus;
            _factory = factory;
            _logger = logger;
        }

        public async override Task HandleAsync(WeatherForecastRequestedEvent @event)
        {
            _logger.LogInformation("Weather forecast requested");

            // Create weather forecasts after a 5 second delay (to simulate latency)
            int delaySeconds = 5;
            var weathers = await _factory.CreateWeather(delaySeconds);

            // Publish event
            await _eventBus.PublishAsync(new WeatherForecastGeneratedEvent(weathers), null, "v1");
        }
    }
}
