using ClassmateApii.BackgroundServices;
using ClassmateApii.Data;
using ClassmateApii.Middleware;
using ClassmateApii.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Text;

// Roman Urdu: Yahan se application ka startup flow shuru hota hai.
DotNetEnv.Env.Load();
var builder = WebApplication.CreateBuilder(args);

// Add Environment Variables to the config (this ensures the .env values are included)
builder.Configuration.AddEnvironmentVariables();
// --- SERVICES CONFIGURATION ---

// Roman Urdu: Database ko dependency injection ke sath register kar rahe hain.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.MigrationsAssembly("ClassmateApii")));

// Roman Urdu: JWT authentication ko configure kar rahe hain.
var jwtSecret = builder.Configuration["Jwt:Secret"] 
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("JWT auth failed: {Message}", ctx.Exception.Message);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.Configure<WebhookOptions>(
    builder.Configuration.GetSection("Webhook"));

// Roman Urdu: Frontend se safe requests allow karne ke liye CORS policy set kar rahe hain.
var frontendUrl = builder.Configuration["Frontend:Url"] ?? "http://localhost:3000";

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(frontendUrl)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});
 
// ── 2. HttpClient factory register karo (agar already nahi hai) ───────────────
builder.Services.AddHttpClient();
// Roman Urdu: Business logic wali services ko register kar rahe hain.
builder.Services.AddScoped<IClassroomRegistrationService, ClassroomRegistrationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IClassroomService, ClassroomService>();
builder.Services.AddScoped<IJobQueueService, JobQueueService>();
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(
        builder.Configuration.GetConnectionString("Redis")
        ?? "localhost:6379"));
builder.Services.AddControllers();
// ── 4. Daily renewal background service register karo ────────────────────────
builder.Services.AddHostedService<RegistrationRenewalService>();
 
// Roman Urdu: Swagger/OpenAPI support for .NET 10
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ClassMate AI API",
        Version = "v1",
        Description = "Backend API for ClassMate AI — automated Google Classroom assignment drafting."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer token. Format: **Bearer <token>**",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHttpContextAccessor();

// --- PIPELINE CONFIGURATION ---

var app = builder.Build();

// Roman Urdu: Middleware order important hai. Error handler sab se pehle.
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ClassMate AI v1");
        c.RoutePrefix = "swagger"; // URL: /swagger
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");    // Must be before Auth
app.UseAuthentication();        // Validate who the user is
app.UseAuthorization();         // Validate what they can do

app.MapControllers();

// Roman Urdu: Dev mode mein automatic migration apply karna.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        log.LogWarning("Auto-migration skipped: {Message}", ex.Message);
    }
}

app.Run();

// Roman Urdu: Testing ke liye class exposure.
public partial class Program { }