using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class ChapterService : IChapterService
    {
        private readonly IChapterRepository _chapterRepository;
        private readonly ICurriculumRepository _curriculumRepository;

        public ChapterService(
            IChapterRepository chapterRepository,
            ICurriculumRepository curriculumRepository)
        {
            _chapterRepository = chapterRepository;
            _curriculumRepository = curriculumRepository;
        }
        public async Task<ApiResponse<ChapterDto>> CreateAsync(CreateChapterDto dto)
        {
            var curriculum = await _curriculumRepository.GetCurriculumByIdAsync(dto.CurriculumId);
            if (curriculum == null)
            {
                return ApiResponse<ChapterDto>.ErrorResponse(
                    "Curriculum not found",
                    new List<string> { $"No curriculum with ID {dto.CurriculumId}" }
                );
            }

            var chapter = new Chapter
            {
                CurriculumId = dto.CurriculumId,
                ChapterName = dto.ChapterName,
                OrderIndex = dto.OrderIndex,
                Description = dto.Description
            };

            var created = await _chapterRepository.CreateAsync(chapter);

            return ApiResponse<ChapterDto>.SuccessResponse(
                MapToDto(created),
                "Chapter created successfully"
            );
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int chapterId)
        {
            var success = await _chapterRepository.DeleteAsync(chapterId);

            return success
                ? ApiResponse<bool>.SuccessResponse(true, "Chapter deleted")
                : ApiResponse<bool>.ErrorResponse("Chapter not found");
        }

        public async Task<ApiResponse<IEnumerable<ChapterDto>>> GetByCurriculumAsync(int curriculumId)
        {
            var chapters = await _chapterRepository.GetByCurriculumIdAsync(curriculumId);

            return ApiResponse<IEnumerable<ChapterDto>>.SuccessResponse(
                chapters.Select(MapToDto)
            );
        }

        public async Task<ApiResponse<ChapterDto>> GetByIdAsync(int chapterId)
        {
            var chapter = await _chapterRepository.GetByIdAsync(chapterId);

            if (chapter == null)
                return ApiResponse<ChapterDto>.ErrorResponse("Chapter not found");

            return ApiResponse<ChapterDto>.SuccessResponse(MapToDto(chapter));
        }

        public async Task<ApiResponse<ChapterDto>> UpdateAsync(int chapterId, UpdateChapterDto dto)
        {
            var existing = await _chapterRepository.GetByIdAsync(chapterId);
            if (existing == null)
                return ApiResponse<ChapterDto>.ErrorResponse("Chapter not found");

            existing.ChapterName = dto.ChapterName;
            existing.OrderIndex = dto.OrderIndex;
            existing.Description = dto.Description;
            existing.IsActive = dto.IsActive;

            var updated = await _chapterRepository.UpdateAsync(existing);

            return ApiResponse<ChapterDto>.SuccessResponse(
                MapToDto(updated!),
                "Chapter updated successfully"
            );
        }
        private static ChapterDto MapToDto(Chapter c)
        {
            return new ChapterDto
            {
                ChapterId = c.ChapterId,
                CurriculumId = c.CurriculumId,
                ChapterName = c.ChapterName,
                OrderIndex = c.OrderIndex,
                Description = c.Description,
                IsActive = c.IsActive
            };
        }
    }
}
