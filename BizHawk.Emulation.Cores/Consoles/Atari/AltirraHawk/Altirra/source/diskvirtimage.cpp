//	Altirra - Atari 800/800XL/5200 emulator
//	Atari DOS 2.x virtual filesystem handler
//	Copyright (C) 2009-2017 Avery Lee
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

//=========================================================================
// Virtual DOS 2.x disk handler
//
// The virtual folder handler maps a host directory onto a virtual DOS 2.x
// disk, such that a compatible DOS using normal access patterns can read
// files on the disk. The data sectors on the disk are allocated
// dynamically so that this is possible even if the total size of the files
// exceeds the space normally available on a 90K single density volume.
// The handler accomplishes this by watching the access pattern and
// dynamically assigning factors according to the "read wavefront" being
// pushed by DOS. This is possible due to DOS's lack of a significant disk
// cache and the singly-linked sector structure limiting visibility.
//
// Disk layout
// -----------
// The disk is laid out as follows:
//
// +------+---------------+----------------+------------------+---------------+
// | boot | start sectors | sector pool 1  | VTOC + directory | sector pool 2 |
// | 3 s. |   64 sectors  |  292 sectors   |     9 sectors    |  351 sectors  |  
// +------+---------------+----------------+------------------+---------------+
// 
// As with DOS 2.x, the last sector on the disk is not used (720).
//
// The first three sectors are reserved for the boot sectors; it is possible to
// place an image of these called $dosboot.bin in the mapped directory and the
// handler will include those to make the disk bootable. For this to work, the
// DOS.SYS must match the boot sectors as the boot sectors are essentially the
// first 384 bytes of DOS.SYS.
//
// 64 sectors on the disk are reserved for the starting sector of any file on
// the disk. This prevents issues with aggressive caching, particularly with
// the SpartaDOS X ATARIDOS.SYS driver.
//
// The remaining 643 sectors are used as a pool to provide data sectors as DOS
// reads files on the disk. They are allocated in LRU order as DOS sees
// successive sector links in the file -- that is, whenever a data sector is
// read, the next sector for that file is allocated and assigned. The cache
// attempts to maintain as consistent of an image as possible within the
// constraints of the pool size vs. total data. If the total working set of
// data sectors accessed by the drive fits within the pool, the disk image will
// be completely stable. If the working set does not fit within the pool, the
// handler attempts to keep as much recent history stable as possible. This
// allows the virtual disk to be accessed by operating systems that have
// dynamic disk caching (SpartaDOS X), or by applications that make extensive
// use of direct reseeking via POINT.
//
// Preallocation
// -------------
// If the drive reads sectors that have not been committed yet because they
// have not been referenced by another sector previously read, the handler
// attempts to preallocate those sectors and store valid data in them. This
// is primarily to make the virtual disk image work with drives that do track
// buffering, since those drives will read an entire track of 18 sectors
// whenever DOS requests a sector on a new track. Preallocation ensures that
// the track buffer holds valid data since the handler cannot invalidate the
// drive's track cache to reassign those sectors.
//
// Note that track buffering causes the handler to see a LOT of false
// references, since the handler cannot tell which sectors are actually
// requested by DOS. The sector pool is big enough to ensure that DOS continues
// to see valid data structures as it advances its read wavefronts, but this
// results in some rather astounding levels of fragmentation once sectors
// start to be recycled in the sector pool. As an example, simply reading
// track 0 requires the handler to allocate the second data sector for as
// many as 15 files due to the initial sector links. It tries to separate
// files into different tracks to keep the read pattern sane, but this
// quickly breaks down once the sector pool starts to fill up.
//
// A side effect of preallocation is that a disk whose total data size fits
// within 90K can be copied using a sector copier to create a working disk.
// The VTOC will be wrong (all sectors allocated), but all files will be
// present on the disk.
//

#include <stdafx.h>
#include <vd2/system/binary.h>
#include <vd2/system/math.h>
#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include <vd2/system/filesys.h>
#include <vd2/system/filewatcher.h>
#include <vd2/system/strutil.h>
#include <vd2/system/time.h>
#include <at/atio/diskimage.h>
#include <at/atio/diskfsdos2util.h>
#include "debuggerlog.h"
#include "hostdeviceutils.h"

ATDebuggerLogChannel g_ATLCVDisk(false, false, "VDISK", "Virtual disk activity");

class ATDiskImageVirtualFolder final : public vdrefcounted<IATDiskImage>, public IVDTimerCallback {
public:
	ATDiskImageVirtualFolder();

	void Init(const wchar_t *path);

	void *AsInterface(uint32 id) override;

	ATImageType GetImageType() const override { return kATImageType_Tape; }

	ATDiskTimingMode GetTimingMode() const override { return kATDiskTimingMode_Any; }

