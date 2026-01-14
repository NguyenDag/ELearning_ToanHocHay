using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class CurriculumService : ICurriculumService
    {
        private readonly ICurriculumRepository _curriculumRepository;

        public CurriculumService(ICurriculumRepository curriculumRepository)
        {
            _curriculumRepository = curriculumRepository;
        }

        public async Task<ApiResponse<CurriculumDto>> CreateAsync(CreateCurriculumDto dto, int currentUserId)
        {
            var curriculum = new Curriculum
            {
                GradeLevel = dto.GradeLevel,
                Subject = dto.Subject,
                CurriculumName = dto.CurriculumName,
                Description = dto.Description,
                Status = dto.Status,
                CreatedBy = currentUserId,
                CreatedAt = DateTime.UtcNow,
                Version = 1
            };

            var created = await _curriculumRepository.CreateCurriculumAsync(curriculum);

            return ApiResponse<CurriculumDto>.SuccessResponse(
                MapToDto(created),
                "Curriculum created successfully"
            );
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int curriculumId)
        {
            var deleted = await _curriculumRepository.DeleteCurriculumAsync(curriculumId);

            if (!deleted)
            {
                return ApiResponse<bool>.ErrorResponse(
                    "Delete failed",
                    new List<string> { $"No curriculum found with ID: {curriculumId}" }
                );
            }

            return ApiResponse<bool>.SuccessResponse(
                true,
                "Curriculum deleted successfully"
            );
        }

        public async Task<ApiResponse<IEnumerable<CurriculumDto>>> GetAllAsync()
        {
            var curriculums = await _curriculumRepository.GetAllAsync();

            var result = curriculums.Select(MapToDto);

            return ApiResponse<IEnumerable<CurriculumDto>>.SuccessResponse(
                result,
                "Get curriculums successfully"
            );
        }

        public async Task<ApiResponse<CurriculumDto>> GetByIdAsync(int curriculumId)
        {
            var curriculum = await _curriculumRepository.GetCurriculumByIdAsync(curriculumId);

            if (curriculum == null)
            {
                return ApiResponse<CurriculumDto>.ErrorResponse(
                    "Curriculum not found",
                    new List<string> { $"No curriculum found with ID: {curriculumId}" }
                );
            }

            return ApiResponse<CurriculumDto>.SuccessResponse(
                MapToDto(curriculum),
                "Get curriculum successfully"
            );
        }

        public async Task<ApiResponse<CurriculumDto>> UpdateAsync(int curriculumId, UpdateCurriculumDto dto)
        {
            var existing = await _curriculumRepository.GetCurriculumByIdAsync(curriculumId);

            if (existing == null)
            {
                return ApiResponse<CurriculumDto>.ErrorResponse(
                    "Curriculum not found",
                    new List<string> { $"No curriculum found with ID: {curriculumId}" }
                );
            }

            // Rule nghiệp vụ: không cho sửa khi đã Archived
            if (existing.Status == CurriculumStatus.Archived)
            {
                return ApiResponse<CurriculumDto>.ErrorResponse(
                    "Curriculum is archived",
                    new List<string> { "Archived curriculum cannot be updated" }
                );
            }

            existing.GradeLevel = dto.GradeLevel;
            existing.Subject = dto.Subject;
            existing.CurriculumName = dto.CurriculumName;
            existing.Description = dto.Description;
            existing.Status = dto.Status;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.Version++;

            var updated = await _curriculumRepository.UpdateCurriculumAsync(existing);

            if (updated == null)
            {
                return ApiResponse<CurriculumDto>.ErrorResponse(
                    "Update failed",
                    new List<string> { "Unable to update curriculum" }
                );
            }

            return ApiResponse<CurriculumDto>.SuccessResponse(
                MapToDto(updated),
                "Curriculum updated successfully"
            );
        }
        private static CurriculumDto MapToDto(Curriculum curriculum)
        {
            return new CurriculumDto
            {
                CurriculumId = curriculum.CurriculumId,
                GradeLevel = curriculum.GradeLevel,
                Subject = curriculum.Subject,
                CurriculumName = curriculum.CurriculumName,
                Description = curriculum.Description,
                Status = curriculum.Status,
                Version = curriculum.Version,
                CreatedAt = curriculum.CreatedAt,
                UpdatedAt = curriculum.UpdatedAt
            };
        }

        public async Task<ApiResponse<bool>> PublishAsync(int curriculumId)
        {
            var curriculum = await _curriculumRepository.GetCurriculumByIdAsync(curriculumId);

            if (curriculum == null)
                return ApiResponse<bool>.ErrorResponse(
                    "Curriculum not found",
                    new List<string> { $"No curriculum with ID {curriculumId}" }
                );

            if (curriculum.Status == CurriculumStatus.Archived)
                return ApiResponse<bool>.ErrorResponse(
                    "Curriculum is archived",
                    new List<string> { "Cannot publish archived curriculum" }
                );

            curriculum.Status = CurriculumStatus.Published;
            curriculum.UpdatedAt = DateTime.UtcNow;

            await _curriculumRepository.UpdateCurriculumAsync(curriculum);

            return ApiResponse<bool>.SuccessResponse(true, "Curriculum published");
        }

        public async Task<ApiResponse<bool>> ArchiveAsync(int curriculumId)
        {
            var curriculum = await _curriculumRepository.GetCurriculumByIdAsync(curriculumId);

            if (curriculum == null)
            {
                return ApiResponse<bool>.ErrorResponse(
                    "Curriculum not found",
                    new List<string> { $"No curriculum found with ID: {curriculumId}" }
                );
            }

            if (curriculum.Status == CurriculumStatus.Archived)
            {
                return ApiResponse<bool>.ErrorResponse(
                    "Curriculum already archived",
                    new List<string> { "This curriculum has already been archived" }
                );
            }

            curriculum.Status = CurriculumStatus.Archived;
            curriculum.UpdatedAt = DateTime.UtcNow;

            await _curriculumRepository.UpdateCurriculumAsync(curriculum);

            return ApiResponse<bool>.SuccessResponse(
                true,
                "Curriculum archived successfully"
            );
        }
    }
}
