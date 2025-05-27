namespace FoodFinder.Models
{
    public class RecommendationRequest
    {
        public List<RecommendationItem> Items { get; set; } = new();
    }
}
