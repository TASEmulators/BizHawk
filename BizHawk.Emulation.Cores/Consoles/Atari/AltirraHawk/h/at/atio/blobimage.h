//	Altirra - Atari 800/800XL/5200 emulator
//	I/O library - executable (program) image definitions
//	Copyright (C) 2008-2016 Avery Lee
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

#ifndef f_AT_ATIO_BLOBIMAGE_H
#define f_AT_ATIO_BLOBIMAGE_H

#include <vd2/system/refcount.h>
#include <at/atio/image.h>

class IVDRandomAccessStream;

class VDINTERFACE IATBlobImage : public IATImage {
public:
	enum : uint32 { kTypeID = 'blim' };

	virtual uint64 GetChecksum() const = 0;

	virtual uint32 GetSize() const = 0;
	virtual const void *GetBuffer() const = 0;
};

void ATLoadBlobImage(ATImageType type, const wchar_t *path, IATBlobImage **ppImage);
void ATLoadBlobImage(ATImageType type, IVDRandomAccessStream& stream, IATBlobImage **ppImage);
void ATCreateBlobImage(ATImageType type, const void *src, uint32 len, IATBlobImage **ppImage);

#endif	// f_AT_ATIO_BLOBIMAGE_H
