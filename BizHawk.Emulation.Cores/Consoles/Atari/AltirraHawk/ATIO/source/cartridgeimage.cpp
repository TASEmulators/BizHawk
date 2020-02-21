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

#include <stdafx.h>
#include <vd2/system/binary.h>
#include <vd2/system/file.h>
#include <vd2/system/zip.h>
#include <at/atcore/checksum.h>
#include <at/atcore/vfs.h>
#include <at/atio/cartridgeimage.h>

namespace {
	void DeinterleaveAtraxSDX64K(uint8 *p) {
		// At -> Epr
		// D0 -> D4
		// D1 -> D0
		// D2 -> D5
		// D3 -> D1
		// D4 -> D7
		// D5 -> D6
		// D6 -> D3
		// D7 -> D2
		//
		// A0 -> A6
		// A1 -> A7
		// A2 -> A12
		// A3 -> A15
		// A4 -> A14
		// A5 -> A13
		// A6 -> A8
		// A7 -> A5
		// A8 -> A4
		// A9 -> A3
		// A10 -> A0
		// A11 -> A1
		// A12 -> A2
		// A13 -> A9
		// A14 -> A11
		// A15 -> A10
		// A16 -> A16

		vdblock<uint8> src(65536);
		memcpy(src.data(), p, 65536);

		uint8 dtab[256];
		uint16 alotab[256];
		uint16 ahitab[256];

		for(int i=0; i<256; ++i) {
			uint8 d = 0;
			uint16 alo = 0;
			uint16 ahi = 0;

			if (i & 0x01) { d += 0x02; alo += 0x0040; ahi += 0x0010; }
			if (i & 0x02) { d += 0x08; alo += 0x0080; ahi += 0x0008; }
			if (i & 0x04) { d += 0x80; alo += 0x1000; ahi += 0x0001; }
			if (i & 0x08) { d += 0x40; alo += 0x8000; ahi += 0x0002; }
			if (i & 0x10) { d += 0x01; alo += 0x4000; ahi += 0x0004; }
			if (i & 0x20) { d += 0x04; alo += 0x2000; ahi += 0x0200; }
			if (i & 0x40) { d += 0x20; alo += 0x0100; ahi += 0x0800; }
			if (i & 0x80) { d += 0x10; alo += 0x0020; ahi += 0x0400; }

			dtab[i] = d;
			alotab[i] = alo;
			ahitab[i] = ahi;
		}

		for(int i=0; i<65536; ++i) {
			p[i] = dtab[src[alotab[i & 0xff] + ahitab[i >> 8]]];
		}
	}

