#pragma once
#include <ArduinoJson.h>
#include <cstdint>

class NvsManager;

class CommandHandler {
public:
    void begin(NvsManager* nvs);
    bool handle(const char* topic, JsonDocument& doc);

private:
    NvsManager* _nvs = nullptr;

    void handleUpdateConfig(JsonObject payload);
    void handleReboot();
};
