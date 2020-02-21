//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2014 Avery Lee
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
#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include <vd2/system/filesys.h>
#include <vd2/system/math.h>
#include <vd2/system/strutil.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/zip.h>
#include <at/atcore/checksum.h>
#include <at/atcore/logging.h>
#include <at/atcore/media.h>
#include <at/atcore/vfs.h>
#include <at/atio/diskimage.h>
#include <at/atio/diskfs.h>

ATLogChannel g_ATLCDiskImage(false, false, "DISKIMAGE", "Disk image load details");

namespace {
	// FM uses 4us bit cells, of which there are clock and data cells.
	// Each data bit is therefore 8us, and data byte 8*8 = 64us.
	static const int kBitsPerTrackFM = 26042;

	// Tables for encoding the angular position of each sector. Note that these are
	// indexed by virtual sector and not physical sector, so they are inverted from
	// the usual way that interleave patterns are written.

	// 1,8,15,4,11,18,7,14,3,10,17,6,13,2,9,16,5,12
	static const int kTrackInterleaveSD_12_1[18]={
		0, 13, 8, 3, 16, 11, 6, 1, 14, 9, 4, 17, 12, 7, 2, 15, 10, 5
	};

	// 1,3,5,7,9,11,13,15,17,2,4,6,8,10,12,14,16,18
	static const int kTrackInterleaveSD_9_1[18]={
		0, 9, 1, 10, 2, 11, 3, 12, 4, 13, 5, 14, 6, 15, 7, 16, 8, 17
	};

	// 17,15,13,11,9,7,5,3,1,18,16,14,12,10,8,6,4,2
	static const int kTrackInterleaveSD_9_1_REV[18]={
		8, 17, 7, 16, 6, 15, 5, 14, 4, 13, 3, 12, 2, 11, 1, 10, 9, 0
	};

	// 4,8,12,16,1,5,9,13,17,2,6,10,14,18,3,7,11,15
	static const int kTrackInterleaveSD_5_1[18]={
		4, 9, 14, 0, 5, 10, 15, 1, 6, 11, 16, 2, 7, 12, 17, 3, 8, 13
	};

	// 1,7,13,6,12,18,5,11,17,4,10,16,3,9,15,2,8,14
	static const int kTrackInterleaveDD_15_1[18]={
		0, 15, 12, 9, 6, 3, 1, 16, 13, 10, 7, 4, 2, 17, 14, 11, 8, 5
	};

	// 1,3,5,7,9,11,13,15,17,2,4,6,8,10,12,14,16,18
	static const int kTrackInterleaveDD_9_1[18]={
		0, 9, 1, 10, 2, 11, 3, 12, 4, 13, 5, 14, 6, 15, 7, 16, 8, 17
	};

	// 1,14,9,4,17,12,7,2,15,10,5,18,13,8,3,16,11,6
	static const int kTrackInterleaveDD_7_1[18]={
		0, 7, 14, 3, 10, 17, 6, 13, 2, 9, 16, 5, 12, 1, 8, 15, 4, 11
	};

	// 1,3,5,7,9,11,13,15,17,19,21,23,25,2,4,6,8,10,12,14,16,18,20,22,24,26
	static const int kTrackInterleaveED_13_1[26]={
		0, 13, 1, 14, 2, 15, 3, 16, 4, 17, 5, 18, 6, 19, 7, 20, 8, 21, 9, 22, 10, 23, 11, 24, 12, 25
	};

	// 9,18,7,16,25,5,14,23,3,12,21,1,10,19,8,17,26,6,15,24,4,13,22,2,11,20
	static const int kTrackInterleaveED_12_1[26]={
		11, 23, 8, 20, 5, 17, 2, 14, 0, 12, 24, 9, 21, 6, 18, 3, 15, 1, 13, 25, 10, 22, 7, 19, 4, 16
	};

	struct ATDCMPassHeader {
		uint8 mArchiveType;
		uint8 mPassInfo;
		uint8 mSectorLo;
		uint8 mSectorHi;
	};

	enum ATDCMDiskType {
		kATDCMDiskType_SD,
		kATDCMDiskType_DD,
		kATDCMDiskType_ED
	};

	enum ATXDensity : uint8 {
		kATXDensity_SD,
		kATXDensity_ED,
		kATXDensity_DD
	};

	struct ATXHeader {
		uint8	mSignature[4];			// AT8X
		uint16	mVersionMajor;			// 1
		uint16	mVersionMinor;			// 1
		uint16	mCreator;
		uint16	mCreatorVersion;
		uint32	mFlags;
		uint16	mImageType;
		uint8	mDensity;
		uint8	mFill1a;
		uint32	mImageId;
		uint16	mImageVersion;
		uint16	mFill1b;
		uint32	mTrackDataOffset;
		uint32	mTotalSize;
		uint8	mFill2[12];
	};

	struct ATXTrackHeader {
		enum : uint32 {
			kFlag_MFM		= 0x00000002,		// track encoded as MFM
			kFlag_NoSkew	= 0x00000100,		// track-to-track relative skew not measured
		};

		uint32	mSize;
		uint16	mType;
		uint16	mReserved06;
		uint8	mTrackNum;
		uint8	mReserved09;
		uint16	mNumSectors;
		uint16	mRate;
		uint8	mFill2[2];
		uint32	mFlags;
		uint32	mDataOffset;
		uint8	mFill4[8];
	};

	struct ATXSectorHeader {
		uint8	mIndex;
		uint8	mFDCStatus;		// not inverted
		uint16	mTimingOffset;
		uint32	mDataOffset;
	};

	struct ATXTrackChunkHeader {
		enum : uint8 {
			kTypeSectorData = 0x00,
			kTypeSectorList = 0x01,
			kTypeWeakBits = 0x10,
			kTypeExtSectorHeader = 0x11,
		};

		uint32	mSize;
		uint8	mType;
		uint8	mNum;
		uint16	mData;
	};

	static_assert(sizeof(ATXHeader) == 48, "ATXHeader size incorrect");
	static_assert(sizeof(ATXTrackHeader) == 32, "ATXTrackHeader size incorrect");
	static_assert(sizeof(ATXSectorHeader) == 8, "ATXSectorHeader size incorrect");
	static_assert(sizeof(ATXTrackChunkHeader) == 8, "ATXTrackChunkHeader size incorrect");
}

class ATDiskImage final : public vdrefcounted<IATDiskImage> {
public:
	ATDiskImage();

	void Init(uint32 sectorCount, uint32 bootSectorCount, uint32 sectorSize);
	void Init(const ATDiskGeometryInfo& geometry);
	void Load(const wchar_t *s);
	void Load(const wchar_t *origPath, const wchar_t *imagePath, IVDRandomAccessStream& stream);

	void *AsInterface(uint32 id) override;

	ATImageType GetImageType() const override { return kATImageType_Disk; }

	ATDiskTimingMode GetTimingMode() const override;

	bool IsDirty() const override;
	bool IsUpdatable() const override;
	bool IsDynamic() const override { return false; }
	ATDiskImageFormat GetImageFormat() const override { return mImageFormat; }

	uint64 GetImageChecksum() const override { return mImageChecksum; }
	std::optional<uint32> GetImageFileCRC() const override { return mImageFileCRC; }

	bool Flush() override;

	void SetPath(const wchar_t *path, ATDiskImageFormat format) override;
	void Save(const wchar_t *path, ATDiskImageFormat format) override;

	ATDiskGeometryInfo GetGeometry() const override { return mGeometry; }
	uint32 GetSectorSize() const override { return mSectorSize; }
	uint32 GetSectorSize(uint32 virtIndex) const override { return virtIndex < mBootSectorCount ? 128 : mSectorSize; }
	uint32 GetBootSectorCount() const  override{ return mBootSectorCount; }

	uint32 GetPhysicalSectorCount() const override;
	void GetPhysicalSectorInfo(uint32 index, ATDiskPhysicalSectorInfo& info) const override;

	void ReadPhysicalSector(uint32 index, void *data, uint32 len) override;
	void WritePhysicalSector(uint32 index, const void *data, uint32 len, uint8 fdcStatus = 0xFF) override;

	uint32 GetVirtualSectorCount() const override;
	void GetVirtualSectorInfo(uint32 index, ATDiskVirtualSectorInfo& info) const override;

	uint32 ReadVirtualSector(uint32 index, void *data, uint32 len) override;
	bool WriteVirtualSector(uint32 index, const void *data, uint32 len) override;

	void Resize(uint32 sectors) override;
	void FormatTrack(uint32 vsIndexStart, uint32 vsCount, const ATDiskVirtualSectorInfo *vsecs, uint32 psCount, const ATDiskPhysicalSectorInfo *psecs, const uint8 *psecData) override;

	bool IsSafeToReinterleave() const override;
	void Reinterleave(ATDiskInterleave interleave) override;

protected:
	typedef ATDiskVirtualSectorInfo VirtSectorInfo;
	typedef ATDiskPhysicalSectorInfo PhysSectorInfo;
	typedef vdfastvector<PhysSectorInfo> PhysSectors;
	typedef vdfastvector<VirtSectorInfo> VirtSectors;

	void LoadXFD(IVDRandomAccessStream& stream, sint64 fileSize);
	void LoadDCM(IVDRandomAccessStream& stream, uint32 len, const wchar_t *origPath, const uint8 *header);
	void LoadATX(IVDRandomAccessStream& stream, uint32 len, const uint8 *header);
	void LoadP2(IVDRandomAccessStream& stream, uint32 len, const uint8 *header);
	void LoadP3(IVDRandomAccessStream& stream, uint32 len, const uint8 *header);
	void LoadATR(IVDRandomAccessStream& stream, uint32 len, const wchar_t *origPath, const uint8 *header);
	void LoadARC(IVDRandomAccessStream& stream, const wchar_t *origPath);
	void ComputeGeometry();

	void SaveATR(VDFile& f, PhysSectors& phySecs);
	void SaveXFD(VDFile& f, PhysSectors& phySecs);
	void SaveP2(VDFile& f, PhysSectors& phySecs);
	void SaveP3(VDFile& f, PhysSectors& phySecs);
	void SaveDCM(VDFile& f, PhysSectors& phySecs);
	void SaveATX(VDFile& f, PhysSectors& phySecs);

	uint32	mBootSectorCount = 0;
	uint32	mSectorSize = 0;
	uint32	mSectorsPerTrack = 0;

	ATDiskImageFormat mImageFormat = {};
	bool	mbDirty = false;
	bool	mbDiskFormatDirty = false;
	bool	mbHasDiskSource = false;
	ATDiskTimingMode	mTimingMode = {};
	ATDiskGeometryInfo	mGeometry = {};
	uint64	mImageChecksum = 0;
	std::optional<uint32> mImageFileCRC {};

	VDStringW	mPath;

	PhysSectors mPhysSectors;
	VirtSectors mVirtSectors;
	vdfastvector<uint8>		mImage;
};

ATDiskImage::ATDiskImage() {
}

void ATDiskImage::Init(uint32 sectorCount, uint32 bootSectorCount, uint32 sectorSize) {
	mBootSectorCount = bootSectorCount;
	mSectorSize = sectorSize;

	mImage.clear();
	mImage.resize(128 * bootSectorCount + sectorSize * (sectorCount - bootSectorCount), 0);

	mPhysSectors.resize(sectorCount);
	mVirtSectors.resize(sectorCount);

	ComputeGeometry();

	const bool mfm = mGeometry.mbMFM;

	mImageChecksum = 0;
	for(uint32 i=0; i<sectorCount; ++i) {
		PhysSectorInfo& psi = mPhysSectors[i];
		VirtSectorInfo& vsi = mVirtSectors[i];

		vsi.mStartPhysSector = i;
		vsi.mNumPhysSectors = 1;

		psi.mOffset		= i < mBootSectorCount ? 128*i : 128*mBootSectorCount + mSectorSize*(i-mBootSectorCount);
		psi.mDiskOffset= -1;
		psi.mImageSize	= i < mBootSectorCount ? 128 : mSectorSize;
		psi.mPhysicalSize = psi.mImageSize;
		psi.mbDirty		= true;
		psi.mbMFM		= mfm;
		psi.mRotPos		= 0;
		psi.mFDCStatus	= 0xFF;
		psi.mWeakDataOffset = -1;

		mImageChecksum += ATComputeZeroBlockChecksum(ATComputeOffsetChecksum(i + 1), psi.mImageSize);
	}

	Reinterleave(kATDiskInterleave_Default);

	mbDirty = true;
	mbDiskFormatDirty = true;
	mImageFileCRC.reset();

	mPath = L"(New disk)";
	mbHasDiskSource = false;
}

