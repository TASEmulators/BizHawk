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

#include <stdafx.h>
#include <vd2/system/math.h>
#include <vd2/system/time.h>
#include <at/atcore/logging.h>
#include <at/atcore/sioutils.h>
#include <at/atcore/device.h>
#include <at/atcore/devicemanager.h>
#include "serialemulator.h"
#include "serialengine.h"

ATLogChannel g_ATLCHookSIOReqs(false, false, "HOOKSIOREQS", "OS SIO hook requests");
ATLogChannel g_ATLCHookSIO(false, false, "HOOKSIO", "OS SIO hook messages");
ATLogChannel g_ATLCSIOData(true, false, "SIODATA", "SIO raw data");
ATLogChannel g_ATLCSIOCmd(false, false, "SIOCMD", "SIO bus commands");
ATLogChannel g_ATLCSIOAccel(false, false, "SIOACCEL", "SIO command acceleration");
ATLogChannel g_ATLCSIOSteps(true, false, "SIOSTEPS", "SIO command steps");

namespace {
	const uint32 kCommandLatencyLimit = 10000;
}

class ATSSerialEmulator::RawDeviceListLock {
public:
	RawDeviceListLock(ATSSerialEmulator *parent);
	~RawDeviceListLock();

protected:
	ATSSerialEmulator *const mpParent;
};

ATSSerialEmulator::RawDeviceListLock::RawDeviceListLock(ATSSerialEmulator *parent)
	: mpParent(parent)
{
	parent->mSIORawDevicesBusy += 2;
}

ATSSerialEmulator::RawDeviceListLock::~RawDeviceListLock() {
	mpParent->mSIORawDevicesBusy -= 2;

	if (mpParent->mSIORawDevicesBusy < 0) {
		mpParent->mSIORawDevicesBusy = 0;

		// remove dead devices
		mpParent->mSIORawDevices.erase(
			std::remove_if(mpParent->mSIORawDevices.begin(), mpParent->mSIORawDevices.end(),
				[](IATDeviceRawSIO *p) { return p == nullptr; }
			)
		);

		// add new devices
		mpParent->mSIORawDevices.insert(mpParent->mSIORawDevices.end(), mpParent->mSIORawDevicesNew.begin(), mpParent->mSIORawDevicesNew.end());
		mpParent->mSIORawDevicesNew.clear();
	}
}

///////////////////////////////////////////////////////////////////////////

ATSSerialEmulator::ATSSerialEmulator() {
	mCurrentStep.mType = kStepType_None;

	mHighSpeedBaudRate = 0;
	mHighSpeedDivisor = 0;
	mHighSpeedCyclesPerBit = 0;
}

ATSSerialEmulator::~ATSSerialEmulator() {
}

void ATSSerialEmulator::Init(ATDeviceManager& dm) {
	dm.AddInitCallback([this](IATDevice& dev) { OnInitingDevice(dev); });
}

void ATSSerialEmulator::Shutdown() {
	if (mpScheduler) {
		mpScheduler->UnsetEvent(mpTransferEvent);
		mpScheduler = nullptr;
	}
}

void ATSSerialEmulator::ColdReset() {
	AbortActiveCommand();

	mTransferLevel = 0;
	mTransferStart = 0;
	mTransferIndex = 0;
	mTransferEnd = 0;
	mbTransferSend = false;
	mbCommandState = false;
	mbCommandPending = false;
	mbMotorState = false;
	mpActiveDevice = nullptr;
}

void ATSSerialEmulator::OnAttach(IATSSerialEngine& eng) {
	mpSerEngine = &eng;

	mpScheduler = &eng.GetScheduler();

	OnConfigChanged(eng.GetActiveConfig());
}

