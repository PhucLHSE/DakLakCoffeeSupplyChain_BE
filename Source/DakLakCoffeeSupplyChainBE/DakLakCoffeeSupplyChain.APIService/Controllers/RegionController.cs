using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.RegionsDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegionController : ControllerBase
    {
        [HttpGet("wards")]
        [EnableQuery]
        public async Task<IActionResult> GetAllRegionsAsync()
        {
            using var http = new HttpClient();
            var response = await http.GetFromJsonAsync<RegionDto>(
                "https://provinces.open-api.vn/api/v2/p/66?depth=2"
            );
            if (response?.Wards != null)
                return Ok(response.Wards);              // Trả đúng dữ liệu
            return NotFound(Const.FAIL_READ_MSG);     // Trả 404 + message
        }
    }
}
