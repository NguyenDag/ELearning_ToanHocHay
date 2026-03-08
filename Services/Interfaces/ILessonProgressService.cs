namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface ILessonProgressService
    {
        Task UpdateLessonProgress(int studentId, int lessonId, int watchTime);
    }
}
