//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2010 Avery Lee
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

#ifndef f_AT_UIKEYBOARD_H
#define f_AT_UIKEYBOARD_H

struct VDAccelTableEntry;
class VDAccelTableDefinition;

struct ATUIKeyboardOptions {
	enum ArrowKeyMode {
		kAKM_InvertCtrl,	// Ctrl state is inverted between host and emulation
		kAKM_AutoCtrl,		// Ctrl state is injected only for unmodded case
		kAKM_DefaultCtrl,	// Shift/Ctrl states are passed through
		kAKMCount
	};

	enum LayoutMode {
		kLM_Natural,
		kLM_Raw,
		kLM_Custom,
		kLMCount
	};

	bool mbRawKeys;
	bool mbFullRawKeys;
	bool mbEnableFunctionKeys;
	bool mbAllowShiftOnColdReset;
	bool mbAllowInputMapOverlap;
	ArrowKeyMode mArrowKeyMode;
	LayoutMode mLayoutMode;
};

bool ATUIGetDefaultScanCodeForCharacter(char c, uint8& ch);
bool ATUIGetScanCodeForCharacter(char c, uint32& ch);
void ATUIInitVirtualKeyMap(const ATUIKeyboardOptions& options);
bool ATUIGetScanCodeForVirtualKey(uint32 virtKey, bool alt, bool ctrl, bool shift, bool extended, uint32& scanCode);

void ATUIGetDefaultKeyMap(const ATUIKeyboardOptions& options, vdfastvector<uint32>& mappings);
void ATUIGetCustomKeyMap(vdfastvector<uint32>& mappings);
void ATUISetCustomKeyMap(const uint32 *mappings, size_t n);

bool ATIsValidScanCode(uint32 c);

// Returns the readable name for a key code, or nullptr if the key code is not
// valid.
const wchar_t *ATUIGetNameForKeyCode(uint32 c);

// Keyboard mappings are packed in bitfields as follows:
//
// Bits 31-25: modifiers
// Bits 24-9: virtual key or character code
// Bits 0-8: scan code
enum ATUIKeyboardMappingModifier : uint32 {
	kATUIKeyboardMappingModifier_Shift = 0x2000000,
	kATUIKeyboardMappingModifier_Ctrl = 0x4000000,
	kATUIKeyboardMappingModifier_Alt = 0x8000000,
	kATUIKeyboardMappingModifier_Extended = 0x10000000,
	kATUIKeyboardMappingModifier_Cooked = 0x20000000
};

enum ATUIKeyScanCode : uint32 {
	kATUIKeyScanCodeFirst = 0x100,
	kATUIKeyScanCode_Start = 0x100,
	kATUIKeyScanCode_Select = 0x101,
	kATUIKeyScanCode_Option = 0x102,
	kATUIKeyScanCode_Break = 0x103,
	kATUIKeyScanCodeLast = 0x103
};

inline uint32 ATUIPackKeyboardMapping(uint32 scancode, uint32 vk, uint32 modifiers) {
	return scancode + (vk << 9) + modifiers;
}

enum ATUIAccelContext {
	kATUIAccelContext_Global,
	kATUIAccelContext_Display,
	kATUIAccelContext_Debugger,
	kATUIAccelContextCount
};

void ATUIInitDefaultAccelTables();
void ATUILoadAccelTables();
void ATUISaveAccelTables();
const VDAccelTableDefinition *ATUIGetDefaultAccelTables();
VDAccelTableDefinition *ATUIGetAccelTables();

const VDAccelTableEntry *ATUIGetAccelByCommand(ATUIAccelContext context, const char *command);
bool ATUIActivateVirtKeyMapping(uint32 vk, bool alt, bool ctrl, bool shift, bool ext, bool up, ATUIAccelContext context);

#endif
