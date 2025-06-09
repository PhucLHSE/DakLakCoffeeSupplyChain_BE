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
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP        -- Ngày cập nhật
);

GO

-- Table Users
CREATE TABLE UserAccounts (
  UserID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),          -- ID người dùng
  UserCode VARCHAR(20) UNIQUE,                                  -- Auto-gen như USR-2024-0012 để phục vụ QR/mã truy xuất nếu cần hiển thị công khai.
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
  LoginType NVARCHAR(20) DEFAULT 'local',                       -- Phương thức login: local, google,...
  Status NVARCHAR(20) DEFAULT 'active',                         -- Trạng thái tài khoản
  RoleID INT NOT NULL,                                          -- Vai trò người dùng
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,        -- Ngày cập nhật

  -- Foreign Keys
  CONSTRAINT FK_UserAccounts_RoleID 
      FOREIGN KEY (RoleID) REFERENCES Roles(RoleID)
);

GO

-- Table BusinessManagers
CREATE TABLE BusinessManagers (
  ManagerID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
  UserID UNIQUEIDENTIFIER NOT NULL,
  ManagerCode VARCHAR(20) UNIQUE,                               -- BM-2024-0012
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
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP         -- Ngày cập nhật

  -- Foreign Keys
  CONSTRAINT FK_BusinessManagers_UserID 
      FOREIGN KEY (UserID) REFERENCES UserAccounts(UserID)
);

GO

-- Bảng lưu thông tin bổ sung chỉ dành riêng cho người dùng có vai trò là Farmer
CREATE TABLE Farmers (
  FarmerID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),             -- Mã định danh riêng cho vai trò Farmer
  FarmerCode VARCHAR(20) UNIQUE,                                     -- FRM-2024-0007
  UserID UNIQUEIDENTIFIER NOT NULL,                                  -- Liên kết đến tài khoản người dùng chung
  FarmLocation NVARCHAR(255),                                        -- Địa điểm canh tác chính
  FarmSize FLOAT,                                                    -- Diện tích nông trại (hecta)
  CertificationStatus NVARCHAR(100),                                 -- Trạng thái chứng nhận: VietGAP, Organic,...
  CertificationURL NVARCHAR(255),                                    -- Link đến tài liệu chứng nhận
  IsVerified BIT DEFAULT 0,                                          -- Tài khoản nông dân đã xác minh chưa
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,             -- Thời điểm tạo bản ghi
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,             -- Thời điểm cập nhật cuối

  -- Foreign Keys
  CONSTRAINT FK_Farmers_UserID 
      FOREIGN KEY (UserID) REFERENCES UserAccounts(UserID)
);

GO

-- Thông tin chuyên gia nông nghiệp
CREATE TABLE AgriculturalExperts (
    ExpertID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),             -- ID riêng của chuyên gia
	ExpertCode VARCHAR(20) UNIQUE,                                     -- EXP-2024-012
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

	-- Foreign Keys
    CONSTRAINT FK_AgriculturalExperts_UserID 
	    FOREIGN KEY (UserID) REFERENCES UserAccounts(UserID)
);

GO

-- Table BusinessBuyers
CREATE TABLE BusinessBuyers (
  BuyerID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),    
  BuyerCode VARCHAR(20) UNIQUE,                                    -- BUY-2024-025
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
  CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
  UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

GO

-- ProcurementPlans – Bảng kế hoạch thu mua tổng quan
CREATE TABLE ProcurementPlans (
    PlanID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),                               -- ID kế hoạch thu mua
	PlanCode VARCHAR(20) UNIQUE,                                                       -- PLAN-2025-0001
    Title NVARCHAR(100) NOT NULL,                                                      -- Tên kế hoạch: "Thu mua cà phê Arabica 2025"
    Description NVARCHAR(MAX),                                                         -- Mô tả yêu cầu và thông tin bổ sung
    TotalQuantity FLOAT,                                                               -- Tổng sản lượng cần thu mua (kg hoặc tấn)
    CreatedBy UNIQUEIDENTIFIER NOT NULL,                                               -- Người tạo kế hoạch (doanh nghiệp)
    StartDate DATE,                                                                    -- Ngày bắt đầu nhận đăng ký
    EndDate DATE,                                                                      -- Ngày kết thúc nhận đăng ký
    Status NVARCHAR(50) DEFAULT 'draft',                                               -- Tình trạng: draft, open, closed, cancelled
    ProgressPercentage FLOAT CHECK (ProgressPercentage BETWEEN 0 AND 100) DEFAULT 0.0, -- % hoàn thành
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,                             -- Ngày tạo
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,                             -- Ngày cập nhật

	-- Foreign Keys
    CONSTRAINT FK_ProcurementPlans_CreatedBy 
	    FOREIGN KEY (CreatedBy) REFERENCES BusinessManagers(ManagerID)
);

GO

-- ProcurementPlansDetails – Bảng chi tiết từng loại cây trong kế hoạch
CREATE TABLE ProcurementPlansDetails (
    PlanDetailsID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),                        -- ID chi tiết kế hoạch
	PlanDetailCode VARCHAR(20) UNIQUE,                                                 -- PLD-2025-A001
    PlanID UNIQUEIDENTIFIER NOT NULL,                                                  -- FK đến bảng ProcurementPlans
	CoffeeTypeID UNIQUEIDENTIFIER NOT NULL,                                            -- Liên kết loại cà phê chính xác
    CropType NVARCHAR(100) NOT NULL,                                                   -- Loại cây trồng: Arabica, Robusta,...
    TargetQuantity FLOAT,                                                              -- Sản lượng mong muốn (kg hoặc tấn)
    TargetRegion NVARCHAR(100),                                                        -- Khu vực thu mua chính: ví dụ "Cư M’gar"
    MinimumRegistrationQuantity FLOAT,                                                 -- Số lượng tối thiểu để nông dân đăng ký (kg)
    BeanSize NVARCHAR(50),                                                             -- Kích thước hạt (ví dụ: screen 16–18)
    BeanColor NVARCHAR(50),                                                            -- Màu hạt
    MoistureContent FLOAT,                                                             -- Hàm lượng ẩm
    DefectRate FLOAT,                                                                  -- Tỷ lệ lỗi hạt cho phép
    MinPriceRange FLOAT,                                                               -- Giá tối thiểu có thể thương lượng
    MaxPriceRange FLOAT,                                                               -- Giá tối đa có thể thương lượng
    Note NVARCHAR(MAX),                                                                -- Ghi chú bổ sung
    BeanColorImageUrl NVARCHAR(255),                                                   -- Link ảnh mẫu hạt
    ProgressPercentage FLOAT CHECK (ProgressPercentage BETWEEN 0 AND 100) DEFAULT 0.0, -- % hoàn thành chi tiết
    Status NVARCHAR(50) DEFAULT 'active',                                              -- Trạng thái: active, closed, disabled
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,                             -- Ngày tạo
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,                             -- Ngày cập nhật

	-- Foreign Keys
    CONSTRAINT FK_ProcurementPlansDetails_PlanID 
	    FOREIGN KEY (PlanID) REFERENCES ProcurementPlans(PlanID),

	CONSTRAINT FK_ProcurementPlansDetails_CoffeeTypeID 
        FOREIGN KEY (CoffeeTypeID) REFERENCES CoffeeTypes(CoffeeTypeID)
);

GO

-- CultivationRegistrations – Đơn đăng ký trồng cây của Farmer
CREATE TABLE CultivationRegistrations (
    RegistrationID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),   -- ID đơn đăng ký
	RegistrationCode VARCHAR(20) UNIQUE,                           -- REG-2025-0045
    PlanID UNIQUEIDENTIFIER NOT NULL,                              -- Kế hoạch thu mua
    FarmerID UNIQUEIDENTIFIER NOT NULL,                            -- Nông dân nộp đơn
    RegisteredArea FLOAT,                                          -- Diện tích đăng ký (hecta)
    RegisteredAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,      -- Thời điểm nộp đơn
    WantedPrice FLOAT,                                             -- Mức giá mong muốn
    Status NVARCHAR(50) DEFAULT 'pending',                         -- Trạng thái: pending, approved,...
    Note NVARCHAR(MAX),                                            -- Ghi chú từ farmer
    SystemNote NVARCHAR(MAX),                                      -- Ghi chú từ hệ thống
    IsDeleted BIT DEFAULT 0,                                       -- Xóa mềm
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
    EstimatedYield FLOAT,                                                         -- Sản lượng ước tính (kg)
    ExpectedHarvestStart DATE,                                                    -- Ngày bắt đầu thu hoạch
    ExpectedHarvestEnd DATE,                                                      -- Ngày kết thúc thu hoạch
    Status NVARCHAR(50) DEFAULT 'pending',                                        -- pending, approved,...
    Note NVARCHAR(MAX),                                                           -- Ghi chú từ farmer
    SystemNote NVARCHAR(MAX),                                                     -- Ghi chú hệ thống
    ApprovedBy UNIQUEIDENTIFIER,                                                  -- Người duyệt (nullable)
    ApprovedAt DATETIME,                                                          -- Thời gian duyệt
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

	-- Foreign Keys
    CONSTRAINT FK_CultivationRegistrationsDetail_CultivationRegistrations 
        FOREIGN KEY (RegistrationID) REFERENCES CultivationRegistrations(RegistrationID),

    CONSTRAINT FK_CultivationRegistrationsDetail_ProcurementPlansDetails 
        FOREIGN KEY (PlanDetailID) REFERENCES ProcurementPlansDetails(PlanDetailsID),

    CONSTRAINT FK_CultivationRegistrationsDetail_ApprovedBy 
        FOREIGN KEY (ApprovedBy) REFERENCES BusinessManagers(ManagerID)
);

