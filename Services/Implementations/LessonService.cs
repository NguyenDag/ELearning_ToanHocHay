using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Lesson;
using ELearning_ToanHocHay_Control.Models.DTOs.LessonContent;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class LessonService : ILessonSevice
    {
        private readonly ILessonRepository _lessonRepository;

        public LessonService(ILessonRepository lessonRepository)
        {
            _lessonRepository = lessonRepository;
        }
        public async Task<ApiResponse<LessonDto>> CreateAsync(CreateLessonDto dto, int creatorId)
        {
            try
            {
                var lesson = new Lesson
                {
                    TopicId = dto.TopicId,
                    LessonName = dto.LessonName,
                    Description = dto.Description,
                    OrderIndex = dto.OrderIndex,
                    DurationMinutes = dto.DurationMinutes,
                    IsFree = true,
                    Status = LessonStatus.Draft,
                    CreatedBy = creatorId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                await _lessonRepository.CreateAsync(lesson);

                return await GetByIdAsync(lesson.LessonId);
            }
            catch (Exception ex)
            {
                return ApiResponse<LessonDto>.ErrorResponse(
                    "Error creating lesson",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<LessonDto>> GetByIdAsync(int lessonId)
        {
            try
            {
                var lesson = await _lessonRepository.GetByIdAsync(lessonId);
                if (lesson == null)
                {
                    return ApiResponse<LessonDto>.ErrorResponse(
                        "Lesson not found",
                        new List<string> { $"No lesson found with ID: {lessonId}" }
                    );
                }

                var dto = new LessonDto
                {
                    LessonId = lesson.LessonId,
                    TopicId = lesson.TopicId,
                    LessonName = lesson.LessonName,
                    Description = lesson.Description,
                    DurationMinutes = lesson.DurationMinutes,
                    OrderIndex = lesson.OrderIndex,
                    IsFree = lesson.IsFree,
                    IsActive = lesson.IsActive,
                    Status = lesson.Status,
                    Contents = lesson.LessonContents
                        .OrderBy(c => c.OrderIndex)
                        .Select(c => new LessonContentDto
                        {
                            ContentId = c.ContentId,
                            BlockType = c.BlockType,
                            ContentText = c.ContentText,
                            ContentUrl = c.ContentUrl,
                            OrderIndex = c.OrderIndex
                        }).ToList()
                };

                return ApiResponse<LessonDto>.SuccessResponse(
                    dto,
                    "Lesson retrieved successfully"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<LessonDto>.ErrorResponse(
                    "Error retrieving lesson",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<IEnumerable<LessonDto>>> GetByTopicAsync(int topicId)
        {
            try
            {
                var lessons = await _lessonRepository.GetByTopicIdAsync(topicId);

                if (lessons == null)
                    return ApiResponse<IEnumerable<LessonDto>>.ErrorResponse(
                        "Topic not found",
                        new List<string> { $"No lesson found with ID: {topicId}" }
                        );

                var result = lessons.Select(lesson => new LessonDto
                {
                    LessonId = lesson.LessonId,
                    TopicId = lesson.TopicId,
                    LessonName = lesson.LessonName,
                    Description = lesson.Description,
                    DurationMinutes = lesson.DurationMinutes,
                    OrderIndex = lesson.OrderIndex,
                    IsFree = lesson.IsFree,
                    IsActive = lesson.IsActive,
                    Status = lesson.Status,
                    Contents = lesson.LessonContents
                        .OrderBy(c => c.OrderIndex)
                        .Select(c => new LessonContentDto
                        {
                            ContentId = c.ContentId,
                            BlockType = c.BlockType,
                            ContentText = c.ContentText,
                            ContentUrl = c.ContentUrl,
                            OrderIndex = c.OrderIndex,
                        }).ToList(),
                });

                return ApiResponse<IEnumerable<LessonDto>>.SuccessResponse(
                    result,
                    "Lessons retrieved successfully"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<LessonDto>>.ErrorResponse(
                    "Error retrieving lessons",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<bool>> PublishAsync(int lessonId)
        {
            try
            {
                var lesson = await _lessonRepository.GetByIdAsync(lessonId);
                if (lesson == null)
                {
                    return ApiResponse<bool>.ErrorResponse(
                        "Lesson not found",
                        new List<string> { $"No lesson found with ID: {lessonId}" }
                    );
                }

                if (lesson.Status != LessonStatus.Approved)
                {
                    return ApiResponse<bool>.ErrorResponse(
                        "Lesson cannot be published",
                        new List<string> { "Lesson must be approved before publishing" }
                    );
                }

                lesson.Status = LessonStatus.Published;
                lesson.PublishedAt = DateTime.UtcNow;

                await _lessonRepository.UpdateAsync(lesson);

                return ApiResponse<bool>.SuccessResponse(
                    true,
                    "Lesson published successfully"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse(
                    "Error publishing lesson",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<bool>> ReviewAsync(int lessonId, ReviewLessonDto dto, int reviewerId)
        {
            try
            {
                var lesson = await _lessonRepository.GetByIdAsync(lessonId);
                if (lesson == null)
                {
                    return ApiResponse<bool>.ErrorResponse(
                        "Lesson not found",
                        new List<string> { $"No lesson found with ID: {lessonId}" }
                    );
                }

                lesson.Status = dto.IsApproved
                    ? LessonStatus.Approved
                    : LessonStatus.Rejected;

                lesson.ReviewedBy = reviewerId;
                lesson.ReviewedAt = DateTime.UtcNow;
                lesson.RejectReason = dto.IsApproved ? null : dto.RejectReason;

                await _lessonRepository.UpdateAsync(lesson);

                return ApiResponse<bool>.SuccessResponse(
                    true,
                    "Lesson reviewed successfully"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse(
                    "Error reviewing lesson",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<bool>> SubmitForReviewAsync(int lessonId)
        {
            try
            {
                var lesson = await _lessonRepository.GetByIdAsync(lessonId);
                if (lesson == null)
                {
                    return ApiResponse<bool>.ErrorResponse(
                        "Lesson not found",
                        new List<string> { $"No lesson found with ID: {lessonId}" }
                    );
                }

                lesson.Status = LessonStatus.PendingReview;
                await _lessonRepository.UpdateAsync(lesson);

                return ApiResponse<bool>.SuccessResponse(
                    true,
                    "Lesson submitted for review"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse(
                    "Error submitting lesson for review",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<LessonDto>> UpdateAsync(int lessonId, UpdateLessonDto dto)
        {
            try
            {
                var lesson = await _lessonRepository.GetByIdAsync(lessonId);
                if (lesson == null)
                {
                    return ApiResponse<LessonDto>.ErrorResponse(
                        "Lesson not found",
                        new List<string> { $"No lesson found with ID: {lessonId}" }
                    );
                }

                if (lesson.Status != LessonStatus.Draft &&
                    lesson.Status != LessonStatus.Rejected)
                {
                    return ApiResponse<LessonDto>.ErrorResponse(
                        "Lesson cannot be updated",
                        new List<string> { "Only Draft or Rejected lessons can be updated" }
                    );
                }

                lesson.LessonName = dto.LessonName;
                lesson.Description = dto.Description;
                lesson.DurationMinutes = dto.DurationMinutes;
                lesson.OrderIndex = dto.OrderIndex;
                lesson.IsFree = dto.IsFree;
                lesson.IsActive = dto.IsActive;

                await _lessonRepository.UpdateAsync(lesson);

                return await GetByIdAsync(lessonId);
            }
            catch (Exception ex)
            {
                return ApiResponse<LessonDto>.ErrorResponse(
                    "Error updating lesson",
                    new List<string> { ex.Message }
                );
            }
        }
    }
}