// FILE: ELearning_ToanHocHay_Control/Services/Implementations/ParentService.cs
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Parent;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class ParentService : IParentService
    {
        private readonly IParentRepository _repository;

        public ParentService(IParentRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<ParentDto>> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return ApiResponse<ParentDto>.ErrorResponse("Not found", null);

            var dto = new ParentDto
            {
                ParentId = entity.ParentId,
                UserId = entity.UserId,
                Job = entity.Job,
                FullName = entity.User?.FullName ?? "",
                Email = entity.User?.Email ?? "",
                ConnectionCode = entity.ConnectionCode,
                Children = entity.StudentParents?
                    .Where(sp => sp.Student?.User != null)
                    .Select(sp => new ChildDto
                    {
                        StudentId = sp.StudentId,
                        FullName = sp.Student!.User!.FullName,
                        GradeLevel = sp.Student.GradeLevel,
                        Relationship = sp.Relationship.ToString()
                    }).ToList() ?? new()
            };

            return ApiResponse<ParentDto>.SuccessResponse(dto);
        }

        public async Task<ApiResponse<ParentDto>> UpdateAsync(int id, UpdateParentDto dto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return ApiResponse<ParentDto>.ErrorResponse("Not found", null);

            entity.Job = dto.Job;
            await _repository.UpdateAsync(entity);

            return ApiResponse<ParentDto>.SuccessResponse(
                new ParentDto
                {
                    ParentId = entity.ParentId,
                    UserId = entity.UserId,
                    Job = entity.Job,
                    FullName = entity.User?.FullName ?? "",
                    Email = entity.User?.Email ?? "",
                    ConnectionCode = entity.ConnectionCode
                }, "Updated successfully");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            var deleted = await _repository.DeleteAsync(id);
            return ApiResponse<bool>.SuccessResponse(deleted);
        }
    }
}