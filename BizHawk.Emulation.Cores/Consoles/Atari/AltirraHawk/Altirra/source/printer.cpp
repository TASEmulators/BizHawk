//	Altirra - Atari 800/800XL emulator
//	Copyright (C) 2009 Avery Lee
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
#include <vd2/system/error.h>
#include <vd2/system/filesys.h>
#include <vd2/system/strutil.h>
#include <at/atcore/cio.h>
#include <at/atcore/devicecio.h>
#include <at/atcore/deviceimpl.h>
#include <at/atcore/deviceprinter.h>
#include <at/atcore/devicesio.h>
#include "kerneldb.h"
#include "oshelper.h"
#include "cio.h"

using namespace ATCIOSymbols;

class ATDevicePrinter final : public ATDevice, public IATDevicePrinter, public IATDeviceCIO, public IATDeviceSIO {
	ATDevicePrinter(const ATDevicePrinter&) = delete;
	ATDevicePrinter& operator=(const ATDevicePrinter&) = delete;
public:
	ATDevicePrinter();
	~ATDevicePrinter();

	void *AsInterface(uint32 id) override;

	void SetHookPageByte(uint8 page) {
		mHookPageByte = page;
	}

	void GetDeviceInfo(ATDeviceInfo& info) override;
	void Shutdown() override;
	void WarmReset() override;
	void ColdReset() override;

public:
	void InitCIO(IATDeviceCIOManager *mgr) override;
	void GetCIODevices(char *buf, size_t len) const override;
	sint32 OnCIOOpen(int channel, uint8 deviceNo, uint8 aux1, uint8 aux2, const uint8 *filename) override;
	sint32 OnCIOClose(int channel, uint8 deviceNo) override;
	sint32 OnCIOGetBytes(int channel, uint8 deviceNo, void *buf, uint32 len, uint32& actual) override;
	sint32 OnCIOPutBytes(int channel, uint8 deviceNo, const void *buf, uint32 len, uint32& actual) override;
	sint32 OnCIOGetStatus(int channel, uint8 deviceNo, uint8 statusbuf[4]) override;
	sint32 OnCIOSpecial(int channel, uint8 deviceNo, uint8 cmd, uint16 bufadr, uint16 buflen, uint8 aux[6]) override;
	void OnCIOAbortAsync() override;

public:
	void SetPrinterOutput(IATPrinterOutput *output) {
		mpOutput = output;
	}

public:
	virtual void InitSIO(IATDeviceSIOManager *mgr) override;
	virtual CmdResponse OnSerialBeginCommand(const ATDeviceSIOCommand& cmd) override;
	virtual void OnSerialAbortCommand() override;
	virtual void OnSerialReceiveComplete(uint32 id, const void *data, uint32 len, bool checksumOK) override;
	virtual void OnSerialFence(uint32 id) override;
	virtual CmdResponse OnSerialAccelCommand(const ATDeviceSIORequest& request) override;

protected:
	void Write(const uint8 *c, uint32 count);

	IATDeviceCIOManager *mpCIOMgr;
	IATDeviceSIOManager *mpSIOMgr;
	IATPrinterOutput *mpOutput;

	uint8		mHookPageByte;
	uint32		mLineBufIdx;
	uint8		mLineBuf[132];
};

void ATCreateDevicePrinter(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATDevicePrinter> p(new ATDevicePrinter);

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefPrinter = { "printer", nullptr, L"Printer (P:)", ATCreateDevicePrinter };

ATDevicePrinter::ATDevicePrinter()
	: mpCIOMgr(nullptr)
	, mpSIOMgr(nullptr)
	, mpOutput(nullptr)
{
	ColdReset();
}

ATDevicePrinter::~ATDevicePrinter() {
	ColdReset();
}

void *ATDevicePrinter::AsInterface(uint32 id) {
	switch(id) {
		case IATDevicePrinter::kTypeID:
			return static_cast<IATDevicePrinter *>(this);
		case IATDeviceSIO::kTypeID:
			return static_cast<IATDeviceSIO *>(this);
		case IATDeviceCIO::kTypeID:
			return static_cast<IATDeviceCIO *>(this);
	}

	return ATDevice::AsInterface(id);
}

void ATDevicePrinter::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefPrinter;
}

void ATDevicePrinter::Shutdown() {
	if (mpCIOMgr) {
		mpCIOMgr->RemoveCIODevice(this);
		mpCIOMgr = nullptr;
	}

	if (mpSIOMgr) {
		mpSIOMgr->RemoveDevice(this);
		mpSIOMgr = nullptr;
	}
}

