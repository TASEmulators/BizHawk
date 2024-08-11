#ifndef BIZFILE_H
#define BIZFILE_H

#include "Platform.h"

namespace melonDS::Platform
{

FileHandle* CreateMemoryFile(u8* fileData, u32 fileLength);

}

#endif
