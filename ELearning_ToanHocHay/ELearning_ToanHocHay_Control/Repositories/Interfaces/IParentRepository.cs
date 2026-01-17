using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface IParentRepository
    {
        Task<Parent> AddAsync(Parent parent);
    }
}
