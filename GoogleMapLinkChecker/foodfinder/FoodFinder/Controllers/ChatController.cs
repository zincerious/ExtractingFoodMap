using FoodFinder.Models;
using FoodFinder.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace FoodFinder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly FoodService _foodService;

        public ChatController(HttpClient httpClient, IConfiguration configuration, FoodService foodService)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Gemini:ApiKey"]!;
            _foodService = foodService;
        }

        [HttpPost("message")]
        public async Task<IActionResult> GenerateRecommendationAsync(UserRequest userRequest)
        {
            return Content(await GetGeminiResponseAsync(userRequest), "application/json");
        }

        [HttpPost("messageToJson")]
        public async Task<IActionResult> GenerateRecommendationToFoodsAsync(UserRequest userRequest)
        {
            using var doc = JsonDocument.Parse(await GetGeminiResponseAsync(userRequest));
            var text = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

            var foodList = _foodService.ExtractFoodsFromGeminiResponse(text ?? "");
            return Ok(new { FoodList = foodList });
        }

        private async Task<string> GetGeminiResponseAsync(UserRequest userRequest)
        {
            userRequest.Scenario = _foodService.PreprocessUserScenario(userRequest.Scenario);
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}";
            var uniqueTags = FoodService.GetFoods()
                .SelectMany(f => f.Tags)
                .Distinct()
                .ToList();
            var foodJsons = JsonSerializer.Serialize(FoodService.GetFoods().Select(f => new { f.Name, Hashtags = f.Tags, Locations = f.Locations }).ToList());
            var tagJsons = JsonSerializer.Serialize(uniqueTags);
            var prompt = $@"
                            Bạn là chatbot tên là 'Ăn Gì Đây' giúp người dùng tìm món ăn.
                            Đây là danh sách món ăn tôi có {foodJsons}
                            Khi người dùng hỏi: {userRequest.Scenario}, bạn hãy chọn 3–5 món ăn phù hợp, rồi trả kết quả theo định dạng:
                            ```json{{""Name"": ""Tên món"", ""Hashtags"": [""#tag1"", ""#tag2"", ""#tag3""], ""Locations"": [ {{ ""Name"": ""Tên địa điểm 1"", ""Latitude"": 10.123456, ""Longitude"": 106.123456, ""Rating"": 4.5 }}, {{ ""Name"": ""Tên địa điểm 2"", ""Latitude"": 10.123456, ""Longitude"": 106.123456, ""Rating"": 4.5 }}, ... ] }}```
                            Trong trường hợp người dùng hỏi đúng 1 món ăn, bạn chỉ cần trả về 1 món ăn duy nhất kèm theo location.
                            Hãy tuân thủ nguyên tắc sau: Khi người dùng hỏi về món ăn, 
                            hãy trả lời theo đúng format trên và không có thông tin gì khác,
                            còn nếu người dùng hỏi về chuyện khác thì hãy trả lời
                            theo yêu cầu của người dùng một cách thân thiện, gần gữi nhưng vẫn lịch sự.";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new []
                        {
                            new
                            {
                                text = prompt
                            }
                        },
                    }
                }
            };

            var httpContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, httpContent);

            var resultString = await response.Content.ReadAsStringAsync();
            return resultString;
        }
    }
}
