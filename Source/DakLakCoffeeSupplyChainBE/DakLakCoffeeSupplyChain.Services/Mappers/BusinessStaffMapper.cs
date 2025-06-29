using DakLakCoffeeSupplyChain.Common.DTOs.BusinessStaffDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class BusinessStaffMapper
    {
        // ✅ Overload mới đúng với cách gọi: (dto, userId, staffCode)
        public static BusinessStaff MapToNewBusinessStaff(this BusinessStaffCreateDto dto, Guid userId, string staffCode, Guid supervisorId)
        {
            return new BusinessStaff
            {
                StaffId = Guid.NewGuid(),
                StaffCode = staffCode,
                UserId = userId,
                SupervisorId = supervisorId,
                Position = dto.Position,
                Department = dto.Department,
                AssignedWarehouseId = dto.AssignedWarehouseId,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

    }
}
