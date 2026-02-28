using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Chapter;
using ELearning_ToanHocHay_Control.Models.DTOs.Lesson;
using ELearning_ToanHocHay_Control.Models.DTOs.LessonContent;
using ELearning_ToanHocHay_Control.Models.DTOs.Topic;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class LessonDataService : ILessonDataService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<LessonDataService> _logger;

        public LessonDataService(IUnitOfWork unitOfWork, ILogger<LessonDataService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        public async Task<ApiResponse<LessonDetailResponseDto>> CreateLessonDataAsync(CreateLessonDataDto dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                int chapterId = 0;
                int topicId = 0;
                int lessonId = 0;

                // 1. Create Chapter if provided
                if (dto.Chapter != null)
                {
                    var curriculumExists = await _unitOfWork.Curriculums.ExistsAsync(dto.Chapter.CurriculumId);
                    if (!curriculumExists)
                    {
                        await _unitOfWork.RollbackAsync();
                        return ApiResponse<LessonDetailResponseDto>.ErrorResponse("Khung chương trình không tồn tại");
                    }

                    var chapter = new Chapter
                    {
                        CurriculumId = dto.Chapter.CurriculumId,
                        ChapterName = dto.Chapter.ChapterName,
                        OrderIndex = dto.Chapter.OrderIndex,
                        Description = dto.Chapter.Description,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.Chapters.CreateAsync(chapter);
                    await _unitOfWork.SaveChangesAsync();

                    chapterId = chapter.ChapterId;
                    _logger.LogInformation("Created new chapter with ID: {ChapterId}", chapterId);
                }

                // 2. Create Topic if provided
                if (dto.Topic != null)
                {
                    // Use the newly created chapter ID or the provided one
                    var finalChapterId = chapterId > 0 ? chapterId : dto.Topic.ChapterId;

                    var chapterExists = await _unitOfWork.Chapters.ExistsAsync(finalChapterId);
                    if (!chapterExists)
                    {
                        await _unitOfWork.RollbackAsync();
                        return ApiResponse<LessonDetailResponseDto>.ErrorResponse("Chương không tồn tại");
                    }

                    var topic = new Topic
                    {
                        ChapterId = finalChapterId,
                        TopicName = dto.Topic.TopicName,
                        OrderIndex = dto.Topic.OrderIndex,
                        Description = dto.Topic.Description,
                        IsFree = dto.Topic.IsFree,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.Topics.CreateAsync(topic);
                    await _unitOfWork.SaveChangesAsync();

                    topicId = topic.TopicId;
                    _logger.LogInformation("Created new topic with ID: {TopicId}", topicId);
                }

                // 3. Create Lesson (required)
                if (dto.Lesson == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return ApiResponse<LessonDetailResponseDto>.ErrorResponse("Thông tin lesson là bắt buộc");
                }

                // Use the newly created topic ID or the provided one
                var finalTopicId = topicId > 0 ? topicId : dto.Lesson.TopicId;

                var topicExists = await _unitOfWork.Topics.ExistsAsync(finalTopicId);
                if (!topicExists)
                {
                    await _unitOfWork.RollbackAsync();
                    return ApiResponse<LessonDetailResponseDto>.ErrorResponse("Topic không tồn tại");
                }

                var lesson = new Lesson
                {
                    TopicId = finalTopicId,
                    LessonName = dto.Lesson.LessonName,
                    Description = dto.Lesson.Description,
                    DurationMinutes = dto.Lesson.DurationMinutes,
                    OrderIndex = dto.Lesson.OrderIndex,
                    IsFree = dto.Lesson.IsFree,
                    IsActive = dto.Lesson.IsActive,
                    Status = dto.Lesson.Status,
                    CreatedBy = dto.Lesson.CreatedBy,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Lessons.CreateAsync(lesson);
                await _unitOfWork.SaveChangesAsync();

                lessonId = lesson.LessonId;
                _logger.LogInformation("Created new lesson with ID: {LessonId}", lessonId);

                // 4. Create Lesson Contents (required)
                if (dto.LessonContents == null || !dto.LessonContents.Any())
                {
                    await _unitOfWork.RollbackAsync();
                    return ApiResponse<LessonDetailResponseDto>.ErrorResponse("Phải có ít nhất 1 content block");
                }

                var lessonContents = dto.LessonContents.Select(lc => new LessonContent
                {
                    LessonId = lessonId,
                    BlockType = lc.BlockType,
                    ContentText = lc.ContentText,
                    ContentUrl = lc.ContentUrl,
                    OrderIndex = lc.OrderIndex,
                }).ToList();

                await _unitOfWork.LessonContents.AddRangeAsync(lessonContents);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Created {Count} lesson contents for lesson {LessonId}", lessonContents.Count, lessonId);

                // Commit transaction
                await _unitOfWork.CommitAsync();

                var userExists = await _unitOfWork.Users.ExistsAsync(dto.Lesson.CreatedBy);
                if (!userExists)
                {
                    await _unitOfWork.RollbackAsync();
                    return ApiResponse<LessonDetailResponseDto>.ErrorResponse(
                        "Người tạo bài học không tồn tại");
                }

                // 5. Fetch the complete lesson with all details
                var createdLesson = await _unitOfWork.Lessons.GetByIdWithContentsAsync(lessonId);

                var result = new LessonDetailResponseDto
                {
                    Lesson = new LessonResponseDto
                    {
                        Id = createdLesson.LessonId,
                        TopicId = createdLesson.TopicId,
                        TopicName = createdLesson.Topic?.TopicName,
                        LessonName = createdLesson.LessonName,
                        Description = createdLesson.Description,
                        OrderIndex = createdLesson.OrderIndex,
                        IsFree = createdLesson.IsFree,
                        IsActive = createdLesson.IsActive,
                        Status = createdLesson.Status.ToString(),
                        CreatedBy = createdLesson.CreatedBy,
                        CreatedAt = createdLesson.CreatedAt
                    },
                    Contents = createdLesson.LessonContents.Select(lc => new LessonContentResponseDto
                    {
                        Id = lc.ContentId,
                        LessonId = lc.LessonId,
                        BlockType = lc.BlockType.ToString(),
                        ContentText = lc.ContentText,
                        ContentUrl = lc.ContentUrl,
                        OrderIndex = lc.OrderIndex
                    }).ToList()
                };

                var message = dto.Chapter != null
                    ? "Đã tạo thành công Chapter, Topic, Lesson và Contents"
                    : dto.Topic != null
                        ? "Đã tạo thành công Topic, Lesson và Contents"
                        : "Đã tạo thành công Lesson và Contents";

                return ApiResponse<LessonDetailResponseDto>.SuccessResponse(result, message);

            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error creating lesson data");
                return ApiResponse<LessonDetailResponseDto>.ErrorResponse(
                    "Có lỗi xảy ra khi tạo dữ liệu bài học: " + ex.Message);
            }
        }
        public async Task<ApiResponse<LessonDetailResponseDto>> CreateOrAddLessonDataAsync(
            CreateOrAddLessonDataDto dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                int chapterId = await ResolveChapterAsync(dto.Chapter);
                int topicId = await ResolveTopicAsync(dto.Topic, chapterId);
                int lessonId = await ResolveLessonAsync(dto.Lesson, topicId);

                if (dto.LessonContents == null || !dto.LessonContents.Any())
                    throw new Exception("Phải có ít nhất 1 content");

                var oldContents = await _unitOfWork.LessonContents.GetByLessonAsync(lessonId);

                if (oldContents.Any())
                {
                    await _unitOfWork.LessonContents.RemoveRangeAsync(oldContents);
                }

                int orderIndex = 1;

                var contents = dto.LessonContents.Select(c => new LessonContent
                {
                    LessonId = lessonId,
                    BlockType = c.BlockType,
                    ContentText = c.ContentText,
                    ContentUrl = c.ContentUrl,
                    OrderIndex = orderIndex++
                }).ToList();

                await _unitOfWork.LessonContents.AddRangeAsync(contents);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitAsync();

                var lesson = await _unitOfWork.Lessons.GetByIdWithContentsAsync(lessonId);

                return ApiResponse<LessonDetailResponseDto>.SuccessResponse(
                    MapLessonDetail(lesson),
                    "Xử lý bài học thành công");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return ApiResponse<LessonDetailResponseDto>.ErrorResponse(ex.Message);
            }
        }

        private LessonDetailResponseDto MapLessonDetail(Lesson lesson)
        {
            if (lesson == null)
                throw new Exception("Lesson không tồn tại");

            return new LessonDetailResponseDto
            {
                Lesson = new LessonResponseDto
                {
                    Id = lesson.LessonId,
                    TopicId = lesson.TopicId,
                    TopicName = lesson.Topic?.TopicName,
                    LessonName = lesson.LessonName,
                    Description = lesson.Description,
                    OrderIndex = lesson.OrderIndex,
                    IsFree = lesson.IsFree,
                    IsActive = lesson.IsActive,
                    Status = lesson.Status.ToString(),
                    CreatedBy = lesson.CreatedBy,
                    CreatedAt = lesson.CreatedAt
                },
                Contents = lesson.LessonContents?
                    .OrderBy(c => c.OrderIndex)
                    .Select(c => new LessonContentResponseDto
                    {
                        Id = c.ContentId,
                        LessonId = c.LessonId,
                        BlockType = c.BlockType.ToString(),
                        ContentText = c.ContentText,
                        ContentUrl = c.ContentUrl,
                        OrderIndex = c.OrderIndex
                    })
                    .ToList()
                    ?? new List<LessonContentResponseDto>()
            };
        }


        // =========================
        // ===== Helper Methods ====
        // =========================

        private async Task<int> ResolveChapterAsync(UpsertChapterDto? dto)
        {
            if (dto == null)
                return 0;

            if (dto.ChapterId.HasValue)
            {
                if (!await _unitOfWork.Chapters.ExistsAsync(dto.ChapterId.Value))
                    throw new Exception("Chapter không tồn tại");

                return dto.ChapterId.Value;
            }

            if (!dto.CurriculumId.HasValue || string.IsNullOrWhiteSpace(dto.ChapterName))
                throw new Exception("Thiếu thông tin để tạo chapter");

            var chapter = new Chapter
            {
                CurriculumId = dto.CurriculumId.Value,
                ChapterName = dto.ChapterName,
                OrderIndex = dto.OrderIndex,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Chapters.CreateAsync(chapter);
            await _unitOfWork.SaveChangesAsync();

            return chapter.ChapterId;
        }

        private async Task<int> ResolveTopicAsync(
            UpsertTopicDto? dto,
            int chapterIdFromCreate)
        {
            if (dto == null)
                return 0;

            if (dto.TopicId.HasValue)
            {
                if (!await _unitOfWork.Topics.ExistsAsync(dto.TopicId.Value))
                    throw new Exception("Topic không tồn tại");

                return dto.TopicId.Value;
            }

            int finalChapterId = chapterIdFromCreate > 0
                ? chapterIdFromCreate
                : dto.ChapterId ?? 0;

            if (finalChapterId <= 0)
                throw new Exception("Thiếu Chapter để tạo Topic");

            if (string.IsNullOrWhiteSpace(dto.TopicName))
                throw new Exception("TopicName là bắt buộc khi tạo mới");

            var topic = new Topic
            {
                ChapterId = finalChapterId,
                TopicName = dto.TopicName,
                OrderIndex = dto.OrderIndex,
                Description = dto.Description,
                IsFree = dto.IsFree,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Topics.CreateAsync(topic);
            await _unitOfWork.SaveChangesAsync();

            return topic.TopicId;
        }

        private async Task<int> ResolveLessonAsync(
            UpsertLessonDto dto,
            int topicIdFromCreate)
        {
            if (dto == null)
                throw new Exception("Lesson là bắt buộc");

            if (dto.LessonId.HasValue)
            {
                if (!await _unitOfWork.Lessons.ExistsAsync(dto.LessonId.Value))
                    throw new Exception("Lesson không tồn tại");

                return dto.LessonId.Value;
            }

            if (topicIdFromCreate <= 0)
                throw new Exception("Muốn tạo lesson mới thì phải chọn hoặc tạo topic");

            if (string.IsNullOrWhiteSpace(dto.LessonName))
                throw new Exception("LessonName là bắt buộc");

            if (dto.CreatedBy <= 0)
                throw new Exception("CreatedBy là bắt buộc khi tạo lesson mới");

            if (!await _unitOfWork.Users.ExistsAsync(dto.CreatedBy))
                throw new Exception("Người tạo không tồn tại");

            var lesson = new Lesson
            {
                TopicId = topicIdFromCreate,
                LessonName = dto.LessonName,
                Description = dto.Description,
                DurationMinutes = dto.DurationMinutes,
                OrderIndex = dto.OrderIndex,
                IsFree = dto.IsFree,
                IsActive = dto.IsActive,
                Status = dto.Status,
                CreatedBy = dto.CreatedBy,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Lessons.CreateAsync(lesson);
            await _unitOfWork.SaveChangesAsync();

            return lesson.LessonId;
        }
    }
}
