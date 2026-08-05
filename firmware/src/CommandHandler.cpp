#include "CommandHandler.h"
#include "NvsManager.h"

void CommandHandler::begin(NvsManager* nvs) {
    _nvs = nvs;
}

void CommandHandler::setForceReadCallback(ForceReadCallback cb) {
    _forceReadCb = cb;
}

bool CommandHandler::handle(const char* topic, JsonDocument& doc) {
    const char* commandType = doc["commandType"];

    if (!commandType) {
        Serial.println("[CMD] Missing commandType");
        return false;
    }

    Serial.printf("[CMD] Received: %s\n", commandType);

    if (strcmp(commandType, "UPDATE_CONFIG") == 0) {
        JsonObject payload = doc["payload"];
        if (!payload.isNull()) {
            handleUpdateConfig(payload);
        }
        return true;
    }

    if (strcmp(commandType, "REBOOT") == 0) {
        handleReboot();
        return true;
    }

    if (strcmp(commandType, "FORCE_READ") == 0) {
        handleForceRead();
        return true;
    }

    Serial.printf("[CMD] Unknown command: %s\n", commandType);
    return false;
}

void CommandHandler::handleUpdateConfig(JsonObject payload) {
    if (payload["telemetryIntervalSeconds"].is<uint32_t>()) {
        uint32_t interval = payload["telemetryIntervalSeconds"];
        _nvs->saveInterval(interval);
        Serial.printf("[CMD] Interval updated to %u s\n", interval);
    }
}

void CommandHandler::handleReboot() {
    Serial.println("[CMD] Rebooting in 1s...");
    delay(1000);
    ESP.restart();
}

void CommandHandler::handleForceRead() {
    Serial.println("[CMD] Force read triggered");
    if (_forceReadCb) {
        _forceReadCb();
    }
}
