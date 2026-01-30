using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class LessonContentService : ILessonContentService
    {
        private readonly ILessonContentRepository _lessonContentRepository;
        private readonly ILessonRepository _lessonRepository;

        public LessonContentService(ILessonContentRepository lessonContentRepository, ILessonRepository lessonRepository)
        {
            _lessonContentRepository = lessonContentRepository;
            _lessonRepository = lessonRepository;
        }
        public async Task<ApiResponse<LessonContentDto>> CreateAsync(int lessonId, CreateLessonContentDto dto)
        {
            try
            {
                var existingLesson = await _lessonRepository.GetByIdAsync(lessonId);
                if (existingLesson == null)
                {
                    return ApiResponse<LessonContentDto>.ErrorResponse(
                    "Lesson content not found",
                    new List<string> { $"No lesson content found with ID: {lessonId}" }
                );
                }

                var lessonContent = new LessonContent
                {
                    LessonId = lessonId,
                    BlockType = dto.BlockType,
                    ContentText = dto.ContentText,
                    ContentUrl = dto.ContentUrl,
                    OrderIndex = dto.OrderIndex,
                };

                await _lessonContentRepository.AddAsync(lessonContent);

                return await GetByIdAsync(lessonContent.ContentId);
            }
            catch (Exception ex)
            {
                return ApiResponse<LessonContentDto>.ErrorResponse(
                    "Error creating lesson",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int contentId)
        {
            try
            {
                var exists = await _lessonContentRepository.GetByIdAsync(contentId);

                if (exists == null)
                {
                    return ApiResponse<bool>.ErrorResponse(
                        "Lesson content not found",
                        new List<string> { $"No lesson content found with ID: {contentId}" }
                    );
                }

                var deleted = await _lessonContentRepository.DeleteAsync(contentId);

                return ApiResponse<bool>.SuccessResponse(
                    deleted,
                    "Lesson content deleted successfully"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse(
                    "Error deleting lesson content",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<LessonContentDto>> GetByIdAsync(int contentId)
        {
            try
            {
                var lessonContent = await _lessonContentRepository.GetByIdAsync(contentId);
                if (lessonContent == null)
                {
                    return ApiResponse<LessonContentDto>.ErrorResponse(
                        "Lesson Content not found",
                        new List<string> { $"No lesson content found with ID: {contentId}" }
                    );
                }

                var dto = new LessonContentDto
                {
                    ContentId = contentId,
                    BlockType = lessonContent.BlockType,
                    ContentText = lessonContent.ContentText,
                    ContentUrl = lessonContent.ContentUrl,
                    OrderIndex = lessonContent.OrderIndex,
                };

                return ApiResponse<LessonContentDto>.SuccessResponse(dto, "Lesson content retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<LessonContentDto>.ErrorResponse(
                    "Error retrieving lessonContent",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<IEnumerable<LessonContentDto>>> GetByLessonAsync(int lessonId)
        {
            try
            {
                var lessonContents = await _lessonContentRepository.GetByLessonAsync(lessonId);

                if (lessonContents == null)
                    return ApiResponse<IEnumerable<LessonContentDto>>.ErrorResponse(
                        "Lesson not found",
                        new List<string> { $"No lesson content found with ID: {lessonId}" }
                        );

                var result = lessonContents.OrderBy(c => c.OrderIndex).Select(lessonContent => new LessonContentDto
                {
                    ContentId = lessonContent.ContentId,
                    BlockType = lessonContent.BlockType,
                    ContentText = lessonContent.ContentText,
                    ContentUrl = lessonContent.ContentUrl,
                    OrderIndex = lessonContent.OrderIndex,
                });

                return ApiResponse<IEnumerable<LessonContentDto>>.SuccessResponse(
                    result,
                    "Lesson contents retrieved successfully"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<LessonContentDto>>.ErrorResponse(
                    "Error retrieving lesson contents",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<LessonContentDto>> UpdateAsync(int contentId, UpdateLessonContentDto dto)
        {
            try
            {
                var lessonContent = await _lessonContentRepository.GetByIdAsync(contentId);
                if (lessonContent == null)
                {
                    return ApiResponse<LessonContentDto>.ErrorResponse(
                        "Lesson content not found",
                        new List<string> { $"No lesson content found with ID: {contentId}" }
                    );
                }

                lessonContent.LessonId = dto.LessonId;
                lessonContent.BlockType = dto.BlockType;
                lessonContent.ContentText = dto.ContentText;
                lessonContent.ContentUrl = dto.ContentUrl;
                lessonContent.OrderIndex = dto.OrderIndex;

                await _lessonContentRepository.UpdateAsync(lessonContent);

                return await GetByIdAsync(contentId);
            }
            catch (Exception ex)
            {
                return ApiResponse<LessonContentDto>.ErrorResponse(
                    "Error updating lesson content",
                    new List<string> { ex.Message }
                );
            }
        }
    }
}
