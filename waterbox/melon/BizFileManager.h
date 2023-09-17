#ifndef BIZFILEMANAGER_H
#define BIZFILEMANAGER_H

#include "types.h"

namespace FileManager
{

const char* InitNAND(bool clearNand, bool dsiWare);
const char* InitCarts(bool gba);

}

#endif
