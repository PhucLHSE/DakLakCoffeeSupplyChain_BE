using CloudinaryDotNet;
    using DakLakCoffeeSupplyChain.Common.Helpers.Security;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OData;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;
using DakLakCoffeeSupplyChain.Common.Helpers;


var builder = WebApplication.CreateBuilder(args);

// Đăng ký service hash password
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

// Đăng ký service tạo mã định danh
builder.Services.AddScoped<ICodeGenerator, CodeGenerator>();

// Unit of Work pattern: quản lý Transaction + Repository access
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Service gửi mail
builder.Services.AddScoped<IEmailService, EmailService>();
// Cấu hình Cloudinary
builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("CloudinarySettings"));

var cloudinarySettings = builder.Configuration
    .GetSection("CloudinarySettings").Get<CloudinarySettings>();

var account = new Account(
    cloudinarySettings.CloudName,
    cloudinarySettings.ApiKey,
    cloudinarySettings.ApiSecret
);

var cloudinary = new Cloudinary(account);
builder.Services.AddSingleton(cloudinary);

// Đăng ký các service nghiệp vụ
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IUserAccountService, UserAccountService>();
builder.Services.AddScoped<IBusinessManagerService, BusinessManagerService>();
builder.Services.AddScoped<IBusinessBuyerService, BusinessBuyerService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IContractItemService, ContractItemService>();
builder.Services.AddScoped<IContractDeliveryBatchService, ContractDeliveryBatchService>();
builder.Services.AddScoped<IContractDeliveryItemService, ContractDeliveryItemService>();
builder.Services.AddScoped<ICropSeasonService, CropSeasonService>();
builder.Services.AddScoped<ICropStageService, CropStageService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProcurementPlanService, ProcurementPlanService>();
builder.Services.AddScoped<IProcessingMethodService, ProcessingMethodService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWarehouseInboundRequestService, WarehouseInboundRequestService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ICropProgressService, CropProgressService>();
builder.Services.AddScoped<IWarehouseReceiptService, WarehouseReceiptService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IWarehouseOutboundRequestService, WarehouseOutboundRequestService>();
builder.Services.AddScoped<IWarehouseOutboundReceiptService, WarehouseOutboundReceiptService>();
builder.Services.AddScoped<ICoffeeTypeService, CoffeeTypeService>();
builder.Services.AddScoped<IProcessingStageService, ProcessingStageService>();
builder.Services.AddScoped<IGeneralFarmerReportService, GeneralFarmerReportService>();
builder.Services.AddScoped<ICultivationRegistrationService, CultivationRegistrationService>();
builder.Services.AddScoped<IProcessingParameterService, ProcessingParameterService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IProcessingBatchService, ProcessingBatchService>();
builder.Services.AddScoped<IProcessingBatchProgressService, ProcessingBatchProgressService>();
builder.Services.AddScoped<ICropSeasonDetailService, CropSeasonDetailService>();
builder.Services.AddScoped<IFarmerService, FarmerService>();
builder.Services.AddScoped<IFarmingCommitmentService, FarmingCommitmentService>();
builder.Services.AddScoped<IBusinessStaffService, BusinessStaffService>();
builder.Services.AddScoped<IExpertAdviceService, ExpertAdviceService>();
builder.Services.AddScoped<IAgriculturalExpertService, AgriculturalExpertService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderItemService, OrderItemService>();
builder.Services.AddScoped<IInventoryLogService, InventoryLogService>();
builder.Services.AddScoped<IProcessingWasteService, ProcessingWasteService>();
builder.Services.AddScoped<IProcessingWasteDisposalService, ProcessingWasteDisposalService>();
builder.Services.AddScoped<IUploadService, UploadService>();

//Add MemoryCache
builder.Services.AddMemoryCache();

// JSON Settings
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

//ODATA
static IEdmModel GetEdmModel()
{
    var odataBuilder = new ODataConventionModelBuilder();

    odataBuilder.EntitySet<Role>("Role");
    odataBuilder.EntitySet<UserAccount>("UserAccount");
    odataBuilder.EntitySet<BusinessManager>("BusinessManager");
    odataBuilder.EntitySet<BusinessBuyer>("BusinessBuyer");
    odataBuilder.EntitySet<Contract>("Contract");
    odataBuilder.EntitySet<ContractDeliveryBatch>("ContractDeliveryBatch");
    odataBuilder.EntitySet<ProcurementPlan>("ProcurementPlan");
    odataBuilder.EntitySet<CropStage>("CropStage");
    odataBuilder.EntitySet<CropSeason>("CropSeason");
    odataBuilder.EntitySet<CropSeasonDetail>("CropSeasonDetail");
    odataBuilder.EntitySet<CropProgress>("CropProgress");
    odataBuilder.EntitySet<FarmingCommitment>("FarmingCommitment");
    odataBuilder.EntitySet<FarmingCommitmentsDetail>("FarmingCommitmentsDetail");
    odataBuilder.EntitySet<ProcessingMethod>("ProcessingMethod");
    odataBuilder.EntitySet<ProcessingStage>("ProcessingStage");
    odataBuilder.EntitySet<ProcessingBatch>("ProcessingBatch");
    odataBuilder.EntitySet<ProcessingBatchProgress>("ProcessingBatchProgress");
    odataBuilder.EntitySet<ProcessingParameter>("ProcessingParameter");
    odataBuilder.EntitySet<Inventory>("Inventory");
    odataBuilder.EntitySet<Warehouse>("Warehouse");
    odataBuilder.EntitySet<WarehouseInboundRequest>("WarehouseInboundRequest");
    odataBuilder.EntitySet<WarehouseReceipt>("WarehouseReceipt");
    odataBuilder.EntitySet<WarehouseOutboundRequest>("WarehouseOutboundRequest");
    odataBuilder.EntitySet<WarehouseOutboundReceipt>("WarehouseOutboundReceipt");
    odataBuilder.EntitySet<Product>("Product");
    odataBuilder.EntitySet<Order>("Order");

    return odataBuilder.GetEdmModel();
}
builder.Services.AddControllers().AddOData(options =>
{
    options.Select().Filter().OrderBy().Expand().SetMaxTop(null).Count();
    options.AddRouteComponents("odata", GetEdmModel());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    option.DescribeAllParametersInCamelCase();
    option.ResolveConflictingActions(conf => conf.First());
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[]{}
        }
    });
});

// Cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Áp dụng CORS cho toàn bộ hệ thống (áp dụng policy phía trên)
app.UseCors("AllowAllOrigins");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();