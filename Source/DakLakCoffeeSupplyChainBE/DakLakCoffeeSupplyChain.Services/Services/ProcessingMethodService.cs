﻿using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.Flow3DTOs.ProcessingMethod;
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
    public class ProcessingMethodService : IProcessingMethodService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProcessingMethodService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IServiceResult> GetAll()
        {
            var methods = await _unitOfWork.ProcessingMethodRepository.GetAllAsync();

            if (methods == null || !methods.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<ProcessingMethodViewAllDto>()
                );
            }

            var methodDtos = methods
                .Select(m => m.MapToProcessingMethodViewAllDto())
                .ToList();

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG,
                methodDtos
            );
        }
        public async Task<IServiceResult> GetById(int methodId)
        {
            var method = await _unitOfWork.ProcessingMethodRepository.GetByIdAsync(methodId);

            if (method == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG
                );
            }

            var dto = method.MapToProcessingMethodDetailDto();

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG,
                dto
            );
        }
        public async Task<IServiceResult> DeleteById(int methodId)
        {
            try
            {
                var method = await _unitOfWork.ProcessingMethodRepository.GetByIdAsync(methodId);

                if (method == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }

                await _unitOfWork.ProcessingMethodRepository.RemoveAsync(method);

                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    return new ServiceResult(
                        Const.SUCCESS_DELETE_CODE,
                        Const.SUCCESS_DELETE_MSG
                    );
                }
                else
                {
                    return new ServiceResult(
                        Const.FAIL_DELETE_CODE,
                        Const.FAIL_DELETE_MSG
                    );
                }
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

    }
}
