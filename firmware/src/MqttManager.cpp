#include "MqttManager.h"
#include "Secrets.h"
#include "Config.h"

MqttManager* MqttManager::_instance = nullptr;

void MqttManager::begin(const char* broker, uint16_t port) {
    _broker = broker;
    _port = port;
    _instance = this;

    _client.setClient(_wifiClient);
    _client.setCallback(onMessage);
    _client.setServer(_broker, _port);
}

void MqttManager::setHardwareId(const char* hwId) {
    _hardwareId = hwId;
}

void MqttManager::setCallback(Callback cb) {
    _callback = cb;
}

void MqttManager::handle() {
    if (!_client.connected()) {
        unsigned long now = millis();
        if (now - _lastConnMs >= MQTT_RETRY_MS) {
            connect();
        }
        return;
    }

    _client.loop();
}

bool MqttManager::isConnected() {
    return _client.connected();
}

bool MqttManager::publish(const char* topic, const JsonDocument& doc) {
    if (!_client.connected()) return false;

    String payload;
    serializeJson(doc, payload);

    bool ok = _client.publish(topic, payload.c_str(), true);
    if (ok) {
        Serial.printf("[MQTT] Published to %s (%u bytes)\n", topic, payload.length());
    } else {
        Serial.printf("[MQTT] FAILED to publish to %s\n", topic);
    }
    return ok;
}

bool MqttManager::subscribe(const char* topic) {
    if (!_client.connected()) return false;
    bool ok = _client.subscribe(topic);
    if (ok) {
        Serial.printf("[MQTT] Subscribed to %s\n", topic);
    }
    return ok;
}

void MqttManager::connect() {
    _lastConnMs = millis();

    if (!_hardwareId || strlen(_hardwareId) == 0) {
        Serial.println("[MQTT] Cannot connect: hardwareId not set");
        return;
    }

    Serial.printf("[MQTT] Connecting to %s:%d as %s...\n", _broker, _port, _hardwareId);

    if (_client.connect(_hardwareId, MQTT_USER, MQTT_PASSWORD)) {
        Serial.println("[MQTT] Connected");
        String cmdTopic = SUB_TOPIC_PREFIX;
        cmdTopic += _hardwareId;
        cmdTopic += SUB_TOPIC_SUFFIX;
        subscribe(cmdTopic.c_str());
    } else {
        Serial.printf("[MQTT] Connection failed, rc=%d\n", _client.state());
    }
}

void MqttManager::onMessage(char* topic, byte* payload, unsigned int length) {
    if (!_instance) return;
    if (!_instance->_callback) return;

    String jsonStr;
    jsonStr.reserve(length + 1);
    for (unsigned int i = 0; i < length; i++) {
        jsonStr += (char)payload[i];
    }

    JsonDocument doc;
    DeserializationError err = deserializeJson(doc, jsonStr);
    if (err) {
        Serial.printf("[MQTT] Parse error on %s: %s\n", topic, err.c_str());
        return;
    }

    Serial.printf("[MQTT] Received on %s: %s\n", topic, jsonStr.c_str());
    _instance->_callback(topic, doc);
}
