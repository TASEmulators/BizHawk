#include <stdafx.h>
#include <vd2/system/w32assist.h>
#include <vd2/Dita/accel.h>

void VDUIExtractAcceleratorTableW32(VDAccelTableDefinition& dst, HACCEL haccel, const VDAccelToCommandEntry *pCommands, uint32 nCommands) {
	uint32 n = CopyAcceleratorTable(haccel, NULL, 0);
	vdfastvector<ACCEL> accels(n);

	n = CopyAcceleratorTable(haccel, accels.data(), n);

	dst.Clear();
	for(uint32 i=0; i<n; ++i) {
		const ACCEL& accel = accels[i];

		bool found = false;
		for(uint32 j=0; j<nCommands; ++j) {
			if (pCommands[j].mId == accel.cmd) {
				VDAccelTableEntry ent;
				ent.mCommandId = accel.cmd;
				ent.mpCommand = pCommands[j].mpName;

				ent.mAccel.mVirtKey = accel.key;
				ent.mAccel.mModifiers = 0;

				VDASSERT(accel.fVirt & FVIRTKEY);
				if (accel.fVirt & FALT)
					ent.mAccel.mModifiers |= VDUIAccelerator::kModAlt;

				if (accel.fVirt & FCONTROL)
					ent.mAccel.mModifiers |= VDUIAccelerator::kModCtrl;

				if (accel.fVirt & FSHIFT)
					ent.mAccel.mModifiers |= VDUIAccelerator::kModShift;

				switch(accel.key) {
					case VK_INSERT:
					case VK_DELETE:
					case VK_HOME:
					case VK_END:
					case VK_NEXT:
					case VK_PRIOR:
					case VK_LEFT:
					case VK_RIGHT:
					case VK_UP:
					case VK_DOWN:
						ent.mAccel.mModifiers |= VDUIAccelerator::kModExtended;
						break;
				}

				dst.Add(ent);
				found = true;
				break;
			}
		}

		VDASSERT(found);
	}
}

void VDUIGetAcceleratorStringInternal(const VDUIAccelerator& accel, VDStringW& s) {
	wchar_t buf[1024];

	UINT scanCode = MapVirtualKey(accel.mVirtKey, 0);
	if (!scanCode)
		return;

	LPARAM lParam = (scanCode << 16) | (1 << 25);

	if (accel.mModifiers & VDUIAccelerator::kModExtended)
		lParam |= (1 << 24);

	if (GetKeyNameTextW(lParam, buf, vdcountof(buf)))
		s.append(buf);
}

void VDUIGetAcceleratorString(const VDUIAccelerator& accel, VDStringW& s) {
	s.clear();

	if (accel.mModifiers & VDUIAccelerator::kModUp)
		s = L"^";

	if (accel.mModifiers & VDUIAccelerator::kModCooked) {
		s += L"\"";

		const wchar_t c = (wchar_t)accel.mVirtKey;
		VDUIAccelerator accel = {};

		switch(c) {
			case ' ':
				accel.mVirtKey = VK_SPACE;
				break;
			case '\t':
				accel.mVirtKey = VK_TAB;
				break;
			case '\b':
				accel.mVirtKey = VK_BACK;
				break;
			case '\r':
				accel.mVirtKey = VK_RETURN;
				break;
			case '\x1b':
				accel.mVirtKey = VK_ESCAPE;
				break;
		}

		if (accel.mVirtKey)
			VDUIGetAcceleratorStringInternal(accel, s);
		else
			s += c;

		s += L"\"";
	} else {
		// Rationale for ordering:
		//
		//	Ctrl+Alt+Del
		//	Alt+Shift+PrtSc
		//
		// Therefore, we use Ctrl+Alt+Shift+key ordering.

		if (accel.mModifiers & VDUIAccelerator::kModCtrl) {
			VDUIAccelerator accelCtrl;
			accelCtrl.mVirtKey = VK_CONTROL;
			accelCtrl.mModifiers = 0;
			VDUIGetAcceleratorStringInternal(accelCtrl, s);

			s += L"+";
		}

		if (accel.mModifiers & VDUIAccelerator::kModAlt) {
			VDUIAccelerator accelAlt;
			accelAlt.mVirtKey = VK_MENU;
			accelAlt.mModifiers = 0;
			VDUIGetAcceleratorStringInternal(accelAlt, s);

			s += L"+";
		}

		if (accel.mModifiers & VDUIAccelerator::kModShift) {
			VDUIAccelerator accelShift;
			accelShift.mVirtKey = VK_SHIFT;
			accelShift.mModifiers = 0;
			VDUIGetAcceleratorStringInternal(accelShift, s);

			s += L"+";
		}

		VDUIGetAcceleratorStringInternal(accel, s);
	}
}

