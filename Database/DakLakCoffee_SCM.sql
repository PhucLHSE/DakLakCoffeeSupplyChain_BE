USE master

GO

IF EXISTS (SELECT * FROM sys.databases WHERE name = 'DakLakCoffee_SCM')
BEGIN
    DROP DATABASE DakLakCoffee_SCM;
END

GO

CREATE DATABASE DakLakCoffee_SCM;

GO

USE DakLakCoffee_SCM;

GO

-- Table Roles
CREATE TABLE Roles (
  RoleID INT PRIMARY KEY IDENTITY(1,1),                        -- ID vai trò
  RoleName NVARCHAR(255) NOT NULL,                             -- Tên vai trò (Admin, BusinessManager, BusinessStaff, Farmer, AgriculturalExpert, DeliveryStaff)
  Description NVARCHAR(2000),                                  -- Mô tả vai trò
  Status NVARCHAR(50) DEFAULT 'Active',                        -- Trạng thái vai trò
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,       -- Ngày tạo
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,       -- Ngày cập nhật
  IsDeleted BIT NOT NULL DEFAULT 0                             -- 0 = chưa xoá, 1 = đã xoá mềm
);

GO

-- Table Users
CREATE TABLE UserAccounts (
  UserID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),          -- ID người dùng
  UserCode VARCHAR(20) UNIQUE,                                  -- Auto-gen như USR-2024-0001 để phục vụ QR/mã truy xuất nếu cần hiển thị công khai.
  Email NVARCHAR(255) UNIQUE NOT NULL,                          -- Email
  PhoneNumber NVARCHAR(20) UNIQUE,                              -- SĐT (nếu đăng ký số)
  Name NVARCHAR(255) NOT NULL,                                  -- Họ tên đầy đủ
  Gender NVARCHAR(20),                                          -- Giới tính (Male/Female/Other)
  DateOfBirth DATE,                                             -- Ngày sinh
  Address NVARCHAR(255),                                        -- Địa chỉ cư trú
  ProfilePictureUrl NVARCHAR(500),                              -- Ảnh đại diện nhị phân
  PasswordHash NVARCHAR(255) NOT NULL,                          -- Mật khẩu mã hóa
  RegistrationDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, -- Ngày đăng ký tài khoản
  LastLogin DATETIME,                                           -- Lần đăng nhập gần nhất
  EmailVerified BIT DEFAULT 0,                                  -- Trạng thái xác thực email
  VerificationCode VARCHAR(10),                                 -- Mã xác thực nếu cần
  IsVerified BIT DEFAULT 0,                                     -- Đã xác thực (qua OTP/email)
  LoginType NVARCHAR(20) DEFAULT 'System',                      -- Phương thức login: system, google,...
  Status NVARCHAR(20) DEFAULT 'Active',                         -- Trạng thái tài khoản
  RoleID INT NOT NULL,                                          -- Vai trò người dùng
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,        -- Ngày cập nhật
  IsDeleted BIT NOT NULL DEFAULT 0                              -- 0 = chưa xoá, 1 = đã xoá mềm

  -- Foreign Keys
  CONSTRAINT FK_UserAccounts_RoleID 
      FOREIGN KEY (RoleID) REFERENCES Roles(RoleID)
);

GO

-- Table PaymentConfigurations – Cấu hình các loại phí
CREATE TABLE PaymentConfigurations (
  ConfigID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),         -- ID cấu hình
  RoleID INT NOT NULL,                                           -- Vai trò áp dụng (Farmer, BusinessManager...)
  FeeType NVARCHAR(50) NOT NULL,                                 -- Loại phí: 'Registration', 'MonthlyFee', ...
  Amount FLOAT NOT NULL,                                         -- Số tiền
  Description NVARCHAR(500),                                     -- Mô tả thêm (nếu có)
  EffectiveFrom DATE NOT NULL,                                   -- Hiệu lực từ ngày
  EffectiveTo DATE,                                              -- Hết hiệu lực (null nếu chưa biết)
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  IsActive BIT DEFAULT 1,                                        -- Có đang được áp dụng không
  IsDeleted BIT NOT NULL DEFAULT 0,                              -- Xoá mềm: 0 = còn hoạt động, 1 = đã xoá

  -- Foreign Keys
  CONSTRAINT FK_PaymentConfigurations_Role                       -- FK tới bảng Roles
      FOREIGN KEY (RoleID) REFERENCES Roles(RoleID)
);

GO

-- Table Payments
CREATE TABLE Payments (
  PaymentID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),       -- ID thanh toán (tự sinh UUID) 
  Email NVARCHAR(255) NOT NULL,                                 -- Email người đăng ký (dùng để xác định tạm khi chưa tạo tài khoản)
  ConfigID UNIQUEIDENTIFIER NOT NULL,                           -- FK tới PaymentConfigurations
  UserID UNIQUEIDENTIFIER NULL,                                 -- Liên kết tới UserAccounts sau khi được duyệt (NULL lúc đầu)
  PaymentCode VARCHAR(20) UNIQUE,                               -- Mã thanh toán duy nhất, ví dụ: PAY-2025-0001
  PaymentAmount FLOAT NOT NULL,                                 -- Số tiền thanh toán
  PaymentMethod NVARCHAR(50),                                   -- Phương thức thanh toán (VNPay, Momo, Banking, ...)
  PaymentPurpose NVARCHAR(100),                                 -- Registration, MonthlyFee,...
  PaymentStatus NVARCHAR(50) DEFAULT 'Pending',                 -- Trạng thái: Pending, Success, Failed, Refunded
  PaymentTime DATETIME,                                         -- Thời điểm thanh toán thành công
  AdminVerified BIT DEFAULT 0,                                  -- Được admin duyệt chưa: 0 = chưa, 1 = đã duyệt
  RefundReason NVARCHAR(MAX),                                   -- Lý do hoàn tiền (nếu bị từ chối)
  RefundTime DATETIME,                                          -- Thời điểm hoàn tiền (nếu có)
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,        -- Thời điểm tạo bản ghi
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,        -- Thời điểm cập nhật bản ghi gần nhất
  RelatedEntityID UNIQUEIDENTIFIER,                             -- ID thực thể liên quan nếu có
  IsDeleted BIT NOT NULL DEFAULT 0,                             -- Xoá mềm: 0 = còn hoạt động, 1 = đã xoá

  -- Foreign Keys
  CONSTRAINT FK_Payments_Config 
      FOREIGN KEY (ConfigID) REFERENCES PaymentConfigurations(ConfigID), -- FK tới bảng PaymentConfigurations
  
  CONSTRAINT FK_RegistrationPayments_UserID 
      FOREIGN KEY (UserID) REFERENCES UserAccounts(UserID)        -- FK tới bảng UserAccounts (nếu đã tạo tài khoản)
);

GO

-- Table Wallets
CREATE TABLE Wallets (
  WalletID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),     -- ID ví
  UserID UNIQUEIDENTIFIER UNIQUE NULL,                       -- NULL nếu là ví hệ thống
  WalletType NVARCHAR(50) NOT NULL,                          -- 'System', 'Business', 'Farmer', ...
  TotalBalance FLOAT NOT NULL DEFAULT 0,                     -- Tổng số dư hiện tại
  LastUpdated DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,   -- Cập nhật gần nhất
  IsDeleted BIT NOT NULL DEFAULT 0,

   -- Foreign Keys
  CONSTRAINT FK_Wallets_UserID                               -- FK nếu có user
      FOREIGN KEY (UserID) REFERENCES UserAccounts(UserID)
);

GO

-- Table WalletTransactions
CREATE TABLE WalletTransactions (
  TransactionID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), -- ID giao dịch ví
  WalletID UNIQUEIDENTIFIER NOT NULL,                         -- Ví bị ảnh hưởng
  PaymentID UNIQUEIDENTIFIER NULL,                            -- Liên kết đến bảng Payments nếu là giao dịch thanh toán
  Amount FLOAT NOT NULL,                                      -- Số tiền (dương: cộng, âm: trừ)
  TransactionType NVARCHAR(50) NOT NULL,                      -- 'TopUp', 'Withdraw', 'TransferIn', 'TransferOut', 'Purchase', 'Fee', ...
  Description NVARCHAR(500),                                  -- Diễn giải
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  IsDeleted BIT NOT NULL DEFAULT 0,

  -- Foreign Keys
  CONSTRAINT FK_WalletTransactions_WalletID FOREIGN KEY (WalletID)
    REFERENCES Wallets(WalletID),

  CONSTRAINT FK_WalletTransactions_PaymentID FOREIGN KEY (PaymentID)
    REFERENCES Payments(PaymentID)
);

GO

-- Table BusinessManagers
CREATE TABLE BusinessManagers (
  ManagerID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
  UserID UNIQUEIDENTIFIER NOT NULL,
  ManagerCode VARCHAR(20) UNIQUE,                               -- BM-2024-0001
  CompanyName NVARCHAR(100) NOT NULL,                           -- Tên doanh nghiệp
  Position NVARCHAR(50),                                        -- Chức vụ trong công ty
  Department NVARCHAR(100),                                     -- Bộ phận (optional)
  CompanyAddress NVARCHAR(255),                                 -- Trụ sở chính doanh nghiệp
  TaxID NVARCHAR(50),                                           -- Mã số thuế
  Website NVARCHAR(255),                                        -- Trang web doanh nghiệp
  ContactEmail NVARCHAR(100),                                   -- Email liên hệ doanh nghiệp
  BusinessLicenseURL NVARCHAR(255),                             -- Link đến ảnh/PDF giấy phép kinh doanh
  IsCompanyVerified BIT DEFAULT 0,                              -- Trạng thái đã được duyệt bởi admin?
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,        -- Ngày tạo
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,        -- Ngày cập nhật
  IsDeleted BIT NOT NULL DEFAULT 0                              -- 0 = chưa xoá, 1 = đã xoá mềm

  -- Foreign Keys
  CONSTRAINT FK_BusinessManagers_UserID 
      FOREIGN KEY (UserID) REFERENCES UserAccounts(UserID)
);

GO

-- Bảng lưu thông tin bổ sung chỉ dành riêng cho người dùng có vai trò là Farmer
CREATE TABLE Farmers (
  FarmerID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),             -- Mã định danh riêng cho vai trò Farmer
  FarmerCode VARCHAR(20) UNIQUE,                                     -- FRM-2024-0001
  UserID UNIQUEIDENTIFIER NOT NULL,                                  -- Liên kết đến tài khoản người dùng chung
  FarmLocation NVARCHAR(255),                                        -- Địa điểm canh tác chính
  FarmSize FLOAT,                                                    -- Diện tích nông trại (hecta)
  CertificationStatus NVARCHAR(100),                                 -- Trạng thái chứng nhận: VietGAP, Organic,...
  CertificationURL NVARCHAR(255),                                    -- Link đến tài liệu chứng nhận
  IsVerified BIT DEFAULT 0,                                          -- Tài khoản nông dân đã xác minh chưa
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,             -- Thời điểm tạo bản ghi
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,             -- Thời điểm cập nhật cuối
  IsDeleted BIT NOT NULL DEFAULT 0                                   -- 0 = chưa xoá, 1 = đã xoá mềm

  -- Foreign Keys
  CONSTRAINT FK_Farmers_UserID 
      FOREIGN KEY (UserID) REFERENCES UserAccounts(UserID)
);

GO

-- Thông tin chuyên gia nông nghiệp
CREATE TABLE AgriculturalExperts (
    ExpertID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),             -- ID riêng của chuyên gia
	ExpertCode VARCHAR(20) UNIQUE,                                     -- EXP-2024-0001
    UserID UNIQUEIDENTIFIER NOT NULL UNIQUE,                           -- Khóa ngoại liên kết bảng Users
    ExpertiseArea NVARCHAR(255),                                       -- Lĩnh vực chuyên môn: Bệnh cây, Tưới tiêu, Canh tác hữu cơ,...
    Qualifications NVARCHAR(255),                                      -- Bằng cấp/chứng chỉ: KS Nông nghiệp, MSc Plant Pathology,...
    YearsOfExperience INT CHECK (YearsOfExperience >= 0),              -- Số năm kinh nghiệm
    AffiliatedOrganization NVARCHAR(255),                              -- Cơ quan công tác (Viện, Trường, Doanh nghiệp...)
    Bio NVARCHAR(MAX),                                                 -- Mô tả chuyên gia, giới thiệu năng lực
    Rating FLOAT CHECK (Rating BETWEEN 0 AND 5),                       -- Điểm đánh giá trung bình nếu có (1–5 sao)
    IsVerified BIT DEFAULT 0,                                          -- Đã xác minh bởi hệ thống chưa
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,             -- Ngày tạo
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,             -- Ngày cập nhật
	IsDeleted BIT NOT NULL DEFAULT 0                                   -- 0 = chưa xoá, 1 = đã xoá mềm

	-- Foreign Keys
    CONSTRAINT FK_AgriculturalExperts_UserID 
	    FOREIGN KEY (UserID) REFERENCES UserAccounts(UserID)
);

GO

-- Table BusinessBuyers
CREATE TABLE BusinessBuyers (
  BuyerID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),    
  BuyerCode VARCHAR(50) UNIQUE,                                    -- BM-2025-0001-BUY-2025-001
  CreatedBy UNIQUEIDENTIFIER NOT NULL,                             -- Người tạo buyer (BusinessManager)
  CompanyName NVARCHAR(100) NOT NULL,                              -- Tên doanh nghiệp mua hàng
  ContactPerson NVARCHAR(100),                                     -- Người đại diện ký hợp đồng
  Position NVARCHAR(50),                                           -- Chức vụ người đại diện
  CompanyAddress NVARCHAR(255),                                    -- Địa chỉ công ty
  TaxID NVARCHAR(50),                                              -- Mã số thuế
  Email NVARCHAR(100),                                             -- Email liên hệ
  Phone NVARCHAR(20),                                              -- Số điện thoại liên hệ
  Website NVARCHAR(255),                                           -- Website doanh nghiệp
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,           -- Ngày tạo
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,           -- Ngày cập nhật
  IsDeleted BIT NOT NULL DEFAULT 0                                 -- 0 = chưa xoá, 1 = đã xoá mềm

  -- Foreign Keys
  CONSTRAINT FK_BusinessBuyers_CreatedBy 
      FOREIGN KEY (CreatedBy) REFERENCES BusinessManagers(ManagerID)
);

GO

-- CoffeeTypes – Các loại cà phê được công nhận
CREATE TABLE CoffeeTypes (
  CoffeeTypeID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),      -- ID loại cà phê
  TypeCode VARCHAR(20) UNIQUE,                                    -- CFT-2025-0001
  TypeName NVARCHAR(100) NOT NULL,                                -- Arabica, Robusta, Culi, Moka, Catimor,...
  BotanicalName NVARCHAR(255),                                    -- Tên khoa học nếu có
  Description NVARCHAR(MAX),                                      -- Mô tả đặc điểm: hương, vị, độ đậm,...
  TypicalRegion NVARCHAR(255),                                    -- Vùng trồng phổ biến: Buôn Ma Thuột, Lâm Đồng,...
  SpecialtyLevel NVARCHAR(50),                                    -- Specialty, Fine Robusta,...
  DefaultYieldPerHectare FLOAT,                                   -- Năng suất trung bình mặc định (Kg/ha)
  Status NVARCHAR(50) DEFAULT 'InActive',                         -- Status active hoặc inActive để admin có thể bật tắt cà phê này trong hệ thống
  CoffeeTypeCategory NVARCHAR(255),                               -- phân loại này là cha hay con, trường này chỉ có 2 option đó
  CoffeeTypeParentID UNIQUEIDENTIFIER NULL,                       -- CoffeeType cha
  CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
  UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
  IsDeleted BIT NOT NULL DEFAULT 0                                -- 0 = chưa xoá, 1 = đã xoá mềm

  -- FOREIGN KEYS
  CONSTRAINT FK_CoffeeTypes_CoffeeTypeParentID FOREIGN KEY (CoffeeTypeParentID) 
      REFERENCES CoffeeTypes(CoffeeTypeID),
);

GO

-- Bảng Crops (Nguồn gốc cho từng loại cà phê)
CREATE TABLE Crops (
    CropID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CropCode VARCHAR(20) UNIQUE,                              -- CROP-2025-0001
    
    -- Địa chỉ DakLak
    Address NVARCHAR(500),                                    -- Địa chỉ cụ thể
    
    -- Thông tin nguồn gốc bổ sung
    FarmName NVARCHAR(200),                                   -- Tên trang trại
    CropArea DECIMAL(10,2),                                   -- Diện tích crop (ha)
    
    -- Trạng thái
    Status NVARCHAR(50) DEFAULT 'Active',                     -- Active, Inactive, Harvested, Processed, Sold

    -- Metadata
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),
    CreatedBy UNIQUEIDENTIFIER,                               -- FK đến Farmers
    UpdatedBy UNIQUEIDENTIFIER,                               -- FK đến Farmers
    IsDeleted BIT DEFAULT 0,                                  -- 0 = chưa xoá, 1 = đã xoá mềm
    
	-- FOREIGN KEYS
    CONSTRAINT FK_Crops_CreatedBy 
        FOREIGN KEY (CreatedBy) REFERENCES Farmers(FarmerID),

    CONSTRAINT FK_Crops_UpdatedBy 
        FOREIGN KEY (UpdatedBy) REFERENCES Farmers(FarmerID)
);

GO

-- Contracts – Hợp đồng B2B giữa doanh nghiệp bán và bên mua
CREATE TABLE Contracts (
  ContractID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),          -- Mã hợp đồng
  ContractCode VARCHAR(20) UNIQUE,                                  -- CTR-2025-0001
  SellerID UNIQUEIDENTIFIER NOT NULL,                               -- Bên bán (doanh nghiệp)
  BuyerID UNIQUEIDENTIFIER NOT NULL,                                -- Bên mua (Trader)
  ContractNumber NVARCHAR(100),                                     -- Số hợp đồng
  ContractTitle NVARCHAR(255),                                      -- Tiêu đề hợp đồng
  ContractFileURL NVARCHAR(255),                                    -- File scan PDF
  DeliveryRounds INT,                                               -- Số đợt giao hàng
  TotalQuantity FLOAT,                                              -- Tổng khối lượng
  TotalValue FLOAT,                                                 -- Tổng trị giá
  StartDate DATE,                                                   -- Ngày bắt đầu
  EndDate DATE,                                                     -- Ngày hết hạn
  SignedAt DATETIME,                                                -- Ngày ký kết
  Status NVARCHAR(50) DEFAULT 'NotStarted',                         -- Trạng thái
  CancelReason NVARCHAR(MAX),                                       -- Lý do hủy
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,            -- Ngày tạo
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,            -- Ngày cập nhật
  IsDeleted BIT NOT NULL DEFAULT 0,                                 -- 0 = chưa xoá, 1 = đã xoá mềm

  -- FOREIGN KEYS
  CONSTRAINT FK_Contracts_SellerID FOREIGN KEY (SellerID) 
      REFERENCES BusinessManagers(ManagerID),

  CONSTRAINT FK_Contracts_BuyerID FOREIGN KEY (BuyerID) 
      REFERENCES BusinessBuyers(BuyerID)
);

GO

-- ContractItems – Chi tiết sản phẩm trong hợp đồng
CREATE TABLE ContractItems (
  ContractItemID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),     -- Mã dòng sản phẩm trong hợp đồng
  ContractItemCode VARCHAR(50) UNIQUE,                             -- CTI-2025-0001
  ContractID UNIQUEIDENTIFIER NOT NULL,                            -- FK đến hợp đồng
  CoffeeTypeID UNIQUEIDENTIFIER NOT NULL,                          -- Gắn với loại cà phê, không phải sản phẩm cụ thể
  Quantity FLOAT,                                                  -- Số lượng đặt mua
  UnitPrice FLOAT,                                                 -- Đơn giá
  DiscountAmount FLOAT DEFAULT 0.0,                                -- Giảm giá dòng này
  Note NVARCHAR(MAX),                                              -- Ghi chú (nếu có)
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,           -- Ngày tạo
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,           -- Ngày cập nhật
  IsDeleted BIT NOT NULL DEFAULT 0                                 -- 0 = chưa xoá, 1 = đã xoá mềm

  -- FOREIGN KEYS
  CONSTRAINT FK_ContractItems_ContractID 
      FOREIGN KEY (ContractID) REFERENCES Contracts(ContractID),

  CONSTRAINT FK_ContractItems_CoffeeTypeID 
      FOREIGN KEY (CoffeeTypeID) REFERENCES CoffeeTypes(CoffeeTypeID)
);

GO

-- ContractDeliveryBatches – đại diện từng đợt giao trong hợp đồng
CREATE TABLE ContractDeliveryBatches (
  DeliveryBatchID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
  DeliveryBatchCode VARCHAR(50) UNIQUE,          -- DELB-2025-0001
  ContractID UNIQUEIDENTIFIER NOT NULL,
  DeliveryRound INT NOT NULL,                    -- Đợt giao hàng số mấy (1, 2, 3...)
  ExpectedDeliveryDate DATE,                     -- Ngày dự kiến giao
  TotalPlannedQuantity FLOAT,                    -- Tổng sản lượng cần giao đợt này
  Status NVARCHAR(50) DEFAULT 'Planned',         -- Planned, InProgress, Fulfilled
  CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
  UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
  IsDeleted BIT NOT NULL DEFAULT 0               -- 0 = chưa xoá, 1 = đã xoá mềm

  -- Foreign Keys
  CONSTRAINT FK_ContractDeliveryBatches_ContractID 
      FOREIGN KEY (ContractID) REFERENCES Contracts(ContractID)
);

GO

-- ContractDeliveryItems – chi tiết mặt hàng của từng đợt giao
CREATE TABLE ContractDeliveryItems (
  DeliveryItemID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
  DeliveryItemCode VARCHAR(50) UNIQUE,          -- DLI-2025-0001
  DeliveryBatchID UNIQUEIDENTIFIER NOT NULL,
  ContractItemID UNIQUEIDENTIFIER NOT NULL,
  PlannedQuantity FLOAT NOT NULL,               -- Số lượng mặt hàng cần giao trong đợt
  FulfilledQuantity FLOAT DEFAULT 0,            -- Đã giao bao nhiêu
  Note NVARCHAR(MAX),
  CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
  UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
  IsDeleted BIT NOT NULL DEFAULT 0              -- 0 = chưa xoá, 1 = đã xoá mềm

  -- Foreign Keys
  CONSTRAINT FK_ContractDeliveryItems_BatchID 
    FOREIGN KEY (DeliveryBatchID) REFERENCES ContractDeliveryBatches(DeliveryBatchID),

  CONSTRAINT FK_ContractDeliveryItems_ContractItemID 
    FOREIGN KEY (ContractItemID) REFERENCES ContractItems(ContractItemID)
);

GO

-- Danh mục phương pháp sơ chế (natural, washed,...)
CREATE TABLE ProcessingMethods (
  MethodID INT PRIMARY KEY IDENTITY(1,1),                      -- ID nội bộ
  MethodCode VARCHAR(50) UNIQUE NOT NULL,                      -- Mã code định danh: 'Natural', 'Washed'...
  Name NVARCHAR(100) NOT NULL,                                 -- Tên hiển thị: 'Sơ chế khô'
  Description NVARCHAR(MAX),                                   -- Mô tả chi tiết
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,       -- Ngày tạo dòng dữ liệu
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,       -- Ngày cập nhật cuối (update thủ công khi chỉnh sửa)
  IsDeleted BIT NOT NULL DEFAULT 0                             -- 0 = chưa xoá, 1 = đã xoá mềm
);

GO

-- ProcurementPlans – Bảng kế hoạch thu mua tổng quan
CREATE TABLE ProcurementPlans (
    PlanID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),                               -- ID kế hoạch thu mua
	PlanCode VARCHAR(20) UNIQUE,                                                       -- PLAN-2025-0001
    Title NVARCHAR(255) NOT NULL,                                                      -- Tên kế hoạch: "Thu mua cà phê Arabica 2025"
    Description NVARCHAR(MAX),                                                         -- Mô tả yêu cầu và thông tin bổ sung
    TotalQuantity FLOAT,                                                               -- Tổng sản lượng cần thu mua (Kg hoặc tấn)
    CreatedBy UNIQUEIDENTIFIER NOT NULL,                                               -- Người tạo kế hoạch (doanh nghiệp)
    StartDate DATE,                                                                    -- Ngày bắt đầu nhận đăng ký
    EndDate DATE,                                                                      -- Ngày kết thúc nhận đăng ký
    Status NVARCHAR(50) DEFAULT 'Draft',                                               -- Tình trạng: Draft, Open, Closed, Cancelled
    ProgressPercentage FLOAT CHECK (ProgressPercentage BETWEEN 0 AND 100) DEFAULT 0.0, -- % hoàn thành
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,                             -- Ngày tạo
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,                             -- Ngày cập nhật
	IsDeleted BIT NOT NULL DEFAULT 0                                                   -- 0 = chưa xoá, 1 = đã xoá mềm

	-- Foreign Keys
    CONSTRAINT FK_ProcurementPlans_CreatedBy 
	    FOREIGN KEY (CreatedBy) REFERENCES BusinessManagers(ManagerID)
);

GO

-- ProcurementPlansDetails – Bảng chi tiết từng loại cây trong kế hoạch
CREATE TABLE ProcurementPlansDetails (
    PlanDetailsID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),                        -- ID chi tiết kế hoạch
	PlanDetailCode VARCHAR(20) UNIQUE,                                                 -- PLD-2025-0001
    PlanID UNIQUEIDENTIFIER NOT NULL,                                                  -- FK đến bảng ProcurementPlans
	CoffeeTypeID UNIQUEIDENTIFIER NOT NULL,                                            -- Liên kết loại cà phê chính xác
	ProcessMethodID INT,													           -- Phương thức sơ chế
    TargetQuantity FLOAT,                                                              -- Sản lượng mong muốn (Kg hoặc tấn)
    TargetRegion NVARCHAR(2000),                                                       -- Khu vực thu mua chính: ví dụ "Cư M’gar"
    MinimumRegistrationQuantity FLOAT,                                                 -- Số lượng tối thiểu để nông dân đăng ký (Kg)
    MinPriceRange FLOAT,                                                               -- Giá tối thiểu có thể thương lượng
    MaxPriceRange FLOAT,                                                               -- Giá tối đa có thể thương lượng
	ExpectedYieldPerHectare FLOAT,                                                     -- Năng suất kỳ vọng (Kg/Ha)
    Note NVARCHAR(MAX),                                                                -- Ghi chú bổ sung
    ProgressPercentage FLOAT CHECK (ProgressPercentage BETWEEN 0 AND 100) DEFAULT 0.0, -- % hoàn thành chi tiết
	ContractItemID UNIQUEIDENTIFIER NULL,                                              -- Gắn tùy chọn với dòng hợp đồng B2B
    Status NVARCHAR(50) DEFAULT 'Active',                                              -- Trạng thái: Active, Closed, Disabled
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,                             -- Ngày tạo
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,                             -- Ngày cập nhật
	IsDeleted BIT NOT NULL DEFAULT 0                                                   -- 0 = chưa xoá, 1 = đã xoá mềm

	-- Foreign Keys
    CONSTRAINT FK_ProcurementPlansDetails_PlanID 
	    FOREIGN KEY (PlanID) REFERENCES ProcurementPlans(PlanID),

	CONSTRAINT FK_ProcurementPlansDetails_CoffeeTypeID 
        FOREIGN KEY (CoffeeTypeID) REFERENCES CoffeeTypes(CoffeeTypeID),

	CONSTRAINT FK_ProcurementPlansDetails_ContractItemID
        FOREIGN KEY (ContractItemID) REFERENCES ContractItems(ContractItemID),

	CONSTRAINT FK_ProcurementPlansDetails_ProcessMethodID
        FOREIGN KEY (ProcessMethodID) REFERENCES ProcessingMethods(MethodID)
);

GO

-- CultivationRegistrations – Đơn đăng ký trồng cây của Farmer
CREATE TABLE CultivationRegistrations (
    RegistrationID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),   -- ID đơn đăng ký
	RegistrationCode VARCHAR(20) UNIQUE,                           -- REG-2025-0001
    PlanID UNIQUEIDENTIFIER NOT NULL,                              -- Kế hoạch thu mua
    FarmerID UNIQUEIDENTIFIER NOT NULL,                            -- Nông dân nộp đơn
    RegisteredArea FLOAT,                                          -- Tổng Diện tích đăng ký (Hecta)
    RegisteredAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,      -- Thời điểm nộp đơn
    TotalWantedPrice FLOAT,                                        -- Tổng mức giá mong muốn
    Status NVARCHAR(50) DEFAULT 'Pending',                         -- Trạng thái: Pending, Approved,...
    Note NVARCHAR(MAX),                                            -- Ghi chú từ farmer
    SystemNote NVARCHAR(MAX),                                      -- Ghi chú từ hệ thống
    IsDeleted BIT NOT NULL DEFAULT 0,                              -- 0 = chưa xoá, 1 = đã xoá mềm
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

	-- Foreign Keys
    CONSTRAINT FK_CultivationRegistrations_ProcurementPlans 
        FOREIGN KEY (PlanID) REFERENCES ProcurementPlans(PlanID),

    CONSTRAINT FK_CultivationRegistrations_Farmers 
        FOREIGN KEY (FarmerID) REFERENCES Farmers(FarmerID)
);

GO

-- CultivationRegistrationsDetail – Chi tiết từng dòng đăng ký theo loại cây trồng cụ thể
CREATE TABLE CultivationRegistrationsDetail (
    CultivationRegistrationDetailID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), -- ID chi tiết đơn đăng ký
    RegistrationID UNIQUEIDENTIFIER NOT NULL,                                     -- FK đến đơn chính
    PlanDetailID UNIQUEIDENTIFIER NOT NULL,                                       -- FK đến loại cây cụ thể
    CropID UNIQUEIDENTIFIER NULL,
    RegisteredArea FLOAT,
    EstimatedYield FLOAT,                                                         -- Sản lượng ước tính (Kg)
    ExpectedHarvestStart DATE,                                                    -- Ngày bắt đầu thu hoạch
    ExpectedHarvestEnd DATE,                                                      -- Ngày kết thúc thu hoạch
	WantedPrice FLOAT,															  -- Mức giá mong muốn
    Status NVARCHAR(50) DEFAULT 'Pending',                                        -- Pending, Approved,...
    Note NVARCHAR(MAX),                                                           -- Ghi chú từ farmer
    SystemNote NVARCHAR(MAX),                                                     -- Ghi chú hệ thống
    ApprovedBy UNIQUEIDENTIFIER,                                                  -- Người duyệt (nullable)
    ApprovedAt DATETIME,                                                          -- Thời gian duyệt
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	IsDeleted BIT NOT NULL DEFAULT 0                                              -- 0 = chưa xoá, 1 = đã xoá mềm

	-- Foreign Keys
    CONSTRAINT FK_CultivationRegistrationsDetail_CultivationRegistrations 
        FOREIGN KEY (RegistrationID) REFERENCES CultivationRegistrations(RegistrationID),

    CONSTRAINT FK_CultivationRegistrationsDetail_ProcurementPlansDetails 
        FOREIGN KEY (PlanDetailID) REFERENCES ProcurementPlansDetails(PlanDetailsID),

    CONSTRAINT FK_CultivationRegistrationsDetail_ApprovedBy 
        FOREIGN KEY (ApprovedBy) REFERENCES BusinessManagers(ManagerID),

    CONSTRAINT FK_CultivationRegistrationsDetail_CropID 
    FOREIGN KEY (CropID) REFERENCES Crops(CropID)
);

GO

