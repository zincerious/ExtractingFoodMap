using FoodFinder.Models;
using System.Text.Json;

namespace FoodFinder.Services
{
    public class WeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        public WeatherService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["WeatherSettings:VisualCrossingApiKey"]!;
        }
        public async Task<WeatherResponse?> GetWeatherAsync(double lat, double lon)
        {
            var baseUrl = "https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline/";
            var location = $"{lat},{lon}";
            var date = "today";

            var url = $"{baseUrl}{location}/{date}" +
                      "?unitGroup=metric" +
                      "&include=days" +
                      "&elements=datetime,temp,feelslike,conditions,humidity,windspeed" +
                      $"&key={_apiKey}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<WeatherResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }
}
