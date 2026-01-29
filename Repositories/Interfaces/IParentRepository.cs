using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface IParentRepository
    {
        Task<Parent?> GetByIdAsync(int parentId);
        Task<Parent?> GetByUserIdAsync(int userId);
        Task<Parent> AddAsync(Parent parent);
    }
}
