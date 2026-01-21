namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendConfirmEmailAsync(
        string toEmail,
        string fullName,
        string confirmLink
        );
    }
}
