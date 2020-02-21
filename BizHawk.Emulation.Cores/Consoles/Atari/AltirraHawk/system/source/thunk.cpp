//	VirtualDub - Video processing and capture application
//	System library component
//	Copyright (C) 1998-2018 Avery Lee, All Rights Reserved.
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

#include <stdafx.h>
#include <windows.h>
#include <map>
#include <vd2/system/atomic.h>
#include <vd2/system/bitmath.h>
#include <vd2/system/refcount.h>
#include <vd2/system/thunk.h>
#include <vd2/system/binary.h>
#include <vd2/system/vdstl.h>

#if VD_USE_DYNAMIC_THUNKS

#if !defined(VD_CPU_X86) && !defined(VD_CPU_X64)
#error Dynamic thunks are only supported on X86 and X64.
#endif

class IVDJITAllocator {};

class VDJITAllocator : public vdrefcounted<IVDJITAllocator> {
public:
	VDJITAllocator();
	~VDJITAllocator();

	void *Allocate(size_t len);
	void Free(void *p, size_t len);

	void EndUpdate(void *p, size_t len);

protected:
	typedef std::map<void *, size_t> FreeChunks;
	FreeChunks mFreeChunks;
	FreeChunks::iterator mNextChunk;

	typedef std::map<void *, size_t> Allocations;
	Allocations mAllocations;

	uintptr		mAllocationGranularity;
};

VDJITAllocator::VDJITAllocator()
	: mNextChunk(mFreeChunks.end())
{
	SYSTEM_INFO si;
	GetSystemInfo(&si);

	mAllocationGranularity = si.dwAllocationGranularity;
}

VDJITAllocator::~VDJITAllocator() {
	for(Allocations::iterator it(mAllocations.begin()), itEnd(mAllocations.end()); it!=itEnd; ++it) {
		VirtualFree(it->first, 0, MEM_RELEASE);
	}
}

void *VDJITAllocator::Allocate(size_t len) {
	len = (len + 15) & ~(size_t)15;

	FreeChunks::iterator itMark(mNextChunk), itEnd(mFreeChunks.end()), it(itMark);

	if (it == itEnd)
		it = mFreeChunks.begin();

	for(;;) {
		for(; it!=itEnd; ++it) {
			if (it->second >= len) {
				it->second -= len;

				void *p = (char *)it->first + it->second;

				if (!it->second) {
					if (mNextChunk == it)
						++mNextChunk;

					mFreeChunks.erase(it);
				}

				return p;
			}
		}

		if (itEnd == itMark)
			break;

		it = mFreeChunks.begin();
		itEnd = itMark;
	}

	size_t alloclen = (len + mAllocationGranularity - 1) & ~(mAllocationGranularity - 1);

	void *p = VirtualAlloc(NULL, alloclen, MEM_COMMIT, PAGE_EXECUTE_READWRITE);
	if (p) {
		try {
			Allocations::iterator itA(mAllocations.insert(Allocations::value_type(p, alloclen)).first);

			try {
				if (len < alloclen)
					mFreeChunks.insert(FreeChunks::value_type((char *)p + len, alloclen - len));

			} catch(...) {
				mAllocations.erase(itA);
				throw;
			}
		} catch(...) {
			VirtualFree(p, 0, MEM_RELEASE);
			p = NULL;
		}
	}

	return p;
}

void VDJITAllocator::Free(void *p, size_t len) {
	VDASSERT(p);
	VDASSERT(len < 0x10000);

	FreeChunks::iterator cur(mFreeChunks.lower_bound(p));
	if (cur != mFreeChunks.end() && (char *)p + len == cur->first) {
		len += cur->second;
		if (mNextChunk == cur)
			++mNextChunk;

		mFreeChunks.erase(cur++);
	}

	if (cur != mFreeChunks.begin()) {
		FreeChunks::iterator prev(cur);

		--prev;
		if ((char *)prev->first + prev->second == p) {
			p = prev->first;
			len += prev->second;
			if (mNextChunk == prev)
				++mNextChunk;
			mFreeChunks.erase(prev);
		}
	}

	uintptr start = (size_t)p;
	uintptr end = start + len;

	if (!((start | end) & (mAllocationGranularity - 1))) {
		Allocations::iterator it(mAllocations.find(p));

		if (it != mAllocations.end()) {
			VirtualFree((void *)start, 0, MEM_RELEASE);
			mAllocations.erase(it);
			return;
		}
	}

	mFreeChunks.insert(FreeChunks::value_type((void *)start, end-start));
}

