using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.Authorization;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContractDeliveryBatchsController : ControllerBase
    {
        private readonly IContractDeliveryBatchService _contractDeliveryBatchService;

        public ContractDeliveryBatchsController(IContractDeliveryBatchService contractDeliveryBatchService)
            => _contractDeliveryBatchService = contractDeliveryBatchService;

        // GET: api/<ContractDeliveryBatchsController>
        [HttpGet]
        [EnableQuery]
        [Authorize(Roles = "BusinessManager,BusinessStaff")]
        public async Task<IActionResult> GetAllContractDeliveryBatchsAsync()
        {
            Guid userId;

            try
            {
                // Lấy userId từ token qua ClaimsHelper
                userId = User.GetUserId();
            }
            catch
            {
                return Unauthorized("Không xác định được userId từ token.");
            }

            var result = await _contractDeliveryBatchService.GetAll(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }
    }
}
