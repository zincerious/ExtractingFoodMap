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
        private readonly WeatherService _weatherService;

        public ChatController(HttpClient httpClient, IConfiguration configuration, FoodService foodService, WeatherService weatherService)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Gemini:ApiKey"]!;
            _foodService = foodService;
            _weatherService = weatherService;
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

        [HttpGet("current")]
        public async Task<IActionResult> GetWeather(double lat, double lon)
        {
            var weather = await _weatherService.GetWeatherAsync(lat, lon);
            if (weather == null)
            {
                return BadRequest("Failed to retrieve weather.");
            }
            return Ok(weather);
        }

        [HttpPost("weather-to-message")]
        public async Task<IActionResult> GetWeatherToMessageAsync([FromBody] WeatherResponse weatherResponse)
        {
            var dayWeather = weatherResponse.Days.FirstOrDefault();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}";
            var uniqueTags = FoodService.GetFoods()
                .SelectMany(f => f.Tags)
                .Distinct()
                .ToList();
            var foodJsons = JsonSerializer.Serialize(FoodService.GetFoods().Select(f => new { f.Name, Hashtags = f.Tags, Locations = f.Locations }).ToList());
            var tagJsons = JsonSerializer.Serialize(uniqueTags);
            var prompt = $@"
                        Bạn là một chatbot tên là 'Ăn Gì Đây', với phong cách thân thiện, vui vẻ, và luôn muốn giúp người dùng tìm được món ăn phù hợp với thời tiết.

                        Thời tiết hiện tại:
                            - Nhiệt độ cảm nhận: {dayWeather!.FeelsLike}°C
                            - Độ ẩm: {dayWeather!.Humidity}%
                            - Tốc độ gió: {dayWeather!.WindSpeed} km/h

                        Dưới đây là danh sách các món ăn có thể chọn:
                        {foodJsons}

                        Dựa trên thời tiết, bạn hãy chọn ra 3–5 món phù hợp nhất.

                        Sau đó, **viết một đoạn hội thoại ngắn gọn, tự nhiên và gần gũi** để gợi ý cho người dùng, ví dụ như:
                        - Chào bạn! Hôm nay trời hơi nóng một chút, mình gợi ý bạn nên thử món gì đó thanh mát nhé!

                        Tránh dùng các định dạng JSON, chỉ trả lại đoạn văn tự nhiên.
                        ";

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
            using var doc = JsonDocument.Parse(resultString);
            var text = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();
            return Ok (new { Message = text });
        }

        [HttpPost("weather-to-message-toJson")]
        public async Task<IActionResult> GetGeminiResponseByWeatherToJson([FromBody] WeatherResponse weatherResponse)
        {
            var dayWeather = weatherResponse.Days.FirstOrDefault();
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
                            Thời tiết hiện tại:
                                - Nhiệt độ cảm nhận: {dayWeather!.FeelsLike}°C
                                - Độ ẩm: {dayWeather!.Humidity}%
                                - Tốc độ gió: {dayWeather!.WindSpeed} km/h
                            Dựa vào kết quả của thời tiết , bạn hãy chọn 3–5 món ăn phù hợp, rồi trả kết quả theo định dạng:
                            ```json{{""Name"": ""Tên món"", ""Hashtags"": [""#tag1"", ""#tag2"", ""#tag3""], ""Locations"": [ {{ ""Name"": ""Tên địa điểm 1"", ""Latitude"": 10.123456, ""Longitude"": 106.123456, ""Rating"": 4.5 }}, {{ ""Name"": ""Tên địa điểm 2"", ""Latitude"": 10.123456, ""Longitude"": 106.123456, ""Rating"": 4.5 }}, ... ] }}```";

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
            using var doc = JsonDocument.Parse(resultString);
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
