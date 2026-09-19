#include "SensorManager.h"
#include "Config.h"
#include <Wire.h>

#define BME680_I2C_ADDR_0  0x76
#define BME680_I2C_ADDR_1  0x77

namespace {
float roundToTwoDecimals(float value) {
    return roundf(value * 100.0f) / 100.0f;
}
}

bool SensorManager::begin() {
    Wire.begin();

    _bmeFound = initBme680();
    _ensFound = initEns160();

    if (!_bmeFound && !_ensFound) {
        Serial.println("[SENSOR] No sensors found on I2C bus");
        return false;
    }

    Serial.printf("[SENSOR] BME680: %s, ENS160: %s\n",
        _bmeFound ? "OK" : "MISSING",
        _ensFound ? "OK" : "MISSING");

    return true;
}

bool SensorManager::initBme680() {
    if (_bme.begin(BME680_I2C_ADDR_0)) {
        Serial.println("[SENSOR] BME680 found at 0x76");
    } else if (_bme.begin(BME680_I2C_ADDR_1)) {
        Serial.println("[SENSOR] BME680 found at 0x77");
    } else {
        return false;
    }

    _bme.setTemperatureOversampling(BME680_OS_8X);
    _bme.setHumidityOversampling(BME680_OS_2X);
    _bme.setPressureOversampling(BME680_OS_4X);
    _bme.setIIRFilterSize(BME680_FILTER_SIZE_3);
    _bme.setGasHeater(320, 150);

    return true;
}

bool SensorManager::initEns160() {
    _ens52.begin();
    if (_ens52.available()) {
        _ens = &_ens52;
        Serial.println("[SENSOR] ENS160 found at 0x52");
    } else {
        _ens53.begin();
        if (!_ens53.available()) {
            return false;
        }
        _ens = &_ens53;
        Serial.println("[SENSOR] ENS160 found at 0x53");
    }

    if (!_ens->setMode(ENS160_OPMODE_STD)) {
        Serial.println("[SENSOR] ENS160 failed to enter standard mode");
        _ens = nullptr;
        return false;
    }

    Serial.println("[SENSOR] ENS160 standard mode enabled");

    return true;
}

bool SensorManager::read(SensorReadings& readings) {
    readings.bmeValid = false;
    readings.ensValid = false;

    if (_bmeFound && _bme.performReading()) {
        readings.temperature = roundToTwoDecimals(_bme.temperature);
        readings.humidity = roundToTwoDecimals(_bme.humidity);
        readings.pressure = roundToTwoDecimals(_bme.pressure / 100.0f);
        readings.gasResistance = _bme.gas_resistance;
        readings.bmeValid = true;
    }

    if (_ensFound) {
        if (readings.bmeValid) {
            _ens->set_envdata(readings.temperature, readings.humidity);
        }

        _ens->measure(true);

        readings.co2 = _ens->geteCO2();
        readings.tvoc = _ens->getTVOC();
        readings.aqi = _ens->getAQI();
        readings.ensValid = true;
    }

    readings.valid = readings.bmeValid || readings.ensValid;
    if (!readings.valid) {
        Serial.println("[SENSOR] All sensors failed, skipping publish");
        return false;
    }

    Serial.printf("[SENSOR] T=%.2f H=%.2f P=%.2f CO2=%u TVOC=%u AQI=%u\n",
        readings.temperature, readings.humidity, readings.pressure,
        readings.co2, readings.tvoc, readings.aqi);

    return true;
}

void SensorManager::toJson(JsonDocument& doc, const char* hardwareId,
    unsigned long epochTime, const SensorReadings& readings) const
{
    doc["hardwareId"] = hardwareId;

    char buf[24];
    formatTimestamp(buf, sizeof(buf), epochTime);
    doc["timestamp"] = buf;

    JsonArray arr = doc["readings"].to<JsonArray>();

    auto add = [&](const char* key, float value, const char* unit) {
        JsonObject obj = arr.add<JsonObject>();
        obj["key"] = key;
        obj["value"] = value;
        if (unit) obj["unit"] = unit;
    };

    if (readings.bmeValid) {
        add("temperature", readings.temperature, "degreeCelsius");
        add("humidity", readings.humidity, "%");
        add("pressure", readings.pressure, "hPa");
        add("gas_resistance", (float)readings.gasResistance, "ohm");
    }

    if (readings.ensValid) {
        add("co2", (float)readings.co2, "ppm");
        add("tvoc", (float)readings.tvoc, "ppb");
        add("aqi", (float)readings.aqi, "score");
    }
}

void SensorManager::formatTimestamp(char* buffer, size_t size, unsigned long epochTime) const {
    time_t raw = epochTime;
    struct tm* ptm = gmtime(&raw);
    snprintf(buffer, size, "%04d-%02d-%02dT%02d:%02d:%02dZ",
        ptm->tm_year + 1900,
        ptm->tm_mon + 1,
        ptm->tm_mday,
        ptm->tm_hour,
        ptm->tm_min,
        ptm->tm_sec);
}