	bool IsDirty() const override { return false; }
	bool IsUpdatable() const override { return false; }
	bool IsDynamic() const override { return true; }
	ATDiskImageFormat GetImageFormat() const override { return kATDiskImageFormat_None; }

	bool Flush() override { return true; }
	virtual uint64 GetImageChecksum() const override { return 0; }
	virtual std::optional<uint32> GetImageFileCRC() const override { return {}; }

	void SetPath(const wchar_t *path, ATDiskImageFormat format) override;
	void Save(const wchar_t *path, ATDiskImageFormat format) override;

	ATDiskGeometryInfo GetGeometry() const override;
	uint32 GetSectorSize() const override;
	uint32 GetSectorSize(uint32 virtIndex) const override;
	uint32 GetBootSectorCount() const override;

	uint32 GetPhysicalSectorCount() const override;
	void GetPhysicalSectorInfo(uint32 index, ATDiskPhysicalSectorInfo& info) const override;

	void ReadPhysicalSector(uint32 index, void *data, uint32 len) override;
	void WritePhysicalSector(uint32 index, const void *data, uint32 len, uint8 fdcStatus) override;

	uint32 GetVirtualSectorCount() const override;
	void GetVirtualSectorInfo(uint32 index, ATDiskVirtualSectorInfo& info) const override;

	uint32 ReadVirtualSector(uint32 index, void *data, uint32 len) override;
	bool WriteVirtualSector(uint32 index, const void *data, uint32 len) override;

	void Resize(uint32 sectors) override;
	void FormatTrack(uint32 vsIndexStart, uint32 vsCount, const ATDiskVirtualSectorInfo *vsecs, uint32 psCount, const ATDiskPhysicalSectorInfo *psecs, const uint8 *psecData) override;

	bool IsSafeToReinterleave() const override;
	void Reinterleave(ATDiskInterleave interleave) override;

public:
	void TimerCallback() override;

protected:
	void UpdateDirectory(bool reportNewFiles);

	struct DirEnt {
		enum {
			kFlagDeleted	= 0x80,
			kFlagInUse		= 0x40,
			kFlagLocked		= 0x20,
			kFlagDOS2		= 0x02,
			kFlagOpenWrite	= 0x01
		};

		uint8	mFlags;
		uint8	mSectorCount[2];
		uint8	mFirstSector[2];
		uint8	mName[11];
	};

	struct XDirBaseEnt {
		VDStringW mPath;
		uint32	mSize = 0;				// File size in bytes.
		uint32	mSectorCount = 0;		// Number of data sectors in the file.
		uint32	mLockedSector = 0;		// Virtual sector number of the next data sector after the last read one, or 0 if none.
	};

	struct XDirEnt : public XDirBaseEnt {
		VDFile	mFile;
		bool	mbValid = false;
		uint32	mSectorsAllocated = 1;
		uint32	mNextPrealloc = 0;
	};

	struct SectorEnt {
		bool	mbInCache;				// True if sector is in the LRU cache and can be reassigned. Locked and special sectors are not.
		sint8	mFileIndex;				// File index, or -1 if not assigned to a file.
		uint16	mSectorIndex;			// 0-based index of sector in file, in file order.
		uint16	mLRUPrev;
		uint16	mLRUNext;
	};

	void PromoteDataSector(uint32 sector);
	uint32 FindDataSector(sint8 fileIndex, uint16 sectorIndex) const;
	void UnlinkDataSector(uint32 sector);
	void LinkDataSector(uint32 sector);
	uint32 FindBestNextDataSector(sint8 fileIndex, uint32 prevSectorIndex);
	void PreallocateTrack(uint32 baseSectorIndex);

	VDStringW mPath;
	uint32	mSectorCount;
	uint32	mFreeSectorCount;
	bool mbBootFilePresent;
	VDDate mBootFileLastDate;
	int mDosEntry;

	vdfunction<float(uint32)> mpInterleaveFn;

	VDLazyTimer mCloseTimer;
	VDFileWatcher mFileWatcher;

	DirEnt	mDirEnt[64];
	XDirEnt	mXDirEnt[64];

	SectorEnt mSectorMap[720];

	uint8 mBootSectors[384];
};

ATDiskImageVirtualFolder::ATDiskImageVirtualFolder()
	: mSectorCount(720)
{
	mpInterleaveFn = ATDiskGetInterleaveFn(kATDiskInterleave_Default, GetGeometry());
}

