#include "WiFiManager.h"
#include "Config.h"

WiFiManager* WiFiManager::_instance = nullptr;

void WiFiManager::begin(const char* ssid, const char* password) {
    _ssid = ssid;
    _password = password;
    _instance = this;

    WiFi.onEvent(onEvent);
    WiFi.setAutoReconnect(false);

    connect();
}

void WiFiManager::handle() {
    if (_state == CONNECTED) return;
    if (_state == CONNECTING) return;

    unsigned long now = millis();
    if (now - _lastAttemptMs >= _retryDelayMs) {
        connect();
    }
}

bool WiFiManager::isConnected() const {
    return _state == CONNECTED;
}

WiFiManager::State WiFiManager::getState() const {
    return _state;
}

void WiFiManager::connect() {
    _state = CONNECTING;
    _lastAttemptMs = millis();
    Serial.printf("[WiFi] Connecting to %s...\n", _ssid);
    WiFi.begin(_ssid, _password);
}

void WiFiManager::onEvent(WiFiEvent_t event, arduino_event_info_t info) {
    switch (event) {
        case ARDUINO_EVENT_WIFI_STA_GOT_IP:
            Serial.printf("[WiFi] Connected, IP: %s\n", WiFi.localIP().toString().c_str());
            _instance->_state = CONNECTED;
            _instance->_retryDelayMs = WIFI_RETRY_INITIAL_MS;
            break;

        case ARDUINO_EVENT_WIFI_STA_DISCONNECTED:
            if (_instance->_state == CONNECTED) {
                Serial.printf("[WiFi] Disconnected, reason: %d\n", info.wifi_sta_disconnected.reason);
            }
            _instance->_state = DISCONNECTED;
            _instance->_retryDelayMs = min(
                _instance->_retryDelayMs * 2,
                (unsigned long)WIFI_RETRY_MAX_MS
            );
            break;

        default:
            break;
    }
}