GO

-- FarmingCommitments – Cam kết chính thức giữa Farmer và hệ thống
CREATE TABLE FarmingCommitments (
    CommitmentID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),         -- ID cam kết
	CommitmentCode VARCHAR(20) UNIQUE,                                 -- COMMIT-2025-0038
    RegistrationDetailID UNIQUEIDENTIFIER NOT NULL,                    -- FK đến chi tiết đơn đã duyệt
    PlanID UNIQUEIDENTIFIER NOT NULL,                                  -- FK đến kế hoạch tổng thể
    PlanDetailID UNIQUEIDENTIFIER NOT NULL,                            -- FK đến loại cây cụ thể
    FarmerID UNIQUEIDENTIFIER NOT NULL,                                -- Nông dân cam kết
    ConfirmedPrice FLOAT,                                              -- Giá xác nhận mua
    CommittedQuantity FLOAT,                                           -- Khối lượng cam kết
    EstimatedDeliveryStart DATE,                                       -- Ngày giao hàng dự kiến bắt đầu
    EstimatedDeliveryEnd DATE,                                         -- Ngày giao hàng dự kiến kết thúc
    CommitmentDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,        -- Ngày xác lập cam kết
    ApprovedBy UNIQUEIDENTIFIER,                                       -- Người duyệt
    ApprovedAt DATETIME,                                               -- Ngày duyệt
    Status NVARCHAR(50) DEFAULT 'active',                              -- Trạng thái cam kết
    RejectionReason NVARCHAR(MAX),                                     -- Lý do từ chối (nếu có)
    Note NVARCHAR(MAX),                                                -- Ghi chú thêm
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    -- Foreign Keys
    CONSTRAINT FK_FarmingCommitments_RegistrationDetailID 
	    FOREIGN KEY (RegistrationDetailID) REFERENCES CultivationRegistrationsDetail(CultivationRegistrationDetailID),

    CONSTRAINT FK_FarmingCommitments_PlanID 
	    FOREIGN KEY (PlanID) REFERENCES ProcurementPlans(PlanID),

    CONSTRAINT FK_FarmingCommitments_PlanDetailID 
	    FOREIGN KEY (PlanDetailID) REFERENCES ProcurementPlansDetails(PlanDetailsID),

    CONSTRAINT FK_FarmingCommitments_FarmerID 
	    FOREIGN KEY (FarmerID) REFERENCES Farmers(FarmerID),

    CONSTRAINT FK_FarmingCommitments_ApprovedBy 
	    FOREIGN KEY (ApprovedBy) REFERENCES BusinessManagers(ManagerID)
);

GO

-- CropSeasons – Quản lý mùa vụ trồng
CREATE TABLE CropSeasons (
    CropSeasonID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),          -- ID của một mùa vụ
	CropSeasonCode VARCHAR(20) UNIQUE,                                  -- SEASON-2025-0021
    RegistrationID UNIQUEIDENTIFIER NOT NULL,                           -- Liên kết đơn đăng ký trồng
    FarmerID UNIQUEIDENTIFIER NOT NULL,                                 -- Nông dân tạo mùa vụ
    CommitmentID UNIQUEIDENTIFIER NOT NULL,                             -- Cam kết từ doanh nghiệp
    SeasonName NVARCHAR(100),                                           -- Tên mùa vụ (vd: Mùa vụ 2025)
    Area FLOAT,                                                         -- Diện tích canh tác (ha)
    StartDate DATE,                                                     -- Ngày bắt đầu
    EndDate DATE,                                                       -- Ngày kết thúc
    Note NVARCHAR(MAX),                                                 -- Ghi chú mùa vụ
    Status NVARCHAR(50) DEFAULT 'active',                               -- Trạng thái: active, paused,...
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,              -- Thời điểm tạo
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,              -- Thời điểm cập nhật

    -- Foreign Keys
    CONSTRAINT FK_CropSeasons_RegistrationID 
	    FOREIGN KEY (RegistrationID) REFERENCES CultivationRegistrations(RegistrationID),

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
    CoffeeTypeID UNIQUEIDENTIFIER NOT NULL,                            -- Liên kết loại cà phê chính xác
    ExpectedHarvestStart DATE,                                         -- Ngày bắt đầu thu hoạch dự kiến
    ExpectedHarvestEnd DATE,                                           -- Ngày kết thúc thu hoạch dự kiến
    EstimatedYield FLOAT,                                              -- Sản lượng dự kiến
    ActualYield FLOAT,                                                 -- Sản lượng thực tế
    AreaAllocated FLOAT,                                               -- Diện tích cho loại này
    PlannedQuality NVARCHAR(50),                                       -- Chất lượng dự kiến
    QualityGrade NVARCHAR(50),                                         -- Chất lượng thực tế
    Status NVARCHAR(50) DEFAULT 'planned',                             -- planned, in_progress, completed
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,             -- Ngày tạo
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

	-- Foreign Keys
    CONSTRAINT FK_CropSeasonDetails_CropSeasonID 
	    FOREIGN KEY (CropSeasonID) REFERENCES CropSeasons(CropSeasonID),

	CONSTRAINT FK_CropSeasonDetails_CoffeeTypeID 
        FOREIGN KEY (CoffeeTypeID) REFERENCES CoffeeTypes(CoffeeTypeID)
);

GO

-- CropStages – Danh mục các giai đoạn mùa vụ)
CREATE TABLE CropStages (
    StageID INT PRIMARY KEY IDENTITY(1,1), 
    StageCode VARCHAR(50) UNIQUE NOT NULL,    -- Mã giai đoạn: planting, harvesting,...
    StageName VARCHAR(100),                   -- Tên hiển thị: Gieo trồng, Ra hoa,...
    Description NVARCHAR(MAX),                -- Mô tả chi tiết giai đoạn
    OrderIndex INT,                           -- Thứ tự giai đoạn
	CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
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

	-- Foreign Keys
    CONSTRAINT FK_CropProgresses_CropSeasonDetailID 
        FOREIGN KEY (CropSeasonDetailID) REFERENCES CropSeasonDetails(DetailID),

    CONSTRAINT FK_CropProgresses_UpdatedBy 
        FOREIGN KEY (UpdatedBy) REFERENCES Farmers(FarmerID),

    CONSTRAINT FK_CropProgresses_StageID 
        FOREIGN KEY (StageID) REFERENCES CropStages(StageID)
);

GO

-- Danh mục phương pháp sơ chế (natural, washed,...)
CREATE TABLE ProcessingMethods (
  MethodID INT PRIMARY KEY IDENTITY(1,1),                      -- ID nội bộ
  MethodCode VARCHAR(50) UNIQUE NOT NULL,                      -- Mã code định danh: 'natural', 'washed'...
  Name NVARCHAR(100) NOT NULL,                                 -- Tên hiển thị: 'Sơ chế khô'
  Description NVARCHAR(MAX),                                   -- Mô tả chi tiết
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,       -- Ngày tạo dòng dữ liệu
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP        -- Ngày cập nhật cuối (update thủ công khi chỉnh sửa)
);

GO

