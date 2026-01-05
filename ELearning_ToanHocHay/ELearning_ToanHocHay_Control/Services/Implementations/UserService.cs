using AutoMapper;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public UserService(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }
        public async Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserDto user)
        {
            try
            {
                // check email is exist or not
                var existingUser = await _userRepository.ExistsByEmail(user.Email);
                if (existingUser)
                {
                    return ApiResponse<UserDto>.ErrorResponse(
                        "Email already exists",
                        new List<string> { "This email is already registered" }
                        );
                }
                // create new user
                var newUser = new User
                {
                    Email = user.Email,
                    PasswordHash = user.Password,
                    FullName = user.FullName,
                    Phone = user.Phone,
                    Dob = user.Dob,
                    AvatarUrl = user.AvatarUrl,
                    UserType = user.UserType,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                };
                var createdUser = await _userRepository.CreateUserAsync(newUser);
                return ApiResponse<UserDto>.SuccessResponse(
                    _mapper.Map<UserDto>(createdUser),
                    "User created successfully"
                    );
            }
            catch (Exception ex)
            {
                return ApiResponse<UserDto>.ErrorResponse(
                    "Error creating user",
                    new List<string> { ex.Message }
                    );
            }
        }

        public async Task<ApiResponse<bool>> DeleteUserAsync(int userId)
        {
            try
            {
                var exists = await _userRepository.GetByIdAsync(userId);

                if (exists == null)
                {
                    return ApiResponse<bool>.ErrorResponse(
                        "User not found",
                        new List<string> { $"No user found with ID: {userId}" }
                    );
                }

                var deleted = await _userRepository.DeleteUserAsync(userId);

                return ApiResponse<bool>.SuccessResponse(
                    deleted,
                    "User deleted successfully"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse(
                    "Error deleting user",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<IEnumerable<UserDto>>> GetAllAsync()
        {
            try
            {
                var _user = await _userRepository.GetAllAsync();
                return ApiResponse<IEnumerable<UserDto>>.SuccessResponse(
                    _mapper.Map<IEnumerable<UserDto>>(_user),
                    "Users retrieved successfully"
                    );
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<UserDto>>.ErrorResponse(
                    "Error retrieving users",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<UserDto>> GetByEmailAsync(string email)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(email);
                if (user == null)
                    return ApiResponse<UserDto>.ErrorResponse(
                        "Email not found",
                        new List<string> { "This email is not register" }
                        );
                return ApiResponse<UserDto>.SuccessResponse(
                    _mapper.Map<UserDto>(user),
                    "User retrieved successfully"
                    );
            }
            catch (Exception ex)
            {
                return ApiResponse<UserDto>.ErrorResponse(
                    "Error retrieving user",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<UserDto>> GetByIdAsync(int userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);

                if (user == null)
                {
                    return ApiResponse<UserDto>.ErrorResponse(
                        "User not found",
                        new List<string> { $"No user found with ID: {userId}" }
                    );
                }

                return ApiResponse<UserDto>.SuccessResponse(
                    _mapper.Map<UserDto>(user),
                    "User retrieved successfully"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<UserDto>.ErrorResponse(
                    "Error retrieving user",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<UserDto>> UpdateUserAsync(int id, UpdateUserDto updateUserDto)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);

                if (user == null)
                {
                    return ApiResponse<UserDto>.ErrorResponse(
                        "User not found",
                        new List<string> { $"No user found with ID: {id}" }
                    );
                }

                // Update info
                if (!string.IsNullOrEmpty(updateUserDto.FullName))
                    user.FullName = updateUserDto.FullName;
                user.PasswordHash = updateUserDto.Password;
                user.Phone = updateUserDto.Phone;
                user.Dob = updateUserDto.Dob;
                user.AvatarUrl = updateUserDto.AvatarUrl;
                user.UserType = updateUserDto.UserType;
                user.IsActive = updateUserDto.IsActive;
                user.UpdatedAt = DateTime.Now;

                var updatedUser = await _userRepository.UpdateUserAsync(user);

                if (updatedUser == null)
                {
                    return ApiResponse<UserDto>.ErrorResponse(
                        "Error updating user",
                        new List<string> { "Failed to update user" }
                    );
                }

                return ApiResponse<UserDto>.SuccessResponse(
                    _mapper.Map<UserDto>(user),
                    "User updated successfully"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<UserDto>.ErrorResponse(
                    "Error updating user",
                    new List<string> { ex.Message }
                );
            }
        }
    }
}
