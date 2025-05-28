using AutoMapper;
using CoffeeManagement.Flow4.DTOs;
using CoffeeManagement.Flow4.Repositories.Models;
using CoffeeManagement.Flow4.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeManagement.Flow4.APIService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WarehouseInboundRequestsController : ControllerBase
    {
        private readonly IWarehouseInboundRequestService _service;
        private readonly IMapper _mapper;

        public WarehouseInboundRequestsController(IWarehouseInboundRequestService service, IMapper mapper)
        {
            _service = service;
            _mapper = mapper;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] WarehouseInboundRequestCreateDto dto)
        {
            if (dto.FarmerId == Guid.Empty || dto.BatchId == Guid.Empty)
                return BadRequest("Invalid data");

            var entity = _mapper.Map<WarehouseInboundRequest>(dto);
            var created = await _service.CreateRequestAsync(entity);
            var resultDto = _mapper.Map<WarehouseInboundRequestDto>(created);

            return CreatedAtAction(nameof(GetById), new { id = resultDto.InboundRequestId }, resultDto);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var entity = await _service.GetRequestByIdAsync(id);
            if (entity == null) return NotFound();

            var dto = _mapper.Map<WarehouseInboundRequestDto>(entity);
            return Ok(dto);
        }

        [HttpGet("farmer/{farmerId}")]
        public async Task<IActionResult> GetByFarmer(Guid farmerId)
        {
            var list = await _service.GetRequestsByFarmerAsync(farmerId);
            var dtoList = list.Select(r => _mapper.Map<WarehouseInboundRequestDto>(r));
            return Ok(dtoList);
        }

        // Optional: Update Status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] WarehouseInboundRequestUpdateStatusDto dto)
        {
            var existing = await _service.GetRequestByIdAsync(id);
            if (existing == null) return NotFound();

            existing.Status = dto.Status;
            existing.ActualDeliveryDate = dto.ActualDeliveryDate.HasValue
    ? DateOnly.FromDateTime(dto.ActualDeliveryDate.Value)
    : null;
            existing.Note = dto.Note;
            existing.UpdatedAt = DateTime.UtcNow;

            // Optional: You can add an UpdateAsync method in service/repo if needed
            // await _service.UpdateRequestAsync(existing);

            return Ok(_mapper.Map<WarehouseInboundRequestDto>(existing));
        }
    }
}
