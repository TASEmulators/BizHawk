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

#include <stdafx.h>
#include <vd2/system/error.h>
#include <vd2/system/registry.h>
#include <vd2/system/VDString.h>
#include <vd2/Dita/accel.h>
#include "uikeyboard.h"
#include <at/atui/uicommandmanager.h>
#include <windows.h>

extern ATUICommandManager g_ATUICommandMgr;

static bool g_ATUICustomKeyMapEnabled;
static vdfastvector<uint32> g_ATDefaultKeyMap;
static vdfastvector<uint32> g_ATCustomKeyMap;

VDAccelTableDefinition g_ATUIDefaultAccelTables[kATUIAccelContextCount];
VDAccelTableDefinition g_ATUIAccelTables[kATUIAccelContextCount];

void ATUIAddDefaultCharMappings(vdfastvector<uint32>& dst) {
	static const uint32 kDefaultCharMappings[]={
#define ATCHAR_MAPPING(ch, sc) (((uint32)(uint8)(ch) << 9) + (uint32)(sc) + kATUIKeyboardMappingModifier_Cooked)
		ATCHAR_MAPPING((uint8)'l', 0x00 ),
		ATCHAR_MAPPING((uint8)'L', 0x40 ),

		ATCHAR_MAPPING((uint8)'j', 0x01 ),
		ATCHAR_MAPPING((uint8)'J', 0x41 ),

		ATCHAR_MAPPING((uint8)';', 0x02 ),
		ATCHAR_MAPPING((uint8)':', 0x42 ),

		ATCHAR_MAPPING((uint8)'k', 0x05 ),
		ATCHAR_MAPPING((uint8)'K', 0x45 ),

		ATCHAR_MAPPING((uint8)'+', 0x06 ),
		ATCHAR_MAPPING((uint8)'\\', 0x46 ),

		ATCHAR_MAPPING((uint8)'*', 0x07 ),
		ATCHAR_MAPPING((uint8)'^', 0x47 ),

		ATCHAR_MAPPING((uint8)'o', 0x08 ),
		ATCHAR_MAPPING((uint8)'O', 0x48 ),

		ATCHAR_MAPPING((uint8)'p', 0x0A ),
		ATCHAR_MAPPING((uint8)'P', 0x4A ),

		ATCHAR_MAPPING((uint8)'u', 0x0B ),
		ATCHAR_MAPPING((uint8)'U', 0x4B ),

		ATCHAR_MAPPING((uint8)'i', 0x0D ),
		ATCHAR_MAPPING((uint8)'I', 0x4D ),

		ATCHAR_MAPPING((uint8)'-', 0x0E ),
		ATCHAR_MAPPING((uint8)'_', 0x4E ),

		ATCHAR_MAPPING((uint8)'=', 0x0F ),
		ATCHAR_MAPPING((uint8)'|', 0x4F ),

		ATCHAR_MAPPING((uint8)'v', 0x10 ),
		ATCHAR_MAPPING((uint8)'V', 0x50 ),

		ATCHAR_MAPPING((uint8)'c', 0x12 ),
		ATCHAR_MAPPING((uint8)'C', 0x52 ),

		ATCHAR_MAPPING((uint8)'b', 0x15 ),
		ATCHAR_MAPPING((uint8)'B', 0x55 ),

		ATCHAR_MAPPING((uint8)'x', 0x16 ),
		ATCHAR_MAPPING((uint8)'X', 0x56 ),

		ATCHAR_MAPPING((uint8)'z', 0x17 ),
		ATCHAR_MAPPING((uint8)'Z', 0x57 ),

		ATCHAR_MAPPING((uint8)'4', 0x18 ),
		ATCHAR_MAPPING((uint8)'$', 0x58 ),

		ATCHAR_MAPPING((uint8)'3', 0x1A ),
		ATCHAR_MAPPING((uint8)'#', 0x5A ),

		ATCHAR_MAPPING((uint8)'6', 0x1B ),
		ATCHAR_MAPPING((uint8)'&', 0x5B ),

		ATCHAR_MAPPING((uint8)'5', 0x1D ),
		ATCHAR_MAPPING((uint8)'%', 0x5D ),

		ATCHAR_MAPPING((uint8)'2', 0x1E ),
		ATCHAR_MAPPING((uint8)'"', 0x5E ),

		ATCHAR_MAPPING((uint8)'1', 0x1F ),
		ATCHAR_MAPPING((uint8)'!', 0x5F ),

		ATCHAR_MAPPING((uint8)',', 0x20 ),
		ATCHAR_MAPPING((uint8)'[', 0x60 ),

		ATCHAR_MAPPING((uint8)' ', 0x21 ),

		ATCHAR_MAPPING((uint8)'.', 0x22 ),
		ATCHAR_MAPPING((uint8)']', 0x62 ),

		ATCHAR_MAPPING((uint8)'n', 0x23 ),
		ATCHAR_MAPPING((uint8)'N', 0x63 ),

		ATCHAR_MAPPING((uint8)'m', 0x25 ),
		ATCHAR_MAPPING((uint8)'M', 0x65 ),

		ATCHAR_MAPPING((uint8)'/', 0x26 ),
		ATCHAR_MAPPING((uint8)'?', 0x66 ),

		ATCHAR_MAPPING((uint8)'r', 0x28 ),
		ATCHAR_MAPPING((uint8)'R', 0x68 ),

		ATCHAR_MAPPING((uint8)'e', 0x2A ),
		ATCHAR_MAPPING((uint8)'E', 0x6A ),

		ATCHAR_MAPPING((uint8)'y', 0x2B ),
		ATCHAR_MAPPING((uint8)'Y', 0x6B ),

		ATCHAR_MAPPING((uint8)'t', 0x2D ),
		ATCHAR_MAPPING((uint8)'T', 0x6D ),

		ATCHAR_MAPPING((uint8)'w', 0x2E ),
		ATCHAR_MAPPING((uint8)'W', 0x6E ),

		ATCHAR_MAPPING((uint8)'q', 0x2F ),
		ATCHAR_MAPPING((uint8)'Q', 0x6F ),

		ATCHAR_MAPPING((uint8)'9', 0x30 ),
		ATCHAR_MAPPING((uint8)'(', 0x70 ),

		ATCHAR_MAPPING((uint8)'0', 0x32 ),
		ATCHAR_MAPPING((uint8)')', 0x72 ),

		ATCHAR_MAPPING((uint8)'7', 0x33 ),
		ATCHAR_MAPPING((uint8)'\'', 0x73 ),

		ATCHAR_MAPPING((uint8)'8', 0x35 ),
		ATCHAR_MAPPING((uint8)'@', 0x75 ),

		ATCHAR_MAPPING((uint8)'<', 0x36 ),
		ATCHAR_MAPPING((uint8)'>', 0x37 ),

		ATCHAR_MAPPING((uint8)'f', 0x38 ),
		ATCHAR_MAPPING((uint8)'F', 0x78 ),

		ATCHAR_MAPPING((uint8)'h', 0x39 ),
		ATCHAR_MAPPING((uint8)'H', 0x79 ),

		ATCHAR_MAPPING((uint8)'d', 0x3A ),
		ATCHAR_MAPPING((uint8)'D', 0x7A ),

		ATCHAR_MAPPING((uint8)'g', 0x3D ),
		ATCHAR_MAPPING((uint8)'G', 0x7D ),

		ATCHAR_MAPPING((uint8)'s', 0x3E ),
		ATCHAR_MAPPING((uint8)'S', 0x7E ),

		ATCHAR_MAPPING((uint8)'a', 0x3F ),
		ATCHAR_MAPPING((uint8)'A', 0x7F ),

		ATCHAR_MAPPING((uint8)'`', 0x27 ),
		ATCHAR_MAPPING((uint8)'~', 0x67 ),
#undef ATCHAR_MAPPING
	};

	dst.insert(dst.end(), std::begin(kDefaultCharMappings), std::end(kDefaultCharMappings));
}