void ATDevicePrinter::WarmReset() {
	ColdReset();
}

void ATDevicePrinter::ColdReset() {
	mLineBufIdx = 0;
}

void ATDevicePrinter::InitCIO(IATDeviceCIOManager *mgr) {
	mpCIOMgr = mgr;
	mpCIOMgr->AddCIODevice(this);
}

void ATDevicePrinter::GetCIODevices(char *buf, size_t len) const {
	vdstrlcpy(buf, "P", len);
}

sint32 ATDevicePrinter::OnCIOOpen(int channel, uint8 deviceNo, uint8 aux1, uint8 aux2, const uint8 *filename) {
	return kATCIOStat_NotSupported;
}

sint32 ATDevicePrinter::OnCIOClose(int channel, uint8 deviceNo) {
	return kATCIOStat_Success;
}

sint32 ATDevicePrinter::OnCIOGetBytes(int channel, uint8 deviceNo, void *buf, uint32 len, uint32& actual) {
	return kATCIOStat_ReadOnly;
}

sint32 ATDevicePrinter::OnCIOPutBytes(int channel, uint8 deviceNo, const void *buf, uint32 len, uint32& actual) {
	actual = len;

	if (mpOutput)
		mpOutput->WriteATASCII(buf, len);

	return kATCIOStat_Success;
}

sint32 ATDevicePrinter::OnCIOGetStatus(int channel, uint8 deviceNo, uint8 statusbuf[4]) {
	statusbuf[0] = 0;
	statusbuf[1] = 0;
	statusbuf[2] = 0x3F;	// not sure what the timeout should be for an 820 printer
	statusbuf[3] = 0;
	return kATCIOStat_Success;
}

sint32 ATDevicePrinter::OnCIOSpecial(int channel, uint8 deviceNo, uint8 cmd, uint16 bufadr, uint16 buflen, uint8 aux[6]) {
	return kATCIOStat_NotSupported;
}

void ATDevicePrinter::OnCIOAbortAsync() {
}

void ATDevicePrinter::InitSIO(IATDeviceSIOManager *mgr) {
	mpSIOMgr = mgr;
	mpSIOMgr->AddDevice(this);
}

IATDeviceSIO::CmdResponse ATDevicePrinter::OnSerialBeginCommand(const ATDeviceSIOCommand& cmd) {
	if (!cmd.mbStandardRate)
		return kCmdResponse_NotHandled;

	if (cmd.mDevice != 0x40)
		return kCmdResponse_NotHandled;

	if (cmd.mCommand == 'W') {
		mpSIOMgr->BeginCommand();

		if (cmd.mAUX[0] == 'S') {
			mpSIOMgr->SendACK();
			mpSIOMgr->ReceiveData(0, 29, true);
			mpSIOMgr->SendComplete();
		} else {
			mpSIOMgr->SendACK();
			mpSIOMgr->ReceiveData(0, 40, true);
			mpSIOMgr->SendComplete();
		}

		mpSIOMgr->EndCommand();

		return kCmdResponse_Start;
	} else if (cmd.mCommand == 'S') {
		mpSIOMgr->BeginCommand();
		mpSIOMgr->SendACK();
		mpSIOMgr->SendComplete();

		const uint8 statusData[4]={
			0,0,16,0
		};

		mpSIOMgr->SendData(statusData, 4, true);
		mpSIOMgr->EndCommand();

		return kCmdResponse_Start;
	}

	return kCmdResponse_NotHandled;
}

void ATDevicePrinter::OnSerialAbortCommand() {
}

void ATDevicePrinter::OnSerialReceiveComplete(uint32 id, const void *data, uint32 len, bool checksumOK) {
	if (!mpOutput)
		return;

	const uint8 *src = (const uint8 *)data;

	while(len && (src[len - 1] == 0x20 || src[len - 1] == 0x9B))
		--len;

	if (len > 40)
		len = 40;

	uint8 buf[41];
	memcpy(buf, src, len);
	buf[len] = 0x9B;

	mpOutput->WriteATASCII(buf, len+1);
}

void ATDevicePrinter::OnSerialFence(uint32 id) {
}

IATDeviceSIO::CmdResponse ATDevicePrinter::OnSerialAccelCommand(const ATDeviceSIORequest& request) {
	return OnSerialBeginCommand(request);
}
