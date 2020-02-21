#include <stdafx.h>
#include <vd2/system/binary.h>
#include <vd2/system/math.h>
#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include <vd2/system/filesys.h>
#include <vd2/system/filewatcher.h>
#include <vd2/system/hash.h>
#include <vd2/system/strutil.h>
#include <vd2/system/time.h>
#include <at/atio/diskimage.h>
#include <at/atio/diskfssdx2util.h>
#include "directorywatcher.h"
#include "debuggerlog.h"
#include "hostdeviceutils.h"

extern ATDebuggerLogChannel g_ATLCVDisk;

// General SDFS virtual disk layout:
//
// +------+-----------+----------------+------------------------------------------+
// | boot | bitmap    | root directory | sector pool                              |
// +------+-----------+----------------+------------------------------------------+
//
// The constraints on the pool sizes are as follows:
//	- The boot region is always 3 sectors.
//	- One bitmap sector is required per 1024 mapped sectors. We use a max size disk
//	  (64K-1), so this is 64 sectors.
//	- One sector is required for the start of the root directory sector map.
//
// Everything else is allocated to the sector pool. Unlike the DOS 2 filesystem,
// which has a fixed root directory and singly linked lists for files, SDFS uses
// a two-level structure where directory entries point to sector map sectors and
// those in turn point to data sectors. This leads to a high branching factor since
// a sector map sector can in turn pin down 64 other sectors (2 sector map + 62
// data). For this reason larger sector pools are required than with DOS2FS. The
// good news is that SDFS also has a change counter which means we do not need to
// pin the entire directory.
//
// The "pinning" concept is central to the virtual folder algorithm. Basically,
// sectors don't have to exist until we report them to SpartaDOS. For instance,
// once we return a sector that contains a directory entry, we "pin" the first
// sector of the sector map for that entry by reserving a sector number for that
// sector map sector. However, we do _not_ recursively pin references from the
// sector map as SpartaDOS hasn't actually read it yet. Thus, the virtual folder
// algorithm always has sectors reserved out one branch farther than those that
// have actually been returned.
//
// The sector pool itself is managed using an LRU algorithm. Unlike the virtual
// DOS 2 system, we don't exclude pinned sectors from being recycled. This is
// because SDFS can have reference cycles. However, SpartaDOS has a far shorter
// memory than we do and we can safely assume it will forget first.
//
// Sector keys
// ===========
// All active files are assigned an ID and used as part of a sector key used
// to manage the sector pool. The sector key is 32-bit:
//
// +-------------+-----+-------------------------------------------+
// | file ID     | map | offset                                    |
// +-------------+-----+-------------------------------------------+
//
// A file can be up to 16MB in size, or 131072 sectors. This means we need at
// least 17 bits for the offset. We use 17 bits for the offset, one bit for
// the sector map byte, and the remaining 14 high bits for the file ID.
//
// File IDs
// ========
// Certain file IDs are special:
// - ID 0 is reserved for special sectors; a non-zero offset means a special
//   sector, and an all zero key means an unused sector.
// - ID 1 is reserved for the root directory.
//
// The remaining file IDs are allocated using an LRU algorithm.
//
// Allocate-on-demand
// ==================
// Almost all of the directory is allocated on demand during traversal:
//
// - When a sector map sector is read, the previous and next map sectors are
//   allocated, as well as all referenced data sectors.
//
// - When a directory sector is read, all referenced sectors are allocated,
//   possibly including the first map sector of the parent directory and all
//   contained subdirectories or files.
//
// As noted above, AoD does _not_ mean that we actually populate the data for
// these references or allocate second-level references. However, reading a
// map sector does trigger a host directory scan for that directory as we do
// need to know how many directory entries there are at that point.
//
// Name mangling
// =============
// All host filenames are compressed into 8.3 naming using the following
// algorithm:
//
//	- The filename is split into base name and extension. If there is more
//	  than one extension, the last one is used and the remaining are treated
//	  as part of the base name.
//
//	- Lowercase characters are converted to uppercase and all non-valid chars
//	  ([a-zA-Z0-9_]) are removed.
//
//	- The filenames are lexicographically sorted.
//
//	- Each filename is tested in order for conflicts and incremented until it
//	  is non-conflicting. This is done by incrementing the number at the end
//	  of the filename, adding a digit if needed, and removing characters when
//	  necessary to make room for more digits.
//
// Unlike the DOS 2 virtual image system, the SDFS virtual image does not
// attempt to preserve filenames or inode positions as generally SpartaDOS
// must be forced to re-read all metadata on a change due to caching.
//
// Note that this name mangling complicates directory traversal as it means
// that we must keep track of mangled directory filenames in subdirectories so
// we can properly set name on the self-entries as we traverse back up toward
// the root, if the parent entries have expired.
//
// Change detection
// ================
// The host directories are monitored for changes and those changes are
// reflected into the virtual filesystem. This causes a bump in the sequence
// number to force SpartaDOS to re-read directory metadata. A change is only
// processed if there is a resident node below the changed directory, though;
// changing a subdirectory that has not yet been traversed is ignored as no
// affected data structures would have been seen by DOS.
//

class ATDiskImageVirtualFolderSDFS final : public vdrefcounted<IATDiskImage>, public IVDTimerCallback {
public:
	ATDiskImageVirtualFolderSDFS();

