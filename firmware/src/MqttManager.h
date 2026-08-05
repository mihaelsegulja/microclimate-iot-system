#pragma once
#include <WiFi.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>

class MqttManager {
public:
    using Callback = std::function<void(const char* topic, JsonDocument& doc)>;

    void begin(const char* broker, uint16_t port);
    void setHardwareId(const char* hwId);
    void setCallback(Callback cb);
    void handle();
    bool isConnected();
    bool publish(const char* topic, const JsonDocument& doc);
    bool subscribe(const char* topic);

private:
    WiFiClient _wifiClient;
    PubSubClient _client;
    const char* _broker = nullptr;
    uint16_t _port = 1883;
    const char* _hardwareId = nullptr;
    Callback _callback = nullptr;
    unsigned long _lastConnMs = 0;

    void connect();
    static void onMessage(char* topic, byte* payload, unsigned int length);
    static MqttManager* _instance;
};
