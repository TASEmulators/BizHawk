#ifndef BIZFILE_H
#define BIZFILE_H

#include "Platform.h"

namespace Platform
{

struct FileCallbackInterface;
void SetFileCallbacks(FileCallbackInterface& fileCallbackInterface);

}

#endif
