//	Altirra - Atari 800/800XL/5200 emulator
//	Device emulation library - joystick port dongle emulation
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
#include <vd2/system/binary.h>
#include <at/atcore/deviceport.h>
#include <at/atcore/propertyset.h>
#include <at/atdevices/dongle.h>

void ATCreateDeviceDongle(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATDeviceDongle> p(new ATDeviceDongle);

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefDongle = { "dongle", "dongle", L"Joystick port dongle", ATCreateDeviceDongle };

///////////////////////////////////////////////////////////////////////////

ATDeviceDongle::ATDeviceDongle() {
}

ATDeviceDongle::~ATDeviceDongle() {
}

void *ATDeviceDongle::AsInterface(uint32 iid) {
	switch(iid) {
		case IATDevicePortInput::kTypeID: return static_cast<IATDevicePortInput *>(this);
	}

	return ATDevice::AsInterface(iid);
}

void ATDeviceDongle::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefDongle;
}

void ATDeviceDongle::GetSettings(ATPropertySet& pset) {
	pset.SetUint32("port", mPortShift >> 2);
	
	wchar_t mappingStr[17] = {};

	for(int i=0; i<16; ++i) {
		mappingStr[i] = L"0123456789ABCDEF"[mResponseTable[i]];
	}

	pset.SetString("mapping", mappingStr);
}

bool ATDeviceDongle::SetSettings(const ATPropertySet& pset) {
	uint32 port = pset.GetUint32("port");
	uint32 portShift = 0;

	if (port < 4)
		portShift = port * 4;

	if (mPortShift != portShift) {
		mPortShift = portShift;

		if (mpPortManager) {
			mpPortManager->SetInput(mPortInput, UINT32_C(0xFFFFFFFF));

			ReinitPortOutput();
		}
	}

	memset(mResponseTable, 0x0F, sizeof mResponseTable);

	const wchar_t *mapping = pset.GetString("mapping");
	if (mapping) {
		for(int i=0; i<16; ++i) {
			wchar_t c = mapping[i];

			if (c >= L'0' && c <= L'9')
				mResponseTable[i] = (uint8)(c - L'0');
			else if (c >= L'A' && c <= L'F')
				mResponseTable[i] = (uint8)((c - L'A') + 10);
			else
				break;
		}

		UpdatePortOutput();
	}

	return true;
}

void ATDeviceDongle::Init() {
	UpdatePortOutput();
}

void ATDeviceDongle::Shutdown() {
	if (mpPortManager) {
		mpPortManager->FreeInput(mPortInput);
		mpPortManager->FreeOutput(mPortOutput);

		mpPortManager = nullptr;
	}
}

void ATDeviceDongle::InitPortInput(IATDevicePortManager *portMgr) {
	mpPortManager = portMgr;

	mPortInput = mpPortManager->AllocInput();

	ReinitPortOutput();

	mLastPortState = mpPortManager->GetOutputState();
}

void ATDeviceDongle::OnPortOutputChanged(uint32 outputState) {
	outputState = (outputState >> mPortShift) & 15;

	if (mLastPortState != outputState) {
		mLastPortState = outputState;

		UpdatePortOutput();
	}
}

void ATDeviceDongle::ReinitPortOutput() {
	if (mPortOutput >= 0)
		mpPortManager->FreeOutput(mPortOutput);

	mPortOutput = mpPortManager->AllocOutput(
		[](void *pThis, uint32 outputState) {
			((ATDeviceDongle *)pThis)->OnPortOutputChanged(outputState);
		},
		this,
		15 << mPortShift);

	UpdatePortOutput();
}

void ATDeviceDongle::UpdatePortOutput() {
	if (mpPortManager) {
		const uint32 outputState = (mpPortManager->GetOutputState() >> mPortShift) & 15;

		mpPortManager->SetInput(mPortInput, ~(uint32)0 ^ ((mResponseTable[outputState] ^ 15) << mPortShift));
	}
}
