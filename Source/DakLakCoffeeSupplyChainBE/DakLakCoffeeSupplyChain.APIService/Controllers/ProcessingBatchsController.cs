using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcessingBatchController : ControllerBase
    {
        private readonly IProcessingBatchService _processingbatchservice;

        public ProcessingBatchController(IProcessingBatchService processingbatchservice)
        {
            _processingbatchservice = processingbatchservice;
        }

        // GET: api/processing-batch
        
        [HttpGet]
        [EnableQuery]
        public async Task<IActionResult> GetAll()
        {
            var result = await _processingbatchservice.GetAll();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }
    }
}
