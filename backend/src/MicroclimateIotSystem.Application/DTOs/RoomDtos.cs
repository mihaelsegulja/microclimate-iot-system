namespace MicroclimateIotSystem.Application.DTOs;

public record RoomResponseDto(
    int Id,
    string Name,
    string? Description,
    bool IsActive,
    int DeviceCount
);

public record CreateRoomRequestDto(
    string Name,
    string? Description,
    List<int>? DeviceIds
);

public record UpdateRoomRequestDto(
    string Name,
    string? Description,
    bool IsActive
);

public record DeviceItemDto(
    int Id,
    string HardwareId,
    string Name
);

public record AssignDevicesRequestDto(List<int> DeviceIds);
