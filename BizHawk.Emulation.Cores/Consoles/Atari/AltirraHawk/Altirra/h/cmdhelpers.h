//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2015 Avery Lee
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

#ifndef f_CMDHELPERS_H
#define f_CMDHELPERS_H

#include <at/atui/uicommandmanager.h>

class ATSimulator;
extern ATSimulator g_sim;

namespace ATCommands {
	typedef bool (*BoolTestFn)();

	template<BoolTestFn A>
	bool Not() { return !A(); }

	template<BoolTestFn A, BoolTestFn B>
	bool And() { return A() && B(); }

	template<BoolTestFn A, BoolTestFn B>
	bool Or() { return A() || B(); }

	template<BoolTestFn A>
	ATUICmdState CheckedIf() { return A() ? kATUICmdState_Checked : kATUICmdState_None; }

	template<BoolTestFn A>
	ATUICmdState RadioCheckedIf() { return A() ? kATUICmdState_RadioChecked : kATUICmdState_None; }

	template<bool (ATSimulator::*T_Method)() const>
	bool SimTest() {
		return (g_sim.*T_Method)() ? true : false;
	}

	inline ATUICmdState ToRadio(bool checked) {
		return checked ? kATUICmdState_RadioChecked : kATUICmdState_None;
	}

	inline ATUICmdState ToChecked(bool checked) {
		return checked ? kATUICmdState_Checked : kATUICmdState_None;
	}

	bool IsDebuggerEnabled();
	bool IsDebuggerRunning();
}

#endif