void ATSSerialEmulator::OnConfigChanged(const ATSSerialConfig& cfg) {
	const double kCyclesPerSecond = 7159090.0 / 4.0;

	switch(cfg.mHighSpeedMode) {
		case ATSSerialConfig::kHighSpeed_Disabled:
			mbHighSpeedMode = false;
			mHighSpeedBaudRate = 0;
			mHighSpeedCyclesPerBit = 0;
			mHighSpeedDivisor = 0;
			mpSerEngine->SetBaudRate(19200);
			break;

		case ATSSerialConfig::kHighSpeed_Standard:
			mHighSpeedBaudRate = cfg.mHSBaudRate;
			mHighSpeedCyclesPerBit = VDRoundToInt(kCyclesPerSecond / (double)cfg.mHSBaudRate);
			mHighSpeedDivisor = std::max<int>(0, (int)(kCyclesPerSecond / (double)cfg.mHSBaudRate / 2.0 - 7.0));
			break;

		case ATSSerialConfig::kHighSpeed_PokeyDivisor:
			mHighSpeedBaudRate = VDRoundToInt((kCyclesPerSecond / 2.0) / (double)(cfg.mHSPokeyDivisor + 7));
			mHighSpeedCyclesPerBit = cfg.mHSPokeyDivisor*2 + 14;
			mHighSpeedDivisor = cfg.mHSPokeyDivisor;
			break;
	}
}

void ATSSerialEmulator::OnControlStateChanged(uint8 newState) {
	if (newState & kATSerialCtlState_Command)
		OnBeginCommand();
	else
		OnEndCommand();
}

void ATSSerialEmulator::OnReadDataAvailable(uint32 len) {
	uint32 avail = 0;
	const void *src = mpSerEngine->LockRead(avail);
	if (src) {
		for(uint32 i=0; i<avail; ++i)
			OnReceiveByte(((const uint8 *)src)[i], mbHighSpeedMode ? mHighSpeedCyclesPerBit : 93);

		mpSerEngine->UnlockRead(avail);
	}
}

void ATSSerialEmulator::OnWriteSpaceAvailable(uint32 len) {
	OnTransmitMore();
}

void ATSSerialEmulator::OnWriteBufferEmpty() {
	// Win32 serial drivers have a bad habit of sending this after all data
	// has been sent without waiting for it all to actually transmit, so we ignore this.
	//OnTransmitComplete();
}

void ATSSerialEmulator::OnReadFramingError() {
	g_ATLCSIOData("[%u] Framing error detected\n", VDGetAccurateTick());

	if (mbCommandState || mbCommandPending) {
		if (!mbCommandBlown) {
			mbCommandBlown = true;

			OnBlownCommand();
		}
	}
}

void ATSSerialEmulator::OnReceiveByte(uint8 c, uint32 cyclesPerBit) {
	g_ATLCSIOData("[%u] Receive: %02X\n", VDGetAccurateTick(), c);

	if (mTransferIndex < mTransferEnd && !mbTransferSend) {
		mTransferBuffer[mTransferIndex++] = c;

		if (mbCommandState || mbCommandPending)
			mTransferCyclesPerBit = cyclesPerBit;
		else if (cyclesPerBit < mTransferCyclesPerBitRecvMin || cyclesPerBit > mTransferCyclesPerBitRecvMax)
			mbTransferError = true;

		if (mTransferIndex >= mTransferEnd) {
			g_ATLCSIOData("[%u] Transfer complete; cp=%d, delay=%d\n", VDGetAccurateTick()
				, mbCommandPending, ATSCHEDULER_GETTIME(mpScheduler) - mCommandDeassertTime);
			if (mbCommandPending && ATSCHEDULER_GETTIME(mpScheduler) - mCommandDeassertTime < kCommandLatencyLimit) {
			g_ATLCSIOData("[%u] Possible EOC\n", VDGetAccurateTick());
				ProcessCommand();
			} else if (mpActiveDevice) {
				const uint32 transferLen = mTransferEnd - mTransferStart;
				const uint8 *data = mTransferBuffer + mTransferStart;
				const bool checksumOK = !mbTransferError && (!transferLen || ATComputeSIOChecksum(data, transferLen - 1) == data[transferLen - 1]);

				if (mCurrentStep.mType == kStepType_ReceiveAutoProtocol) {
					if (!checksumOK) {
						mStepQueue.clear();
						// SIO protocol requires 850us minimum delay.
						Delay(1530);
						SendNAK();
						ExecuteNextStep();
						return;
					}

					mpActiveDevice->OnSerialReceiveComplete(mCurrentStep.mTransferId, data, transferLen - 1, true);
				} else {
					mpActiveDevice->OnSerialReceiveComplete(mCurrentStep.mTransferId, data, transferLen, checksumOK);
				}

				mCurrentStep.mType = kStepType_None;
				ExecuteNextStep();
			}

			mbCommandPending = false;
		}
	}

	{
		RawDeviceListLock lock(this);

		for(auto *rawdev : mSIORawDevices) {
			if (rawdev)
				rawdev->OnReceiveByte(c, mbCommandState, cyclesPerBit);
		}
	}
}