void ATDiskImageVirtualFolder::Init(const wchar_t *path) {
	mPath = path;
	mbBootFilePresent = false;
	mBootFileLastDate.mTicks = 0;
	mDosEntry = -1;
	memset(mDirEnt, 0, sizeof mDirEnt);

	UpdateDirectory(false);

	// Mark all sectors as in use.
	for(uint32 i=0; i<(uint32)vdcountof(mSectorMap); ++i) {
		SectorEnt& se = mSectorMap[i];
		se.mbInCache = false;
		se.mFileIndex = -1;
		se.mLRUNext = i;
		se.mLRUPrev = i;
		se.mSectorIndex = 0;
	}

	// Sectors 3-66 are permanently dedicated to the first sector of each file.
	for(uint32 i=3; i<=66; ++i) {
		mSectorMap[i].mFileIndex = i-3;
		mSectorMap[i].mSectorIndex = 0;
	}

	// Put sectors 67-358 and 368-718 in the pool for rotating data sector use.
	mFreeSectorCount = 0;

	for(uint32 i=67; i<=358; ++i)
		LinkDataSector(i);

	for(uint32 i=368; i<=718; ++i)
		LinkDataSector(i);

	try {
		mFileWatcher.InitDir(path, false, NULL);
	} catch(const MyError&) {
	}
}

void *ATDiskImageVirtualFolder::AsInterface(uint32 id) {
	switch(id) {
		case IATDiskImage::kTypeID: return static_cast<IATDiskImage *>(this);
	}

	return nullptr;
}

void ATDiskImageVirtualFolder::SetPath(const wchar_t *path, ATDiskImageFormat format) {
}

void ATDiskImageVirtualFolder::Save(const wchar_t *path, ATDiskImageFormat format) {
}

ATDiskGeometryInfo ATDiskImageVirtualFolder::GetGeometry() const {
	ATDiskGeometryInfo info;
	info.mSectorSize = 128;
	info.mBootSectorCount = 3;
	info.mTotalSectorCount = 720;
	info.mTrackCount = 40;
	info.mSectorsPerTrack = 18;
	info.mSideCount = 1;
	info.mbMFM = false;
	return info;
}

uint32 ATDiskImageVirtualFolder::GetSectorSize() const {
	return 128;
}

uint32 ATDiskImageVirtualFolder::GetSectorSize(uint32 virtIndex) const {
	return 128;
}

uint32 ATDiskImageVirtualFolder::GetBootSectorCount() const {
	return 3;
}

uint32 ATDiskImageVirtualFolder::GetPhysicalSectorCount() const {
	return 720;
}

void ATDiskImageVirtualFolder::GetPhysicalSectorInfo(uint32 index, ATDiskPhysicalSectorInfo& info) const {
	info.mOffset = 0;
	info.mDiskOffset = -1;
	info.mPhysicalSize = 128;
	info.mImageSize = 128;
	info.mbDirty = false;
	info.mbMFM = false;
	info.mRotPos = mpInterleaveFn(index);
	info.mFDCStatus = 0xFF;
	info.mWeakDataOffset = -1;
}

