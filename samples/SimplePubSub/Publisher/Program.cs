using EventDriven.EventBus.Dapr;
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
                    var eventBusSchemaOptions = new DaprEventBusSchemaOptions();
                    hostContext.Configuration.GetSection(nameof(DaprEventBusSchemaOptions)).Bind(eventBusSchemaOptions);

                    // Add Dapr service bus and enable schema registry with schemas added on publish.
                    services.AddDaprEventBus(eventBusOptions.PubSubName, options =>
                    {
                        options.UseSchemaRegistry = eventBusSchemaOptions.UseSchemaRegistry;
                        options.SchemaRegistryType = eventBusSchemaOptions.SchemaRegistryType;
                        options.MongoStateStoreOptions = eventBusSchemaOptions.MongoStateStoreOptions;
                        options.SchemaValidatorType = eventBusSchemaOptions.SchemaValidatorType;
                        options.AddSchemaOnPublish = eventBusSchemaOptions.AddSchemaOnPublish;
                    });
                });
    }
}