-- ProcessingStages – Danh mục chuẩn các bước trong sơ chế
CREATE TABLE ProcessingStages (
  StageID INT PRIMARY KEY IDENTITY(1,1),                         -- ID bước xử lý
  MethodID INT NOT NULL,                                         -- Phương pháp sơ chế áp dụng (FK)
  StageCode VARCHAR(50) NOT NULL,                                -- Mã bước: 'drying', 'fermentation'...
  StageName NVARCHAR(100) NOT NULL,                              -- Tên hiển thị: 'Phơi', 'Lên men'
  Description NVARCHAR(MAX),                                     -- Mô tả chi tiết
  OrderIndex INT NOT NULL,                                       -- Thứ tự thực hiện
  IsRequired BIT DEFAULT 1,                                      -- Bước này có bắt buộc không?
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,         -- Ngày tạo dòng dữ liệu
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP          -- Ngày cập nhật cuối (update thủ công khi chỉnh sửa)

  CONSTRAINT FK_ProcessingStages_Method 
    FOREIGN KEY (MethodID) REFERENCES ProcessingMethods(MethodID)
);

GO

-- Hồ sơ lô sơ chế (Batch sơ chế của từng Farmer)
CREATE TABLE ProcessingBatches (
  BatchID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),        -- ID định danh lô sơ chế
  SystemBatchCode  VARCHAR(20) UNIQUE,                         -- BATCH-2025-0010
  CoffeeTypeID UNIQUEIDENTIFIER NOT NULL,                      -- Loại cà phê được sơ chế
  CropSeasonID UNIQUEIDENTIFIER NOT NULL,                      -- FK đến mùa vụ
  FarmerID UNIQUEIDENTIFIER NOT NULL,                          -- FK đến nông dân thực hiện sơ chế
  BatchCode NVARCHAR(50) NOT NULL,                             -- Mã lô do người dùng tự đặt
  MethodID INT NOT NULL,                                       -- Mã phương pháp sơ chế (FK)
  InputQuantity FLOAT NOT NULL,                                -- Số lượng đầu vào (kg quả cà phê)
  InputUnit NVARCHAR(20) DEFAULT 'kg',                         -- Đơn vị (thường là kg)
  CreatedAt DATETIME,                                          -- Ngày tạo hồ sơ
  UpdatedAt DATETIME,                                          -- Ngày câp nhật hồ sơ
  Status NVARCHAR(50),                                         -- Trạng thái: pending, processing, completed

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

-- Ghi nhận tiến trình từng bước trong sơ chế (drying, dehulling...)
CREATE TABLE ProcessingBatchProgresses (
  ProgressID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),     -- ID từng bước sơ chế
  BatchID UNIQUEIDENTIFIER NOT NULL,                           -- FK tới lô sơ chế
  StepIndex INT NOT NULL,                                      -- Thứ tự tiến trình trong batch
  StageID INT NOT NULL,                                        -- FK đến bảng chuẩn `ProcessingStages`
  StageDescription NVARCHAR(MAX),                              -- Mô tả chi tiết quá trình
  ProgressDate DATE,                                           -- Ngày thực hiện bước
  OutputQuantity FLOAT,                                        -- Sản lượng thu được (nếu có)
  OutputUnit NVARCHAR(20) DEFAULT 'kg',                        -- Đơn vị sản lượng
  UpdatedBy UNIQUEIDENTIFIER NOT NULL,                         -- Người ghi nhận bước này (Farmer / Manager)
  PhotoURL VARCHAR(255),                                       -- Link ảnh (nếu có)
  VideoURL VARCHAR(255),                                       -- Link video (tùy chọn)
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,       -- Thời điểm tạo
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,       -- Thời điểm cập nhật lần cuối

  -- FOREIGN KEYS
  CONSTRAINT FK_BatchProgresses_Batch 
      FOREIGN KEY (BatchID) REFERENCES ProcessingBatches(BatchID),

  CONSTRAINT FK_BatchProgresses_ProcessingStages 
      FOREIGN KEY (StageID) REFERENCES ProcessingStages(StageID)
);

GO

-- Ghi nhận thông số kỹ thuật từng bước (nếu có nhập tay)
CREATE TABLE ProcessingParameters (
  ParameterID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),    -- ID thông số
  ProgressID UNIQUEIDENTIFIER NOT NULL,                        -- FK tới bước sơ chế cụ thể
  ParameterName NVARCHAR(100),                                 -- Tên thông số: humidity, temperature...
  ParameterValue NVARCHAR(100) NULL,                           -- Giá trị đo được
  Unit NVARCHAR(20) NULL,                                      -- Đơn vị: %, °C,...
  RecordedAt DATETIME,                                         -- Ngày ghi nhận
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,       -- Ngày tạo yêu cầu
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,       -- Ngày cập nhật cuối

  -- FOREIGN KEYS
  CONSTRAINT FK_ProcessingParameters_Progress 
      FOREIGN KEY (ProgressID) REFERENCES ProcessingBatchProgresses(ProgressID)
);

GO

-- GeneralFarmerReports – Bảng báo cáo sự cố chung từ Farmer
CREATE TABLE GeneralFarmerReports (
    ReportID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),                  -- ID báo cáo chung
	ReportCode VARCHAR(20) UNIQUE,                                          -- REP-2025-0003
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
    ReportID UNIQUEIDENTIFIER NOT NULL,                                -- FK tới báo cáo chung
    ExpertID UNIQUEIDENTIFIER NOT NULL,                                -- Ai phản hồi
    ResponseType VARCHAR(50),                                          -- preventive, corrective, observation
    AdviceSource VARCHAR(50) DEFAULT 'human',                          -- human hoặc AI
    AdviceText NVARCHAR(MAX),                                          -- Nội dung tư vấn
    AttachedFileUrl VARCHAR(255),
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    -- Foreign Keys
    CONSTRAINT FK_GeneralExpertAdvice_Report 
        FOREIGN KEY (ReportID) REFERENCES GeneralFarmerReports(ReportID),

    CONSTRAINT FK_GeneralExpertAdvice_Expert 
        FOREIGN KEY (ExpertID) REFERENCES AgriculturalExperts(ExpertID)
);

-- ProcessingBatchEvaluations – Đánh giá chất lượng batch
CREATE TABLE ProcessingBatchEvaluations (
  EvaluationID UNIQUEIDENTIFIER PRIMARY KEY,                    -- ID đánh giá
  EvaluationCode VARCHAR(20) UNIQUE,                            -- EVAL-2025-0048
  BatchID UNIQUEIDENTIFIER NOT NULL,                            -- FK tới batch
  EvaluatedBy UNIQUEIDENTIFIER,                                 -- Ai đánh giá (Expert/Manager)
  EvaluationResult NVARCHAR(50),                                -- Kết quả: Pass, Fail, Rework
  Comments NVARCHAR(MAX),                                       -- Nhận xét chi tiết
  EvaluatedAt DATETIME,                                         -- Ngày đánh giá
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,        -- Ngày tạo dòng dữ liệu
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP         -- Ngày cập nhật cuối (update thủ công khi chỉnh sửa)

  -- FOREIGN KEYS
  CONSTRAINT FK_Evaluations_Batch 
    FOREIGN KEY (BatchID) REFERENCES ProcessingBatches(BatchID)
);

GO

-- ProcessingBatchWastes – Ghi nhận phế phẩm từng bước sơ chế
CREATE TABLE ProcessingBatchWastes (
  WasteID UNIQUEIDENTIFIER PRIMARY KEY,                         -- ID dòng phế phẩm
  WasteCode VARCHAR(20) UNIQUE,                                 -- WASTE-2025-0012
  ProgressID UNIQUEIDENTIFIER NOT NULL,                         -- FK tới bước gây ra phế phẩm
  WasteType NVARCHAR(100),                                      -- Loại phế phẩm: vỏ quả, hạt lép...
  Quantity FLOAT,                                               -- Khối lượng
  Unit NVARCHAR(20),                                            -- Đơn vị: kg, g, pcs...
  Note NVARCHAR(MAX),                                           -- Ghi chú nếu có
  RecordedAt DATETIME,                                          -- Thời điểm ghi nhận
  RecordedBy UNIQUEIDENTIFIER,                                  -- Ai ghi nhận (Farmer/Manager)
  IsDisposed BIT DEFAULT 0,                                     -- Cờ đánh dấu đã được xử lý
  DisposedAt DATETIME,                                          -- Ngày xử lý gần nhất (nếu có)
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,        -- Ngày tạo dòng dữ liệu
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP         -- Ngày cập nhật cuối (update thủ công khi chỉnh sửa)

  -- FOREIGN KEYS
  CONSTRAINT FK_Wastes_Progress 
    FOREIGN KEY (ProgressID) REFERENCES ProcessingBatchProgresses(ProgressID)
);

GO

