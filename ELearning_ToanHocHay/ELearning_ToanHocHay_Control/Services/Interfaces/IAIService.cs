namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface IAIService
    {
        Task<string> GenerateHintAsync(string prompt);
        Task<string> GenerateFeedbackAsync(string prompt);
    }
}
