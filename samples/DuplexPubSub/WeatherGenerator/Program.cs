using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WeatherGenerator.Factories;
using WeatherGenerator.Handlers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add weather repository
builder.Services.AddSingleton<WeatherFactory>();

// Add handlers
builder.Services.AddSingleton<WeatherForecastRequestedEventHandler>();

// Add Dapr event bus
builder.Services.AddDaprEventBus(builder.Configuration);
            
// Add Redis event cache
builder.Services.AddRedisEventCache(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
app.UseRouting();
app.UseCloudEvents();

#pragma warning disable ASP0014 // Need to use endpoints to map event bus
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapSubscribeHandler();
    endpoints.MapDaprEventBus(eventBus =>
    {
        // Subscribe with a handler
        var forecastRequestedEventHandler = app.Services.GetRequiredService<WeatherForecastRequestedEventHandler>();
        eventBus.Subscribe(forecastRequestedEventHandler, null, "v1");
    });
});
#pragma warning restore ASP0014

app.Run();