	void Init(const wchar_t *path, uint64 unique);

	void *AsInterface(uint32 id) override;

	ATImageType GetImageType() const override { return kATImageType_Tape; }

	ATDiskTimingMode GetTimingMode() const override { return kATDiskTimingMode_Any; }

	bool IsDirty() const override { return false; }
	bool IsUpdatable() const override { return false; }
	bool IsDynamic() const override { return true; }
	ATDiskImageFormat GetImageFormat() const override { return kATDiskImageFormat_None; }

	uint64 GetImageChecksum() const override { return 0; }
	std::optional<uint32> GetImageFileCRC() const override { return {}; }

	bool Flush() override { return true; }

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

protected:
	struct DirEnt;

	// SDFS directory entry (23 bytes).
	struct DirEnt {
		enum {
			kFlagLocked		= 0x01,
			kFlagHidden		= 0x02,
			kFlagArchived	= 0x04,
			kFlagInUse		= 0x08,
			kFlagDeleted	= 0x10,
			kFlagDirectory	= 0x20,
			kFlagOpenWrite	= 0x80
		};

		uint8	mFlags;		// +0		Flags
		uint8	mSecMap[2];	// +1-2		Start of sector map
		uint8	mSize[3];	// +3-5		Size in bytes
		uint8	mName[8];	// +6-13	Filename
		uint8	mExt[3];	// +14-16	Filename extension
		uint8	mDay;		// +17		Timestamp
		uint8	mMonth;		// +18
		uint8	mYear;		// +19
		uint8	mHour;		// +20
		uint8	mMinute;	// +21
		uint8	mSecond;	// +22
	};

	struct XDirEnt {
		DirEnt mDirEnt;
		VDStringW mRelPath;
		uint32 mSize;
	};

	struct XDirEntIndexPred;
	struct XDirEntNameHash;
	struct XDirEntNamePred;

	struct File : public vdlist_node {
		File *mpHashNext;
		uint32 mHashCode;
		VDFile mFile;
		VDStringW mRelPath;
		uint32 mSize;
		bool mbIsDirectory;
		bool mbDirectoryScanned;
		vdvector<XDirEnt> mXDirEnts;
		vdfastvector<uint8> mDOSPath;

		File()
			: mpHashNext(NULL)
			, mHashCode(0)
			, mSize(0)
			, mbIsDirectory(false)
			, mbDirectoryScanned(false)
		{
		}
	};

	struct Sector {
		bool	mbLinked;
		sint8	mFileIndex;
		uint16	mSectorIndex;
		uint16	mLRUPrev;
		uint16	mLRUNext;
	};

	struct SectorNode {
		uint32 mHashNext;
		uint32 mSectorKey;
		uint16 mLRUNext;
		uint16 mLRUPrev;
	};

	enum : uint32 {
		kSectorKeyOffsetMask	= 0x0001ffff,
		kSectorKeyMapBit		= 0x00020000,
		kSectorKeyFileMask		= 0xfffc0000
	};

	enum {
		kSectorKeyMapShift		= 17,
		kSectorKeyFileShift		= 18,
		kSectorPoolBase			= 68,
		kSectorPoolLimit		= 65535
	};

	void TimerCallback();
	void InitRoot();
	void InvalidateAll();
	void InvalidatePartial();
	void ScanDirectory(File& dir);
	uint32 ReserveFile(const XDirEnt& xde, const vdspan<const uint8>& parentDosPath);
	uint32 ReserveFile(const wchar_t *relPath, bool isDir, uint32 size, const uint8 name[8], const uint8 ext[3], const vdspan<const uint8>& parentDosPath);
	void PromoteFile(uint32 fidx);
	void UnlinkFile(uint32 fidx, bool scanSectors);
	uint32 ReserveSector(uint32 sectorKey);
	void PromoteSector(uint32 index);
	uint32 AllocateSector();
	void UnlinkSector(uint32 index);
	uint32 FindSector(uint32 sectorKey) const;
	uint32 FindSector(uint32 sectorKey, uint32 hc) const;
	uint32 HashSectorKey(uint32 sectorKey) const;

private:
	VDStringW mPath;

	vdlist<File> mFileLRU;
	uint8 mVolumeName[8];
	uint8 mVolSeqNumber;
	uint8 mVolRandNumber;
	bool mbVolChangePending;

	VDLazyTimer mCloseTimer;
	ATDirectoryWatcher mDirWatcher;
	vdfastvector<wchar_t> mDirChanges;
	
	vdfunction<float(uint32)> mpInterleaveFn;

	// This is a hash table used to accelerate sector key searches. It is keyed
	// off of all four bytes of the sector key added together.
	uint32 mSectorHash[256];
	File *mpFileHash[256];

	File mFiles[2048];

	// This array contains the sector key for each sector.
	SectorNode mSectorPool[65535];	// 1MB!
};

struct ATDiskImageVirtualFolderSDFS::XDirEntIndexPred {
	size_t operator()(const XDirEnt *a, const XDirEnt *b) const {
		return a->mRelPath.comparei(b->mRelPath) < 0;
	}
};

