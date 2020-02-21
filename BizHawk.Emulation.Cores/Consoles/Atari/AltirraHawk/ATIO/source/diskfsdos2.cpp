//	Altirra - Atari 800/800XL/5200 emulator
//	I/O library - Atari DOS 2.x filesystem handler
//	Copyright (C) 2009-2015 Avery Lee
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
#include <vd2/system/bitmath.h>
#include <vd2/system/binary.h>
#include <vd2/system/strutil.h>
#include <vd2/system/vdalloc.h>
#include <at/atio/diskfs.h>
#include <at/atio/diskimage.h>

#include "bootsecdos2.inl"

// DOS 1/2/MyDOS filesystem.
//
// Inode keys are as follows:
//	- Bits 0-5: File ID (0-63)
//	- Bits 6-21: Parent directory starting sector
//
class ATDiskFSDOS2 final : public IATDiskFS {
public:
	ATDiskFSDOS2();
	~ATDiskFSDOS2();

public:
	void InitNew(IATDiskImage *image, bool mydos, bool dos1);
	void Init(IATDiskImage *image, bool readOnly);
	void GetInfo(ATDiskFSInfo& info);

	bool IsReadOnly() { return mbReadOnly; }
	void SetReadOnly(bool readOnly);
	void SetAllowExtend(bool allow) {}
	void SetStrictNameChecking(bool strict) { mbStrictNames = strict; }

	bool Validate(ATDiskFSValidationReport& report);
	void Flush();

	ATDiskFSFindHandle FindFirst(ATDiskFSKey key, ATDiskFSEntryInfo& info);
	bool FindNext(ATDiskFSFindHandle searchKey, ATDiskFSEntryInfo& info);
	void FindEnd(ATDiskFSFindHandle searchKey);

	void GetFileInfo(ATDiskFSKey key, ATDiskFSEntryInfo& info);
	ATDiskFSKey GetParentDirectory(ATDiskFSKey dirKey);

	ATDiskFSKey LookupFile(ATDiskFSKey parentKey, const char *filename);

	void DeleteFile(ATDiskFSKey key);
	void ReadFile(ATDiskFSKey key, vdfastvector<uint8>& dst);
	ATDiskFSKey WriteFile(ATDiskFSKey parentKey, const char *filename, const void *src, uint32 len);
	void RenameFile(ATDiskFSKey key, const char *newFileName);
	void SetFileTimestamp(ATDiskFSKey key, const VDExpandedDate& date) {}

	ATDiskFSKey CreateDir(ATDiskFSKey parentKey, const char *filename);

protected:
	struct DirEnt;
	
	ATDiskFSKey LookupEntry(const char *filename) const;

	void DecodeDirEnt(DirEnt& de, const uint8 *src) const;

	bool IsVisible(const DirEnt& de) const;
	bool IsDOS1File(const DirEnt& de) const;
	bool IsExtendedAddressingUsed(const DirEnt& de) const;

	uint8 GetSectorDataBytes(bool isDOS1File, const uint8 *secBuf) const;
	uint32 GetNextSector(bool isDOS1File, bool extendedAddressing, uint8 fileId, const uint8 *secBuf) const;
	bool IsSectorAllocated(uint32 sector) const;
	void AllocateSector(uint32 sector);
	void FreeSector(uint32 sector);

	// Recompute number of free sectors in VTOC from sector bits.
	uint32 CountFreeSectors() const;

	// Return number of set bits within a bitfield, starting from MSB.
	uint32 CountFreeSectors(const uint8 *bitmap, uint32 bitcount) const;

	static void WriteFileName(DirEnt& de, const char *filename);
	bool IsValidFileName(const char *filename) const;

	void FlushDirectoryCache();
	void LoadDirectory(ATDiskFSKey key);
	void LoadDirectoryByStart(uint32 startSector);

	IATDiskImage *mpImage;
	bool mbDirty;
	bool mbReadOnly;
	bool mbStrictNames = true;
	bool mbDOS1;
	bool mbDOS25;
	bool mbMyDOS;
	uint32 mSectorSize;

	struct DirEnt {
		enum {
			kFlagDeleted	= 0x80,
			kFlagInUse		= 0x40,
			kFlagLocked		= 0x20,
			kFlagSubDir		= 0x10,		// MyDOS only: subdirectory
			kFlagExtFile	= 0x04,		// MyDOS only: file ID not in sector links
			kFlagDOS2		= 0x02,
			kFlagOpenWrite	= 0x01
		};

		uint16	mSectorCount;
		uint16	mFirstSector;
		uint32	mBytes;
		uint8	mFlags;
		char	mName[13];
	};

	struct FindHandle {
		uint32	mDirectoryStart;
		uint32	mPos;
	};

	uint32	mDirectoryStart;
	uint32	mDirectoryBaseKey = 0;
	bool	mbDirectoryDirty;
	DirEnt	mDirectory[64];

	uint8	mSectorBuffer[256];

	// First 10 bytes of compatible VTOC, followed by VTOC bitmap, padded to sector size.
	vdfastvector<uint8> mVTOCBitmap;

	vdfastvector<uint8> mTempSectorMap;

	vdhashmap<uint32, uint32> mDirStartToKeyMap;
};

ATDiskFSDOS2::ATDiskFSDOS2() {
}

ATDiskFSDOS2::~ATDiskFSDOS2() {
}

