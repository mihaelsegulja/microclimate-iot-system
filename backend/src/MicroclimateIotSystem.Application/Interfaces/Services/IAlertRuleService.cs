using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Models;

namespace MicroclimateIotSystem.Application.Interfaces.Services;

public interface IAlertRuleService
{
    Task<PaginatedResponse<AlertRuleResponseDto>> GetAlertRulesAsync(PagingQueryParams paging, FilterQueryParams? filters);
    Task<StandardResponse<AlertRuleResponseDto>> GetAlertRuleByIdAsync(int id);
    Task<StandardResponse<int>> CreateAlertRuleAsync(CreateAlertRuleRequestDto request);
    Task<StandardResponse<bool>> ToggleAlertRuleActiveAsync(int id, bool isActive);
    Task<StandardResponse<bool>> DeleteAlertRuleAsync(int id);
}