-- FarmingCommitments – Cam kết chính thức giữa Farmer và hệ thống
CREATE TABLE FarmingCommitments (
    CommitmentID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),                                  -- ID cam kết
	CommitmentCode VARCHAR(20) UNIQUE,                                                          -- FC-2025-0001
	CommitmentName NVARCHAR(MAX),									                            -- Tên của bảng cam kết
    PlanID UNIQUEIDENTIFIER NOT NULL,                                                           -- FK đến kế hoạch chính
    RegistrationID UNIQUEIDENTIFIER NOT NULL UNIQUE,                                            -- FK đến đơn đăng ký chính
    FarmerID UNIQUEIDENTIFIER NOT NULL,                                                         -- Nông dân cam kết
    TotalPrice FLOAT DEFAULT 0,                                                                 -- Tổng giá tiền
    TotalAdvancePayment FLOAT DEFAULT 0,                                                        -- Tổng số tiền ứng trước (nếu có)
    TotalTaxPrice FLOAT DEFAULT 0,                                                              -- Tổng giá trị thuế (nếu có)
    CommitmentDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,                                 -- Ngày xác lập cam kết
    ApprovedBy UNIQUEIDENTIFIER,                                                                -- Người duyệt
    ApprovedAt DATETIME,                                                                        -- Ngày duyệt
    Status NVARCHAR(50) DEFAULT 'Pending',                                                      -- Trạng thái cam kết
    ProgressPercentage FLOAT CHECK (ProgressPercentage BETWEEN 0 AND 100) DEFAULT 0.0,          -- % hoàn thành
    RejectionReason NVARCHAR(MAX),                                                              -- Lý do từ chối (nếu có)
    Note NVARCHAR(MAX),                                                                         -- Ghi chú thêm
    TotalRatingByBusiness FLOAT CHECK (TotalRatingByBusiness BETWEEN 0 AND 5) DEFAULT NULL,     -- Tổng điểm đánh giá (nếu có)
    TotalRatingByFarmer FLOAT CHECK (TotalRatingByFarmer BETWEEN 0 AND 5) DEFAULT NULL,         -- Tổng điểm đánh giá (nếu có)
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	IsDeleted BIT NOT NULL DEFAULT 0                                                            -- 0 = chưa xoá, 1 = đã xoá mềm

    -- Foreign Keys
    CONSTRAINT FK_FarmingCommitments_FarmerID 
	    FOREIGN KEY (FarmerID) REFERENCES Farmers(FarmerID),

    CONSTRAINT FK_FarmingCommitments_PlanID 
	    FOREIGN KEY (PlanID) REFERENCES ProcurementPlans(PlanID),

    CONSTRAINT FK_FarmingCommitments_RegistrationID 
	    FOREIGN KEY (RegistrationID) REFERENCES CultivationRegistrations(RegistrationID),

    CONSTRAINT FK_FarmingCommitments_ApprovedBy 
	    FOREIGN KEY (ApprovedBy) REFERENCES BusinessManagers(ManagerID),
);

GO

-- FarmingCommitmentsDetails – Chi tiết từng dòng cam kết
CREATE TABLE FarmingCommitmentsDetails (
    CommitmentDetailID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),                   -- ID chi tiết cam kết
    CommitmentDetailCode VARCHAR(20) UNIQUE,                                           -- FCD-2025-0001
    CommitmentID UNIQUEIDENTIFIER NOT NULL,                                            -- FK đến cam kết chính
    RegistrationDetailID UNIQUEIDENTIFIER NOT NULL,                                    -- FK đến chi tiết đơn đã duyệt
    PlanDetailID UNIQUEIDENTIFIER NOT NULL,                                            -- FK đến loại cây cụ thể
    ConfirmedPrice FLOAT,                                                              -- Giá xác nhận mua
    AdvancePayment FLOAT DEFAULT 0,                                                    -- Số tiền ứng trước (nếu có)
    CommittedQuantity FLOAT,                                                           -- Khối lượng cam kết
    EstimatedDeliveryStart DATE,                                                       -- Ngày giao hàng dự kiến bắt đầu
    EstimatedDeliveryEnd DATE,                                                         -- Ngày giao hàng dự kiến kết thúc
    Note NVARCHAR(MAX),                                                                -- Ghi chú thêm
    Status NVARCHAR(50) DEFAULT 'Pending',                                             -- Trạng thái cam kết
    DeliveriedQuantity FLOAT DEFAULT 0,                                                -- Số lượng đã giao
    ProgressPercentage FLOAT CHECK (ProgressPercentage BETWEEN 0 AND 100) DEFAULT 0.0, -- % hoàn thành
    TaxPrice FLOAT DEFAULT 0,                                                          -- Giá trị thuế (nếu có)
    RejectionReason NVARCHAR(MAX),                                                     -- Lý do từ chối (nếu có)
    RejectionBy UNIQUEIDENTIFIER NULL,                                                 -- Người từ chối (nếu có)
    RejectionAt DATETIME,                                                              -- Thời gian từ chối (nếu có)
    BreachedReason NVARCHAR(MAX),                                                      -- Nguyên nhân cam kết bị vi phạm (nếu có)
    BreachedBy UNIQUEIDENTIFIER NULL,                                                  -- Người khiến cam kết bị vi phạm (nếu có)
    BreachedAt DATETIME,                                                               -- Thời gian cam kết bị vi phạm (nếu có)
    RatingByBusiness FLOAT CHECK (RatingByBusiness BETWEEN 0 AND 5) DEFAULT NULL,      -- Đánh giá của doanh nghiệp về cam kết sau khi đã hoàn thành hợp đồng
    RatingCommentByBusiness NVARCHAR(MAX) NULL,                                        -- Nhận xét đánh giá (nếu có)
    RatingByFarmer FLOAT CHECK (RatingByFarmer BETWEEN 0 AND 5) DEFAULT NULL,          -- Đánh giá của nông dân về cam kết sau khi đã hoàn thành hợp đồng
    RatingCommentByFarmer NVARCHAR(MAX) NULL,                                          -- Nhận xét đánh giá (nếu có)
    ContractDeliveryItemID UNIQUEIDENTIFIER NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	IsDeleted BIT NOT NULL DEFAULT 0

    -- Foreign Keys
    CONSTRAINT FK_FarmingCommitmentsDetails_RegistrationDetailID       -- FK tới bảng CultivationRegistrationsDetail
	   FOREIGN KEY (RegistrationDetailID) REFERENCES CultivationRegistrationsDetail(CultivationRegistrationDetailID),

    CONSTRAINT FK_FarmingCommitmentsDetails_PlanDetailID               -- FK tới bảng ProcurementPlansDetails
	    FOREIGN KEY (PlanDetailID) REFERENCES ProcurementPlansDetails(PlanDetailsID),

    CONSTRAINT FK_FarmingCommitmentsDetails_ContractDeliveryItem       -- FK tới bảng ContractDeliveryItems
        FOREIGN KEY (ContractDeliveryItemID) REFERENCES ContractDeliveryItems(DeliveryItemID),

    CONSTRAINT FK_FarmingCommitmentsDetails_CommitmentID               -- FK tới bảng FarmingCommitments
        FOREIGN KEY (CommitmentID) REFERENCES FarmingCommitments(CommitmentID),

    CONSTRAINT FK_FarmingCommitmentsDetails_RejectionBy                -- FK tới bảng UserAccounts
        FOREIGN KEY (RejectionBy) REFERENCES UserAccounts(UserID),

    CONSTRAINT FK_FarmingCommitmentsDetails_BreachedBy                 -- FK tới bảng UserAccounts
        FOREIGN KEY (BreachedBy) REFERENCES UserAccounts(UserID)
);

GO

-- CropSeasons – Quản lý mùa vụ trồng
CREATE TABLE CropSeasons (
    CropSeasonID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),          -- ID của một mùa vụ
	CropSeasonCode VARCHAR(20) UNIQUE,                                  -- SEASON-2025-0001
    --RegistrationID UNIQUEIDENTIFIER NOT NULL,                         -- Liên kết đơn đăng ký trồng
    FarmerID UNIQUEIDENTIFIER NOT NULL,                                 -- Nông dân tạo mùa vụ
    CommitmentID UNIQUEIDENTIFIER NOT NULL,                             -- Cam kết từ doanh nghiệp
    SeasonName NVARCHAR(100),                                           -- Tên mùa vụ (vd: Mùa vụ 2025)
    Area FLOAT,                                                         -- Diện tích canh tác (ha)
    StartDate DATE,                                                     -- Ngày bắt đầu
    EndDate DATE,                                                       -- Ngày kết thúc
    Note NVARCHAR(MAX),                                                 -- Ghi chú mùa vụ
    Status NVARCHAR(50) DEFAULT 'Active',                               -- Trạng thái: Active, Paused,...
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,              -- Thời điểm tạo
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,              -- Thời điểm cập nhật
	IsDeleted BIT NOT NULL DEFAULT 0                                    -- 0 = chưa xoá, 1 = đã xoá mềm

    -- Foreign Keys
    CONSTRAINT FK_CropSeasons_FarmerID 
	    FOREIGN KEY (FarmerID) REFERENCES Farmers(FarmerID),

    CONSTRAINT FK_CropSeasons_CommitmentID 
	    FOREIGN KEY (CommitmentID) REFERENCES FarmingCommitments(CommitmentID)
);

GO

-- CropSeasonDetails – Chi tiết từng loại cà phê trong mùa vụ
CREATE TABLE CropSeasonDetails (
    DetailID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),             -- ID chi tiết mùa vụ
    CropSeasonID UNIQUEIDENTIFIER NOT NULL,                            -- FK đến mùa vụ
    CommitmentDetailID UNIQUEIDENTIFIER NOT NULL,                      -- FK đến chi tiết cam kết, lấy coffeeTypeID từ đây
    CropID UNIQUEIDENTIFIER NULL,                                           -- FK đến vùng trồng
    ExpectedHarvestStart DATE,                                         -- Ngày bắt đầu thu hoạch dự kiến
    ExpectedHarvestEnd DATE,                                           -- Ngày kết thúc thu hoạch dự kiến
    EstimatedYield FLOAT,                                              -- Sản lượng dự kiến
    ActualYield FLOAT,                                                 -- Sản lượng thực tế
    AreaAllocated FLOAT,                                               -- Diện tích cho loại này
    PlannedQuality NVARCHAR(50),                                       -- Chất lượng dự kiến
    QualityGrade NVARCHAR(50),                                         -- Chất lượng thực tế
    Status NVARCHAR(50) DEFAULT 'Planned',                             -- Planned, InProgress, Completed
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,             -- Ngày tạo
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	IsDeleted BIT NOT NULL DEFAULT 0                                   -- 0 = chưa xoá, 1 = đã xoá mềm

	-- Foreign Keys
    CONSTRAINT FK_CropSeasonDetails_CropSeasonID 
	    FOREIGN KEY (CropSeasonID) REFERENCES CropSeasons(CropSeasonID),

    CONSTRAINT FK_CropSeasonDetails_CommitmentDetailID
        FOREIGN KEY (CommitmentDetailID) REFERENCES FarmingCommitmentsDetails (CommitmentDetailID),

    CONSTRAINT FK_CropSeasonDetails_CropID 
        FOREIGN KEY (CropID) REFERENCES Crops(CropID)
);

GO

-- CropStages – Danh mục các giai đoạn mùa vụ)
CREATE TABLE CropStages (
    StageID INT PRIMARY KEY IDENTITY(1,1), 
    StageCode VARCHAR(50) UNIQUE NOT NULL,    -- Mã giai đoạn: Planting, Harvesting,...
    StageName NVARCHAR(100),                  -- Tên hiển thị: Gieo trồng, Ra hoa,...
    Description NVARCHAR(MAX),                -- Mô tả chi tiết giai đoạn
    OrderIndex INT,                           -- Thứ tự giai đoạn
	CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	IsDeleted BIT NOT NULL DEFAULT 0          -- 0 = chưa xoá, 1 = đã xoá mềm
);

GO

-- CropProgresses – Tiến độ mùa vụ
CREATE TABLE CropProgresses (
    ProgressID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),                   -- ID bản ghi tiến độ
    CropSeasonDetailID UNIQUEIDENTIFIER NOT NULL,                              -- Gắn với chi tiết mùa vụ
    UpdatedBy UNIQUEIDENTIFIER NOT NULL,                                       -- Nông dân cập nhật
    StageID INT NOT NULL,                                                      -- Giai đoạn tương ứng
    StageDescription NVARCHAR(MAX),                                            -- Mô tả chi tiết tiến độ
    ProgressDate DATE,                                                         -- Ngày ghi nhận tiến độ
    PhotoUrl VARCHAR(255),                                                     -- Ảnh minh họa
    VideoUrl VARCHAR(255),                                                     -- Video minh họa
    Note NVARCHAR(MAX),                                                        -- Ghi chú tiến trình
	StepIndex INT,                                                             -- Bước thứ mấy trong mùa vụ
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,                     -- Ngày tạo
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,                     -- Ngày cập nhật
	IsDeleted BIT NOT NULL DEFAULT 0                                           -- 0 = chưa xoá, 1 = đã xoá mềm

	-- Foreign Keys
    CONSTRAINT FK_CropProgresses_CropSeasonDetailID 
        FOREIGN KEY (CropSeasonDetailID) REFERENCES CropSeasonDetails(DetailID),

    CONSTRAINT FK_CropProgresses_UpdatedBy 
        FOREIGN KEY (UpdatedBy) REFERENCES Farmers(FarmerID),

    CONSTRAINT FK_CropProgresses_StageID 
        FOREIGN KEY (StageID) REFERENCES CropStages(StageID)
);

GO

-- ProcessingStages – Danh mục chuẩn các bước trong sơ chế
CREATE TABLE ProcessingStages (
  StageID INT PRIMARY KEY IDENTITY(1,1),                         -- ID bước xử lý
  MethodID INT NOT NULL,                                         -- Phương pháp sơ chế áp dụng (FK)
  StageCode VARCHAR(50) NOT NULL,                                -- Mã bước: 'Drying', 'Fermentation'...
  StageName NVARCHAR(100) NOT NULL,                              -- Tên hiển thị: 'Phơi', 'Lên men'
  Description NVARCHAR(MAX),                                     -- Mô tả chi tiết
  OrderIndex INT NOT NULL,                                       -- Thứ tự thực hiện
  IsRequired BIT DEFAULT 1,                                      -- Bước này có bắt buộc không?
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,         -- Ngày tạo dòng dữ liệu
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,         -- Ngày cập nhật cuối
  IsDeleted BIT NOT NULL DEFAULT 0                               -- 0 = chưa xoá, 1 = đã xoá mềm

  -- Foreign Keys
  CONSTRAINT FK_ProcessingStages_Method 
      FOREIGN KEY (MethodID) REFERENCES ProcessingMethods(MethodID)
);

GO

-- Hồ sơ lô sơ chế (Batch sơ chế của từng Farmer)
CREATE TABLE ProcessingBatches (
  BatchID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),        -- ID định danh lô sơ chế
  SystemBatchCode  VARCHAR(20) UNIQUE,                         -- BATCH-2025-0001
  CoffeeTypeID UNIQUEIDENTIFIER NOT NULL,                      -- Loại cà phê được sơ chế
  CropSeasonID UNIQUEIDENTIFIER NOT NULL,                      -- FK đến mùa vụ
  FarmerID UNIQUEIDENTIFIER NOT NULL,                          -- FK đến nông dân thực hiện sơ chế
  BatchCode NVARCHAR(50) NOT NULL,                             -- Mã lô do người dùng tự đặt
  MethodID INT NOT NULL,                                       -- Mã phương pháp sơ chế (FK)
  InputQuantity FLOAT NOT NULL,                                -- Số lượng đầu vào (Kg quả cà phê)
  InputUnit NVARCHAR(20) DEFAULT 'Kg',                         -- Đơn vị (thường là Kg)
  CreatedAt DATETIME,                                          -- Ngày tạo hồ sơ
  UpdatedAt DATETIME,                                          -- Ngày câp nhật hồ sơ
  Status NVARCHAR(50),                                         -- Trạng thái: Pending, processing, completed
  IsDeleted BIT NOT NULL DEFAULT 0                             -- 0 = chưa xoá, 1 = đã xoá mềm

  -- FOREIGN KEYS
  CONSTRAINT FK_ProcessingBatches_CropSeason 
      FOREIGN KEY (CropSeasonID) REFERENCES CropSeasons(CropSeasonID),

  CONSTRAINT FK_ProcessingBatches_Farmer 
      FOREIGN KEY (FarmerID) REFERENCES Farmers(FarmerID),

  CONSTRAINT FK_ProcessingBatches_Method 
      FOREIGN KEY (MethodID) REFERENCES ProcessingMethods(MethodID),

  CONSTRAINT FK_ProcessingBatches_CoffeeTypeID
      FOREIGN KEY (CoffeeTypeID) REFERENCES CoffeeTypes(CoffeeTypeID)
);

GO

-- Ghi nhận tiến trình từng bước trong sơ chế (Drying, Dehulling...)
CREATE TABLE ProcessingBatchProgresses (
  ProgressID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),     -- ID từng bước sơ chế
  BatchID UNIQUEIDENTIFIER NOT NULL,                           -- FK tới lô sơ chế
  StepIndex INT NOT NULL,                                      -- Thứ tự tiến trình trong batch
  StageID INT NOT NULL,                                        -- FK đến bảng chuẩn `ProcessingStages`
  StageDescription NVARCHAR(MAX),                              -- Mô tả chi tiết quá trình
  ProgressDate DATE,                                           -- Ngày thực hiện bước
  OutputQuantity FLOAT,                                        -- Sản lượng thu được (nếu có)
  OutputUnit NVARCHAR(20) DEFAULT 'Kg',                        -- Đơn vị sản lượng
  UpdatedBy UNIQUEIDENTIFIER NOT NULL,                         -- Người ghi nhận bước này (Farmer)
  PhotoURL VARCHAR(255),                                       -- Link ảnh (nếu có)
  VideoURL VARCHAR(255),                                       -- Link video (tùy chọn)
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,       -- Thời điểm tạo
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,       -- Thời điểm cập nhật lần cuối
  IsDeleted BIT NOT NULL DEFAULT 0                             -- 0 = chưa xoá, 1 = đã xoá mềm

  -- FOREIGN KEYS
  CONSTRAINT FK_BatchProgresses_Batch 
      FOREIGN KEY (BatchID) REFERENCES ProcessingBatches(BatchID),

  CONSTRAINT FK_BatchProgresses_ProcessingStages 
      FOREIGN KEY (StageID) REFERENCES ProcessingStages(StageID),

  CONSTRAINT FK_BatchProgresses_Farmer 
      FOREIGN KEY (UpdatedBy) REFERENCES Farmers(FarmerID)
);

GO

-- Ghi nhận thông số kỹ thuật từng bước (nếu có nhập tay)
CREATE TABLE ProcessingParameters (
  ParameterID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),    -- ID thông số
  ProgressID UNIQUEIDENTIFIER NOT NULL,                        -- FK tới bước sơ chế cụ thể
  ParameterName NVARCHAR(100),                                 -- Tên thông số: Humidity, Temperature...
  ParameterValue NVARCHAR(100) NULL,                           -- Giá trị đo được
  Unit NVARCHAR(20) NULL,                                      -- Đơn vị: %, °C,...
  RecordedAt DATETIME,                                         -- Ngày ghi nhận
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,       -- Ngày tạo yêu cầu
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,       -- Ngày cập nhật cuối
  IsDeleted BIT NOT NULL DEFAULT 0                             -- 0 = chưa xoá, 1 = đã xoá mềm

  -- FOREIGN KEYS
  CONSTRAINT FK_ProcessingParameters_Progress 
      FOREIGN KEY (ProgressID) REFERENCES ProcessingBatchProgresses(ProgressID)
);

GO

-- GeneralFarmerReports – Bảng báo cáo sự cố chung từ Farmer
CREATE TABLE GeneralFarmerReports (
    ReportID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),                 -- ID báo cáo chung
	ReportCode VARCHAR(20) UNIQUE,                                         -- REP-2025-0001
    ReportType VARCHAR(50) NOT NULL CHECK (ReportType IN ('Crop', 'Processing')),  -- Phân loại
    CropProgressID UNIQUEIDENTIFIER,                                       -- FK nếu là mùa vụ
    ProcessingProgressID UNIQUEIDENTIFIER,                                 -- FK nếu là sơ chế
    ReportedBy UNIQUEIDENTIFIER NOT NULL,                                  -- Người gửi báo cáo
    Title NVARCHAR(255),                                                   -- Tiêu đề/Loại sự cố
    Description NVARCHAR(MAX),                                             -- Mô tả chi tiết
    SeverityLevel INT CHECK (SeverityLevel BETWEEN 0 AND 2),               -- Mức độ (tuỳ chọn)
    ImageUrl VARCHAR(255),
    VideoUrl VARCHAR(255),
    IsResolved BIT DEFAULT 0,
    ReportedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ResolvedAt DATETIME,
	IsDeleted BIT NOT NULL DEFAULT 0                                       -- 0 = chưa xoá, 1 = đã xoá mềm

    -- Foreign Keys (chỉ dùng được từng loại tùy theo ReportType)
    CONSTRAINT FK_GeneralReports_CropProgress 
        FOREIGN KEY (CropProgressID) REFERENCES CropProgresses(ProgressID),

    CONSTRAINT FK_GeneralReports_ProcessingProgress 
        FOREIGN KEY (ProcessingProgressID) REFERENCES ProcessingBatchProgresses(ProgressID),

    CONSTRAINT FK_GeneralReports_ReportedBy 
        FOREIGN KEY (ReportedBy) REFERENCES UserAccounts(UserID)
);

GO

-- ExpertAdvice – Bảng phản hồi từ chuyên gia đối với báo cáo
CREATE TABLE ExpertAdvice (
    AdviceID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),             
    ReportID UNIQUEIDENTIFIER NOT NULL,                       -- FK tới báo cáo chung
    ExpertID UNIQUEIDENTIFIER NOT NULL,                       -- Ai phản hồi
    ResponseType VARCHAR(50),                                 -- Preventive, Corrective, Observation
    AdviceSource VARCHAR(50) DEFAULT 'human',                 -- Human hoặc AI
    AdviceText NVARCHAR(MAX),                                 -- Nội dung tư vấn
    AttachedFileUrl VARCHAR(255),
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	IsDeleted BIT NOT NULL DEFAULT 0                          -- 0 = chưa xoá, 1 = đã xoá mềm

    -- Foreign Keys
    CONSTRAINT FK_GeneralExpertAdvice_Report 
        FOREIGN KEY (ReportID) REFERENCES GeneralFarmerReports(ReportID),

    CONSTRAINT FK_GeneralExpertAdvice_Expert 
        FOREIGN KEY (ExpertID) REFERENCES AgriculturalExperts(ExpertID)
);

GO

-- ProcessingBatchEvaluations – Đánh giá chất lượng batch
CREATE TABLE ProcessingBatchEvaluations (
  EvaluationID UNIQUEIDENTIFIER PRIMARY KEY,                    -- ID đánh giá
  EvaluationCode VARCHAR(20) UNIQUE,                            -- EVAL-2025-0001
  BatchID UNIQUEIDENTIFIER NOT NULL,                            -- FK tới batch
  EvaluatedBy UNIQUEIDENTIFIER,                                 -- Ai đánh giá (Expert/Manager)
  EvaluationResult NVARCHAR(50),                                -- Kết quả: Pass, Fail, Rework
  Comments NVARCHAR(MAX),                                       -- Nhận xét chi tiết
  EvaluatedAt DATETIME,                                         -- Ngày đánh giá
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,        -- Ngày tạo dòng dữ liệu
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,        -- Ngày cập nhật cuối
  IsDeleted BIT NOT NULL DEFAULT 0                              -- 0 = chưa xoá, 1 = đã xoá mềm

  -- FOREIGN KEYS
  CONSTRAINT FK_Evaluations_Batch 
      FOREIGN KEY (BatchID) REFERENCES ProcessingBatches(BatchID)
);

GO

-- ProcessingBatchWastes – Ghi nhận phế phẩm từng bước sơ chế
CREATE TABLE ProcessingBatchWastes (
  WasteID UNIQUEIDENTIFIER PRIMARY KEY,                         -- ID dòng phế phẩm
  WasteCode VARCHAR(20) UNIQUE,                                 -- WASTE-2025-0001
  ProgressID UNIQUEIDENTIFIER NOT NULL,                         -- FK tới bước gây ra phế phẩm
  WasteType NVARCHAR(100),                                      -- Loại phế phẩm: vỏ quả, hạt lép...
  Quantity FLOAT,                                               -- Khối lượng
  Unit NVARCHAR(20),                                            -- Đơn vị: Kg, G, pcs...
  Note NVARCHAR(MAX),                                           -- Ghi chú nếu có
  RecordedAt DATETIME,                                          -- Thời điểm ghi nhận
  RecordedBy UNIQUEIDENTIFIER,                                  -- Ai ghi nhận (Farmer/Manager)
  IsDisposed BIT DEFAULT 0,                                     -- Cờ đánh dấu đã được xử lý
  DisposedAt DATETIME,                                          -- Ngày xử lý gần nhất (nếu có)
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,        -- Ngày tạo dòng dữ liệu
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,        -- Ngày cập nhật cuối
  IsDeleted BIT NOT NULL DEFAULT 0                              -- 0 = chưa xoá, 1 = đã xoá mềm

  -- FOREIGN KEYS
  CONSTRAINT FK_Wastes_Progress 
      FOREIGN KEY (ProgressID) REFERENCES ProcessingBatchProgresses(ProgressID)
);

GO

-- ProcessingWasteDisposals – Ghi nhận cách xử lý phế phẩm
CREATE TABLE ProcessingWasteDisposals (
  DisposalID UNIQUEIDENTIFIER PRIMARY KEY,                -- ID xử lý phế phẩm
  DisposalCode VARCHAR(20) UNIQUE,                        -- DISP-2025-0001
  WasteID UNIQUEIDENTIFIER NOT NULL,                      -- FK tới dòng phế phẩm
  DisposalMethod NVARCHAR(100) NOT NULL,                  -- Phương pháp xử lý: Compost, Sell, Discard
  HandledBy UNIQUEIDENTIFIER,                             -- Ai thực hiện xử lý
  HandledAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,  -- Thời điểm xử lý
  Notes NVARCHAR(MAX),                                    -- Ghi chú thêm nếu có
  IsSold BIT DEFAULT 0,                                   -- Đã bán lại không?
  Revenue MONEY,                                          -- Nếu có bán, ghi nhận doanh thu
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,  -- Ngày tạo yêu cầu
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,  -- Ngày cập nhật cuối
  IsDeleted BIT NOT NULL DEFAULT 0                        -- 0 = chưa xoá, 1 = đã xoá mềm

  -- FOREIGN KEY
  CONSTRAINT FK_Disposals_Waste 
      FOREIGN KEY (WasteID) REFERENCES ProcessingBatchWastes(WasteID)
);

GO

-- Warehouses – Danh sách kho thuộc doanh nghiệp
CREATE TABLE Warehouses (
    WarehouseID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),       -- ID kho
	WarehouseCode VARCHAR(20) UNIQUE,                               -- WH-2025-DL001
    ManagerID UNIQUEIDENTIFIER NOT NULL,                            -- Người quản lý chính (BusinessManager)
    Name NVARCHAR(100) NOT NULL,                                    -- Tên kho (VD: "Kho Cư M’gar")
    Location NVARCHAR(255),                                         -- Địa chỉ cụ thể
    Capacity FLOAT,                                                 -- Dung lượng tối đa (Kg, tấn...)
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,          -- Ngày tạo
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,          -- Ngày cập nhật
	IsDeleted BIT NOT NULL DEFAULT 0                                -- 0 = chưa xoá, 1 = đã xoá mềm

    -- FOREIGN KEY
    CONSTRAINT FK_Warehouses_Manager 
        FOREIGN KEY (ManagerID) REFERENCES BusinessManagers(ManagerID)
);

GO

-- BusinessStaffs – Thông tin nhân viên doanh nghiệp
CREATE TABLE BusinessStaffs (
  StaffID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),          -- Mã định danh riêng của nhân viên
  StaffCode VARCHAR(20) UNIQUE,                                  -- STAFF-2025-0001
  UserID UNIQUEIDENTIFIER NOT NULL UNIQUE,                       -- FK đến bảng Users (1:1)
  SupervisorID UNIQUEIDENTIFIER,                                 -- Người quản lý trực tiếp (nếu có) – có thể là Manager
  Position NVARCHAR(100),                                        -- Chức danh: Thủ kho, Kế toán kho,...
  Department NVARCHAR(100),                                      -- Bộ phận công tác: "Kho Đắk Lắk"
  AssignedWarehouseID UNIQUEIDENTIFIER,                          -- Gắn với kho được phân công (nếu có)
  IsActive BIT DEFAULT 1,                                        -- Nhân viên còn đang làm việc?
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,         -- Ngày tạo bản ghi
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,         -- Ngày cập nhật gần nhất
  IsDeleted BIT NOT NULL DEFAULT 0                               -- 0 = chưa xoá, 1 = đã xoá mềm

  -- FOREIGN KEYS
  CONSTRAINT FK_BusinessStaffs_UserID 
      FOREIGN KEY (UserID) REFERENCES UserAccounts(UserID),

  CONSTRAINT FK_BusinessStaffs_Supervisor 
      FOREIGN KEY (SupervisorID) REFERENCES BusinessManagers(ManagerID),

  CONSTRAINT FK_BusinessStaffs_Warehouse 
      FOREIGN KEY (AssignedWarehouseID) REFERENCES Warehouses(WarehouseID)
);

GO

-- Inventories – Ghi nhận tồn kho theo từng batch
CREATE TABLE Inventories (
    InventoryID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),       -- Mã dòng tồn kho
	InventoryCode VARCHAR(20) UNIQUE,                               -- INV-2025-0001
    WarehouseID UNIQUEIDENTIFIER NOT NULL,                          -- Gắn với kho cụ thể
    BatchID UNIQUEIDENTIFIER,                                       -- Gắn với mẻ sơ chế (Batch)
	DetailID UNIQUEIDENTIFIER,
    Quantity FLOAT NOT NULL,                                        -- Số lượng hiện tại trong kho
    Unit NVARCHAR(20) DEFAULT 'Kg',                                 -- Đơn vị tính (Kg, Tấn...)
	-- Cột trạng thái computed: 1 = Xanh, 0 = Thô (tự tính từ BatchID)
	IsGreen AS (
      CASE WHEN BatchID IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END
    ) PERSISTED,
	CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,          -- Ngày tạo
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,          -- Thời điểm cập nhật
	IsDeleted BIT NOT NULL DEFAULT 0                                -- 0 = chưa xoá, 1 = đã xoá mềm

    -- FOREIGN KEYS
    CONSTRAINT FK_Inventories_Warehouse 
        FOREIGN KEY (WarehouseID) REFERENCES Warehouses(WarehouseID),

    CONSTRAINT FK_Inventories_Batch 
        FOREIGN KEY (BatchID) REFERENCES ProcessingBatches(BatchID),

	CONSTRAINT FK_Inventories_CropSeasonDetails
        FOREIGN KEY (DetailID) REFERENCES CropSeasonDetails(DetailID)
);

GO

-- InventoryLogs – Lịch sử thay đổi tồn kho
CREATE TABLE InventoryLogs (
    LogID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),             -- Mã nhật ký
    InventoryID UNIQUEIDENTIFIER NOT NULL,                          -- Gắn với dòng tồn kho nào
    ActionType NVARCHAR(50) NOT NULL,                               -- Loại hành động: Increase, Decrease, Correction
    QuantityChanged FLOAT NOT NULL,                                 -- Lượng thay đổi (+/-)
    UpdatedBy UNIQUEIDENTIFIER,                                     -- Ai thực hiện thay đổi
    TriggeredBySystem BIT DEFAULT 0,                                -- Có phải hệ thống tự động ghi không
    Note NVARCHAR(MAX),                                             -- Ghi chú chi tiết
    LoggedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,           -- Thời điểm ghi log
	IsDeleted BIT NOT NULL DEFAULT 0                                -- 0 = chưa xoá, 1 = đã xoá mềm

    -- FOREIGN KEY
    CONSTRAINT FK_InventoryLogs_Inventory 
        FOREIGN KEY (InventoryID) REFERENCES Inventories(InventoryID)
);

