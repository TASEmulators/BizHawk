//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2012 Avery Lee
//
//	This program is free software; you can redistribute it and/or modify
//	it under the terms of the GNU General Public License as published by
//	the Free Software Foundation; either version 2 of the License, or
//	(at your option) any later version.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.

#include <stdafx.h>

#if defined(_MSC_VER) && defined(_DEBUG)

#include <crtdbg.h>
#include <windows.h>

// dbghelp.h(1540): warning C4091: 'typedef ': ignored on left of '' when no variable is declared (compiling source file source\exceptionfilter.cpp)
#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable: 4091)
#endif
#include <dbghelp.h>
#ifdef _MSC_VER
#pragma warning(pop)
#endif

#include <vd2/system/w32assist.h>
#include <vd2/system/filesys.h>

// must match CRT internal format -- using different name to avoid
// symbol conflicts
namespace {
	struct CrtBlockHeader {
		CrtBlockHeader *pNext, *pPrev;
		const char *pFilename;
		int			line;
#if defined(VD_CPU_AMD64) || _MSC_VER >= 1600		// VC14/UCRT uses this layout regardless
		int			type;
		size_t		size;
#else
		size_t		size;
		int			type;
#endif
		unsigned	reqnum;
		char		redzone_head[4];
		char		data[1];
	};
}

struct ATDbgHelpDynamicLoaderW32 {
public:
	BOOL (APIENTRY *pSymInitialize)(HANDLE hProcess, PTSTR UserSearchPath, BOOL fInvadeProcess);
	BOOL (APIENTRY *pSymCleanup)(HANDLE hProcess);
	BOOL (APIENTRY *pSymSetSearchPath)(HANDLE hProcess, PTSTR SearchPath);
	DWORD64 (APIENTRY *pSymLoadModule64)(HANDLE hProcess, HANDLE hFile, PCSTR ImageFile, PCSTR ModuleName, DWORD64 BaseOfDll, DWORD SizeOfDll);
	BOOL (APIENTRY *pSymGetSymFromAddr64)(HANDLE hProcess, DWORD64 qwAddr, PDWORD64 pdwDisplacement, PIMAGEHLP_SYMBOL64 Symbol);
	BOOL (APIENTRY *pSymGetModuleInfo64)(HANDLE hProcess, DWORD64 dwAddr, PIMAGEHLP_MODULE64 ModuleInfo);
	BOOL (APIENTRY *pUnDecorateSymbolName)(PCTSTR DecoratedName, PTSTR UnDecoratedName, DWORD UndecoratedLength, DWORD Flags);

	HMODULE hmodDbgHelp;

	ATDbgHelpDynamicLoaderW32();
	~ATDbgHelpDynamicLoaderW32();

	bool ready() const { return hmodDbgHelp != 0; }
};

ATDbgHelpDynamicLoaderW32::ATDbgHelpDynamicLoaderW32()
{
	// XP DbgHelp doesn't pick up some VC8 symbols -- need DbgHelp 6.2+ for that
	hmodDbgHelp = LoadLibrary(_T("c:\\program files\\debugging tools for windows\\dbghelp"));
	if (!hmodDbgHelp) {
		hmodDbgHelp = LoadLibrary(_T("c:\\program files (x86)\\debugging tools for windows\\dbghelp"));

		if (!hmodDbgHelp)
			hmodDbgHelp = LoadLibrary(_T("dbghelp"));
	}

	static const char *const sFuncTbl[]={
#ifdef UNICODE
		"SymInitializeW",
		"SymCleanup",
		"SymSetSearchPathW",
		"SymLoadModule64",
		"SymGetSymFromAddr64",
		"SymGetModuleInfo64",
		"UnDecorateSymbolNameW",
#else
		"SymInitialize",
		"SymCleanup",
		"SymSetSearchPath",
		"SymLoadModule64",
		"SymGetSymFromAddr64",
		"SymGetModuleInfo64",
		"UnDecorateSymbolName",
#endif
	};

	enum { kFuncs = sizeof(sFuncTbl)/sizeof(sFuncTbl[0]) };

	if (hmodDbgHelp) {
		int i;
		for(i=0; i<kFuncs; ++i) {
			FARPROC fp = GetProcAddress(hmodDbgHelp, sFuncTbl[i]);

			if (!fp)
				break;

			((FARPROC *)this)[i] = fp;
		}

		if (i >= kFuncs)
			return;

		FreeModule(hmodDbgHelp);
		hmodDbgHelp = 0;
	}

	for(int j=0; j<kFuncs; ++j)
		((FARPROC *)this)[j] = 0;
}

