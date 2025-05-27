namespace FoodFinder.Models
{
    public class RecommendationItem
    {
        public string Recommedation { get; set; } = null!;
        public List<string> Tags { get; set; } = new();
    }
}
