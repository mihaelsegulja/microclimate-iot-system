using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Models;

namespace MicroclimateIotSystem.Application.Interfaces.Services;

public interface IAlertRuleService
{
    Task<PaginatedResponse<AlertRuleResponseDto>> GetAlertRulesAsync(PagingQueryParams paging, FilterQueryParams? filters, CancellationToken cancellationToken = default);
    Task<StandardResponse<AlertRuleResponseDto>> GetAlertRuleByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<StandardResponse<int>> CreateAlertRuleAsync(CreateAlertRuleRequestDto request, CancellationToken cancellationToken = default);
    Task<StandardResponse<bool>> ToggleAlertRuleActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);
    Task<StandardResponse<bool>> DeleteAlertRuleAsync(int id, CancellationToken cancellationToken = default);
}