void ATDiskImage::Init(const ATDiskGeometryInfo& geometry) {
	const uint32 sectorCount = geometry.mTrackCount * geometry.mSideCount * geometry.mSectorsPerTrack;

	mGeometry = geometry;
	mGeometry.mTotalSectorCount = sectorCount;
	mSectorsPerTrack = geometry.mSectorsPerTrack;
	mBootSectorCount = geometry.mBootSectorCount;
	mSectorSize = geometry.mSectorSize;

	mImage.clear();
	mImage.resize(128 * mBootSectorCount + mSectorSize * (sectorCount - mBootSectorCount), 0);

	mPhysSectors.resize(sectorCount);
	mVirtSectors.resize(sectorCount);

	mImageChecksum = 0;
	for(uint32 i=0; i<sectorCount; ++i) {
		PhysSectorInfo& psi = mPhysSectors[i];
		VirtSectorInfo& vsi = mVirtSectors[i];

		vsi.mStartPhysSector = i;
		vsi.mNumPhysSectors = 1;

		psi.mOffset		= i < mBootSectorCount ? 128*i : 128*mBootSectorCount + mSectorSize*(i-mBootSectorCount);
		psi.mDiskOffset= -1;
		psi.mImageSize		= i < mBootSectorCount ? 128 : mSectorSize;
		psi.mPhysicalSize	= psi.mImageSize;
		psi.mbDirty		= true;
		psi.mbMFM		= geometry.mbMFM;
		psi.mRotPos		= 0;
		psi.mFDCStatus	= 0xFF;
		psi.mWeakDataOffset = -1;

		mImageChecksum += ATComputeZeroBlockChecksum(ATComputeOffsetChecksum(i + 1), psi.mImageSize);
	}

	Reinterleave(kATDiskInterleave_Default);

	mbDirty = true;
	mbDiskFormatDirty = true;
	mImageFileCRC.reset();

	mPath = L"(New disk)";
	mbHasDiskSource = false;
}

void ATDiskImage::Load(const wchar_t *s) {
	vdrefptr<ATVFSFileView> view;
	
	ATVFSOpenFileView(s, false, ~view);
	Load(s, view->GetFileName(), view->GetStream());
}

void ATDiskImage::Load(const wchar_t *origPath, const wchar_t *imagePath, IVDRandomAccessStream& stream) {
	sint64 fileSize = stream.Length();
	const wchar_t *ext = VDFileSplitExt(imagePath);

	mImageFileCRC.reset();

	if (!vdwcsicmp(ext, L".arc")) {
		LoadARC(stream, origPath);
	} else if (fileSize <= 65535 * 128 && imagePath && !vdwcsicmp(ext, L".xfd")) {
		LoadXFD(stream, fileSize);
	} else {
		
		uint8 header[16];
		stream.Read(header, 16);

		sint32 len = VDClampToSint32(stream.Length()) - 16;
		mImage.resize(len);
		stream.Read(mImage.data(), len);

		VDCRCChecker crcChecker(VDCRCTable::CRC32);
		crcChecker.Process(header, 16);
		crcChecker.Process(mImage.data(), len);
		mImageFileCRC = crcChecker.CRC();

		mTimingMode = kATDiskTimingMode_Any;

		if (header[0] == 0xF9 || header[0] == 0xFA) {
			LoadDCM(stream, len, origPath, header);
		} else if (header[0] == 'A' && header[1] == 'T' && header[2] == '8' && header[3] == 'X') {
			LoadATX(stream, len, header);
		} else if (header[2] == 'P' && header[3] == '2') {
			LoadP2(stream, len, header);
		} else if (header[2] == 'P' && header[3] == '3') {
			LoadP3(stream, len, header);
		} else if (header[0] == 0x96 && header[1] == 0x02) {
			LoadATR(stream, len, origPath, header);
		} else {
			// Hmm. Okay, we don't recognize the signature on this file. Let's
			// check if it is a multiple of 128 bytes. If so, assume it's a headerless
			// XFD image. There are ".ATR" files stored this way. :-/
			if (!(fileSize & 127)) {
				stream.Seek(0);
				LoadXFD(stream, fileSize);
			} else if (origPath)
				throw MyError("Disk image \"%ls\" is corrupt or uses an unsupported format.", VDFileSplitPath(origPath));
			else
				throw MyError("Disk image is corrupt or uses an unsupported format.");
		}
	}

	if (origPath)
		mPath = origPath;
	else
		mPath.clear();

	mbDirty = false;
	mbDiskFormatDirty = false;
	mbHasDiskSource = true;

	if (!origPath || (imagePath && wcscmp(origPath, imagePath)))
		mImageFormat = kATDiskImageFormat_None;
}

class ATInvalidDiskFormatException : public MyError {
public:
	ATInvalidDiskFormatException(const wchar_t *path) {
		if (path)
			setf("Disk image \"%ls\" is corrupt or uses an unsupported format.", VDFileSplitPath(path));
		else
			setf("Disk image is corrupt or uses an unsupported format.");
	}
};

void ATDiskImage::LoadXFD(IVDRandomAccessStream& stream, sint64 fileSize) {
	sint32 len = (sint32)fileSize;

	mImage.resize(len);
	stream.Read(mImage.data(), len);

	mBootSectorCount = 3;
	mImageFormat = kATDiskImageFormat_XFD;

	uint32 sectorCount;

	if (!(len & 255) && len >= 720 * 256) {
		mSectorSize = 256;
		sectorCount = len >> 8;
	} else {
		mSectorSize = 128;
		sectorCount = len >> 7;
	}

	mPhysSectors.resize(sectorCount);
	mVirtSectors.resize(sectorCount);
	
	ComputeGeometry();

	const bool isDD = (mSectorSize == 256);
	const bool isED = (len == 1040 * 128);
	const bool mfm	= isDD || isED;

	mImageChecksum = 0;
	for(uint32 i=0; i<sectorCount; ++i) {
		PhysSectorInfo& psi = mPhysSectors[i];
		VirtSectorInfo& vsi = mVirtSectors[i];

		vsi.mStartPhysSector = i;
		vsi.mNumPhysSectors = 1;

		psi.mOffset		= mSectorSize*i;
		psi.mDiskOffset= -1;
		psi.mImageSize		= mSectorSize;
		psi.mPhysicalSize	= mSectorSize;
		psi.mFDCStatus	= 0xFF;
		psi.mRotPos		= 0;
		psi.mWeakDataOffset = -1;
		psi.mbDirty		= false;
		psi.mbMFM		= mfm;

		mImageChecksum += ATComputeBlockChecksum(ATComputeOffsetChecksum(i + 1), mImage.data() + psi.mOffset, psi.mImageSize);
	}

	Reinterleave(kATDiskInterleave_Default);
}

void ATDiskImage::LoadDCM(IVDRandomAccessStream& stream, uint32 len, const wchar_t *origPath, const uint8 *header) {
	stream.Seek(0);
	mImage.clear();

	// read passes
	uint8 sectorBuffer[256] = {0};

	VirtSectorInfo dummySector;
	dummySector.mNumPhysSectors = 0;
	dummySector.mStartPhysSector = 0;

	mImageChecksum = 0;

	uint32 mainSectorSize = 128;
	uint32 mainSectorCount = 0;
	bool mfm = false;

	ATDCMDiskType diskType = kATDCMDiskType_SD;

	for(;;) {
		ATDCMPassHeader passHeader;

		stream.Read(&passHeader, sizeof(ATDCMPassHeader));

		if (passHeader.mArchiveType != 0xF9 && passHeader.mArchiveType != 0xFA)
			throw ATInvalidDiskFormatException(origPath);

		uint32 sectorSize = (passHeader.mPassInfo & 0x60) == 0x20 ? 256 : 128;
		uint32 sectorNum = passHeader.mSectorLo + 256*passHeader.mSectorHi;

		switch(passHeader.mPassInfo & 0x60) {
			case 0x00:
				mainSectorCount = 720;
				diskType = kATDCMDiskType_SD;
				break;
			case 0x40:
				mainSectorCount = 1040;
				diskType = kATDCMDiskType_ED;
				mfm = true;
				break;
			case 0x20:
				mainSectorCount = 720;
				diskType = kATDCMDiskType_DD;
				mfm = true;
				break;
		}

		mainSectorSize = sectorSize;

		for(;;) {
			uint8 contentType;
			stream.Read(&contentType, 1);

			if ((contentType & 0x7F) == 0x45)
				break;

			// This check has to be after the 0x45 token check, as it's legal to have a sector number of
			// 0 before it.
			if (!sectorNum)
				throw ATInvalidDiskFormatException(origPath);

			uint8 c;
			switch(contentType & 0x7F) {
				case 0x41:		// modify begin
					stream.Read(&c, 1);
					stream.Read(sectorBuffer, c + 1);
					for(uint32 i=0, j=c; i < j; ++i, --j) {
						std::swap(sectorBuffer[i], sectorBuffer[j]);
					}
					break;
				case 0x42:		// 128 byte DOS sector
					stream.Read(sectorBuffer + 123, 5);
					memset(sectorBuffer, sectorBuffer[123], 123);
					break;
				case 0x43:		// compressed sector
					{
						uint32 pos = 0;
						uint8 offset;
						bool compressed = false;
						bool first = true;
						while(pos < sectorSize) {
							stream.Read(&offset, 1);

							// offset cannot exceed sectorSize, ever.
							if (offset > sectorSize)
								throw ATInvalidDiskFormatException(origPath);

							// offset cannot go backwards, except in two specific cases:
							//  - offset is 0, pos is 0 (null span)
							//  - offset is 0, sectorSize is 256 (fill to end of DD sector)
							if (offset < pos && (offset || (pos && sectorSize != 256)))
								throw ATInvalidDiskFormatException(origPath);

							uint32 spanLen = (offset || first ? offset : sectorSize) - pos;
							if (compressed) {
								stream.Read(&c, 1);
								memset(sectorBuffer + pos, c, spanLen);
							} else if (spanLen) {
								stream.Read(sectorBuffer + pos, spanLen);
							}

							pos += spanLen;
							compressed = !compressed;
							first = false;
						}
					}
					break;
				case 0x44:		// modify end
					stream.Read(&c, 1);
					if (c >= sectorSize)
						throw ATInvalidDiskFormatException(origPath);
					stream.Read(sectorBuffer + c, sectorSize - c);
					break;
				case 0x46:		// repeat last sector
					break;
				case 0x47:		// uncompressed sector
					stream.Read(sectorBuffer, sectorSize);
					break;

				default:
					throw ATInvalidDiskFormatException(origPath);
			}

			// create entry for sector
			if (mVirtSectors.size() < sectorNum)
				mVirtSectors.resize(sectorNum, dummySector);

			VirtSectorInfo& vsi = mVirtSectors[sectorNum - 1];
			vsi.mNumPhysSectors = 1;
			vsi.mStartPhysSector = (uint32)mPhysSectors.size();

			PhysSectorInfo& psi =  mPhysSectors.push_back();
			psi.mOffset = (uint32)mImage.size();
			psi.mDiskOffset = -1;
			psi.mImageSize = sectorNum <= 3 ? 128 : sectorSize;
			psi.mPhysicalSize = psi.mImageSize;
			psi.mRotPos = 0;
			psi.mFDCStatus = 0xFF;
			psi.mWeakDataOffset = -1;
			psi.mbDirty = false;
			psi.mbMFM = mfm;

			mImageChecksum += ATComputeBlockChecksum(ATComputeOffsetChecksum(sectorNum), sectorBuffer, psi.mImageSize);

			mImage.insert(mImage.end(), sectorBuffer, sectorBuffer + psi.mImageSize);

			// increment sector number if sequential flag is set, else read new sector number
			if (contentType & 0x80) {
				++sectorNum;
			} else {
				uint8 newSec[2];
				stream.Read(newSec, 2);
				sectorNum = VDReadUnalignedLEU16(newSec);
			}
		}

		if (passHeader.mPassInfo & 0x80)
			break;
	}

	// fill in any missing sectors
	if (mVirtSectors.size() < mainSectorCount)
		mVirtSectors.resize(mainSectorCount, dummySector);

	for(VirtSectors::iterator it(mVirtSectors.begin()), itEnd(mVirtSectors.end()); it != itEnd; ++it) {
		VirtSectorInfo& vsi = *it;

		if (!vsi.mNumPhysSectors) {
			vsi.mNumPhysSectors = 1;
			vsi.mStartPhysSector = (uint32)mPhysSectors.size();

			uint32 secNum = (uint32)(it - mVirtSectors.begin()) + 1;

			PhysSectorInfo& psi =  mPhysSectors.push_back();
			psi.mOffset = (uint32)mImage.size();
			psi.mDiskOffset = -1;
			psi.mImageSize = secNum <= 3 ? 128 : mainSectorSize;
			psi.mPhysicalSize = psi.mImageSize;
			psi.mFDCStatus = 0xFF;
			psi.mWeakDataOffset = -1;
			psi.mbDirty = false;
			psi.mbMFM = mfm;
			psi.mRotPos = 0;

			mImageChecksum += ATComputeZeroBlockChecksum(ATComputeOffsetChecksum(secNum), psi.mImageSize);
			mImage.resize(mImage.size() + psi.mImageSize, 0);
		}
	}

	mBootSectorCount = 3;
	mSectorSize = mainSectorSize;
	mImageFormat = kATDiskImageFormat_DCM;

	ComputeGeometry();
	Reinterleave(kATDiskInterleave_Default);
}

