using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface ILessonContentRepository
    {
        Task<IEnumerable<LessonContent>> GetByLessonAsync(int lessonId);
        Task<LessonContent?> GetByIdAsync(int contentId);
        Task<LessonContent> AddAsync(LessonContent content);
        Task<LessonContent?> UpdateAsync(LessonContent content);
        Task<bool> DeleteAsync(int contentId);
        Task<bool> ExistsAsync(int id);
        Task<IEnumerable<LessonContent>> AddRangeAsync(IEnumerable<LessonContent> entities);
    }
}
