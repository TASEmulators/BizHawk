//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2011 Avery Lee
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
#define INITGUID
#include <vd2/system/filesys.h>
#include <vd2/system/process.h>
#include <vd2/system/w32assist.h>
#include <windows.h>
#include <shellapi.h>
#include <shlwapi.h>
#include <shlobj.h>
#include "resource.h"
#include <at/atnativeui/dialog.h>
#include <at/atnativeui/uiproxies.h>
#include "oshelper.h"

#ifndef SEE_MASK_WAITFORINPUTIDLE
#define SEE_MASK_WAITFORINPUTIDLE  0x02000000
#endif

#ifndef __IApplicationAssociationRegistrationUI_INTERFACE_DEFINED__
#define __IApplicationAssociationRegistrationUI_INTERFACE_DEFINED__

typedef struct IApplicationAssociationRegistrationUI IApplicationAssociationRegistrationUI;

struct IApplicationAssociationRegistrationUI : public IUnknown
{
	virtual HRESULT STDMETHODCALLTYPE LaunchAdvancedAssociationUI(LPCWSTR pszAppRegistryName) = 0;
};

DEFINE_GUID(IID_IApplicationAssociationRegistrationUI, 0x1f76a169, 0xf994, 0x40ac, 0x8f, 0xc8, 0x09, 0x59, 0xe8, 0x87, 0x47, 0x10);
DEFINE_GUID(CLSID_ApplicationAssociationRegistrationUI, 0x1968106d, 0xf3b5, 0x44cf, 0x89, 0x0e, 0x11, 0x6f, 0xcb, 0x9e, 0xce, 0xf1);

#endif

///////////////////////////////////////////////////////////////////////////

namespace {
	bool WriteRegistryStringValue(HKEY hkey, const wchar_t *valueName, const wchar_t *s) {
		return ERROR_SUCCESS == RegSetValueExW(hkey, valueName, 0, REG_SZ, (const BYTE *)s, (DWORD)((wcslen(s) + 1) * sizeof(WCHAR)));
	}

	bool ReadRegistryStringValue(HKEY hkey, const wchar_t *valueName, VDStringW& s) {
		DWORD type;
		DWORD cbData = 0;
		if (ERROR_SUCCESS == RegQueryValueExW(hkey, NULL, 0, &type, NULL, &cbData) && type == REG_SZ) {
			vdfastvector<WCHAR> buf(cbData / sizeof(WCHAR) + 1, 0);

			if (ERROR_SUCCESS == RegQueryValueExW(hkey, NULL, 0, NULL, (LPBYTE)buf.data(), &cbData) && cbData >= sizeof(WCHAR)) {
				VDStringSpanW existingProgId(buf.data(), buf.data() + cbData / sizeof(WCHAR) - 1);

				s = existingProgId;
				return true;
			}
		}

		s.clear();
		return false;
	}

	bool ReadRegistryStringValue(HKEY hkeyBase, const wchar_t *keyName, const wchar_t *valueName, VDStringW& s) {
		HKEY hkey;
		LONG err = RegOpenKeyExW(hkeyBase, keyName, 0, KEY_QUERY_VALUE, &hkey);
		if (err == ERROR_SUCCESS) {
			bool success = ReadRegistryStringValue(hkey, valueName, s);
			RegCloseKey(hkey);

			return success;
		}

		return false;
	}
}

///////////////////////////////////////////////////////////////////////////

struct ATFileAssociation {
	const wchar_t *mpExtList;
	const wchar_t *mpProgId;
	const wchar_t *mpDesc;
	const wchar_t *mpCommand;
	UINT mIconId;
} g_ATFileAssociations[]={
	{
		L"xex",
		L"Altirra.bin.1",
		L"Atari 8-bit Executable",
		L"/launch /run \"%1\"",
		IDI_XEX
	},
	{
		L"bin|car|rom|a52",
		L"Altirra.crt.1",
		L"Atari 8-bit Cartridge Image",
		L"/launch /cart \"%1\"",
		IDI_CART
	},
	{
		L"atr|dcm|atz|xfd|pro|atx",
		L"Altirra.dsk.1",
		L"Atari 8-bit Disk Image",
		L"/launch /disk \"%1\"",
		IDI_DISK
	},
	{
		L"cas",
		L"Altirra.tap.1",
		L"Atari 8-bit Tape Image",
		L"/launch /tape \"%1\"",
		IDI_TAPE
	},
};

