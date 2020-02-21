//	Altirra - Atari 800/800XL/5200 emulator
//	I/O library - filesystem handler defines
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
#include <vd2/system/file.h>
#include <vd2/system/function.h>
#include <vd2/system/strutil.h>
#include <at/atio/diskfs.h>
#include <at/atio/diskfsutil.h>

ATDiskFSDirScan::ATDiskFSDirScan(IATDiskFS& fs, ATDiskFSKey key)
	: mFS(fs)
	, mFH(ATDiskFSFindHandle::Invalid)
{
	mFH = fs.FindFirst(key, mEntry);
}

ATDiskFSDirScan::~ATDiskFSDirScan() {
	if (mFH != ATDiskFSFindHandle::Invalid)
		mFS.FindEnd(mFH);
}

ATDiskFSDirScanIterator ATDiskFSDirScan::begin() {
	return ATDiskFSDirScanIterator(mFH != ATDiskFSFindHandle::Invalid ? this : nullptr);
}

ATDiskFSDirScanIterator ATDiskFSDirScan::end() {
	return ATDiskFSDirScanIterator(nullptr);
}

///////////////////////////////////////////////////////////////////////////

ATDiskFSDirScanIteratorProxy::ATDiskFSDirScanIteratorProxy(const ATDiskFSEntryInfo& e)
	: mEntry(e)
{
}

///////////////////////////////////////////////////////////////////////////

ATDiskFSDirScanIterator& ATDiskFSDirScanIterator::operator++() {
	if (!mpParent->mFS.FindNext(mpParent->mFH, mpParent->mEntry))
		mpParent = nullptr;

	return *this;
}

ATDiskFSDirScanIteratorProxy ATDiskFSDirScanIterator::operator++(int) {
	ATDiskFSDirScanIteratorProxy proxy { mpParent->mEntry };

	operator++();
	return proxy;
}

///////////////////////////////////////////////////////////////////////////

void ATDiskFSCopyTree(IATDiskFS& dst, ATDiskFSKey dstKey, IATDiskFS& src, ATDiskFSKey srcKey, bool allowNameRehashing) {
	struct TransferStep {
		ATDiskFSKey srcKey;
		ATDiskFSKey dstKey;
	};
	
	vdfastvector<uint8> buf;
	vdfastvector<TransferStep> transferStack;

	transferStack.push_back({srcKey, dstKey});

	const auto rehashName = [](VDStringA& buf, const char *prevName, uint32 hashIdx, bool allowUnderscores) {
		buf.clear();
		
		// compute hash
		uint32 hash = 2166136261U;
		for(const char *s = prevName; *s; ++s)
			hash = (hash * 16777619U) ^ (unsigned char)*s;

		hash ^= hashIdx*hashIdx;

		// create hash string
		uint8 c0 = 0x30 + (hash%36);
		uint8 c1 = 0x30 + (hash/36)%36;

		if (c0 >= 0x3A) c0 += (0x41 - 0x3A);
		if (c1 >= 0x3A) c1 += (0x41 - 0x3A);

		char hashBuf[4];

		if (allowUnderscores) {
			hashBuf[0] = '_';
			hashBuf[1] = (char)c0;
			hashBuf[2] = (char)c1;
			hashBuf[3] = 0;
		} else {
			hashBuf[0] = (char)c0;
			hashBuf[1] = (char)c1;
			hashBuf[2] = 0;
		}

		uint32 fnlen = 0;
		uint32 extlen = 0;
		bool inExt = false;
		while(uint8 c = (uint8)*prevName++) {
			if (c == '.') {
				if (!inExt) {
					inExt = true;
					buf += hashBuf;
					buf += '.';
				}
			} else {
				// check if digit
				if ((uint8)(c - 0x30) >= 10) {
					// check if letter and convert lowercase
					if ((uint8)(c - 0x61) < 26)
						c -= 0x20;
					else if ((uint8)(c - 0x41) >= 26)
						continue;

					if (c == (uint8)'_' && !allowUnderscores)
						continue;
				}

				if (inExt) {
					if (extlen >= 3)
						continue;

					++extlen;
				} else {
					if (fnlen >= (allowUnderscores ? 5U : 6U))
						continue;

					++fnlen;
				}

				buf += (char)c;
			}
		}

		if (!inExt)
			buf += hashBuf;
	};

	while(!transferStack.empty()) {
		const auto xstep = transferStack.back();
		transferStack.pop_back();

		srcKey = xstep.srcKey;
		dstKey = xstep.dstKey;

		for(const ATDiskFSEntryInfo& einfo : ATDiskFSDirScan(src, srcKey)) {
			ATDiskFSKey newKey;
			VDStringA newName = einfo.mFileName;

			if (!einfo.mbIsDirectory)
				src.ReadFile(einfo.mKey, buf);

			bool allowUnderscores = true;
			for(uint32 hashIdx = 0;; ++hashIdx) {
				try {
					if (einfo.mbIsDirectory)
						newKey = dst.CreateDir(dstKey, newName.c_str());
					else
						newKey = dst.WriteFile(dstKey, newName.c_str(), buf.data(), (uint32)buf.size());
					break;
				} catch(const ATDiskFSException& e) {
					if (hashIdx >= 256 || !allowNameRehashing)
						throw;

					const auto code = e.GetErrorCode();
					if (hashIdx > 0 && code == kATDiskFSError_InvalidFileName)
						allowUnderscores = false;
					else if (code != kATDiskFSError_FileExists)
						throw;
						
					rehashName(newName, einfo.mFileName.c_str(), hashIdx, allowUnderscores);
				}
			}

			if (einfo.mbIsDirectory)
				transferStack.push_back({einfo.mKey, newKey});

			if (einfo.mbDateValid)
				dst.SetFileTimestamp(newKey, einfo.mDate);
		}
	}
}