void ATDiskImage::LoadATX(IVDRandomAccessStream& stream, uint32 len, const uint8 *header) {
	ATXHeader atxhdr;
	stream.Seek(0);
	stream.Read(&atxhdr, sizeof atxhdr);

	stream.Seek(atxhdr.mTrackDataOffset);

	mImage.clear();
	mBootSectorCount = 3;
	mSectorSize = 128;

	mImageChecksum = 0;

	sint64 imageSize = stream.Length();
	sint32 imageSize32 = (sint32)imageSize;

	if (imageSize != imageSize32)
		throw MyError("Invalid ATX image: file exceeds 2GB in size.");

	vdblock<uint8> trackBuf;
	vdfastvector<ATXSectorHeader> sectorHeaders;
	vdfastvector<sint32> phySectorLookup;

	struct PhysicalSector {
		ATDiskPhysicalSectorInfo mInfo;
		uint32_t mNext;
	};

	vdfastvector<PhysicalSector> psecs;
	vdfastvector<sint32> vsecs(720, -1);

	bool isTrack0MFM = false;

	for(uint32 i=0; i<40; ++i) {
		ATXTrackHeader trkhdr;
		sint32 trackBaseOffset = (sint32)stream.Pos();

		stream.Read(&trkhdr, sizeof trkhdr);

		// validate track
		if (trackBaseOffset + trkhdr.mSize > imageSize)
			throw MyError("Invalid ATX image: Chunk at %08x extends beyond end of file.", (uint32)trackBaseOffset);

		if (trkhdr.mSize < sizeof trkhdr)
			throw MyError("Invalid ATX image: Track header at %08x has invalid size.", (uint32)trackBaseOffset);

		if (trkhdr.mType != 0)
			throw MyError("Invalid ATX image: Track header at %08x has the wrong type.", (uint32)trackBaseOffset);

		// read in the entire track chunk
		if (trackBuf.size() < trkhdr.mSize)
			trackBuf.resize(trkhdr.mSize);

		memcpy(trackBuf.data(), &trkhdr, sizeof trkhdr);
		stream.Read(trackBuf.data() + sizeof trkhdr, trkhdr.mSize - sizeof trkhdr);

		// validate the chunk list and pull out the sector list
		uint32 sectorDataStart = 0;
		uint32 sectorDataLen = 0;

		sectorHeaders.resize(trkhdr.mNumSectors);
		bool foundSectorList = false;

		for(uint32 chunkOffset = trkhdr.mDataOffset;;) {
			if (trkhdr.mSize - chunkOffset < sizeof(ATXTrackChunkHeader))
				throw MyError("Invalid ATX image: Unterminated chunk chain.");

			uint32 chunkSize = VDReadUnalignedLEU32(&trackBuf[chunkOffset]);

			if (!chunkSize) {
				// If we don't have a sector data chunk, assume that all remaining data can be sector data.
				// The Wizard and the Princess has this issue.
				if (!sectorDataLen) {
					sectorDataStart = chunkOffset;
					sectorDataLen = trkhdr.mSize - chunkOffset;
				}
				break;
			}

			if (chunkOffset + chunkSize > trkhdr.mSize)
				throw MyError("Invalid ATX image: Chunk at %08x extends outside of track at %08x.", (uint32)chunkOffset + (uint32)trackBaseOffset, (uint32)trackBaseOffset);

			if (chunkSize < sizeof(ATXTrackChunkHeader))
				throw MyError("Invalid ATX image: Chunk at %08x has invalid size.", (uint32)chunkOffset + (uint32)trackBaseOffset);

			ATXTrackChunkHeader ch;
			memcpy(&ch, &trackBuf[chunkOffset], sizeof ch);

			if (ch.mType == ATXTrackChunkHeader::kTypeSectorData) {
				sectorDataStart = chunkOffset + sizeof(ATXTrackChunkHeader);
				sectorDataLen = chunkSize - sizeof(ATXTrackChunkHeader);
			} else if (ch.mType == ATXTrackChunkHeader::kTypeSectorList) {
				if (chunkSize < sizeof(ATXTrackChunkHeader) + sizeof(ATXSectorHeader) * trkhdr.mNumSectors)
					throw MyError("Invalid ATX image: Sector list at %08x has size %08x insufficient to hold %u sectors.", (uint32)chunkOffset + (uint32)trackBaseOffset, chunkSize, trkhdr.mNumSectors);

				foundSectorList = true;
				memcpy(sectorHeaders.data(), &trackBuf[chunkOffset] + sizeof(ATXTrackChunkHeader), sizeof(sectorHeaders[0])*sectorHeaders.size());
			}

			chunkOffset += chunkSize;
		}

		if (!foundSectorList)
			throw MyError("Invalid ATX image: Track at %08x does not have a sector list.", (uint32)trackBaseOffset);

		phySectorLookup.clear();
		phySectorLookup.resize(trkhdr.mNumSectors, -1);

		// check track density
		const bool isTrackMFM = (trkhdr.mFlags & ATXTrackHeader::kFlag_MFM) != 0;
		const bool isED = isTrackMFM;

		if (i == 0)
			isTrack0MFM = isTrackMFM;

		// compute virtual sector range
		const uint32 vsecsPerTrack = isED ? 26 : 18;
		const uint32 vsecStart = i * vsecsPerTrack;
		const uint32 vsecEnd = vsecStart + vsecsPerTrack;

		// extend vsec array to ED if needed
		if (isED && vsecs.size() < 1040)
			vsecs.resize(1040, -1);

		// scan all physical sectors and bin sort into virtual sectors
		uint32 sectorsWithExtraData = 0;

		for(uint32 k=0; k<trkhdr.mNumSectors; ++k) {
			const ATXSectorHeader& sechdr = sectorHeaders[k];

			// drop sectors that can't be found with normal addressing (and don't fit into
			// our vsec scheme)
			if (!sechdr.mIndex || sechdr.mIndex > vsecsPerTrack) {
				g_ATLCDiskImage("Dropping track %u, sector %u: inaccessible with standard disk drive", i, sechdr.mIndex);
				continue;
			}

			if (sechdr.mFDCStatus & 0x40)
				++sectorsWithExtraData;

			phySectorLookup[k] = (sint32)psecs.size();

			PhysicalSector& psec = psecs.push_back();
			PhysSectorInfo& psi = psec.mInfo;
			psi.mFDCStatus = ~sechdr.mFDCStatus | 0xc0;		// purpose of bit 7 is unknown
			psi.mOffset = (sint32)mImage.size();
			psi.mDiskOffset = trackBaseOffset + sechdr.mDataOffset;
			psi.mImageSize = sechdr.mFDCStatus & 0x10 ? 0 : 128;
			psi.mPhysicalSize = psi.mFDCStatus & 0x04 ? 128 : 256;
			psi.mRotPos = (float)sechdr.mTimingOffset / (float)kBitsPerTrackFM;
			psi.mWeakDataOffset = -1;
			psi.mbDirty = false;
			psi.mbMFM = isTrackMFM;

			auto& vsecLink = vsecs[vsecStart + sechdr.mIndex - 1];
			psec.mNext = vsecLink;
			vsecLink = (uint32)psecs.size() - 1;

			g_ATLCDiskImage("Track %d, sector %d | pos %4.2f%s%s%s%s%s\n"
				, i
				, sechdr.mIndex
				, psi.mRotPos
				, psi.mFDCStatus & 0x20 ? "" : " deleted"
				, psi.mFDCStatus & 0x10 ? "" : " missing"
				, psi.mFDCStatus & 0x08 ? "" : " CRCerror"
				, psi.mFDCStatus & 0x04 ? "" : " long"
				, psi.mWeakDataOffset >= 0 ? " weak" : ""
				);

			// Missing sectors do not have data.
			if (!(sechdr.mFDCStatus & 0x10)) {
				uint32 dataEnd = sechdr.mDataOffset + 128;

				if (sechdr.mDataOffset < sectorDataStart || (dataEnd - sectorDataStart) > sectorDataLen)
					throw MyError("Invalid protected disk: track %u, sector %u extends outside of sector data region.", i, sechdr.mIndex);

				const uint8 *srcData = &trackBuf[sechdr.mDataOffset];
				mImage.insert(mImage.end(), srcData, srcData + 128);

				mImageChecksum += ATComputeBlockChecksum(ATComputeOffsetChecksum(mVirtSectors.size()), srcData, psi.mImageSize);
			}
		}

		// process extra sector data
		for(uint32 chunkOffset = trkhdr.mDataOffset;;) {
			uint32 chunkSize = VDReadUnalignedLEU32(&trackBuf[chunkOffset]);

			if (!chunkSize)
				break;

			ATXTrackChunkHeader ch;
			memcpy(&ch, &trackBuf[chunkOffset], sizeof ch);

			if (ch.mType == ATXTrackChunkHeader::kTypeWeakBits || 
				ch.mType == ATXTrackChunkHeader::kTypeExtSectorHeader) {

				if (ch.mNum >= trkhdr.mNumSectors)
					throw MyError("Invalid ATX image: extra track data chunk at %08x has invalid sector number.", (uint32)(trackBaseOffset + chunkOffset));

				int phyIndex = phySectorLookup[ch.mNum];
				if (phyIndex < 0) {
					VDASSERT(phyIndex >= 0);
					continue;
				}

				PhysSectorInfo& psi = psecs[phyIndex].mInfo;

				if (ch.mType == ATXTrackChunkHeader::kTypeWeakBits) {
					uint16 weakOffset = VDFromLE16(ch.mData);

					if (weakOffset < psi.mPhysicalSize) {
						psi.mWeakDataOffset = weakOffset;

						g_ATLCDiskImage("Sector index %d -> weak offset %d\n"
							, phyIndex
							, weakOffset
							);
					}
				} else if (ch.mType == ATXTrackChunkHeader::kTypeExtSectorHeader) {
					const uint8 secSizeCode = ch.mData & 3;

					if (!(psi.mFDCStatus & 0x04) && secSizeCode >= 2) {
						psi.mPhysicalSize = 128 << secSizeCode;
					}
				}
			}

			chunkOffset += chunkSize;
		}
	}

	// Serialize to master arrays. This has to be done at the end of all tracks because
	// tracks may cover vsecs out of order if mixed FM/MFM tracks are present.
	const uint32 numVsecs = (uint32)vsecs.size();

	mVirtSectors.resize(numVsecs);
	mPhysSectors.resize((uint32)psecs.size());

	PhysSectorInfo *psi = mPhysSectors.data();
	uint32 numPsecs = 0;

	for(uint32 vsecIndex = 0; vsecIndex < numVsecs; ++vsecIndex) {
		VirtSectorInfo& vsi = mVirtSectors[vsecIndex];

		vsi.mStartPhysSector = numPsecs;
		vsi.mNumPhysSectors = 0;

		for(sint32 psecIndex = vsecs[vsecIndex]; psecIndex >= 0; ) {
			const PhysicalSector& psec = psecs[psecIndex];

			*psi++ = psec.mInfo;
			++numPsecs;
			++vsi.mNumPhysSectors;

			psecIndex = psec.mNext;
		}
	}

	mTimingMode = kATDiskTimingMode_UsePrecise;
	mImageFormat = kATDiskImageFormat_ATX;

	ComputeGeometry();

	// ATX is a bit special as it's the only format so far that can support mixed
	// tracks, and therefore confuse the geometry detection. Override the MFM flag
	// with the state of track 0.
	mGeometry.mbMFM = isTrack0MFM;
}

void ATDiskImage::LoadP2(IVDRandomAccessStream& stream, uint32 len, const uint8 *header) {
	mBootSectorCount = 3;
	mSectorSize = 128;
	mImageFormat = kATDiskImageFormat_P2;
	mImageChecksum = 0;
	mTimingMode = kATDiskTimingMode_UseOrdered;

	int sectorCount = VDReadUnalignedBEU16(&header[0]);

	g_ATLCDiskImage("PRO header: %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X\n"
		, header[0]
		, header[1]
		, header[2]
		, header[3]
		, header[4]
		, header[5]
		, header[6]
		, header[7]
		, header[8]
		, header[9]
		, header[10]
		, header[11]
		, header[12]
		, header[13]
		, header[14]
		, header[15]);

	// read sector headers
	for(int i=0; i<sectorCount; ++i) {
		const uint8 *sectorhdr = &mImage[(128+12) * i];

		g_ATLCDiskImage("Sector %03d: %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X\n"
			, i + 1
			, sectorhdr[0]
			, sectorhdr[1]
			, sectorhdr[2]
			, sectorhdr[3]
			, sectorhdr[4]
			, sectorhdr[5]
			, sectorhdr[6]
			, sectorhdr[7]
			, sectorhdr[8]
			, sectorhdr[9]
			, sectorhdr[10]
			, sectorhdr[11]);

		mPhysSectors.push_back();
		PhysSectorInfo& psi = mPhysSectors.back();

		psi.mOffset		= (128+12)*i+12;
		psi.mDiskOffset = psi.mOffset + 16;
		psi.mImageSize		= 128;
		psi.mPhysicalSize	= 128;
		psi.mFDCStatus	= sectorhdr[1];
		psi.mRotPos		= 0;
		psi.mWeakDataOffset = -1;
		psi.mbDirty		= false;
		psi.mbMFM		= false;

		if (!(psi.mFDCStatus & 0x10)) {
			psi.mImageSize = 0;
		} else {
			mImageChecksum += ATComputeBlockChecksum(ATComputeOffsetChecksum(mVirtSectors.size() + 1), &mImage[psi.mDiskOffset], psi.mImageSize);
		}

		mVirtSectors.push_back();
		VirtSectorInfo& vsi = mVirtSectors.back();

		vsi.mStartPhysSector = (uint32)mPhysSectors.size() - 1;
		vsi.mNumPhysSectors = 1;

		uint16 phantomSectorCount = sectorhdr[5];
		if (phantomSectorCount) {
			vsi.mNumPhysSectors = phantomSectorCount + 1;

			sectorCount -= phantomSectorCount;

			for(uint32 j=0; j<phantomSectorCount; ++j) {
				uint32 phantomSectorOffset = sectorCount + sectorhdr[7 + j] - 1;
				uint32 phantomSectorByteOffset = (128+12) * phantomSectorOffset;
				if (phantomSectorByteOffset + 128 > mImage.size())
					throw MyError("Invalid protected disk.");
				const uint8 *sectorhdr2 = &mImage[phantomSectorByteOffset];

				mPhysSectors.push_back();
				PhysSectorInfo& psi = mPhysSectors.back();

				psi.mOffset		= phantomSectorByteOffset+12;
				psi.mDiskOffset = psi.mOffset + 16;
				psi.mImageSize		= 128;
				psi.mPhysicalSize	= 128;
				psi.mFDCStatus	= sectorhdr2[1];
				psi.mRotPos		= 0;
				psi.mWeakDataOffset = -1;
				psi.mbDirty		= false;
				psi.mbMFM		= false;

				if (!(psi.mFDCStatus & 0x10)) {
					psi.mImageSize = 0;
				} else {
					mImageChecksum += ATComputeBlockChecksum(ATComputeOffsetChecksum(mVirtSectors.size()), &mImage[psi.mDiskOffset], psi.mImageSize);
				}
			}
		}
	}

	ComputeGeometry();
	Reinterleave(kATDiskInterleave_Default);
}

