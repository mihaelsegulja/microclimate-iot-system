using MicroclimateIotSystem.Application.Constants;
using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Interfaces;
using MicroclimateIotSystem.Domain.Entities;
using MicroclimateIotSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MicroclimateIotSystem.Application.Services;

public class AlertEvaluator(
    IAppDbContext db,
    ICacheService cache,
    IAlertBroadcaster broadcaster,
    ILogger<AlertEvaluator> logger) : IAlertEvaluator
{
    private static readonly TimeSpan RulesCacheTtl = TimeSpan.FromSeconds(60);

    /// <summary>Edge detector state: (ruleId, hardwareId) -> currently above/below threshold.</summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> State = new();

    private static volatile bool _hydrated;
    private static readonly SemaphoreSlim HydrateLock = new(1, 1);

    public async Task EvaluateAsync(
        int deviceId, int? roomId, string hardwareId, TelemetryReadingDto message, CancellationToken cancellationToken = default)
    {
        await EnsureHydratedAsync(cancellationToken);

        var rules = await GetActiveRulesAsync(cancellationToken);
        if (rules.Count == 0)
            return;

        foreach (var reading in message.Readings)
        {
            foreach (var rule in rules)
            {
                if (!string.Equals(rule.TelemetryKey, reading.Key, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!RuleAppliesToDevice(rule, deviceId, roomId))
                    continue;

                var now = Evaluate(rule.Operator, reading.Value, rule.ThresholdValue);
                var stateKey = $"{rule.Id}:{hardwareId}";
                var prev = State.TryGetValue(stateKey, out var p) && p;

                if (now && !prev)
                    await TriggerAsync(rule, deviceId, hardwareId, reading, message.Timestamp, stateKey, cancellationToken);
                else if (!now && prev)
                    await ClearAsync(rule, deviceId, hardwareId, message.Timestamp, stateKey, cancellationToken);
            }
        }
    }

    private static bool RuleAppliesToDevice(AlertRule rule, int deviceId, int? roomId)
    {
        if (rule.DeviceId.HasValue)
            return rule.DeviceId == deviceId;
        if (rule.RoomId.HasValue)
            return roomId.HasValue && rule.RoomId == roomId;
        return true;
    }

    private static bool Evaluate(AlertRuleOperator op, double value, double threshold) => op switch
    {
        AlertRuleOperator.GreaterThan => value > threshold,
        AlertRuleOperator.GreaterThanOrEqualTo => value >= threshold,
        AlertRuleOperator.LessThan => value < threshold,
        AlertRuleOperator.LessThanOrEqualTo => value <= threshold,
        _ => false,
    };

    private async Task TriggerAsync(
        AlertRule rule, int deviceId, string hardwareId, SensorReadingDto reading,
        DateTime timestamp, string stateKey, CancellationToken cancellationToken)
    {
        var alert = new Alert
        {
            AlertRuleId = rule.Id,
            DeviceId = deviceId,
            HardwareId = hardwareId,
            TelemetryKey = reading.Key,
            Unit = reading.Unit,
            Value = reading.Value,
            ThresholdValue = rule.ThresholdValue,
            Operator = rule.Operator,
            Status = AlertStatus.Active,
            TriggeredAt = timestamp,
        };

        db.Alerts.Add(alert);
        await db.SaveChangesAsync(cancellationToken);

        State[stateKey] = true;
        logger.LogInformation("Alert triggered for device {HardwareId} key {Key} (value {Value})",
            hardwareId, reading.Key, reading.Value);

        await BroadcastAsync(alert, rule, AlertStatus.Active, timestamp, cancellationToken);
    }

    private async Task ClearAsync(
        AlertRule rule, int deviceId, string hardwareId, DateTime timestamp, string stateKey, CancellationToken cancellationToken)
    {
        State[stateKey] = false;

        var open = await db.Alerts
            .Where(a => a.AlertRuleId == rule.Id && a.DeviceId == deviceId && a.Status == AlertStatus.Active)
            .OrderByDescending(a => a.TriggeredAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (open is null)
        {
            logger.LogDebug("Clear edge for rule {RuleId} but no open alert; ignoring", rule.Id);
            return;
        }

        open.Status = AlertStatus.Cleared;
        open.ClearedAt = timestamp;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Alert cleared for device {HardwareId} rule {RuleId}", hardwareId, rule.Id);
        await BroadcastAsync(open, rule, AlertStatus.Cleared, timestamp, cancellationToken);
    }

    private async Task BroadcastAsync(
        Alert alert, AlertRule rule, AlertStatus status, DateTime timestamp, CancellationToken cancellationToken)
    {
        try
        {
            await broadcaster.BroadcastAsync(
                new AlertEventDto(
                    alert.Id, rule.Id, rule.Name, alert.DeviceId, alert.HardwareId,
                    alert.TelemetryKey, alert.Unit, alert.Value, alert.ThresholdValue,
                    alert.Operator, status, timestamp),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Alert broadcast failed for rule {RuleId}", rule.Id);
        }
    }

    private async Task<List<AlertRule>> GetActiveRulesAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGet(CacheKeys.ActiveAlertRules(), out List<AlertRule>? cached) && cached is not null)
            return cached;

        var rules = await db.AlertRules
            .AsNoTracking()
            .Where(r => r.IsActive)
            .ToListAsync(cancellationToken);

        cache.Set(CacheKeys.ActiveAlertRules(), rules, RulesCacheTtl);
        return rules;
    }

    private async Task EnsureHydratedAsync(CancellationToken cancellationToken)
    {
        if (_hydrated)
            return;

        await HydrateLock.WaitAsync(cancellationToken);
        try
        {
            if (_hydrated)
                return;

            var open = await db.Alerts
                .AsNoTracking()
                .Where(a => a.Status == AlertStatus.Active)
                .Select(a => new { a.AlertRuleId, a.HardwareId })
                .ToListAsync(cancellationToken);

            foreach (var a in open)
                State[$"{a.AlertRuleId}|{a.HardwareId}"] = true;

            _hydrated = true;
        }
        finally
        {
            HydrateLock.Release();
        }
    }
}