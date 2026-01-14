using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface IChapterRepository
    {
        Task<IEnumerable<Chapter>> GetByCurriculumIdAsync(int curriculumId);
        Task<Chapter?> GetByIdAsync(int chapterId);
        Task<Chapter> CreateAsync(Chapter chapter);
        Task<Chapter?> UpdateAsync(Chapter chapter);
        Task<bool> DeleteAsync(int chapterId);
    }
}