void ATRegisterForDefaultPrograms(bool userOnly) {
	const HKEY hkeyRoot = userOnly ? HKEY_CURRENT_USER : HKEY_LOCAL_MACHINE;
	HKEY hkey;

	LONG err = RegCreateKeyExW(hkeyRoot, L"SOFTWARE\\RegisteredApplications", 0, NULL, REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &hkey, NULL);
	if (err == ERROR_SUCCESS) {
		WriteRegistryStringValue(hkey, L"Altirra", L"Software\\virtualdub.org\\Altirra\\Capabilities");
		RegCloseKey(hkey);
	}

	err = RegCreateKeyExW(hkeyRoot, L"Software\\virtualdub.org\\Altirra\\Capabilities", 0, NULL, REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &hkey, NULL);
	if (err == ERROR_SUCCESS) {
		WriteRegistryStringValue(hkey, L"ApplicationDescription", L"Altirra 8-bit computer emulator");

		HKEY hkey2;
		err = RegCreateKeyExW(hkey, L"FileAssociations", 0, NULL, REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &hkey2, NULL);
		if (err == ERROR_SUCCESS) {
			for(size_t i=0; i<sizeof(g_ATFileAssociations)/sizeof(g_ATFileAssociations[0]); ++i) {
				const ATFileAssociation& assoc = g_ATFileAssociations[i];

				VDStringRefW exts(assoc.mpExtList);
				VDStringRefW ext;

				while(!exts.empty()) {
					if (!exts.split('|', ext)) {
						ext = exts;
						exts.clear();
					}

					VDStringW keyName(L".");
					keyName += ext;
					WriteRegistryStringValue(hkey2, keyName.c_str(), assoc.mpProgId);
				}
			}

			RegCloseKey(hkey2);
		}

		RegCloseKey(hkey);
	}
}

void ATCreateFileAssociationProgId(uint32 index, bool userOnly) {
	HKEY hkey;

	if (index >= sizeof(g_ATFileAssociations)/sizeof(g_ATFileAssociations[0]))
		return;

	const ATFileAssociation& assoc = g_ATFileAssociations[index];

	LONG err = RegCreateKeyExW(userOnly ? HKEY_CURRENT_USER : HKEY_LOCAL_MACHINE, (VDStringW(L"Software\\Classes\\") + assoc.mpProgId).c_str(), 0, NULL, REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &hkey, NULL);
	if (err == ERROR_SUCCESS) {
		// set file type description
		RegSetValueExW(hkey, NULL, 0, REG_SZ, (const BYTE *)assoc.mpDesc, (DWORD)((wcslen(assoc.mpDesc) + 1) * sizeof(wchar_t)));

		// set default icon
		HKEY hkey2;
		err = RegCreateKeyExW(hkey, L"DefaultIcon", 0, NULL, REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &hkey2, NULL);
		if (err == ERROR_SUCCESS) {
			VDStringW iconPath;
			iconPath.sprintf(L"%ls,-%u", VDGetProgramFilePath().c_str(), assoc.mIconId);
			RegSetValueExW(hkey2, NULL, 0, REG_SZ, (const BYTE *)iconPath.c_str(), (iconPath.size() + 1) * sizeof(wchar_t));

			RegCloseKey(hkey2);
		}

		// Annoyingly, ApplicationCompany and FriendlyAppName are required for user-local registrations to
		// work. They are not necessary for system-wide registrations. If they are missing, LaunchAdvancedAssociationUI()
		// will fail with an error.
		err = RegCreateKeyExW(hkey, L"Application", 0, NULL, REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &hkey2, NULL);
		if (err == ERROR_SUCCESS) {
			WriteRegistryStringValue(hkey2, L"ApplicationCompany", L"virtualdub.org");
			RegCloseKey(hkey2);
		}

		err = RegCreateKeyExW(hkey, L"shell\\open", 0, NULL, REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &hkey2, NULL);
		if (err == ERROR_SUCCESS) {
			WriteRegistryStringValue(hkey2, L"FriendlyAppName", L"Altirra");
			RegCloseKey(hkey2);
		}

		err = RegCreateKeyExW(hkey, L"shell\\open\\command", 0, NULL, REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &hkey2, NULL);
		if (err == ERROR_SUCCESS) {
			const VDStringW& programPath = VDGetProgramFilePath();

			VDStringW cmdLine;
			cmdLine = L"\"";
			cmdLine += programPath;
			cmdLine += L"\" ";
			cmdLine += assoc.mpCommand;

			RegSetValueExW(hkey2, NULL, 0, REG_SZ, (const BYTE *)cmdLine.c_str(), (cmdLine.size() + 1) * sizeof(wchar_t));

			RegCloseKey(hkey2);
		}

		RegCloseKey(hkey);
	}
}

