//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2014 Avery Lee
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

#ifndef AT_ATCORE_CIO_H
#define AT_ATCORE_CIO_H

enum : uint8 {
	kATCIOStat_Success		= 0x01,
	kATCIOStat_SuccessEOF	= 0x03,	// succeeded, but at end of file (undocumented)
	kATCIOStat_Break		= 0x80,	// break key abort
	kATCIOStat_IOCBInUse	= 0x81,	// IOCB in use
	kATCIOStat_UnkDevice	= 0x82,	// unknown device
	kATCIOStat_WriteOnly	= 0x83,	// opened for write only
	kATCIOStat_InvalidCmd	= 0x84,	// invalid command
	kATCIOStat_NotOpen		= 0x85,	// device or file not open
	kATCIOStat_InvalidIOCB	= 0x86,	// invalid IOCB number
	kATCIOStat_ReadOnly		= 0x87,	// opened for read only
	kATCIOStat_EndOfFile	= 0x88,	// end of file reached
	kATCIOStat_TruncRecord	= 0x89,	// record truncated
	kATCIOStat_Timeout		= 0x8A,	// device timeout
	kATCIOStat_NAK			= 0x8B,	// device NAK
	kATCIOStat_SerFrameErr	= 0x8C,	// serial bus framing error
	kATCIOStat_CursorRange	= 0x8D,	// cursor out of range
	kATCIOStat_SerOverrun	= 0x8E,	// serial frame overrun error
	kATCIOStat_SerChecksum	= 0x8F,	// serial checksum error
	kATCIOStat_DeviceError	= 0x90,	// device error reported
	kATCIOStat_BadScrnMode	= 0x91,	// bad screen mode
	kATCIOStat_NotSupported	= 0x92,	// function not supported by handler
	kATCIOStat_OutOfMemory	= 0x93,	// not enough memory
	kATCIOStat_PathNotFound	= 0x96,	// [SDX] path not found
	kATCIOStat_FileExists	= 0x97,	// [SDX] file exists
	kATCIOStat_BadParameter	= 0x9C,	// [SDX] bad parameter
	kATCIOStat_DriveNumErr	= 0xA0,	// disk drive # error
	kATCIOStat_TooManyFiles	= 0xA1,	// too many open disk files
	kATCIOStat_DiskFull		= 0xA2,	// disk full
	kATCIOStat_FatalDiskIO	= 0xA3,	// fatal disk I/O error
	kATCIOStat_IllegalWild	= 0xA3,	// [SDX] Illegal wildcard in name
	kATCIOStat_FileNumDiff	= 0xA4,	// internal file # mismatch
	kATCIOStat_FileNameErr	= 0xA5,	// filename error
	kATCIOStat_PointDLen	= 0xA6,	// point data length error
	kATCIOStat_FileLocked	= 0xA7,	// file locked
	kATCIOStat_DirNotEmpty	= 0xA7,	// [SDX] directory not empty
	kATCIOStat_InvDiskCmd	= 0xA8,	// invalid command for disk
	kATCIOStat_DirFull		= 0xA9,	// directory full (64 files)
	kATCIOStat_FileNotFound	= 0xAA,	// file not found
	kATCIOStat_InvPoint		= 0xAB,	// invalid point
	kATCIOStat_AccessDenied	= 0xB0,	// [SDX] access denied
	kATCIOStat_PathTooLong	= 0xB6,	// [SDX] path too long
	kATCIOStat_SystemError	= 0xFF,	// {SDX] system error

	kATCIOCmd_Open			= 0x03,
	kATCIOCmd_GetRecord		= 0x05,
	kATCIOCmd_GetChars		= 0x07,
	kATCIOCmd_PutRecord		= 0x09,
	kATCIOCmd_PutChars		= 0x0B,
	kATCIOCmd_Close			= 0x0C,
	kATCIOCmd_GetStatus		= 0x0D,
	kATCIOCmd_Special		= 0x0E	// $0E and up is escape
};

#endif	// AT_CIO_H