const vdfastvector<uint32>& ATUIGetCurrentKeyMap() {
	return g_ATUICustomKeyMapEnabled ? g_ATCustomKeyMap : g_ATDefaultKeyMap;
}

bool ATUIGetScanCodeForKeyInput(uint32 keyInputCode, uint32& ch) {
	const auto& keyMap = ATUIGetCurrentKeyMap();

	auto it = std::lower_bound(keyMap.begin(), keyMap.end(), keyInputCode);
	if (it == keyMap.end() || (*it & 0xFFFFFE00) != keyInputCode)
		return false;

	ch = (*it & 0x1FF);
	return true;
}

bool ATUIGetScanCodeForCharacter(char c, uint32& ch) {
	return ATUIGetScanCodeForKeyInput(ATUIPackKeyboardMapping(0, c, kATUIKeyboardMappingModifier_Cooked), ch);
}

bool ATUIGetDefaultScanCodeForCharacter(char c, uint8& ch) {
	switch(c) {
#define ATCHAR_CASE(chval, sc) case chval: ch = sc; return true;
		ATCHAR_CASE('l', 0x00 )
		ATCHAR_CASE('L', 0x40 )

		ATCHAR_CASE('j', 0x01 )
		ATCHAR_CASE('J', 0x41 )

		ATCHAR_CASE(';', 0x02 )
		ATCHAR_CASE(':', 0x42 )

		ATCHAR_CASE('k', 0x05 )
		ATCHAR_CASE('K', 0x45 )

		ATCHAR_CASE('+', 0x06 )
		ATCHAR_CASE('\\', 0x46 )

		ATCHAR_CASE('*', 0x07 )
		ATCHAR_CASE('^', 0x47 )

		ATCHAR_CASE('o', 0x08 )
		ATCHAR_CASE('O', 0x48 )

		ATCHAR_CASE('p', 0x0A )
		ATCHAR_CASE('P', 0x4A )

		ATCHAR_CASE('u', 0x0B )
		ATCHAR_CASE('U', 0x4B )

		ATCHAR_CASE('i', 0x0D )
		ATCHAR_CASE('I', 0x4D )

		ATCHAR_CASE('-', 0x0E )
		ATCHAR_CASE('_', 0x4E )

		ATCHAR_CASE('=', 0x0F )
		ATCHAR_CASE('|', 0x4F )

		ATCHAR_CASE('v', 0x10 )
		ATCHAR_CASE('V', 0x50 )

		ATCHAR_CASE('c', 0x12 )
		ATCHAR_CASE('C', 0x52 )

		ATCHAR_CASE('b', 0x15 )
		ATCHAR_CASE('B', 0x55 )

		ATCHAR_CASE('x', 0x16 )
		ATCHAR_CASE('X', 0x56 )

		ATCHAR_CASE('z', 0x17 )
		ATCHAR_CASE('Z', 0x57 )

		ATCHAR_CASE('4', 0x18 )
		ATCHAR_CASE('$', 0x58 )

		ATCHAR_CASE('3', 0x1A )
		ATCHAR_CASE('#', 0x5A )

		ATCHAR_CASE('6', 0x1B )
		ATCHAR_CASE('&', 0x5B )

		ATCHAR_CASE('5', 0x1D )
		ATCHAR_CASE('%', 0x5D )

		ATCHAR_CASE('2', 0x1E )
		ATCHAR_CASE('"', 0x5E )

		ATCHAR_CASE('1', 0x1F )
		ATCHAR_CASE('!', 0x5F )

		ATCHAR_CASE(',', 0x20 )
		ATCHAR_CASE('[', 0x60 )

		ATCHAR_CASE(' ', 0x21 )

		ATCHAR_CASE('.', 0x22 )
		ATCHAR_CASE(']', 0x62 )

		ATCHAR_CASE('n', 0x23 )
		ATCHAR_CASE('N', 0x63 )

		ATCHAR_CASE('m', 0x25 )
		ATCHAR_CASE('M', 0x65 )

		ATCHAR_CASE('/', 0x26 )
		ATCHAR_CASE('?', 0x66 )

		ATCHAR_CASE('r', 0x28 )
		ATCHAR_CASE('R', 0x68 )

		ATCHAR_CASE('e', 0x2A )
		ATCHAR_CASE('E', 0x6A )

		ATCHAR_CASE('y', 0x2B )
		ATCHAR_CASE('Y', 0x6B )

		ATCHAR_CASE('t', 0x2D )
		ATCHAR_CASE('T', 0x6D )

		ATCHAR_CASE('w', 0x2E )
		ATCHAR_CASE('W', 0x6E )

		ATCHAR_CASE('q', 0x2F )
		ATCHAR_CASE('Q', 0x6F )

		ATCHAR_CASE('9', 0x30 )
		ATCHAR_CASE('(', 0x70 )

		ATCHAR_CASE('0', 0x32 )
		ATCHAR_CASE(')', 0x72 )

		ATCHAR_CASE('7', 0x33 )
		ATCHAR_CASE('\'', 0x73 )

		ATCHAR_CASE('8', 0x35 )
		ATCHAR_CASE('@', 0x75 )

		ATCHAR_CASE('<', 0x36 )
		ATCHAR_CASE('>', 0x37 )

		ATCHAR_CASE('f', 0x38 )
		ATCHAR_CASE('F', 0x78 )

		ATCHAR_CASE('h', 0x39 )
		ATCHAR_CASE('H', 0x79 )

		ATCHAR_CASE('d', 0x3A )
		ATCHAR_CASE('D', 0x7A )

		ATCHAR_CASE('g', 0x3D )
		ATCHAR_CASE('G', 0x7D )

		ATCHAR_CASE('s', 0x3E )
		ATCHAR_CASE('S', 0x7E )

		ATCHAR_CASE('a', 0x3F )
		ATCHAR_CASE('A', 0x7F )

		ATCHAR_CASE('`', 0x27 )
		ATCHAR_CASE('~', 0x67 )
#undef ATCHAR_CASE
	}

	return false;
}

