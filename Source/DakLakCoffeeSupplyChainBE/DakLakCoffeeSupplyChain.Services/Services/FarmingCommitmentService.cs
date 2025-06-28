using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.FarmingCommitmentDTOs;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class FarmingCommitmentService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator) : IFarmingCommitmentService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly ICodeGenerator _codeGenerator = codeGenerator;
        public async Task<IServiceResult> GetAll(Guid userId)
        {
            // Kiểm tra BM có tồn tại không
            var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                predicate: m => m.UserId == userId,
                asNoTracking: true
            );

            if (manager == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy BusinessManager tương ứng với tài khoản."
                );
            }

            var commitments = await _unitOfWork.FarmingCommitmentRepository.GetAllAsync(
                predicate: fm => fm.IsDeleted != true && fm.ApprovedByNavigation.User.UserId == userId,
                include: fm => fm.
                Include(fm => fm.Farmer).
                    ThenInclude(fm => fm.User).
                Include(fm => fm.ApprovedByNavigation).
                    ThenInclude(fm => fm.User),
                orderBy: fm => fm.OrderBy(fm => fm.CommitmentCode),
                asNoTracking: true
                );

            if (commitments == null || commitments.Count == 0)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<FarmingCommitmentViewAllDto>()   // Trả về danh sách rỗng
                );
            }
            else
            {
                // Map danh sách entity sang DTO
                var commitmentDto = commitments
                    .Select(commitments => commitments.MapToFarmingCommitmentViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    commitmentDto
                );
            }
        }

        public async Task<IServiceResult> GetById(Guid commitmentId)
        {
            // Tìm commitment theo ID
            var commitment = await _unitOfWork.FarmingCommitmentRepository.GetByIdAsync(
                predicate: c =>
                   c.CommitmentId == commitmentId &&
                   !c.IsDeleted,
                include: fm => fm.
                Include(fm => fm.Farmer).
                    ThenInclude(fm => fm.User).
                Include(fm => fm.ApprovedByNavigation).
                    ThenInclude(fm => fm.User),
                asNoTracking: true
            );

            // Kiểm tra nếu không tìm thấy commitment
            if (commitment == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new FarmingCommitmentViewDetailsDto()  // Trả về DTO rỗng
                );
            }
            else
            {
                // Map sang DTO chi tiết để trả về
                var commitmentDto = commitment.MapToFarmingCommitmentViewDetailsDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    commitmentDto
                );
            }
        }
    }
}
