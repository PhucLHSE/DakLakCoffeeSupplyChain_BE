using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.FarmingCommitmentDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.FarmingCommitmentEnums;
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
        public async Task<IServiceResult> GetAllBusinessManagerCommitment(Guid userId)
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

        public async Task<IServiceResult> GetAllFarmerCommitment(Guid userId)
        {
            // Kiểm tra Farmer có tồn tại không
            var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                predicate: m => m.UserId == userId,
                asNoTracking: true
            );

            if (farmer == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy Farmer tương ứng với tài khoản."
                );
            }

            var commitments = await _unitOfWork.FarmingCommitmentRepository.GetAllAsync(
                predicate: fm => fm.IsDeleted != true && fm.Farmer.User.UserId == userId,
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

        //Minh làm thêm cái này để lọc 
        public async Task<IServiceResult> GetAvailableForCropSeason(Guid userId)
        {
            // Tìm farmer
            var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                predicate: f => f.UserId == userId && !f.IsDeleted,
                asNoTracking: true
            );

            if (farmer == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy Farmer tương ứng với tài khoản."
                );
            }

            // Lấy tất cả cam kết active của farmer
            var allCommitments = await _unitOfWork.FarmingCommitmentRepository.GetAllAsync(
                predicate: c =>
                    c.FarmerId == farmer.FarmerId &&
                    c.Status == FarmingCommitmentStatus.Active.ToString() &&
                    !c.IsDeleted,
                include: c => c.Include(c => c.Farmer).ThenInclude(f => f.User),
                asNoTracking: true
            );

            // Lọc ra những commitment chưa bị dùng để tạo mùa vụ
            var available = new List<Repositories.Models.FarmingCommitment>();

            foreach (var c in allCommitments)
            {
                bool used = await _unitOfWork.CropSeasonRepository.ExistsAsync(
                    cs => cs.CommitmentId == c.CommitmentId && !cs.IsDeleted
                );

                if (!used)
                    available.Add(c);
            }

            var dtoList = available.Select(c => c.MapToFarmingCommitmentViewAllDto()).ToList();

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG,
                dtoList
            );
        }

    }
}
