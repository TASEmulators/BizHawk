//	Altirra - Atari 800/800XL/5200 emulator
//	Device emulation library - executable loader module
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
#include <vd2/system/binary.h>
#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include <at/atcore/propertyset.h>
#include <at/atcore/logging.h>
#include "exeloader.h"
#include "exeloader.inl"
#include "exeloader-0700.inl"
#include "exeloader-hispeed.inl"
#include "exeloader-hispeed-0700.inl"
#include "exeloader-nobasic.inl"

ATLogChannel g_ATLCExeLoader(true, false, "EXELOADER", "Executable loader");

void ATCreateDeviceExeLoader(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATDeviceExeLoader> p(new ATDeviceExeLoader);

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefExeLoader = { "exeloader", "exeloader", L"EXE Loader", ATCreateDeviceExeLoader };

///////////////////////////////////////////////////////////////////////////

ATDeviceExeLoader::ATDeviceExeLoader() {
	Load(nullptr);

	VDASSERTCT(vdcountof(g_ATFirmwareExeLoader) == 128);
	VDASSERTCT(vdcountof(g_ATFirmwareExeLoader0700) == 128);
	VDASSERTCT(vdcountof(g_ATFirmwareExeLoaderHiSpeed) == 128*4);
	VDASSERTCT(vdcountof(g_ATFirmwareExeLoaderHiSpeed0700) == 128*4);
}

ATDeviceExeLoader::~ATDeviceExeLoader() {
}

void *ATDeviceExeLoader::AsInterface(uint32 iid) {
	switch(iid) {
		case IATDeviceSIO::kTypeID: return static_cast<IATDeviceSIO *>(this);
			break;
	}

	return ATDevice::AsInterface(iid);
}

void ATDeviceExeLoader::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefExeLoader;
}

void ATDeviceExeLoader::GetSettings(ATPropertySet& pset) {
	pset.SetString("path", mPath.c_str());

	if (mbAutoDisableBasic)
		pset.SetBool("nobasic", true);
}

bool ATDeviceExeLoader::SetSettings(const ATPropertySet& pset) {
	mbAutoDisableBasic = pset.GetBool("nobasic");

	const wchar_t *path = pset.GetString("path", L"");

	if (mPath != path)
		Load(path);
	else
		RecreatePreloadSegments();

	return true;
}

void ATDeviceExeLoader::Init() {
}

void ATDeviceExeLoader::Shutdown() {
	Load(nullptr);

	if (mpSIOMgr) {
		mpSIOMgr->RemoveDevice(this);
		mpSIOMgr = nullptr;
	}
}

void ATDeviceExeLoader::Load(const wchar_t *path) {
	mPath.clear();
	mExeData.clear();
	mSegments.clear();
	mSegmentIndex = 0;

	if (!path)
		return;

	vdfastvector<uint8> newData;
	vdfastvector<Segment> newSegments;
	uint32 len32;

	{
		VDFile f(path);

		sint64 len = f.size();
		if (len > 0x400000)
			throw MyError("Executable is too large: %llu bytes.", (unsigned long long)len);

		len32 = (uint32)len;
		newData.resize(len32);

		f.read(newData.data(), (long)len32);
		f.close();
	}

	const uint8 *src0 = newData.data();
	const uint8 *src = src0;
	const uint8 *srcEnd = src0 + len32;
	bool writtenRUNAD1 = false;
	bool writtenRUNAD2 = false;

	while(srcEnd - src >= 4) {
		// read start/end addresses for this segment
		uint16 start = VDReadUnalignedLEU16(src+0);
		if (start == 0xFFFF) {
			src += 2;
			continue;
		}

		uint16 end = VDReadUnalignedLEU16(src+2);
		src += 4;

		uint32 len = (uint32)(end - start) + 1;
		if (end < start || (uint32)(srcEnd - src) < len) {
			if (end >= start) {
				len = (uint32)(srcEnd - src);
				g_ATLCExeLoader <<= "WARNING: Invalid Atari executable: bad start/end range.\n";
			} else {
				g_ATLCExeLoader <<= "ERROR: Invalid Atari executable: bad start/end range.\n";
				src = srcEnd;
				break;
			}
		}

		// check whether this segment overwrites RUNAD
		if (start < 0x02E1 && end >= 0x02E0)
			writtenRUNAD1 = true;

		if (start < 0x02E2 && end >= 0x02E1)
			writtenRUNAD2 = true;

		// queue sends in 4K chunks; this prevents overflowing the SIO manager buffer
		// and also permits better response with errors
		uint32 offset = (uint32)(src - src0);
		src += len;

		while(len) {
			uint32 tc = len > 4096 ? 4096 : len;

			newSegments.push_back( { (uint16)start, (uint16)tc, offset } );

			start += tc;
			len -= tc;
			offset += tc;
		}
	}

	// check if we completely overwrote RUNAD; if not, we need to push a segment to do so
	if ((!writtenRUNAD1 || !writtenRUNAD2) && !mSegments.empty()) {
		uint8 runad[2];
		VDWriteUnalignedLEU16(runad, mSegments.front().mStart);

		newSegments.insert(mSegments.begin(), { 0x02E0, 2, (uint32)mExeData.size() });
		newData.insert(mExeData.end(), runad, runad+2);
	}

	mExeData = std::move(newData);
	mSegments = std::move(newSegments);

	mPath = path;

	RecreatePreloadSegments();
}

