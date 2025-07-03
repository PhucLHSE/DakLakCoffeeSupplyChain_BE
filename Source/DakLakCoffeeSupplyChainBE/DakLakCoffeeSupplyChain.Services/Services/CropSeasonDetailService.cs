using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDetailDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class CropSeasonDetailService : ICropSeasonDetailService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CropSeasonDetailService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IServiceResult> GetAll(Guid userId, bool isAdmin = false)
        {
            try
            {
                var details = await _unitOfWork.CropSeasonDetailRepository.GetAllAsync(
                    predicate: d =>
                        !d.IsDeleted &&
                        (isAdmin || d.CropSeason.Farmer.UserId == userId),
                    include: query => query
                        .Include(d => d.CoffeeType)
                        .Include(d => d.CropSeason)
                            .ThenInclude(cs => cs.Farmer)
                                .ThenInclude(f => f.User),
                    asNoTracking: true
                );

                if (details == null || !details.Any())
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có dòng mùa vụ nào.");
                }

                var dtos = details.Select(d => d.MapToCropSeasonDetailViewDto()).ToList();
                return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtos);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.ToString());
            }
        }


        public async Task<IServiceResult> GetById(Guid detailId, Guid userId, bool isAdmin = false)
        {
            try
            {
                var detail = await _unitOfWork.CropSeasonDetailRepository.GetByIdAsync(
                    predicate: d => d.DetailId == detailId && !d.IsDeleted,
                    include: query => query
                        .Include(d => d.CoffeeType)
                        .Include(d => d.CropSeason)
                            .ThenInclude(cs => cs.Farmer)
                                .ThenInclude(f => f.User), // 👈 Quan trọng: cần include cả User để lấy tên nông hộ
                    asNoTracking: true
                );

                if (detail == null)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy chi tiết mùa vụ.");

                // ✅ Phân quyền: chỉ admin hoặc chính chủ nông hộ mới được xem
                if (!isAdmin && detail.CropSeason?.Farmer?.UserId != userId)
                    return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền xem chi tiết mùa vụ này.");

                var dto = detail.MapToCropSeasonDetailViewDto();
                return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dto);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.ToString());
            }
        }



        public async Task<IServiceResult> Create(CropSeasonDetailCreateDto dto)
        {
            try
            {
                var season = await _unitOfWork.CropSeasonRepository.GetByIdAsync(
                    predicate: s => s.CropSeasonId == dto.CropSeasonId && !s.IsDeleted,
                    asNoTracking: true
                );

                if (season == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy mùa vụ tương ứng."
                    );
                }

                var entity = dto.MapToNewCropSeasonDetail();
                await _unitOfWork.CropSeasonDetailRepository.CreateAsync(entity);
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    var created = await _unitOfWork.CropSeasonDetailRepository.GetByIdAsync(
                        predicate: d => d.DetailId == entity.DetailId,
                        include: query => query.Include(d => d.CoffeeType),
                        asNoTracking: true
                    );

                    var view = created.MapToCropSeasonDetailViewDto();
                    return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, view);
                }


                return new ServiceResult(
                    Const.FAIL_CREATE_CODE,
                    "Tạo mới dòng mùa vụ thất bại."
                );
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        public async Task<IServiceResult> Update(CropSeasonDetailUpdateDto dto)
        {
            try
            {
                var existing = await _unitOfWork.CropSeasonDetailRepository.GetByIdAsync(
                    predicate: d => d.DetailId == dto.DetailId && !d.IsDeleted,
                    asNoTracking: false
                );

                if (existing == null)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy dòng mùa vụ.");
                }

                dto.MapToExistingEntity(existing);

                await _unitOfWork.CropSeasonDetailRepository.UpdateAsync(existing);
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    var view = existing.MapToCropSeasonDetailViewDto();
                    return new ServiceResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG, view);
                }

                return new ServiceResult(Const.FAIL_UPDATE_CODE, Const.FAIL_UPDATE_MSG);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.ToString());
            }
        }

        public async Task<IServiceResult> DeleteById(Guid detailId)
        {
            try
            {
                var existing = await _unitOfWork.CropSeasonDetailRepository.GetByIdAsync(
                    predicate: d => d.DetailId == detailId && !d.IsDeleted,
                    asNoTracking: false
                );

                if (existing == null)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy dòng mùa vụ.");
                }

                await _unitOfWork.CropSeasonDetailRepository.RemoveAsync(existing);
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    return new ServiceResult(Const.SUCCESS_DELETE_CODE, Const.SUCCESS_DELETE_MSG);
                }

                return new ServiceResult(Const.FAIL_DELETE_CODE, Const.FAIL_DELETE_MSG);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.ToString());
            }
        }

        public async Task<IServiceResult> SoftDeleteById(Guid detailId)
        {
            try
            {
                var existing = await _unitOfWork.CropSeasonDetailRepository.GetByIdAsync(
                    predicate: d => d.DetailId == detailId && !d.IsDeleted,
                    asNoTracking: false
                );

                if (existing == null)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy dòng mùa vụ.");
                }

                existing.IsDeleted = true;
                existing.UpdatedAt = DateHelper.NowVietnamTime();

                await _unitOfWork.CropSeasonDetailRepository.UpdateAsync(existing);
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    return new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xóa mềm thành công.");
                }

                return new ServiceResult(Const.FAIL_DELETE_CODE, "Xóa mềm thất bại.");
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.ToString());
            }
        }
    }
}
