using System.ComponentModel.DataAnnotations;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    // 1. DTO chính để trả về Web
    public class CurriculumDto
    {
        public int CurriculumId { get; set; }
        public int GradeLevel { get; set; }
        public string Subject { get; set; } = null!;
        public string CurriculumName { get; set; } = null!;
        public string? Description { get; set; }
        public CurriculumStatus Status { get; set; }
        public int Version { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Luôn khởi tạo List để tránh lỗi Null Reference
        public List<ChapterDto> Chapters { get; set; } = new();
    }

    // 2. DTO để hứng dữ liệu tạo mới
    public class CreateCurriculumDto
    {
        [Required]
        [Range(6, 9)]
        public int GradeLevel { get; set; }
        [Required, MaxLength(100)]
        public string Subject { get; set; } = null!;
        [Required, MaxLength(255)]
        public string CurriculumName { get; set; } = null!;
        public string? Description { get; set; }
        public CurriculumStatus Status { get; set; } = CurriculumStatus.Draft;
    }

    // 3. DTO để cập nhật
    public class UpdateCurriculumDto
    {
        [Required]
        [Range(6, 9)]
        public int GradeLevel { get; set; }
        [Required, MaxLength(100)]
        public string Subject { get; set; } = null!;
        [Required, MaxLength(255)]
        public string CurriculumName { get; set; } = null!;
        public string? Description { get; set; }
        public CurriculumStatus Status { get; set; }
    }

    // LƯU Ý: Nếu bạn đã có file ChapterDto.cs, TopicDto.cs riêng lẻ, 
    // hãy XÓA các class ChapterDto, TopicDto, LessonDto ở file này đi.
}