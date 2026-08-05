using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Models;

namespace MicroclimateIotSystem.Application.Interfaces.Services;

public interface ITelemetryService
{
    Task<StandardResponse<List<ChartSeriesDto>>> GetChartAsync(
        int deviceId,
        DateTime? from,
        DateTime? to,
        IReadOnlyList<string>? keys,
        CancellationToken cancellationToken = default);

    Task<StandardResponse<LatestTelemetryDto>> GetLatestAsync(int deviceId, CancellationToken cancellationToken = default);
}
