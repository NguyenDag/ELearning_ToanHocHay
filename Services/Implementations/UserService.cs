using AutoMapper;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IMapper _mapper;

        public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher, IMapper mapper)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
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
                    PasswordHash = _passwordHasher.HashPassword(user.Password),
                    FullName = user.FullName,
                    Phone = user.Phone,
                    Dob = user.Dob,
                    AvatarUrl = user.AvatarUrl,
                    UserType = user.UserType,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                };
                var createdUser = await _userRepository.CreateUserAsync(newUser);
                return ApiResponse<UserDto>.SuccessResponse(
                    _mapper.Map<UserDto>(createdUser),
                    "User created successfully"
                    );
            }
            catch (Exception)
            {
                return ApiResponse<UserDto>.ErrorResponse(
                    "Error creating user",
                    new List<string>()
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
            catch (Exception)
            {
                return ApiResponse<bool>.ErrorResponse(
                    "Error deleting user",
                    new List<string>()
                );
            }
        }

        public async Task<ApiResponse<IEnumerable<UserDto>>> GetAllAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return ApiResponse<IEnumerable<UserDto>>.SuccessResponse(
                _mapper.Map<IEnumerable<UserDto>>(users), "Users retrieved successfully");
        }

        public async Task<ApiResponse<PagedResult<UserDto>>> GetPagedAsync(Common.PagedRequest request)
        {
            var query = _userRepository.Query();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var s = request.Search.Trim().ToLower();
                query = query.Where(u => u.Email.ToLower().Contains(s) || u.FullName.ToLower().Contains(s));
            }

            var page = await query.OrderByDescending(u => u.CreatedAt)
                .ToPagedResultAsync(request);

            return ApiResponse<PagedResult<UserDto>>.SuccessResponse(
                page.Map(u => _mapper.Map<UserDto>(u)));
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
            catch (Exception)
            {
                return ApiResponse<UserDto>.ErrorResponse(
                    "Error retrieving user",
                    new List<string>()
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
            catch (Exception)
            {
                return ApiResponse<UserDto>.ErrorResponse(
                    "Error retrieving user",
                    new List<string>()
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

                // Patch semantics: only apply fields that were sent.
                // Password / UserType / IsActive are NOT editable here (A2-01).
                if (!string.IsNullOrWhiteSpace(updateUserDto.FullName))
                    user.FullName = updateUserDto.FullName;
                if (updateUserDto.Phone != null)
                    user.Phone = updateUserDto.Phone;
                if (updateUserDto.Dob.HasValue)
                    user.Dob = updateUserDto.Dob;
                if (updateUserDto.AvatarUrl != null)
                    user.AvatarUrl = updateUserDto.AvatarUrl;
                user.UpdatedAt = DateTime.UtcNow;

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
            catch (Exception)
            {
                return ApiResponse<UserDto>.ErrorResponse(
                    "Error updating user",
                    new List<string>()
                );
            }
        }

        public async Task<ApiResponse<UserDto>> UpdateProfileAsync(int userId, UpdateProfileDto dto)
        {
            try
            {
                // GetByIdAsync tracks the User together with its Student navigation.
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                    return ApiResponse<UserDto>.ErrorResponse(
                        "User not found",
                        new List<string> { $"No user found with ID: {userId}" });

                var changed = false;

                if (!string.IsNullOrWhiteSpace(dto.FullName))
                {
                    user.FullName = dto.FullName;
                    changed = true;
                }

                if (dto.SchoolName != null && user.Student != null)
                {
                    user.Student.SchoolName = dto.SchoolName;
                    changed = true;
                }

                if (changed)
                {
                    user.UpdatedAt = DateTime.UtcNow;
                    await _userRepository.UpdateUserAsync(user);
                }

                return ApiResponse<UserDto>.SuccessResponse(
                    _mapper.Map<UserDto>(user),
                    "Profile updated successfully");
            }
            catch (Exception)
            {
                return ApiResponse<UserDto>.ErrorResponse(
                    "Error updating profile",
                    new List<string>());
            }
        }
    }
}
