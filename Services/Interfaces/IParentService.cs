using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Parent;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface IParentService
    {
        Task<ApiResponse<ParentDto>> GetByIdAsync(int id);
        Task<ApiResponse<ParentDto>> UpdateAsync(int id, UpdateParentDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int id);
    }
}
