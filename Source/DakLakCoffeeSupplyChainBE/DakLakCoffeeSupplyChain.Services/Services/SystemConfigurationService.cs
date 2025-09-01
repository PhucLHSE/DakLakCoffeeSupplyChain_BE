using System;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.SystemConfigurationDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.SystemConfigurationDTOs.ProcessingBatchCriteria;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class SystemConfigurationService (IUnitOfWork unitOfWork) : ISystemConfigurationService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        public async Task<IServiceResult> GetAll(Guid userId)
        {
            var admin = await _unitOfWork.UserAccountRepository.GetByIdAsync(
                predicate: u => u.UserId == userId,
                include: u => u.Include( u => u.Role),
                asNoTracking: true
                );
            if (admin == null || !admin.Role.RoleName.Equals("Admin"))
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Tài khoản không có quyền hạn này."
                );
            var configs = await _unitOfWork.SystemConfigurationRepository.GetAllAsync(
                predicate: c => c.IsDeleted != true,
                asNoTracking: true);

            if (configs == null || configs.Count == 0)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<SystemConfigurationViewAllDto>()
                );
            }
            else
            {
                var systemConfigurationViewAllDto = configs
                    .Select(c => c.MapToSystemConfigurationViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    systemConfigurationViewAllDto
                );
            }
        }

        public async Task<IServiceResult> GetByName(string name)
        {
            var configs = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(
                predicate: c => c.IsDeleted != true && c.Name == name,
                asNoTracking: true);

            if (configs == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<SystemConfigurationViewDetailDto>()
                );
            }
            else
            {
                var response = configs.MapToSystemConfigurationViewDetailDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    response
                );
            }
        }
        
        // ========== CRUD TIÊU CHÍ ĐÁNH GIÁ CHẤT LƯỢNG PROCESSINGBATCH ==========
        
        public async Task<IServiceResult> GetProcessingBatchCriteriaAsync()
        {
            try
            {
                var criteria = await _unitOfWork.SystemConfigurationRepository.GetAllAsync(
                    predicate: c => !c.IsDeleted && c.TargetEntity == "ProcessingBatch",
                    orderBy: q => q.OrderBy(c => c.Name),
                    asNoTracking: true
                );
                
                if (!criteria.Any())
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không có tiêu chí đánh giá chất lượng nào.",
                        new List<ProcessingBatchCriteriaViewDto>()
                    );
                }
                
                var dtos = criteria.Select(c => new ProcessingBatchCriteriaViewDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    MinValue = c.MinValue,
                    MaxValue = c.MaxValue,
                    Unit = c.Unit,
                    Operator = c.Operator,
                    Severity = c.Severity,
                    RuleGroup = c.RuleGroup,
                    IsActive = c.IsActive,
                    EffectedDateFrom = c.EffectedDateFrom,
                    EffectedDateTo = c.EffectedDateTo,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    CreatedBy = c.CreatedBy,
                    UpdatedBy = c.UpdatedBy
                }).ToList();
                
                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    dtos
                );
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    $"Lỗi khi lấy danh sách tiêu chí: {ex.Message}"
                );
            }
        }
        
        public async Task<IServiceResult> GetProcessingBatchCriteriaByNameAsync(string name)
        {
            try
            {
                var criterion = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(
                    predicate: c => !c.IsDeleted && c.TargetEntity == "ProcessingBatch" && c.Name == name,
                    asNoTracking: true
                );
                
                if (criterion == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        $"Không tìm thấy tiêu chí '{name}'."
                    );
                }
                
                var dto = new ProcessingBatchCriteriaViewDto
                {
                    Id = criterion.Id,
                    Name = criterion.Name,
                    Description = criterion.Description,
                    MinValue = criterion.MinValue,
                    MaxValue = criterion.MaxValue,
                    Unit = criterion.Unit,
                    Operator = criterion.Operator,
                    Severity = criterion.Severity,
                    RuleGroup = criterion.RuleGroup,
                    IsActive = criterion.IsActive,
                    EffectedDateFrom = criterion.EffectedDateFrom,
                    EffectedDateTo = criterion.EffectedDateTo,
                    CreatedAt = criterion.CreatedAt,
                    UpdatedAt = criterion.UpdatedAt,
                    CreatedBy = criterion.CreatedBy,
                    UpdatedBy = criterion.UpdatedBy
                };
                
                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    dto
                );
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    $"Lỗi khi lấy tiêu chí: {ex.Message}"
                );
            }
        }
        
        public async Task<IServiceResult> GetProcessingBatchCriteriaByIdAsync(int id)
        {
            try
            {
                var criterion = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(
                    predicate: c => !c.IsDeleted && c.TargetEntity == "ProcessingBatch" && c.Id == id,
                    asNoTracking: true
                );
                
                if (criterion == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        $"Không tìm thấy tiêu chí với ID {id}."
                    );
                }
                
                var dto = new ProcessingBatchCriteriaViewDto
                {
                    Id = criterion.Id,
                    Name = criterion.Name,
                    Description = criterion.Description,
                    MinValue = criterion.MinValue,
                    MaxValue = criterion.MaxValue,
                    Unit = criterion.Unit,
                    Operator = criterion.Operator,
                    Severity = criterion.Severity,
                    RuleGroup = criterion.RuleGroup,
                    IsActive = criterion.IsActive,
                    EffectedDateFrom = criterion.EffectedDateFrom,
                    EffectedDateTo = criterion.EffectedDateTo,
                    CreatedAt = criterion.CreatedAt,
                    UpdatedAt = criterion.UpdatedAt,
                    CreatedBy = criterion.CreatedBy,
                    UpdatedBy = criterion.UpdatedBy
                };
                
                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    dto
                );
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    $"Lỗi khi lấy tiêu chí: {ex.Message}"
                );
            }
        }
        
        public async Task<IServiceResult> CreateProcessingBatchCriteriaAsync(CreateProcessingBatchCriteriaDto dto, Guid userId)
        {
            try
            {
                // Kiểm tra tên tiêu chí đã tồn tại chưa
                var existingCriterion = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(
                    predicate: c => !c.IsDeleted && c.TargetEntity == "ProcessingBatch" && c.Name == dto.Name,
                    asNoTracking: true
                );
                
                if (existingCriterion != null)
                {
                    return new ServiceResult(
                        Const.ERROR_VALIDATION_CODE,
                        $"Tiêu chí '{dto.Name}' đã tồn tại."
                    );
                }
                
                var entity = new Repositories.Models.SystemConfiguration
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    MinValue = dto.MinValue,
                    MaxValue = dto.MaxValue,
                    Unit = dto.Unit,
                    Operator = dto.Operator,
                    Severity = dto.Severity,
                    RuleGroup = dto.RuleGroup,
                    IsActive = dto.IsActive,
                    EffectedDateFrom = dto.EffectedDateFrom,
                    EffectedDateTo = dto.EffectedDateTo,
                    TargetEntity = "ProcessingBatch",
                    TargetField = dto.Name, // Sử dụng Name làm TargetField
                    ScopeType = "Global",
                    ScopeId = null,
                    VersionNo = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = userId,
                    UpdatedBy = userId,
                    IsDeleted = false
                };
                
                await _unitOfWork.SystemConfigurationRepository.CreateAsync(entity);
                var saved = await _unitOfWork.SaveChangesAsync();
                
                if (saved > 0)
                {
                    return new ServiceResult(
                        Const.SUCCESS_CREATE_CODE,
                        Const.SUCCESS_CREATE_MSG,
                        entity
                    );
                }
                
                return new ServiceResult(
                    Const.FAIL_CREATE_CODE,
                    Const.FAIL_CREATE_MSG
                );
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    $"Lỗi khi tạo tiêu chí: {ex.Message}"
                );
            }
        }
        
        public async Task<IServiceResult> UpdateProcessingBatchCriteriaAsync(string name, UpdateProcessingBatchCriteriaDto dto, Guid userId)
        {
            try
            {
                var existingCriterion = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(
                    predicate: c => !c.IsDeleted && c.TargetEntity == "ProcessingBatch" && c.Name == name,
                    asNoTracking: false
                );
                
                if (existingCriterion == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        $"Không tìm thấy tiêu chí '{name}' để cập nhật."
                    );
                }
                
                // Cập nhật thông tin
                existingCriterion.Description = dto.Description ?? existingCriterion.Description;
                existingCriterion.MinValue = dto.MinValue;
                existingCriterion.MaxValue = dto.MaxValue;
                existingCriterion.Unit = dto.Unit ?? existingCriterion.Unit;
                existingCriterion.Operator = dto.Operator ?? existingCriterion.Operator;
                existingCriterion.Severity = dto.Severity ?? existingCriterion.Severity;
                existingCriterion.RuleGroup = dto.RuleGroup ?? existingCriterion.RuleGroup;
                existingCriterion.IsActive = dto.IsActive ?? existingCriterion.IsActive;
                existingCriterion.EffectedDateFrom = dto.EffectedDateFrom ?? existingCriterion.EffectedDateFrom;
                existingCriterion.EffectedDateTo = dto.EffectedDateTo;
                existingCriterion.UpdatedAt = DateTime.UtcNow;
                existingCriterion.UpdatedBy = userId;
                
                await _unitOfWork.SystemConfigurationRepository.UpdateAsync(existingCriterion);
                var saved = await _unitOfWork.SaveChangesAsync();
                
                if (saved > 0)
                {
                    return new ServiceResult(
                        Const.SUCCESS_UPDATE_CODE,
                        Const.SUCCESS_UPDATE_MSG,
                        existingCriterion
                    );
                }
                
                return new ServiceResult(
                    Const.FAIL_UPDATE_CODE,
                    Const.FAIL_UPDATE_MSG
                );
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    $"Lỗi khi cập nhật tiêu chí: {ex.Message}"
                );
            }
        }
        
        public async Task<IServiceResult> DeleteProcessingBatchCriteriaAsync(string name, Guid userId)
        {
            try
            {
                var existingCriterion = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(
                    predicate: c => !c.IsDeleted && c.TargetEntity == "ProcessingBatch" && c.Name == name,
                    asNoTracking: false
                );
                
                if (existingCriterion == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        $"Không tìm thấy tiêu chí '{name}' để xóa."
                    );
                }
                
                // Soft delete
                existingCriterion.IsDeleted = true;
                existingCriterion.UpdatedAt = DateTime.UtcNow;
                existingCriterion.UpdatedBy = userId;
                
                await _unitOfWork.SystemConfigurationRepository.UpdateAsync(existingCriterion);
                var saved = await _unitOfWork.SaveChangesAsync();
                
                if (saved > 0)
                {
                    return new ServiceResult(
                        Const.SUCCESS_DELETE_CODE,
                        Const.SUCCESS_DELETE_MSG
                    );
                }
                
                return new ServiceResult(
                    Const.FAIL_DELETE_CODE,
                    Const.FAIL_DELETE_MSG
                );
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    $"Lỗi khi xóa tiêu chí: {ex.Message}"
                );
            }
        }
        
        public async Task<IServiceResult> ActivateProcessingBatchCriteriaAsync(string name, Guid userId)
        {
            try
            {
                var existingCriterion = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(
                    predicate: c => !c.IsDeleted && c.TargetEntity == "ProcessingBatch" && c.Name == name,
                    asNoTracking: false
                );
                
                if (existingCriterion == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        $"Không tìm thấy tiêu chí '{name}' để kích hoạt."
                    );
                }
                
                existingCriterion.IsActive = true;
                existingCriterion.UpdatedAt = DateTime.UtcNow;
                existingCriterion.UpdatedBy = userId;
                
                await _unitOfWork.SystemConfigurationRepository.UpdateAsync(existingCriterion);
                var saved = await _unitOfWork.SaveChangesAsync();
                
                if (saved > 0)
                {
                    return new ServiceResult(
                        Const.SUCCESS_UPDATE_CODE,
                        "Kích hoạt tiêu chí thành công."
                    );
                }
                
                return new ServiceResult(
                    Const.FAIL_UPDATE_CODE,
                    "Kích hoạt tiêu chí thất bại."
                );
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    $"Lỗi khi kích hoạt tiêu chí: {ex.Message}"
                );
            }
        }
        
        public async Task<IServiceResult> DeactivateProcessingBatchCriteriaAsync(string name, Guid userId)
        {
            try
            {
                var existingCriterion = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(
                    predicate: c => !c.IsDeleted && c.TargetEntity == "ProcessingBatch" && c.Name == name,
                    asNoTracking: false
                );
                
                if (existingCriterion == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        $"Không tìm thấy tiêu chí '{name}' để vô hiệu hóa."
                    );
                }
                
                existingCriterion.IsActive = false;
                existingCriterion.UpdatedAt = DateTime.UtcNow;
                existingCriterion.UpdatedBy = userId;
                
                await _unitOfWork.SystemConfigurationRepository.UpdateAsync(existingCriterion);
                var saved = await _unitOfWork.SaveChangesAsync();
                
                if (saved > 0)
                {
                    return new ServiceResult(
                        Const.SUCCESS_UPDATE_CODE,
                        "Vô hiệu hóa tiêu chí thành công."
                    );
                }
                
                return new ServiceResult(
                    Const.FAIL_UPDATE_CODE,
                    "Vô hiệu hóa tiêu chí thất bại."
                );
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    $"Lỗi khi vô hiệu hóa tiêu chí: {ex.Message}"
                );
            }
        }
    }
}
