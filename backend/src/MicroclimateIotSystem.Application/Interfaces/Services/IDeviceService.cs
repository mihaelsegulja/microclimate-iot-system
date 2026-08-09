using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Models;

namespace MicroclimateIotSystem.Application.Interfaces.Services;

public interface IDeviceService
{
    Task<PaginatedResponse<DeviceResponseDto>> GetDevicesAsync(PagingQueryParams pagingQueryParams, FilterQueryParams? filterQueryParams, CancellationToken cancellationToken = default);
    Task<StandardResponse<DeviceResponseDto>> GetDeviceByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<StandardResponse<int>> CreateDeviceAsync(CreateDeviceRequestDto request, CancellationToken cancellationToken = default);
    Task<StandardResponse<bool>> UpdateDeviceAsync(int id, UpdateDeviceRequestDto request, CancellationToken cancellationToken = default);
    Task<StandardResponse<bool>> DeleteDeviceAsync(int id, CancellationToken cancellationToken = default);
    Task<StandardResponse<bool>> ToggleDeviceActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<LookupItemDto>> GetDevicesLookupAsync(LookupPagingQueryParams paging, FilterQueryParams? filters, bool? available = null, CancellationToken cancellationToken = default);
    Task<StandardResponse<bool>> UpdateDeviceConfigAsync(int id, DeviceConfigRequestDto request, CancellationToken cancellationToken = default);
    Task<StandardResponse<bool>> RebootDeviceAsync(int id, CancellationToken cancellationToken = default);
}
