using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.AgriculturalExpertDTOs;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Common.Helpers;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class AgriculturalExpertService : IAgriculturalExpertService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;

        public AgriculturalExpertService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
        }

        // Lấy tất cả chuyên gia (ViewAll)
        public async Task<IServiceResult> GetAllAsync()
        {
            var experts = await _unitOfWork.AgriculturalExpertRepository.GetAllAsync(
                predicate: e => !e.IsDeleted,
                include: query => query.Include(e => e.User),
                orderBy: q => q.OrderBy(e => e.ExpertCode),
                asNoTracking: true
            );

            if (experts == null || !experts.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy chuyên gia nào.",
                    new List<AgriculturalExpertViewAllDto>()
                );
            }

            var dtoList = experts
                .Select(e => e.MapToViewAllDto())
                .ToList();

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG,
                dtoList
            );
        }

        // Lấy chuyên gia theo ID (ViewDetail)
        public async Task<IServiceResult> GetByIdAsync(Guid expertId)
        {
            var expert = await _unitOfWork.AgriculturalExpertRepository.GetByIdAsync(
                predicate: e => e.ExpertId == expertId && !e.IsDeleted,
                include: query => query.Include(e => e.User),
                asNoTracking: true
            );

            if (expert == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy chuyên gia.",
                    new AgriculturalExpertViewDetailDto()
                );
            }

            var dto = expert.MapToViewDetailDto();

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG,
                dto
            );
        }

        // Lấy chuyên gia theo UserId
        public async Task<IServiceResult> GetByUserIdAsync(Guid userId)
        {
            var expert = await _unitOfWork.AgriculturalExpertRepository.GetByUserIdAsync(userId);

            if (expert == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy chuyên gia cho người dùng này.",
                    new AgriculturalExpertViewDetailDto()
                );
            }

            var dto = expert.MapToViewDetailDto();

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG,
                dto
            );
        }

        // Tạo mới chuyên gia
        public async Task<IServiceResult> CreateAsync(AgriculturalExpertCreateDto dto, Guid userId)
        {
            try
            {
                // Lấy thông tin người dùng theo userId
                var user = await _unitOfWork.UserAccountRepository.GetByIdAsync(
                    predicate: u => u.UserId == userId,
                    include: query => query.Include(u => u.Role),
                    asNoTracking: true
                );

                // Kiểm tra người dùng có tồn tại, chưa bị xóa và có vai trò "AgriculturalExpert"
                if (user == null || 
                    user.IsDeleted || 
                    user.Role == null || 
                    user.Role.RoleName != "AgriculturalExpert")
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Người dùng không có quyền tạo tài khoản chuyên gia."
                    );
                }

                // Kiểm tra xem người dùng đã là AgriculturalExpert chưa (tránh trùng)
                var existingExpert = await _unitOfWork.AgriculturalExpertRepository
                    .GetByUserIdAsync(userId);

                if (existingExpert != null)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE, 
                        "Người dùng đã là chuyên gia nông nghiệp."
                    );
                }

                // Sinh mã định danh cho AgriculturalExpert
                string expertCode = await _codeGenerator.GenerateExpertCodeAsync();

                // Ánh xạ dữ liệu từ DTO vào entity
                var newAgriculturalExpert = dto.MapToNewAgriculturalExpert(userId, expertCode);

                // Tạo chuyên gia nông nghiệp ở repository
                await _unitOfWork.AgriculturalExpertRepository.CreateAsync(newAgriculturalExpert);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Ánh xạ thực thể đã lưu sang DTO phản hồi
                    var responseDto = newAgriculturalExpert.MapToViewDetailDto();
                    responseDto.Email = user.Email;
                    responseDto.FullName = user.Name;
                    responseDto.PhoneNumber = user.PhoneNumber;

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

        // Cập nhật chuyên gia
        public async Task<IServiceResult> UpdateAsync(AgriculturalExpertUpdateDto dto, Guid userId, string userRole)
        {
            try
            {
                // Kiểm tra role
                if (userRole != "Admin" &&
                    userRole != "AgriculturalExpert")
                {
                    return new ServiceResult(
                        Const.FAIL_READ_CODE,
                        "Bạn không có quyền truy cập thông tin này."
                    );
                }

                // Nếu là AgriculturalExpert thì chỉ được cập nhật chính mình
                if (userRole == "AgriculturalExpert")
                {
                    var currentExpert = await _unitOfWork.AgriculturalExpertRepository.GetByIdAsync(
                        predicate: e => 
                           e.UserId == userId && 
                           !e.IsDeleted,
                        asNoTracking: true
                    );

                    if (currentExpert == null || 
                        currentExpert.ExpertId != dto.ExpertId)
                    {
                        return new ServiceResult(
                            Const.FAIL_UPDATE_CODE,
                            "Bạn chỉ có quyền cập nhật thông tin của chính bạn."
                        );
                    }
                }

                // Tìm đối tượng AgriculturalExpert cần update theo ID
                var agriculturalExpert = await _unitOfWork.AgriculturalExpertRepository.GetByIdAsync(
                    predicate: e => 
                       e.ExpertId == dto.ExpertId && 
                       !e.IsDeleted,
                    include: query => query.Include(e => e.User)
                );

                // Nếu không tìm thấy
                if (agriculturalExpert == null)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Chuyên gia nông nghiệp không tồn tại hoặc đã bị xóa."
                    );
                }

                // Ánh xạ dữ liệu từ DTO vào entity
                dto.MapToUpdateAgriculturalExpert(agriculturalExpert);

                // Cập nhật agriculturalExpert ở repository
                await _unitOfWork.AgriculturalExpertRepository.UpdateAsync(agriculturalExpert);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Ánh xạ thực thể đã lưu sang DTO phản hồi
                    var responseDto = agriculturalExpert.MapToViewDetailDto();
                    responseDto.Email = agriculturalExpert.User.Email ?? string.Empty;
                    responseDto.FullName = agriculturalExpert.User.Name ?? string.Empty;
                    responseDto.PhoneNumber = agriculturalExpert.User.PhoneNumber ?? string.Empty;

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

        // Xóa cứng chuyên gia
        public async Task<IServiceResult> DeleteAsync(Guid expertId)
        {
            try
            {
                // Tìm chuyên gia theo ID
                var agriculturalExpert = await _unitOfWork.AgriculturalExpertRepository.GetByIdAsync(
                    predicate: e => e.ExpertId == expertId,
                    include: query => query.Include(e => e.User),
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (agriculturalExpert == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Xóa chuyên gia khỏi repository
                    await _unitOfWork.AgriculturalExpertRepository.RemoveAsync(agriculturalExpert);

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
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        // Xóa mềm chuyên gia
        public async Task<IServiceResult> SoftDeleteAsync(Guid expertId, Guid userId, string userRole)
        {
            try
            {
                // Kiểm tra role
                if (userRole != "Admin" && 
                    userRole != "AgriculturalExpert")
                {
                    return new ServiceResult(
                        Const.FAIL_DELETE_CODE,
                        "Bạn không có quyền thực hiện thao tác này."
                    );
                }

                // Nếu là AgriculturalExpert thì chỉ được xóa chính mình
                if (userRole == "AgriculturalExpert")
                {
                    var currentExpert = await _unitOfWork.AgriculturalExpertRepository.GetByIdAsync(
                        predicate: e =>
                            e.UserId == userId &&
                            !e.IsDeleted,
                        asNoTracking: true
                    );

                    if (currentExpert == null || 
                        currentExpert.ExpertId != expertId)
                    {
                        return new ServiceResult(
                            Const.FAIL_DELETE_CODE,
                            "Bạn chỉ được phép xóa thông tin của chính mình."
                        );
                    }
                }

                // Tìm chuyên gia theo ID
                var agriculturalExpert = await _unitOfWork.AgriculturalExpertRepository.GetByIdAsync(
                    predicate: e => e.ExpertId == expertId,
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (agriculturalExpert == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Đánh dấu xoá mềm bằng IsDeleted
                    agriculturalExpert.IsDeleted = true;
                    agriculturalExpert.UpdatedAt = DateHelper.NowVietnamTime();

                    // Cập nhật xoá mềm chuyên gia ở repository
                    await _unitOfWork.AgriculturalExpertRepository.UpdateAsync(agriculturalExpert);

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
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        // Lấy danh sách chuyên gia đã xác thực
        public async Task<IServiceResult> GetVerifiedExpertsAsync()
        {
            var experts = await _unitOfWork.AgriculturalExpertRepository.GetAllAsync(
                predicate: e => e.IsVerified == true && !e.IsDeleted,
                include: query => query.Include(e => e.User),
                orderBy: q => q.OrderBy(e => e.ExpertCode),
                asNoTracking: true
            );

            if (experts == null || !experts.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy chuyên gia đã xác thực nào.",
                    new List<AgriculturalExpertViewAllDto>()
                );
            }

            var dtoList = experts
                .Select(e => e.MapToViewAllDto())
                .ToList();

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG,
                dtoList
            );
        }
    }
}
