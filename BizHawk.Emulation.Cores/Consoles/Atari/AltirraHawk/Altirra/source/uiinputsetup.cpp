//	Altirra - Atari 800/800XL emulator
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
#include <vd2/system/strutil.h>
#include <at/atnativeui/dialog.h>
#include "inputmanager.h"
#include "inputcontroller.h"
#include <at/atnativeui/uinativewindow.h>
#include <at/atnativeui/uiproxies.h>
#include "resource.h"
#include "joystick.h"

///////////////////////////////////////////////////////////////////////////

class ATUIJoyInputView final : public ATUINativeWindow {
public:
	void SetTravel(bool horiz, bool vert);
	void SetPosition(sint32 x, sint32 y);
	void SetDeadifiedPosition(sint32 x, sint32 y);
	void SetDirection(float angle);
	void SetAnalogDeadZone(sint32 dz);
	void SetDigitalDeadZone(sint32 dz);

	LRESULT WndProc(UINT msg, WPARAM wParam, LPARAM lParam) override;

private:
	HDC mhdc = nullptr;
	HBITMAP mhbm = nullptr;
	HGDIOBJ mhbmOld = nullptr;
	sint32 mCachedWidth = 0;
	sint32 mCachedHeight = 0;
	sint32 mPosX = 0;
	sint32 mPosY = 0;
	sint32 mDPosX = 0;
	sint32 mDPosY = 0;
	sint32 mAnalogDeadZone = 0;
	sint32 mDigitalDeadZone = 0;
	float mDir = -1;
	bool mbHorizontalTravel = true;
	bool mbVerticalTravel = true;
	bool mbCachedImageValid = false;
};

void ATUIJoyInputView::SetTravel(bool horiz, bool vert) {
	if (mbHorizontalTravel != horiz || mbVerticalTravel != vert) {
		mbHorizontalTravel = horiz;
		mbVerticalTravel = vert;
		mbCachedImageValid = false;

		if (mhwnd)
			InvalidateRect(mhwnd, NULL, TRUE);
	}
}

void ATUIJoyInputView::SetPosition(sint32 x, sint32 y) {
	if (mPosX != x || mPosY != y) {
		mPosX = x;
		mPosY = y;
		mbCachedImageValid = false;

		if (mhwnd)
			InvalidateRect(mhwnd, NULL, TRUE);
	}
}

void ATUIJoyInputView::SetDeadifiedPosition(sint32 x, sint32 y) {
	if (mDPosX != x || mDPosY != y) {
		mDPosX = x;
		mDPosY = y;
		mbCachedImageValid = false;

		if (mhwnd)
			InvalidateRect(mhwnd, NULL, TRUE);
	}
}

void ATUIJoyInputView::SetDirection(float angle) {
	if (mDir != angle) {
		mDir = angle;
		mbCachedImageValid = false;

		if (mhwnd)
			InvalidateRect(mhwnd, NULL, TRUE);
	}
}

void ATUIJoyInputView::SetAnalogDeadZone(sint32 dz) {
	if (mAnalogDeadZone != dz) {
		mAnalogDeadZone = dz;
		mbCachedImageValid = false;

		if (mhwnd)
			InvalidateRect(mhwnd, NULL, TRUE);
	}
}

void ATUIJoyInputView::SetDigitalDeadZone(sint32 dz) {
	if (mDigitalDeadZone != dz) {
		mDigitalDeadZone = dz;
		mbCachedImageValid = false;
		InvalidateRect(mhwnd, NULL, TRUE);
	}
}

