using Common;
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

            // Add Dapr service bus
            services.AddDaprEventBus(Constants.DaprPubSubName);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
            WeatherForecastRequestedEventHandler forecastRequestedEventHandler)
        {
            // Use Dapr service bus
            app.UseDaprEventBus(eventBus =>
            {
                // Subscribe with a handler
                eventBus.Subscribe(forecastRequestedEventHandler);
            });
        }
    }
}
