#pragma once
#include <Adafruit_BME680.h>
#include <ScioSense_ENS160.h>
#include <ArduinoJson.h>

struct SensorReadings {
    float temperature; // °C
    float humidity; // %
    float pressure; // hPa
    uint32_t gasResistance; // ohm

    uint16_t co2; // ppm
    uint16_t tvoc; // ppb
    uint8_t aqi; // 1-5

    bool bmeValid;
    bool ensValid;
    bool valid;
};

class SensorManager {
public:
    bool begin();
    bool read(SensorReadings& readings);
    void toJson(JsonDocument& doc, const char* hardwareId, unsigned long epochTime, const SensorReadings& readings) const;

private:
    Adafruit_BME680 _bme;
    ScioSense_ENS160 _ens;
    bool _bmeFound = false;
    bool _ensFound = false;

    bool initBme680();
    bool initEns160();
    void formatTimestamp(char* buffer, size_t size, unsigned long epochTime) const;
};
