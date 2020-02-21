//	Altirra - Atari 800/800XL/5200 emulator
//	Application base library - C runtime library hooks
//	Copyright (C) 2009-2016 Avery Lee
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
#include <signal.h>
#include <at/atappbase/exceptionfilter.h>

void __cdecl ATOnAbort(int) {
	__try {
		*(volatile char *)0 = 10;
	} __except(ATExceptionFilter(GetExceptionCode(), GetExceptionInformation())) {
	}
}

void __cdecl ATOnPureCall() {
	__try {
		*(volatile char *)0 = 11;
	} __except(ATExceptionFilter(GetExceptionCode(), GetExceptionInformation())) {
	}
}

void __cdecl ATOnInvalidParameter(const wchar_t *, const wchar_t *, const wchar_t *, unsigned int, uintptr_t) {
	__try {
		*(volatile char *)0 = 12;
	} __except(ATExceptionFilter(GetExceptionCode(), GetExceptionInformation())) {
	}
}

void ATInitCRTHooks() {
	#ifdef _MSC_VER
		_CrtSetDbgFlag(_CrtSetDbgFlag(_CRTDBG_REPORT_FLAG) | _CRTDBG_LEAK_CHECK_DF);
	#endif
	
	signal(SIGABRT, ATOnAbort);

	_set_purecall_handler(ATOnPureCall);
	_set_invalid_parameter_handler(ATOnInvalidParameter);
}