void ATDiskFSSummarize(IATDiskFS& src, vdfunction<void(uint32)> onDir, vdfunction<void(uint32)> onFile) {
	vdfastvector<ATDiskFSKey> traversalStack;

	ATDiskFSKey key = ATDiskFSKey::None;

	for(;;) {
		uint32 dirents = 0;

		for(const ATDiskFSEntryInfo& einfo : ATDiskFSDirScan(src, key)) {
			if (einfo.mbIsDirectory)
				traversalStack.push_back(einfo.mKey);
			else
				onFile(einfo.mBytes);

			++dirents;
		}

		onDir(dirents);

		if (traversalStack.empty())
			break;

		key = traversalStack.back();
		traversalStack.pop_back();
	}
}

uint32 ATDiskFSEstimateDOS2SectorsNeeded(IATDiskFS& src, uint32 sectorSize) {
	// there is only one possible disk size for DOS 2.0D
	if (sectorSize > 128)
		return 720;

	uint32 sectors = 13;	// 3 boot sectors + 1 VTOC + 8 dir + reserved 720

	ATDiskFSSummarize(src,
		[](uint32 dirents) {},
		[&](uint32 fileSize) {
			if (fileSize)
				sectors += (fileSize - 1) / (sectorSize - 3) + 1;
			else
				++sectors;
		}
	);

	if (sectors < 720)
		return 720;
	else
		return 1040;
}

uint32 ATDiskFSEstimateMyDOSSectorsNeeded(IATDiskFS& src, uint32 sectorSize) {
	uint32 sectors = 3;		// boot sectors

	ATDiskFSSummarize(src,
		[&](uint32 dirents) {
			sectors += 8;
		},
		[&](uint32 fileSize) {
			if (fileSize)
				sectors += (fileSize - 1) / (sectorSize - 3) + 1;
			else
				++sectors;
		}
	);

	// Compute needed VTOC size. We run the loop twice in case adding the
	// VTOC sectors itself requires adding another VTOC sector.
	uint32 vtocSize = 1;

	for(int i=0; i<2; ++i) {
		if (sectorSize < 256) {
			if (sectors + vtocSize >= 944)
				vtocSize = ((sectors + vtocSize + 1 + 80 + 2047) >> 11) * 2;

		} else {
			if (sectors + vtocSize >= 1024)
				vtocSize = ((sectors + vtocSize) + 1 + 80 + 2047) >> 11;
		}
	}

	uint32 totalSectors = sectors + vtocSize;

	if (totalSectors < 720)
		totalSectors = 720;
	else if (totalSectors < 1040 && sectorSize == 128)
		totalSectors = 1040;
	else if (totalSectors < 1440)
		totalSectors = 1440;

	return totalSectors;
}