ATDbgHelpDynamicLoaderW32::~ATDbgHelpDynamicLoaderW32() {
	if (hmodDbgHelp) {
		FreeModule(hmodDbgHelp);
		hmodDbgHelp = 0;
	}
}

namespace {
	template<class T>
	class heapvector {
	public:
		typedef	T *					pointer_type;
		typedef	const T *			const_pointer_type;
		typedef T&					reference_type;
		typedef const T&			const_reference_type;
		typedef pointer_type		iterator;
		typedef	const_pointer_type	const_iterator;
		typedef size_t				size_type;
		typedef	ptrdiff_t			difference_type;

		heapvector() : pStart(0), pEnd(0), pEndAlloc(0) {}
		~heapvector() {
			if (pStart)
				HeapFree(GetProcessHeap(), 0, pStart);
		}

		iterator begin() { return pStart; }
		const_iterator begin() const { return pStart; }
		iterator end() { return pEnd; }
		const_iterator end() const { return pEnd; }

		reference_type operator[](size_type i) { return pStart[i]; }
		const_reference_type operator[](size_type i) const { return pStart[i]; }

		bool empty() const { return pEnd == pStart; }
		size_type size() const { return pEnd-pStart; }
		size_type capacity() const { return pEndAlloc-pStart; }

		void resize(size_type s) {
			if (capacity() < s)
				reserve(std::min<size_type>(size()*2, s));

			pEnd = pStart + s;
		}

		void reserve(size_type s) {
			if (s > capacity()) {
				HANDLE h = GetProcessHeap();
				size_type siz = size();
				T *pNewBlock = (T*)HeapAlloc(h, 0, s * sizeof(T));

				if (pStart) {
					memcpy(pNewBlock, pStart, (char *)pEnd - (char *)pStart);
					HeapFree(h, 0, pStart);
				}

				pStart = pNewBlock;
				pEnd = pStart + siz;
				pEndAlloc = pStart + s;
			}
		}
			
		void push_back(const T& x) {
			if (pEnd == pEndAlloc)
				reserve(pEndAlloc==pStart ? 16 : size()*2);

			*pEnd++ = x;
		}

	protected:
		T *pStart, *pEnd, *pEndAlloc;

		union trivial_check { T x; };
	};

	struct BlockInfo {
		const CrtBlockHeader *pBlock;
		bool marked;
	};

	bool operator<(const BlockInfo& x, const BlockInfo& y) {
		return (uintptr)x.pBlock < (uintptr)y.pBlock;
	}

	bool operator<(uintptr x, const BlockInfo& y) {
		return x < (uintptr)y.pBlock;
	}

	bool operator<(const BlockInfo& x, uintptr y) {
		return (uintptr)x.pBlock < y;
	}
}