///////////////////////////////////////////////////////////////////////////////

namespace {
	enum : uint32 {
		kShift = kATUIKeyboardMappingModifier_Shift,
		kCtrl = kATUIKeyboardMappingModifier_Ctrl,
		kAlt = kATUIKeyboardMappingModifier_Alt,
		kExtended = kATUIKeyboardMappingModifier_Extended
	};
}

#define VKEYMAP(vkey, mods, sc) (((vkey) << 9) + (mods) + (sc))
#define VKEYMAP_CSALL(vkey, sc) \
	VKEYMAP((vkey), 0, (sc)),	\
	VKEYMAP((vkey), kShift, (sc) + 0x40),	\
	VKEYMAP((vkey), kCtrl, (sc) + 0x80),	\
	VKEYMAP((vkey), kCtrl + kShift, (sc) + 0xC0)

#define VKEYMAP_CSXOR(vkey, sc) \
	VKEYMAP((vkey), 0, (sc)),	\
	VKEYMAP((vkey), kShift, (sc) + 0x40),	\
	VKEYMAP((vkey), kCtrl, (sc) + 0x80)

#define VKEYMAP_C(vkey, sc) \
	VKEYMAP((vkey), kCtrl, (sc) + 0x80)

#define VKEYMAP_C_SALL(vkey, sc) \
	VKEYMAP((vkey), kCtrl, (sc) + 0x80),	\
	VKEYMAP((vkey), kCtrl + kShift, (sc) + 0xC0)

static const uint32 g_ATDefaultVKeyMap[]={
	VKEYMAP_CSALL(VK_TAB,		0x2C),	// Tab
	VKEYMAP_CSALL(VK_BACK,		0x34),	// Backspace
	VKEYMAP_CSALL(VK_RETURN,	0x0C),	// Enter
	VKEYMAP(VK_RETURN, kExtended, 0x0C),	// keypad Enter
	VKEYMAP(VK_RETURN, kExtended + kShift, 0x4C),	// keypad Enter
	VKEYMAP(VK_RETURN, kExtended + kCtrl, 0x8C),	// keypad Enter
	VKEYMAP(VK_RETURN, kExtended + kCtrl + kShift, 0xCC),	// keypad Enter
	VKEYMAP_CSALL(VK_ESCAPE,	0x1C),	// Esc
	VKEYMAP_CSALL(VK_END,		0x27),	// Fuji
	VKEYMAP_CSXOR(VK_F6,		0x11),	// Help
	VKEYMAP(VK_OEM_1, kCtrl,	0x82),	// ;:
	//VKEYMAP(VK_OEM_1, kCtrl + kShift, 0xC2),	// ;:
	VKEYMAP(VK_OEM_PLUS, kCtrl,			0x86),	// +
	//VKEYMAP(VK_OEM_PLUS, kCtrl + kShift,0xC6),	// +
	VKEYMAP(VK_OEM_4, kCtrl,			0xE0),	// [{
	VKEYMAP(VK_OEM_4, kCtrl + kShift,	0xE0),	// [{
	VKEYMAP(VK_OEM_5, kCtrl,			0x9C),	// Ctrl+\| -> Ctrl+Esc
	VKEYMAP(VK_OEM_5, kCtrl + kShift,	0xDC),	// Ctrl+Shift+\| -> Ctrl+Shift+Esc
	VKEYMAP(VK_OEM_6, kCtrl,			0xE2),	// ]}
	VKEYMAP(VK_OEM_6, kCtrl + kShift,	0xE2),	// ]}
	VKEYMAP(VK_OEM_COMMA, kCtrl,		0xA0),	// Ctrl+,
	VKEYMAP(VK_OEM_PERIOD, kCtrl,		0xA2),	// Ctrl+,
	VKEYMAP(VK_OEM_2, kCtrl,			0xA6),	// Ctrl+/
	VKEYMAP(VK_OEM_2, kCtrl + kShift,	0xE6),	// Ctrl+?
	VKEYMAP(VK_HOME,	0,				0x76),	// Home -> Shift+< (Clear)
	VKEYMAP(VK_HOME,	kShift,			0x76),	// Shift+Home -> Shift+< (Clear)
	VKEYMAP(VK_HOME,	kCtrl,			0xB6),	// Ctrl+Home -> Shift+< (Clear)
	VKEYMAP(VK_HOME,	kCtrl + kShift,	0xF6),	// Ctrl+Shift+Home -> Shift+< (Clear)
	VKEYMAP(VK_DELETE,	0,				0xB4),	// Delete -> Ctrl+Backspace
	VKEYMAP(VK_DELETE,	kShift,			0x74),	// Shift+Delete -> Shift+Backspace
	VKEYMAP(VK_DELETE,	kCtrl,			0xF4),	// Shift+Delete -> Ctrl+Shift+Backspace
	VKEYMAP(VK_DELETE,	kCtrl + kShift,	0xF4),	// Ctrl+Shift+Delete -> Ctrl+Shift+Backspace
	VKEYMAP(VK_INSERT,	0,				0xB7),	// Insert -> Ctrl+>
	VKEYMAP(VK_INSERT,	kShift,			0x77),	// Shift+Insert -> Shift+> (Insert)
	VKEYMAP(VK_INSERT,	kCtrl,			0xF7),	// Shift+Insert -> Ctrl+Shift+>
	VKEYMAP(VK_INSERT,	kCtrl + kShift,	0xF7),	// Ctrl+Shift+Insert -> Ctrl+Shift+>
	VKEYMAP(VK_SPACE,	kShift,			0x61),	// Shift+Space
	VKEYMAP(VK_SPACE,	kCtrl,			0xA1),	// Ctrl+Space
	VKEYMAP(VK_SPACE,	kCtrl + kShift,	0xE1),	// Ctrl+Shift+Space

	VKEYMAP_C_SALL('A', 0x3F),
	VKEYMAP_C     ('B', 0x15),
	VKEYMAP_C     ('C', 0x12),
	VKEYMAP_C_SALL('D', 0x3A),
	VKEYMAP_C_SALL('E', 0x2A),
	VKEYMAP_C_SALL('F', 0x38),
	VKEYMAP_C_SALL('G', 0x3D),
	VKEYMAP_C_SALL('H', 0x39),
	VKEYMAP_C_SALL('I', 0x0D),
	VKEYMAP_C     ('J', 0x01),
	VKEYMAP_C     ('K', 0x05),
	VKEYMAP_C     ('L', 0x00),
	VKEYMAP_C_SALL('M', 0x25),
	VKEYMAP_C_SALL('N', 0x23),
	VKEYMAP_C_SALL('O', 0x08),
	VKEYMAP_C_SALL('P', 0x0A),
	VKEYMAP_C_SALL('Q', 0x2F),
	VKEYMAP_C_SALL('R', 0x28),
	VKEYMAP_C_SALL('S', 0x3E),
	VKEYMAP_C_SALL('T', 0x2D),
	VKEYMAP_C_SALL('U', 0x0B),	
	VKEYMAP_C     ('V', 0x10),
	VKEYMAP_C_SALL('W', 0x2E),
	VKEYMAP_C     ('X', 0x16),
	VKEYMAP_C_SALL('Y', 0x2B),
	VKEYMAP_C     ('Z', 0x17),
	VKEYMAP_C_SALL('0', 0x32),
	VKEYMAP_C_SALL('1', 0x1F),
	VKEYMAP_C_SALL('2', 0x1E),
	VKEYMAP_C_SALL('3', 0x1A),
	VKEYMAP_C_SALL('4', 0x18),
	VKEYMAP_C_SALL('5', 0x1D),
	VKEYMAP_C_SALL('6', 0x1B),
	VKEYMAP_C_SALL('7', 0x33),
	VKEYMAP_C_SALL('8', 0x35),
	VKEYMAP_C_SALL('9', 0x30),

	VKEYMAP_CSALL(VK_CAPITAL, 0x3C),
};

