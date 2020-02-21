//	Altirra - Atari 800/800XL/5200 emulator
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

#ifndef f_AT_MIO_H
#define f_AT_MIO_H

#include <vd2/system/vdstl.h>
#include <vd2/system/refcount.h>
#include <at/atcore/deviceimpl.h>
#include <at/atcore/deviceparentimpl.h>
#include <at/atcore/deviceprinter.h>
#include <at/atcore/deviceserial.h>
#include <at/atcore/scheduler.h>
#include "pia.h"
#include <at/atemulation/acia.h>
#include <at/atemulation/scsi.h>

class ATMemoryLayer;
class ATIRQController;
class IATBlockDevice;

class ATMIOEmulator final : public ATDevice
	, public IATDeviceMemMap
	, public IATDeviceFirmware
	, public IATDeviceIRQSource
	, public IATDeviceScheduling
	, public IATDeviceParent
	, public ATDeviceBus
	, public IATDeviceIndicators
	, public IATDevicePrinter
	, public IATSCSIBusMonitor
	, public IATSchedulerCallback
{
public:
	ATMIOEmulator();
	~ATMIOEmulator();

	void *AsInterface(uint32 iid) override;

	void GetDeviceInfo(ATDeviceInfo& info) override;
	void GetSettings(ATPropertySet& settings) override;
	bool SetSettings(const ATPropertySet& settings) override;
	void Init() override;
	void Shutdown() override;
	void WarmReset() override;
	void ColdReset() override;

public:
	void InitMemMap(ATMemoryManager *memmap) override;
	bool GetMappedRange(uint32 index, uint32& lo, uint32& hi) const override;

public:
	void InitFirmware(ATFirmwareManager *fwman) override;
	bool ReloadFirmware() override;
	const wchar_t *GetWritableFirmwareDesc(uint32 idx) const override { return nullptr; }
	bool IsWritableFirmwareDirty(uint32 idx) const override { return false; }
	void SaveWritableFirmware(uint32 idx, IVDStream& stream) override {}
	ATDeviceFirmwareStatus GetFirmwareStatus() const override;

public:
	virtual void InitIRQSource(ATIRQController *fwirq) override;

public:
	virtual void InitScheduling(ATScheduler *sch, ATScheduler *slowsch) override;

public:
	IATDeviceBus *GetDeviceBus(uint32 index) override;

public:
	const wchar_t *GetBusName() const override;
	const char *GetBusTag() const override;
	const char *GetSupportedType(uint32 index) override;
	void GetChildDevices(vdfastvector<IATDevice *>& devs) override;
	void GetChildDevicePrefix(uint32 index, VDStringW& s) override;
	void AddChildDevice(IATDevice *dev) override;
	void RemoveChildDevice(IATDevice *dev) override;

public:
	virtual void InitIndicators(IATDeviceIndicatorManager *r) override;

public:
	virtual void SetPrinterOutput(IATPrinterOutput *out) override;

public:
	virtual void OnSCSIControlStateChanged(uint32 state) override;

public:
	virtual void OnScheduledEvent(uint32 id) override;

public:
	void OnControlStateChanged(const ATDeviceSerialStatus& status);

protected:
	static sint32 OnDebugRead(void *thisptr, uint32 addr);
	static sint32 OnRead(void *thisptr, uint32 addr);
	static bool OnWrite(void *thisptr, uint32 addr, uint8 value);

	void OnACIAControlStateChanged(bool rts, bool dtr);
	void OnACIAIRQStateChanged(bool active);
	void OnACIAReceiveReady();
	void OnACIATransmit(uint8 data, uint32 baudRate);
	void UpdateSerialControlLines();

	void SetPBIBANK(sint8 bank);
	void SetRAMPAGE(uint32 page);

	sint8	mPBIBANK;
	uint32	mRAMPAGE;
	uint8	mActiveIRQs;
	uint8	mDataIn;
	uint8	mDataOut;
	uint8	mStatus1;
	uint8	mStatus2;
	uint32	mPrevSCSIState;
	bool	mbSCSIBlockSize256;
	bool	mbPrinterIRQEnabled;
	bool	mbACIAIRQActive;
	bool	mbFirmwareUsable;

	ATFirmwareManager *mpFwMan;
	IATDeviceIndicatorManager *mpUIRenderer;

	ATIRQController *mpIRQController;
	uint32	mIRQBit;

	ATScheduler *mpScheduler;
	ATEvent *mpEventUpdateSCSIBus;
	ATMemoryManager *mpMemMan;
	ATMemoryLayer *mpMemLayerPBI;
	ATMemoryLayer *mpMemLayerRAM;
	ATMemoryLayer *mpMemLayerFirmware;

	IATPrinterOutput *mpPrinterOutput;

	struct SCSIDiskEntry {
		IATDevice *mpDevice;
		IATSCSIDevice *mpSCSIDevice;
		IATBlockDevice *mpDisk;
	};

	vdfastvector<SCSIDiskEntry> mSCSIDisks;

	IATDeviceSerial *mpSerialDevice;
	bool mbRTS;
	bool mbDTR;
	uint8 mSerialCtlInputs;
	bool mbLastPrinterStrobe;

	ATACIA6551Emulator mACIA;
	ATSCSIBusEmulator mSCSIBus;

	ATDeviceBusSingleChild mSerialBus;

	uint8 mFirmware[0x2000];
	uint8 mRAM[0x100000];
};

#endif