GO

-- WarehouseInboundRequests – Yêu cầu nhập kho từ Farmer
CREATE TABLE WarehouseInboundRequests (
  InboundRequestID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),    -- Mã yêu cầu nhập kho,
  InboundRequestCode VARCHAR(20) UNIQUE,                            -- INREQ-2025-0001
  BatchID UNIQUEIDENTIFIER,                                         -- Gắn với mẻ sơ chế
  DetailID UNIQUEIDENTIFIER,
  FarmerID UNIQUEIDENTIFIER NOT NULL,                               -- Người gửi yêu cầu (Farmer)
  BusinessStaffID UNIQUEIDENTIFIER NULL,							-- Người đại diện doanh nghiệp nhận
  RequestedQuantity FLOAT,                                          -- Sản lượng yêu cầu giao
  PreferredDeliveryDate DATE,                                       -- Ngày giao hàng mong muốn
  ActualDeliveryDate DATE,                                          -- Ngày giao thực tế (khi nhận thành công)
  Status NVARCHAR(50) DEFAULT 'Pending',                            -- Trạng thái: Pending, Approved, Rejected, Completed
  Note NVARCHAR(MAX),                                               -- Ghi chú thêm từ Farmer
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,            -- Ngày tạo
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,            -- Ngày cập nhật cuối
  IsDeleted BIT NOT NULL DEFAULT 0                                  -- 0 = chưa xoá, 1 = đã xoá mềm

  -- FOREIGN KEYS
  CONSTRAINT FK_WarehouseInboundRequests_Batch 
      FOREIGN KEY (BatchID) REFERENCES ProcessingBatches(BatchID),

  CONSTRAINT FK_WarehouseInboundRequests_Farmer 
      FOREIGN KEY (FarmerID) REFERENCES Farmers(FarmerID),

  CONSTRAINT FK_WarehouseInboundRequests_Manager 
      FOREIGN KEY (BusinessStaffID) REFERENCES BusinessStaffs(StaffID),

  CONSTRAINT FK_WarehouseInboundRequests_CropSeasonDetails
      FOREIGN KEY (DetailID) REFERENCES CropSeasonDetails(DetailID)
);

GO

-- WarehouseReceipts – Phiếu nhập kho
CREATE TABLE WarehouseReceipts (
  ReceiptID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),      -- Mã phiếu nhập kho
  ReceiptCode VARCHAR(20) UNIQUE,                              -- RECEIPT-2025-0001
  InboundRequestID UNIQUEIDENTIFIER NOT NULL,                  -- Gắn với yêu cầu nhập kho
  WarehouseID UNIQUEIDENTIFIER NOT NULL,                       -- Kho tiếp nhận
  BatchID UNIQUEIDENTIFIER,                                    -- Mẻ cà phê
  DetailID UNIQUEIDENTIFIER,
  ReceivedBy UNIQUEIDENTIFIER NOT NULL,                        -- Nhân viên kho tiếp nhận
  LotCode NVARCHAR(100),                                       -- Mã lô nội bộ (nếu cần)
  ReceivedQuantity FLOAT,                                      -- Sản lượng thực nhận
  ReceivedAt DATETIME,                                         -- Thời điểm tiếp nhận
  Note NVARCHAR(MAX),                                          -- Ghi chú kiểm tra hàng
  QRCodeURL NVARCHAR(255),                                     -- Link QR truy xuất
  IsDeleted BIT NOT NULL DEFAULT 0                             -- 0 = chưa xoá, 1 = đã xoá mềm

  -- FOREIGN KEYS
  CONSTRAINT FK_WarehouseReceipts_Request 
      FOREIGN KEY (InboundRequestID) REFERENCES WarehouseInboundRequests(InboundRequestID),

  CONSTRAINT FK_WarehouseReceipts_Warehouse 
      FOREIGN KEY (WarehouseID) REFERENCES Warehouses(WarehouseID),

  CONSTRAINT FK_WarehouseReceipts_Batch 
      FOREIGN KEY (BatchID) REFERENCES ProcessingBatches(BatchID),

  CONSTRAINT FK_WarehouseReceipts_Receiver 
      FOREIGN KEY (ReceivedBy) REFERENCES BusinessStaffs(StaffID),

   CONSTRAINT FK_WarehouseReceipts_CropSeasonDetails
      FOREIGN KEY (DetailID) REFERENCES CropSeasonDetails(DetailID)
);

GO

-- Products – Thông tin sản phẩm
CREATE TABLE Products (
  ProductID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),           -- Mã sản phẩm duy nhất
  ProductCode VARCHAR(50) UNIQUE,                                   -- PROD-001-BM-2025-0001
  ProductName NVARCHAR(100) NOT NULL,                               -- Tên thương mại sản phẩm
  Description NVARCHAR(MAX),                                        -- Mô tả chi tiết sản phẩm
  UnitPrice FLOAT,                                                  -- Giá bán B2B (VNĐ/Kg)
  QuantityAvailable FLOAT,                                          -- Số lượng còn lại (Kg)
  Unit NVARCHAR(20) DEFAULT 'Kg',                                   -- Đơn vị tính
  CreatedBy UNIQUEIDENTIFIER NOT NULL,                              -- Người tạo sản phẩm
  BatchID UNIQUEIDENTIFIER,                                         -- Mẻ sơ chế gốc
  InventoryID UNIQUEIDENTIFIER NOT NULL,                            -- Gắn với kho để lấy hàng
  CoffeeTypeID UNIQUEIDENTIFIER NOT NULL,                           -- Loại cà phê của sản phẩm
  OriginRegion NVARCHAR(100),                                       -- Vùng sản xuất
  OriginFarmLocation NVARCHAR(255),                                 -- Vị trí nông trại gốc
  GeographicalIndicationCode NVARCHAR(100),                         -- Chỉ dẫn địa lý
  CertificationURL NVARCHAR(255),                                   -- Link chứng nhận
  EvaluatedQuality NVARCHAR(100),                                   -- Chất lượng: Specialty...
  EvaluationScore FLOAT,                                            -- Điểm cupping
  Status NVARCHAR(50) DEFAULT 'Pending',                            -- Trạng thái sản phẩm
  ApprovedBy UNIQUEIDENTIFIER,                                      -- Người duyệt
  ApprovalNote NVARCHAR(MAX),                                       -- Ghi chú duyệt
  ApprovedAt DATETIME,                                              -- Ngày duyệt
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,            -- Ngày tạo
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,            -- Ngày cập nhật
  IsDeleted BIT NOT NULL DEFAULT 0                                  -- 0 = chưa xoá, 1 = đã xoá mềm

  -- FOREIGN KEYS
  CONSTRAINT FK_Products_CreatedBy 
      FOREIGN KEY (CreatedBy) REFERENCES UserAccounts(UserID),

  CONSTRAINT FK_Products_BatchID 
      FOREIGN KEY (BatchID) REFERENCES ProcessingBatches(BatchID),

  CONSTRAINT FK_Products_InventoryID 
      FOREIGN KEY (InventoryID) REFERENCES Inventories(InventoryID),

  CONSTRAINT FK_Products_ApprovedBy 
      FOREIGN KEY (ApprovedBy) REFERENCES UserAccounts(UserID),

  CONSTRAINT FK_Products_CoffeeType                         
      FOREIGN KEY (CoffeeTypeID) REFERENCES CoffeeTypes(CoffeeTypeID)
);

GO

-- Orders – Thông tin đơn hàng theo hợp đồng
CREATE TABLE Orders (
  OrderID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),           -- Mã đơn hàng
  OrderCode VARCHAR(20) UNIQUE,                                   -- ORD-2025-0001
  DeliveryBatchID UNIQUEIDENTIFIER NOT NULL,
  DeliveryRound INT,                                              -- Đợt giao lần mấy
  OrderDate DATETIME,                                             -- Ngày đặt hàng
  ActualDeliveryDate DATE,                                        -- Ngày giao thực tế
  TotalAmount FLOAT,                                              -- Tổng tiền đơn hàng
  Note NVARCHAR(MAX),                                             -- Ghi chú giao hàng
  Status NVARCHAR(50) DEFAULT 'Pending',                          -- Trạng thái đơn: Preparing, Shipped, Delivered,...
  CancelReason NVARCHAR(MAX),                                     -- Lý do hủy (nếu có)
  InvoiceNumber NVARCHAR(100) NULL,                               -- Số hóa đơn/VAT
  PaymentProgressJSON NVARCHAR(MAX) NULL,                         -- Tiến độ thanh toán (JSON)
  InvoiceFileURL NVARCHAR(255) NULL,                              -- File hóa đơn (scan/PDF)
  PaidAmount DECIMAL(19,2) NOT NULL DEFAULT(0),                   -- đã thanh toán bao nhiêu
  LastPaidAt DATETIME NULL,                                       -- lần thanh toán gần nhất
  PaidPercent AS (                                                -- % đã thanh toán (0–100), dựa vào TotalAmount (FLOAT)
      CASE 
        WHEN ISNULL(TotalAmount,0)=0 
          THEN CAST(0.00 AS DECIMAL(5,2))
        ELSE CAST(ROUND(
               (CAST(PaidAmount AS DECIMAL(19,2)) 
               / NULLIF(CAST(TotalAmount AS DECIMAL(19,2)),0)) * 100.0
             , 2) AS DECIMAL(5,2))
      END
  ),
  PaymentStatus AS (                               -- Trạng thái: Unpriced/Unpaid/PartiallyPaid/Paid
      CASE 
        WHEN ISNULL(TotalAmount,0)=0 THEN N'Unpriced'
        WHEN PaidAmount <= 0 THEN N'Unpaid'
        WHEN CAST(PaidAmount AS DECIMAL(19,2)) < CAST(TotalAmount AS DECIMAL(19,2)) THEN N'PartiallyPaid'
        ELSE N'Paid'
      END
  ),
  CreatedBy UNIQUEIDENTIFIER NOT NULL,                            -- Ai tạo đơn hàng
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,          -- Ngày tạo
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,          -- Ngày cập nhật
  IsDeleted BIT NOT NULL DEFAULT 0                                -- 0 = chưa xoá, 1 = đã xoá mềm

  -- FOREIGN KEYS
  CONSTRAINT FK_Orders_DeliveryBatchID 
      FOREIGN KEY (DeliveryBatchID) REFERENCES ContractDeliveryBatches(DeliveryBatchID),

  CONSTRAINT FK_Orders_CreatedBy
      FOREIGN KEY (CreatedBy) REFERENCES UserAccounts(UserID),

  CONSTRAINT CK_Orders_PaidAmount_NonNegative CHECK (PaidAmount >= 0)
);

GO

-- OrderItems – Chi tiết dòng sản phẩm trong đơn hàng
CREATE TABLE OrderItems (
  OrderItemID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),       -- Mã dòng sản phẩm trong đơn
  OrderID UNIQUEIDENTIFIER NOT NULL,                              -- Gắn với đơn hàng
  ContractDeliveryItemID UNIQUEIDENTIFIER NOT NULL,               -- Dòng đợt giao nào của hợp đồng
  ProductID UNIQUEIDENTIFIER NOT NULL,                            -- Sản phẩm cụ thể
  Quantity FLOAT,                                                 -- Số lượng được giao
  UnitPrice FLOAT,                                                -- Đơn giá đã áp dụng
  DiscountAmount FLOAT DEFAULT 0.0,                               -- Giảm giá áp dụng dòng này
  TotalPrice FLOAT,                                               -- Tổng thành tiền dòng này
  Note NVARCHAR(MAX),                                             -- Ghi chú riêng dòng hàng
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,          -- Ngày tạo
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,          -- Ngày cập nhật
  IsDeleted BIT NOT NULL DEFAULT 0                                -- 0 = chưa xoá, 1 = đã xoá mềm

  -- FOREIGN KEYS
  CONSTRAINT FK_OrderItems_OrderID 
      FOREIGN KEY (OrderID) REFERENCES Orders(OrderID),

  CONSTRAINT FK_OrderItems_ContractDeliveryItemID 
      FOREIGN KEY (ContractDeliveryItemID) REFERENCES ContractDeliveryItems(DeliveryItemID),

  CONSTRAINT FK_OrderItems_ProductID 
      FOREIGN KEY (ProductID) REFERENCES Products(ProductID),
);

GO

-- WarehouseOutboundRequests – Yêu cầu xuất kho từ nhân sự nội bộ
CREATE TABLE WarehouseOutboundRequests (
  OutboundRequestID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),     -- Mã yêu cầu xuất kho
  OutboundRequestCode VARCHAR(20) UNIQUE,                             -- OUTREQ-2025-0001
  WarehouseID UNIQUEIDENTIFIER NOT NULL,                              -- Kho chứa hàng cần xuất
  InventoryID UNIQUEIDENTIFIER NOT NULL,                              -- Dòng tồn kho cần xuất
  RequestedQuantity FLOAT NOT NULL,                                   -- Số lượng yêu cầu xuất
  Unit NVARCHAR(20) DEFAULT 'Kg',                                     -- Đơn vị tính
  RequestedBy UNIQUEIDENTIFIER NOT NULL,                              -- Người tạo yêu cầu (BusinessStaff)
  Purpose NVARCHAR(100),                                              -- Mục đích xuất: Giao đơn hàng, Kiểm định, Nội bộ...
  OrderItemID UNIQUEIDENTIFIER,                                       -- (Nullable) Liên kết dòng đơn hàng nếu xuất cho B2B
  Reason NVARCHAR(MAX),                                               -- Ghi chú/giải thích chi tiết
  Status NVARCHAR(50) DEFAULT 'Pending',                              -- Trạng thái: Pending, Approved, Rejected, Completed
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,              -- Ngày tạo yêu cầu
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,              -- Ngày cập nhật cuối
  IsDeleted BIT NOT NULL DEFAULT 0                                    -- 0 = chưa xoá, 1 = đã xoá mềm

  -- FOREIGN KEYS
  CONSTRAINT FK_WarehouseOutboundRequests_Warehouse 
      FOREIGN KEY (WarehouseID) REFERENCES Warehouses(WarehouseID),

  CONSTRAINT FK_WarehouseOutboundRequests_Inventory 
      FOREIGN KEY (InventoryID) REFERENCES Inventories(InventoryID),

  CONSTRAINT FK_WarehouseOutboundRequests_Manager 
      FOREIGN KEY (RequestedBy) REFERENCES BusinessManagers(ManagerID),

  CONSTRAINT FK_WarehouseOutboundRequests_OrderItem 
      FOREIGN KEY (OrderItemID) REFERENCES OrderItems(OrderItemID)
);

GO

-- WarehouseOutboundReceipts – Phiếu xuất kho
CREATE TABLE WarehouseOutboundReceipts (
  OutboundReceiptID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),  -- Mã phiếu xuất kho
  OutboundReceiptCode VARCHAR(50) UNIQUE,                          -- OUT-RECEIPT-2025-0001 (Format: OUT-RECEIPT-YYYY-####)
  OutboundRequestID UNIQUEIDENTIFIER NOT NULL,                     -- Gắn với yêu cầu xuất kho
  WarehouseID UNIQUEIDENTIFIER NOT NULL,                           -- Kho xuất
  InventoryID UNIQUEIDENTIFIER NOT NULL,                           -- Dòng tồn kho đã xuất
  BatchID UNIQUEIDENTIFIER NOT NULL,                               -- Mẻ cà phê xuất
  Quantity FLOAT NOT NULL,                                         -- Lượng xuất
  ExportedBy UNIQUEIDENTIFIER NOT NULL,                            -- Nhân viên xác nhận
  ExportedAt DATETIME,                                             -- Thời điểm xuất
  DestinationNote NVARCHAR(MAX),                                   -- Địa điểm hoặc mục đích giao
  Note NVARCHAR(MAX),                                              -- Ghi chú thêm
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,           -- Ngày tạo yêu cầu
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,           -- Ngày cập nhật cuối
  IsDeleted BIT NOT NULL DEFAULT 0                                 -- 0 = chưa xoá, 1 = đã xoá mềm

  -- FOREIGN KEYS
  CONSTRAINT FK_WarehouseOutboundReceipts_Request 
      FOREIGN KEY (OutboundRequestID) REFERENCES WarehouseOutboundRequests(OutboundRequestID),

  CONSTRAINT FK_WarehouseOutboundReceipts_Warehouse 
      FOREIGN KEY (WarehouseID) REFERENCES Warehouses(WarehouseID),

  CONSTRAINT FK_WarehouseOutboundReceipts_Inventory 
      FOREIGN KEY (InventoryID) REFERENCES Inventories(InventoryID),

  CONSTRAINT FK_WarehouseOutboundReceipts_Batch 
      FOREIGN KEY (BatchID) REFERENCES ProcessingBatches(BatchID),

  CONSTRAINT FK_WarehouseOutboundReceipts_Exporter 
      FOREIGN KEY (ExportedBy) REFERENCES BusinessStaffs(StaffID)
);

GO

-- Shipments – Thông tin chuyến giao hàng
CREATE TABLE Shipments (
  ShipmentID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),        -- Mã giao hàng
  ShipmentCode VARCHAR(20) UNIQUE,                                -- SHIP-2025-0001
  OrderID UNIQUEIDENTIFIER NOT NULL,                              -- Gắn với đơn hàng
  DeliveryStaffID UNIQUEIDENTIFIER NOT NULL,                      -- Nhân viên giao hàng
  ShippedQuantity FLOAT,                                          -- Khối lượng đã giao
  ShippedAt DATETIME,                                             -- Ngày bắt đầu giao
  DeliveryStatus NVARCHAR(50) DEFAULT 'In_Transit',               -- Trạng thái: In_Transit, Delivered, Failed
  ReceivedAt DATETIME,                                            -- Ngày nhận hàng thành công
  CreatedBy UNIQUEIDENTIFIER NOT NULL,                            -- Người tạo chuyến giao
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,          -- Ngày tạo yêu cầu
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,          -- Ngày cập nhật cuối
  IsDeleted BIT NOT NULL DEFAULT 0                                -- 0 = chưa xoá, 1 = đã xoá mềm

  -- FOREIGN KEYS
  CONSTRAINT FK_Shipments_OrderID 
      FOREIGN KEY (OrderID) REFERENCES Orders(OrderID),

  CONSTRAINT FK_Shipments_CreatedBy 
      FOREIGN KEY (CreatedBy) REFERENCES UserAccounts(UserID),

  CONSTRAINT FK_Shipments_DeliveryStaffID 
      FOREIGN KEY (DeliveryStaffID) REFERENCES UserAccounts(UserID)
);

GO

-- ShipmentDetails – Chi tiết sản phẩm theo từng chuyến hàng
CREATE TABLE ShipmentDetails (
  ShipmentDetailID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), -- Mã dòng sản phẩm trong shipment
  ShipmentID UNIQUEIDENTIFIER NOT NULL,                          -- Gắn với chuyến hàng
  OrderItemID UNIQUEIDENTIFIER NOT NULL,                         -- Gắn với dòng sản phẩm cụ thể trong đơn
  Quantity FLOAT,                                                -- Số lượng giao
  Unit NVARCHAR(20) DEFAULT 'Kg',                                -- Đơn vị tính
  Note NVARCHAR(MAX),                                            -- Ghi chú riêng dòng hàng
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,         -- Ngày tạo
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,         -- Ngày cập nhật cuối
  IsDeleted BIT NOT NULL DEFAULT 0                               -- 0 = chưa xoá, 1 = đã xoá mềm

  -- FOREIGN KEYS
  CONSTRAINT FK_ShipmentDetails_ShipmentID 
      FOREIGN KEY (ShipmentID) REFERENCES Shipments(ShipmentID),

  CONSTRAINT FK_ShipmentDetails_OrderItemID 
      FOREIGN KEY (OrderItemID) REFERENCES OrderItems(OrderItemID)
);

GO

-- OrderComplaints – Khiếu nại đơn hàng
CREATE TABLE OrderComplaints (
  ComplaintID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),    -- Mã khiếu nại
  ComplaintCode VARCHAR(20) UNIQUE,                            -- CMP-2025-0001
  OrderItemID UNIQUEIDENTIFIER NOT NULL,                       -- Dòng hàng bị khiếu nại
  RaisedBy UNIQUEIDENTIFIER NOT NULL,                          -- Người mua khiếu nại
  ComplaintType NVARCHAR(100),                                 -- Loại khiếu nại: "Sai chất lượng", "Thiếu số lượng", "Vỡ bao bì"
  Description NVARCHAR(MAX),                                   -- Nội dung chi tiết
  EvidenceURL NVARCHAR(255),                                   -- Link ảnh/video minh chứng (nếu có)
  Status NVARCHAR(50) DEFAULT 'Open',                          -- Trạng thái xử lý: Open, Investigating, Resolved, Rejected
  ResolutionNote NVARCHAR(MAX),                                -- Hướng xử lý hoặc kết quả
  ResolvedBy UNIQUEIDENTIFIER,                                 -- Người xử lý (doanh nghiệp bán)
  CreatedBy UNIQUEIDENTIFIER NOT NULL,                         -- Người thao tác tạo bản ghi
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,       -- Ngày tạo khiếu nại
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,       -- Ngày cập nhật cuối
  ResolvedAt DATETIME,                                         -- Ngày xử lý hoàn tất
  IsDeleted BIT NOT NULL DEFAULT 0                             -- 0 = chưa xoá, 1 = đã xoá mềm

  -- FOREIGN KEYS
  CONSTRAINT FK_OrderComplaints_OrderItemID 
      FOREIGN KEY (OrderItemID) REFERENCES OrderItems(OrderItemID),

  CONSTRAINT FK_OrderComplaints_CreatedBy 
      FOREIGN KEY (CreatedBy) REFERENCES UserAccounts(UserID),

  CONSTRAINT FK_OrderComplaints_RaisedBy 
      FOREIGN KEY (RaisedBy) REFERENCES BusinessBuyers(BuyerID),

  CONSTRAINT FK_OrderComplaints_ResolvedBy 
      FOREIGN KEY (ResolvedBy) REFERENCES BusinessManagers(ManagerID)
);

GO

-- SystemNotifications – Thông báo hệ thống
CREATE TABLE SystemNotifications (
  NotificationID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),    -- Mã thông báo
  NotificationCode VARCHAR(20) UNIQUE,                            -- NOTI-2025-0001
  Title NVARCHAR(255),                                            -- Tiêu đề
  Message NVARCHAR(MAX),                                          -- Nội dung chi tiết
  Type NVARCHAR(50),                                              -- Loại thông báo: "Low_Stock", "Issue_Reported",...
  CreatedBy UNIQUEIDENTIFIER,                                     -- Người khởi tạo (nullable)
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,          -- Thời gian tạo
  IsDeleted BIT NOT NULL DEFAULT 0                                -- 0 = chưa xoá, 1 = đã xoá mềm

  -- FOREIGN KEYS
  CONSTRAINT FK_SystemNotifications_CreatedBy 
      FOREIGN KEY (CreatedBy) REFERENCES UserAccounts(UserID)
);

GO

-- SystemNotificationRecipients – Danh sách người nhận thông báo
CREATE TABLE SystemNotificationRecipients (
  ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),                -- Mã dòng
  NotificationID UNIQUEIDENTIFIER NOT NULL,                       -- Gắn thông báo
  RecipientID UNIQUEIDENTIFIER NOT NULL,                          -- Người nhận thông báo
  IsRead BIT DEFAULT 0,                                           -- Đã đọc chưa?
  ReadAt DATETIME,                                                -- Thời điểm đọc
  IsDeleted BIT NOT NULL DEFAULT 0                                -- 0 = chưa xoá, 1 = đã xoá mềm

  -- FOREIGN KEYS
  CONSTRAINT FK_NotificationRecipients_Notification 
      FOREIGN KEY (NotificationID) REFERENCES SystemNotifications(NotificationID),

  CONSTRAINT FK_NotificationRecipients_Recipient 
      FOREIGN KEY (RecipientID) REFERENCES UserAccounts(UserID)
);

GO

-- MediaFiles – Ảnh/video đính kèm cho nhiều thực thể khác nhau
-- Bảng này lưu trữ các media (ảnh/video) được liên kết với các thực thể như:
-- - CropProgress: ảnh theo dõi tiến độ mùa vụ
-- - ProcessingProgress: ảnh từng bước trong quá trình sơ chế
-- - GeneralFarmerReport: ảnh báo cáo sự cố từ Farmer
-- Những thực thể khác như ExpertAdvice, WasteDisposal... nếu có nhu cầu sẽ được thêm sau qua ALTER TABLE.
CREATE TABLE MediaFiles (
    MediaID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),           -- ID media
    RelatedEntity NVARCHAR(50) NOT NULL CHECK (
        RelatedEntity IN ('CropProgress', 'ProcessingProgress', 'GeneralFarmerReport', 'ExpertAdvice')
    ),                                                              -- Loại thực thể liên kết
    RelatedID UNIQUEIDENTIFIER NOT NULL,                            -- ID của thực thể cụ thể
    MediaType VARCHAR(10) NOT NULL CHECK (
        MediaType IN ('image', 'video')
    ),                                                              -- Loại media
    MediaURL VARCHAR(255) NOT NULL,                                 -- Đường dẫn ảnh/video
    Caption NVARCHAR(255),                                          -- Ghi chú
    UploadedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,         -- Ngày upload
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,          -- Ngày tạo dòng dữ liệu
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,          -- Ngày cập nhật cuối
	IsDeleted BIT NOT NULL DEFAULT 0                                -- 0 = chưa xoá, 1 = đã xoá mềm
);

GO

-- SystemConfiguration - Bảng lưu các tham số hệ thống để phục vụ validation động theo phạm vi áp dụng.
CREATE TABLE SystemConfiguration (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,                 -- Mã định danh tham số
    Description NVARCHAR(255),                   -- Mô tả ý nghĩa tham số
    MinValue DECIMAL(18,2),                      -- Giá trị tối thiểu (nếu có)
    MaxValue DECIMAL(18,2),                      -- Giá trị tối đa (nếu có)
    Unit NVARCHAR(20),                           -- Đơn vị đo (Kg, %, times,...)
    IsActive BIT NOT NULL DEFAULT 1,             -- Còn hiệu lực không?
    EffectedDateFrom DATETIME NOT NULL,          -- Thời điểm bắt đầu áp dụng
    EffectedDateTo DATETIME NULL,                -- Thời điểm kết thúc áp dụng (nếu có)
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL DEFAULT GETDATE(),
	IsDeleted BIT NOT NULL DEFAULT 0             -- 0 = chưa xoá, 1 = đã xoá mềm
);

GO

-- SystemConfigurationUsers – Liên kết người dùng cụ thể với quyền quản lý tham số hệ thống
CREATE TABLE SystemConfigurationUsers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SystemConfigurationID INT NOT NULL,                  -- FK đến bảng SystemConfiguration
    UserID UNIQUEIDENTIFIER NOT NULL,                    -- Người dùng có quyền chỉnh sửa
    PermissionLevel NVARCHAR(50) DEFAULT 'manage',       -- Quyền: manage / view / approve (nếu mở rộng sau)
    GrantedAt DATETIME NOT NULL DEFAULT GETDATE(),       -- Thời điểm gán quyền
    RevokedAt DATETIME NULL,                             -- Nếu bị thu hồi
	UpdatedAt DATETIME NULL DEFAULT GETDATE(),
	IsDeleted BIT NOT NULL DEFAULT 0                     -- 0 = chưa xoá, 1 = đã xoá mềm

    -- Foreign Keys
    CONSTRAINT FK_SystemConfigUsers_ConfigID 
        FOREIGN KEY (SystemConfigurationID) REFERENCES SystemConfiguration(Id),

    CONSTRAINT FK_SystemConfigUsers_UserID 
        FOREIGN KEY (UserID) REFERENCES UserAccounts(UserID)
);

GO

-- Insert vào bảng Roles
INSERT INTO Roles (RoleName, Description)
VALUES 
(N'Admin', N'Quản trị toàn hệ thống'),
(N'BusinessManager', N'Người quản lý doanh nghiệp'),
(N'BusinessStaff', N'Nhân viên doanh nghiệp'),
(N'Farmer', N'Nông dân tham gia chuỗi cung ứng'),
(N'AgriculturalExpert', N'Chuyên gia nông nghiệp'),
(N'DeliveryStaff', N'Nhân viên giao hàng');

GO

-- Insert mẫu vào bảng UserAccounts
-- Admin
INSERT INTO UserAccounts (
   UserCode, Email, PhoneNumber, Name, Gender, DateOfBirth, Address, PasswordHash, RoleID, isVerified, emailVerified
)
VALUES (
   'USR-2025-0001', 'admin@gmail.com', '0344033388', N'Phạm Huỳnh Xuân Đăng', 'Male', 
   '1990-01-01', N'Đắk Lắk', '$2a$11$mHeU1UxLZyZrwtWtikdAJeM3BteW4QrgBJOd8rWMz2sR9ZZyWayRS', 1, 1, 1
);

-- Business Manager
INSERT INTO UserAccounts (
   UserCode, Email, PhoneNumber, Name, Gender, DateOfBirth, Address, PasswordHash, RoleID, isVerified, emailVerified
)
VALUES (
   'USR-2025-0002', 'businessManager@gmail.com', '0325194357', N'Lê Hoàng Phúc', 'Male', 
   '1985-05-10', N'Hồ Chí Minh', '$2a$11$mHeU1UxLZyZrwtWtikdAJeM3BteW4QrgBJOd8rWMz2sR9ZZyWayRS', 2, 1, 1
);

INSERT INTO UserAccounts (
   UserCode, Email, PhoneNumber, Name, Gender, DateOfBirth, Address, PasswordHash, RoleID, isVerified, emailVerified
)
VALUES (
   'USR-2025-0008', 'manager2@gmail.com', '0901000001', N'Nguyễn Thị Thanh Trúc', 'Female', 
   '1982-11-20', N'Lâm Đồng', '$2a$11$mHeU1UxLZyZrwtWtikdAJeM3BteW4QrgBJOd8rWMz2sR9ZZyWayRS', 2, 1, 1
);

-- Farmer
INSERT INTO UserAccounts (
   UserCode, Email, PhoneNumber, Name, Gender, DateOfBirth, Address, PasswordHash, RoleID, isVerified, emailVerified
)
VALUES (
   'USR-2025-0003', 'farmer@gmail.com', '0942051066', N'Nguyễn Nhật Minh', 'Male', 
   '1988-03-15', N'Buôn Ma Thuột', '$2a$11$mHeU1UxLZyZrwtWtikdAJeM3BteW4QrgBJOd8rWMz2sR9ZZyWayRS', 4, 1, 1
);

