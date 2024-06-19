#include "Platform.h"

#include <emulibc.h>

#include <stdarg.h>

namespace melonDS::Platform
{

using LogCallback_t = void (*)(LogLevel level, const char* message);

ECL_INVISIBLE static LogCallback_t LogCallback;
ECL_INVISIBLE static char LogBuffer[1 << 16];

ECL_EXPORT void SetLogCallback(LogCallback_t logCallback)
{
	LogCallback = logCallback;
}

void Log(LogLevel level, const char* fmt, ...)
{
	va_list args;
	va_start(args, fmt);
	int bufferSize = vsnprintf(LogBuffer, sizeof(LogBuffer), fmt, args);
	va_end(args);

	if (bufferSize < 0)
	{
		return;
	}

	LogCallback(level, LogBuffer);
}

}