void ATDiskImage::LoadP3(IVDRandomAccessStream& stream, uint32 len, const uint8 *header) {
	g_ATLCDiskImage("PRO header: %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X\n"
		, header[0]
		, header[1]
		, header[2]
		, header[3]
		, header[4]
		, header[5]
		, header[6]
		, header[7]
		, header[8]
		, header[9]
		, header[10]
		, header[11]
		, header[12]
		, header[13]
		, header[14]
		, header[15]);

	mBootSectorCount = 3;
	mSectorSize = 128;
	mImageFormat = kATDiskImageFormat_P3;
	mImageChecksum = 0;
	mTimingMode = kATDiskTimingMode_UseOrdered;

	uint32 sectorCount = VDReadUnalignedBEU16(&header[6]);

	// read sector headers
	for(uint32 i=0; i<sectorCount; ++i) {
		const uint8 *sectorhdr = &mImage[(128+12) * i];

		g_ATLCDiskImage("Sector %03d: %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X\n"
			, i + 1
			, sectorhdr[0]
			, sectorhdr[1]
			, sectorhdr[2]
			, sectorhdr[3]
			, sectorhdr[4]
			, sectorhdr[5]
			, sectorhdr[6]
			, sectorhdr[7]
			, sectorhdr[8]
			, sectorhdr[9]
			, sectorhdr[10]
			, sectorhdr[11]);

		uint32 phantomSectorCount = sectorhdr[5];

		float rotationalIncrement = phantomSectorCount ? (1.0f / (int)phantomSectorCount) : 0.0f;

		mVirtSectors.push_back();
		VirtSectorInfo& vsi = mVirtSectors.back();

		vsi.mStartPhysSector = (uint32)mPhysSectors.size();
		vsi.mNumPhysSectors = phantomSectorCount + 1;

		for(uint32 j=0; j<=phantomSectorCount; ++j) {
			uint8 idx = sectorhdr[6 + j];

			uint32 phantomSectorOffset = idx ? sectorCount + idx - 1 : i;
			uint32 phantomSectorByteOffset = (128+12) * phantomSectorOffset;
			if (phantomSectorByteOffset + 128 > mImage.size())
				throw MyError("Invalid protected disk.");
			const uint8 *sectorhdr2 = &mImage[phantomSectorByteOffset];

			mPhysSectors.push_back();
			PhysSectorInfo& psi = mPhysSectors.back();

			psi.mOffset		= phantomSectorByteOffset+12;
			psi.mDiskOffset = psi.mOffset + 16;
			psi.mImageSize		= 128;
			psi.mPhysicalSize	= 128;
			psi.mFDCStatus	= sectorhdr2[1];
			psi.mRotPos		= 0;
			psi.mWeakDataOffset = -1;
			psi.mbDirty		= false;
			psi.mbMFM		= false;

			if (!(psi.mFDCStatus & 0x10)) {
				psi.mImageSize = 0;
			} else {
				mImageChecksum += ATComputeBlockChecksum(ATComputeOffsetChecksum(mVirtSectors.size()), &mImage[psi.mDiskOffset], psi.mImageSize);
			}
		}
	}

	ComputeGeometry();
	Reinterleave(kATDiskInterleave_Default);
}

void ATDiskImage::LoadATR(IVDRandomAccessStream& stream, uint32 len, const wchar_t *origPath, const uint8 *header) {
	mSectorSize = header[4] + 256*header[5];

	uint32 imageBootSectorCount = 0;
	bool packedBootSectors = false;

	if (mSectorSize <= 256) {
		mBootSectorCount = 3;
		imageBootSectorCount = 3;

		// Check if this is a FUBARed DD disk where the boot sectors are 256 bytes.
		// We assume this is the case if the paragraphs count works out for that.
		if (mSectorSize == 256) {
			uint32 headerParas = header[2] + 256*header[3];

			if (!(headerParas & 0x0f)) {
				imageBootSectorCount = 0;

				// Okay, now we need to check for REALLY screwed up images where the
				// first three sectors are stored back to back, followed by a 192 byte
				// section of nulls.
				bool slotTwoEmpty = true;

				for(int i=0; i<128; ++i) {
					if (mImage[16 + 128 + i]) {
						slotTwoEmpty = false;
						break;
					}
				}

				bool slotFiveEmpty = true;

				for(int i=0; i<128; ++i) {
					if (mImage[16 + 128*4 + i]) {
						slotFiveEmpty = false;
						break;
					}
				}

				if (!slotTwoEmpty && slotFiveEmpty)
					packedBootSectors = true;
			}
		}
	} else
		mBootSectorCount = 0;

	if (mSectorSize > 8192) {
		if (origPath)
			throw MyError("Disk image \"%ls\" uses an unsupported sector size of %u bytes.", VDFileSplitPath(origPath), mSectorSize);
		else
			throw MyError("Disk image uses an unsupported sector size of %u bytes.", mSectorSize);
	}

	mImageFormat = kATDiskImageFormat_ATR;
	mImageChecksum = 0;
	
	if (len < 128*imageBootSectorCount) {
		imageBootSectorCount = len >> 7;
		if (mBootSectorCount > imageBootSectorCount)
			mBootSectorCount = imageBootSectorCount;
	}

	uint32 wholeSectors = (len - 128*imageBootSectorCount) / mSectorSize;
	uint32 partialSector = (len - 128*imageBootSectorCount) % mSectorSize;
	uint32 sectorCount = wholeSectors + imageBootSectorCount;

	if (partialSector) {
		++sectorCount;

		mImage.resize(mImage.size() + (mSectorSize - partialSector), 0);
	}

	mPhysSectors.resize(sectorCount);
	mVirtSectors.resize(sectorCount);

	ComputeGeometry();	// needed earlier for interleave

	const auto interleaveFn = ATDiskGetInterleaveFn(kATDiskInterleave_Default, mGeometry);

	for(uint32 i=0; i<sectorCount; ++i) {
		PhysSectorInfo& psi = mPhysSectors[i];
		VirtSectorInfo& vsi = mVirtSectors[i];

		vsi.mStartPhysSector = i;
		vsi.mNumPhysSectors = 1;

		if (packedBootSectors && i < 3) {
			psi.mOffset		= 128*i;
		} else {
			psi.mOffset		= i < imageBootSectorCount ? 128*i : 128*imageBootSectorCount + mSectorSize*(i-imageBootSectorCount);
		}

		psi.mDiskOffset = psi.mOffset + 16;
		psi.mImageSize		= i < mBootSectorCount ? 128 : mSectorSize;
		psi.mPhysicalSize	= psi.mImageSize;
		psi.mFDCStatus	= 0xFF;
		psi.mRotPos		= interleaveFn(i);
		psi.mWeakDataOffset = -1;
		psi.mbDirty		= false;
		psi.mbMFM		= mGeometry.mbMFM;

		mImageChecksum += ATComputeBlockChecksum(ATComputeOffsetChecksum(mVirtSectors.size()), &mImage[psi.mOffset], psi.mImageSize);
	}
}

void ATDiskImage::LoadARC(IVDRandomAccessStream& stream, const wchar_t *origPath) {
	// mount the ARC file system
	vdautoptr<IATDiskFS> arcfs(ATDiskMountImageARC(stream, origPath));

	// iterate over all files and get an estimate of how much disk space we need:
	//
	// - data sectors
	// - map sectors (1 map sector per 60 data sectors in SD)
	// - bitmap sectors (1 sector per 1024 total sectors)
	// - 3 superblock/bootblock sectors
	// - directory sectors (23 bytes for root entry and for each file)

	uint32 dataSectors = 0;
	uint32 mapSectors = 0;
	uint32 fileCount = 0;

	ATDiskFSEntryInfo entryInfo;
	ATDiskFSFindHandle fh = arcfs->FindFirst(ATDiskFSKey::None, entryInfo);
	if (fh != ATDiskFSFindHandle::Invalid) {
		try {
			do {
				uint32 size = entryInfo.mBytes;
				uint32 secs = ((size + 127) >> 7);

				dataSectors += secs;
				mapSectors += (secs + 59) / 60;
				++fileCount;
			} while(arcfs->FindNext(fh, entryInfo));

			arcfs->FindEnd(fh);
		} catch(...) {
			arcfs->FindEnd(fh);
			throw;
		}
	}

	const uint32 bootSectors = 3;
	const uint32 dirSectors = ((fileCount + 1) * 23 + 127) >> 7;
	const uint32 dirMapSectors = (dirSectors + 59) / 60;
	const uint32 nonBitmapSectors = bootSectors + dirSectors + dirMapSectors + dataSectors + mapSectors;

	// computing the number of needed bitmap sectors is tricky as they are part
	// of the bitmap -- we do a loop here to handle the tricky case where the
	// addition of the bitmap itself requires adding bitmap sectors
	uint32 bitmapSectors = 0;
	for(;;) {
		uint32 t = ((nonBitmapSectors + bitmapSectors) >> 10) + 1;	// (!!) sector 0 bit is unused

		if (t <= bitmapSectors)
			break;

		bitmapSectors = t;
	}

	// compute total sector count needed
	uint32 totalSectors = bitmapSectors + nonBitmapSectors;

	// increase to a "nice" disk size if too small
	if (totalSectors < 720)
		totalSectors = 720;
	else if (totalSectors < 1040)
		totalSectors = 1040;
	else if (totalSectors < 1440)
		totalSectors = 1440;

	// initialize a new disk image (on us!)
	Init(totalSectors, bootSectors, 128);
	
	// mount SDFS on us
	vdautoptr<IATDiskFS> sdfs(ATDiskFormatImageSDX2(this, origPath ? VDTextWToA(VDFileSplitPath(origPath)).c_str() : NULL));

	// copy over files
	ATDiskFSFindHandle fh2 = arcfs->FindFirst(ATDiskFSKey::None, entryInfo);
	if (fh2 != ATDiskFSFindHandle::Invalid) {
		try {
			vdfastvector<uint8> buf;
			do {
				arcfs->ReadFile(entryInfo.mKey, buf);
				ATDiskFSKey fileKey = sdfs->WriteFile(ATDiskFSKey::None, entryInfo.mFileName.c_str(), buf.data(), (uint32)buf.size());
				sdfs->SetFileTimestamp(fileKey, entryInfo.mDate);
			} while(arcfs->FindNext(fh2, entryInfo));

			arcfs->FindEnd(fh2);
		} catch(...) {
			arcfs->FindEnd(fh2);
			throw;
		}
	}

	sdfs->Flush();

	mPath = origPath;
	mbHasDiskSource = true;
	mImageFormat = kATDiskImageFormat_None;
	ComputeGeometry();
}

void *ATDiskImage::AsInterface(uint32 id) {
	switch(id) {
		case IATDiskImage::kTypeID: return static_cast<IATDiskImage *>(this);
	}

	return nullptr;
}

ATDiskTimingMode ATDiskImage::GetTimingMode() const {
	return mTimingMode;
}

bool ATDiskImage::IsDirty() const {
	return mbDirty;
}

bool ATDiskImage::IsUpdatable() const {
	return mbHasDiskSource && mImageFormat != kATDiskImageFormat_None;
}

bool ATDiskImage::Flush() {
	if (!mbDirty)
		return true;

	if (!IsUpdatable())
		return false;

	if (mbDiskFormatDirty) {
		Save(VDStringW(mPath).c_str(), mImageFormat);
		return true;
	}

	// build a list of dirty sectors
	typedef vdfastvector<PhysSectorInfo *> DirtySectors;
	DirtySectors dirtySectors;

	for(PhysSectors::iterator it(mPhysSectors.begin()), itEnd(mPhysSectors.end()); it != itEnd; ++it) {
		PhysSectorInfo *psi = &*it;

		if (psi->mbDirty) {
			if (psi->mDiskOffset < 0) {
				// uh oh... this sector doesn't have a straightforward position on disk. force
				// a full write
				Save(VDStringW(mPath).c_str(), mImageFormat);
				return true;
			}

			dirtySectors.push_back(psi);
		}
	}

	// sort dirty sectors by on-disk position
	std::sort(dirtySectors.begin(), dirtySectors.end(),
		[](const PhysSectorInfo *x, const PhysSectorInfo *y) {
			return x->mDiskOffset < y->mDiskOffset;
		}
	);

	// open file for rewriting
	VDFile f(mPath.c_str(), nsVDFile::kWrite | nsVDFile::kDenyAll | nsVDFile::kOpenExisting);

	// check if we have enough dirty sectors to bother coalescing
	if (dirtySectors.size() < 16) {
		// no - write individual sectors
		for(PhysSectorInfo* psi : dirtySectors) {
			f.seek(psi->mDiskOffset);
			f.write(&mImage[psi->mOffset], psi->mImageSize);
		}
	} else {
		// yes - allocate coalescing buffer
		const uint32 kWriteBufLen = 65536;
		vdblock<uint8> writeBuf(kWriteBufLen);
		uint32 wbLevel = 0;

		DirtySectors::const_iterator it(dirtySectors.begin()), itEnd(dirtySectors.end());
		while(it != itEnd) {
			PhysSectorInfo *psi = *it;
			sint32 writeOffset = psi->mDiskOffset;
			sint32 writeLen = psi->mImageSize;
			
			f.seek(writeOffset);

			for(;;) {
				if (wbLevel + psi->mImageSize > kWriteBufLen) {
					f.write(writeBuf.data(), wbLevel);
					wbLevel = 0;
				}

				memcpy(writeBuf.data() + wbLevel, &mImage[psi->mOffset], psi->mImageSize);
				wbLevel += psi->mImageSize;

				if (++it == itEnd)
					break;

				psi = *it;

				if (psi->mDiskOffset != writeOffset + writeLen)
					break;

				writeLen += psi->mImageSize;
			}

			if (wbLevel) {
				f.write(writeBuf.data(), wbLevel);
				wbLevel = 0;
			}
		}
	}

	// clear dirty flags on sectors
	for(PhysSectorInfo* psi : dirtySectors)
		psi->mbDirty = false;

	// clear global dirty flag
	mbDirty = false;

	// all done
	return true;
}