LRESULT ATUIJoyInputView::WndProc(UINT msg, WPARAM wParam, LPARAM lParam) {
	switch(msg) {
		case WM_CREATE:
			if (!mhdc) {
				HDC hdc = GetDC(mhwnd);
				if (hdc) {
					mhdc = CreateCompatibleDC(hdc);

					if (!SaveDC(mhdc)) {
						VDVERIFY(DeleteDC(mhdc));
						mhdc = nullptr;
					}

					SelectObject(mhdc, (HPEN)GetStockObject(DC_PEN));
					SelectObject(mhdc, (HBRUSH)GetStockObject(DC_BRUSH));

					ReleaseDC(mhwnd, hdc);
				}
			}
			break;

		case WM_DESTROY:
			if (mhbmOld) {
				VDVERIFY(SelectObject(mhdc, mhbmOld));
				mhbmOld = nullptr;
			}

			if (mhdc) {
				RestoreDC(mhdc, -1);
				DeleteDC(mhdc);
				mhdc = nullptr;
			}
			break;

		case WM_SIZE:
			if (mbCachedImageValid) {
				const int w = LOWORD(lParam);
				const int h = HIWORD(lParam);

				if (mCachedWidth != w || mCachedHeight != h) {
					mbCachedImageValid = false;
					InvalidateRect(mhwnd, nullptr, FALSE);
				}
			}
			break;

		case WM_ERASEBKGND:
			return 0;

		case WM_PAINT:
			{
				PAINTSTRUCT ps;

				if (HDC hdc = BeginPaint(mhwnd, &ps)) {
					RECT r;
					
					if (GetClientRect(mhwnd, &r) && r.right > 0 && r.bottom > 0) {
						const sint32 w = r.right;
						const sint32 h = r.bottom;

						if (mCachedWidth != w || mCachedWidth != h) {
							mCachedWidth = w;
							mCachedHeight = h;

							if (mhbmOld) {
								SelectObject(mhdc, mhbmOld);
								mhbmOld = nullptr;
							}

							if (mhbm) {
								DeleteObject(mhbm);
								mhbm = nullptr;
							}

							mhbm = CreateCompatibleBitmap(hdc, w, h);

							if (mhbm)
								mhbmOld = SelectObject(mhdc, mhbm);

							mbCachedImageValid = false;
						}

						if (mhbmOld && !mbCachedImageValid) {
							mbCachedImageValid = true;

							// clear
							HBRUSH hbr = (HBRUSH)GetStockObject(DC_BRUSH);
							VDVERIFY(FillRect(mhdc, &r, (HBRUSH)(COLOR_3DFACE + 1)));

							// draw analog dead zone (gray disc or band)
							if (mAnalogDeadZone > 0) {
								sint32 x1 = VDRoundToInt((0.5f - (float)mAnalogDeadZone / 131072.0f) * w);
								sint32 x2 = VDRoundToInt((0.5f + (float)mAnalogDeadZone / 131072.0f) * w);
								sint32 y1 = VDRoundToInt((0.5f - (float)mAnalogDeadZone / 131072.0f) * h);
								sint32 y2 = VDRoundToInt((0.5f + (float)mAnalogDeadZone / 131072.0f) * h);

								SelectObject(mhdc, hbr);
								SelectObject(mhdc, GetStockObject(NULL_PEN));
								SetDCBrushColor(mhdc, ((GetSysColor(COLOR_3DFACE) >> 2) & 0x3F3F3F)*3 + 0x202020);

								if (mbHorizontalTravel) {
									if (mbVerticalTravel)
										Ellipse(mhdc, x1, y1, x2+1, y2+1);
									else {
										RECT r = { x1, 0, x2, h };
										FillRect(mhdc, &r, hbr);
									}
								} else {
									RECT r = { 0, y1, w, y2 };
									FillRect(mhdc, &r, hbr);
								}
							}

							// draw digital dead zone (red circle)
							if (mDigitalDeadZone > 0) {
								sint32 x1 = VDRoundToInt((0.5f - (float)mDigitalDeadZone / 131072.0f) * w);
								sint32 x2 = VDRoundToInt((0.5f + (float)mDigitalDeadZone / 131072.0f) * w);
								sint32 y1 = VDRoundToInt((0.5f - (float)mDigitalDeadZone / 131072.0f) * h);
								sint32 y2 = VDRoundToInt((0.5f + (float)mDigitalDeadZone / 131072.0f) * h);

								SetDCPenColor(mhdc, ((GetSysColor(COLOR_3DFACE) >> 1) & 0x7F7F7F) + 0x000080);
								SelectObject(mhdc, GetStockObject(NULL_BRUSH));
								SelectObject(mhdc, GetStockObject(DC_PEN));

								if (mbHorizontalTravel) {
									if (mbVerticalTravel)
										Ellipse(mhdc, x1, y1, x2, y2);
									else {
										MoveToEx(mhdc, x1, 0, nullptr);
										LineTo(mhdc, x1, h);
										MoveToEx(mhdc, x2, 0, nullptr);
										LineTo(mhdc, x2, h);
									}
								} else {
									MoveToEx(mhdc, 0, y1, nullptr);
									LineTo(mhdc, w, y1);
									MoveToEx(mhdc, 0, y2, nullptr);
									LineTo(mhdc, w, y2);
								}
							}

							// draw digital direction arrow
							if (mDir >= 0) {
								static const float kArrow[][2]={
									{ -1.00f,  0.4f },
									{  0.00f,  0.4f },
									{  0.00f,  1.0f },
									{  1.00f,  0.0f },
									{  0.00f, -1.0f },
									{  0.00f, -0.4f },
									{ -1.00f, -0.4f },
								};

								POINT pt[vdcountof(kArrow)];
								float m11 = cosf(mDir) * (mbHorizontalTravel ? (float)w / 8.0f : (float)h / 2.0f);
								float m12 = -sinf(mDir) * (mbVerticalTravel ? (float)h / 8.0f : (float)w / 2.0f);
								float m21 = -m12;
								float m22 = m11;
								float m31 = 0.5f * w + m11 * 3.0f;
								float m32 = 0.5f * h + m12 * 3.0f;

								for(size_t i=0; i<vdcountof(kArrow); ++i) {
									pt[i].x = VDRoundToInt(kArrow[i][0] * m11 + kArrow[i][1] * m21 + m31);
									pt[i].y = VDRoundToInt(kArrow[i][0] * m12 + kArrow[i][1] * m22 + m32);
								}

								SelectObject(mhdc, GetStockObject(NULL_PEN));
								SelectObject(mhdc, hbr);
								SetDCBrushColor(mhdc, RGB(224, 0, 0));

								VDVERIFY(Polygon(mhdc, pt, (int)vdcountof(pt)));
							}

							// draw centerlines
							SetDCPenColor(mhdc, RGB(0xc0, 0xc0, 0xff));
							SelectObject(mhdc, GetStockObject(DC_PEN));

							if (mbVerticalTravel) {
								MoveToEx(mhdc, 0, h/2, NULL);
								LineTo(mhdc, w, h/2);
							}

							if (mbHorizontalTravel) {
								MoveToEx(mhdc, w/2, 0, NULL);
								LineTo(mhdc, w/2, h);
							}

							// draw adjusted position dot
							sint32 dpx = VDRoundToInt32(((float)mDPosX / (float)0x20000 + 0.5f) * w);
							sint32 dpy = VDRoundToInt32(((float)mDPosY / (float)0x20000 + 0.5f) * h);
							RECT rdp = RECT { dpx - 1, dpy - 1, dpx + 2, dpy + 2 };

							SetDCBrushColor(mhdc, RGB(0x00, 0x80, 0x00));
							FillRect(mhdc, &rdp, hbr);

							// draw raw position crosshairs
							sint32 curdx = w / 16;
							sint32 curdy = h / 16;
							sint32 px = ((w - 1)*((mPosX >> 8) + 0x100)) >> 9;
							sint32 py = ((h - 1)*((mPosY >> 8) + 0x100)) >> 9;

							SetDCBrushColor(mhdc, RGB(0, 0, 0));

							if (mbHorizontalTravel) {
								if (mbVerticalTravel) {
									RECT rh = { px - curdx, py, px + curdx + 1, py + 1 };
									VDVERIFY(FillRect(mhdc, &rh, hbr));

									RECT rv = { px, py - curdy, px + 1, py + curdy + 1 };
									VDVERIFY(FillRect(mhdc, &rv, hbr));
								} else {
									RECT rv = { px, 0, px + 1, h };
									VDVERIFY(FillRect(mhdc, &rv, hbr));
								}
							} else {
								RECT rh = { 0, py, w, py + 1 };
								VDVERIFY(FillRect(mhdc, &rh, hbr));
							}

							// draw border
							SelectObject(mhdc, GetStockObject(DC_PEN));
							SelectObject(mhdc, GetStockObject(NULL_BRUSH));
							SetDCPenColor(mhdc, RGB(0x80, 0x80, 0x80));
							VDVERIFY(Rectangle(mhdc, 0, 0, w, h));
						}

						if (mbCachedImageValid) {
							VDVERIFY(BitBlt(hdc, 0, 0, w, h, mhdc, 0, 0, SRCCOPY));
						}
					}

					EndPaint(mhwnd, &ps);
				}
			}
			return 0;
	}

	return ATUINativeWindow::WndProc(msg, wParam, lParam);
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogInputSetup final : public VDDialogFrameW32 {
public:
	ATUIDialogInputSetup(ATInputManager& iman, IATJoystickManager *ijoy);
	~ATUIDialogInputSetup();

protected:
	bool OnLoaded() override;
	void OnDestroy() override;
	bool OnCancel() override;
	void OnDataExchange(bool write) override;
	bool OnTimer(uint32 id) override;
	void OnHScroll(uint32 id, int code) override;
	void OnDpiChanged() override;

	sint32 GetDeadZone(uint32 id);
	void SetDeadZone(uint32 id, sint32 dz);
	float GetPower(uint32 id);
	void SetPower(uint32 id, float power);

	void UpdateDeadZoneLabel(uint32 id, sint32 val);
	void UpdateAnalogPowerLabel(uint32 id, uint32 srcid);

	ATInputManager& mInputMan;
	IATJoystickManager *const mpJoyMan;
	sint32 mLastUnit = -1;

	ATJoystickTransforms mCurrentTransforms;
	ATJoystickTransforms mSavedTransforms;

	vdrefptr<ATUIJoyInputView> mpInputViews[4];

	static const uint32 kInputAreaIds[4][2];
	static const float kAnalogPowerExponentScale;
};

const uint32 ATUIDialogInputSetup::kInputAreaIds[4][2]={
	{ IDC_STATIC_LSTICKAREA, IDC_LSTICKVIEW },
	{ IDC_STATIC_RSTICKAREA, IDC_RSTICKVIEW },
	{ IDC_STATIC_LTRIGAREA, IDC_LTRIGVIEW },
	{ IDC_STATIC_RTRIGAREA, IDC_RTRIGVIEW },
};

const float ATUIDialogInputSetup::kAnalogPowerExponentScale = 0.13862943611198906188344642429164f; // ln(4)/10

ATUIDialogInputSetup::ATUIDialogInputSetup(ATInputManager& iman, IATJoystickManager *ijoy)
	: VDDialogFrameW32(IDD_INPUT_SETUP)
	, mInputMan(iman)
	, mpJoyMan(ijoy)
{
}

ATUIDialogInputSetup::~ATUIDialogInputSetup() {
}

bool ATUIDialogInputSetup::OnLoaded() {
	mSavedTransforms = mpJoyMan->GetTransforms();

	TBSetRange(IDC_ANALOG_DEAD_ZONE, 0, 100);
	TBSetPageStep(IDC_ANALOG_DEAD_ZONE, 5);
	TBSetRange(IDC_DIGITAL_DEAD_ZONE, 0, 100);
	TBSetPageStep(IDC_DIGITAL_DEAD_ZONE, 5);
	TBSetRange(IDC_TRIGGER_ANALOG_DEAD_ZONE, 0, 100);
	TBSetPageStep(IDC_TRIGGER_ANALOG_DEAD_ZONE, 5);
	TBSetRange(IDC_TRIGGER_DIGITAL_DEAD_ZONE, 0, 100);
	TBSetPageStep(IDC_TRIGGER_DIGITAL_DEAD_ZONE, 5);
	TBSetRange(IDC_ANALOG_POWER, -10, 10);
	TBSetPageStep(IDC_ANALOG_POWER, 5);
	TBSetRange(IDC_TRIGGER_ANALOG_POWER, -10, 10);
	TBSetPageStep(IDC_TRIGGER_ANALOG_POWER, 5);

	for(int i=0; i<4; ++i) {
		const vdrect32 r = GetControlPos(kInputAreaIds[i][0]);
		ShowWindow(GetControl(kInputAreaIds[i][0]), SW_HIDE);

		mpInputViews[i] = new ATUIJoyInputView;
		mpInputViews[i]->CreateChild(mhdlg, kInputAreaIds[i][1], r.left, r.top, r.width(), r.height(), WS_CHILD | WS_VISIBLE, 0);
	}

	// set triggers to vertical only
	mpInputViews[2]->SetTravel(false, true);
	mpInputViews[3]->SetTravel(false, true);

	OnDataExchange(false);

	mpJoyMan->SetCaptureMode(true);

	SetPeriodicTimer(1, 20);

	return true;
}

void ATUIDialogInputSetup::OnDestroy() {
	mpJoyMan->SetCaptureMode(false);

	for(auto& inputView : mpInputViews) {
		inputView->Destroy();
		inputView = nullptr;
	}
}

bool ATUIDialogInputSetup::OnCancel() {
	mpJoyMan->SetTransforms(mSavedTransforms);
	return false;
}

void ATUIDialogInputSetup::OnDataExchange(bool write) {
	if (write) {
		mpJoyMan->SetTransforms(mCurrentTransforms);
	} else {
		mCurrentTransforms = mpJoyMan->GetTransforms();

		SetDeadZone(IDC_ANALOG_DEAD_ZONE, mCurrentTransforms.mStickAnalogDeadZone);
		SetDeadZone(IDC_DIGITAL_DEAD_ZONE, mCurrentTransforms.mStickDigitalDeadZone);
		SetDeadZone(IDC_TRIGGER_ANALOG_DEAD_ZONE, mCurrentTransforms.mTriggerAnalogDeadZone);
		SetDeadZone(IDC_TRIGGER_DIGITAL_DEAD_ZONE, mCurrentTransforms.mTriggerDigitalDeadZone);

		SetPower(IDC_ANALOG_POWER, mCurrentTransforms.mStickAnalogPower);
		SetPower(IDC_TRIGGER_ANALOG_POWER, mCurrentTransforms.mTriggerAnalogPower);

		UpdateDeadZoneLabel(IDC_STATIC_ANALOGDEADZONE, mCurrentTransforms.mStickAnalogDeadZone);
		UpdateDeadZoneLabel(IDC_STATIC_DIGITALDEADZONE, mCurrentTransforms.mStickDigitalDeadZone);
		UpdateDeadZoneLabel(IDC_STATIC_TRIGGERANALOGDEADZONE, mCurrentTransforms.mTriggerAnalogDeadZone);
		UpdateDeadZoneLabel(IDC_STATIC_TRIGGERDIGITALDEADZONE, mCurrentTransforms.mTriggerDigitalDeadZone);
		UpdateAnalogPowerLabel(IDC_STATIC_ANALOGPOWER, IDC_ANALOG_POWER);
		UpdateAnalogPowerLabel(IDC_STATIC_TRIGGERANALOGPOWER, IDC_TRIGGER_ANALOG_POWER);

		for(int i=0; i<2; ++i) {
			mpInputViews[i]->SetAnalogDeadZone(mCurrentTransforms.mStickAnalogDeadZone);
			mpInputViews[i]->SetDigitalDeadZone(mCurrentTransforms.mStickDigitalDeadZone);
		}

		for(int i=0; i<2; ++i) {
			mpInputViews[i+2]->SetAnalogDeadZone(mCurrentTransforms.mTriggerAnalogDeadZone);
			mpInputViews[i+2]->SetDigitalDeadZone(mCurrentTransforms.mTriggerDigitalDeadZone);
		}
	}
}

bool ATUIDialogInputSetup::OnTimer(uint32 id) {
	if (id == 1) {
		uint32 n;
		const ATJoystickState *states = mpJoyMan->PollForCapture(n);

		ATJoystickState state = {};
		bool foundLastUnit = false;
		bool foundActiveUnit = false;

		for(uint32 i=0; i<n; ++i, ++states) {
			if (std::max<uint32>(abs(states->mAxisVals[0]), abs(states->mAxisVals[1])) > 0x4000) {
				foundActiveUnit = true;
			} else {
				if (mLastUnit >= 0 && (uint32)mLastUnit != states->mUnit)
					continue;
			}

			state = *states;

			mLastUnit = states->mUnit;
			if (foundActiveUnit)
				break;
		}

		static const float kDirectionLookup[16]={
			// bit 0: left
			// bit 1: right
			// bit 2: up
			// bit 3: down

			-1.0f,
			4.0f * nsVDMath::kfTwoPi / 8.0f,
			0.0f * nsVDMath::kfTwoPi / 8.0f,
			-1.0f,
			2.0f * nsVDMath::kfTwoPi / 8.0f,
			3.0f * nsVDMath::kfTwoPi / 8.0f,
			1.0f * nsVDMath::kfTwoPi / 8.0f,
			2.0f * nsVDMath::kfTwoPi / 8.0f,
			6.0f * nsVDMath::kfTwoPi / 8.0f,
			5.0f * nsVDMath::kfTwoPi / 8.0f,
			7.0f * nsVDMath::kfTwoPi / 8.0f,
			6.0f * nsVDMath::kfTwoPi / 8.0f,
			-1.0f,
			4.0f * nsVDMath::kfTwoPi / 8.0f,
			0.0f * nsVDMath::kfTwoPi / 8.0f,
			-1.0f,
		};

		// left stick
		if (mpInputViews[0]) {
			mpInputViews[0]->SetPosition(state.mAxisVals[0], state.mAxisVals[1]);
			mpInputViews[0]->SetDeadifiedPosition(state.mDeadifiedAxisVals[0], state.mDeadifiedAxisVals[1]);
			mpInputViews[0]->SetDirection(kDirectionLookup[state.mAxisButtons & 15]);
		}

		// right stick
		if (mpInputViews[1]) {
			mpInputViews[1]->SetPosition(state.mAxisVals[3], state.mAxisVals[4]);
			mpInputViews[1]->SetDeadifiedPosition(state.mDeadifiedAxisVals[3], state.mDeadifiedAxisVals[4]);
			mpInputViews[1]->SetDirection(kDirectionLookup[(state.mAxisButtons >> 6) & 15]);
		}

		// left trigger
		if (mpInputViews[2]) {
			mpInputViews[2]->SetPosition(0, state.mAxisVals[2]);
			mpInputViews[2]->SetDeadifiedPosition(0, state.mDeadifiedAxisVals[2]);
			mpInputViews[2]->SetDirection(kDirectionLookup[(state.mAxisButtons >> 2) & 12]);
		}

		// right trigger
		if (mpInputViews[3]) {
			mpInputViews[3]->SetPosition(0, state.mAxisVals[5]);
			mpInputViews[3]->SetDeadifiedPosition(0, state.mDeadifiedAxisVals[5]);
			mpInputViews[3]->SetDirection(kDirectionLookup[(state.mAxisButtons >> 8) & 12]);
		}

		return true;
	}

	return false;
}

void ATUIDialogInputSetup::OnHScroll(uint32 id, int code) {
	switch(id) {
		case IDC_ANALOG_DEAD_ZONE:
			mCurrentTransforms.mStickAnalogDeadZone = GetDeadZone(IDC_ANALOG_DEAD_ZONE);

			for(int i=0; i<2; ++i)
				mpInputViews[i]->SetAnalogDeadZone(mCurrentTransforms.mStickAnalogDeadZone);

			UpdateDeadZoneLabel(IDC_STATIC_ANALOGDEADZONE, mCurrentTransforms.mStickAnalogDeadZone);
			OnDataExchange(true);
			break;

		case IDC_DIGITAL_DEAD_ZONE:
			mCurrentTransforms.mStickDigitalDeadZone = GetDeadZone(IDC_DIGITAL_DEAD_ZONE);

			for(int i=0; i<2; ++i)
				mpInputViews[i]->SetDigitalDeadZone(mCurrentTransforms.mStickDigitalDeadZone);

			UpdateDeadZoneLabel(IDC_STATIC_DIGITALDEADZONE, mCurrentTransforms.mStickDigitalDeadZone);
			OnDataExchange(true);
			break;

		case IDC_TRIGGER_ANALOG_DEAD_ZONE:
			mCurrentTransforms.mTriggerAnalogDeadZone = GetDeadZone(IDC_TRIGGER_ANALOG_DEAD_ZONE);

			for(int i=0; i<2; ++i)
				mpInputViews[i+2]->SetAnalogDeadZone(mCurrentTransforms.mTriggerAnalogDeadZone);

			UpdateDeadZoneLabel(IDC_STATIC_TRIGGERANALOGDEADZONE, mCurrentTransforms.mTriggerAnalogDeadZone);
			OnDataExchange(true);
			break;

		case IDC_TRIGGER_DIGITAL_DEAD_ZONE:
			mCurrentTransforms.mTriggerDigitalDeadZone = GetDeadZone(IDC_TRIGGER_DIGITAL_DEAD_ZONE);

			for(int i=0; i<2; ++i)
				mpInputViews[i+2]->SetDigitalDeadZone(mCurrentTransforms.mTriggerDigitalDeadZone);

			UpdateDeadZoneLabel(IDC_STATIC_TRIGGERDIGITALDEADZONE, mCurrentTransforms.mTriggerDigitalDeadZone);
			OnDataExchange(true);
			break;

		case IDC_ANALOG_POWER:
			mCurrentTransforms.mStickAnalogPower = GetPower(IDC_ANALOG_POWER);
			UpdateAnalogPowerLabel(IDC_STATIC_ANALOGPOWER, IDC_ANALOG_POWER);
			OnDataExchange(true);
			break;

		case IDC_TRIGGER_ANALOG_POWER:
			mCurrentTransforms.mTriggerAnalogPower = GetPower(IDC_TRIGGER_ANALOG_POWER);
			UpdateAnalogPowerLabel(IDC_STATIC_TRIGGERANALOGPOWER, IDC_TRIGGER_ANALOG_POWER);
			OnDataExchange(true);
			break;
	}
}

void ATUIDialogInputSetup::OnDpiChanged() {
	for(size_t i=0; i<4; ++i) {
		const vdrect32 r = GetControlPos(kInputAreaIds[i][0]);

		SetWindowPos(mpInputViews[i]->GetHandleW32(), nullptr, r.left, r.top, r.width(), r.height(), SWP_NOZORDER | SWP_NOACTIVATE);
	}
}

sint32 ATUIDialogInputSetup::GetDeadZone(uint32 id) {
	float v = (float)TBGetValue(id) / 100;

	return VDRoundToInt(v * v * 65536.0f);
}

void ATUIDialogInputSetup::SetDeadZone(uint32 id, sint32 dz) {
	TBSetValue(id, VDRoundToInt(sqrtf((float)dz / 65536.0f) * 100.0f));
}

float ATUIDialogInputSetup::GetPower(uint32 id) {
	return expf(TBGetValue(id) * kAnalogPowerExponentScale);
}

void ATUIDialogInputSetup::SetPower(uint32 id, float power) {
	TBSetValue(id, VDRoundToInt(logf(power) / kAnalogPowerExponentScale));
}

void ATUIDialogInputSetup::UpdateDeadZoneLabel(uint32 id, sint32 val) {
	SetControlTextF(id, L"%u%%", VDRoundToInt(val * (100.0f / 65536.0f)));
}

void ATUIDialogInputSetup::UpdateAnalogPowerLabel(uint32 id, uint32 srcid) {
	SetControlTextF(id, L"%+d", TBGetValue(srcid));
}

///////////////////////////////////////////////////////////////////////////

void ATUIShowDialogInputSetup(VDZHWND parent, ATInputManager& iman, IATJoystickManager *ijoy) {
	ATUIDialogInputSetup dlg(iman, ijoy);

	dlg.ShowDialog((VDGUIHandle)parent);
}
