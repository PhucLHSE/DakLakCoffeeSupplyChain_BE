using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcessingParametersController : ControllerBase
    {
        private readonly IProcessingParameterService _processingParameterService;

        public ProcessingParametersController(IProcessingParameterService processingParameterService)
        {
            _processingParameterService = processingParameterService;
        }

        [HttpGet]
        //[Authorize(Roles = "Admin,BusinessManager,AgriculturalExpert,BusinessStaff,Farmer")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _processingParameterService.GetAll();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

    }
}
