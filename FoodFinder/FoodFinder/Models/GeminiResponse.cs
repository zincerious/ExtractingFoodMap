namespace FoodFinder.Models
{
    public class GeminiResponse
    {
        public List<RecommendationItem> Recommendations { get; set; } = new();
        public string? RawResponse { get; set; }
    }
}
