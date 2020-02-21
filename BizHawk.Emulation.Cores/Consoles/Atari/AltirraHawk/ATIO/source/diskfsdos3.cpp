//	Altirra - Atari 800/800XL/5200 emulator
//	I/O library - Atari DOS 3.x filesystem handler
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
#include <vd2/system/binary.h>
#include <vd2/system/strutil.h>
#include <vd2/system/vdalloc.h>
#include <at/atio/diskfs.h>
#include <at/atio/diskimage.h>

class ATDiskFSDOS3 final : public IATDiskFS {
public:
	ATDiskFSDOS3();
	~ATDiskFSDOS3();

public:
	void InitNew(IATDiskImage *image);
	void Init(IATDiskImage *image, bool readOnly);
	void GetInfo(ATDiskFSInfo& info) override;

	bool IsReadOnly() override { return mbReadOnly; }
	void SetReadOnly(bool readOnly) override;
	void SetAllowExtend(bool allow) override {}
	void SetStrictNameChecking(bool strict) override { mbStrictNames = strict; }

	bool Validate(ATDiskFSValidationReport& report) override;
	void Flush() override;

	ATDiskFSFindHandle FindFirst(ATDiskFSKey key, ATDiskFSEntryInfo& info) override;
	bool FindNext(ATDiskFSFindHandle searchKey, ATDiskFSEntryInfo& info) override;
	void FindEnd(ATDiskFSFindHandle searchKey) override;

	void GetFileInfo(ATDiskFSKey key, ATDiskFSEntryInfo& info) override;
	ATDiskFSKey GetParentDirectory(ATDiskFSKey dirKey) override;

	ATDiskFSKey LookupFile(ATDiskFSKey parentKey, const char *filename) override;

	void DeleteFile(ATDiskFSKey key) override;
	void ReadFile(ATDiskFSKey key, vdfastvector<uint8>& dst) override;
	ATDiskFSKey WriteFile(ATDiskFSKey parentKey, const char *filename, const void *src, uint32 len) override;
	void RenameFile(ATDiskFSKey key, const char *newFileName) override;
	void SetFileTimestamp(ATDiskFSKey key, const VDExpandedDate& date) override {}
	ATDiskFSKey CreateDir(ATDiskFSKey parentKey, const char *filename) override;

protected:
	struct DirEnt;

	static void WriteFileName(DirEnt& de, const char *filename);
	static bool IsValidFileName(const char *filename);

	IATDiskImage *mpImage;
	bool mbDirty;
	bool mbReadOnly;
	bool mbStrictNames = true;
	uint32 mClusterCount;

	struct DirEnt {
		enum : uint8 {
			kFlagValid		= 0x80,		// directory entry exists
			kFlagActive		= 0x40,		// non-deleted file exists
			kFlagProtDeleted= 0x02		// read-only or deleted
		};

		uint8	mFlags;
		char	mName[13];
		uint8	mClusterCount;
		uint8	mClusterStart;
		uint32	mByteSize;
	};

	struct FindHandle {
		uint32	mPos;
	};

	DirEnt	mDirectory[63];
	uint8	mSectorBuffer[128];
	uint8	mAllocTableBuffer[128];

	vdfastvector<uint8> mTempSectorMap;
};

ATDiskFSDOS3::ATDiskFSDOS3() {
}

ATDiskFSDOS3::~ATDiskFSDOS3() {
}

void ATDiskFSDOS3::InitNew(IATDiskImage *image) {
	uint32 sectorSize = image->GetSectorSize();
	if (sectorSize != 128)
		throw MyError("Unsupported sector size for DOS 3.x image: %d bytes.", sectorSize);

	uint32 sectorCount = image->GetVirtualSectorCount();
	if (sectorCount != 720 && sectorCount != 1040)
		throw MyError("Unsupported disk size for DOS 3.x image: %u sectors.", sectorCount);

	mpImage = image;
	mbDirty = true;
	mbReadOnly = false;
	mClusterCount = (sectorCount - 24) >> 3;

	// initialize directory
	memset(mDirectory, 0, sizeof mDirectory);

	// initialize FAT
	memset(mAllocTableBuffer, 0, sizeof mAllocTableBuffer);
	memset(mAllocTableBuffer, 0xFE, mClusterCount);

	// initialize boot sector
	memset(mSectorBuffer, 0, sizeof mSectorBuffer);

	static const uint8 kBootSector[]={
		0x01,			// $01 boot flags
		0x09,			// $09 sectors
		0x00, 0x32,		// $3200
		0x06, 0x32,		// $3206
		0xA2, 0x00,		// LDX #$00
		0x38,			// SEC
		0x60,			// RTS
	};

	memcpy(mSectorBuffer, kBootSector, sizeof kBootSector);

	image->WriteVirtualSector(0, mSectorBuffer, 128);

	memset(mSectorBuffer, 0, sizeof mSectorBuffer);
	for(int i=1; i<9; ++i)
		image->WriteVirtualSector(i, mSectorBuffer, 128);
}

