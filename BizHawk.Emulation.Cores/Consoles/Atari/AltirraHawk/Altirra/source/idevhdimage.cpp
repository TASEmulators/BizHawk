//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2012 Avery Lee
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
#include <vd2/system/bitmath.h>
#include <vd2/system/date.h>
#include <vd2/system/error.h>
#include <vd2/system/filesys.h>
#include <vd2/system/math.h>
#include <at/atcore/propertyset.h>
#include <at/atcore/progress.h>
#include "idevhdimage.h"
#include "oshelper.h"

namespace {
	uint32 sumbytes(const uint8 *src, uint32 n) {
		uint32 sum = 0;
		for(uint32 i=0; i<n; ++i)
			sum += src[i];

		return sum;
	}

	uint32 ComputeChecksum(const ATVHDFooter& footer) {
		const uint8 *p = (const uint8 *)&footer;

		// The VHD checksum is a one's complement of the sum of all bytes except
		// for the checksum. Note that this is invariant to endianness.
		return ~(sumbytes(p, 64) + sumbytes(p + 68, 512 - 68));
	}

	uint32 ComputeChecksum(const ATVHDDynamicDiskHeader& header) {
		const uint8 *p = (const uint8 *)&header;

		// The VHD checksum is a one's complement of the sum of all bytes except
		// for the checksum. Note that this is invariant to endianness.
		return ~(sumbytes(p, 36) + sumbytes(p + 40, 1024 - 40));
	}
}

void ATCreateDeviceHardDiskVHDImage(const ATPropertySet& pset, IATDevice **dev);

extern const ATDeviceDefinition g_ATDeviceDefIDEVHDImage = { "hdvhdimage", "harddisk", L"Hard disk image (VHD file)", ATCreateDeviceHardDiskVHDImage };

const uint8 kATVHDFooterSignature[8] = { 'c', 'o', 'n', 'e', 'c', 't', 'i', 'x' };
const uint8 kATVHDDynamicHeaderSignature[8] = { 'c', 'x', 's', 'p', 'a', 'r', 's', 'e' };

class ATInvalidVHDImageException : public MyError {
public:
	ATInvalidVHDImageException(const wchar_t *path) : MyError("%ls is not a valid VHD image.", path) {}
};

class ATUnsupportedVHDImageException : public MyError {
public:
	ATUnsupportedVHDImageException(const wchar_t *path) : MyError("%ls is a valid but unsupported VHD image.", path) {}
};

void ATSwapEndian(uint32& v) {
	v = VDSwizzleU32(v);
}

void ATSwapEndian(uint64& v) {
	v = VDSwizzleU64(v);
}

template<class T>
void ATSwapEndianArray(T *p, uint32 n) {
	while(n--)
		ATSwapEndian(*p++);
}

void ATSwapEndian(ATVHDFooter& footer) {
	ATSwapEndian(footer.mFeatures);
	ATSwapEndian(footer.mVersion);
	ATSwapEndian(footer.mDataOffset);
	ATSwapEndian(footer.mTimestamp);
	ATSwapEndian(footer.mCreatorApplication);
	ATSwapEndian(footer.mCreatorVersion);
	ATSwapEndian(footer.mCreatorHostOS);
	ATSwapEndian(footer.mOriginalSize);
	ATSwapEndian(footer.mCurrentSize);
	ATSwapEndian(footer.mDiskGeometry);
	ATSwapEndian(footer.mDiskType);
	ATSwapEndian(footer.mChecksum);
}

void ATSwapEndian(ATVHDDynamicDiskHeader& header) {
	ATSwapEndian(header.mDataOffset);
	ATSwapEndian(header.mTableOffset);
	ATSwapEndian(header.mHeaderVersion);
	ATSwapEndian(header.mMaxTableEntries);
	ATSwapEndian(header.mBlockSize);
	ATSwapEndian(header.mChecksum);
}

ATIDEVHDImage::ATIDEVHDImage()
	: mbReadOnly(false)
	, mFooterLocation(0)
	, mSectorCount(0)
	, mBlockSizeShift(0)
	, mBlockLBAMask(0)
	, mBlockSize(0)
	, mBlockBitmapSize(0)
	, mCurrentBlock(0)
	, mCurrentBlockDataOffset(0)
	, mbCurrentBlockBitmapDirty(false)
	, mbCurrentBlockAllocated(false)
{
	VDASSERTCT(sizeof(ATVHDFooter) == 512);
	VDASSERTCT(sizeof(ATVHDDynamicDiskHeader) == 1024);
}

