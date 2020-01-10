// MSXHawk.cpp : Defines the exported functions for the DLL.
//

#include "MSXHawk.h"
#include "Core.h"

using namespace MSXHawk;

// This is an example of an exported variable
MSXHAWK_EXPORT int nMSXHawk=0;

// This is an example of an exported function.
MSXHAWK_EXPORT MSXCore* MSX_create() {
	return new MSXCore();
}