void ATDiskFSDOS2::InitNew(IATDiskImage *image, bool mydos, bool dos1) {
	uint32 sectorSize = image->GetSectorSize();

	if (sectorSize != 128 && dos1)
		throw MyError("Unsupported sector size for DOS 1.x image: %d bytes.", sectorSize);

	if (sectorSize != 128 && sectorSize != 256)
		throw MyError("Unsupported sector size for DOS 2.x/MyDOS image: %d bytes.", sectorSize);

	const uint32 sectorCount = image->GetVirtualSectorCount();
	if (dos1) {
		if (sectorCount != 720)
			throw MyError("Unsupported sector count for DOS 1.x image: %u sectors.", sectorCount);
	} else if (mydos) {
		if (sectorCount < 720)
			throw MyError("Unsupported sector count for MyDOS image: %u sectors.", sectorCount);
	} else {
		if (sectorCount != 720 && (sectorCount != 1040 || sectorSize != 128))
			throw MyError("Unsupported sector count for DOS 2.x image: %u sectors.", sectorCount);
	}

	mpImage = image;
	mbDirty = true;
	mbReadOnly = false;
	mbDOS1 = dos1;
	mbDOS25 = false;
	mbMyDOS = mydos;
	mSectorSize = sectorSize;

	// check if we need to use DOS 2.5 semantics
	if (!mydos) {
		if (sectorCount == 1040 && sectorSize == 128)
			mbDOS25 = true;
	}

	// initialize root directory
	memset(mSectorBuffer, 0, sizeof mSectorBuffer);

	for(int i=0; i<8; ++i)
		mpImage->WriteVirtualSector(361 + i, mSectorBuffer, mSectorSize);

	// invalidate directory cache
	mDirectoryStart = 0;
	mbDirectoryDirty = false;

	// initialize VTOC
	uint32 vtocSectorCount = 1;
	uint8 vtocCode = 0x02;

	if (mbMyDOS) {
		// first VTOC sector holds up to 118 bytes (944 sector bits), then additional VTOC sectors
		// are allocated 256 bytes at a time in either density
		//
		// Examples (MyDOS 4.55):
		//	943 sector SD -> $02, 1 VTOC sector
		//	944 sector SD -> $03, 2 VTOC sectors
		//	1967 sector SD -> $03, 2 VTOC sectors
		//	1968 sector SD -> $04, 4 VTOC sectors
		//	4015 sector SD -> $04, 4 VTOC sectors
		//	4016 sector SD -> $05, 6 VTOC sectors
		//	1023 sector DD -> $02, 1 VTOC sector
		//	1024 sector DD -> $03, 1 VTOC sector
		//	1967 sector DD -> $03, 1 VTOC sector
		//	1968 sector DD -> $04, 2 VTOC sectors

		if (sectorSize == 128) {
			// single-sector SD VTOC can hold up to 944 sector bits (943 valid sectors)
			// until we have to switch to more than one VTOC sector
			if (sectorCount > 943) {
				// - VTOC sectors are allocated in pairs above this threshold
				// - first 10 bytes (80 bits) are used by metadata
				// - first bitmap bit is for invalid sector 0
				vtocSectorCount = ((sectorCount + 1 + 80 + 2047) >> 11) << 1;

				vtocCode = (vtocSectorCount >> 1) + 2;
			}
		} else {
			// $02 code is used in DD until we exceed 1023 sectors, where
			// extended sector links are needed
			if (sectorCount >= 1024) {
				vtocSectorCount = (sectorCount + 1 + 80 + 2047) >> 11;
				vtocCode = vtocSectorCount + 2;
			}
		}

	} else if (mbDOS25)
		vtocSectorCount = 2;
	else if (mbDOS1)
		vtocCode = 0x01;

	mVTOCBitmap.resize(vtocSectorCount * mSectorSize, 0);

	mVTOCBitmap[0] = vtocCode;

	// mark all sectors as free; note that bit 0 is invalid sector 0
	memset(&mVTOCBitmap[10], 0xFF, (sectorCount + 1) >> 3);

	// handle leftovers
	if (uint32 finalSectors = (sectorCount + 1) & 7)
		mVTOCBitmap[10 + ((sectorCount + 1) >> 3)] = (uint8)(0x100 - (0x100 >> finalSectors));

	// allocate sector 0 (invalid) and sectors 1-3 (boot)
	for(uint32 i=0; i<(uint32)(dos1 ? 2 : 4); ++i)
		AllocateSector(i);

	// allocate root directory sectors
	for(uint32 i=0; i<8; ++i)
		AllocateSector(361 + i);

	// allocate VTOC sectors
	if (mbDOS25)
		AllocateSector(360);
	else {
		for(uint32 i=0; i<vtocSectorCount; ++i)
			AllocateSector(360 - i);
	}

	// set total available sector count (free sector count will be updated on flush)
	uint32 freeSectors = sectorCount - vtocSectorCount - 11;

	// allocate unusable sector 720 if DOS 2.x
	if (!mbMyDOS) {
		AllocateSector(720);
		--freeSectors;
	}

	// set total available sectors
	if (mbDOS25)
		VDWriteUnalignedLEU16(&mVTOCBitmap[1], 707);
	else
		VDWriteUnalignedLEU16(&mVTOCBitmap[1], freeSectors);

	// initialize boot sectors
	uint8 buf[0x180] = {};

	VDASSERTCT(sizeof(g_ATResDOSBootSector) < sizeof(buf));

	memcpy(buf, g_ATResDOSBootSector, sizeof g_ATResDOSBootSector);

	for(int i=0; i<3; ++i)
		image->WriteVirtualSector(i, buf + 128*i, 128);

	mTempSectorMap.resize(sectorCount, 0);
}

void ATDiskFSDOS2::Init(IATDiskImage *image, bool readOnly) {
	mpImage = image;
	mbDirty = false;
	mbReadOnly = readOnly;
	mbMyDOS = false;
	mbDOS1 = false;
	mbDOS25 = false;

	uint32 sectorSize = image->GetSectorSize();
	if (sectorSize != 128 && sectorSize != 256)
		throw MyError("Unsupported sector size for DOS 2.x image: %d bytes.", sectorSize);

	mSectorSize = sectorSize;

	// read VTOC
	mpImage->ReadVirtualSector(359, mSectorBuffer, sectorSize);

	// Check if the VTOC sector count is valid -- the VTOC starts on sector 360, so it cannot be
	// larger than 357 sectors. In double density, this is always fine, but in single density,
	// a high page count can exceed the available space. The VTOC limit in SD is 357 sectors, or
	// 177 pages. However, 33 pages are the maximum ever needed, since that is enough to
	// accommodate the maximum addressable size disk of 65535 sectors.
	if (mSectorBuffer[0] == 0 || mSectorBuffer[0] > 2 + 33)
		throw MyError("Invalid DOS 1.x/2.x/MyDOS disk (unrecognized VTOC signature).");


	mbDOS1 = (mSectorBuffer[0] == 1);

	uint32 numVTOCPages = mSectorBuffer[0] >= 2 ? mSectorBuffer[0] - 2 : 0;

	mVTOCBitmap.resize(numVTOCPages ? 256*numVTOCPages : mSectorSize, 0);

	memcpy(&mVTOCBitmap[0], mSectorBuffer, sectorSize);

	const uint32 numVTOCExtraSectors = numVTOCPages ? (mSectorSize > 128 ? numVTOCPages : numVTOCPages*2) - 1 : 0;

	for(uint32 i=0; i<numVTOCExtraSectors; ++i)
		mpImage->ReadVirtualSector(358 - i, &mVTOCBitmap[mSectorSize * (i+1)], mSectorSize);

	// check for DOS 2.5
	const uint32 sectorCount = mpImage->GetVirtualSectorCount();
	if (sectorCount == 1040 && mSectorSize == 128) {
		if (mVTOCBitmap[0] == 2) {
			mbDOS25 = true;

			// Ack... it's DOS 2.5. Extend the VTOC bitmap to two sectors and read in
			// the second sector's bitmap.
			mVTOCBitmap.resize(256, 0);

			mpImage->ReadVirtualSector(1023, mSectorBuffer, 128);
			memcpy(&mVTOCBitmap[100], mSectorBuffer + 84, 38);
		}
	}

	// check for MyDOS
	if (!mbDOS25 && !mbDOS1) {
		if (mVTOCBitmap[0] > 2)
			mbMyDOS = true;
		else if (sectorCount > 720)
			mbMyDOS = true;
		else if (sectorCount == 720 && VDReadUnalignedLEU16(&mVTOCBitmap[1]) == 708)
			mbMyDOS = true;
	}

	// for MyDOS, check if the VTOC page count is too low
	if (mbMyDOS) {
		uint32 minVTOCSize = 128;
		
		// standard VTOC sector can hold up to 944 sector bits before we start
		// overflowing to pages (paired sectors in SD)
		if (sectorCount > 944)
			minVTOCSize = (((sectorCount + 8) >> 3) + 10 + 255) & ~255;

		if (mVTOCBitmap.size() < minVTOCSize) {
			mVTOCBitmap.resize(minVTOCSize, 0);

			mbReadOnly = true;
		}
	}

	// invalidate directory cache
	mDirectoryStart = 0;
	mbDirectoryDirty = false;
	memset(mDirectory, 0, sizeof mDirectory);

	mTempSectorMap.resize(sectorCount, 0);
}

