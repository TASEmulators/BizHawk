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

#include <stdafx.h>
#include <regex>
#include <vd2/system/math.h>
#include <vd2/system/time.h>
#include <vd2/Dita/services.h>
#include <vd2/Kasumi/pixmapops.h>
#include <vd2/Kasumi/resample.h>
#include <vd2/VDDisplay/font.h>
#include <at/atcore/device.h>
#include <at/atcore/devicevideo.h>
#include "console.h"
#include "debugger.h"
#include "inputmanager.h"
#include "oshelper.h"
#include "pokey.h"
#include "simulator.h"
#include "symbols.h"
#include "uiaccessors.h"
#include "uicaptionupdater.h"
#include "uidragdrop.h"
#include "uienhancedtext.h"
#include "uikeyboard.h"
#include <at/atui/uimanager.h>
#include "uirender.h"
#include "uivideodisplaywindow.h"
#include "uionscreenkeyboard.h"
#include "uisettingswindow.h"
#include "uitypes.h"
#include <at/atuicontrols/uilabel.h>
#include <at/atui/uianchor.h>
#include <at/atui/uidragdrop.h>
#include "xep80.h"

extern ATSimulator g_sim;

extern IATUIWindowCaptionUpdater *g_winCaptionUpdater;

extern bool g_fullscreen;
extern bool g_mouseClipped;
extern bool g_mouseCaptured;
extern bool g_mouseAutoCapture;
extern ATUIKeyboardOptions g_kbdOpts;
extern bool g_xepViewEnabled;
extern bool g_xepViewAutoswitchingEnabled;
extern ATDisplayStretchMode g_displayStretchMode;
extern ATDisplayFilterMode g_dispFilterMode;
extern int g_dispFilterSharpness;
extern ATUIVideoDisplayWindow *g_pATVideoDisplayWindow;

void ATCreateUISettingsScreenMain(IATUISettingsScreen **screen);
void OnCommandEditPasteText();

bool g_xepViewAvailable;

///////////////////////////////////////////////////////////////////////////

bool ATUIGetXEPViewEnabled() {
	return g_xepViewEnabled;
}

void ATUISetXEPViewEnabled(bool enabled) {
	if (g_xepViewEnabled == enabled)
		return;

	g_xepViewEnabled = enabled;

	if (g_xepViewAvailable) {
		IATUIRenderer *uir = g_sim.GetUIRenderer();

		if (uir) {
			if (enabled)
				uir->SetStatusMessage(L"XEP80 View");
			else
				uir->SetStatusMessage(L"Normal View");
		}
	}

	if (g_pATVideoDisplayWindow)
		g_pATVideoDisplayWindow->UpdateAltDisplay();
}

///////////////////////////////////////////////////////////////////////////

namespace {
	const ATUIDropFilesTarget kDropFileTargets[] = {
		ATUIDropFilesTarget::MountCart,
		ATUIDropFilesTarget::MountDisk4,
		ATUIDropFilesTarget::MountDisk3,
		ATUIDropFilesTarget::MountDisk2,
		ATUIDropFilesTarget::MountDisk1,
		ATUIDropFilesTarget::MountImage,
		ATUIDropFilesTarget::BootImage,
	};
}

ATUIVideoDisplayWindow::ATUIVideoDisplayWindow()
	: mDisplayRect(0, 0, 0, 0)
	, mbDragActive(false)
	, mbDragInitial(false)
	, mDragAnchorX(0)
	, mDragAnchorY(0)
	, mbMouseHidden(false)
	, mMouseHideX(0)
	, mMouseHideY(0)
	, mbOpenSidePanelDeferred(false)
	, mbCoordIndicatorActive(false)
	, mbCoordIndicatorEnabled(false)
	, mHoverTipArea(0, 0, 0, 0)
	, mbHoverTipActive(false)
	, mpEnhTextEngine(NULL)
	, mpOSK(NULL)
	, mpOSKPanel(nullptr)
	, mpSidePanel(NULL)
	, mpSEM(NULL)
	, mpDevMgr(nullptr)
	, mpXEP(NULL)
	, mAltVOChangeCount(0)
	, mAltVOLayoutChangeCount(0)
	, mXEPDataReceivedCount(0)
	, mpUILabelBadSignal(NULL)
{
	mbFastClip = true;
	SetAlphaFillColor(0);
	SetTouchMode(kATUITouchMode_Dynamic);
	SetDropTarget(true);
}

ATUIVideoDisplayWindow::~ATUIVideoDisplayWindow() {
	Shutdown();
}

bool ATUIVideoDisplayWindow::Init(ATSimulatorEventManager& sem, ATDeviceManager& devMgr) {
	mpSEM = &sem;
	mEventCallbackIdWarmReset = mpSEM->AddEventCallback(kATSimEvent_WarmReset, [this] { OnReset(); });
	mEventCallbackIdColdReset = mpSEM->AddEventCallback(kATSimEvent_ColdReset, [this] { OnReset(); });
	mEventCallbackIdFrameTick = mpSEM->AddEventCallback(kATSimEvent_FrameTick, [this] { OnFrameTick(); });

	mpDevMgr = &devMgr;
	mpDevMgr->AddDeviceChangeCallback(IATDeviceVideoOutput::kTypeID, this);

	for(IATDeviceVideoOutput *vo : mpDevMgr->GetInterfaces<IATDeviceVideoOutput>(false, false))
		SetXEP(vo);

	return true;
}

void ATUIVideoDisplayWindow::Shutdown() {
	if (mpDevMgr) {
		mpDevMgr->RemoveDeviceChangeCallback(IATDeviceVideoOutput::kTypeID, this);
	}

	if (mpSEM) {
		mpSEM->RemoveEventCallback(mEventCallbackIdWarmReset);
		mpSEM->RemoveEventCallback(mEventCallbackIdColdReset);
		mpSEM->RemoveEventCallback(mEventCallbackIdFrameTick);

		mpSEM = nullptr;
	}

	vdsaferelease <<= mpUILabelEnhTextSize;
}

void ATUIVideoDisplayWindow::ToggleHoldKeys() {
	mbHoldKeys = !mbHoldKeys;

	if (!mbHoldKeys) {
		g_sim.ClearPendingHeldKey();
		g_sim.SetPendingHeldSwitches(0);
	}

	g_sim.GetUIRenderer()->SetPendingHoldMode(mbHoldKeys);
}

void ATUIVideoDisplayWindow::ToggleCaptureMouse() {
	if (g_mouseCaptured)
		ReleaseMouse();
	else
		CaptureMouse();
}

void ATUIVideoDisplayWindow::ReleaseMouse() {
	ReleaseCursor();
	OnCaptureLost();
}

void ATUIVideoDisplayWindow::CaptureMouse() {
	ATInputManager *im = g_sim.GetInputManager();

	if (im->IsMouseMapped() && g_sim.IsRunning()) {
		g_mouseCaptured = true;

		if (im->IsMouseAbsoluteMode()) {
			CaptureCursor(false, true);

			g_mouseClipped = true;
		} else {
			SetCursorImage(kATUICursorImage_Hidden);
			CaptureCursor(true, false);

		}

		g_winCaptionUpdater->SetMouseCaptured(true, !im->IsInputMapped(0, kATInputCode_MouseMMB));
	}
}

void ATUIVideoDisplayWindow::OpenOSK() {
	if (!mpOSK) {
		CloseSidePanel();

		mpOSKPanel = new ATUIContainer;
		mpOSKPanel->AddRef();
		AddChild(mpOSKPanel);

		mpOSK = new ATUIOnScreenKeyboard;
		mpOSK->AddRef();
		mpOSKPanel->AddChild(mpOSK);

		mpOSK->Focus();
		OnSize();

		if (mpOnOSKChange)
			mpOnOSKChange();
	}
}

void ATUIVideoDisplayWindow::CloseOSK() {
	if (mpOSK) {
		mpOSKPanel->Destroy();
		vdsaferelease <<= mpOSK;
		vdsaferelease <<= mpOSKPanel;

		if (mpOnOSKChange)
			mpOnOSKChange();
	}
}

void ATUIVideoDisplayWindow::OpenSidePanel() {
	if (mpSidePanel)
		return;

	CloseOSK();

	vdrefptr<IATUISettingsScreen> screen;
	ATCreateUISettingsScreenMain(~screen);

	ATCreateUISettingsWindow(&mpSidePanel);
	mpSidePanel->SetOnDestroy([this]() { vdsaferelease <<= mpSidePanel; });
	mpSidePanel->SetSettingsScreen(screen);
	AddChild(mpSidePanel);
	mpSidePanel->Focus();
}

void ATUIVideoDisplayWindow::CloseSidePanel() {
	if (mpSidePanel) {
		mpSidePanel->Destroy();
		vdsaferelease <<= mpSidePanel;
	}
}

void ATUIVideoDisplayWindow::BeginEnhTextSizeIndicator() {
	mbShowEnhSizeIndicator = true;
}

void ATUIVideoDisplayWindow::EndEnhTextSizeIndicator() {
	mbShowEnhSizeIndicator = false;

	if (mpUILabelEnhTextSize) {
		mpUILabelEnhTextSize->Destroy();
		vdsaferelease <<= mpUILabelEnhTextSize;
	}
}

void ATUIVideoDisplayWindow::Copy(bool enableEscaping) {
	if (mDragPreviewSpans.empty())
		return;

	uint8 data[80];
	VDStringA s;

	for(const TextSpan& ts : mDragPreviewSpans) {
		int actual;

		if (mpAltVideoOutput) {
			actual = mpAltVideoOutput->ReadRawText(data, ts.mX, ts.mY, 80);
		} else {
			actual = ReadText(data, ts.mY, ts.mCharX, ts.mCharWidth);
		}

		if (!actual)
			continue;

		if (enableEscaping) {
			uint8 inv = 0;
			bool started = false;

			for(int i=0; i<actual; ++i) {
				uint8 c = data[i];

				if (!started) {
					if (c == 0x20)
						continue;

					started = true;
				}

				if ((c ^ inv) & 0x80) {
					inv ^= 0x80;

					s.append("{inv}");
				}

				c &= 0x7F;

				if (c == 0x00) {
					s.append("{^},");
				} else if (c >= 0x01 && c < 0x1B) {
					s.append("{^}");
					s += (char)('a' + (c - 0x01));
				} else if (c == 0x1B) {
					s.append("{esc}{esc}");
				} else if (c == 0x1C) {
					if (inv)
						s.append("{esc}{+delete}");
					else
						s.append("{esc}{up}");
				} else if (c == 0x1D) {
					if (inv)
						s.append("{esc}{+insert}");
					else
						s.append("{esc}{down}");
				} else if (c == 0x1E) {
					if (inv)
						s.append("{esc}{^tab}");
					else
						s.append("{esc}{left}");
				} else if (c == 0x1F) {
					if (inv)
						s.append("{esc}{+tab}");
					else
						s.append("{esc}{right}");
				} else if (c >= 0x20 && c < 0x60) {
					s += (char)c;
				} else if (c == 0x60) {
					s.append("{^}.");
				} else if (c >= 0x61 && c < 0x7B) {
					s += (char)c;
				} else if (c == 0x7B) {
					s.append("{^};");
				} else if (c == 0x7C) {
					s += (char)c;
				} else if (c == 0x7D) {
					if (inv)
						s.append("{esc}{^}2");
					else
						s.append("{esc}{clear}");
				} else if (c == 0x7E) {
					if (inv)
						s.append("{esc}{del}");
					else
						s.append("{esc}{back}");
				} else if (c == 0x7F) {
					if (inv)
						s.append("{esc}{ins}");
					else
						s.append("{esc}{tab}");
				}
			}

			while(!s.empty() && s.back() == ' ')
				s.pop_back();

			if (inv)
				s.append("{inv}");
		} else {
			for(int i=0; i<actual; ++i) {
				data[i] &= 0x7f;

				if ((uint8)(data[i] - 0x20) >= 0x7d)
					data[i] = ' ';
			}

			int base = 0;
			while(base < actual && data[base] == 0x20)
				++base;

			while(actual > base && data[actual - 1] == 0x20)
				--actual;

			s.append((const char *)data + base, (const char *)data + actual);
		}

		s += "\r\n";
	}

	if (s.size() > 2) {
		s.pop_back();
		s.pop_back();
		mpManager->GetClipboard()->CopyText(s.c_str());
	}
}