void ATSSerialEmulator::OnBeginCommand() {
	AbortActiveCommand();

	mStepQueue.clear();
	mCurrentStep.mType = kStepType_None;

	mbTransferSend = false;
	mTransferIndex = 0;
	mTransferLevel = 0;
	mTransferStart = 0;
	mTransferEnd = 5;

	if (mbCommandPending) {
		mbCommandPending = false;

		if (!mbCommandBlown && !mbCommandSuccessful) {
			mbCommandBlown = true;
			OnBlownCommand();
		}
	}

	mbCommandSuccessful = false;
	mbCommandBlown = false;

	if (!mbCommandState) {
		mbCommandState = true;

		g_ATLCSIOData("[%u] +COMMAND\n", VDGetAccurateTick());

		RawDeviceListLock lock(this);

		for(auto *rawdev : mSIORawDevices) {
			if (rawdev)
				rawdev->OnCommandStateChanged(mbCommandState);
		}
	}
}

void ATSSerialEmulator::OnEndCommand() {
	if (!mbCommandState)
		return;
	
	mbCommandState = false;
	mbCommandPending = true;
	mCommandDeassertTime = ATSCHEDULER_GETTIME(mpScheduler);

	g_ATLCSIOData("[%u] -COMMAND\n", VDGetAccurateTick());

	if (mTransferIndex >= mTransferEnd)
		ProcessCommand();
	else if (mTransferIndex != mTransferStart)
		OnBlownCommand();
}

void ATSSerialEmulator::OnBlownCommand() {
	if (!mbCommandBlown)
		return;

	mbCommandBlown = false;
	++mCommandRetryCount;

	if (mCommandRetryCount >= 2) {
		mCommandRetryCount = 0;

		if (mHighSpeedBaudRate) {
			mbHighSpeedMode = !mbHighSpeedMode;

			mpSerEngine->SetBaudRate(mbHighSpeedMode ? mHighSpeedBaudRate : 19200);
		}
	}
}

