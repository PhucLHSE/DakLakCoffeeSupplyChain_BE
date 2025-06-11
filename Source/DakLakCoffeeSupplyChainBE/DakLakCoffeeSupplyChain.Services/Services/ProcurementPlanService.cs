using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class ProcurementPlanService : IProcurementPlanService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProcurementPlanService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }
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
            var plan = await _unitOfWork.ProcurementPlanRepository.GetByIdAsync(planId);
            ICollection<Guid> planDetails = new HashSet<Guid>(); //Cần sửa lại sau, để tạm

            if (plan == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new ProcurementPlanViewDetailsDto()
                );
            }
            else
            {
                var planDto = plan.MapToProcurementPlanViewDetailsDto(planDetails);

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
                //Kiểm tra người dùng có quyền tạo kế hoạch hay không
                var user = await _unitOfWork.UserAccountRepository.GetUserAccountByIdAsync(procurementPlanDto.CreatedById);
                if (user == null || user.Role.RoleName != "BusinessManager")
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Bạn không có quyền tạo kế hoạch mua hàng."
                    );
                }

                var newPlan = procurementPlanDto.MapToProcurementPlanCreateDto(user.UserId);

                // Save data to database
                await _unitOfWork.ProcurementPlanRepository.CreateAsync(newPlan);
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Map the saved entity to a response DTO
                    //var responseDto = newPlan.MapToUserAccountViewDetailsDto();
                    //responseDto.RoleName = role.RoleName;

                    return new ServiceResult(
                        Const.SUCCESS_CREATE_CODE,
                        Const.SUCCESS_CREATE_MSG
                        //responseDto
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
        public async Task<IServiceResult> DeleteById(Guid planId)
        {
            try
            {
                var plan = await _unitOfWork.ProcurementPlanRepository.GetByIdAsync(planId);
                var planDetails = await _unitOfWork.ProcurementPlanDetailsRepository.
                    GetAllAsync(predicate: p => p.PlanId == planId, asNoTracking: true);
                if (plan == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    if (planDetails.Count != 0)
                    {
                        foreach (var item in planDetails)
                            await _unitOfWork.ProcurementPlanDetailsRepository.RemoveAsync(item);
                    }
                    await _unitOfWork.ProcurementPlanRepository.RemoveAsync(plan);
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
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        //Xóa mềm
        //public async Task<IServiceResult> DeleteById(Guid planId)
        //{
        //    try
        //    {

        //        var plan = await _unitOfWork.ProcurementPlanRepository.GetByIdAsync(planId);

        //        if (plan == null)
        //        {
        //            return new ServiceResult(
        //                Const.WARNING_NO_DATA_CODE,
        //                Const.WARNING_NO_DATA_MSG
        //            );
        //        }
        //        else
        //        {
        //            plan.Status = "Deleted";
        //            await _unitOfWork.ProcurementPlanRepository.UpdateAsync(plan);
        //            var result = await _unitOfWork.SaveChangesAsync();

        //            if (result == Const.SUCCESS_DELETE_CODE)
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
    }
}
