using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OlapAnalytics.API.Middleware;
using OlapAnalytics.API.Services;
using OlapAnalytics.Application.Services;
using OlapAnalytics.Application.Interfaces;
using OlapAnalytics.Infrastructure.DependencyInjection;
using OlapAnalytics.Infrastructure.Ssas;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Infrastructure (SSAS + Cache + optional SQL Server)
builder.Services.AddInfrastructure();

// Application Services
builder.Services.AddScoped<AnalyticsService>();
builder.Services.AddScoped<KpiService>();
builder.Services.AddScoped<TrendService>();
builder.Services.AddScoped<CubeMetadataResolver>(); // Auto-detects dimensions/measures from SSAS

// Tenant Connection Provider
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantConnectionProvider, TenantConnectionProvider>();

// New Analytics Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<ISqlProvisioningService, SqlProvisioningService>();
builder.Services.AddScoped<ISsasProvisioningService, SsasProvisioningService>();

// HttpClient and GeminiService
builder.Services.AddHttpClient<IGeminiService, GeminiService>();


// Controllers
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "OLAP Analytics API — Smart Data Platform", Version = "v1",
        Description = "OLAP analytics API for Data Warehouse cubes. Use /api/auth/login to get a JWT token." });
});

// JWT Authentication
var rawJwtKey = builder.Configuration["Jwt:Key"] ?? "OlapAnalyticsSuperSecretKey2024!!";
var encryptionService = new OlapAnalytics.Infrastructure.Security.AesEncryptionService(builder.Configuration);
var jwtKey = encryptionService.Decrypt(rawJwtKey);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "OlapAnalytics",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "OlapAnalyticsClients",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("AnalystOrAbove", policy => policy.RequireRole("Admin", "Analyst"));
    options.AddPolicy("AllUsers", policy => policy.RequireRole("Admin", "Analyst", "Viewer"));
});

// CORS — allow frontend dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// ──── App Pipeline ────────────────────────────────────────────────────────────
var app = builder.Build();

// Global exception handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "OLAP Analytics API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => new { Status = "Healthy", Timestamp = DateTime.UtcNow })
   .AllowAnonymous();

app.Run();
