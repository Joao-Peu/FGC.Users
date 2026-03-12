using System.Text;
using Azure.Messaging.ServiceBus;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using FGC.Users.API.Middlewares;
using FGC.Users.Application.Commands.AuthenticateUser;
using FGC.Users.Application.Commands.CreateUser;
using FGC.Users.Application.Commands.UpdateProfile;
using FGC.Users.Application.Interfaces;
using FGC.Users.Application.Queries.GetProfile;
using FGC.Users.Infrastructure.Audit;
using FGC.Users.Infrastructure.EventPublishers;
using FGC.Users.Infrastructure.Identity;
using FGC.Users.Infrastructure.Persistence;
using FGC.Users.Infrastructure.Persistence.Repositories;
using FGC.Users.Infrastructure.Security;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(opts => { opts.JsonSerializerOptions.PropertyNamingPolicy = null; });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FGC.Users API", Version = "v1" });
    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "JWT",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header"
    };
    c.AddSecurityDefinition("bearer", jwtScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtScheme, Array.Empty<string>() }
    });
});

// Database — Azure SQL Serverless (auto-pause/resume may take up to 60s)
var connectionString = configuration.GetValue<string>("ConnectionStrings:Default")
    ?? "Server=localhost,1433;Database=FGCUsersDb;User Id=sa;Password=Your_password123;TrustServerCertificate=true;";
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(connectionString, sql =>
        sql.EnableRetryOnFailure(
            maxRetryCount: 6,
            maxRetryDelay: TimeSpan.FromSeconds(60),
            errorNumbersToAdd: null)));

// CQRS Handlers
builder.Services.AddScoped<CreateUserCommandHandler>();
builder.Services.AddScoped<AuthenticateUserCommandHandler>();
builder.Services.AddScoped<UpdateProfileCommandHandler>();
builder.Services.AddScoped<GetProfileQueryHandler>();

// Infrastructure
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// Event Publisher
var serviceBusConn = configuration.GetValue<string>("ServiceBus:ConnectionString");
if (!string.IsNullOrEmpty(serviceBusConn))
{
    builder.Services.AddSingleton(_ => new ServiceBusClient(serviceBusConn));
    builder.Services.AddSingleton<IEventPublisher, ServiceBusEventPublisher>();
}
else
{
    builder.Services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();
}

// Authentication
var jwtIssuer = configuration.GetValue<string>("Jwt:Issuer") ?? "fgc.local";
var jwtAudience = configuration.GetValue<string>("Jwt:Audience") ?? "fgc.clients";
var jwtSecret = configuration.GetValue<string>("Jwt:Secret") ?? "super-secret-key-please-change";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,
        ValidateLifetime = true
    };
});

builder.Services.AddAuthorization();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserCommandValidator>();

// Azure Monitor / OpenTelemetry
var appInsightsConn = configuration.GetValue<string>("ApplicationInsights:ConnectionString");
if (!string.IsNullOrEmpty(appInsightsConn))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor(o =>
        o.ConnectionString = appInsightsConn);
}

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Middleware services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICorrelationContext, CorrelationContext>();

var app = builder.Build();

// Auto-apply migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Pipeline
app.UseMiddleware<CorrelationMiddleware>();
app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
