using EventDriven.EventBus.Dapr;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Subscriber.Events;
using Subscriber.Models;

namespace Subscriber
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
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Subscriber", Version = "v1" });
            });

            // Add weather repository
            services.AddSingleton<WeatherForecastRepository>();

            // Add handlers
            services.AddSingleton<WeatherForecastEventHandler>();

            // Configuration
            var eventBusOptions = new DaprEventBusOptions();
            Configuration.GetSection(nameof(DaprEventBusOptions)).Bind(eventBusOptions);

            // Add Dapr event bus
            services.AddDaprEventBus(eventBusOptions.PubSubName);
            
            // Add Mongo event cache
            services.AddMongoEventCache(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger,
            WeatherForecastEventHandler forecastEventHandler)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Subscriber v1"));
            }

            app.UseRouting();
            app.UseCloudEvents();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapSubscribeHandler();
                endpoints.MapDaprEventBus(eventBus =>
                {
                    // Subscribe with a handler
                    eventBus.Subscribe(forecastEventHandler, null, "v1");
                });
            });
        }
    }
}
