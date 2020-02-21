//	VirtualDub - Video processing and capture application
//	System library component
//	Copyright (C) 1998-2015 Avery Lee, All Rights Reserved.
//
//	Beginning with 1.6.0, the VirtualDub system library is licensed
//	differently than the remainder of VirtualDub.  This particular file is
//	thus licensed as follows (the "zlib" license):
//
//	This software is provided 'as-is', without any express or implied
//	warranty.  In no event will the authors be held liable for any
//	damages arising from the use of this software.
//
//	Permission is granted to anyone to use this software for any purpose,
//	including commercial applications, and to alter it and redistribute it
//	freely, subject to the following restrictions:
//
//	1.	The origin of this software must not be misrepresented; you must
//		not claim that you wrote the original software. If you use this
//		software in a product, an acknowledgment in the product
//		documentation would be appreciated but is not required.
//	2.	Altered source versions must be plainly marked as such, and must
//		not be misrepresented as being the original software.
//	3.	This notice may not be removed or altered from any source
//		distribution.

//---------------------------------------------------------------------
// VS2013 x64 startup crash fix
//
// Versions of the Visual Studio 2013 up through Update 4 have a bug
// in their x64 CRT that causes a crash on startup on systems that
// have FMA3-capable CPUs but XSAVE disabled. This is due to a bad
// CPU feature check in the CRT which checks the FMA3 feature bit but
// not the OSXSAVE or AVX enable bits. One symptom of this is a crash
// in pow() on a VMOVSD instruction. Windows XP 64-bit, Vista x64,
// Windows 7 pre-SP1, and Windows 7 post-SP1 with XSAVE disabled are
// affected. One symptom of this problem is a crash on a VMOVSD
// instruction when pow() is called.
//
// Irritatingly, Microsoft has decided that they will not fix this for
// VS2013:
//
// https://connect.microsoft.com/VisualStudio/feedback/details/811093
//
// Therefore, we have to do the CPU detection they should be doing.
// The CRT routine that does the check runs at init_seg(compiler)
// priority, and we can have initializers at init_seg(user) that call
// transcedental functions, so we need the hack fix in between.
//
// Note that this only works for code in the EXE. If another DLL brings
// in a version of MSVCR120/MSVCR120D with the bug -- especially if it
// happens during hard DLL link init --  we'll crash the same way.
// Nothing we can do about that.
//

#include <vd2/system/w32assist.h>
#include <math.h>
#include <intrin.h>

extern "C" int __use_fma3_lib;

#pragma warning(disable: 4073)	// warning C4073: initializers put in library initialization area
#pragma init_seg(lib)

class VDFixVS2013FMACrash {
public:
	VDFixVS2013FMACrash() {
#if _MSC_VER < 1900
		if (__use_fma3_lib) {
			// Check if OSXSAVE is set (bit 27) and AVX (bit 28).
			int cpuInfo[4] = {0};
			__cpuid(cpuInfo, 1);

			const int OSXSAVE = (1 << 27);
			const int AVX = (1 << 28);

			if ((cpuInfo[2] & (OSXSAVE | AVX)) == (OSXSAVE | AVX)) {
				// Execute XGETBV and check that XMM and YMM state are enabled.
				if ((_xgetbv(0) & 6) == 6) {
					// all good -- leave it set
					return;
				}
			}

			// We're missing some necessary OS support. Force off use of FMA3
			// instructions.
			_set_FMA3_enable(0);
		}
#endif
	}
} g_VDFixVS2013FMACrash;