void VDJITAllocator::EndUpdate(void *p, size_t len) {
	FlushInstructionCache(GetCurrentProcess(), p, len);
}

///////////////////////////////////////////////////////////////////////////

VDJITAllocator *g_pVDJITAllocator;
VDAtomicInt g_VDJITAllocatorLock;

bool VDInitThunkAllocator() {
	bool success = true;

	while(g_VDJITAllocatorLock.xchg(1))
		::Sleep(1);

	if (!g_pVDJITAllocator) {
		g_pVDJITAllocator = new_nothrow VDJITAllocator;
		if (!g_pVDJITAllocator)
			success = false;
	}

	if (success)
		g_pVDJITAllocator->AddRef();

	VDVERIFY(1 == g_VDJITAllocatorLock.xchg(0));

	return success;
}

void VDShutdownThunkAllocator() {
	while(g_VDJITAllocatorLock.xchg(1))
		::Sleep(1);

	VDASSERT(g_pVDJITAllocator);

	if (!g_pVDJITAllocator->Release())
		g_pVDJITAllocator = NULL;

	VDVERIFY(1 == g_VDJITAllocatorLock.xchg(0));
}

void *VDAllocateThunkMemory(size_t len) {
	return g_pVDJITAllocator->Allocate(len);
}

void VDFreeThunkMemory(void *p, size_t len) {
	g_pVDJITAllocator->Free(p, len);
}

void VDSetThunkMemory(void *p, const void *src, size_t len) {
	memcpy(p, src, len);
	g_pVDJITAllocator->EndUpdate(p, len);
}

void VDFlushThunkMemory(void *p, size_t len) {
	g_pVDJITAllocator->EndUpdate(p, len);
}

///////////////////////////////////////////////////////////////////////////

#ifdef _M_AMD64
	extern "C" void VDCDECL VDMethodToFunctionThunk64();
#else
	extern "C" void VDCDECL VDMethodToFunctionThunk32();
	extern "C" void VDCDECL VDMethodToFunctionThunk32_4();
	extern "C" void VDCDECL VDMethodToFunctionThunk32_8();
	extern "C" void VDCDECL VDMethodToFunctionThunk32_12();
	extern "C" void VDCDECL VDMethodToFunctionThunk32_16();
#endif

