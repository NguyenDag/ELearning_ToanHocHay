using ELearning_ToanHocHay_Control.Models.DTOs;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface IPackageService
    {
        Task<ApiResponse<IEnumerable<PackageDto>>> GetAllAsync();
        Task<ApiResponse<PackageDto>> GetByIdAsync(int packageId);
        Task<ApiResponse<PackageDto>> CreateAsync(int userId, CreateOrUpdatePackageDto dto);
        Task<ApiResponse<PackageDto>> UpdateAsync(int packageId, CreateOrUpdatePackageDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int packageId);
    }
}