void ATSSerialEmulator::ProcessCommand() {
	mbCommandPending = false;

	if (mTransferIndex >= mTransferEnd && !mbCommandBlown) {
		if (ATComputeSIOChecksum(mTransferBuffer, 4) == mTransferBuffer[4]) {
			mbCommandBlown = false;

			ATDeviceSIOCommand cmd = {};
			cmd.mDevice = mTransferBuffer[0];
			cmd.mCommand = mTransferBuffer[1];
			cmd.mAUX[0] = mTransferBuffer[2];
			cmd.mAUX[1] = mTransferBuffer[3];
			cmd.mCyclesPerBit = mTransferCyclesPerBit;
			cmd.mbStandardRate = (mTransferCyclesPerBit >= 91 && mTransferCyclesPerBit <= 98);
			cmd.mPollCount = mPollCount;

			// Check if this is a type 3 poll command -- we provide assistance for these.
			// Note that we've already recorded the poll count above.
			if (cmd.mCommand == 0x40 && cmd.mAUX[0] == cmd.mAUX[1]) {
				if (cmd.mAUX[0] == 0x00)		// Type 3 poll (??/40/00/00) - increment counter
					++mPollCount;
				else {
					// Any command that is not a type 3 poll command resets the counter. This
					// This includes the null poll (??/40/4E/4E) and poll reset (??/40/4F/4F).
					mPollCount = 0;
				}
			} else {
				// Any command to any dveice that is not a type 3 poll command resets
				// the counter.
				mPollCount = 0;
			}

			mTransferStart = 0;
			mTransferIndex = 0;
			mTransferEnd = 0;
			mTransferLevel = 0;

			if (g_ATLCSIOCmd.IsEnabled())
				g_ATLCSIOCmd("Device %02X | Command %02X | %02X %02X (%s)\n", cmd.mDevice, cmd.mCommand, cmd.mAUX[0], cmd.mAUX[1], ATDecodeSIOCommand(cmd.mDevice, cmd.mCommand, cmd.mAUX));

			ResetTransferRate();

			for(IATDeviceSIO *dev : mSIODevices) {
				mpActiveDevice = dev;

				const IATDeviceSIO::CmdResponse response = dev->OnSerialBeginCommand(cmd);
				if (response) {
					switch(response) {
						case IATDeviceSIO::kCmdResponse_Start:
							break;

						case IATDeviceSIO::kCmdResponse_Send_ACK_Complete:
							BeginCommand();
							SendACK();
							SendComplete();
							EndCommand();
							break;

						case IATDeviceSIO::kCmdResponse_Fail_NAK:
							BeginCommand();
							SendNAK();
							EndCommand();
							break;
					}
					break;
				}

				mpActiveDevice = nullptr;
			}

			mbCommandSuccessful = true;
			mbCommandBlown = false;
			mCommandRetryCount = 0;
		} else {
			if (!mbCommandBlown) {
				mbCommandBlown = true;

				OnBlownCommand();
			}
		}
	}

	{
		RawDeviceListLock lock(this);

		for(auto *rawdev : mSIORawDevices) {
			if (rawdev)
				rawdev->OnCommandStateChanged(mbCommandState);
		}
	}
}

void ATSSerialEmulator::AddDevice(IATDeviceSIO *dev) {
	mSIODevices.push_back(dev);
}

void ATSSerialEmulator::RemoveDevice(IATDeviceSIO *dev) {
	auto it = std::find(mSIODevices.begin(), mSIODevices.end(), dev);

	if (it != mSIODevices.end())
		mSIODevices.erase(it);

	if (mpActiveDevice == dev) {
		AbortActiveCommand();
	}
}

void ATSSerialEmulator::BeginCommand() {
}

void ATSSerialEmulator::SendData(const void *data, uint32 len, bool addChecksum) {
	if (!len)
		return;

	uint32 spaceRequired = len;

	if (addChecksum)
		++spaceRequired;

	if (vdcountof(mTransferBuffer) - mTransferLevel < spaceRequired) {
		// The transmit buffer is full -- let's see if we can shift it down.
		ShiftTransmitBuffer();

		// check again
		if (vdcountof(mTransferBuffer) - mTransferLevel < spaceRequired) {
			VDASSERT(!"No room left in transfer buffer.");
			return;
		}
	}

	Step& step = mStepQueue.push_back();
	step.mType = addChecksum ? kStepType_SendAutoProtocol : kStepType_Send;
	step.mTransferLength = spaceRequired;
	
	memcpy(mTransferBuffer + mTransferLevel, data, len);

	if (addChecksum)
		mTransferBuffer[mTransferLevel + len] = ATComputeSIOChecksum(mTransferBuffer + mTransferLevel, len);

	mTransferLevel += spaceRequired;

	ExecuteNextStep();
}

void ATSSerialEmulator::SendACK() {
	SendData("A", 1, false);
}

void ATSSerialEmulator::SendNAK() {
	SendData("N", 1, false);
}

void ATSSerialEmulator::SendComplete(bool autoDelay) {
	// SIO protocol requires minimum 250us delay here.
	if (autoDelay)
		Delay(450);

	SendData("C", 1, false);
}

void ATSSerialEmulator::SendError(bool autoDelay) {
	// SIO protocol requires minimum 250us delay here.
	if (autoDelay)
		Delay(450);

	SendData("E", 1, false);
}