void ATUIVideoDisplayWindow::CopySaveFrame(bool saveFrame, bool trueAspect, const wchar_t *path) {
	VDPixmap frameView;
	double par = 1;

	if (mpAltVideoOutput) {
		const auto& videoInfo = mpAltVideoOutput->GetVideoInfo();
		const auto& framebuffer = mpAltVideoOutput->GetFrameBuffer();

		par = videoInfo.mPixelAspectRatio;
		frameView = framebuffer;
	} else {
		ATGTIAEmulator& gtia = g_sim.GetGTIA();
		const VDPixmap *frame = gtia.GetLastFrameBuffer();

		if (!frame)
			return;

		int px = 2;
		int py = 2;
		gtia.GetPixelAspectMultiple(px, py);

		par = (double)py / (double)px;
		frameView = *frame;

		const bool pal = g_sim.GetVideoStandard() != kATVideoStandard_NTSC && g_sim.GetVideoStandard() != kATVideoStandard_PAL60;
		par *= (pal ? 1.03964f : 0.857141f);
	}

	// We may get a really evil format like Pal1 from the XEP-80 layer, so first blit
	// it to RGB32.
	VDPixmapBuffer buf(frameView.w, frameView.h, nsVDPixmap::kPixFormat_XRGB8888);

	VDPixmapBlt(buf, frameView);

	// Perform aspect ratio correction.
	int sw = frameView.w;
	int sh = frameView.h;
	double dw = sw;
	double dh = sh;

	if (trueAspect) {
		if (par < 1.0) {
			dh *= 2;
			par *= 2;
		}

		dw *= par;
	} else {
		if (par < 0.75)
			dh *= 2;
		else if (par > 1.5)
			dw *= 2;
	}

	int iw = VDRoundToInt(dw);
	int ih = VDRoundToInt(dh);

	if (iw != buf.w || ih != buf.h) {
		VDPixmapBuffer buf2(iw, ih, nsVDPixmap::kPixFormat_XRGB8888);

		if (trueAspect) {
			vdautoptr<IVDPixmapResampler> r(VDCreatePixmapResampler());

			r->SetFilters(IVDPixmapResampler::kFilterLinear, IVDPixmapResampler::kFilterLinear, false);

			const vdrect32f dstRect {
				(float)((iw - dw) * 0.5),
				(float)((ih - dh) * 0.5),
				(float)((iw + dw) * 0.5),
				(float)((ih + dh) * 0.5)
			};

			const vdrect32f srcRect {
				(float)0,
				(float)0,
				(float)frameView.w,
				(float)frameView.h
			};

			r->Init(dstRect, buf2.w, buf2.h, buf2.format, srcRect, buf.w, buf.h, buf.format);
			r->Process(buf2, buf);
		} else
			VDPixmapStretchBltNearest(buf2, buf);

		buf.swap(buf2);
	}

	frameView = buf;

	if (saveFrame) {
		VDStringW fn;
		
		if (path)
			fn = path;
		else
			fn = VDGetSaveFileName('scrn', ATUIGetMainWindow(), L"Save Screenshot", L"Portable Network Graphics (*.png)\0*.png\0", L"png");

		if (!fn.empty())
			ATSaveFrame(frameView, fn.c_str());
	} else
		ATCopyFrameToClipboard(frameView);

}

vdrect32 ATUIVideoDisplayWindow::GetOSKSafeArea() const {
	vdrect32 r(GetArea());
	r.translate(-r.left, -r.top);

	if (mpOSKPanel) {
		int bottomLimit = mpOSKPanel->GetArea().top;

		if (bottomLimit < r.bottom/2)
			bottomLimit = r.bottom/2;

		if (bottomLimit < r.bottom)
			r.bottom = bottomLimit;
	}

	return r;
}

void ATUIVideoDisplayWindow::SetDisplaySourceMapping(vdfunction<bool(vdfloat2&)> dispToSrcFn, vdfunction<bool(vdfloat2&)> srcToDispFn) {
	mpMapDisplayToSourcePt = std::move(dispToSrcFn);
	mpMapSourceToDisplayPt = std::move(srcToDispFn);
}

void ATUIVideoDisplayWindow::SetDisplayRect(const vdrect32& r) {
	mDisplayRect = r;
	UpdateDragPreviewRects();
}

void ATUIVideoDisplayWindow::ClearHighlights() {
	if (!mHighlightPoints.empty()) {
		mHighlightPoints.clear();
		Invalidate();
	}
}

void ATUIVideoDisplayWindow::SetXEP(IATDeviceVideoOutput *xep) {
	mpXEP = xep;

	if (xep) {
		const auto& vi = xep->GetVideoInfo();

		// _don't_ force a data received event next frame
		mXEPDataReceivedCount = xep->GetActivityCounter();

		g_xepViewAvailable = true;
	} else {
		mAltVOImageView.SetImage();

		g_xepViewAvailable = false;
	}

	UpdateAltDisplay();
}

void ATUIVideoDisplayWindow::SetEnhancedTextEngine(IATUIEnhancedTextEngine *p) {
	if (mpEnhTextEngine == p)
		return;

	mpEnhTextEngine = p;

	if (p) {
		const auto& r = GetClientArea();
		p->OnSize(r.width(), r.height());
	}

	UpdateAltDisplay();
}

void ATUIVideoDisplayWindow::InvalidateTextOutput() {
	Invalidate();
}

void ATUIVideoDisplayWindow::OnReset() {
	mbHoldKeys = false;
	g_sim.GetUIRenderer()->SetPendingHoldMode(false);

	if (mpXEP) {
		if (g_xepViewAutoswitchingEnabled)
			ATUISetXEPViewEnabled(false);

		mXEPDataReceivedCount = mpXEP->GetActivityCounter();
	}
}

void ATUIVideoDisplayWindow::OnFrameTick() {
	if (mpXEP) {
		uint32 c = mpXEP->GetActivityCounter();

		if (mXEPDataReceivedCount != c) {
			mXEPDataReceivedCount = c;

			if (g_xepViewAutoswitchingEnabled && !g_xepViewEnabled)
				ATUISetXEPViewEnabled(true);
		}
	}
}

void ATUIVideoDisplayWindow::OnDeviceAdded(uint32 iid, IATDevice *dev, void *iface) {
	IATDeviceVideoOutput *vo = (IATDeviceVideoOutput *)iface;

	if (vo)
		SetXEP(vo);
}

void ATUIVideoDisplayWindow::OnDeviceRemoving(uint32 iid, IATDevice *dev, void *iface) {
}

void ATUIVideoDisplayWindow::OnDeviceRemoved(uint32 iid, IATDevice *dev, void *iface) {
	IATDeviceVideoOutput *vo = (IATDeviceVideoOutput *)iface;

	if (mpXEP == vo)
		SetXEP(nullptr);
}

ATUITouchMode ATUIVideoDisplayWindow::GetTouchModeAtPoint(const vdpoint32& pt) const {
	// allow swiping in the bottom quarter to bring up the OSK
	if (pt.y >= mArea.height() * 3 / 4)
		return kATUITouchMode_Default;

	// check for input mapping
	ATInputManager *im = g_sim.GetInputManager();
	
	if (im->IsInputMapped(0, kATInputCode_MouseLMB) && !im->IsInputMapped(0, kATInputCode_MouseRMB))
		return kATUITouchMode_Immediate;

	return kATUITouchMode_Default;
}

