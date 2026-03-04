using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.Student.Dashboard;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface IPackageRepository
    {
        Task<List<Package>> GetAllAsync();
        Task<Package?> GetByIdAsync(int id);
        Task AddAsync(Package package);
        Task UpdateAsync(Package package);
        Task DeleteAsync(Package package);
        Task<Subscription?> GetActivePackageAsync(int studentId);
        Task<PackageType?> GetActivePackageTypeAsync(int studentId);
    }
}
