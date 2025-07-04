﻿using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FarmingCommitmentController(IFarmingCommitmentService farmingCommitmentService) : ControllerBase
    {
        private readonly IFarmingCommitmentService _service = farmingCommitmentService;

        // GET api/<FarmingCommitments>/BusinessManager
        [HttpGet("BusinessManger")]
        [EnableQuery]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> GetAllBusinessManagerCommitment()
        {
            Guid userId;

            try
            {
                // Lấy userId từ token qua ClaimsHelper
                userId = User.GetUserId();
            }
            catch
            {
                return Unauthorized("Không xác định được người đăng nhập từ token.");
            }

            var result = await _service.GetAllBusinessManagerCommitment(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả object chi tiết

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 nếu không tìm thấy

            return StatusCode(500, result.Message);  // Lỗi hệ thống
        }

        // GET api/<FarmingCommitments>/Farmer
        [HttpGet("Farmer")]
        [EnableQuery]
        [Authorize(Roles = "Farmer")]
        public async Task<IActionResult> GetAllFarmerCommitment()
        {
            Guid userId;

            try
            {
                // Lấy userId từ token qua ClaimsHelper
                userId = User.GetUserId();
            }
            catch
            {
                return Unauthorized("Không xác định được người đăng nhập từ token.");
            }

            var result = await _service.GetAllFarmerCommitment(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả object chi tiết

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 nếu không tìm thấy

            return StatusCode(500, result.Message);  // Lỗi hệ thống
        }

        // GET api/<FarmingCommitment>/{commitmentId}
        [HttpGet("{commitmentId}")]
        [EnableQuery]
        [Authorize(Roles = "Admin, BusinessManager")]
        public async Task<IActionResult> GetById(Guid commitmentId)
        {
            var result = await _service.GetById(commitmentId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả object chi tiết

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 nếu không tìm thấy

            return StatusCode(500, result.Message);  // Lỗi hệ thống
        }
    }
}
