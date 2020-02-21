//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2012 Avery Lee
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

#ifndef f_AT_SIOMANAGER_H
#define f_AT_SIOMANAGER_H

#include <at/atcore/devicesio.h>
#include <at/atcore/scheduler.h>
#include "pokey.h"

class ATCPUEmulator;
class ATCPUEmulatorMemory;
class ATSimulator;
class IATUIRenderer;
struct ATCPUHookNode;
class ATPokeyEmulator;
class ATPIAEmulator;

struct ATTraceContext;
class ATTraceChannelFormatted;

struct ATSIORequest {
	uint8	mDevice;
	uint8	mCommand;
	uint8	mMode;
	uint8	mTimeout;
	uint16	mAddress;
	uint16	mLength;
	uint16	mSector;
	uint8	mAUX[2];
};

class ATSIOManager final : public IATPokeySIODevice, public IATDeviceSIOManager, public IATSchedulerCallback {
	ATSIOManager(const ATSIOManager&) = delete;
	ATSIOManager& operator=(const ATSIOManager&) = delete;
public:
	ATSIOManager();
	~ATSIOManager();

	bool IsSIOPatchEnabled() const { return mbSIOPatchEnabled; }
	void SetSIOPatchEnabled(bool enable);

	bool GetDiskSIOAccelEnabled() const { return mbDiskSIOAccelEnabled; }
	void SetDiskSIOAccelEnabled(bool enabled);

	bool GetOtherSIOAccelEnabled() const { return mbOtherSIOAccelEnabled; }
	void SetOtherSIOAccelEnabled(bool enabled);

	bool GetDiskBurstTransfersEnabled() const { return mbDiskBurstTransfersEnabled; }
	void SetDiskBurstTransfersEnabled(bool enabled) { mbDiskBurstTransfersEnabled = enabled; }

	bool GetBurstTransfersEnabled() const { return mbBurstTransfersEnabled; }
	void SetBurstTransfersEnabled(bool enabled) { mbBurstTransfersEnabled = enabled; }

	void Init(ATCPUEmulator *cpu, ATSimulator *sim);
	void Shutdown();

	void ColdReset();
	void WarmReset();

	void ReinitHooks();
	void UninitHooks();

	void SetTraceContext(ATTraceContext *context);

	void TryAccelPBIRequest();

	bool TryAccelRequest(const ATSIORequest& req);

public:
	virtual void PokeyAttachDevice(ATPokeyEmulator *pokey) override;
	virtual bool PokeyWriteSIO(uint8 c, bool command, uint32 cyclesPerBit, uint64 startTime, bool framingError) override;
	virtual void PokeyBeginCommand() override;
	virtual void PokeyEndCommand() override;
	virtual void PokeySerInReady() override;

public:
	virtual void AddDevice(IATDeviceSIO *dev) override;
	virtual void RemoveDevice(IATDeviceSIO *dev) override;
	virtual void BeginCommand() override;
	virtual void SendData(const void *data, uint32 len, bool addChecksum) override;
	virtual void SendACK() override;
	virtual void SendNAK() override;
	virtual void SendComplete(bool autoDelay = true) override;
	virtual void SendError(bool autoDelay = true) override;
	virtual void ReceiveData(uint32 id, uint32 len, bool autoProtocol) override;
	virtual void SetTransferRate(uint32 cyclesPerBit, uint32 cyclesPerByte) override;
	virtual void SetSynchronousTransmit(bool enable) override;
	virtual void Delay(uint32 ticks) override;
	virtual void InsertFence(uint32 id) override;
	virtual void FlushQueue() override;
	virtual void EndCommand() override;
	virtual bool IsAccelRequest() const override { return mpAccelRequest != nullptr; }
	virtual uint32 GetAccelTimeSkew() const override { return mAccelTimeSkew; }
	virtual sint32 GetHighSpeedIndex() const override { return 10; }
	virtual uint32 GetCyclesPerBitRecv() const override;
	virtual uint32 GetRecvResetCounter() const override;

	virtual void AddRawDevice(IATDeviceRawSIO *dev) override;
	virtual void RemoveRawDevice(IATDeviceRawSIO *dev) override;
	virtual void SendRawByte(uint8 byte, uint32 cyclesPerBit, bool synchronous, bool forceFramingError, bool simulateInput) override;
	virtual void SetRawInput(bool input) override;

	bool IsSIOCommandAsserted() const override;
	bool IsSIOMotorAsserted() const override;

	virtual void SetSIOInterrupt(IATDeviceRawSIO *dev, bool state) override;
	virtual void SetSIOProceed(IATDeviceRawSIO *dev, bool state) override;

	virtual void SetExternalClock(IATDeviceRawSIO *dev, uint32 initialOffset, uint32 period) override;

public:
	virtual void OnScheduledEvent(uint32 id) override;

private:
	enum {
		kEventId_Delay = 1,
		kEventId_Send
	};

