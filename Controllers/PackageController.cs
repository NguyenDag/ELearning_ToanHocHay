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
    [Route("api/packages")]
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

            return response.ToActionResult();
        }

        // GET: api/package/5
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _packageService.GetByIdAsync(id);

            return response.ToActionResult();
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

            return response.ToActionResult();
        }

        // PUT: api/package/5 — Finance/Admin only
        [HttpPut("{id:int}")]
        [AuthorizeUserType(UserType.FinanceManager, UserType.SystemAdmin)]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] CreateOrUpdatePackageDto dto)
        {
            var response = await _packageService.UpdateAsync(id, dto);

            return response.ToActionResult();
        }

        // DELETE: api/package/5 — Finance/Admin only
        [HttpDelete("{id:int}")]
        [AuthorizeUserType(UserType.FinanceManager, UserType.SystemAdmin)]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _packageService.DeleteAsync(id);

            return response.ToActionResult();
        }
    }
}
