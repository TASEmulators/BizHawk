//	Altirra - Atari 800/800XL/5200 emulator
//	Native UI library - synchronous wait support
//	Copyright (C) 2008-2015 Avery Lee
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

#ifndef f_AT_ATNATIVEUI_SYNCWAIT_H
#define f_AT_ATNATIVEUI_SYNCWAIT_H

#include <vd2/system/atomic.h>

class ATNativeUISyncWait {
	ATNativeUISyncWait(const ATNativeUISyncWait&) = delete;
	ATNativeUISyncWait& operator=(const ATNativeUISyncWait&) = delete;

public:
	ATNativeUISyncWait();
	~ATNativeUISyncWait();

	void Wait();
	void Signal();

private:
	void *mhEvent;
	VDAtomicBool mbWaitSatisfied;
};

#endif
