using ELearning_ToanHocHay_Control.Attributes;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.Catalog;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    /// <summary>
    /// A3/P2 — catalog layer. Active rows are public (course browsing); inactive rows and
    /// all writes are limited to the content-management roles.
    /// </summary>
    [Route("api/catalog")]
    [ApiController]
    public class CatalogController : ControllerBase
    {
        private readonly ICatalogService _catalog;

        public CatalogController(ICatalogService catalog)
        {
            _catalog = catalog;
        }

        private bool CanSeeInactive =>
            User.HasUserType(UserType.ContentEditor, UserType.AcademicReviewer, UserType.SystemAdmin);

        // ---------------- Subjects ----------------
        [HttpGet("subjects")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSubjects([FromQuery] bool includeInactive = false)
            => Ok(await _catalog.GetSubjectsAsync(includeInactive && CanSeeInactive));

        [HttpGet("subjects/{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSubject(int id)
        {
            var r = await _catalog.GetSubjectAsync(id);
            if (!r.Success) return NotFound(r);
            if (!r.Data!.IsActive && !CanSeeInactive) return NotFound(r);
            return Ok(r);
        }

        [HttpPost("subjects")]
        [AuthorizeContentRole]
        public async Task<IActionResult> CreateSubject([FromBody] SubjectRequestDto dto)
        {
            var r = await _catalog.CreateSubjectAsync(dto);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        [HttpPut("subjects/{id:int}")]
        [AuthorizeContentRole]
        public async Task<IActionResult> UpdateSubject(int id, [FromBody] SubjectRequestDto dto)
        {
            var r = await _catalog.UpdateSubjectAsync(id, dto);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        // ---------------- Grade levels ----------------
        [HttpGet("grade-levels")]
        [AllowAnonymous]
        public async Task<IActionResult> GetGradeLevels([FromQuery] bool includeInactive = false)
            => Ok(await _catalog.GetGradeLevelsAsync(includeInactive && CanSeeInactive));

        [HttpGet("grade-levels/{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetGradeLevel(int id)
        {
            var r = await _catalog.GetGradeLevelAsync(id);
            if (!r.Success) return NotFound(r);
            if (!r.Data!.IsActive && !CanSeeInactive) return NotFound(r);
            return Ok(r);
        }

        [HttpPost("grade-levels")]
        [AuthorizeContentRole]
        public async Task<IActionResult> CreateGradeLevel([FromBody] GradeLevelRequestDto dto)
        {
            var r = await _catalog.CreateGradeLevelAsync(dto);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        [HttpPut("grade-levels/{id:int}")]
        [AuthorizeContentRole]
        public async Task<IActionResult> UpdateGradeLevel(int id, [FromBody] GradeLevelRequestDto dto)
        {
            var r = await _catalog.UpdateGradeLevelAsync(id, dto);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        // ---------------- Curriculum frameworks ----------------
        [HttpGet("frameworks")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFrameworks([FromQuery] bool includeInactive = false)
            => Ok(await _catalog.GetFrameworksAsync(includeInactive && CanSeeInactive));

        [HttpGet("frameworks/{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFramework(int id)
        {
            var r = await _catalog.GetFrameworkAsync(id);
            if (!r.Success) return NotFound(r);
            if (!r.Data!.IsActive && !CanSeeInactive) return NotFound(r);
            return Ok(r);
        }

        [HttpPost("frameworks")]
        [AuthorizeContentRole]
        public async Task<IActionResult> CreateFramework([FromBody] FrameworkRequestDto dto)
        {
            var r = await _catalog.CreateFrameworkAsync(dto);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        [HttpPut("frameworks/{id:int}")]
        [AuthorizeContentRole]
        public async Task<IActionResult> UpdateFramework(int id, [FromBody] FrameworkRequestDto dto)
        {
            var r = await _catalog.UpdateFrameworkAsync(id, dto);
            return r.Success ? Ok(r) : BadRequest(r);
        }
    }
}
