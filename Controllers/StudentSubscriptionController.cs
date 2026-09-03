using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Services.Implementations;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/students/{studentId:int}")]
    [ApiController]
    [Authorize]
    public class StudentSubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IResourceAccessService _access;

        public StudentSubscriptionController(ISubscriptionService subscriptionService, IResourceAccessService access)
        {
            _subscriptionService = subscriptionService;
            _access = access;
        }

        /// <summary>
        /// GET /api/student/{studentId}/subscription/current
        /// Returns the current subscription info (Free when there is no active subscription).
        /// </summary>
        [HttpGet("subscription/current")]
        public async Task<IActionResult> GetCurrentSubscription(int studentId)
        {
            if (!await _access.CanAccessStudentAsync(User, studentId))
                return this.Forbidden();

            var info = await _subscriptionService.GetActiveSubscriptionInfoAsync(studentId);

            // Always return 200 — Free is a valid state, not an error
            return Ok(new
            {
                success = true,
                data = info,
                message = info.IsActive
                    ? $"Đang dùng gói {info.PackageName}"
                    : "Đang dùng gói Free"
            });
        }
    }
}