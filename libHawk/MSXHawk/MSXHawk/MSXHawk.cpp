// MSXHawk.cpp : Defines the exported functions for the DLL.
//

#include "pch.h"
#include "framework.h"
#include "MSXHawk.h"


// This is an example of an exported variable
MSXHAWK_API int nMSXHawk=0;

// This is an example of an exported function.
MSXHAWK_API int fnMSXHawk(void)
{
    return 0;
}

// This is the constructor of a class that has been exported.
CMSXHawk::CMSXHawk()
{
    return;
}