INSERT INTO UserAccounts (
   UserCode, Email, PhoneNumber, Name, Gender, DateOfBirth, Address, PasswordHash, RoleID, isVerified, emailVerified
)
VALUES 
('USR-2025-0009', 'farmer2@gmail.com', '0901503702', N'Hồ Văn Tiến', 'Male', '1980-02-10', N'Đắk Song', '$2a$11$mHeU1UxLZyZrwtWtikdAJeM3BteW4QrgBJOd8rWMz2sR9ZZyWayRS', 4, 1, 1),
('USR-2025-0010', 'farmer3@gmail.com', '0902893043', N'Y Moal Êban', 'Male', '1975-12-01', N'Buôn Đôn', '$2a$11$mHeU1UxLZyZrwtWtikdAJeM3BteW4QrgBJOd8rWMz2sR9ZZyWayRS', 4, 1, 1),
('USR-2025-0011', 'farmer4@gmail.com', '0905354882', N'H Nguyễn H Lan', 'Female', '1990-07-18', N'Krông Pắk', '$2a$11$mHeU1UxLZyZrwtWtikdAJeM3BteW4QrgBJOd8rWMz2sR9ZZyWayRS', 4, 1, 1),
('USR-2025-0012', 'farmer5@gmail.com', '0903765951', N'Y Thảo Niê', 'Female', '1986-06-30', N'Cư M’gar', '$2a$11$mHeU1UxLZyZrwtWtikdAJeM3BteW4QrgBJOd8rWMz2sR9ZZyWayRS', 4, 1, 1);

-- Expert
INSERT INTO UserAccounts (
   UserCode, Email, PhoneNumber, Name, Gender, DateOfBirth, Address, PasswordHash, RoleID, isVerified, emailVerified
)
VALUES (
   'USR-2025-0004', 'expert@gmail.com', '0975616076', N'Lê Hoàng Thiên Vũ', 'Male', 
   '1978-08-22', N'Hà Nội', '$2a$11$mHeU1UxLZyZrwtWtikdAJeM3BteW4QrgBJOd8rWMz2sR9ZZyWayRS', 5, 1, 1
);

INSERT INTO UserAccounts (
   UserCode, Email, PhoneNumber, Name, Gender, DateOfBirth, Address, PasswordHash, RoleID, isVerified, emailVerified
)
VALUES (
   'USR-2025-0013', 'expert2@gmail.com', '0906478253', N'Phan Minh Thông', 'Male', 
   '1972-03-03', N'Cần Thơ', '$2a$11$mHeU1UxLZyZrwtWtikdAJeM3BteW4QrgBJOd8rWMz2sR9ZZyWayRS', 5, 1, 1
);

-- Business Staff
INSERT INTO UserAccounts (
   UserCode, Email, PhoneNumber, Name, Gender, DateOfBirth, Address, PasswordHash, RoleID, isVerified, emailVerified
)
VALUES (
   'USR-2025-0005', 'businessStaff@gmail.com', '0941716075', N'Phạm Trường Nam', 'Male', 
   '1999-09-12', N'Đắk Lắk', '$2a$11$mHeU1UxLZyZrwtWtikdAJeM3BteW4QrgBJOd8rWMz2sR9ZZyWayRS', 3, 1, 1
);

INSERT INTO UserAccounts (
   UserCode, Email, PhoneNumber, Name, Gender, DateOfBirth, Address, PasswordHash, RoleID, isVerified, emailVerified
)
VALUES 
('USR-2025-0014', 'staff3@gmail.com', '0901046707', N'Nguyễn Khắc Minh', 'Male', '1992-01-01', N'Đà Nẵng', '$2a$11$mHeU1UxLZyZrwtWtikdAJeM3BteW4QrgBJOd8rWMz2sR9ZZyWayRS', 3, 1, 1),
('USR-2025-0015', 'staff4@gmail.com', '0901874688', N'Trần Nhật Linh', 'Female', '1995-05-05', N'Huế', '$2a$11$mHeU1UxLZyZrwtWtikdAJeM3BteW4QrgBJOd8rWMz2sR9ZZyWayRS', 3, 1, 1),
('USR-2025-0016', 'staff5@gmail.com', '0902546569', N'Lý Văn Hùng', 'Male', '1998-08-08', N'Quảng Ngãi', '$2a$11$mHeU1UxLZyZrwtWtikdAJeM3BteW4QrgBJOd8rWMz2sR9ZZyWayRS', 3, 1, 1);

-- Delivery Staff
INSERT INTO UserAccounts (
   UserCode, Email, PhoneNumber, Name, Gender, DateOfBirth, Address, PasswordHash, RoleID, isVerified, emailVerified
)
VALUES (
   'USR-2025-0006', 'deliverystaff@gmail.com', '0901234568', N'Trần Văn Giang', 'Male', 
   '1994-06-19', N'Gia Lai', '$2a$11$mHeU1UxLZyZrwtWtikdAJeM3BteW4QrgBJOd8rWMz2sR9ZZyWayRS', 6, 1, 1
);

INSERT INTO UserAccounts (
   UserCode, Email, PhoneNumber, Name, Gender, DateOfBirth, Address, PasswordHash, RoleID, isVerified, emailVerified
)
VALUES (
   'USR-2025-0017', 'deliverystaff2@gmail.com', '0901010010', N'Ngô Văn Quý', 'Male', 
   '1991-04-14', N'Kon Tum', '$2a$11$mHeU1UxLZyZrwtWtikdAJeM3BteW4QrgBJOd8rWMz2sR9ZZyWayRS', 6, 1, 1
)

GO

-- Insert vào bảng PaymentConfigurations
-- BusinessManager: Đăng ký & phí duy trì hằng tháng
INSERT INTO PaymentConfigurations (RoleID, FeeType, Amount, Description, EffectiveFrom)
VALUES 
((SELECT RoleID FROM Roles WHERE RoleName = 'BusinessManager'), 'Registration', 1000000, 
 N'Phí đăng ký tài khoản doanh nghiệp', '2025-06-01'),

((SELECT RoleID FROM Roles WHERE RoleName = 'BusinessManager'), 'MonthlyFee', 400000, 
 N'Phí duy trì hệ thống theo tháng cho doanh nghiệp', '2025-06-01');

-- Farmer: Đăng ký
INSERT INTO PaymentConfigurations (RoleID, FeeType, Amount, Description, EffectiveFrom)
VALUES 
((SELECT RoleID FROM Roles WHERE RoleName = 'Farmer'), 'Registration', 200000, 
 N'Phí đăng ký tham gia chuỗi cung ứng cho nông hộ', '2025-06-01');

-- Farmer: Phí duy trì theo năm
INSERT INTO PaymentConfigurations (RoleID, FeeType, Amount, Description, EffectiveFrom)
VALUES 
((SELECT RoleID FROM Roles WHERE RoleName = 'Farmer'), 'AnnualMaintenanceFee', 300000, 
 N'Phí duy trì tài khoản theo năm cho nông hộ. Không bắt buộc ngay, nhưng cần để tiếp tục đăng ký kế hoạch với doanh nghiệp.', 
 '2025-07-01');

 -- BusinessManager: Phí đăng bài
 INSERT INTO PaymentConfigurations (RoleID, FeeType, Amount, Description, EffectiveFrom)
VALUES 
((SELECT RoleID FROM Roles WHERE RoleName = 'BusinessManager'), 'PlanPosting', 100000, 
 N'Phí áp dụng cho Quản lý doanh nghiệp khi đăng tải kế hoạch thu mua cafe trên hệ thống.', '2025-06-01');

GO

-- Insert vào bảng Payments
-- Business Manager đăng ký
DECLARE @PayTime1 DATETIME = '2025-06-01 08:00:00';

INSERT INTO Payments (
  Email, UserID, PaymentCode, ConfigID, PaymentAmount,
  PaymentMethod, PaymentStatus, PaymentTime, AdminVerified,
  CreatedAt, RelatedEntityID
)
SELECT
  'businessmanager@gmail.com',
  (SELECT UserID FROM UserAccounts WHERE Email = 'businessmanager@gmail.com'),
  'PAY-2025-0001',
  pc.ConfigID,
  pc.Amount,                               -- lấy số tiền từ cấu hình
  'VNPay',
  'success',
  @PayTime1,
  1,
  CURRENT_TIMESTAMP,
  NULL
FROM (
  SELECT TOP (1) pc.*
  FROM PaymentConfigurations pc
  JOIN Roles r ON r.RoleID = pc.RoleID
  WHERE r.RoleName = 'BusinessManager'
    AND pc.FeeType = 'Registration'
    AND pc.EffectiveFrom <= @PayTime1
    AND (pc.EffectiveTo IS NULL OR pc.EffectiveTo >= @PayTime1)
  ORDER BY pc.EffectiveFrom DESC
) pc;


-- Business Manager đóng phí tháng 6
DECLARE @PayTime2 DATETIME = '2025-06-03 09:30:00';

INSERT INTO Payments (
  Email, UserID, PaymentCode, ConfigID, PaymentAmount,
  PaymentMethod, PaymentStatus, PaymentTime, AdminVerified,
  CreatedAt, RelatedEntityID
)
SELECT
  'businessmanager@gmail.com',
  (SELECT UserID FROM UserAccounts WHERE Email = 'businessmanager@gmail.com'),
  'PAY-2025-0002',
  pc.ConfigID,
  pc.Amount,
  'VNPay',
  'success',
  @PayTime2,
  1,
  CURRENT_TIMESTAMP,
  NULL
FROM (
  SELECT TOP (1) pc.*
  FROM PaymentConfigurations pc
  JOIN Roles r ON r.RoleID = pc.RoleID
  WHERE r.RoleName = 'BusinessManager'
    AND pc.FeeType = 'MonthlyFee'
    AND pc.EffectiveFrom <= @PayTime2
    AND (pc.EffectiveTo IS NULL OR pc.EffectiveTo >= @PayTime2)
  ORDER BY pc.EffectiveFrom DESC
) pc;

-- Farmer đăng ký
DECLARE @PayTime3 DATETIME = '2025-06-05 10:15:00';

INSERT INTO Payments (
  Email, UserID, PaymentCode, ConfigID, PaymentAmount,
  PaymentMethod, PaymentStatus, PaymentTime, AdminVerified,
  CreatedAt, RelatedEntityID
)
SELECT
  'farmer@gmail.com',
  (SELECT UserID FROM UserAccounts WHERE Email = 'farmer@gmail.com'),
  'PAY-2025-0003',
  pc.ConfigID,
  pc.Amount,
  'VNPay',
  'success',
  @PayTime3,
  1,
  CURRENT_TIMESTAMP,
  NULL
FROM (
  SELECT TOP (1) pc.*
  FROM PaymentConfigurations pc
  JOIN Roles r ON r.RoleID = pc.RoleID
  WHERE r.RoleName = 'Farmer'
    AND pc.FeeType = 'Registration'
    AND pc.EffectiveFrom <= @PayTime3
    AND (pc.EffectiveTo IS NULL OR pc.EffectiveTo >= @PayTime3)
  ORDER BY pc.EffectiveFrom DESC
) pc;

GO

-- Insert vào bảng Wallets
-- Ví hệ thống duy nhất
INSERT INTO Wallets (UserID, WalletType, TotalBalance)
VALUES (NULL, 'System', 1600000);

-- Ví người dùng khởi tạo, nhưng số dư = 0
INSERT INTO Wallets (UserID, WalletType, TotalBalance)
VALUES 
((SELECT UserID FROM UserAccounts WHERE Email = 'businessmanager@gmail.com'), 'Business', 0),
((SELECT UserID FROM UserAccounts WHERE Email = 'farmer@gmail.com'), 'Farmer', 0);

GO

-- Insert vào bảng WalletTransactions
-- BusinessManager đăng ký (PAY-2025-0001)
INSERT INTO WalletTransactions (WalletID, PaymentID, Amount, TransactionType, Description)
VALUES (
  (SELECT WalletID FROM Wallets WHERE WalletType = 'System' AND UserID IS NULL),
  (SELECT PaymentID FROM Payments WHERE PaymentCode = 'PAY-2025-0001'),
  1000000, 'TopUp', N'Thu phí đăng ký tài khoản BusinessManager'
);

-- BusinessManager đóng phí tháng (PAY-2025-0002)
INSERT INTO WalletTransactions (WalletID, PaymentID, Amount, TransactionType, Description)
VALUES (
  (SELECT WalletID FROM Wallets WHERE WalletType = 'System' AND UserID IS NULL),
  (SELECT PaymentID FROM Payments WHERE PaymentCode = 'PAY-2025-0002'),
  400000, 'TopUp', N'Thu phí duy trì tháng cho BusinessManager'
);

-- Farmer đăng ký (PAY-2025-0003)
INSERT INTO WalletTransactions (WalletID, PaymentID, Amount, TransactionType, Description)
VALUES (
  (SELECT WalletID FROM Wallets WHERE WalletType = 'System' AND UserID IS NULL),
  (SELECT PaymentID FROM Payments WHERE PaymentCode = 'PAY-2025-0003'),
  200000, 'TopUp', N'Thu phí đăng ký tài khoản Farmer'
);

GO

-- Insert vào bảng BusinessManagers
-- Lấy UserID của Business Manager
DECLARE @BMUserID UNIQUEIDENTIFIER = (
   SELECT UserID FROM UserAccounts WHERE Email = 'businessmanager@gmail.com'
);

INSERT INTO BusinessManagers (
   UserID, ManagerCode, CompanyName, Position, Department, 
   CompanyAddress, TaxID, Website, ContactEmail
)
VALUES (
   @BMUserID, 'BM-2025-0001', N'Công ty Cà Phê DakLak', N'Giám đốc điều hành', N'Phòng điều phối', 
   N'15 Lê Duẩn, BMT, Đắk Lắk', '6001234567', 'https://daklakcoffee.vn', 'lehoangphuc14122003@gmail.com'
);

DECLARE @BM2UserID UNIQUEIDENTIFIER = (
   SELECT UserID FROM UserAccounts WHERE Email = 'manager2@gmail.com'
);

INSERT INTO BusinessManagers (
   UserID, ManagerCode, CompanyName, Position, Department, 
   CompanyAddress, TaxID, Website, ContactEmail
)
VALUES (
   @BM2UserID, 'BM-2025-0002', N'Công ty TNHH Cà Phê Trúc Phúc', N'Tổng Giám đốc', N'Phòng Kinh doanh',
   N'56 Lê Thánh Tông, Bảo Lộc, Lâm Đồng', '5800987654', 'https://trucphuccoffee.vn', 'manager2@gmail.com'
);

GO

-- Insert vào bảng Farmers
-- Lấy UserID của Farmer
DECLARE @FarmerUserID UNIQUEIDENTIFIER = (
   SELECT UserID FROM UserAccounts WHERE Email = 'farmer@gmail.com'
);

INSERT INTO Farmers (UserID, FarmerCode, FarmLocation, FarmSize, CertificationStatus)
VALUES (@FarmerUserID, 'FRM-2025-0001', N'Xã Ea Tu, TP. Buôn Ma Thuột', 2.5, N'VietGAP');

DECLARE @Farmer2ID UNIQUEIDENTIFIER = (
   SELECT UserID FROM UserAccounts WHERE Email = 'farmer2@gmail.com'
);

DECLARE @Farmer3ID UNIQUEIDENTIFIER = (
   SELECT UserID FROM UserAccounts WHERE Email = 'farmer3@gmail.com'
);

DECLARE @Farmer4ID UNIQUEIDENTIFIER = (
   SELECT UserID FROM UserAccounts WHERE Email = 'farmer4@gmail.com'
);

DECLARE @Farmer5ID UNIQUEIDENTIFIER = (
   SELECT UserID FROM UserAccounts WHERE Email = 'farmer5@gmail.com'
);

INSERT INTO Farmers (UserID, FarmerCode, FarmLocation, FarmSize, CertificationStatus) VALUES
(@Farmer2ID, 'FRM-2025-0002', N'Đắk Song, Đắk Nông', 3.2, N'VietGAP'),
(@Farmer3ID, 'FRM-2025-0003', N'Buôn Đôn, Đắk Lắk', 4.1, N'GlobalGAP'),
(@Farmer4ID, 'FRM-2025-0004', N'Krông Pắk, Đắk Lắk', 2.8, N'OCOP 3*'),
(@Farmer5ID, 'FRM-2025-0005', N'Cư M’gar, Đắk Lắk', 3.5, N'VietGAP');

GO

-- Insert vào bảng AgriculturalExperts
-- Lấy UserID của Expert
DECLARE @ExpertUserID UNIQUEIDENTIFIER = (
   SELECT UserID FROM UserAccounts WHERE Email = 'expert@gmail.com'
);

INSERT INTO AgriculturalExperts (
   UserID, ExpertCode, ExpertiseArea, Qualifications, 
   YearsOfExperience, AffiliatedOrganization, Bio, Rating
)
VALUES (
   @ExpertUserID, 'EXP-2025-0001', N'Bệnh cây cà phê', N'Tiến sĩ Nông nghiệp', 12, 
   N'Viện Khoa học Kỹ thuật Nông Lâm nghiệp Tây Nguyên', N'Chuyên gia hàng đầu về sâu bệnh và canh tác bền vững.', 4.8
);

DECLARE @Expert2ID UNIQUEIDENTIFIER = (
   SELECT UserID FROM UserAccounts WHERE Email = 'expert2@gmail.com'
);

INSERT INTO AgriculturalExperts (
   UserID, ExpertCode, ExpertiseArea, Qualifications, 
   YearsOfExperience, AffiliatedOrganization, Bio, Rating
)
VALUES (
   @Expert2ID, 'EXP-2025-0002', N'Phân tích đất – dinh dưỡng cây trồng', N'Thạc sĩ Nông học', 10,
   N'Trung tâm Kiểm nghiệm & Ứng dụng Nông nghiệp miền Nam',
   N'Chuyên gia giàu kinh nghiệm về dinh dưỡng cây cà phê và quy trình canh tác sinh thái.', 4.6
);

GO

-- Lấy ManagerID
DECLARE @BMID UNIQUEIDENTIFIER = (
  SELECT ManagerID FROM BusinessManagers 
  WHERE UserID = (SELECT UserID FROM UserAccounts WHERE Email = 'businessmanager@gmail.com')
);

-- Insert Business Buyer: CTCP Thương Mại Xuất Khẩu VinCafé
INSERT INTO BusinessBuyers (
    BuyerID, BuyerCode, CreatedBy, CompanyName, ContactPerson, Position, 
    CompanyAddress, TaxID, Email, Phone, CreatedAt, UpdatedAt
)
VALUES (
    'ED49B648-F170-48AC-8535-823C80381179', 'BM-2025-0001-BUY-2025-001', @BMID, N'CTCP Thương Mại Xuất Khẩu VinCafé',
    N'Nguyễn Văn Hậu', N'Tổng Giám Đốc', N'123 Đường Cà Phê, P. Tân Lợi, TP. Buôn Ma Thuột, Đắk Lắk',
    '6001234567', 'vincafe@coffee.com.vn', '02623779999',
    '2025-06-15 11:37:11', '2025-06-15 11:37:11'
);

INSERT INTO BusinessBuyers (BuyerID, BuyerCode, CreatedBy, CompanyName, ContactPerson, Position, CompanyAddress, TaxID, Email, Phone, CreatedAt, UpdatedAt)
VALUES 
(NEWID(), 'BM-2025-0001-BUY-2025-002', @BMID, N'Công ty TNHH CoffeeLand',       N'Lê Minh Tâm',     N'Giám đốc thu mua', N'52 Phan Chu Trinh, TP. Buôn Ma Thuột',      '6001123456', 'coffeeland@gmail.com',      '0262384783', GETDATE(), GETDATE()),
(NEWID(), 'BM-2025-0001-BUY-2025-003', @BMID, N'CTCP Cà Phê Bảo An',            N'Trịnh Thanh Hà',  N'Quản lý cung ứng',  N'121 Lê Thánh Tông, TP. Buôn Ma Thuột',     '6001567890', 'baoan@coffee.vn',          '0262397349', GETDATE(), GETDATE()),
(NEWID(), 'BM-2025-0001-BUY-2025-004', @BMID, N'VietnamCoffee Export Co.',      N'Hoàng Đức Tài',   N'Trưởng phòng thu mua', N'234 Nguyễn Tất Thành, Đắk Lắk',         '6001789012', 'vncoffee@export.vn',       '0262377121', GETDATE(), GETDATE()),
(NEWID(), 'BM-2025-0001-BUY-2025-005', @BMID, N'Công ty TNHH Cà Phê Tây Nguyên', N'Ngô Thị Huyền',   N'Giám sát chất lượng', N'03 Hùng Vương, Cư M’gar, Đắk Lắk',       '6001678901', 'taynguyen.coffee@gmail.com','0262371556', GETDATE(), GETDATE());

GO

-- Insert CoffeeTypes – Danh sách các loại cà phê phổ biến tại Đắk Lắk
-- Định dạng TypeCode: CFT-2025-0001

