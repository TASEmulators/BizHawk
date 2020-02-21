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

#ifndef f_VD2_SYSTEM_THUNK_H
#define f_VD2_SYSTEM_THUNK_H

#ifdef _MSC_VER
	#pragma once
#endif

#include <vd2/system/vdtypes.h>

#if defined(VD_CPU_X86) || defined(VD_CPU_X64)
	#define VD_USE_DYNAMIC_THUNKS 1
#else
	#define VD_USE_DYNAMIC_THUNKS 0
#endif

bool VDInitThunkAllocator();
void VDShutdownThunkAllocator();

class VDFunctionThunkInfo;

void VDDestroyFunctionThunk(VDFunctionThunkInfo *thunk);

///////////////////////////////////////////////////////////////////////////////

#if VD_USE_DYNAMIC_THUNKS
template<typename T> struct VDMetaSizeofArg { enum { value = (sizeof(T) + sizeof(void *) - 1) & ~(sizeof(void *) - 1) }; };

// This doesn't work for references. Sadly, these seem to get stripped during template matching.
template<class T, class R>
char (&VDMetaGetMethodArgBytes(R (T::*method)()))[1];

template<class T, class R, class A1>
char (&VDMetaGetMethodArgBytes(R (T::*method)(A1)))[1 + VDMetaSizeofArg<A1>::value];

template<class T, class R, class A1, class A2>
char (&VDMetaGetMethodArgBytes(R (T::*method)(A1, A2)))[1 + VDMetaSizeofArg<A1>::value + VDMetaSizeofArg<A2>::value];

template<class T, class R, class A1, class A2, class A3>
char (&VDMetaGetMethodArgBytes(R (T::*method)(A1, A2, A3)))[1 + VDMetaSizeofArg<A1>::value + VDMetaSizeofArg<A2>::value + VDMetaSizeofArg<A3>::value];

template<class T, class R, class A1, class A2, class A3, class A4>
char (&VDMetaGetMethodArgBytes(R (T::*method)(A1, A2, A3, A4)))[1 + VDMetaSizeofArg<A1>::value + VDMetaSizeofArg<A2>::value + VDMetaSizeofArg<A3>::value + VDMetaSizeofArg<A4>::value];

template<class T, class R, class A1, class A2, class A3, class A4, class A5>
char (&VDMetaGetMethodArgBytes(R (T::*method)(A1, A2, A3, A4)))[1 + VDMetaSizeofArg<A1>::value + VDMetaSizeofArg<A2>::value + VDMetaSizeofArg<A3>::value + VDMetaSizeofArg<A4>::value + VDMetaSizeofArg<A5>::value];

VDFunctionThunkInfo *VDCreateFunctionThunkFromMethod(void *method, void *pThis, size_t argbytes, bool stdcall_thunk);

template<class T, class T_Method>
VDFunctionThunkInfo *VDCreateFunctionThunkFromMethod(T *pThis, T_Method method, bool stdcall_thunk) {
	return VDCreateFunctionThunkFromMethod(*(void **)&method, pThis, sizeof VDMetaGetMethodArgBytes(method) - 1, stdcall_thunk);
}

template<typename T>
T VDGetThunkFunction(VDFunctionThunkInfo *p) {
	return (T)p;
}

#else	// !VD_USE_DYNAMIC_THUNKS

// In the non-dynamic mode, we restrict the function type to only a select few that are actually
// used since we need to preallocate thunks. The function types used, respectively, are
// TIMERPROC, WNDPROC, and HOOKPROC.
#if VD_PTR_SIZE > 4
typedef void    (*VDThunkTypeT)(void *pThis, const void *pData, void *a, unsigned b, unsigned __int64 c, unsigned long d);
typedef __int64 (*VDThunkTypeW)(void *pThis, const void *pData, void *a, unsigned b, unsigned __int64 c, __int64 d);
typedef __int64 (*VDThunkTypeH)(void *pThis, const void *pData, int a, unsigned __int64 b, __int64 c);
#else
typedef void (*VDThunkTypeT)(void *pThis, const void *pData, void *a, unsigned b, unsigned c, unsigned long d);
typedef long (*VDThunkTypeW)(void *pThis, const void *pData, void *a, unsigned b, unsigned c, long d);
typedef long (*VDThunkTypeH)(void *pThis, const void *pData, int a, unsigned b, long c);
#endif

VDFunctionThunkInfo *VDCreateFunctionThunkFromMethod(void *pThis, void *pData, size_t nData, VDThunkTypeT pfn);
VDFunctionThunkInfo *VDCreateFunctionThunkFromMethod(void *pThis, void *pData, size_t nData, VDThunkTypeW pfn);
VDFunctionThunkInfo *VDCreateFunctionThunkFromMethod(void *pThis, void *pData, size_t nData, VDThunkTypeH pfn);

template<class T, typename T_Handle, typename T_1, typename T_2, typename T_3>
VDFunctionThunkInfo *VDCreateFunctionThunkFromMethod(T *pThis, void (T::*method)(T_Handle *, T_1, T_2, T_3), bool stdcall_thunk) {
	return VDCreateFunctionThunkFromMethod(pThis,
		&method,
		sizeof method,
		[](void *pThis, const void *pData, void *a, T_1 b, T_2 c, T_3 d)
		{
			(((T *)pThis)->**(decltype(method)*)pData)((T_Handle *)a, b, c, d);
		});
}

template<class T, typename T_R, typename T_Handle, typename T_1, typename T_2, typename T_3>
VDFunctionThunkInfo *VDCreateFunctionThunkFromMethod(T *pThis, T_R (T::*method)(T_Handle *, T_1, T_2, T_3), bool stdcall_thunk) {
	return VDCreateFunctionThunkFromMethod(pThis,
		&method,
		sizeof method,
		[](void *pThis, const void *pData, void *a, T_1 b, T_2 c, T_3 d)
		{
			return (((T *)pThis)->**(decltype(method)*)pData)((T_Handle *)a, b, c, d);
		});
}

template<class T, typename T_R, typename T_1, typename T_2, typename T_3>
VDFunctionThunkInfo *VDCreateFunctionThunkFromMethod(T *pThis, T_R (T::*method)(T_1, T_2, T_3), bool stdcall_thunk) {
	return VDCreateFunctionThunkFromMethod(pThis,
		&method,
		sizeof method,
		[](void *pThis, const void *pData, T_1 a, T_2 b, T_3 c)
		{
			return (((T *)pThis)->**(decltype(method)*)pData)(a, b, c);
		});
}

template<typename T>
T VDGetThunkFunction(VDFunctionThunkInfo *p) {
	return (T)*(void **)p;
}
#endif

#endif