bool VDUIGetVkAcceleratorForChar(VDUIAccelerator& accel, wchar_t c) {
	SHORT vkCode = VkKeyScanW(c);
	if (vkCode == (SHORT)-1)
		return false;

	if (vkCode & 0xF800)
		return false;

	accel.mVirtKey = LOBYTE(vkCode);
	accel.mModifiers = 0;

	if (vkCode & 0x100)
		accel.mModifiers += VDUIAccelerator::kModShift;

	if (vkCode & 0x200)
		accel.mModifiers += VDUIAccelerator::kModCtrl;

	if (vkCode & 0x400)
		accel.mModifiers += VDUIAccelerator::kModAlt;

	return true;
}

bool VDUIGetCharAcceleratorForVk(VDUIAccelerator& accel) {
	if (accel.mModifiers & VDUIAccelerator::kModCooked)
		return true;

	// map the virtual key to a scan code
	UINT scanCode = MapVirtualKey(accel.mVirtKey, MAPVK_VK_TO_VSC);
	if (!scanCode)
		return false;

	if (scanCode > 0xFF)
		return false;

	// build keyboard state
	BYTE keyState[256] = {0};

	keyState[scanCode] = 0xFF;

	if (accel.mModifiers & VDUIAccelerator::kModShift) {
		keyState[VK_SHIFT] = 0xFF;
		keyState[VK_LSHIFT] = 0xFF;
	}

	if (accel.mModifiers & VDUIAccelerator::kModCtrl) {
		keyState[VK_CONTROL] = 0xFF;
		keyState[VK_LCONTROL] = 0xFF;
	}

	if (accel.mModifiers & VDUIAccelerator::kModAlt) {
		keyState[VK_MENU] = 0xFF;
		keyState[VK_LMENU] = 0xFF;
	}

	// map the virtual key + scan code to a character
	WCHAR outBuf[16];
	if (1 != ToUnicode(accel.mVirtKey, scanCode, keyState, outBuf, vdcountof(outBuf), 0))
		return false;

	accel.mModifiers = VDUIAccelerator::kModCooked;
	accel.mVirtKey = (uint32)outBuf[0];
	return true;
}

HACCEL VDUIBuildAcceleratorTableW32(const VDAccelTableDefinition& def) {
	uint32 n = def.GetSize();
	vdfastvector<ACCEL> accels(n);

	for(size_t i=0; i<n; ++i) {
		const VDAccelTableEntry& entry = def[i];

		ACCEL& accel = accels[i];

		accel.fVirt = FVIRTKEY;

		if (entry.mAccel.mModifiers & VDUIAccelerator::kModCtrl)
			accel.fVirt |= FCONTROL;

		if (entry.mAccel.mModifiers & VDUIAccelerator::kModShift)
			accel.fVirt |= FSHIFT;

		if (entry.mAccel.mModifiers & VDUIAccelerator::kModAlt)
			accel.fVirt |= FALT;

		accel.key = (WORD)entry.mAccel.mVirtKey;
		accel.cmd = (WORD)entry.mCommandId;
	}

	return CreateAcceleratorTable(accels.data(), n);
}

void VDUIUpdateMenuAcceleratorsW32(HMENU hmenu, const VDAccelTableDefinition& def) {
	int n = GetMenuItemCount(hmenu);

	VDStringA bufa;
	VDStringW keystr;
	for(int i=0; i<n; ++i) {
		MENUITEMINFOA miia;

		miia.cbSize		= sizeof(MENUITEMINFOA);
		miia.fMask		= MIIM_ID | MIIM_SUBMENU | MIIM_FTYPE;
		miia.dwTypeData	= NULL;
		miia.cch		= 0;

		if (GetMenuItemInfoA(hmenu, i, TRUE, &miia)) {
			if (miia.hSubMenu) {
				VDUIUpdateMenuAcceleratorsW32(miia.hSubMenu, def);
			} else {
				const uint32 id = miia.wID;

				miia.fMask		= MIIM_STRING;
				miia.dwTypeData = NULL;
				miia.cch		= 0;
				if (GetMenuItemInfoA(hmenu, i, TRUE, &miia)) {
					++miia.cch;
					bufa.resize(miia.cch);
		
					miia.dwTypeData	= (LPSTR)bufa.data();

					if (GetMenuItemInfoA(hmenu, i, TRUE, &miia)) {
						VDStringA::size_type pos = bufa.find('\t');
						if (pos != VDStringA::npos)
							bufa.resize(pos);
						else
							bufa.resize(miia.cch);

						const uint32 m = def.GetSize();
						for(uint32 j=0; j<m; ++j) {
							const VDAccelTableEntry& ent = def[j];

							if (ent.mCommandId == id) {
								VDUIGetAcceleratorString(ent.mAccel, keystr);
					
								bufa.push_back('\t');
								bufa.append(VDTextWToA(keystr).c_str());
								break;
							}
						}

						miia.fMask = MIIM_STRING;
						miia.dwTypeData = (LPSTR)bufa.c_str();
						VDVERIFY(SetMenuItemInfoA(hmenu, i, TRUE, &miia));
					}
				}
			}
		}
	}
}
