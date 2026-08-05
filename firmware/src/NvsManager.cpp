#include "NvsManager.h"
#include "Config.h"

void NvsManager::begin() {
    _prefs.begin(NVS_NAMESPACE, false);
}

uint32_t NvsManager::loadInterval() {
    return _prefs.getUInt(NVS_KEY_INTERVAL, DEFAULT_INTERVAL_SEC);
}

void NvsManager::saveInterval(uint32_t seconds) {
    if (seconds < 5) seconds = 5;
    if (seconds > 3600) seconds = 3600;
    _prefs.putUInt(NVS_KEY_INTERVAL, seconds);
}
