using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Extensions;
using MicroclimateIotSystem.Application.Interfaces;
using MicroclimateIotSystem.Application.Interfaces.Services;
using MicroclimateIotSystem.Application.Models;
using MicroclimateIotSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MicroclimateIotSystem.Application.Services;

public class AlertRuleService(IAppDbContext db) : IAlertRuleService
{
    public async Task<PaginatedResponse<AlertRuleResponseDto>> GetAlertRulesAsync(
        PagingQueryParams paging, FilterQueryParams? filters, CancellationToken cancellationToken = default)
    {
        return await db.AlertRules
            .AsNoTracking()
            .Select(r => new AlertRuleResponseDto(
                r.Id,
                r.Name,
                r.TelemetryKey,
                r.Operator,
                r.ThresholdValue,
                r.IsActive,
                r.RoomId,
                r.Room != null ? r.Room.Name : null,
                r.DeviceId,
                r.Device != null ? r.Device.Name : null
            ))
            .ApplyFilters(filters?.FilterRules)
            .ToPaginatedResponseAsync(paging, cancellationToken);
    }

    public async Task<StandardResponse<AlertRuleResponseDto>> GetAlertRuleByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var dto = await db.AlertRules
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new AlertRuleResponseDto(
                r.Id,
                r.Name,
                r.TelemetryKey,
                r.Operator,
                r.ThresholdValue,
                r.IsActive,
                r.RoomId,
                r.Room != null ? r.Room.Name : null,
                r.DeviceId,
                r.Device != null ? r.Device.Name : null
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (dto == null)
            return StandardResponse<AlertRuleResponseDto>.NotFound($"Alert rule with id {id} not found.");

        return StandardResponse<AlertRuleResponseDto>.SuccessOk(dto);
    }

    public async Task<StandardResponse<int>> CreateAlertRuleAsync(
        CreateAlertRuleRequestDto request, CancellationToken cancellationToken = default)
    {
        var rule = new AlertRule
        {
            Name = request.Name,
            TelemetryKey = request.TelemetryKey,
            Operator = request.Operator,
            ThresholdValue = request.ThresholdValue,
            IsActive = true,
            RoomId = request.RoomId,
            DeviceId = request.DeviceId
        };

        db.AlertRules.Add(rule);
        await db.SaveChangesAsync(cancellationToken);

        return StandardResponse<int>.SuccessCreated(rule.Id, "Alert rule created successfully.");
    }

    public async Task<StandardResponse<bool>> ToggleAlertRuleActiveAsync(
        int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var rule = await db.AlertRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (rule == null)
            return StandardResponse<bool>.NotFound($"Alert rule with id {id} not found.");

        rule.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);

        return StandardResponse<bool>.SuccessOk(true, isActive ? "Alert rule activated." : "Alert rule deactivated.");
    }

    public async Task<StandardResponse<bool>> DeleteAlertRuleAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var rule = await db.AlertRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (rule == null)
            return StandardResponse<bool>.NotFound($"Alert rule with id {id} not found.");

        db.AlertRules.Remove(rule);
        await db.SaveChangesAsync(cancellationToken);

        return StandardResponse<bool>.SuccessOk(true, "Alert rule deleted successfully.");
    }
}
