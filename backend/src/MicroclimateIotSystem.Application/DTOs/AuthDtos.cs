using System.Text.Json.Serialization;

namespace MicroclimateIotSystem.Application.DTOs;

public record AuthResponseDto(
    string AccessToken,
    [property: JsonIgnore] string RefreshToken
);

public record LoginRequestDto(
    string Username,
    string Password
);

public record RefreshTokenRequestDto(
    string RefreshToken
);

public record RegisterRequestDto(
    string Username,
    string Password
);
