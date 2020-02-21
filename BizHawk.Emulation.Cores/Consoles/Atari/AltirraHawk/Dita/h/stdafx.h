#ifndef f_DITA_STDAFX_H
#define f_DITA_STDAFX_H

#if defined(_MSC_VER) && _MSC_VER < 1300
#pragma warning(disable: 4786)
static const struct VD_MSVC_C4786Workaround { VD_MSVC_C4786Workaround() {} } g_VD_MSVC_C4786Workaround;
#endif

#ifndef _WIN32_WINNT
#define _WIN32_WINNT 0x0600
#elif _WIN32_WINNT < 0x0600
#error _WIN32_WINNT is less than 5.0. This will break the places bar on the load/save dialog.
#endif

struct IUnknown;

#include <vd2/system/vdtypes.h>

#include <windows.h>

#include <vd2/system/VDString.h>
#include <vd2/Dita/services.h>

#endif
