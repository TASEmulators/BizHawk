#include <stdafx.h>
#include "oshelper.h"
#include <windows.h>
#include <shlwapi.h>
#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include <vd2/system/filesys.h>
#include <vd2/system/registry.h>
#include <vd2/system/vdalloc.h>
#include <vd2/system/w32assist.h>
#include <vd2/Kasumi/pixmap.h>
#include <vd2/Kasumi/pixmapops.h>
#include <vd2/Kasumi/pixmaputils.h>
#include <vd2/Riza/bitmap.h>
#include <at/atnativeui/uiframe.h>
#include "decode_png.h"
#include "encode_png.h"

const void *ATLockResource(uint32 id, size_t& size) {
	HMODULE hmod = VDGetLocalModuleHandleW32();

	HRSRC hrsrc = FindResourceA(hmod, MAKEINTRESOURCEA(id), "STUFF");
	if (!hrsrc)
		return false;

	size = SizeofResource(hmod, hrsrc);

	HGLOBAL hg = LoadResource(hmod, hrsrc);
	const void *p = LockResource(hg);

	return p;
}

bool ATLoadKernelResource(int id, void *dst, uint32 offset, uint32 size, bool allowPartial) {
	HMODULE hmod = VDGetLocalModuleHandleW32();

	HRSRC hrsrc = FindResourceA(hmod, MAKEINTRESOURCEA(id), "KERNEL");
	if (!hrsrc)
		return false;

	DWORD rsize = SizeofResource(hmod, hrsrc);
	if (offset > rsize)
		return false;

	if ((rsize - offset) < size) {
		if (!allowPartial)
			return false;

		size = rsize - offset;
	}

	HGLOBAL hg = LoadResource(hmod, hrsrc);

	const void *p = LockResource(hg);

	if (!p)
		return false;

	memcpy(dst, (const char *)p + offset, size);

	return true;
}

bool ATLoadKernelResource(int id, vdfastvector<uint8>& buf) {
	HMODULE hmod = VDGetLocalModuleHandleW32();

	HRSRC hrsrc = FindResourceA(hmod, MAKEINTRESOURCEA(id), "KERNEL");
	if (!hrsrc)
		return false;

	DWORD rsize = SizeofResource(hmod, hrsrc);
	HGLOBAL hg = LoadResource(hmod, hrsrc);

	const uint8 *p = (const uint8 *)LockResource(hg);

	if (!p)
		return false;

	buf.assign(p, p + rsize);
	return true;
}

bool ATLoadKernelResourceLZPacked(int id, vdfastvector<uint8>& data) {
	HMODULE hmod = VDGetLocalModuleHandleW32();

	HRSRC hrsrc = FindResourceA(hmod, MAKEINTRESOURCEA(id), "KERNEL");
	if (!hrsrc)
		return false;

	HGLOBAL hg = LoadResource(hmod, hrsrc);
	const void *p = LockResource(hg);

	if (!p)
		return false;

	uint32 len = VDReadUnalignedLEU32(p);

	data.clear();
	data.resize(len);

	uint8 *dst = data.data();
	const uint8 *src = (const uint8 *)p + 4;

	for(;;) {
		uint8 c = *src++;

		if (!c)
			break;

		if (c & 1) {
			int distm1 = *src++;
			int len;

			if (c & 2) {
				distm1 += (c & 0xfc) << 6;
				len = *src++;
			} else {
				distm1 += ((c & 0x1c) << 6);
				len = c >> 5;
			}

			len += 3;

			const uint8 *csrc = dst - distm1 - 1;

			do {
				*dst++ = *csrc++;
			} while(--len);
		} else {
			c >>= 1;

			memcpy(dst, src, c);
			src += c;
			dst += c;
		}
	}

	return true;
}

bool ATLoadMiscResource(int id, vdfastvector<uint8>& data) {
	HMODULE hmod = VDGetLocalModuleHandleW32();

	HRSRC hrsrc = FindResourceA(hmod, MAKEINTRESOURCEA(id), "STUFF");
	if (!hrsrc)
		return false;

	DWORD rsize = SizeofResource(hmod, hrsrc);
	HGLOBAL hg = LoadResource(hmod, hrsrc);
	const void *p = LockResource(hg);

	if (!p)
		return false;

	data.resize(rsize);

	memcpy(data.data(), p, rsize);

	return true;
}