void VDCDECL ATDumpMemoryLeaksVC() {
    _CrtMemState msNow;

	// disable CRT tracking of memory blocks
	_CrtSetDbgFlag(_CrtSetDbgFlag(0) & ~(_CRTDBG_ALLOC_MEM_DF | _CRTDBG_LEAK_CHECK_DF));

	// checkpoint the current memory layout
    _CrtMemCheckpoint(&msNow);

	// traverse memory
	typedef heapvector<BlockInfo> tHeapInfo;
	tHeapInfo heapinfo;

	const CrtBlockHeader *pHdr = (const CrtBlockHeader *)msNow.pBlockHeader;
	for(; pHdr; pHdr = pHdr->pNext) {
		const int type = (pHdr->type & 0xffff);

		if (type != _CLIENT_BLOCK && type != _NORMAL_BLOCK)
			continue;

		BlockInfo info = {
			pHdr,
			false
		};

		heapinfo.push_back(info);
	}

	// check if we found any leaks
	if (heapinfo.empty())
		return;

	// okay, try to load dbghelp
	ATDbgHelpDynamicLoaderW32 dbghelp;

	if (!dbghelp.ready()) {
		_CrtDumpMemoryLeaks();
		return;
	}

	HANDLE hProc = GetCurrentProcess();

	dbghelp.pSymInitialize(hProc, NULL, FALSE);

	TCHAR filename[MAX_PATH], path[MAX_PATH];
	GetModuleFileName(NULL, filename, vdcountof(filename));

	_tcscpy(path, filename);
	*VDFileSplitPath(path) = 0;

	dbghelp.pSymSetSearchPath(hProc, path);
	SetCurrentDirectory(path);

#ifdef UNICODE
	CHAR filenameA[MAX_PATH];
	filenameA[0] = 0;
	WideCharToMultiByte(CP_ACP, 0, filename, -1, filenameA, vdcountof(filenameA), NULL, NULL);
#else
	TCHAR *const filenameA = filename;
#endif

	DWORD64 dwAddr = dbghelp.pSymLoadModule64(hProc, NULL, filenameA, NULL, (DWORD64)GetModuleHandle(NULL), 0);

	IMAGEHLP_MODULE64 modinfo = {sizeof(IMAGEHLP_MODULE64)};

	dbghelp.pSymGetModuleInfo64(hProc, dwAddr, &modinfo);


	_RPT0(0, "\n\n===== MEMORY LEAKS DETECTED =====\n\n");

	std::sort(heapinfo.begin(), heapinfo.end());

	tHeapInfo::iterator itBase(heapinfo.begin());
	for(tHeapInfo::iterator it(itBase), itEnd(heapinfo.end()); it!=itEnd; ++it) {
		BlockInfo& blk = *it;
		size_t pointers = blk.pBlock->size / sizeof(void *);
		uintptr *pp = (uintptr *)blk.pBlock->data;

		for(size_t i=0; i<pointers; ++i) {
			uintptr ip = pp[i];

			tHeapInfo::iterator itTarget(std::upper_bound(itBase, itEnd, ip));

			if (itTarget != itBase) {
				BlockInfo& blk2 = *--itTarget;

				if (ip - (uintptr)blk2.pBlock->data < blk2.pBlock->size)
					blk2.marked = true;
			}
		}
	}

	for(int pass=0; pass<2; ++pass) {
		bool test = pass ? true : false;

		if (test) {
			_RPT0(0, "\nSecondary leaks:\n\n");
		} else {
			_RPT0(0, "\nPrimary leaks:\n\n");
		}

		for(tHeapInfo::iterator it(heapinfo.begin()), itEnd(heapinfo.end()); it!=itEnd; ++it) {
			BlockInfo& blk = *it;

			if (blk.marked != test)
				continue;

			pHdr = blk.pBlock;

			char buf[1024], *s = buf;

			s += wsprintfA(buf, "    #%-5d %p (%8ld bytes)", pHdr->reqnum, pHdr->data, (long)pHdr->size);

			if (pHdr->pFilename && !strcmp(pHdr->pFilename, "return address")) {
#if VD_PTR_SIZE > 4
				void *pRet = (void *)((size_t)pHdr->line + (size_t)&__ImageBase);
#else
				void *pRet = (void *)pHdr->line;
#endif

				struct {
					IMAGEHLP_SYMBOL64 hdr;
					CHAR nameext[511];
				} sym;

				sym.hdr.SizeOfStruct = sizeof(IMAGEHLP_SYMBOL64);
				sym.hdr.MaxNameLength = 512;

				if (dbghelp.pSymGetSymFromAddr64(hProc, (DWORD64)pRet, 0, &sym.hdr)) {
					s += wsprintfA(s, "  Allocator: %p [%s]", pRet, sym.hdr.Name);
				} else
					s += wsprintfA(s, "  Allocator: %p", pRet);
			}

			if (pHdr->size >= sizeof(void *)) {
				void *vtbl = *(void **)pHdr->data;

				if (vtbl >= (char *)modinfo.BaseOfImage && vtbl < (char *)modinfo.BaseOfImage + modinfo.ImageSize) {
					struct {
						IMAGEHLP_SYMBOL64 hdr;
						CHAR nameext[511];
					} sym;

					sym.hdr.SizeOfStruct = sizeof(IMAGEHLP_SYMBOL64);
					sym.hdr.MaxNameLength = 512;

					char *t;

					if (dbghelp.pSymGetSymFromAddr64(hProc, (DWORD64)vtbl, 0, &sym.hdr) && (t = strstr(sym.hdr.Name, "::`vftable'"))) {
						*t = 0;
						s += wsprintfA(s, " [Type: %s]", sym.hdr.Name);
					}
				}
			}

			*s = 0;

			_RPT1(0, "%s\n", buf);
		}
	}

	_RPT0(0, "\nEnd of leak dump.\n");

	dbghelp.pSymCleanup(hProc);
}

#pragma section(".CRT$XPB",long,read)

extern "C" {
	static __declspec(allocate(".CRT$XPB")) void (__cdecl *g_leaktrap)() = ATDumpMemoryLeaksVC;
}

#endif
