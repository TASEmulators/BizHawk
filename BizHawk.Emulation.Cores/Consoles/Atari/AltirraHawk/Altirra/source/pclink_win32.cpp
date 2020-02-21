//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2011 Avery Lee
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
#include <windows.h>
#include "cio.h"

uint8 ATTranslateWin32ErrorToSIOError(uint32 err) {
	switch(err) {
		case ERROR_FILE_NOT_FOUND:
			return ATCIOSymbols::CIOStatFileNotFound;

		case ERROR_PATH_NOT_FOUND:
			return ATCIOSymbols::CIOStatPathNotFound;

		case ERROR_FILE_EXISTS:
		case ERROR_ALREADY_EXISTS:
			return ATCIOSymbols::CIOStatFileExists;

		case ERROR_DISK_FULL:
			return ATCIOSymbols::CIOStatDiskFull;

		case ERROR_DIR_NOT_EMPTY:
			return ATCIOSymbols::CIOStatDirNotEmpty;

		case ERROR_ACCESS_DENIED:
			return ATCIOSymbols::CIOStatAccessDenied;

		case ERROR_SHARING_VIOLATION:
			return ATCIOSymbols::CIOStatFileLocked;

		default:
			return ATCIOSymbols::CIOStatSystemError;
	}
}
