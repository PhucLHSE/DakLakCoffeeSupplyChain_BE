using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class ProcessingBatchService : IProcessingBatchService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProcessingBatchService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IServiceResult> GetAll()
        {
            var batches = await _unitOfWork.ProcessingBatchRepository.GetAll();

            if (batches == null || !batches.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<ProcessingBatchViewDto>()
                );
            }

            var dtoList = batches.Select(b => b.MapToProcessingBatchViewDto()).ToList();

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG,
                dtoList
            );
        }
        public async Task<IServiceResult> GetAllByUserId(Guid userId, bool isAdmin = false)
        {
            List<ProcessingBatch> batches;

            if (isAdmin)
            {
                // Admin xem tất cả
                batches = await _unitOfWork.ProcessingBatchRepository
                    .GetQueryable()
                    .Include(pb => pb.CropSeason)
                    .Include(pb => pb.Farmer).ThenInclude(f => f.User)
                    .Include(pb => pb.Method)
                    .Include(pb => pb.ProcessingBatchProgresses)
                    .Where(pb => !pb.IsDeleted)
                    .ToListAsync();
            }
            else
            {
                // Farmer chỉ xem batch của chính họ
                var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
                if (farmer == null)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy Farmer tương ứng.", new List<ProcessingBatchViewDto>());
                }

                batches = await _unitOfWork.ProcessingBatchRepository
                    .GetQueryable()
                    .Include(pb => pb.CropSeason)
                    .Include(pb => pb.Farmer).ThenInclude(f => f.User)
                    .Include(pb => pb.Method)
                    .Include(pb => pb.ProcessingBatchProgresses)
                    .Where(pb => pb.FarmerId == farmer.FarmerId && !pb.IsDeleted)
                    .ToListAsync();
            }

            if (!batches.Any())
            {
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có dữ liệu ProcessingBatch.", new List<ProcessingBatchViewDto>());
            }

            var dtoList = batches.Select(b => b.MapToProcessingBatchViewDto()).ToList();
            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtoList);
        }

    }
}