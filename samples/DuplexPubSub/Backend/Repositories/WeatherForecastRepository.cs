using Common.Models;
using System;
using System.Collections.Generic;

namespace WeatherGenerator.Repositories
{
    public class WeatherForecastRepository
    {
        IEnumerable<WeatherForecast> _weatherForecasts = new List<WeatherForecast>();

        public event EventHandler WeatherChangedEvent;

        public IEnumerable<WeatherForecast> WeatherForecasts
        {
            get { return _weatherForecasts; }
            set
            { 
                _weatherForecasts = value;
                WeatherChangedEvent?.Invoke(this, new EventArgs());
            }
        }
    }
}
