using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Refund;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ELearning_ToanHocHay_Control.Controllers
{
    /// <summary>
    /// Pha 2 — hoàn tiền bán tự động, phía người dùng: học sinh / phụ huynh tự gửi yêu cầu
    /// hoàn tiền cho giao dịch của chính mình và theo dõi trạng thái.
    /// Finance duyệt / gộp lô / xuất CSV ở <c>/api/finance/refunds</c>.
    /// </summary>
    [Route("api/refunds")]
    [ApiController]
    [Authorize]
    public class RefundsController : ControllerBase
    {
        private readonly IRefundService _service;

        public RefundsController(IRefundService service)
        {
            _service = service;
        }

        // POST: api/refunds — gửi yêu cầu hoàn tiền cho một giao dịch của mình
        [HttpPost]
        [EnableRateLimiting("refund")]
        public async Task<IActionResult> Create([FromBody] CreateRefundRequestDto dto)
        {
            var response = await _service.CreateAsync(dto, User);
            return response.ToActionResult();
        }

        // GET: api/refunds/me — các yêu cầu hoàn tiền của mình (là người thụ hưởng hoặc người tạo)
        [HttpGet("me")]
        public async Task<IActionResult> GetMine([FromQuery] PagedRequest request)
        {
            var userId = User.GetUserId();
            if (userId == null) return this.Forbidden("Token không hợp lệ");
            var response = await _service.GetMineAsync(userId.Value, request);
            return response.ToActionResult();
        }

        // GET: api/refunds/5 — chi tiết + timeline sự kiện (chỉ chủ sở hữu / Finance)
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _service.GetByIdAsync(id, User);
            return response.ToActionResult();
        }
    }
}