-- Arabica (vùng cao Đắk Lắk như M'Đrắk, Krông Bông)
INSERT INTO CoffeeTypes (TypeCode, TypeName, BotanicalName, Description, TypicalRegion, SpecialtyLevel, DefaultYieldPerHectare)
VALUES (
  'CFT-2025-0001', N'Arabica', N'Coffea Arabica',
  N'Cà phê Arabica có vị chua thanh, hương thơm đặc trưng và hàm lượng caffeine thấp hơn Robusta.',
  N'M''Đrắk, Krông Bông – Đắk Lắk (vùng cao)',
  N'Specialty', 1200 -- Kg/ha
);

-- Robusta (chủ lực tại Đắk Lắk)
INSERT INTO CoffeeTypes (TypeCode, TypeName, BotanicalName, Description, TypicalRegion, SpecialtyLevel, DefaultYieldPerHectare)
VALUES (
  'CFT-2025-0002', N'Robusta', N'Coffea Canephora',
  N'Robusta có vị đậm đắng, hậu vị mạnh, hàm lượng caffeine cao, phù hợp espresso hoặc cà phê truyền thống Việt.',
  N'Buôn Ma Thuột, Krông Pắc, Ea Kar – Đắk Lắk',
  N'Fine Robusta', 2700
);

-- Culi (Robusta dạng đột biến, phổ biến tại Đắk Lắk)
INSERT INTO CoffeeTypes (TypeCode, TypeName, BotanicalName, Description, TypicalRegion, SpecialtyLevel, DefaultYieldPerHectare)
VALUES (
  'CFT-2025-0003', N'Culi', NULL,
  N'Hạt cà phê tròn đều do đột biến tự nhiên, thường mạnh và đậm hơn Arabica và Robusta thông thường.',
  N'Đắk Lắk',
  N'Premium', 2300
);

-- Robusta Honey (phương pháp sơ chế đặc biệt)
INSERT INTO CoffeeTypes (TypeCode, TypeName, BotanicalName, Description, TypicalRegion, SpecialtyLevel, DefaultYieldPerHectare)
VALUES (
  'CFT-2025-0004', N'Robusta Honey', N'Coffea Canephora (Honey Processed)',
  N'Cà phê Robusta được sơ chế theo phương pháp honey để giữ lại vị ngọt và hương trái cây tự nhiên.',
  N'Ea H’leo, Cư M’gar – Đắk Lắk',
  N'Fine Robusta', 2500
);

-- Robusta Natural (phơi nguyên trái)
INSERT INTO CoffeeTypes (TypeCode, TypeName, BotanicalName, Description, TypicalRegion, SpecialtyLevel, DefaultYieldPerHectare)
VALUES (
  'CFT-2025-0005', N'Robusta Natural', N'Coffea Canephora (Natural Processed)',
  N'Sơ chế tự nhiên (phơi nguyên trái), giữ được vị ngọt hậu, phù hợp thị trường rang xay cao cấp.',
  N'Ea H’leo, Krông Năng – Đắk Lắk',
  N'Fine Robusta', 2600
);

-- Typica (giống Arabica nguyên thủy, đang thử nghiệm tại Đắk Lắk)
INSERT INTO CoffeeTypes (TypeCode, TypeName, BotanicalName, Description, TypicalRegion, SpecialtyLevel, DefaultYieldPerHectare)
VALUES (
  'CFT-2025-0006', N'Typica', N'Coffea Arabica Typica',
  N'Một trong những giống cà phê Arabica nguyên thủy, đang được thử nghiệm tại vùng cao Krông Bông.',
  N'Krông Bông – Đắk Lắk',
  N'Specialty', 1000
);

-- Robusta Washed (sơ chế ướt, phổ biến cho xuất khẩu cao cấp)
INSERT INTO CoffeeTypes (TypeCode, TypeName, BotanicalName, Description, TypicalRegion, SpecialtyLevel, DefaultYieldPerHectare)
VALUES (
  'CFT-2025-0007', N'Robusta Washed', N'Coffea Canephora (Washed Processed)',
  N'Cà phê Robusta được sơ chế ướt giúp vị trong trẻo hơn, hậu vị sạch, được ưa chuộng bởi thị trường châu Âu.',
  N'Krông Pắc, Buôn Ma Thuột – Đắk Lắk',
  N'Fine Robusta', 2550
);

-- Robusta TR9 (giống chọn lọc năng suất cao tại Đắk Lắk)
INSERT INTO CoffeeTypes (TypeCode, TypeName, BotanicalName, Description, TypicalRegion, SpecialtyLevel, DefaultYieldPerHectare)
VALUES (
  'CFT-2025-0008', N'Robusta TR9', N'Coffea Canephora TR9',
  N'Giống Robusta cao sản được Viện Eakmat chọn tạo, phù hợp điều kiện Đắk Lắk, cho năng suất vượt trội.',
  N'Ea Kar, Krông Ana – Đắk Lắk',
  N'Standard', 3200
);

GO

-- Insert bảng Contracts
-- Giả định SellerID (BusinessManagerID) và BuyerID đã có
DECLARE @SellerID UNIQUEIDENTIFIER = (
   SELECT ManagerID FROM BusinessManagers WHERE ManagerCode = 'BM-2025-0001'
);

DECLARE @BuyerID UNIQUEIDENTIFIER = (
   SELECT BuyerID FROM BusinessBuyers WHERE BuyerCode = 'BM-2025-0001-BUY-2025-001'
);

DECLARE @Buyer2 UNIQUEIDENTIFIER = (
   SELECT BuyerID FROM BusinessBuyers WHERE BuyerCode = 'BM-2025-0001-BUY-2025-002'
);

DECLARE @Buyer3 UNIQUEIDENTIFIER = (
   SELECT BuyerID FROM BusinessBuyers WHERE BuyerCode = 'BM-2025-0001-BUY-2025-003'
);

DECLARE @Buyer4 UNIQUEIDENTIFIER = (
   SELECT BuyerID FROM BusinessBuyers WHERE BuyerCode = 'BM-2025-0001-BUY-2025-004'
);

-- Tạo hợp đồng
DECLARE @ContractID UNIQUEIDENTIFIER = NEWID();

INSERT INTO Contracts (
    ContractID, ContractCode, SellerID, BuyerID, ContractNumber, ContractTitle,
    DeliveryRounds, TotalQuantity, TotalValue, StartDate, EndDate,
    SignedAt, Status, CreatedAt, UpdatedAt
) VALUES (
    @ContractID, 'CTR-2025-0001', @SellerID, @BuyerID, N'HĐ-2025-001-VINCAFE',
    N'Hợp đồng cung ứng 100 tấn cà phê Đắk Lắk trong 3 năm',
    4, 100000, 90000000000,
    '2025-06-15', '2028-06-15', '2025-06-10 14:00:00', N'InProgress',
    GETDATE(), GETDATE()
);

INSERT INTO Contracts (ContractID, ContractCode, SellerID, BuyerID, ContractNumber, ContractTitle, DeliveryRounds, TotalQuantity, TotalValue, StartDate, EndDate, SignedAt, Status, CreatedAt, UpdatedAt)
VALUES 
(NEWID(), 'CTR-2025-0002', @SellerID, @Buyer2, N'HĐ-2025-002-COFFEELAND', N'Cung cấp 50 tấn Robusta Washed', 2, 50000, 28000000000, '2025-07-01', '2026-07-01', '2025-06-20', N'InProgress', GETDATE(), GETDATE()),
(NEWID(), 'CTR-2025-0003', @SellerID, @Buyer3, N'HĐ-2025-003-BAOAN',      N'Cung cấp 30 tấn Arabica & Culi', 3, 30000, 20000000000, '2025-07-10', '2026-01-10', '2025-06-25', N'InProgress', GETDATE(), GETDATE()),
(NEWID(), 'CTR-2025-0004', @SellerID, @Buyer4, N'HĐ-2025-004-VNCOFFEE',   N'Cung cấp cà phê đặc sản phối trộn', 1, 20000, 16000000000, '2025-07-15', '2025-12-31', '2025-07-01', N'InProgress', GETDATE(), GETDATE());

GO

-- Insert bảng ContractItems
-- Lấy ContractIDs
DECLARE @ContractID UNIQUEIDENTIFIER = (
    SELECT ContractID FROM Contracts WHERE ContractCode = 'CTR-2025-0001'
);

DECLARE @CID2 UNIQUEIDENTIFIER = (
   SELECT ContractID FROM Contracts WHERE ContractCode = 'CTR-2025-0002'
);

DECLARE @CID3 UNIQUEIDENTIFIER = (
   SELECT ContractID FROM Contracts WHERE ContractCode = 'CTR-2025-0003'
);

DECLARE @CID4 UNIQUEIDENTIFIER = (
   SELECT ContractID FROM Contracts WHERE ContractCode = 'CTR-2025-0004'
);

-- Lấy CoffeeTypeIDs
-- Arabica
DECLARE @ArabicaID UNIQUEIDENTIFIER = (
    SELECT CoffeeTypeID FROM CoffeeTypes WHERE TypeName = N'Arabica'
);

-- Robusta
DECLARE @RobustaID UNIQUEIDENTIFIER = (
    SELECT CoffeeTypeID FROM CoffeeTypes WHERE TypeName = N'Robusta'
);

-- Robusta Honey
DECLARE @HoneyID UNIQUEIDENTIFIER = (
    SELECT CoffeeTypeID FROM CoffeeTypes WHERE TypeName = N'Robusta Honey'
);

-- Robusta Washed
DECLARE @WashedID UNIQUEIDENTIFIER = (
    SELECT CoffeeTypeID FROM CoffeeTypes WHERE TypeName = N'Robusta Washed'
);

-- Robusta Natural
DECLARE @NaturalID UNIQUEIDENTIFIER = (
    SELECT CoffeeTypeID FROM CoffeeTypes WHERE TypeName = N'Robusta Natural'
);

-- Culi
DECLARE @CuliID UNIQUEIDENTIFIER = (
    SELECT CoffeeTypeID FROM CoffeeTypes WHERE TypeName = N'Culi'
);

-- Typica
DECLARE @TypicaID UNIQUEIDENTIFIER = (
    SELECT CoffeeTypeID FROM CoffeeTypes WHERE TypeName = N'Typica'
);

-- Robusta TR9
DECLARE @TR9ID UNIQUEIDENTIFIER = (
   SELECT CoffeeTypeID FROM CoffeeTypes WHERE TypeName = N'Robusta TR9'
);

-- Thêm dòng hợp đồng: Arabica 20.000 Kg
INSERT INTO ContractItems (
    ContractItemCode, ContractID, CoffeeTypeID, Quantity, UnitPrice,
    DiscountAmount, Note, CreatedAt, UpdatedAt
) VALUES (
    'CTI-001-CTR-2025-0001', @ContractID, @ArabicaID, 20000, 65000, 0,
    N'Cà phê Arabica chất lượng cao', GETDATE(), GETDATE()
);

-- Thêm dòng hợp đồng: Robusta 50.000 Kg
INSERT INTO ContractItems (
    ContractItemCode, ContractID, CoffeeTypeID, Quantity, UnitPrice,
    DiscountAmount, Note, CreatedAt, UpdatedAt
) VALUES (
    'CTI-002-CTR-2025-0001', @ContractID, @RobustaID, 50000, 50000, 0,
    N'Cà phê Robusta xuất khẩu', GETDATE(), GETDATE()
);

-- Contract 1
INSERT INTO ContractItems (
   ContractItemCode, ContractID, CoffeeTypeID, Quantity, UnitPrice, DiscountAmount, Note, CreatedAt, UpdatedAt
)
VALUES 
('CTI-003-CTR-2025-0001', @ContractID, @HoneyID,   10000, 57000, 0, N'Robusta xử lý mật ong (Honey)',        GETDATE(), GETDATE()),
('CTI-004-CTR-2025-0001', @ContractID, @WashedID,  8000,  56000, 0, N'Robusta sơ chế ướt (Washed)',          GETDATE(), GETDATE()),
('CTI-005-CTR-2025-0001', @ContractID, @CuliID,    5000,  60000, 0, N'Cà phê Culi đậm vị, đột biến tự nhiên', GETDATE(), GETDATE()),
('CTI-006-CTR-2025-0001', @ContractID, @NaturalID, 5000,  53000, 0, N'Robusta sơ chế tự nhiên (Natural)',     GETDATE(), GETDATE()),
('CTI-007-CTR-2025-0001', @ContractID, @TypicaID,  2000,  68000, 0, N'Giống Arabica Typica quý hiếm',         GETDATE(), GETDATE());

-- Contract 2
INSERT INTO ContractItems (ContractItemCode, ContractID, CoffeeTypeID, Quantity, UnitPrice, DiscountAmount, Note, CreatedAt, UpdatedAt)
VALUES 
('CTI-001-CTR-2025-0002', @CID2, @WashedID, 30000, 56000, 0, N'Lô 1 Robusta Washed', GETDATE(), GETDATE()),
('CTI-002-CTR-2025-0002', @CID2, @RobustaID, 20000, 50000, 0, N'Lô 2 Robusta thường', GETDATE(), GETDATE());

-- Contract 3: Arabica & Culi
INSERT INTO ContractItems (ContractItemCode, ContractID, CoffeeTypeID, Quantity, UnitPrice, DiscountAmount, Note, CreatedAt, UpdatedAt)
VALUES 
('CTI-001-CTR-2025-0003', @CID3, @ArabicaID, 15000, 65000, 0, N'Arabica nguyên chất', GETDATE(), GETDATE()),
('CTI-002-CTR-2025-0003', @CID3, @CuliID,    15000, 60000, 0, N'Culi đậm vị',         GETDATE(), GETDATE());

-- Contract 4: Phối trộn
INSERT INTO ContractItems (ContractItemCode, ContractID, CoffeeTypeID, Quantity, UnitPrice, DiscountAmount, Note, CreatedAt, UpdatedAt)
VALUES 
('CTI-001-CTR-2025-0004', @CID4, @TypicaID,  5000,  68000, 0, N'Typica quý hiếm',        GETDATE(), GETDATE()),
('CTI-002-CTR-2025-0004', @CID4, @TR9ID,     7000,  52000, 0, N'Robusta TR9 năng suất cao', GETDATE(), GETDATE()),
('CTI-003-CTR-2025-0004', @CID4, @HoneyID,   8000,  57000, 0, N'Robusta sơ chế mật ong',  GETDATE(), GETDATE());

GO

-- Insert ContractDeliveryBatches và ContractDeliveryItems
-- Đợt 1 – Giao 30,000 Kg (Arabica, Robusta, Honey)
DECLARE @ContractID UNIQUEIDENTIFIER = (
   SELECT ContractID FROM Contracts WHERE ContractCode = 'CTR-2025-0001'
);

DECLARE @Batch1 UNIQUEIDENTIFIER = NEWID();

INSERT INTO ContractDeliveryBatches (
    DeliveryBatchID, DeliveryBatchCode, ContractID, DeliveryRound,
    ExpectedDeliveryDate, TotalPlannedQuantity, Status, CreatedAt, UpdatedAt
) VALUES (
    @Batch1, 'DELB-2025-0001', @ContractID, 1, '2025-07-01', 30000, 'InProgress', GETDATE(), GETDATE()
);

DECLARE @CTI_Arabica UNIQUEIDENTIFIER = (
   SELECT ContractItemID FROM ContractItems WHERE ContractItemCode = 'CTI-001-CTR-2025-0001'
);

DECLARE @CTI_Robusta UNIQUEIDENTIFIER = (
   SELECT ContractItemID FROM ContractItems WHERE ContractItemCode = 'CTI-002-CTR-2025-0001'
);

DECLARE @CTI_Honey UNIQUEIDENTIFIER = (
   SELECT ContractItemID FROM ContractItems WHERE ContractItemCode = 'CTI-003-CTR-2025-0001'
);

INSERT INTO ContractDeliveryItems (
    DeliveryItemID, DeliveryItemCode, DeliveryBatchID, ContractItemID,
    PlannedQuantity, FulfilledQuantity, Note, CreatedAt, UpdatedAt
) VALUES 
(NEWID(), 'DLI-2025-0001', @Batch1, @CTI_Arabica, 5000, 0, N'Arabica đợt 1', GETDATE(), GETDATE()),
(NEWID(), 'DLI-2025-0002', @Batch1, @CTI_Robusta, 20000, 0, N'Robusta đợt 1', GETDATE(), GETDATE()),
(NEWID(), 'DLI-2025-0003', @Batch1, @CTI_Honey,   5000, 0, N'Honey đợt 1', GETDATE(), GETDATE());

-- Đợt 2 – Giao 20,000 Kg (Robusta Washed, Culi)
DECLARE @Batch2 UNIQUEIDENTIFIER = NEWID();

INSERT INTO ContractDeliveryBatches (
    DeliveryBatchID, DeliveryBatchCode, ContractID, DeliveryRound,
    ExpectedDeliveryDate, TotalPlannedQuantity, Status, CreatedAt, UpdatedAt
) VALUES (
    @Batch2, 'DELB-2025-0002', @ContractID, 2, '2025-10-01', 20000, 'InProgress', GETDATE(), GETDATE()
);

DECLARE @CTI_Washed UNIQUEIDENTIFIER = (
   SELECT ContractItemID FROM ContractItems WHERE ContractItemCode = 'CTI-004-CTR-2025-0001'
);

DECLARE @CTI_Culi UNIQUEIDENTIFIER = (
   SELECT ContractItemID FROM ContractItems WHERE ContractItemCode = 'CTI-005-CTR-2025-0001'
);

INSERT INTO ContractDeliveryItems (
    DeliveryItemID, DeliveryItemCode, DeliveryBatchID, ContractItemID,
    PlannedQuantity, FulfilledQuantity, Note, CreatedAt, UpdatedAt
) VALUES 
(NEWID(), 'DLI-2025-0004', @Batch2, @CTI_Washed, 12000, 0, N'Robusta Washed đợt 2', GETDATE(), GETDATE()),
(NEWID(), 'DLI-2025-0005', @Batch2, @CTI_Culi,   8000,  0, N'Culi đợt 2', GETDATE(), GETDATE());

-- Đợt 3 – Giao 25,000 Kg (Robusta Natural, Arabica, Robusta)
DECLARE @Batch3 UNIQUEIDENTIFIER = NEWID();

INSERT INTO ContractDeliveryBatches (
    DeliveryBatchID, DeliveryBatchCode, ContractID, DeliveryRound,
    ExpectedDeliveryDate, TotalPlannedQuantity, Status, CreatedAt, UpdatedAt
) VALUES (
    @Batch3, 'DELB-2025-0003', @ContractID, 3, '2026-01-01', 25000, 'InProgress', GETDATE(), GETDATE()
);

DECLARE @CTI_Natural UNIQUEIDENTIFIER = (
   SELECT ContractItemID FROM ContractItems WHERE ContractItemCode = 'CTI-006-CTR-2025-0001'
);

INSERT INTO ContractDeliveryItems (
    DeliveryItemID, DeliveryItemCode, DeliveryBatchID, ContractItemID,
    PlannedQuantity, FulfilledQuantity, Note, CreatedAt, UpdatedAt
) VALUES 
(NEWID(), 'DLI-2025-0006', @Batch3, @CTI_Natural, 10000, 0, N'Robusta Natural đợt 3', GETDATE(), GETDATE()),
(NEWID(), 'DLI-2025-0007', @Batch3, @CTI_Arabica, 5000,  0, N'Arabica đợt 3', GETDATE(), GETDATE()),
(NEWID(), 'DLI-2025-0008', @Batch3, @CTI_Robusta, 10000, 0, N'Robusta đợt 3', GETDATE(), GETDATE());

-- Đợt 4 – Giao 25,000 Kg (Typica, Robusta, Robusta Washed)
DECLARE @Batch4 UNIQUEIDENTIFIER = NEWID();

INSERT INTO ContractDeliveryBatches (
    DeliveryBatchID, DeliveryBatchCode, ContractID, DeliveryRound,
    ExpectedDeliveryDate, TotalPlannedQuantity, Status, CreatedAt, UpdatedAt
) VALUES (
    @Batch4, 'DELB-2025-0004', @ContractID, 4, '2026-04-01', 25000, 'InProgress', GETDATE(), GETDATE()
);

DECLARE @CTI_Typica UNIQUEIDENTIFIER = (
   SELECT ContractItemID FROM ContractItems WHERE ContractItemCode = 'CTI-007-CTR-2025-0001'
);

INSERT INTO ContractDeliveryItems (
    DeliveryItemID, DeliveryItemCode, DeliveryBatchID, ContractItemID,
    PlannedQuantity, FulfilledQuantity, Note, CreatedAt, UpdatedAt
) VALUES 
(NEWID(), 'DLI-2025-0009', @Batch4, @CTI_Typica, 2000,  0, N'Typica đợt 4', GETDATE(), GETDATE()),
(NEWID(), 'DLI-2025-0010', @Batch4, @CTI_Robusta, 18000, 0, N'Robusta đợt 4', GETDATE(), GETDATE()),
(NEWID(), 'DLI-2025-0011', @Batch4, @CTI_Washed, 5000,  0, N'Robusta Washed đợt 4', GETDATE(), GETDATE());

GO

-- Insert ContractDeliveryBatches và ContractDeliveryItems
DECLARE @CID2 UNIQUEIDENTIFIER = (
   SELECT ContractID FROM Contracts WHERE ContractCode = 'CTR-2025-0002'
);

DECLARE @CID3 UNIQUEIDENTIFIER = (
   SELECT ContractID FROM Contracts WHERE ContractCode = 'CTR-2025-0003'
);

DECLARE @CID4 UNIQUEIDENTIFIER = (
   SELECT ContractID FROM Contracts WHERE ContractCode = 'CTR-2025-0004'
);

-- Contract 2: CTR-2025-0002
-- Đợt 1 - Giao 25.000 Kg (Robusta Washed)
DECLARE @Batch_C2_D1 UNIQUEIDENTIFIER = NEWID();

INSERT INTO ContractDeliveryBatches (
    DeliveryBatchID, DeliveryBatchCode, ContractID, DeliveryRound,
    ExpectedDeliveryDate, TotalPlannedQuantity, Status,
    CreatedAt, UpdatedAt, IsDeleted
) VALUES (
    @Batch_C2_D1, 'DELB-2025-0005', @CID2, 1,
    '2025-07-15', 25000, 'InProgress',
    GETDATE(), GETDATE(), 0
);

DECLARE @CTI_C2_Washed UNIQUEIDENTIFIER = (
    SELECT ContractItemID FROM ContractItems WHERE ContractItemCode = 'CTI-001-CTR-2025-0002'
);

INSERT INTO ContractDeliveryItems (
    DeliveryItemID, DeliveryItemCode, DeliveryBatchID, ContractItemID,
    PlannedQuantity, FulfilledQuantity, Note,
    CreatedAt, UpdatedAt, IsDeleted
) VALUES (
    NEWID(), 'DLI-2025-0012', @Batch_C2_D1, @CTI_C2_Washed,
    25000, 0, N'Robusta Washed đợt 1',
    GETDATE(), GETDATE(), 0
);

-- Đợt 2 - Giao 25.000 Kg (Robusta thường)
DECLARE @Batch_C2_D2 UNIQUEIDENTIFIER = NEWID();

INSERT INTO ContractDeliveryBatches (
    DeliveryBatchID, DeliveryBatchCode, ContractID, DeliveryRound,
    ExpectedDeliveryDate, TotalPlannedQuantity, Status,
    CreatedAt, UpdatedAt, IsDeleted
) VALUES (
    @Batch_C2_D2, 'DELB-2025-0006', @CID2, 2,
    '2026-03-01', 25000, 'InProgress',
    GETDATE(), GETDATE(), 0
);

DECLARE @CTI_C2_Robusta UNIQUEIDENTIFIER = (
    SELECT ContractItemID FROM ContractItems WHERE ContractItemCode = 'CTI-002-CTR-2025-0002'
);

INSERT INTO ContractDeliveryItems (
    DeliveryItemID, DeliveryItemCode, DeliveryBatchID, ContractItemID,
    PlannedQuantity, FulfilledQuantity, Note,
    CreatedAt, UpdatedAt, IsDeleted
) VALUES (
    NEWID(), 'DLI-2025-0013', @Batch_C2_D2, @CTI_C2_Robusta,
    25000, 0, N'Robusta thường đợt 2',
    GETDATE(), GETDATE(), 0
);

-- Contract 3: CTR-2025-0003
-- Đợt 1 - Giao 10000 Kg (Arabica)
DECLARE @Batch_C3_D1 UNIQUEIDENTIFIER = NEWID();

INSERT INTO ContractDeliveryBatches VALUES (
    @Batch_C3_D1, 'DELB-2025-0007', @CID3, 1, '2025-08-01', 10000, 'InProgress', GETDATE(), GETDATE(), 0
);

DECLARE @CTI_C3_Arabica UNIQUEIDENTIFIER = (
    SELECT ContractItemID FROM ContractItems WHERE ContractItemCode = 'CTI-001-CTR-2025-0003'
);

INSERT INTO ContractDeliveryItems VALUES (
    NEWID(), 'DLI-2025-0014', @Batch_C3_D1, @CTI_C3_Arabica, 10000, 0, N'Arabica đợt 1', GETDATE(), GETDATE(), 0
);

-- Đợt 2 - Giao 10000 Kg (Culi)
DECLARE @Batch_C3_D2 UNIQUEIDENTIFIER = NEWID();

INSERT INTO ContractDeliveryBatches VALUES (
    @Batch_C3_D2, 'DELB-2025-0008', @CID3, 2, '2025-10-15', 10000, 'InProgress', GETDATE(), GETDATE(), 0
);

DECLARE @CTI_C3_Culi UNIQUEIDENTIFIER = (
    SELECT ContractItemID FROM ContractItems WHERE ContractItemCode = 'CTI-002-CTR-2025-0003'
);

INSERT INTO ContractDeliveryItems VALUES (
    NEWID(), 'DLI-2025-0015', @Batch_C3_D2, @CTI_C3_Culi, 10000, 0, N'Culi đợt 2', GETDATE(), GETDATE(), 0
);

-- Đợt 3 - Giao 10000 Kg (Arabica + Culi phối trộn)
DECLARE @Batch_C3_D3 UNIQUEIDENTIFIER = NEWID();

INSERT INTO ContractDeliveryBatches VALUES (
    @Batch_C3_D3, 'DELB-2025-0009', @CID3, 3, '2026-01-05', 10000, 'InProgress', GETDATE(), GETDATE(), 0
);

INSERT INTO ContractDeliveryItems VALUES 
(NEWID(), 'DLI-2025-0016', @Batch_C3_D3, @CTI_C3_Arabica, 5000, 0, N'Arabica đợt 3', GETDATE(), GETDATE(), 0),
(NEWID(), 'DLI-2025-0017', @Batch_C3_D3, @CTI_C3_Culi,    5000, 0, N'Culi đợt 3', GETDATE(), GETDATE(), 0);

-- Contract 4: CTR-2025-0004
-- Đợt 1 - Giao toàn bộ 20000 Kg
DECLARE @Batch_C4_D1 UNIQUEIDENTIFIER = NEWID();

INSERT INTO ContractDeliveryBatches VALUES (
    @Batch_C4_D1, 'DELB-2025-0010', @CID4, 1, '2025-08-20', 20000, 'InProgress', GETDATE(), GETDATE(), 0
);

DECLARE @CTI_C4_Typica UNIQUEIDENTIFIER = (
    SELECT ContractItemID FROM ContractItems WHERE ContractItemCode = 'CTI-001-CTR-2025-0004'
);

DECLARE @CTI_C4_TR9 UNIQUEIDENTIFIER = (
    SELECT ContractItemID FROM ContractItems WHERE ContractItemCode = 'CTI-002-CTR-2025-0004'
);

DECLARE @CTI_C4_Honey UNIQUEIDENTIFIER = (
    SELECT ContractItemID FROM ContractItems WHERE ContractItemCode = 'CTI-003-CTR-2025-0004'
);

INSERT INTO ContractDeliveryItems VALUES 
(NEWID(), 'DLI-2025-0018', @Batch_C4_D1, @CTI_C4_Typica, 5000, 0, N'Typica đợt 1', GETDATE(), GETDATE(), 0),
(NEWID(), 'DLI-2025-0019', @Batch_C4_D1, @CTI_C4_TR9,    7000, 0, N'Robusta TR9 đợt 1', GETDATE(), GETDATE(), 0),
(NEWID(), 'DLI-2025-0020', @Batch_C4_D1, @CTI_C4_Honey,  8000, 0, N'Robusta Honey đợt 1', GETDATE(), GETDATE(), 0);

GO

-- Insert vào bảng ProcessingMethods
INSERT INTO ProcessingMethods (MethodCode, Name, Description)
VALUES 
-- Phơi tự nhiên (natural/dry)
('natural', N'Sơ chế khô (Natural)', N'Cà phê được phơi nguyên trái dưới ánh nắng mặt trời. Giữ được độ ngọt và hương trái cây.'),

-- Sơ chế ướt (washed)
('washed', N'Sơ chế ướt (Washed)', N'Loại bỏ lớp thịt quả trước khi lên men và rửa sạch. Cho vị sạch, hậu vị trong trẻo.'),

-- Sơ chế mật ong (honey)
('honey', N'Sơ chế mật ong (Honey)', N'Giữ lại một phần lớp nhớt trên hạt trong quá trình phơi. Tạo vị ngọt và hương đặc trưng.'),

-- Semi-washed (bán ướt)
('semi-washed', N'Semi-washed (Bán ướt)', N'Kết hợp giữa sơ chế khô và ướt. Giảm chi phí nhưng vẫn giữ chất lượng.'),

-- Carbonic maceration (hiếm)
('carbonic', N'Lên men yếm khí (Carbonic Maceration)', N'Kỹ thuật lên men nguyên trái trong môi trường CO2, cho hương độc đáo và hậu vị phức tạp.');

GO

-- Insert vào bảng ProcurementPlans và ProcurementPlansDetails
-- Lấy ManagerID (nếu chưa có)
DECLARE @BMID UNIQUEIDENTIFIER = (
  SELECT ManagerID 
  FROM BusinessManagers 
  WHERE UserID = (SELECT UserID FROM UserAccounts WHERE Email = 'businessmanager@gmail.com')
);

-- Tạo kế hoạch thu mua lần 1
DECLARE @PlanID UNIQUEIDENTIFIER = NEWID();

INSERT INTO ProcurementPlans (
  PlanID, PlanCode, Title, Description, TotalQuantity, CreatedBy, StartDate, EndDate, Status, ProgressPercentage
)
VALUES (
  @PlanID, 'PLAN-2025-0003', 
  N'Kế hoạch thu mua cà phê cho đợt giao lần 1 của hợp đồng CTR-2025-0001',
  N'Gồm Arabica, Robusta và Robusta Honey, phục vụ giao hàng lần đầu cho VinCafé theo hợp đồng B2B.',
  25000, @BMID, '2025-06-10', '2025-06-25', 'open', 0
);

-- Lấy CoffeeTypeID
DECLARE @CoffeeID_Arabica UNIQUEIDENTIFIER = (
  SELECT CoffeeTypeID FROM CoffeeTypes WHERE TypeName = N'Arabica'
);

DECLARE @CoffeeID_Robusta UNIQUEIDENTIFIER = (
  SELECT CoffeeTypeID FROM CoffeeTypes WHERE TypeName = N'Robusta'
);

DECLARE @CoffeeID_Honey UNIQUEIDENTIFIER   = (
  SELECT CoffeeTypeID FROM CoffeeTypes WHERE TypeName = N'Robusta Honey'
);

-- Arabica
DECLARE @CTI_Arabica UNIQUEIDENTIFIER = (
  SELECT ContractItemID FROM ContractItems WHERE ContractItemCode = 'CTI-2025-0001'
);

-- Robusta
DECLARE @CTI_Robusta UNIQUEIDENTIFIER = (
  SELECT ContractItemID FROM ContractItems WHERE ContractItemCode = 'CTI-2025-0002'
);

-- Robusta Honey
DECLARE @CTI_Honey UNIQUEIDENTIFIER = (
  SELECT ContractItemID FROM ContractItems WHERE ContractItemCode = 'CTI-2025-0003'
);

DECLARE @Natural INT = (
   SELECT MethodID FROM ProcessingMethods WHERE MethodCode = 'natural'
);

DECLARE @Washed INT = (
   SELECT MethodID FROM ProcessingMethods WHERE MethodCode = 'washed'
);

DECLARE @Honey INT = (
   SELECT MethodID FROM ProcessingMethods WHERE MethodCode = 'honey'
);

DECLARE @SemiWashed INT = (
   SELECT MethodID FROM ProcessingMethods WHERE MethodCode = 'semi-washed'
);

DECLARE @Carbonic INT = (
   SELECT MethodID FROM ProcessingMethods WHERE MethodCode = 'carbonic'
);

-- Chi tiết: Arabica 5,000 Kg
INSERT INTO ProcurementPlansDetails (
  PlanDetailCode, PlanID, CoffeeTypeID, ProcessMethodID, TargetQuantity, TargetRegion,
  MinimumRegistrationQuantity, MinPriceRange, MaxPriceRange, Note, 
  ContractItemID, ProgressPercentage, ExpectedYieldPerHectare
)
VALUES (
  'PLD-2025-0001', @PlanID, @CoffeeID_Arabica, @Washed, 5000, N'Krông Bông',
  100, 75, 95, N'Phục vụ hợp đồng CTR-2025-0001 – đợt giao 1', 
  @CTI_Arabica, 0, 1100
);

-- Chi tiết: Robusta 12,500 Kg
INSERT INTO ProcurementPlansDetails (
  PlanDetailCode, PlanID, CoffeeTypeID, ProcessMethodID, TargetQuantity, TargetRegion,
  MinimumRegistrationQuantity, MinPriceRange, MaxPriceRange, Note, ExpectedYieldPerHectare
)
VALUES (
  'PLD-2025-0002', @PlanID, @CoffeeID_Robusta, @Natural, 12500, N'Ea Kar',
  150, 50, 65, N'Robusta thông thường – giao lần 1', 2500
);


-- Chi tiết: Robusta Honey 2,500 Kg
INSERT INTO ProcurementPlansDetails (
  PlanDetailCode, PlanID, CoffeeTypeID, ProcessMethodID, TargetQuantity, TargetRegion,
  MinimumRegistrationQuantity, MinPriceRange, MaxPriceRange, Note, ExpectedYieldPerHectare
)
VALUES (
  'PLD-2025-0003', @PlanID, @CoffeeID_Honey, @Honey, 2500, N'Cư M’gar',
  100, 60, 75, N'Robusta sơ chế Honey đợt giao đầu tiên', 2450
);

GO

-- Insert vào bảng ProcurementPlans
-- Lấy ManagerID
DECLARE @BMID UNIQUEIDENTIFIER = (
  SELECT ManagerID FROM BusinessManagers 
  WHERE UserID = (SELECT UserID FROM UserAccounts WHERE Email = 'businessmanager@gmail.com')
);

-- Kế hoạch 1: Thu mua Arabica & Typica
DECLARE @PlanID1 UNIQUEIDENTIFIER = NEWID();

INSERT INTO ProcurementPlans (
  PlanID, PlanCode, Title, Description, TotalQuantity, CreatedBy, StartDate, EndDate, Status, ProgressPercentage
)
VALUES (
  @PlanID1, 'PLAN-2025-0001', N'Thu mua cà phê Arabica chất lượng cao mùa vụ 2025',
  N'Kế hoạch thu mua Arabica và Typica từ vùng cao Krông Bông, yêu cầu chất lượng đạt chuẩn Specialty.',
  6000, @BMID, '2025-06-07', '2025-06-21', 'open', 36.67
);

-- Kế hoạch 2: Thu mua Robusta Honey & TR9
DECLARE @PlanID2 UNIQUEIDENTIFIER = NEWID();

INSERT INTO ProcurementPlans (
  PlanID, PlanCode, Title, Description, TotalQuantity, CreatedBy, StartDate, EndDate, Status
)
VALUES (
  @PlanID2, 'PLAN-2025-0002', N'Thu mua Robusta sơ chế đặc biệt khu vực Ea H’leo',
  N'Ưu tiên Robusta Honey và Robusta TR9, phục vụ thị trường xuất khẩu châu Âu.',
  12000, @BMID, '2025-06-14', '2025-06-28', 'open'
);

GO

-- Insert vào bảng ProcurementPlansDetails
-- Lấy PlanID từ mã kế hoạch
DECLARE @PlanID1 UNIQUEIDENTIFIER = (
  SELECT PlanID FROM ProcurementPlans WHERE PlanCode = 'PLAN-2025-0001'
);

DECLARE @PlanID2 UNIQUEIDENTIFIER = (
  SELECT PlanID FROM ProcurementPlans WHERE PlanCode = 'PLAN-2025-0002'
);

-- Lấy CoffeeTypeID theo TypeCode mới
DECLARE @CoffeeID_Arabica UNIQUEIDENTIFIER = (
  SELECT CoffeeTypeID FROM CoffeeTypes WHERE TypeCode = 'CFT-2025-0001'
);

DECLARE @CoffeeID_Typica UNIQUEIDENTIFIER = (
  SELECT CoffeeTypeID FROM CoffeeTypes WHERE TypeCode = 'CFT-2025-0006'
);

DECLARE @CoffeeID_Honey UNIQUEIDENTIFIER = (
  SELECT CoffeeTypeID FROM CoffeeTypes WHERE TypeCode = 'CFT-2025-0004'
);

DECLARE @CoffeeID_TR9 UNIQUEIDENTIFIER = (
  SELECT CoffeeTypeID FROM CoffeeTypes WHERE TypeCode = 'CFT-2025-0008'
);

DECLARE @Natural INT = (
   SELECT MethodID FROM ProcessingMethods WHERE MethodCode = 'natural'
);

DECLARE @Washed INT = (
   SELECT MethodID FROM ProcessingMethods WHERE MethodCode = 'washed'
);

DECLARE @Honey INT = (
   SELECT MethodID FROM ProcessingMethods WHERE MethodCode = 'honey'
);

DECLARE @SemiWashed INT = (
   SELECT MethodID FROM ProcessingMethods WHERE MethodCode = 'semi-washed'
);

DECLARE @Carbonic INT = (
   SELECT MethodID FROM ProcessingMethods WHERE MethodCode = 'carbonic'
);

-- Chi tiết 1: Arabica
INSERT INTO ProcurementPlansDetails (
  PlanDetailCode, PlanID, CoffeeTypeID, ProcessMethodID, TargetQuantity, TargetRegion,
  MinimumRegistrationQuantity, MinPriceRange, MaxPriceRange, Note,
  ProgressPercentage, ExpectedYieldPerHectare
)
VALUES (
  'PLD-2025-0004', @PlanID1, @CoffeeID_Arabica, @Washed, 3000, N'Krông Bông',
  100, 80, 100, N'Thu mua dành cho thị trường specialty',
  73.33, 950
);

-- Chi tiết 2: Typica
INSERT INTO ProcurementPlansDetails (
  PlanDetailCode, PlanID, CoffeeTypeID, ProcessMethodID, TargetQuantity, TargetRegion,
  MinimumRegistrationQuantity, MinPriceRange, MaxPriceRange, Note,
  ProgressPercentage, ExpectedYieldPerHectare
)
VALUES (
  'PLD-2025-0005', @PlanID1, @CoffeeID_Typica, @SemiWashed, 3000, N'M''Đrắk',
  150, 90, 120, N'Sản phẩm trưng bày hội chợ cà phê 2025',
  0, 1000
);

-- Chi tiết 3: Robusta Honey
INSERT INTO ProcurementPlansDetails (
  PlanDetailCode, PlanID, CoffeeTypeID, ProcessMethodID, TargetQuantity, TargetRegion,
  MinimumRegistrationQuantity, MinPriceRange, MaxPriceRange, Note,
  ProgressPercentage, ExpectedYieldPerHectare
)
VALUES (
  'PLD-2025-0006', @PlanID2, @CoffeeID_Honey, @Honey, 7000, N'Cư M’gar',
  200, 60, 75, N'Yêu cầu sơ chế Honey tại chỗ, không vận chuyển trước khi phơi',
  0, 2450
);

-- Chi tiết 4: Robusta TR9
INSERT INTO ProcurementPlansDetails (
  PlanDetailCode, PlanID, CoffeeTypeID, ProcessMethodID, TargetQuantity, TargetRegion,
  MinimumRegistrationQuantity, MinPriceRange, MaxPriceRange, Note,
  ProgressPercentage, ExpectedYieldPerHectare
)
VALUES (
  'PLD-2025-0007', @PlanID2, @CoffeeID_TR9, @Natural, 5000, N'Ea Kar',
  300, 55, 68, N'Áp dụng tiêu chuẩn ISO 8451',
  0, 3100
);

GO

-- Insert ProcurementPlans & ProcurementPlansDetails (tự động từ hợp đồng)
-- Lấy ManagerID (Seller) để làm CreatedBy
DECLARE @BMID UNIQUEIDENTIFIER = (
  SELECT ManagerID
  FROM BusinessManagers
  WHERE UserID = (SELECT UserID FROM UserAccounts WHERE Email = 'businessmanager@gmail.com')
);

-- Cursor duyệt các hợp đồng
DECLARE contract_cursor CURSOR FOR
SELECT ContractID, ContractCode, ContractTitle, TotalQuantity
FROM Contracts
WHERE IsDeleted = 0;

DECLARE @ContractID UNIQUEIDENTIFIER;
DECLARE @ContractCode VARCHAR(50);
DECLARE @ContractTitle NVARCHAR(255);
DECLARE @ContractTotalQty FLOAT;
DECLARE @PlanID UNIQUEIDENTIFIER;
DECLARE @PlanCode VARCHAR(20);
DECLARE @PlanCounter INT = 1;

OPEN contract_cursor;

FETCH_NEXT:
FETCH NEXT FROM contract_cursor 
INTO @ContractID, @ContractCode, @ContractTitle, @ContractTotalQty;

IF @@FETCH_STATUS = 0
BEGIN
    -- Sinh mã kế hoạch không trùng
    WHILE 1 = 1
    BEGIN
        SET @PlanCode = 'PLAN-2025-' + RIGHT('000' + CAST(@PlanCounter AS VARCHAR), 4);

        IF NOT EXISTS (
            SELECT 1 FROM ProcurementPlans WHERE PlanCode = @PlanCode
        )
            BREAK;

        SET @PlanCounter += 1;
    END

    SET @PlanID = NEWID();

    INSERT INTO ProcurementPlans (
        PlanID, PlanCode, Title, Description, TotalQuantity, CreatedBy, StartDate, EndDate, Status, ProgressPercentage
    )
    VALUES (
        @PlanID, 
        @PlanCode,
        N'Kế hoạch thu mua: ' + @ContractTitle,
		N'Dựa trên hợp đồng ' + @ContractCode + N' – ' + @ContractTitle,
        @ContractTotalQty,
        @BMID,
        DATEADD(DAY, 1, GETDATE()),
        DATEADD(DAY, 15, GETDATE()),
        'open',
        0
    );

    -- Duyệt ContractItems
    DECLARE item_cursor CURSOR FOR
    SELECT ContractItemID, CoffeeTypeID, Quantity, Note
    FROM ContractItems
    WHERE ContractID = @ContractID;

    DECLARE @ItemID UNIQUEIDENTIFIER;
    DECLARE @CoffeeID UNIQUEIDENTIFIER;
    DECLARE @Quantity FLOAT;
    DECLARE @Note NVARCHAR(MAX);
    DECLARE @DetailCode VARCHAR(20);
    DECLARE @DetailCounter INT = 1;

    OPEN item_cursor;

    FETCH NEXT FROM item_cursor INTO @ItemID, @CoffeeID, @Quantity, @Note;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @DetailCode = 'PLD-' + REPLACE(@ContractCode, 'CTR-', '') + '-' + FORMAT(@DetailCounter, '000');

        DECLARE @MethodID INT = (
            SELECT TOP 1 MethodID
            FROM ProcessingMethods
            WHERE MethodCode IN (
                CASE 
                    WHEN @Note LIKE N'%Honey%' THEN 'honey'
                    WHEN @Note LIKE N'%Washed%' THEN 'washed'
                    WHEN @Note LIKE N'%Natural%' THEN 'natural'
                    ELSE 'natural'
                END
            )
        );

        DECLARE @Yield FLOAT = (
            SELECT TOP 1 DefaultYieldPerHectare 
            FROM CoffeeTypes 
            WHERE CoffeeTypeID = @CoffeeID
        );

        INSERT INTO ProcurementPlansDetails (
            PlanDetailCode, PlanID, CoffeeTypeID, ProcessMethodID,
            TargetQuantity, TargetRegion, MinimumRegistrationQuantity,
            MinPriceRange, MaxPriceRange, Note, ContractItemID, 
            ProgressPercentage, ExpectedYieldPerHectare
        )
        VALUES (
            @DetailCode, @PlanID, @CoffeeID, @MethodID,
            @Quantity, N'Đắk Lắk', 100,
            50, 70, @Note, @ItemID,
            0, @Yield
        );

        SET @DetailCounter += 1;
        FETCH NEXT FROM item_cursor INTO @ItemID, @CoffeeID, @Quantity, @Note;
    END

    CLOSE item_cursor;
    DEALLOCATE item_cursor;

    GOTO FETCH_NEXT
END

CLOSE contract_cursor;
DEALLOCATE contract_cursor;

GO

-- Insert vào bảng CultivationRegistrations
-- Lấy FarmerID & PlanID để đăng ký
DECLARE @FarmerID UNIQUEIDENTIFIER = (
   SELECT FarmerID FROM Farmers WHERE FarmerCode = 'FRM-2025-0001'
);

DECLARE @PlanID UNIQUEIDENTIFIER = (
   SELECT PlanID FROM ProcurementPlans WHERE PlanCode = 'PLAN-2025-0001'
);

-- Tạo đơn đăng ký
DECLARE @RegistrationID UNIQUEIDENTIFIER = NEWID();

INSERT INTO CultivationRegistrations (
    RegistrationID, RegistrationCode, PlanID, FarmerID, RegisteredArea, TotalWantedPrice, Note
)
VALUES (
    @RegistrationID, 'REG-2025-0001', @PlanID, @FarmerID, 1.8, 95, 
	N'Đăng ký trồng cà phê Arabica với kỹ thuật tưới nhỏ giọt'
);

GO

-- Insert vào bảng CultivationRegistrationsDetail
-- Lấy PlanDetailID của Arabica trong kế hoạch PLAN-2025-0001
DECLARE @RegistrationID UNIQUEIDENTIFIER = (
    SELECT RegistrationID FROM CultivationRegistrations WHERE RegistrationCode = 'REG-2025-0001'
);

DECLARE @PlanDetailID UNIQUEIDENTIFIER = (
    SELECT PlanDetailsID FROM ProcurementPlansDetails WHERE PlanDetailCode = 'PLD-2025-0001'
);

DECLARE @RegistrationDetailID UNIQUEIDENTIFIER = NEWID();

INSERT INTO CultivationRegistrationsDetail (
    CultivationRegistrationDetailID, RegistrationID, PlanDetailID, EstimatedYield,
    ExpectedHarvestStart, ExpectedHarvestEnd, 
	Note, WantedPrice
)
VALUES (
    @RegistrationDetailID, @RegistrationID, @PlanDetailID, 2200,
    '2025-11-01', '2026-01-15',
    N'Dự kiến sử dụng phân bón hữu cơ', 95
);

GO

-- Insert vào bảng CultivationRegistrations và CultivationRegistrationsDetail
/* =========================================================
   NHIỀU ĐƠN ĐĂNG KÝ CULTIVATION + CHI TIẾT (multi-farmers)
   - Không hardcode GUID
   - Theo style: DECLARE biến, lấy ID từ Code rồi INSERT
   - Tự sinh RegistrationCode nối tiếp theo năm hiện tại
   ========================================================= */

-- Helper: hàm sinh mã REG-YYYY-XXXX tiếp theo trong cùng transaction
DECLARE @REG_PREFIX VARCHAR(20) = 'REG-' + CONVERT(VARCHAR(4), YEAR(GETDATE())) + '-';
DECLARE @NextRegCode VARCHAR(20);

-- Hàm inline: trả về mã kế tiếp
-- (dùng như subquery mỗi lần cần phát sinh mã)
-- SELECT @NextRegCode = (
--   SELECT @REG_PREFIX + RIGHT('0000' + CAST(ISNULL(MAX(TRY_CAST(RIGHT(RegistrationCode,4) AS INT)),0) + 1 AS VARCHAR(10)), 4)
--   FROM CultivationRegistrations WITH (UPDLOCK, HOLDLOCK)
--   WHERE RegistrationCode LIKE @REG_PREFIX + '%'
--);

----------------------------------------------------------------
-- R1: Farmer FRM-2025-0001 đăng ký cho PLAN-2025-0001
----------------------------------------------------------------
BEGIN TRAN;

DECLARE @PlanID_1 UNIQUEIDENTIFIER = (
   SELECT PlanID FROM ProcurementPlans WHERE PlanCode='PLAN-2025-0001'
);

DECLARE @FarmerID_1 UNIQUEIDENTIFIER = (
   SELECT FarmerID FROM Farmers WHERE FarmerCode='FRM-2025-0001'
);

-- Lấy số tiếp theo (giữ lock để tránh trùng trong concurrent insert)
SELECT @NextRegCode = @REG_PREFIX
    + RIGHT('0000' + CAST(ISNULL(MAX(TRY_CAST(RIGHT(RegistrationCode,4) AS INT)),0) + 1 AS VARCHAR(10)),4)
FROM CultivationRegistrations WITH (UPDLOCK, HOLDLOCK)
WHERE RegistrationCode LIKE @REG_PREFIX + '%';

DECLARE @RegID_1 UNIQUEIDENTIFIER = NEWID();

INSERT INTO CultivationRegistrations
(RegistrationID, RegistrationCode, PlanID, FarmerID, RegisteredArea, RegisteredAt, TotalWantedPrice,
 Status, Note, SystemNote, IsDeleted, CreatedAt, UpdatedAt)
VALUES
(@RegID_1, @NextRegCode, @PlanID_1, @FarmerID_1, 1.8, GETDATE(), 95,
 N'Pending', N'Đăng ký Arabica, tưới nhỏ giọt', NULL, 0, GETDATE(), GETDATE());

-- Chi tiết R1.1: PLD-2025-A001 (1.2 ha, price 95)
DECLARE @PDID_R1A UNIQUEIDENTIFIER = (
   SELECT PlanDetailsID FROM ProcurementPlansDetails WHERE PlanDetailCode='PLD-2025-0001'
);

DECLARE @Min_R1A FLOAT, @Max_R1A FLOAT, @YieldHa_R1A FLOAT;

SELECT @Min_R1A=MinPriceRange, @Max_R1A=MaxPriceRange, @YieldHa_R1A=ExpectedYieldPerHectare
FROM ProcurementPlansDetails WHERE PlanDetailsID=@PDID_R1A;

DECLARE @Area_R1A FLOAT = 1.2;

DECLARE @Wanted_R1A FLOAT = CASE 
   WHEN 95 < @Min_R1A THEN @Min_R1A
   WHEN 95 > @Max_R1A THEN @Max_R1A
   ELSE 95 END;

INSERT INTO CultivationRegistrationsDetail
(CultivationRegistrationDetailID, RegistrationID, PlanDetailID, EstimatedYield,
 ExpectedHarvestStart, ExpectedHarvestEnd, WantedPrice, Status, Note, SystemNote,
 ApprovedBy, ApprovedAt, CreatedAt, UpdatedAt, IsDeleted)
VALUES
(NEWID(), @RegID_1, @PDID_R1A, @Area_R1A * @YieldHa_R1A,
 '2025-11-15','2026-01-15', @Wanted_R1A, N'Pending', N'Arabica specialty Krông Bông', NULL,
 NULL, NULL, GETDATE(), GETDATE(), 0);

-- Chi tiết R1.2: PLD-2025-A002 (0.6 ha, price 105)
DECLARE @PDID_R1B UNIQUEIDENTIFIER = (
   SELECT PlanDetailsID FROM ProcurementPlansDetails WHERE PlanDetailCode='PLD-2025-0002'
);

DECLARE @Min_R1B FLOAT, @Max_R1B FLOAT, @YieldHa_R1B FLOAT;

SELECT @Min_R1B=MinPriceRange, @Max_R1B=MaxPriceRange, @YieldHa_R1B=ExpectedYieldPerHectare
FROM ProcurementPlansDetails WHERE PlanDetailsID=@PDID_R1B;

DECLARE @Area_R1B FLOAT = 0.6;

DECLARE @Wanted_R1B FLOAT = CASE 
   WHEN 105 < @Min_R1B THEN @Min_R1B
   WHEN 105 > @Max_R1B THEN @Max_R1B
   ELSE 105 END;

INSERT INTO CultivationRegistrationsDetail
(CultivationRegistrationDetailID, RegistrationID, PlanDetailID, EstimatedYield,
 ExpectedHarvestStart, ExpectedHarvestEnd, WantedPrice, Status, Note, SystemNote,
 ApprovedBy, ApprovedAt, CreatedAt, UpdatedAt, IsDeleted)
VALUES
(NEWID(), @RegID_1, @PDID_R1B, @Area_R1B * @YieldHa_R1B,
 '2025-11-15','2026-01-15', @Wanted_R1B, N'Pending', N'Typica trưng bày hội chợ', NULL,
 NULL, NULL, GETDATE(), GETDATE(), 0);

COMMIT TRAN;

----------------------------------------------------------------
-- R2: Farmer FRM-2025-0002 → PLAN-2025-0002 (Robusta)
----------------------------------------------------------------
BEGIN TRAN;

DECLARE @PlanID_2 UNIQUEIDENTIFIER = (
   SELECT PlanID FROM ProcurementPlans WHERE PlanCode='PLAN-2025-0002'
);

DECLARE @FarmerID_2 UNIQUEIDENTIFIER = (
   SELECT FarmerID FROM Farmers WHERE FarmerCode='FRM-2025-0002'
);

SELECT @NextRegCode = @REG_PREFIX
    + RIGHT('0000' + CAST(ISNULL(MAX(TRY_CAST(RIGHT(RegistrationCode,4) AS INT)),0) + 1 AS VARCHAR(10)),4)
FROM CultivationRegistrations WITH (UPDLOCK, HOLDLOCK)
WHERE RegistrationCode LIKE @REG_PREFIX + '%';

DECLARE @RegID_2 UNIQUEIDENTIFIER = NEWID();

INSERT INTO CultivationRegistrations
(RegistrationID, RegistrationCode, PlanID, FarmerID, RegisteredArea, RegisteredAt, TotalWantedPrice,
 Status, Note, SystemNote, IsDeleted, CreatedAt, UpdatedAt)
VALUES
(@RegID_2, @NextRegCode, @PlanID_2, @FarmerID_2, 2.5, GETDATE(), NULL,
 N'Pending', N'Robusta Ea Kar/Đắk Lắk', NULL, 0, GETDATE(), GETDATE());

-- R2.1: PLD-2025-0002-002 (Washed) 1.5 ha, price = clamp(65)
DECLARE @PDID_R2A UNIQUEIDENTIFIER = (
   SELECT PlanDetailsID FROM ProcurementPlansDetails WHERE PlanDetailCode='PLD-2025-0003'
);

DECLARE @Min_R2A FLOAT, @Max_R2A FLOAT, @YieldHa_R2A FLOAT;

SELECT @Min_R2A=MinPriceRange, @Max_R2A=MaxPriceRange, @YieldHa_R2A=ExpectedYieldPerHectare
FROM ProcurementPlansDetails WHERE PlanDetailsID=@PDID_R2A;

DECLARE @Area_R2A FLOAT = 1.5;

DECLARE @Wanted_R2A FLOAT = CASE 
   WHEN 65 < @Min_R2A THEN @Min_R2A
   WHEN 65 > @Max_R2A THEN @Max_R2A
   ELSE 65 END;

INSERT INTO CultivationRegistrationsDetail
(CultivationRegistrationDetailID, RegistrationID, PlanDetailID, EstimatedYield,
 ExpectedHarvestStart, ExpectedHarvestEnd, WantedPrice, Status, Note, SystemNote,
 ApprovedBy, ApprovedAt, CreatedAt, UpdatedAt, IsDeleted)
VALUES
(NEWID(), @RegID_2, @PDID_R2A, @Area_R2A * @YieldHa_R2A,
 '2025-11-20','2026-02-15', @Wanted_R2A, N'Pending', N'Robusta Washed', NULL,
 NULL, NULL, GETDATE(), GETDATE(), 0);

-- R2.2: PLD-2025-0002-001 (Robusta thường) 1.0 ha, price = clamp(65)
DECLARE @PDID_R2B UNIQUEIDENTIFIER = (
   SELECT PlanDetailsID FROM ProcurementPlansDetails WHERE PlanDetailCode='PLD-2025-0004'
);

DECLARE @Min_R2B FLOAT, @Max_R2B FLOAT, @YieldHa_R2B FLOAT;

SELECT @Min_R2B=MinPriceRange, @Max_R2B=MaxPriceRange, @YieldHa_R2B=ExpectedYieldPerHectare
FROM ProcurementPlansDetails WHERE PlanDetailsID=@PDID_R2B;

DECLARE @Area_R2B FLOAT = 1.0;
DECLARE @Wanted_R2B FLOAT = CASE 
   WHEN 65 < @Min_R2B THEN @Min_R2B
   WHEN 65 > @Max_R2B THEN @Max_R2B
   ELSE 65 END;

INSERT INTO CultivationRegistrationsDetail
(CultivationRegistrationDetailID, RegistrationID, PlanDetailID, EstimatedYield,
 ExpectedHarvestStart, ExpectedHarvestEnd, WantedPrice, Status, Note, SystemNote,
 ApprovedBy, ApprovedAt, CreatedAt, UpdatedAt, IsDeleted)
VALUES
(NEWID(), @RegID_2, @PDID_R2B, @Area_R2B * @YieldHa_R2B,
 '2025-11-20','2026-02-15', @Wanted_R2B, N'Pending', N'Robusta thường', NULL,
 NULL, NULL, GETDATE(), GETDATE(), 0);

COMMIT TRAN;

----------------------------------------------------------------
-- R3: Farmer FRM-2025-0003 → PLAN-2025-0004 (Robusta Washed & TR9)
----------------------------------------------------------------
BEGIN TRAN;

DECLARE @PlanID_3 UNIQUEIDENTIFIER = (
   SELECT PlanID FROM ProcurementPlans WHERE PlanCode='PLAN-2025-0004'
);

DECLARE @FarmerID_3 UNIQUEIDENTIFIER = (
   SELECT FarmerID FROM Farmers WHERE FarmerCode='FRM-2025-0003'
);

SELECT @NextRegCode = @REG_PREFIX
    + RIGHT('0000' + CAST(ISNULL(MAX(TRY_CAST(RIGHT(RegistrationCode,4) AS INT)),0) + 1 AS VARCHAR(10)),4)
FROM CultivationRegistrations WITH (UPDLOCK, HOLDLOCK)
WHERE RegistrationCode LIKE @REG_PREFIX + '%';

DECLARE @RegID_3 UNIQUEIDENTIFIER = NEWID();

INSERT INTO CultivationRegistrations
(RegistrationID, RegistrationCode, PlanID, FarmerID, RegisteredArea, RegisteredAt, TotalWantedPrice,
 Status, Note, SystemNote, IsDeleted, CreatedAt, UpdatedAt)
VALUES
(@RegID_3, @NextRegCode, @PlanID_3, @FarmerID_3, 3.0, GETDATE(), NULL,
 N'Pending', N'Robusta Washed + TR9 năng suất cao', NULL, 0, GETDATE(), GETDATE());

-- R3.1: PLD-2025-0004-001 (Honey) 1.5 ha, price clamp(65)
DECLARE @PDID_R3A UNIQUEIDENTIFIER = (
   SELECT PlanDetailsID FROM ProcurementPlansDetails WHERE PlanDetailCode='PLD-2025-0005'
);

DECLARE @Min_R3A FLOAT, @Max_R3A FLOAT, @YieldHa_R3A FLOAT;

SELECT @Min_R3A=MinPriceRange, @Max_R3A=MaxPriceRange, @YieldHa_R3A=ExpectedYieldPerHectare
FROM ProcurementPlansDetails WHERE PlanDetailsID=@PDID_R3A;

DECLARE @Area_R3A FLOAT = 1.5;

DECLARE @Wanted_R3A FLOAT = CASE 
   WHEN 65 < @Min_R3A THEN @Min_R3A
   WHEN 65 > @Max_R3A THEN @Max_R3A
   ELSE 65 END;

INSERT INTO CultivationRegistrationsDetail
(CultivationRegistrationDetailID, RegistrationID, PlanDetailID, EstimatedYield,
 ExpectedHarvestStart, ExpectedHarvestEnd, WantedPrice, Status, Note, SystemNote,
 ApprovedBy, ApprovedAt, CreatedAt, UpdatedAt, IsDeleted)
VALUES
(NEWID(), @RegID_3, @PDID_R3A, @Area_R3A * @YieldHa_R3A,
 '2025-11-20','2026-02-20', @Wanted_R3A, N'Pending', N'Robusta Honey', NULL,
 NULL, NULL, GETDATE(), GETDATE(), 0);

-- R3.2: PLD-2025-0004-002 (TR9) 1.5 ha, price clamp(60)
DECLARE @PDID_R3B UNIQUEIDENTIFIER = (
   SELECT PlanDetailsID FROM ProcurementPlansDetails WHERE PlanDetailCode='PLD-2025-0006'
);

DECLARE @Min_R3B FLOAT, @Max_R3B FLOAT, @YieldHa_R3B FLOAT;

SELECT @Min_R3B=MinPriceRange, @Max_R3B=MaxPriceRange, @YieldHa_R3B=ExpectedYieldPerHectare
FROM ProcurementPlansDetails WHERE PlanDetailsID=@PDID_R3B;

DECLARE @Area_R3B FLOAT = 1.5;

DECLARE @Wanted_R3B FLOAT = CASE 
   WHEN 60 < @Min_R3B THEN @Min_R3B
   WHEN 60 > @Max_R3B THEN @Max_R3B
   ELSE 60 END;

INSERT INTO CultivationRegistrationsDetail
(CultivationRegistrationDetailID, RegistrationID, PlanDetailID, EstimatedYield,
 ExpectedHarvestStart, ExpectedHarvestEnd, WantedPrice, Status, Note, SystemNote,
 ApprovedBy, ApprovedAt, CreatedAt, UpdatedAt, IsDeleted)
VALUES
(NEWID(), @RegID_3, @PDID_R3B, @Area_R3B * @YieldHa_R3B,
 '2025-11-20','2026-02-20', @Wanted_R3B, N'Pending', N'Robusta TR9', NULL,
 NULL, NULL, GETDATE(), GETDATE(), 0);

COMMIT TRAN;

----------------------------------------------------------------
-- R4: Farmer FRM-2025-0004 → PLAN-2025-0003 (GIAO LẦN 1)
----------------------------------------------------------------
BEGIN TRAN;

DECLARE @PlanID_4 UNIQUEIDENTIFIER = (
   SELECT PlanID FROM ProcurementPlans WHERE PlanCode='PLAN-2025-0003'
);

DECLARE @FarmerID_4 UNIQUEIDENTIFIER = (
   SELECT FarmerID FROM Farmers WHERE FarmerCode='FRM-2025-0004'
);

SELECT @NextRegCode = @REG_PREFIX
    + RIGHT('0000' + CAST(ISNULL(MAX(TRY_CAST(RIGHT(RegistrationCode,4) AS INT)),0) + 1 AS VARCHAR(10)),4)
FROM CultivationRegistrations WITH (UPDLOCK, HOLDLOCK)
WHERE RegistrationCode LIKE @REG_PREFIX + '%';

DECLARE @RegID_4 UNIQUEIDENTIFIER = NEWID();

INSERT INTO CultivationRegistrations
(RegistrationID, RegistrationCode, PlanID, FarmerID, RegisteredArea, RegisteredAt, TotalWantedPrice,
 Status, Note, SystemNote, IsDeleted, CreatedAt, UpdatedAt)
VALUES
(@RegID_4, @NextRegCode, @PlanID_4, @FarmerID_4, 2.0, GETDATE(), NULL,
 N'Pending', N'Arabica + Culi cho đợt giao 1', NULL, 0, GETDATE(), GETDATE());

-- R4.1: PLD-GIAO1-001 (Arabica) 1.0 ha, price = midpoint
DECLARE @PDID_R4A UNIQUEIDENTIFIER = (
   SELECT PlanDetailsID FROM ProcurementPlansDetails WHERE PlanDetailCode='PLD-2025-0007'
);

DECLARE @Min_R4A FLOAT, @Max_R4A FLOAT, @YieldHa_R4A FLOAT;

SELECT @Min_R4A=MinPriceRange, @Max_R4A=MaxPriceRange, @YieldHa_R4A=ExpectedYieldPerHectare
FROM ProcurementPlansDetails WHERE PlanDetailsID=@PDID_R4A;

DECLARE @Area_R4A FLOAT = 1.0;

DECLARE @Wanted_R4A_INPUT FLOAT = NULL; -- không set -> midpoint

DECLARE @Wanted_R4A FLOAT = 
  CASE 
    WHEN COALESCE(@Wanted_R4A_INPUT, (@Min_R4A+@Max_R4A)/2.0) < @Min_R4A THEN @Min_R4A
    WHEN COALESCE(@Wanted_R4A_INPUT, (@Min_R4A+@Max_R4A)/2.0) > @Max_R4A THEN @Max_R4A
    ELSE COALESCE(@Wanted_R4A_INPUT, (@Min_R4A+@Max_R4A)/2.0)
  END;

INSERT INTO CultivationRegistrationsDetail
(CultivationRegistrationDetailID, RegistrationID, PlanDetailID, EstimatedYield,
 ExpectedHarvestStart, ExpectedHarvestEnd, WantedPrice, Status, Note, SystemNote,
 ApprovedBy, ApprovedAt, CreatedAt, UpdatedAt, IsDeleted)
VALUES
(NEWID(), @RegID_4, @PDID_R4A, @Area_R4A * @YieldHa_R4A,
 '2025-11-15','2026-01-31', @Wanted_R4A, N'Pending', N'Arabica nguyên chất', NULL,
 NULL, NULL, GETDATE(), GETDATE(), 0);

-- R4.2: PLD-2025-C002 (Robusta) 1.0 ha, price = midpoint
DECLARE @PDID_R4B UNIQUEIDENTIFIER = (
   SELECT PlanDetailsID FROM ProcurementPlansDetails WHERE PlanDetailCode='PLD-2025-0007'
);

DECLARE @Min_R4B FLOAT, @Max_R4B FLOAT, @YieldHa_R4B FLOAT;

SELECT @Min_R4B=MinPriceRange, @Max_R4B=MaxPriceRange, @YieldHa_R4B=ExpectedYieldPerHectare
FROM ProcurementPlansDetails WHERE PlanDetailsID=@PDID_R4B;

DECLARE @Area_R4B FLOAT = 1.0;

DECLARE @Wanted_R4B_INPUT FLOAT = NULL; -- midpoint

DECLARE @Wanted_R4B FLOAT = 
  CASE 
    WHEN COALESCE(@Wanted_R4B_INPUT, (@Min_R4B+@Max_R4B)/2.0) < @Min_R4B THEN @Min_R4B
    WHEN COALESCE(@Wanted_R4B_INPUT, (@Min_R4B+@Max_R4B)/2.0) > @Max_R4B THEN @Max_R4B
    ELSE COALESCE(@Wanted_R4B_INPUT, (@Min_R4B+@Max_R4B)/2.0)
  END;

INSERT INTO CultivationRegistrationsDetail
(CultivationRegistrationDetailID, RegistrationID, PlanDetailID, EstimatedYield,
 ExpectedHarvestStart, ExpectedHarvestEnd, WantedPrice, Status, Note, SystemNote,
 ApprovedBy, ApprovedAt, CreatedAt, UpdatedAt, IsDeleted)
VALUES
(NEWID(), @RegID_4, @PDID_R4B, @Area_R4B * @YieldHa_R4B,
 '2025-11-15','2026-01-31', @Wanted_R4B, N'Pending', N'Robusta thông thường – giao 1', NULL,
 NULL, NULL, GETDATE(), GETDATE(), 0);

COMMIT TRAN;

/* QUICK CHECK */
SELECT cr.RegistrationCode, f.FarmerCode, pp.PlanCode, cr.RegisteredArea,
       cr.TotalWantedPrice, cr.Status, cr.CreatedAt
FROM CultivationRegistrations cr
JOIN Farmers f          ON cr.FarmerID = f.FarmerID
JOIN ProcurementPlans pp ON cr.PlanID  = pp.PlanID
WHERE cr.CreatedAt >= DATEADD(DAY, -1, GETDATE())   -- hoặc lọc theo năm hiện tại
ORDER BY cr.CreatedAt DESC;

SELECT d.RegistrationID, pld.PlanDetailCode, d.EstimatedYield, d.WantedPrice, d.Status,
       d.ExpectedHarvestStart, d.ExpectedHarvestEnd, d.CreatedAt
FROM CultivationRegistrationsDetail d
JOIN ProcurementPlansDetails pld ON d.PlanDetailID = pld.PlanDetailsID
WHERE d.RegistrationID IN (
    SELECT cr2.RegistrationID
    FROM CultivationRegistrations cr2
    WHERE cr2.CreatedAt >= DATEADD(DAY, -1, GETDATE())
)
ORDER BY d.CreatedAt DESC;

GO

-- Insert vào bảng FarmingCommitments
-- Lấy PlanID, PlanDetailID và ManagerID để hoàn tất cam kết
DECLARE @RegistrationID UNIQUEIDENTIFIER = (
    SELECT RegistrationID FROM CultivationRegistrations WHERE RegistrationCode = 'REG-2025-0001'
);

DECLARE @PlanID UNIQUEIDENTIFIER = (
    SELECT PlanID FROM ProcurementPlans WHERE PlanCode = 'PLAN-2025-0001'
);

DECLARE @FarmerID UNIQUEIDENTIFIER = (
    SELECT FarmerID FROM Farmers WHERE FarmerCode = 'FRM-2025-0001'
);

DECLARE @ManagerID UNIQUEIDENTIFIER = (
    SELECT ManagerID FROM BusinessManagers 
    WHERE UserID = (SELECT UserID FROM UserAccounts WHERE Email = 'businessmanager@gmail.com')
);

INSERT INTO FarmingCommitments (
    CommitmentCode, RegistrationID, PlanID, FarmerID,
    CommitmentName, ApprovedBy, ApprovedAt, 
	Note, TotalPrice
)
VALUES (
    'FC-2025-0001', @RegistrationID, @PlanID, @FarmerID,
    N'Bảng cam kết Kế hoạch thu mua cà phê 2025-2026', @ManagerID, GETDATE(),
    N'Cam kết cung ứng đúng chuẩn và đúng hạn, đã qua thẩm định nội bộ', 960000
);

GO

-- Insert vào bảng FarmingCommitmentsDetails
DECLARE @RegistrationDetailID UNIQUEIDENTIFIER = (
    SELECT CultivationRegistrationDetailID FROM CultivationRegistrationsDetail
    WHERE RegistrationID = (SELECT RegistrationID FROM CultivationRegistrations WHERE RegistrationCode = 'REG-2025-0001')
    AND PlanDetailID = (SELECT PlanDetailsID FROM ProcurementPlansDetails WHERE PlanDetailCode = 'PLD-2025-0001')
);

DECLARE @PlanDetailID UNIQUEIDENTIFIER = (
    SELECT PlanDetailsID FROM ProcurementPlansDetails WHERE PlanDetailCode = 'PLD-2025-0001'
);

DECLARE @CommitmentID UNIQUEIDENTIFIER = (
    SELECT CommitmentID FROM FarmingCommitments WHERE CommitmentCode = 'FC-2025-0001'
);

INSERT INTO FarmingCommitmentsDetails (
    CommitmentDetailCode, RegistrationDetailID, PlanDetailID, CommitmentID,
    ConfirmedPrice, CommittedQuantity, EstimatedDeliveryStart, EstimatedDeliveryEnd,
    Note
)
VALUES (
    'FCD-2025-0001', @RegistrationDetailID, @PlanDetailID, @CommitmentID,
    960000, 2100, '2026-01-20', '2026-01-30',
    N'Cam kết cung ứng đúng chuẩn và đúng hạn, đã qua thẩm định nội bộ'
);

GO

-- Insert vào bảng CropSeasons
-- Lấy các ID cần thiết
DECLARE @FarmerID UNIQUEIDENTIFIER = (
   SELECT FarmerID FROM Farmers WHERE FarmerCode = 'FRM-2025-0001'
);

DECLARE @CommitmentID UNIQUEIDENTIFIER = (
   SELECT CommitmentID FROM FarmingCommitments WHERE CommitmentCode = 'FC-2025-0001'
);

INSERT INTO CropSeasons (
    CropSeasonCode, FarmerID, CommitmentID,
    SeasonName, Area, StartDate, EndDate, Note
)
VALUES (
    'SEASON-2025-0001', @FarmerID, @CommitmentID,
    N'Mùa vụ Arabica Krông Bông 2025', 1.8, '2025-07-01', '2026-01-20',
    N'Mùa vụ đầu tiên với công nghệ giám sát AI và tưới nhỏ giọt'
);

GO

-- Insert vào bảng CropSeasonDetails
DECLARE @CommitmentDetailID UNIQUEIDENTIFIER = (
    SELECT CommitmentDetailID FROM FarmingCommitmentsDetails WHERE CommitmentDetailCode = 'FCD-2025-0001'
);

DECLARE @CropSeasonID UNIQUEIDENTIFIER = (
    SELECT CropSeasonID FROM CropSeasons WHERE CropSeasonCode = 'SEASON-2025-0001'
);

DECLARE @CropSeasonDetailID UNIQUEIDENTIFIER = 'b21d7b4c-b5ca-4e4c-a39b-ae09a2e13a4a';

INSERT INTO CropSeasonDetails (
    DetailID, CropSeasonID, CommitmentDetailID, ExpectedHarvestStart, ExpectedHarvestEnd,
    EstimatedYield, ActualYield, AreaAllocated, PlannedQuality, QualityGrade
)
VALUES (
    @CropSeasonDetailID, @CropSeasonID, @CommitmentDetailID, '2025-11-01', '2026-01-15',
    2200, 0, 1.8, N'Specialty', NULL
);

GO

-- Insert vào bảng CropStages
INSERT INTO CropStages (StageCode, StageName, Description, OrderIndex)
VALUES 
('planting', N'Gieo trồng', N'Bắt đầu trồng cây giống', 1),
('flowering', N'Ra hoa', N'Cây bắt đầu ra hoa', 2),
('fruiting', N'Kết trái', N'Giai đoạn nuôi quả', 3),
('ripening', N'Chín', N'Trái chín sẵn sàng thu hoạch', 4),
('harvesting', N'Thu hoạch', N'Tiến hành thu hoạch cà phê', 5);

GO

-- Insert vào bảng CropProgresses
DECLARE @CropSeasonDetailID UNIQUEIDENTIFIER = 'b21d7b4c-b5ca-4e4c-a39b-ae09a2e13a4a';

DECLARE @FarmerID UNIQUEIDENTIFIER = (
    SELECT FarmerID FROM Farmers WHERE FarmerCode = 'FRM-2025-0001'
);

-- planting - Trồng
DECLARE @StageID INT = (
   SELECT StageID FROM CropStages WHERE StageCode = 'planting'
);

INSERT INTO CropProgresses (
    CropSeasonDetailID, UpdatedBy, StageID, StageDescription,
    ProgressDate, PhotoUrl, VideoUrl, Note, StepIndex
)
VALUES (
    @CropSeasonDetailID, @FarmerID, @StageID, 
    N'Đã hoàn tất trồng 1.8ha Arabica tại Krông Bông, cây giống khỏe mạnh.',
    '2025-07-05', NULL, NULL, N'Gieo trồng đầu mùa', 1
);

-- flowering – Ra hoa
DECLARE @StageID_Flowering INT = (
   SELECT StageID FROM CropStages WHERE StageCode = 'flowering'
);

INSERT INTO CropProgresses (
    CropSeasonDetailID, UpdatedBy, StageID, StageDescription,
    ProgressDate, PhotoUrl, VideoUrl, Note, StepIndex
)
VALUES (
    @CropSeasonDetailID, @FarmerID, @StageID_Flowering,
    N'Cây bắt đầu ra hoa đồng loạt sau 2 tháng trồng. Sinh trưởng tốt.',
    '2025-09-10', NULL, NULL, N'Bón phân vi sinh hỗ trợ ra hoa', 2
);

-- fruiting – Kết trái
DECLARE @StageID_Fruiting INT = (
   SELECT StageID FROM CropStages WHERE StageCode = 'fruiting'
);

INSERT INTO CropProgresses (
    CropSeasonDetailID, UpdatedBy, StageID, StageDescription,
    ProgressDate, PhotoUrl, VideoUrl, Note, StepIndex
)
VALUES (
    @CropSeasonDetailID, @FarmerID, @StageID_Fruiting,
    N'Quả bắt đầu phát triển rõ. Dự kiến năng suất khả quan.',
    '2025-10-25', NULL, NULL, N'Dự định phun bổ sung vi lượng giai đoạn đầu trái.', 3
);

-- ripening – Chín
DECLARE @StageID_Ripening INT = (
   SELECT StageID FROM CropStages WHERE StageCode = 'ripening'
);

INSERT INTO CropProgresses (
    CropSeasonDetailID, UpdatedBy, StageID, StageDescription,
    ProgressDate, PhotoUrl, VideoUrl, Note, StepIndex
)
VALUES (
    @CropSeasonDetailID, @FarmerID, @StageID_Ripening,
    N'Trái cà phê chuyển màu đỏ đều, đạt độ chín thu hoạch.',
    '2026-01-05', NULL, NULL, N'Tiến hành kiểm tra ngẫu nhiên độ ẩm trước thu hoạch.', 4
);

-- harvesting – Thu hoạch
DECLARE @StageID_Harvesting INT = (
   SELECT StageID FROM CropStages WHERE StageCode = 'harvesting'
);

INSERT INTO CropProgresses (
    CropSeasonDetailID, UpdatedBy, StageID, StageDescription,
    ProgressDate, PhotoUrl, VideoUrl, Note, StepIndex
)
VALUES (
    @CropSeasonDetailID, @FarmerID, @StageID_Harvesting,
    N'Đã hoàn tất thu hoạch toàn bộ diện tích Arabica.',
    '2026-01-18', NULL, NULL, N'Chuyển mẻ sơ chế về khu vực phơi.', 5
);

GO

-- Insert vào bảng ProcessingStages
DECLARE @Natural INT = (
   SELECT MethodID FROM ProcessingMethods WHERE MethodCode = 'natural'
);

DECLARE @Washed INT = (
   SELECT MethodID FROM ProcessingMethods WHERE MethodCode = 'washed'
);

DECLARE @Honey INT = (
   SELECT MethodID FROM ProcessingMethods WHERE MethodCode = 'honey'
);

DECLARE @SemiWashed INT = (
   SELECT MethodID FROM ProcessingMethods WHERE MethodCode = 'semi-washed'
);

DECLARE @Carbonic INT = (
   SELECT MethodID FROM ProcessingMethods WHERE MethodCode = 'carbonic'
);

-- Các bước cho phương pháp natural
INSERT INTO ProcessingStages (MethodID, StageCode, StageName, Description, OrderIndex)
VALUES 
(@Natural, 'harvest', N'Thu hoạch', N'Hái trái cà phê chín tại vườn.', 1),
(@Natural, 'drying', N'Phơi', N'Phơi nguyên trái từ 10–25 ngày tùy thời tiết.', 2),
(@Natural, 'hulling', N'Xay vỏ', N'Xay tách vỏ khô để lấy nhân.', 3),
(@Natural, 'grading', N'Phân loại', N'Phân loại theo kích thước, trọng lượng và màu sắc.', 4);

-- Các bước cho phương pháp washed
INSERT INTO ProcessingStages (MethodID, StageCode, StageName, Description, OrderIndex)
VALUES 
(@Washed, 'harvest', N'Thu hoạch', N'Hái trái cà phê chín.', 1),
(@Washed, 'pulping', N'Xát vỏ', N'Loại bỏ lớp vỏ quả bên ngoài.', 2),
(@Washed, 'fermentation', N'Lên men', N'Lên men loại bỏ lớp nhớt trong 12–36h.', 3),
(@Washed, 'washing', N'Rửa sạch', N'Rửa kỹ bằng nước sạch.', 4),
(@Washed, 'drying', N'Phơi khô', N'Phơi hạt nhân đến khi đạt độ ẩm 11–12%.', 5),
(@Washed, 'hulling', N'Xay vỏ trấu', N'Loại bỏ lớp vỏ trấu bảo vệ.', 6);

-- Các bước cho phương pháp honey
INSERT INTO ProcessingStages (MethodID, StageCode, StageName, Description, OrderIndex)
VALUES 
(@Honey, 'harvest', N'Thu hoạch', N'Thu hoạch thủ công trái chín.', 1),
(@Honey, 'pulping', N'Xát vỏ', N'Xát lớp vỏ ngoài nhưng giữ lại nhớt.', 2),
(@Honey, 'drying', N'Phơi', N'Phơi hạt có nhớt trên bề mặt.', 3),
(@Honey, 'hulling', N'Xay vỏ', N'Tách vỏ trấu sau khi khô.', 4);

-- Các bước cho phương pháp semi-washed
INSERT INTO ProcessingStages (MethodID, StageCode, StageName, Description, OrderIndex)
VALUES 
(@SemiWashed, 'harvest',     N'Thu hoạch',     N'Thu hoạch trái chín bằng tay hoặc máy.', 1),
(@SemiWashed, 'pulping',     N'Xát vỏ',        N'Loại bỏ vỏ quả nhưng không lên men.', 2),
(@SemiWashed, 'partial-wash',N'Rửa sơ',        N'Rửa nhẹ loại bỏ chất nhầy mà không lên men sâu.', 3),
(@SemiWashed, 'drying',      N'Phơi khô',      N'Phơi hạt đến khi đạt độ ẩm 11–12%.', 4),
(@SemiWashed, 'hulling',     N'Xay vỏ',        N'Tách lớp trấu bảo vệ sau khi khô.', 5);

-- Các bước cho phương pháp carbonic (lên men yếm khí)
INSERT INTO ProcessingStages (MethodID, StageCode, StageName, Description, OrderIndex)
VALUES 
(@Carbonic, 'harvest',        N'Thu hoạch',         N'Chọn lọc trái cà phê chín đều.', 1),
(@Carbonic, 'carbonic-ferment',N'Lên men yếm khí', N'Ủ trái trong bồn kín chứa CO₂ trong 24–72 giờ.', 2),
(@Carbonic, 'drying',         N'Phơi',              N'Phơi nguyên trái dưới nắng hoặc sấy nhẹ.', 3),
(@Carbonic, 'hulling',        N'Xay vỏ',            N'Xay vỏ sau khi khô để lấy nhân.', 4),
(@Carbonic, 'grading',        N'Phân loại',         N'Phân loại theo kích cỡ, màu và trọng lượng.', 5);

GO

-- Insert vào bảng ProcessingBatches
DECLARE @FarmerID UNIQUEIDENTIFIER = (
  SELECT FarmerID FROM Farmers WHERE FarmerCode = 'FRM-2025-0001'
);

DECLARE @CoffeeTypeID UNIQUEIDENTIFIER = (
  SELECT CoffeeTypeID FROM CoffeeTypes WHERE TypeCode = 'CFT-2025-0001' -- Arabica
);

DECLARE @CropSeasonID UNIQUEIDENTIFIER = (
  SELECT CropSeasonID FROM CropSeasons WHERE CropSeasonCode = 'SEASON-2025-0001'
);

DECLARE @MethodID INT = (
  SELECT MethodID FROM ProcessingMethods WHERE MethodCode = 'natural'
);

DECLARE @UserID UNIQUEIDENTIFIER = @FarmerID; -- người cập nhật

INSERT INTO ProcessingBatches (
  SystemBatchCode, CoffeeTypeID, CropSeasonID, FarmerID,
  BatchCode, MethodID, InputQuantity, InputUnit, CreatedAt, UpdatedAt, Status
)
VALUES (
  'BATCH-2025-0001', @CoffeeTypeID, @CropSeasonID, @FarmerID,
  N'Lô Arabica đầu mùa', @MethodID, 1500.0, N'Kg', GETDATE(), GETDATE(), N'processing'
);

GO

-- Insert vào bảng ProcessingBatchProgresses
DECLARE @MethodID INT = (
  SELECT MethodID FROM ProcessingMethods WHERE MethodCode = 'natural'
);

DECLARE @BatchID UNIQUEIDENTIFIER = (
  SELECT BatchID FROM ProcessingBatches WHERE SystemBatchCode = 'BATCH-2025-0001'
);

DECLARE @FarmerID UNIQUEIDENTIFIER = (
  SELECT FarmerID FROM Farmers WHERE FarmerCode = 'FRM-2025-0001'
);

-- Step 1: Harvest
DECLARE @ProgressID1 UNIQUEIDENTIFIER = NEWID();

DECLARE @StageID1 INT = (
  SELECT StageID FROM ProcessingStages 
  WHERE MethodID = @MethodID AND StageCode = 'harvest'
);

INSERT INTO ProcessingBatchProgresses (
  ProgressID, BatchID, StepIndex, StageID, StageDescription,
  ProgressDate, OutputQuantity, OutputUnit, UpdatedBy, PhotoURL, VideoURL
)
VALUES (
  @ProgressID1, @BatchID, 1, @StageID1, N'Hái trái cà phê chín tại vườn.',
  GETDATE(), 1410, N'Kg', @FarmerID, NULL, NULL
);

-- Step 2: Drying
DECLARE @ProgressID2 UNIQUEIDENTIFIER = NEWID();

DECLARE @StageID2 INT = (
  SELECT StageID FROM ProcessingStages 
  WHERE MethodID = @MethodID AND StageCode = 'drying'
);

INSERT INTO ProcessingBatchProgresses (
  ProgressID, BatchID, StepIndex, StageID, StageDescription,
  ProgressDate, OutputQuantity, OutputUnit, UpdatedBy, PhotoURL, VideoURL
)
VALUES (
  @ProgressID2, @BatchID, 2, @StageID2, N'Phơi nguyên trái từ 10–25 ngày tùy thời tiết.',
  GETDATE(), 1420, N'Kg', @FarmerID, NULL, NULL
);

-- Step 3: Hulling
DECLARE @ProgressID3 UNIQUEIDENTIFIER = NEWID();

DECLARE @StageID3 INT = (
  SELECT StageID FROM ProcessingStages 
  WHERE MethodID = @MethodID AND StageCode = 'hulling'
);

INSERT INTO ProcessingBatchProgresses (
  ProgressID, BatchID, StepIndex, StageID, StageDescription,
  ProgressDate, OutputQuantity, OutputUnit, UpdatedBy, PhotoURL, VideoURL
)
VALUES (
  @ProgressID3, @BatchID, 3, @StageID3, N'Xay tách vỏ khô để lấy nhân.',
  GETDATE(), 1430, N'Kg', @FarmerID, NULL, NULL
);

-- Step 4: Grading
DECLARE @ProgressID4 UNIQUEIDENTIFIER = NEWID();

DECLARE @StageID4 INT = (
  SELECT StageID FROM ProcessingStages 
  WHERE MethodID = @MethodID AND StageCode = 'grading'
);

INSERT INTO ProcessingBatchProgresses (
  ProgressID, BatchID, StepIndex, StageID, StageDescription,
  ProgressDate, OutputQuantity, OutputUnit, UpdatedBy, PhotoURL, VideoURL
)
VALUES (
  @ProgressID4, @BatchID, 4, @StageID4, N'Phân loại theo kích thước, trọng lượng và màu sắc.',
  GETDATE(), 1440, N'Kg', @FarmerID, NULL, NULL
);

-- Insert vào bảng ProcessingParameters
INSERT INTO ProcessingParameters (
   ParameterID, ProgressID, ParameterName, ParameterValue, Unit, RecordedAt
)
VALUES 
(NEWID(), @ProgressID1, N'Humidity', '13.5', N'%', GETDATE()),
(NEWID(), @ProgressID2, N'Humidity', '12.8', N'%', GETDATE()),
(NEWID(), @ProgressID3, N'Humidity', '12.1', N'%', GETDATE()),
(NEWID(), @ProgressID4, N'Humidity', '11.4', N'%', GETDATE()),
(NEWID(), @ProgressID2, N'Temperature', '36.5', N'°C', GETDATE()),
(NEWID(), @ProgressID3, N'pH', '5.2', N'', GETDATE());

GO

-- Insert vào bảng GeneralFarmerReports
DECLARE @CropProgressID UNIQUEIDENTIFIER = (
  SELECT TOP 1 ProgressID
  FROM CropProgresses
  WHERE CropSeasonDetailID = (
    SELECT DetailID
    FROM CropSeasonDetails
    WHERE CropSeasonID = (
      SELECT CropSeasonID
      FROM CropSeasons
      WHERE CropSeasonCode = 'SEASON-2025-0001'
    )
  )
);

DECLARE @ReportedBy UNIQUEIDENTIFIER = (
  SELECT UserID FROM UserAccounts WHERE Email = 'farmer@gmail.com'
);

DECLARE @ReportID UNIQUEIDENTIFIER = NEWID();

INSERT INTO GeneralFarmerReports (
    ReportID, ReportCode, ReportType, CropProgressID,
    ReportedBy, Title, Description, SeverityLevel,
    ImageUrl, VideoUrl, ReportedAt, UpdatedAt
)
VALUES (
    @ReportID, 'REP-2025-0001', 'Crop', @CropProgressID,
    @ReportedBy, N'Cây cà phê bị vàng lá hàng loạt',
    N'Phát hiện hiện tượng vàng lá và rụng sớm trên 20% diện tích vườn. Có thể do sâu bệnh hoặc thiếu dinh dưỡng.',
    2,
    'https://example.com/images/leaf-yellowing.jpg',
    NULL,
    GETDATE(), GETDATE()
);

GO

-- Insert vào bảng ExpertAdvice
DECLARE @ReportID UNIQUEIDENTIFIER = (
  SELECT ReportID FROM GeneralFarmerReports WHERE ReportCode = 'REP-2025-0001'
);

DECLARE @ExpertID UNIQUEIDENTIFIER = (
  SELECT ExpertID FROM AgriculturalExperts WHERE ExpertCode = 'EXP-2025-0001'
);

INSERT INTO ExpertAdvice (
    ReportID, ExpertID, ResponseType, AdviceSource,
    AdviceText, AttachedFileUrl
)
VALUES (
    @ReportID, @ExpertID, 'corrective', 'human',
    N'Hiện tượng vàng lá có thể do tuyến trùng hoặc thiếu kali. Đề xuất kiểm tra pH đất, bổ sung phân kali và dùng thuốc trừ tuyến trùng nếu cần.',
    'https://example.com/docs/yellow-leaf-treatment.pdf'
);

GO

-- Insert vào bảng ProcessingBatchEvaluations
DECLARE @EvalID UNIQUEIDENTIFIER = NEWID();

DECLARE @BatchID UNIQUEIDENTIFIER = (
  SELECT BatchID FROM ProcessingBatches WHERE SystemBatchCode = 'BATCH-2025-0001'
);

DECLARE @ExpertUserID UNIQUEIDENTIFIER = (
  SELECT UserID FROM UserAccounts WHERE Email = 'expert@gmail.com'
);

INSERT INTO ProcessingBatchEvaluations (
  EvaluationID, EvaluationCode, BatchID, EvaluatedBy,
  EvaluationResult, Comments, EvaluatedAt
)
VALUES (
  @EvalID, 'EVAL-2025-0001', @BatchID, @ExpertUserID,
  N'Pass', N'Lô hàng đạt tiêu chuẩn độ ẩm và màu sắc, phù hợp để xuất khẩu.',
  GETDATE()
);

GO

-- Insert vào bảng ProcessingBatchWastes và ProcessingWasteDisposals theo từng Progress của Batch
DECLARE @BatchID UNIQUEIDENTIFIER = (
  SELECT BatchID FROM ProcessingBatches WHERE SystemBatchCode = 'BATCH-2025-0001'
);

DECLARE @FarmerUserID UNIQUEIDENTIFIER = (
  SELECT UserID FROM UserAccounts WHERE Email = 'farmer@gmail.com'
);

-- 1: Thu hoạch (Harvest)
DECLARE @ProgressID1 UNIQUEIDENTIFIER = (
  SELECT ProgressID FROM ProcessingBatchProgresses 
  WHERE BatchID = @BatchID AND StepIndex = 1
);

DECLARE @WasteID1 UNIQUEIDENTIFIER = NEWID();

INSERT INTO ProcessingBatchWastes (
  WasteID, WasteCode, ProgressID, WasteType, Quantity, Unit, Note, RecordedAt, RecordedBy
)
VALUES (
  @WasteID1, 'WASTE-2025-0001', @ProgressID1, N'Trái non/hỏng', 30, N'Kg',
  N'Loại bỏ trái non, trái dập trong quá trình hái.', GETDATE(), @FarmerUserID
);

INSERT INTO ProcessingWasteDisposals (
  DisposalID, DisposalCode, WasteID, DisposalMethod, HandledBy, Notes, IsSold, Revenue
)
VALUES (
  NEWID(), 'DISP-2025-0001', @WasteID1, N'Chôn lấp', @FarmerUserID, N'Trái không dùng được, chôn tại rìa vườn.', 0, NULL
);

-- 2: Phơi (Drying)
DECLARE @ProgressID2 UNIQUEIDENTIFIER = (
  SELECT ProgressID FROM ProcessingBatchProgresses 
  WHERE BatchID = @BatchID AND StepIndex = 2
);

DECLARE @WasteID2 UNIQUEIDENTIFIER = NEWID();

INSERT INTO ProcessingBatchWastes (
  WasteID, WasteCode, ProgressID, WasteType, Quantity, Unit, Note, RecordedAt, RecordedBy
)
VALUES (
  @WasteID2, 'WASTE-2025-0002', @ProgressID2, N'Vỏ quả khô', 80, N'Kg', 
  N'Phế phẩm từ vỏ sau khi phơi nguyên trái.', GETDATE(), @FarmerUserID
);

INSERT INTO ProcessingWasteDisposals (
  DisposalID, DisposalCode, WasteID, DisposalMethod, HandledBy, Notes, IsSold, Revenue
)
VALUES (
  NEWID(), 'DISP-2025-0002', @WasteID2, N'Sử dụng làm phân compost', 
  @FarmerUserID, N'Trộn với trấu và ủ tại trang trại.', 0, NULL
);

-- 3: Xay vỏ (Hulling)
DECLARE @ProgressID3 UNIQUEIDENTIFIER = (
  SELECT ProgressID FROM ProcessingBatchProgresses 
  WHERE BatchID = @BatchID AND StepIndex = 3
);

DECLARE @WasteID3 UNIQUEIDENTIFIER = NEWID();

INSERT INTO ProcessingBatchWastes (
  WasteID, WasteCode, ProgressID, WasteType, Quantity, Unit, Note, RecordedAt, RecordedBy
)
VALUES (
  @WasteID3, 'WASTE-2025-0003', @ProgressID3, N'Vỏ trấu', 50, N'Kg',
  N'Vỏ trấu sau khi xay bóc nhân.', GETDATE(), @FarmerUserID
);

INSERT INTO ProcessingWasteDisposals (
  DisposalID, DisposalCode, WasteID, DisposalMethod, HandledBy, Notes, IsSold, Revenue
)
VALUES (
  NEWID(), 'DISP-2025-0003', @WasteID3, N'Bán cho cơ sở đốt lò', 
  @FarmerUserID, N'Thu hồi nhiệt hoặc làm chất đốt.', 1, 350000
);

-- 4: Phân loại (Grading)
DECLARE @ProgressID4 UNIQUEIDENTIFIER = (
  SELECT ProgressID FROM ProcessingBatchProgresses 
  WHERE BatchID = @BatchID AND StepIndex = 4
);

DECLARE @WasteID4 UNIQUEIDENTIFIER = NEWID();

INSERT INTO ProcessingBatchWastes (
  WasteID, WasteCode, ProgressID, WasteType, Quantity, Unit, Note, RecordedAt, RecordedBy
)
VALUES (
  @WasteID4, 'WASTE-2025-0004', @ProgressID4, N'Hạt lép/hỏng', 20, N'Kg',
  N'Hạt không đạt tiêu chuẩn kích cỡ hoặc màu sắc.', GETDATE(), @FarmerUserID
);

INSERT INTO ProcessingWasteDisposals (
  DisposalID, DisposalCode, WasteID, DisposalMethod, HandledBy, Notes, IsSold, Revenue
)
VALUES (
  NEWID(), 'DISP-2025-0004', @WasteID4, N'Dùng làm thức ăn gia súc', 
  @FarmerUserID, N'Trộn với cám để nuôi gà vịt.', 0, NULL
);

GO

-- Insert vào bảng Warehouses
-- Lấy ManagerID từ BusinessManager đã có
DECLARE @BM_ManagerID UNIQUEIDENTIFIER = (
  SELECT ManagerID FROM BusinessManagers
  WHERE UserID = (SELECT UserID FROM UserAccounts WHERE Email = 'businessmanager@gmail.com')
);

-- Thêm kho 1: Kho Cư M’gar
INSERT INTO Warehouses (
   WarehouseCode, ManagerID, Name, Location, Capacity, CreatedAt, UpdatedAt, IsDeleted
)
VALUES (
   'WH-2025-DL001', @BM_ManagerID, N'Kho Cư M’gar', N'Thôn 3, Xã Cư M’gar, Đắk Lắk', 
   50000, GETDATE(), GETDATE(), 0
);

-- Thêm kho 2: Kho Buôn Hồ
INSERT INTO Warehouses (
   WarehouseCode, ManagerID, Name, Location, Capacity, CreatedAt, UpdatedAt, IsDeleted
)
VALUES (
   'WH-2025-DL002', @BM_ManagerID, N'Kho Buôn Hồ', N'Đường Nguyễn Huệ, Phường An Bình, TX. Buôn Hồ', 
   35000, GETDATE(), GETDATE(), 0
);

-- Lấy ManagerID từ BusinessManager thứ 2
DECLARE @BM2_ManagerID UNIQUEIDENTIFIER = (
  SELECT ManagerID FROM BusinessManagers
  WHERE UserID = (SELECT UserID FROM UserAccounts WHERE Email = 'manager2@gmail.com')
);

-- Thêm Kho 3: Kho Ea Kar
INSERT INTO Warehouses (
   WarehouseCode, ManagerID, Name, Location, Capacity, CreatedAt, UpdatedAt, IsDeleted
)
VALUES (
   'WH-2025-DL003',
   @BM2_ManagerID,
   N'Kho Ea Kar',
   N'Thị trấn Ea Kar, Huyện Ea Kar, Đắk Lắk',
   40000,
   GETDATE(),
   GETDATE(),
   0
);

-- Thêm Kho 4: Kho Krông Năng
INSERT INTO Warehouses (
   WarehouseCode, ManagerID, Name, Location, Capacity, CreatedAt, UpdatedAt, IsDeleted
)
VALUES (
   'WH-2025-DL004',
   @BM2_ManagerID,
   N'Kho Krông Năng',
   N'Thị trấn Krông Năng, Huyện Krông Năng, Đắk Lắk',
   30000,
   GETDATE(),
   GETDATE(),
   0
);

GO

-- Insert vào bảng BusinessStaffs
-- Lấy ManagerID từ BusinessManager đã có
DECLARE @BM_ManagerID UNIQUEIDENTIFIER = (
  SELECT ManagerID FROM BusinessManagers
  WHERE UserID = (SELECT UserID FROM UserAccounts WHERE Email = 'businessmanager@gmail.com')
);

-- Lấy UserID của nhân viên 1
DECLARE @Staff1UserID UNIQUEIDENTIFIER = (
  SELECT UserID FROM UserAccounts WHERE Email = 'businessstaff@gmail.com'
);

-- Tạo user phụ cho nhân viên kho 2 (nếu chưa có)
INSERT INTO UserAccounts (
   UserCode, Email, PhoneNumber, Name, Gender, DateOfBirth, Address, PasswordHash, RoleID
)
VALUES (
   'USR-2025-0007', 'warehouse2@gmail.com', '0901234567', N'Nguyễn Văn Khang', 
   'Male', '1992-04-20', N'Buôn Hồ', '$2a$11$mHeU1UxLZyZrwtWtikdAJeM3BteW4QrgBJOd8rWMz2sR9ZZyWayRS', 3
);

-- Lấy UserID của nhân viên 2
DECLARE @Staff2UserID UNIQUEIDENTIFIER = (
  SELECT UserID FROM UserAccounts WHERE Email = 'warehouse2@gmail.com'
);

-- Lấy WarehouseID cho từng kho
DECLARE @Warehouse1ID UNIQUEIDENTIFIER = (
  SELECT WarehouseID FROM Warehouses WHERE WarehouseCode = 'WH-2025-DL001'
);

DECLARE @Warehouse2ID UNIQUEIDENTIFIER = (
  SELECT WarehouseID FROM Warehouses WHERE WarehouseCode = 'WH-2025-DL002'
);

-- Thêm nhân viên 1: gán cho kho 1
INSERT INTO BusinessStaffs (
  StaffCode, UserID, SupervisorID, Position, Department, AssignedWarehouseID,
  IsActive, CreatedAt, UpdatedAt, IsDeleted
)
VALUES (
  'STAFF-2025-0007',
  @Staff1UserID,
  @BM_ManagerID,
  N'Thủ kho Cư M’gar',
  N'Kho Đắk Lắk',
  @Warehouse1ID,
  1,
  GETDATE(),
  GETDATE(),
  0
);

-- Thêm nhân viên 2: gán cho kho 2
INSERT INTO BusinessStaffs (
  StaffCode, UserID, SupervisorID, Position, Department, AssignedWarehouseID,
  IsActive, CreatedAt, UpdatedAt, IsDeleted
)
VALUES (
  'STAFF-2025-0008',
  @Staff2UserID,
  @BM_ManagerID,
  N'Thủ kho Buôn Hồ',
  N'Kho Đắk Lắk',
  @Warehouse2ID,
  1,
  GETDATE(),
  GETDATE(),
  0
);

DECLARE @BM2ID UNIQUEIDENTIFIER = (
  SELECT ManagerID FROM BusinessManagers WHERE UserID = (SELECT UserID FROM UserAccounts WHERE Email = 'manager2@gmail.com')
);

DECLARE @Staff3ID UNIQUEIDENTIFIER = (
   SELECT UserID FROM UserAccounts WHERE Email = 'staff3@gmail.com'
);

DECLARE @Staff4ID UNIQUEIDENTIFIER = (
   SELECT UserID FROM UserAccounts WHERE Email = 'staff4@gmail.com'
);

DECLARE @Staff5ID UNIQUEIDENTIFIER = (
   SELECT UserID FROM UserAccounts WHERE Email = 'staff5@gmail.com'
);

-- Giả định bạn có sẵn 2 kho để liên kết:
DECLARE @Warehouse3ID UNIQUEIDENTIFIER = (
  SELECT WarehouseID FROM Warehouses WHERE WarehouseCode = 'WH-2025-LD003'
);

DECLARE @Warehouse4ID UNIQUEIDENTIFIER = (
  SELECT WarehouseID FROM Warehouses WHERE WarehouseCode = 'WH-2025-LD004'
);

INSERT INTO BusinessStaffs (
  StaffCode, UserID, SupervisorID, Position, Department, AssignedWarehouseID,
  IsActive, CreatedAt, UpdatedAt, IsDeleted
)
VALUES 
('STAFF-2025-0009', @Staff3ID, @BM2ID, N'Thủ kho Lâm Hà', N'Kho Lâm Đồng', @Warehouse3ID, 1, GETDATE(), GETDATE(), 0),
('STAFF-2025-0010', @Staff4ID, @BM2ID, N'Trưởng nhóm đóng gói', N'Phòng Hậu cần', NULL, 1, GETDATE(), GETDATE(), 0),
('STAFF-2025-0011', @Staff5ID, @BM2ID, N'Kỹ thuật viên phân loại', N'Phòng Sơ chế', @Warehouse4ID, 1, GETDATE(), GETDATE(), 0);

GO

-- Insert vào bảng WarehouseInboundRequests
DECLARE @BatchID UNIQUEIDENTIFIER = (
  SELECT BatchID FROM ProcessingBatches WHERE SystemBatchCode = 'BATCH-2025-0001'
);

DECLARE @FarmerID UNIQUEIDENTIFIER = (
  SELECT FarmerID FROM Farmers WHERE FarmerCode = 'FRM-2025-0001'
);

DECLARE @BusinessStaffID UNIQUEIDENTIFIER = (
  SELECT StaffID FROM BusinessStaffs WHERE StaffCode = 'STAFF-2025-0007'
);

INSERT INTO WarehouseInboundRequests (
  InboundRequestCode, BatchID, FarmerID, BusinessStaffID,
  RequestedQuantity, PreferredDeliveryDate, ActualDeliveryDate,
  Status, Note, CreatedAt, UpdatedAt, IsDeleted
)
VALUES (
  'INREQ-2025-0001', @BatchID, @FarmerID, @BusinessStaffID,
  1400, '2025-06-28', '2025-06-30',
  'completed', N'Hàng đạt chất lượng tốt', GETDATE(), GETDATE(), 0
);

GO

-- Insert vào bảng WarehouseReceipts
DECLARE @WarehouseID UNIQUEIDENTIFIER = (
  SELECT WarehouseID FROM Warehouses WHERE WarehouseCode = 'WH-2025-DL001'
);

DECLARE @InboundRequestID UNIQUEIDENTIFIER = (
  SELECT InboundRequestID FROM WarehouseInboundRequests WHERE InboundRequestCode = 'INREQ-2025-0001'
);

DECLARE @StaffID UNIQUEIDENTIFIER = (
  SELECT StaffID FROM BusinessStaffs WHERE StaffCode = 'STAFF-2025-0007'
);

DECLARE @BatchID UNIQUEIDENTIFIER = (
  SELECT BatchID FROM ProcessingBatches WHERE SystemBatchCode = 'BATCH-2025-0001'
);

INSERT INTO WarehouseReceipts (
  ReceiptCode, InboundRequestID, WarehouseID, BatchID,
  ReceivedBy, LotCode, ReceivedQuantity, ReceivedAt,
  Note, QRCodeURL, IsDeleted
)
VALUES (
  'RECEIPT-2025-0001', @InboundRequestID, @WarehouseID, @BatchID,
  @StaffID, 'LOT-BATCH-0001', 1385.5, GETDATE(),
  N'Đã kiểm tra đầy đủ, không hư hỏng', 'https://qrdemo.vn/RECEIPT-2025-0145', 0
);

GO

-- Insert vào bảng Inventories
DECLARE @WarehouseID UNIQUEIDENTIFIER = (
  SELECT WarehouseID FROM Warehouses WHERE WarehouseCode = 'WH-2025-DL001'
);

DECLARE @BatchID UNIQUEIDENTIFIER = (
  SELECT BatchID FROM ProcessingBatches WHERE SystemBatchCode = 'BATCH-2025-0001'
);

INSERT INTO Inventories (
  InventoryCode, WarehouseID, BatchID, Quantity,
  Unit, CreatedAt, UpdatedAt, IsDeleted
)
VALUES (
  'INV-2025-0001', @WarehouseID, @BatchID, 1385.5,
  N'Kg', GETDATE(), GETDATE(), 0
);

GO

-- Insert vào bảng InventoryLogs
DECLARE @InventoryID UNIQUEIDENTIFIER = (
  SELECT InventoryID FROM Inventories WHERE InventoryCode = 'INV-2025-0001'
);

DECLARE @StaffID UNIQUEIDENTIFIER = (
  SELECT StaffID FROM BusinessStaffs WHERE StaffCode = 'STAFF-2025-0007'
);

INSERT INTO InventoryLogs (
  InventoryID, ActionType, QuantityChanged,
  UpdatedBy, TriggeredBySystem, Note,
  LoggedAt, IsDeleted
)
VALUES (
  @InventoryID, N'increase', 1385.5,
  @StaffID, 0, N'Nhập kho từ INREQ-2025-0008',
  GETDATE(), 0
);

INSERT INTO InventoryLogs (
  InventoryID, ActionType, QuantityChanged,
  UpdatedBy, TriggeredBySystem, Note,
  LoggedAt, IsDeleted
)
VALUES (
  @InventoryID, N'decrease', 500,
  @StaffID, 0, N'Xuất kho theo OUTREQ-2025-0032',
  GETDATE(), 0
);

GO

-- Insert vào bảng Products
DECLARE @CreatedBy UNIQUEIDENTIFIER = (
  SELECT UserID FROM UserAccounts WHERE Email = 'businessManager@gmail.com'
);

DECLARE @BatchID UNIQUEIDENTIFIER = (
  SELECT BatchID FROM ProcessingBatches WHERE SystemBatchCode = 'BATCH-2025-0001'
);

DECLARE @InventoryID UNIQUEIDENTIFIER = (
  SELECT InventoryID FROM Inventories WHERE InventoryCode = 'INV-2025-0001'
);

DECLARE @CoffeeTypeID UNIQUEIDENTIFIER = (
  SELECT CoffeeTypeID FROM CoffeeTypes WHERE TypeCode = 'CFT-2025-0001'
);

DECLARE @ApprovedBy UNIQUEIDENTIFIER = (
  SELECT UserID FROM UserAccounts WHERE Email = 'businessManager@gmail.com'
);

DECLARE @ProductID UNIQUEIDENTIFIER = NEWID();

INSERT INTO Products (
  ProductID, ProductCode, ProductName, Description, UnitPrice, QuantityAvailable,
  Unit, CreatedBy, BatchID, InventoryID, CoffeeTypeID, OriginRegion,
  OriginFarmLocation, GeographicalIndicationCode, CertificationURL,
  EvaluatedQuality, EvaluationScore, Status, ApprovedBy, ApprovalNote,
  ApprovedAt, CreatedAt, UpdatedAt, IsDeleted
)
VALUES (
  @ProductID, 'PROD-001-BM-2025-0001', N'Arabica DakLak - Natural',
  N'Arabica chất lượng Specialty, được sơ chế theo phương pháp tự nhiên tại Ea Tu.',
  95000, 1385.5, N'Kg',
  @CreatedBy, @BatchID, @InventoryID, @CoffeeTypeID,
  N'Đắk Lắk', N'Xã Ea Tu, TP. Buôn Ma Thuột', 'DLK-GI-0001',
  'https://certs.example.com/vietgap/arabica.pdf',
  N'Đặc sản', 84.5, N'Approved', @ApprovedBy, N'Đáp ứng đầy đủ tiêu chuẩn chấm điểm.',
  GETDATE(), GETDATE(), GETDATE(), 0
);

GO

-- Insert vào bảng Orders
DECLARE @CreatedBy UNIQUEIDENTIFIER = (
  SELECT UserID FROM UserAccounts WHERE UserCode = 'USR-2025-0005'
);

DECLARE @DeliveryBatchID UNIQUEIDENTIFIER = (
  SELECT DeliveryBatchID FROM ContractDeliveryBatches WHERE DeliveryBatchCode = 'DELB-2025-0001'
);

DECLARE @OrderID UNIQUEIDENTIFIER = NEWID();

INSERT INTO Orders (
  OrderID, OrderCode, DeliveryBatchID, DeliveryRound,
  OrderDate, ActualDeliveryDate, TotalAmount,
  Note, Status, CancelReason, CreatedAt, UpdatedAt, IsDeleted,
  CreatedBy
)
VALUES (
  @OrderID, 'ORD-2025-0001', @DeliveryBatchID, 1,
  GETDATE(), GETDATE(), 95000 * 500,
  N'Đợt giao hàng Arabica đầu tiên', 'preparing', NULL,
  GETDATE(), GETDATE(), 0,
  @CreatedBy
);

GO

-- Insert vào bảng OrderItems
DECLARE @ContractDeliveryItemID UNIQUEIDENTIFIER = (
  SELECT DeliveryItemID FROM ContractDeliveryItems WHERE DeliveryItemCode = 'DLI-2025-0001'
);

DECLARE @OrderID UNIQUEIDENTIFIER = (
  SELECT OrderID FROM Orders WHERE OrderCode = 'ORD-2025-0001'
);

DECLARE @ProductID UNIQUEIDENTIFIER = (
  SELECT ProductID FROM Products WHERE ProductCode = 'PROD-001-BM-2025-0001'
);

INSERT INTO OrderItems (
  OrderItemID, OrderID, ContractDeliveryItemID, ProductID,
  Quantity, UnitPrice, DiscountAmount, TotalPrice,
  Note, CreatedAt, UpdatedAt, IsDeleted
)
VALUES (
  NEWID(), @OrderID, @ContractDeliveryItemID, @ProductID,
  500, 95000, 0.0, 95000 * 500,
  N'Mặt hàng giao tiêu chuẩn', GETDATE(), GETDATE(), 0
);

GO

-- Insert vào bảng WarehouseOutboundRequests
DECLARE @InventoryID UNIQUEIDENTIFIER = (
  SELECT InventoryID FROM Inventories WHERE InventoryCode = 'INV-2025-0001'
);

DECLARE @WarehouseID UNIQUEIDENTIFIER = (
  SELECT WarehouseID FROM Warehouses WHERE WarehouseCode = 'WH-2025-DL001'
);

DECLARE @RequestedBy UNIQUEIDENTIFIER = (
  SELECT ManagerID FROM BusinessManagers WHERE ManagerCode = 'BM-2025-0001'
);

DECLARE @OrderItemID UNIQUEIDENTIFIER = (
  SELECT TOP 1 OrderItemID FROM OrderItems WHERE Quantity = 500
);

-- Insert yêu cầu xuất kho
INSERT INTO WarehouseOutboundRequests (
  OutboundRequestCode, WarehouseID, InventoryID,
  RequestedQuantity, Unit, RequestedBy,
  Purpose, OrderItemID, Reason, Status
)
VALUES (
  'OUTREQ-2025-0032',
  @WarehouseID,
  @InventoryID,
  500,
  N'Kg',
  @RequestedBy,
  N'Giao đơn hàng ORD-2025-0001',
  @OrderItemID,
  N'Yêu cầu xuất 500Kg Arabica để giao cho khách hàng B2B.',
  N'Pending'
);

GO

-- Insert vào bảng WarehouseOutboundReceipts
DECLARE @OutboundRequestID UNIQUEIDENTIFIER = (
  SELECT OutboundRequestID FROM WarehouseOutboundRequests WHERE OutboundRequestCode = 'OUTREQ-2025-0032'
);

DECLARE @StaffID UNIQUEIDENTIFIER = (
  SELECT StaffID FROM BusinessStaffs WHERE StaffCode = 'STAFF-2025-0007'
);

DECLARE @BatchID UNIQUEIDENTIFIER = (
  SELECT BatchID FROM ProcessingBatches WHERE SystemBatchCode = 'BATCH-2025-0001'
);

DECLARE @InventoryID UNIQUEIDENTIFIER = (
  SELECT InventoryID FROM Inventories WHERE InventoryCode = 'INV-2025-0001'
);

DECLARE @WarehouseID UNIQUEIDENTIFIER = (
  SELECT WarehouseID FROM Warehouses WHERE WarehouseCode = 'WH-2025-DL001'
);

-- Insert phiếu xuất kho
INSERT INTO WarehouseOutboundReceipts (
  OutboundReceiptCode, OutboundRequestID, WarehouseID,
  InventoryID, BatchID, Quantity, ExportedBy,
  ExportedAt, DestinationNote, Note
)
VALUES (
  'OUT-RECEIPT-2025-0078',
  @OutboundRequestID,
  @WarehouseID,
  @InventoryID,
  @BatchID,
  500,
  @StaffID,
  GETDATE(),
  N'Kho khách hàng B2B tại Bình Dương',
  N'Xác nhận đã xuất 500Kg Arabica đúng số lượng và tiêu chuẩn.'
);

-- Sau khi xuất kho 500Kg, cập nhật lại số lượng tồn kho
UPDATE Inventories
SET Quantity = Quantity - 500,
    UpdatedAt = GETDATE()
WHERE InventoryID = @InventoryID;

GO

-- Các ID cần dùng
DECLARE @ShipmentID UNIQUEIDENTIFIER = NEWID();
DECLARE @ShipmentCode VARCHAR(20) = 'SHIP-2025-0001';

DECLARE @ComplaintID UNIQUEIDENTIFIER = NEWID();
DECLARE @ComplaintCode VARCHAR(20) = 'CMP-2025-0001';

DECLARE @OrderID UNIQUEIDENTIFIER = (
	SELECT TOP 1 OrderID FROM Orders WHERE OrderCode = 'ORD-2025-0001'
);

DECLARE @OrderItemID UNIQUEIDENTIFIER = (
	SELECT TOP 1 OrderItemID FROM OrderItems WHERE OrderID = @OrderID
);

DECLARE @DeliveryStaffID UNIQUEIDENTIFIER = (
	SELECT TOP 1 UserID FROM UserAccounts WHERE UserCode = 'USR-2025-0006'
);

DECLARE @CreatedBy UNIQUEIDENTIFIER = (
	SELECT TOP 1 UserID FROM UserAccounts WHERE UserCode = 'USR-2025-0002'
);

DECLARE @ResolvedBy UNIQUEIDENTIFIER = (
	SELECT TOP 1 ManagerID FROM BusinessManagers WHERE ManagerCode = 'BM-2025-0001'
);

DECLARE @BuyerID UNIQUEIDENTIFIER = 'ED49B648-F170-48AC-8535-823C80381179';

-- Insert vào bảng Shipments
INSERT INTO Shipments (
  ShipmentID, ShipmentCode, OrderID, DeliveryStaffID,
  ShippedQuantity, ShippedAt, DeliveryStatus, ReceivedAt,
  CreatedBy, CreatedAt, UpdatedAt, IsDeleted
)
VALUES (
  @ShipmentID, @ShipmentCode, @OrderID, @DeliveryStaffID,
  500, GETDATE(), 'Pending', GETDATE(),
  @CreatedBy, GETDATE(), GETDATE(), 0
);

-- Insert vào bảng ShipmentDetails
INSERT INTO ShipmentDetails (
  ShipmentDetailID, ShipmentID, OrderItemID, Quantity,
  Unit, Note, CreatedAt, UpdatedAt, IsDeleted
)
VALUES (
  NEWID(), @ShipmentID, @OrderItemID,
  500, 'Kg', N'Hàng giao nguyên kiện', GETDATE(), GETDATE(), 0
);

-- Insert vào bảng OrderComplaints
INSERT INTO OrderComplaints (
  ComplaintID, ComplaintCode, OrderItemID, RaisedBy,
  ComplaintType, Description, EvidenceURL, Status,
  ResolutionNote, ResolvedBy, CreatedBy, CreatedAt, UpdatedAt,
  ResolvedAt, IsDeleted
)
VALUES (
  @ComplaintID, @ComplaintCode, @OrderItemID, @BuyerID,
  N'Sai chất lượng', N'Sản phẩm không đạt như cam kết. Độ ẩm cao.',
  N'https://drive.google.com/evidence1.jpg', 'Resolved',
  N'Đã hoàn tiền 10%. Kiểm tra lại lô hàng còn lại.',
  @ResolvedBy, @CreatedBy, GETDATE(), GETDATE(), GETDATE(), 0
);

GO

-- Insert vào bảng SystemConfiguration
-- Tuổi tối thiểu để đăng ký tài khoản người dùng
INSERT INTO SystemConfiguration (
   Name, Description, MinValue, MaxValue, Unit, IsActive, EffectedDateFrom
)
VALUES (
   'MIN_AGE_FOR_REGISTRATION', N'Tuổi tối thiểu để đăng ký tài khoản', 18, NULL, 'years', 1, GETDATE()
);

GO

-- Set mềm giá trị thuế cho bảng cam kết
INSERT INTO SystemConfiguration 
    (Name, Description, MinValue, MaxValue, Unit, IsActive, EffectedDateFrom)
VALUES 
    ('TAX_RATE_FOR_COMMITMENT', N'Giá trị thuế khi tạo cam kết với nông dân', 0.05, NULL, '%', 1, GETDATE());

INSERT INTO SystemConfiguration 
    (Name, Description, MinValue, MaxValue, Unit, IsActive, EffectedDateFrom)
VALUES 
    ('CULTIVATION_REGISTRATION_CREATION_LIMIT', N'Giới hạn số lần nông dân được phép đăng ký trong cùng một kế hoạch', 3, NULL, 'times', 1, GETDATE());

GO

-- Insert vào bảng SystemConfigurationUsers
-- Lấy ID của tham số "MIN_AGE_FOR_REGISTRATION"
DECLARE @MinAgeConfigID INT = (
   SELECT Id FROM SystemConfiguration WHERE Name = 'MIN_AGE_FOR_REGISTRATION'
);

-- Gán quyền 'manage' cho admin (giả sử admin là Phạm Huỳnh Xuân Đăng)
DECLARE @AdminID UNIQUEIDENTIFIER = (
   SELECT UserID FROM UserAccounts WHERE Email = 'admin@gmail.com'
);

INSERT INTO SystemConfigurationUsers (
   SystemConfigurationID, UserID, PermissionLevel
)
VALUES (
   @MinAgeConfigID, @AdminID, 'manage'
);

GO

-- Insert vào bảng SystemConfigurationUsers
DECLARE @TaxRateConfigID INT = (
   SELECT Id FROM SystemConfiguration WHERE Name = 'TAX_RATE_FOR_COMMITMENT'
);

DECLARE @AdminID UNIQUEIDENTIFIER = (
   SELECT UserID FROM UserAccounts WHERE Email = 'admin@gmail.com'
);

INSERT INTO SystemConfigurationUsers (
   SystemConfigurationID, UserID, PermissionLevel
)
VALUES (
   @TaxRateConfigID, @AdminID, 'manage'
);

GO

DECLARE @CultivationRegistrationLimitConfigID INT = (
   SELECT Id FROM SystemConfiguration WHERE Name = 'CULTIVATION_REGISTRATION_CREATION_LIMIT'
);

DECLARE @AdminID UNIQUEIDENTIFIER = (
   SELECT UserID FROM UserAccounts WHERE Email = 'admin@gmail.com'
);

INSERT INTO SystemConfigurationUsers (
   SystemConfigurationID, UserID, PermissionLevel
)
VALUES (
   @CultivationRegistrationLimitConfigID, @AdminID, 'manage'
);

GO

-- Cải tiến Database
/* ============================================================
   FINAL PATCH (SAFE RERUN): Delivery/Payment tracking + Settlement + Notes
   SQL Server 2016+ (ISJSON/CREATE OR ALTER VIEW). 
   Nếu <2016: BỎ/COMMENT các CHECK dùng ISJSON + thay CREATE OR ALTER VIEW bằng CREATE VIEW.
   ============================================================ */

---------------------------------------------------------------
-- 1) ADD COLUMNS — Contracts
---------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contracts') AND name = 'ContractType')
  ALTER TABLE Contracts ADD ContractType NVARCHAR(50) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contracts') AND name = 'ParentContractID')
  ALTER TABLE Contracts ADD ParentContractID UNIQUEIDENTIFIER NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contracts') AND name = 'PaymentRounds')
  ALTER TABLE Contracts ADD PaymentRounds INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contracts') AND name = 'SettlementStatus')
  ALTER TABLE Contracts ADD SettlementStatus NVARCHAR(30) NOT NULL DEFAULT 'None';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contracts') AND name = 'SettledAt')
  ALTER TABLE Contracts ADD SettledAt DATE NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contracts') AND name = 'SettlementFileURL')
  ALTER TABLE Contracts ADD SettlementFileURL NVARCHAR(255) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contracts') AND name = 'SettlementFilesJson')
  ALTER TABLE Contracts ADD SettlementFilesJson NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contracts') AND name = 'SettlementNote')
  ALTER TABLE Contracts ADD SettlementNote NVARCHAR(MAX) NULL;
