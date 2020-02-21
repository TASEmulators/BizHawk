//	Altirra - Atari 800/800XL/5200 emulator
//	I/O library - filesystem utilities
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

#ifndef f_AT_ATIO_DISKFSUTIL_H
#define f_AT_ATIO_DISKFSUTIL_H

#include <at/atio/diskfs.h>

class ATDiskFSDirScanIterator;

class ATDiskFSDirScan {
public:
	ATDiskFSDirScan(IATDiskFS& fs, ATDiskFSKey key);
	~ATDiskFSDirScan();

	ATDiskFSDirScanIterator begin();
	ATDiskFSDirScanIterator end();

private:
	friend class ATDiskFSDirScanIterator;

	IATDiskFS& mFS;
	ATDiskFSFindHandle mFH;
	ATDiskFSEntryInfo mEntry;
};

class ATDiskFSDirScanIteratorProxy {
public:
	ATDiskFSDirScanIteratorProxy(const ATDiskFSEntryInfo& e);

	const ATDiskFSEntryInfo& operator*() const { return mEntry; }

private:
	ATDiskFSEntryInfo mEntry;
};

class ATDiskFSDirScanIterator {
public:
	typedef ptrdiff_t difference_type;
	typedef const ATDiskFSEntryInfo value_type;
	typedef const ATDiskFSEntryInfo *pointer;
	typedef const ATDiskFSEntryInfo& reference_type;
	typedef std::input_iterator_tag iterator_category;

	ATDiskFSDirScanIterator(ATDiskFSDirScan *parent)
		: mpParent(parent) {}

	ATDiskFSDirScanIterator& operator++();
	ATDiskFSDirScanIteratorProxy operator++(int);

	const ATDiskFSEntryInfo& operator*() const { return mpParent->mEntry; }
	const ATDiskFSEntryInfo *operator->() const { return &mpParent->mEntry; }

	bool operator==(const ATDiskFSDirScanIterator& other) const {
		return mpParent == other.mpParent;
	}

	bool operator!=(const ATDiskFSDirScanIterator& other) const {
		return mpParent != other.mpParent;
	}

private:
	ATDiskFSDirScan *mpParent;
};

void ATDiskFSCopyTree(IATDiskFS& dst, ATDiskFSKey dstKey, IATDiskFS& src, ATDiskFSKey srcKey, bool allowNameRehashing);
uint32 ATDiskFSEstimateDOS2SectorsNeeded(IATDiskFS& src, uint32 sectorSize);
uint32 ATDiskFSEstimateMyDOSSectorsNeeded(IATDiskFS& src, uint32 sectorSize);
uint32 ATDiskFSEstimateSDX2SectorsNeeded(IATDiskFS& src, uint32 sectorSize);

uint32 ATDiskRecursivelyExpandARCs(IATDiskFS& fs);

#endif