void ATDiskFSDOS2::GetInfo(ATDiskFSInfo& info) {
	info.mFSType = mbMyDOS ? "MyDOS"
		: mbDOS25 ? "Atari DOS 2.5"
		: mbDOS1 ? "Atari DOS 1.x"
		: mSectorSize == 256 ? "Atari DOS 2.0D"
		: "Atari DOS 2.0S";
	info.mFreeBlocks = CountFreeSectors();
	info.mBlockSize = mSectorSize;
}

void ATDiskFSDOS2::SetReadOnly(bool readOnly) {
	mbReadOnly = readOnly;
}

bool ATDiskFSDOS2::Validate(ATDiskFSValidationReport& report) {
	bool errorsFound = false;

	report = {};

	const uint32 sectorCount = mpImage->GetVirtualSectorCount();
	const uint32 sectorSize = mpImage->GetSectorSize();

	vdfastvector<uint8> newVTOC(mVTOCBitmap);

	uint32 totalMaskBits = 720;
	uint32 sectorsAvailable = mbDOS1 ? 709 + 8 : 707 + 8;

	if (mbDOS25) {
		totalMaskBits = 1024;
		sectorsAvailable = 1010 + 8;
	} else if (mbMyDOS) {
		totalMaskBits = sectorCount + 1;
		sectorsAvailable = totalMaskBits - 5;
	}

	// mark all sectors as free
	memset(&newVTOC[10], 0xFF, totalMaskBits >> 3);
	if (totalMaskBits & 7)
		newVTOC[10 + (totalMaskBits >> 3)] = (uint8)(0U - (0x100 >> (totalMaskBits & 7)));

	// allocate sectors 0 (reserved) and 1-3 (boot) -> 716 (only 1 boot sector for DOS 1.x)
	newVTOC[10] = mbDOS1 ? 0x3f : 0x0f;

	// allocate sector 360 (VTOC)
	newVTOC[55] = 0x7f;

	// set signature byte
	newVTOC[0] = mbDOS1 ? 1 : 2;

	if (mbMyDOS) {
		// see InitNew() for the logic behind MyDOS VTOC allocation
		if (sectorCount > (mSectorSize > 128 ? 1023U : 943U)) {
			const uint32 vtocExtraSectors = mVTOCBitmap.size() / mSectorSize - 1;

			for(uint32 i=0; i<vtocExtraSectors; ++i) {
				uint32 sector = 359 - i;

				newVTOC[10 + (sector >> 3)] &= ~(0x80U >> (sector & 7));
			}

			sectorsAvailable -= vtocExtraSectors;

			// set signature byte
			newVTOC[0] = (uint8)((mVTOCBitmap.size() >> 8) + 2);
		}
	} else if (mbDOS25) {
		// DOS 2.x can't use sector 720, and DOS 2.5 premarks it as allocated in ED
		newVTOC[100] = 0x7F;
	}

	vdfastvector<uint32> directoryStack;
	uint8 secBuf2[512];

	directoryStack.push_back(361);

	while(!directoryStack.empty()) {
		const uint32 directoryStart = directoryStack.back();
		directoryStack.pop_back();

		// allocate the directory sectors
		bool dirValid = true;

		for(uint32 i=0; i<8; ++i) {
			uint32 sector = directoryStart + i;
			uint8 maskBit = (0x80U >> (sector & 7));
			uint8& maskByte = newVTOC[10 + (sector >> 3)];

			if (maskByte & maskBit) {
				maskByte -= maskBit;
				--sectorsAvailable;
			} else {
				report.mbBrokenFiles = true;
				errorsFound = true;
				dirValid = false;
				break;
			}
		}

		if (!dirValid)
			continue;

		for(uint32 i=0; i<64; ++i) {
			if (!(i & 7)) {
				// Even on DD, only the first 128 bytes are used.
				mpImage->ReadVirtualSector(directoryStart + (i >> 3) - 1, mSectorBuffer, 128);
			}

			DirEnt de;
			DecodeDirEnt(de, mSectorBuffer + 16*(i & 7));

			// check for end of directory
			if (!de.mFlags)
				break;

			// check if this is a subdirectory
			if (de.mFlags & DirEnt::kFlagSubDir) {
				// check if starting sector is plausible
				if (de.mFirstSector > 3 && (uint32)de.mFirstSector + 7 < sectorCount) {
					directoryStack.push_back(de.mFirstSector);
				} else {
					report.mbBrokenFiles = true;
					errorsFound = true;
				}

				continue;
			}

			if (!IsVisible(de)) {
				// check for open files
				if (de.mFlags & DirEnt::kFlagOpenWrite) {
					report.mbOpenWriteFiles = true;
					errorsFound = true;
					break;
				}

				continue;
			}

			const bool isDOS1File = IsDOS1File(de);
			const bool extendedAddressing = IsExtendedAddressingUsed(de);
			uint32 sector = de.mFirstSector;

			try {
				while(sector) {
					if (sector > sectorCount) {
						report.mbBrokenFiles = true;
						errorsFound = true;
						break;
					}

					uint8& vtocByte = newVTOC[10 + (sector >> 3)];
					const uint8& vtocBit = 0x80 >> (sector & 7);
					if (!(vtocByte & vtocBit)) {
						report.mbBrokenFiles = true;
						errorsFound = true;
						break;
					}
					
					// allocate sector
					vtocByte &= ~vtocBit;
					--sectorsAvailable;

					if (sectorSize != mpImage->ReadVirtualSector(sector - 1, secBuf2, sectorSize)) {
						report.mbBrokenFiles = true;
						errorsFound = true;
						break;
					}

					GetSectorDataBytes(isDOS1File, secBuf2);
					sector = GetNextSector(isDOS1File, extendedAddressing, i, secBuf2);
				}
			} catch(const ATDiskFSException& ) {
				report.mbBrokenFiles = true;
				errorsFound = true;
			}
		}
	}

	// deal with DOS 2.5's annoying split VTOC
	if (mbDOS25) {
		VDWriteUnalignedLEU16(&newVTOC[3], CountFreeSectors(&newVTOC[10], 720));

		if (128 != mpImage->ReadVirtualSector(1023, mSectorBuffer, 128)) {
			report.mbBitmapIncorrect = true;
			errorsFound = true;
		}

		if (memcmp(&newVTOC[100], mSectorBuffer + 84, 38)) {
			report.mbBitmapIncorrect = true;
			errorsFound = true;
		}

		uint32 highFreeSectors = CountFreeSectors(&newVTOC[100], 304);
		if (highFreeSectors != VDReadUnalignedLEU16(&mSectorBuffer[122])) {
			report.mbBitmapIncorrect = true;
			errorsFound = true;
		}
	} else
		VDWriteUnalignedLEU16(&newVTOC[3], sectorsAvailable);

	if (newVTOC != mVTOCBitmap) {
		report.mbBitmapIncorrect = true;
		errorsFound = true;
	}

	return !errorsFound;
}

