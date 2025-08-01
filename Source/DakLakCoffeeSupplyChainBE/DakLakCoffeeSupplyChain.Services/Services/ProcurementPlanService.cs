using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.ProcurementPlanEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class ProcurementPlanService(IUnitOfWork unitOfWork, ICodeGenerator planCodeGenerator) : IProcurementPlanService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly ICodeGenerator _planCode = planCodeGenerator ?? throw new ArgumentNullException(nameof(planCodeGenerator));

        //Hiển thị toàn bộ plan đang mở ở màn hình public
        public async Task<IServiceResult> GetAllProcurementPlansAvailable()
        {

            var procurementPlans = await _unitOfWork.ProcurementPlanRepository.GetAllAsync(
                predicate: p => p.Status == "Open",
                include: p => p.
                Include(p => p.CreatedByNavigation).
                Include(p => p.ProcurementPlansDetails).
                    ThenInclude(d => d.CoffeeType).
                Include(p => p.ProcurementPlansDetails).
                    ThenInclude(p => p.ProcessMethod),
                orderBy: p => p.OrderBy(p => p.PlanCode),
                asNoTracking: true);

            if (procurementPlans == null || procurementPlans.Count == 0)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<ProcurementPlanViewAllDto>()
                );
            }
            else
            {
                var procurementPlansDtos = procurementPlans
                    .Select(procurementPlans => procurementPlans.MapToProcurementPlanViewAllAvailableDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    procurementPlansDtos
                );
            }
        }
        //Hiển thị toàn bộ plan ở màn hình dashboard của BM
        public async Task<IServiceResult> GetAll(Guid userId)
        {
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

            var procurementPlans = await _unitOfWork.ProcurementPlanRepository.GetAllAsync(
                predicate: p => p.IsDeleted != true && p.CreatedBy == manager.ManagerId,
                include: p => p.
                Include(p => p.CreatedByNavigation).
                Include(p => p.FarmingCommitments).
                    ThenInclude(p => p.Farmer).
                        ThenInclude(p => p.User),
                orderBy: p => p.OrderBy(p => p.PlanCode),
                asNoTracking: true);

            if (procurementPlans == null || procurementPlans.Count == 0)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<ProcurementPlanViewAllDto>()
                );
            }
            else
            {
                var procurementPlansDtos = procurementPlans
                    .Select(procurementPlans => procurementPlans.MapToProcurementPlanViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    procurementPlansDtos
                );
            }
        }
        public async Task<IServiceResult> GetById(Guid planId)
        {
            var plan = await _unitOfWork.ProcurementPlanRepository.GetByIdAsync(
                predicate: p => p.PlanId == planId,
                include: p => p.
                Include(p => p.CreatedByNavigation).
                Include(p => p.ProcurementPlansDetails).    //Order ProcurementPlansDetails bên mapper
                    ThenInclude(d => d.CoffeeType).
                Include(p => p.ProcurementPlansDetails).
                    ThenInclude(p => p.ProcessMethod), 
                asNoTracking: true
                );

            if (plan == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new ProcurementPlanViewDetailsSumaryDto()
                );
            }
            else
            {
                var planDto = plan.MapToProcurementPlanViewDetailsDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    planDto
                );
            }
        }
        //Hiển thị chi tiết plan nhưng loại ko trả về các chi tiết plan đã bị khóa (màn hình public)
        public async Task<IServiceResult> GetByIdExceptDisablePlanDetails(Guid planId)
        {
            var plan = await _unitOfWork.ProcurementPlanRepository.GetByIdAsync(
                predicate: p => p.PlanId == planId,
                include: p => p.
                    Include(p => p.CreatedByNavigation).
                    Include(p => p.ProcurementPlansDetails.Where(p => p.Status != "Disable")).    //Order ProcurementPlansDetails bên mapper
                        ThenInclude(d => d.CoffeeType).
                    Include(p => p.ProcurementPlansDetails.Where(p => p.Status != "Disable")).
                        ThenInclude(d => d.ProcessMethod),
                asNoTracking: true
                );

            if (plan == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new ProcurementPlanViewDetailsSumaryDto()
                );
            }
            else
            {
                var planDto = plan.MapToProcurementPlanViewDetailsDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    planDto
                );
            }
        }
        public async Task<IServiceResult> Create(ProcurementPlanCreateDto procurementPlanDto, Guid userId)
        {
            try
            {
                //Nếu status là open
                    //Start date là null thì Start date sẽ tự động cập nhật thành thời điểm hiện tại
                    //Start date không phải null thì không được phép trong quá khứ, không được sau EndDate và chỉ hiển thị lên public khi đến ngày đó
                //Nếu status là draft
                    //Start date là null thì Bỏ qua toàn bộ logic kiểm tra
                    //Start date không phải null thì không đuọc phép trong quá khứ, không được sau EndDate

                //Logic cho TotalQuantity/TargetQuantity/MinimumRegistrationQuantity (plan/detail) đã xong
                //Logic cho StartDate/EndDate (plan) đã xong
                //Logic cho MinPriceRange/MaxPriceRange (detail) đã xong

                //Logic chỉ hiển thị lên public khi đến ngày StartDate và active chưa xong

                var businessManager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: m => m.UserId == userId,
                    asNoTracking: true
                );
                if (businessManager == null)
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy BusinessManager tương ứng với tài khoản."
                    );

                //Generate code
                string procurementPlanCode = await _planCode.GenerateProcurementPlanCodeAsync();
                string procurementPlanDetailsCode = await _planCode.GenerateProcurementPlanDetailsCodeAsync();
                var count = GeneratedCodeHelpler.GetGeneratedCodeLastNumber(procurementPlanDetailsCode);

                //Map dto to model
                var newPlan = procurementPlanDto.MapToProcurementPlanCreateDto(procurementPlanCode, businessManager.ManagerId);

                //Nếu như BM để status kế hoạch là open nhưng chưa set StartDate thì kế hoạch sẽ tự động mở đăng ký luôn
                //Nếu BM để status open và đã set StartDate thì kế hoạch mới mở đăng ký khi đến ngày đó thôi.
                //if(newPlan.Status == "Open" && !newPlan.StartDate.HasValue)
                //    newPlan.StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date);

                // Logic TotalQuantity/TargetQuantity
                // Tạo code hàng loạt
                foreach(var item in newPlan.ProcurementPlansDetails)
                {
                    item.PlanDetailCode = $"PLD-{DateTime.UtcNow.Year}-{count:D4}";
                    count++;
                    newPlan.TotalQuantity += item.TargetQuantity;                    
                }

                // Save data to database
                await _unitOfWork.ProcurementPlanRepository.CreateAsync(newPlan);
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Không thể xài cách này được vì responseDTO không chứa các thông tin của các DTO navigation
                    // Map the saved entity to a response DTO
                    //var responseDto = newPlan.MapToProcurementPlanViewDetailsDto();

                    var plan = await _unitOfWork.ProcurementPlanRepository.GetByIdAsync(
                        predicate: p => p.PlanId == newPlan.PlanId,
                        include: p => p.
                        Include(p => p.CreatedByNavigation).
                        Include(p => p.ProcurementPlansDetails).
                            ThenInclude(d => d.CoffeeType).
                        Include(p => p.ProcurementPlansDetails).
                            ThenInclude(d => d.ProcessMethod),
                        asNoTracking: true
                        );

                    if (plan == null)
                        return new ServiceResult(
                                Const.WARNING_NO_DATA_CODE,
                                Const.WARNING_NO_DATA_MSG,
                                new ProcurementPlanViewDetailsSumaryDto() //Trả về DTO rỗng
                            );
                    var responseDto = plan.MapToProcurementPlanViewDetailsDto();

                    return new ServiceResult(
                        Const.SUCCESS_CREATE_CODE,
                        Const.SUCCESS_CREATE_MSG,
                    responseDto
                    );
                }
                else
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        Const.FAIL_CREATE_MSG
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

        //Xóa cứng
        //public async Task<IServiceResult> DeleteById(Guid planId)
        //{
        //    try
        //    {
        //        var plan = await _unitOfWork.ProcurementPlanRepository.GetByIdAsync(planId);
        //        var planDetails = await _unitOfWork.ProcurementPlanDetailsRepository.
        //            GetAllAsync(predicate: p => p.PlanId == planId, asNoTracking: true);
        //        if (plan == null)
        //        {
        //            return new ServiceResult(
        //                Const.WARNING_NO_DATA_CODE,
        //                Const.WARNING_NO_DATA_MSG
        //            );
        //        }
        //        else
        //        {
        //            if (planDetails.Count != 0)
        //            {
        //                foreach (var item in planDetails)
        //                    await _unitOfWork.ProcurementPlanDetailsRepository.RemoveAsync(item);
        //            }
        //            await _unitOfWork.ProcurementPlanRepository.RemoveAsync(plan);
        //            var result = await _unitOfWork.SaveChangesAsync();

        //            if (result > 0)
        //            {
        //                return new ServiceResult(
        //                    Const.SUCCESS_DELETE_CODE,
        //                    Const.SUCCESS_DELETE_MSG
        //                );
        //            }
        //            else
        //            {
        //                return new ServiceResult(
        //                    Const.FAIL_DELETE_CODE,
        //                    Const.FAIL_DELETE_MSG
        //                );
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ServiceResult(
        //            Const.ERROR_EXCEPTION,
        //            ex.ToString()
        //        );
        //    }
        //}

        //Xóa mềm
        public async Task<IServiceResult> SoftDeleteById(Guid planId)
        {
            try
            {
                // Tìm plan theo ID
                var plan = await _unitOfWork.ProcurementPlanRepository.GetByIdAsync(planId);

                // Kiểm tra nếu không tồn tại
                if (plan == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Đánh dấu xoá mềm bằng IsDeleted
                    plan.IsDeleted = true;
                    plan.UpdatedAt = DateTime.Now;

                    // Cập nhật xoá mềm vai trò ở repository
                    await _unitOfWork.ProcurementPlanRepository.UpdateAsync(plan);

                    // Lưu thay đổi
                    var result = await _unitOfWork.SaveChangesAsync();

                    // Kiểm tra kết quả
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
            }
            catch (Exception ex)
            {
                // Trả về lỗi nếu có exception
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        public async Task<IServiceResult> Update(ProcurementPlanUpdateDto dto, Guid userId, Guid planId)
        {
            try
            {
                //Lấy plan
                var plan = await _unitOfWork.ProcurementPlanRepository.GetByIdAsync(
                    predicate: p => p.PlanId == planId && !p.IsDeleted,
                    include: p => p.
                        Include(p => p.CreatedByNavigation).
                        Include(p => p.ProcurementPlansDetails)
                        );

                if (plan == null || plan.CreatedByNavigation.UserId != userId)
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy kế hoạch hoặc không thuộc quyền quản lý."
                    );

                // Map dto to model
                dto.MapToProcurementPlanUpdate(plan);

                // Đồng bộ dữ liệu
                var planDetailsIds = dto.ProcurementPlansDetailsUpdateDto.Select(i => i.PlanDetailsId).ToHashSet();
                var now = DateHelper.NowVietnamTime();

                // Xóa mềm planDetails
                foreach(var oldItem in plan.ProcurementPlansDetails)
                {
                    if (!planDetailsIds.Contains(oldItem.PlanDetailsId) && !oldItem.IsDeleted)
                    {
                        plan.TotalQuantity -= oldItem.TargetQuantity;
                        oldItem.IsDeleted = true;
                        oldItem.UpdatedAt = now;
                        await _unitOfWork.ProcurementPlanDetailsRepository.UpdateAsync(oldItem);
                    }
                }

                // Cập nhật plan detail đang tồn tại
                foreach (var itemDto in dto.ProcurementPlansDetailsUpdateDto)
                {
                    var existingPlanDetail = plan.ProcurementPlansDetails.
                        FirstOrDefault(p => p.PlanDetailsId == itemDto.PlanDetailsId);
                    
                    if (existingPlanDetail != null)
                    {
                        plan.TotalQuantity = plan.TotalQuantity - existingPlanDetail.TargetQuantity + (itemDto.TargetQuantity ?? 0);
                        existingPlanDetail.CoffeeTypeId = itemDto.CoffeeTypeId != Guid.Empty ? itemDto.CoffeeTypeId : existingPlanDetail.CoffeeTypeId;
                        existingPlanDetail.ProcessMethodId = itemDto.ProcessMethodId != 0 ? itemDto.ProcessMethodId : existingPlanDetail.ProcessMethodId;
                        existingPlanDetail.TargetQuantity = itemDto.TargetQuantity.HasValue ? itemDto.TargetQuantity : existingPlanDetail.TargetQuantity;
                        existingPlanDetail.TargetRegion = itemDto.TargetRegion.HasValue() ? itemDto.TargetRegion : existingPlanDetail.TargetRegion;
                        existingPlanDetail.MinimumRegistrationQuantity = itemDto.MinimumRegistrationQuantity.HasValue ? itemDto.MinimumRegistrationQuantity : existingPlanDetail.MinimumRegistrationQuantity;
                        existingPlanDetail.MinPriceRange = itemDto.MinPriceRange.HasValue ? itemDto.MinPriceRange : existingPlanDetail.MinPriceRange;
                        existingPlanDetail.MaxPriceRange = itemDto.MaxPriceRange.HasValue ? itemDto.MaxPriceRange : existingPlanDetail.MaxPriceRange;
                        existingPlanDetail.ExpectedYieldPerHectare = itemDto.ExpectedYieldPerHectare.HasValue ? itemDto.ExpectedYieldPerHectare : existingPlanDetail.ExpectedYieldPerHectare;
                        existingPlanDetail.Note = itemDto.Note.HasValue() ? itemDto.Note : existingPlanDetail.Note;
                        existingPlanDetail.ContractItemId = itemDto.ContractItemId.HasValue ? itemDto.ContractItemId : existingPlanDetail.ContractItemId;
                        existingPlanDetail.Status = itemDto.Status.ToString() != "Unknown" ? itemDto.Status.ToString() : existingPlanDetail.Status;
                        existingPlanDetail.UpdatedAt = now;

                        await _unitOfWork.ProcurementPlanDetailsRepository.UpdateAsync(existingPlanDetail);
                    }
                }

                // Tạo plan detail mới nếu có
                if (dto.ProcurementPlansDetailsCreateDto.Count > 0)
                {
                    string procurementPlanDetailsCode = await _planCode.GenerateProcurementPlanDetailsCodeAsync();
                    var count = GeneratedCodeHelpler.GetGeneratedCodeLastNumber(procurementPlanDetailsCode);
                    foreach (var detailDto in dto.ProcurementPlansDetailsCreateDto)
                    {
                        var newDetail = new ProcurementPlansDetail
                        {
                            PlanDetailsId = Guid.NewGuid(),
                            PlanDetailCode = $"PLD-{now.Year}-{count:D4}",
                            CoffeeTypeId = detailDto.CoffeeTypeId,
                            ProcessMethodId = detailDto.ProcessMethodId,
                            TargetQuantity = detailDto.TargetQuantity,
                            TargetRegion = detailDto.TargetRegion,
                            MinimumRegistrationQuantity = detailDto.MinimumRegistrationQuantity,
                            MinPriceRange = detailDto.MinPriceRange,
                            MaxPriceRange = detailDto.MaxPriceRange,
                            ExpectedYieldPerHectare = detailDto.ExpectedYieldPerHectare,
                            Note = detailDto.Note,
                            Status = ProcurementPlanDetailsStatus.Active.ToString(),
                            ContractItemId = detailDto.ContractItemId,
                            CreatedAt = now,
                            UpdatedAt = now
                        };

                        count++;
                        plan.ProcurementPlansDetails.Add(newDetail);
                        plan.TotalQuantity += detailDto.TargetQuantity;

                        await _unitOfWork.ProcurementPlanDetailsRepository.CreateAsync(newDetail);
                    }
                }

                // Save data to database
                await _unitOfWork.ProcurementPlanRepository.UpdateAsync(plan);
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    var updatedPlan = await _unitOfWork.ProcurementPlanRepository.GetByIdAsync(
                        predicate: p => p.PlanId == plan.PlanId,
                        include: p => p.
                        Include(p => p.CreatedByNavigation).
                        Include(p => p.ProcurementPlansDetails).
                            ThenInclude(d => d.CoffeeType).
                        Include(p => p.ProcurementPlansDetails).
                            ThenInclude(d => d.ProcessMethod),
                        asNoTracking: true
                        );

                    if (updatedPlan == null)
                        return new ServiceResult(
                                Const.WARNING_NO_DATA_CODE,
                                Const.WARNING_NO_DATA_MSG,
                                new ProcurementPlanViewDetailsSumaryDto() //Trả về DTO rỗng
                            );
                    var responseDto = updatedPlan.MapToProcurementPlanViewDetailsDto();

                    return new ServiceResult(
                        Const.SUCCESS_CREATE_CODE,
                        Const.SUCCESS_CREATE_MSG,
                    responseDto
                    );
                }
                else
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        Const.FAIL_CREATE_MSG
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
        public async Task<IServiceResult> UpdateStatus(ProcurementPlanUpdateStatusDto dto, Guid userId, Guid planId)
        {
            try
            {
                //Lấy plan
                var plan = await _unitOfWork.ProcurementPlanRepository.GetByIdAsync(
                    predicate: p => p.PlanId == planId && !p.IsDeleted,
                    include: p => p.
                        Include(p => p.CreatedByNavigation).
                        Include(p => p.ProcurementPlansDetails)
                        );

                if (plan == null || plan.CreatedByNavigation.UserId != userId)
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy kế hoạch hoặc không thuộc quyền quản lý."
                    );

                // Nếu như plan đang ở trạng thái hủy mà được chuyển sang open thì phải set lại endDate về null để tránh trường hợp ngày kết thúc có khả năng trước ngày mở đơn
                plan.StartDate = dto.Status.ToString() == "Open" ? DateHelper.ParseDateOnlyFormatVietNamCurrentTime() : plan.StartDate;
                // Sau khi cập nhật StartDate, nếu như StartDate sau endDate thì nên set EndDate về null)
                if (dto.Status.ToString() == "Open" && plan.StartDate > plan.EndDate)
                    plan.EndDate = null;
                plan.EndDate = dto.Status.ToString() == "Closed" ? DateHelper.ParseDateOnlyFormatVietNamCurrentTime() : plan.EndDate;

                plan.Status = dto.Status.ToString();
                plan.UpdatedAt = DateHelper.NowVietnamTime();

                await _unitOfWork.ProcurementPlanRepository.UpdateAsync(plan);
                var result = await _unitOfWork.SaveChangesAsync();
                if (result > 0)
                {
                    var updatedPlan = await _unitOfWork.ProcurementPlanRepository.GetByIdAsync(
                        predicate: p => p.PlanId == plan.PlanId,
                        include: p => p.
                        Include(p => p.CreatedByNavigation).
                        Include(p => p.ProcurementPlansDetails).
                            ThenInclude(d => d.CoffeeType).
                        Include(p => p.ProcurementPlansDetails).
                            ThenInclude(d => d.ProcessMethod),
                        asNoTracking: true
                        );

                    if (updatedPlan == null)
                        return new ServiceResult(
                                Const.WARNING_NO_DATA_CODE,
                                Const.WARNING_NO_DATA_MSG,
                                new ProcurementPlanViewDetailsSumaryDto() //Trả về DTO rỗng
                            );
                    var responseDto = updatedPlan.MapToProcurementPlanViewDetailsDto();

                    return new ServiceResult(
                        Const.SUCCESS_UPDATE_CODE,
                        Const.SUCCESS_UPDATE_MSG,
                        responseDto
                    );
                }
                else
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        Const.FAIL_UPDATE_MSG
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
