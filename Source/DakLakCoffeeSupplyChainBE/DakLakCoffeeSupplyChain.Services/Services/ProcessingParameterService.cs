using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingParameterDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class ProcessingParameterService : IProcessingParameterService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProcessingParameterService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IServiceResult> GetAll()
        {
            var parameters = await _unitOfWork.ProcessingParameterRepository.GetAllActiveAsync();

            if (parameters == null || !parameters.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<ProcessingParameterViewAllDto>()
                );
            }

            var parameterDtos = parameters
                .Select(p => p.MapToProcessingParameterViewAllDto())
                .ToList();

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG,
                parameterDtos
            );
        }
        public async Task<IServiceResult> GetById(Guid id)
        {
            var parameter = await _unitOfWork.ProcessingParameterRepository.GetByIdAsync(id);

            if (parameter == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    $"Không tìm thấy thông số chế biến với ID = {id}",
                    null
                );
            }

            var dto = parameter.MapToProcessingParameterDetailDto();

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG,
                dto
            );
        }
        public async Task<IServiceResult> CreateAsync(ProcessingParameterCreateDto dto)
        {
            // 🛡️ Validate thủ công
            if (dto.ProgressId == Guid.Empty)
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "ProgressId không được để trống");

            if (string.IsNullOrWhiteSpace(dto.ParameterName))
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "ParameterName không được để trống");

            if (string.IsNullOrWhiteSpace(dto.ParameterValue))
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "ParameterValue không được để trống");

            if (string.IsNullOrWhiteSpace(dto.Unit))
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Unit không được để trống");

            // 🔁 Kiểm tra trùng tên trong cùng Progress
            var isDuplicate = await _unitOfWork.ProcessingParameterRepository
                .AnyAsync(x => x.ProgressId == dto.ProgressId
                               && x.ParameterName.ToLower() == dto.ParameterName.ToLower()
                               && !x.IsDeleted);

            if (isDuplicate)
            {
                return new ServiceResult(
                    Const.ERROR_VALIDATION_CODE,
                    $"Thông số \"{dto.ParameterName}\" đã tồn tại cho bước này."
                );
            }

            try
            {
                var entity = new ProcessingParameter
                {
                    ParameterId = Guid.NewGuid(),
                    ProgressId = dto.ProgressId,
                    ParameterName = dto.ParameterName,
                    ParameterValue = dto.ParameterValue,
                    Unit = dto.Unit,
                    RecordedAt = dto.RecordedAt,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _unitOfWork.ProcessingParameterRepository.CreateAsync(entity);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, entity.MapToProcessingParameterDetailDto());
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG, ex.Message);
            }
        }
        public async Task<IServiceResult> UpdateAsync(ProcessingParameterUpdateDto dto)
        {
            if (dto.ParameterId == Guid.Empty)
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "ParameterId không hợp lệ");

            if (string.IsNullOrWhiteSpace(dto.ParameterName) ||
                string.IsNullOrWhiteSpace(dto.ParameterValue) ||
                string.IsNullOrWhiteSpace(dto.Unit))
            {
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Dữ liệu không hợp lệ");
            }

            var entity = await _unitOfWork.ProcessingParameterRepository.GetByIdAsync(dto.ParameterId);
            if (entity == null || entity.IsDeleted)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Thông số không tồn tại");

            // Check trùng tên (nếu cần)
            var isDuplicate = await _unitOfWork.ProcessingParameterRepository.AnyAsync(x =>
                x.ParameterId != dto.ParameterId &&
                x.ProgressId == entity.ProgressId &&
                x.ParameterName.ToLower() == dto.ParameterName.ToLower() &&
                !x.IsDeleted);

            if (isDuplicate)
                return new ServiceResult(Const.ERROR_VALIDATION_CODE, $"Thông số \"{dto.ParameterName}\" đã tồn tại trong bước này");

            // Cập nhật
            entity.ParameterName = dto.ParameterName;
            entity.ParameterValue = dto.ParameterValue;
            entity.Unit = dto.Unit;
            entity.RecordedAt = dto.RecordedAt;
            entity.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.ProcessingParameterRepository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG);
        }
        public async Task<IServiceResult> SoftDeleteAsync(Guid parameterId)
        {
            try
            {
                var success = await _unitOfWork.ProcessingParameterRepository.SoftDeleteAsync(parameterId);
                if (!success)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Parameter not found or already deleted"
                    );
                }

                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult(
                    Const.SUCCESS_DELETE_CODE,
                    Const.SUCCESS_DELETE_MSG
                );
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.Message
                );
            }
        }
    }
}
