using System;
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

            // Add Dapr event bus
            services.AddDaprEventBus(Configuration);
            
            // Add Redis event cache
            services.AddRedisEventCache(Configuration);
            // services.AddRedisEventCache(options => options.AppName = "subscriber",
            //     options =>
            //     {
            //         options.ConnectionString = "localhost:6379";
            //         options.DistributedCacheEntryOptions.SlidingExpiration = TimeSpan.FromMinutes(5);
            //     });
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