void ATDeleteFileAssociationProgId(uint32 index, bool userOnly) {
	HKEY hkey;

	if (index >= sizeof(g_ATFileAssociations)/sizeof(g_ATFileAssociations[0]))
		return;

	const ATFileAssociation& assoc = g_ATFileAssociations[index];

	const HKEY hkeyRoot = userOnly ? HKEY_CURRENT_USER : HKEY_LOCAL_MACHINE;
	const VDStringW keyName = VDStringW(L"Software\\Classes\\") + assoc.mpProgId;
	LONG err = RegOpenKeyExW(hkeyRoot, keyName.c_str(), 0, KEY_ALL_ACCESS, &hkey);
	if (err == ERROR_SUCCESS) {
		RegCloseKey(hkey);

		SHDeleteKeyW(hkeyRoot, keyName.c_str());
	}
}

void ATSetFileAssociations(uint32 index, uint32 extmask, bool userOnly) {
	HKEY hkey;

	if (index >= sizeof(g_ATFileAssociations)/sizeof(g_ATFileAssociations[0]))
		return;

	const HKEY hkeyRoot = userOnly ? HKEY_CURRENT_USER : HKEY_LOCAL_MACHINE;
	const ATFileAssociation& assoc = g_ATFileAssociations[index];
	const wchar_t *extList = assoc.mpExtList;
	LONG err;

	for(;;) {
		const wchar_t *extEnd = extList;

		while(*extEnd && *extEnd != L'|')
			++extEnd;

		VDStringW ext(L".");
		ext.append(extList, extEnd);

		const VDStringW keyName = VDStringW(L"Software\\Classes\\") + ext;
		if (extmask & 1) {
			err = RegCreateKeyExW(hkeyRoot, keyName.c_str(), 0, NULL, REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &hkey, NULL);
			if (err == ERROR_SUCCESS) {
				RegSetValueExW(hkey, NULL, 0, REG_SZ, (const BYTE *)assoc.mpProgId, (DWORD)((wcslen(assoc.mpProgId) + 1) * sizeof(wchar_t)));

				HKEY hkey2;
				err = RegCreateKeyExW(hkey, L"OpenWithProgids", 0, NULL, REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &hkey2, NULL);
				if (err == ERROR_SUCCESS) {
					RegSetValueExW(hkey2, assoc.mpProgId, 0, REG_NONE, 0, 0);
					RegCloseKey(hkey2);
				}

				RegCloseKey(hkey);
			}
		} else {
			err = RegOpenKeyExW(hkeyRoot, keyName.c_str(), 0, KEY_ALL_ACCESS, &hkey);
			if (err == ERROR_SUCCESS) {
				VDStringW value;
				if (ReadRegistryStringValue(hkey, NULL, value) && value == assoc.mpProgId) {
					RegDeleteValueW(hkey, NULL);
				}

				HKEY hkey2;
				err = RegOpenKeyExW(hkey, L"OpenWithProgids", 0, KEY_ALL_ACCESS, &hkey2);
				if (err == ERROR_SUCCESS) {
					RegDeleteValueW(hkey2, assoc.mpProgId);
					RegCloseKey(hkey2);
				}

				RegCloseKey(hkey);
			}			
		}

		extmask >>= 1;

		if (!*extEnd)
			break;

		extList = extEnd + 1;
	}
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogFileAssociations : public VDDialogFrameW32 {
public:
	ATUIDialogFileAssociations(bool userOnly);

protected:
	bool OnLoaded();
	void OnDataExchange(bool write);

	class FileAssocItem : public vdrefcounted<IVDUIListViewVirtualItem> {
	public:
		virtual void GetText(int subItem, VDStringW& s) const {
			if (subItem)
				s = mpAssoc->mpDesc;
			else
				s = mExt;
		}

		const ATFileAssociation *mpAssoc;
		VDStringW mExt;
		uint32 mAssocIndex;
		uint32 mExtIndex;
		bool mbEnabled;
	};

	class FileAssocItemSorter : public IVDUIListViewVirtualComparer {
	public:
		virtual int Compare(IVDUIListViewVirtualItem *x, IVDUIListViewVirtualItem *y) {
			return static_cast<FileAssocItem *>(x)->mExt.compare(static_cast<FileAssocItem *>(y)->mExt);
		}
	};

	VDUIProxyListView mListView;
	bool mbUserOnly;
};

ATUIDialogFileAssociations::ATUIDialogFileAssociations(bool userOnly)
	: VDDialogFrameW32(IDD_FILE_ASSOC)
	, mbUserOnly(userOnly)
{
}

bool ATUIDialogFileAssociations::OnLoaded() {
	AddProxy(&mListView, IDC_LIST1);

	mListView.SetRedraw(false);
	mListView.SetItemCheckboxesEnabled(true);
	mListView.SetFullRowSelectEnabled(true);
	mListView.InsertColumn(0, L"Ext", 0);
	mListView.InsertColumn(1, L"Description", 0);

	VDStringW exePath(VDGetProgramFilePath());

	for(uint32 i = 0; i < sizeof(g_ATFileAssociations)/sizeof(g_ATFileAssociations[0]); ++i) {
		const ATFileAssociation& assoc = g_ATFileAssociations[i];

		const wchar_t *extList = assoc.mpExtList;
		uint32 extIndex = 0;

		for(;;) {
			const wchar_t *extEnd = extList;

			while(*extEnd && *extEnd != L'|')
				++extEnd;

			FileAssocItem *item = new FileAssocItem;
			item->mpAssoc = &assoc;
			item->mExt.assign(extList, extEnd);
			item->mAssocIndex = i;
			item->mExtIndex = extIndex++;

			int idx = mListView.InsertVirtualItem(-1, item);
			if (idx >= 0) {
				VDStringW keyName;
				keyName.sprintf(L".%ls", item->mExt.c_str());

				DWORD len = 0;
				HRESULT hr = AssocQueryStringW((ASSOCF)0, ASSOCSTR_EXECUTABLE, keyName.c_str(), NULL, NULL, &len);

				if (hr == S_FALSE) {
					++len;
					vdfastvector<WCHAR> assocExePath(len, 0);

					hr = AssocQueryStringW((ASSOCF)0, ASSOCSTR_EXECUTABLE, keyName.c_str(), NULL, assocExePath.data(), &len);

					if (SUCCEEDED(hr)) {
						if (VDFileIsPathEqual(assocExePath.data(), exePath.c_str()))
							mListView.SetItemChecked(idx, true);
					}
				}
			}

			if (!*extEnd)
				break;

			extList = extEnd + 1;
		}
	}

	FileAssocItemSorter sorter;
	mListView.Sort(sorter);
	mListView.AutoSizeColumns(false);
	mListView.SetRedraw(true);

	SetFocusToControl(IDC_LIST1);
	return true;
}

void ATUIDialogFileAssociations::OnDataExchange(bool write) {
	if (write) {
		int n = mListView.GetItemCount();

		typedef vdfastvector<FileAssocItem *> Items;
		Items items;
		items.reserve(n);

		for(int i=0; i<n; ++i) {
			FileAssocItem *item = static_cast<FileAssocItem *>(mListView.GetVirtualItem(i));

			if (item) {
				item->mbEnabled = mListView.IsItemChecked(i);
				items.push_back(item);
			}
		}

		for(Items::iterator it(items.begin());
			it != items.end();
			++it)
		{
			FileAssocItem *item = *it;

			uint32 extMask = 0;
			
			if (item->mbEnabled)
				extMask = 1 << item->mExtIndex;

			Items::iterator it2(it + 1);
			while(it2 != items.end()) {
				FileAssocItem *item2 = *it2;

				if (item2->mpAssoc == item->mpAssoc) {
					if (item2->mbEnabled)
						extMask |= 1 << item2->mExtIndex;

					*it2 = items.back();
					items.pop_back();
				} else {
					++it2;
				}
			}

			if (extMask)
				ATCreateFileAssociationProgId(item->mAssocIndex, mbUserOnly);
			else
				ATDeleteFileAssociationProgId(item->mAssocIndex, mbUserOnly);

			ATSetFileAssociations(item->mAssocIndex, extMask, mbUserOnly);
		}

		SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, NULL, NULL);
	}
}

