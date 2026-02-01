namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface ISePayService
    {
        string GenerateQrUrl(int subscriptionId, decimal amount);
        bool ValidateApiKey(string? apiKey);
        int? ExtractSubscriptionId(string? content);
        bool IsValidAmount(long transferAmount, decimal expectedAmount);
    }

}