namespace {
	VDStringW GetTipMessage(char *data, sint32 x) {
		// trim and null-terminate the string
		char *s = data;

		while(*s == ' ')
			++s;

		char *t = s + strlen(s);

		while(t != s && t[-1] == ' ')
			--t;

		*t = 0;

		// convert to uppercase
		for(char *s2 = s; *s2; ++s2)
			*s2 = toupper((unsigned char)*s2);

		// look for an error
		VDStringW msg;
		if (t = strstr(s, "ERROR"); t) {
			// skip ERROR string
			t += 5;

			// skip blanks
			while(*t == ' ')
				++t;

			// look for an optional dash or pound
			if (*t == '#' || *t == '-') {
				++t;

				// skip more blanks
				while(*t == ' ')
					++t;
			}

			// look for a digit
			if ((unsigned char)(*t - '0') < 10) {
				int errCode = atoi(t);

				if (errCode >= 2 && errCode <= 255) {
					msg.sprintf(L"<b>Error %u</b>\n", errCode);

					switch(errCode) {
						case 2: msg += L"<b>Atari BASIC:</b> Out of memory"; break;
						case 3: msg += L"<b>Atari BASIC:</b> Value error"; break;
						case 4: msg += L"<b>Atari BASIC:</b> Too many variables"; break;
						case 5: msg += L"<b>Atari BASIC:</b> String length error"; break;
						case 6: msg += L"<b>Atari BASIC:</b> Out of data"; break;
						case 7: msg += L"<b>Atari BASIC:</b> Number &gt;32767"; break;
						case 8: msg += L"<b>Atari BASIC:</b> Input statement error"; break;
						case 9: msg += L"<b>Atari BASIC:</b> DIM error"; break;
						case 10: msg += L"<b>Atari BASIC:</b> Argument stack overflow"; break;
						case 11: msg += L"<b>Atari BASIC:</b> Floating point overflow/underflow"; break;
						case 12: msg += L"<b>Atari BASIC:</b> Line not found"; break;
						case 13: msg += L"<b>Atari BASIC:</b> No matching FOR statement"; break;
						case 14: msg += L"<b>Atari BASIC:</b> Line too long"; break;
						case 15: msg += L"<b>Atari BASIC:</b> GOSUB or FOR line deleted"; break;
						case 16: msg += L"<b>Atari BASIC:</b> RETURN error"; break;
						case 17: msg += L"<b>Atari BASIC:</b> Garbage error"; break;
						case 18: msg += L"<b>Atari BASIC:</b> Invalid string character"; break;
						case 19: msg += L"<b>Atari BASIC:</b> LOAD program too long"; break;
						case 20: msg += L"<b>Atari BASIC:</b> Device number error"; break;
						case 21: msg += L"<b>Atari BASIC:</b> LOAD file error"; break;

						case 128: msg += L"<b>CIO:</b> User break abort"; break;
						case 129: msg += L"<b>CIO:</b> IOCB in use"; break;
						case 130: msg += L"<b>CIO:</b> Unknown device"; break;
						case 131: msg += L"<b>CIO:</b> IOCB write only"; break;
						case 132: msg += L"<b>CIO:</b> Invalid command"; break;
						case 133: msg += L"<b>CIO:</b> IOCB not open"; break;
						case 134: msg += L"<b>CIO:</b> Invalid IOCB"; break;
						case 135: msg += L"<b>CIO:</b> IOCB read only"; break;
						case 136: msg += L"<b>CIO:</b> End of file"; break;
						case 137: msg += L"<b>CIO:</b> Truncated record"; break;
						case 138: msg += L"<b>CIO/SIO:</b> Timeout"; break;
						case 139: msg += L"<b>CIO/SIO:</b> Device NAK"; break;
						case 140: msg += L"<b>CIO/SIO:</b> Bad frame"; break;
						case 142: msg += L"<b>CIO/SIO:</b> Serial input overrun"; break;
						case 143: msg += L"<b>CIO/SIO:</b> Checksum error"; break;
						case 144: msg += L"<b>CIO/SIO:</b> Device error or write protected disk"; break;
						case 145: msg += L"<b>CIO:</b> Bad screen mode"; break;
						case 146: msg += L"<b>CIO:</b> Not supported"; break;
						case 147: msg += L"<b>CIO:</b> Out of memory"; break;

						case 160: msg += L"<b>DOS:</b> Invalid drive number"; break;
						case 161: msg += L"<b>DOS:</b> Too many open files"; break;
						case 162: msg += L"<b>DOS:</b> Disk full"; break;
						case 163: msg += L"<b>DOS:</b> Fatal disk I/O error"; break;
						case 164: msg += L"<b>DOS:</b> File number mismatch"; break;
						case 165: msg += L"<b>DOS:</b> File name error"; break;
						case 166: msg += L"<b>DOS:</b> POINT data length error"; break;
						case 167: msg += L"<b>DOS:</b> File locked"; break;
						case 168: msg += L"<b>DOS:</b> Command invalid"; break;
						case 169: msg += L"<b>DOS:</b> Directory full"; break;
						case 170: msg += L"<b>DOS:</b> File not found"; break;
						case 171: msg += L"<b>DOS:</b> Invalid POINT"; break;

						case 173: msg += L"<b>DOS 3:</b> Bad sectors at format time"; break;
						case 174: msg += L"<b>DOS 3:</b> Duplicate filename"; break;
						case 175: msg += L"<b>DOS 3:</b> Bad load file"; break;
						case 176: msg += L"<b>DOS 3:</b> Incompatible format"; break;
						case 177: msg += L"<b>DOS 3:</b> Disk structure damaged"; break;
					}
				}
			}
		}
		
		if (msg.empty()) {
			unsigned address = 0;
			bool isRead = false;

			const std::regex peekRegex(R"--(PEEK\( *([0-9]+) *\))--", std::regex::extended);
			for(auto it = std::cregex_iterator(s, s+strlen(s), peekRegex, std::regex_constants::match_any), itEnd = std::cregex_iterator(); it != itEnd; ++it) {
				const auto& capture = (*it)[0];
				ptrdiff_t offset1 = capture.first - data;
				ptrdiff_t offset2 = capture.second - data;

				if (offset1 <= x && offset2 > x) {
					address = atoi((*it)[1].str().c_str());
					isRead = true;
					break;
				}
			}


			const std::regex pokeRegex(R"--(POKE +([0-9]+) *,)--", std::regex::extended);
			for(auto it = std::cregex_iterator(s, s+strlen(s), pokeRegex, std::regex_constants::match_any), itEnd = std::cregex_iterator(); it != itEnd; ++it) {
				const auto& capture = (*it)[0];
				ptrdiff_t offset1 = capture.first - data;
				ptrdiff_t offset2 = capture.second - data;

				if (offset1 <= x && offset2 > x) {
					address = atoi((*it)[1].str().c_str());
					isRead = false;
					break;
				}
			}

			if (address) {
				ATSymbol sym;

				if (ATGetDebuggerSymbolLookup()->LookupSymbol(address, isRead ? kATSymbol_Read : kATSymbol_Write, sym)) {
					msg.sprintf(L"<b>Address %u ($%0*X):</b> %hs", address, address >= 256 ? 4 : 2, address, sym.mpName);
				}
			}
		}

		if (msg.empty() && *s)
			msg = L"There is no help for this message.\nHover over an SIO, CIO, BASIC, or DOS error message for help.";

		return msg;
	}
}

void ATUIVideoDisplayWindow::OnMouseDown(sint32 x, sint32 y, uint32 vk, bool dblclk) {
	ATInputManager *im = g_sim.GetInputManager();

	Focus();

	if (mpOSK) {
		CloseOSK();
		return;
	}

	// If the mouse is mapped, it gets first crack at inputs unless Alt is down.
	const bool alt = mpManager->IsKeyDown(kATUIVK_Alt);
	if (im->IsMouseMapped() && g_sim.IsRunning() && !alt) {

		// Check if auto-capture is on and we haven't captured the mouse yet. If so, we
		// should capture the mouse but otherwise eat the click
		if (g_mouseAutoCapture && !g_mouseCaptured) {
			if (vk == kATUIVK_LButton) {
				CaptureMouse();
				return;
			}
		} else {
			const bool absMode = im->IsMouseAbsoluteMode();

			// Check if the mouse is captured or we are in absolute mode. If we are in
			// relative mode and haven't captured the mouse we should not route this
			// shunt to the input manager.
			if (g_mouseCaptured || absMode) {
				if (absMode)
					UpdateMousePosition(x, y);

				switch(vk) {
					case kATUIVK_LButton:
						im->OnButtonDown(0, kATInputCode_MouseLMB);
						break;

					case kATUIVK_MButton:
						if (im->IsInputMapped(0, kATInputCode_MouseMMB))
							im->OnButtonDown(0, kATInputCode_MouseMMB);
						else if (g_mouseCaptured)
							ReleaseMouse();
						break;

					case kATUIVK_RButton:
						im->OnButtonDown(0, kATInputCode_MouseRMB);
						break;

					case kATUIVK_XButton1:
						im->OnButtonDown(0, kATInputCode_MouseX1B);
						break;

					case kATUIVK_XButton2:
						im->OnButtonDown(0, kATInputCode_MouseX2B);
						break;
				}

				return;
			}
		}
	}
	
	// We aren't routing this mouse event to the input manager, so do selection if it's the
	// LMB.

	if (vk == kATUIVK_LButton) {
		if (alt) {
			// tooltip request -- let's try to grab text
			int xc;
			int yc;

			bool valid = false;

			if (mpAltVideoOutput) {
				const auto& videoInfo = mpAltVideoOutput->GetVideoInfo();
				const vdrect32& rBlit = GetAltDisplayArea();
				const vdrect32& rDisp = videoInfo.mDisplayArea;

				if (rBlit.contains(vdpoint32(x, y)) && !rDisp.empty()) {
					int dx = VDRoundToInt((float)x * (float)rDisp.width() / (float)rBlit.width());
					int dy = VDRoundToInt((float)y * (float)rDisp.height() / (float)rBlit.height());

					const vdpoint32 caretPos = mpAltVideoOutput->PixelToCaretPos(vdpoint32(dx, dy));

					uint8 buf[81];
					int actual = mpAltVideoOutput->ReadRawText(buf, 0, caretPos.y, 80);

					char text[81];
					for(int i=0; i<actual; ++i) {
						uint8 c = buf[i] & 0x7f;

						if ((uint8)(c - 0x20) > 0x5f)
							c = 0x20;

						text[i] = (char)c;
					}

					text[actual] = 0;

					const VDStringW& msg = GetTipMessage(text, caretPos.x);

					if (!msg.empty()) {
						const vdrect32& lineRect = mpAltVideoOutput->CharToPixelRect(vdrect32(0, caretPos.y, videoInfo.mTextColumns, caretPos.y + 1));

						float scaleX = (float)rBlit.width() / (float)rDisp.width();
						float scaleY = (float)rBlit.height() / (float)rDisp.height();

						mHoverTipArea.set(
							VDRoundToInt(lineRect.left * scaleX + rBlit.left),
							VDRoundToInt(lineRect.top * scaleY + rBlit.top),
							VDRoundToInt(lineRect.right * scaleX + rBlit.left),
							VDRoundToInt(lineRect.bottom * scaleY + rBlit.top));

						g_sim.GetUIRenderer()->SetHoverTip(x, y, msg.c_str());
						mbHoverTipActive = true;
						valid = true;
					}
				}
			} else if (!g_sim.IsRunning() && ATIsDebugConsoleActive()) {
				mbCoordIndicatorEnabled = true;
				CaptureCursor();
				SetCoordinateIndicator(x, y);
				SetCursorImage(kATUICursorImage_Cross);
			} else if (MapPixelToBeamPosition(x, y, xc, yc, false)) {
				// attempt to copy out text
				auto [xmode, ymode] = GetModeLineXYPos(xc, yc, true);

				if (ymode >= 0) {
					uint8 data[49];

					int actual = ReadText(data, ymode, 0, 48);
					data[actual] = 0;

					char cdata[49];
					std::transform(std::begin(data), std::end(data), std::begin(cdata), [](uint8 c) { return (char)(c & 0x7F); });

					const VDStringW& msg = GetTipMessage(cdata, xmode);

					if (!msg.empty()) {
						int xp1, xp2, yp1, yp2;
						MapBeamPositionToPoint(0, ymode, xp1, yp1);

						ATAnticEmulator& antic = g_sim.GetAntic();
						const ATAnticEmulator::DLHistoryEntry *dlhist = antic.GetDLHistory();
						while(++ymode < 248 && !dlhist[ymode].mbValid)
							;

						MapBeamPositionToPoint(228, ymode, xp2, yp2);

						mHoverTipArea.set(xp1, yp1, xp2, yp2);

						g_sim.GetUIRenderer()->SetHoverTip(x, y, msg.c_str());
						mbHoverTipActive = true;
						valid = true;
					}
				}
			}

			if (!valid)
				ClearHoverTip();
		} else {
			// double-click on the left 10% of the screen opens the side panel
			if (dblclk) {
				if (x < GetArea().width() / 10) {
					mbOpenSidePanelDeferred = true;
					return;
				}
			}

			if (mpAltVideoOutput) {
				mbDragActive = GetAltDisplayArea().contains(vdpoint32(x, y));

				if (mbDragActive) {
					mDragAnchorX = x;
					mDragAnchorY = y;
				}
			} else {
				mbDragActive = MapPixelToBeamPosition(x, y, mDragAnchorX, mDragAnchorY, true)
					&& GetModeLineYPos(mDragAnchorY, true) >= 0;
			}

			mDragStartTime = VDGetCurrentTick();
			
			if (mbDragActive) {
				// We specifically don't clear the drag preview here as that would make it
				// impossible to use Copy from the context menu with touch.
				mbDragInitial = true;
				mbMouseHidden = false;
				CaptureCursor();
			}
		}
	}
}

