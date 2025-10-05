using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.CropEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using DakLakCoffeeSupplyChain.Services.Generators;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class CropService : ICropService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;

        public CropService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork
                ?? throw new ArgumentNullException(nameof(unitOfWork));

            _codeGenerator = codeGenerator
                ?? throw new ArgumentNullException(nameof(codeGenerator));
        }

        private bool IsDakLakAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return false;

            var lowerAddress = address.ToLower();
            
            // Check for Đắk Lắk region keywords
            var dakLakKeywords = new[]
            {
                "đắk lắk", "dak lak", "buôn ma thuột", "buon ma thuot",
                "ea ", "krông", "krong", "cư ", "cu ", "lắk", "lak",
                "m'drắk", "mdrak", "ea h'leo", "ea kar", "ea súp",
                "ea drăng", "ea hiao", "ea wy", "ea khăl", "ea rốk",
                "ea bung", "ea wer", "ea nuôl", "ea kiết", "ea tul",
                "ea m'droh", "ea drông", "ea knốp", "ea păl", "ea ô",
                "ea riêng", "ea trang", "ea kly", "ea phê", "ea knuếc",
                "ea ning", "ea ktur", "ea na", "krông năng", "krông pắc",
                "krông búk", "krông ana", "krông bông", "krông nô", "krông á",
                "cư m'gar", "cư jút", "cư yang", "cư prao", "cư m'ta",
                "cư pui", "cư pơng", "cư bao", "đắk mil", "đắk r'lấp",
                "đắk song", "đắk glong", "đắk liêng", "đắk phơi", "lắk",
                "m'drắk", "tuy đức", "buôn đôn", "dang kang", "yang mao",
                "hòa sơn", "liên sơn lắk", "nam ka", "dray bhăng", "dur kmăl"
            };

            return dakLakKeywords.Any(keyword => lowerAddress.Contains(keyword));
        }

        public async Task<IServiceResult> GetAllCrops(Guid userId, string userRole)
        {
            // Kiểm tra quyền truy cập
            if (userRole != "Admin" && userRole != "Farmer")
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    $"Người dùng có role '{userRole}' không có quyền truy cập. Chỉ Farmer và Admin mới được phép.",
                    new List<CropViewAllDto>()
                );
            }

            // Get farmer if user is farmer
            Farmer farmer = null;
            if (userRole == "Farmer")
            {
                farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                    predicate: f => f.UserId == userId && !f.IsDeleted,
                    asNoTracking: true
                );
                // Note: farmer có thể null nếu chưa có Farmer record
            }

            // Get crops based on role
            List<Crop> crops;
            
            if (userRole == "Admin")
            {
                // Admin can see all crops
                crops = await _unitOfWork.CropRepository.GetAllAsync(
                    predicate: c => (c.IsDeleted == null || c.IsDeleted == false),
                    orderBy: query => query.OrderBy(c => c.CreatedAt),
                    asNoTracking: true
                );
            }
            else if (userRole == "Farmer")
            {
                if (farmer != null)
                {
                    // Farmer có Farmer record - xem crops của mình
                    crops = await _unitOfWork.CropRepository.GetAllAsync(
                        predicate: c => (c.IsDeleted == null || c.IsDeleted == false) && 
                                        c.CreatedBy == farmer.FarmerId,
                        orderBy: query => query.OrderBy(c => c.CreatedAt),
                        asNoTracking: true
                    );
                }
                else
                {
                    // Farmer chưa có Farmer record - không có crops nào
                    crops = new List<Crop>();
                }
            }
            else
            {
                // User không phải Admin và không phải Farmer
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Người dùng không có quyền truy cập.",
                    new List<CropViewAllDto>()
                );
            }

            // Check if no data
            if (crops == null || !crops.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<CropViewAllDto>()
                );
            }
            else
            {
                // Convert to DTO list for client
                var cropDtos = crops
                    .Select(crops => crops.MapToCropViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    cropDtos
                );
            }
        }

        public async Task<IServiceResult> GetCropById(Guid cropId, Guid userId, string userRole)
        {
            // Kiểm tra quyền truy cập
            if (userRole != "Admin" && userRole != "Farmer")
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    $"Người dùng có role '{userRole}' không có quyền truy cập. Chỉ Farmer và Admin mới được phép."
                );
            }

            // Get farmer if user is farmer
            Farmer farmer = null;
            if (userRole == "Farmer")
            {
                farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                    predicate: f => f.UserId == userId && !f.IsDeleted,
                    asNoTracking: true
                );
                // Note: farmer có thể null nếu chưa có Farmer record
            }

            // Get crop by ID and check ownership
            Crop crop;
            
            if (userRole == "Admin")
            {
                // Admin can see any crop
                crop = await _unitOfWork.CropRepository.GetByIdAsync(
                    predicate: c => c.CropId == cropId &&
                                   (c.IsDeleted == null || c.IsDeleted == false),
                    asNoTracking: true
                );
            }
            else if (userRole == "Farmer")
            {
                if (farmer != null)
                {
                    // Farmer có Farmer record - xem crop của mình
                    crop = await _unitOfWork.CropRepository.GetByIdAsync(
                        predicate: c => c.CropId == cropId &&
                                       (c.IsDeleted == null || c.IsDeleted == false) &&
                                       c.CreatedBy == farmer.FarmerId,
                        asNoTracking: true
                    );
                }
                else
                {
                    // Farmer chưa có Farmer record - không có crop nào
                    crop = null;
                }
            }
            else
            {
                // User không phải Admin và không phải Farmer
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Người dùng không có quyền truy cập."
                );
            }

            if (crop == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy Crop hoặc bạn không có quyền truy cập."
                );
            }

            var cropDto = crop.MapToCropViewDetailsDto();

            // Lấy media files cho crop
            try
            {
                var mediaFiles = await _unitOfWork.MediaFileRepository.GetAllAsync(
                    m => !m.IsDeleted && m.RelatedEntity == "Crop" && m.RelatedId == cropId,
                    orderBy: q => q.OrderByDescending(m => m.UploadedAt)
                );

                cropDto.Images = mediaFiles.Where(m => m.MediaType == "image").Select(m => m.MediaUrl).ToList();
                cropDto.Videos = mediaFiles.Where(m => m.MediaType == "video").Select(m => m.MediaUrl).ToList();
                cropDto.Documents = mediaFiles.Where(m => m.MediaType == "document").Select(m => m.MediaUrl).ToList();
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng không fail toàn bộ request
                cropDto.Images = new List<string>();
                cropDto.Videos = new List<string>();
                cropDto.Documents = new List<string>();
            }

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG,
                cropDto
            );
        }

        public async Task<IServiceResult> CreateCrop(CropCreateDto cropCreateDto, Guid farmerUserId)
        {
            // Validate input data
            if (string.IsNullOrWhiteSpace(cropCreateDto.Address))
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    "Địa chỉ trang trại là bắt buộc"
                );
            }

            if (string.IsNullOrWhiteSpace(cropCreateDto.FarmName))
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    "Tên trang trại là bắt buộc"
                );
            }

            // Validate address length (đã được validate bởi DataAnnotations nhưng double-check)
            if (cropCreateDto.Address.Trim().Length < 10 || cropCreateDto.Address.Trim().Length > 200)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    "Địa chỉ phải từ 10-200 ký tự"
                );
            }

            // Validate farm name length (đã được validate bởi DataAnnotations nhưng double-check)
            if (cropCreateDto.FarmName.Trim().Length < 3 || cropCreateDto.FarmName.Trim().Length > 100)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    "Tên trang trại phải từ 3-100 ký tự"
                );
            }

            // Validate farm name characters (đã được validate bởi DataAnnotations nhưng double-check)
            var farmNameRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-ZÀ-ỹ0-9\s\-_.,()]+$");
            if (!farmNameRegex.IsMatch(cropCreateDto.FarmName.Trim()))
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    "Tên trang trại chỉ được chứa chữ cái, số, dấu cách và các ký tự: - _ . , ( )"
                );
            }

            // Validate crop area if provided (đã được validate bởi DataAnnotations nhưng double-check)
            if (cropCreateDto.CropArea.HasValue)
            {
                if (cropCreateDto.CropArea.Value < 0.01m || cropCreateDto.CropArea.Value > 10000m)
                {
                    return new ServiceResult(
                        Const.ERROR_EXCEPTION,
                        "Diện tích phải từ 0.01-10,000 ha"
                    );
                }
            }

            // Validate Note length if provided
            if (!string.IsNullOrWhiteSpace(cropCreateDto.Note) &&
                cropCreateDto.Note.Length > 1000)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    "Ghi chú không được vượt quá 1000 ký tự"
                );
            }

            // Validate Đắk Lắk address
            if (!IsDakLakAddress(cropCreateDto.Address))
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    "Địa chỉ phải thuộc khu vực Đắk Lắk. Vui lòng chọn từ danh sách gợi ý."
                );
            }

            // Get farmer from farmerUserId
            var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                predicate: f =>
                   f.UserId == farmerUserId &&
                   !f.IsDeleted,
                asNoTracking: false
            );

            if (farmer == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy Farmer tương ứng với tài khoản."
                );
            }

            // Check if address already exists for this farmer
            var existingCrop = await _unitOfWork.CropRepository.GetByIdAsync(
                predicate: c => 
                    c.Address == cropCreateDto.Address && 
                    c.CreatedBy == farmer.FarmerId && 
                    (c.IsDeleted == null || (c.IsDeleted == null || c.IsDeleted == false)),
                asNoTracking: true
            );

            if (existingCrop != null)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    "Địa chỉ này đã được sử dụng cho vùng trồng khác. Vui lòng chọn địa chỉ khác."
                );
            }

            // Generate CropCode using code generator
            var cropCode = await _codeGenerator.GenerateCropCodeAsync();

            // Create new crop
            var newCrop = cropCreateDto.MapToCreateCrop(farmer.FarmerId, cropCode);

            await _unitOfWork.CropRepository.CreateAsync(newCrop);
            await _unitOfWork.SaveChangesAsync();

            var cropDto = newCrop.MapToCropViewAllDto();

            return new ServiceResult(
                Const.SUCCESS_CREATE_CODE,
                Const.SUCCESS_CREATE_MSG,
                cropDto
            );
        }

        public async Task<IServiceResult> UpdateCrop(CropUpdateDto cropUpdateDto, Guid farmerUserId)
        {
            // Validate input data
            if (string.IsNullOrWhiteSpace(cropUpdateDto.Address))
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    "Địa chỉ trang trại là bắt buộc"
                );
            }

            if (string.IsNullOrWhiteSpace(cropUpdateDto.FarmName))
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    "Tên trang trại là bắt buộc"
                );
            }

            // Validate address length
            if (cropUpdateDto.Address.Trim().Length < 10 || cropUpdateDto.Address.Trim().Length > 200)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    "Địa chỉ phải từ 10-200 ký tự"
                );
            }

            // Validate farm name length
            if (cropUpdateDto.FarmName.Trim().Length < 3 || cropUpdateDto.FarmName.Trim().Length > 100)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    "Tên trang trại phải từ 3-100 ký tự"
                );
            }

            // Validate farm name characters
            var farmNameRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-ZÀ-ỹ0-9\s\-_.,()]+$");
            if (!farmNameRegex.IsMatch(cropUpdateDto.FarmName.Trim()))
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    "Tên trang trại chỉ được chứa chữ cái, số, dấu cách và các ký tự: - _ . , ( )"
                );
            }

            // Validate crop area if provided
            if (cropUpdateDto.CropArea.HasValue)
            {
                if (cropUpdateDto.CropArea.Value < 0.01m || cropUpdateDto.CropArea.Value > 10000m)
                {
                    return new ServiceResult(
                        Const.ERROR_EXCEPTION,
                        "Diện tích phải từ 0.01-10,000 ha"
                    );
                }
            }

            // Validate Note length if provided
            if (!string.IsNullOrWhiteSpace(cropUpdateDto.Note) &&
                cropUpdateDto.Note.Length > 1000)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    "Ghi chú không được vượt quá 1000 ký tự"
                );
            }

            // Validate Đắk Lắk address
            if (!IsDakLakAddress(cropUpdateDto.Address))
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    "Địa chỉ phải thuộc khu vực Đắk Lắk. Vui lòng chọn từ danh sách gợi ý."
                );
            }

            // Get farmer from farmerUserId
            var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                predicate: f =>
                   f.UserId == farmerUserId &&
                   !f.IsDeleted,
                asNoTracking: false
            );

            if (farmer == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy Farmer tương ứng với tài khoản."
                );
            }

            // Get current crop and check ownership
            var existingCrop = await _unitOfWork.CropRepository.GetByIdAsync(
                predicate: c =>
                   c.CropId == cropUpdateDto.CropId &&
                   (c.IsDeleted == null || c.IsDeleted == false) &&
                   c.CreatedBy == farmer.FarmerId,
                asNoTracking: false
            );

            if (existingCrop == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy Crop hoặc bạn không có quyền chỉnh sửa."
                );
            }

            // Check if new address already exists for this farmer (excluding current crop)
            if (existingCrop.Address != cropUpdateDto.Address)
            {
                var duplicateCrop = await _unitOfWork.CropRepository.GetByIdAsync(
                    predicate: c => 
                        c.Address == cropUpdateDto.Address && 
                        c.CreatedBy == farmer.FarmerId && 
                        c.CropId != cropUpdateDto.CropId &&
                        (c.IsDeleted == null || (c.IsDeleted == null || c.IsDeleted == false)),
                    asNoTracking: true
                );

                if (duplicateCrop != null)
                {
                    return new ServiceResult(
                        Const.ERROR_EXCEPTION,
                        "Địa chỉ này đã được sử dụng cho vùng trồng khác. Vui lòng chọn địa chỉ khác."
                    );
                }
            }

            // Update crop
            cropUpdateDto.MapToUpdateCrop(existingCrop, farmer.FarmerId);

            await _unitOfWork.CropRepository.UpdateAsync(existingCrop);
            await _unitOfWork.SaveChangesAsync();

            var cropDto = existingCrop.MapToCropViewAllDto();

            return new ServiceResult(
                Const.SUCCESS_UPDATE_CODE,
                Const.SUCCESS_UPDATE_MSG,
                cropDto
            );
        }

        public async Task<IServiceResult> SoftDeleteCrop(Guid cropId, Guid farmerUserId)
        {
            // Get farmer from farmerUserId
            var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                predicate: f =>
                   f.UserId == farmerUserId &&
                   !f.IsDeleted,
                asNoTracking: false
            );

            if (farmer == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy Farmer tương ứng với tài khoản."
                );
            }

            // Get current crop and check ownership
            var existingCrop = await _unitOfWork.CropRepository.GetByIdAsync(
                predicate: c =>
                   c.CropId == cropId &&
                   (c.IsDeleted == null || c.IsDeleted == false) &&
                   c.CreatedBy == farmer.FarmerId,
                asNoTracking: false
            );

            if (existingCrop == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy Crop hoặc bạn không có quyền xóa."
                );
            }

            // Soft delete crop
            existingCrop.IsDeleted = true;
            existingCrop.UpdatedAt = DateTime.UtcNow;
            existingCrop.UpdatedBy = farmer.FarmerId;

            await _unitOfWork.CropRepository.UpdateAsync(existingCrop);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(
                Const.SUCCESS_DELETE_CODE,
                "Xóa mềm vùng trồng thành công"
            );
        }

        public async Task<IServiceResult> HardDeleteCrop(Guid cropId, Guid farmerUserId)
        {
            // Get farmer from farmerUserId
            var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                predicate: f =>
                   f.UserId == farmerUserId,
                asNoTracking: false
            );

            if (farmer == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy Farmer tương ứng với tài khoản."
                );
            }

            // Get current crop and check ownership (including soft-deleted ones)
            var existingCrop = await _unitOfWork.CropRepository.GetByIdAsync(
                predicate: c =>
                   c.CropId == cropId &&
                   c.CreatedBy == farmer.FarmerId,
                asNoTracking: false
            );

            if (existingCrop == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy Crop hoặc bạn không có quyền xóa."
                );
            }

            // Hard delete crop (permanently remove from database)
            await _unitOfWork.CropRepository.RemoveAsync(existingCrop);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(
                Const.SUCCESS_DELETE_CODE,
                "Xóa vĩnh viễn vùng trồng thành công"
            );
        }

        public async Task AutoUpdateCropStatusAsync(Guid cropId)
        {
            try
            {
                var crop = await _unitOfWork.CropRepository.GetByIdAsync(
                    predicate: c => c.CropId == cropId && (c.IsDeleted == null || (c.IsDeleted == null || c.IsDeleted == false)),
                    include: q => q.Include(c => c.CropSeasonDetails),
                    asNoTracking: true
                );

                if (crop == null) return;

                // Lấy tất cả CropSeason thông qua CropSeasonDetail
                var cropSeasons = await _unitOfWork.CropSeasonRepository.GetAllAsync(
                    predicate: cs => cs.CropSeasonDetails.Any(csd => csd.CropId == cropId && !csd.IsDeleted) && !cs.IsDeleted,
                    asNoTracking: true
                );

                if (!cropSeasons.Any())
                {
                    // Không có CropSeason nào liên kết với Crop này -> Inactive
                    await UpdateCropStatusAsync(cropId, CropStatus.Inactive);
                    return;
                }

                // Đếm số lượng CropSeason theo trạng thái
                var activeCount = cropSeasons.Count(cs => cs.Status == "Active");
                var completedCount = cropSeasons.Count(cs => cs.Status == "Completed");
                var cancelledCount = cropSeasons.Count(cs => cs.Status == "Cancelled");
                var totalCount = cropSeasons.Count();

                CropStatus newStatus;

                // Logic xác định trạng thái mới:
                if (activeCount > 0)
                {
                    // Có ít nhất 1 CropSeason đang Active -> Crop Active
                    newStatus = CropStatus.Active;
                }
                else if (completedCount == totalCount)
                {
                    // Tất cả CropSeason đã Completed -> Crop Active (vẫn có thể tạo mùa mới)
                    newStatus = CropStatus.Active;
                }
                else if (cancelledCount == totalCount)
                {
                    // Tất cả CropSeason bị Cancelled -> Crop Inactive
                    newStatus = CropStatus.Inactive;
                }
                else
                {
                    // Mix trạng thái -> Crop Active
                    newStatus = CropStatus.Active;
                }

                // Cập nhật status nếu có thay đổi
                var currentStatus = Enum.TryParse<CropStatus>(crop.Status, out var parsedStatus) 
                    ? parsedStatus 
                    : CropStatus.Inactive;

                if (newStatus != currentStatus)
                {
                    await UpdateCropStatusAsync(cropId, newStatus);
                }
            }
            catch (Exception ex)
            {
                _ = ex;
                return;
            }
        }

        private async Task UpdateCropStatusAsync(Guid cropId, CropStatus newStatus)
        {
            await _unitOfWork.CropRepository.GetAllQueryable()
                .Where(c => c.CropId == cropId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.Status, newStatus.ToString())
                    .SetProperty(c => c.UpdatedAt, DateHelper.NowVietnamTime()));
        }

        public async Task<IServiceResult> ApproveCropAsync(Guid cropId, CropApproveDto dto, Guid adminUserId)
        {
            // Kiểm tra crop tồn tại và chưa được duyệt
            var crop = await _unitOfWork.CropRepository.GetByIdAsync(
                predicate: c => c.CropId == cropId && 
                               (c.IsDeleted == null || c.IsDeleted == false),
                asNoTracking: false
            );

            if (crop == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy Crop hoặc Crop đã bị xóa."
                );
            }

            // Kiểm tra đã được duyệt chưa
            if (crop.IsApproved == true)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    "Crop này đã được duyệt rồi."
                );
            }

            // Duyệt crop
            crop.IsApproved = true;
            crop.ApprovedAt = DateHelper.NowVietnamTime();
            crop.ApprovedBy = adminUserId;
            crop.RejectReason = null; // Xóa lý do từ chối nếu có
            crop.UpdatedAt = DateHelper.NowVietnamTime();
            // Note: Không set UpdatedBy vì Admin không phải Farmer

            await _unitOfWork.CropRepository.UpdateAsync(crop);
            await _unitOfWork.SaveChangesAsync();

            var cropDto = crop.MapToCropViewDetailsDto();

            return new ServiceResult(
                Const.SUCCESS_UPDATE_CODE,
                "Duyệt Crop thành công",
                cropDto
            );
        }

        public async Task<IServiceResult> RejectCropAsync(Guid cropId, CropRejectDto dto, Guid adminUserId)
        {
            // Kiểm tra crop tồn tại và chưa được duyệt
            var crop = await _unitOfWork.CropRepository.GetByIdAsync(
                predicate: c => c.CropId == cropId && 
                               (c.IsDeleted == null || c.IsDeleted == false),
                asNoTracking: false
            );

            if (crop == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy Crop hoặc Crop đã bị xóa."
                );
            }

            // Kiểm tra đã được duyệt chưa
            if (crop.IsApproved == true)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    "Crop này đã được duyệt rồi, không thể từ chối."
                );
            }

            // Từ chối crop
            crop.IsApproved = false;
            crop.ApprovedAt = DateHelper.NowVietnamTime();
            crop.ApprovedBy = adminUserId;
            crop.RejectReason = dto.RejectReason;
            crop.UpdatedAt = DateHelper.NowVietnamTime();
            // Note: Không set UpdatedBy vì Admin không phải Farmer

            await _unitOfWork.CropRepository.UpdateAsync(crop);
            await _unitOfWork.SaveChangesAsync();

            var cropDto = crop.MapToCropViewDetailsDto();

            return new ServiceResult(
                Const.SUCCESS_UPDATE_CODE,
                "Từ chối Crop thành công",
                cropDto
            );
        }
    }
}
