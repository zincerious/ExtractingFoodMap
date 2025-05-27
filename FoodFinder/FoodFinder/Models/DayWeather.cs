using System.Text.Json.Serialization;

namespace FoodFinder.Models
{
    public class DayWeather
    {
        [JsonPropertyName("dateTime")]
        public DateTime DateTime { get; set; }
        [JsonPropertyName("temp")]
        public double Temp { get; set; }
        [JsonPropertyName("feelslike")]
        public double FeelsLike { get; set; }
        [JsonPropertyName("conditions")]
        public string? Conditions { get; set; }
        [JsonPropertyName("humidity")]
        public double Humidity { get; set; }
        [JsonPropertyName("windspeed")]
        public double WindSpeed { get; set; }
    }
}