void ATDiskFSDOS2::Flush() {
	if (!mbDirty || mbReadOnly)
		return;

	FlushDirectoryCache();

	// Update VTOC
	if (mbDOS25) {
		// recompute free sector count for VTOC1
		VDWriteUnalignedLEU16(&mVTOCBitmap[3], CountFreeSectors(&mVTOCBitmap[10], 720));

		// reform VTOC1 in sector buffer
		memcpy(mSectorBuffer, &mVTOCBitmap[0], 100);
		memset(mSectorBuffer + 100, 0, 28);

		// write VTOC1
		mpImage->WriteVirtualSector(359, &mSectorBuffer[0], 128);

		// copy bits for sectors 48-1023 to VTOC2
		memcpy(mSectorBuffer, &mVTOCBitmap[16], 122);

		// recompute free sector count for VTOC2
		VDWriteUnalignedLEU16(&mSectorBuffer[122], CountFreeSectors(&mVTOCBitmap[100], 1024 - 720));

		// write VTOC2
		mpImage->WriteVirtualSector(1023, mSectorBuffer, 128);
	} else {
		uint32 numVTOCBitmap = mVTOCBitmap.size() / mSectorSize;

		// recompute free sector count
		VDWriteUnalignedLEU16(&mVTOCBitmap[3], CountFreeSectors(&mVTOCBitmap[10], mpImage->GetVirtualSectorCount() + (mbMyDOS ? 1 : 0)));

		for(uint32 i=0; i<numVTOCBitmap; ++i)
			mpImage->WriteVirtualSector(359 - i, &mVTOCBitmap[mSectorSize * i], mSectorSize);
	}

	mbDirty = false;
}

ATDiskFSFindHandle ATDiskFSDOS2::FindFirst(ATDiskFSKey key, ATDiskFSEntryInfo& info) {
	// determine the starting sector
	uint32 directoryStart = 361;

	if (key != ATDiskFSKey::None) {
		// load the parent directory
		LoadDirectoryByStart((uint32)key >> 6);

		// check the directory entry to see if it's actually a dir
		const DirEnt& de = mDirectory[(uint32)key & 63];
		if (!(de.mFlags & DirEnt::kFlagSubDir))
			return ATDiskFSFindHandle::Invalid;

		directoryStart = de.mFirstSector;
	}

	vdautoptr<FindHandle> h(new FindHandle);
	h->mPos = 0;
	h->mDirectoryStart = directoryStart;

	if (!FindNext((ATDiskFSFindHandle)(uintptr)h.get(), info)) {
		return ATDiskFSFindHandle::Invalid;
	}

	return (ATDiskFSFindHandle)(uintptr)h.release();
}

bool ATDiskFSDOS2::FindNext(ATDiskFSFindHandle searchKey, ATDiskFSEntryInfo& info) {
	FindHandle *h = (FindHandle *)searchKey;

	// reload the directory, just in case something happened in the meantime
	LoadDirectoryByStart(h->mDirectoryStart);

	while(h->mPos < 64) {
		const DirEnt& de = mDirectory[h->mPos++];

		if (!de.mFlags)
			break;

		if (!IsVisible(de))
			continue;

		GetFileInfo((ATDiskFSKey)((h->mDirectoryStart << 6) + h->mPos - 1), info);
		return true;
	}

	return false;
}

void ATDiskFSDOS2::FindEnd(ATDiskFSFindHandle searchKey) {
	delete (FindHandle *)searchKey;
}

void ATDiskFSDOS2::GetFileInfo(ATDiskFSKey key, ATDiskFSEntryInfo& info) {
	LoadDirectoryByStart((uint32)key >> 6);

	const DirEnt& de = mDirectory[(uint32)key & 63];

	int nameLen = 8;
	int extLen = 3;
	while(nameLen && de.mName[nameLen - 1] == ' ')
		--nameLen;

	while(extLen && de.mName[extLen + 7] == ' ')
		--extLen;

	info.mFileName	= de.mName;
	info.mSectors	= de.mSectorCount;
	info.mBytes		= de.mBytes;
	info.mKey		= key;
	info.mbIsDirectory = false;
	info.mbDateValid = false;

	if (mbMyDOS && (de.mFlags & DirEnt::kFlagSubDir)) {
		info.mbIsDirectory = true;
		info.mSectors = 8;
		info.mBytes = 8 * mSectorSize;
	}
}

ATDiskFSKey ATDiskFSDOS2::GetParentDirectory(ATDiskFSKey dirKey) {
	auto it = mDirStartToKeyMap.find((uint32)dirKey >> 6);

	if (it != mDirStartToKeyMap.end())
		return (ATDiskFSKey)it->second;

	return ATDiskFSKey::None;
}

ATDiskFSKey ATDiskFSDOS2::LookupFile(ATDiskFSKey parentKey, const char *filename) {
	LoadDirectory(parentKey);

	return LookupEntry(filename);
}