-- ProcessingWasteDisposals – Ghi nhận cách xử lý phế phẩm
CREATE TABLE ProcessingWasteDisposals (
  DisposalID UNIQUEIDENTIFIER PRIMARY KEY,                -- ID xử lý phế phẩm
  DisposalCode VARCHAR(20) UNIQUE,                        -- DISP-2025-0005
  WasteID UNIQUEIDENTIFIER NOT NULL,                      -- FK tới dòng phế phẩm
  DisposalMethod NVARCHAR(100) NOT NULL,                  -- Phương pháp xử lý: compost, sell, discard
  HandledBy UNIQUEIDENTIFIER,                             -- Ai thực hiện xử lý
  HandledAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,  -- Thời điểm xử lý
  Notes NVARCHAR(MAX),                                    -- Ghi chú thêm nếu có
  IsSold BIT DEFAULT 0,                                   -- Đã bán lại không?
  Revenue MONEY,                                          -- Nếu có bán, ghi nhận doanh thu
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,  -- Ngày tạo yêu cầu
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,  -- Ngày cập nhật cuối

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
    Capacity FLOAT,                                                 -- Dung lượng tối đa (kg, tấn...)
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,          -- Ngày tạo
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,          -- Ngày cập nhật

    -- FOREIGN KEY
    CONSTRAINT FK_Warehouses_Manager 
        FOREIGN KEY (ManagerID) REFERENCES BusinessManagers(ManagerID)
);

GO

-- BusinessStaffs – Thông tin nhân viên doanh nghiệp
CREATE TABLE BusinessStaffs (
  StaffID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),          -- Mã định danh riêng của nhân viên
  StaffCode VARCHAR(20) UNIQUE,                                  -- STAFF-2025-0007
  UserID UNIQUEIDENTIFIER NOT NULL UNIQUE,                       -- FK đến bảng Users (1:1)
  SupervisorID UNIQUEIDENTIFIER,                                 -- Người quản lý trực tiếp (nếu có) – có thể là Manager
  Position NVARCHAR(100),                                        -- Chức danh: Thủ kho, Kế toán kho,...
  Department NVARCHAR(100),                                      -- Bộ phận công tác: "Kho Đắk Lắk"
  AssignedWarehouseID UNIQUEIDENTIFIER,                          -- Gắn với kho được phân công (nếu có)
  IsActive BIT DEFAULT 1,                                        -- Nhân viên còn đang làm việc?
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,         -- Ngày tạo bản ghi
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,         -- Ngày cập nhật gần nhất

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
	InventoryCode VARCHAR(20) UNIQUE,                               -- INV-2025-0056
    WarehouseID UNIQUEIDENTIFIER NOT NULL,                          -- Gắn với kho cụ thể
    BatchID UNIQUEIDENTIFIER NOT NULL,                              -- Gắn với mẻ sơ chế (Batch)
    Quantity FLOAT NOT NULL,                                        -- Số lượng hiện tại trong kho
    Unit NVARCHAR(20) DEFAULT 'kg',                                 -- Đơn vị tính (kg, tấn...)
	CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,          -- Ngày tạo
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,          -- Thời điểm cập nhật

    -- FOREIGN KEYS
    CONSTRAINT FK_Inventories_Warehouse 
        FOREIGN KEY (WarehouseID) REFERENCES Warehouses(WarehouseID),

    CONSTRAINT FK_Inventories_Batch 
        FOREIGN KEY (BatchID) REFERENCES ProcessingBatches(BatchID)
);

GO

-- InventoryLogs – Lịch sử thay đổi tồn kho
CREATE TABLE InventoryLogs (
    LogID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),             -- Mã nhật ký
    InventoryID UNIQUEIDENTIFIER NOT NULL,                          -- Gắn với dòng tồn kho nào
    ActionType NVARCHAR(50) NOT NULL,                               -- Loại hành động: increase, decrease, correction
    QuantityChanged FLOAT NOT NULL,                                 -- Lượng thay đổi (+/-)
    UpdatedBy UNIQUEIDENTIFIER,                                     -- Ai thực hiện thay đổi
    TriggeredBySystem BIT DEFAULT 0,                                -- Có phải hệ thống tự động ghi không
    Note NVARCHAR(MAX),                                             -- Ghi chú chi tiết
    LoggedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,           -- Thời điểm ghi log

    -- FOREIGN KEY
    CONSTRAINT FK_InventoryLogs_Inventory 
        FOREIGN KEY (InventoryID) REFERENCES Inventories(InventoryID)
);

GO

-- WarehouseInboundRequests – Yêu cầu nhập kho từ Farmer
CREATE TABLE WarehouseInboundRequests (
  InboundRequestID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),    -- Mã yêu cầu nhập kho,
  InboundRequestCode VARCHAR(20) UNIQUE,                            -- INREQ-2025-0008
  BatchID UNIQUEIDENTIFIER NOT NULL,                                -- Gắn với mẻ sơ chế
  FarmerID UNIQUEIDENTIFIER NOT NULL,                               -- Người gửi yêu cầu (Farmer)
  BusinessStaffID UNIQUEIDENTIFIER NOT NULL,                        -- Người đại diện doanh nghiệp nhận
  RequestedQuantity FLOAT,                                          -- Sản lượng yêu cầu giao (sau sơ chế)
  PreferredDeliveryDate DATE,                                       -- Ngày giao hàng mong muốn
  ActualDeliveryDate DATE,                                          -- Ngày giao thực tế (khi nhận thành công)
  Status NVARCHAR(50) DEFAULT 'pending',                            -- Trạng thái: pending, approved, rejected, completed
  Note NVARCHAR(MAX),                                               -- Ghi chú thêm từ Farmer
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,            -- Ngày tạo
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,            -- Ngày cập nhật cuối

  -- FOREIGN KEYS
  CONSTRAINT FK_WarehouseInboundRequests_Batch 
    FOREIGN KEY (BatchID) REFERENCES ProcessingBatches(BatchID),

  CONSTRAINT FK_WarehouseInboundRequests_Farmer 
    FOREIGN KEY (FarmerID) REFERENCES Farmers(FarmerID),

  CONSTRAINT FK_WarehouseInboundRequests_Manager 
    FOREIGN KEY (BusinessStaffID) REFERENCES BusinessStaffs(StaffID)
);

GO

-- WarehouseReceipts – Phiếu nhập kho
CREATE TABLE WarehouseReceipts (
  ReceiptID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),           -- Mã phiếu nhập kho
  ReceiptCode VARCHAR(20) UNIQUE,                                   -- RECEIPT-2025-0145
  InboundRequestID UNIQUEIDENTIFIER NOT NULL,                       -- Gắn với yêu cầu nhập kho
  WarehouseID UNIQUEIDENTIFIER NOT NULL,                            -- Kho tiếp nhận
  BatchID UNIQUEIDENTIFIER NOT NULL,                                -- Mẻ cà phê
  ReceivedBy UNIQUEIDENTIFIER NOT NULL,                             -- Nhân viên kho tiếp nhận
  LotCode NVARCHAR(100),                                            -- Mã lô nội bộ (nếu cần)
  ReceivedQuantity FLOAT,                                           -- Sản lượng thực nhận
  ReceivedAt DATETIME,                                              -- Thời điểm tiếp nhận
  Note NVARCHAR(MAX),                                               -- Ghi chú kiểm tra hàng
  QRCodeURL NVARCHAR(255),                                          -- Link QR truy xuất

  -- FOREIGN KEYS
  CONSTRAINT FK_WarehouseReceipts_Request 
    FOREIGN KEY (InboundRequestID) REFERENCES WarehouseInboundRequests(InboundRequestID),

  CONSTRAINT FK_WarehouseReceipts_Warehouse 
    FOREIGN KEY (WarehouseID) REFERENCES Warehouses(WarehouseID),

  CONSTRAINT FK_WarehouseReceipts_Batch 
    FOREIGN KEY (BatchID) REFERENCES ProcessingBatches(BatchID),

  CONSTRAINT FK_WarehouseReceipts_Receiver 
    FOREIGN KEY (ReceivedBy) REFERENCES BusinessStaffs(StaffID)
);

GO

