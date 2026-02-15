using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface ITopicRepository
    {
        Task<IEnumerable<Topic>> GetByChapterIdAsync(int chapterId);
        Task<Topic?> GetByIdAsync(int topicId);
        Task<Topic> CreateAsync(Topic topic);
        Task<Topic?> UpdateAsync(Topic topic);
        Task<bool> DeleteAsync(int topicId);
        Task<bool> ExistsAsync(int id);
    }
}