void ATDiskFSDOS3::Init(IATDiskImage *image, bool readOnly) {
	mpImage = image;
	mbDirty = false;
	mbReadOnly = readOnly;

	uint32 sectorSize = image->GetSectorSize();
	if (sectorSize != 128 && sectorSize != 256)
		throw MyError("Unsupported sector size for DOS 3.x image: %d bytes.", sectorSize);

	const uint32 sectorCount = mpImage->GetVirtualSectorCount();
	if (sectorCount != 720 && sectorCount != 1040)
		throw MyError("Unsupported disk size for DOS 3.x image: %u sectors.", sectorCount);
	
	mClusterCount = (sectorCount - 24) >> 3;

	// read FAT
	mpImage->ReadVirtualSector(23, mAllocTableBuffer, sectorSize);

	// initialize directory
	memset(mDirectory, 0, sizeof mDirectory);

	DirEnt *pde = mDirectory;

	for(uint32 dirSector = 15; dirSector < 23; ++dirSector) {
		if (sectorSize != mpImage->ReadVirtualSector(dirSector, mSectorBuffer, sectorSize))
			continue;

		for(uint32 i = (dirSector == 15 ? 1 : 0); i < 8; ++i) {
			const uint8 *dirent = mSectorBuffer + 16*i;
			const uint8 flags = dirent[0];

			if (!flags)
				goto directory_end;

			DirEnt& de = *pde++;

			de.mFlags = flags;
			de.mClusterCount = dirent[12];
			de.mClusterStart = dirent[13];
			de.mByteSize = VDReadUnalignedLEU16(dirent+14);

			// DOS 3 supports very large files, but ones that are 64 clusters or above may
			// have their byte size wrapped, and therefore we need to fix up the file size.
			de.mByteSize += (de.mClusterCount << 10) & ~0xffff;

			const uint8 *fnstart = dirent + 1;
			const uint8 *fnend = dirent + 9;

			while(fnend != fnstart && fnend[-1] == 0x20)
				--fnend;

			char *namedst = de.mName;
			while(fnstart != fnend)
				*namedst++ = *fnstart++;

			const uint8 *extstart = dirent + 9;
			const uint8 *extend = dirent + 12;

			while(extend != extstart && extend[-1] == 0x20)
				--extend;

			if (extstart != extend) {
				*namedst++ = '.';

				while(extstart != extend)
					*namedst++ = *extstart++;
			}

			*namedst = 0;
		}
	}

directory_end:
	;
}

void ATDiskFSDOS3::GetInfo(ATDiskFSInfo& info) {
	info.mFSType = "Atari DOS 3";
	info.mFreeBlocks = 0;

	for(uint32 i=0; i<mClusterCount; ++i)
		if (mAllocTableBuffer[i] == 0xFE)
			++info.mFreeBlocks;

	info.mBlockSize = 1024;
}

void ATDiskFSDOS3::SetReadOnly(bool readOnly) {
	mbReadOnly = readOnly;
}

bool ATDiskFSDOS3::Validate(ATDiskFSValidationReport& report) {
	report = {};

	bool clusterMarked[128] = {false};

	for(const DirEnt& de : mDirectory) {
		if (!de.mFlags)
			break;

		if (~de.mFlags & (DirEnt::kFlagActive | DirEnt::kFlagValid))
			continue;

		// mark allocation chain
		uint32 cluster = de.mClusterStart;

		for(;;) {
			if (cluster >= mClusterCount || clusterMarked[cluster])
				goto fail;

			clusterMarked[cluster] = true;

			cluster = mAllocTableBuffer[cluster];
			if (cluster == 0xFD)
				break;
		}
	}

	return true;

fail:
	report.mbBitmapIncorrect = true;
	return false;
}

