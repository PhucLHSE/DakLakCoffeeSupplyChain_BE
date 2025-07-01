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
        public static BusinessStaffDetailDto MapToDetailDto(this BusinessStaff staff)
        {
            return new BusinessStaffDetailDto
            {
                StaffId = staff.StaffId,
                StaffCode = staff.StaffCode,
                FullName = staff.User?.Name ?? string.Empty,
                Email = staff.User?.Email ?? string.Empty,
                PhoneNumber = staff.User?.PhoneNumber,
                Department = staff.Department,
                Position = staff.Position,
                AssignedWarehouseId = staff.AssignedWarehouseId,
                CreatedAt = staff.CreatedAt
            };
        }
        public static BusinessStaffListDto MapToListDto(this BusinessStaff staff)
        {
            return new BusinessStaffListDto
            {
                StaffId = staff.StaffId,
                StaffCode = staff.StaffCode,
                FullName = staff.User?.Name ?? string.Empty,
                Email = staff.User?.Email ?? string.Empty,
                Position = staff.Position,
                Department = staff.Department
            };
        }
        public static void MapToUpdateBusinessStaff(this BusinessStaffUpdateDto dto, BusinessStaff entity)
        {
            entity.Position = dto.Position;
            entity.Department = dto.Department;
            entity.AssignedWarehouseId = dto.AssignedWarehouseId;
            entity.IsActive = dto.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
        }


    }
}