uint32 ATDiskFSEstimateSDX2SectorsNeeded(IATDiskFS& src, uint32 sectorSize) {
	uint32 sectors = sectorSize >= 512 ? 1 : 3;		// boot sectors

	const uint32 sectorsPerMapSector = sectorSize / 2 - 2;
	ATDiskFSSummarize(src,
		[&](uint32 dirents) {
			// compute raw directory size
			const uint32 dirSectors = (23 * (dirents + 1) - 1) / sectorSize + 1;

			// compute number of map sectors
			const uint32 mapSectors = (dirSectors - 1) / sectorsPerMapSector + 1;

			sectors += dirSectors + mapSectors;
		},
		[&](uint32 fileSize) {
			// compute number of data sectors
			const uint32 dataSectors = (fileSize + sectorSize - 1) / sectorSize;

			// compute number of map sectors
			const uint32 mapSectors = dataSectors ? (dataSectors - 1) / sectorsPerMapSector + 1 : 1;

			sectors += dataSectors + mapSectors;
		}
	);

	// Compute needed bitmap size. We run the loop twice in case adding the
	// bitmap sectors itself requires adding another bitmap sector.
	uint32 bitmapSize = 1;

	for(int i=0; i<2; ++i) {
		if (sectorSize >= 512)
			bitmapSize = (sectors + bitmapSize + 1 + 4095) >> 12;
		else if (sectorSize >= 256)
			bitmapSize = (sectors + bitmapSize + 1 + 2047) >> 11;
		else
			bitmapSize = ((sectors + bitmapSize) + 1 + 1023) >> 10;
	}

	uint32 totalSectors = sectors + bitmapSize;

	if (totalSectors < 720)
		totalSectors = 720;
	else if (totalSectors < 1040 && sectorSize == 128)
		totalSectors = 1040;
	else if (totalSectors < 1440)
		totalSectors = 1440;

	return totalSectors;
}

///////////////////////////////////////////////////////////////////////////

uint32 ATDiskRecursivelyExpandARCs(IATDiskFS& fs, ATDiskFSKey parentKey, int nestingDepth) {
	uint32 totalExpanded = 0;
	vdfastvector<uint8> buf;
	vdfastvector<uint8> buf2;

	uint32 tempfnCounter = 1;

	// Preread the directory. Our filesystems don't allow modifications during
	// directory searches just yet.
	vdvector<ATDiskFSEntryInfo> arcEnts;
	vdfastvector<ATDiskFSKey> dirKeys;

	for(const ATDiskFSEntryInfo& info : ATDiskFSDirScan(fs, parentKey)) {
		size_t len = info.mFileName.size();

		if (info.mbIsDirectory)
			dirKeys.push_back(info.mKey);
		else if (len > 4 && !vdstricmp(info.mFileName.c_str() + len - 4, ".arc")) {
			arcEnts.push_back(info);
		}
	}

	for(const auto& arcInfo : arcEnts) {
		// try to read and mount the ARChive
		fs.ReadFile(arcInfo.mKey, buf);

		VDMemoryStream ms(buf.data(), (uint32)buf.size());
		vdautoptr<IATDiskFS> arcfs;

		try {
			arcfs = ATDiskMountImageARC(ms, VDTextAToW(arcInfo.mFileName).c_str());
		} catch(ATDiskFSException) {
			// Hmm, we couldn't extract. Oh well, skip it.
		}

		if (arcfs) {
			// Hey, we mounted the ARChive. Try to create a temporary directory.
			VDStringA fn;
			ATDiskFSKey tempDirKey = ATDiskFSKey::None;
					
			for(int i=0; i<100; ++i) {
				fn.sprintf("arct%u.tmp", tempfnCounter++);

				try {
					tempDirKey = fs.CreateDir(parentKey, fn.c_str());
					break;
				} catch(const ATDiskFSException& e) {
					if (e.GetErrorCode() != kATDiskFSError_FileExists)
						throw;
				}
			}

			if (tempDirKey != ATDiskFSKey::None) {
				// Hey, we got a key. Okay, let's copy files one at a time.
				ATDiskFSEntryInfo info2;

				for(const ATDiskFSEntryInfo& info2 : ATDiskFSDirScan(*arcfs, ATDiskFSKey::None)) {
					arcfs->ReadFile(info2.mKey, buf2);

					ATDiskFSKey fileKey = fs.WriteFile(tempDirKey, info2.mFileName.c_str(), buf2.data(), (uint32)buf2.size());

					if (info2.mbDateValid)
						fs.SetFileTimestamp(fileKey, info2.mDate);
				}

				// Delete the archive.
				fs.DeleteFile(arcInfo.mKey);

				// Rename the temporary directory to match the original archive.
				fs.RenameFile(tempDirKey, arcInfo.mFileName.c_str());

				// Set the timestamp on the directory to match the original archive.
				if (arcInfo.mbDateValid)
					fs.SetFileTimestamp(tempDirKey, arcInfo.mDate);

				// Queue this directory for a rescan in case there are sub-ARCs.
				dirKeys.push_back(tempDirKey);

				++totalExpanded;
			}
		}
	}

	// Recursively all subdirectories.
	if (nestingDepth < 32) {
		for(ATDiskFSKey subDirKey : dirKeys)
			totalExpanded += ATDiskRecursivelyExpandARCs(fs, subDirKey, nestingDepth + 1);
	}

	return totalExpanded;
}

uint32 ATDiskRecursivelyExpandARCs(IATDiskFS& fs) {
	return ATDiskRecursivelyExpandARCs(fs, ATDiskFSKey::None, 0);
}