ATIDEVHDImage::~ATIDEVHDImage() {
	Shutdown();
}

int ATIDEVHDImage::AddRef() {
	return ATDevice::AddRef();
}

int ATIDEVHDImage::Release() {
	return ATDevice::Release();
}

void *ATIDEVHDImage::AsInterface(uint32 iid) {
	switch(iid) {
		case IATBlockDevice::kTypeID: return static_cast<IATBlockDevice *>(this);
		default:
			return ATDevice::AsInterface(iid);
	}
}

void ATIDEVHDImage::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefIDEVHDImage;
}

void ATIDEVHDImage::GetSettingsBlurb(VDStringW& buf) {
	buf = VDFileSplitPathRightSpan(mPath);
}

void ATIDEVHDImage::GetSettings(ATPropertySet& settings) {
	settings.SetString("path", mPath.c_str());
	settings.SetBool("write_enabled", !mbReadOnly);
	settings.SetBool("solid_state", mbSolidState);
}

bool ATIDEVHDImage::SetSettings(const ATPropertySet& settings) {
	return false;
}

uint32 ATIDEVHDImage::GetSectorCount() const {
	return mSectorCount;
}

ATBlockDeviceGeometry ATIDEVHDImage::GetGeometry() const {
	ATBlockDeviceGeometry geo = {};

	geo.mbSolidState = mbSolidState;

	return geo;
}

uint32 ATIDEVHDImage::GetSerialNumber() const {
	return VDHashString32I(mPath.c_str());
}