void ATDiskImage::SetPath(const wchar_t *path, ATDiskImageFormat format) {
	mbDirty = true;
	mbDiskFormatDirty = true;
	mbHasDiskSource = true;
	mPath = path;
	mImageFormat = format;
}

void ATDiskImage::Save(const wchar_t *s, ATDiskImageFormat format) {
	// Image types:
	// ATR	Any sector size
	// XFD	128b sectors only
	// P2	128b sectors only
	// P3	128b sectors only
	// ATX	128b sectors only
	// DCM	128b or 256b sectors

	if (format == kATDiskImageFormat_None)
		throw MyError("Invalid disk image format type specified.");

	// scan for virtual sectors with errors or phantoms and check if it is supported
	bool supportPhantoms = false;
	uint32 maxPhantoms = 0;
	bool supportErrors = false;
	bool supportWeakSectors = false;
	bool supportSectorSize256 = false;
	bool supportSectorSizeOther = false;

	switch(format) {
		case kATDiskImageFormat_ATR:
			supportSectorSize256 = true;
			supportSectorSizeOther = true;
			break;

		case kATDiskImageFormat_P2:
		case kATDiskImageFormat_P3:
			supportPhantoms = true;
			supportErrors = true;
			supportWeakSectors = false;
			maxPhantoms = 7;
			break;

		case kATDiskImageFormat_ATX:
			if (mSectorSize != 128)
				throw MyError("Cannot save disk image: disk geometry is not supported.");

			supportPhantoms = true;
			supportErrors = true;
			supportWeakSectors = true;
			supportSectorSize256 = true;
			supportSectorSizeOther = true;
			break;

		case kATDiskImageFormat_DCM:
			// DCM only supports three standard disk formats.
			if ((mSectorSize != 128 || mVirtSectors.size() != 720)
				&& (mSectorSize != 128 || mVirtSectors.size() != 1040)
				&& (mSectorSize != 256 || mVirtSectors.size() != 720))
			{
				throw MyError("Cannot save disk image: disk geometry is not supported.");
			}

			supportSectorSize256 = true;
			break;

		case kATDiskImageFormat_XFD:
			supportSectorSize256 = true;
			break;
	}

	if (!supportPhantoms) {
		for(const VirtSectorInfo& vsi : mVirtSectors) {
			if (vsi.mNumPhysSectors != 1)
				throw MyError("Cannot save disk image: disk contains phantom or missing sectors.");
		}
	} else if (maxPhantoms) {
		for(const VirtSectorInfo& vsi : mVirtSectors) {
			if (vsi.mNumPhysSectors > maxPhantoms)
				throw MyError("Cannot save disk image: disk contains too many phantom sectors.");
		}
	}

	for(const PhysSectorInfo& psi : mPhysSectors) {
		if (!supportErrors) {
			if (psi.mFDCStatus != 0xFF)
				throw MyError("Cannot save disk image: disk contains sector errors.");
		}

		if (!supportWeakSectors) {
			if (psi.mWeakDataOffset >= 0)
				throw MyError("Cannot save disk image: disk contains weak data sectors.");
		}

		if (psi.mPhysicalSize == 256 ? !supportSectorSize256 : (psi.mPhysicalSize != 128 && !supportSectorSizeOther))
			throw MyError("Cannot save disk image: disk contains an unsupported sector size.");
	}

	// copy the temp physical sector array as we will need to rewrite the disk offsets,
	// but don't want to leave them trashed if the save fails midway
	PhysSectors tempPhysSectors(mPhysSectors);

	VDFile f(s, nsVDFile::kWrite | nsVDFile::kDenyAll | nsVDFile::kCreateAlways);

	switch(format) {
		case kATDiskImageFormat_ATR:
			SaveATR(f, tempPhysSectors);
			break;

		case kATDiskImageFormat_XFD:
			SaveXFD(f, tempPhysSectors);
			break;

		case kATDiskImageFormat_P2:
			SaveP2(f, tempPhysSectors);
			break;

		case kATDiskImageFormat_P3:
			SaveP3(f, tempPhysSectors);
			break;

		case kATDiskImageFormat_ATX:
			SaveATX(f, tempPhysSectors);
			break;

		case kATDiskImageFormat_DCM:
			SaveDCM(f, tempPhysSectors);
			break;
	}

	// swap the new physical sector array in and clear the dirty flags
	mPhysSectors.swap(tempPhysSectors);
	for(PhysSectors::iterator it(mPhysSectors.begin()), itEnd(mPhysSectors.end());
		it != itEnd;
		++it)
	{
		PhysSectorInfo& psi = *it;
		psi.mbDirty = false;
	}

	mPath = s;
	mbDirty = false;
	mbDiskFormatDirty = false;
	mbHasDiskSource = true;
	mImageFormat = format;
}

uint32 ATDiskImage::GetPhysicalSectorCount() const {
	return (uint32)mPhysSectors.size();
}

void ATDiskImage::GetPhysicalSectorInfo(uint32 index, ATDiskPhysicalSectorInfo& info) const {
	info = mPhysSectors[index];
}

uint32 ATDiskImage::GetVirtualSectorCount() const {
	return (uint32)mVirtSectors.size();
}

void ATDiskImage::GetVirtualSectorInfo(uint32 index, ATDiskVirtualSectorInfo& info) const {
	info = mVirtSectors[index];
}

void ATDiskImage::ReadPhysicalSector(uint32 index, void *data, uint32 len) {
	const PhysSectorInfo& psec = mPhysSectors[index];

	if (len > psec.mPhysicalSize)
		len = psec.mPhysicalSize;

	const uint32 copyLen = std::min<uint32>(len, psec.mImageSize);
	memcpy(data, mImage.data() + psec.mOffset, copyLen);

	if (copyLen < len)
		memset((char *)data + copyLen, 0, len - copyLen);
}

void ATDiskImage::WritePhysicalSector(uint32 index, const void *data, uint32 len, uint8 fdcStatus) {
	PhysSectorInfo& psi = mPhysSectors[index];

	memcpy(mImage.data() + psi.mOffset, data, std::min<uint32>(len, psi.mImageSize));
	psi.mbDirty = true;
	psi.mFDCStatus = fdcStatus;
	mbDirty = true;
	mImageFileCRC.reset();
}

uint32 ATDiskImage::ReadVirtualSector(uint32 index, void *data, uint32 len) {
	if (index >= (uint32)mVirtSectors.size())
		return 0;

	const VirtSectorInfo& vsi = mVirtSectors[index];

	if (!vsi.mNumPhysSectors)
		return 0;

	const PhysSectorInfo& psi = mPhysSectors[vsi.mStartPhysSector];

	if (len > psi.mPhysicalSize)
		len = psi.mPhysicalSize;

	if (len) {
		const uint32 copyLen = std::min<uint32>(len, psi.mImageSize);
		memcpy(data, mImage.data() + psi.mOffset, copyLen);

		if (copyLen < len)
			memset((char *)data + copyLen, 0, len - copyLen);
	}

	return len;
}

bool ATDiskImage::WriteVirtualSector(uint32 index, const void *data, uint32 len) {
	if (index >= (uint32)mVirtSectors.size())
		return false;

	const VirtSectorInfo& vsi = mVirtSectors[index];

	if (!vsi.mNumPhysSectors)
		return false;

	PhysSectorInfo& psi = mPhysSectors[vsi.mStartPhysSector];

	if (len != psi.mImageSize)
		return false;

	memcpy(mImage.data() + psi.mOffset, data, len);
	psi.mbDirty = true;
	mbDirty = true;
	mImageFileCRC.reset();
	return true;
}

void ATDiskImage::Resize(uint32 newSectors) {
	uint32 curSectors = (uint32)mVirtSectors.size();

	if (curSectors == newSectors)
		return;

	const uint32 newPhysStart = (uint32)mPhysSectors.size();

	// check if we're shrinking
	if (newSectors < curSectors) {
		// Mark the image dirty.
		mbDirty = true;
		mbDiskFormatDirty = true;
		mImageFileCRC.reset();

		// Remove the extra virtual sectors.
		mVirtSectors.resize(newSectors);

		// Sort virtual sectors by physical sector order.
		vdfastvector<ATDiskVirtualSectorInfo *> vsptrs(newSectors);
		for(uint32 i = 0; i < curSectors; ++i)
			vsptrs[i] = &mVirtSectors[i];

		std::sort(vsptrs.begin(), vsptrs.end(),
			[](const ATDiskVirtualSectorInfo *p, const ATDiskVirtualSectorInfo *q) { return p->mStartPhysSector < q->mStartPhysSector; }
		);

		// Compact the physical sector array.
		uint32 newPhys = 0;

		for(ATDiskVirtualSectorInfo *vs : vsptrs) {
			const uint32 start = vs->mStartPhysSector;
			const uint32 count = vs->mNumPhysSectors;

			if (start != newPhys)
				std::copy(mPhysSectors.begin() + start, mPhysSectors.begin() + start + count, mPhysSectors.begin() + newPhys);

			vs->mStartPhysSector = newPhys;
			newPhys += count;
		}

		// Trim the physical sector array.
		mPhysSectors.resize(newPhys);

		// Image compaction is more tricky because sectors may overlap in the image. They
		// generally shouldn't, but we don't always do the full checking necessary at load
		// time to avoid this. Instead, we build a free list and compact by that. It's slower,
		// but safer.

		typedef std::pair<uint32, uint32> Span;
		std::vector<Span> imageSpans(newPhys);

		// Extract spans from all remaining physical sectors.
		for(uint32 i = 0; i < newPhys; ++i) {
			const auto& ps = mPhysSectors[i];
			imageSpans[i] = { ps.mOffset, ps.mOffset + ps.mImageSize };
		}

		// Sort spans by ascending starting range.
		std::sort(imageSpans.begin(), imageSpans.end(), [](const Span& a, const Span& b) { return a.first < b.first; });

		// Run through the list and collapse gaps, turning ranges into remapping pairs
		// as we go.
		uint32 dstLastStart = 0;
		uint32 dstLastEnd = 0;
		uint32 srcLastStart = 0;
		uint32 srcLastEnd = 0;
		uint32 imageSizeDelta = 0;

		for(auto& span : imageSpans) {
			if (span.first > srcLastEnd) {
				// shift the previous block
				memmove(&mImage[dstLastStart], &mImage[srcLastStart], dstLastEnd - dstLastStart);

				// remove the gap from the remapping
				imageSizeDelta += span.first - srcLastEnd;

				// start a new block
				dstLastStart = dstLastEnd;
				srcLastStart = span.first;
				srcLastEnd = span.second;
			} else if (span.second > srcLastEnd) {
				srcLastEnd = span.second;
				dstLastEnd = srcLastEnd - imageSizeDelta;
			}

			span.second = span.first - imageSizeDelta;
		}

		// shift the last block
		memmove(&mImage[dstLastStart], &mImage[srcLastStart], dstLastEnd - dstLastStart);

		// trim the image buffer; note that this won't actually release
		// memory, but it does prevent us from accumulating cruft on a later
		// extend
		mImage.resize(dstLastEnd);

		// relocate all offsets
		for(auto& ps : mPhysSectors) {
			ps.mOffset = std::lower_bound(imageSpans.begin(), imageSpans.end(), ps.mOffset,
				[](const Span& a, uint32 b) { return a.first < b; })->second;
		}
	} else {
		// We're growing. Extend the image first.
		const uint32 sectorsToAdd = newSectors - curSectors;
		const uint32 imageOffset = (uint32)mImage.size();

		mImage.resize(imageOffset + mSectorSize * sectorsToAdd, 0);
		
		try {
			// Extend the physical sector array.
			mPhysSectors.resize(newPhysStart + sectorsToAdd);

			try {
				// Initialize the new physical sectors.
				for(uint32 i = 0; i < sectorsToAdd; ++i) {
					auto& ps = mPhysSectors[newPhysStart + i];

					ps.mbDirty = true;
					ps.mDiskOffset = -1;
					ps.mFDCStatus = 0xFF;
					ps.mOffset = imageOffset + mSectorSize * i;
					ps.mRotPos = 0;
					ps.mImageSize = mSectorSize;
					ps.mPhysicalSize = mSectorSize;
					ps.mWeakDataOffset = -1;
				}
		
				// Extend the virtual sector array.
				mVirtSectors.resize(newSectors);

				// At this point, we're clear -- we do no allocation past
				// this point, so we can commit all arrays.

				// Initialize the new virtual sectors.
				for(uint32 i = 0; i < sectorsToAdd; ++i) {
					auto& vs = mVirtSectors[curSectors + i];
					vs.mNumPhysSectors = 1;
					vs.mStartPhysSector = newPhysStart + i;
				}

				ComputeGeometry();

				// Go back and give the new physical sectors some sort of sane
				// interleave.
				for(uint32 i = 0; i < sectorsToAdd; ++i) {
					auto& ps = mPhysSectors[newPhysStart + i];
					uint32 trackSec = (curSectors + i) % mSectorsPerTrack;

					if (trackSec & 1)
						trackSec += mSectorsPerTrack;

					trackSec >>= 1;

					ps.mRotPos = (float)trackSec / (float)mSectorsPerTrack;
				}

				// Mark the disk dirty.
				mbDirty = true;
				mbDiskFormatDirty = true;
				mImageFileCRC.reset();
			} catch(...) {
				mPhysSectors.resize(newPhysStart);
				throw;
			}
		} catch(...) {
			mImage.resize(imageOffset);
			throw;
		}
	}
}