bool ATLoadImageResource(uint32 id, VDPixmapBuffer& buf) {
	HMODULE hmod = VDGetLocalModuleHandleW32();

	HRSRC hrsrc = FindResourceW(hmod, MAKEINTRESOURCEW(id), L"PNG");
	if (!hrsrc)
		return false;

	DWORD rsize = SizeofResource(hmod, hrsrc);
	HGLOBAL hg = LoadResource(hmod, hrsrc);
	const void *p = LockResource(hg);

	if (!p)
		return false;

	vdautoptr<IVDImageDecoderPNG> decoder(VDCreateImageDecoderPNG());

	if (decoder->Decode(p, rsize))
		return false;

	buf.assign(decoder->GetFrameBuffer());
	return true;
}

void ATFileSetReadOnlyAttribute(const wchar_t *path, bool readOnly) {
	VDStringA s;
	DWORD attrs;

	attrs = GetFileAttributesW(path);

	if (attrs == INVALID_FILE_ATTRIBUTES)
		throw MyWin32Error("Unable to change read-only flag on file: %s", GetLastError());

	if (readOnly)
		attrs |= FILE_ATTRIBUTE_READONLY;
	else
		attrs &= ~FILE_ATTRIBUTE_READONLY;

	BOOL success = SetFileAttributesW(path, attrs);

	if (!success)
		throw MyWin32Error("Unable to change read-only flag on file: %s", GetLastError());
}

void ATCopyFrameToClipboard(const VDPixmap& px) {
	if (::OpenClipboard(nullptr)) {
		if (::EmptyClipboard()) {
			HANDLE hMem;
			void *lpvMem;

			VDPixmapLayout layout;
			uint32 imageSize = VDMakeBitmapCompatiblePixmapLayout(layout, px.w, px.h, nsVDPixmap::kPixFormat_RGB888, 0);

			vdstructex<VDAVIBitmapInfoHeader> bih;
			VDMakeBitmapFormatFromPixmapFormat(bih, nsVDPixmap::kPixFormat_RGB888, 0, px.w, px.h);

			uint32 headerSize = (uint32)bih.size();

			if (hMem = ::GlobalAlloc(GMEM_MOVEABLE | GMEM_DDESHARE, headerSize + imageSize)) {
				if (lpvMem = ::GlobalLock(hMem)) {
					memcpy(lpvMem, bih.data(), headerSize);

					VDPixmapBlt(VDPixmapFromLayout(layout, (char *)lpvMem + headerSize), px);

					::GlobalUnlock(lpvMem);
					::SetClipboardData(CF_DIB, hMem);
					::CloseClipboard();
					return;
				}
				::GlobalFree(hMem);
			}
		}
		::CloseClipboard();
	}
}

void ATSaveFrame(const VDPixmap& px, const wchar_t *filename) {
	VDPixmapBuffer pxbuf(px.w, px.h, nsVDPixmap::kPixFormat_RGB888);

	VDPixmapBlt(pxbuf, px);

	vdautoptr<IVDImageEncoderPNG> encoder(VDCreateImageEncoderPNG());
	const void *mem;
	uint32 len;
	encoder->Encode(pxbuf, mem, len, false);

	VDFile f(filename, nsVDFile::kWrite | nsVDFile::kDenyRead | nsVDFile::kCreateAlways);

	f.write(mem, len);
}

void ATCopyTextToClipboard(void *hwnd, const char *s) {
	if (::OpenClipboard((HWND)hwnd)) {
		if (::EmptyClipboard()) {
			HANDLE hMem;
			void *lpvMem;

			size_t len = strlen(s);

			if (hMem = ::GlobalAlloc(GMEM_MOVEABLE | GMEM_DDESHARE, len + 1)) {
				if (lpvMem = ::GlobalLock(hMem)) {
					memcpy(lpvMem, s, len + 1);

					::GlobalUnlock(lpvMem);
					::SetClipboardData(CF_TEXT, hMem);
					::CloseClipboard();
					return;
				}
				::GlobalFree(hMem);
			}
		}
		::CloseClipboard();
	}
}