void ATUIVideoDisplayWindow::OnMouseUp(sint32 x, sint32 y, uint32 vk) {
	if (vk == kATUIVK_LButton) {
		ClearCoordinateIndicator();
		ClearHoverTip();

		if (mbCoordIndicatorEnabled) {
			mbCoordIndicatorEnabled = false;
			ReleaseCursor();
		}

		if (mbDragActive) {
			mbDragActive = false;

			if (VDGetCurrentTick() - mDragStartTime < 250)
				mbDragInitial = false;

			ReleaseCursor();
			UpdateDragPreview(x, y);
			return;
		} else if (!mDragPreviewSpans.empty()) {
			if (VDGetCurrentTick() - mDragStartTime < 250)
				ClearDragPreview();
		}

		if (mbOpenSidePanelDeferred) {
			mbOpenSidePanelDeferred = false;
			OpenSidePanel();
			return;
		}
	}

	ATInputManager *im = g_sim.GetInputManager();

	if (g_mouseCaptured || im->IsMouseAbsoluteMode()) {
		if (im->IsMouseMapped()) {
			switch(vk) {
				case kATUIVK_LButton:
					im->OnButtonUp(0, kATInputCode_MouseLMB);
					break;
				case kATUIVK_RButton:
					im->OnButtonUp(0, kATInputCode_MouseRMB);
					break;
				case kATUIVK_MButton:
					im->OnButtonUp(0, kATInputCode_MouseMMB);
					break;
				case kATUIVK_XButton1:
					im->OnButtonUp(0, kATInputCode_MouseX1B);
					break;
				case kATUIVK_XButton2:
					im->OnButtonUp(0, kATInputCode_MouseX2B);
					break;
			}

			// Eat the message to prevent a context menu.
			return;
		}
	}

	if (vk == kATUIVK_RButton) {
		if (mpOnAllowContextMenu)
			mpOnAllowContextMenu();
	}
}

void ATUIVideoDisplayWindow::OnMouseRelativeMove(sint32 dx, sint32 dy) {
	ATInputManager *im = g_sim.GetInputManager();

	im->OnMouseMove(0, dx, dy);
	SetCursorImage(kATUICursorImage_Hidden);
}

void ATUIVideoDisplayWindow::OnMouseMove(sint32 x, sint32 y) {
	// MPC-HC sometimes injects mouse moves in order to prevent the screen from
	// going to sleep. We need to filter out these moves to prevent the cursor
	// from blinking.
	if (mbMouseHidden) {
		if (mMouseHideX == x && mMouseHideY == y)
			return;

		mbMouseHidden = false;
	}

	// If we have already entered a selection drag, it has highest priority.
	if (mbDragActive) {
		SetCursorImage(kATUICursorImage_IBeam);
		UpdateDragPreview(x, y);
		return;
	}

	// Check if we're stopped and should do debug queries.
	if (mbCoordIndicatorEnabled) {
		SetCoordinateIndicator(x, y);
		SetCursorImage(kATUICursorImage_Cross);
		return;
	}

	SetCursorImage(ComputeCursorImage(vdpoint32(x, y)));

	auto *pIM = g_sim.GetInputManager();
	if ((g_mouseCaptured || !g_mouseAutoCapture) && pIM->IsMouseAbsoluteMode()) {
		UpdateMousePosition(x, y);
	} else if (mbHoverTipActive) {
		if (!mHoverTipArea.contains(vdpoint32(x, y))) {
			ClearHoverTip();
		}
	}
}

void ATUIVideoDisplayWindow::OnMouseLeave() {
	ClearCoordinateIndicator();
	mbCoordIndicatorEnabled = false;

	ClearHoverTip();

	mbOpenSidePanelDeferred = false;
}

void ATUIVideoDisplayWindow::OnMouseHover(sint32 x, sint32 y) {
	if (g_mouseCaptured)
		return;

	if (!mpManager->IsKeyDown(kATUIVK_Alt)) {
		SetCursorImage(kATUICursorImage_Hidden);
		mbMouseHidden = true;
		mMouseHideX = x;
		mMouseHideY = y;
	}
}

bool ATUIVideoDisplayWindow::OnContextMenu(const vdpoint32 *pt) {
	// For now we do a bit of a hack and let the top-level native display code handle this,
	// as it is too hard currently to display the menu here.
	if (mpOnDisplayContextMenu) {
		if (pt)
			mpOnDisplayContextMenu(*pt);
		else
			mpOnDisplayContextMenu(TranslateClientPtToScreenPt(vdpoint32(mClientArea.width() >> 1, mClientArea.height() >> 1)));
	}

	return true;
}

bool ATUIVideoDisplayWindow::OnKeyDown(const ATUIKeyEvent& event) {
	// Right-Alt kills capture.
	if (event.mExtendedVirtKey == kATUIVK_RAlt && g_mouseCaptured) {
		ReleaseMouse();
		return true;
	}

	// fall through so the simulator still receives the alt key, in case a key is typed
	if (ProcessKeyDown(event, !mpEnhTextEngine || mpEnhTextEngine->IsRawInputEnabled())) {
		ClearDragPreview();
		return true;
	}

	if (mpEnhTextEngine) {
		if (!mpEnhTextEngine->IsRawInputEnabled() && event.mVirtKey == kATUIVK_A + ('V'-'A') && mpManager->IsKeyDown(kATUIVK_Control) && !mpManager->IsKeyDown(kATUIVK_Shift) && !mpManager->IsKeyDown(kATUIVK_Alt)) {
			OnCommandEditPasteText();
			ClearDragPreview();
			return true;
		}

		if (mpEnhTextEngine->OnKeyDown(event.mVirtKey)) {
			ClearDragPreview();
			return true;
		}
	}

	return ATUIWidget::OnKeyDown(event);
}

bool ATUIVideoDisplayWindow::OnKeyUp(const ATUIKeyEvent& event) {
	if (ProcessKeyUp(event, !mpEnhTextEngine || mpEnhTextEngine->IsRawInputEnabled()) || (mpEnhTextEngine && mpEnhTextEngine->OnKeyUp(event.mVirtKey)))
		return true;

	return ATUIWidget::OnKeyUp(event);
}

bool ATUIVideoDisplayWindow::OnChar(const ATUICharEvent& event) {
	ClearDragPreview();

	if (mpEnhTextEngine && !mpEnhTextEngine->IsRawInputEnabled()) {
		if (mpEnhTextEngine)
			mpEnhTextEngine->OnChar(event.mCh);

		return true;
	}

	int code = event.mCh;
	if (code <= 0 || code > 127)
		return false;

	if (g_kbdOpts.mbRawKeys) {
		uint32 ch;

		if (!event.mbIsRepeat && ATUIGetScanCodeForCharacter(code, ch)) {
			if (ch >= 0x100)
				ProcessVirtKey(0, event.mScanCode, ch, false);
			else if (mbHoldKeys)
				ToggleHeldKey((uint8)ch);
			else {
				mbShiftToggledPostKeyDown = false;

				auto it = std::find_if(mActiveKeys.begin(), mActiveKeys.end(), [=](const ActiveKey& ak) { return ak.mVkey == 0 && ak.mNativeScanCode == event.mScanCode; });
				if (it != mActiveKeys.end())
					it->mScanCode = ch;
				else
					mActiveKeys.push_back(ActiveKey { 0, event.mScanCode, (uint8)ch });

				UpdateCtrlShiftState();
				g_sim.GetPokey().PushRawKey((uint8)ch, !g_kbdOpts.mbFullRawKeys);
			}
		}
	} else {
		uint32 ch;

		if (ATUIGetScanCodeForCharacter(event.mCh, ch)) {
			if (ch >= 0x100)
				ProcessVirtKey(0, event.mScanCode, ch, false);
			else
				g_sim.GetPokey().PushKey(ch, event.mbIsRepeat, false, true, true);
		}
	}

	return true;
}

bool ATUIVideoDisplayWindow::OnCharUp(const ATUICharEvent& event) {
	auto it = std::find_if(mActiveKeys.begin(), mActiveKeys.end(), [=](const ActiveKey& ak) { return ak.mVkey == 0 && ak.mNativeScanCode == event.mScanCode; });
	if (it != mActiveKeys.end()) {
		g_sim.GetPokey().ReleaseRawKey(it->mScanCode, !g_kbdOpts.mbFullRawKeys);
		*it = mActiveKeys.back();
		mActiveKeys.pop_back();
		UpdateCtrlShiftState();
	}

	for(uint32 i=0; i<(uint32)vdcountof(mActiveSpecialVKeys); ++i) {
		if (mActiveSpecialVKeys[i] == 0 && mActiveSpecialScanCodes[i] == event.mScanCode) {
			mActiveSpecialVKeys[i] = 0;
			mActiveSpecialScanCodes[i] = 0;

			ProcessSpecialKey(kATUIKeyScanCodeFirst + i, false);
			break;
		}
	}

	return true;
}

void ATUIVideoDisplayWindow::OnForceKeysUp() {
	auto& pokey = g_sim.GetPokey();
	pokey.SetShiftKeyState(false, !g_kbdOpts.mbFullRawKeys);
	pokey.SetControlKeyState(false);
	pokey.ReleaseAllRawKeys(!g_kbdOpts.mbFullRawKeys);

	mbShiftDepressed = false;
	mbShiftToggledPostKeyDown = false;
	mActiveKeys.clear();
	UpdateCtrlShiftState();

	g_sim.GetGTIA().SetConsoleSwitch(0x07, false);
}

void ATUIVideoDisplayWindow::OnActionStart(uint32 id) {
	switch(id) {
		case kActionOpenSidePanel:
			OpenSidePanel();
			break;

		case kActionOpenOSK:
			OpenOSK();
			break;

		default:
			return ATUIContainer::OnActionStart(id);
	}
}

void ATUIVideoDisplayWindow::OnActionStop(uint32 id) {
	switch(id) {
		case kActionCloseOSK:
			CloseOSK();
			break;
	}
}

void ATUIVideoDisplayWindow::OnCreate() {
	ATUIContainer::OnCreate();

	BindAction(kATUIVK_UIMenu, kActionOpenSidePanel);
	BindAction(kATUIVK_UIOption, kActionOpenOSK);
	BindAction(kATUIVK_UIReject, kActionCloseOSK);

	mpUILabelBadSignal = new ATUILabel;
	mpUILabelBadSignal->AddRef();
	mpUILabelBadSignal->SetVisible(true);
	mpUILabelBadSignal->SetFont(mpManager->GetThemeFont(kATUIThemeFont_Default));
	mpUILabelBadSignal->SetBorderColor(0xFFFFFFFF);
	mpUILabelBadSignal->SetFillColor(0xFF204050);
	mpUILabelBadSignal->SetTextColor(0xFFFFFFFF);
	mpUILabelBadSignal->SetTextOffset(8, 8);

	vdrefptr<IATUIAnchor> anchor;
	ATUICreateTranslationAnchor(0.5f, 0.5f, ~anchor);
	mpUILabelBadSignal->SetAnchor(anchor);

	AddChild(mpUILabelBadSignal);
}

void ATUIVideoDisplayWindow::OnDestroy() {
	UnbindAllActions();

	vdsaferelease <<= mpUILabelBadSignal;
	vdsaferelease <<= mpOSK;
	vdsaferelease <<= mpOSKPanel;
	vdsaferelease <<= mpSidePanel;

	ATUIContainer::OnDestroy();
}

void ATUIVideoDisplayWindow::OnSize() {
	const vdsize32& csz = mClientArea.size();

	if (mpOSK) {
		mpOSK->AutoSize();

		const vdsize32& osksz = mpOSK->GetArea().size();
		mpOSK->SetPosition(vdpoint32((csz.w - osksz.w) >> 1, 0));

		mpOSKPanel->SetArea(vdrect32(0, csz.h - osksz.h, csz.w, csz.h));
	}

	UpdateEnhTextSize();

	ATUIContainer::OnSize();
}

