using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Models;

namespace MicroclimateIotSystem.Application.Interfaces.Services;

public interface IDeviceService
{
    Task<PaginatedResponse<DeviceResponseDto>> GetDevicesAsync(PagingQueryParams pagingQueryParams, FilterQueryParams? filterQueryParams);
    Task<StandardResponse<DeviceResponseDto>> GetDeviceByIdAsync(int id);
    Task<StandardResponse<int>> CreateDeviceAsync(CreateDeviceRequestDto request);
    Task<StandardResponse<bool>> UpdateDeviceAsync(int id, UpdateDeviceRequestDto request);
    Task<StandardResponse<bool>> DeleteDeviceAsync(int id);
    Task<StandardResponse<bool>> ToggleDeviceActiveAsync(int id, bool isActive);
    Task<PaginatedResponse<LookupItemDto>> GetDevicesLookupAsync(LookupPagingQueryParams paging, FilterQueryParams? filters, bool? available = null);
}