//	VirtualDub - Video processing and capture application
//	System library component
//	Copyright (C) 1998-2007 Avery Lee, All Rights Reserved.
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

#ifndef f_VD2_SYSTEM_VDTYPES_H
#define f_VD2_SYSTEM_VDTYPES_H

#ifdef _MSC_VER
	#pragma once
#endif

#include <algorithm>
#include <stdio.h>
#include <stdarg.h>
#include <new>

///////////////////////////////////////////////////////////////////////////
//
//	compiler detection
//
///////////////////////////////////////////////////////////////////////////

#ifndef VD_COMPILER_DETECTED
	#define VD_COMPILER_DETECTED

	#if defined(_MSC_VER)
		#define VD_COMPILER_MSVC	_MSC_VER

		#if _MSC_VER < 1911
			#error Visual Studio 2017 15.3 or newer is required.
		#endif

		#define VD_COMPILER_MSVC_VC8_OR_LATER 1
	#elif defined(__GNUC__)
		#define VD_COMPILER_GCC
		#if defined(__MINGW32__) || defined(__MINGW64__)
			#define VD_COMPILER_GCC_MINGW
		#endif
	#endif
#endif

#ifndef VD_CPU_DETECTED
	#define VD_CPU_DETECTED

	#if defined(_M_AMD64)
		#define VD_CPU_AMD64	1
		#define VD_CPU_X64		1
	#elif defined(_M_IX86) || defined(__i386__)
		#define VD_CPU_X86		1
	#elif defined(_M_ARM64)
		#define VD_CPU_ARM64	1
	#endif
#endif

#ifndef VD_PTR_SIZE
	#if defined(VD_CPU_AMD64) || defined(VD_CPU_ARM64)
		#define VD_PTR_SIZE		8
	#else
		#define VD_PTR_SIZE		4
	#endif
#endif

///////////////////////////////////////////////////////////////////////////
//
//	types
//
///////////////////////////////////////////////////////////////////////////

#include <stdint.h>

#ifndef VD_STANDARD_TYPES_DECLARED
	typedef int64_t				sint64;
	typedef uint64_t			uint64;
	typedef int32_t				sint32;
	typedef uint32_t			uint32;
	typedef int16_t				sint16;
	typedef uint16_t			uint16;
	typedef int8_t				sint8;
	typedef uint8_t				uint8;

	typedef int64_t				int64;
	typedef int32_t				int32;
	typedef int16_t				int16;
	typedef int8_t				int8;

	typedef intptr_t			sintptr;
	typedef uintptr_t			uintptr;
#endif

#define VD64(x) INT64_C(x)
	
typedef	struct __VDGUIHandle *VDGUIHandle;

// enforce wchar_t under Visual C++

#if defined(_MSC_VER) && !defined(_WCHAR_T_DEFINED)
	#include <ctype.h>
#endif

///////////////////////////////////////////////////////////////////////////
//
//	allocation
//
///////////////////////////////////////////////////////////////////////////

#define new_nothrow new(std::nothrow)

///////////////////////////////////////////////////////////////////////////
//
//	attribute support
//
///////////////////////////////////////////////////////////////////////////

#if defined(VD_COMPILER_MSVC)
	#define VDINTERFACE			__declspec(novtable)
	#define VDNORETURN			__declspec(noreturn)
	#define VDPUREFUNC
	#define VDRESTRICT			__restrict

	#define VDNOINLINE			__declspec(noinline)
	#define VDFORCEINLINE		__forceinline
	#define VDALIGN(alignment)	__declspec(align(alignment))

	#define VDCDECL				__cdecl
#elif defined(VD_COMPILER_GCC)
	#define VDINTERFACE
	#define VDNORETURN			__attribute__((noreturn))
	#define VDPUREFUNC			__attribute__((pure))
	#define VDRESTRICT			__restrict
	#define VDNOINLINE			__attribute__((noinline))
	#define VDFORCEINLINE		inline __attribute__((always_inline))
	#define VDALIGN(alignment)	__attribute__((aligned(alignment)))
	#define VDCDECL
#else
	#define VDINTERFACE
	#define VDNORETURN
	#define VDPUREFUNC
	#define VDRESTRICT
	#define VDFORCEINLINE
	#define VDALIGN(alignment)
	#define VDCDECL
