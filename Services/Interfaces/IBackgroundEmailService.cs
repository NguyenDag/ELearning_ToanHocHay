namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface IBackgroundEmailService
    {
        void QueueConfirmationEmail(string toEmail, string fullName, string confirmLink);
        void QueuePasswordResetEmail(string toEmail, string fullName, string resetLink);
        void QueueTabSwitchEmail(
            string toEmail, string parentName, string studentName, string exerciseName,
            System.DateTime switchedAt, int switchCount);
    }
}
