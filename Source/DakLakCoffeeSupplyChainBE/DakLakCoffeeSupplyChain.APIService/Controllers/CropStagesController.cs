using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropStageDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CropStagesController : ControllerBase
{
    private readonly ICropStageService _cropStageService;

    public CropStagesController(ICropStageService service)
    {
        _cropStageService = service;
    }

    [HttpGet]
    [EnableQuery]
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

    [HttpPost]
    [Authorize(Roles = "Admin,AgriculturalExpert")]

    public async Task<IActionResult> CreateCropStage([FromBody] CropStageCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _cropStageService.Create(dto);

        if (result.Status == Const.SUCCESS_CREATE_CODE)
            return Ok(result.Data);

        if (result.Status == Const.FAIL_CREATE_CODE)
            return Conflict(result.Message);

        return StatusCode(500, result.Message);
    }

    [HttpPut("{stageId}")]
    [Authorize(Roles = "Admin,AgriculturalExpert")]
    public async Task<IActionResult> UpdateCropStage(int stageId, [FromBody] CropStageUpdateDto dto)
    {
        if (stageId != dto.StageId)
            return BadRequest("ID không khớp với nội dung cập nhật.");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _cropStageService.Update(dto);

        if (result.Status == Const.SUCCESS_UPDATE_CODE)
            return Ok(result.Data);

        if (result.Status == Const.FAIL_UPDATE_CODE)
            return Conflict(result.Message);

        if (result.Status == Const.WARNING_NO_DATA_CODE)
            return NotFound(result.Message);

        return StatusCode(500, result.Message);
    }

    [HttpDelete("{stageId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCropStage(int stageId)
    {
        var result = await _cropStageService.DeleteById(stageId); 
        if (result.Status == Const.SUCCESS_DELETE_CODE)
            return Ok(result.Message);

        if (result.Status == Const.WARNING_NO_DATA_CODE)
            return NotFound(result.Message);

        return StatusCode(500, result.Message);
    }

    [HttpPatch("{stageId}/soft-delete")]
    [Authorize(Roles = "Admin,BusinessManager")]
    public async Task<IActionResult> SoftDeleteCropStage(int stageId)
    {
        var result = await _cropStageService.SoftDeleteById(stageId); 

        if (result.Status == Const.SUCCESS_DELETE_CODE)
            return Ok(result.Message);

        if (result.Status == Const.WARNING_NO_DATA_CODE)
            return NotFound(result.Message);

        return StatusCode(500, result.Message);
    }

}
