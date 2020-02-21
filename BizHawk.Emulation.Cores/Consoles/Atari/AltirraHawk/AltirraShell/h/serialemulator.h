//	Altirra - Atari 800/800XL/5200 emulator
//	Native device emulator - SIO protocol emulator
//	Copyright (C) 2009-2015 Avery Lee
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

#ifndef f_ATS_SERIALEMULATOR_H
#define f_ATS_SERIALEMULATOR_H

#include <at/atcore/devicesio.h>
#include <at/atcore/scheduler.h>
#include "serialhandler.h"

class IATSSerialEngine;
class ATDeviceManager;

class ATSSerialEmulator final : public IATDeviceSIOManager, public IATSchedulerCallback, public IATSSerialHandler {
	ATSSerialEmulator(const ATSSerialEmulator&) = delete;
	ATSSerialEmulator& operator=(const ATSSerialEmulator&) = delete;
public:
	ATSSerialEmulator();
	~ATSSerialEmulator();

	void Init(ATDeviceManager& dm);

	void Attach(ATScheduler *sch);
	void Shutdown();

	void ColdReset();

public:
	void OnAttach(IATSSerialEngine& eng) override;
	void OnConfigChanged(const ATSSerialConfig&) override;
	void OnControlStateChanged(uint8 newState) override;
	void OnReadDataAvailable(uint32 len) override;
	void OnWriteSpaceAvailable(uint32 len) override;
	void OnWriteBufferEmpty() override;
	void OnReadFramingError() override;

public:
	void AddDevice(IATDeviceSIO *dev) override;
	void RemoveDevice(IATDeviceSIO *dev) override;
	void BeginCommand() override;
	void SendData(const void *data, uint32 len, bool addChecksum) override;
	void SendACK() override;
	void SendNAK() override;
	void SendComplete(bool autoDelay = true) override;
	void SendError(bool autoDelay = true) override;
	void ReceiveData(uint32 id, uint32 len, bool autoProtocol) override;
	void SetTransferRate(uint32 cyclesPerBit, uint32 cyclesPerByte) override;
	void SetSynchronousTransmit(bool) override {}
	void Delay(uint32 ticks) override;
	void InsertFence(uint32 id) override;
	void FlushQueue() override;
	void EndCommand() override;
	bool IsAccelRequest() const override { return false; }
	uint32 GetAccelTimeSkew() const override { return 0; }
	sint32 GetHighSpeedIndex() const override { return mHighSpeedBaudRate ? mHighSpeedDivisor : -1; }
	uint32 GetCyclesPerBitRecv() const override { return 0; }
	uint32 GetRecvResetCounter() const override { return 0; }

	void AddRawDevice(IATDeviceRawSIO *dev) override;
	void RemoveRawDevice(IATDeviceRawSIO *dev) override;
	void SendRawByte(uint8 byte, uint32 cyclesPerBit, bool synchronous, bool forceFramingError, bool simulateInput) override;
	void SetRawInput(bool state) override;

	bool IsSIOCommandAsserted() const override { return mbCommandState; }
	bool IsSIOMotorAsserted() const override { return mbMotorState; }

	void SetSIOInterrupt(IATDeviceRawSIO *dev, bool state) override;
	void SetSIOProceed(IATDeviceRawSIO *dev, bool state) override;

	void SetExternalClock(IATDeviceRawSIO *dev, uint32 initialOffset, uint32 period) override;

public:
	void OnScheduledEvent(uint32 id) override;

private:
	enum {
		kEventId_Delay = 1,
		kEventId_TransmitComplete
	};

	class RawDeviceListLock;
	friend class RawDeviceListLock;

	void OnReceiveByte(uint8 c, uint32 cyclesPerBit);
	void OnBeginCommand();
	void OnEndCommand();
	void OnBlownCommand();

	void ProcessCommand();

	void AbortActiveCommand();
	void ExecuteNextStep();
	void ShiftTransmitBuffer();
	void ResetTransferRate();
	void OnTransmitMore();
	void OnTransmitComplete();
	void OnMotorStateChanged(bool asserted);

	void OnInitingDevice(IATDevice& dev);

	IATSSerialEngine *mpSerEngine = nullptr;
	ATScheduler *mpScheduler = nullptr;

	uint32	mTransferLevel = 0;		// Write pointer for accumulating send data.
	uint32	mTransferStart = 0;		// Starting offset for current transfer.
	uint32	mTransferIndex = 0;		// Next byte to send/receive for current transfer.
	uint32	mTransferEnd = 0;		// Stopping offset for current transfer.
	uint32	mTransferCyclesPerBit = 0;
	uint32	mTransferCyclesPerBitRecvMin = 0;
	uint32	mTransferCyclesPerBitRecvMax = 0;
	uint32	mTransferCyclesPerByte = 0;
	bool	mbTransferSend = false;
	bool	mbTransferError = false;
	bool	mbCommandState = false;
	bool	mbCommandPending = false;
	bool	mbCommandSuccessful = false;
	bool	mbCommandBlown = false;
	int		mCommandRetryCount = 0;
	bool	mbMotorState = false;
	bool	mbHighSpeedMode = false;
	uint32	mHighSpeedBaudRate = 0;
	uint32	mHighSpeedCyclesPerBit = 0;
	uint8	mHighSpeedDivisor = 0;
	uint8	mPollCount = 0;
	uint32	mCommandDeassertTime = 0;
	ATEvent *mpTransferEvent = nullptr;
	IATDeviceSIO *mpActiveDevice = nullptr;

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
		kStepType_Fence,
		kStepType_EndCommand,
		kStepType_AccelSendNAK,
		kStepType_AccelSendError,
	};

	struct Step {
		StepType mType;
		union {
			uint32 mTransferLength;
			uint32 mFenceId;
			uint32 mDelayTicks;
			uint32 mTransferCyclesPerBit;
		};

		union {
			uint32 mTransferCyclesPerByte;
			uint32 mTransferId;
		};
	};

	Step mCurrentStep;

	vdfastdeque<Step> mStepQueue;

	uint8 mTransferBuffer[65536];
};

#endif