	// Reference: Atari800 4.0.0 cart.txt
	void DeinterleaveAtrax128K(uint8 *p) {
		// At -> Epr
		// D0 -> D5
		// D1 -> D6
		// D2 -> D2
		// D3 -> D4
		// D4 -> D0
		// D5 -> D1
		// D6 -> D7
		// D7 -> D3
		//
		// A0 -> A5
		// A1 -> A6
		// A2 -> A7
		// A3 -> A12
		// A4 -> A0
		// A5 -> A1
		// A6 -> A2
		// A7 -> A3
		// A8 -> A4
		// A9 -> A8
		// A10 -> A10
		// A11 -> A11
		// A12 -> A9
		// A13 -> A13
		// A14 -> A14
		// A15 -> A15
		// A16 -> A16

		uint8 dtab[256];
		uint16 alotab[256];
		uint16 ahitab[32];

		for(int i=0; i<256; ++i) {
			uint8 d = 0;
			uint16 alo = 0;

			if (i & 0x01) { d += 0x10; alo += 0x0020; }
			if (i & 0x02) { d += 0x20; alo += 0x0040; }
			if (i & 0x04) { d += 0x04; alo += 0x0080; }
			if (i & 0x08) { d += 0x80; alo += 0x1000; }
			if (i & 0x10) { d += 0x08; alo += 0x0001; }
			if (i & 0x20) { d += 0x01; alo += 0x0002; }
			if (i & 0x40) { d += 0x02; alo += 0x0004; }
			if (i & 0x80) { d += 0x40; alo += 0x0008; }

			dtab[i] = d;
			alotab[i] = alo;
		}

		for(int i=0; i<32; ++i) {
			uint16 ahi = 0;

			if (i & 0x01) ahi += 0x0010;
			if (i & 0x02) ahi += 0x0100;
			if (i & 0x04) ahi += 0x0400;
			if (i & 0x08) ahi += 0x0800;
			if (i & 0x10) ahi += 0x0200;

			ahitab[i] = ahi;
		}

		vdblock<uint8> src(131072);

		memcpy(src.data(), p, 131072);

		for(int i=0; i<131072; ++i) {
			const uint32 rawAddr = alotab[i & 0xff] + ahitab[(i >> 8) & 0x1f] + (i & 0x1e000);
			const uint8 rawData = src[rawAddr];

			p[i] = dtab[rawData];
		}
	}

#if defined(VD_CPU_X86) || defined(VD_CPU_X64)
	uint32 ComputeByteSum32(const uint8 *src, size_t len) {
		uint32 sum = 0;

		if (len >= 1024) {
			uint32 align = ~(uint32)(uintptr)src & 15;
			len -= align;

			while(align--)
				sum += *src++;

			uint32 blocks = len >> 6;
			len &= 0x3f;

			__m128i zero = _mm_setzero_si128();
			__m128i acc0 = zero;
			__m128i acc1 = zero;
			while(blocks--) {
				__m128i x0 = _mm_load_si128((const __m128i *)(src +  0));
				__m128i x1 = _mm_load_si128((const __m128i *)(src + 16));
				__m128i x2 = _mm_load_si128((const __m128i *)(src + 32));
				__m128i x3 = _mm_load_si128((const __m128i *)(src + 48));
				src += 64;

				acc0 = _mm_add_epi32(acc0, _mm_sad_epu8(x0, zero));
				acc1 = _mm_add_epi32(acc1, _mm_sad_epu8(x1, zero));
				acc0 = _mm_add_epi32(acc0, _mm_sad_epu8(x2, zero));
				acc1 = _mm_add_epi32(acc1, _mm_sad_epu8(x3, zero));
			}

			__m128i acc = _mm_add_epi32(acc0, acc1);
			__m128i acchi = _mm_castps_si128(_mm_movehl_ps(_mm_undefined_ps(), _mm_castsi128_ps(acc)));

			sum += (uint32)_mm_cvtsi128_si32(_mm_add_epi32(acc, acchi));
		}

		while(len--)
			sum += *src++;

		return sum;
	}
#else
	uint32 ComputeByteSum32(const uint8 *src, size_t len) {
		uint32 sum = 0;

		while(len--)
			sum += *src++;

		return sum;
	}
#endif
}

class ATCartridgeImage final : public vdrefcounted<IATCartridgeImage> {
public:
	void Init(ATCartridgeMode mode);
	bool Load(const wchar_t *path, IVDRandomAccessStream& stream, ATCartLoadContext *loadCtx);

	void *AsInterface(uint32 id) override;

	ATImageType GetImageType() const override { return kATImageType_Cartridge; }

	ATCartridgeMode GetMode() const override { return mCartMode; }
	const wchar_t *GetPath() const override { return mImagePath.empty() ? nullptr : mImagePath.c_str(); }
	uint32 GetImageSize() const override { return mCartSize; }

	void *GetBuffer() override { return mCARTROM.data(); }

	uint64 GetChecksum() override;
	std::optional<uint32> GetFileCRC() const override { return mFileCRC; }

	bool IsDirty() const override { return mbDirty; }
	void SetClean() { mbDirty = false; }
	void SetDirty() {
		mbDirty = true;
		mbCartChecksumValid = false;
		mFileCRC = {};
	}

private:
	static uint64 ComputeChecksum(const uint8 *p, size_t len);

