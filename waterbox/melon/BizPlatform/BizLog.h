#ifndef BIZLOG_H
#define BIZLOG_H

#include "Platform.h"

namespace Platform
{

using LogCallback_t = void (*)(LogLevel level, const char* message);
void SetLogCallback(LogCallback_t logCallback);

}

#endif
