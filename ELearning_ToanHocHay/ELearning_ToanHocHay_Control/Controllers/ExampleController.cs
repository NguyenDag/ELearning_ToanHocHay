using ELearning_ToanHocHay_Control.Attributes;
using ELearning_ToanHocHay_Control.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    /// <summary>
    /// Ví dụ controller sử dụng AuthorizeUserType attribute
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Yêu cầu authentication cho toàn bộ controller
    public class ExampleController : ControllerBase
    {
        /// <summary>
        /// Endpoint chỉ dành cho Student
        /// </summary>
        [HttpGet("student-only")]
        [AuthorizeUserType(UserType.Student)]
        public IActionResult StudentOnly()
        {
            return Ok(new
            {
                message = "Chỉ Student mới truy cập được endpoint này"
            });
        }

        /// <summary>
        /// Endpoint dành cho Student và Parent
        /// </summary>
        [HttpGet("student-or-parent")]
        [AuthorizeUserType(UserType.Student, UserType.Parent)]
        public IActionResult StudentOrParent()
        {
            return Ok(new
            {
                message = "Student hoặc Parent có thể truy cập endpoint này"
            });
        }

        /// <summary>
        /// Endpoint chỉ dành cho Admin
        /// </summary>
        [HttpGet("admin-only")]
        [AuthorizeUserType(UserType.SystemAdmin)]
        public IActionResult AdminOnly()
        {
            return Ok(new
            {
                message = "Chỉ SystemAdmin mới truy cập được endpoint này"
            });
        }

        /// <summary>
        /// Endpoint dành cho Content Editor và Academic Reviewer
        /// </summary>
        [HttpGet("content-management")]
        [AuthorizeUserType(UserType.ContentEditor, UserType.AcademicReviewer)]
        public IActionResult ContentManagement()
        {
            return Ok(new
            {
                message = "ContentEditor và AcademicReviewer có thể truy cập endpoint này"
            });
        }

        /// <summary>
        /// Endpoint public (không cần authentication)
        /// </summary>
        [HttpGet("public")]
        [AllowAnonymous]
        public IActionResult PublicEndpoint()
        {
            return Ok(new
            {
                message = "Endpoint này public, ai cũng truy cập được"
            });
        }

        /// <summary>
        /// Endpoint yêu cầu authentication nhưng không giới hạn UserType
        /// </summary>
        [HttpGet("authenticated-only")]
        public IActionResult AuthenticatedOnly()
        {
            var userName = User.Identity?.Name;
            var userType = User.FindFirst("UserType")?.Value;

            return Ok(new
            {
                message = "Chỉ cần đăng nhập là truy cập được",
                userName,
                userType
            });
        }
    }
}