VDFunctionThunkInfo *VDCreateFunctionThunkFromMethod(void *method, void *pThis, size_t argbytes, bool stdcall_thunk) {
#if defined(_M_IX86)
	void *pThunk = VDAllocateThunkMemory(16);

	if (!pThunk)
		return NULL;

	if (stdcall_thunk || !argbytes) {	// thiscall -> stdcall (easy case)
		uint8 thunkbytes[16]={
			0xB9, 0x00, 0x00, 0x00, 0x00,				// mov ecx, this
			0xE9, 0x00, 0x00, 0x00, 0x00				// jmp fn
		};


		VDWriteUnalignedLEU32(thunkbytes+1, (uint32)(uintptr)pThis);
		VDWriteUnalignedLEU32(thunkbytes+6, (uint32)method - ((uint32)pThunk + 10));

		VDSetThunkMemory(pThunk, thunkbytes, 15);
	} else {				// thiscall -> cdecl (hard case)
		uint8 thunkbytes[16]={
			0xE8, 0x00, 0x00, 0x00, 0x00,				// call VDFunctionThunk32
			0xC3,										// ret
			(uint8)argbytes,							// db argbytes
			0,											// db 0
			0x00, 0x00, 0x00, 0x00,						// dd method
			0x00, 0x00, 0x00, 0x00,						// dd this
		};

		void (VDCDECL *adapter)();

		switch(argbytes) {
		case 4:		adapter = VDMethodToFunctionThunk32_4;	break;
		case 8:		adapter = VDMethodToFunctionThunk32_8;	break;
		case 12:	adapter = VDMethodToFunctionThunk32_12;	break;
		case 16:	adapter = VDMethodToFunctionThunk32_16;	break;
		default:	adapter = VDMethodToFunctionThunk32;	break;
		}

		VDWriteUnalignedLEU32(thunkbytes+1, (uint32)(uintptr)adapter - ((uint32)pThunk + 5));
		VDWriteUnalignedLEU32(thunkbytes+8, (uint32)(uintptr)method);
		VDWriteUnalignedLEU32(thunkbytes+12, (uint32)(uintptr)pThis);

		VDSetThunkMemory(pThunk, thunkbytes, 16);
	}

	return (VDFunctionThunkInfo *)pThunk;
#elif defined(_M_AMD64)
	void *pThunk = VDAllocateThunkMemory(44);
	if (!pThunk)
		return NULL;

	uint8 thunkbytes[44]={
		0x48, 0x8D, 0x05, 0x09, 0x00, 0x00, 0x00,	// lea rax, [rip+9]
		0xFF, 0x25, 0x03, 0x00, 0x00, 0x00,			// jmp qword ptr [rip+3]
		0x90,										// nop
		0x90,										// nop
		0x90,										// nop
		0, 0, 0, 0, 0, 0, 0, 0,						// dq VDFunctionThunk64
		0, 0, 0, 0, 0, 0, 0, 0,						// dq method
		0, 0, 0, 0, 0, 0, 0, 0,						// dq this
		0, 0, 0, 0									// dd argspillbytes
	};

	VDWriteUnalignedLEU64(thunkbytes+16, (uint64)(uintptr)VDMethodToFunctionThunk64);
	VDWriteUnalignedLEU64(thunkbytes+24, (uint64)(uintptr)method);
	VDWriteUnalignedLEU64(thunkbytes+32, (uint64)(uintptr)pThis);

	// The stack must be aligned to a 16 byte boundary when the CALL
	// instruction occurs. On entry to VDFunctionThunk64(), the stack is misaligned
	// to 16n+8. Therefore, the number of argbytes must be 16m+8 and the number of
	// argspillbytes must be 16m+8-24.
	VDWriteUnalignedLEU32(thunkbytes+40, argbytes < 32 ? 0 : ((argbytes - 16 + 15) & ~15));

	VDSetThunkMemory(pThunk, thunkbytes, 44);

	return (VDFunctionThunkInfo *)pThunk;
#else
	return NULL;
#endif
}

void VDDestroyFunctionThunk(VDFunctionThunkInfo *pFnThunk) {
	// validate thunk
#if defined(_M_IX86)
	VDASSERT(((const uint8 *)pFnThunk)[0] == 0xB9 || ((const uint8 *)pFnThunk)[0] == 0xE8);
	VDFreeThunkMemory(pFnThunk, 16);
#elif defined(_M_AMD64)
	VDFreeThunkMemory(pFnThunk, 44);
#else
	static_assert(false, "Platform not supported");
#endif
}

#else	// VD_USE_DYNAMIC_THUNKS

bool VDInitThunkAllocator() {
	return true;
}

void VDShutdownThunkAllocator() {
}

template<unsigned IdBase, unsigned N, typename T_Fn>
struct VDThunkTable {
	typedef void *Thunk;

	static void *spThis[N];
	static void *sData[N][4];
	static T_Fn spFns[N];
	static uint32 sBitField[N / 32];

	VDCriticalSection mMutex;

	static_assert(N % 32 == 0);

	template<unsigned Index, typename T_Ret, typename... T_Args>
	static constexpr T_Ret (__stdcall *GetThunk(T_Ret (*)(void *, const void *, T_Args...)))(T_Args...) {
		return [](T_Args... args) {
			return spFns[Index](spThis[Index], sData[Index], args...);
		};
	}