void ATDiskImage::FormatTrack(uint32 vsIndexStart, uint32 vsCount, const ATDiskVirtualSectorInfo *vsecs, uint32 psCount, const ATDiskPhysicalSectorInfo *psecs, const uint8 *psecData) {
	// Compute total size needed for old and new sectors.
	const uint32 totalVirtSecs = std::max<uint32>((uint32)mVirtSectors.size(), vsIndexStart + vsCount);
	const uint32 existingVirtSecs = (uint32)mVirtSectors.size();
	uint32 totalImageSize = 0;
	uint32 totalPhysSecs = 0;

	for(uint32 i=0; i<totalVirtSecs; ++i) {
		if (i >= vsIndexStart && i - vsIndexStart < vsCount) {
			const auto& vsi = vsecs[i - vsIndexStart];

			for(uint32 j=0; j<vsi.mNumPhysSectors; ++j) {
				const auto& psi = psecs[vsi.mStartPhysSector + j];

				totalImageSize += psi.mImageSize;
			}

			totalPhysSecs += vsi.mNumPhysSectors;
		} else if (i < existingVirtSecs) {
			const auto& vsi = mVirtSectors[i];

			for(uint32 j=0; j<vsi.mNumPhysSectors; ++j) {
				const auto& psi = mPhysSectors[vsi.mStartPhysSector + j];

				totalImageSize += psi.mImageSize;
			}

			totalPhysSecs += vsi.mNumPhysSectors;
		}
	}

	// allocate new vsec, psec, and image arrays
	PhysSectors newPhysSectors;
	newPhysSectors.resize(totalPhysSecs);

	VirtSectors newVirtSectors(totalVirtSecs, {});

	vdfastvector<uint8> newImage;
	newImage.resize(totalImageSize);

	// copy over sector data
	auto *dstPhys = newPhysSectors.data();
	auto *dstImageBase = newImage.data();
	auto *dstImage = dstImageBase;
	uint32 nextPhys = 0;

	for(uint32 i=0; i<totalVirtSecs; ++i) {
		uint32 numPhys = 0;
		const ATDiskPhysicalSectorInfo *srcPhys = nullptr;
		const uint8 *srcImageBase = nullptr;

		if (i >= vsIndexStart && i - vsIndexStart < vsCount) {
			const auto& vsi = vsecs[i - vsIndexStart];

			numPhys = vsi.mNumPhysSectors;
			srcPhys = &psecs[vsi.mStartPhysSector];
			srcImageBase = psecData;
		} else if (i < existingVirtSecs) {
			const auto& vsi = mVirtSectors[i];

			numPhys = vsi.mNumPhysSectors;
			srcPhys = &mPhysSectors[vsi.mStartPhysSector];
			srcImageBase = mImage.data();
		}

		newVirtSectors[i] = { nextPhys, numPhys };
		nextPhys += numPhys;

		for(uint32 j=0; j<numPhys; ++j) {
			*dstPhys = *srcPhys;
			dstPhys->mOffset = 0;

			if (srcPhys->mImageSize > 0) {
				dstPhys->mOffset = (uint32)(dstImage - dstImageBase);
				memcpy(dstImage, srcImageBase + srcPhys->mOffset, srcPhys->mImageSize);
				dstImage += srcPhys->mImageSize;
			}

			++dstPhys;
			++srcPhys;
		}
	}

	VDASSERT(dstPhys - newPhysSectors.data() == newPhysSectors.size());
	VDASSERT(dstImage - dstImageBase == newImage.size());

	// swap arrays
	mPhysSectors.swap(newPhysSectors);
	mVirtSectors.swap(newVirtSectors);
	mImage.swap(newImage);

	mbDirty = true;
	mbDiskFormatDirty = true;
	mImageFileCRC.reset();

	// if we overwrote track 0 / sector 1, force the disk geometry MFM flag.
	if (vsIndexStart == 0 && totalVirtSecs > 0) {
		const ATDiskVirtualSectorInfo& vsi0 = mVirtSectors[0];

		if (vsi0.mNumPhysSectors) {
			const ATDiskPhysicalSectorInfo *psis = &mPhysSectors[vsi0.mStartPhysSector];

			mGeometry.mbMFM = false;

			for(uint32 i=0; i<vsi0.mNumPhysSectors; ++i) {
				if (psis[i].mbMFM) {
					mGeometry.mbMFM = true;
					break;
				}
			}

		}
	}
}

bool ATDiskImage::IsSafeToReinterleave() const {
	for(const ATDiskVirtualSectorInfo& vsi : mVirtSectors) {
		if (vsi.mNumPhysSectors > 1)
			return false;
	}

	return true;
}

void ATDiskImage::Reinterleave(ATDiskInterleave interleave) {
	const auto interleaveFn = ATDiskGetInterleaveFn(interleave, mGeometry);
	uint32 vsIndex = 0;

	for(const ATDiskVirtualSectorInfo& vsi : mVirtSectors) {
		const uint32 n = vsi.mNumPhysSectors;

		if (n) {
			const uint32 psec0 = vsi.mStartPhysSector;
			float pos = interleaveFn(vsIndex);
			float posInc = 1.0f / (float)n;

			for(uint32 i=0; i<n; ++i) {
				mPhysSectors[psec0 + i].mRotPos = pos;
				pos += posInc;
			}
		}

		++vsIndex;
	}
}

void ATDiskImage::ComputeGeometry() {
	uint32 sectorCount = (uint32)mVirtSectors.size();
	mSectorsPerTrack = mSectorSize >= 512 ? sectorCount : mSectorSize >= 256 ? 18 : sectorCount > 720 && !(sectorCount % 26) ? 26 : 18;

	mGeometry.mTrackCount = 1;
	mGeometry.mSideCount = 1;
	mGeometry.mbMFM = false;
	mGeometry.mSectorsPerTrack = sectorCount;
	mGeometry.mBootSectorCount = mBootSectorCount;
	mGeometry.mSectorSize = mSectorSize;
	mGeometry.mTotalSectorCount = sectorCount;

	if (mGeometry.mBootSectorCount > 0) {
		if (mGeometry.mSectorSize == 128) {
			switch(mGeometry.mTotalSectorCount) {
				default:
					if (mGeometry.mTotalSectorCount > 720)
						break;

					// fall through
				case 720:
					mGeometry.mSectorsPerTrack = 18;
					mGeometry.mSideCount = 1;
					break;

				case 1440:
				case 2880:
					mGeometry.mSectorsPerTrack = 18;
					mGeometry.mSideCount = 2;
					break;

				case 1040:
					mGeometry.mSectorsPerTrack = 26;
					mGeometry.mSideCount = 1;
					mGeometry.mbMFM = true;
					break;
			}
		} else if (mGeometry.mSectorSize == 256) {
			switch(mGeometry.mTotalSectorCount) {
				case 720:
					mGeometry.mSectorsPerTrack = 18;
					mGeometry.mSideCount = 1;
					mGeometry.mbMFM = true;
					break;

				case 1440:
				case 2880:
					mGeometry.mSectorsPerTrack = 18;
					mGeometry.mSideCount = 2;
					mGeometry.mbMFM = true;
					break;
			}
		}
	}

	if (mGeometry.mSectorsPerTrack > 0)
		mGeometry.mTrackCount = (mGeometry.mTotalSectorCount + mGeometry.mSectorsPerTrack - 1) / mGeometry.mSectorsPerTrack;

	if (mGeometry.mSideCount > 1)
		mGeometry.mTrackCount = (mGeometry.mTrackCount + 1) >> 1;
}

void ATDiskImage::SaveATR(VDFile& f, PhysSectors& phySecs) {
	// compute total sector sizes
	uint32 totalSize = mBootSectorCount * 128;
	const uint32 numVsecs = (uint32)mVirtSectors.size();
	for(uint32 vsIndex = mBootSectorCount; vsIndex < numVsecs; ++vsIndex) {
		const VirtSectorInfo& vsi = mVirtSectors[vsIndex];

		const PhysSectorInfo& psi = phySecs[vsi.mStartPhysSector];
		totalSize += psi.mImageSize;
	}

	// create ATR header
	uint8 header[16] = {0};
	uint32 paras = totalSize >> 4;
	VDWriteUnalignedLEU16(header+0, 0x0296);
	VDWriteUnalignedLEU16(header+2, (uint16)paras);
	VDWriteUnalignedLEU16(header+4, mSectorSize);
	header[6] = (uint8)(paras >> 16);

	f.write(header, 16);

	uint32 diskOffset = 16;

	vdblock<char> sectorBuffer;

	for(uint32 vsIndex = 0; vsIndex < numVsecs; ++vsIndex) {
		const VirtSectorInfo& vsi = mVirtSectors[vsIndex];
		PhysSectorInfo& psi = phySecs[vsi.mStartPhysSector];

		const uint32 writeSize = vsIndex < mBootSectorCount ? 128 : mSectorSize;

		if (psi.mImageSize <= writeSize) {
			f.write(&mImage[psi.mOffset], psi.mImageSize);
		} else {
			sectorBuffer.resize(writeSize);

			const uint32 copyLen = std::min<uint32>(writeSize, psi.mImageSize);
			if (copyLen)
				memcpy(sectorBuffer.data(), &mImage[psi.mOffset], copyLen);

			if (copyLen < writeSize)
				memset(sectorBuffer.data() + copyLen, 0, writeSize - copyLen);
		}

		psi.mDiskOffset = diskOffset;
		diskOffset += psi.mImageSize;
	}
}

void ATDiskImage::SaveXFD(VDFile& f, PhysSectors& phySecs) {
	uint32 diskOffset = 0;

	vdblock<char> sectorBuffer;

	for(const VirtSectorInfo& vsi : mVirtSectors) {
		PhysSectorInfo& psi = phySecs[vsi.mStartPhysSector];

		// If we have a 128 byte boot sector on a double-density disk, we must pad
		// the sectors to 256 bytes.

		if (psi.mImageSize < mSectorSize) {
			sectorBuffer.resize(mSectorSize);

			const uint32 copyLen = std::min<uint32>(mSectorSize, psi.mImageSize);
			if (copyLen)
				memcpy(sectorBuffer.data(), &mImage[psi.mOffset], copyLen);

			if (copyLen < mSectorSize)
				memset(sectorBuffer.data() + copyLen, 0, mSectorSize - copyLen);

			f.write(sectorBuffer.data(), mSectorSize);
		} else
			f.write(&mImage[psi.mOffset], mSectorSize);

		psi.mDiskOffset = diskOffset;
		diskOffset += mSectorSize;
	}
}

void ATDiskImage::SaveP2(VDFile& f, PhysSectors& phySecs) {
	// select emulation mode
	uint8 mode = 0x00;

	for(const VirtSectorInfo& vsi : mVirtSectors) {
		if (vsi.mNumPhysSectors > 1) {
			// select flip/flop if there are any phantom sectors
			mode = 0x02;
			break;
		}
	}

	// write file header
	const uint32 numSectors = (uint32)mVirtSectors.size();
	uint8 header[16] = {0};

	VDWriteUnalignedBEU16(&header[0], (uint16)mPhysSectors.size());
	header[2] = (uint8)'P';
	header[3] = (uint8)'2';
	header[4] = mode;

	f.write(header, 16);

	vdfastvector<uint8> phantomSectorCounts(numSectors, 0);
	vdfastvector<const PhysSectorInfo *> sectorOrdering(numSectors, nullptr);

	for(uint32 i=0; i<numSectors; ++i) {
		const VirtSectorInfo& vsi = mVirtSectors[i];

		if (!vsi.mNumPhysSectors)
			continue;

		PhysSectorInfo *psi = &phySecs[vsi.mStartPhysSector];
		psi->mDiskOffset = 16 + 12 + (12+128) * i;
		sectorOrdering[i] = psi++;
		phantomSectorCounts[i] = (uint8)(vsi.mNumPhysSectors - 1);

		for(uint32 j=1; j<vsi.mNumPhysSectors; ++j) {
			psi->mDiskOffset = 16 + 12 + (12+128) * (uint32)sectorOrdering.size();
			sectorOrdering.push_back(psi++);
			phantomSectorCounts.push_back(0);
		}
	}

	// write sector headers
	struct PROSectorData {
		uint8 mStatus[5];
		uint8 mPhantomCount;
		uint8 mPhantoms[6];
		uint8 mData[128];
	} sectorData;

	// set time out
	sectorData.mStatus[2] = 0xE0;
	sectorData.mStatus[3] = 0x00;

	const uint32 totalSectors = (uint32)sectorOrdering.size();
	uint32 phantomIndex = 1;
	for(uint32 i=0; i<totalSectors; ++i) {
		const PhysSectorInfo *psi = sectorOrdering[i];
		memset(sectorData.mPhantoms, 0, sizeof sectorData.mPhantoms);

		// set drive and FDC status and copy sector data
		if (psi) {
			memcpy(sectorData.mData, &mImage[psi->mOffset], 128);

			sectorData.mStatus[1] = psi->mFDCStatus;
		} else {
			// missing sector data
			sectorData.mStatus[1] = 0xF7;
		}

		sectorData.mStatus[0] = (sectorData.mStatus[1] != 0xFF) ? 0x1C : 0x18;

		// compute checksum
		uint32 checkSum = sectorData.mStatus[0] + sectorData.mStatus[1] + sectorData.mStatus[2] + sectorData.mStatus[3];

		while(checkSum >= 0x100)
			checkSum = (checkSum & 0xFF) + (checkSum >> 8);

		sectorData.mStatus[4] = checkSum;
		sectorData.mPhantomCount = phantomSectorCounts[i];

		for(uint32 i = 1; i <= sectorData.mPhantomCount; ++i)
			sectorData.mPhantoms[i] = phantomIndex++;

		f.write(&sectorData, sizeof sectorData);
	}
}

