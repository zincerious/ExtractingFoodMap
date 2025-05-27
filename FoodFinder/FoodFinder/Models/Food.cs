using System.Text.Json.Serialization;

namespace FoodFinder.Models
{
    public class Food
    {
        public string Name { get; set; } = null!;
        [JsonPropertyName("Hashtags")]
        public List<string> Tags { get; set; } = new();
        public List<Location> Locations { get; set; } = new();
    }
}