///////////////////////////////////////////////////////////////////////////

void ATUIShowDialogFileAssociationsVista(bool userOnly) {
	IApplicationAssociationRegistrationUI *pAARegUI = NULL;

	ATRegisterForDefaultPrograms(userOnly);

	for(size_t i=0; i<sizeof(g_ATFileAssociations)/sizeof(g_ATFileAssociations[0]); ++i)
		ATCreateFileAssociationProgId((uint32)i, userOnly);

	// If we are on Windows 10 or higher, LaunchAdvancedAssociationUI() is useless because all
	// it does is display a dialog box telling the user to go to Settings. Worse, the user
	// can't refer to the dialog because it is modal, and it doesn't mention that the user
	// needs to click on the small Set Defaults By App link at the bottom, which can be missed
	// if the window is small enough to require scrolling.
	//
	// This annoying change dropped late in the Windows 10 Insider Preview cycle and persists
	// through Threshold 2, and to this date (Nov 2015) no documentation has actually been
	// released about how desktop programs are supposed to handle this, besides a patronizing
	// Windows build 10122 announcement that says that programs will need to be updated to
	// the Windows 10 way of handling file associations, without actually having any info
	// for developers describing what that is. /rant
	//
	// To work around this brain damaged UI flow while still trying to do "the right thing,"
	// we launch the Default Programs UI directly, which is all that the Settings link does
	// anyway, and which is still accessible through Control Panel. The user will still have
	// to choose the program from the list.
	//
	// Note that in order to properly detect Windows 10, the application manifest must be
	// marked Win10-compatible.

	if (VDIsAtLeast10W32()) {
		VDLaunchProgram(VDMakePath(VDGetSystemPath().c_str(), L"control.exe").c_str(),
			L"/name Microsoft.DefaultPrograms /page pageDefaultProgram");
	} else {
		HRESULT hr = CoCreateInstance(CLSID_ApplicationAssociationRegistrationUI, NULL,
			CLSCTX_INPROC, IID_IApplicationAssociationRegistrationUI, (void **)&pAARegUI);

		if (FAILED(hr))
			return;

		pAARegUI->LaunchAdvancedAssociationUI(L"Altirra");
		pAARegUI->Release();
	}
}