void ATUIVideoDisplayWindow::OnSetFocus() {
	g_sim.GetInputManager()->SetRestrictedMode(false);

	CloseSidePanel();
}

void ATUIVideoDisplayWindow::OnKillFocus() {
	g_sim.GetInputManager()->SetRestrictedMode(true);

	if (mbDragActive) {
		mbDragActive = false;

		ReleaseCursor();
	}

	OnForceKeysUp();
}

void ATUIVideoDisplayWindow::OnDeactivate() {
	ATInputManager *im = g_sim.GetInputManager();
	im->ReleaseButtons(0, kATInputCode_JoyClass-1);

	OnForceKeysUp();
}

void ATUIVideoDisplayWindow::OnCaptureLost() {
	mbDragActive = false;
	g_mouseCaptured = false;
	g_mouseClipped = false;
	g_winCaptionUpdater->SetMouseCaptured(false, false);
}

ATUIDragEffect ATUIVideoDisplayWindow::OnDragEnter(sint32 x, sint32 y, ATUIDragModifiers modifiers, IATUIDragDropObject *obj) {
	CreateDropTargetOverlays();
	return OnDragOver(x, y, modifiers, obj);
}

ATUIDragEffect ATUIVideoDisplayWindow::OnDragOver(sint32 x, sint32 y, ATUIDragModifiers modifiers, IATUIDragDropObject *obj) {
	int idx = FindDropTargetOverlay(x, y);

	HighlightDropTargetOverlay(idx);

	if (idx >= 0) {
		switch(kDropFileTargets[idx]) {
			case ATUIDropFilesTarget::BootImage:
				obj->SetDropDescription(ATUIDropIconType::Copy, L"Boot image", nullptr);
				break;
			case ATUIDropFilesTarget::MountImage:
				obj->SetDropDescription(ATUIDropIconType::Copy, L"Mount image", nullptr);
				break;
			case ATUIDropFilesTarget::MountCart:
				obj->SetDropDescription(ATUIDropIconType::Copy, L"Mount as cartridge", nullptr);
				break;
			case ATUIDropFilesTarget::MountDisk1:
				obj->SetDropDescription(ATUIDropIconType::Copy, L"Mount as disk %1", L"D1:");
				break;
			case ATUIDropFilesTarget::MountDisk2:
				obj->SetDropDescription(ATUIDropIconType::Copy, L"Mount as disk %1", L"D2:");
				break;
			case ATUIDropFilesTarget::MountDisk3:
				obj->SetDropDescription(ATUIDropIconType::Copy, L"Mount as disk %1", L"D3:");
				break;
			case ATUIDropFilesTarget::MountDisk4:
				obj->SetDropDescription(ATUIDropIconType::Copy, L"Mount as disk %1", L"D4:");
				break;
		}
	} else {
		obj->ClearDropDescription();
	}

	return idx >= 0 ? ATUIDragEffect::Copy : ATUIDragEffect::None;
}

void ATUIVideoDisplayWindow::OnDragLeave() {
	DestroyDropTargetOverlays();
}

ATUIDragEffect ATUIVideoDisplayWindow::OnDragDrop(sint32 x, sint32 y, ATUIDragModifiers modifiers, IATUIDragDropObject *obj) {
	int idx = FindDropTargetOverlay(x, y);

	HighlightDropTargetOverlay(idx);

	if (idx < 0)
		return ATUIDragEffect::None;

	ATUIDropFilesTarget target = kDropFileTargets[idx];

	ATUIDropFiles(TranslateClientPtToScreenPt(vdpoint32(x, y)), target, obj);

	return ATUIDragEffect::Copy;
}

void ATUIVideoDisplayWindow::Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h) {
	if (mpAltVideoOutput) {
		mpAltVideoOutput->UpdateFrame();

		const auto& vi = mpAltVideoOutput->GetVideoInfo();
		uint32 lcc = vi.mFrameBufferLayoutChangeCount;
		if (mAltVOLayoutChangeCount != lcc) {
			mAltVOLayoutChangeCount = lcc;
			mAltVOImageView.SetImage(mpAltVideoOutput->GetFrameBuffer(), true);
		}

		uint32 cc = vi.mFrameBufferChangeCount;
		if (cc != mAltVOChangeCount) {
			mAltVOChangeCount = cc;
			mAltVOImageView.Invalidate();
		}

		if (!vi.mbSignalValid) {
			rdr.SetColorRGB(0);
			rdr.FillRect(0, 0, w, h);

			mpUILabelBadSignal->SetVisible(true);
			mpUILabelBadSignal->SetTextAlign(ATUILabel::kAlignCenter);
			mpUILabelBadSignal->SetTextF(L"Unsupported video mode\n%.3fKHz, %.1fHz", vi.mHorizScanRate / 1000.0f, vi.mVertScanRate);
			mpUILabelBadSignal->AutoSize();
			mpUILabelBadSignal->SetArea(mpUILabelBadSignal->GetAnchor()->Position(vdrect32(0, 0, w, h), mpUILabelBadSignal->GetArea().size()));
		} else {
			const vdrect32& dst = GetAltDisplayArea();

			if (dst.left > 0 || dst.top > 0 || dst.right < w || dst.bottom < h) {
				rdr.SetColorRGB(0);
				rdr.FillRect(0, 0, w, h);
			}

			mpUILabelBadSignal->SetVisible(false);

			VDDisplayBltOptions opts = {};

			switch(g_dispFilterMode) {
				case kATDisplayFilterMode_Point:
					opts.mFilterMode = VDDisplayBltOptions::kFilterMode_Point;
					break;

				case kATDisplayFilterMode_AnySuitable:
				case kATDisplayFilterMode_Bilinear:
				case kATDisplayFilterMode_Bicubic:
					opts.mFilterMode = VDDisplayBltOptions::kFilterMode_Bilinear;
					break;

				case kATDisplayFilterMode_SharpBilinear:
					opts.mFilterMode = VDDisplayBltOptions::kFilterMode_Bilinear;

					{
						static const float kFactors[5] = { 1.259f, 1.587f, 2.0f, 2.520f, 3.175f };

						const float factor = kFactors[std::max(0, std::min(4, g_dispFilterSharpness + 2))];

						opts.mSharpnessX = std::max(1.0f, factor / 2.0f);
						opts.mSharpnessY = std::max(1.0f, factor);
					}
					break;
			}

			const vdrect32& src = vi.mDisplayArea;

			if (vi.mBorderColor) {
				rdr.SetColorRGB(vi.mBorderColor);

				if (dst.top > 0)
					rdr.FillRect(0, 0, w, dst.top);

				if (dst.left > 0)
					rdr.FillRect(0, dst.top, dst.left, dst.height());

				if (dst.right < w)
					rdr.FillRect(dst.right, dst.top, w - dst.right, dst.height());

				if (dst.bottom < h)
					rdr.FillRect(0, 0, w, h);
			}

			rdr.StretchBlt(dst.left, dst.top, dst.width(), dst.height(), mAltVOImageView, src.left, src.top, src.width(), src.height(), opts);
		}
	} else
		mpUILabelBadSignal->SetVisible(false);

	if (rdr.GetCaps().mbSupportsAlphaBlending) {
		vdfastvector<vdfloat2> pts;

		for (const HighlightPoint& xr : mHighlightPoints) {
			pts.push_back(vdfloat2{(float)xr.mX, (float)xr.mY});

			if (xr.mbClose) {
				rdr.AlphaTriStrip(pts.data(), pts.size(), 0x8000A0FF);
				pts.clear();
			}
		}
	} else {
		rdr.SetColorRGB(0x00A0FF);

		// Convert the tristrips to polygon outlines. For each tristrip, we need to separate the
		// even and odd points and place the odd points reversed after the even points.
		vdfastvector<vdpoint32> pts[2];
		bool odd = false;

		for (const HighlightPoint& xr : mHighlightPoints) {
			const vdpoint32 pt(xr.mX, xr.mY);

			pts[odd].push_back(pt);
			odd = !odd;

			if (xr.mbClose) {
				for(auto it = pts[1].rbegin(), itEnd = pts[1].rend(); it != itEnd; ++it)
					pts[0].push_back(*it);

				pts[0].push_back(pts[0].front());

				rdr.PolyLine(pts[0].data(), pts[0].size() - 1);

				pts[0].clear();
				pts[1].clear();
			}
		}
	}

	ATUIContainer::Paint(rdr, w, h);
}

void ATUIVideoDisplayWindow::UpdateAltDisplay() {
	IATDeviceVideoOutput *prev = mpAltVideoOutput;

	if (mpEnhTextEngine)
		mpAltVideoOutput = mpEnhTextEngine->GetVideoOutput();
	else if (mpXEP && g_xepViewEnabled)
		mpAltVideoOutput = mpXEP;
	else
		mpAltVideoOutput = nullptr;

	if (mpAltVideoOutput && prev != mpAltVideoOutput) {
		const auto& vi = mpAltVideoOutput->GetVideoInfo();

		// do force a change event next frame
		mAltVOChangeCount = vi.mFrameBufferChangeCount - 1;
		mAltVOLayoutChangeCount = vi.mFrameBufferLayoutChangeCount - 1;
	}
}

bool ATUIVideoDisplayWindow::ProcessKeyDown(const ATUIKeyEvent& event, bool enableKeyInput) {
	ATInputManager *im = g_sim.GetInputManager();

	const int key = event.mVirtKey;
	const bool alt = mpManager->IsKeyDown(kATUIVK_Alt);

	if (!alt) {
		const int inputCode = event.mExtendedVirtKey;

		if (im->IsInputMapped(0, inputCode)) {
			im->OnButtonDown(0, inputCode);

			if (!g_kbdOpts.mbAllowInputMapOverlap)
				return true;
		}
	}

	const bool isRepeat = event.mbIsRepeat;
	const bool shift = mpManager->IsKeyDown(kATUIVK_Shift);
	const bool ctrl = mpManager->IsKeyDown(kATUIVK_Control);
	const uint8 ctrlmod = (ctrl ? 0x80 : 0x00);
	const uint8 shiftmod = (shift ? 0x40 : 0x00);

	const uint8 modifier = ctrlmod + shiftmod;
	const bool ext = event.mbIsExtendedKey;
	if (ATUIActivateVirtKeyMapping(key, alt, ctrl, shift, ext, false, kATUIAccelContext_Display)) {
		return true;
	} else {
		uint32 scanCode;
		if (ATUIGetScanCodeForVirtualKey(key, alt, ctrl, shift, ext, scanCode)) {
			if (!enableKeyInput)
				return false;

			ProcessVirtKey(key, 0, scanCode, isRepeat);
			return true;
		}

		switch(key) {
			case kATUIVK_Shift:
				if (!mbShiftDepressed) {
					mbShiftDepressed = true;
					mbShiftToggledPostKeyDown = true;
					UpdateCtrlShiftState();
				}
				break;
		}
	}

	return false;
}