static const uint32 g_ATRawVKeyMap[]={
	VKEYMAP_CSXOR('L', 0x00),
	VKEYMAP_CSXOR('J', 0x01),
	VKEYMAP_CSXOR(VK_OEM_1, 0x02),	// ;:
	VKEYMAP_CSXOR('K', 0x05),
	VKEYMAP_CSXOR(VK_OEM_7, 0x06),	// '"
	VKEYMAP_CSXOR(VK_OEM_5, 0x07),	// \|
	VKEYMAP_CSALL('O', 0x08),
	VKEYMAP_CSALL('P', 0x0A),
	VKEYMAP_CSALL('U', 0x0B),
	VKEYMAP_CSALL(VK_RETURN, 0x0C),	// Enter
	VKEYMAP(VK_RETURN, kExtended, 0x0C),	// keypad Enter
	VKEYMAP(VK_RETURN, kExtended + kShift, 0x4C),	// keypad Enter
	VKEYMAP(VK_RETURN, kExtended + kCtrl, 0x8C),	// keypad Enter
	VKEYMAP(VK_RETURN, kExtended + kCtrl + kShift, 0xCC),	// keypad Enter
	VKEYMAP_CSALL('I', 0x0D),
	VKEYMAP_CSALL(VK_OEM_4, 0x0E),	// [{
	VKEYMAP_CSALL(VK_OEM_6, 0x0F),	// ]}

	VKEYMAP_CSXOR('V', 0x10),
	VKEYMAP_CSXOR(VK_F6, 0x11),	// Help
	VKEYMAP_CSXOR('C', 0x12),
	VKEYMAP_CSXOR('B', 0x15),
	VKEYMAP_CSXOR('X', 0x16),
	VKEYMAP_CSXOR('Z', 0x17),
	VKEYMAP_CSALL('4', 0x18),
	VKEYMAP_CSALL('3', 0x1A),
	VKEYMAP_CSALL('6', 0x1B),
	VKEYMAP_CSALL(VK_ESCAPE, 0x1C),	// Esc
	VKEYMAP_CSALL('5', 0x1D),
	VKEYMAP_CSALL('2', 0x1E),
	VKEYMAP_CSALL('1', 0x1F),

	VKEYMAP_CSALL(VK_OEM_COMMA, 0x20),	// ,<
	VKEYMAP_CSALL(VK_OEM_PERIOD, 0x22),	// .>
	VKEYMAP_CSALL('N', 0x23),
	VKEYMAP_CSALL('M', 0x25),
	VKEYMAP_CSALL(VK_OEM_2, 0x26),	// /?
	VKEYMAP_CSALL(VK_END, 0x27),	// Fuji
	VKEYMAP_CSALL('R', 0x28),
	VKEYMAP_CSALL('E', 0x2A),
	VKEYMAP_CSALL('Y', 0x2B),
	VKEYMAP_CSALL(VK_TAB, 0x2C),	// Tab
	VKEYMAP_CSALL('T', 0x2D),
	VKEYMAP_CSALL('W', 0x2E),
	VKEYMAP_CSALL('Q', 0x2F),

	VKEYMAP_CSALL('9', 0x30),
	VKEYMAP_CSALL('0', 0x32),
	VKEYMAP_CSALL('7', 0x33),
	VKEYMAP_CSALL(VK_BACK, 0x34),	// Backspace
	VKEYMAP_CSALL('8', 0x35),
	VKEYMAP_CSALL(VK_OEM_MINUS, 0x36),	// -_
	VKEYMAP_CSALL(VK_OEM_PLUS, 0x37),	// +=
	VKEYMAP_CSALL('F', 0x38),
	VKEYMAP_CSALL('H', 0x39),
	VKEYMAP_CSALL('D', 0x3A),
	VKEYMAP_CSALL(VK_CAPITAL, 0x3C),
	VKEYMAP_CSALL('G', 0x3D),
	VKEYMAP_CSALL('S', 0x3E),
	VKEYMAP_CSALL('A', 0x3F),
};

static const uint32 g_ATDefaultVKeyMapCommonSSO[]={
	VKEYMAP(VK_F2, 0, kATUIKeyScanCode_Start),
	VKEYMAP(VK_F2, kShift, kATUIKeyScanCode_Start),
	VKEYMAP(VK_F2, kCtrl, kATUIKeyScanCode_Start),
	VKEYMAP(VK_F2, kCtrl + kShift, kATUIKeyScanCode_Start),
	VKEYMAP(VK_F3, 0, kATUIKeyScanCode_Select),
	VKEYMAP(VK_F3, kShift, kATUIKeyScanCode_Select),
	VKEYMAP(VK_F3, kCtrl, kATUIKeyScanCode_Select),
	VKEYMAP(VK_F3, kCtrl + kShift, kATUIKeyScanCode_Select),
	VKEYMAP(VK_F4, 0, kATUIKeyScanCode_Option),
	VKEYMAP(VK_F4, kShift, kATUIKeyScanCode_Option),
	VKEYMAP(VK_F4, kCtrl, kATUIKeyScanCode_Option),
	VKEYMAP(VK_F4, kCtrl + kShift, kATUIKeyScanCode_Option),
};

