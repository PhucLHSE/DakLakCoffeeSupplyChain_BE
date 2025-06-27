using DakLakCoffeeSupplyChain.Common.Helpers.Security;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.AspNetCore.OData;


var builder = WebApplication.CreateBuilder(args);

// Đăng ký service hash password
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

// Đăng ký service tạo mã định danh
builder.Services.AddScoped<ICodeGenerator, CodeGenerator>();

// Unit of Work pattern: quản lý Transaction + Repository access
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Service gửi mail
builder.Services.AddScoped<IEmailService, EmailService>();

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
    odataBuilder.EntitySet<CropProgress>("CropProgress");
    odataBuilder.EntitySet<ProcessingMethod>("ProcessingMethod");
    odataBuilder.EntitySet<WarehouseInboundRequest>("WarehouseInboundRequest");
    odataBuilder.EntitySet<Product>("Product");

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