#include "BizLog.h"

#include <emulibc.h>

#include <stdarg.h>

namespace Platform
{

ECL_INVISIBLE static LogCallback_t LogCallback;

void SetLogCallback(LogCallback_t logCallback)
{
	LogCallback = logCallback;
}

void Log(LogLevel level, const char* fmt, ...)
{
	va_list args;

	va_start(args, fmt);
	size_t bufferSize = vsnprintf(nullptr, 0, fmt, args);
	va_end(args);

	if ((int)bufferSize < 0)
	{
		return;
	}

	auto buffer = std::make_unique<char[]>(bufferSize + 1);

	va_start(args, fmt);
	vsnprintf(buffer.get(), bufferSize + 1, fmt, args);
	va_end(args);

	LogCallback(level, buffer.get());
}

}
