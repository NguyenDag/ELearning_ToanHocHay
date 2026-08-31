using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface IParentLinkRepository
    {
        Task<ParentLink?> GetByIdAsync(int parentLinkId);
        Task<ParentLink?> GetAsync(int parentId, int studentId);
        Task<List<ParentLink>> GetByParentAsync(int parentId, bool activeOnly = false);
        Task<List<ParentLink>> GetByStudentAsync(int studentId, bool activeOnly = false);
        Task<bool> ExistsActiveAsync(int studentId, int parentId);
        Task<ParentLink> AddAsync(ParentLink link);
        Task UpdateAsync(ParentLink link);
    }
}