	template<unsigned... T_Indices>
	static const Thunk *GetThunks(std::integer_sequence<unsigned, T_Indices...>) {
		// This generates a unique table of thunk functions, each specialized to use a
		// specific index. Thus, we are constrained in the non-dynamic mode to have a fixed
		// size pool of thunks.
		static constexpr Thunk kThunks[]={
			GetThunk<T_Indices>((T_Fn)nullptr)...
		};

		return kThunks;
	}

	template<typename T_Indices = std::make_integer_sequence<unsigned, N>>
	static const Thunk& GetThunk(unsigned index) {
		const Thunk *kThunks = GetThunks(T_Indices{});
		return kThunks[index];
	}

	VDFunctionThunkInfo *AllocThunk(void *pThis, void *pData, size_t nData, T_Fn fn) {
		VDCriticalSection::AutoLock lock(mMutex);

		uint32 index = UINT32_MAX;

		for (uint32 i = 0; i < vdcountof(sBitField); ++i) {
			uint32 freeBits = ~sBitField[i];

			if (freeBits) {
				uint32 bitPos = VDFindLowestSetBitFast(freeBits);

				sBitField[i] |= (1U << bitPos);

				index = (i << 5) + bitPos;
				break;
			}
		}

		if (index == UINT32_MAX)
			VDBREAK;

		spThis[index] = pThis;
		memcpy(sData[index], pData, nData);
		spFns[index] = fn;

		return (VDFunctionThunkInfo *)&GetThunk(index);
	}

	bool FreeThunk(VDFunctionThunkInfo *thunk) {
		if (!thunk)
			return true;

		const uintptr offset = ((uintptr)thunk - (uintptr)&GetThunk(0));
		if (offset >= sizeof(Thunk[N]))
			return false;

		VDCriticalSection::AutoLock lock(mMutex);

		const uint32 index = (uint32)offset / (uint32)sizeof(Thunk);
		VDASSERT(sBitField[index >> 5] & (1U << (index & 31))); 

		sBitField[index >> 5] &= ~(1U << (index & 31));
		return true;
	}

	static VDThunkTable& GetInstance() {
		static VDThunkTable s;

		return s;
	}
};

template<unsigned IdBase, unsigned N, typename T_Fn>
void *VDThunkTable<IdBase, N, T_Fn>::spThis[N];

template<unsigned IdBase, unsigned N, typename T_Fn>
void *VDThunkTable<IdBase, N, T_Fn>::sData[N][4];

template<unsigned IdBase, unsigned N, typename T_Fn>
typename T_Fn VDThunkTable<IdBase, N, T_Fn>::spFns[N];

template<unsigned IdBase, unsigned N, typename T_Fn>
uint32 typename VDThunkTable<IdBase, N, T_Fn>::sBitField[(N+31)/32];

typedef VDThunkTable<0, 64, VDThunkTypeT> VDThunkT;
typedef VDThunkTable<1, 512, VDThunkTypeW> VDThunkW;
typedef VDThunkTable<2, 64, VDThunkTypeH> VDThunkH;

VDFunctionThunkInfo *VDCreateFunctionThunkFromMethod(void *pThis, void *pData, size_t nData, VDThunkTypeT pfn) {
	return VDThunkT::GetInstance().AllocThunk(pThis, pData, nData, pfn);
}

VDFunctionThunkInfo * VDCreateFunctionThunkFromMethod(void *pThis, void *pData, size_t nData, VDThunkTypeW pfn) {
	return VDThunkW::GetInstance().AllocThunk(pThis, pData, nData, pfn);
}

VDFunctionThunkInfo * VDCreateFunctionThunkFromMethod(void *pThis, void *pData, size_t nData, VDThunkTypeH pfn) {
	return VDThunkH::GetInstance().AllocThunk(pThis, pData, nData, pfn);
}

void VDDestroyFunctionThunk(VDFunctionThunkInfo *thunk) {
	VDVERIFY(VDThunkT::GetInstance().FreeThunk(thunk)
		|| VDThunkW::GetInstance().FreeThunk(thunk)
		|| VDThunkH::GetInstance().FreeThunk(thunk));
}

#endif
