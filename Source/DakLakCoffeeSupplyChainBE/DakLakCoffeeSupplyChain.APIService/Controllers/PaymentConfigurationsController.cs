using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentConfigurationsController : ControllerBase
    {
        private readonly IPaymentConfigurationService _paymentConfigurationService;

        public PaymentConfigurationsController(IPaymentConfigurationService paymentConfigurationService)
            => _paymentConfigurationService = paymentConfigurationService;

        // GET: api/<PaymentConfigurationsController>
        [HttpGet]
        [EnableQuery]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllPaymentConfigurationsAsync()
        {
            var result = await _paymentConfigurationService
                .GetAll();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }
    }
}