void ATDiskFSDOS3::Flush() {
	if (!mbDirty || mbReadOnly)
		return;

	// Reform and rewrite directory sectors.
	uint32 sectorSize = mpImage->GetSectorSize();
	const DirEnt *pde = mDirectory;
	for(uint32 i = 0; i < 8; ++i) {
		memset(mSectorBuffer, 0, sizeof mSectorBuffer);

		// if this is the first sector, the first entry is a signature entry
		if (i == 0) {
			mSectorBuffer[14] = (uint8)mClusterCount;
			mSectorBuffer[15] = 0xA5;
		}

		for(uint32 j = (i ? 0 : 1); j < 8; ++j) {
			const DirEnt& de = *pde++;

			if (!(de.mFlags & (DirEnt::kFlagActive | DirEnt::kFlagValid)))
				continue;

			uint8 *rawde = &mSectorBuffer[16*j];

			rawde[0] = de.mFlags;

			const char *ext = strchr(de.mName, '.');
			uint32 fnLen = ext ? (uint32)(ext - de.mName) : 8;

			memset(rawde + 1, 0x20, 11);
			memcpy(rawde + 1, de.mName, fnLen);

			if (ext)
				memcpy(rawde + 9, ext + 1, strlen(ext + 1));

			rawde[12] = de.mClusterCount;
			rawde[13] = de.mClusterStart;

			// truncate the file size to 16 bits (yes, DOS 3 does this itself)
			VDWriteUnalignedLEU16(rawde + 14, (uint16)de.mByteSize);
		}

		mpImage->WriteVirtualSector(15 + i, mSectorBuffer, sectorSize);
	}

	// Update FAT
	mpImage->WriteVirtualSector(23, mAllocTableBuffer, sectorSize);

	mbDirty = false;
}

ATDiskFSFindHandle ATDiskFSDOS3::FindFirst(ATDiskFSKey key, ATDiskFSEntryInfo& info) {
	if (key != ATDiskFSKey::None)
		return ATDiskFSFindHandle::Invalid;

	FindHandle *h = new FindHandle;
	h->mPos = 0;

	if (!FindNext((ATDiskFSFindHandle)(uintptr)h, info)) {
		delete h;
		return ATDiskFSFindHandle::Invalid;
	}

	return (ATDiskFSFindHandle)(uintptr)h;
}

bool ATDiskFSDOS3::FindNext(ATDiskFSFindHandle searchKey, ATDiskFSEntryInfo& info) {
	FindHandle *h = (FindHandle *)searchKey;

	while(h->mPos < 64) {
		const DirEnt& de = mDirectory[h->mPos++];

		if (!de.mFlags)
			break;

		if (!(de.mFlags & DirEnt::kFlagActive))
			continue;

		GetFileInfo((ATDiskFSKey)h->mPos, info);
		return true;
	}

	return false;
}

void ATDiskFSDOS3::FindEnd(ATDiskFSFindHandle searchKey) {
	delete (FindHandle *)searchKey;
}

void ATDiskFSDOS3::GetFileInfo(ATDiskFSKey key, ATDiskFSEntryInfo& info) {
	const DirEnt& de = mDirectory[(uint32)key - 1];

	int nameLen = 8;
	int extLen = 3;
	while(nameLen && de.mName[nameLen - 1] == ' ')
		--nameLen;

	while(extLen && de.mName[extLen + 7] == ' ')
		--extLen;

	info.mFileName	= de.mName;
	info.mSectors	= de.mClusterCount * 8;
	info.mBytes		= de.mByteSize;
	info.mKey		= key;
	info.mbIsDirectory = false;
	info.mbDateValid = false;
}

ATDiskFSKey ATDiskFSDOS3::GetParentDirectory(ATDiskFSKey dirKey) {
	return ATDiskFSKey::None;
}

ATDiskFSKey ATDiskFSDOS3::LookupFile(ATDiskFSKey parentKey, const char *filename) {
	if (parentKey != ATDiskFSKey::None)
		return ATDiskFSKey::None;

	uint32 fileKey = 0;
	for(const DirEnt& de : mDirectory) {
		++fileKey;

		// The first entry with flags=$00 is the end of the directory.
		if (!de.mFlags)
			break;

		if (!(de.mFlags & DirEnt::kFlagActive))
			continue;

		if (!vdstricmp(de.mName, filename))
			return (ATDiskFSKey)fileKey;
	}

	return ATDiskFSKey::None;
}