void ATDeviceExeLoader::InitSIO(IATDeviceSIOManager *mgr) {
	mpSIOMgr = mgr;

	mgr->AddDevice(this);

	if (!mExeData.empty())
		RecreatePreloadSegments();
}

IATDeviceSIO::CmdResponse ATDeviceExeLoader::OnSerialBeginCommand(const ATDeviceSIOCommand& cmd) {
	if (mSegments.empty())
		return kCmdResponse_NotHandled;

	if (cmd.mDevice == 0xFE) {
		switch(cmd.mCommand) {
			case 0x52:		// read segment
				return OnCmdReadSegment(cmd);
		}

		return kCmdResponse_Fail_NAK;
	} else if (cmd.mDevice == 0x31) {
		switch(cmd.mCommand) {
			case 0x52:		// read sector
				if (mpSIOMgr->GetHighSpeedIndex() < 0) {
					if (cmd.mAUX[0] > 0 && cmd.mAUX[0] < 3 && !cmd.mAUX[1]) {
						mSegmentIndex = 0;

						mpSIOMgr->BeginCommand();
						SetupHighSpeed(cmd);
						mpSIOMgr->SendACK();
						mpSIOMgr->SendComplete();
						mpSIOMgr->SendData(g_ATFirmwareExeLoader0700, 128, true);
						mpSIOMgr->EndCommand();
						return kCmdResponse_Start;
					}
				} else {
					if (cmd.mAUX[0] > 0 && cmd.mAUX[0] < 5 && !cmd.mAUX[1]) {
						mSegmentIndex = 0;

						mpSIOMgr->BeginCommand();
						SetupHighSpeed(cmd);
						mpSIOMgr->SendACK();
						mpSIOMgr->SendComplete();

						char buf[128];
						memcpy(buf, g_ATFirmwareExeLoaderHiSpeed0700 + 128 * (cmd.mAUX[0] - 1), 128);

						// patch high-speed index byte
						if (cmd.mAUX[0] == 4)
							buf[127] = (uint8)mpSIOMgr->GetHighSpeedIndex();

						mpSIOMgr->SendData(buf, 128, true);
						mpSIOMgr->EndCommand();
						return kCmdResponse_Start;
					}
				}
				return kCmdResponse_Fail_NAK;

			case 0x53:		// get status
				{
					static const uint8 status[4] = {
						0x00,
						0xFF,
						0xE0,
						0x00
					};

					mpSIOMgr->BeginCommand();
						SetupHighSpeed(cmd);
					mpSIOMgr->SendACK();
					mpSIOMgr->SendComplete();
					mpSIOMgr->SendData(status, 4, true);
					mpSIOMgr->EndCommand();
				}
				return kCmdResponse_Start;
		}

		return kCmdResponse_NotHandled;
	} else {
		return kCmdResponse_NotHandled;
	}
}