static const uint32 g_ATDefaultVKeyMapCommonBreak[]={
	VKEYMAP(VK_F7, 0, kATUIKeyScanCode_Break),
	VKEYMAP(VK_F7, kShift, kATUIKeyScanCode_Break),
	VKEYMAP(VK_F7, kCtrl, kATUIKeyScanCode_Break),
	VKEYMAP(VK_F7, kCtrl + kShift, kATUIKeyScanCode_Break),
	VKEYMAP(VK_PAUSE, 0, kATUIKeyScanCode_Break),
	VKEYMAP(VK_PAUSE, kShift, kATUIKeyScanCode_Break),
	VKEYMAP(VK_PAUSE, kCtrl, kATUIKeyScanCode_Break),
	VKEYMAP(VK_PAUSE, kCtrl + kShift, kATUIKeyScanCode_Break),
	VKEYMAP(VK_CANCEL, 0, kATUIKeyScanCode_Break),
	VKEYMAP(VK_CANCEL, kShift, kATUIKeyScanCode_Break),
	VKEYMAP(VK_CANCEL, kCtrl, kATUIKeyScanCode_Break),
	VKEYMAP(VK_CANCEL, kCtrl + kShift, kATUIKeyScanCode_Break),
};

static const uint32 g_ATDefaultVKeyMapFKey[]={
	VKEYMAP_CSXOR(VK_F1, 0x03),
	VKEYMAP_CSXOR(VK_F2, 0x04),
	VKEYMAP_CSXOR(VK_F3, 0x13),
	VKEYMAP_CSXOR(VK_F4, 0x14),
};

void ATUIRegisterVirtualKeyMappings(vdfastvector<uint32>& dst, const uint32 *mappings, uint32 n) {
	while(n--) {
		uint32 mapping = *mappings++;

		// Force extended flag for keys that need it.
		switch((mapping >> 9) & 0xFFFF) {
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
				mapping |= kExtended;
				break;
		}

		dst.push_back(mapping);
	}
}

void ATUIGetDefaultKeyMap(const ATUIKeyboardOptions& options, vdfastvector<uint32>& mappings) {
	VDASSERT(options.mLayoutMode != ATUIKeyboardOptions::kLM_Custom);

	mappings.clear();

	switch(options.mLayoutMode) {
		case ATUIKeyboardOptions::kLM_Natural:
		default:
			ATUIRegisterVirtualKeyMappings(mappings, g_ATDefaultVKeyMap, vdcountof(g_ATDefaultVKeyMap));
			break;

		case ATUIKeyboardOptions::kLM_Raw:
			ATUIRegisterVirtualKeyMappings(mappings, g_ATRawVKeyMap, vdcountof(g_ATRawVKeyMap));
			break;
	}

	ATUIRegisterVirtualKeyMappings(mappings, g_ATDefaultVKeyMapCommonBreak, vdcountof(g_ATDefaultVKeyMapCommonBreak));

	if (options.mbEnableFunctionKeys)
		ATUIRegisterVirtualKeyMappings(mappings, g_ATDefaultVKeyMapFKey, vdcountof(g_ATDefaultVKeyMapFKey));
	else
		ATUIRegisterVirtualKeyMappings(mappings, g_ATDefaultVKeyMapCommonSSO, vdcountof(g_ATDefaultVKeyMapCommonSSO));

	// set up arrow keys
	static const uint8 kArrowVKs[4]={ VK_UP, VK_DOWN, VK_LEFT, VK_RIGHT };
	static const uint8 kArrowKCs[4]={ 0x0E, 0x0F, 0x06, 0x07 };

	static const uint8 kCtrlShiftMasks[][4]={
		//              N     S     C     C+S
		/* invert */  { 0x80, 0xC0, 0x00, 0x40 },
		/* auto */    { 0x80, 0x40, 0x80, 0xC0 },
		/* default */ { 0x00, 0x40, 0x80, 0xC0 },
	};

	VDASSERTCT(sizeof(kCtrlShiftMasks)/sizeof(kCtrlShiftMasks[0]) == ATUIKeyboardOptions::kAKMCount);

	const uint8 *csmasks = kCtrlShiftMasks[options.mArrowKeyMode];

	for(int i=0; i<4; ++i) {
		const uint32 baseVK = kArrowVKs[i];
		const uint8 kbcode = kArrowKCs[i];

		for(int j=0; j<4; ++j) {
			uint8 kbcode2 = kbcode | csmasks[j];

			mappings.push_back(ATUIPackKeyboardMapping(kbcode2, baseVK, (j << 25) + kATUIKeyboardMappingModifier_Extended));
			mappings.push_back(ATUIPackKeyboardMapping(kbcode2, baseVK, (j << 25) + kATUIKeyboardMappingModifier_Alt + kATUIKeyboardMappingModifier_Extended));
		}
	}

	ATUIAddDefaultCharMappings(mappings);

	// strip invalid scan codes
	mappings.erase(
		std::remove_if(
			mappings.begin(),
			mappings.end(),
			[](uint32 mapping) -> bool { return !ATIsValidScanCode((uint8)mapping); }),
		mappings.end());

	std::sort(mappings.begin(), mappings.end());
}

void ATUIInitVirtualKeyMap(const ATUIKeyboardOptions& options) {
	if (options.mLayoutMode == ATUIKeyboardOptions::kLM_Custom)
		g_ATDefaultKeyMap = g_ATCustomKeyMap;
	else {
		g_ATDefaultKeyMap.clear();
		g_ATDefaultKeyMap.reserve(2048);
		ATUIGetDefaultKeyMap(options, g_ATDefaultKeyMap);
	}
}

bool ATUIGetScanCodeForVirtualKey(uint32 virtKey, bool alt, bool ctrl, bool shift, bool extended, uint32& scanCode) {
	if (virtKey >= 0x10000)
		return false;

	uint32 baseCode = virtKey << 9;

	if (alt)
		baseCode += kAlt;

	if (ctrl)
		baseCode += kCtrl;

	if (shift)
		baseCode += kShift;

	if (extended)
		baseCode += kExtended;

	return ATUIGetScanCodeForKeyInput(baseCode, scanCode);
}

void ATUIGetCustomKeyMap(vdfastvector<uint32>& mappings) {
	mappings = g_ATCustomKeyMap;
}

