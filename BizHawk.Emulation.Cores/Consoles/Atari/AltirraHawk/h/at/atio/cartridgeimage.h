//	Altirra - Atari 800/800XL/5200 emulator
//	I/O library - cartridge image
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

#ifndef f_AT_ATIO_CARTRIDGEIMAGE_H
#define f_AT_ATIO_CARTRIDGEIMAGE_H

#include <optional>
#include <vd2/system/refcount.h>
#include <vd2/system/vdstl.h>
#include <at/atio/cartridgetypes.h>
#include <at/atio/image.h>

class IVDRandomAccessStream;

enum ATCartLoadStatus {
	kATCartLoadStatus_Ok,
	kATCartLoadStatus_UnknownMapper
};

struct ATCartLoadContext {
	bool mbIgnoreMapper = false;
	bool mbIgnoreChecksum = false;
	bool mbReturnOnUnknownMapper = false;
	int mCartMapper = -1;
	uint32 mCartSize = 0;
	uint64 mRawImageChecksum = 0;

	ATCartLoadStatus mLoadStatus = kATCartLoadStatus_Ok;

	vdfastvector<uint8> *mpCaptureBuffer = nullptr;
};

class IATCartridgeImage : public IATImage {
public:
	enum : uint32 { kTypeID = 'ctim' };

	virtual ATCartridgeMode GetMode() const = 0;
	virtual const wchar_t *GetPath() const = 0;
	virtual uint32 GetImageSize() const = 0;

	virtual void *GetBuffer() = 0;

	virtual uint64 GetChecksum() = 0;
	virtual std::optional<uint32> GetFileCRC() const = 0;

	virtual bool IsDirty() const = 0;
	virtual void SetClean() = 0;
	virtual void SetDirty() = 0;
};

bool ATLoadCartridgeImage(const wchar_t *path, IATCartridgeImage **ppImage);
bool ATLoadCartridgeImage(const wchar_t *origPath, IVDRandomAccessStream& stream, ATCartLoadContext *loadCtx, IATCartridgeImage **ppImage);
void ATCreateCartridgeImage(ATCartridgeMode mode, IATCartridgeImage **ppImage);
void ATSaveCartridgeImage(IATCartridgeImage *image, const wchar_t *path, bool includeHeader);

#endif
