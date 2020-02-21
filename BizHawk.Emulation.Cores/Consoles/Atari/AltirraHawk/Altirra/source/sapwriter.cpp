//	Altirra - Atari 800/800XL/5200 emulator
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
#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include "sapwriter.h"
#include "pokey.h"
#include "simeventmanager.h"
#include "uirender.h"

class ATSAPWriter final : public IATSAPWriter {
public:
	~ATSAPWriter();

	void Init(ATSimulatorEventManager *evtMgr, ATPokeyEmulator *pokey, IATUIRenderer *uir, const wchar_t *filename, bool pal) override;
	void Shutdown() override;

	void CheckExceptions() override;

private:
	void OnVBlank();

	void Write(const void *buf, uint32 len);
	void Flush();

	ATSimulatorEventManager *mpSimEvtMgr = nullptr;
	uint32 mSimEvtRegId = 0;
	ATPokeyEmulator *mpPokey = nullptr;
	IATUIRenderer *mpUIRenderer = nullptr;

	VDFile mFile;

	sint64 mTimePosition = 0;
	uint32 mWriteLevel = 0;
	sint64 mFlushedSize = 0;

	MyError *mpPendingException = nullptr;
	bool mbPal = false;
	bool mbSilencePassed = false;
	uint32 mFrames = 0;

	uint8 mWriteBuffer[4096] = {};
};

IATSAPWriter *ATCreateSAPWriter() {
	return new ATSAPWriter;
}

ATSAPWriter::~ATSAPWriter() {
	Shutdown();

	vdsafedelete <<= mpPendingException;
}

void ATSAPWriter::Init(ATSimulatorEventManager *evtMgr, ATPokeyEmulator *pokey, IATUIRenderer *uir, const wchar_t *filename, bool pal) {
	VDASSERT(!mpSimEvtMgr);

	try {
		mpSimEvtMgr = evtMgr;
		mSimEvtRegId = mpSimEvtMgr->AddEventCallback(kATSimEvent_VBLANK, [this] { OnVBlank(); });

		mpPokey = pokey;
		mpUIRenderer = uir;

		mbPal = pal;

		mFile.open(filename, nsVDFile::kWrite | nsVDFile::kDenyAll | nsVDFile::kCreateAlways);

		static const char kHeader1[] =
			"SAP\r\n"
			"AUTHOR \"<no author>\"\r\n"
			"NAME \"<no name>\"\r\n";

		Write(kHeader1, sizeof kHeader1 - 1);

		if (!pal)
			Write("NTSC\r\n", 6);

		static const char kHeader2[] =
			"TYPE R\r\n"
			"TIME 00:00.000\r\n\r\n";

		Write(kHeader2, sizeof kHeader2 - 1);

		mTimePosition = mFile.tell() + mWriteLevel - 13;

		CheckExceptions();
	} catch(const MyError&) {
		Shutdown();
		throw;
	}
}

void ATSAPWriter::Shutdown() {
	if (mFile.isOpen()) {
		try {
			Flush();

			mFile.seek(mTimePosition);

			char timebuf[9];

			double t = (double)mFrames / (mbPal ? 49.8607 : 59.9227);
			if (t > 6000.0f)
				t = 6000.0f;

			uint32 ms = (uint32)(t * 1000.0);

			if (ms > 5999999)
				ms = 5999999;

			timebuf[8] = '0' + (ms % 10); ms /= 10;
			timebuf[7] = '0' + (ms % 10); ms /= 10;
			timebuf[6] = '0' + (ms % 10); ms /= 10;
			timebuf[5] = '.';

			uint32 mins = ms / 60;
			uint32 secs = ms % 60;
			timebuf[4] = '0' + (secs % 10);
			timebuf[3] = '0' + (secs / 10);
			timebuf[2] = ':';
			timebuf[1] = '0' + (mins % 10);
			timebuf[0] = '0' + (mins / 10);

			mFile.write(timebuf, 9);

			mFile.close();
		} catch(const MyError& e) {
			if (!mpPendingException)
				mpPendingException = new MyError(e);
		}

		mFile.closeNT();
	}

	if (mpSimEvtMgr) {
		mpSimEvtMgr->RemoveEventCallback(mSimEvtRegId);
		mSimEvtRegId = 0;
		mpSimEvtMgr = nullptr;
	}

	if (mpUIRenderer) {
		mpUIRenderer->SetRecordingPosition();
		mpUIRenderer = nullptr;
	}

	mpPokey = nullptr;
}

void ATSAPWriter::CheckExceptions() {
	if (mpPendingException) {
		MyError tmp;
		tmp.TransferFrom(*mpPendingException);

		vdsafedelete <<= mpPendingException;

		throw tmp;
	}
}

void ATSAPWriter::OnVBlank() {
	if (mpPendingException)
		return;

	ATPokeyRegisterState rstate;
	mpPokey->GetRegisterState(rstate);

	if (!mbSilencePassed) {
		const uint8 volumes = rstate.mReg[0] | rstate.mReg[2] | rstate.mReg[4] | rstate.mReg[6];

		if (!(volumes & 15))
			return;

		mbSilencePassed = true;
	}

	Write(rstate.mReg, 9);

	++mFrames;

	mpUIRenderer->SetRecordingPosition((float)mFrames / (mbPal ? 49.8607f : 59.9227f), mFlushedSize + mWriteLevel);
}

void ATSAPWriter::Write(const void *buf, uint32 len) {
	uint32 spaceLeft = sizeof mWriteBuffer - mWriteLevel;

	if (spaceLeft < len) {
		memcpy(mWriteBuffer + mWriteLevel, buf, spaceLeft);
		buf = (const char *)buf + spaceLeft;
		len -= spaceLeft;
		mWriteLevel += spaceLeft;

		Flush();
	}

	if (len >= sizeof mWriteBuffer) {
		uint32 bigLen = len & ((uint32)0 - (uint32)sizeof mWriteBuffer);

		mFile.write(buf, bigLen);
		mFlushedSize += bigLen;
		len -= bigLen;
		buf = (const char *)buf + bigLen;
	}

	if (len) {
		memcpy(mWriteBuffer + mWriteLevel, buf, len);
		mWriteLevel += len;
	}
}

void ATSAPWriter::Flush() {
	if (mWriteLevel) {
		uint32 lvl = mWriteLevel;
		mWriteLevel = 0;

		mFile.write(mWriteBuffer, lvl);
		mFlushedSize += lvl;
	}
}
