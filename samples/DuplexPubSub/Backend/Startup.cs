using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Backend.Handlers;
using Common;
using WeatherGenerator.Repositories;

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

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Backend", Version = "v1" });
            });

            // Add repositories
            services.AddSingleton<WeatherForecastRepository>();

            // Add handlers
            services.AddSingleton<WeatherForecastGeneratedEventHandler>();

            // Add Dapr service bus
            services.AddDaprEventBus(Constants.DaprPubSubName);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
            WeatherForecastGeneratedEventHandler forecastGeneratedEventHandler)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Backend v1"));
            }

            app.UseRouting();
            app.UseCloudEvents();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                // Map Dapr service bus
                endpoints.MapDaprEventBus(eventBus =>
                {
                    // Subscribe with a handler
                    eventBus.Subscribe(forecastGeneratedEventHandler);
                });
            });
        }
    }
}
