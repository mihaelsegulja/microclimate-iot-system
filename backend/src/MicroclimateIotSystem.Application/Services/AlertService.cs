using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Extensions;
using MicroclimateIotSystem.Application.Interfaces;
using MicroclimateIotSystem.Application.Interfaces.Services;
using MicroclimateIotSystem.Application.Models;
using MicroclimateIotSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MicroclimateIotSystem.Application.Services;

public class AlertService(IAppDbContext db) : IAlertService
{
    public async Task<PaginatedResponse<AlertResponseDto>> GetAlertsAsync(
        PagingQueryParams paging,
        AlertStatus? status,
        int? deviceId,
        int? ruleId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var query = db.Alerts
            .AsNoTracking()
            .Where(a => status == null || a.Status == status)
            .Where(a => deviceId == null || a.DeviceId == deviceId)
            .Where(a => ruleId == null || a.AlertRuleId == ruleId)
            .Where(a => from == null || a.TriggeredAt >= from)
            .Where(a => to == null || a.TriggeredAt <= to)
            .OrderByDescending(a => a.TriggeredAt)
            .Select(a => new AlertResponseDto(
                a.Id,
                a.AlertRuleId,
                a.AlertRule.Name,
                a.DeviceId,
                a.Device.Name,
                a.HardwareId,
                a.TelemetryKey,
                a.Unit,
                a.Value,
                a.ThresholdValue,
                a.Operator,
                a.Status,
                a.TriggeredAt,
                a.ClearedAt));

        return await query.ToPaginatedResponseAsync(paging, cancellationToken);
    }
}