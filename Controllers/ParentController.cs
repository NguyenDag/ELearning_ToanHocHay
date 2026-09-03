using ELearning_ToanHocHay_Control.Attributes;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.Parent;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/parents")]
    [ApiController]
    [Authorize]
    public class ParentController : ControllerBase
    {
        private readonly IParentService _service;
        private readonly IParentLinkService _links;

        public ParentController(IParentService service, IParentLinkService links)
        {
            _service = service;
            _links = links;
        }

        // The parent themselves (parent_id in the token) or an admin.
        private bool CanAccess(int parentId)
            => User.IsSystemAdmin() || User.GetParentId() == parentId;

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (!CanAccess(id)) return this.Forbidden();
            return (await _service.GetByIdAsync(id)).ToActionResult();
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateParentDto dto)
        {
            if (!CanAccess(id)) return this.Forbidden();
            return (await _service.UpdateAsync(id, dto)).ToActionResult();
        }

        [HttpDelete("{id:int}")]
        [AuthorizeUserType(UserType.SystemAdmin)]
        public async Task<IActionResult> Delete(int id)
            => (await _service.DeleteAsync(id)).ToActionResult();

        // ---------------- P6 — parent ⇄ child linking ----------------

        /// <summary>Parent creates a one-time invite code to share with a child.</summary>
        [HttpPost("{id:int}/invites")]
        public async Task<IActionResult> CreateInvite(int id, [FromBody] CreateParentInviteDto dto)
        {
            if (!CanAccess(id)) return this.Forbidden();
            var r = await _links.CreateInviteAsync(id, dto);
            return r.ToActionResult();
        }

        /// <summary>Student links to a parent using an invite code or the parent's connection code.</summary>
        [HttpPost("link")]
        public async Task<IActionResult> LinkByCode([FromBody] LinkParentDto dto)
        {
            var studentId = User.GetStudentId();
            if (studentId == null) return this.Forbidden("Only a student can accept a parent link");
            var r = await _links.LinkByCodeAsync(studentId.Value, dto);
            return r.ToActionResult();
        }

        [HttpGet("{id:int}/children")]
        public async Task<IActionResult> GetChildren(int id)
        {
            if (!CanAccess(id)) return this.Forbidden();
            return (await _links.GetLinksAsync(id)).ToActionResult();
        }

        [HttpDelete("{id:int}/children/{studentId:int}")]
        public async Task<IActionResult> RevokeChild(int id, int studentId)
        {
            if (!CanAccess(id)) return this.Forbidden();
            var r = await _links.RevokeAsync(id, studentId);
            return r.ToActionResult();
        }

        [HttpGet("{id:int}/children/overview")]
        public async Task<IActionResult> ChildrenOverview(int id)
        {
            if (!CanAccess(id)) return this.Forbidden();
            return (await _links.GetChildrenOverviewAsync(id)).ToActionResult();
        }
    }
}