void ATSSerialEmulator::ReceiveData(uint32 id, uint32 len, bool autoProtocol) {
	if (autoProtocol)
		++len;

	if (vdcountof(mTransferBuffer) - mTransferLevel < len) {
		// The transmit buffer is full -- let's see if we can shift it down.
		ShiftTransmitBuffer();

		// check again
		if (vdcountof(mTransferBuffer) - mTransferLevel < len) {
			VDASSERT(!"No room left in transfer buffer.");
			return;
		}
	}

	mTransferLevel += len;

	Step& step = mStepQueue.push_back();
	step.mType = autoProtocol ? kStepType_ReceiveAutoProtocol : kStepType_Receive;
	step.mTransferLength = len;
	step.mTransferId = id;

	if (autoProtocol) {
		// SIO protocol requires 850us minimum delay.
		Delay(1530);
		SendACK();
	}

	ExecuteNextStep();
}

void ATSSerialEmulator::SetTransferRate(uint32 cyclesPerBit, uint32 cyclesPerByte) {
	Step& step = mStepQueue.push_back();
	step.mType = kStepType_SetTransferRate;
	step.mTransferCyclesPerBit = cyclesPerBit;
	step.mTransferCyclesPerByte = cyclesPerByte;

	ExecuteNextStep();
}

void ATSSerialEmulator::Delay(uint32 ticks) {
	if (!ticks)
		return;

	Step& step = mStepQueue.push_back();
	step.mType = kStepType_Delay;
	step.mDelayTicks = ticks;

	ExecuteNextStep();
}

void ATSSerialEmulator::InsertFence(uint32 id) {
	Step& step = mStepQueue.push_back();
	step.mType = kStepType_Fence;
	step.mFenceId = id;
}

void ATSSerialEmulator::FlushQueue() {
	mStepQueue.clear();
}

void ATSSerialEmulator::EndCommand() {
	Step& step = mStepQueue.push_back();
	step.mType = kStepType_EndCommand;
}

void ATSSerialEmulator::AddRawDevice(IATDeviceRawSIO *dev) {
	if (std::find(mSIORawDevices.begin(), mSIORawDevices.end(), dev) != mSIORawDevices.end())
		return;

	if (std::find(mSIORawDevicesNew.begin(), mSIORawDevicesNew.end(), dev) != mSIORawDevicesNew.end())
		return;

	if (mSIORawDevicesBusy)
		mSIORawDevicesNew.push_back(dev);
	else
		mSIORawDevices.push_back(dev);
}

void ATSSerialEmulator::RemoveRawDevice(IATDeviceRawSIO *dev) {
	SetSIOInterrupt(dev, false);
	SetSIOProceed(dev, false);

	SetExternalClock(dev, 0, 0);

	auto it = std::find(mSIORawDevicesNew.begin(), mSIORawDevicesNew.end(), dev);
	if (it != mSIORawDevicesNew.end()) {
		mSIORawDevicesNew.erase(it);
		return;
	}

	auto it2 = std::find(mSIORawDevices.begin(), mSIORawDevices.end(), dev);
	if (it2 != mSIORawDevices.end()) {
		if (mSIORawDevicesBusy) {
			if (!(mSIORawDevicesBusy & 1))
				--mSIORawDevicesBusy;

			*it2 = nullptr;
		} else
			mSIORawDevices.erase(it2);
	}
}

void ATSSerialEmulator::SendRawByte(uint8 byte, uint32 cyclesPerBit, bool synchronous, bool forceFramingError, bool simulateInput) {
	void *dst = mpSerEngine->LockWrite(1);

	if (dst) {
		*(uint8 *)dst = byte;
		mpSerEngine->UnlockWrite(true);
	}
}

void ATSSerialEmulator::SetRawInput(bool state) {
}

void ATSSerialEmulator::SetSIOInterrupt(IATDeviceRawSIO *dev, bool state) {
	auto it = std::lower_bound(mSIOInterruptActive.begin(), mSIOInterruptActive.end(), dev);

	if (state) {
		if (it == mSIOInterruptActive.end() || *it != dev) {
			if (mSIOInterruptActive.empty())
				mpSerEngine->SetSIOInterrupt(true);

			mSIOInterruptActive.insert(it, dev);
		}
	} else {
		if (it != mSIOInterruptActive.end()) {
			mSIOInterruptActive.erase(it);

			if (mSIOInterruptActive.empty())
				mpSerEngine->SetSIOInterrupt(false);
		}
	}
}