///////////////////////////////////////////////////////////////////////////

void ATRelaunchElevated(VDGUIHandle parent, const wchar_t *params) {
	const VDStringW path = VDGetProgramFilePath();

	SHELLEXECUTEINFOW execInfo = {sizeof(SHELLEXECUTEINFOW)};
	execInfo.fMask = SEE_MASK_NOCLOSEPROCESS | SEE_MASK_UNICODE | SEE_MASK_WAITFORINPUTIDLE;
	execInfo.hwnd = (HWND)parent;
	execInfo.lpVerb = L"runas";
	execInfo.lpFile = path.c_str();
	execInfo.lpParameters = params;
	execInfo.nShow = SW_SHOWNORMAL;

	if (ShellExecuteExW(&execInfo) && execInfo.hProcess) {
		for(;;) {
			DWORD r = MsgWaitForMultipleObjects(1, &execInfo.hProcess, FALSE, INFINITE, QS_ALLINPUT);

			if (r == WAIT_OBJECT_0 + 1) {
				MSG msg;
				while(PeekMessage(&msg, NULL, 0, 0, PM_REMOVE)) {
					if (CallMsgFilter(&msg, 0))
						continue;

					TranslateMessage(&msg);
					DispatchMessage(&msg);
				}
			} else
				break;
		}

		CloseHandle(execInfo.hProcess);
	}
}

