using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface IUserService
    {
        Task<ApiResponse<IEnumerable<UserDto>>> GetAllAsync();
        Task<ApiResponse<UserDto>> GetByIdAsync(int userId);
        Task<ApiResponse<UserDto>> GetByEmailAsync(string email);

        Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserDto user);
        Task<ApiResponse<UserDto>> UpdateUserAsync(int id, UpdateUserDto updateUserDto);
        Task<ApiResponse<bool>> DeleteUserAsync(int userId);
    }
}
