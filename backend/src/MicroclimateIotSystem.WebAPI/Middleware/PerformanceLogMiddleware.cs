using System.Diagnostics;
using MicroclimateIotSystem.Application.Configurations;
using Microsoft.Extensions.Options;

namespace MicroclimateIotSystem.WebAPI.Middleware;

public class PerformanceLogMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceLogMiddleware> _logger;
    private readonly PerformanceOptions _options;
    
    public PerformanceLogMiddleware(RequestDelegate next, ILogger<PerformanceLogMiddleware> logger,  IOptions<PerformanceOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var duration = stopwatch.ElapsedMilliseconds;
            
            var logLevel = duration > _options.WarningThresholdInMilliseconds ? LogLevel.Warning : LogLevel.Information;

            _logger.Log(logLevel,
                "HTTP {Method} {Path} responded {StatusCode} in {Duration}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                duration);
        }
    }
}