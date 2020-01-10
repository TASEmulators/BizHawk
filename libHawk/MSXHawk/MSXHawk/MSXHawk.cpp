// MSXHawk.cpp : Defines the exported functions for the DLL.
//

#include "MSXHawk.h"
#include "Core.h"

using namespace MSXHawk;

// Create pointer to a core instance
MSXHAWK_EXPORT MSXCore* MSX_create() 
{
	return new MSXCore();
}

// free the memory from the core pointer
MSXHAWK_EXPORT void MSX_destroy(MSXCore* p)
{
	std::free(p);
}