ATDiskFSKey ATDiskFSDOS2::LookupEntry(const char *filename) const {
	for(uint32 i=0; i<64; ++i) {
		const DirEnt& de = mDirectory[i];

		// The first entry with flags=$00 is the end of the directory.
		if (!de.mFlags)
			break;

		if (!IsVisible(de))
			break;

		if (!vdstricmp(de.mName, filename))
			return (ATDiskFSKey)(((uintptr)mDirectoryStart << 6) + (uintptr)(i + 1));
	}

	return ATDiskFSKey::None;
}

void ATDiskFSDOS2::DeleteFile(ATDiskFSKey key) {
	if (mbReadOnly)
		throw ATDiskFSException(kATDiskFSError_ReadOnly);

	if (key == ATDiskFSKey::None)
		return;

	LoadDirectoryByStart((uint32)key >> 6);

	uint8 fileId = (uint8)key & 63;
	DirEnt& de = mDirectory[fileId];

	if (!IsVisible(de))
		return;

	vdfastvector<uint32> sectorsToFree;

	if (de.mFlags & DirEnt::kFlagSubDir) {
		// temporarily load the subdirectory and check if it is empty
		LoadDirectory(key);

		for(const DirEnt& de2 : mDirectory) {
			if (!de2.mFlags)
				break;

			if (IsVisible(de2))
				throw ATDiskFSException(kATDiskFSError_DirectoryNotEmpty);
		}

		// reload the parent directory
		LoadDirectoryByStart((uint32)key >> 6);

		if ((uint32)(de.mFirstSector + 8) > mpImage->GetVirtualSectorCount())
			throw ATDiskFSException(kATDiskFSError_CorruptedFileSystem);

		sectorsToFree.reserve(8);

		for(int i=0; i<8; ++i) {
			uint32 sector = de.mFirstSector + i;

			if (!IsSectorAllocated(sector))
				throw ATDiskFSException(kATDiskFSError_CorruptedFileSystem);

			sectorsToFree.push_back(sector);
		}

		mDirStartToKeyMap.erase(de.mFirstSector);
	} else {
		std::fill(mTempSectorMap.begin(), mTempSectorMap.end(), 0);

		const bool extendedAddressing = IsExtendedAddressingUsed(de);
		const bool isDOS1File = IsDOS1File(de);
		uint32 sector = de.mFirstSector;
		while(sector) {
			if (sector > mTempSectorMap.size())
				throw ATDiskFSException(kATDiskFSError_CorruptedFileSystem);

			if (!IsSectorAllocated(sector))
				throw ATDiskFSException(kATDiskFSError_CorruptedFileSystem);

			if (mTempSectorMap[sector - 1])
				throw ATDiskFSException(kATDiskFSError_CorruptedFileSystem);

			mTempSectorMap[sector - 1] = 1;

			if (mSectorSize != mpImage->ReadVirtualSector(sector - 1, mSectorBuffer, mSectorSize))
				throw ATDiskFSException(kATDiskFSError_CorruptedFileSystem);

			sectorsToFree.push_back(sector);

			GetSectorDataBytes(isDOS1File, mSectorBuffer);
			sector = GetNextSector(isDOS1File, extendedAddressing, fileId, mSectorBuffer);
		}
	}

	// free sectors
	while(!sectorsToFree.empty()) {
		uint32 sec = sectorsToFree.back();
		sectorsToFree.pop_back();

		FreeSector(sec);
	}

	// free directory entry
	de.mFlags &= ~DirEnt::kFlagInUse;
	de.mFlags &= ~DirEnt::kFlagOpenWrite;
	de.mFlags &= ~DirEnt::kFlagSubDir;
	de.mFlags |= DirEnt::kFlagDeleted;

	mbDirectoryDirty = true;
	mbDirty = true;
}

void ATDiskFSDOS2::ReadFile(ATDiskFSKey key, vdfastvector<uint8>& dst) {
	VDASSERT(key != ATDiskFSKey::None);

	LoadDirectoryByStart((uint32)key >> 6);

	const uint8 fileId = (uint8)((uint32)key & 63);
	const DirEnt& de = mDirectory[fileId];

	uint32 sector = de.mFirstSector;

	std::fill(mTempSectorMap.begin(), mTempSectorMap.end(), 0);

	dst.clear();

	const bool isDOS1File = IsDOS1File(de);
	const bool extendedAddressing = IsExtendedAddressingUsed(de);
	while(sector) {
		if (sector > mTempSectorMap.size())
			throw ATDiskFSException(kATDiskFSError_CorruptedFileSystem);

		if (mTempSectorMap[sector - 1] || !IsSectorAllocated(sector))
			throw ATDiskFSException(kATDiskFSError_CorruptedFileSystem);

		mTempSectorMap[sector - 1] = 1;

		if (mSectorSize != mpImage->ReadVirtualSector(sector - 1, mSectorBuffer, mSectorSize))
			throw ATDiskFSException(kATDiskFSError_CorruptedFileSystem);

		const uint8 sectorDataBytes = GetSectorDataBytes(isDOS1File, mSectorBuffer);

		dst.insert(dst.end(), mSectorBuffer, mSectorBuffer + sectorDataBytes);

		sector = GetNextSector(isDOS1File, extendedAddressing, fileId, mSectorBuffer);
	}
}