void ATUISetCustomKeyMap(const uint32 *mappings, size_t n) {
	g_ATCustomKeyMap.clear();
	g_ATCustomKeyMap.assign(mappings, mappings + n);

	std::sort(g_ATCustomKeyMap.begin(), g_ATCustomKeyMap.end());
}

bool ATIsValidScanCode(uint32 c) {
	// check for our special scan codes
	if (c >= 0x100)
		return c <= kATUIKeyScanCodeLast;

	// six values are never produced by the matrix
	switch(c & 0x3F) {
		case 0x09:
		case 0x19:
		case 0x24:
		case 0x29:
		case 0x31:
		case 0x3B:
			return false;

		default:
			return true;
	}
}

const wchar_t *ATUIGetNameForKeyCode(uint32 c) {
	switch(c) {
	case kATUIKeyScanCode_Start: return L"Start";
	case kATUIKeyScanCode_Select: return L"Select";
	case kATUIKeyScanCode_Option: return L"Option";
	case kATUIKeyScanCode_Break: return L"Break";

	case 0x3F: return L"A";
	case 0x15: return L"B";
	case 0x12: return L"C";
	case 0x3A: return L"D";
	case 0x2A: return L"E";
	case 0x38: return L"F";
	case 0x3D: return L"G";
	case 0x39: return L"H";
	case 0x0D: return L"I";
	case 0x01: return L"J";
	case 0x05: return L"K";
	case 0x00: return L"L";
	case 0x25: return L"M";
	case 0x23: return L"N";
	case 0x08: return L"O";
	case 0x0A: return L"P";
	case 0x2F: return L"Q";
	case 0x28: return L"R";
	case 0x3E: return L"S";
	case 0x2D: return L"T";
	case 0x0B: return L"U";
	case 0x10: return L"V";
	case 0x2E: return L"W";
	case 0x16: return L"X";
	case 0x2B: return L"Y";
	case 0x17: return L"Z";
	case 0x1F: return L"1";
	case 0x1E: return L"2";
	case 0x1A: return L"3";
	case 0x18: return L"4";
	case 0x1D: return L"5";
	case 0x1B: return L"6";
	case 0x33: return L"7";
	case 0x35: return L"8";
	case 0x30: return L"9";
	case 0x32: return L"0";
	case 0x03: return L"F1";
	case 0x04: return L"F2";
	case 0x13: return L"F3";
	case 0x14: return L"F4";
	case 0x22: return L".";
	case 0x20: return L",";
	case 0x02: return L";";
	case 0x06: return L"+";
	case 0x07: return L"*";
	case 0x0E: return L"-";
	case 0x0F: return L"=";
	case 0x26: return L"/";
	case 0x36: return L"<";
	case 0x37: return L">";
	case 0x21: return L"Space";
	case 0x0C: return L"Enter";
	case 0x34: return L"Backspace";
	case 0x1C: return L"Esc";
	case 0x2C: return L"Tab";
	case 0x27: return L"Invert (Fuji)";
	case 0x11: return L"Help";
	case 0x3C: return L"Caps";
	case 0x7F: return L"Shift+A";
	case 0x55: return L"Shift+B";
	case 0x52: return L"Shift+C";
	case 0x7A: return L"Shift+D";
	case 0x6A: return L"Shift+E";
	case 0x78: return L"Shift+F";
	case 0x7D: return L"Shift+G";
	case 0x79: return L"Shift+H";
	case 0x4D: return L"Shift+I";
	case 0x41: return L"Shift+J";
	case 0x45: return L"Shift+K";
	case 0x40: return L"Shift+L";
	case 0x65: return L"Shift+M";
	case 0x63: return L"Shift+N";
	case 0x48: return L"Shift+O";
	case 0x4A: return L"Shift+P";
	case 0x6F: return L"Shift+Q";
	case 0x68: return L"Shift+R";
	case 0x7E: return L"Shift+S";
	case 0x6D: return L"Shift+T";
	case 0x4B: return L"Shift+U";
	case 0x50: return L"Shift+V";
	case 0x6E: return L"Shift+W";
	case 0x56: return L"Shift+X";
	case 0x6B: return L"Shift+Y";
	case 0x57: return L"Shift+Z";
	case 0x5F: return L"Shift+1 (!)";
	case 0x5E: return L"Shift+2 (\")";
	case 0x5A: return L"Shift+3 (#)";
	case 0x58: return L"Shift+4 ($)";
	case 0x5D: return L"Shift+5 (%)";
	case 0x5B: return L"Shift+6 (&)";
	case 0x73: return L"Shift+7 (')";
	case 0x75: return L"Shift+8 (@)";
	case 0x70: return L"Shift+9 (()";
	case 0x72: return L"Shift+0 ())";
	case 0x43: return L"Shift+F1";
	case 0x44: return L"Shift+F2";
	case 0x53: return L"Shift+F3";
	case 0x54: return L"Shift+F4";
	case 0x60: return L"Shift+, ([)";
	case 0x62: return L"Shift+. (])";
	case 0x42: return L"Shift+; (:)";
	case 0x46: return L"Shift++ (\\)";
	case 0x47: return L"Shift+* (^)";
	case 0x4E: return L"Shift+- (_)";
	case 0x4F: return L"Shift+= (|)";
	case 0x66: return L"Shift+/ (?)";
	case 0x76: return L"Shift+< (Clear)";
	case 0x77: return L"Shift+> (Insert Line)";
	case 0x61: return L"Shift+Space";
	case 0x4C: return L"Shift+Enter";
	case 0x74: return L"Shift+Back (Delete Line)";
	case 0x5C: return L"Shift+Esc";
	case 0x6C: return L"Shift+Tab";
	case 0x67: return L"Shift+Invert (Fuji)";
	case 0x51: return L"Shift+Help";
	case 0x7C: return L"Shift+Caps";
	case 0xBF: return L"Ctrl+A";
	case 0x95: return L"Ctrl+B";
	case 0x92: return L"Ctrl+C";
	case 0xBA: return L"Ctrl+D";
	case 0xAA: return L"Ctrl+E";
	case 0xB8: return L"Ctrl+F";
	case 0xBD: return L"Ctrl+G";
	case 0xB9: return L"Ctrl+H";
	case 0x8D: return L"Ctrl+I";
	case 0x81: return L"Ctrl+J";
	case 0x85: return L"Ctrl+K";
	case 0x80: return L"Ctrl+L";
	case 0xA5: return L"Ctrl+M";
	case 0xA3: return L"Ctrl+N";
	case 0x88: return L"Ctrl+O";
	case 0x8A: return L"Ctrl+P";
	case 0xAF: return L"Ctrl+Q";
	case 0xA8: return L"Ctrl+R";
	case 0xBE: return L"Ctrl+S";
	case 0xAD: return L"Ctrl+T";
	case 0x8B: return L"Ctrl+U";
	case 0x90: return L"Ctrl+V";
	case 0xAE: return L"Ctrl+W";
	case 0x96: return L"Ctrl+X";
	case 0xAB: return L"Ctrl+Y";
	case 0x97: return L"Ctrl+Z";
	case 0x9F: return L"Ctrl+1";
	case 0x9E: return L"Ctrl+2";
	case 0x9A: return L"Ctrl+3";
	case 0x98: return L"Ctrl+4";
	case 0x9D: return L"Ctrl+5";
	case 0x9B: return L"Ctrl+6";
	case 0xB3: return L"Ctrl+7";
	case 0xB5: return L"Ctrl+8";
	case 0xB0: return L"Ctrl+9";
	case 0xB2: return L"Ctrl+0";
	case 0x83: return L"Ctrl+F1";
	case 0x84: return L"Ctrl+F2";
	case 0x93: return L"Ctrl+F3";
	case 0x94: return L"Ctrl+F4";
	case 0xA0: return L"Ctrl+,";
	case 0xA2: return L"Ctrl+.";
	case 0x82: return L"Ctrl+;";
	case 0x86: return L"Ctrl++ (Left)";
	case 0x87: return L"Ctrl+* (Right)";
	case 0x8E: return L"Ctrl+- (Up)";
	case 0x8F: return L"Ctrl+= (Down)";
	case 0xA6: return L"Ctrl+/";
	case 0xB6: return L"Ctrl+<";
	case 0xB7: return L"Ctrl+> (Insert Char)";
	case 0xA1: return L"Ctrl+Space";
	case 0x8C: return L"Ctrl+Enter";
	case 0xB4: return L"Ctrl+Back (Delete Char)";
	case 0x9C: return L"Ctrl+Esc";
	case 0xAC: return L"Ctrl+Tab";
	case 0xA7: return L"Ctrl+Invert (Fuji)";
	case 0x91: return L"Ctrl+Help";
	case 0xBC: return L"Ctrl+Caps";
	case 0xFF: return L"Ctrl+Shift+A";
	case 0xD5: return L"Ctrl+Shift+B";
	case 0xD2: return L"Ctrl+Shift+C";
	case 0xFA: return L"Ctrl+Shift+D";
	case 0xEA: return L"Ctrl+Shift+E";
	case 0xF8: return L"Ctrl+Shift+F";
	case 0xFD: return L"Ctrl+Shift+G";
	case 0xF9: return L"Ctrl+Shift+H";
	case 0xCD: return L"Ctrl+Shift+I";
	case 0xC1: return L"Ctrl+Shift+J";
	case 0xC5: return L"Ctrl+Shift+K";
	case 0xC0: return L"Ctrl+Shift+L";
	case 0xE5: return L"Ctrl+Shift+M";
	case 0xE3: return L"Ctrl+Shift+N";
	case 0xC8: return L"Ctrl+Shift+O";
	case 0xCA: return L"Ctrl+Shift+P";
	case 0xEF: return L"Ctrl+Shift+Q";
	case 0xE8: return L"Ctrl+Shift+R";
	case 0xFE: return L"Ctrl+Shift+S";
	case 0xED: return L"Ctrl+Shift+T";
	case 0xCB: return L"Ctrl+Shift+U";
	case 0xD0: return L"Ctrl+Shift+V";
	case 0xEE: return L"Ctrl+Shift+W";
	case 0xD6: return L"Ctrl+Shift+X";
	case 0xEB: return L"Ctrl+Shift+Y";
	case 0xD7: return L"Ctrl+Shift+Z";
	case 0xDF: return L"Ctrl+Shift+1";
	case 0xDE: return L"Ctrl+Shift+2";
	case 0xDA: return L"Ctrl+Shift+3";
	case 0xD8: return L"Ctrl+Shift+4";
	case 0xDD: return L"Ctrl+Shift+5";
	case 0xDB: return L"Ctrl+Shift+6";
	case 0xF3: return L"Ctrl+Shift+7";
	case 0xF5: return L"Ctrl+Shift+8";
	case 0xF0: return L"Ctrl+Shift+9";
	case 0xF2: return L"Ctrl+Shift+0";
	case 0xC3: return L"Ctrl+Shift+F1";
	case 0xC4: return L"Ctrl+Shift+F2";
	case 0xD3: return L"Ctrl+Shift+F3";
	case 0xD4: return L"Ctrl+Shift+F4";
	case 0xE0: return L"Ctrl+Shift+,";
	case 0xE2: return L"Ctrl+Shift+.";
	case 0xC2: return L"Ctrl+Shift+;";
	case 0xC6: return L"Ctrl+Shift++";
	case 0xC7: return L"Ctrl+Shift+*";
	case 0xCE: return L"Ctrl+Shift+-";
	case 0xCF: return L"Ctrl+Shift+=";
	case 0xE6: return L"Ctrl+Shift+/";
	case 0xF6: return L"Ctrl+Shift+<";
	case 0xF7: return L"Ctrl+Shift+>";
	case 0xE1: return L"Ctrl+Shift+Space";
	case 0xCC: return L"Ctrl+Shift+Enter";
	case 0xF4: return L"Ctrl+Shift+Backspace";
	case 0xDC: return L"Ctrl+Shift+Esc";
	case 0xEC: return L"Ctrl+Shift+Tab";
	case 0xE7: return L"Ctrl+Shift+Invert (Fuji)";
	case 0xD1: return L"Ctrl+Help";
	case 0xFC: return L"Ctrl+Shift+Caps";
	default:
		return nullptr;
	}
}