struct ATDiskImageVirtualFolderSDFS::XDirEntNameHash {
	size_t operator()(const XDirEnt *xde) const {
		return VDReadUnalignedLEU32(xde->mDirEnt.mName)
			+ VDReadUnalignedLEU32(xde->mDirEnt.mName + 4)
			+ ((uint32)VDReadUnalignedLEU16(xde->mDirEnt.mExt) << 8)
			+ xde->mDirEnt.mExt[2];
	}
};

struct ATDiskImageVirtualFolderSDFS::XDirEntNamePred {
	bool operator()(const XDirEnt *a, const XDirEnt *b) const {
		return !memcmp(a->mDirEnt.mName, b->mDirEnt.mName, 8) && !memcmp(a->mDirEnt.mExt, b->mDirEnt.mExt, 3);
	}
};

ATDiskImageVirtualFolderSDFS::ATDiskImageVirtualFolderSDFS() {
}

void ATDiskImageVirtualFolderSDFS::Init(const wchar_t *path, uint64 uniquenessValue) {
	mPath = path;

	// Compute volume name and signature.
	//
	// We combine the hash of the path, the uniqueness value, and the current date to
	// produce the volume name, sequence number, and random number. We have
	// 36^8 * 2^16 = 0x290D74100000000 combinations, or a bit over 2^58 bits to use.
	// We fold the top 6 bits to produce this value.
	uint64 volumeHash = VDHashString32I(path) + uniquenessValue + VDGetCurrentDate().mTicks;

	volumeHash += volumeHash >> 58;

	mVolSeqNumber = (uint8)volumeHash;
	mVolRandNumber = (uint8)(volumeHash >> 8);

	volumeHash >>= 16;

	for(int i=0; i<8; ++i) {
		int c = (int)(volumeHash % 36);
		volumeHash /= 36;

		mVolumeName[i] = (c >= 10) ? 0x41 + (c - 10) : 0x30 + c;
	}

	mbVolChangePending = false;

	// reset file hash table
	std::fill(mpFileHash, mpFileHash + vdcountof(mpFileHash), (File *)NULL);

	// reset sector hash table
	memset(mSectorHash, 0, sizeof mSectorHash);

	// reset sector pool - sectors 0-67 are special reserved, and sector 68
	// is the root directory sector map start
	memset(mSectorPool, 0, sizeof mSectorPool);

	for(int i=0; i<67; ++i)
		mSectorPool[i].mSectorKey = i + 1;

	mSectorPool[67].mSectorKey = (1 << kSectorKeyFileShift) + kSectorKeyMapBit;

	// link sectors 68-65534 into the free list
	for(int i=65534; i>=68; --i) {
		mSectorPool[i].mLRUPrev = i+1;
		mSectorPool[i].mLRUNext = i-1;
	}

	mSectorPool[68].mLRUNext = 0;
	mSectorPool[65534].mLRUPrev = 0;
	mSectorPool[0].mLRUPrev = 68;
	mSectorPool[0].mLRUNext = 65534;

	// set up file #1 (root)
	InitRoot();

	// link files 2-2047 into the free list
	for(size_t i=2; i<vdcountof(mFiles); ++i)
		mFileLRU.push_front(&mFiles[i]);

	// set interleave
	Reinterleave(kATDiskInterleave_Default);

	// init watcher (OK if this fails)
	try {
		mDirWatcher.Init(mPath.c_str());
	} catch(const MyError&) {
		// eat it
	}
}

void *ATDiskImageVirtualFolderSDFS::AsInterface(uint32 id) {
	switch(id) {
		case IATDiskImage::kTypeID: return static_cast<IATDiskImage *>(this);
	}

	return nullptr;
}

void ATDiskImageVirtualFolderSDFS::SetPath(const wchar_t *path, ATDiskImageFormat format) {
}

void ATDiskImageVirtualFolderSDFS::Save(const wchar_t *path, ATDiskImageFormat format) {
}

ATDiskGeometryInfo ATDiskImageVirtualFolderSDFS::GetGeometry() const {
	ATDiskGeometryInfo info;
	info.mSectorSize = 128;
	info.mBootSectorCount = 3;
	info.mTotalSectorCount = 65535;
	info.mTrackCount = 1;
	info.mSectorsPerTrack = 65535;
	info.mSideCount = 1;
	info.mbMFM = false;

	return info;
}

uint32 ATDiskImageVirtualFolderSDFS::GetSectorSize() const {
	return 128;
}

uint32 ATDiskImageVirtualFolderSDFS::GetSectorSize(uint32 virtIndex) const {
	return 128;
}

uint32 ATDiskImageVirtualFolderSDFS::GetBootSectorCount() const {
	return 3;
}

uint32 ATDiskImageVirtualFolderSDFS::GetPhysicalSectorCount() const {
	return 65535;
}