ATDiskFSKey ATDiskFSDOS2::WriteFile(ATDiskFSKey parentKey, const char *filename, const void *src, uint32 len) {
	if (mbReadOnly)
		throw ATDiskFSException(kATDiskFSError_ReadOnly);

	if (!IsValidFileName(filename))
		throw ATDiskFSException(kATDiskFSError_InvalidFileName);

	// load directory and check for a duplicate file
	if (LookupFile(parentKey, filename) != ATDiskFSKey::None)
		throw ATDiskFSException(kATDiskFSError_FileExists);

	// find an empty directory entry
	uint32 dirIdx = 0;
	for(;;) {
		if (!IsVisible(mDirectory[dirIdx]))
			break;

		++dirIdx;
		if (dirIdx >= 64)
			throw ATDiskFSException(kATDiskFSError_DirectoryFull);
	}

	// find free sectors
	const uint32 sectorSize = mpImage->GetSectorSize();
	const uint32 dataBytesPerSector = sectorSize - 3;

	bool highFile = false;
	uint32 maxSector = 0;

	if (mbMyDOS)
		maxSector = mpImage->GetVirtualSectorCount();
	else if (mbDOS25)
		maxSector = 1023;
	else
		maxSector = 719;

	vdfastvector<uint32> sectorsToUse;

	// force at least one data sector for zero byte files (DOS 2.0S / MyDOS behavior)
	uint32 sectorCount = len ? (len + dataBytesPerSector - 1) / dataBytesPerSector : 1;
	uint32 sectorsToAllocate = sectorCount;
	for(uint32 i = 1; i <= maxSector; ++i) {
		if (mVTOCBitmap[10 + (i >> 3)] & (0x80 >> (i & 7))) {
			sectorsToUse.push_back(i);

			// check for DOS 2.5 high file
			if (i >= 720)
				highFile = true;

			if (!--sectorsToAllocate)
				break;
		}
	}

	if (sectorsToAllocate)
		throw ATDiskFSException(kATDiskFSError_DiskFull);

	// check if we should use 16-bit sector addressing
	bool extFile = mbMyDOS && mVTOCBitmap[0] > 2;

	// write data sectors -- we always use DOS 1 format if this was originally a DOS 1 disk
	for(uint32 i=0; i<sectorCount; ++i) {
		uint32 offset = dataBytesPerSector*i;
		uint32 dataBytes = len - offset;

		if (dataBytes > dataBytesPerSector)
			dataBytes = dataBytesPerSector;

		memcpy(mSectorBuffer, (const char *)src + offset, dataBytes);
		memset(mSectorBuffer + dataBytes, 0, (sectorSize - 3) - dataBytes);

		uint32 nextSector = (i == sectorCount - 1) ? 0 : sectorsToUse[i + 1];

		if (extFile)
			mSectorBuffer[sectorSize - 3] = (nextSector >> 8);
		else
			mSectorBuffer[sectorSize - 3] = (dirIdx << 2) + (nextSector >> 8);

		mSectorBuffer[sectorSize - 2] = (uint8)nextSector;

		// For DOS 1.x, the last byte holds either the sector index in the file with bit 7
		// cleared or the number of bytes in the last sctor with bit 7 set.
		//
		// For DOS 2.x/MyDOS, the last byte holds the number of bytes in the sector, up to
		// either 125 for SD or 253 for DD.
		if (!mbDOS1)
			mSectorBuffer[sectorSize - 1] = dataBytes;
		else if (nextSector)
			mSectorBuffer[sectorSize - 1] = i & 0x7F;
		else
			mSectorBuffer[sectorSize - 1] = dataBytes | 0x80;

		uint32 sector = sectorsToUse[i];
		mpImage->WriteVirtualSector(sector - 1, mSectorBuffer, sectorSize);

		VDASSERT(!IsSectorAllocated(sector));
		AllocateSector(sector);
	}

	// write directory entry
	DirEnt& de = mDirectory[dirIdx];
	de.mFlags = mbDOS1 ? DirEnt::kFlagInUse : DirEnt::kFlagDOS2 | DirEnt::kFlagInUse;

	if (mbDOS25) {
		if (highFile)
			de.mFlags = DirEnt::kFlagDOS2 | DirEnt::kFlagOpenWrite;
	} else if (mbMyDOS) {
		if (extFile)
			de.mFlags |= DirEnt::kFlagExtFile;
	}

	de.mBytes = len;
	de.mSectorCount = sectorCount;
	de.mFirstSector = sectorCount ? sectorsToUse.front() : 0;

	WriteFileName(de, filename);

	mbDirty = true;
	mbDirectoryDirty = true;

	return (ATDiskFSKey)(mDirectoryBaseKey + dirIdx);
}

void ATDiskFSDOS2::RenameFile(ATDiskFSKey key, const char *filename) {
	if (mbReadOnly)
		throw ATDiskFSException(kATDiskFSError_ReadOnly);

	if (!IsValidFileName(filename))
		throw ATDiskFSException(kATDiskFSError_InvalidFileName);

	// load directory and look for conflicting name
	LoadDirectoryByStart((uint32)key >> 6);

	ATDiskFSKey conflictingKey = LookupEntry(filename);

	// check if we're renaming a file/dir to itself
	if (conflictingKey == key)
		return;

	if (conflictingKey != ATDiskFSKey::None)
		throw ATDiskFSException(kATDiskFSError_FileExists);

	WriteFileName(mDirectory[(uint32)key & 63], filename);
	mbDirty = true;
	mbDirectoryDirty = true;
}

ATDiskFSKey ATDiskFSDOS2::CreateDir(ATDiskFSKey parentKey, const char *filename) {
	if (!mbMyDOS)
		throw ATDiskFSException(kATDiskFSError_NotSupported);

	if (mbReadOnly)
		throw ATDiskFSException(kATDiskFSError_ReadOnly);

	if (!IsValidFileName(filename))
		throw ATDiskFSException(kATDiskFSError_InvalidFileName);
	
	// load directory and look for conflicting name
	ATDiskFSKey conflictingKey = LookupFile(parentKey, filename);

	if (conflictingKey != ATDiskFSKey::None)
		throw ATDiskFSException(kATDiskFSError_FileExists);

	// find an empty directory entry
	uint32 dirIdx = 0;
	for(;;) {
		if (!IsVisible(mDirectory[dirIdx]))
			break;

		++dirIdx;
		if (dirIdx >= 64)
			throw ATDiskFSException(kATDiskFSError_DirectoryFull);
	}

	// scan the VTOC for a *contiguous* set of 8 sectors
	uint32 searchLen = ((mpImage->GetVirtualSectorCount() + (mbMyDOS ? 1 : 0)) >> 3) - 1;
	uint8 *vtocptr = &mVTOCBitmap[10];
	uint32 start = 1;

	while(searchLen--) {
		uint16 vtocBits = VDReadUnalignedBEU16(vtocptr);
		uint16 mask = 0x7F80;

		for(int i=0; i<8; ++i) {
			if ((mask & vtocBits) == mask) {
				VDASSERT(!IsSectorAllocated(start + i));

				// allocate the sectors and exit
				VDWriteUnalignedBEU16(vtocptr, vtocBits - mask);
				goto found;
			}

			++start;
			mask >>= 1;
		}

		++vtocptr;
	}

	// check whether there were 8 sectors free anywhere on the disk
	if (CountFreeSectors() >= 8)
		throw ATDiskFSException(kATDiskFSError_DiskFullFragmented);
	else
		throw ATDiskFSException(kATDiskFSError_DiskFull);

found:
	// allocate the sectors
	for(int i=0; i<8; ++i)
		AllocateSector(start + i);

	// write new directory entry
	DirEnt& de = mDirectory[dirIdx];

	WriteFileName(de, filename);
	de.mFlags = DirEnt::kFlagSubDir;
	de.mFirstSector = start;
	de.mSectorCount = 8;
	de.mBytes = 8*mSectorSize;

	mbDirectoryDirty = true;
	mbDirty = true;

	// clear all directory sectors
	memset(mSectorBuffer, 0, sizeof mSectorBuffer);

	for(int i=0; i<8; ++i)
		mpImage->WriteVirtualSector(start - 1 + i, mSectorBuffer, mSectorSize);

	return (ATDiskFSKey)(mDirectoryBaseKey + dirIdx);
}

