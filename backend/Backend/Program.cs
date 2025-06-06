using System.Security.Claims;
using System.Text;
using Backend.DBContext;
using Backend.Helper;
using Backend.Hubs;
using Backend.Models.Auth;
using Backend.Models.AWS;
using Backend.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Load Configuration
builder
    .Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Identity Configuration
builder
    .Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Dependency Injection
builder.Services.AddScoped<IDBHelper, DBHelper>();
builder.Services.AddScoped<IEmailService, EmailService>();

// SignalR Configuration
builder.Services.AddSignalR();

// CORS Configuration with fallback
var allowedOriginsRaw =
    builder.Configuration.GetValue<string>("allowedOrigins") ?? "http://localhost:4200";
var allowedOrigins = allowedOriginsRaw.Split(',');

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

// JWT Authentication
var jwtKey = builder.Configuration["JWT:Key"];
if (string.IsNullOrEmpty(jwtKey))
    throw new InvalidOperationException("JWT Key is missing in configuration.");

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Works for HTTP locally
        options.Cookie.SameSite = SameSiteMode.None; // Cross-site compatibility
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true; // Ensure it's not blocked by consent
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"],
            ValidAudience = builder.Configuration["JWT:Audience"],
            IssuerSigningKey = key,
            RoleClaimType = ClaimTypes.Role,
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["ACCESS_TOKEN"];
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Append("Token-Expired", "true");
                }
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                var result = System.Text.Json.JsonSerializer.Serialize(
                    new { message = "Invalid token" }
                );
                return context.Response.WriteAsync(result);
            },
        };
    });

builder.Services.AddHealthChecks();

// Distributed Memory Cache and Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.None; // Match Google's cookie
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // HTTP locally
});

// Cookie Policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.None; // Match Google's cookie
    options.Secure = CookieSecurePolicy.SameAsRequest; // options.Secure = CookieSecurePolicy.Always; // Secure in production
});

// AWS S3 Configuration
builder.Services.Configure<AWSSettings>(builder.Configuration.GetSection("AWS"));
builder.Services.AddScoped<IS3Service, S3Service>();

// Add Stripe configuration
//builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

// Add payment service
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Add Stripe Connect service
builder.Services.AddScoped<IStripeConnectService, StripeConnectService>();

// Controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TravelWise API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter JWT token in the format: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
    };

    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement { { securityScheme, new string[] { } } }
    );
});

// Dynamic port configuration for Railway
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

var app = builder.Build();

// Apply migrations at startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        dbContext.Database.Migrate(); // Applies all pending migrations
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to apply database migrations");
        throw; // Rethrow to ensure the application doesn't start with failed migrations
    }
}

// Middleware
app.UseCors();

app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    // In production, rely on Railway's HTTPS termination
}

app.UseHealthChecks("/health");
app.UseCookiePolicy();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// Configure SignalR endpoints
app.MapHub<ChatHub>("/chatHub");

app.MapControllers();

app.Run();
