#pragma once
#include <Preferences.h>

class NvsManager {
public:
    void begin();
    uint32_t loadInterval();
    void saveInterval(uint32_t seconds);

private:
    Preferences _prefs;
};
