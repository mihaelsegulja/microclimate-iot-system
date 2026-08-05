#pragma once
#include <ArduinoJson.h>
#include <cstdint>

class NvsManager;

class CommandHandler {
public:
    using ForceReadCallback = void(*)();

    void begin(NvsManager* nvs);
    void setForceReadCallback(ForceReadCallback cb);
    bool handle(const char* topic, JsonDocument& doc);

private:
    NvsManager* _nvs = nullptr;
    ForceReadCallback _forceReadCb = nullptr;

    void handleUpdateConfig(JsonObject payload);
    void handleReboot();
    void handleForceRead();
};