	ATCartridgeMode mCartMode = {};
	uint32 mCartSize = 0;

	vdfastvector<uint8> mCARTROM;
	VDStringW mImagePath;

	uint64 mCartChecksum;
	bool mbCartChecksumValid = false;
	bool mbDirty = false;
	std::optional<uint32> mFileCRC {};
};

void ATCartridgeImage::Init(ATCartridgeMode mode) {
	mCartMode = mode;
	mCartSize = ATGetImageSizeForCartridgeType(mode);
	mCARTROM.resize(mCartSize, 0xFF);
	mImagePath.clear();
	mbCartChecksumValid = false;
	mbDirty = false;
	mFileCRC = {};
}

bool ATCartridgeImage::Load(const wchar_t *path, IVDRandomAccessStream& stream, ATCartLoadContext *loadCtx) {
	sint64 size = stream.Length();

	if (size < 1024 || size > 128*1024*1024 + 16)
		throw MyError("Unsupported cartridge size.");

	mFileCRC = {};

	// check for header
	char buf[16];
	stream.Read(buf, 16);

	bool validHeader = false;
	uint32 size32 = (uint32)size;

	if (!memcmp(buf, "CART", 4)) {
		uint32 type = VDReadUnalignedBEU32(buf + 4);
		uint32 checksum = VDReadUnalignedBEU32(buf + 8);

		size32 -= 16;
		mCARTROM.resize(size32);
		mCartSize = size32;
		stream.Read(mCARTROM.data(), size32);

		bool useChecksum = !loadCtx || !loadCtx->mbIgnoreChecksum;
		uint32 csum = 0;

		if (useChecksum) {
			csum = ComputeByteSum32(mCARTROM.data(), size32);
		}

		if (csum == checksum || !useChecksum) {
			validHeader = true;

			int mode = ATGetCartridgeModeForMapper(type);

			if (!mode && !(loadCtx && loadCtx->mbIgnoreMapper))
				throw MyError("The selected cartridge cannot be loaded as it uses unsupported mapper mode %d.", type);

			mCartMode = (ATCartridgeMode)mode;

			VDCRCChecker crcChecker(VDCRCTable::CRC32);

			crcChecker.Process(buf, 16);
			crcChecker.Process(mCARTROM.data(), size32);
			mFileCRC = crcChecker.CRC();
		}
	}

	if (loadCtx)
		loadCtx->mCartSize = size32;

	if (!validHeader) {
		if (loadCtx && loadCtx->mbReturnOnUnknownMapper) {
			loadCtx->mLoadStatus = kATCartLoadStatus_UnknownMapper;

			// If the cartridge isn't too big, capture it.
			if (loadCtx->mpCaptureBuffer && size32 <= 1048576 + 8192) {
				loadCtx->mpCaptureBuffer->resize(size32);
				stream.Seek(0);
				stream.Read(loadCtx->mpCaptureBuffer->data(), size32);
				stream.Seek(0);

				loadCtx->mRawImageChecksum = ComputeChecksum(loadCtx->mpCaptureBuffer->data(), size32);
			}
			return false;
		}

		mCARTROM.resize(size32);
		mCartSize = size32;
		stream.Seek(0);
		stream.Read(mCARTROM.data(), size32);

		if (loadCtx && loadCtx->mCartMapper > 0) {
			mCartMode = (ATCartridgeMode)loadCtx->mCartMapper;
		} else {
			if (size32 <= 8192)
				mCartMode = kATCartridgeMode_8K;
			else if (size32 == 16384)
				mCartMode = kATCartridgeMode_16K;
			else if (size32 == 0x8000)
				mCartMode = kATCartridgeMode_XEGS_32K;
			else if (size32 == 0xA000)
				mCartMode = kATCartridgeMode_BountyBob800;
			else if (size32 == 0x10000)
				mCartMode = kATCartridgeMode_XEGS_64K;
			else if (size32 == 131072)
				mCartMode = kATCartridgeMode_MaxFlash_128K;
			else if (size32 == 524288)
				mCartMode = kATCartridgeMode_MegaCart_512K;
			else if (size32 == 1048576)
				mCartMode = kATCartridgeMode_MaxFlash_1024K;
			else
				throw MyError("Unsupported cartridge size.");
		}

		mFileCRC = VDCRCTable::CRC32.CRC(mCARTROM.data(), size32);
	}

	if (loadCtx) {
		if (!validHeader)
			loadCtx->mCartMapper = mCartMode;
	}

	if (mCartMode) {
		uint32 allocSize = ATGetImageSizeForCartridgeType(mCartMode);

		if (mCartMode == kATCartridgeMode_TelelinkII)
			mCARTROM.resize(allocSize, 0xFF);
		else
			mCARTROM.resize(allocSize, 0);

		mCartSize = allocSize;
	}

	if (mCartMode == kATCartridgeMode_8K) {
		// For the 8K cart, we have a special case if the ROM is 2K or 4K -- in that case,
		// we mirror the existing ROM to fit.
		uint8 *p = mCARTROM.data();

		if (size32 == 2048) {
			for(int i=0; i<3; ++i)
				memcpy(p + 2048*(i + 1), p, 2048);
		} else if (size32 == 4096) {
			memcpy(p + 4096, p, 4096);
		}
	} else if (mCartMode == kATCartridgeMode_SIC) {
		uint8 *p = mCARTROM.data();

		if (size32 == 0x20000)
			memcpy(p + 0x20000, p, 0x20000);

		if (size32 == 0x20000 || size32 == 0x40000)
			memcpy(p + 0x40000, p, 0x40000);
	} else if (mCartMode == kATCartridgeMode_Atrax_SDX_64K) {
		uint8 *p = mCARTROM.data();

		DeinterleaveAtraxSDX64K(p);
	} else if (mCartMode == kATCartridgeMode_Atrax_SDX_128K) {
		uint8 *p = mCARTROM.data();

		DeinterleaveAtraxSDX64K(p);
		DeinterleaveAtraxSDX64K(p+65536);
	} else if (mCartMode == kATCartridgeMode_Atrax_128K_Raw) {
		uint8 *p = mCARTROM.data();

		DeinterleaveAtrax128K(p);
	} else if (mCartMode == kATCartridgeMode_OSS_034M) {
		mCARTROM.resize(0x7000);

		uint8 *p = mCARTROM.data();

		memset(p + 0x4000, 0xFF, 0x1000);

		for(int i=0; i<0x1000; ++i)
			p[i + 0x5000] = p[i] & p[i + 0x1000];

		for(int i=0; i<0x1000; ++i)
			p[i + 0x6000] = p[i + 0x2000] & p[i + 0x1000];
	} else if (mCartMode == kATCartridgeMode_OSS_043M) {
		mCARTROM.resize(0x7000);

		uint8 *p = mCARTROM.data();

		memset(p + 0x4000, 0xFF, 0x1000);

		for(int i=0; i<0x1000; ++i)
			p[i + 0x5000] = p[i] & p[i + 0x2000];

		for(int i=0; i<0x1000; ++i)
			p[i + 0x6000] = p[i + 0x2000] & p[i + 0x1000];
	}

	// shift/realign images
	switch(mCartMode) {
	case kATCartridgeMode_2K:
		// Shift the ROM image so that the bottom 6K is open ($FF) and the image
		// resides in the top 2K.
		mCARTROM.resize(8192);
		memcpy(&mCARTROM[6144], &mCARTROM[0], 2048);
		memset(&mCARTROM[0], 0xFF, 6144);
		break;
	case kATCartridgeMode_4K:
	case kATCartridgeMode_RightSlot_4K:
		// Shift the ROM image so that the bottom 4K is open ($FF) and the image
		// resides in the top 4K.
		mCARTROM.resize(8192);
		memcpy(&mCARTROM[4096], &mCARTROM[0], 4096);
		memset(&mCARTROM[0], 0xFF, 4096);
		break;
	}

	if (path)
		mImagePath = path;
	else
		mImagePath.clear();

	if (loadCtx) {
		loadCtx->mCartMapper = mCartMode;
		loadCtx->mLoadStatus = kATCartLoadStatus_Ok;
	}

	mbCartChecksumValid = false;

	mbDirty = false;
	return true;
}

