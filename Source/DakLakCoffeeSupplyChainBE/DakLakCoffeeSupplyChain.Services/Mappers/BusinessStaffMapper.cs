using DakLakCoffeeSupplyChain.Common.DTOs.BusinessStaffDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class BusinessStaffMapper
    {
        // ✅ Overload mới đúng với cách gọi: (dto, userId, staffCode)
        public static BusinessStaff MapToNewBusinessStaff(this BusinessStaffCreateDto dto, Guid userId, string staffCode)
        {
            return new BusinessStaff
            {
                StaffId = Guid.NewGuid(),
                StaffCode = staffCode,
                UserId = userId,
                SupervisorId = dto.SupervisorId,
                Position = dto.Position,
                Department = dto.Department,
                AssignedWarehouseId = dto.AssignedWarehouseId,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}
