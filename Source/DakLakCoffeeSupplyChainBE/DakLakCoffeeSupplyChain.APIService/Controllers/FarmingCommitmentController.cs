﻿using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.FarmingCommitmentDTOs;
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
        [HttpGet("BusinessManager")]
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

        // GET api/FarmingCommitment/Farmer/AvailableForCropSeason
        [HttpGet("Farmer/AvailableForCropSeason")]
        [EnableQuery]
        [Authorize(Roles = "Farmer")]
        public async Task<IActionResult> GetAvailableCommitmentsForCropSeason()
        {
            Guid userId;

            try
            {
                userId = User.GetUserId(); // Lấy từ token
            }
            catch
            {
                return Unauthorized("Không xác định được người dùng.");
            }

            var result = await _service.GetAvailableForCropSeason(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }


        // POST api/<FarmingCommitment>
        [HttpPost]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> Create([FromBody] FarmingCommitmentCreateDto commitment)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); // Trả lỗi nếu dữ liệu không hợp lệ
            }
            var result = await _service.Create(commitment);
            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return CreatedAtAction(nameof(GetById), new { 
                    commitmentId = ((FarmingCommitmentViewDetailsDto)result.Data).CommitmentId 
                }, result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        // POST api/<FarmingCommitment>
        [HttpPost("BulkCreate")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> BulkCreate([FromBody] FarmingCommitmentBulkCreateDto commitments)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); // Trả lỗi nếu dữ liệu không hợp lệ
            }
            var result = await _service.BulkCreate(commitments);
            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }
    }
}
