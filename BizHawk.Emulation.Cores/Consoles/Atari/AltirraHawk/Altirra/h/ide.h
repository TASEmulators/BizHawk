//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2011 Avery Lee
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

#ifndef f_AT_IDE_H
#define f_AT_IDE_H

#include <vd2/system/vdstl.h>
#include <vd2/system/VDString.h>
#include <vd2/system/time.h>

class ATScheduler;
class IATDeviceIndicatorManager;
class ATIDEPhysicalDisk;
class IATBlockDevice;

class ATIDEEmulator : protected IVDTimerCallback {
	ATIDEEmulator(const ATIDEEmulator&);
	ATIDEEmulator& operator=(const ATIDEEmulator&);
public:
	enum : uint32 { kTypeID = 'ata ' };

	ATIDEEmulator();
	~ATIDEEmulator();

	void Init(ATScheduler *mpScheduler, IATDeviceIndicatorManager *uirenderer, bool isSingle = true, bool isSlave = false);
	
	// Set whether a device is the lone device on the bus, particularly a master with no
	// slave. This is needed so that the master can respond appropriately to commands to
	// the slave -- for the most part mirroring the master registers except for status
	// and alternate status returning $00. For this to work properly the master emulator
	// must handle read requests when the slave is selected but not absent.
	void SetIsSingle(bool single);

	void Shutdown();

	void OpenImage(IATBlockDevice *dev);
	void CloseImage();

	bool IsWriteEnabled() const { return mbWriteEnabled; }
	bool IsFastDevice() const { return mbFastDevice; }
	uint32 GetImageSizeMB() const { return mSectorCount >> 11; }
	uint32 GetCylinderCount() const { return mCylinderCount; }
	uint32 GetHeadCount() const { return mHeadCount; }
	uint32 GetSectorsPerTrack() const { return mSectorsPerTrack; }

	bool GetReset() const { return mbHardwareReset; }
	void SetReset(bool asserted);

	void ColdReset();

	void DumpStatus() const;

	uint32 ReadDataLatch(bool advance);
	void  WriteDataLatch(uint8 lo, uint8 hi);

	uint8 DebugReadByte(uint8 address);
	uint8 ReadByte(uint8 address);
	void WriteByte(uint8 address, uint8 value);

	// alternate read/write registers
	uint8 ReadByteAlt(uint8 address);
	void WriteByteAlt(uint8 address, uint8 value);

	void DebugReadSector(uint32 lba, void *dst, uint32 len);
	void DebugWriteSector(uint32 lba, const void *dst, uint32 len);

protected:
	void TimerCallback();

protected:
	struct DecodedCHS;

	void ResetDevice();
	void UpdateStatus();
	void StartCommand(uint8 cmd);
	void BeginReadTransfer(uint32 bytes);
	void BeginWriteTransfer(uint32 bytes);
	void CompleteCommand();
	void AbortCommand(uint8 cmd);
	bool ReadLBA(uint32& lba);
	void WriteLBA(uint32 lba);
	void ResetCHSTranslation();
	void AdjustCHSTranslation();
	DecodedCHS DecodeCHS(uint32 lba);
	void ScheduleLazyFlush();

	union {
		uint8	mRegisters[8];
		struct {
			uint8 mData;
			uint8 mErrors;
			uint8 mSectorCount;
			uint8 mSectorNumber;
			uint8 mCylinderLow;
			uint8 mCylinderHigh;
			uint8 mHead;
			uint8 mStatus;
		} mRFile;
	};

	uint8 mFeatures;

	ATScheduler *mpScheduler;
	IATDeviceIndicatorManager *mpUIRenderer;

	uint32 mMaxSectorTransferCount;
	uint32 mSectorCount;
	uint32 mSectorsPerTrack;
	uint32 mHeadCount;
	uint32 mCylinderCount;
	uint32 mCurrentSectorsPerTrack;
	uint32 mCurrentHeadCount;
	uint32 mCurrentCylinderCount;
	uint32 mSectorsPerBlock;
	uint32 mIODelaySetting;
	uint32 mTransferIndex;
	uint32 mTransferLength;
	uint32 mTransferSectorCount;
	uint32 mTransferLBA;
	uint32 mActiveCommandNextTime;
	uint8 mActiveCommand;
	uint8 mActiveCommandState;
	bool mbTransferAsWrites;
	bool mbTransfer16Bit;
	bool mbWriteEnabled;
	bool mbWriteInProgress;
	bool mbIsSingle;
	bool mbIsSlave;
	bool mbFastDevice;
	bool mbHardwareReset;
	bool mbSoftwareReset;

	vdfastvector<uint8> mTransferBuffer;

	IATBlockDevice *mpDisk;

	VDLazyTimer mFlushTimer;
};

#endif
