namespace MicroclimateIotSystem.Application.Models.Messaging;

public record DeviceCommandMessage(
    string CommandId,
    string HardwareId,
    string CommandType,
    DateTime SentAt,
    object? Payload
);