void ATIDEVHDImage::Init(const wchar_t *path, bool write, bool solidState) {
	Shutdown();

	mPath = path;
	mFile.open(path, write ? nsVDFile::kReadWrite | nsVDFile::kDenyAll | nsVDFile::kOpenAlways : nsVDFile::kRead | nsVDFile::kDenyWrite | nsVDFile::kOpenExisting);
	mbReadOnly = !write;
	mbSolidState = solidState;

	uint64 size = mFile.size();

	if (size < 512)
		throw ATInvalidVHDImageException(path);

	mFooterLocation = size - 512;
	mFile.seek(mFooterLocation);

	uint8 footerbuf[512];
	mFile.read(footerbuf, 512);

	// We need to handle either 511 or 512 byte headers here, per the spec.
	if (!memcmp(footerbuf + 1, kATVHDFooterSignature, 8)) {
		// 511 byte header -- make it a 512 byte header.
		memmove(footerbuf, footerbuf + 1, 511);
		footerbuf[511] = 0;
		++mFooterLocation;
	} else if (memcmp(footerbuf, kATVHDFooterSignature, 8))
		throw ATInvalidVHDImageException(path);

	VDASSERTCT(sizeof(ATVHDFooter) == 512);
	memcpy(&mFooter, footerbuf, 512);

	// swizzle to little endian
	ATSwapEndian(mFooter);

	// validate the footer
	if ((mFooter.mVersion & 0xffff0000) != 0x00010000)
		throw ATUnsupportedVHDImageException(path);

	if (!(mFooter.mFeatures & 0x2)) {
		// reserved bit in features mask was not set
		throw ATInvalidVHDImageException(path);
	}

	// checksum the footer (~ of sum of all bytes except checksum field at 64-67); note
	// that we do this on the *unswizzled* buffer
	if (mFooter.mChecksum != ComputeChecksum(mFooter))
		throw ATInvalidVHDImageException(path);

	// check for a supported disk type
	if (mFooter.mDiskType != ATVHDFooter::kDiskTypeFixed && mFooter.mDiskType != ATVHDFooter::kDiskTypeDynamic)
		throw MyError("Only fixed and dynamic disk type VHD images are currently supported.");

	// read off drive size
	mSectorCount = VDClampToUint32(mFooter.mCurrentSize >> 9);

	// if we've got a dynamic disk, read the dyndisk header
	if (mFooter.mDiskType == ATVHDFooter::kDiskTypeDynamic) {
		if (mFooter.mDataOffset >= size || size - mFooter.mDataOffset < sizeof(ATVHDDynamicDiskHeader))
			throw ATInvalidVHDImageException(path);

		uint8 rawdynheader[1024];

		mFile.seek(mFooter.mDataOffset);
		mFile.read(rawdynheader, sizeof rawdynheader);

		// validate signature
		if (memcmp(rawdynheader, kATVHDDynamicHeaderSignature, 8))
			throw ATInvalidVHDImageException(path);

		// swizzle to local endian
		memcpy(&mDynamicHeader, rawdynheader, sizeof mDynamicHeader);
		ATSwapEndian(mDynamicHeader);

		// validate the version
		if ((mDynamicHeader.mHeaderVersion & 0xffff0000) != 0x00010000)
			throw ATUnsupportedVHDImageException(path);

		// checksum the header
		if (mDynamicHeader.mChecksum != ComputeChecksum(mDynamicHeader))
			throw ATInvalidVHDImageException(path);

		// block size must always be a power of two
		if (mDynamicHeader.mBlockSize < 512 || (mDynamicHeader.mBlockSize & (mDynamicHeader.mBlockSize - 1)))
			throw ATInvalidVHDImageException(path);

		mBlockSize = mDynamicHeader.mBlockSize;
		mBlockSizeShift = VDFindLowestSetBit(mBlockSize);
		mBlockLBAMask = mBlockSizeShift < 41 ? (1 << (mBlockSizeShift - 9)) - 1 : 0xFFFFFFFFU;

		// compute additional size with bitmap -- the bitmap is always padded
		// to a 512 byte sector (4096 bits, or equivalent to 2MB)
		mBlockBitmapSize = (((mBlockSize - 1) >> 21) + 1) * 512;
		mCurrentBlockBitmap.resize(mBlockBitmapSize);

		// validate the size of the block allocation table
		uint64 blockCount64 = ((mFooter.mOriginalSize - 1) >> mBlockSizeShift) + 1;

		if (blockCount64 != mDynamicHeader.mMaxTableEntries)
			throw ATInvalidVHDImageException(path);

		uint32 blockCount = (uint32)blockCount64;

		// validate the location of the BAT
		uint32 batSize = sizeof(uint64) * blockCount;
		if (mDynamicHeader.mTableOffset >= size && size - mDynamicHeader.mTableOffset < batSize)
			throw ATInvalidVHDImageException(path);

		// read in the BAT
		mBlockAllocTable.resize(blockCount);

		mFile.seek(mDynamicHeader.mTableOffset);
		mFile.read(mBlockAllocTable.data(), (long)mBlockAllocTable.size() * sizeof(mBlockAllocTable[0]));

		// swizzle the BAT
		ATSwapEndianArray(mBlockAllocTable.data(), (long)mBlockAllocTable.size());

		// validate the BAT
		for(uint32 i=0; i<blockCount; ++i) {
			uint32 sectorOffset = mBlockAllocTable[i];

			// check if the block is allocated
			if (sectorOffset != 0xFFFFFFFF) {
				uint64 byteOffset = (uint64)sectorOffset << 9;

				// the block cannot be beyond the end of the file or overlap the footer copy
				// at the beginning
				if (byteOffset >= size || size - byteOffset < mBlockSize + mBlockBitmapSize ||
					byteOffset < sizeof(ATVHDFooter))
				{
					throw ATInvalidVHDImageException(path);
				}
			}
		}
	}

	InitCommon();
}

