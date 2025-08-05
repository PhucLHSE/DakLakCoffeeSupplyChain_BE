using DakLakCoffeeSupplyChain.Common.DTOs.MediaDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace DakLakCoffeeSupplyChain.API.Controllers
{
    [Route("api/media")]
    [ApiController]
    public class MediaController : ControllerBase
    {
        private readonly IMediaService _mediaService;

        public MediaController(IMediaService mediaService)
        {
            _mediaService = mediaService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] MediaUploadRequest request)
        {
            if (request.Files == null || request.Files.Count == 0)
                return BadRequest("No files uploaded");

            var userId = "test-user"; 

            var result = await _mediaService.UploadAndSaveMediaAsync(
                request.Files,
                request.RelatedEntity,
                request.RelatedId,
                userId
            );

            return Ok(result);
        }
    }
}
