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

#include <stdafx.h>
#include <at/atio/diskfs.h>
#include <at/atio/diskimage.h>

IATDiskFS *ATDiskMountImageDOS2(IATDiskImage *image, bool readOnly);
IATDiskFS *ATDiskMountImageDOS3(IATDiskImage *image, bool readOnly);
IATDiskFS *ATDiskMountImageSDX2(IATDiskImage *image, bool readOnly);

ATDiskFSException::ATDiskFSException(ATDiskFSError error)
	: mErrorCode(error)
{
	switch(error) {
		case kATDiskFSError_InvalidFileName:
			assign("The file name is not allowed by this file system.");
			break;

		case kATDiskFSError_DiskFull:
			assign("There is not enough space on the disk.");
			break;

		case kATDiskFSError_DiskFullFragmented:
			assign("There is not enough contiguous space on the disk.");
			break;

		case kATDiskFSError_DirectoryFull:
			assign("The directory is full and cannot hold any more file entries.");
			break;

		case kATDiskFSError_CorruptedFileSystem:
			assign("The file system is damaged.");
			break;

		case kATDiskFSError_FileExists:
			assign("A file or directory already exists with the same name.");
			break;

		case kATDiskFSError_ReadOnly:
			assign("The file system has been mounted read-only.");
			break;

		case kATDiskFSError_FileTooLarge:
			assign("The file is too large for this file system.");
			break;

		case kATDiskFSError_ReadError:
			assign("An I/O error was encountered while reading from the disk.");
			break;

		case kATDiskFSError_WriteError:
			assign("An I/O error was encountered while writing to the disk.");
			break;

		case kATDiskFSError_CannotReadSparseFile:
			assign("The file cannot be read as it is sparsely allocated.");
			break;

		case kATDiskFSError_DirectoryNotEmpty:
			assign("The directory is not empty.");
			break;

		case kATDiskFSError_UnsupportedCompressionMode:
			assign("The file uses an unsupported compression mode.");
			break;

		case kATDiskFSError_DecompressionError:
			assign("An error was encountered while decompressing the file.");
			break;

		case kATDiskFSError_CRCError:
			assign("A CRC error was encountered while decompressing the file.");
			break;

		case kATDiskFSError_NotSupported:
			assign("The operation is not supported on this type of file system.");
			break;

		case kATDiskFSError_MediaNotSupported:
			assign("The supplied media is not supported on this type of file system.");
			break;
	}
}

IATDiskFS *ATDiskMountImage(IATDiskImage *image, bool readOnly) {
	uint8 secbuf[128];

	if (image->ReadVirtualSector(0, secbuf, 128) < 128)
		return NULL;

	// $80 is the signature byte for SDX SD/DD disks; $40 is for DD 512.
	if (secbuf[7] == 0x80 || secbuf[7] == 0x40)
		return ATDiskMountImageSDX2(image, readOnly);

	// DOS 2 and DOS 3 don't have any overlapping signatures, which is a problem.
	// There are a couple of things that we know, though:
	//
	// - DOS 3 only supports 128 bytes/sector (SD/ED).
	// - The 15th and 16th bytes of the first directory sector (16)
	//   must be 57 A5 for SD and 7F A5 for ED.
	//
	// This could still result in a valid DOS 2 disk being mistaken as DOS 3,
	// unfortunately, so to this we add more criteria which should reduce the
	// chances acceptably low:
	//
	// - The disk image must be exactly 720 or 1040 sectors. We assume that
	//   no one is going to intentionally use DOS 3 for a non-physically sized
	//   image.
	// - The boot sector must load 9 sectors starting at $3200 and begin
	//   execution at $3206, with the first instruction being an LDX #imm
	//   ($A2) instruction.

	static const uint8 kBootSectorCheck[7] = { 0x01, 0x09, 0x00, 0x32, 0x06, 0x32, 0xA2 };

	if (image->GetSectorSize() == 128 || !memcmp(secbuf, kBootSectorCheck, sizeof kBootSectorCheck)) {
		const uint32 sectorCount = image->GetVirtualSectorCount();

		if (sectorCount == 720 || sectorCount == 1040) {
			// read the first catalog sector and check its signature
			if (image->ReadVirtualSector(16 - 1, secbuf, 128) == 128) {
				if (secbuf[14] == (sectorCount == 720 ? 0x57 : 0x7F) &&
					secbuf[15] == 0xA5)
				{
					// okay, we'll take this as sufficient proof that someone actually
					// bothered using DOS 3.
					return ATDiskMountImageDOS3(image, readOnly);
				}
			}
		}
	}
	
	// Check the VTOC - $00 in the first byte is not valid for DOS 2.x/MyDOS; $01 indicates DOS 1.
	// Values above $23 are invalid for MyDOS because they would correspond to disks larger than
	// 64K-1 sectors.
	if (image->ReadVirtualSector(359, secbuf, 128) == 128) {
		if (secbuf[0] == 0 || secbuf[0] > 35)
			return NULL;
	}

	return ATDiskMountImageDOS2(image, readOnly);
}
