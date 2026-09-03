using ELearning_ToanHocHay_Control.Attributes;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Subscription;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/subscriptions")]
    [ApiController]
    [Authorize]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _service;
        private readonly ISubscriptionPaymentService _subscriptionPaymentService;
        private readonly ISePayService _sePayService;
        private readonly IResourceAccessService _access;

        public SubscriptionController(
            ISubscriptionService service,
            ISubscriptionPaymentService subscriptionPaymentService,
            ISePayService sePayService,
            IResourceAccessService access)
        {
            _service = service;
            _subscriptionPaymentService = subscriptionPaymentService;
            _sePayService = sePayService;
            _access = access;
        }

        // GET: api/subscription — all financial data, Finance/Admin only (paged, ?status=)
        [HttpGet]
        [AuthorizeUserType(UserType.FinanceManager, UserType.SystemAdmin)]
        public async Task<IActionResult> GetAll([FromQuery] PagedRequest request, [FromQuery] SubscriptionStatus? status)
        {
            var response = await _service.GetPagedAsync(request, status);
            return response.ToActionResult();
        }

        // GET: api/subscription/me — the caller's current package (Free when none)
        [HttpGet("me")]
        public async Task<IActionResult> GetMine()
        {
            var studentId = User.GetStudentId();
            if (studentId == null)
                return this.Forbidden("Only students have a personal subscription — parents use /api/student/{id}/subscription/current");

            var info = await _service.GetActiveSubscriptionInfoAsync(studentId.Value);
            return Ok(ApiResponse<SubscriptionInfoDto>.SuccessResponse(info,
                info.IsActive ? $"Đang dùng gói {info.PackageName}" : "Đang dùng gói Free"));
        }

        // GET: api/subscription/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (!await _access.CanAccessSubscriptionAsync(User, id))
                return this.Forbidden();

            var response = await _service.GetByIdAsync(id);
            return response.ToActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> CreateSubscriptionAndQr(CreateSubscriptionDto dto)
        {
            if (!await _access.CanAccessStudentAsync(User, dto.StudentId))
                return this.Forbidden("You cannot create a subscription for this student");

            var payerId = User.GetUserId();
            if (payerId == null) return this.Forbidden();

            var result = await _subscriptionPaymentService.CreatePendingAsync(dto, payerId.Value);
            if (!result.Success)
                return result.ToActionResult();

            var qrUrl = _sePayService.GenerateQrUrl(result.Data.SubscriptionId, result.Data.Amount);
            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                subscriptionId = result.Data.SubscriptionId,
                amount = result.Data.Amount,
                qrUrl
            }, "Đã tạo yêu cầu thanh toán"));
        }


        // PUT: api/subscription/cancel/5
        [HttpPut("cancel/{id:int}")]
        public async Task<IActionResult> Cancel(int id)
        {
            if (!await _access.CanAccessSubscriptionAsync(User, id))
                return this.Forbidden();

            var response = await _service.CancelAsync(id);
            return response.ToActionResult();
        }

        // GET: api/subscription/check-premium/10
        [HttpGet("check-premium/{studentId:int}")]
        public async Task<IActionResult> CheckPremium(int studentId)
        {
            if (!await _access.CanAccessStudentAsync(User, studentId))
                return this.Forbidden();

            var response = await _service.CheckPremiumAsync(studentId);
            return response.ToActionResult();
        }

        [HttpGet("status/{id:int}")]
        public async Task<IActionResult> GetStatus(int id)
        {
            if (!await _access.CanAccessSubscriptionAsync(User, id))
                return this.Forbidden();

            var response = await _service.GetByIdAsync(id);
            if (!response.Success || response.Data == null)
                return response.ToActionResult();

            var sub = response.Data;
            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                status = sub.Status.ToString(),
                endDate = sub.Status == SubscriptionStatus.Active ? sub.EndDate.ToString("dd/MM/yyyy") : null
            }));
        }

        /// <summary>
        /// PATCH api/subscription/{id}/status — Finance/Admin only (to be replaced by the IPN flow in P5).
        /// Body: { "status": "Active" | "Expired" | "Cancelled" }
        /// </summary>
        [HttpPatch("{id:int}/status")]
        [AuthorizeUserType(UserType.FinanceManager, UserType.SystemAdmin)]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateSubscriptionStatusDto dto)
        {
            var response = await _service.UpdateStatusAsync(id, dto.Status);
            return response.ToActionResult();
        }
    }
}
