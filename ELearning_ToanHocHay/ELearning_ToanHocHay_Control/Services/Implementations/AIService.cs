using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class AIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        public AIService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;

            _httpClient.BaseAddress =
                new Uri(_configuration["OpenAI:BaseUrl"]!);

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer",
                    _configuration["OpenAI:ApiKey"]);
        }
        public async Task<string> GenerateFeedbackAsync(string prompt)
        {
            return await SendPromptAsync(prompt, temperature: 0.2);
        }

        public async Task<string> GenerateHintAsync(string prompt)
        {
            return await SendPromptAsync(prompt, temperature: 0.3);
        }

        // ================== CORE METHOD ==================
        private async Task<string> SendPromptAsync(string prompt, double temperature)
        {
            var requestBody = new
            {
                model = _configuration["OpenAI:Model"],
                messages = new[]
                {
                new { role = "system", content = "Bạn là giáo viên Toán, giải thích dễ hiểu cho học sinh." },
                new { role = "user", content = prompt }
            },
                temperature = temperature,
                max_tokens = 300
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"OpenAI API Error: {error}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(responseJson);

            var result = document
                .RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return result?.Trim() ?? "AI không thể tạo gợi ý.";
        }
    }
}
