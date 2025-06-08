using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcurementPlansController : ControllerBase
    {
        private readonly IProcurementPlanService _procurementPlanService;

        public ProcurementPlansController(IProcurementPlanService procurementPlanService)
            => _procurementPlanService = procurementPlanService;

        // GET: api/<ProcurementPlansController>
        [HttpGet]
        public async Task<IActionResult> GetAllProcurementPlansAsync()
        {
            var result = await _procurementPlanService.GetAll();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả đúng dữ liệu

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 + message

            return StatusCode(500, result.Message);  // Trả 500 + message
        }
    }
}
