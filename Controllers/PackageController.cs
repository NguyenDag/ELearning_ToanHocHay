using ELearning_ToanHocHay_Control.Attributes;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.Package;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
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

        // GET: api/package — pricing page, allowed anonymously
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var response = await _packageService.GetAllAsync();

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        // GET: api/package/5
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _packageService.GetByIdAsync(id);

            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }

        // POST: api/package — Finance/Admin only
        [HttpPost]
        [AuthorizeUserType(UserType.FinanceManager, UserType.SystemAdmin)]
        public async Task<IActionResult> Create([FromBody] CreateOrUpdatePackageDto dto)
        {
            var userId = User.GetUserId();
            if (userId == null)
                return this.Forbidden();

            var response = await _packageService.CreateAsync(userId.Value, dto);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        // PUT: api/package/5 — Finance/Admin only
        [HttpPut("{id:int}")]
        [AuthorizeUserType(UserType.FinanceManager, UserType.SystemAdmin)]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] CreateOrUpdatePackageDto dto)
        {
            var response = await _packageService.UpdateAsync(id, dto);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        // DELETE: api/package/5 — Finance/Admin only
        [HttpDelete("{id:int}")]
        [AuthorizeUserType(UserType.FinanceManager, UserType.SystemAdmin)]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _packageService.DeleteAsync(id);

            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
    }
}
