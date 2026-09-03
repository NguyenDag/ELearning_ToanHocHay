using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Content;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    /// <summary>
    /// A3/P2 — authoring side of the content tree. All mutations require the owning
    /// CourseVersion to be in the Draft state.
    /// </summary>
    public interface IContentAuthoringService
    {
        // ----- tree / nodes -----
        Task<ApiResponse<List<ContentNodeDto>>> GetVersionTreeAsync(int courseVersionId);
        Task<ApiResponse<ContentNodeDetailDto>> GetNodeAsync(int nodeId);
        Task<ApiResponse<ContentNodeDto>> CreateNodeAsync(int courseVersionId, CreateContentNodeDto dto, int userId);
        Task<ApiResponse<ContentNodeDto>> UpdateNodeAsync(int nodeId, UpdateContentNodeDto dto, int userId);
        Task<ApiResponse<bool>> DeleteNodeAsync(int nodeId);
        Task<ApiResponse<bool>> ReorderChildrenAsync(int courseVersionId, int? parentNodeId, ReorderNodesDto dto);
        Task<ApiResponse<ContentNodeDto>> MoveNodeAsync(int nodeId, MoveNodeDto dto, int userId);

        // ----- revisions -----
        Task<ApiResponse<List<NodeRevisionDto>>> GetRevisionsAsync(int nodeId);
        Task<ApiResponse<ContentNodeDto>> RestoreRevisionAsync(int nodeId, int revisionNumber, int userId);

        // ----- blocks -----
        Task<ApiResponse<ContentBlockDto>> AddBlockAsync(int nodeId, ContentBlockRequestDto dto);
        Task<ApiResponse<ContentBlockDto>> UpdateBlockAsync(int blockId, ContentBlockRequestDto dto);
        Task<ApiResponse<bool>> DeleteBlockAsync(int blockId);

        // ----- resources -----
        Task<ApiResponse<LessonResourceDto>> AddResourceAsync(int nodeId, LessonResourceRequestDto dto);
        Task<ApiResponse<LessonResourceDto>> UpdateResourceAsync(int resourceId, LessonResourceRequestDto dto);
        Task<ApiResponse<bool>> DeleteResourceAsync(int resourceId);

        // ----- flashcards -----
        Task<ApiResponse<FlashcardDeckDto>> AddDeckAsync(int nodeId, FlashcardDeckRequestDto dto);
        Task<ApiResponse<bool>> DeleteDeckAsync(int deckId);
        Task<ApiResponse<FlashcardDto>> AddCardAsync(int deckId, FlashcardRequestDto dto);
        Task<ApiResponse<FlashcardDto>> UpdateCardAsync(int cardId, FlashcardRequestDto dto);
        Task<ApiResponse<bool>> DeleteCardAsync(int cardId);
    }
}
