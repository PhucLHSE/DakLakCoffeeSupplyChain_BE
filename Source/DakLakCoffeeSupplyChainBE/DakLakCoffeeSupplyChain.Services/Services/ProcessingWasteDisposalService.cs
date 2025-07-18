using DakLakCoffeeSupplyChain.Common;
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
    public class ProcessingWasteDisposalService : IProcessingWasteDisposalService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProcessingWasteDisposalService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IServiceResult> GetAllAsync(Guid userId, bool isAdmin)
        {
            // Lấy toàn bộ users để ánh xạ tên người xử lý
            var users = await _unitOfWork.UserAccountRepository.GetAllAsync(
                predicate: u => !u.IsDeleted,
                asNoTracking: true
            );
            var userMap = users.ToDictionary(u => u.UserId, u => u.Name);

            // Nếu không phải Admin → chỉ lấy các bản ghi có Waste thuộc về Farmer hiện tại
            IQueryable<ProcessingWasteDisposal> query = _unitOfWork.ProcessingWasteDisposalRepository.GetAllQueryable()
                .Where(x => !x.IsDeleted);

            if (!isAdmin)
            {
                var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
                if (farmer == null)
                    return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy nông hộ.");

                var wasteIds = await _unitOfWork.ProcessingWasteRepository.GetAllQueryable()
                    .Where(w => w.RecordedBy == farmer.FarmerId && !w.IsDeleted)
                    .Select(w => w.WasteId)
                    .ToListAsync();

                query = query.Where(d => wasteIds.Contains(d.WasteId));
            }

            var disposals = await query.ToListAsync();

            var dtoList = disposals.Select(disposal =>
            {
                var handledByName = disposal.HandledBy.HasValue && userMap.ContainsKey(disposal.HandledBy.Value)
                    ? userMap[disposal.HandledBy.Value]
                    : "N/A";

                return disposal.MapToViewAllDto(handledByName);
            }).ToList();

            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtoList);
        }
    }

}
