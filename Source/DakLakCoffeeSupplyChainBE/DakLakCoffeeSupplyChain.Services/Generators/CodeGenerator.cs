using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;

namespace DakLakCoffeeSupplyChain.Services.Generators
{
    public class CodeGenerator(IUnitOfWork unitOfWork) : ICodeGenerator
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        private static int CurrentYear => DateTime.UtcNow.Year;

        public async Task<string> GenerateUserCodeAsync()
        {
            // Đếm số user tạo trong năm
            var count = await _unitOfWork.UserAccountRepository.CountUsersRegisteredInYearAsync(CurrentYear);

            return $"USR-{CurrentYear}-{(count + 1):D4}";
        }

        public async Task<string> GenerateManagerCodeAsync()
        {
            // Đếm số manager tạo trong năm
            var count = await _unitOfWork.BusinessManagerRepository.CountBusinessManagersRegisteredInYearAsync(CurrentYear);

            return $"BM-{CurrentYear}-{(count + 1):D4}";
        }

        public async Task<string> GenerateBuyerCodeAsync(Guid managerId)
        {
            // Đếm số buyer mà manager này đã tạo trong năm
            var count = await _unitOfWork.BusinessBuyerRepository.CountBuyersCreatedByManagerInYearAsync(managerId, CurrentYear);

            // Lấy ManagerCode để dùng làm tiền tố
            var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                predicate: m => m.ManagerId == managerId && !m.IsDeleted,
                asNoTracking: true
            );

            var managerCode = manager?.ManagerCode;

            if (string.IsNullOrWhiteSpace(managerCode))
                managerCode = "BM-UNKNOWN"; // fallback

