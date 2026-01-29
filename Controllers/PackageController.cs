using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PackageController : ControllerBase
    {
        private readonly IPackageService _packageService;

        public PackageController(IPackageService packageService)
        {
            _packageService = packageService;
        }

        // GET: api/package
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await _packageService.GetAllAsync();

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        // GET: api/package/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _packageService.GetByIdAsync(id);

            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }

        // POST: api/package
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromQuery] int userId,
            [FromBody] CreateOrUpdatePackageDto dto)
        {
            var response = await _packageService.CreateAsync(userId, dto);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        // PUT: api/package/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] CreateOrUpdatePackageDto dto)
        {
            var response = await _packageService.UpdateAsync(id, dto);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        // DELETE: api/package/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _packageService.DeleteAsync(id);

            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
    }
}
