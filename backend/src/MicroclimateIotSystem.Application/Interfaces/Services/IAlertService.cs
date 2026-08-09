using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Models;
using MicroclimateIotSystem.Domain.Enums;

namespace MicroclimateIotSystem.Application.Interfaces.Services;

public interface IAlertService
{
    Task<PaginatedResponse<AlertResponseDto>> GetAlertsAsync(
        PagingQueryParams paging,
        AlertStatus? status,
        int? deviceId,
        int? ruleId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);
}