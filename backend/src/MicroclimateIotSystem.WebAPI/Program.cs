using System.Text.Json.Serialization;
using MicroclimateIotSystem.Application;
using MicroclimateIotSystem.Application.Configurations;
using MicroclimateIotSystem.Infrastructure;
using MicroclimateIotSystem.WebAPI.Abstractions;
using MicroclimateIotSystem.WebAPI.Endpoints;
using MicroclimateIotSystem.WebAPI.Extensions;
using MicroclimateIotSystem.WebAPI.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.AddSwagger();

builder.Services.Configure<AppConfig>(builder.Configuration.GetSection("AppConfig"));
builder.Services.Configure<PerformanceConfig>(builder.Configuration.GetSection("PerformanceConfig"));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMqConfig"));

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

var app = builder.Build();

app.UseExceptionHandler();

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

app.Run();
