using System.Text;
using Backend.DBContext;
using Backend.Helper;
using Backend.Models.Auth;
using Backend.Models.AWS;
using Backend.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;

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
        // Password Policy
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
    .AddCookie()
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
            RoleClaimType = ClaimTypes.Role
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
                    context.Response.Headers.Add("Token-Expired", "true");
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
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.SaveTokens = true;
        options.CallbackPath = "/api/auth/google-callback";
        options.UsePkce = false;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.None; // Change this for development
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.IsEssential = true;
        options.CorrelationCookie.HttpOnly = true;
    });

var allowedOrigins = builder.Configuration.GetValue<string>("allowedOrigins")!.Split(',');

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(origin => true); // For development only
    });
});

// Add this before builder.Build()
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Change this for development
});

// Add cookie policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.Secure = CookieSecurePolicy.None; // Change this for development
});

// Add AWS S3 configuration
builder.Services.Configure<AWSSettings>(
    builder.Configuration.GetSection("AWS"));

// Register S3 service
builder.Services.AddScoped<IS3Service, S3Service>();

// Controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Configure Swagger to support JWT authentication
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

var app = builder.Build();

// Middleware
app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseCookiePolicy();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
