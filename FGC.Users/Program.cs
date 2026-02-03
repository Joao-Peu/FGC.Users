using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using FluentValidation.AspNetCore;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry.Logs;
using Azure.Messaging.ServiceBus;
using FGC.Users.Infrastructure;
using FGC.Users.Application.Interfaces;
using FGC.Users.Application.Services;
using FGC.Users.Application.Validators;
using FGC.Users.Infrastructure.EventPublishers;
using FGC.Users.API.Middlewares;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var configuration = builder.Configuration;
var env = builder.Environment;

// Add services
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
        { jwtScheme, new string[] { } }
    });
});

// DB
var connectionString = configuration.GetValue<string>("ConnectionStrings:Default") ?? "Server=localhost,1433;Database=FGCUsersDb;User Id=sa;Password=Your_password123;TrustServerCertificate=true;";
builder.Services.AddDbContext<ApplicationDbContext>(opt => opt.UseSqlServer(connectionString));

// Application/infrastructure DI
builder.Services.AddScoped<IUserService, FGC.Users.Application.Services.UserService>();
builder.Services.AddScoped<IAuditService, FGC.Users.Infrastructure.AuditService>();

// Service Bus client factory - optional real implementation
var serviceBusConn = configuration.GetValue<string>("ServiceBus:ConnectionString");
if (!string.IsNullOrEmpty(serviceBusConn))
{
    builder.Services.AddSingleton(_ => new ServiceBusClient(serviceBusConn));
    builder.Services.AddSingleton<IEventPublisher, FGC.Users.Infrastructure.EventPublishers.ServiceBusEventPublisher>();
}
else
{
    builder.Services.AddSingleton<IEventPublisher, FGC.Users.Infrastructure.EventPublishers.InMemoryEventPublisher>();
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

// FluentValidation - register validators from assembly
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterValidator>();

// OpenTelemetry
var serviceName = "FGC.Users";
var serviceVersion = "1.0.0";

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName: serviceName, serviceVersion: serviceVersion))
            .AddAspNetCoreInstrumentation();

        var otlpEndpoint = configuration.GetValue<string>("OpenTelemetry:OtlpEndpoint");
        if (!string.IsNullOrEmpty(otlpEndpoint))
        {
            tracerProviderBuilder.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
        }
    });

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
});

// Middleware, services
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ICorrelationContext, CorrelationContext>();

var app = builder.Build();

// Ensure DB created & migrations applied on startup in dev
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Configure pipeline
app.UseMiddleware<CorrelationMiddleware>();
app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
