using ELearning_ToanHocHay_Control.Attributes;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.Course;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    /// <summary>
    /// A3/P2 — Course + CourseVersion. Browsing published courses is public; authoring and the
    /// Draft → InReview → Approved → Published workflow is limited to content-management roles.
    /// </summary>
    [Route("api/courses")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseService _courses;

        public CoursesController(ICourseService courses)
        {
            _courses = courses;
        }

        private bool IsContentRole =>
            User.HasUserType(UserType.ContentEditor, UserType.AcademicReviewer, UserType.SystemAdmin);

        // ---------------- Browse ----------------
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCourses(
            [FromQuery] int? subjectId, [FromQuery] int? gradeLevelId, [FromQuery] bool includeUnpublished = false)
        {
            var publishedOnly = !(includeUnpublished && IsContentRole);
            return Ok(await _courses.GetCoursesAsync(subjectId, gradeLevelId, publishedOnly));
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCourse(int id)
        {
            var r = await _courses.GetCourseAsync(id);
            if (!r.Success) return NotFound(r);
            if (r.Data!.Status != CourseStatus.Published && !IsContentRole) return NotFound(r);
            return Ok(r);
        }

        [HttpGet("by-slug/{slug}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCourseBySlug(string slug)
        {
            var r = await _courses.GetCourseBySlugAsync(slug);
            if (!r.Success) return NotFound(r);
            if (r.Data!.Status != CourseStatus.Published && !IsContentRole) return NotFound(r);
            return Ok(r);
        }

        // ---------------- Authoring ----------------
        [HttpPost]
        [AuthorizeContentRole]
        public async Task<IActionResult> CreateCourse([FromBody] CourseRequestDto dto)
        {
            var r = await _courses.CreateCourseAsync(dto, User.GetUserId()!.Value);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        [HttpPut("{id:int}")]
        [AuthorizeContentRole]
        public async Task<IActionResult> UpdateCourse(int id, [FromBody] CourseRequestDto dto)
        {
            var r = await _courses.UpdateCourseAsync(id, dto);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        [HttpPost("{id:int}/archive")]
        [AuthorizeContentRole]
        public async Task<IActionResult> ArchiveCourse(int id)
        {
            var r = await _courses.SetCourseArchivedAsync(id, true);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        [HttpPost("{id:int}/unarchive")]
        [AuthorizeContentRole]
        public async Task<IActionResult> UnarchiveCourse(int id)
        {
            var r = await _courses.SetCourseArchivedAsync(id, false);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        // ---------------- Versions ----------------
        [HttpGet("{courseId:int}/versions")]
        [AuthorizeContentRole]
        public async Task<IActionResult> GetVersions(int courseId)
        {
            var r = await _courses.GetVersionsAsync(courseId);
            return r.Success ? Ok(r) : NotFound(r);
        }

        [HttpPost("{courseId:int}/versions")]
        [AuthorizeContentRole]
        public async Task<IActionResult> CreateVersion(int courseId, [FromBody] CreateCourseVersionDto dto)
        {
            var r = await _courses.CreateVersionAsync(courseId, dto, User.GetUserId()!.Value);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        [HttpPost("versions/{versionId:int}/submit")]
        [AuthorizeContentRole]
        public async Task<IActionResult> SubmitVersion(int versionId)
        {
            var r = await _courses.SubmitVersionAsync(versionId, User.GetUserId()!.Value);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        [HttpPost("versions/{versionId:int}/review")]
        [AuthorizeUserType(UserType.AcademicReviewer, UserType.SystemAdmin)]
        public async Task<IActionResult> ReviewVersion(int versionId, [FromBody] ReviewCourseVersionDto dto)
        {
            var r = await _courses.ReviewVersionAsync(versionId, dto, User.GetUserId()!.Value);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        [HttpPost("versions/{versionId:int}/publish")]
        [AuthorizeUserType(UserType.AcademicReviewer, UserType.SystemAdmin)]
        public async Task<IActionResult> PublishVersion(int versionId)
        {
            var r = await _courses.PublishVersionAsync(versionId, User.GetUserId()!.Value);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        [HttpPost("versions/{versionId:int}/archive")]
        [AuthorizeContentRole]
        public async Task<IActionResult> ArchiveVersion(int versionId)
        {
            var r = await _courses.ArchiveVersionAsync(versionId);
            return r.Success ? Ok(r) : BadRequest(r);
        }
    }
}
