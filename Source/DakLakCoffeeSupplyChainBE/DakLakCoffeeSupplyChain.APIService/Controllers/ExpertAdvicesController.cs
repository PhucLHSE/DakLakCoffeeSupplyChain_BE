using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExpertAdvicesController : ControllerBase
    {
        private readonly IExpertAdviceService _expertAdviceService;

        public ExpertAdvicesController(IExpertAdviceService expertAdviceService)
            => _expertAdviceService = expertAdviceService;

        // GET: api/ExpertAdvices
        [HttpGet]
        public async Task<IActionResult> GetAllExpertAdvicesAsync()
        {
            var result = await _expertAdviceService.GetAllAsync();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // GET: api/ExpertAdvices/{adviceId}
        [HttpGet("{adviceId}")]
        public async Task<IActionResult> GetExpertAdviceByIdAsync(Guid adviceId)
        {
            var result = await _expertAdviceService.GetByIdAsync(adviceId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }
    }
}
