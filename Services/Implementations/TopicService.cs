using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Topic;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class TopicService : ITopicService
    {
        private readonly ITopicRepository _topicRepository;
        private readonly IChapterRepository _chapterRepository;

        public TopicService(
            ITopicRepository topicRepository,
            IChapterRepository chapterRepository)
        {
            _topicRepository = topicRepository;
            _chapterRepository = chapterRepository;
        }

        public async Task<ApiResponse<TopicDto>> CreateAsync(CreateTopicDto dto)
        {
            // Check Chapter tồn tại
            var chapter = await _chapterRepository.GetByIdAsync(dto.ChapterId);
            if (chapter == null)
            {
                return ApiResponse<TopicDto>.ErrorResponse(
                    "Chapter not found",
                    new List<string> { $"No chapter with ID {dto.ChapterId}" }
                );
            }

            var topic = new Topic
            {
                ChapterId = dto.ChapterId,
                TopicName = dto.TopicName,
                OrderIndex = dto.OrderIndex,
                Description = dto.Description,
                IsFree = dto.IsFree
            };

            var created = await _topicRepository.CreateAsync(topic);

            return ApiResponse<TopicDto>.SuccessResponse(
                MapToDto(created),
                "Topic created successfully"
            );
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int topicId)
        {
            var success = await _topicRepository.DeleteAsync(topicId);

            return success
                ? ApiResponse<bool>.SuccessResponse(true, "Topic deleted")
                : ApiResponse<bool>.ErrorResponse("Topic not found");
        }

        public async Task<ApiResponse<IEnumerable<TopicDto>>> GetByChapterAsync(int chapterId)
        {
            var topics = await _topicRepository.GetByChapterIdAsync(chapterId);

            return ApiResponse<IEnumerable<TopicDto>>.SuccessResponse(
                topics.Select(MapToDto)
            );
        }

        public async Task<ApiResponse<TopicDto>> GetByIdAsync(int topicId)
        {
            var topic = await _topicRepository.GetByIdAsync(topicId);
            if (topic == null)
                return ApiResponse<TopicDto>.ErrorResponse("Topic not found");

            return ApiResponse<TopicDto>.SuccessResponse(MapToDto(topic));
        }

        public async Task<ApiResponse<TopicDto>> UpdateAsync(int topicId, UpdateTopicDto dto)
        {
            var existing = await _topicRepository.GetByIdAsync(topicId);
            if (existing == null)
                return ApiResponse<TopicDto>.ErrorResponse("Topic not found");

            existing.TopicName = dto.TopicName;
            existing.OrderIndex = dto.OrderIndex;
            existing.Description = dto.Description;
            existing.IsFree = dto.IsFree;
            existing.IsActive = dto.IsActive;

            var updated = await _topicRepository.UpdateAsync(existing);

            return ApiResponse<TopicDto>.SuccessResponse(
                MapToDto(updated!),
                "Topic updated successfully"
            );
        }

        private static TopicDto MapToDto(Topic t)
        {
            return new TopicDto
            {
                TopicId = t.TopicId,
                ChapterId = t.ChapterId,
                TopicName = t.TopicName,
                OrderIndex = t.OrderIndex,
                Description = t.Description,
                IsFree = t.IsFree,
                IsActive = t.IsActive
            };
        }
    }
}