void ATDiskFSDOS2::DecodeDirEnt(DirEnt& de, const uint8 *src) const {
	de.mFlags = src[0];
	de.mFirstSector = VDReadUnalignedLEU16(src + 3);

	const uint8 *fnstart = src + 5;
	const uint8 *fnend = src + 13;

	while(fnend != fnstart && fnend[-1] == 0x20)
		--fnend;

	char *namedst = de.mName;
	while(fnstart != fnend)
		*namedst++ = *fnstart++;

	const uint8 *extstart = src + 13;
	const uint8 *extend = src + 16;

	while(extend != extstart && extend[-1] == 0x20)
		--extend;

	if (extstart != extend) {
		*namedst++ = '.';

		while(extstart != extend)
			*namedst++ = *extstart++;
	}

	*namedst = 0;

	// The sector count in the directory can be WRONG, so we recompute it
	// from the sector chain.
	de.mSectorCount = 0;
	de.mBytes = 0;
}

bool ATDiskFSDOS2::IsVisible(const DirEnt& de) const {
	if (!de.mFlags)
		return false;

	if (mbMyDOS) {
		if (de.mFlags & DirEnt::kFlagSubDir)
			return true;
	}

	if (de.mFlags & DirEnt::kFlagDeleted)
		return false;

	// check for special DOS 2.5 case
	if (mbDOS25) {
		if ((de.mFlags & (DirEnt::kFlagInUse | DirEnt::kFlagOpenWrite | DirEnt::kFlagDOS2))
			== (DirEnt::kFlagOpenWrite | DirEnt::kFlagDOS2))
		{
			return true;
		}
	}

	// reject files that are open for write -- DOS 2.x does not show these
	if (de.mFlags & DirEnt::kFlagOpenWrite)
		return false;

	// reject files that are not valid
	if (!(de.mFlags & DirEnt::kFlagInUse))
		return false;

	return true;
}

bool ATDiskFSDOS2::IsDOS1File(const DirEnt& de) const {
	return !(de.mFlags & DirEnt::kFlagDOS2);
}

bool ATDiskFSDOS2::IsExtendedAddressingUsed(const DirEnt& de) const {
	return mbMyDOS && (de.mFlags & DirEnt::kFlagExtFile);
}

uint8 ATDiskFSDOS2::GetSectorDataBytes(bool isDOS1File, const uint8 *secBuf) const {
	// In DOS 1.x, the last byte holds the sector index instead of the byte count for all
	// but the last sector. However, we can't use the DOS 1 detection for this as DOS 2
	// can write v2 files onto a DOS 1 disk -- we have to use the flag from the directory.
	// We also cannot validate the sector index as DOS 1 doesn't either -- which is why
	// it can almost read DOS 2 files as-is. The sector index can be $7D for all sectors
	// and neither DOS 1.x won't care.
	//
	// The high bit is important as it signals the end of the file, not the sector link --
	// if the link is 0 and the high bit is not set, DOS 1.x attempts to read sector 0.
	// This can go unnoticed in BASIC since BASIC does a sized read and thus never causes
	// DOS 1.x to attempt to read past the end of a DOS 2.x file.
	if (isDOS1File) {
		if (!(secBuf[127] & 0x80)) {
			return 125;
		} else {
			if (secBuf[127] > 0xFD)
				throw ATDiskFSException(kATDiskFSError_CorruptedFileSystem);

			return secBuf[127] & 0x7F;
		}
	}

	const uint8 sectorDataBytes = mSectorSize > 128 ? secBuf[mSectorSize - 1] : secBuf[mSectorSize - 1] & 127;

	if (sectorDataBytes > mSectorSize - 3)
		throw ATDiskFSException(kATDiskFSError_CorruptedFileSystem);

	return sectorDataBytes;
}

uint32 ATDiskFSDOS2::GetNextSector(bool isDOS1File, bool extendedAddressing, uint8 fileId, const uint8 *secBuf) const {
	// On a DOS 1.x file, the file stops if bit 7 is set on the last byte even if
	// the sector link is 0.
	if (isDOS1File && (secBuf[127] & 0x80))
		return 0;

	if (!extendedAddressing) {
		const uint8 sectorFileId = secBuf[mSectorSize - 3] >> 2;

		if (fileId != sectorFileId)
			throw ATDiskFSException(kATDiskFSError_CorruptedFileSystem);
	}

	if (extendedAddressing)
		return ((uint32)secBuf[mSectorSize - 3] << 8) + secBuf[mSectorSize - 2];
	else
		return ((uint32)(secBuf[mSectorSize - 3] & 3) << 8) + secBuf[mSectorSize - 2];
}

bool ATDiskFSDOS2::IsSectorAllocated(uint32 sector) const {
	const uint32 mask = mVTOCBitmap[10 + (sector >> 3)];
	const uint8 bit = (0x80 >> (sector & 7));
	return !(mask & bit);
}

void ATDiskFSDOS2::AllocateSector(uint32 sector) {
	mVTOCBitmap[10 + (sector >> 3)] &= ~(0x80 >> (sector & 7));
}

void ATDiskFSDOS2::FreeSector(uint32 sector) {
	mVTOCBitmap[10 + (sector >> 3)] |= (0x80 >> (sector & 7));
}

uint32 ATDiskFSDOS2::CountFreeSectors() const {
	return CountFreeSectors(&mVTOCBitmap[10], mpImage->GetVirtualSectorCount() + (mbMyDOS ? 1 : 0));
}

uint32 ATDiskFSDOS2::CountFreeSectors(const uint8 *bitmap, uint32 bitcount) const {
	uint32 fullByteCount = bitcount >> 3;
	int totalFree = 0;

	while(fullByteCount--) {
		uint8 c = *bitmap++;

		totalFree += VDCountBits8(c);
	}

	if (bitcount & 7) {
		uint8 mask = (uint8)(0U - (0x100U >> (bitcount & 7)));

		totalFree += VDCountBits8(*bitmap & mask);
	}

	return (uint32)totalFree;
}

void ATDiskFSDOS2::WriteFileName(DirEnt& de, const char *filename) {
	for(int i=0; i<12; ++i) {
		uint8 c = filename[i];

		if ((uint8)(c - 0x61) < 26)
			c &= 0xdf;

		de.mName[i] = c;
		if (!c)
			break;
	}

	de.mName[12] = 0;
}

