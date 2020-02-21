//	Altirra - Atari 800/800XL/5200 emulator
//	Browser (B:) device
//	Copyright (C) 2017 Avery Lee
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
#include <windows.h>
#include <shellapi.h>
#include <vd2/system/time.h>
#include <at/atcore/cio.h>
#include <at/atnativeui/genericdialog.h>
#include "uiaccessors.h"
#include "browser.h"

void ATCreateDeviceBrowser(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATDeviceBrowser> p(new ATDeviceBrowser);

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefBrowser = { "browser", nullptr, L"Browser (B:)", ATCreateDeviceBrowser };

ATDeviceBrowser::ATDeviceBrowser() {
}

ATDeviceBrowser::~ATDeviceBrowser() {
}

void *ATDeviceBrowser::AsInterface(uint32 iid) {
	switch(iid) {
		case IATDeviceCIO::kTypeID: return static_cast<IATDeviceCIO *>(this);
	}

	return ATDevice::AsInterface(iid);
}

void ATDeviceBrowser::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefBrowser;
}

void ATDeviceBrowser::Shutdown(){
	if (mpCIOMgr) {
		mpCIOMgr->RemoveCIODevice(this);
		mpCIOMgr = nullptr;
	}
}

void ATDeviceBrowser::InitCIO(IATDeviceCIOManager *mgr) {
	mpCIOMgr = mgr;
	mgr->AddCIODevice(this);
}

void ATDeviceBrowser::GetCIODevices(char *buf, size_t len) const {
	if (len > 0)
		buf[0] = 'B';
}

sint32 ATDeviceBrowser::OnCIOOpen(int channel, uint8 deviceNo, uint8 aux1, uint8 aux2, const uint8 *filename) {
	mUrlLen = 0;
	mbUrlValid = true;
	return kATCIOStat_Success;
}

sint32 ATDeviceBrowser::OnCIOClose(int channel, uint8 deviceNo) {
	mUrlLen = 0;
	mbUrlValid = true;
	return kATCIOStat_Success;
}

sint32 ATDeviceBrowser::OnCIOGetBytes(int channel, uint8 deviceNo, void *buf, uint32 len, uint32& actual) {
	actual = 0;
	return kATCIOStat_NotSupported;
}

sint32 ATDeviceBrowser::OnCIOPutBytes(int channel, uint8 deviceNo, const void *buf, uint32 len, uint32& actual) {
	// check if the cooldown timer is active. If so, wait until that amount of
	// time has passed before allowing another attempt.
	if (mLastDenyRealTick) {
		uint32 delta = VDGetCurrentTick() - mLastDenyRealTick;

		if (delta + mCooldownTimer < mCooldownTimer * 2)
			return -1;

		mLastDenyRealTick = 0;
	}

	const uint8 *src = (const uint8 *)buf;
	actual = len;

	while(len--) {
		uint8 c = *src++;

		if (mUrlLen >= vdcountof(mUrl)) {
			mbUrlValid = false;
			continue;
		}

		if (c == 0x9B) {
			mUrl[mUrlLen] = 0;
			FlushUrl();
		} else if (c < 0x21 || c >= 0x7F) {
			mbUrlValid = false;
		} else {
			mUrl[mUrlLen++] = (char)c;
		}
	}

	return kATCIOStat_Success;
}

sint32 ATDeviceBrowser::OnCIOGetStatus(int channel, uint8 deviceNo, uint8 statusbuf[4]) {
	statusbuf[0] = 0;
	statusbuf[1] = 0;
	statusbuf[2] = 0x0F;
	statusbuf[3] = 0;
	return kATCIOStat_Success;
}

sint32 ATDeviceBrowser::OnCIOSpecial(int channel, uint8 deviceNo, uint8 cmd, uint16 bufadr, uint16 buflen, uint8 aux[6]) {
	return kATCIOStat_NotSupported;	
}

void ATDeviceBrowser::OnCIOAbortAsync() {
}

void ATDeviceBrowser::FlushUrl() {
	if (mbUrlValid) {
		size_t prefixLen = 0;

		if (!strncmp(mUrl, "http://", 7))
			prefixLen = 7;
		else if (!strncmp(mUrl, "https://", 8))
			prefixLen = 8;

		if (prefixLen) {
			const char *serverStart = mUrl + prefixLen;
			const char *serverEnd = serverStart;

			while(*serverEnd && *serverEnd != '/' && *serverEnd != '?')
				++serverEnd;

			VDStringW message;
			message.sprintf(L"The running program would like to open the website:\n\n\t%.*hs", (int)(serverEnd - serverStart), serverStart);

			ATUIGenericDialogOptions opts {};
			opts.mhParent = ATUIGetMainWindow();
			opts.mpMessage = message.c_str();
			opts.mpTitle = L"Browser request";
			opts.mpIgnoreTag = "browser";
			opts.mIconType = kATUIGenericIconType_Info;
			opts.mResultMask = kATUIGenericResultMask_AllowDeny;
			opts.mValidIgnoreMask = kATUIGenericResultMask_Allow;

			auto result = ATUIShowGenericDialog(opts);

			if (result == kATUIGenericResult_Allow) {
				ShellExecuteW((HWND)ATUIGetMainWindow(), L"open", VDTextAToW(mUrl).c_str(), nullptr, nullptr, SW_SHOWNORMAL);

				mCooldownTimer = 0;
			} else {
				// start at 2s, then double to 4s
				if (!mCooldownTimer)
					mCooldownTimer = 2000;
				else if (mCooldownTimer < 4000)
					mCooldownTimer *= 2;

				// set cooldown time base (ensuring never 0)
				mLastDenyRealTick = VDGetCurrentTick() | 1;
			}
		}
	}

	mUrlLen = 0;
	mbUrlValid = true;
}
