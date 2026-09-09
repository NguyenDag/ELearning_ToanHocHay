using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface IPackageRepository
    {
        Task<List<Package>> GetAllAsync();

        /// <summary>Mọi gói kể cả đã tắt (màn hình quản lý gói của Finance/Admin).</summary>
        Task<List<Package>> GetAllIncludingInactiveAsync();

        Task<Package?> GetByIdAsync(int id);
        Task AddAsync(Package package);
        Task UpdateAsync(Package package);
        Task DeleteAsync(Package package);
        Task<Subscription?> GetActivePackageAsync(int studentId);
        Task<PackageTier?> GetActivePackageTierAsync(int studentId);

        /// <summary>Đếm thuê bao Active theo từng gói: { PackageId → số lượng }.</summary>
        Task<Dictionary<int, int>> GetActiveSubscriberCountsAsync();

        /// <summary>Có ít nhất một thuê bao (bất kỳ trạng thái) gắn với gói này?</summary>
        Task<bool> HasAnySubscriptionAsync(int packageId);
    }
}
