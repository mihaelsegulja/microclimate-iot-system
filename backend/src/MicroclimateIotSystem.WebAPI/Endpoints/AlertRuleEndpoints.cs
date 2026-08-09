using System.Text.Json;
using System.Text.Json.Serialization;
using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Interfaces.Services;
using MicroclimateIotSystem.Application.Models;
using MicroclimateIotSystem.WebAPI.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace MicroclimateIotSystem.WebAPI.Endpoints;

public static class AlertRuleEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static IEndpointRouteBuilder MapAlertRuleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/alert-rules")
                       .WithTags("Alert Rules")
                       .RequireAuthorization();

        group.MapGet("/", GetAllAsync).WithName("GetAlertRules").WithOpenApi();
        group.MapGet("/{id:int}", GetByIdAsync).WithName("GetAlertRuleById").WithOpenApi();
        group.MapPost("/", CreateAsync).WithName("CreateAlertRule").WithOpenApi();
        group.MapPatch("/{id:int}/active", ToggleActiveAsync).WithName("ToggleAlertRuleActive").WithOpenApi();
        group.MapDelete("/{id:int}", DeleteAsync).WithName("DeleteAlertRule").WithOpenApi();

        return app;
    }

    private static async Task<IResult> GetAllAsync(
        [AsParameters] PagingQueryParams paging,
        [FromQuery] string? filters,
        IAlertRuleService alertRuleService,
        CancellationToken cancellationToken)
    {
        FilterQueryParams? filterParams = null;
        if (!string.IsNullOrWhiteSpace(filters))
        {
            var rules = JsonSerializer.Deserialize<List<FilterRule>>(filters, JsonOptions);
            if (rules != null)
                filterParams = new FilterQueryParams(rules);
        }

        var response = await alertRuleService.GetAlertRulesAsync(paging, filterParams, cancellationToken);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> GetByIdAsync(
        int id,
        IAlertRuleService alertRuleService,
        CancellationToken cancellationToken)
    {
        var response = await alertRuleService.GetAlertRuleByIdAsync(id, cancellationToken);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateAlertRuleRequestDto request,
        IAlertRuleService alertRuleService,
        CancellationToken cancellationToken)
    {
        var response = await alertRuleService.CreateAlertRuleAsync(request, cancellationToken);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> ToggleActiveAsync(
        int id,
        [FromBody] ToggleActiveRequestDto request,
        IAlertRuleService alertRuleService,
        CancellationToken cancellationToken)
    {
        var response = await alertRuleService.ToggleAlertRuleActiveAsync(id, request.IsActive, cancellationToken);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> DeleteAsync(
        int id,
        IAlertRuleService alertRuleService,
        CancellationToken cancellationToken)
    {
        var response = await alertRuleService.DeleteAlertRuleAsync(id, cancellationToken);
        return ResultHandler.Handle(response);
    }
}
