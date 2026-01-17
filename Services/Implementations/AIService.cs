using System.Text;
using System.Text.Json;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using System.Text.Json.Serialization;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class AIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIService> _logger;

        public AIService(HttpClient httpClient, IConfiguration configuration, ILogger<AIService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            // Configure HttpClient to call Flask AI server
            var baseUrl = configuration["AI:PythonServerUrl"] ?? "http://localhost:5000";
            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        // ==================== HINT GENERATION ====================
        public async Task<string> GenerateHintAsync(string prompt)
        {
            return await SendPromptAsync(prompt, temperature: 0.3);
        }

        // ==================== FEEDBACK GENERATION ====================
        public async Task<string> GenerateFeedbackAsync(string prompt)
        {
            return await SendPromptAsync(prompt, temperature: 0.2);
        }

        // ==================== NEW METHODS - STRUCTURED RESPONSES ====================

        /// <summary>
        /// Generate AI hint with structured response
        /// </summary>
        public async Task<HintResponse?> GenerateHintStructuredAsync(HintRequest request)
        {
            try
            {
                _logger.LogInformation($"Generating hint for question {request.QuestionId}, level {request.HintLevel}");

                var jsonContent = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/hint", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"AI Hint API Error: {response.StatusCode} - {errorContent}");
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var hintResponse = JsonSerializer.Deserialize<HintResponse>(responseContent, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation($"Hint generated successfully: {hintResponse?.Status}");
                return hintResponse;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP Error calling AI Hint API: {ex.Message}");
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError($"JSON Deserialization Error: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating hint: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generate AI feedback with structured response
        /// </summary>
        public async Task<FeedbackResponse?> GenerateFeedbackStructuredAsync(FeedbackRequest request)
        {
            try
            {
                _logger.LogInformation($"Generating feedback for attempt {request.AttemptId}");

                var jsonContent = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/feedback", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"AI Feedback API Error: {response.StatusCode} - {errorContent}");
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var feedbackResponse = JsonSerializer.Deserialize<FeedbackResponse>(responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation($"Feedback generated successfully: {feedbackResponse?.Status}");
                return feedbackResponse;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"HTTP Error calling AI Feedback API: {ex.Message}");
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError($"JSON Deserialization Error: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating feedback: {ex.Message}");
                return null;
            }
        }

        // ==================== CORE METHOD (Legacy Support) ==================
        private async Task<string> SendPromptAsync(string prompt, double temperature)
        {
            try
            {
                // For backward compatibility - returns basic string response
                // In production, use GenerateHintStructuredAsync or GenerateFeedbackStructuredAsync

                var hintRequest = new HintRequest
                {
                    QuestionText = prompt,
                    QuestionType = "Essay",
                    DifficultyLevel = "Medium",
                    StudentAnswer = "Chưa trả lời",
                    HintLevel = 1
                };

                var result = await GenerateHintStructuredAsync(hintRequest);
                return result?.HintText ?? "AI không thể tạo gợi ý.";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SendPromptAsync: {ex.Message}");
                return "AI không thể tạo gợi ý.";
            }
        }
    }

    // ==================== REQUEST DTOs ====================
    public class HintRequest
    {
        [JsonPropertyName("question_text")]
        public string QuestionText { get; set; }

        [JsonPropertyName("question_type")]
        public string QuestionType { get; set; }

        [JsonPropertyName("difficulty_level")]
        public string DifficultyLevel { get; set; }

        [JsonPropertyName("student_answer")]
        public string StudentAnswer { get; set; }

        [JsonPropertyName("hint_level")]
        public int HintLevel { get; set; } = 1;

        [JsonPropertyName("question_id")]
        public int? QuestionId { get; set; }

        [JsonPropertyName("question_image_url")]
        public string? QuestionImageUrl { get; set; }

        [JsonPropertyName("options")]
        public List<OptionDto>? Options { get; set; }
    }

    public class FeedbackRequest
    {
        [JsonPropertyName("question_text")]
        public string QuestionText { get; set; }

        [JsonPropertyName("question_type")]
        public string QuestionType { get; set; }

        [JsonPropertyName("student_answer")]
        public string StudentAnswer { get; set; }

        [JsonPropertyName("correct_answer")]
        public string CorrectAnswer { get; set; }

        [JsonPropertyName("is_correct")]
        public bool IsCorrect { get; set; }

        [JsonPropertyName("explanation")]
        public string? Explanation { get; set; }

        [JsonPropertyName("attempt_id")]
        public int? AttemptId { get; set; }

        [JsonPropertyName("question_id")]
        public int? QuestionId { get; set; }

        [JsonPropertyName("question_image_url")]
        public string? QuestionImageUrl { get; set; }

        [JsonPropertyName("options")]
        public List<OptionDto>? Options { get; set; }
    }

    public class OptionDto
    {
        [JsonPropertyName("option_id")]
        public int OptionId { get; set; }

        [JsonPropertyName("option_text")]
        public string OptionText { get; set; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("is_correct")]
        public bool IsCorrect { get; set; }
    }

    // ==================== RESPONSE DTOs ====================
    public class HintResponse
    {
        [JsonPropertyName("hint_text")]
        public string HintText { get; set; }

        [JsonPropertyName("hint_level")]
        public int HintLevel { get; set; }

        [JsonPropertyName("question_id")]
        public int? QuestionId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class FeedbackResponse
    {
        [JsonPropertyName("full_solution")]
        public string FullSolution { get; set; }

        [JsonPropertyName("mistake_analysis")]
        public string MistakeAnalysis { get; set; }

        [JsonPropertyName("improvement_advice")]
        public string ImprovementAdvice { get; set; }

        [JsonPropertyName("attempt_id")]
        public int? AttemptId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