IATDeviceSIO::CmdResponse ATDeviceExeLoader::OnCmdReadSegment(const ATDeviceSIOCommand& cmd) {
	mpSIOMgr->BeginCommand();
	SetupHighSpeed(cmd);
	mpSIOMgr->SendACK();
	mpSIOMgr->SendComplete();

	if ((mSegmentIndex ^ cmd.mAUX[0]) & 1)
		++mSegmentIndex;

	if (mSegmentIndex >= (mPreloadSegments.size() + mSegments.size()) * 2) {
		uint8 termData[8] = {0};
		
		mpSIOMgr->SendData(termData, 8, true);
	} else {
		const uint32 idx = mSegmentIndex >> 1;
		const Segment& seg = idx < mPreloadSegments.size()
			? mPreloadSegments[idx]
			: mSegments[idx - mPreloadSegments.size()];

		if (mSegmentIndex & 1) {
			if (idx < mPreloadSegments.size())
				mpSIOMgr->SendData(mPreloadData.data() + seg.mOffset, seg.mLen, true);
			else
				mpSIOMgr->SendData(mExeData.data() + seg.mOffset, seg.mLen, true);
		} else {
			uint8 stepData[8];

			VDWriteUnalignedLEU16(stepData, seg.mStart);

			// The timeout is in units of 64 vblanks. For NTSC, we can transfer ~2K per tick,
			// and we limit transfers to 4K. Add a bit more for good measure.
			const uint16 timeout = 4;
			VDWriteUnalignedLEU16(stepData + 2, timeout);

			VDWriteUnalignedLEU16(stepData + 4, seg.mLen);

			VDWriteUnalignedLEU16(stepData + 6, mSegmentIndex + 1);

			mpSIOMgr->SendData(stepData, 8, true);
		}
	}

	mpSIOMgr->EndCommand();
	return kCmdResponse_Start;
}

void ATDeviceExeLoader::SetupHighSpeed(const ATDeviceSIOCommand& cmd) {
	if (!cmd.mbStandardRate)
		mpSIOMgr->SetTransferRate(cmd.mCyclesPerBit, cmd.mCyclesPerBit * 10);
}

void ATDeviceExeLoader::RecreatePreloadSegments() {
	if (!mpSIOMgr)
		return;

	mPreloadData.clear();
	mPreloadSegments.clear();

	if (mbAutoDisableBasic)
		QueuePreload(g_ATFirmwareExeLoaderNoBasic, sizeof(g_ATFirmwareExeLoaderNoBasic));

	// If we're using a loader at $0700+, add in a preload segment to reset MEMLO. The
	// regular EXE loader is 1 sector ($0780) and the high-speed loader is 4 sectors
	// ($0900).
	mPreloadSegments.push_back( { 0x02E7, 2, (uint32)mPreloadData.size() } );

	if (mpSIOMgr->GetHighSpeedIndex() >= 0) {
		mPreloadData.push_back(0x00);
		mPreloadData.push_back(0x09);
	} else {
		mPreloadData.push_back(0x80);
		mPreloadData.push_back(0x07);
	}
}

void ATDeviceExeLoader::QueuePreload(const uint8 *data, size_t len) {
	// We need another little mini-EXE parser here. Fortunately, we can rely on these segments
	// being well-formed.
	const uint8 *src = data;
	const uint8 *srcEnd = data + len;

	while(srcEnd - src >= 4) {
		// read start/end addresses for this segment
		uint16 start = VDReadUnalignedLEU16(src+0);
		if (start == 0xFFFF) {
			src += 2;
			continue;
		}

		uint16 end = VDReadUnalignedLEU16(src+2);
		src += 4;

		uint32 len = (uint32)(end - start) + 1;

		// queue sends in 4K chunks; this prevents overflowing the SIO manager buffer
		// and also permits better response with errors
		uint32 offset = (uint32)mPreloadData.size();

		mPreloadData.insert(mPreloadData.end(), src, src + len);
		src += len;

		while(len) {
			uint32 tc = len > 4096 ? 4096 : len;

			mPreloadSegments.push_back( { (uint16)start, (uint16)tc, offset } );

			start += tc;
			len -= tc;
			offset += tc;
		}
	}
}
