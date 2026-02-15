using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface ILessonRepository
    {
        Task<IEnumerable<Lesson>> GetByTopicIdAsync(int topicId);
        Task<Lesson?> GetByIdAsync(int lessonId);
        Task<Lesson> CreateAsync(Lesson lesson);
        Task<Lesson?> UpdateAsync(Lesson lesson);
        Task<bool> DeleteAsync(int lessonId);
        Task<bool> ExistsAsync(int id);
        Task<Lesson> GetByIdWithContentsAsync(int id);
    }
}
