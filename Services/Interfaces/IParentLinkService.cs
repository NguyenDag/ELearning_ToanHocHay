using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Parent;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    /// <summary>P6 — parent ⇄ child linking (invites, connection code, revoke, overview).</summary>
    public interface IParentLinkService
    {
        Task<ApiResponse<ParentInviteDto>> CreateInviteAsync(int parentId, CreateParentInviteDto dto);
        Task<ApiResponse<ParentLinkDto>> LinkByCodeAsync(int studentId, LinkParentDto dto);
        Task<ApiResponse<List<ParentLinkDto>>> GetLinksAsync(int parentId);
        Task<ApiResponse<bool>> RevokeAsync(int parentId, int studentId);
        Task<ApiResponse<List<ChildOverviewDto>>> GetChildrenOverviewAsync(int parentId);
    }
}