void *ATCartridgeImage::AsInterface(uint32 id) {
	switch(id) {
		case IATCartridgeImage::kTypeID: return static_cast<IATCartridgeImage *>(this);
	}

	return nullptr;
}

uint64 ATCartridgeImage::GetChecksum() {
	if (!mbCartChecksumValid) {
		// Checksum pages at a time
		mCartChecksum = 0;

		size_t len = mCartSize;
		const uint8 *p = mCARTROM.data();

		mCartChecksum = ComputeChecksum(p, len);
	}

	return mCartChecksum;
}

uint64 ATCartridgeImage::ComputeChecksum(const uint8 *p, size_t len) {
	uint64 checksum = 0;
	uint32 offset = 0;

	while(len) {
		size_t tc = len > 256 ? 256 : len;

		checksum += ATComputeBlockChecksum(ATComputeOffsetChecksum(offset), p, tc);

		len -= tc;
		p += tc;
		++offset;
	}

	return checksum;
}

///////////////////////////////////////////////////////////////////////////

bool ATLoadCartridgeImage(const wchar_t *path, IATCartridgeImage **ppImage) {
	VDFileStream f(path);

	return ATLoadCartridgeImage(path, f, nullptr, ppImage);
}	

bool ATLoadCartridgeImage(const wchar_t *origPath, IVDRandomAccessStream& stream, ATCartLoadContext *loadCtx, IATCartridgeImage **ppImage) {
	vdrefptr<ATCartridgeImage> cartImage(new ATCartridgeImage);

	if (!cartImage->Load(origPath, stream, loadCtx))
		return false;

	*ppImage = cartImage.release();
	return true;
}