-- Products – Thông tin sản phẩm
CREATE TABLE Products (
  ProductID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),           -- Mã sản phẩm duy nhất
  ProductCode VARCHAR(20) UNIQUE,                                   -- PROD-2025-0101
  ProductName NVARCHAR(100) NOT NULL,                               -- Tên thương mại sản phẩm
  Description NVARCHAR(MAX),                                        -- Mô tả chi tiết sản phẩm
  UnitPrice FLOAT,                                                  -- Giá bán B2B (VNĐ/kg)
  QuantityAvailable FLOAT,                                          -- Số lượng còn lại (kg)
  Unit NVARCHAR(20) DEFAULT 'kg',                                   -- Đơn vị tính
  CreatedBy UNIQUEIDENTIFIER NOT NULL,                              -- Người tạo sản phẩm
  BatchID UNIQUEIDENTIFIER NOT NULL,                                -- Mẻ sơ chế gốc
  InventoryID UNIQUEIDENTIFIER NOT NULL,                            -- Gắn với kho để lấy hàng
  CoffeeTypeID UNIQUEIDENTIFIER NOT NULL,                           -- Loại cà phê của sản phẩm
  OriginRegion NVARCHAR(100),                                       -- Vùng sản xuất
  OriginFarmLocation NVARCHAR(255),                                 -- Vị trí nông trại gốc
  GeographicalIndicationCode NVARCHAR(100),                         -- Chỉ dẫn địa lý
  CertificationURL NVARCHAR(255),                                   -- Link chứng nhận
  EvaluatedQuality NVARCHAR(100),                                   -- Chất lượng: Specialty...
  EvaluationScore FLOAT,                                            -- Điểm cupping
  Status NVARCHAR(50) DEFAULT 'pending',                            -- Trạng thái sản phẩm
  ApprovedBy UNIQUEIDENTIFIER,                                      -- Người duyệt
  ApprovalNote NVARCHAR(MAX),                                       -- Ghi chú duyệt
  ApprovedAt DATETIME,                                              -- Ngày duyệt
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,            -- Ngày tạo
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,            -- Ngày cập nhật

  -- FOREIGN KEYS
  CONSTRAINT FK_Products_CreatedBy 
      FOREIGN KEY (CreatedBy) REFERENCES BusinessManagers(ManagerID),

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

-- Contracts – Hợp đồng B2B giữa doanh nghiệp bán và bên mua
CREATE TABLE Contracts (
  ContractID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),          -- Mã hợp đồng
  ContractCode VARCHAR(20) UNIQUE,                                  -- CTR-2025-0023
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
  Status NVARCHAR(50) DEFAULT 'active',                             -- Trạng thái
  CancelReason NVARCHAR(MAX),                                       -- Lý do hủy
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,            -- Ngày tạo
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,            -- Ngày cập nhật

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
  ContractID UNIQUEIDENTIFIER NOT NULL,                            -- FK đến hợp đồng
  ProductID UNIQUEIDENTIFIER NOT NULL,                             -- Sản phẩm cụ thể
  Quantity FLOAT,                                                  -- Số lượng đặt mua
  UnitPrice FLOAT,                                                 -- Đơn giá
  DiscountAmount FLOAT DEFAULT 0.0,                                -- Giảm giá dòng này
  Note NVARCHAR(MAX),                                              -- Ghi chú (nếu có)
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,           -- Ngày tạo
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,           -- Ngày cập nhật

  -- FOREIGN KEYS
  CONSTRAINT FK_ContractItems_ContractID 
      FOREIGN KEY (ContractID) REFERENCES Contracts(ContractID),

  CONSTRAINT FK_ContractItems_ProductID 
      FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
);

GO

-- Orders – Thông tin đơn hàng theo hợp đồng
CREATE TABLE Orders (
  OrderID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),           -- Mã đơn hàng
  OrderCode VARCHAR(20) UNIQUE,                                   -- ORD-2025-0452
  ContractID UNIQUEIDENTIFIER NOT NULL,                           -- Gắn với hợp đồng
  BuyerID UNIQUEIDENTIFIER NOT NULL,                              -- Trader
  SellerID UNIQUEIDENTIFIER NOT NULL,                             -- Business Manager
  DeliveryRound INT,                                              -- Đợt giao lần mấy
  OrderDate DATETIME,                                             -- Ngày đặt hàng
  ExpectedDeliveryDate DATE,                                      -- Ngày dự kiến giao
  ActualDeliveryDate DATE,                                        -- Ngày giao thực tế
  TotalAmount FLOAT,                                              -- Tổng tiền đơn hàng
  Note NVARCHAR(MAX),                                             -- Ghi chú giao hàng
  Status NVARCHAR(50) DEFAULT 'pending',                          -- Trạng thái đơn: preparing, shipped, delivered,...
  CancelReason NVARCHAR(MAX),                                     -- Lý do hủy (nếu có)
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,          -- Ngày tạo
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,          -- Ngày cập nhật

  -- FOREIGN KEYS
  CONSTRAINT FK_Orders_ContractID 
      FOREIGN KEY (ContractID) REFERENCES Contracts(ContractID),

  CONSTRAINT FK_Orders_BuyerID 
      FOREIGN KEY (BuyerID) REFERENCES BusinessBuyers(BuyerID),

  CONSTRAINT FK_Orders_SellerID 
      FOREIGN KEY (SellerID) REFERENCES BusinessManagers(ManagerID)
);

GO

-- OrderItems – Chi tiết dòng sản phẩm trong đơn hàng
CREATE TABLE OrderItems (
  OrderItemID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),       -- Mã dòng sản phẩm trong đơn
  OrderID UNIQUEIDENTIFIER NOT NULL,                              -- Gắn với đơn hàng
  ProductID UNIQUEIDENTIFIER NOT NULL,                            -- Sản phẩm cụ thể
  Quantity FLOAT,                                                 -- Số lượng được giao
  UnitPrice FLOAT,                                                -- Đơn giá đã áp dụng
  DiscountAmount FLOAT DEFAULT 0.0,                               -- Giảm giá áp dụng dòng này
  TotalPrice FLOAT,                                               -- Tổng thành tiền dòng này
  Note NVARCHAR(MAX),                                             -- Ghi chú riêng dòng hàng
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,          -- Ngày tạo
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,          -- Ngày cập nhật

  -- FOREIGN KEYS
  CONSTRAINT FK_OrderItems_OrderID 
      FOREIGN KEY (OrderID) REFERENCES Orders(OrderID),

  CONSTRAINT FK_OrderItems_ProductID 
      FOREIGN KEY (ProductID) REFERENCES Products(ProductID),
);

GO

-- WarehouseOutboundRequests – Yêu cầu xuất kho từ nhân sự nội bộ
CREATE TABLE WarehouseOutboundRequests (
  OutboundRequestID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),     -- Mã yêu cầu xuất kho
  OutboundRequestCode VARCHAR(20) UNIQUE,                             -- OUTREQ-2025-0032
  WarehouseID UNIQUEIDENTIFIER NOT NULL,                              -- Kho chứa hàng cần xuất
  InventoryID UNIQUEIDENTIFIER NOT NULL,                              -- Dòng tồn kho cần xuất
  RequestedQuantity FLOAT NOT NULL,                                   -- Số lượng yêu cầu xuất
  Unit NVARCHAR(20) DEFAULT 'kg',                                     -- Đơn vị tính
  RequestedBy UNIQUEIDENTIFIER NOT NULL,                              -- Người tạo yêu cầu (BusinessStaff)
  Purpose NVARCHAR(100),                                              -- Mục đích xuất: Giao đơn hàng, Kiểm định, Nội bộ...
  OrderItemID UNIQUEIDENTIFIER,                                       -- (Nullable) Liên kết dòng đơn hàng nếu xuất cho B2B
  Reason NVARCHAR(MAX),                                               -- Ghi chú/giải thích chi tiết
  Status NVARCHAR(50) DEFAULT 'pending',                              -- Trạng thái: pending, approved, rejected, completed
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,              -- Ngày tạo yêu cầu
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,              -- Ngày cập nhật cuối

  -- FOREIGN KEYS
  CONSTRAINT FK_WarehouseOutboundRequests_Warehouse 
    FOREIGN KEY (WarehouseID) REFERENCES Warehouses(WarehouseID),

  CONSTRAINT FK_WarehouseOutboundRequests_Inventory 
    FOREIGN KEY (InventoryID) REFERENCES Inventories(InventoryID),

  CONSTRAINT FK_WarehouseOutboundRequests_Staff 
    FOREIGN KEY (RequestedBy) REFERENCES BusinessStaffs(StaffID),

  CONSTRAINT FK_WarehouseOutboundRequests_OrderItem 
    FOREIGN KEY (OrderItemID) REFERENCES OrderItems(OrderItemID)
);

GO

