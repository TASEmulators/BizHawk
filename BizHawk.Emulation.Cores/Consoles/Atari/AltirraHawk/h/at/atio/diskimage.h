//	Altirra - Atari 800/800XL/5200 emulator
//	I/O library - disk image definitions
//	Copyright (C) 2008-2015 Avery Lee
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

#ifndef f_AT_ATIO_DISKIMAGE_H
#define f_AT_ATIO_DISKIMAGE_H

#include <optional>
#include <vd2/system/function.h>
#include <vd2/system/refcount.h>
#include <at/atio/image.h>

class IVDRandomAccessStream;

enum ATDiskTimingMode {
	kATDiskTimingMode_Any,
	kATDiskTimingMode_UsePrecise,
	kATDiskTimingMode_UseOrdered
};

struct ATDiskVirtualSectorInfo {
	uint32	mStartPhysSector;
	uint32	mNumPhysSectors;
};

struct ATDiskPhysicalSectorInfo {
	uint32	mOffset;			// offset within memory image
	sint32	mDiskOffset;		// offset within disk image (for rewriting)
	uint16	mImageSize;			// size within image
	uint16	mPhysicalSize;		// size on media
	bool	mbDirty;
	bool	mbMFM;
	float	mRotPos;
	uint8	mFDCStatus;			// FDC status as seen by 810 firmware (inverted)
	sint16	mWeakDataOffset;
};

struct ATDiskGeometryInfo {
	uint16	mSectorSize;
	uint8	mBootSectorCount;
	uint32	mTotalSectorCount;
	uint8	mTrackCount;
	uint32	mSectorsPerTrack;
	uint8	mSideCount;

	// Returns the primary density for the disk; essentially, the density that
	// an MFM-capable drive would detect from the disk. This would usually be
	// the density used by track 0, sector 1 since otherwise the disk would not
	// boot... though there is no requirement that the disk have that sector,
	// nor that the drive uses it (some use sector 4).
	bool	mbMFM;
};

enum ATDiskImageFormat {
	kATDiskImageFormat_None,
	kATDiskImageFormat_ATR,
	kATDiskImageFormat_XFD,
	kATDiskImageFormat_P2,
	kATDiskImageFormat_P3,
	kATDiskImageFormat_ATX,
	kATDiskImageFormat_DCM
};

enum ATDiskInterleave {
	kATDiskInterleave_Default,
	kATDiskInterleave_1_1,			// 1,2,3,...
	kATDiskInterleave_SD_12_1,		// 1,8,15,4,11,18,7,14,3,10,17,6,13,2,9,16,5,12
	kATDiskInterleave_SD_9_1,		// 1,3,5,7,9,11,13,15,17,2,4,6,8,10,12,14,16,18
	kATDiskInterleave_SD_9_1_REV,	// 17,15,13,11,9,7,5,3,1,18,16,14,12,10,8,6,4,2
	kATDiskInterleave_SD_5_1,		// 4,8,12,16,1,5,9,13,17,2,6,10,14,18,3,7,11,15
	kATDiskInterleave_ED_13_1,		// 1,3,5,7,9,11,13,15,17,19,21,23,25,2,4,6,8,10,12,14,16,18,20,22,24,26
	kATDiskInterleave_ED_12_1,		// 9,18,7,16,25,5,14,23,3,12,21,1,10,19,8,17,26,6,15,24,4,13,22,2,11,20
	kATDiskInterleave_DD_15_1,		// 1,7,13,6,12,18,5,11,17,4,10,16,3,9,15,2,8,14
	kATDiskInterleave_DD_9_1,		// 1,3,5,7,9,11,13,15,17,2,4,6,8,10,12,14,16,18
	kATDiskInterleave_DD_7_1,		// 1,14,9,4,17,12,7,2,15,10,5,18,13,8,3,16,11,6
};

// Disk image interface
//
// IATDiskImage is an abstraction of a disk image interfaced to SIO via the standard
// disk drive protocol. It most often represents a floppy disk but may also represent
// a hard disk via SIO2PC or a PBI device driver. The most general form of image
// represented is therefore a simple linear block device.
//
// To support authentic floppy disk behavior, disk images use notions of virtual and
// physical sectors. Virtual sectors are those presented to SIO; physical sectors are
// the ones on the storage medium. Most images have 1:1 virtual to physical sectors;
// floppy disks may have 0-N physical sectors per virtual sector when there are missing
// or phantom sectors. Note that both virtual and physical sectors are indexed from 0
// here even though the SIO interface numbers sectors starting at 1.
//
// It is unspecified whether boot sectors have a size of 128 bytes or larger, so calling
// code must force 128 byte transfers as appropriate. Boot sectors may explicitly be
// larger than 128 bytes when representing the physical medium, since most double density
// formats actually store sectors 1-3 on track 0 as 256 bytes and in some cases that
// additional data must be preserved (bootable CP/M disks).
//
// The disk image interface also supports managing the persistent backing store for
// a device, including checking if there is a backing store to update, whether the
// disk image is dirty, and requesting a flush.
//
// Dynamic disk images are a special case; these are images that can change dynamically
// even without write commands to the image. This is usually because the image is
// virtual and being created on the fly.
//
class VDINTERFACE IATDiskImage : public IATImage {
public:
	enum : uint32 { kTypeID = 'dsim' };