GO

---------------------------------------------------------------
-- 2) ADD COLUMNS — ContractDeliveryBatches
---------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ContractDeliveryBatches') AND name = 'ExpectedPaymentDate')
  ALTER TABLE ContractDeliveryBatches ADD ExpectedPaymentDate DATE NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ContractDeliveryBatches') AND name = 'ExpectedPaymentAmount')
  ALTER TABLE ContractDeliveryBatches ADD ExpectedPaymentAmount DECIMAL(18,2) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ContractDeliveryBatches') AND name = 'PaymentStatus')
  ALTER TABLE ContractDeliveryBatches ADD PaymentStatus NVARCHAR(30) NOT NULL DEFAULT 'Planned';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ContractDeliveryBatches') AND name = 'PaidAmount')
  ALTER TABLE ContractDeliveryBatches ADD PaidAmount DECIMAL(18,2) NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ContractDeliveryBatches') AND name = 'LastPaidAt')
  ALTER TABLE ContractDeliveryBatches ADD LastPaidAt DATETIME NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ContractDeliveryBatches') AND name = 'PaymentNote')
  ALTER TABLE ContractDeliveryBatches ADD PaymentNote NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ContractDeliveryBatches') AND name = 'PaymentReceiptFilesJson')
  ALTER TABLE ContractDeliveryBatches ADD PaymentReceiptFilesJson NVARCHAR(MAX) NULL;