void ATDiskImageVirtualFolder::ReadPhysicalSector(uint32 index, void *data, uint32 len) {
	memset(data, 0, len);

	if (len != 128 || index >= 720)
		return;

	// check for updates
	if (mFileWatcher.Wait(0))
		UpdateDirectory(true);

	// check for boot sector
	if (index < 3) {
		if (mbBootFilePresent) {
			memcpy(data, mBootSectors + 128*index, 128);

			// If this is the first sector, we need to patch it to include
			// info about where DOS.SYS is.
			if (!index) {
				uint8 *dst = (uint8 *)data;
				if (mDosEntry >= 0) {
					dst[0x0E] = 1;
					dst[0x0F] = mDirEnt[mDosEntry].mFirstSector[0];
					dst[0x10] = mDirEnt[mDosEntry].mFirstSector[1];
				} else {
					dst[0x0E] = 0;
				}
			}
		} else {
			unsigned offset = index * 128;

			if (offset < g_ATResDOSBootSectorLen)
				memcpy(data, g_ATResDOSBootSector + offset, std::min<size_t>(g_ATResDOSBootSectorLen - offset, 128));
		}
		return;
	}

	// check for VTOC
	if (index == 359) {
		static const uint8 kVTOCSector[]={
			0x02, 0xC3, 0x02
		};

		memcpy(data, kVTOCSector, sizeof kVTOCSector);
		return;
	}

	// check for directory
	if (index >= 360 && index < 368) {
		memcpy(data, &mDirEnt[(index - 360) << 3], 128);
		return;
	}

	// Must be data sector.
	//
	// We have a total of 707 data sectors to play with, 3-358 and 368-718 (we are zero-based
	// here, and DOS does not allocate the last sector). Sectors 3-66 are reserved as the first
	// sectors; we use the remaining sectors for rotating data sector use.

	// First, check if the sector is assigned to a file. If not, bail.
	SectorEnt& se = mSectorMap[index];

	if (se.mFileIndex < 0) {
		if (!se.mbInCache)
			return;

		// Sector is not allocated. Try to preallocate all sectors on the track, and then check if
		// the sector is still unallocated; if so, blacklist the sector by moving it to the end.
		// We need to preallocate the entire track if we can or else fragmentation gets horrific
		// on track caching drives.
		PreallocateTrack(index);

		if (se.mFileIndex < 0) {
			// We didn't find anything useful to allocate this sector to. Move it to
			// the back of the LRU reuse order and return an empty sector.
			g_ATLCVDisk("Blacklisting sector %u\n", index+1);
			UnlinkDataSector(index);
			LinkDataSector(index);
			return;
		}
	}

	// Retrieve the data corresponding to that file.
	XDirEnt& xd = mXDirEnt[se.mFileIndex];

	// Check if this file entry is actually in use. If it is not, we might be reading the
	// preallocated entry for a file slot that's unused, in which case we should bail.
	if (!xd.mbValid)
		return;

	// Promote this sector to LRU head.
	PromoteDataSector(index);
	
	g_ATLCVDisk("Reading sector %u [%2u:%2u] (sector %u/%u of file %d / %ls)\n"
		, index+1
		, index/18
		, index%18+1
		, se.mSectorIndex
		, xd.mSectorCount
		, se.mFileIndex
		, VDFileSplitPath(xd.mPath.c_str()));

	uint8 validLen = 0;
	uint32 link = (uint32)0 - 1;

	// Check if we are beyond the end of the file -- this can happen with an update. If
	// this happens, we report an empty terminator sector.
	if (se.mSectorIndex < xd.mSectorCount) {
		if (!xd.mFile.isOpen()) {
			g_ATLCVDisk("Opening file: %ls\n", xd.mPath.c_str());
			xd.mFile.open(xd.mPath.c_str());
		}

		const uint32 offset = (uint32)se.mSectorIndex * 125;
		const uint32 remainder = xd.mSize - offset;

		validLen = (uint8)std::min<uint32>(125, remainder);

		// Read in the file data.
		if (validLen) {
			xd.mFile.seek(offset);
			xd.mFile.read(data, validLen);

			mCloseTimer.SetOneShot(this, 3000);
		}

		// Check if there will be another sector. If so, we need to determine the sector link and
		// pre-allocate that sector if needed.
		if (remainder > 125) {
			link = xd.mLockedSector;

			if (!link || mSectorMap[link].mSectorIndex != se.mSectorIndex + 1) {
				// Release the locked sector back into the LRU list.
				if (link)
					LinkDataSector(link);

				// Check if the sector we need is still present.
				link = FindDataSector(se.mFileIndex, se.mSectorIndex + 1);

				if (link) {
					UnlinkDataSector(link);
				} else {
					// No -- allocate a fresh sector. Try to grab the next sector if it has not been used yet; otherwise,
					// allocate a sector from the LRU cache.
					link = FindBestNextDataSector(se.mFileIndex, index);
					
					UnlinkDataSector(link);

					// Reassign the sector.
					SectorEnt& le = mSectorMap[link];

					g_ATLCVDisk("%s sector %u [%2u:%2u] as sector %u of file %ls\n"
						, le.mFileIndex >= 0 ? "Reassigning" : "Allocating"
						, link + 1
						, link / 18
						, link % 18 + 1
						, se.mSectorIndex + 1
						, VDFileSplitPath(xd.mPath.c_str()));

					if (le.mFileIndex >= 0) {
						VDASSERT(mXDirEnt[le.mFileIndex].mSectorsAllocated > 1);
						--mXDirEnt[le.mFileIndex].mSectorsAllocated;
					}

					le.mFileIndex = se.mFileIndex;
					le.mSectorIndex = se.mSectorIndex + 1;
					++xd.mSectorsAllocated;
				}

				xd.mLockedSector = link;
			} else {
				g_ATLCVDisk("Using cached link at sector %u\n", link + 1);
			}
		}
	}

	// Byte 125 contains the file ID in bits 2-7 and the high two bits of the link in bits 0-1.
	// Byte 126 contains the low 8 bits of the link (note that we must make this one-based).
	// Byte 127 contains the number of valid data bytes.
	uint8 *dst = (uint8 *)data;

	dst[125] = (uint8)(((int)se.mFileIndex << 2) + ((link + 1) >> 8));
	dst[126] = (uint8)(link + 1);
	dst[127] = validLen;
}

void ATDiskImageVirtualFolder::WritePhysicalSector(uint32 index, const void *data, uint32 len, uint8 fdcStatus) {
	throw MyError("Writes are not supported to a virtual disk.");
}