-- WarehouseOutboundReceipts – Phiếu xuất kho
CREATE TABLE WarehouseOutboundReceipts (
  OutboundReceiptID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),  -- Mã phiếu xuất kho
  OutboundReceiptCode VARCHAR(20) UNIQUE,                          -- OUT-RECEIPT-2025-0078 (Format: OUT-RECEIPT-YYYY-####)
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
  ShipmentCode VARCHAR(20) UNIQUE,                                -- SHIP-2025-0201
  OrderID UNIQUEIDENTIFIER NOT NULL,                              -- Gắn với đơn hàng
  DeliveryStaffID UNIQUEIDENTIFIER NOT NULL,                      -- Nhân viên giao hàng
  ShippedQuantity FLOAT,                                          -- Khối lượng đã giao
  ShippedAt DATETIME,                                             -- Ngày bắt đầu giao
  DeliveryStatus NVARCHAR(50) DEFAULT 'in_transit',               -- Trạng thái: in_transit, delivered, failed
  ReceivedAt DATETIME,                                            -- Ngày nhận hàng thành công
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,          -- Ngày tạo yêu cầu
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,          -- Ngày cập nhật cuối

  -- FOREIGN KEYS
  CONSTRAINT FK_Shipments_OrderID 
      FOREIGN KEY (OrderID) REFERENCES Orders(OrderID),

  CONSTRAINT FK_Shipments_DeliveryStaffID 
      FOREIGN KEY (DeliveryStaffID) REFERENCES UserAccounts(UserID)
);

GO

-- ShipmentDetails – Chi tiết sản phẩm theo từng chuyến hàng
CREATE TABLE ShipmentDetails (
  ShipmentDetailID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), -- Mã dòng sản phẩm trong shipment
  ShipmentID UNIQUEIDENTIFIER NOT NULL,                          -- Gắn với chuyến hàng
  ProductID UNIQUEIDENTIFIER NOT NULL,                           -- Sản phẩm được giao
  Quantity FLOAT,                                                -- Số lượng giao
  Unit NVARCHAR(20) DEFAULT 'kg',                                -- Đơn vị tính
  Note NVARCHAR(MAX),                                            -- Ghi chú riêng dòng hàng
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,         -- Ngày tạo
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,         -- Ngày cập nhật cuối

  -- FOREIGN KEYS
  CONSTRAINT FK_ShipmentDetails_ShipmentID 
      FOREIGN KEY (ShipmentID) REFERENCES Shipments(ShipmentID),

  CONSTRAINT FK_ShipmentDetails_ProductID 
      FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
);

GO

-- OrderComplaints – Khiếu nại đơn hàng
CREATE TABLE OrderComplaints (
  ComplaintID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),    -- Mã khiếu nại
  ComplaintCode VARCHAR(20) UNIQUE,                            -- CMP-2025-0012
  OrderItemID UNIQUEIDENTIFIER NOT NULL,                       -- Dòng hàng bị khiếu nại
  RaisedBy UNIQUEIDENTIFIER NOT NULL,                          -- Người mua khiếu nại
  ComplaintType NVARCHAR(100),                                 -- Loại khiếu nại: "Sai chất lượng", "Thiếu số lượng", "Vỡ bao bì"
  Description NVARCHAR(MAX),                                   -- Nội dung chi tiết
  EvidenceURL NVARCHAR(255),                                   -- Link ảnh/video minh chứng (nếu có)
  Status NVARCHAR(50) DEFAULT 'open',                          -- Trạng thái xử lý: open, investigating, resolved, rejected
  ResolutionNote NVARCHAR(MAX),                                -- Hướng xử lý hoặc kết quả
  ResolvedBy UNIQUEIDENTIFIER,                                 -- Người xử lý (doanh nghiệp bán)
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,       -- Ngày tạo khiếu nại
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,       -- Ngày cập nhật cuối
  ResolvedAt DATETIME,                                         -- Ngày xử lý hoàn tất

  -- FOREIGN KEYS
  CONSTRAINT FK_OrderComplaints_OrderItemID 
      FOREIGN KEY (OrderItemID) REFERENCES OrderItems(OrderItemID),

  CONSTRAINT FK_OrderComplaints_RaisedBy 
      FOREIGN KEY (RaisedBy) REFERENCES BusinessBuyers(BuyerID),

  CONSTRAINT FK_OrderComplaints_ResolvedBy 
      FOREIGN KEY (ResolvedBy) REFERENCES BusinessManagers(ManagerID)
);

GO

-- Conversations – Chủ đề trao đổi
CREATE TABLE Conversations (
  ConversationID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),        -- ID cuộc hội thoại
  Topic NVARCHAR(255),                                                -- Chủ đề (VD: "Tư vấn sâu bệnh vụ M001")
  CropProgressID UNIQUEIDENTIFIER,                                    -- Gắn với tiến độ mùa vụ (nếu có)
  BatchID UNIQUEIDENTIFIER,                                           -- Gắn với mẻ sơ chế (nếu có)
  CreatedBy UNIQUEIDENTIFIER,                                         -- Người tạo cuộc trò chuyện
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,              -- Ngày tạo

  -- FOREIGN KEYS (nullable theo business logic)
  CONSTRAINT FK_Conversations_CropProgress 
    FOREIGN KEY (CropProgressID) REFERENCES CropProgresses(ProgressID),

  CONSTRAINT FK_Conversations_Batch 
    FOREIGN KEY (BatchID) REFERENCES ProcessingBatches(BatchID),

  CONSTRAINT FK_Conversations_CreatedBy 
    FOREIGN KEY (CreatedBy) REFERENCES UserAccounts(UserID)
);

GO

-- ConversationParticipants – Người tham gia hội thoại
CREATE TABLE ConversationParticipants (
  ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),                   -- Mã dòng
  ConversationID UNIQUEIDENTIFIER NOT NULL,                          -- Gắn cuộc hội thoại
  UserID UNIQUEIDENTIFIER NOT NULL,                                  -- Người tham gia
  JoinedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,              -- Thời điểm tham gia

  -- FOREIGN KEYS
  CONSTRAINT FK_ConversationParticipants_Conversation 
    FOREIGN KEY (ConversationID) REFERENCES Conversations(ConversationID),

  CONSTRAINT FK_ConversationParticipants_User 
    FOREIGN KEY (UserID) REFERENCES UserAccounts(UserID)
);

GO

-- Messages – Tin nhắn trong hội thoại
CREATE TABLE ConversationMessages (
  MessageID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),            -- Mã tin nhắn
  ConversationID UNIQUEIDENTIFIER NOT NULL,                          -- Gắn hội thoại
  SenderID UNIQUEIDENTIFIER NOT NULL,                                -- Người gửi
  MessageText NVARCHAR(MAX),                                         -- Nội dung tin nhắn
  ImageURL NVARCHAR(255),                                            -- Link ảnh (nếu có)
  VideoURL NVARCHAR(255),                                            -- Link video (nếu có)
  IsRead BIT DEFAULT 0,                                              -- Đã đọc hay chưa
  ReadAt DATETIME,                                                   -- Thời gian đọc
  SentAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,                -- Thời điểm gửi

  -- FOREIGN KEYS
  CONSTRAINT FK_Messages_Conversation 
    FOREIGN KEY (ConversationID) REFERENCES Conversations(ConversationID),

  CONSTRAINT FK_Messages_Sender 
    FOREIGN KEY (SenderID) REFERENCES UserAccounts(UserID)
);

GO

-- SystemNotifications – Thông báo hệ thống
CREATE TABLE SystemNotifications (
  NotificationID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),    -- Mã thông báo
  NotificationCode VARCHAR(20) UNIQUE,                            -- NOTI-2025-0053
  Title NVARCHAR(255),                                            -- Tiêu đề
  Message NVARCHAR(MAX),                                          -- Nội dung chi tiết
  Type NVARCHAR(50),                                              -- Loại thông báo: "low_stock", "issue_reported",...
  CreatedBy UNIQUEIDENTIFIER,                                     -- Người khởi tạo (nullable)
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,          -- Thời gian tạo

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
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP           -- Ngày cập nhật cuối
);

GO

