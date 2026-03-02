using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class StudentParentService : IStudentParentService
    {
        private readonly IStudentParentRepository _repository;
        private readonly IUserRepository _userRepository;

        public StudentParentService(
            IStudentParentRepository repository,
            IUserRepository userRepository)
        {
            _repository = repository;
            _userRepository = userRepository;
        }

        public async Task<ApiResponse<StudentParentDto>> ConnectParentAsync(int studentUserId, ConnectParentDto dto)
        {
            try
            {
                // 1️ Lấy student từ userId
                var user = await _userRepository.GetByIdAsync(studentUserId);

                if (user == null || user.Student == null)
                {
                    return ApiResponse<StudentParentDto>.ErrorResponse(
                        "Invalid student",
                        new List<string> { "User is not a student" });
                }

                var student = user.Student;

                // 2️ Tìm parent theo mã
                var parent = await _repository.GetParentByConnectionCodeAsync(dto.ConnectionCode);

                if (parent == null)
                {
                    return ApiResponse<StudentParentDto>.ErrorResponse(
                        "Invalid code",
                        new List<string> { "Connection code not found" });
                }

                // 3️ Check đã tồn tại chưa
                var exists = await _repository.ExistsAsync(student.StudentId, parent.ParentId);

                if (exists)
                {
                    return ApiResponse<StudentParentDto>.ErrorResponse(
                        "Already connected",
                        new List<string> { "Student already connected to this parent" });
                }

                // 4️ Tạo relation
                var relation = new StudentParent
                {
                    StudentId = student.StudentId,
                    ParentId = parent.ParentId,
                    Relationship = dto.Relationship
                };

                await _repository.CreateAsync(relation);

                var result = new StudentParentDto
                {
                    StudentId = student.StudentId,
                    ParentId = parent.ParentId,
                    Relationship = dto.Relationship,
                    ParentName = parent.User.FullName,
                    ParentEmail = parent.User.Email
                };

                return ApiResponse<StudentParentDto>.SuccessResponse(
                    result,
                    "Connected successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<StudentParentDto>.ErrorResponse(
                    "Error connecting parent",
                    new List<string> { ex.Message });
            }
        }
    }

}