uint32 ATDiskImageVirtualFolder::GetVirtualSectorCount() const {
	return 720;
}

void ATDiskImageVirtualFolder::GetVirtualSectorInfo(uint32 index, ATDiskVirtualSectorInfo& info) const {
	info.mStartPhysSector = index;
	info.mNumPhysSectors = 1;
}

uint32 ATDiskImageVirtualFolder::ReadVirtualSector(uint32 index, void *data, uint32 len) {
	if (len < 128)
		return 0;

	ReadPhysicalSector(index, data, len > 128 ? 128 : len);
	return 128;
}

bool ATDiskImageVirtualFolder::WriteVirtualSector(uint32 index, const void *data, uint32 len) {
	return false;
}

void ATDiskImageVirtualFolder::Resize(uint32 sectors) {
	throw MyError("A virtual disk cannot be resized.");
}

void ATDiskImageVirtualFolder::FormatTrack(uint32 vsIndexStart, uint32 vsCount, const ATDiskVirtualSectorInfo *vsecs, uint32 psCount, const ATDiskPhysicalSectorInfo *psecs, const uint8 *psecData) {
	throw MyError("A virtual disk cannot be formatted.");
}

bool ATDiskImageVirtualFolder::IsSafeToReinterleave() const {
	return true;
}

void ATDiskImageVirtualFolder::Reinterleave(ATDiskInterleave interleave) {
	mpInterleaveFn = ATDiskGetInterleaveFn(interleave, GetGeometry());
}

void ATDiskImageVirtualFolder::TimerCallback() {
	for(size_t i=0; i<vdcountof(mXDirEnt); ++i) {
		XDirEnt& xd = mXDirEnt[i];

		if (xd.mFile.isOpen()) {
			g_ATLCVDisk("Closing file: %ls\n", xd.mPath.c_str());
			xd.mFile.closeNT();
		}
	}
}

void ATDiskImageVirtualFolder::PromoteDataSector(uint32 sector) {
	if (!mSectorMap[sector].mbInCache)
		return;

	UnlinkDataSector(sector);
	LinkDataSector(sector);
}

uint32 ATDiskImageVirtualFolder::FindDataSector(sint8 fileIndex, uint16 sectorIndex) const {
	for(uint32 i=1; i<720; ++i) {
		if (mSectorMap[i].mFileIndex == fileIndex && mSectorMap[i].mSectorIndex == sectorIndex)
			return i;
	}

	return 0;
}

void ATDiskImageVirtualFolder::UnlinkDataSector(uint32 sector) {
	VDASSERT(sector > 0 && sector < 720);

	// unlink sector
	SectorEnt& se = mSectorMap[sector];
	VDASSERT(se.mbInCache);

	SectorEnt& pe = mSectorMap[se.mLRUPrev];
	SectorEnt& ne = mSectorMap[se.mLRUNext];

	ne.mLRUPrev = se.mLRUPrev;
	pe.mLRUNext = se.mLRUNext;
	se.mLRUPrev = sector;
	se.mLRUNext = sector;
	se.mbInCache = false;

	if (se.mFileIndex < 0)
		--mFreeSectorCount;
}

void ATDiskImageVirtualFolder::LinkDataSector(uint32 sector) {
	VDASSERT(sector > 0 && sector < 720);

	// relink sector at head
	SectorEnt& se = mSectorMap[sector];
	VDASSERT(!se.mbInCache);

	SectorEnt& re = mSectorMap[0];
	SectorEnt& he = mSectorMap[re.mLRUNext];
	se.mLRUPrev = 0;
	se.mLRUNext = re.mLRUNext;

	he.mLRUPrev = sector;
	re.mLRUNext = sector;

	se.mbInCache = true;

	if (se.mFileIndex < 0)
		++mFreeSectorCount;
}