void ATDiskImage::SaveP3(VDFile& f, PhysSectors& phySecs) {
	// select emulation mode
	uint8 mode = 0x00;

	for(const VirtSectorInfo& vsi : mVirtSectors) {
		if (vsi.mNumPhysSectors > 1) {
			// select flip/flop if there are any phantom sectors
			mode = 0x02;
			break;
		}
	}

	// write file header
	const uint32 numSectors = (uint32)mVirtSectors.size();
	uint8 header[16] = {0};

	VDWriteUnalignedBEU16(&header[0], (uint16)mPhysSectors.size());
	header[2] = (uint8)'P';
	header[3] = (uint8)'3';
	header[4] = mode;
	VDWriteUnalignedBEU16(&header[6], (uint16)numSectors);

	f.write(header, 16);

	vdfastvector<uint8> phantomSectorCounts(numSectors, 0);
	vdfastvector<const PhysSectorInfo *> sectorOrdering(numSectors, nullptr);

	for(uint32 i=0; i<numSectors; ++i) {
		const VirtSectorInfo& vsi = mVirtSectors[i];

		if (!vsi.mNumPhysSectors)
			continue;

		PhysSectorInfo *psi = &phySecs[vsi.mStartPhysSector];
		psi->mDiskOffset = 16 + 12 + (12+128) * i;
		sectorOrdering[i] = psi++;
		phantomSectorCounts[i] = (uint8)(vsi.mNumPhysSectors - 1);

		for(uint32 j=1; j<vsi.mNumPhysSectors; ++j) {
			psi->mDiskOffset = 16 + 12 + (12+128) * (uint32)sectorOrdering.size();
			sectorOrdering.push_back(psi++);
			phantomSectorCounts.push_back(0);
		}
	}

	// write sector headers
	struct PROSectorData {
		uint8 mStatus[5];
		uint8 mPhantomCount;
		uint8 mPhantoms[6];
		uint8 mData[128];
	} sectorData;

	// set time out
	sectorData.mStatus[2] = 0xE0;
	sectorData.mStatus[3] = 0x00;

	const uint32 totalSectors = (uint32)sectorOrdering.size();
	uint32 phantomIndex = 1;
	for(uint32 i=0; i<totalSectors; ++i) {
		const PhysSectorInfo *psi = sectorOrdering[i];
		memset(sectorData.mPhantoms, 0, sizeof sectorData.mPhantoms);

		// set drive and FDC status and copy sector data
		if (psi) {
			memcpy(sectorData.mData, &mImage[psi->mOffset], 128);

			sectorData.mStatus[1] = psi->mFDCStatus;
		} else {
			// missing sector data
			sectorData.mStatus[1] = 0xF7;
		}

		sectorData.mStatus[0] = (sectorData.mStatus[1] != 0xFF) ? 0x1C : 0x18;

		// compute checksum
		uint32 checkSum = sectorData.mStatus[0] + sectorData.mStatus[1] + sectorData.mStatus[2] + sectorData.mStatus[3];

		while(checkSum >= 0x100)
			checkSum = (checkSum & 0xFF) + (checkSum >> 8);

		sectorData.mStatus[4] = checkSum;
		sectorData.mPhantomCount = phantomSectorCounts[i];

		for(uint32 i = 1; i <= sectorData.mPhantomCount; ++i)
			sectorData.mPhantoms[i] = phantomIndex++;

		f.write(&sectorData, sizeof sectorData);
	}
}

void ATDiskImage::SaveDCM(VDFile& f, PhysSectors& phySecs) {
	uint8 packBuf[0x6200];
	uint32 packBufLevel = 0;

	// Initialize pass header at the base of the pack buffer
	packBuf[0] = 0xFA;		// single file

	if (mSectorSize == 128) {
		if (mVirtSectors.size() == 720)
			packBuf[1] = 0x80;	// pass 0, last pass, single density
		else
			packBuf[1] = 0xA0;	// pass 0, last pass, enhanced density
	} else {
		packBuf[1] = 0xC0;	// pass 0, last pass, double density
	}

	packBuf[2] = 0;
	packBuf[3] = 0;
	packBufLevel = 4;

	uint8 buf1[256] = {0};
	uint8 buf2[256];

	uint8 *prevBuf = buf1;
	uint8 *nextBuf = buf2;
	uint32 prevSector = 0;
	sint32 prevSectorFlagOffset = -1;

	uint8 rleBuf[512];

	const uint32 numSecs = (uint32)mVirtSectors.size();
	for(uint32 sector=1; sector<=numSecs; ++sector) {
		const VirtSectorInfo& vsi = mVirtSectors[sector - 1];
		PhysSectorInfo& psi = phySecs[vsi.mStartPhysSector];

		// invalidate the image offset since DCMs cannot be rewritten in place
		psi.mDiskOffset = -1;

		// copy sector data into sector buffer, since we need to pad 128b
		// to 256b in DD mode
		const uint32 copyLen = std::min<uint32>(mSectorSize, psi.mImageSize);
		memcpy(nextBuf, &mImage[psi.mOffset], copyLen);
		memset(nextBuf + copyLen, 0, sizeof(buf2) - copyLen);

		// check for an all zero sector
		bool allZero = true;
		for(uint32 i=0; i<mSectorSize; ++i) {
			if (nextBuf[i]) {
				allZero = false;
				break;
			}
		}

		// if the sector is all zero, skip -- do not update prev/next buffers
		if (allZero)
			continue;

		// update header if this is the first sector in the pass
		if (prevSectorFlagOffset < 0)
			VDWriteUnalignedLEU16(&packBuf[2], sector);

		uint32 packBufSectorStart = packBufLevel;

		// compute common prefix
		uint32 prefixLen = 0;
		while(prefixLen < mSectorSize && prevBuf[prefixLen] == nextBuf[prefixLen])
			++prefixLen;

		// check if the entire sector is the same as prev
		if (prefixLen == mSectorSize) {
			// repeat last sector
			packBuf[packBufLevel++] = 0x46;
		} else {
			// compute common suffix
			uint32 suffixLen = 0;
			while(suffixLen < mSectorSize && prevBuf[mSectorSize - 1 - suffixLen] == nextBuf[mSectorSize - 1 - suffixLen])
				++suffixLen;

			// attempt RLE compression
			uint32 runStart = 0;
			uint32 rleLen = 0;

			while(runStart < mSectorSize) {
				VDASSERT(rleLen <= sizeof(rleBuf));
				// compute uncompressed length
				uint32 runEnd = runStart;
				for(;;) {
					// If we have three bytes or less, just include them in the current
					// uncompressed run and then exit.
					if (runEnd >= mSectorSize - 3) {
						runEnd = mSectorSize;
						break;
					}

					// Check if we can start a run.
					if (nextBuf[runEnd] == nextBuf[runEnd + 1]
						&& nextBuf[runEnd] == nextBuf[runEnd + 2])
					{
						// yes -- terminate the uncompressed run
						break;
					}

					// include this byte in the uncompressed run
					++runEnd;
				}

				// check if we are encoding a 256 byte uncompressed run -- this is
				// a special case that the encoding doesn't support. fortunately,
				// it's also suboptimal, so we should never use it.
				if (runStart == 0 && runEnd == 256) {
					// mark the RLE encoding as max sector size and bail
					rleLen = mSectorSize;
					break;
				}

				// copy uncompressed run to RLE buffer
				rleBuf[rleLen++] = runEnd;
				memcpy(&rleBuf[rleLen], &nextBuf[runStart], runEnd - runStart);
				rleLen += runEnd - runStart;

				// check if we're done
				if (runEnd >= mSectorSize)
					break;

				// begin encoding RLE segment
				const uint8 runVal = nextBuf[runEnd];
				runStart = runEnd;
			
				do {
					++runEnd;
				} while(runEnd < mSectorSize && nextBuf[runEnd] == runVal);

				// write compressed run
				rleBuf[rleLen++] = runEnd;
				rleBuf[rleLen++] = runVal;

				// loop back for next uncompressed run
				runStart = runEnd;
			}

			// compare sizes
			const uint32 prefixEncodingSize = prefixLen > 1 ? mSectorSize - prefixLen + 1 : ~(uint32)0;
			const uint32 suffixEncodingSize = suffixLen > 1 ? mSectorSize - suffixLen + 1 : ~(uint32)0;

			// pick smallest encoding -- note that RLE is set up to win
			// over prefix/suffix whenever none are advantageous
			if (rleLen < prefixEncodingSize && rleLen < suffixEncodingSize) {
				if (rleLen >= mSectorSize) {
					// write uncompressed sector
					packBuf[packBufLevel++] = 0x47;
					memcpy(&packBuf[packBufLevel], nextBuf, mSectorSize);
					packBufLevel += mSectorSize;
				} else {
					// write RLE sector
					packBuf[packBufLevel++] = 0x43;
					memcpy(&packBuf[packBufLevel], rleBuf, rleLen);
					packBufLevel += rleLen;
				}
			} else if (prefixEncodingSize < suffixEncodingSize) {
				// write modify end
				packBuf[packBufLevel++] = 0x44;
				packBuf[packBufLevel++] = prefixLen;
				memcpy(&packBuf[packBufLevel], nextBuf + prefixLen, mSectorSize - prefixLen);
				packBufLevel += mSectorSize - (uint8)prefixLen;
			} else {
				// write modify begin
				packBuf[packBufLevel++] = 0x41;
				packBuf[packBufLevel++] = (uint8)(mSectorSize - 1 - suffixLen);

				for(uint32 i = mSectorSize - suffixLen; i; --i)
					packBuf[packBufLevel++] = nextBuf[i - 1];
			}
		}

		// check if the pack buffer is full -- must not exceed 0x6000 bytes
		if (packBufLevel >= 0x5FFF) {
			// set sequential flag on last sector
			if (prevSectorFlagOffset >= 0)
				packBuf[prevSectorFlagOffset] |= 0x80;

			// temporarily clear the final pass flag in the header
			packBuf[1] &= 0x7F;

			// write out the previous pass, NOT including this sector
			f.write(packBuf, packBufSectorStart);

			// write out pass end
			const uint8 passEnd = 0x45;
			f.write(&passEnd, 1);

			// move new data down
			uint32_t newDataLen = packBufLevel - packBufSectorStart;
			memmove(&packBuf[4], &packBuf[packBufSectorStart], newDataLen);
			packBufLevel = 4 + newDataLen;

			// set final pass flag again and increment pass number
			packBuf[1] = ((packBuf[1] + 1) & 0x1F) + (packBuf[1] & 0x60) + 0x80;

			// update header starting sector
			VDWriteUnalignedLEU16(&packBuf[2], sector);

			prevSectorFlagOffset = 4;
		} else {
			// pack buffer not full -- set the previous sector pointer
			if (prevSectorFlagOffset >= 0) {
				if (prevSector + 1 == sector) {
					// set sequential flag on previous sector
					packBuf[prevSectorFlagOffset] |= 0x80;
				} else {
					// insert new sector number
					memmove(&packBuf[packBufSectorStart + 2], &packBuf[packBufSectorStart], packBufLevel - packBufSectorStart);
					VDWriteUnalignedLEU16(&packBuf[packBufSectorStart], sector);
					packBufLevel += 2;
					packBufSectorStart += 2;
				}
			}

			prevSectorFlagOffset = packBufSectorStart;
		}

		// swap the sector buffers and go on
		std::swap(nextBuf, prevBuf);
		prevSector = sector;
	}

	// terminate last sector number
	if (prevSectorFlagOffset >= 0)
		packBuf[prevSectorFlagOffset] |= 0x80;

	// write out final pass
	f.write(packBuf, packBufLevel);
	const uint8 passEnd = 0x45;
	f.write(&passEnd, 1);
}

