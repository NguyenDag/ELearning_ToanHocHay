using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    /// <summary>
    /// P1 — phân giải tier gói hiện hành của một học sinh (subscription Active còn hạn, tier cao nhất).
    /// Tách khỏi <see cref="Implementations.AuthService"/> để <c>LoginAsync</c> fake được toàn bộ nhánh.
    /// </summary>
    public interface IPackageTierResolver
    {
        Task<PackageTier> ResolveAsync(int studentId);
    }
}