	class RawDeviceListLock;
	friend class RawDeviceListLock;

	uint8 OnHookDSKINV(uint16);
	uint8 OnHookSIOV(uint16);

	void AbortActiveCommand();
	void ExecuteNextStep();
	void ShiftTransmitBuffer();
	void ResetTransfer();
	void OnMotorStateChanged(bool asserted);
	void TraceReceive(uint8 c, uint32 cyclesPerBit);
	void UpdatePollState(uint8 cmd, uint8 aux1, uint8 aux2);

	ATCPUEmulator *mpCPU = nullptr;
	ATCPUEmulatorMemory *mpMemory = nullptr;
	ATSimulator *mpSim = nullptr;
	IATUIRenderer *mpUIRenderer = nullptr;
	ATScheduler *mpScheduler = nullptr;
	ATPokeyEmulator *mpPokey = nullptr;
	ATPIAEmulator *mpPIA = nullptr;
	int mPIAOutput = 0;

	ATCPUHookNode *mpSIOVHook = nullptr;
	ATCPUHookNode *mpDSKINVHook = nullptr;

	uint32	mTransferLevel = 0;		// Write pointer for accumulating send data.
	uint32	mTransferStart = 0;		// Starting offset for current transfer.
	uint32	mTransferIndex = 0;		// Next byte to send/receive for current transfer.
	uint32	mTransferEnd = 0;		// Stopping offset for current transfer.
	uint32	mTransferBurstOffset = 0;
	uint32	mTransferCyclesPerBit = 0;
	uint32	mTransferCyclesPerBitRecvMin = 0;
	uint32	mTransferCyclesPerBitRecvMax = 0;
	uint32	mTransferCyclesPerByte = 0;
	bool	mbTransferSend = false;
	bool	mbTransferError = false;
	bool	mbTransmitSynchronous = false;
	bool	mbCommandState = false;
	bool	mbMotorState = false;
	bool	mbSIOPatchEnabled = false;
	bool	mbOtherSIOAccelEnabled = false;
	bool	mbDiskSIOAccelEnabled = false;
	bool	mbBurstTransfersEnabled = false;
	bool	mbDiskBurstTransfersEnabled = false;
	uint8	mPollCount = 0;
	uint32	mAccelTimeSkew = 0;
	uint32	mAccelBufferAddress = 0;
	const ATDeviceSIORequest *mpAccelRequest = nullptr;
	uint8	*mpAccelStatus = nullptr;
	ATEvent *mpDelayEvent = nullptr;
	ATEvent *mpTransferEvent = nullptr;
	IATDeviceSIO *mpActiveDevice = nullptr;
	bool	mbActiveDeviceDisk = false;

	uint32	mAccessedDisks = 0;

	vdfastvector<IATDeviceSIO *> mSIODevices;
	vdfastvector<IATDeviceRawSIO *> mSIORawDevices;
	vdfastvector<IATDeviceRawSIO *> mSIORawDevicesNew;
	sint32 mSIORawDevicesBusy = 0;

	vdfastvector<IATDeviceRawSIO *> mSIOInterruptActive;
	vdfastvector<IATDeviceRawSIO *> mSIOProceedActive;

	struct ExternalClock {
		IATDeviceRawSIO *mpDevice;
		uint32 mTimeBase;
		uint32 mPeriod;
	};

	vdfastvector<ExternalClock> mExternalClocks;

	enum StepType {
		kStepType_None,
		kStepType_Delay,
		kStepType_Send,
		kStepType_SendAutoProtocol,
		kStepType_Receive,
		kStepType_ReceiveAutoProtocol,
		kStepType_SetTransferRate,
		kStepType_SetSynchronousTransmit,
		kStepType_Fence,
		kStepType_EndCommand,
		kStepType_AccelSendACK,
		kStepType_AccelSendNAK,
		kStepType_AccelSendComplete,
		kStepType_AccelSendError,
	};

	struct Step {
		StepType mType;
		union {
			uint32 mTransferLength;
			uint32 mFenceId;
			uint32 mDelayTicks;
			uint32 mTransferCyclesPerBit;
			bool mbEnable;
		};

		union {
			uint32 mTransferCyclesPerByte;
			uint32 mTransferId;
		};
	};

	Step mCurrentStep = {};

	vdfastdeque<Step> mStepQueue;

	ATTraceContext *mpTraceContext = nullptr;
	ATTraceChannelFormatted *mpTraceChannelBusSend = nullptr;
	ATTraceChannelFormatted *mpTraceChannelBusReceive = nullptr;
	uint64 mTraceCommandStartTime = 0;

	uint8 mTransferBuffer[65536] = {};
};

#endif	// f_AT_SIOMANAGER_H