void ATUIShowDialogSetFileAssociations(VDGUIHandle parent, bool allowElevation, bool userOnly) {
	if (!userOnly && !ATIsUserAdministrator() && allowElevation) {
		VDStringW params;
		params.sprintf(L"/showfileassocdlg %llx", (unsigned long long)(uintptr_t)parent);

		ATRelaunchElevated(parent, params.c_str());
		return;
	}

	if (VDIsAtLeastVistaW32()) {
		ATUIShowDialogFileAssociationsVista(userOnly);
	} else {
		ATUIDialogFileAssociations dlg(userOnly);

		dlg.ShowDialog(parent);
	}
}

void ATUIShowDialogRemoveFileAssociations(VDGUIHandle parent, bool allowElevation, bool userOnly) {
	if (!userOnly && allowElevation) {
		if (!ATIsUserAdministrator()) {
			VDStringW params;
			params.sprintf(L"/removefileassocs %llx", (unsigned long long)(uintptr_t)parent);

			ATRelaunchElevated(parent, params.c_str());
			return;
		}
	}

	if (IDOK != MessageBoxW((HWND)parent, L"Are you sure you want to disable all of this program's file associations?", L"Altirra Warning", MB_ICONEXCLAMATION | MB_OKCANCEL))
		return;

	const HKEY hkey = userOnly ? HKEY_CURRENT_USER : HKEY_LOCAL_MACHINE;

	// Remove Default Programs registration
	SHDeleteValueW(hkey, L"SOFTWARE\\RegisteredApplications", L"Altirra");

	// Remove local machine registration
	SHDeleteKeyW(hkey, L"Software\\virtualdub.org\\Altirra\\Capabilities");
	SHDeleteEmptyKeyW(hkey, L"Software\\virtualdub.org\\Altirra");

	// Remove all ProgIDs. We purposely leave the extension references dangling as
	// that is recommended by Microsoft -- the shell handles this.
	for(size_t i=0; i<sizeof(g_ATFileAssociations)/sizeof(g_ATFileAssociations[0]); ++i) {
		const ATFileAssociation& assoc = g_ATFileAssociations[i];

		SHDeleteKeyW(hkey, (VDStringW(L"Software\\Classes\\") + assoc.mpProgId).c_str());
	}

	SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, NULL, NULL);

	MessageBoxW((HWND)parent, L"File associations removed.", L"Altirra", MB_ICONINFORMATION | MB_OK);
}
