using System;
using System.Net.Http;
using System.Text.Json;

namespace EnetSenderNet
{
    public class WeatherService
    {
        private static readonly HttpClient _defaultClient = new HttpClient();

        private readonly HttpClient _httpClient;
        private readonly double _latitude;
        private readonly double _longitude;

        public WeatherService(double latitude, double longitude, HttpClient httpClient = null)
        {
            _latitude = latitude;
            _longitude = longitude;
            _httpClient = httpClient ?? _defaultClient;
        }

        public double? GetCurrentTemperature()
        {
            try
            {
                var url = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "https://api.open-meteo.com/v1/forecast?latitude={0}&longitude={1}&current=temperature_2m",
                    _latitude, _longitude);
                var json = _httpClient.GetStringAsync(url).GetAwaiter().GetResult();
                return JsonDocument.Parse(json).RootElement.GetProperty("current").GetProperty("temperature_2m").GetDouble();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Weather fetch failed: {ex.Message}");
                return null;
            }
        }

        public double? GetDailyHighTemperature()
        {
            try
            {
                var url = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "https://api.open-meteo.com/v1/forecast?latitude={0}&longitude={1}&daily=temperature_2m_max&forecast_days=1",
                    _latitude, _longitude);
                var json = _httpClient.GetStringAsync(url).GetAwaiter().GetResult();
                return JsonDocument.Parse(json).RootElement.GetProperty("daily").GetProperty("temperature_2m_max")[0].GetDouble();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Weather fetch failed: {ex.Message}");
                return null;
            }
        }

        public bool IsHot(double thresholdCelsius = 24)
        {
            var temp = GetDailyHighTemperature();
            if (temp.HasValue)
                Console.WriteLine($"Today's high: {temp}°C");
            return temp.HasValue && temp.Value > thresholdCelsius;
        }
    }
}