void ATSSerialEmulator::SetSIOProceed(IATDeviceRawSIO *dev, bool state) {
	auto it = std::lower_bound(mSIOProceedActive.begin(), mSIOProceedActive.end(), dev);

	if (state) {
		if (it == mSIOProceedActive.end() || *it != dev) {
			if (mSIOProceedActive.empty())
				mpSerEngine->SetSIOProceed(true);

			mSIOProceedActive.insert(it, dev);
		}
	} else {
		if (it != mSIOProceedActive.end()) {
			mSIOProceedActive.erase(it);

			if (mSIOProceedActive.empty())
				mpSerEngine->SetSIOProceed(false);
		}
	}
}

void ATSSerialEmulator::SetExternalClock(IATDeviceRawSIO *dev, uint32 initialOffset, uint32 period) {
	bool updatePOKEY = false;

	VDASSERT(!period || std::find(mSIORawDevices.begin(), mSIORawDevices.end(), dev) != mSIORawDevices.end()
		|| std::find(mSIORawDevicesNew.begin(), mSIORawDevicesNew.end(), dev) != mSIORawDevicesNew.end());

	auto it = std::find_if(mExternalClocks.begin(), mExternalClocks.end(),
		[=](const ExternalClock& x) { return x.mpDevice == dev; });

	if (period) {
		uint32 timeBase = initialOffset + ATSCHEDULER_GETTIME(mpScheduler);

		if (it != mExternalClocks.end()) {
			if (it->mPeriod == period && it->mTimeBase == timeBase)
				return;

			mExternalClocks.erase(it);

			if (it == mExternalClocks.begin())
				updatePOKEY = true;
		}

		const ExternalClock newEntry { dev, timeBase, period };
		auto it2 = std::lower_bound(mExternalClocks.begin(), mExternalClocks.end(), newEntry,
			[](const ExternalClock& x, const ExternalClock& y) {
				return x.mPeriod < y.mPeriod;
			}
		);

		if (it2 == mExternalClocks.begin())
			updatePOKEY = true;

		mExternalClocks.insert(it2, newEntry);
	} else {
		if (it != mExternalClocks.end()) {
			if (it == mExternalClocks.begin())
				updatePOKEY = true;

			mExternalClocks.erase(it);
		}
	}
}

void ATSSerialEmulator::OnScheduledEvent(uint32 id) {
	mpTransferEvent = nullptr;

	switch(id) {
		case kEventId_Delay:
			mCurrentStep.mType = kStepType_None;
			ExecuteNextStep();
			break;

		case kEventId_TransmitComplete:
			if (mTransferIndex == mTransferEnd) {
				mCurrentStep.mType = kStepType_None;
				ExecuteNextStep();
			}
			break;
	}
}

void ATSSerialEmulator::AbortActiveCommand() {
	if (mpActiveDevice) {
		mpActiveDevice->OnSerialAbortCommand();
		mpActiveDevice = nullptr;
	}

	mStepQueue.clear();
	mCurrentStep.mType = kStepType_None;

	mpScheduler->UnsetEvent(mpTransferEvent);
}

