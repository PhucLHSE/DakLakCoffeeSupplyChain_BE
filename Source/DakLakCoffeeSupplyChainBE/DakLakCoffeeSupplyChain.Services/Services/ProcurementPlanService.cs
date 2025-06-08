using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.UserAccountDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class ProcurementPlanService : IProcurementPlanService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProcurementPlanService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }
        public async Task<IServiceResult> GetAll()
        {

            var procurementPlans = await _unitOfWork.ProcurementPlanRepository.GetAllAsync();

            if (procurementPlans == null || !procurementPlans.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<UserAccountViewAllDto>()
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
    }
}
