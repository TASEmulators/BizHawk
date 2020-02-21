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

#include <stdafx.h>
#include <vd2/system/registry.h>
#include <at/atnativeui/dialog.h>
#include "resource.h"
#include "uitypes.h"
#include "videowriter.h"

class ATUIDialogVideoRecording : public VDDialogFrameW32 {
public:
	ATUIDialogVideoRecording(bool pal)
		: VDDialogFrameW32(IDD_VIDEO_RECORDING)
		, mbPAL(pal)
		, mbHalfRate(false)
		, mbEncodeAllFrames(false)
		, mEncoding(kATVideoEncoding_ZMBV)
		, mFrameRate(kATVideoRecordingFrameRate_Normal)
	{}

	ATVideoEncoding GetEncoding() const { return mEncoding; }
	ATVideoRecordingFrameRate GetFrameRate() const { return mFrameRate; }
	bool GetHalfRate() const { return mbHalfRate; }
	bool GetEncodeAllFrames() const { return mbEncodeAllFrames; }

protected:
	bool OnLoaded();
	void OnDataExchange(bool write);
	bool OnCommand(uint32 id, uint32 extcode);
	void UpdateFrameRateControls();

	bool mbPAL;
	bool mbHalfRate;
	bool mbEncodeAllFrames;
	ATVideoEncoding mEncoding;
	ATVideoRecordingFrameRate mFrameRate;

	VDStringW mBaseFrameRateLabels[3];

	static const uint32 kFrameRateIds[];
};

const uint32 ATUIDialogVideoRecording::kFrameRateIds[]={
	IDC_FRAMERATE_NORMAL,
	IDC_FRAMERATE_NTSCRATIO,
	IDC_FRAMERATE_INTEGRAL,
};

bool ATUIDialogVideoRecording::OnLoaded() {
	for(int i=0; i<3; ++i)
		GetControlText(kFrameRateIds[i], mBaseFrameRateLabels[i]);

	return VDDialogFrameW32::OnLoaded();
}

void ATUIDialogVideoRecording::OnDataExchange(bool write) {
	VDRegistryAppKey key("Settings");

	if (write) {
		if (IsButtonChecked(IDC_VC_ZMBV))
			mEncoding = kATVideoEncoding_ZMBV;
		else if (IsButtonChecked(IDC_VC_RLE))
			mEncoding = kATVideoEncoding_RLE;
		else
			mEncoding = kATVideoEncoding_Raw;

		if (IsButtonChecked(IDC_FRAMERATE_NORMAL))
			mFrameRate = kATVideoRecordingFrameRate_Normal;
		else if (IsButtonChecked(IDC_FRAMERATE_NTSCRATIO))
			mFrameRate = kATVideoRecordingFrameRate_NTSCRatio;
		else if (IsButtonChecked(IDC_FRAMERATE_INTEGRAL))
			mFrameRate = kATVideoRecordingFrameRate_Integral;

		mbHalfRate = IsButtonChecked(IDC_HALF_RATE);
		mbEncodeAllFrames = IsButtonChecked(IDC_ENCODE_ALL_FRAMES);

		key.setInt("Video Recording: Compression Mode", mEncoding);
		key.setInt("Video Recording: Frame Rate", mFrameRate);
		key.setBool("Video Recording: Half Rate", mbHalfRate);
		key.setBool("Video Recording: Encode All Frames", mbEncodeAllFrames);
	} else {
		mEncoding = (ATVideoEncoding)key.getEnumInt("Video Recording: Compression Mode", kATVideoEncodingCount, kATVideoEncoding_ZMBV);
		mFrameRate = (ATVideoRecordingFrameRate)key.getEnumInt("Video Recording: FrameRate", kATVideoRecordingFrameRateCount, kATVideoRecordingFrameRate_Normal);
		mbHalfRate = key.getBool("Video Recording: Half Rate", false);
		mbEncodeAllFrames = key.getBool("Video Recording: Encode All Frames", mbEncodeAllFrames);

		CheckButton(IDC_HALF_RATE, mbHalfRate);
		CheckButton(IDC_ENCODE_ALL_FRAMES, mbEncodeAllFrames);

		switch(mEncoding) {
			case kATVideoEncoding_Raw:
				CheckButton(IDC_VC_NONE, true);
				break;

			case kATVideoEncoding_RLE:
				CheckButton(IDC_VC_RLE, true);
				break;

			case kATVideoEncoding_ZMBV:
				CheckButton(IDC_VC_ZMBV, true);
				break;
		}

		switch(mFrameRate) {
			case kATVideoRecordingFrameRate_Normal:
				CheckButton(IDC_FRAMERATE_NORMAL, true);
				break;

			case kATVideoRecordingFrameRate_NTSCRatio:
				CheckButton(IDC_FRAMERATE_NTSCRATIO, true);
				break;

			case kATVideoRecordingFrameRate_Integral:
				CheckButton(IDC_FRAMERATE_INTEGRAL, true);
				break;
		}

		UpdateFrameRateControls();
	}
}

bool ATUIDialogVideoRecording::OnCommand(uint32 id, uint32 extcode) {
	if (id == IDC_HALF_RATE) {
		bool halfRate = IsButtonChecked(IDC_HALF_RATE);

		if (mbHalfRate != halfRate) {
			mbHalfRate = halfRate;

			UpdateFrameRateControls();
		}
	}

	return false;
}

void ATUIDialogVideoRecording::UpdateFrameRateControls() {
	VDStringW s;

	static const double kFrameRates[][2]={
		{ 3579545.0 / (2.0*114.0*262.0), 1773447.0 / (114.0*312.0) },
		{ 60000.0/1001.0, 50000.0/1001.0 },
		{ 60.0, 50.0 },
	};

	for(int i=0; i<3; ++i) {
		VDStringRefW label(mBaseFrameRateLabels[i]);
		VDStringW::size_type pos = label.find(L'|');

		if (pos != VDStringW::npos) {
			if (mbPAL)
				label = label.subspan(pos+1, VDStringW::npos);
			else
				label = label.subspan(0, pos);
		}

		VDStringW t;

		t.sprintf(L"%.3f fps (%.*ls)", kFrameRates[i][mbPAL] * (mbHalfRate ? 0.5 : 1.0), label.size(), label.data());

		SetControlText(kFrameRateIds[i], t.c_str());
	}
}

///////////////////////////////////////////////////////////////////////////

bool ATUIShowDialogVideoEncoding(VDGUIHandle parent, bool hz50, ATVideoEncoding& encoding, ATVideoRecordingFrameRate& frameRate, bool& halfRate, bool& encodeAll) {
	ATUIDialogVideoRecording dlg(hz50);

	if (!dlg.ShowDialog(parent))
		return false;

	encoding = dlg.GetEncoding();
	frameRate = dlg.GetFrameRate();
	halfRate = dlg.GetHalfRate();
	encodeAll = dlg.GetEncodeAllFrames();
	return true;
}