void ATIDEVHDImage::InitNew(const wchar_t *path, uint8 heads, uint8 spt, uint32 totalSectorCount, bool dynamic) {
	Shutdown();

	mSectorCount = totalSectorCount;
	mFile.open(path, nsVDFile::kReadWrite | nsVDFile::kDenyAll | nsVDFile::kCreateAlways | nsVDFile::kSequential);
	mbReadOnly = false;
	mbSolidState = false;

	// set up footer
	const uint64 vhdEpoch = 0x01bf53eb256d4000ull;	// January 1, 2000 midnight UTC in ticks

	memset(&mFooter, 0, sizeof mFooter);

	uint32 cylinders = totalSectorCount / ((uint32)heads * (uint32)spt);

	if (cylinders > 0xffff)
		cylinders = 0xffff;

	memcpy(mFooter.mCookie, kATVHDFooterSignature, sizeof mFooter.mCookie);
	mFooter.mFeatures = 0x2;
	mFooter.mVersion = 0x00010000;
	mFooter.mDataOffset = dynamic ? sizeof(mFooter) : 0xFFFFFFFFFFFFFFFFull;
	mFooter.mTimestamp = (uint32)((VDGetCurrentDate().mTicks - vhdEpoch) / 10000000);
	mFooter.mCreatorApplication = VDMAKEFOURCC('a', 'r', 't', 'A');
	mFooter.mCreatorVersion = 0x00020000;
	mFooter.mCreatorHostOS = 0x5769326B;	// Wi2k
	mFooter.mOriginalSize = (uint64)totalSectorCount << 9;
	mFooter.mCurrentSize = mFooter.mOriginalSize;
	mFooter.mDiskGeometry = (cylinders << 16) + ((uint32)heads << 8) + spt;
	mFooter.mDiskType = dynamic ? ATVHDFooter::kDiskTypeDynamic : ATVHDFooter::kDiskTypeFixed;
	ATGenerateGuid(mFooter.mUniqueId);
	mFooter.mSavedState = 0;

	// compute checksum -- fortunately this is endian agnostic
	mFooter.mChecksum = ComputeChecksum(mFooter);

	// copy to raw buffer and swizzle
	ATVHDFooter rawFooter = mFooter;
	ATSwapEndian(rawFooter);

	// check if we are creating a dynamic disk
	if (dynamic) {
		// write out the footer copy
		mFile.write(&rawFooter, sizeof rawFooter);

		// initialize dynamic parameters and the BAT
		mBlockSize = 0x200000;		// 2MB
		mBlockSizeShift = 21;
		mBlockLBAMask = 0xFFF;
		mBlockBitmapSize = 0x200;

		const uint32 blockCount = (totalSectorCount + 4095) >> 12;
		const uint32 headerSize = sizeof(mFooter) + sizeof(mDynamicHeader);
		const uint32 batSize = (blockCount * 4 + 511) & ~511;

		// set up the dynamic header
		memset(&mDynamicHeader, 0, sizeof mDynamicHeader);
		memcpy(mDynamicHeader.mCookie, kATVHDDynamicHeaderSignature, sizeof mDynamicHeader.mCookie);
		mDynamicHeader.mDataOffset = 0xFFFFFFFFFFFFFFFFull;
		mDynamicHeader.mTableOffset = headerSize;
		mDynamicHeader.mHeaderVersion = 0x00010000;
		mDynamicHeader.mMaxTableEntries = blockCount;
		mDynamicHeader.mBlockSize = mBlockSize;

		// compute checksum
		mDynamicHeader.mChecksum = ComputeChecksum(mDynamicHeader);

		// write out the dynamic header
		ATVHDDynamicDiskHeader rawDynamicHeader(mDynamicHeader);
		ATSwapEndian(rawDynamicHeader);
		mFile.write(&rawDynamicHeader, sizeof(rawDynamicHeader));

		// write out the BAT
		vdblock<uint32> batBuf(16384);

		memset(batBuf.data(), 0xFF, 16384*sizeof(uint32));

		uint32 batBytesToWrite = batSize;
		while(batBytesToWrite) {
			uint32 tc = std::min<uint32>(batBytesToWrite, 65536);

			mFile.write(batBuf.data(), tc);
			batBytesToWrite -= tc;
		}

		// init runtime buffers
		mBlockAllocTable.resize(blockCount, 0xFFFFFFFF);
		mCurrentBlockBitmap.resize(mBlockBitmapSize);
	} else {
		// write blank data
		vdblock<uint8> clearData(262144);

		memset(clearData.data(), 0, 262144);

		uint32 sectorsToClear = mSectorCount;
		uint32 sectorsCleared = 0;

		ATProgress progress;

		progress.InitF(((sectorsToClear - 1) >> 11) + 1, L"Initialized %uMB / %uMB", L"Clearing fixed disk image");

		while(sectorsToClear) {
			uint32 tc = std::min<uint32>(sectorsToClear, 512);

			mFile.write(clearData.data(), tc * 512);

			sectorsCleared += tc;
			sectorsToClear -= tc;

			progress.Update(sectorsCleared >> 11);
		}
	}

	// write out the footer
	mFile.write(&rawFooter, sizeof rawFooter);
	mFooterLocation = mFile.tell();

	InitCommon();
}

void ATIDEVHDImage::InitCommon() {
	mCurrentBlock = 0xFFFFFFFFU;
	mCurrentBlockDataOffset = 0;
	mbCurrentBlockBitmapDirty = false;
	mbCurrentBlockAllocated = false;
}

void ATIDEVHDImage::Shutdown() {
	// Flush the disk if we can, but eat I/O errors.
	try {
		Flush();
	} catch(const MyError&) {
	}

	mFile.closeNT();
}

