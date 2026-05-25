using System.Text.Json.Serialization;

namespace MicroclimateIotSystem.Application.DTOs.Auth;

public class AuthResponseDto
{
    public string AccessToken { get; set; } = null!;

    [JsonIgnore]
    public string RefreshToken { get; set; } = null!;
}

