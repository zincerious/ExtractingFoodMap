using FoodFinder.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FoodFinder.Services
{
    public class FoodService
    {
        public static List<Food> GetFoods()
        {
            return new()
            {
                new Food { Name = "Phở Bò", Tags = new List<string> { "#MonVietNam", "#MonNuoc", "#MonNong" } },
                new Food { Name = "Bánh Mì Thịt", Tags = new List<string> { "#MonVietNam", "#MonKho", "#MonSang" } },
                new Food { Name = "Bún Chả", Tags = new List<string> { "#MonVietNam", "#MonNuoc", "#MonTrua" } },
                new Food { Name = "Mì Quảng", Tags = new List<string> { "#MonVietNam", "#MonNuoc", "#MonMienTrung" } },
                new Food { Name = "Cơm Tấm", Tags = new List<string> { "#MonVietNam", "#MonKho", "#MonTrua" }, Locations = new List<Location>
                {
                    new Location
                    {
                        Name = "Cơm Tấm Ba Ghiền",
                        Latitude = 10.790152,
                        Longitude = 106.678024,
                        Rating = 4.7f
                    },
                    new Location
                    {
                        Name = "Cơm Tấm Cali",
                        Latitude = 10.772729,
                        Longitude = 106.698040,
                        Rating = 4.3f
                    },
                    new Location
                    {
                        Name = "Cơm Tấm Kiều Giang",
                        Latitude = 10.776887,
                        Longitude = 106.690109,
                        Rating = 4.4f
                    }
                } },
                new Food { Name = "Hủ Tiếu Nam Vang", Tags = new List<string> { "#MonVietNam", "#MonNuoc", "#MonSang" } },
                new Food { Name = "Gỏi Cuốn", Tags = new List<string> { "#MonVietNam", "#MonLanh", "#MonNhe" } },
                new Food { Name = "Bánh Xèo", Tags = new List<string> { "#MonVietNam", "#MonChien", "#MonMienNam" } },
                new Food { Name = "Bún Bò Huế", Tags = new List<string> { "#MonVietNam", "#MonNuoc", "#MonCay" } },
                new Food { Name = "Cao Lầu", Tags = new List<string> { "#MonVietNam", "#MonKho", "#MonMienTrung" } },
                new Food { Name = "Lẩu Thái", Tags = new List<string> { "#MonThai", "#MonNuoc", "#MonCay" } },
                new Food { Name = "Mì Cay 7 Cấp Độ", Tags = new List<string> { "#MonHan", "#MonNuoc", "#MonCay" } },
                new Food { Name = "Kimbap", Tags = new List<string> { "#MonHan", "#MonLanh", "#MonNhe" } },
                new Food { Name = "Sushi", Tags = new List<string> { "#MonNhat", "#MonLanh", "#MonNhe" } },
                new Food { Name = "Cơm Gà Hải Nam", Tags = new List<string> { "#MonTrung", "#MonKho", "#MonTrua" } },
                new Food { Name = "Tokbokki", Tags = new List<string> { "#MonHan", "#MonNuoc", "#MonCay" } },
                new Food { Name = "Gà Rán", Tags = new List<string> { "#MonTay", "#MonChien", "#MonNong" } },
                new Food { Name = "Hamburger", Tags = new List<string> { "#MonTay", "#MonKho", "#MonNhanh" } },
                new Food { Name = "Pizza", Tags = new List<string> { "#MonY", "#MonNong", "#MonChiaNhom" } },
                new Food { Name = "Salad Trái Cây", Tags = new List<string> { "#MonTay", "#MonLanh", "#MonAnKieng" } }
            };
        }

        public List<Food> GetFoodsByTag(List<string> tags)
        {
            //return GetFoods().Where(f => tags.Count(t => f.Tags.Contains(t)) == tags.Count).ToList();
            return GetFoods().Select(f => new { Food = f, MatchCount = f.Tags.Intersect(tags).Count() })
                .Where(x => x.MatchCount > 0)
                .OrderByDescending(x => x.MatchCount)
                .Select(x => x.Food)
                .Select(f => new Food
                {
                    Name = f.Name,
                    Tags = f.Tags,
                    Locations = f.Locations.OrderByDescending(l => l.Rating).ToList()
                })
                .ToList();
        }

        public List<Food>? ExtractFoodsFromGeminiResponse(string geminiText)
        {
            var foods = new List<Food>();

            if (string.IsNullOrWhiteSpace(geminiText))
            {
                return foods;
            }

            if (geminiText.TrimStart().StartsWith("```json\n"))
            {
                var jsonOnly = geminiText.Replace("```json\n", "").Replace("```\n", "").Trim();

                try
                {
                    //if (jsonOnly.TrimStart().StartsWith("["))
                    //{
                    //    foods = JsonSerializer.Deserialize<List<Food>>(jsonOnly);
                    //}
                    //else
                    //{
                    //    var food = JsonSerializer.Deserialize<Food>(jsonOnly);
                    //    if (food != null) foods.Add(food);                   
                    //}
                    if (jsonOnly.TrimStart().StartsWith("["))
                    {
                        var parsedFoods = JsonSerializer.Deserialize<List<Food>>(jsonOnly.TrimStart(),
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (parsedFoods != null)
                            foods.AddRange(FilterValidFoods(parsedFoods));

                        return foods;
                    }
                    else
                    {
                        var food = JsonSerializer.Deserialize<Food>(jsonOnly.TrimStart(),
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (IsValidFood(food))
                            foods.Add(food!);

                        return foods;
                    }
                }
                catch { }
            }

            var cleanedText = geminiText.Trim();
            if (cleanedText.StartsWith("```json"))
            {
                cleanedText = cleanedText
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();
            }
                var matches = Regex.Matches(cleanedText, @"\{(?:[^{}]|(?<open>\{)|(?<-open>\}))*\}(?(open)(?!))");
            foreach (Match match in matches)
            {
                try
                {
                    var food = JsonSerializer.Deserialize<Food>(match.Value, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (food != null) foods?.Add(food);
                }
                catch { }
            }

            return foods;
        }

        private bool IsValidFood(Food? food)
        {
            return food != null &&
                   !string.IsNullOrWhiteSpace(food.Name) &&
                   (
                       (food.Tags != null && food.Tags.Any()) ||
                       (food.Locations != null && food.Locations.Any())
                   );
        }

        private IEnumerable<Food> FilterValidFoods(IEnumerable<Food> foods)
        {
            return foods.Where(f => IsValidFood(f));
        }

        public string PreprocessUserScenario(string scenario)
        {
            // Nếu câu quá ngắn, có thể là tên món => thêm ngữ cảnh rõ hơn
            if (scenario.Length <= 20 && !scenario.Contains("ăn") && !scenario.Contains("?"))
            {
                return $"Tôi muốn ăn {scenario}";
            }

            return scenario;
        }
    }
}
