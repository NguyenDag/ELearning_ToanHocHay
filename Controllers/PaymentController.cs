using ELearning_ToanHocHay_Control.Models.DTOs.Payment;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _service;

        public PaymentController(IPaymentService service)
        {
            _service = service;
        }

        // GET: api/payment
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await _service.GetAllAsync();
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        // GET: api/payment/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _service.GetByIdAsync(id);
            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }

        // POST: api/payment
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePaymentDto dto)
        {
            var response = await _service.CreateAsync(dto);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        // PUT: api/payment/update-status/5
        [HttpPut("update-status/{id:int}")]
        public async Task<IActionResult> UpdateStatus(int id, UpdatePaymentStatusDto dto)
        {
            var response = await _service.UpdateStatusAsync(id, dto);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
    }
}