bool ATUIVideoDisplayWindow::ProcessKeyUp(const ATUIKeyEvent& event, bool enableKeyInput) {
	ATInputManager *im = g_sim.GetInputManager();

	const int key = event.mVirtKey;
	const bool alt = mpManager->IsKeyDown(kATUIVK_Alt);

	if (!alt) {
		int inputCode = event.mExtendedVirtKey;

		if (im->IsInputMapped(0, inputCode)) {
			im->OnButtonUp(0, inputCode);

			if (!g_kbdOpts.mbAllowInputMapOverlap)
				return true;
		}
	}

	auto it = std::find_if(mActiveKeys.begin(), mActiveKeys.end(), [=](const ActiveKey& ak) { return ak.mVkey == key && ak.mNativeScanCode == 0; });
	if (it != mActiveKeys.end()) {
		g_sim.GetPokey().ReleaseRawKey(it->mScanCode, !g_kbdOpts.mbFullRawKeys);
		*it = mActiveKeys.back();
		mActiveKeys.pop_back();
		UpdateCtrlShiftState();
	}

	for(uint32 i=0; i<(uint32)vdcountof(mActiveSpecialVKeys); ++i) {
		if (mActiveSpecialVKeys[i] == key) {
			mActiveSpecialVKeys[i] = 0;
			mActiveSpecialScanCodes[i] = 0;

			ProcessSpecialKey(kATUIKeyScanCodeFirst + i, false);
			return true;
		}
	}

	const bool shift = mpManager->IsKeyDown(kATUIVK_Shift);
	const bool ctrl = mpManager->IsKeyDown(kATUIVK_Control);
	const bool ext = event.mbIsExtendedKey;

	if (!ATUIActivateVirtKeyMapping(key, alt, ctrl, shift, ext, true, kATUIAccelContext_Display)) {
		switch(key) {
			case kATUIVK_Shift:
				if (mbShiftDepressed) {
					mbShiftDepressed = false;
					mbShiftToggledPostKeyDown = true;
					UpdateCtrlShiftState();
				}
				break;
		}
	}

	return false;
}

void ATUIVideoDisplayWindow::ProcessVirtKey(uint32 vkey, uint32 scancode, uint32 keycode, bool repeat) {
	if (keycode >= kATUIKeyScanCodeFirst && keycode <= kATUIKeyScanCodeLast) {
		if (!repeat) {
			ProcessSpecialKey(keycode, true);

			mActiveSpecialVKeys[keycode - kATUIKeyScanCodeFirst] = vkey;
			mActiveSpecialScanCodes[keycode - kATUIKeyScanCodeFirst] = scancode;
		}
	} else if (mbHoldKeys) {
		if (!repeat)
			ToggleHeldKey((uint8)keycode);
	} else {
		if (g_kbdOpts.mbRawKeys) {
			if (!repeat) {
				mbShiftToggledPostKeyDown = false;

				auto it = std::find_if(mActiveKeys.begin(), mActiveKeys.end(), [=](const ActiveKey& ak) { return ak.mVkey == vkey && ak.mNativeScanCode == scancode; });
				if (it != mActiveKeys.end())
					it->mScanCode = keycode;
				else
					mActiveKeys.push_back(ActiveKey { vkey, scancode, (uint8)keycode });

				UpdateCtrlShiftState();
				g_sim.GetPokey().PushRawKey(keycode, !g_kbdOpts.mbFullRawKeys);
			}
		} else
			g_sim.GetPokey().PushKey(keycode, repeat);
	}
}

void ATUIVideoDisplayWindow::ProcessSpecialKey(uint32 scanCode, bool state) {
	switch(scanCode) {
		case kATUIKeyScanCode_Start:
			if (mbHoldKeys) {
				if (state)
					ToggleHeldConsoleButton(0x01);
			} else
				g_sim.GetGTIA().SetConsoleSwitch(0x01, state);
			break;
		case kATUIKeyScanCode_Select:
			if (mbHoldKeys) {
				if (state)
					ToggleHeldConsoleButton(0x02);
			} else
				g_sim.GetGTIA().SetConsoleSwitch(0x02, state);
			break;
		case kATUIKeyScanCode_Option:
			if (mbHoldKeys) {
				if (state)
					ToggleHeldConsoleButton(0x04);
			} else
				g_sim.GetGTIA().SetConsoleSwitch(0x04, state);
			break;
		case kATUIKeyScanCode_Break:
			g_sim.GetPokey().SetBreakKeyState(state, !g_kbdOpts.mbFullRawKeys);
			break;
	}
}

void ATUIVideoDisplayWindow::ToggleHeldKey(uint8 keycode) {
	if (g_sim.GetPendingHeldKey() == keycode)
		g_sim.ClearPendingHeldKey();
	else
		g_sim.SetPendingHeldKey(keycode);
}

void ATUIVideoDisplayWindow::ToggleHeldConsoleButton(uint8 encoding) {
	uint8 buttons = g_sim.GetPendingHeldSwitches();

	g_sim.SetPendingHeldSwitches(buttons ^ encoding);
}

void ATUIVideoDisplayWindow::UpdateCtrlShiftState() {
	uint8 c = 0;
	
	// It is possible for us to have a conflict where a key mapping has been
	// created with the Shift or Ctrl key to a key that doesn't require Shift or Ctrl
	// on the Atari keyboard. Therefore, we override the Shift key state on the host
	// with whatever is required by the scan code, unless the Shift key has been
	// toggled after the initial key down (see below).
	if (mActiveKeys.empty()) {
		if (mbShiftDepressed)
			c = 0x40;
	} else {
		for(const auto& ak : mActiveKeys)
			c |= ak.mScanCode;
	}

	auto& pokey = g_sim.GetPokey();
	pokey.SetControlKeyState((c & 0x80) != 0);

	// If the Shift key is toggled after a key down, force the Shift key state to
	// the host Shift key. This will be consistent except in the case where the
	// key down has a Shift key mismatch between the host keys, which is an ambiguous
	// case anyway. When there is no such mismatch, this makes Shift key up/down after
	// non-Shift key up/down work as expected.
	if (g_kbdOpts.mbRawKeys && !mbShiftToggledPostKeyDown) {
		pokey.SetShiftKeyState((c & 0x40) != 0, !g_kbdOpts.mbFullRawKeys);
	} else
		pokey.SetShiftKeyState(mbShiftDepressed, true);
}

uint32 ATUIVideoDisplayWindow::ComputeCursorImage(const vdpoint32& pt) const {
	bool validBeamPosition;
	
	if (mpAltVideoOutput) {
		validBeamPosition = GetAltDisplayArea().contains(pt);
	} else {
		int xs, ys;
		validBeamPosition = (MapPixelToBeamPosition(pt.x, pt.y, xs, ys, false) && GetModeLineYPos(ys, true) >= 0);
	}

	bool cursorSet = false;

	auto *pIM = g_sim.GetInputManager();
	if (!g_mouseCaptured && validBeamPosition && mpManager->IsKeyDown(kATUIVK_Alt))
		return kATUICursorImage_Query;

	if (g_sim.GetInputManager()->IsMouseMapped()) {
		cursorSet = true;

		if (g_mouseAutoCapture && !g_mouseCaptured) {

			// Auto-capture is on but we have not captured the cursor. In this case we show the
			// arrow. We don't show the I-Beam because left-click selection functionality is
			// overridden by the mouse auto-capture.

			return kATUICursorImage_Arrow;

		} else if (pIM->IsMouseAbsoluteMode()) {

			// We're in absolute mode, and either the mouse is captured or auto-capture is off.
			// In this case we will be passing absolute mouse inputs to the input manager. Show
			// the target or off-target depending on whether the target is active (light pen
			// aimed at screen or otherwise not a light pen/gun).

			if (g_sim.GetInputManager()->IsMouseActiveTarget())
				return kATUICursorImage_Target;
			else
				return kATUICursorImage_TargetOff;

		} else if (IsCursorCaptured()) {

			// We're in relative mode and the cursor is captured. We need to hide the mouse
			// cursor so you don't see the jitters.

			return kATUICursorImage_Hidden;

		}

		// The mouse is bound in relative mode but not captured, so we should fall through.
	}

	if (validBeamPosition)
		return kATUICursorImage_IBeam;

	return kATUICursorImage_Arrow;
}

void ATUIVideoDisplayWindow::UpdateMousePosition(int x, int y) {
	int padX = 0;
	int padY = 0;

	const vdsize32 size = mClientArea.size();

	if (size.w)
		padX = VDRoundToInt(x * 131072.0f / ((float)size.w - 1) - 0x10000);

	if (size.h)
		padY = VDRoundToInt(y * 131072.0f / ((float)size.h - 1) - 0x10000);

	ATInputManager *im = g_sim.GetInputManager();
	im->SetMousePadPos(padX, padY);

	float xc, yc;
	if (!MapPixelToBeamPosition(x, y, xc, yc, false))
		return;

	// map cycles to normalized position (note that this must match the light pen
	// computations)
	float xn = (xc - 128.0f) * (65536.0f / 94.0f);
	float yn = (yc - 128.0f) * (65536.0f / 188.0f);

	im->SetMouseBeamPos(VDRoundToInt(xn), VDRoundToInt(yn));
}

const vdrect32 ATUIVideoDisplayWindow::GetAltDisplayArea() const {
	if (!mpAltVideoOutput)
		return vdrect32(0, 0, 0, 0);

	const auto& vi = mpAltVideoOutput->GetVideoInfo();
	const vdrect32& r = vi.mDisplayArea;
	const sint32 w = mArea.width();
	const sint32 h = mArea.height();
	sint32 dw = w;
	sint32 dh = h;

	if (mpAltVideoOutput != mpXEP) {
		double ratio = std::min<double>(1, std::min<double>((double)dw / (double)r.width(), (double)dh / (double)r.height()));

		dw = VDRoundToInt32((double)r.width() * ratio);
		dh = VDRoundToInt32((double)r.height() * ratio);
	} else if (g_displayStretchMode != kATDisplayStretchMode_Unconstrained) {
		double par = 0.5;
		
		switch(g_displayStretchMode) {
			case kATDisplayStretchMode_SquarePixels:
			case kATDisplayStretchMode_Integral:
				break;

			default:
				par = vi.mPixelAspectRatio;
				break;
		}

		const double fitw = (double)r.width() * par;
		const double fith = (double)r.height();
		double scale = std::min<double>(w / fitw, h / fith);

		switch(g_displayStretchMode) {
			case kATDisplayStretchMode_Integral:
			case kATDisplayStretchMode_IntegralPreserveAspectRatio:
				if (scale > 1.0)
					scale = floor(scale);
				break;
		}

		dw = VDRoundToInt32(fitw * scale);
		dh = VDRoundToInt32(fith * scale);
	}

	const int dx = (w - dw) >> 1;
	const int dy = (h - dh) >> 1;
	return vdrect32(dx, dy, dx + dw, dy + dh);
}

bool ATUIVideoDisplayWindow::MapPixelToBeamPosition(int x, int y, float& hcyc, float& vcyc, bool clamp) const {
	if (!mDisplayRect.contains(vdpoint32(x, y))) {
		if (!clamp)
			return false;

		if (mDisplayRect.empty())
			return false;
	}

	x -= mDisplayRect.left;
	y -= mDisplayRect.top;

	if (clamp) {
		x = std::clamp(x, 0, mDisplayRect.width());
		y = std::clamp(y, 0, mDisplayRect.height());
	}

	const vdfloat2 displayRectSize {(float)mDisplayRect.width(), (float)mDisplayRect.height()};
	vdfloat2 pt = (vdfloat2{(float)x, (float)y} + vdfloat2{0.5f, 0.5f}) / displayRectSize;

	if (mpMapDisplayToSourcePt && !mpMapDisplayToSourcePt(pt) && !clamp)
		return false;

	pt *= displayRectSize;
	x = VDFloorToInt(pt.x);
	y = VDFloorToInt(pt.y);

	ATGTIAEmulator& gtia = g_sim.GetGTIA();
	const vdrect32 scanArea(gtia.GetFrameScanArea());

	// map position to cycles
	hcyc = (float)scanArea.left + ((float)x + 0.5f) * (float)scanArea.width()  / (float)mDisplayRect.width()  - 0.5f;
	vcyc = (float)scanArea.top  + ((float)y + 0.5f) * (float)scanArea.height() / (float)mDisplayRect.height() - 0.5f;
	return true;
}