///////////////////////////////////////////////////////////////////////////

namespace {
	const auto CTRL = VDUIAccelerator::kModCtrl;
	const auto SHIFT = VDUIAccelerator::kModShift;
	const auto ALT = VDUIAccelerator::kModAlt;
	const auto UP = VDUIAccelerator::kModUp;
	const auto EXT = VDUIAccelerator::kModExtended;

	const VDAccelTableEntry kATDefaultAccelTableDisplay[]={
		{ "System.PulseWarpOn", 0, { VK_F1, 0 } },
		{ "System.PulseWarpOff", 0, { VK_F1, UP } },
		{ "Input.CycleQuickMaps", 0, { VK_F1, SHIFT } },
		{ "Console.HoldKeys", 0, { VK_F1, ALT } },
		{ "System.WarmReset", 0, { VK_F5, 0 } },
		{ "System.ColdReset", 0, { VK_F5, SHIFT } },
		{ "Video.ToggleStandardNTSCPAL", 0, { VK_F7, CTRL } },
		{ "View.NextANTICVisMode", 0, { VK_F8, SHIFT } },
		{ "View.NextGTIAVisMode", 0, { VK_F8, CTRL } },
		{ "System.TogglePause", 0, { VK_F9, 0 } },
		{ "Input.CaptureMouse", 0, { VK_F12, 0 } },
		{ "View.ToggleFullScreen", 0, { VK_RETURN, ALT } },
		{ "System.ToggleSlowMotion", 0, { VK_BACK, ALT } },
		{ "Audio.ToggleChannel1", 0, { '1', CTRL+ALT } },
		{ "Audio.ToggleChannel2", 0, { '2', CTRL+ALT } },
		{ "Audio.ToggleChannel3", 0, { '3', CTRL+ALT } },
		{ "Audio.ToggleChannel4", 0, { '4', CTRL+ALT } },
		{ "Edit.PasteText", 0, { 'V', ALT+SHIFT } },
		{ "Edit.SaveFrame", 0, { VK_F10, ALT } },
		{ "Edit.CopyText", 0, { 'C', ALT+SHIFT } },
		{ "Edit.CopyFrame", 0, { 'M', ALT+SHIFT } },
	};

