//	Altirra - Atari 800/800XL emulator
//	Copyright (C) 2009 Avery Lee
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

#ifndef AT_CIO_H
#define AT_CIO_H

namespace ATCIOSymbols {
	enum {
		CIOStatSuccess		= 0x01,
		CIOStatSuccessEOF	= 0x03,	// succeeded, but at end of file (undocumented)
		CIOStatBreak		= 0x80,	// break key abort
		CIOStatIOCBInUse	= 0x81,	// IOCB in use
		CIOStatUnkDevice	= 0x82,	// unknown device
		CIOStatWriteOnly	= 0x83,	// opened for write only
		CIOStatInvalidCmd	= 0x84,	// invalid command
		CIOStatNotOpen		= 0x85,	// device or file not open
		CIOStatInvalidIOCB	= 0x86,	// invalid IOCB number
		CIOStatReadOnly		= 0x87,	// opened for read only
		CIOStatEndOfFile	= 0x88,	// end of file reached
		CIOStatTruncRecord	= 0x89,	// record truncated
		CIOStatTimeout		= 0x8A,	// device timeout
		CIOStatNAK			= 0x8B,	// device NAK
		CIOStatSerFrameErr	= 0x8C,	// serial bus framing error
		CIOStatCursorRange	= 0x8D,	// cursor out of range
		CIOStatSerOverrun	= 0x8E,	// serial frame overrun error
		CIOStatSerChecksum	= 0x8F,	// serial checksum error
		CIOStatDeviceDone	= 0x90,	// device done error
		CIOStatBadScrnMode	= 0x91,	// bad screen mode
		CIOStatNotSupported	= 0x92,	// function not supported by handler
		CIOStatOutOfMemory	= 0x93,	// not enough memory
		CIOStatPathNotFound	= 0x96,	// [SDX] path not found
		CIOStatFileExists	= 0x97,	// [SDX] file exists
		CIOStatBadParameter	= 0x9C,	// [SDX] bad parameter
		CIOStatDriveNumErr	= 0xA0,	// disk drive # error
		CIOStatTooManyFiles	= 0xA1,	// too many open disk files
		CIOStatDiskFull		= 0xA2,	// disk full
		CIOStatFatalDiskIO	= 0xA3,	// fatal disk I/O error
		CIOStatIllegalWild	= 0xA3,	// [SDX] Illegal wildcard in name
		CIOStatFileNumDiff	= 0xA4,	// internal file # mismatch
		CIOStatFileNameErr	= 0xA5,	// filename error
		CIOStatPointDLen	= 0xA6,	// point data length error
		CIOStatFileLocked	= 0xA7,	// file locked
		CIOStatDirNotEmpty	= 0xA7,	// [SDX] directory not empty
		CIOStatInvDiskCmd	= 0xA8,	// invalid command for disk
		CIOStatDirFull		= 0xA9,	// directory full (64 files)
		CIOStatFileNotFound	= 0xAA,	// file not found
		CIOStatInvPoint		= 0xAB,	// invalid point
		CIOStatAccessDenied	= 0xB0,	// [SDX] access denied
		CIOStatPathTooLong	= 0xB6,	// [SDX] path too long
		CIOStatSystemError	= 0xFF,	// {SDX] system error

		CIOCmdOpen			= 0x03,
		CIOCmdGetRecord		= 0x05,
		CIOCmdGetChars		= 0x07,
		CIOCmdPutRecord		= 0x09,
		CIOCmdPutChars		= 0x0B,
		CIOCmdClose			= 0x0C,
		CIOCmdGetStatus		= 0x0D,
		CIOCmdSpecial		= 0x0E	// $0E and up is escape
	};
}

#endif	// AT_CIO_H
