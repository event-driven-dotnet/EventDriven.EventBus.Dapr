using EventDriven.EventBus.Dapr;
using EventDriven.SchemaRegistry.Dapr;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Publisher.Models;

namespace Publisher
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                    services.AddSingleton<WeatherFactory>();

                    // Configuration
                    var eventBusOptions = new DaprEventBusOptions();
                    hostContext.Configuration.GetSection(nameof(DaprEventBusOptions)).Bind(eventBusOptions);
                    var stateStoreOptions = new DaprStateStoreOptions();
                    hostContext.Configuration.GetSection(nameof(DaprStateStoreOptions)).Bind(stateStoreOptions);

                    // Add Dapr service bus and enable schema registry with schemas added on publish.
                    services.AddDaprEventBus(eventBusOptions.PubSubName, options =>
                    {
                        options.UseSchemaRegistry = true;
                        options.SchemaRegistryStateStoreName = stateStoreOptions.StateStoreName;
                        options.SchemaValidatorType = SchemaValidatorType.Json;
                        options.AddSchemaOnPublish = true;
                    });
                });
    }
}
