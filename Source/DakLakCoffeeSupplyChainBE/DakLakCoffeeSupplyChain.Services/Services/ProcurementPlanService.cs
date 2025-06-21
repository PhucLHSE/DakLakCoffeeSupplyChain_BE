using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.UserAccountDTOs;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class ProcurementPlanService(IUnitOfWork unitOfWork, IProcurementPlanCodeGenerator planCodeGenerator) : IProcurementPlanService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly IProcurementPlanCodeGenerator _planCode = planCodeGenerator ?? throw new ArgumentNullException(nameof(planCodeGenerator));

        //Hiển thị toàn bộ plan đang mở ở màn hình public
        public async Task<IServiceResult> GetAllProcurementPlansAvailable()
        {

            var procurementPlans = await _unitOfWork.ProcurementPlanRepository.GetAllAsync(
                predicate: p => p.Status == "open",
                include: p => p.Include(p => p.CreatedByNavigation),
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
        //Hiển thị toàn bộ plan ở màn hình dashboard của BM
        public async Task<IServiceResult> GetAll()
        {

            var procurementPlans = await _unitOfWork.ProcurementPlanRepository.GetAllAsync(
                predicate: p => p.IsDeleted != true,
                include: p => p.Include(p => p.CreatedByNavigation),
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
                ThenInclude(d => d.CoffeeType), 
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
                ThenInclude(d => d.CoffeeType),
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
        public async Task<IServiceResult> Create(ProcurementPlanCreateDto procurementPlanDto)
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

                //Generate code
                string procurementPlanCode = await _planCode.GenerateProcurementPlanCodeAsync();
                string procurementPlanDetailsCode = await _planCode.GenerateProcurementPlanDetailsCodeAsync();

                //Map dto to model
                var newPlan = procurementPlanDto.MapToProcurementPlanCreateDto(procurementPlanCode, procurementPlanDetailsCode);

                //Nếu như BM để status kế hoạch là open nhưng chưa set StartDate thì kế hoạch sẽ tự động mở đăng ký luôn
                //Nếu BM để status open và đã set StartDate thì kế hoạch mới mở đăng ký khi đến ngày đó thôi.
                if(newPlan.Status == "Open" && !newPlan.StartDate.HasValue)
                    newPlan.StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date);

                //Logic TotalQuantity/TargetQuantity
                foreach(var item in newPlan.ProcurementPlansDetails)
                    newPlan.TotalQuantity += item.TargetQuantity;                    

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
                        ThenInclude(d => d.CoffeeType),
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
    }
}
