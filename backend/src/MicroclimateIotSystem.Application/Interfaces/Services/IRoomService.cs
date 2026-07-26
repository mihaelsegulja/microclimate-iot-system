using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Models;

namespace MicroclimateIotSystem.Application.Interfaces.Services;

public interface IRoomService
{
    Task<PaginatedResponse<RoomResponseDto>> GetRoomsAsync(PagingQueryParams pagingQueryParams, FilterQueryParams? filterQueryParams);
    Task<StandardResponse<RoomResponseDto>> GetRoomByIdAsync(int id);
    Task<PaginatedResponse<DeviceItemDto>> GetRoomDevicesAsync(int roomId, PagingQueryParams pagingQueryParams, FilterQueryParams? filterQueryParams);
    Task<StandardResponse<bool>> AssignDevicesToRoomAsync(int roomId, AssignDevicesRequestDto request);
    Task<StandardResponse<int>> CreateRoomAsync(CreateRoomRequestDto request);
    Task<StandardResponse<bool>> UpdateRoomAsync(int id, UpdateRoomRequestDto request);
    Task<StandardResponse<bool>> DeleteRoomAsync(int id);
    Task<StandardResponse<bool>> ToggleRoomActiveAsync(int id, bool isActive);
    Task<PaginatedResponse<LookupItemDto>> GetRoomsLookupAsync(LookupPagingQueryParams paging, FilterQueryParams? filters);
}