            return $"{managerCode}-BUY-{CurrentYear}-{(count + 1):D3}";
        }

        public async Task<string> GenerateContractCodeAsync()
        {
            // Đếm số hợp đồng tạo trong năm hiện tại
            var count = await _unitOfWork.ContractRepository.CountContractsInYearAsync(CurrentYear);

            // Trả về mã có dạng: CTR-2025-0032
            return $"CTR-{CurrentYear}-{(count + 1):D4}";
        }

        public async Task<string> GenerateContractItemCodeAsync(Guid contractId)
        {
            // Lấy contract để có ContractCode
            var contract = await _unitOfWork.ContractRepository.GetByIdAsync(
                predicate: c => c.ContractId == contractId && 
                                !c.IsDeleted,
                asNoTracking: true
            );

            if (contract == null || string.IsNullOrWhiteSpace(contract.ContractCode))
            {
                return $"CTI-UNKNOWN-{CurrentYear}-{Guid.NewGuid().ToString()[..3]}"; // fallback tránh null
            }

            var contractCode = contract.ContractCode.Replace(" ", "").ToUpper();

            // Đếm số item trong hợp đồng này
            var count = await _unitOfWork.ContractItemRepository.CountByContractIdAsync(contractId);

            // Format: CTI-001-HDX2025
            return $"CTI-{(count + 1):D3}-{contractCode}";
        }

        public async Task<string> GenerateDeliveryBatchCodeAsync()
        {
            // Đếm số lô giao hàng đã tạo trong năm hiện tại
            var count = await _unitOfWork.ContractDeliveryBatchRepository.CountByYearAsync(CurrentYear);

            // Trả về mã có dạng: DELB-2025-0034
            return $"DELB-{CurrentYear}-{(count + 1):D4}";
        }

        public async Task<string> GenerateContractDeliveryItemCodeAsync(Guid deliveryBatchId)
        {
            // Lấy contractDeliveryBatch để có DeliveryBatchCode
            var contractDeliveryBatch = await _unitOfWork.ContractDeliveryBatchRepository.GetByIdAsync(
                predicate: cdb => cdb.DeliveryBatchId == deliveryBatchId &&
                                !cdb.IsDeleted,
                asNoTracking: true
            );

            if (contractDeliveryBatch == null || string.IsNullOrWhiteSpace(contractDeliveryBatch.DeliveryBatchCode))
            {
                return $"DLI-UNKNOWN-{CurrentYear}-{Guid.NewGuid().ToString()[..3]}"; // fallback tránh null
            }

            var deliveryBatchCode = contractDeliveryBatch.DeliveryBatchCode.Replace(" ", "").ToUpper();

            // Đếm số item trong danh mục giao của hợp đồng này
            var count = await _unitOfWork.ContractDeliveryItemRepository.CountByDeliveryBatchIdAsync(deliveryBatchId);

            // Format: DLI-001-HDX2025
            return $"DLI-{(count + 1):D3}-{deliveryBatchCode}";
        }

        public async Task<string> GenerateCropSeasonCodeAsync(int year)
        {
            int count = await _unitOfWork.CropSeasonRepository.CountByYearAsync(year);

            return $"SEASON-{year}-{(count + 1):D4}";
        }

        public async Task<string> GenerateProcurementPlanCodeAsync()
        {
            // Đếm số procurement plan tạo trong năm
            var count = await _unitOfWork.ProcurementPlanRepository.CountProcurementPlansInYearAsync(CurrentYear);

            return $"PLAN-{CurrentYear}-{(count + 1):D4}";
        }

        public async Task<string> GenerateProcurementPlanDetailsCodeAsync()
        {
            // Đếm số procurement plan detail tạo trong năm
            var count = await _unitOfWork.ProcurementPlanDetailsRepository.CountProcurementPlanDetailsInYearAsync(CurrentYear);

            return $"PLD-{CurrentYear}-{(count + 1):D4}";
        }

        public async Task<string> GenerateCoffeeTypeCodeAsync()
        {
            var count = await _unitOfWork.CoffeeTypeRepository.CountCoffeeTypeInYearAsync(CurrentYear);

            return $"CFT-{CurrentYear}-{(count + 1):D4}";
        }

        public async Task<string> GenerateFarmerCodeAsync()
        {
            var count = await _unitOfWork.FarmerRepository.CountFarmerInYearAsync(CurrentYear);
            return $"FRM-{CurrentYear}-{(count + 1):D4}";
        }

        public async Task<string> GenerateGeneralFarmerReportCodeAsync()
        {
            var count = await _unitOfWork.GeneralFarmerReportRepository.CountReportsInYearAsync(CurrentYear);

            return $"RPT-{CurrentYear}-{(count + 1):D4}";
        }

        public async Task<string> GenerateCultivationRegistrationCodeAsync()
        {
            var count = await _unitOfWork.CultivationRegistrationRepository.CountCultivationRegistrationsInYearAsync(CurrentYear);

            return $"REG-{CurrentYear}-{(count + 1):D4}";
        }
        public async Task<string> GenerateStaffCodeAsync()
        {
            var count = await _unitOfWork.BusinessStaffRepository.CountStaffCreatedInYearAsync(CurrentYear);
            return $"STF-{CurrentYear}-{(count + 1):D4}";
        }
        public async Task<string> GenerateInboundRequestCodeAsync()
        {
            var count = await _unitOfWork.WarehouseInboundRequests.CountInboundRequestsInYearAsync(DateTime.UtcNow.Year);
            return $"IR-{DateTime.UtcNow:yyyy}-{(count + 1):D4}";
        }
        public async Task<string> GenerateWarehouseCodeAsync()
        {
            var count = await _unitOfWork.Warehouses.CountWarehousesCreatedInYearAsync(CurrentYear);
            return $"WH-{CurrentYear}-{(count + 1):D4}";
        }
        public async Task<string> GenerateWarehouseReceiptCodeAsync()
        {
            var count = await _unitOfWork.WarehouseReceipts.CountCreatedInYearAsync(CurrentYear);
            return $"WR-{CurrentYear}-{(count + 1):D4}";
        }
        public async Task<string> GenerateInventoryCodeAsync()
        {
            var count = await _unitOfWork.Inventories.CountCreatedInYearAsync(DateTime.UtcNow.Year);
            return $"INV-{DateTime.UtcNow:yyyy}-{(count + 1):D4}";
        }
    }
}