uint32 ATDiskImageVirtualFolder::FindBestNextDataSector(sint8 fileIndex, uint32 prevSectorIndex) {
	// If we are out of free sectors, free up some until we have three tracks' worth again.
	if (!mFreeSectorCount) {
		uint32 vsi = mSectorMap[0].mLRUPrev;

		while(mFreeSectorCount < 18*3 && vsi) {
			const int fileIndex = mSectorMap[vsi].mFileIndex;
			if (fileIndex >= 0) {
				--mXDirEnt[fileIndex].mSectorsAllocated;
				mSectorMap[vsi].mFileIndex = -1;
				++mFreeSectorCount;
			}

			vsi = mSectorMap[vsi].mLRUPrev;
		}
	}

	// validate count
	VDASSERT(mFreeSectorCount == std::count_if(std::begin(mSectorMap), std::end(mSectorMap), [](const SectorEnt& se) { return se.mbInCache && se.mFileIndex < 0; }));

	// Find a suitable sector to use as the next data link sector for a file. Ideally, we want
	// to cluster them within the same track and preferably within the same track, to reduce
	// pollution with track buffered drives.
	
	// 1) If there are unallocated sectors within the same track, use them.
	uint32 trackStart = prevSectorIndex - prevSectorIndex % 18;

	for(uint32 i=0, testSec = prevSectorIndex; i<18; ++i) {
		if (mSectorMap[testSec].mbInCache && mSectorMap[testSec].mFileIndex < 0)
			return testSec;

		if (++testSec >= trackStart + 18)
			testSec = trackStart;
	}

	// 2) Scan all other tracks on the disk and try to find another track we can use. Score
	// tracks first by the number of other files included and then by the number of free sectors.
	//
	// Tracks we want to avoid, due to how hot they are for access and pollution potential:
	//	- Tracks 0-3: These include the initial sectors for files.
	//	- Track 20: This holds the directory.

	uint32 bestTrackScore = 0;
	uint32 bestFreeSector = 0;

	for(uint32 track = 4; track < 40; ++track) {
		if (track == 20)
			continue;

		const uint32 trackStartIndex = track * 18;
		bool fileMap[64] = {false};
		fileMap[fileIndex] = true;
		uint32 otherFilesOnTrack = 0;
		uint32 freeSectorsOnTrack = 0;
		uint32 firstFreeSector = 0;

		for(uint32 i=0; i<18; ++i) {
			const SectorEnt& se = mSectorMap[trackStartIndex + i];
			if (se.mFileIndex >= 0) {
				if (!fileMap[se.mFileIndex]) {
					fileMap[se.mFileIndex] = true;
					++otherFilesOnTrack;
				}
			} else if (se.mbInCache) {
				++freeSectorsOnTrack;
				if (!firstFreeSector)
					firstFreeSector = trackStartIndex + i;
			}
		}

		if (freeSectorsOnTrack) {
			uint32 trackScore = ((18 - otherFilesOnTrack) << 8) + freeSectorsOnTrack;

			if (trackScore > bestTrackScore) {
				bestTrackScore = trackScore;
				bestFreeSector = firstFreeSector;
			}
		}
	}

	if (bestFreeSector)
		return bestFreeSector;

	// Uh oh, looks like all tracks are in use. Whelp, guess we'll have to just return the next sector
	// in the LRU cache.

	const uint32 recycleSectorIndex = mSectorMap[0].mLRUPrev;
	VDASSERT(recycleSectorIndex);

	return recycleSectorIndex;
}

void ATDiskImageVirtualFolder::PreallocateTrack(uint32 baseSectorIndex) {
	const uint32 trackStart = baseSectorIndex - baseSectorIndex % 18;
	const uint32 trackEnd = trackStart + 18;
	uint32 nextSectorIndex = baseSectorIndex;
	uint32 filesScanned = 0;

	// Construct file order for track. Use the files that we currently have on the track,
	// then the remainder of files.
	uint8 fileOrder[64];
	bool filesSeen[64] = {};
	int nextFileOrder = 0;

	for(uint32 i=trackStart; i<trackEnd; ++i) {
		int fi = mSectorMap[i].mFileIndex;
		if (fi >= 0 && !filesSeen[fi]) {
			filesSeen[fi] = true;
			fileOrder[nextFileOrder++] = (uint8)fi;

			mXDirEnt[fi].mNextPrealloc = mSectorMap[i].mSectorIndex + 1;
		}
	}

	for(uint32 i=0; i<64; ++i) {
		if (!filesSeen[i]) {
			filesSeen[i] = true;
			fileOrder[nextFileOrder++] = (uint8)i;
		}
	}

	uint32 nextFileOrderIndex = 0;
	for(;;) {
		// Find the next sector on the track that is in the LRU cache and not currently
		// allocated to a file.
		while(!mSectorMap[nextSectorIndex].mbInCache || mSectorMap[nextSectorIndex].mFileIndex >= 0) {
			if (++nextSectorIndex == trackEnd)
				nextSectorIndex = trackStart;

			if (nextSectorIndex == baseSectorIndex)
				return;
		}

		const uint32 fileIndex = fileOrder[nextFileOrderIndex];
		XDirEnt& xdpre = mXDirEnt[fileIndex];

		// check if this file still has sectors to allocate and we're still within the
		// file
		while(xdpre.mSectorsAllocated < xdpre.mSectorCount) {
			if (xdpre.mNextPrealloc >= xdpre.mSectorCount)
				xdpre.mNextPrealloc = 1;

			uint32 vsec = FindDataSector((sint8)fileIndex, (uint16)xdpre.mNextPrealloc);

			if (vsec == 0) {
				// Hurray, we found a sector in this file that hasn't be allocated yet.
				// Allocate this sector to the file and then stop; the normal read code will then
				// allocate the next sector for the link.
				UnlinkDataSector(nextSectorIndex);

				SectorEnt& se = mSectorMap[nextSectorIndex];
				se.mFileIndex = fileIndex;
				se.mSectorIndex = xdpre.mNextPrealloc;
				++xdpre.mSectorsAllocated;

				VDASSERT(xdpre.mSectorsAllocated <= xdpre.mSectorCount);

				g_ATLCVDisk("Preallocating sector %u [%2u:%2u] as sector %u/%u of file %d / %ls\n"
					, nextSectorIndex+1
					, nextSectorIndex/18
					, nextSectorIndex%18 + 1
					, se.mSectorIndex
					, xdpre.mSectorCount
					, fileIndex
					, VDFileSplitPath(xdpre.mPath.c_str()));

				LinkDataSector(nextSectorIndex);

				goto next_sector;
			}

			++xdpre.mNextPrealloc;
		}

		if (++nextFileOrderIndex >= 64)
			break;

next_sector:
		;
	}
}