void ATIDEVHDImage::Flush() {
	FlushCurrentBlockBitmap();
}

void ATIDEVHDImage::ReadSectors(void *data, uint32 lba, uint32 n) {
	if (mFooter.mDiskType == ATVHDFooter::kDiskTypeDynamic) {
		ReadDynamicDiskSectors(data, lba, n);
	} else {
		mFile.seek((sint64)lba << 9);

		uint32 requested = n << 9;
		uint32 actual = mFile.readData(data, requested);

		if (requested < actual)
			memset((char *)data + actual, 0, requested - actual);
	}
}

void ATIDEVHDImage::WriteSectors(const void *data, uint32 lba, uint32 n) {
	if (mFooter.mDiskType == ATVHDFooter::kDiskTypeDynamic) {
		WriteDynamicDiskSectors(data, lba, n);
	} else {
		mFile.seek((sint64)lba << 9);
		mFile.write(data, 512 * n);
	}
}

void ATIDEVHDImage::ReadDynamicDiskSectors(void *data, uint32 lba, uint32 n) {
	// preclear memory
	memset(data, 0, 512*n);

	// compute starting block
	uint32 blockIndex = lba >> (mBlockSizeShift - 9);

	while(n) {
		// compute count we can handle in this block
		uint32 blockCount = std::min<uint32>(n, (~lba & mBlockLBAMask) + 1);

		// read in block bitmap if necessary
		SetCurrentBlock(blockIndex);

		// read in valid sectors
		uint32 blockSectorOffset = lba & mBlockLBAMask;

		for(uint32 i=0; i<blockCount; ++i) {
			if (mCurrentBlockBitmap[blockSectorOffset >> 3] & (0x80 >> (blockSectorOffset & 7))) {
				mFile.seek(mCurrentBlockDataOffset + ((sint64)(blockSectorOffset + i) << 9));
				mFile.read((char *)data + i*512, 512);
			}
		}

		// next block
		lba += blockCount;
		data = (char *)data + blockCount * 512;
		n -= blockCount;
		++blockIndex;
	}
}

void ATIDEVHDImage::WriteDynamicDiskSectors(const void *data, uint32 lba, uint32 n) {
	// compute starting block
	uint32 blockIndex = lba >> (mBlockSizeShift - 9);

	while(n) {
		// compute count we can handle in this block
		uint32 blockCount = std::min<uint32>(n, (~lba & mBlockLBAMask) + 1);

		// read in block bitmap if necessary
		SetCurrentBlock(blockIndex);

		// write sectors
		uint32 blockSectorOffset = lba & mBlockLBAMask;

		for(uint32 i=0; i<blockCount; ++i) {
			uint8& sectorMaskByte = mCurrentBlockBitmap[blockSectorOffset >> 3];
			const uint8 sectorBit = (0x80 >> (blockSectorOffset & 7));

			// check if we're writing zeroes to this sector
			const uint8 *secsrc = (const uint8 *)data + i*512;
			bool writingZero = true;

			for(uint32 j=0; j<512; ++j) {
				if (secsrc[j]) {
					writingZero = false;
					break;
				}
			}

			// Check if this sector is currently allocated and allocate
			// or deallocate it as necessary.
			//
			// allocated   writing-0	action
			// -----------------------------------------------------
			//	no			no			allocate and write sector
			//	no			yes			do nothing
			//	yes			no			write sector
			//	yes			yes			deallocate and write sector

			bool wasZero = !(sectorMaskByte & sectorBit);

			if (wasZero != writingZero) {
				// if this block is not allocated, we must be trying to allocate a sector in it,
				// and must extend the file to allocate the block now
				if (!writingZero && !mbCurrentBlockAllocated)
					AllocateBlock();

				sectorMaskByte ^= sectorBit;
				mbCurrentBlockBitmapDirty = true;
			}

			// write out new data if the sector is or was allocated (deallocated
			// sectors must still be zero).
			if (!writingZero || !wasZero) {
				mFile.seek(mCurrentBlockDataOffset + ((sint64)(blockSectorOffset + i) << 9));
				mFile.write(secsrc, 512);
			}
		}

		// next block
		lba += blockCount;
		data = (char *)data + blockCount * 512;
		n -= blockCount;
		++blockIndex;
	}
}