#endif

///////////////////////////////////////////////////////////////////////////
//
//	debug support
//
///////////////////////////////////////////////////////////////////////////

enum VDAssertResult {
	kVDAssertBreak,
	kVDAssertContinue,
	kVDAssertIgnore
};

extern VDAssertResult VDAssert(const char *exp, const char *file, int line);
extern VDAssertResult VDAssertPtr(const char *exp, const char *file, int line);
extern void VDDebugPrint(const char *format, ...);

#if defined(_MSC_VER)
	#define VDBREAK		__debugbreak()
#elif defined(__GNUC__)
	#define VDBREAK		__asm__ volatile ("int3" : : )
#else
	#define VDBREAK		*(volatile char *)0 = *(volatile char *)0
#endif

#define VDASSERTCT(exp)	static_assert((exp), #exp)

#ifdef _DEBUG

	namespace {
		template<int line>
		struct VDAssertHelper {
			VDAssertHelper(const char *exp, const char *file) {
				switch(VDAssert(exp, file, line)) {
				case kVDAssertBreak:
					VDBREAK;
					break;
				}
			}
		};
	}

	#define VDFAIL(str)			if (VDAssert(#str, __FILE__, __LINE__) == kVDAssertBreak) { VDBREAK; } else ((void)0)
	#define VDASSERT(exp)		if (!(exp) && VDAssert   (#exp, __FILE__, __LINE__) == kVDAssertBreak) { VDBREAK; } else ((void)0)
	#define VDASSERTPTR(exp) 	if (!(exp) && VDAssertPtr(#exp, __FILE__, __LINE__) == kVDAssertBreak) { VDBREAK; } else ((void)0)
	#define VDVERIFY(exp)		if (!(exp) && VDAssert   (#exp, __FILE__, __LINE__) == kVDAssertBreak) { VDBREAK; } else ((void)0)
	#define VDVERIFYPTR(exp) 	if (!(exp) && VDAssertPtr(#exp, __FILE__, __LINE__) == kVDAssertBreak) { VDBREAK; } else ((void)0)

	#define VDINLINEASSERT(exp)			((exp)||(VDAssertHelper<__LINE__>(#exp, __FILE__),false))
	#define VDINLINEASSERTFALSE(exp)	((exp)&&(VDAssertHelper<__LINE__>("!("#exp")", __FILE__),true))

	#define	VDNEVERHERE			do { if (VDAssert( "[never here]", __FILE__, __LINE__ )) VDBREAK; __assume(false); } while(false)

	#define VDDEBUG(...)		VDDebugPrint(__VA_ARGS__)

#else

	#if defined(_MSC_VER)
		#define VDASSERT(exp)		((void)0)
		#define VDASSERTPTR(exp)	((void)0)
	#elif defined(__GNUC__)
		#define VDASSERT(exp)		__builtin_expect(0 != (exp), 1)
		#define VDASSERTPTR(exp)	__builtin_expect(0 != (exp), 1)
	#endif

	#define VDFAIL(str)			(void)(str)
	#define VDVERIFY(exp)		(void)(exp)
	#define VDVERIFYPTR(exp)	(void)(exp)

	#define VDINLINEASSERT(exp)	(exp)
	#define VDINLINEASSERTFALSE(exp)	(exp)

	#if defined(VD_COMPILER_MSVC)
		#define	VDNEVERHERE			__assume(false)
	#else
		#define	VDNEVERHERE			VDASSERT(false)
	#endif

	#define VDDEBUG(...)		((void)0)

#endif

#define VDDEBUG2(...)		VDDebugPrint(__VA_ARGS__)

#define vdnoexcept			noexcept
#define vdnoexcept_(cond)	noexcept(cond)
#define vdnoexcept_false	noexcept(false)

// The VC++ x86 compiler has an awful implementation of noexcept that incurs
// major runtime costs, so we additionally add nothrow to suppress the
// terminate handler (which adds runtime cost in the nonexceptional case on
// x86). We used to use throw() for this, but this is now mapped to noexcept
// in C++17.

#if defined(_MSC_VER)
	#define vdnothrow			__declspec(nothrow)
#else
	#define vdnothrow
#endif

#endif
