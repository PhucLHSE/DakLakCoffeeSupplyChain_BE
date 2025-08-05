using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadImageController : ControllerBase
    {
        private readonly IUploadService _uploadService;

        public UploadImageController(IUploadService uploadService)
        {
            _uploadService = uploadService;
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File không hợp lệ");

            var result = await _uploadService.UploadAsync(file);
            return Ok(result);
        }
    }
}
