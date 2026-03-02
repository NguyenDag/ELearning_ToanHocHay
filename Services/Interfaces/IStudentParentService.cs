using ELearning_ToanHocHay_Control.Models.DTOs;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface IStudentParentService
    {
        Task<ApiResponse<StudentParentDto>> ConnectParentAsync(int userId, ConnectParentDto dto);
    }
}