-- Delivery window (plan & actual)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ContractDeliveryBatches') AND name = 'ExpectedDeliveryStartDate')
  ALTER TABLE ContractDeliveryBatches ADD ExpectedDeliveryStartDate DATE NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ContractDeliveryBatches') AND name = 'ExpectedDeliveryEndDate')
  ALTER TABLE ContractDeliveryBatches ADD ExpectedDeliveryEndDate DATE NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ContractDeliveryBatches') AND name = 'ActualDeliveryStartDate')
  ALTER TABLE ContractDeliveryBatches ADD ActualDeliveryStartDate DATE NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ContractDeliveryBatches') AND name = 'ActualDeliveryEndDate')
  ALTER TABLE ContractDeliveryBatches ADD ActualDeliveryEndDate DATE NULL;

-- Delivery notes & actual quantity
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ContractDeliveryBatches') AND name = 'DeliveryNoteCode')
  ALTER TABLE ContractDeliveryBatches ADD DeliveryNoteCode VARCHAR(50) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ContractDeliveryBatches') AND name = 'DeliveryNoteFilesJson')
  ALTER TABLE ContractDeliveryBatches ADD DeliveryNoteFilesJson NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ContractDeliveryBatches') AND name = 'ActualDeliveredQuantity')
  ALTER TABLE ContractDeliveryBatches ADD ActualDeliveredQuantity FLOAT NULL;
