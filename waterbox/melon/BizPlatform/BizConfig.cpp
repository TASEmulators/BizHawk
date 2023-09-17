#include "BizConfig.h"

#include <emulibc.h>

namespace Platform
{

struct ConfigCallbackInterface
{
	bool (*GetBoolean)(ConfigEntry entry);
	int (*GetInteger)(ConfigEntry entry);
	void (*GetString)(ConfigEntry entry, char* buffer, int bufferSize);
	bool (*GetArray)(ConfigEntry entry, void* buffer);
};

ECL_INVISIBLE static ConfigCallbackInterface ConfigCallbacks;

void SetConfigCallbacks(ConfigCallbackInterface& configCallbackInterface)
{
	ConfigCallbacks = configCallbackInterface;
}

bool GetConfigBool(ConfigEntry entry)
{
	return ConfigCallbacks.GetBoolean(entry);
}

int GetConfigInt(ConfigEntry entry)
{
	return ConfigCallbacks.GetInteger(entry);
}

std::string GetConfigString(ConfigEntry entry)
{
	char buffer[4096]{};
	ConfigCallbacks.GetString(entry, buffer, sizeof(buffer));
	return buffer;
}

bool GetConfigArray(ConfigEntry entry, void* data)
{
	return ConfigCallbacks.GetArray(entry, data);
}

}
