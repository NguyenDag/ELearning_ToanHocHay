using System.ComponentModel.DataAnnotations;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Content
{
    // ---------------- ContentNode ----------------
    public class ContentNodeDto
    {
        public int NodeId { get; set; }
        public int CourseVersionId { get; set; }
        public int? ParentNodeId { get; set; }
        public NodeType NodeType { get; set; }
        public string Title { get; set; } = "";
        public string? Slug { get; set; }
        public int OrderIndex { get; set; }
        public int Depth { get; set; }
        public string MaterializedPath { get; set; } = "/";
        public bool IsFree { get; set; }
        public bool IsHidden { get; set; }
        public int? DurationMinutes { get; set; }
        public List<ContentNodeDto> Children { get; set; } = new();
    }

    public class ContentNodeDetailDto : ContentNodeDto
    {
        public List<ContentBlockDto> Blocks { get; set; } = new();
        public List<LessonResourceDto> Resources { get; set; } = new();
        public List<FlashcardDeckDto> FlashcardDecks { get; set; } = new();
    }

    public class CreateContentNodeDto
    {
        public int? ParentNodeId { get; set; }
        [Required] public NodeType NodeType { get; set; }
        [Required, MaxLength(255)] public string Title { get; set; } = "";
        [MaxLength(255)] public string? Slug { get; set; }
        public int? OrderIndex { get; set; }
        public bool IsFree { get; set; }
        public int? DurationMinutes { get; set; }
    }

    public class UpdateContentNodeDto
    {
        [MaxLength(255)] public string? Title { get; set; }
        [MaxLength(255)] public string? Slug { get; set; }
        public bool? IsFree { get; set; }
        public bool? IsHidden { get; set; }
        public int? DurationMinutes { get; set; }
    }

    public class ReorderNodesDto
    {
        /// <summary>Sibling node ids in the desired order (must share one parent).</summary>
        [Required] public List<int> OrderedNodeIds { get; set; } = new();
    }

    public class MoveNodeDto
    {
        /// <summary>New parent; null moves the node to the version root.</summary>
        public int? NewParentNodeId { get; set; }
        public int? OrderIndex { get; set; }
    }

    public class NodeRevisionDto
    {
        public long RevisionId { get; set; }
        public int NodeId { get; set; }
        public int RevisionNumber { get; set; }
        public string? Snapshot { get; set; }
        public int EditedBy { get; set; }
        public DateTime EditedAt { get; set; }
    }

    // ---------------- ContentBlock ----------------
    public class ContentBlockDto
    {
        public int BlockId { get; set; }
        public int NodeId { get; set; }
        public LessonBlockType BlockType { get; set; }
        public string? ContentText { get; set; }
        public string? ContentUrl { get; set; }
        public string? MetadataJson { get; set; }
        public int OrderIndex { get; set; }
    }

    public class ContentBlockRequestDto
    {
        [Required] public LessonBlockType BlockType { get; set; }
        public string? ContentText { get; set; }
        [MaxLength(500)] public string? ContentUrl { get; set; }
        public string? MetadataJson { get; set; }
        public int? OrderIndex { get; set; }
    }

    // ---------------- LessonResource ----------------
    public class LessonResourceDto
    {
        public int ResourceId { get; set; }
        public int NodeId { get; set; }
        public string Title { get; set; } = "";
        public ResourceType ResourceType { get; set; }
        public int? MediaAssetId { get; set; }
        public string? ExternalUrl { get; set; }
        public bool IsDownloadable { get; set; }
        public int OrderIndex { get; set; }
    }

    public class LessonResourceRequestDto
    {
        [Required, MaxLength(255)] public string Title { get; set; } = "";
        [Required] public ResourceType ResourceType { get; set; }
        public int? MediaAssetId { get; set; }
        [MaxLength(1000)] public string? ExternalUrl { get; set; }
        public bool IsDownloadable { get; set; } = true;
        public int? OrderIndex { get; set; }
    }

    // ---------------- Flashcards ----------------
    public class FlashcardDeckDto
    {
        public int DeckId { get; set; }
        public int NodeId { get; set; }
        public string Title { get; set; } = "";
        public List<FlashcardDto> Cards { get; set; } = new();
    }

    public class FlashcardDeckRequestDto
    {
        [Required, MaxLength(255)] public string Title { get; set; } = "";
    }

    public class FlashcardDto
    {
        public int CardId { get; set; }
        public int DeckId { get; set; }
        public string FrontText { get; set; } = "";
        public string BackText { get; set; } = "";
        public string? FrontImageUrl { get; set; }
        public string? BackImageUrl { get; set; }
        public string? Hint { get; set; }
        public int OrderIndex { get; set; }
    }

    public class FlashcardRequestDto
    {
        [Required] public string FrontText { get; set; } = "";
        [Required] public string BackText { get; set; } = "";
        [MaxLength(500)] public string? FrontImageUrl { get; set; }
        [MaxLength(500)] public string? BackImageUrl { get; set; }
        public string? Hint { get; set; }
        public int? OrderIndex { get; set; }
    }
}
