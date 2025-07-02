using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgriculturalExpertsController : ControllerBase
    {
        private readonly IAgriculturalExpertService _agriculturalExpertService;

        public AgriculturalExpertsController(IAgriculturalExpertService agriculturalExpertService)
        {
            _agriculturalExpertService = agriculturalExpertService;
        }

        // GET: api/agriculturalexperts
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _agriculturalExpertService.GetAllAsync();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // GET: api/agriculturalexperts/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _agriculturalExpertService.GetByIdAsync(id);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }
    }
}
