using System.Text.Json.Serialization;
using MicroclimateIotSystem.Application;
using MicroclimateIotSystem.Application.Configurations;
using MicroclimateIotSystem.Application.Interfaces;
using MicroclimateIotSystem.Infrastructure;
using MicroclimateIotSystem.WebAPI.Abstractions;
using MicroclimateIotSystem.WebAPI.Endpoints;
using MicroclimateIotSystem.WebAPI.Extensions;
using MicroclimateIotSystem.WebAPI.Hubs;
using MicroclimateIotSystem.WebAPI.Middleware;
using MicroclimateIotSystem.WebAPI.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.AddSwagger();

builder.Services.Configure<AppOptions>(builder.Configuration.GetSection("AppOptions"));
builder.Services.Configure<PerformanceOptions>(builder.Configuration.GetSection("PerformanceOptions"));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMqOptions"));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection("CorsOptions"));

var corsOptions = builder.Configuration.GetSection("CorsOptions").Get<CorsOptions>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOptions!.AllowedOrigins.ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddSingleton<ITelemetryBroadcaster, TelemetryBroadcaster>();
builder.Services.AddSingleton<IAlertBroadcaster, AlertBroadcaster>();

var app = builder.Build();

app.UseExceptionHandler();

app.UseCors();

app.UseMiddleware<PerformanceLogMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    app.ApplyMigrations();
    await app.SeedDataAsync();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapDeviceEndpoints();
app.MapRoomEndpoints();
app.MapDashboardEndpoints();
app.MapAlertRuleEndpoints();
app.MapAlertEndpoints();

app.MapHub<NotificationHub>("/hubs/telemetry").RequireAuthorization();

app.Run();
