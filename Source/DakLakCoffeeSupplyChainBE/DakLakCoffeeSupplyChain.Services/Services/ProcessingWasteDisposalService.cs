using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWasteDisposalDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.Generators;
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
    public class ProcessingWasteDisposalService : IProcessingWasteDisposalService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;

        public ProcessingWasteDisposalService(IUnitOfWork unitOfWork, ICodeGenerator CodeGenerator)
        {
            _unitOfWork = unitOfWork;
            _codeGenerator = CodeGenerator;
        }


        public async Task<IServiceResult> GetAllAsync(Guid userId, bool isAdmin)
        {
            try
            {
                // 1. Lấy danh sách Farmer + User để ánh xạ UserId -> Name
                var farmers = await _unitOfWork.FarmerRepository.GetAllAsync(
                    predicate: f => !f.IsDeleted,
                    include: q => q.Include(f => f.User),
                    asNoTracking: true
                );

                var userMap = farmers.ToDictionary(
                    f => f.UserId,
                    f => f.User?.Name ?? "N/A"
                );

                // 2. Xây dựng truy vấn disposal với chaining LINQ
                var query = _unitOfWork.ProcessingWasteDisposalRepository.GetAllQueryable();

                if (!isAdmin)
                {
                    var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
                    if (farmer == null)
                        return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy Farmer.");

                    query = query.Where(x => !x.IsDeleted && x.HandledBy == userId);
                }
                else
                {
                    query = query.Where(x => !x.IsDeleted);
                }

                var list = await query
                    .Include(x => x.Waste)
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync();
                // 5. Mapping DTO
                var dtos = list.Select(x =>
                {
                    var handledBy = x.HandledBy ?? Guid.Empty;
                    var handledByName = userMap.TryGetValue(handledBy, out var name) ? name : "N/A";
                    return x.MapToDto(handledByName);
                }).ToList();

                return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtos);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }

    }

}
