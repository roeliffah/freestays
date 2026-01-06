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
using Hangfire.PostgreSql;
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

// ✅ ENVİRONMENT-SPECIFIC CONFIGURATION
// Development: appsettings.Development.json'ı otomatik yükle (local DB bağlantısı)
// Production: appsettings.json veya environment variables kullan (production DB bağlantısı)
var env = builder.Environment;
if (env.IsDevelopment())
{
    // Development ortamında appsettings.Development.json'ı kullan
    builder.Configuration.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: false, reloadOnChange: true);
}
// Production ortamında environment variables production DB bağlantısını override etmesi için:
// CONNECTIONSTRINGS__DEFAULTCONNECTION=... şeklinde ayarla (Dokploy UI'de)
builder.Configuration.AddEnvironmentVariables();

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

// ⭐ Hangfire - PostgreSQL Storage (FIX: Redis was causing OOM crash)
// ❌ REMOVED: Redis storage was filling up RAM with job payloads, state, history, etc.
// ✅ FIXED: Using PostgreSQL for job storage (safe, scalable, no RAM bloat)
// 📌 Redis is now used ONLY for caching (not for Hangfire jobs)
var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// ✅ FIXED: Set worker count to 2 (fixed, not dynamic)
// ❌ REASON: SunHotels sync is memory-intensive; dynamic ProcessorCount causes OOM crashes
// 📌 Even 4 workers can spike RAM to 2GB+; keeping it at 2 prevents container restart loops
const int workerCount = 2;

Log.Information("🔧 Hangfire Configuration - Storage: PostgreSQL, WorkerCount: {Workers}", workerCount);

try
{
    Log.Information("🚀 Configuring Hangfire with PostgreSQL storage...");

    builder.Services.AddHangfire(config =>
    {
        config.UsePostgreSqlStorage(defaultConnectionString, new Hangfire.PostgreSql.PostgreSqlStorageOptions
        {
            QueuePollInterval = TimeSpan.FromSeconds(15),      // ✅ Reduce DB polling
            InvisibilityTimeout = TimeSpan.FromMinutes(5),     // ✅ Job visibility timeout
            PrepareSchemaIfNecessary = true                    // ✅ Auto-create schema
        });
        config.UseSimpleAssemblyNameTypeSerializer();
        config.UseRecommendedSerializerSettings();
    });

    // ⭐ CRITICAL: Set automatic retry limit
    // ❌ WITHOUT THIS: Default = 10 retries → retry storm on failure (RAM/CPU/DB spike)
    // ✅ WITH THIS: Only 1 retry per job (from appsettings.json)
    var retryAttempts = 1; // ✅ Fixed: only 1 automatic retry per job
    GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = retryAttempts });
    Log.Information("✅ Hangfire AutomaticRetry set to {Attempts} attempt(s)", retryAttempts);

    Log.Information("✅ Hangfire configured with PostgreSQL storage successfully");
}
catch (Exception ex)
{
    Log.Error(ex, "❌ Hangfire PostgreSQL configuration failed: {Message}", ex.Message);
    throw;
}

builder.Services.AddHangfireServer(options =>
{
    options.ServerName = $"{Environment.MachineName}-{Guid.NewGuid().ToString()[..8]}";
    options.WorkerCount = 2;  // ✅ Optimized: 2 workers instead of ProcessorCount * 2
    options.Queues = new[] { "default", "critical" };
    options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);  // ✅ Graceful shutdown timeout

    // ✅ Memory optimization - Reduce how many jobs are processed in parallel
    options.HeartbeatInterval = TimeSpan.FromSeconds(30);
    options.ServerCheckInterval = TimeSpan.FromSeconds(30);
    options.StopTimeout = TimeSpan.FromSeconds(30);
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
    // ✅ QueueLimit = 0 for memory optimization (2GB RAM server)
    options.AddPolicy("fixed", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0  // ✅ No queue = less memory (reject immediately)
            }));

    // Auth endpoints: 10 requests per minute (anti brute-force)
    // ✅ QueueLimit = 0 for memory optimization
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0  // ✅ No queue = less memory
            }));

    // Search endpoints: 30 requests per minute
    // ✅ QueueLimit = 0 for memory optimization
    options.AddPolicy("search", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0  // ✅ No queue = less memory
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
    Authorization = new[] { new HangfireAuthorizationFilter(app.Environment) },
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

try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<FreeStays.Infrastructure.Persistence.Context.FreeStaysDbContext>();
        await dbContext.Database.MigrateAsync();
        Log.Information("✅ Database migration completed successfully (Development only)");
    }
}
catch (Exception ex)
{
    Log.Error(ex, "⚠️ An error occurred during database migration (Development)");
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

// Hangfire Authorization Filter - SECURE IN PRODUCTION
// ❌ REMOVED: public bool Authorize(...) => true; (everyone could trigger jobs)
// ✅ FIXED: Only allow in Development; Production requires Admin auth
public class HangfireAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    private readonly IWebHostEnvironment _env;

    public HangfireAuthorizationFilter(IWebHostEnvironment env)
    {
        _env = env;
    }

    public bool Authorize(Hangfire.Dashboard.DashboardContext context)
    {
        // Allow all in Development
        if (_env.IsDevelopment())
        {
            return true;
        }

        // In Production: DENY by default
        // To enable dashboard in production, implement proper JWT/role-based auth
        Log.Warning("⚠️ Hangfire dashboard access denied in Production environment");
        return false;
    }
}
