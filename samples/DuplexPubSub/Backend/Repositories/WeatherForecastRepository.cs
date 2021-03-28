using System;
using System.Collections.Generic;
using Common.Models;

namespace Backend.Repositories
{
    public class WeatherForecastRepository
    {
        IEnumerable<WeatherForecast> _weatherForecasts = new List<WeatherForecast>();

        public event EventHandler WeatherChangedEvent;

        public IEnumerable<WeatherForecast> WeatherForecasts
        {
            get => _weatherForecasts;
            set
            { 
                _weatherForecasts = value;
                WeatherChangedEvent?.Invoke(this, new EventArgs());
            }
        }
    }
}
