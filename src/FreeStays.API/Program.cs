using System.Text;
using System.Threading.RateLimiting;
using AspNetCoreRateLimit;
using FreeStays.API.Data;
using FreeStays.API.Middleware;
using FreeStays.API.Services;
using FreeStays.Application;
using FreeStays.Application.Common.Interfaces;
using FreeStays.Infrastructure;
using Hangfire;
using Hangfire.InMemory;
using Hangfire.Redis.StackExchange;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Serilog Configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

// JWT Settings
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            // AllowAnonymous endpoint'lerde expired token hatalarını ignore et
            var endpoint = context.HttpContext.GetEndpoint();
            var allowAnonymous = endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null;

            if (allowAnonymous && context.Exception is SecurityTokenExpiredException)
            {
                Log.Warning("Expired token on AllowAnonymous endpoint: {Path}", context.HttpContext.Request.Path);
                context.Response.Headers.Append("Token-Expired", "true");
                // AllowAnonymous endpoint için authentication'ı başarılı say
                context.NoResult();
                return Task.CompletedTask;
            }

            Log.Error(context.Exception, "JWT Authentication failed");
            if (context.Exception is SecurityTokenExpiredException)
            {
                context.Response.Headers.Append("Token-Expired", "true");
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Log.Information("JWT Token validated successfully for user: {User}", context.Principal?.Identity?.Name);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            // AllowAnonymous endpoint'lerde challenge'ı bypass et
            var endpoint = context.HttpContext.GetEndpoint();
            var allowAnonymous = endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null;

            if (allowAnonymous)
            {
                Log.Information("Challenge bypassed for AllowAnonymous endpoint: {Path}", context.HttpContext.Request.Path);
                context.HandleResponse();
                return Task.CompletedTask;
            }

            Log.Warning("JWT Authentication challenge: {Error}, {ErrorDescription}", context.Error, context.ErrorDescription);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Application & Infrastructure Layer DI
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// API Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddHttpContextAccessor();

// File Upload Settings
builder.Services.Configure<FileUploadSettings>(builder.Configuration.GetSection("FileUpload"));

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });

    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Hangfire - Redis Storage (Production Ready)
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
Log.Information("🔍 Redis Connection String: {Redis}", redisConnectionString ?? "NULL");

if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    try
    {
        Log.Information("🚀 Attempting to configure Hangfire with Redis storage...");

        builder.Services.AddHangfire(config =>
        {
            config.UseRedisStorage(redisConnectionString, new RedisStorageOptions
            {
                Prefix = "hangfire:",
                ExpiryCheckInterval = TimeSpan.FromHours(1),
                DeletedListSize = 5000,
                SucceededListSize = 5000,
                InvisibilityTimeout = TimeSpan.FromMinutes(30)
            });
        });

        Log.Information("✅ Hangfire configured with Redis storage successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "⚠️ Redis connection failed, falling back to InMemory storage. Error: {Message}", ex.Message);
        builder.Services.AddHangfire(config => config.UseInMemoryStorage());
    }
}
else
{
    // Development fallback
    builder.Services.AddHangfire(config => config.UseInMemoryStorage());
    Log.Information("Hangfire configured with InMemory storage (Development)");
}

builder.Services.AddHangfireServer(options =>
{
    options.ServerName = $"{Environment.MachineName}-{Guid.NewGuid().ToString()[..8]}";
    options.WorkerCount = Environment.ProcessorCount * 2;
    options.Queues = new[] { "default", "critical" };
    options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FreeStays API",
        Version = "v1",
        Description = "FreeStays - Otel, Uçuş ve Araç Kiralama Rezervasyon API'si",
        Contact = new OpenApiContact
        {
            Name = "FreeStays Destek",
            Email = "support@freestays.com",
            Url = new Uri("https://freestays.com")
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // JWT Authentication for Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.\n\nExample: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });

    // Enable XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // API Groups and Tags
    options.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
    options.DocInclusionPredicate((name, api) => true);
});

// Health Checks
builder.Services.AddHealthChecks();

// Rate Limiting - Brute Force / DDoS Protection
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Global rate limit: 100 requests per minute per IP
    options.AddPolicy("fixed", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));

    // Auth endpoints: 10 requests per minute (anti brute-force)
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2
            }));

    // Search endpoints: 30 requests per minute
    options.AddPolicy("search", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Too many requests",
            message = "Rate limit exceeded. Please try again later.",
            retryAfter = "60 seconds"
        }, token);
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FreeStays API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "FreeStays API Documentation";
    });
    app.UseCors("AllowAll");
}
else
{
    app.UseCors("AllowSpecificOrigins");
}

// Rate Limiting Middleware
app.UseRateLimiter();

// Exception Handling Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Serilog Request Logging
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard
app.UseHangfireDashboard(builder.Configuration["Hangfire:DashboardPath"] ?? "/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() },
    DashboardTitle = "FreeStays Background Jobs"
});

// Register Recurring Jobs
// SunHotels Static Data Sync - Her gün gece yarısı çalışır
RecurringJob.AddOrUpdate<FreeStays.Infrastructure.BackgroundJobs.SunHotelsStaticDataSyncJob>(
    "sunhotels-static-data-sync",
    job => job.SyncAllStaticDataAsync(),
    Cron.Daily(3, 0), // Her gün saat 03:00'te çalışır
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul") }
);

// SunHotels Basic Data Sync - Her 6 saatte bir (hızlı güncelleme)
RecurringJob.AddOrUpdate<FreeStays.Infrastructure.BackgroundJobs.SunHotelsStaticDataSyncJob>(
    "sunhotels-basic-data-sync",
    job => job.SyncBasicDataAsync(),
    "0 */6 * * *", // Her 6 saatte bir
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul") }
);

// Health Check Endpoint
app.MapHealthChecks("/health");

app.MapControllers();

// Auto Migrate Database
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<FreeStays.Infrastructure.Persistence.Context.FreeStaysDbContext>();
        await dbContext.Database.MigrateAsync();
        Log.Information("Database migration completed successfully");
    }
}
catch (Exception ex)
{
    Log.Error(ex, "An error occurred during database migration");
}

// Seed Database
await DatabaseSeeder.SeedAsync(app.Services);

// Log application startup
Log.Information("FreeStays API started at {Time}", DateTime.UtcNow);

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Hangfire Authorization Filter
public class HangfireAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context)
    {
        // Allow all in development (for simplicity)
        // In production, you should properly check user authentication
        return true;
    }
}
