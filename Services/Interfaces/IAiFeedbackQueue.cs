namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    /// <summary>
    /// Queues AI feedback generation so that submitting an exercise returns immediately
    /// instead of waiting for the (slow) AI service.
    /// </summary>
    public interface IAiFeedbackQueue
    {
        void Enqueue(int attemptId, int questionId, string? studentAnswer);
    }
}
