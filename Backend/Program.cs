using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SkillSnap.Backend.Data;
using SkillSnap.Backend.Services;
using SkillSnap.Backend.Middleware;
using SkillSnap.Shared.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<SkillSnapContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Identity with enhanced security settings
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Enhanced password settings following Azure security best practices
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequiredUniqueChars = 4;
    
    // Enhanced lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    
    // Enhanced user settings
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    
    // Sign-in settings for enhanced security
    options.SignIn.RequireConfirmedEmail = false; // Set to true in production
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<SkillSnapContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication with enhanced security
var jwtKey = builder.Configuration["Jwt:Key"]!;
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true; // Enhanced security - require HTTPS in production
    options.SaveToken = false; // Don't save tokens for better security
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1), // Reduced clock skew for better security
        RequireExpirationTime = true,
        RequireSignedTokens = true
    };
    
    // Enhanced event handling for security monitoring
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("JWT authentication failed: {Error}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogDebug("JWT token validated for user: {User}", context.Principal?.Identity?.Name);
            return Task.CompletedTask;
        }
    };
});

// Register JWT service
builder.Services.AddScoped<IJwtService, JwtService>();

// Register Security Service for XSS and SQL injection protection
builder.Services.AddScoped<ISecurityService, SecurityService>();

// Register Metrics Service (as Singleton to persist metrics across requests)
builder.Services.AddSingleton<IMetricsService, MetricsService>();

// Configure Memory Cache with optimization settings
builder.Services.AddMemoryCache(options =>
{
    // Set memory limits to prevent excessive memory usage
    options.SizeLimit = 1024; // Max 1024 cache entries
    options.CompactionPercentage = 0.20; // Remove 20% of entries when limit is reached
    options.ExpirationScanFrequency = TimeSpan.FromMinutes(5); // Check for expired entries every 5 minutes
});

// Register Cache Service
builder.Services.AddScoped<ICacheService, CacheService>();

// Enhanced CORS configuration with security considerations
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorApp",
        builder =>
        {
            builder.WithOrigins("http://localhost:5008", "https://localhost:5009") // Support both HTTP and HTTPS
                   .AllowAnyHeader()
                   .AllowAnyMethod()
                   .AllowCredentials() // Allow credentials for authentication
                   .SetPreflightMaxAge(TimeSpan.FromMinutes(10)); // Cache preflight requests
        });
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<SkillSnapContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await DbInitializer.InitializeAsync(context, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline with enhanced security
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Security middleware pipeline - order is critical for security
app.UseHttpsRedirection();

// CORS must be before authentication
app.UseCors("AllowBlazorApp");

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