bool ATDiskFSDOS2::IsValidFileName(const char *filename) const {
	// can't be empty
	if (!*filename)
		return false;

	// first character must not be a number
	if (mbStrictNames) {
		if ((uint8)((uint8)*filename - 0x30) < 10)
			return false;
	}

	// up to 8 alphanumeric characters are allowed in the filename
	int count = 0;

	for(;;) {
		uint8 c = *filename;

		if ((uint8)(c - 0x30) >= 10 && (uint8)((c & 0xdf) - 0x41) >= 26) {
			// MyDOS also allows _ and @
			if ((mbStrictNames && !mbMyDOS) || (c != '@' && c != '_'))
				break;
		}

		++filename;

		if (++count > 8)
			return false;
	}

	// next needs to be EOS or a period
	if (!*filename)
		return true;

	if (*filename++ != '.')
		return false;

	// up to 3 alphanumeric characters may follow
	count = 0;

	for(;;) {
		uint8 c = *filename++;

		if ((uint8)(c - 0x30) >= 10 && (uint8)((c & 0xdf) - 0x41) >= 26) {
			if ((mbStrictNames && !mbMyDOS) || (c != '@' && c != '_'))
				break;
		}

		if (++count > 3)
			return false;
	}

	// looks OK
	return true;
}

void ATDiskFSDOS2::FlushDirectoryCache() {
	if (!mbDirectoryDirty)
		return;

	VDASSERT(mDirectoryStart);

	// Reform and rewrite directory sectors.
	const DirEnt *pde = mDirectory;
	const DirEnt *pdeEnd = mDirectory + vdcountof(mDirectory);

	while(pdeEnd != pde && !IsVisible(pdeEnd[-1]))
		--pdeEnd;

	uint32 maxSectors = std::min<uint32>(((uint32)(pdeEnd - pde) + 8) >> 3, 8);
	for(uint32 i=0; i<maxSectors; ++i) {
		memset(mSectorBuffer, 0, sizeof mSectorBuffer);

		for(uint32 j=0; j<8; ++j) {
			if (pde == pdeEnd)
				break;

			const DirEnt& de = *pde++;

			uint8 *rawde = &mSectorBuffer[16*j];

			if (!IsVisible(de)) {
				rawde[0] = DirEnt::kFlagDeleted;
				continue;
			}

			rawde[0] = de.mFlags;
			VDWriteUnalignedLEU16(rawde + 1, de.mSectorCount);
			VDWriteUnalignedLEU16(rawde + 3, de.mFirstSector);

			const char *ext = strchr(de.mName, '.');
			uint32 fnLen = ext ? (uint32)(ext - de.mName) : (uint32)strlen(de.mName);

			memset(rawde + 5, 0x20, 11);
			memcpy(rawde + 5, de.mName, fnLen);

			if (ext)
				memcpy(rawde + 13, ext + 1, strlen(ext + 1));
		}

		mpImage->WriteVirtualSector(mDirectoryStart - 1 + i, mSectorBuffer, mSectorSize);
	}

	mbDirectoryDirty = false;
}

void ATDiskFSDOS2::LoadDirectory(ATDiskFSKey key) {
	if (key == ATDiskFSKey::None)
		LoadDirectoryByStart(361);
	else {
		const uint32 parentDirStart = (uint32)key >> 6;
		LoadDirectoryByStart(parentDirStart);

		const uint32 newDirStart = mDirectory[(uint32)key & 63].mFirstSector;
		LoadDirectoryByStart(newDirStart);
	}
}

void ATDiskFSDOS2::LoadDirectoryByStart(uint32 directoryStart) {
	VDASSERT(directoryStart);

	if (mDirectoryStart == directoryStart)
		return;

	FlushDirectoryCache();

	mDirectoryStart = 0;
	mbDirectoryDirty = false;

	// initialize directory
	memset(mDirectory, 0, sizeof mDirectory);

	const uint32 sectorCount = mpImage->GetVirtualSectorCount();

	uint32 fileId = 0;
	uint8 secBuf2[512];
	for(uint32 dirSector = 0; dirSector < 8; ++dirSector) {
		if (mSectorSize != mpImage->ReadVirtualSector(directoryStart + dirSector - 1, mSectorBuffer, mSectorSize)) {
			fileId += 8;
			continue;
		}

		for(uint32 i = 0; i < 8; ++i, ++fileId) {
			const uint8 *dirent = mSectorBuffer + 16*i;
			const uint8 flags = dirent[0];

			if (!flags)
				goto directory_end;

			DirEnt& de = mDirectory[fileId];

			DecodeDirEnt(de, dirent);

			if (!IsVisible(de))
				continue;

			// Check if we have a MyDOS subdirectory
			if (mbMyDOS && (de.mFlags & DirEnt::kFlagSubDir)) {
				mDirStartToKeyMap[de.mFirstSector] = (directoryStart << 6) + fileId;
				de.mSectorCount = 8;
				continue;
			}

			// Recalculate the sector count, since it can be wrong
			std::fill(mTempSectorMap.begin(), mTempSectorMap.end(), 0);

			const bool isDOS1File = IsDOS1File(de);
			const bool extendedAddressing = IsExtendedAddressingUsed(de);
			uint32 sector = de.mFirstSector;

			try {
				for(;;) {
					if (!sector || sector > sectorCount)
						break;

					if (mTempSectorMap[sector - 1])
						break;

					mTempSectorMap[sector - 1] = 1;

					if (mSectorSize != mpImage->ReadVirtualSector(sector - 1, secBuf2, mSectorSize))
						break;

					const uint8 sectorDataBytes = GetSectorDataBytes(isDOS1File, secBuf2);
					sector = GetNextSector(isDOS1File, extendedAddressing, fileId, secBuf2);

					++de.mSectorCount;
					de.mBytes += sectorDataBytes;
				}
			} catch(const MyError&) {
			}
		}
	}

directory_end:
	mDirectoryStart = directoryStart;
	mDirectoryBaseKey = directoryStart << 6;
}

///////////////////////////////////////////////////////////////////////////

IATDiskFS *ATDiskFormatImageDOS1(IATDiskImage *image) {
	vdautoptr<ATDiskFSDOS2> fs(new ATDiskFSDOS2);

	fs->InitNew(image, false, true);

	return fs.release();
}

IATDiskFS *ATDiskFormatImageDOS2(IATDiskImage *image) {
	vdautoptr<ATDiskFSDOS2> fs(new ATDiskFSDOS2);

	fs->InitNew(image, false, false);

	return fs.release();
}

IATDiskFS *ATDiskFormatImageMyDOS(IATDiskImage *image) {
	vdautoptr<ATDiskFSDOS2> fs(new ATDiskFSDOS2);

	fs->InitNew(image, true, false);

	return fs.release();
}

IATDiskFS *ATDiskMountImageDOS2(IATDiskImage *image, bool readOnly) {
	vdautoptr<ATDiskFSDOS2> fs(new ATDiskFSDOS2);

	fs->Init(image, readOnly);

	return fs.release();
}
