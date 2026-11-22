using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using AffordStack.API.Middleware;
using AffordStack.Application.Interfaces;
using AffordStack.Application.Services;
using AffordStack.Application.Validators;
using AffordStack.Infrastructure.Data;
using AffordStack.Infrastructure.Services;
using System.Reflection;
using System.Text;

// Enable legacy timestamp behavior if needed across PostgreSQL Npgsql
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// 1. Database Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString, b =>
    {
        b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
    });
});
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

// 2. Register Application & Infrastructure Services
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IBackgroundEmailQueue, BackgroundEmailQueue>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPdfQuotationGenerator, PdfQuotationGenerator>();
builder.Services.AddHttpClient<ICashfreeService, CashfreeService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserAuthService, UserAuthService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<IEnquiryService, EnquiryService>();
builder.Services.AddScoped<IQuotationService, QuotationService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IContentService, ContentService>();

// 3. Background Services
builder.Services.AddHostedService<BackgroundEmailWorker>();
builder.Services.AddHostedService<EmailOutboxProcessor>();

// 4. FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

// 5. JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "SoftwareServices_SuperSecret_Jwt_Security_Key_2026_Minimum_32_Chars_Long!!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AffordStackPlatform";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AffordStackPlatformUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(5)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CustomerOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("user_type", "customer");
        policy.RequireRole("Customer");
    });

    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("user_type", "admin");
        policy.RequireRole(
            AffordStack.Domain.Enums.AdminRole.SuperAdmin.ToString(),
            AffordStack.Domain.Enums.AdminRole.Admin.ToString(),
            AffordStack.Domain.Enums.AdminRole.Manager.ToString()
        );
    });
});

// 6. CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "http://127.0.0.1:5173",
                "http://localhost:5000",
                "https://localhost:5001"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// 7. Controllers & JSON Options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// 8. Swagger / OpenAPI Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AffordStack Platform API",
        Version = "v1",
        Description = "Production-grade REST API backend for AffordStack Low-Cost Custom Software Platform (.NET 10 LTS + PostgreSQL)"
    });

    // JWT Bearer Security Definition
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

    // XML Comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// 9. Startup Database Migration & Seeding Check
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        if (db.Database.CanConnect())
        {
            try
            {
                logger.LogInformation("Applying pending database migrations...");
                await db.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully.");
            }
            catch (Exception migEx)
            {
                logger.LogWarning("Notice during automatic EF migrations: {Message}. Continuing to seeder and schema verification...", migEx.Message);
            }

            logger.LogInformation("Running database seeder and schema verification...");
            await DatabaseSeeder.SeedAsync(db, logger);
        }
        else
        {
            logger.LogWarning("PostgreSQL database is currently unreachable. Please ensure PostgreSQL is running.");
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning("Database initialization notice: {Message}. Make sure PostgreSQL is started and connection string is configured.", ex.Message);
    }
}

// 10. HTTP Pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Always enable Swagger documentation and UI so it works across environments and debugging sessions
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AffordStack Platform API v1");
    c.RoutePrefix = "swagger";
});

app.UseRouting();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Automatically redirect root URL "/" to Swagger UI in browser
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