void ATDiskFSDOS3::DeleteFile(ATDiskFSKey key) {
	if (mbReadOnly)
		throw ATDiskFSException(kATDiskFSError_ReadOnly);

	VDASSERT((uint32)key >= 1 && (uint32)key <= 63);

	uint8 fileId = (uint8)key - 1;
	DirEnt& de = mDirectory[fileId];

	if (!(de.mFlags & DirEnt::kFlagActive))
		return;

	vdfastvector<uint32> clustersToFree;

	uint32 cluster = de.mClusterStart;

	for(;;) {
		if (cluster >= mClusterCount)
			throw ATDiskFSException(kATDiskFSError_CorruptedFileSystem);

		uint32 nextCluster = mAllocTableBuffer[cluster];

		if (nextCluster >= mClusterCount && nextCluster != 0xFD)
			throw ATDiskFSException(kATDiskFSError_CorruptedFileSystem);

		auto it = std::lower_bound(clustersToFree.begin(), clustersToFree.end(), cluster);

		if (it != clustersToFree.end() && *it == cluster)
			throw ATDiskFSException(kATDiskFSError_CorruptedFileSystem);
				
		clustersToFree.insert(it, cluster);

		if (nextCluster == 0xFD)
			break;

		cluster = nextCluster;
	}

	// free clusters
	while(!clustersToFree.empty()) {
		uint32 cluster = clustersToFree.back();
		clustersToFree.pop_back();

		mAllocTableBuffer[cluster] = 0xFE;
	}

	// free directory entry
	de.mFlags &= ~DirEnt::kFlagActive;
	de.mFlags |= DirEnt::kFlagProtDeleted;

	mbDirty = true;
}

void ATDiskFSDOS3::ReadFile(ATDiskFSKey key, vdfastvector<uint8>& dst) {
	VDASSERT((uint32)key >= 1 && (uint32)key <= 63);

	const uint8 fileId = (uint8)key - 1;
	const DirEnt& de = mDirectory[fileId];

	dst.clear();

	const uint32 sectorSize = mpImage->GetSectorSize();
	bool clustersSeen[128] = {false};
	uint32 cluster = de.mClusterStart;
	uint32 bytesLeft = de.mByteSize;

	for(;;) {
		if (cluster >= mClusterCount)
			throw ATDiskFSException(kATDiskFSError_CorruptedFileSystem);

		uint32 nextCluster = mAllocTableBuffer[cluster];

		if (nextCluster != 0xFD && nextCluster >= mClusterCount)
			throw ATDiskFSException(kATDiskFSError_CorruptedFileSystem);

		if (clustersSeen[cluster])
			throw ATDiskFSException(kATDiskFSError_CorruptedFileSystem);

		clustersSeen[cluster] = true;

		uint32 toCopy = bytesLeft > 1024 ? 1024 : bytesLeft;
		bytesLeft -= toCopy;

		uint32 sectorIndex = 24 + cluster * 8;

		while(toCopy) {
			uint32 sectorDataBytes = toCopy > 128 ? 128 : toCopy;
			toCopy -= sectorDataBytes;

			if (sectorSize != mpImage->ReadVirtualSector(sectorIndex, mSectorBuffer, sectorSize))
				throw ATDiskFSException(kATDiskFSError_CorruptedFileSystem);

			dst.insert(dst.end(), mSectorBuffer, mSectorBuffer + sectorDataBytes);

			++sectorIndex;
		}

		if (nextCluster == 0xFD)
			break;

		cluster = nextCluster;
	}
}

