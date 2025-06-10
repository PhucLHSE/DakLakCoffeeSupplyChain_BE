using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class CropStagesController : ControllerBase
{
    private readonly ICropStageService _cropStageService;

    public CropStagesController(ICropStageService service)
    {
        _cropStageService = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _cropStageService.GetAll();

        if (result.Status == Const.SUCCESS_READ_CODE)
            return Ok(result.Data);

        if (result.Status == Const.WARNING_NO_DATA_CODE)
            return NotFound(result.Message);

        return StatusCode(500, result.Message);
    }
    [HttpGet("{stageId}")]
    public async Task<IActionResult> GetById(int stageId)
    {
        var result = await _cropStageService.GetById(stageId);

        if (result.Status == Const.SUCCESS_READ_CODE)
            return Ok(result.Data);            

        if (result.Status == Const.WARNING_NO_DATA_CODE)
            return NotFound(result.Message);    

        return StatusCode(500, result.Message);  
    }

}