	virtual ATDiskTimingMode GetTimingMode() const = 0;

	// Returns true if the disk image has been written to since the image was created
	// or loaded. It is unspecified whether writing a sector with contents identical to
	// the existing sector sets the dirty flag (other factors like copy-on-write, timestamp,
	// and sector error flags may require dirty to be set). Flush() is a no-op if the
	// image is not dirty.
	virtual bool IsDirty() const = 0;

	// Returns true if the disk image has a persistent backing store that can
	// be updated from the active image.
	virtual bool IsUpdatable() const = 0;

	// Returns true if the disk image has dynamic contents based on access pattern
	// or external updates, and false if the image is statically established on
	// load.
	virtual bool IsDynamic() const = 0;

	// Returns the persistent image format for the image. This may be None if the image
	// does not have an image file based backing store.
	virtual ATDiskImageFormat GetImageFormat() const = 0;

	// Flush any changes back to the persistent store. Returns true on success or no-op;
	// false if the image is not updatable. I/O exceptions may be thrown. A dirty image
	// becomes clean after a successful flush.
	virtual bool Flush() = 0;

	virtual uint64 GetImageChecksum() const = 0;

	virtual std::optional<uint32> GetImageFileCRC() const = 0;

	virtual void SetPath(const wchar_t *path, ATDiskImageFormat format) = 0;
	virtual void Save(const wchar_t *path, ATDiskImageFormat format) = 0;

	virtual ATDiskGeometryInfo GetGeometry() const = 0;
	virtual uint32 GetSectorSize() const = 0;
	virtual uint32 GetSectorSize(uint32 virtIndex) const = 0;
	virtual uint32 GetBootSectorCount() const = 0;

	virtual uint32 GetPhysicalSectorCount() const = 0;
	virtual void GetPhysicalSectorInfo(uint32 index, ATDiskPhysicalSectorInfo& info) const = 0;

	virtual void ReadPhysicalSector(uint32 index, void *data, uint32 len) = 0;
	virtual void WritePhysicalSector(uint32 index, const void *data, uint32 len, uint8 fdcStatus = 0xFF) = 0;

	virtual uint32 GetVirtualSectorCount() const = 0;
	virtual void GetVirtualSectorInfo(uint32 index, ATDiskVirtualSectorInfo& info) const = 0;

	virtual uint32 ReadVirtualSector(uint32 index, void *data, uint32 len) = 0;
	virtual bool WriteVirtualSector(uint32 index, const void *data, uint32 len) = 0;

	// Attempt to resize the disk image to the given number of virtual sectors.
	// In general, this should be <65536 sectors since that is the limit of the
	// standard SIO disk protocol, but this is not enforced. New sectors are
	// filled with $00 and the disk is marked as dirty. The disk geometry is
	// auto-computed according to inference rules. An exception is thrown if
	// the disk is dynamic.
	virtual void Resize(uint32 sectors) = 0;

	// Replace a series of virtual sectors with new ones.
	virtual void FormatTrack(uint32 vsIndexStart, uint32 vsCount, const ATDiskVirtualSectorInfo *vsecs, uint32 psCount, const ATDiskPhysicalSectorInfo *psecs, const uint8 *psecData) = 0;

	// Returns true if the disk has standard sectors and can safely be reinterleaved;
	// false if it has phantom sectors that can't be safely reordered.
	virtual bool IsSafeToReinterleave() const = 0;

	// Reorder sectors with a different interleave pattern.
	virtual void Reinterleave(ATDiskInterleave interleave) = 0;
};

void ATLoadDiskImage(const wchar_t *path, IATDiskImage **ppImage);
void ATLoadDiskImage(const wchar_t *origPath, const wchar_t *imagePath, IVDRandomAccessStream& stream, IATDiskImage **ppImage);
void ATMountDiskImageVirtualFolder(const wchar_t *path, uint32 sectorCount, IATDiskImage **ppImage);
void ATMountDiskImageVirtualFolderSDFS(const wchar_t *path, uint32 sectorCount, uint64 uniquenessValue, IATDiskImage **ppImage);
void ATCreateDiskImage(uint32 sectorCount, uint32 bootSectorCount, uint32 sectorSize, IATDiskImage **ppImage);
void ATCreateDiskImage(const ATDiskGeometryInfo& geometry, IATDiskImage **ppImage);

void ATDiskConvertGeometryToPERCOM(uint8 percom[12], const ATDiskGeometryInfo& geom);
void ATDiskConvertPERCOMToGeometry(ATDiskGeometryInfo& geom, const uint8 percom[12]);

ATDiskInterleave ATDiskGetDefaultInterleave(const ATDiskGeometryInfo& info);

vdfunction<float(uint32)> ATDiskGetInterleaveFn(ATDiskInterleave interleave, const ATDiskGeometryInfo& info);


#endif	// f_AT_ATIO_DISKIMAGE_H
