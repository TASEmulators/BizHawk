#ifndef BIZCONFIG_H
#define BIZCONFIG_H

#include "Platform.h"

namespace Platform
{

struct ConfigCallbackInterface;
void SetConfigCallbacks(ConfigCallbackInterface& configCallbackInterface);

}

#endif