bool ATUIVideoDisplayWindow::MapPixelToBeamPosition(int x, int y, int& xc, int& yc, bool clamp) const {
	float xf, yf;

	if (!MapPixelToBeamPosition(x, y, xf, yf, clamp))
		return false;

	xc = (int)floorf(xf + 0.5f);
	yc = (int)floorf(yf + 0.5f);
	return true;
}

// Map a beam position in (half cycles, scan) to a screen point. This maps points at the corners of
// pixels instead of the centers since it is used for rectangle/polygon mapping.
void ATUIVideoDisplayWindow::MapBeamPositionToPoint(int xc, int yc, int& x, int& y) const {
	const vdfloat2 pt = MapBeamPositionToPointF(vdfloat2 {(float)xc, (float)yc});

	x = VDRoundToInt(pt.x);
	y = VDRoundToInt(pt.y);
}

vdfloat2 ATUIVideoDisplayWindow::MapBeamPositionToPointF(vdfloat2 pt) const {
	ATGTIAEmulator& gtia = g_sim.GetGTIA();
	const vdrect32 scanArea(gtia.GetFrameScanArea());

	// map position to cycles
	pt = pt - vdfloat2{(float)scanArea.left, (float)scanArea.top};

	pt /= vdfloat2 { (float)scanArea.width(), (float)scanArea.height() };

	if (mpMapSourceToDisplayPt)
		mpMapSourceToDisplayPt(pt);

	const vdfloat2 drPos { (float)mDisplayRect.left, (float)mDisplayRect.top };
	const vdfloat2 drSize { (float)mDisplayRect.width(), (float)mDisplayRect.height() };
	return pt * drSize + drPos;
}

void ATUIVideoDisplayWindow::UpdateDragPreview(int x, int y) {
	if (mpAltVideoOutput)
		UpdateDragPreviewAlt(x, y);
	else
		UpdateDragPreviewAntic(x, y);
}

void ATUIVideoDisplayWindow::UpdateDragPreviewAlt(int x, int y) {
	const auto& vi = mpAltVideoOutput->GetVideoInfo();
	if (!vi.mTextRows || !vi.mTextColumns)
		return;

	const vdrect32& drawArea = GetAltDisplayArea();
	if (drawArea.empty())
		return;

	const vdrect32& dispArea = vi.mDisplayArea;
	if (dispArea.empty())
		return;

	const int xepx1 = VDFloorToInt(((float)(mDragAnchorX - drawArea.left) + 0.5f) * (float)dispArea.width() / (float)drawArea.width());
	const int xepy1 = VDFloorToInt(((float)(mDragAnchorY - drawArea.top) + 0.5f) * (float)dispArea.height() / (float)drawArea.height());
	const int xepx2 = VDFloorToInt(((float)(x - drawArea.left) + 0.5f) * (float)dispArea.width() / (float)drawArea.width());
	const int xepy2 = VDFloorToInt(((float)(y - drawArea.top) + 0.5f) * (float)dispArea.height() / (float)drawArea.height());

	if (mbDragInitial) {
		if (xepx1 == xepx2 && xepy1 == xepy2)
			return;

		mbDragInitial = false;
	}

	vdpoint32 caretPos1 = mpAltVideoOutput->PixelToCaretPos(vdpoint32(xepx1, xepy1));
	vdpoint32 caretPos2 = mpAltVideoOutput->PixelToCaretPos(vdpoint32(xepx2, xepy2));

	mDragPreviewSpans.clear();

	if (caretPos1.y == caretPos2.y) {
		if (caretPos1.x == caretPos2.x) {
			UpdateDragPreviewRects();
			return;
		}

		if (caretPos1.x > caretPos2.x)
			std::swap(caretPos1, caretPos2);
	} else if (caretPos1.y > caretPos2.y)
		std::swap(caretPos1, caretPos2);

	for(int cy = caretPos1.y; cy <= caretPos2.y; ++cy) {
		TextSpan& ts = mDragPreviewSpans.push_back();

		ts.mX = (cy == caretPos1.y) ? caretPos1.x : 0;
		ts.mY = cy;
		ts.mWidth = ((cy == caretPos2.y) ? caretPos2.x : vi.mTextColumns) - ts.mX;
		ts.mHeight = 1;
		ts.mCharX = ts.mX;
		ts.mCharWidth = ts.mWidth;
	}

	UpdateDragPreviewRects();
}

void ATUIVideoDisplayWindow::UpdateDragPreviewAntic(int x, int y) {
	int xc2, yc2;

	if (!MapPixelToBeamPosition(x, y, xc2, yc2, true)) {
		ClearDragPreview();
		return;
	}

	int xc1 = mDragAnchorX;
	int yc1 = mDragAnchorY;

	if (mbDragInitial) {
		if (xc1 == xc2 && yc1 == yc2)
			return;

		mbDragInitial = false;
	}

	yc1 = GetModeLineYPos(yc1, false);
	yc2 = GetModeLineYPos(yc2, false);

	if ((yc1 | yc2) < 0) {
		ClearDragPreview();
		return;
	}

	if (yc1 > yc2 || (yc1 == yc2 && xc1 > xc2)) {
		std::swap(xc1, xc2);
		std::swap(yc1, yc2);
	}

	ATAnticEmulator& antic = g_sim.GetAntic();
	const ATAnticEmulator::DLHistoryEntry *dlhist = antic.GetDLHistory();

	ATGTIAEmulator& gtia = g_sim.GetGTIA();
	const vdrect32 scanArea(gtia.GetFrameScanArea());

	mDragPreviewSpans.clear();

	for(int yc = yc1; yc <= yc2; ++yc) {
		if (!dlhist[yc].mbValid)
			continue;

		bool textModeLine = false;
		switch(dlhist[yc].mControl & 15) {
			case 2:
			case 3:
			case 6:
			case 7:
				textModeLine = true;
				break;
		}

		if (!textModeLine)
			continue;

		int pfwidth = dlhist[yc].mDMACTL & 3;
		if (!pfwidth)
			continue;

		if (pfwidth < 3 && (dlhist[yc].mControl & 0x10))
			++pfwidth;

		int left = (yc == yc1) ? xc1 : scanArea.left;
		int right = (yc == yc2) ? xc2 : scanArea.right;

		const int leftborder = 0x50 - 0x10*pfwidth;
		left = std::max<int>(left, leftborder);
		right = std::min<int>(right, 0xB0 + 0x10*pfwidth);

		bool dblwide = false;
		switch(dlhist[yc].mControl & 15) {
			case 2:
			case 3:
				left = (left + 2) & ~3;
				right = (right + 2) & ~3;
				break;

			case 6:
			case 7:
				left  = (left + 4) & ~7;
				right = (right + 4) & ~7;
				dblwide = true;
				break;
		}

		if (left >= right)
			continue;

		TextSpan& ts = mDragPreviewSpans.push_back();
		ts.mX = left;
		ts.mWidth = right - left;
		ts.mY = yc;
		ts.mHeight = 0;

		if (dblwide) {
			ts.mCharX = (left - leftborder) >> 3;
			ts.mCharWidth = (right - left) >> 3;
		} else {
			ts.mCharX = (left - leftborder) >> 2;
			ts.mCharWidth = (right - left) >> 2;
		}

		for(int i=0; i<16; ++i) {
			++ts.mHeight;

			if (yc + ts.mHeight >= 248 || dlhist[yc + ts.mHeight].mbValid)
				break;
		}
	}

	UpdateDragPreviewRects();
}

void ATUIVideoDisplayWindow::UpdateDragPreviewRects() {
	ClearHighlights();

	if (mDragPreviewSpans.empty())
		return;

	if (mpAltVideoOutput) {
		const vdrect32& drawArea = GetAltDisplayArea();
		const auto& vi = mpAltVideoOutput->GetVideoInfo();
		const vdrect32& dispArea = vi.mDisplayArea;
		
		if (!dispArea.empty()) {
			const float scaleX = (float)drawArea.width() / (float)dispArea.width();
			const float scaleY = (float)drawArea.height() / (float)dispArea.height();

			TextSpans::const_iterator it(mDragPreviewSpans.begin()), itEnd(mDragPreviewSpans.end());	
			for(; it != itEnd; ++it) {
				const TextSpan& ts = *it;
				const vdrect32& spanArea = mpAltVideoOutput->CharToPixelRect(vdrect32(ts.mX, ts.mY, ts.mX + ts.mWidth, ts.mY + 1));

				const int x1 = VDRoundToInt(spanArea.left   * scaleX) + drawArea.left;
				const int y1 = VDRoundToInt(spanArea.top    * scaleY) + drawArea.top;
				const int x2 = VDRoundToInt(spanArea.right  * scaleX) + drawArea.left;
				const int y2 = VDRoundToInt(spanArea.bottom * scaleY) + drawArea.top;

				mHighlightPoints.push_back(HighlightPoint { x1, y1, false });
				mHighlightPoints.push_back(HighlightPoint { x1, y2, false });
				mHighlightPoints.push_back(HighlightPoint { x2, y1, false });
				mHighlightPoints.push_back(HighlightPoint { x2, y2, true });
			}
		}
	} else {
		// Due to distortion effects our rect may not stay as one when mapped from beam position to screen.
		// Worse yet, the resulting polygon may not even be convex. We can, however, expect that a horizontal
		// strip will mostly stay as such, so we can make do by checking for excessive curvature on the top and
		// bottom edges and subdivide the strip as necessary, relying on the rendering code using a horizontal
		// tri-strip.
		vdfastvector<float> ustack;

		for(const TextSpan& ts : mDragPreviewSpans) {
			// discard empty spans just in case, as these will cause tesselation problems
			if (!ts.mWidth || !ts.mHeight)
				continue;

			int u1 = (float)ts.mX;
			int u2 = u1 + (float)ts.mWidth;
			const int v1 = (float)ts.mY;
			const int v2 = v1 + (float)ts.mHeight;

			for (int u = u1; u <= u2; ) {
				int x1, y1, x2, y2;
				MapBeamPositionToPoint(u, v1, x1, y1);
				MapBeamPositionToPoint(u, v2, x2, y2);

				mHighlightPoints.push_back(HighlightPoint { x1, y1, false });
				mHighlightPoints.push_back(HighlightPoint { x2, y2, false });

				if (u == u2)
					break;

				if (mpMapSourceToDisplayPt) {
					// While adaptive tesselation would be more precise and efficient, it runs into
					// problems with inconsistent tesselation between rows causing gaps or overlaps.
					// To avoid this, we uniformly tesselate on a common grid used for all highlight
					// rects.

					u = (u + 16) & ~15;
					if (u > u2)
						u = u2;
				} else {
					u = u2;
				}
			}

			mHighlightPoints.back().mbClose = true;
		}
	}

	Invalidate();
}

void ATUIVideoDisplayWindow::ClearDragPreview() {
	mDragPreviewSpans.clear();

	ClearHighlights();
}