	const VDAccelTableEntry kATDefaultAccelTableGlobal[]={
		{ "Cheat.CheatDialog", 0, { 'H', ALT+SHIFT } },
		{ "File.BootImage", 0, { 'B', ALT } },
		{ "File.OpenImage", 0, { 'O', ALT } },
		{ "Debug.OpenSourceFile", 0, { 'O', ALT+SHIFT } },
		{ "Disk.DrivesDialog", 0, { 'D', ALT } },
		{ "Pane.Display", 0, { '1', ALT } },
		{ "Pane.Console", 0, { '2', ALT } },
		{ "Pane.Registers", 0, { '3', ALT } },
		{ "Pane.Disassembly", 0, { '4', ALT } },
		{ "Pane.CallStack", 0, { '5', ALT } },
		{ "Pane.History", 0, { '6', ALT } },
		{ "Pane.Memory1", 0, { '7', ALT } },
		{ "Pane.PrinterOutput", 0, { '8', ALT } },
		{ "Pane.ProfileView", 0, { '0', ALT+SHIFT } },
		{ "System.Configure", 0, { 'S', ALT } },

		{ "Debug.RunStop", 0, { VK_F8, 0 } },
		{ "Debug.StepInto", 0, { VK_F11, 0 } },
		{ "Debug.StepOver", 0, { VK_F10, 0 } },
		{ "Debug.StepOut", 0, { VK_F11, SHIFT } },
		{ "Debug.Break", 0, { VK_CANCEL, CTRL + EXT } },
	};

	const VDAccelTableEntry kATDefaultAccelTableDebugger[]={
		{ "Debug.Run", 0, { VK_F5, 0 } },
		{ "Debug.ToggleBreakpoint", 0, { VK_F9, 0 } },
	};
}

void ATUIInitDefaultAccelTables() {
	g_ATUIDefaultAccelTables[kATUIAccelContext_Global].AddRange(kATDefaultAccelTableGlobal, vdcountof(kATDefaultAccelTableGlobal));
	g_ATUIDefaultAccelTables[kATUIAccelContext_Display].AddRange(kATDefaultAccelTableDisplay, vdcountof(kATDefaultAccelTableDisplay));
	g_ATUIDefaultAccelTables[kATUIAccelContext_Debugger].AddRange(kATDefaultAccelTableDebugger, vdcountof(kATDefaultAccelTableDebugger));

	for(int i=0; i<kATUIAccelContextCount; ++i)
		g_ATUIAccelTables[i] = g_ATUIDefaultAccelTables[i];
}

void ATUILoadAccelTables() {
	vdfastvector<VDAccelToCommandEntry> commands;

	g_ATUICommandMgr.ListCommands(commands);

	VDStringA keyName;

	for(int i=0; i<kATUIAccelContextCount; ++i) {
		keyName.sprintf("AccelTables2\\%d", i);

		VDRegistryKey key(keyName.c_str(), false, false);

		if (key.isReady()) {
			try {
				g_ATUIAccelTables[i].Load(key, commands.data(), (uint32)commands.size());
			} catch(const MyError&) {
				// eat load error
			}
		}
	}
}

void ATUISaveAccelTables() {
	VDStringA keyName;

	for(int i=0; i<kATUIAccelContextCount; ++i) {
		keyName.sprintf("AccelTables2\\%d", i);

		VDRegistryKey key(keyName.c_str());
		g_ATUIAccelTables[i].Save(key);
	}
}

const VDAccelTableDefinition *ATUIGetDefaultAccelTables() {
	return g_ATUIDefaultAccelTables;
}

VDAccelTableDefinition *ATUIGetAccelTables() {
	return g_ATUIAccelTables;
}

const VDAccelTableEntry *ATUIGetAccelByCommand(ATUIAccelContext context, const char *command) {
	for(;;) {
		const VDAccelTableDefinition& table = g_ATUIAccelTables[context];
		uint32 numAccels = table.GetSize();
		for(uint32 i=0; i<numAccels; ++i) {
			const VDAccelTableEntry& entry = table[i];

			if (!strcmp(entry.mpCommand, command))
				return &entry;
		}

		if (context == kATUIAccelContext_Global)
			break;

		context = kATUIAccelContext_Global;
	}

	return NULL;
}

bool ATUIActivateVirtKeyMapping(uint32 vk, bool alt, bool ctrl, bool shift, bool ext, bool up, ATUIAccelContext context) {
	uint8 flags = 0;

	if (ctrl)
		flags += VDUIAccelerator::kModCtrl;

	if (shift)
		flags += VDUIAccelerator::kModShift;

	if (alt)
		flags += VDUIAccelerator::kModAlt;

	if (ext)
		flags += VDUIAccelerator::kModExtended;

	if (up)
		flags += VDUIAccelerator::kModUp;

	for(;;) {
		VDUIAccelerator accel;
		accel.mVirtKey = vk;
		accel.mModifiers = flags;

		const VDAccelTableDefinition& table = g_ATUIAccelTables[context];
		const VDAccelTableEntry *entry = table(accel);

		if (entry) {
			g_ATUICommandMgr.ExecuteCommand(entry->mpCommand);
			return true;
		}

		if (up) {
			// It looks like we're doing an up and might have only a down mapping.
			// If we don't find a direct up mapping by the end, we should eat this
			// keystroke anyway to prevent other systems from seeing just the up.

			accel.mModifiers -= VDUIAccelerator::kModUp;

			if (table(accel))
				return true;
		}

		if (context == kATUIAccelContext_Global)
			return false;

		context = kATUIAccelContext_Global;
	}
}
