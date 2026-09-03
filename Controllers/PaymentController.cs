using ELearning_ToanHocHay_Control.Attributes;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.Payment;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/payments")]
    [ApiController]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _service;
        private readonly IResourceAccessService _access;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IPaymentService service, IResourceAccessService access, ILogger<PaymentController> logger)
        {
            _service = service;
            _access = access;
            _logger = logger;
        }

        // GET: api/payment — all financial data, Finance/Admin only (paged, ?status=)
        [HttpGet]
        [AuthorizeUserType(UserType.FinanceManager, UserType.SystemAdmin)]
        public async Task<IActionResult> GetAll([FromQuery] PagedRequest request, [FromQuery] PaymentStatus? status)
        {
            var response = await _service.GetPagedAsync(request, status);
            return response.ToActionResult();
        }

        // GET: api/payment/me — the caller's own payment history (payer or beneficiary)
        [HttpGet("me")]
        public async Task<IActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = User.GetUserId();
            if (userId == null) return Unauthorized();
            return Ok(await _service.GetMyPaymentsAsync(userId.Value, page, pageSize));
        }

        // GET: api/payment/5 — payer / beneficiary / Finance / Admin
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (!await _access.CanAccessPaymentAsync(User, id))
                return this.Forbidden();

            var response = await _service.GetByIdAsync(id);
            return response.ToActionResult();
        }

        // PUT: api/payment/update-status/5 — Finance/Admin only
        [HttpPut("update-status/{id:int}")]
        [AuthorizeUserType(UserType.FinanceManager, UserType.SystemAdmin)]
        public async Task<IActionResult> UpdateStatus(int id, UpdatePaymentStatusDto dto)
        {
            var response = await _service.UpdateStatusAsync(id, dto);
            return response.ToActionResult();
        }

        // POST: api/payment/5/refund — Finance/Admin only
        [HttpPost("{id:int}/refund")]
        [AuthorizeUserType(UserType.FinanceManager, UserType.SystemAdmin)]
        public async Task<IActionResult> Refund(int id, [FromBody] RefundPaymentDto dto)
        {
            var response = await _service.RefundAsync(id, dto);
            return response.ToActionResult();
        }
    }
}