void ATCopyTextToClipboard(void *hwnd, const wchar_t *s) {
	if (!::OpenClipboard((HWND)hwnd))
		return;

	if (::EmptyClipboard()) {
		HANDLE hMem;
		void *lpvMem;

		size_t len = wcslen(s);

		if (hMem = ::GlobalAlloc(GMEM_MOVEABLE | GMEM_DDESHARE, (len + 1) * sizeof(WCHAR))) {
			if (lpvMem = ::GlobalLock(hMem)) {
				memcpy(lpvMem, s, (len + 1) * sizeof(WCHAR));

				::GlobalUnlock(lpvMem);
				::SetClipboardData(CF_UNICODETEXT, hMem);
				::CloseClipboard();
				return;
			}
			::GlobalFree(hMem);
		}
	}
	::CloseClipboard();
}

namespace {
	struct ATUISavedWindowPlacement {
		sint32 mLeft;
		sint32 mTop;
		sint32 mRight;
		sint32 mBottom;
		uint8 mbMaximized;
		uint8 mPad[3];
		uint32 mDpi;			// added - v3
	};
}

void ATUISaveWindowPlacement(void *hwnd, const char *name) {
	WINDOWPLACEMENT wp = {sizeof(WINDOWPLACEMENT)};

	if (GetWindowPlacement((HWND)hwnd, &wp)) {
		ATUISaveWindowPlacement(name,
			vdrect32 {
				wp.rcNormalPosition.left,
				wp.rcNormalPosition.top,
				wp.rcNormalPosition.right,
				wp.rcNormalPosition.bottom,
			},
			wp.showCmd == SW_MAXIMIZE,
			ATUIGetWindowDpiW32((HWND)hwnd));
	}
}

void ATUISaveWindowPlacement(const char *name, const vdrect32& r, bool isMaximized, uint32 dpi) {
	VDRegistryAppKey key("Window Placement");

	ATUISavedWindowPlacement sp {};
	sp.mLeft	= r.left;
	sp.mTop		= r.top;
	sp.mRight	= r.right;
	sp.mBottom	= r.bottom;
	sp.mbMaximized = isMaximized;
	sp.mDpi		= dpi;
	key.setBinary(name, (const char *)&sp, sizeof sp);
}

void ATUIRestoreWindowPlacement(void *hwnd, const char *name, int nCmdShow, bool sizeOnly) {
	if (nCmdShow < 0)
		nCmdShow = SW_SHOW;

	if (!IsZoomed((HWND)hwnd) && !IsIconic((HWND)hwnd)) {
		VDRegistryAppKey key("Window Placement");
		ATUISavedWindowPlacement sp = {0};

		// Earlier versions only saved a RECT.
		int len = key.getBinaryLength(name);

		if (len > (int)sizeof(ATUISavedWindowPlacement))
			len = sizeof(ATUISavedWindowPlacement);

		if (len >= offsetof(ATUISavedWindowPlacement, mbMaximized) && key.getBinary(name, (char *)&sp, len)) {
			WINDOWPLACEMENT wp = {sizeof(WINDOWPLACEMENT)};

			if (GetWindowPlacement((HWND)hwnd, &wp)) {
				wp.length			= sizeof(WINDOWPLACEMENT);
				wp.flags			= 0;
				wp.showCmd			= nCmdShow;

				sint32 width = sp.mRight - sp.mLeft;
				sint32 height = sp.mBottom - sp.mTop;

				// If we have a DPI value, try to compensate for DPI differences.
				if (sp.mDpi) {
					// Obtain the primary work area.
					RECT rWorkArea = {};
					if (SystemParametersInfo(SPI_GETWORKAREA, 0, &rWorkArea, FALSE)) {
						// Translate rcNormalPosition to screen coordinates.
						RECT rScreen {
							wp.rcNormalPosition.left + rWorkArea.left,
							wp.rcNormalPosition.top + rWorkArea.top,
							wp.rcNormalPosition.right + rWorkArea.left,
							wp.rcNormalPosition.bottom + rWorkArea.top,
						};

						HMONITOR hMon = MonitorFromRect(&rScreen, MONITOR_DEFAULTTONEAREST);
						uint32 currentDpi = ATUIGetMonitorDpiW32(hMon);

						if (currentDpi) {
							const double dpiConversionFactor = (double)currentDpi / (double)sp.mDpi;
							width = VDRoundToInt32((double)width * dpiConversionFactor);
							height = VDRoundToInt32((double)height * dpiConversionFactor);
						}
					}
				}

				if (sizeOnly) {
					wp.rcNormalPosition.right = wp.rcNormalPosition.left + width;
					wp.rcNormalPosition.bottom = wp.rcNormalPosition.top + height;
				} else {
					wp.rcNormalPosition.left = sp.mLeft;
					wp.rcNormalPosition.top = sp.mTop;
					wp.rcNormalPosition.right = sp.mLeft + width;
					wp.rcNormalPosition.bottom = sp.mTop + height;
				}

				if ((wp.showCmd == SW_SHOW || wp.showCmd == SW_SHOWNORMAL || wp.showCmd == SW_SHOWDEFAULT) && sp.mbMaximized)
					wp.showCmd = SW_SHOWMAXIMIZED;

				SetWindowPlacement((HWND)hwnd, &wp);
			}
		}
	}
}