void ATDiskImage::SaveATX(VDFile& f, PhysSectors& phySecs) {
	ATXHeader hdr = {};
	memcpy(hdr.mSignature, "AT8X", 4);
	hdr.mVersionMajor = 1;
	hdr.mVersionMinor = 1;
	memcpy(&hdr.mCreator, "AT", 2);
	hdr.mTrackDataOffset = 48;
	hdr.mDensity = kATXDensity_SD;

	f.write(&hdr, sizeof hdr);

	// scan all physical sectors and find highest FM and MFM vsecs
	const uint32 totalVsecs = (uint32)mVirtSectors.size();
	uint32 vsecLimitSD = 0;
	uint32 vsecLimitED = 0;
	for(uint32 i = 0; i < totalVsecs; ++i) {
		const VirtSectorInfo& vsi = mVirtSectors[i];

		for(uint32 j = 0; j < vsi.mNumPhysSectors; ++j) {
			const PhysSectorInfo& psi = phySecs[vsi.mStartPhysSector + j];

			if (psi.mbMFM)
				vsecLimitED = i + 1;
			else
				vsecLimitSD = i + 1;
		}
	}

	// compute track count
	const uint32 numTracksSD = (vsecLimitSD + 17) / 18;
	const uint32 numTracksED = (vsecLimitED + 25) / 26;
	const uint32 numTracks = std::max<uint32>(numTracksSD, numTracksED);

	// write one track at a time
	const uint32 numSectors = (uint32)mVirtSectors.size();

	vdfastvector<ATXSectorHeader> sechdrs;
	vdfastvector<ATXTrackChunkHeader> xhdrs;
	vdfastvector<PhysSectorInfo *> psecs;
	vdfastvector<int> secorder;
	for(uint32 track=0; track<numTracks; ++track) {
		sechdrs.clear();
		xhdrs.clear();
		psecs.clear();

		uint32 dataOffset = 0;

		// if there are any ED sectors, try to form an ED track first, otherwise use SD
		bool isTrackMFM = true;

		for(int type=0; type<2; ++type) {
			const uint32 vsecsPerTrack = type ? 18 : 26;

			uint32 vsecIndex = track * vsecsPerTrack;
			if (vsecIndex < numSectors) {
				uint32 vsecCount = std::min<uint32>(numSectors - vsecIndex, vsecsPerTrack);

				for(uint32 i = 0; i < vsecCount; ++i) {
					const VirtSectorInfo& vsi = mVirtSectors[vsecIndex + i];

					for(uint32 j = 0; j < vsi.mNumPhysSectors; ++j) {
						PhysSectorInfo& psi = phySecs[vsi.mStartPhysSector + j];

						if (psi.mbMFM != isTrackMFM)
							continue;

						ATXSectorHeader& sechdr = sechdrs.push_back();

						sechdr.mIndex = i + 1;
						sechdr.mFDCStatus = ~psi.mFDCStatus & 0x3F;
						sechdr.mTimingOffset = (uint32)((psi.mRotPos - floorf(psi.mRotPos)) * kBitsPerTrackFM);
						if (sechdr.mTimingOffset >= kBitsPerTrackFM)
							sechdr.mTimingOffset -= kBitsPerTrackFM;

						sechdr.mDataOffset = dataOffset;
						dataOffset += psi.mFDCStatus & 0x10 ? 128 : 0;

						if (psi.mWeakDataOffset >= 0) {
							sechdr.mFDCStatus |= 0x40;

							ATXTrackChunkHeader& xhdr = xhdrs.push_back();

							xhdr.mSize = sizeof(xhdr);
							xhdr.mType = xhdr.kTypeWeakBits;
							xhdr.mNum = (uint8)(sechdrs.size() - 1);
							xhdr.mData = (uint16)psi.mWeakDataOffset;
						}

						// if the physical sector is larger than 256 bytes, write an extended
						// sector header entry so the real size is captured
						if (psi.mPhysicalSize > 256) {
							ATXTrackChunkHeader& xhdr = xhdrs.push_back();

							xhdr.mSize = sizeof(xhdr);
							xhdr.mType = xhdr.kTypeExtSectorHeader;
							xhdr.mNum = (uint8)(sechdrs.size() - 1);
							xhdr.mData = psi.mPhysicalSize <= 512 ? 2 : 3;							
						}

						psecs.push_back(&psi);
					}
				}
			}

			if (!psecs.empty())
				break;

			isTrackMFM = false;
		}

		if (track == 0 && isTrackMFM)
			hdr.mDensity = kATXDensity_ED;

		// now that we know the physical sector count for this track,
		// adjust the data offsets to be relative to the start of the track chunk
		const uint32 psecCount = (uint32)sechdrs.size();
		const uint32 preDataSize = sizeof(ATXTrackHeader) + sizeof(ATXTrackChunkHeader) + sizeof(ATXSectorHeader)*psecCount + sizeof(ATXTrackChunkHeader);

		for(ATXSectorHeader& secHdr : sechdrs) {
			secHdr.mDataOffset += preDataSize;
		}

		// resort physical sectors by disk position
		secorder.resize(psecCount);
		for(uint32 i = 0; i < psecCount; ++i)
			secorder[i] = i;

		std::sort(secorder.begin(), secorder.end(),
			[&](int x, int y) {
				return sechdrs[x].mTimingOffset < sechdrs[y].mTimingOffset;
			}
		);

		// write track header
		const sint32 trackChunkBase = (sint32)f.tell();

		ATXTrackHeader trkhdr = {0};
		trkhdr.mSize = preDataSize + dataOffset + sizeof(ATXTrackChunkHeader) * ((uint32)xhdrs.size() + 1);
		trkhdr.mType = 0;
		trkhdr.mTrackNum = track;
		trkhdr.mNumSectors = psecCount;
		trkhdr.mDataOffset = sizeof(ATXTrackHeader);
		trkhdr.mFlags = isTrackMFM ? ATXTrackHeader::kFlag_MFM : 0;

		f.write(&trkhdr, sizeof trkhdr);

		// write sector list header
		ATXTrackChunkHeader slhdr = {0};
		slhdr.mSize = sizeof(ATXTrackChunkHeader) + sizeof(ATXSectorHeader) * psecCount;
		slhdr.mType = 1;

		f.write(&slhdr, sizeof slhdr);

		// write sector list
		for(uint32 i = 0; i < psecCount; ++i) {
			f.write(&sechdrs[secorder[i]], sizeof(ATXSectorHeader));
		}

		// write sector data header
		ATXTrackChunkHeader sdhdr = {0};
		sdhdr.mSize = sizeof(ATXTrackChunkHeader) + dataOffset;
		sdhdr.mType = ATXTrackChunkHeader::kTypeSectorData;

		f.write(&sdhdr, sizeof sdhdr);

		// write sector data
		for(uint32 i = 0; i < psecCount; ++i) {
			const ATXSectorHeader& sechdr = sechdrs[i];
			PhysSectorInfo& psi = *psecs[i];

			psi.mDiskOffset = trackChunkBase + sechdr.mDataOffset;

			if (psi.mFDCStatus & 0x10) {
				if (psi.mImageSize < 128) {
					char buf[128] = {0};

					if (psi.mImageSize)
						memcpy(buf, &mImage[psi.mOffset], psi.mImageSize);

					f.write(buf, 128);
				} else
					f.write(&mImage[psi.mOffset], 128);
			}
		}

		// write extra sector data
		if (!xhdrs.empty()) {
			// adjust indices for sorting
			for(auto& xhdr : xhdrs) {
				auto it = std::find(secorder.begin(), secorder.end(), xhdr.mNum);

				VDASSERT(it != secorder.end());
				xhdr.mNum = (uint8)(it - secorder.begin());
			}

			f.write(xhdrs.data(), (uint32)xhdrs.size() * sizeof(xhdrs[0]));
		}

		// write sentinel
		ATXTrackChunkHeader endhdr = {0};
		f.write(&endhdr, sizeof endhdr);
	}

	// backpatch size
	hdr.mTotalSize = (uint32)f.tell();

	f.seek(0);
	f.write(&hdr, sizeof hdr);
}

///////////////////////////////////////////////////////////////////////////

void ATLoadDiskImage(const wchar_t *path, IATDiskImage **ppImage) {
	vdrefptr<ATDiskImage> diskImage(new ATDiskImage);

	diskImage->Load(path);

	*ppImage = diskImage.release();
}

void ATLoadDiskImage(const wchar_t *origPath, const wchar_t *imagePath, IVDRandomAccessStream& stream, IATDiskImage **ppImage) {
	vdrefptr<ATDiskImage> diskImage(new ATDiskImage);

	diskImage->Load(origPath, imagePath, stream);

	*ppImage = diskImage.release();
}

void ATCreateDiskImage(uint32 sectorCount, uint32 bootSectorCount, uint32 sectorSize, IATDiskImage **ppImage) {
	vdrefptr<ATDiskImage> diskImage(new ATDiskImage);

	diskImage->Init(sectorCount, bootSectorCount, sectorSize);

	*ppImage = diskImage.release();
}

void ATCreateDiskImage(const ATDiskGeometryInfo& geometry, IATDiskImage **ppImage) {
	vdrefptr<ATDiskImage> diskImage(new ATDiskImage);

	diskImage->Init(geometry);

	*ppImage = diskImage.release();
}

void ATDiskConvertGeometryToPERCOM(uint8 percom[12], const ATDiskGeometryInfo& geom) {
	// track count
	percom[0] = (uint8)geom.mTrackCount;

	// step rate
	percom[1] = 0x01;

	// sectors per track
	percom[2] = (uint8)(geom.mSectorsPerTrack >> 8);
	percom[3] = (uint8)(geom.mSectorsPerTrack);

	// sides minus one
	percom[4] = geom.mSideCount - 1;

	// record method
	percom[5] = geom.mbMFM ? 4 : 0;

	// bytes per sector
	percom[6] = (uint8)(geom.mSectorSize >> 8);
	percom[7] = (uint8)geom.mSectorSize;

	// drive online
	percom[8] = 0xFF;

	// unused
	percom[9] = 0;
	percom[10] = 0;
	percom[11] = 0;
}

void ATDiskConvertPERCOMToGeometry(ATDiskGeometryInfo& geom, const uint8 percom[12]) {
	geom.mTrackCount = percom[0];
	geom.mSectorSize = VDReadUnalignedBEU16(&percom[6]);
	geom.mSectorsPerTrack = VDReadUnalignedBEU16(&percom[2]);
	geom.mSideCount = percom[4] + 1;
	geom.mbMFM = (percom[5] & 4) != 0;
	geom.mBootSectorCount = geom.mSectorSize < 512 ? 3 : 0;
	geom.mTotalSectorCount = geom.mSectorsPerTrack * geom.mSideCount * geom.mTrackCount;
}

ATDiskInterleave ATDiskGetDefaultInterleave(const ATDiskGeometryInfo& info) {
	if (info.mSectorSize >= 256)
		return kATDiskInterleave_DD_15_1;
	else if (info.mSectorsPerTrack >= 26)
		return kATDiskInterleave_ED_13_1;
	else
		return kATDiskInterleave_SD_9_1;
}

vdfunction<float(uint32)> ATDiskGetInterleaveFn(ATDiskInterleave interleave, const ATDiskGeometryInfo& info) {
	// 810/1050/XF551 sector spacing: 11.072ms
	static constexpr float kTurnsPerSectorSD = 11.072f / (60000.0f / 288.0f);

	// 1050/XF551 sector spacing: 7.680ms
	static constexpr float kTurnsPerSectorED = 7.680f / (60000.0f / 288.0f);

	// XF551 sector spacing: 10.944ms
	static constexpr float kTurnsPerSectorDD = 10.944f / (60000.0f / 288.0f);

	if (interleave == kATDiskInterleave_Default)
		interleave = ATDiskGetDefaultInterleave(info);

	switch(interleave) {
		case kATDiskInterleave_Default:
		default:
			VDFAIL("Invalid interleave passed to ATDiskGetInterleaveFn().");
		case kATDiskInterleave_1_1:
			return [spt = (uint32)info.mSectorsPerTrack, secLen = 1.0f / (float)info.mSectorsPerTrack](uint32 secIdx) { return (float)(secIdx % spt) * secLen; };

		case kATDiskInterleave_SD_12_1:
			return [](uint32 secIdx) { return (float)kTrackInterleaveSD_12_1[secIdx % 18] * kTurnsPerSectorSD; };

		case kATDiskInterleave_SD_9_1:
			return [](uint32 secIdx) { return (float)kTrackInterleaveSD_9_1[secIdx % 18] * kTurnsPerSectorSD; };

		case kATDiskInterleave_SD_9_1_REV:
			return [](uint32 secIdx) { return (float)kTrackInterleaveSD_9_1_REV[secIdx % 18] * kTurnsPerSectorSD; };

		case kATDiskInterleave_SD_5_1:
			return [](uint32 secIdx) { return (float)kTrackInterleaveSD_5_1[secIdx % 18] * kTurnsPerSectorSD; };

		case kATDiskInterleave_ED_13_1:
			return [](uint32 secIdx) { return (float)kTrackInterleaveED_13_1[secIdx % 26] * kTurnsPerSectorED; };

		case kATDiskInterleave_ED_12_1:
			return [](uint32 secIdx) { return (float)kTrackInterleaveED_12_1[secIdx % 26] * kTurnsPerSectorED; };

		case kATDiskInterleave_DD_15_1:
			return [](uint32 secIdx) { return (float)kTrackInterleaveDD_15_1[secIdx % 18] * kTurnsPerSectorDD; };

		case kATDiskInterleave_DD_9_1:
			return [](uint32 secIdx) { return (float)kTrackInterleaveDD_9_1[secIdx % 18] * kTurnsPerSectorDD; };

		case kATDiskInterleave_DD_7_1:
			return [](uint32 secIdx) { return (float)kTrackInterleaveDD_7_1[secIdx % 18] * kTurnsPerSectorDD; };
	}
}
