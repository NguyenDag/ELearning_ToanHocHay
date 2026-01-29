namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface IBackgroundEmailService
    {
        void QueueConfirmationEmail(string toEmail, string fullName, string confirmLink);
    }
}
