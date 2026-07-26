namespace MicroclimateIotSystem.Application.DTOs;

public record ToggleActiveRequestDto(bool IsActive);

public record LookupItemDto(int Id, string Display, bool IsActive);