void ATDiskImageVirtualFolder::UpdateDirectory(bool reportNewFiles) {
	// Build a hash table of the existing entries.
	typedef vdhashmap<VDStringW, int> ExistingLookup;
	ExistingLookup existingLookup;

	for(size_t i=0; i<vdcountof(mXDirEnt); ++i) {
		if (mXDirEnt[i].mbValid)
			existingLookup[mXDirEnt[i].mPath] = (int)i;
	}

	// Clear valid flags for all entries.
	for(size_t i=0; i<vdcountof(mXDirEnt); ++i)
		mXDirEnt[i].mbValid = false;

	// Iterate over the directory. Collect them separately so we don't disturb existing entries.
	XDirBaseEnt newList[vdcountof(mXDirEnt)];

	uint32 totalEntries = 0;
	uint32 newEntries = 0;

	bool bootPresent = false;

	for(VDDirectoryIterator it(VDMakePath(mPath.c_str(), L"*.*").c_str()); it.Next();) {
		if (it.IsDirectory())
			continue;

		// Skip hidden files.
		if (it.GetAttributes() & kVDFileAttr_Hidden)
			continue;

		// Check for the magic boot sector.
		if (!vdwcsicmp(it.GetName(), L"$dosboot.bin")) {
			if (it.GetSize() == 384) {
				bootPresent = true;

				VDDate lastWriteDate = it.GetLastWriteDate();
				if (mBootFileLastDate != lastWriteDate) {
					mBootFileLastDate = lastWriteDate;

					// Try to read in the boot sector.
					try {
						VDFile f(it.GetFullPath().c_str());

						f.read(mBootSectors, sizeof mBootSectors);
					} catch(const MyError& e) {
						g_ATLCVDisk("Unable to read boot file: %s\n", e.gets());
						bootPresent = false;
					}
				}
			}

			continue;
		}

		// If we still have room in the emulated directory, add the file. Note that we
		// must continue to scan the host directory in case the boot sector is present.
		if (totalEntries < vdcountof(mDirEnt)) {
			++totalEntries;

			// Check if we had this entry previously; if so we want to preserve it.
			XDirBaseEnt *xdst;

			const VDStringW& filePath = it.GetFullPath();
			ExistingLookup::const_iterator it2(existingLookup.find(filePath));
			if (it2 == existingLookup.end()) {
				xdst = &newList[newEntries++];
				xdst->mPath = filePath;

				if (reportNewFiles)
					g_ATLCVDisk("Adding new file: %ls\n", it.GetName());
			} else {
				mXDirEnt[it2->second].mbValid = true;
				xdst = &mXDirEnt[it2->second];
			}

			uint32 size = VDClampToUint32(it.GetSize());
			uint32 sectors = VDClampToUint16((size + 124) / 125);
			xdst->mSize = size;
			xdst->mSectorCount = sectors;
		}
	}

	mbBootFilePresent = bootPresent;

	// Delete all directory entries that are no longer valid.
	for(size_t i=0; i<vdcountof(mDirEnt); ++i) {
		DirEnt& de = mDirEnt[i];
		XDirEnt& xde = mXDirEnt[i];

		if (!xde.mbValid) {
			memset(&de, 0, sizeof de);
			xde.mFile.closeNT();
			xde.mLockedSector = 0;
			xde.mSectorCount = 0;
			xde.mSize = 0;
		}
	}

	// Assign new slots for all new entries.
	uint32 freeNext = 0;
	for(uint32 i=0; i<newEntries; ++i) {
		// Copy base info into new entry.
		while(mXDirEnt[freeNext].mbValid)
			++freeNext;

		XDirEnt& xe = mXDirEnt[freeNext];
		DirEnt& de = mDirEnt[freeNext];

		static_cast<XDirBaseEnt&>(xe) = newList[i];
		xe.mbValid = true;

		// Assign a non-conflicting name.
		const VDStringA& name = VDTextWToA(VDFileSplitPath(xe.mPath.c_str()));
		size_t len1 = std::min<size_t>(name.size(), name.find('.'));
		const char *s = name.c_str();

		memset(de.mName, 0x20, sizeof de.mName);

		for(size_t j=0, k=0; k<8 && j<len1; ++j) {
			unsigned char c = toupper(s[j]);

			if ((c>='A' && c<='Z') || (c>='0' && c<='9'))
				de.mName[k++] = c;
		}

		if (len1 < name.size()) {
			size_t j = len1 + 1;
			size_t k = 8;
			
			while(k < 11) {
				unsigned char c = toupper((unsigned char)s[j++]);

				if (!c)
					break;

				if ((c>='A' && c<='Z') || (c>='0' && c<='9'))
					de.mName[k++] = c;
			}
		}

		// check for conflicts (a bit slow)
		for(;;) {
			bool clear = true;

			for(size_t j=0; j<vdcountof(mDirEnt); ++j) {
				if (freeNext != j && !memcmp(mDirEnt[j].mName, de.mName, 11)) {
					clear = false;
					break;
				}
			}

			if (clear)
				break;

			// increment the name
			for(int i=7; i>=0; --i) {
				if ((unsigned)(de.mName[i]-'0') >= 10) {
					de.mName[i] = '1';
					break;
				} else if (de.mName[i] == '9') {
					de.mName[i] = '0';
				} else {
					++de.mName[i];
					break;
				}
			}
		}
	}

	// Fill in all holes in the directory with the deleted flag.
	bool foundValidEntry = false;
	for(size_t i=vdcountof(mDirEnt); i; --i) {
		if (mXDirEnt[i-1].mbValid)
			foundValidEntry = true;
		else if (foundValidEntry)
			mDirEnt[i-1].mFlags = DirEnt::kFlagDeleted;
	}

	// Scan the sector map and deallocate all sectors that are no longer valid because
	// either the file slot is no longer in use or the index extends beyond the new
	// length of the file. Scan in reverse so the sectors are placed in ascending order
	// on the disk.
	for(uint32 i=718; i>=67; --i) {
		SectorEnt& se = mSectorMap[i];

		if (se.mFileIndex >= 0) {
			XDirEnt& xde = mXDirEnt[se.mFileIndex];

			if (!xde.mbValid || se.mSectorIndex >= xde.mSectorCount) {
				// file or sector is not valid -- push the sector back on the LRU list
				se.mFileIndex = -1;
				se.mSectorIndex = 0;

				if (se.mbInCache)
					++mFreeSectorCount;
				else
					LinkDataSector(i);

				VDASSERT(xde.mSectorsAllocated > 1);
				--xde.mSectorsAllocated;
			}
		}

		// skip the VTOC and directory at 359-367
		if (i == 368)
			i = 359;
	}

	// Recompute directory entry metadata.
	mDosEntry = -1;

	for(size_t i=0; i<vdcountof(mDirEnt); ++i) {
		DirEnt& de = mDirEnt[i];
		const XDirEnt& xde = mXDirEnt[i];

		if (xde.mbValid) {
			// Check if this is DOS.SYS so we can update the boot sector.
			if (!memcmp(de.mName, "DOS     SYS", 11))
				mDosEntry = (int)i;

			// There is an anomaly we have to deal with here regarding zero byte files.
			// Specifically, there is no provision in the disk format for a file with
			// no data sectors attached to it, so in the case of an zero byte file we
			// must point to a valid but empty data sector and put a sector count of 1
			// in the directory entry. DOS 2.0S and MyDOS 4.5D both do this. SpartaDOS X
			// also creates the empty data sector but puts a sector count of zero into
			// the directory entry, which makes VTOCFIX complain.

			VDWriteUnalignedLEU16(de.mSectorCount, std::max<uint16>(1, (uint16)std::min<uint32>(999, xde.mSectorCount)));
			de.mFirstSector[0] = (uint8)(4 + i);
			de.mFirstSector[1] = 0;
			de.mFlags = DirEnt::kFlagDOS2 | DirEnt::kFlagInUse;
		}
	}
}

///////////////////////////////////////////////////////////////////////////

void ATMountDiskImageVirtualFolder(const wchar_t *path, uint32 sectorCount, IATDiskImage **ppImage) {
	vdrefptr<ATDiskImageVirtualFolder> p(new ATDiskImageVirtualFolder);
	
	p->Init(path);
	*ppImage = p.release();
}