int ATUIVideoDisplayWindow::GetModeLineYPos(int ys, bool checkValidCopyText) const {
	return GetModeLineXYPos(0, ys, checkValidCopyText).second;
}

std::pair<int, int> ATUIVideoDisplayWindow::GetModeLineXYPos(int xcc, int ys, bool checkValidCopyText) const {
	ATAnticEmulator& antic = g_sim.GetAntic();
	const ATAnticEmulator::DLHistoryEntry *dlhist = antic.GetDLHistory();

	for(int i=0; i<16; ++i, --ys) {
		if (ys >= 8 && ys < 248) {
			const ATAnticEmulator::DLHistoryEntry& modeLine = dlhist[ys];

			if (modeLine.mbValid) {
				int mode = modeLine.mControl & 15;

				if (checkValidCopyText) {
					if (mode != 2 && mode != 3 && mode != 6 && mode != 7)
						return {-1,-1};
				}

				static constexpr uint8 kXCycShift[16] = {
					15, 15, 2, 2, 2, 2, 3, 3, 2, 2, 1, 1, 0, 0, 0, 0
				};

				static constexpr int kPFStart[4] = { 64, 64, 48, 32 };

				int hscroll = modeLine.mHVScroll & 15;

				return {
					((xcc - hscroll - kPFStart[modeLine.mDMACTL & 3]) >> kXCycShift[mode]),
					ys
				};
			}
		}
	}

	return {-1, -1};
}

/// Read characters from a text mode line into a buffer; returns the number
/// of characters read, or zero if an error occurs (range out of bounds, no
/// mode line, not a supported text mode line). The returned buffer is _not_
/// null terminated.
///
int ATUIVideoDisplayWindow::ReadText(uint8 *dst, int yc, int startChar, int numChars) const {
	ATAnticEmulator& antic = g_sim.GetAntic();
	const ATAnticEmulator::DLHistoryEntry *dlhist = antic.GetDLHistory();
	const ATAnticEmulator::DLHistoryEntry& dle = dlhist[yc];

	// check that mode line is valid
	if (!dle.mbValid)
		return 0;

	// check mode
	switch(dle.mControl & 15) {
		case 2:
		case 3:
		case 6:
		case 7:
			break;

		default:
			return 0;
	}

	// compute width
	static const int kWidthLookup[2][4] = {
		{ 0, 16, 20, 24 },	// no horizontal scrolling
		{ 0, 20, 24, 24 },	// horizontal scrolling
	};

	int len = (dle.mControl & 4 ? 1 : 2) * kWidthLookup[(dle.mControl & 0x10) != 0][dle.mDMACTL & 3];

	// clip
	if (numChars <= 0 || startChar >= len)
		return 0;

	if (startChar < 0) {
		numChars += startChar;
		startChar = 0;
	}

	if (numChars > len - startChar)
		numChars = len - startChar;

	if (numChars <= 0)
		return 0;

	// read out raw bytes
	uint8 data[48];
	g_sim.GetMemoryManager()->DebugAnticReadMemory(data, dle.mPFAddress + startChar, len);

	static const uint8 kInternalToATASCIIXorTab[4]={
		0x20, 0x60, 0x40, 0x00
	};

	uint8 mask = (dle.mControl & 4) ? 0x3f : 0xff;
	uint8 xorval = (dle.mControl & 4) && (dle.mCHBASE & 1) ? 0x40 : 0x00;

	for(int i=0; i<numChars; ++i) {
		uint8 c = data[i];

		// convert INTERNAL char to ATASCII
		c &= mask;
		c ^= xorval;

		c ^= kInternalToATASCIIXorTab[(c & 0x60) >> 5];

		*dst++ = c;
	}

	return numChars;
}

void ATUIVideoDisplayWindow::ClearCoordinateIndicator() {
	if (mbCoordIndicatorActive) {
		mbCoordIndicatorActive = false;

		IATUIRenderer *uir = g_sim.GetUIRenderer();
		if (uir)
			uir->SetHoverTip(0, 0, NULL);
	}
}

void ATUIVideoDisplayWindow::SetCoordinateIndicator(int x, int y) {
	int hcyc, vcyc;

	if (!MapPixelToBeamPosition(x, y, hcyc, vcyc, false)) {
		ClearCoordinateIndicator();
		return;
	}

	mbCoordIndicatorActive = true;

	IATUIRenderer *uir = g_sim.GetUIRenderer();
	if (uir) {
		VDStringW s;

		s.sprintf(L"<b>Pos:</b> (%u,%u)\n", hcyc, vcyc);

		ATAnticEmulator& antic = g_sim.GetAntic();
		const ATAnticEmulator::DLHistoryEntry *dlhist = antic.GetDLHistory();

		bool dlvalid = false;
		if (vcyc >= 8 && vcyc < 248) {
			int y = vcyc;

			while(y > 8 && !dlhist[y].mbValid)
				--y;

			const ATAnticEmulator::DLHistoryEntry& dlent = dlhist[y];

			if (dlent.mbValid) {
				uint8 mode = dlent.mControl & 15;
				uint8 special = dlent.mControl & 0xf0;

				dlvalid = true;
				s.append_sprintf(L"<b>DL[$%04X]:</b> ", dlent.mDLAddress);

				if (mode >= 2) {
					const wchar_t *const kPFWidths[]={ L"Disabled", L"Narrow", L"Normal", L"Wide" };

					s.append_sprintf(L"Mode %X %ls @ $%04X", mode, kPFWidths[dlent.mDMACTL & 3], dlent.mPFAddress);
				} else if (mode == 1) {
					if (dlent.mControl & 0x40)
						s.append(L"JVB");
					else
						s.append(L"Jump");

					special &= 0xb0;
				} else {
					s.append_sprintf(L"Blank x%u", ((dlent.mControl >> 4) & 7) + 1);
					special &= 0x80;
				}

				if (special) {
					s.append(L" (");

					if (special & 0x80)
						s.append(L"DLI, ");

					if (special & 0x40)
						s.append(L"LMS, ");

					if (special & 0x20)
						s.append(L"VSCR, ");

					if (special & 0x10)
						s.append(L"HSCR, ");

					s.pop_back();
					s.pop_back();

					s.append(L")");
				}
			}
		}

		if (!dlvalid)
			s.append(L"<b>DL:</b> None");

		uir->SetHoverTip(x, y, s.c_str());
	}
}

void ATUIVideoDisplayWindow::ClearHoverTip() {
	if (mbHoverTipActive) {
		mbHoverTipActive = false;
		g_sim.GetUIRenderer()->SetHoverTip(0, 0, NULL);
	}
}

sint32 ATUIVideoDisplayWindow::FindDropTargetOverlay(sint32 x, sint32 y) const {
	for (int i=0; i<(int)vdcountof(mpDropTargetOverlays); ++i) {
		ATUIWidget *w = mpDropTargetOverlays[i];

		if (w && w->GetArea().contains(vdpoint32(x, y)))
			return i;
	}

	return -1;
}

void ATUIVideoDisplayWindow::HighlightDropTargetOverlay(int index) {
	for (int i=0; i<(int)vdcountof(mpDropTargetOverlays); ++i) {
		ATUILabel *label = static_cast<ATUILabel *>(mpDropTargetOverlays[i].get());

		if (label) {
			const uint32 c = (i == index) ? 0xF0A0A0A0 : 0xF0606060;

			label->SetAlphaFillColor(c);
		}
	}
}

void ATUIVideoDisplayWindow::CreateDropTargetOverlays() {
	if (mpDropTargetOverlays[0])
		return;

	static_assert(vdcountof(kDropFileTargets) == vdcountof(mpDropTargetOverlays));

	for(int i=0; i<7; ++i) {
		vdrefptr<ATUILabel> label { new ATUILabel };
		label->SetTextColor(0xFF000000);
		label->SetTextAlign(ATUILabel::kAlignCenter);
		label->SetTextVAlign(ATUILabel::kVAlignMiddle);

		const ATUIDropFilesTarget target = kDropFileTargets[i];
		switch(target) {
			case ATUIDropFilesTarget::MountCart:
				label->SetText(L"Mount cartridge");
				break;

			case ATUIDropFilesTarget::MountDisk1:
			case ATUIDropFilesTarget::MountDisk2:
			case ATUIDropFilesTarget::MountDisk3:
			case ATUIDropFilesTarget::MountDisk4:
				label->SetTextF(L"Mount disk D%u:", (unsigned)target - (unsigned)ATUIDropFilesTarget::MountDisk1 + 1);
				break;

			case ATUIDropFilesTarget::MountImage:
				label->SetText(L"Mount image");
				break;

			case ATUIDropFilesTarget::BootImage:
				label->SetText(L"Boot image");
				break;
		}

		vdrect32f r;

		if (i == 6) {
			r.left = 0.0f;
			r.right = 0.74f;
			r.top = 0.0f;
			r.bottom = 1.0f;
		} else {
			r.left = 0.75f;
			r.right = 1.0f;
			r.bottom = 1.0f - 0.15f * i;
			r.top = r.bottom - 0.14f;
		}

		vdrefptr<IATUIAnchor> anchor; 
		ATUICreateProportionAnchor(r, ~anchor);
		label->SetAnchor(anchor);
		
		GetManager()->GetMainWindow()->AddChild(label);
		label->SetFont(GetManager()->GetThemeFont(kATUIThemeFont_Header));

		mpDropTargetOverlays[i] = label;
	}

	HighlightDropTargetOverlay(-1);
}

void ATUIVideoDisplayWindow::DestroyDropTargetOverlays() {
	for(vdrefptr<ATUIWidget>& w : mpDropTargetOverlays) {
		if (w) {
			w->GetParent()->RemoveChild(w);
			w = nullptr;
		}
	}
}

void ATUIVideoDisplayWindow::UpdateEnhTextSize() {
	if (mpEnhTextEngine) {
		auto *vo = mpEnhTextEngine->GetVideoOutput();

		const auto videoInfoPrev = vo->GetVideoInfo();

		const auto csz = GetClientArea().size();
		mpEnhTextEngine->OnSize(csz.w, csz.h);

		const auto videoInfoNext = vo->GetVideoInfo();

		if (videoInfoPrev.mTextRows != videoInfoNext.mTextRows || videoInfoPrev.mTextColumns != videoInfoNext.mTextColumns) {
			if (mbShowEnhSizeIndicator) {
				if (!mpUILabelEnhTextSize) {
					mpUILabelEnhTextSize = new ATUILabel;
					mpUILabelEnhTextSize->AddRef();

					vdrefptr<IATUIAnchor> anchor;
					ATUICreateTranslationAnchor(0.5f, 0.5f, ~anchor);
					mpUILabelEnhTextSize->SetFont(mpManager->GetThemeFont(kATUIThemeFont_Default));
					mpUILabelEnhTextSize->SetAnchor(anchor);
					mpUILabelEnhTextSize->SetTextOffset(8, 8);
					mpUILabelEnhTextSize->SetTextColor(0xFFFFFF);
					mpUILabelEnhTextSize->SetBorderColor(0xFFFFFF);
					mpUILabelEnhTextSize->SetFillColor(0x404040);

					AddChild(mpUILabelEnhTextSize);
				}

				mpUILabelEnhTextSize->SetTextF(L"%ux%u", videoInfoNext.mTextColumns, videoInfoNext.mTextRows);
				mpUILabelEnhTextSize->AutoSize();
			}
		}
	}
}