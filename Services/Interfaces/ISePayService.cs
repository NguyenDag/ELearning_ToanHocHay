namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface ISePayService
    {
        string GenerateQrUrl(int subscriptionId, decimal amount);
        bool ValidateApiKey(string? apiKey);

        /// <summary>
        /// Extract SubscriptionId từ content
        /// </summary>
        int? ExtractSubscriptionId(string? content);
        bool IsValidAmount(long transferAmount, decimal expectedAmount);
    }

}
