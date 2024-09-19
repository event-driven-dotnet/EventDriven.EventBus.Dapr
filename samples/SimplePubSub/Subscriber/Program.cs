using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Subscriber.Events;
using Subscriber.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add weather repository
builder.Services.AddSingleton<WeatherForecastRepository>();

// Add handlers
builder.Services.AddSingleton<WeatherForecastEventHandler>();

// Add Dapr event bus
builder.Services.AddDaprEventBus(builder.Configuration);
            
// Add Redis event cache
// builder.Services.AddRedisEventCache(builder.Configuration);
builder.Services.AddRedisEventCache(options => options.AppName = "subscriber",
    options =>
    {
        options.ConnectionString = "localhost:6379";
        options.DistributedCacheEntryOptions.SlidingExpiration = TimeSpan.FromMinutes(5);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
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
        var forecastEventHandler = app.Services.GetRequiredService<WeatherForecastEventHandler>();
        eventBus.Subscribe(forecastEventHandler, null, "v1");
    });
});
#pragma warning restore ASP0014

app.Run();