ATDiskFSKey ATDiskFSDOS3::WriteFile(ATDiskFSKey parentKey, const char *filename, const void *src, uint32 len) {
	if (mbReadOnly)
		throw ATDiskFSException(kATDiskFSError_ReadOnly);

	if (len > 0xFFFF)
		throw ATDiskFSException(kATDiskFSError_FileTooLarge);

	if (!IsValidFileName(filename))
		throw ATDiskFSException(kATDiskFSError_InvalidFileName);

	if (LookupFile(parentKey, filename) != ATDiskFSKey::None)
		throw ATDiskFSException(kATDiskFSError_FileExists);

	// find an empty directory entry
	uint32 dirIdx = 0;
	for(;;) {
		if (!(mDirectory[dirIdx].mFlags & DirEnt::kFlagActive))
			break;

		++dirIdx;
		if (dirIdx >= vdcountof(mDirectory))
			throw ATDiskFSException(kATDiskFSError_DirectoryFull);
	}

	// allocate clusters -- note that at least 1 cluster is allocated, even for
	// an empty file
	const uint32 clusterCount = len ? ((len + 1023) >> 10) : 1;

	vdfastvector<uint32> clustersToUse;
	uint32 clustersToAllocate = clusterCount;
	for(uint32 i = 0; i < mClusterCount; ++i) {
		if (mAllocTableBuffer[i] == 0xFE) {
			clustersToUse.push_back(i);

			if (!--clustersToAllocate)
				break;
		}
	}

	if (clustersToAllocate)
		throw ATDiskFSException(kATDiskFSError_DiskFull);

	// build the FAT chain
	for(uint32 i = 1; i < clusterCount; ++i)
		mAllocTableBuffer[clustersToUse[i - 1]] = clustersToUse[i];

	mAllocTableBuffer[clustersToUse.back()] = 0xFD;

	// write data sectors
	for(uint32 i=0; i<clusterCount; ++i) {
		const uint32 clusterOffset = i << 10;
		const uint32 clusterBaseSectorIndex = 24 + 8*clustersToUse[i];

		for(uint32 j=0; j<8; ++j) {
			const uint32 sectorOffset = clusterOffset + 128*j;

			if (sectorOffset >= len)
				break;

			uint32 dataBytes = len - sectorOffset;

			if (dataBytes > 128)
				dataBytes = 128;

			memcpy(mSectorBuffer, (const char *)src + sectorOffset, dataBytes);
			memset(mSectorBuffer + dataBytes, 0, 128 - dataBytes);

			mpImage->WriteVirtualSector(clusterBaseSectorIndex + j, mSectorBuffer, 128);
		}
	}

	// write directory entry
	DirEnt& de = mDirectory[dirIdx];
	de.mFlags = DirEnt::kFlagValid | DirEnt::kFlagActive;
	de.mByteSize = len;
	de.mClusterStart = clustersToUse.front();
	de.mClusterCount = clusterCount;

	WriteFileName(de, filename);

	mbDirty = true;

	return (ATDiskFSKey)(dirIdx + 1);
}

void ATDiskFSDOS3::RenameFile(ATDiskFSKey key, const char *filename) {
	if (mbReadOnly)
		throw ATDiskFSException(kATDiskFSError_ReadOnly);

	if (!IsValidFileName(filename))
		throw ATDiskFSException(kATDiskFSError_InvalidFileName);

	ATDiskFSKey conflictingKey = LookupFile(ATDiskFSKey::None, filename);

	if (conflictingKey == key)
		return;

	if (conflictingKey != ATDiskFSKey::None)
		throw ATDiskFSException(kATDiskFSError_FileExists);

	WriteFileName(mDirectory[(uint32)key - 1], filename);
	mbDirty = true;
}

ATDiskFSKey ATDiskFSDOS3::CreateDir(ATDiskFSKey parentKey, const char *filename) {
	throw ATDiskFSException(kATDiskFSError_NotSupported);
}

void ATDiskFSDOS3::WriteFileName(DirEnt& de, const char *filename) {
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

bool ATDiskFSDOS3::IsValidFileName(const char *filename) {

	// first character must be alphanumeric
	if ((uint8)(((uint8)*filename++ & 0xdf) - 0x41) >= 26)
		return false;

	// up to 7 alphanumeric characters may follow
	int count = 0;

	for(;;) {
		uint8 c = *filename;

		if ((uint8)(c - 0x30) >= 10 && (uint8)((c & 0xdf) - 0x41) >= 26)
			break;

		++filename;

		if (++count > 7)
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

		if ((uint8)(c - 0x30) >= 10 && (uint8)((c & 0xdf) - 0x41) >= 26)
			break;

		if (++count > 3)
			return false;
	}

	// looks OK
	return true;
}

///////////////////////////////////////////////////////////////////////////

IATDiskFS *ATDiskFormatImageDOS3(IATDiskImage *image) {
	vdautoptr<ATDiskFSDOS3> fs(new ATDiskFSDOS3);

	fs->InitNew(image);

	return fs.release();
}

IATDiskFS *ATDiskMountImageDOS3(IATDiskImage *image, bool readOnly) {
	vdautoptr<ATDiskFSDOS3> fs(new ATDiskFSDOS3);

	fs->Init(image, readOnly);

	return fs.release();
}