void ATDiskImageVirtualFolderSDFS::GetPhysicalSectorInfo(uint32 index, ATDiskPhysicalSectorInfo& info) const {
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

void ATDiskImageVirtualFolderSDFS::ReadPhysicalSector(uint32 index, void *data, uint32 len) {
	memset(data, 0, len);

	if (len != 128 || index >= 65535)
		return;

	// Retrieve the sector key for this sector.
	const uint32 sectorKey = mSectorPool[index].mSectorKey;

	if (sectorKey < 0x00010000) {
		// This is a special sector. Check for a boot sector.
		if (index < 3) {
			if (!index) {
				// Check for a pending change.
				if (mDirWatcher.CheckForChanges(mDirChanges)) {
					// arbitrary change... wipe EVERYTHING
					InvalidateAll();
				} else if (!mDirChanges.empty())
					InvalidatePartial();

				memcpy(data, kATSDFSBootSector0, sizeof kATSDFSBootSector0);

				// increment the volume sequence now if a change is pending -- this ensures that
				// we do not wrap the sequence counter before DOS sees it
				if (mbVolChangePending) {
					mbVolChangePending = false;
					++mVolSeqNumber;
				}

				// patch in volume name/seq/rand
				memcpy((char *)data + 22, mVolumeName, 8);
				((uint8 *)data)[38] = mVolSeqNumber;
				((uint8 *)data)[39] = mVolRandNumber;
			} else if (index == 1)
				memcpy(data, kATSDFSBootSector1, sizeof kATSDFSBootSector1);

			return;
		}

		// Must be bitmap sector.
		//
		// Zero bits mean the sectors are in use, and we already cleared
		// the sector buffer... so we're done here.

		if (!sectorKey) {
			g_ATLCVDisk("Warning: Returning empty data for unallocated sector %u.", index + 1);
		}
		return;
	}

	// Promote this sector to the head of the LRU list.
	PromoteSector(index);

	// Look up the file corresponding to this sector.
	const uint32 fileIdx = sectorKey >> 18;
	PromoteFile(fileIdx);

	File& file = mFiles[fileIdx];
	const uint32 subIndex = sectorKey & 0x1ffff;

	// Okay, this corresponds to a file sector. Let's see if it's a sector map
	// sector.
	if (sectorKey & kSectorKeyMapBit) {
		// Yes, this is part of the sector map. First, scan the directory if it's one and we
		// haven't done so already.
		if (file.mbIsDirectory && !file.mbDirectoryScanned)
			ScanDirectory(file);
		
		// Compute the range of sectors that it maps.
		const uint32 sectorCount = (file.mSize + 127) >> 7;
		const uint32 sectorMapBase = subIndex * 62;

		// Fill out the next pointer.
		VDWriteUnalignedLEU16((char *)data + 0, (sectorMapBase + 62 < sectorCount) ? ReserveSector(sectorKey+1) + 1 : 0);

		// Fill out the prev pointer.
		VDWriteUnalignedLEU16((char *)data + 2, subIndex ? ReserveSector(sectorKey-1) + 1 : 0);		

		// Fill out the data sector pointers, reserving new sectors as necessary.
		const uint32 dataSectorKeyBase = (sectorKey & kSectorKeyFileMask) + sectorMapBase;

		for(uint32 i=0; i<62; ++i) {
			if (sectorMapBase + i >= sectorCount)
				break;

			VDWriteUnalignedLEU16((char *)data + 4 + 2*i, ReserveSector(dataSectorKeyBase + i) + 1);
		}

		return;
	}

	// It's a data sector. Let's see if it's a directory or not.
	if (file.mbIsDirectory) {
		// Yes, it's a directory -- copy directory entries into the buffer, reserving
		// the sector map entries as needed. Annoyingly, since the directory entries are
		// 23 bytes long, it is possible for the sector map pointer to be split between
		// sectors. We conservatively reserve sector keys for all partially covered
		// entries.
		uint32 numEnts = (uint32)file.mXDirEnts.size();
		uint32 ent = (subIndex << 7) / 23;
		sint32 offset = (sint32)ent * 23 - (sint32)(subIndex << 7);

		while(offset < 128 && ent < numEnts) {
			XDirEnt& xde = file.mXDirEnts[ent];

			// update the sector map pointer
			if (!ent) {
				// This is the self entry, where the sector map points to the parent.
				if (file.mRelPath.empty()) {
					// This is the root directory -- parent=0.
					VDWriteUnalignedLEU16(xde.mDirEnt.mSecMap, 0);
				} else {
					// Compute the relative path to the parent.
					const wchar_t *relPath = file.mRelPath.c_str();
					const wchar_t *relPathSplit = VDFileSplitPath(relPath);

					if (relPathSplit != relPath && VDIsPathSeparator(relPathSplit[-1]))
						--relPathSplit;

					// check if we have the root as the parent
					uint32 parentFileId = 1;

					if (relPath != relPathSplit) {
						// not the root -- reserve the parent file (it may have expired!).
						const uint8 *dosPath = file.mDOSPath.data();
						const uint8 *dosPathEnd = file.mDOSPath.size() >= 22 ? dosPath + file.mDOSPath.size() - 22 : dosPath;

						parentFileId = ReserveFile(VDStringW(relPath, relPathSplit).c_str(), true, sizeof(DirEnt), dosPathEnd, dosPathEnd + 8, vdspan<const uint8>(dosPath, dosPathEnd));
					}

					// set the sector map pointer to that of the parent
					const uint32 mapStartSectorKey = (parentFileId << kSectorKeyFileShift) + kSectorKeyMapBit;
					VDWriteUnalignedLEU16(xde.mDirEnt.mSecMap, ReserveSector(mapStartSectorKey) + 1);
				}

			} else if (xde.mSize || (xde.mDirEnt.mFlags & DirEnt::kFlagDirectory)) {
				const uint32 fileId = ReserveFile(xde, vdspan<const uint8>(file.mDOSPath.begin(), file.mDOSPath.end()));
				const uint32 mapStartSectorKey = (fileId << kSectorKeyFileShift) + kSectorKeyMapBit;
				VDWriteUnalignedLEU16(xde.mDirEnt.mSecMap, ReserveSector(mapStartSectorKey) + 1);
			} else
				VDWriteUnalignedLEU16(xde.mDirEnt.mSecMap, 0);

			// copy the directory entry
			sint32 dstoffset = offset;
			uint32 srcoffset = 0;
			sint32 copylen = 23;

			if (dstoffset < 0) {
				srcoffset = -dstoffset;
				copylen += dstoffset;
				dstoffset = 0;
			}

			if (dstoffset + copylen > (sint32)len)
				copylen = len - dstoffset;

			VDASSERT(copylen > 0);

			memcpy((char *)data + dstoffset, (const char *)&xde.mDirEnt + srcoffset, copylen);

			++ent;
			offset += 23;
		}
	} else {
		// No, it's not a directory. Open the file if necessary.
		if (!file.mFile.isOpen()) {
			if (!file.mFile.isOpen()) {
				g_ATLCVDisk("Opening file: %ls\n", file.mRelPath.c_str());
				file.mFile.open(VDMakePath(mPath.c_str(), file.mRelPath.c_str()).c_str());
			}
		}

		// Read sector data.
		uint32 byteOffset = subIndex << 7;
		uint32 validLen = 0;

		if (byteOffset < file.mSize)
			validLen = std::min<uint32>(len, file.mSize - byteOffset);

		file.mFile.seek(byteOffset);
		file.mFile.read(data, validLen);

		mCloseTimer.SetOneShot(this, 3000);
	}
}

void ATDiskImageVirtualFolderSDFS::WritePhysicalSector(uint32 index, const void *data, uint32 len, uint8 fdcStatus) {
	throw MyError("Writes are not supported to a virtual disk.");
}

uint32 ATDiskImageVirtualFolderSDFS::GetVirtualSectorCount() const {
	return 65535;
}

void ATDiskImageVirtualFolderSDFS::GetVirtualSectorInfo(uint32 index, ATDiskVirtualSectorInfo& info) const {
	info.mStartPhysSector = index;
	info.mNumPhysSectors = 1;
}

uint32 ATDiskImageVirtualFolderSDFS::ReadVirtualSector(uint32 index, void *data, uint32 len) {
	if (len < 128)
		return 0;

	ReadPhysicalSector(index, data, len > 128 ? 128 : len);
	return 128;
}

bool ATDiskImageVirtualFolderSDFS::WriteVirtualSector(uint32 index, const void *data, uint32 len) {
	return false;
}

void ATDiskImageVirtualFolderSDFS::Resize(uint32 sectors) {
	throw MyError("A virtual disk cannot be resized.");
}

void ATDiskImageVirtualFolderSDFS::FormatTrack(uint32 vsIndexStart, uint32 vsCount, const ATDiskVirtualSectorInfo *vsecs, uint32 psCount, const ATDiskPhysicalSectorInfo *psecs, const uint8 *psecData) {
	throw MyError("A virtual disk cannot be formatted.");
}

bool ATDiskImageVirtualFolderSDFS::IsSafeToReinterleave() const {
	return true;
}

void ATDiskImageVirtualFolderSDFS::Reinterleave(ATDiskInterleave interleave) {
	mpInterleaveFn = ATDiskGetInterleaveFn(interleave, GetGeometry());
}

void ATDiskImageVirtualFolderSDFS::InitRoot() {
	File& root = mFiles[1];
	root.mbIsDirectory = true;
	root.mbDirectoryScanned = false;
	root.mSize = sizeof(DirEnt);

	XDirEnt& xdeBase = root.mXDirEnts.push_back();
	memset(&xdeBase.mDirEnt, 0, sizeof(DirEnt));
	memset(xdeBase.mDirEnt.mName, 0x20, 8);
	memset(xdeBase.mDirEnt.mExt, 0x20, 3);
	xdeBase.mDirEnt.mName[0] = 'M';
	xdeBase.mDirEnt.mName[1] = 'A';
	xdeBase.mDirEnt.mName[2] = 'I';
	xdeBase.mDirEnt.mName[3] = 'N';
}

void ATDiskImageVirtualFolderSDFS::InvalidateAll() {
	mbVolChangePending = true;

	for(size_t i=0; i<vdcountof(mpFileHash); ++i) {
		while(mpFileHash[i])
			UnlinkFile((uint32)(mpFileHash[i] - mFiles), false);
	}

	for(uint32 sector = kSectorPoolBase; sector < kSectorPoolLimit; ++sector)
		UnlinkSector(sector);
}

void ATDiskImageVirtualFolderSDFS::InvalidatePartial() {
	const wchar_t *changeBase = mDirChanges.data();
	const wchar_t *changeLimit = changeBase + mDirChanges.size();

	while(changeBase != changeLimit) {
		size_t changeLen = wcslen(changeBase);

		// invalidate every file with this base (yes, this is slow)
		for(size_t i=1; i<vdcountof(mFiles); ++i) {
			const VDStringW& relPath = mFiles[i].mRelPath;
			const wchar_t *relPathPtr = relPath.c_str();

			if (!changeLen || (relPath.size() >= changeLen &&
				vdwcsnicmp(relPathPtr, changeBase, changeLen) == 0 &&
				(relPathPtr[changeLen] == 0 || VDIsPathSeparator(relPathPtr[changeLen]))))
			{
				UnlinkFile((uint32)i, true);

				if (i == 1)
					InitRoot();

				mbVolChangePending = true;
			}
		}

		// skip to next change
		while(*changeBase++)
			;
	}

	mDirChanges.clear();
}

void ATDiskImageVirtualFolderSDFS::TimerCallback() {
	for(size_t i=0; i<vdcountof(mFiles); ++i) {
		File& f = mFiles[i];

		if (f.mFile.isOpen()) {
			g_ATLCVDisk("Closing file: %ls\n", f.mRelPath.c_str());
			f.mFile.closeNT();
		}
	}
}

void ATDiskImageVirtualFolderSDFS::ScanDirectory(File& dir) {
	VDASSERT(dir.mbIsDirectory);

	dir.mbDirectoryScanned = true;

	// scan the directory
	VDDirectoryIterator dirIt(VDMakePath(VDMakePath(mPath.c_str(), dir.mRelPath.c_str()).c_str(), L"*").c_str());

	while(dirIt.Next() && dir.mXDirEnts.size() < 256) {
		// skip dot directories
		if (dirIt.IsDotDirectory())
			continue;

		// check if this is a super-secret file that Explorer likes to hide
		const uint32 attr = dirIt.GetAttributes();

		if ((attr & (kVDFileAttr_Hidden | kVDFileAttr_System)) == (kVDFileAttr_Hidden | kVDFileAttr_System))
			continue;

		// check if this file is too big
		if (dirIt.GetSize() > 0xffffff)
			continue;

		// okay, let's encode it
		XDirEnt& xde = dir.mXDirEnts.push_back();
		xde.mRelPath = VDMakePath(dir.mRelPath.c_str(), dirIt.GetName());
		xde.mSize = (uint32)dirIt.GetSize();

		xde.mDirEnt.mFlags = DirEnt::kFlagInUse;

		if (dirIt.IsDirectory()) {
			xde.mDirEnt.mFlags |= DirEnt::kFlagDirectory;

			// The size of a directory in the parent is not kept up to date
			// by SpartaDOS -- that's why the directory itself contains its
			// real size at the beginning. This is fortunate as it saves us
			// a lot of updating trouble.
			xde.mSize = sizeof(DirEnt);

		} else if (attr & kVDFileAttr_ReadOnly) {
			// In Windows, read-only on a directory is a hint to Explorer, not a lock
			// state. Therefore, we only propagate the read-only flag to SDFS for files.
			xde.mDirEnt.mFlags |= DirEnt::kFlagLocked;
		}

		if (attr & kVDFileAttr_Archive)
			xde.mDirEnt.mFlags |= DirEnt::kFlagArchived;

		if (attr & kVDFileAttr_Hidden)
			xde.mDirEnt.mFlags |= DirEnt::kFlagHidden;

		xde.mDirEnt.mSecMap[0] = 0;
		xde.mDirEnt.mSecMap[1] = 0;

		const wchar_t *name = dirIt.GetName();
		const wchar_t *ext = VDFileSplitExt(name);

		int encNameLen = 0;
		int encExtLen = 0;

		// encode name and ext chars
		for(const wchar_t *s = name; *s && *s != '.' && encNameLen < 8; ++s) {
			uint32 c = (uint32)*s;

			if (c >= 'a' && c <= 'z')
				c &= 0xDF;
			else if ((c < 'A' || c > 'Z') && (c < '0' || c > '9') && c != '_')
				continue;

			xde.mDirEnt.mName[encNameLen++] = (uint8)c;
		}

		if (*ext == '.')
			++ext;

		for(const wchar_t *s = ext; *s && *s != '.' && encExtLen < 3; ++s) {
			uint32 c = (uint32)*s;

			if (c >= 'a' && c <= 'z')
				c &= 0xDF;
			else if ((c < 'A' || c > 'Z') && (c < '0' || c > '9') && c != '_')
				continue;

			xde.mDirEnt.mExt[encExtLen++] = (uint8)c;
		}

		// space pad both the name and extension
		while(encNameLen < 8)
			xde.mDirEnt.mName[encNameLen++] = 0x20;

		while(encExtLen < 3)
			xde.mDirEnt.mExt[encExtLen++] = 0x20;

		// encode size
		xde.mDirEnt.mSize[0] = (uint8)xde.mSize;
		xde.mDirEnt.mSize[1] = (uint8)(xde.mSize >> 8);
		xde.mDirEnt.mSize[2] = (uint8)(xde.mSize >> 16);

		// encode date
		VDDate fileDate = dirIt.GetLastWriteDate();
		const VDExpandedDate& expDate = VDGetLocalDate(fileDate);
		xde.mDirEnt.mDay = expDate.mDay;
		xde.mDirEnt.mMonth = expDate.mMonth;
		xde.mDirEnt.mYear = expDate.mYear % 100;
		xde.mDirEnt.mHour = expDate.mHour;
		xde.mDirEnt.mMinute = expDate.mMinute;
		xde.mDirEnt.mSecond = expDate.mSecond;
	}

	// create a pointer index of all entries
	vdfastvector<XDirEnt *> dirIndex;
	dirIndex.reserve(dir.mXDirEnts.size() - 1);
	for(vdfastvector<XDirEnt>::iterator it(dir.mXDirEnts.begin() + 1), itEnd(dir.mXDirEnts.end());
		it != itEnd;
		++it)
	{
		dirIndex.push_back(&*it);
	}

	// sort the index by name
	std::sort(dirIndex.begin(), dirIndex.end(), XDirEntIndexPred());

	// resolve 8.3 name conflicts
	vdhashset<XDirEnt *, XDirEntNameHash, XDirEntNamePred> nameSet;
	for(vdfastvector<XDirEnt *>::const_iterator it(dirIndex.begin()), itEnd(dirIndex.end());
		it != itEnd;
		++it)
	{
		XDirEnt& xde = **it;

		while(!nameSet.insert(&xde).second) {
			// determine how many spaces we have at the end of the name
			int nameLen = 8;

			while(nameLen > 1 && xde.mDirEnt.mName[nameLen - 1] == ' ')
				--nameLen;

			// increment the name
			for(int i=nameLen-1; i>=0; --i) {
				if (i && xde.mDirEnt.mName[i-1] == ' ')
					continue;

				if ((unsigned)(xde.mDirEnt.mName[i]-'0') >= 10) {
					// We have run out of numbers with the current set of digits.
					// Check if we have spaces at the end. If we do, increase the
					// length of the filename to add another digit; otherwise, stomp
					// the preceding character.
					if (nameLen < 8) {
						++i;
						memmove(xde.mDirEnt.mName + i + 1, xde.mDirEnt.mName + i, nameLen - i);
					}

					xde.mDirEnt.mName[i] = '1';
					break;
				} else if (xde.mDirEnt.mName[i] == '9') {
					xde.mDirEnt.mName[i] = '0';
				} else {
					++xde.mDirEnt.mName[i];
					break;
				}
			}
		}
	}

	// fix up the size of the directory file
	const uint32 dirSize = (uint32)(sizeof(DirEnt) * dir.mXDirEnts.size());
	dir.mSize = dirSize;

	// patch the first entry (note that we need to re-get this entry!)
	XDirEnt& xdeBase2 = dir.mXDirEnts.front();
	xdeBase2.mDirEnt.mSize[0] = (uint8)(dirSize >> 0);
	xdeBase2.mDirEnt.mSize[1] = (uint8)(dirSize >> 8);
	xdeBase2.mDirEnt.mSize[2] = (uint8)(dirSize >> 16);
}

uint32 ATDiskImageVirtualFolderSDFS::ReserveFile(const XDirEnt& xde, const vdspan<const uint8>& parentDosPath) {
	const bool isDir = (xde.mDirEnt.mFlags & DirEnt::kFlagDirectory) != 0;

	return ReserveFile(xde.mRelPath.c_str(), isDir, xde.mSize, xde.mDirEnt.mName, xde.mDirEnt.mExt, parentDosPath);
}

uint32 ATDiskImageVirtualFolderSDFS::ReserveFile(const wchar_t *relPath, bool isDir, uint32 size, const uint8 name[8], const uint8 ext[3], const vdspan<const uint8>& parentDosPath) {
	if (!*relPath)
		return 1;

	const uint32 hc = VDHashString32I(relPath);
	const uint32 hidx = hc & 0xff;

	for(File *f = mpFileHash[hidx]; f; f = f->mpHashNext) {
		if (f->mHashCode == hc && f->mRelPath == relPath)
			return (uint32)(f - mFiles);
	}

	// allocate a new file and set it up
	File& file = *mFileLRU.back();
	uint32 fidx = (uint32)(&file - mFiles);

	g_ATLCVDisk("Allocating index %u to file %ls\n", fidx, relPath);

	UnlinkFile(fidx, true);

	PromoteFile(fidx);
	file.mpHashNext = mpFileHash[hidx];
	mpFileHash[hidx] = &file;

	file.mHashCode = hc;
	file.mFile.closeNT();
	file.mRelPath = relPath;
	file.mSize = size;
	file.mbIsDirectory = isDir;
	file.mbDirectoryScanned = false;
	file.mXDirEnts.clear();
	file.mDOSPath.reserve(parentDosPath.size() + 11);
	file.mDOSPath.assign(parentDosPath.begin(), parentDosPath.end());
	file.mDOSPath.insert(file.mDOSPath.end(), name, name + 8);
	file.mDOSPath.insert(file.mDOSPath.end(), ext, ext + 3);

	if (file.mbIsDirectory) {
		// populate the self/parent entry
		XDirEnt& xdeBase = file.mXDirEnts.push_back();

		memset(&xdeBase.mDirEnt, 0, sizeof(xdeBase.mDirEnt));

		memcpy(xdeBase.mDirEnt.mName, name, 8);
		memcpy(xdeBase.mDirEnt.mExt, ext, 3);

		xdeBase.mRelPath.assign(relPath, VDFileSplitPath(relPath));
		if (!xdeBase.mRelPath.empty() && VDIsPathSeparator(xdeBase.mRelPath.back()))
			xdeBase.mRelPath.pop_back();

		xdeBase.mDirEnt.mFlags = DirEnt::kFlagInUse | DirEnt::kFlagDirectory;
	}

	return fidx;
}

void ATDiskImageVirtualFolderSDFS::PromoteFile(uint32 fidx) {
	if (fidx >= 2) {
		File& file = mFiles[fidx];

		if (mFileLRU.front() != &file)
			mFileLRU.splice(mFileLRU.begin(), mFileLRU, mFileLRU.fast_find(&file));
	}
}

void ATDiskImageVirtualFolderSDFS::UnlinkFile(uint32 index, bool scanSectors) {
	if (scanSectors) {
		const uint32 fileId = index << kSectorKeyFileShift;
		uint32 invSectorCount = 0;

		for(uint32 i=kSectorPoolBase; i<kSectorPoolLimit; ++i) {
			if ((mSectorPool[i].mSectorKey & kSectorKeyFileMask) == fileId) {
				UnlinkSector(i);
				++invSectorCount;
			}
		}

		if (invSectorCount)
			g_ATLCVDisk("Invalidating %u sectors for file: %ls\n", invSectorCount, mFiles[index].mRelPath.c_str());
	}

	// unlink from the hash table
	File& file = mFiles[index];
	File **fp = &mpFileHash[file.mHashCode & 0xff];
	while(File *f = *fp) {
		if (f == &file) {
			*fp = file.mpHashNext;
			break;
		}

		fp = &f->mpHashNext;
	}

	// clear the file
	file.mHashCode = 0;
	file.mFile.closeNT();
	file.mRelPath.clear();
	file.mSize = 0;
	file.mbIsDirectory = false;
	file.mbDirectoryScanned = false;
	file.mXDirEnts.clear();
}

uint32 ATDiskImageVirtualFolderSDFS::ReserveSector(uint32 sectorKey) {
	const uint32 hc = HashSectorKey(sectorKey);
	uint32 index = FindSector(sectorKey, hc);

	if (!index) {
		index = AllocateSector();

		mSectorPool[index].mSectorKey = sectorKey;
		mSectorPool[index].mHashNext = mSectorHash[hc];
		mSectorHash[hc] = index;

		g_ATLCVDisk("Allocating sector %u to %s sector %u of file: %ls\n"
			, index + 1
			, sectorKey & kSectorKeyMapBit ? "map" : "data"
			, sectorKey & kSectorKeyOffsetMask
			, mFiles[sectorKey >> kSectorKeyFileShift].mRelPath.c_str()
			);
	}

	return index;
}

void ATDiskImageVirtualFolderSDFS::PromoteSector(uint32 index) {
	if (index >= 68 && index < 65535) {
		SectorNode& sc = mSectorPool[index];
		SectorNode& sp = mSectorPool[sc.mLRUPrev];
		SectorNode& sn = mSectorPool[sc.mLRUNext];

		// delink node
		sp.mLRUNext = sc.mLRUNext;
		sn.mLRUPrev = sc.mLRUPrev;

		// relink at head
		SectorNode& sh = mSectorPool[0];
		mSectorPool[sh.mLRUNext].mLRUPrev = index;
		sc.mLRUNext = sh.mLRUNext;
		sc.mLRUPrev = 0;
		sh.mLRUNext = index;
	}
}

uint32 ATDiskImageVirtualFolderSDFS::AllocateSector() {
	// Grab the next sector at the tail of the LRU list.
	const uint32 index = mSectorPool[0].mLRUPrev;

	// Delink it from the sector key hash table.
	UnlinkSector(index);

	// Move it to the head.
	PromoteSector(index);

	return index;
}

void ATDiskImageVirtualFolderSDFS::UnlinkSector(uint32 index) {
	const uint32 sectorKey = mSectorPool[index].mSectorKey;
	if (!sectorKey)
		return;

	mSectorPool[index].mSectorKey = 0;

	const uint32 hc = HashSectorKey(sectorKey);
	uint32 prev = mSectorHash[hc];
	uint32 next = mSectorPool[index].mHashNext;

	if (prev == index)
		mSectorHash[hc] = next;
	else {
		while(prev) {
			const uint32 prevNext = mSectorPool[prev].mHashNext;

			if (prevNext == index) {
				mSectorPool[prev].mHashNext = next;
				return;
			}

			prev = prevNext;
		}

		VDASSERT(!"Unable to find sector in hash table.");
	}
}

uint32 ATDiskImageVirtualFolderSDFS::FindSector(uint32 sectorKey) const {
	uint32 hc = HashSectorKey(sectorKey);

	return FindSector(sectorKey, hc);
}

uint32 ATDiskImageVirtualFolderSDFS::FindSector(uint32 sectorKey, uint32 hc) const {

	for(uint32 idx = mSectorHash[hc]; idx; idx = mSectorPool[idx].mHashNext) {
		if (mSectorPool[idx].mSectorKey == sectorKey)
			return idx;
	}

	return 0;
}

uint32 ATDiskImageVirtualFolderSDFS::HashSectorKey(uint32 sectorKey) const {
	return (uint8)(((uint64)sectorKey * 0x01010101) >> 32);
}

///////////////////////////////////////////////////////////////////////////

void ATMountDiskImageVirtualFolderSDFS(const wchar_t *path, uint32 sectorCount, uint64 unique, IATDiskImage **ppImage) {
	vdrefptr<ATDiskImageVirtualFolderSDFS> p(new ATDiskImageVirtualFolderSDFS);
	
	p->Init(path, unique);
	*ppImage = p.release();
}