void ATCreateCartridgeImage(ATCartridgeMode mode, IATCartridgeImage **ppImage) {
	vdrefptr<ATCartridgeImage> cartImage(new ATCartridgeImage);

	cartImage->Init(mode);

	*ppImage = cartImage.release();
}

void ATSaveCartridgeImage(IATCartridgeImage *image, const wchar_t *path, bool includeHeader) {
	auto cartMode = image->GetMode();
	uint32 cartSize = image->GetImageSize();
	int type = 0;

	if (includeHeader) {
		type = ATGetCartridgeMapperForMode(cartMode, cartSize);

		if (!type)
			throw MyError("This cartridge type is not supported in the .CAR file format and must be saved as a raw image.");
	}

	VDFile f(path, nsVDFile::kWrite | nsVDFile::kDenyAll | nsVDFile::kCreateAlways);

	uint32 size = image->GetImageSize();
	const uint8 *src = (const uint8 *)image->GetBuffer();

	// apply adjustment for cartridge types that we pad the ROM image for
	switch(cartMode) {
		case kATCartridgeMode_2K:
			size = 2048;
			src += 6144;
			break;

		case kATCartridgeMode_4K:
		case kATCartridgeMode_RightSlot_4K:
			size = 4096;
			src += 4096;
			break;
	}

	// write header
	if (includeHeader) {
		char header[16] = { 'C', 'A', 'R', 'T' };

		VDWriteUnalignedBEU32(header + 4, type);

		uint32 checksum = 0;
		for(uint32 i=0; i<size; ++i)
			checksum += src[i];

		VDWriteUnalignedBEU32(header + 8, checksum);

		f.write(header, 16);
	}

	f.write(src, size);

	image->SetClean();
}