void ATIDEVHDImage::SetCurrentBlock(uint32 blockIndex) {
	if (mCurrentBlock == blockIndex)
		return;

	// if we have dirtied the current block bitmap, write it out
	if (mbCurrentBlockBitmapDirty)
		FlushCurrentBlockBitmap();

	// stomp the current block index in case we get an I/O error
	mCurrentBlock = 0xFFFFFFFFU;
	mCurrentBlockDataOffset = 0;

	// check if the sector is allocated
	uint32 sectorOffset = mBlockAllocTable[blockIndex];
	if (sectorOffset == 0xFFFFFFFFU) {
		// no -- set the block bitmap to all unallocated (0)
		memset(mCurrentBlockBitmap.data(), 0, mCurrentBlockBitmap.size() * sizeof(mCurrentBlockBitmap[0]));
		mbCurrentBlockAllocated = false;
	} else {
		// yes it is -- read in the bitmap from the new block
		mFile.seek((sint64)sectorOffset << 9);
		mFile.read(mCurrentBlockBitmap.data(), (long)(mCurrentBlockBitmap.size() * sizeof(mCurrentBlockBitmap[0])));
		mbCurrentBlockAllocated = true;
		mCurrentBlockDataOffset = ((sint64)sectorOffset << 9) + mBlockBitmapSize;
	}

	// all done
	mCurrentBlock = blockIndex;
}

void ATIDEVHDImage::FlushCurrentBlockBitmap() {
	if (mbCurrentBlockBitmapDirty) {
		uint32 sectorOffset = mBlockAllocTable[mCurrentBlock];
		mFile.seek((uint64)sectorOffset << 9);
		mFile.write(mCurrentBlockBitmap.data(), (long)(mCurrentBlockBitmap.size() * sizeof(mCurrentBlockBitmap[0])));
		mbCurrentBlockBitmapDirty = false;
	}
}

void ATIDEVHDImage::AllocateBlock() {
	// fast out if the block is already allocated
	if (mbCurrentBlockAllocated)
		return;

	// Compute where we're going to place the new block, based on the footer
	// that we're going to overwrite. Note that we strategically place the block
	// so that the block data is on a 4K boundary.
	const sint64 newBlockDataLoc = (mFooterLocation + mBlockBitmapSize + 4095) & ~(sint64)4095;
	const sint64 newBlockBitmapLoc = newBlockDataLoc - mBlockBitmapSize;

	// Compute the new footer location.
	sint64 newFooterLocation = newBlockDataLoc + mBlockSize;

	// Extend the file and rewrite the footer. There is nothing we need to change
	// here so this is pretty easy. We do, however, need to swizzle it back. The
	// checksum should still be valid.
	ATVHDFooter rawFooter = mFooter;
	ATSwapEndian(rawFooter);

	VDASSERT(ComputeChecksum(rawFooter) == mFooter.mChecksum);

	mFile.seek(newFooterLocation);
	mFile.write(&rawFooter, sizeof rawFooter);

	// Flush to disk so we know the VHD is valid again.
	mFile.flush();
	mFooterLocation = newFooterLocation;

	// Write the new block bitmap.
	mFile.seek(newBlockBitmapLoc);
	mFile.write(mCurrentBlockBitmap.data(), (long)(mCurrentBlockBitmap.size() * sizeof(mCurrentBlockBitmap[0])));

	// Zero the data; technically not needed with NTFS since the bitmap is always at least
	// as big as the footer and NTFS zeroes new space, but we might be running on FAT32
	vdblock<uint8> zerobuf(65536);
	memset(zerobuf.data(), 0, 65536);

	uint32 bytesLeft = mBlockSize;
	while(bytesLeft) {
		uint32 tc = std::min<uint32>(mBlockSize, 65536);

		mFile.write(zerobuf.data(), tc);
		bytesLeft -= tc;
	}

	// flush again, so the block is OK on disk
	mFile.flush();

	// now update the BAT; we don't need to flush this as if it doesn't go through
	// the file is still valid
	mFile.seek(mDynamicHeader.mTableOffset + 4*mCurrentBlock);

	uint32 sectorOffset = (uint32)(newBlockBitmapLoc >> 9);
	mBlockAllocTable[mCurrentBlock] = sectorOffset;

	uint8 rawSectorOffset[4];
	VDWriteUnalignedBEU32(&rawSectorOffset, sectorOffset);

	mFile.write(rawSectorOffset, 4);

	// all done!
	mCurrentBlockDataOffset = newBlockDataLoc;
	mbCurrentBlockAllocated = true;
}