GO

---------------------------------------------------------------
-- 3) CONSTRAINTS — chỉ tạo khi cột tồn tại
---------------------------------------------------------------
-- FK ParentContract
IF COL_LENGTH('Contracts', 'ParentContractID') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Contracts_ParentContract')
BEGIN
  ALTER TABLE Contracts
  ADD CONSTRAINT FK_Contracts_ParentContract
      FOREIGN KEY (ParentContractID) REFERENCES Contracts(ContractID);
END
GO

-- JSON checks (comment 2 dòng này nếu SQL Server < 2016)
IF COL_LENGTH('Contracts', 'SettlementFilesJson') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Contracts_SettlementFilesJson_IsJson')
BEGIN
  ALTER TABLE Contracts
  ADD CONSTRAINT CK_Contracts_SettlementFilesJson_IsJson
  CHECK (SettlementFilesJson IS NULL OR ISJSON(SettlementFilesJson) = 1);
END

IF COL_LENGTH('ContractDeliveryBatches', 'PaymentReceiptFilesJson') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Batches_PaymentReceiptFilesJson_IsJson')
BEGIN
  ALTER TABLE ContractDeliveryBatches
  ADD CONSTRAINT CK_Batches_PaymentReceiptFilesJson_IsJson
  CHECK (PaymentReceiptFilesJson IS NULL OR ISJSON(PaymentReceiptFilesJson) = 1);
END

IF COL_LENGTH('ContractDeliveryBatches', 'DeliveryNoteFilesJson') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Batches_DeliveryNoteFilesJson_IsJson')
BEGIN
  ALTER TABLE ContractDeliveryBatches
  ADD CONSTRAINT CK_Batches_DeliveryNoteFilesJson_IsJson
  CHECK (DeliveryNoteFilesJson IS NULL OR ISJSON(DeliveryNoteFilesJson) = 1);
END
GO

---------------------------------------------------------------
-- 4) INDEXES (safe)
---------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ContractDeliveryBatches_ContractID' AND object_id = OBJECT_ID('ContractDeliveryBatches'))
  CREATE INDEX IX_ContractDeliveryBatches_ContractID ON ContractDeliveryBatches(ContractID) WHERE IsDeleted = 0;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ContractDeliveryItems_BatchID' AND object_id = OBJECT_ID('ContractDeliveryItems'))
  CREATE INDEX IX_ContractDeliveryItems_BatchID ON ContractDeliveryItems(DeliveryBatchID) WHERE IsDeleted = 0;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Contracts_ParentContractID' AND object_id = OBJECT_ID('Contracts'))
  CREATE INDEX IX_Contracts_ParentContractID ON Contracts(ParentContractID);
GO

---------------------------------------------------------------
-- 5) VIEWS (tạo SAU khi cột đã có)
---------------------------------------------------------------
-- Nếu SQL Server < 2016: thay CREATE OR ALTER VIEW bằng CREATE VIEW (và drop trước nếu tồn tại)
CREATE OR ALTER VIEW v_ContractPaymentProgress AS
SELECT
  c.ContractID,
  c.ContractCode,
  c.TotalValue,
  SUM(CASE WHEN b.IsDeleted = 0 THEN ISNULL(b.PaidAmount,0) ELSE 0 END) AS TotalPaid,
  CASE
    WHEN ISNULL(c.TotalValue,0) = 0 THEN 0
    ELSE TRY_CONVERT(DECIMAL(5,2),
         100.0 * SUM(CASE WHEN b.IsDeleted = 0 THEN ISNULL(b.PaidAmount,0) ELSE 0 END) / c.TotalValue)
  END AS PaidPercent
FROM Contracts c
LEFT JOIN ContractDeliveryBatches b ON b.ContractID = c.ContractID
GROUP BY c.ContractID, c.ContractCode, c.TotalValue;
GO

CREATE OR ALTER VIEW v_BatchPaymentProgress AS
SELECT
  b.DeliveryBatchID,
  b.DeliveryBatchCode,
  b.ContractID,
  b.ExpectedDeliveryStartDate,
  b.ExpectedDeliveryEndDate,
  b.ActualDeliveryStartDate,
  b.ActualDeliveryEndDate,
  b.ExpectedPaymentDate,
  b.ExpectedPaymentAmount,
  b.PaidAmount AS TotalPaidForBatch,
  b.PaymentStatus,
  b.LastPaidAt
FROM ContractDeliveryBatches b
WHERE b.IsDeleted = 0;
GO

---------------------------------------------------------------
-- 6) TRIGGERS (safe)
---------------------------------------------------------------
CREATE OR ALTER TRIGGER TR_Contracts_UpdateTimestamp ON Contracts
AFTER UPDATE AS
BEGIN
  SET NOCOUNT ON;
  UPDATE c SET UpdatedAt = CURRENT_TIMESTAMP
  FROM Contracts c
  INNER JOIN inserted i ON c.ContractID = i.ContractID;
END;
GO

CREATE OR ALTER TRIGGER TR_ContractDeliveryBatches_UpdateTimestamp ON ContractDeliveryBatches
AFTER UPDATE AS
BEGIN
  SET NOCOUNT ON;
  UPDATE b SET UpdatedAt = CURRENT_TIMESTAMP
  FROM ContractDeliveryBatches b
  INNER JOIN inserted i ON b.DeliveryBatchID = i.DeliveryBatchID;
END;

GO

-- Bổ sung metadata để biết rule áp vào entity/field nào, phạm vi nào
ALTER TABLE SystemConfiguration
ADD TargetEntity NVARCHAR(100) NULL,     -- 'ProcessingBatch', 'Shipment',...
    TargetField  NVARCHAR(100) NULL,     -- 'MoisturePercent', 'DefectRate',...
    Operator     NVARCHAR(10)  NULL,     -- '<','<=','>','>=','=','between'
    ScopeType    NVARCHAR(50)  NULL,     -- 'Global','CoffeeType','Factory','Region',...
    ScopeId      UNIQUEIDENTIFIER NULL,  -- ID phạm vi (nếu có)
    Severity     NVARCHAR(20)  NULL,     -- 'Hard' (fail ngay) | 'Soft' (cảnh báo/điểm)
    RuleGroup    NVARCHAR(50)  NULL,     -- Nhóm tiêu chí ('QualityCore','Sensory',...)
    VersionNo    INT           NULL,     -- Phiên bản bộ quy tắc
    CreatedBy    UNIQUEIDENTIFIER NULL,
    UpdatedBy    UNIQUEIDENTIFIER NULL;
GO

-- Index để truy vấn rule đang hiệu lực nhanh
CREATE INDEX IX_SystemConfiguration_Effective
ON SystemConfiguration (TargetEntity, TargetField, ScopeType, ScopeId, EffectedDateFrom, EffectedDateTo)
INCLUDE (IsActive, MinValue, MaxValue, Operator, Severity, RuleGroup, Name, Description);
GO

-- Tránh Name bị trùng khi đang active (tối giản; có thể thay bằng composite theo Target/Scope)
CREATE UNIQUE INDEX UX_SystemConfiguration_Name_Active
ON SystemConfiguration(Name, IsActive, IsDeleted)
WHERE IsActive = 1 AND IsDeleted = 0;

GO

ALTER TABLE ProcessingBatchEvaluations
ADD TotalScore DECIMAL(5,2) NULL,         -- tuỳ chọn (nếu chấm điểm Soft)
    DecisionReason NVARCHAR(255) NULL,    -- tóm tắt lý do
    CriteriaSnapshot NVARCHAR(MAX) NULL;  -- JSON checklist đã tick

GO

-- Seed 10 tiêu chí đánh giá sau sơ chế (green coffee) cho ProcessingBatch
INSERT INTO SystemConfiguration
(Name, Description, MinValue, MaxValue, Unit, IsActive, EffectedDateFrom,
 TargetEntity, TargetField, Operator, ScopeType, ScopeId, Severity, RuleGroup, VersionNo)
VALUES
-- 1) Độ ẩm tối đa ≤ 12%
('PB.MoisturePercent', N'Độ ẩm tối đa (green coffee)', NULL, 12.00, N'%', 1, GETDATE(),
 'ProcessingBatch', 'MoisturePercent', '<=', 'Global', NULL, 'Hard', 'QualityCore', 1),

-- 2) Water Activity ≤ 0.70
('PB.WaterActivity', N'Nước tự do (aw) tối đa', NULL, 0.70, N'aw', 1, GETDATE(),
 'ProcessingBatch', 'WaterActivity', '<=', 'Global', NULL, 'Hard', 'QualityCore', 1),

-- 3) Tỉ lệ lỗi tổng ≤ 3%
('PB.DefectRate', N'Tỉ lệ lỗi tổng tối đa', NULL, 3.00, N'%', 1, GETDATE(),
 'ProcessingBatch', 'DefectRate', '<=', 'Global', NULL, 'Hard', 'Defects', 1),

-- 4) Tạp chất (foreign matter) = 0 g/kg
('PB.ForeignMatter', N'Tạp chất (vỏ, đá, kim loại...) phải bằng 0', 0.00, 0.00, N'g/kg', 1, GETDATE(),
 'ProcessingBatch', 'ForeignMatter', '=', 'Global', NULL, 'Hard', 'Safety', 1),

-- 5) Không có mùi lạ (Off-odor) = 0
('PB.OffOdor', N'Không có mùi lạ', 0.00, 0.00, N'flag', 1, GETDATE(),
 'ProcessingBatch', 'HasOffOdor', '=', 'Global', NULL, 'Soft', 'Sensory', 1),

-- 6) Hạt đen (black beans) ≤ 0.5%
('PB.BlackBeansRate', N'Tỉ lệ hạt đen tối đa', NULL, 0.50, N'%', 1, GETDATE(),
 'ProcessingBatch', 'BlackBeansRate', '<=', 'Global', NULL, 'Hard', 'Defects', 1),

-- 7) Hạt vỡ (broken/chipped) ≤ 3%
('PB.BrokenBeansRate', N'Tỉ lệ hạt vỡ tối đa', NULL, 3.00, N'%', 1, GETDATE(),
 'ProcessingBatch', 'BrokenBeansRate', '<=', 'Global', NULL, 'Soft', 'Defects', 1),

-- 8) Hạt sâu (insect-damaged) ≤ 1%
('PB.InsectDamagedRate', N'Tỉ lệ hạt sâu tối đa', NULL, 1.00, N'%', 1, GETDATE(),
 'ProcessingBatch', 'InsectDamagedRate', '<=', 'Global', NULL, 'Hard', 'Defects', 1),

-- 9) Độ đồng đều cỡ sàng: ≥ 80% trên sàng 16
('PB.Screen16UpPct', N'≥ 80% hạt trên sàng 16', 80.00, NULL, N'%', 1, GETDATE(),
 'ProcessingBatch', 'Screen16UpPct', '>=', 'Global', NULL, 'Soft', 'Grading', 1),

-- 10) Hạt còn vỏ trấu (parchment) ≤ 0.5%
('PB.ParchmentRate', N'Tỉ lệ hạt còn vỏ trấu tối đa', NULL, 0.50, N'%', 1, GETDATE(),
 'ProcessingBatch', 'ParchmentRate', '<=', 'Global', NULL, 'Hard', 'Defects', 1);