-- SystemConfiguration - Bảng lưu các tham số hệ thống để phục vụ validation động theo phạm vi áp dụng.
CREATE TABLE SystemConfiguration (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,                 -- Mã định danh tham số
    Description NVARCHAR(255),                   -- Mô tả ý nghĩa tham số
    MinValue DECIMAL(18,2),                      -- Giá trị tối thiểu (nếu có)
    MaxValue DECIMAL(18,2),                      -- Giá trị tối đa (nếu có)
    Unit NVARCHAR(20),                           -- Đơn vị đo (kg, %, times,...)
    IsActive BIT NOT NULL DEFAULT 1,             -- Còn hiệu lực không?
    EffectedDateFrom DATETIME NOT NULL,          -- Thời điểm bắt đầu áp dụng
    EffectedDateTo DATETIME NULL,                -- Thời điểm kết thúc áp dụng (nếu có)
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL DEFAULT GETDATE()
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
	UpdatedAt DATETIME NULL DEFAULT GETDATE()

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
INSERT INTO UserAccounts (UserCode, Email, PhoneNumber, Name, Gender, DateOfBirth, Address, PasswordHash, RoleID)
VALUES ('USR-2025-0001', 'admin@gmail.com', '0344033388', N'Phạm Huỳnh Xuân Đăng', 'Male', '1990-01-01', N'Đắk Lắk', 'Admin@12345', 1);

-- Business Manager
INSERT INTO UserAccounts (UserCode, Email, PhoneNumber, Name, Gender, DateOfBirth, Address, PasswordHash, RoleID)
VALUES ('USR-2025-0002', 'businessManager@gmail.com', '0325194357', N'Lê Hoàng Phúc', 'Male', '1985-05-10', N'Hồ Chí Minh', 'BusinessManager@12345', 2);

-- Farmer
INSERT INTO UserAccounts (UserCode, Email, PhoneNumber, Name, Gender, DateOfBirth, Address, PasswordHash, RoleID)
VALUES ('USR-2025-0003', 'farmer@gmail.com', '0942051066', N'Nguyễn Nhật Minh', 'Male', '1988-03-15', N'Buôn Ma Thuột', 'Farmer@12345', 4);

-- Expert
INSERT INTO UserAccounts (UserCode, Email, PhoneNumber, Name, Gender, DateOfBirth, Address, PasswordHash, RoleID)
VALUES ('USR-2025-0004', 'expert@gmail.com', '0975616076', N'Lê Hoàng Thiên Vũ', 'Male', '1978-08-22', N'Hà Nội', 'Expert@12345', 5);

-- Business Staff
INSERT INTO UserAccounts (UserCode, Email, PhoneNumber, Name, Gender, DateOfBirth, Address, PasswordHash, RoleID)
VALUES ('USR-2025-0005', 'businessStaff@gmail.com', '0941716075', N'Phạm Trường Nam', 'Male', '1999-09-12', N'Đắk Lắk', 'BusinessStaff@12345', 3);

GO

-- Insert vào bảng BusinessManagers
-- Lấy UserID của Business Manager
DECLARE @BMUserID UNIQUEIDENTIFIER = (SELECT UserID FROM UserAccounts WHERE Email = 'businessmanager@gmail.com');

INSERT INTO BusinessManagers (UserID, ManagerCode, CompanyName, Position, Department, CompanyAddress, TaxID, Website, ContactEmail)
VALUES (@BMUserID, 'BM-2025-0001', N'Công ty Cà Phê DakLak', N'Giám đốc điều hành', N'Phòng điều phối', N'15 Lê Duẩn, BMT, Đắk Lắk', '6001234567', 'https://daklakcoffee.vn', 'lehoangphuc14122003@gmail.com');

GO

-- Insert vào bảng Farmers
-- Lấy UserID của Farmer
DECLARE @FarmerUserID UNIQUEIDENTIFIER = (SELECT UserID FROM UserAccounts WHERE Email = 'farmer@gmail.com');

INSERT INTO Farmers (UserID, FarmerCode, FarmLocation, FarmSize, CertificationStatus)
VALUES (@FarmerUserID, 'FRM-2025-0001', N'Xã Ea Tu, TP. Buôn Ma Thuột', 2.5, N'VietGAP');

GO

-- Insert vào bảng AgriculturalExperts
-- Lấy UserID của Expert
DECLARE @ExpertUserID UNIQUEIDENTIFIER = (SELECT UserID FROM UserAccounts WHERE Email = 'expert@gmail.com');

INSERT INTO AgriculturalExperts (UserID, ExpertCode, ExpertiseArea, Qualifications, YearsOfExperience, AffiliatedOrganization, Bio, Rating)
VALUES (@ExpertUserID, 'EXP-2025-0001', N'Bệnh cây cà phê', N'Tiến sĩ Nông nghiệp', 12, N'Viện Khoa học Kỹ thuật Nông Lâm nghiệp Tây Nguyên', N'Chuyên gia hàng đầu về sâu bệnh và canh tác bền vững.', 4.8);

GO

-- CoffeeTypes – Danh sách các loại cà phê phổ biến tại Đắk Lắk
-- Định dạng TypeCode: CFT-2025-0001++

-- Arabica (vùng cao Đắk Lắk như M'Đrắk, Krông Bông)
INSERT INTO CoffeeTypes (TypeCode, TypeName, BotanicalName, Description, TypicalRegion, SpecialtyLevel)
VALUES (
  'CFT-2025-0001', N'Arabica', N'Coffea Arabica',
  N'Cà phê Arabica có vị chua thanh, hương thơm đặc trưng và hàm lượng caffeine thấp hơn Robusta.',
  N'M''Đrắk, Krông Bông – Đắk Lắk (vùng cao)',
  N'Specialty'
);

-- Robusta (chủ lực tại Đắk Lắk)
INSERT INTO CoffeeTypes (TypeCode, TypeName, BotanicalName, Description, TypicalRegion, SpecialtyLevel)
VALUES (
  'CFT-2025-0002', N'Robusta', N'Coffea Canephora',
  N'Robusta có vị đậm đắng, hậu vị mạnh, hàm lượng caffeine cao, phù hợp espresso hoặc cà phê truyền thống Việt.',
  N'Buôn Ma Thuột, Krông Pắc, Ea Kar – Đắk Lắk',
  N'Fine Robusta'
);

-- Culi (Robusta dạng đột biến, phổ biến tại Đắk Lắk)
INSERT INTO CoffeeTypes (TypeCode, TypeName, BotanicalName, Description, TypicalRegion, SpecialtyLevel)
VALUES (
  'CFT-2025-0003', N'Culi', NULL,
  N'Hạt cà phê tròn đều do đột biến tự nhiên, thường mạnh và đậm hơn Arabica và Robusta thông thường.',
  N'Đắk Lắk',
  N'Premium'
);

-- Robusta Honey (phương pháp sơ chế đặc biệt)
INSERT INTO CoffeeTypes (TypeCode, TypeName, BotanicalName, Description, TypicalRegion, SpecialtyLevel)
VALUES (
  'CFT-2025-0004', N'Robusta Honey', N'Coffea Canephora (Honey Processed)',
  N'Cà phê Robusta được sơ chế theo phương pháp honey để giữ lại vị ngọt và hương trái cây tự nhiên.',
  N'Ea H’leo, Cư M’gar – Đắk Lắk',
  N'Fine Robusta'
);

-- Robusta Natural (phơi nguyên trái)
INSERT INTO CoffeeTypes (TypeCode, TypeName, BotanicalName, Description, TypicalRegion, SpecialtyLevel)
VALUES (
  'CFT-2025-0005', N'Robusta Natural', N'Coffea Canephora (Natural Processed)',
  N'Sơ chế tự nhiên (phơi nguyên trái), giữ được vị ngọt hậu, phù hợp thị trường rang xay cao cấp.',
  N'Ea H’leo, Krông Năng – Đắk Lắk',
  N'Fine Robusta'
);

-- Typica (giống Arabica nguyên thủy, đang thử nghiệm tại Đắk Lắk)
INSERT INTO CoffeeTypes (TypeCode, TypeName, BotanicalName, Description, TypicalRegion, SpecialtyLevel)
VALUES (
  'CFT-2025-0006', N'Typica', N'Coffea Arabica Typica',
  N'Một trong những giống cà phê Arabica nguyên thủy, đang được thử nghiệm tại vùng cao Krông Bông.',
  N'Krông Bông – Đắk Lắk',
  N'Specialty'
);

-- Robusta Washed (sơ chế ướt, phổ biến cho xuất khẩu cao cấp)
INSERT INTO CoffeeTypes (TypeCode, TypeName, BotanicalName, Description, TypicalRegion, SpecialtyLevel)
VALUES (
  'CFT-2025-0007', N'Robusta Washed', N'Coffea Canephora (Washed Processed)',
  N'Cà phê Robusta được sơ chế ướt giúp vị trong trẻo hơn, hậu vị sạch, được ưa chuộng bởi thị trường châu Âu.',
  N'Krông Pắc, Buôn Ma Thuột – Đắk Lắk',
  N'Fine Robusta'
);

-- Robusta TR9 (giống chọn lọc năng suất cao tại Đắk Lắk)
INSERT INTO CoffeeTypes (TypeCode, TypeName, BotanicalName, Description, TypicalRegion, SpecialtyLevel)
VALUES (
  'CFT-2025-0008', N'Robusta TR9', N'Coffea Canephora TR9',
  N'Giống Robusta cao sản được Viện Eakmat chọn tạo, phù hợp điều kiện Đắk Lắk, cho năng suất vượt trội.',
  N'Ea Kar, Krông Ana – Đắk Lắk',
  N'Standard'
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

-- Chi tiết 1: Arabica
INSERT INTO ProcurementPlansDetails (
  PlanDetailCode, PlanID, CoffeeTypeID, CropType, TargetQuantity, TargetRegion, MinimumRegistrationQuantity,
  BeanSize, BeanColor, MoistureContent, DefectRate, MinPriceRange, MaxPriceRange, Note, ProgressPercentage
)
VALUES (
  'PLD-2025-A001', @PlanID1, @CoffeeID_Arabica, N'Arabica', 3000, N'Krông Bông',
  100, N'16–18', N'Nâu sáng', 12.5, 5, 80, 100, N'Thu mua dành cho thị trường specialty', 73.33
);

-- Chi tiết 2: Typica
INSERT INTO ProcurementPlansDetails (
  PlanDetailCode, PlanID, CoffeeTypeID, CropType, TargetQuantity, TargetRegion, MinimumRegistrationQuantity,
  BeanSize, BeanColor, MoistureContent, DefectRate, MinPriceRange, MaxPriceRange, Note
)
VALUES (
  'PLD-2025-A002', @PlanID1, @CoffeeID_Typica, N'Typica', 3000, N'M''Đrắk',
  150, N'16+', N'Nâu vàng', 11.5, 3, 90, 120, N'Sản phẩm trưng bày hội chợ cà phê 2025'
);

-- Chi tiết 3: Robusta Honey
INSERT INTO ProcurementPlansDetails (
  PlanDetailCode, PlanID, CoffeeTypeID, CropType, TargetQuantity, TargetRegion, MinimumRegistrationQuantity,
  BeanSize, BeanColor, MoistureContent, DefectRate, MinPriceRange, MaxPriceRange, Note
)
VALUES (
  'PLD-2025-B001', @PlanID2, @CoffeeID_Honey, N'Robusta', 7000, N'Cư M’gar',
  200, N'18+', N'Nâu sẫm', 12.0, 4, 60, 75, N'Yêu cầu sơ chế Honey tại chỗ, không vận chuyển trước khi phơi'
);

-- Chi tiết 4: Robusta TR9
INSERT INTO ProcurementPlansDetails (
  PlanDetailCode, PlanID, CoffeeTypeID, CropType, TargetQuantity, TargetRegion, MinimumRegistrationQuantity,
  BeanSize, BeanColor, MoistureContent, DefectRate, MinPriceRange, MaxPriceRange, Note
)
VALUES (
  'PLD-2025-B002', @PlanID2, @CoffeeID_TR9, N'Robusta', 5000, N'Ea Kar',
  300, N'17–18', N'Nâu đen', 13.0, 6, 55, 68, N'Áp dụng tiêu chuẩn ISO 8451'
);

GO

-- Insert vào bảng CultivationRegistrations
-- Lấy FarmerID & PlanID để đăng ký
DECLARE @FarmerID UNIQUEIDENTIFIER = (SELECT FarmerID FROM Farmers WHERE FarmerCode = 'FRM-2025-0001');
DECLARE @PlanID UNIQUEIDENTIFIER = (SELECT PlanID FROM ProcurementPlans WHERE PlanCode = 'PLAN-2025-0001');

-- Tạo đơn đăng ký
DECLARE @RegistrationID UNIQUEIDENTIFIER = NEWID();
INSERT INTO CultivationRegistrations (
    RegistrationID, RegistrationCode, PlanID, FarmerID, RegisteredArea, WantedPrice, Note
)
VALUES (
    @RegistrationID, 'REG-2025-0001', @PlanID, @FarmerID, 1.8, 95, N'Đăng ký trồng cà phê Arabica với kỹ thuật tưới nhỏ giọt'
);

GO

-- Insert vào bảng CultivationRegistrationsDetail
-- Lấy PlanDetailID của Arabica trong kế hoạch PLAN-2025-0001
DECLARE @RegistrationID UNIQUEIDENTIFIER = (
    SELECT RegistrationID FROM CultivationRegistrations WHERE RegistrationCode = 'REG-2025-0001'
);
DECLARE @PlanDetailID UNIQUEIDENTIFIER = (
    SELECT PlanDetailsID FROM ProcurementPlansDetails WHERE PlanDetailCode = 'PLD-2025-A001'
);
DECLARE @RegistrationDetailID UNIQUEIDENTIFIER = NEWID();

INSERT INTO CultivationRegistrationsDetail (
    CultivationRegistrationDetailID, RegistrationID, PlanDetailID, EstimatedYield,
    ExpectedHarvestStart, ExpectedHarvestEnd, Note
)
VALUES (
    @RegistrationDetailID, @RegistrationID, @PlanDetailID, 2200,
    '2025-11-01', '2026-01-15',
    N'Dự kiến sử dụng phân bón hữu cơ và công nghệ AI kiểm tra sâu bệnh'
);

GO

-- Insert vào bảng FarmingCommitments
-- Lấy PlanID, PlanDetailID và ManagerID để hoàn tất cam kết
DECLARE @RegistrationDetailID UNIQUEIDENTIFIER = (
    SELECT CultivationRegistrationDetailID FROM CultivationRegistrationsDetail
    WHERE RegistrationID = (SELECT RegistrationID FROM CultivationRegistrations WHERE RegistrationCode = 'REG-2025-0001')
    AND PlanDetailID = (SELECT PlanDetailsID FROM ProcurementPlansDetails WHERE PlanDetailCode = 'PLD-2025-A001')
);

DECLARE @FarmerID UNIQUEIDENTIFIER = (
    SELECT FarmerID FROM Farmers WHERE FarmerCode = 'FRM-2025-0001'
);
DECLARE @PlanID UNIQUEIDENTIFIER = (
    SELECT PlanID FROM ProcurementPlans WHERE PlanCode = 'PLAN-2025-0001'
);
DECLARE @PlanDetailID UNIQUEIDENTIFIER = (
    SELECT PlanDetailsID FROM ProcurementPlansDetails WHERE PlanDetailCode = 'PLD-2025-A001'
);
DECLARE @ManagerID UNIQUEIDENTIFIER = (
    SELECT ManagerID FROM BusinessManagers 
    WHERE UserID = (SELECT UserID FROM UserAccounts WHERE Email = 'businessmanager@gmail.com')
);

INSERT INTO FarmingCommitments (
    CommitmentCode, RegistrationDetailID, PlanID, PlanDetailID, FarmerID,
    ConfirmedPrice, CommittedQuantity, EstimatedDeliveryStart, EstimatedDeliveryEnd,
    ApprovedBy, ApprovedAt, Note
)
VALUES (
    'COMMIT-2025-0001', @RegistrationDetailID, @PlanID, @PlanDetailID, @FarmerID,
    96.5, 2100, '2026-01-20', '2026-01-30', @ManagerID, GETDATE(),
    N'Cam kết cung ứng đúng chuẩn và đúng hạn, đã qua thẩm định nội bộ'
);

GO

-- Insert vào bảng SystemConfiguration
-- Tuổi tối thiểu để đăng ký tài khoản người dùng
INSERT INTO SystemConfiguration 
    (Name, Description, MinValue, MaxValue, Unit, IsActive, EffectedDateFrom)
VALUES 
    ('MIN_AGE_FOR_REGISTRATION', N'Tuổi tối thiểu để đăng ký tài khoản', 18, NULL, 'years', 1, GETDATE());

GO

-- Insert vào bảng SystemConfigurationUsers
-- Lấy ID của tham số "MIN_AGE_FOR_REGISTRATION"
DECLARE @MinAgeConfigID INT = (SELECT Id FROM SystemConfiguration WHERE Name = 'MIN_AGE_FOR_REGISTRATION');

-- Gán quyền 'manage' cho admin (giả sử admin là Phạm Huỳnh Xuân Đăng)
DECLARE @AdminID UNIQUEIDENTIFIER = (SELECT UserID FROM UserAccounts WHERE Email = 'admin@gmail.com');

INSERT INTO SystemConfigurationUsers (SystemConfigurationID, UserID, PermissionLevel)
VALUES (@MinAgeConfigID, @AdminID, 'manage');