void ATSSerialEmulator::ExecuteNextStep() {
	while(!mCurrentStep.mType && !mStepQueue.empty()) {
		mCurrentStep = mStepQueue.front();
		mStepQueue.pop_front();

		switch(mCurrentStep.mType) {
			case kStepType_Send:
			case kStepType_SendAutoProtocol:
				g_ATLCSIOSteps("[%u] Sending %u bytes\n", VDGetAccurateTick(), mCurrentStep.mTransferLength);

				mbTransferSend = true;
				mTransferStart = mTransferIndex;
				mTransferEnd = mTransferStart + mCurrentStep.mTransferLength;
				VDASSERT(mTransferEnd <= vdcountof(mTransferBuffer));

				OnTransmitMore();
				break;

			case kStepType_Receive:
			case kStepType_ReceiveAutoProtocol:
				g_ATLCSIOSteps("Receiving %u bytes\n", mCurrentStep.mTransferLength);

				mbTransferSend = false;
				mbTransferError = false;
				mTransferStart = mTransferIndex;
				mTransferEnd = mTransferStart + mCurrentStep.mTransferLength;

				mTransferCyclesPerBitRecvMin = mTransferCyclesPerBit - (mTransferCyclesPerBit + 19)/20;
				mTransferCyclesPerBitRecvMax = mTransferCyclesPerBit + (mTransferCyclesPerBit + 19)/20;
				break;

			case kStepType_SetTransferRate:
				mTransferCyclesPerBit = mCurrentStep.mTransferCyclesPerBit;
				mTransferCyclesPerByte = mCurrentStep.mTransferCyclesPerByte;
				mCurrentStep.mType = kStepType_None;
				break;

			case kStepType_Delay:
				g_ATLCSIOSteps("Delaying for %u ticks\n", mCurrentStep.mDelayTicks);

				VDASSERT(mCurrentStep.mDelayTicks);
				mpScheduler->SetEvent(mCurrentStep.mDelayTicks, this, kEventId_Delay, mpTransferEvent);
				break;

			case kStepType_Fence:
				mCurrentStep.mType = kStepType_None;
				mpActiveDevice->OnSerialFence(mCurrentStep.mFenceId);
				break;

			case kStepType_EndCommand:
				g_ATLCSIOSteps <<= "Ending command\n";
				mpActiveDevice = nullptr;
				mCurrentStep.mType = kStepType_None;
				break;

			default:
				VDASSERT("Unknown step in step queue.");
				break;
		}
	}
}

void ATSSerialEmulator::ShiftTransmitBuffer() {
	if (mTransferStart > 0) {
		memmove(mTransferBuffer, mTransferBuffer + mTransferStart, mTransferLevel - mTransferStart);

		mTransferEnd -= mTransferStart;
		mTransferIndex -= mTransferStart;
		mTransferLevel -= mTransferStart;
		mTransferStart = 0;
	}
}

void ATSSerialEmulator::OnTransmitMore() {
	if (mbTransferSend && mTransferIndex < mTransferEnd) {
		uint32 avail;
		void *dst = mpSerEngine->LockWriteAny(mTransferEnd - mTransferIndex, avail);
		if (dst && avail) {
			memcpy(dst, mTransferBuffer + mTransferIndex, avail);
			mTransferIndex += avail;
			mpSerEngine->UnlockWrite(mTransferIndex == mTransferEnd);

			if (mTransferIndex == mTransferEnd) {
				uint32 ticks = VDRoundToInt(avail * (7159090.0/4.0 * 10.0) / (mbHighSpeedMode ? (double)mHighSpeedBaudRate : 19200.0));
				mpScheduler->SetEvent(ticks, this, kEventId_TransmitComplete, mpTransferEvent);
			}
		}
	}
}

void ATSSerialEmulator::ResetTransferRate() {
	mTransferCyclesPerByte = 932;
	mTransferCyclesPerBit = 93;
}

void ATSSerialEmulator::OnTransmitComplete() {
	g_ATLCSIOData("[%u] Transmit complete\n", VDGetAccurateTick());

	if (mbTransferSend && mTransferStart != mTransferEnd) {
		mTransferStart = mTransferEnd;
		mCurrentStep.mType = kStepType_None;
		ExecuteNextStep();
	}
}

void ATSSerialEmulator::OnMotorStateChanged(bool asserted) {
	if (mbMotorState != asserted) {
		mbMotorState = asserted;

		RawDeviceListLock lock(this);

		for(auto *rawdev : mSIORawDevices) {
			if (rawdev)
				rawdev->OnMotorStateChanged(mbMotorState);
		}
	}
}

void ATSSerialEmulator::OnInitingDevice(IATDevice& dev) {
	if (auto *p = vdpoly_cast<IATDeviceScheduling *>(&dev))
		p->InitScheduling(mpScheduler, nullptr);

	if (auto *p = vdpoly_cast<IATDeviceSIO *>(&dev))
		p->InitSIO(this);
}

