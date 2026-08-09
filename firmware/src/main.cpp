#include <Arduino.h>
#include <NTPClient.h>
#include <WiFiUdp.h>

#include "Secrets.h"
#include "Config.h"
#include "NvsManager.h"
#include "WiFiManager.h"
#include "MqttManager.h"
#include "SensorManager.h"
#include "CommandHandler.h"

NvsManager nvs;
WiFiManager wifi;
MqttManager mqtt;
SensorManager sensors;
CommandHandler cmdHandler;

WiFiUDP ntpUdp;
NTPClient ntpClient(ntpUdp, "pool.ntp.org", 0, 60000);

char hardwareId[24];
char pubTopic[64];
char subTopic[64];

volatile bool forceReadFlag = false;
unsigned long lastReadMs = 0;
uint32_t telemetryIntervalSec = DEFAULT_INTERVAL_SEC;

void onForceRead() {
    forceReadFlag = true;
}

void onMqttMessage(const char* topic, JsonDocument& doc) {
    cmdHandler.handle(topic, doc);

    const char* cmdType = doc["commandType"];
    if (cmdType && strcmp(cmdType, "UPDATE_CONFIG") == 0) {
        telemetryIntervalSec = nvs.loadInterval();
        Serial.printf("[MAIN] Telemetry interval updated to %u s\n", telemetryIntervalSec);
    }
}

void setup() {
    Serial.begin(115200);
    delay(1000);
    Serial.printf("\n\n[BOOT] Microclimate IoT Sensor\n");

    nvs.begin();
    telemetryIntervalSec = nvs.loadInterval();
    Serial.printf("[BOOT] Telemetry interval: %u s\n", telemetryIntervalSec);

    uint8_t mac[6];
    esp_read_mac(mac, ESP_MAC_WIFI_STA);
    snprintf(hardwareId, sizeof(hardwareId), "ESP32-%02x%02x%02x%02x%02x%02x",
        mac[0], mac[1], mac[2], mac[3], mac[4], mac[5]);
    Serial.printf("[BOOT] Hardware ID: %s\n", hardwareId);

    snprintf(pubTopic, sizeof(pubTopic), "%s%s%s", PUB_TOPIC_PREFIX, hardwareId, PUB_TOPIC_SUFFIX);
    snprintf(subTopic, sizeof(subTopic), "%s%s%s", SUB_TOPIC_PREFIX, hardwareId, SUB_TOPIC_SUFFIX);
    Serial.printf("[BOOT] Publish topic: %s\n", pubTopic);
    Serial.printf("[BOOT] Subscribe topic: %s\n", subTopic);

    sensors.begin();

    wifi.begin(WIFI_SSID, WIFI_PASSWORD);
    
    ntpClient.begin();

    mqtt.begin(MQTT_BROKER, MQTT_PORT);
    mqtt.setHardwareId(hardwareId);
    mqtt.setCallback(onMqttMessage);

    cmdHandler.begin(&nvs);
    cmdHandler.setForceReadCallback(onForceRead);

    Serial.println("[BOOT] Setup complete");
}

void loop() {
    wifi.handle();
    mqtt.handle();
    ntpClient.update();

    bool readyToRead = forceReadFlag ||
        (mqtt.isConnected() && millis() - lastReadMs >= telemetryIntervalSec * 1000UL);

    if (readyToRead) {
        forceReadFlag = false;

        SensorReadings readings;
        if (!sensors.read(readings)) {
            Serial.println("[MAIN] Sensor read failed, skipping publish");
            return;
        }

        if (!ntpClient.isTimeSet()) {
            Serial.println("[MAIN] NTP time not set yet, using millis() as fallback");
        }

        unsigned long epoch = ntpClient.isTimeSet()
            ? ntpClient.getEpochTime()
            : 0;

        JsonDocument doc;
        sensors.toJson(doc, hardwareId, epoch, readings);

        mqtt.publish(pubTopic, doc);

        lastReadMs = millis();
    }
}
