//	Altirra - Atari 800/800XL/5200 emulator
//	I/O library - blob (binary large object) image definitions
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

#include <stdafx.h>
#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include <at/atcore/checksum.h>
#include <at/atcore/vfs.h>
#include <at/atio/blobimage.h>

class IVDRandomAccessStream;

class ATBlobImage final : public vdrefcounted<IATBlobImage> {
public:
	ATBlobImage(ATImageType type) : mType(type) {}

	void Load(IVDRandomAccessStream& stream);
	void Load(const void *src, uint32 len);

	void *AsInterface(uint32 id) override;

	ATImageType GetImageType() const override;

	uint64 GetChecksum() const override;

	uint32 GetSize() const override { return (uint32)mProgram.size(); }
	const void *GetBuffer() const override { return mProgram.data(); }

private:
	const ATImageType mType;

	vdfastvector<uint8> mProgram;

	mutable uint64 mChecksum = 0;
	mutable bool mbChecksumDirty = true;
};

void *ATBlobImage::AsInterface(uint32 id) {
	switch(id) {
		case IATBlobImage::kTypeID: return static_cast<IATBlobImage *>(this);
	}

	return nullptr;
}

ATImageType ATBlobImage::GetImageType() const {
	return mType;
}

uint64 ATBlobImage::GetChecksum() const {
	if (mbChecksumDirty) {
		mChecksum = ATComputeBlockChecksum(kATBaseChecksum, mProgram.data(), mProgram.size());
		mbChecksumDirty = false;
	}

	return mChecksum;
}

void ATBlobImage::Load(IVDRandomAccessStream& stream) {
	mbChecksumDirty = true;

	auto len = stream.Length();

	if (mType == kATImageType_SaveState) {
		if (len > 0x10000000U)
			throw MyError("Save state too large: %llu bytes", (unsigned long long)len);
	} else if (mType == kATImageType_SAP) {
		if (len > 0x100000U)
			throw MyError("SAP module too large: %llu bytes", (unsigned long long)len);
	} else {
		if (len > 0x10000000U)
			throw MyError("Executable too large: %llu bytes", (unsigned long long)len);
	}

	uint32 len32 = (uint32)len;

	mProgram.resize(len32);

	stream.Seek(0);
	stream.Read(mProgram.data(), (sint32)len32);
}

void ATBlobImage::Load(const void *src, uint32 len) {
	mbChecksumDirty = true;

	const uint8 *src8 = (const uint8 *)src;
	mProgram.assign(src8, src8 + len);
}

///////////////////////////////////////////////////////////////////////////

void ATLoadBlobImage(ATImageType type, const wchar_t *path, IATBlobImage **ppImage) {
	vdrefptr<ATVFSFileView> view;
	
	ATVFSOpenFileView(path, false, ~view);

	return ATLoadBlobImage(type, view->GetStream(), ppImage);
}

void ATLoadBlobImage(ATImageType type, IVDRandomAccessStream& stream, IATBlobImage **ppImage) {
	vdrefptr<ATBlobImage> img(new ATBlobImage(type));

	img->Load(stream);

	*ppImage = img.release();
}

void ATCreateBlobImage(ATImageType type, const void *src, uint32 len, IATBlobImage **ppImage) {
	vdrefptr<ATBlobImage> img(new ATBlobImage(type));

	img->Load(src, len);

	*ppImage = img.release();
}
