using ELearning_ToanHocHay_Control.Models.DTOs.Subscription;
using ELearning_ToanHocHay_Control.Services.Implementations;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _service;
        private readonly ISubscriptionPaymentService _subscriptionPaymentService;
        private readonly ISePayService _sePayService;

        public SubscriptionController(ISubscriptionService service, ISubscriptionPaymentService subscriptionPaymentService, ISePayService sePayService)
        {
            _service = service;
            _subscriptionPaymentService = subscriptionPaymentService;
            _sePayService = sePayService;
        }

        // GET: api/subscription
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await _service.GetAllAsync();
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        // GET: api/subscription/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _service.GetByIdAsync(id);
            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateSubscriptionAndQr(CreateSubscriptionDto dto)
        {
            var result = await _subscriptionPaymentService.CreatePendingAsync(dto);

            if (!result.Success)
                return BadRequest(result);

            var qrUrl = _sePayService.GenerateQrUrl(result.Data, dto.AmountPaid);

            return Ok(new
            {
                subscriptionId = result.Data,
                qrUrl
            });
        }


        // PUT: api/subscription/cancel/5
        [HttpPut("cancel/{id:int}")]
        public async Task<IActionResult> Cancel(int id)
        {
            var response = await _service.CancelAsync(id);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        // GET: api/subscription/check-premium/10
        [HttpGet("check-premium/{studentId:int}")]
        public async Task<IActionResult> CheckPremium(int studentId)
        {
            var response = await _service.CheckPremiumAsync(studentId);
            return Ok(response);
        }
    }
}
