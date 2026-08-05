using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Models;

namespace MicroclimateIotSystem.Application.Interfaces.Services;

public interface IRoomService
{
    Task<PaginatedResponse<RoomResponseDto>> GetRoomsAsync(PagingQueryParams pagingQueryParams, FilterQueryParams? filterQueryParams, CancellationToken cancellationToken = default);
    Task<StandardResponse<RoomResponseDto>> GetRoomByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<DeviceItemDto>> GetRoomDevicesAsync(int roomId, PagingQueryParams pagingQueryParams, FilterQueryParams? filterQueryParams, CancellationToken cancellationToken = default);
    Task<StandardResponse<bool>> AssignDevicesToRoomAsync(int roomId, AssignDevicesRequestDto request, CancellationToken cancellationToken = default);
    Task<StandardResponse<int>> CreateRoomAsync(CreateRoomRequestDto request, CancellationToken cancellationToken = default);
    Task<StandardResponse<bool>> UpdateRoomAsync(int id, UpdateRoomRequestDto request, CancellationToken cancellationToken = default);
    Task<StandardResponse<bool>> DeleteRoomAsync(int id, CancellationToken cancellationToken = default);
    Task<StandardResponse<bool>> ToggleRoomActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<LookupItemDto>> GetRoomsLookupAsync(LookupPagingQueryParams paging, FilterQueryParams? filters, CancellationToken cancellationToken = default);
}