void ATUIEnableEditControlAutoComplete(void *hwnd) {
	if (hwnd)
		SHAutoComplete((HWND)hwnd, SHACF_FILESYSTEM | SHACF_AUTOAPPEND_FORCE_OFF);
}

VDStringW ATGetHelpPath() {
	return VDMakePath(VDGetProgramPath().c_str(), L"Altirra.chm");
}

void ATShowHelp(void *hwnd, const wchar_t *filename) {
	try {
		VDStringW helpFile(ATGetHelpPath());

		if (!VDDoesPathExist(helpFile.c_str()))
			throw MyError("Cannot find help file: %ls", helpFile.c_str());

		// If we're on Windows NT, check for the ADS and/or network drive.
		{
			VDStringW helpFileADS(helpFile);
			helpFileADS += L":Zone.Identifier";
			if (VDDoesPathExist(helpFileADS.c_str())) {
				int rv = MessageBox((HWND)hwnd, _T("Altirra has detected that its help file, Altirra.chm, has an Internet Explorer download location marker on it. This may prevent the help file from being displayed properly, resulting in \"Action canceled\" errors being displayed. Would you like to remove it?"), _T("Altirra warning"), MB_YESNO|MB_ICONEXCLAMATION);

				if (rv == IDYES)
					DeleteFileW(helpFileADS.c_str());
			}
		}

		if (filename) {
			helpFile.append(L"::/");
			helpFile.append(filename);
		}

		VDStringW helpCommand(VDStringW(L"\"hh.exe\" \"") + helpFile + L'"');

		PROCESS_INFORMATION pi;
		BOOL retval;

		// CreateProcess will actually modify the string that it gets, soo....
		{
			STARTUPINFOW si = {sizeof(STARTUPINFOW)};
			std::vector<wchar_t> tempbufW(helpCommand.size() + 1, 0);
			helpCommand.copy(&tempbufW[0], (uint32)tempbufW.size());
			retval = CreateProcessW(NULL, &tempbufW[0], NULL, NULL, FALSE, CREATE_DEFAULT_ERROR_MODE, NULL, NULL, &si, &pi);
		}

		if (retval) {
			CloseHandle(pi.hThread);
			CloseHandle(pi.hProcess);
		} else
			throw MyWin32Error("Cannot launch HTML Help: %%s", GetLastError());
	} catch(const MyError& e) {
		e.post((HWND)hwnd, "Altirra Error");
	}
}

bool ATIsUserAdministrator() {
	if (!VDIsAtLeastVistaW32())
		return TRUE;

	BOOL isAdmin = FALSE;

	HMODULE hmodAdvApi = LoadLibraryW(L"advapi32");

	if (hmodAdvApi) {
		typedef BOOL (WINAPI *tpCreateWellKnownSid)(WELL_KNOWN_SID_TYPE WellKnownSidType, PSID DomainSid, PSID pSid, DWORD *cbSid);
		tpCreateWellKnownSid pCreateWellKnownSid = (tpCreateWellKnownSid)GetProcAddress(hmodAdvApi, "CreateWellKnownSid");

		if (pCreateWellKnownSid) {
			DWORD sidLen = SECURITY_MAX_SID_SIZE;
			BYTE localAdminsGroupSid[SECURITY_MAX_SID_SIZE];

			if (pCreateWellKnownSid(WinBuiltinAdministratorsSid, NULL, localAdminsGroupSid, &sidLen)) {
				CheckTokenMembership(NULL, localAdminsGroupSid, &isAdmin);
			}
		}

		FreeLibrary(hmodAdvApi);
	}

	return isAdmin != 0;
}

void ATGenerateGuid(uint8 rawguid[16]) {
	GUID guid = {0};
	CoCreateGuid(&guid);

	memcpy(rawguid, &guid, 16);
}
