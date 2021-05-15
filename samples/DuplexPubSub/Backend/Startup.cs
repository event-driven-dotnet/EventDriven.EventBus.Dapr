using Backend.Handlers;
using Backend.Repositories;
using EventDriven.EventBus.Dapr;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace Backend
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

            // Configuration
            var eventBusOptions = new DaprEventBusOptions();
            Configuration.GetSection(nameof(DaprEventBusOptions)).Bind(eventBusOptions);
            var eventBusSchemaOptions = new DaprEventBusSchemaOptions();
            Configuration.GetSection(nameof(DaprEventBusSchemaOptions)).Bind(eventBusSchemaOptions);

            // Add Dapr service bus
            services.AddDaprEventBus(eventBusOptions.PubSubName, options =>
            {
                options.UseSchemaRegistry = eventBusSchemaOptions.UseSchemaRegistry;
                options.SchemaRegistryType = eventBusSchemaOptions.SchemaRegistryType;
                options.MongoStateStoreOptions = eventBusSchemaOptions.MongoStateStoreOptions;
                options.SchemaValidatorType = eventBusSchemaOptions.SchemaValidatorType;
                options.AddSchemaOnPublish = eventBusSchemaOptions.AddSchemaOnPublish;
            });
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
                endpoints.MapSubscribeHandler();
                endpoints.MapDaprEventBus(eventBus =>
                {
                    // Subscribe with a handler
                    eventBus.Subscribe(forecastGeneratedEventHandler, null, "v1");
                });
            });
        }
    }
}
