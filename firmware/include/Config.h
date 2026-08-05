#pragma once

#define DEFAULT_INTERVAL_SEC 60
#define NVS_NAMESPACE "microclimate"
#define NVS_KEY_INTERVAL "interval"

#define PUB_TOPIC_PREFIX "devices/"
#define PUB_TOPIC_SUFFIX "/telemetry"
#define SUB_TOPIC_PREFIX "devices/"
#define SUB_TOPIC_SUFFIX "/commands"

#define WIFI_RETRY_INITIAL_MS 1000
#define WIFI_RETRY_MAX_MS 30000
#define MQTT_RETRY_MS 5000

#define SENSOR_READ_TIMEOUT_MS 5000
