namespace FoodFinder.Models
{
    public class WeatherResponse
    {
        public string? Address { get; set; }
        public List<DayWeather> Days { get; set; } = new();
    }
}
