namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendConfirmEmailAsync(
        string toEmail,
        string fullName,
        string confirmLink
        );

        Task SendPasswordResetEmailAsync(
        string toEmail,
        string fullName,
        string resetLink
        );

        Task SendTabSwitchNotificationAsync(
            string toEmail,
            string parentName,
            string studentName,
            string exerciseName,
            DateTime switchedAt,
            int switchCount
        );
    }
}
