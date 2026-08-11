using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Models;

namespace MicroclimateIotSystem.Application.Interfaces.Services;

public interface IDashboardService
{
    Task<StandardResponse<DashboardTelemetryDto>> GetRoomTelemetryAsync(
        int roomId, DateTime? from, DateTime? to, int maxPoints,
        CancellationToken cancellationToken = default);
}