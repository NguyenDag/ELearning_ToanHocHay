using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Package;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface IPackageService
    {
        Task<ApiResponse<IEnumerable<PackageDto>>> GetAllAsync();

        /// <summary>Danh sách gói cho màn hình quản lý (gồm cả gói đã tắt + số thuê bao).</summary>
        Task<ApiResponse<IEnumerable<PackageDto>>> GetAllForManagementAsync();

        Task<ApiResponse<PackageDto>> GetByIdAsync(int packageId);
        Task<ApiResponse<PackageDto>> CreateAsync(int userId, CreateOrUpdatePackageDto dto);
        Task<ApiResponse<PackageDto>> UpdateAsync(int packageId, CreateOrUpdatePackageDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int packageId);
    }
}
