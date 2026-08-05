#pragma once
#include <WiFi.h>
#include "Config.h"

class WiFiManager {
public:
    enum State : uint8_t {
        DISCONNECTED,
        CONNECTING,
        CONNECTED
    };

    void begin(const char* ssid, const char* password);
    void handle();
    bool isConnected() const;
    State getState() const;

private:
    State _state = DISCONNECTED;
    const char* _ssid = nullptr;
    const char* _password = nullptr;
    unsigned long _lastAttemptMs = 0;
    unsigned long _retryDelayMs = WIFI_RETRY_INITIAL_MS;

    void connect();
    static void onEvent(WiFiEvent_t event, arduino_event_info_t info);
    static WiFiManager* _instance;
};
