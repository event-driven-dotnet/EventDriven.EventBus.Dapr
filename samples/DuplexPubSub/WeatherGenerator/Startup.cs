using Common;
using EventDriven.EventBus.Dapr;
using EventDriven.SchemaRegistry.Dapr;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeatherGenerator.Factories;
using WeatherGenerator.Handlers;

namespace WeatherGenerator
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add factories
            services.AddSingleton<WeatherFactory>();

            // Add handlers
            services.AddSingleton<WeatherForecastRequestedEventHandler>();

            // Configuration
            var eventBusOptions = new DaprEventBusOptions();
            Configuration.GetSection(nameof(DaprEventBusOptions)).Bind(eventBusOptions);
            var stateStoreOptions = new DaprStateStoreOptions();
            Configuration.GetSection(nameof(DaprStateStoreOptions)).Bind(stateStoreOptions);

            // Add Dapr service bus
            services.AddDaprEventBus(eventBusOptions.PubSubName, options =>
            {
                options.UseSchemaRegistry = true;
                options.SchemaRegistryStateStoreName = stateStoreOptions.StateStoreName;
                options.SchemaValidatorType = SchemaValidatorType.Json;
                options.AddSchemaOnPublish = true;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
            WeatherForecastRequestedEventHandler forecastRequestedEventHandler)
        {
            app.UseRouting();
            app.UseCloudEvents();
            app.UseEndpoints(endpoints =>
            {
                // Map SubscribeHandler and DaprEventBus
                endpoints.MapSubscribeHandler();
                endpoints.MapDaprEventBus(eventBus =>
                {
                    // Subscribe with a handler
                    eventBus.Subscribe(forecastRequestedEventHandler, null, "v1");
                });
            });
        }
    }
}
