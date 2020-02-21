//	Altirra - Atari 800/800XL emulator
//	Copyright (C) 2008 Avery Lee
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
#define INITGUID
#define DIRECTINPUT_VERSION 0x0800
#include <guiddef.h>
#include <dinput.h>
#include <setupapi.h>
#include <xinput.h>
#include <vd2/system/bitmath.h>
#include <vd2/system/math.h>
#include <vd2/system/refcount.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/vdalloc.h>
#include "joystick.h"
#include "inputmanager.h"

#pragma comment(lib, "setupapi")

#ifndef DIDFT_OPTIONAL 
#define DIDFT_OPTIONAL          0x80000000 
#endif 

// This code is similar to that from the "XInput and DirectInput" article on MSDN to detect
// XInput controllers. However, it uses the Setup API to get access to the hardware IDs
// instead of WMI, which is slower and more annoying. Also, the original code leaked memory
// due to failing to call VariantClear().

namespace {
	#ifndef GUID_DEVINTERFACE_HID
		const GUID GUID_DEVINTERFACE_HID = {0x4D1E55B2, 0xF16F, 0x11CF, 0x88, 0xCB, 0x00, 0x11, 0x11, 0x00, 0x00, 0x30 };
	#endif

	void GetXInputDevices(vdfastvector<DWORD>& devs) {
		// open the device collection for all HID devices
		HDEVINFO hdi = SetupDiGetClassDevs(&GUID_DEVINTERFACE_HID, NULL, NULL, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
		if (hdi == INVALID_HANDLE_VALUE)
			return;

		// enumerate through all the HID-class devices
		vdblock<WCHAR> buf(128);

		UINT idx = 0;
		for(;;) {
			SP_DEVINFO_DATA sdd = {sizeof(SP_DEVINFO_DATA)};

			// get next specific device
			if (!SetupDiEnumDeviceInfo(hdi, idx++, &sdd))
				break;

			// read the hardware ID string
			DWORD regType = 0;
			DWORD required = 0;
			if (!SetupDiGetDeviceRegistryPropertyW(hdi, &sdd, SPDRP_HARDWAREID, &regType, (PBYTE)buf.data(), (DWORD)((buf.size() - 1) * sizeof(buf[0])), &required)
				&& GetLastError() == ERROR_INSUFFICIENT_BUFFER)
			{
				buf.resize(required / sizeof(WCHAR) + 2);

				if (!SetupDiGetDeviceRegistryPropertyW(hdi, &sdd, SPDRP_HARDWAREID, &regType, (PBYTE)buf.data(), (DWORD)((buf.size() - 1) * sizeof(buf[0])), &required))
					continue;
			}

			// check that we actually got a string -- it should be of MULTI_SZ type, but we can take
			// SZ too as we only care about the first one anyway
			if (regType != REG_MULTI_SZ && regType != REG_SZ)
				continue;

			// force null termination
			buf.back() = 0;

			// check if the hardware ID string contains &IG_
			const WCHAR *s = buf.data();
			if (wcsstr(s, L"&IG_")) {
				// yup, it's an Xbox 360 controller under the Xbox driver -- grab the VID and PID
				const WCHAR *pidstr = wcsstr(s, L"PID_");
				const WCHAR *vidstr = wcsstr(s, L"VID_");

				unsigned pid;
				unsigned vid;
				if (pidstr &&
					vidstr &&
					1 == swscanf(pidstr+4, L"%X", &pid) &&
					1 == swscanf(vidstr+4, L"%X", &vid))
				{
					devs.push_back(MAKELONG(vid, pid));
				}
			}
		}

		SetupDiDestroyDeviceInfoList(hdi);
	}

	template<class T>
	bool BindProcAddress(T *&ptr, HMODULE hmod, const char *name) {
		ptr = (T *)GetProcAddress(hmod, name);

		return ptr != NULL;
	}
}

///////////////////////////////////////////////////////////////////////////

struct ATXInputBinding {
	HMODULE mhmodXInput;
	DWORD (WINAPI *mpXInputGetState)(DWORD, XINPUT_STATE *);

	ATXInputBinding();
	~ATXInputBinding();

	bool IsInited() const { return mhmodXInput != NULL; }

	bool Init();
	void Shutdown();
};

ATXInputBinding::ATXInputBinding()
	: mhmodXInput(NULL)
{
}

ATXInputBinding::~ATXInputBinding() {
	Shutdown();
}

bool ATXInputBinding::Init() {
	if (mhmodXInput)
		return true;

	// We prefer XInput 1.3 to get access to the guide button, but if we can't
	// get that, fall back to XInput 0.91.
	bool have13 = true;

	mhmodXInput = LoadLibraryW(L"xinput1_3");
	if (!mhmodXInput) {
		have13 = false;

		mhmodXInput = LoadLibraryW(L"xinput9_1_0");
		if (!mhmodXInput)
			return false;
	}

	// Try to get access to XInputGetStateEx() first, and if not, fall back
	// to regular XInputGetState().
	if (!have13 || !BindProcAddress(mpXInputGetState, mhmodXInput, (char *)100)) {
		if (!BindProcAddress(mpXInputGetState, mhmodXInput, "XInputGetState")) {
			Shutdown();
			return false;
		}
	}

	// all good
	return true;
}

void ATXInputBinding::Shutdown() {
	if (mhmodXInput) {
		FreeLibrary(mhmodXInput);
		mhmodXInput = NULL;
	}
}

///////////////////////////////////////////////////////////////////////////

class ATController {
public:
	ATController();
	virtual ~ATController() {}

	const ATInputUnitIdentifier& GetId() const { return mId; }

	bool IsMarked() const { return mbMarked; }
	void SetMarked(bool mark) { mbMarked = mark;}

	void SetTransforms(const ATJoystickTransforms& transforms) {
		mTransforms = transforms;
	}

	virtual bool Poll(bool& bigActivity) = 0;
	virtual bool PollForCapture(int& unit, uint32& inputCode, uint32& inputCode2) = 0;
	virtual void PollForCapture(ATJoystickState& dst) = 0;

protected:
	static uint32 ConvertAnalogToDirectionMask(sint32 x, sint32 y, sint32 deadZone);

	bool mbMarked;
	ATInputUnitIdentifier mId;
	ATJoystickTransforms mTransforms;
};

ATController::ATController()
	: mbMarked(false)
{
}

uint32 ATController::ConvertAnalogToDirectionMask(sint32 x, sint32 y, sint32 deadZone) {
	const float kTan22_5d = 0.4142135623730950488016887242097f;
	float dxf = fabsf((float)x);
	float dyf = fabsf((float)y);
	uint32 mask = 0;

	if (dxf * dxf + dyf * dyf < (float)deadZone * (float)deadZone)
		return 0;

	if (dxf > dyf * kTan22_5d) {
		if (x < 0) mask |= (1 << 0);
		if (x > 0) mask |= (1 << 1);
	}

	if (dyf > dxf * kTan22_5d) {
		if (y > 0) mask |= (1 << 2);
		if (y < 0) mask |= (1 << 3);
	}

	return mask;
}

///////////////////////////////////////////////////////////////////////////

class ATControllerXInput final : public ATController, public IATInputUnitNameSource {
public:
	ATControllerXInput(ATXInputBinding& xinput, uint32 xid, ATInputManager *inputMan, const ATInputUnitIdentifier& id);
	~ATControllerXInput();

	bool Poll(bool& bigActivity) override;
	bool PollForCapture(int& unit, uint32& inputCode, uint32& inputCode2) override;
	void PollForCapture(ATJoystickState& dst) override;

public:
	bool GetInputCodeName(uint32 id, VDStringW& name) const override;

protected:
	struct DecodedState {
		sint32 mAxisVals[6];
		sint32 mDeadifiedAxisVals[6];
		uint32 mAxisButtons;
		uint32 mButtons;
	};

	void PollState(DecodedState& state, const XINPUT_STATE& xis);
	void ConvertStick(sint32 dst[2], sint32 x, sint32 y);

	ATXInputBinding& mXInput;
	ATInputManager *const mpInputManager;
	uint32 mXid;
	int mUnit;

	DWORD mLastPacketId;
	DecodedState mLastState;
};

ATControllerXInput::ATControllerXInput(ATXInputBinding& xinput, uint32 xid, ATInputManager *inputMan, const ATInputUnitIdentifier& id)
	: mXInput(xinput)
	, mpInputManager(inputMan)
	, mXid(xid)
	, mLastPacketId(0)
	, mLastState()
{
	mId = id;

	VDStringW name;
	name.sprintf(L"XInput Controller #%u", xid + 1);
	mUnit = inputMan->RegisterInputUnit(mId, name.c_str(), this);
}

ATControllerXInput::~ATControllerXInput() {
}

bool ATControllerXInput::Poll(bool& bigActivity) {
	XINPUT_STATE xis;

	if (ERROR_SUCCESS != mXInput.mpXInputGetState(mXid, &xis))
		return false;

	if (mLastPacketId && mLastPacketId == xis.dwPacketNumber)
		return false;

	mLastPacketId = xis.dwPacketNumber;

	DecodedState dstate;

	PollState(dstate, xis);

	const uint32 axisButtonDelta = dstate.mAxisButtons ^ mLastState.mAxisButtons;
	for(uint32 i=0; i<16; ++i) {
		if (axisButtonDelta & (1 << i)) {
			if (dstate.mAxisButtons & (1 << i))
				mpInputManager->OnButtonDown(mUnit, kATInputCode_JoyStick1Left + i);
			else
				mpInputManager->OnButtonUp(mUnit, kATInputCode_JoyStick1Left + i);
		}
	}

	const uint32 buttonDelta = dstate.mButtons ^ mLastState.mButtons;
	for(int i=0; i<11; ++i) {
		if (buttonDelta & (1 << i)) {
			if (dstate.mButtons & (1 << i))
				mpInputManager->OnButtonDown(mUnit, kATInputCode_JoyButton0 + i);
			else
				mpInputManager->OnButtonUp(mUnit, kATInputCode_JoyButton0 + i);
		}
	}

	if (axisButtonDelta || buttonDelta)
		bigActivity = true;

	for(int i=0; i<6; ++i) {
		if (dstate.mAxisVals[i] != mLastState.mAxisVals[i])
			mpInputManager->OnAxisInput(mUnit, kATInputCode_JoyHoriz1 + i, dstate.mAxisVals[i], dstate.mDeadifiedAxisVals[i]);
	}

	mLastState = dstate;
	return true;
}

bool ATControllerXInput::PollForCapture(int& unit, uint32& inputCode, uint32& inputCode2) {

	XINPUT_STATE xis;

	if (ERROR_SUCCESS != mXInput.mpXInputGetState(mXid, &xis))
		return false;

	if (mLastPacketId && mLastPacketId == xis.dwPacketNumber)
		return false;

	mLastPacketId = xis.dwPacketNumber;

	DecodedState state;
	PollState(state, xis);

	const uint32 newButtons = state.mButtons & ~mLastState.mButtons;
	const uint32 newAxisButtons = state.mAxisButtons & ~mLastState.mAxisButtons;

	mLastState = state;

	if (newButtons) {
		unit = mUnit;
		inputCode = kATInputCode_JoyButton0 + VDFindLowestSetBitFast(newButtons);
		inputCode2 = 0;
		return true;
	}

	if (newAxisButtons) {
		unit = mUnit;

		const int idx = VDFindLowestSetBitFast(newAxisButtons);
		inputCode = kATInputCode_JoyStick1Left + idx;
		inputCode2 = kATInputCode_JoyHoriz1 + (idx >> 1);
		return true;
	}

	return false;
}

void ATControllerXInput::PollForCapture(ATJoystickState& dst) {
	XINPUT_STATE xis;

	dst.mUnit = mUnit;
	dst.mButtons = 0;
	dst.mAxisButtons = 0;
	memset(dst.mAxisVals, 0, sizeof dst.mAxisVals);

	if (ERROR_SUCCESS != mXInput.mpXInputGetState(mXid, &xis))
		return;

	if (!mLastPacketId || mLastPacketId != xis.dwPacketNumber) {
		mLastPacketId = xis.dwPacketNumber;

		DecodedState dstate;

		PollState(dstate, xis);
		mLastState = dstate;
	}

	dst.mButtons = mLastState.mButtons;
	dst.mAxisButtons = mLastState.mAxisButtons;

	for(int i=0; i<6; ++i) {
		dst.mAxisVals[i] = mLastState.mAxisVals[i];
		dst.mDeadifiedAxisVals[i] = mLastState.mDeadifiedAxisVals[i];
	}
}

bool ATControllerXInput::GetInputCodeName(uint32 id, VDStringW& name) const {
	static const wchar_t *const kButtonNames[]={
		L"A button",
		L"B button",
		L"X button",
		L"Y button",
		L"LB button",
		L"RB button",
		L"Back button",
		L"Start button",
		L"Left thumb button",
		L"Right thumb button",
		L"Guide button",
	};

	uint32 buttonIdx = (uint32)(id - kATInputCode_JoyButton0);

	if (buttonIdx < vdcountof(kButtonNames)) {
		name = kButtonNames[buttonIdx];
		return true;
	}

	static const wchar_t *const kAxisButtonNames[]={
		L"Left stick left",
		L"Left stick right",
		L"Left stick up",
		L"Left stick down",
		NULL,
		L"Left trigger pressed",
		L"Right stick left",
		L"Right stick right",
		L"Right stick up",
		L"Right stick down",
		NULL,
		L"Right trigger pressed",
		L"D-pad left",
		L"D-pad right",
		L"D-pad up",
		L"D-pad down",
	};

	const uint32 axisButtonIdx = (uint32)(id - kATInputCode_JoyStick1Left);
	if (axisButtonIdx < vdcountof(kAxisButtonNames) && kAxisButtonNames[axisButtonIdx]) {
		name = kAxisButtonNames[axisButtonIdx];
		return true;
	}

	static const wchar_t *const kAxisNames[]={
		L"Left stick horiz.",
		L"Left stick vert.",
		L"Left trigger",
		L"Right stick horiz.",
		L"Right stick vert.",
		L"Right trigger",
	};

	const uint32 axisIdx = (uint32)(id - kATInputCode_JoyHoriz1);
	if (axisIdx < vdcountof(kAxisNames)) {
		name = kAxisNames[axisIdx];
		return true;
	}

	return false;
}

void ATControllerXInput::PollState(DecodedState& state, const XINPUT_STATE& xis) {
	ConvertStick(state.mDeadifiedAxisVals, xis.Gamepad.sThumbLX, xis.Gamepad.sThumbLY);
	ConvertStick(state.mDeadifiedAxisVals + 3, xis.Gamepad.sThumbRX, xis.Gamepad.sThumbRY);

	state.mAxisVals[0] = xis.Gamepad.sThumbLX * 2;
	state.mAxisVals[1] = xis.Gamepad.sThumbLY * -2;
	state.mAxisVals[3] = xis.Gamepad.sThumbRX * 2;
	state.mAxisVals[4] = xis.Gamepad.sThumbRY * -2;

	state.mDeadifiedAxisVals[1] = -state.mDeadifiedAxisVals[1];
	state.mDeadifiedAxisVals[4] = -state.mDeadifiedAxisVals[4];

	const float triggerThreshold = (float)mTransforms.mTriggerAnalogDeadZone / (float)0x10000;

	for(int i=0; i<2; ++i) {
		const int rawVal = i ? xis.Gamepad.bRightTrigger : xis.Gamepad.bLeftTrigger;
		const float fVal = (float)rawVal / 255.0f;
		const sint32 axisVal = VDRoundToInt32(fVal * (float)0x10000);
		sint32 adjVal = 0;

		if (fVal > triggerThreshold) {
			const float deadVal = (fVal - triggerThreshold) / (1.0f - triggerThreshold);

			adjVal = VDRoundToInt32(65536.0f * powf(deadVal, mTransforms.mTriggerAnalogPower));
		}

		if (i) {
			state.mAxisVals[5] = axisVal;
			state.mDeadifiedAxisVals[5] = adjVal;
		} else {
			state.mAxisVals[2] = axisVal;
			state.mDeadifiedAxisVals[2] = adjVal;
		}
	}

	// Axis buttons 0-3 map to -/+X and -/+Y on left stick.
	// Axis button 4 maps to +RT, to match +Z from DirectInput.
	// Axis button 5 maps to +LT, to match -Z from DirectInput.
	// Axis buttons 6-9 map to -/+X and -/+Y on right stick.
	// Axis button 12 is D-pad left.
	// Axis button 13 is D-pad right.
	// Axis button 14 is D-pad up.
	// Axis button 15 is D-pad down.

	uint32 axisButtonStates = 0;
	axisButtonStates |= ConvertAnalogToDirectionMask(xis.Gamepad.sThumbLX, xis.Gamepad.sThumbLY, mTransforms.mStickDigitalDeadZone / 2);
	if (xis.Gamepad.bLeftTrigger > (mTransforms.mTriggerDigitalDeadZone >> 8)) axisButtonStates |= (1 << 5);
	if (xis.Gamepad.bRightTrigger > (mTransforms.mTriggerDigitalDeadZone >> 8)) axisButtonStates |= (1 << 11);
	axisButtonStates |= ConvertAnalogToDirectionMask(xis.Gamepad.sThumbRX, xis.Gamepad.sThumbRY, mTransforms.mStickDigitalDeadZone / 2) << 6;
	if (xis.Gamepad.wButtons & XINPUT_GAMEPAD_DPAD_LEFT)				axisButtonStates |= (1 << 12);
	if (xis.Gamepad.wButtons & XINPUT_GAMEPAD_DPAD_RIGHT)				axisButtonStates |= (1 << 13);
	if (xis.Gamepad.wButtons & XINPUT_GAMEPAD_DPAD_UP)					axisButtonStates |= (1 << 14);
	if (xis.Gamepad.wButtons & XINPUT_GAMEPAD_DPAD_DOWN)				axisButtonStates |= (1 << 15);

	state.mAxisButtons = axisButtonStates;

	// Button 0 is A (definitely not cross).
	// Button 1 is B (definitely not circle).
	// Button 2 is X (definitely not square).
	// Button 3 is Y (definitely not triangle).
	// Button 4 is left shoulder.
	// Button 5 is right shoulder.
	// Button 6 is back.
	// Button 7 is start.
	// Button 8 is left stick.
	// Button 9 is right stick.
	// Button 10 is the Xbox button.
	static const WORD kButtonMappings[]={
		XINPUT_GAMEPAD_A,
		XINPUT_GAMEPAD_B,
		XINPUT_GAMEPAD_X,
		XINPUT_GAMEPAD_Y,
		XINPUT_GAMEPAD_LEFT_SHOULDER,
		XINPUT_GAMEPAD_RIGHT_SHOULDER,
		XINPUT_GAMEPAD_BACK,
		XINPUT_GAMEPAD_START,
		XINPUT_GAMEPAD_LEFT_THUMB,
		XINPUT_GAMEPAD_RIGHT_THUMB,
		0x400,		// undocumented from XInputGetStateEx - guide button
	};

	uint32 buttonStates = 0;
	for(size_t i=0; i<vdcountof(kButtonMappings); ++i) {
		if (xis.Gamepad.wButtons & kButtonMappings[i])
			buttonStates += (1 << i);
	}

	state.mButtons = buttonStates;		
}

void ATControllerXInput::ConvertStick(sint32 dst[2], sint32 x, sint32 y) {
	float fx = (float)x * 2;
	float fy = (float)y * 2;
	const float mag = sqrtf(fx*fx + fy*fy);
	sint32 rx = 0;
	sint32 ry = 0;

	if (mag > mTransforms.mStickAnalogDeadZone) {
		float scale = (mag - mTransforms.mStickAnalogDeadZone) / (mag * (65536 - mTransforms.mStickAnalogDeadZone));

		// vec * scale = intended vector at power=1, so each
		// integer power needs to scale by mag*scale. Note that
		// we need to scale along the vector and not component-wise.
		// Also, analog power must not be 0 or else we will potentially
		// divide by zero.
		scale *= powf(mag * scale, mTransforms.mStickAnalogPower - 1.0f);

		fx *= scale;
		fy *= scale;

		rx = VDRoundToInt(fx * 65536.0f);
		ry = VDRoundToInt(fy * 65536.0f);

		if (rx < -65536) rx = -65536; else if (rx > 65536) rx = 65536;
		if (ry < -65536) ry = -65536; else if (ry > 65536) ry = 65536;
	}

	dst[0] = rx;
	dst[1] = ry;
}

///////////////////////////////////////////////////////////////////////////

class ATControllerDirectInput final : public ATController, public IATInputUnitNameSource {
public:
	ATControllerDirectInput(IDirectInput8 *di);
	~ATControllerDirectInput();

	bool Init(LPCDIDEVICEINSTANCE devInst, HWND hwnd, ATInputManager *inputMan);
	void Shutdown();

	bool Poll(bool& bigActivity);
	bool PollForCapture(int& unit, uint32& inputCode, uint32& inputCode2);
	void PollForCapture(ATJoystickState& dst);

public:
	bool GetInputCodeName(uint32 id, VDStringW& name) const;

protected:
	struct DecodedState {
		uint32	mButtonStates;
		uint32	mAxisButtonStates;
		sint32	mAxisVals[6];
		sint32	mAxisDeadVals[6];
	};

	void PollState(DecodedState& state);
	void UpdateButtons(int baseId, uint32 states, uint32 mask);

	vdrefptr<IDirectInput8> mpDI;
	vdrefptr<IDirectInputDevice8> mpDevice;
	ATInputManager *mpInputManager;

	int			mUnit;
	DecodedState	mLastSentState;
	DecodedState	mLastPolledState;
	DIJOYSTATE	mState;
};

ATControllerDirectInput::ATControllerDirectInput(IDirectInput8 *di)
	: mpDI(di)
	, mUnit(-1)
	, mpInputManager(NULL)
{
	memset(&mState, 0, sizeof mState);
	memset(&mLastSentState, 0, sizeof mLastSentState);
	memset(&mLastPolledState, 0, sizeof mLastPolledState);
}

ATControllerDirectInput::~ATControllerDirectInput() {
	Shutdown();
}

bool ATControllerDirectInput::Init(LPCDIDEVICEINSTANCE devInst, HWND hwnd, ATInputManager *inputMan) {
	HRESULT hr = mpDI->CreateDevice(devInst->guidInstance, ~mpDevice, NULL);
	if (FAILED(hr))
		return false;

	// c_dfDIJoystick requires dinput8.lib, which is not available on ARM64.
	// That's the only thing we need from that lib, so hardcoding the definition here saves
	// linking it in at all.
	DIOBJECTDATAFORMAT kJoystickObjectDataFormat[] = {
		{ &GUID_XAxis,	DIJOFS_X,			DIDFT_OPTIONAL | DIDFT_AXIS		| DIDFT_ANYINSTANCE, DIDOI_ASPECTPOSITION },
		{ &GUID_YAxis,	DIJOFS_Y,			DIDFT_OPTIONAL | DIDFT_AXIS		| DIDFT_ANYINSTANCE, DIDOI_ASPECTPOSITION },
		{ &GUID_ZAxis,	DIJOFS_Z,			DIDFT_OPTIONAL | DIDFT_AXIS		| DIDFT_ANYINSTANCE, DIDOI_ASPECTPOSITION },
		{ &GUID_RxAxis,	DIJOFS_RX,			DIDFT_OPTIONAL | DIDFT_AXIS		| DIDFT_ANYINSTANCE, DIDOI_ASPECTPOSITION },
		{ &GUID_RyAxis,	DIJOFS_RY,			DIDFT_OPTIONAL | DIDFT_AXIS		| DIDFT_ANYINSTANCE, DIDOI_ASPECTPOSITION },
		{ &GUID_RzAxis,	DIJOFS_RZ,			DIDFT_OPTIONAL | DIDFT_AXIS		| DIDFT_ANYINSTANCE, DIDOI_ASPECTPOSITION },
		{ &GUID_Slider,	DIJOFS_SLIDER(0),	DIDFT_OPTIONAL | DIDFT_AXIS		| DIDFT_ANYINSTANCE, DIDOI_ASPECTPOSITION },
		{ &GUID_Slider,	DIJOFS_SLIDER(1),	DIDFT_OPTIONAL | DIDFT_AXIS		| DIDFT_ANYINSTANCE, DIDOI_ASPECTPOSITION },
		{ &GUID_POV,	DIJOFS_POV(0),		DIDFT_OPTIONAL | DIDFT_POV		| DIDFT_ANYINSTANCE, 0 },
		{ &GUID_POV,	DIJOFS_POV(1),		DIDFT_OPTIONAL | DIDFT_POV		| DIDFT_ANYINSTANCE, 0 },
		{ &GUID_POV,	DIJOFS_POV(2),		DIDFT_OPTIONAL | DIDFT_POV		| DIDFT_ANYINSTANCE, 0 },
		{ &GUID_POV,	DIJOFS_POV(3),		DIDFT_OPTIONAL | DIDFT_POV		| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(0),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(1),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(2),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(3),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(4),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(5),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(6),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(7),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(8),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(9),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(10),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(11),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(12),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(13),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(14),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(15),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(16),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(17),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(18),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(19),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(20),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(21),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(22),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(23),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(24),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(25),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(26),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(27),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(28),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(29),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(30),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
		{ nullptr,		DIJOFS_BUTTON(31),	DIDFT_OPTIONAL | DIDFT_BUTTON	| DIDFT_ANYINSTANCE, 0 },
	};

	DIDATAFORMAT kJoystickDataFormat = {
		sizeof(DIDATAFORMAT),
		sizeof(DIOBJECTDATAFORMAT),
		DIDF_ABSAXIS,
		sizeof(DIJOYSTATE),
		vdcountof(kJoystickObjectDataFormat),
		&kJoystickObjectDataFormat[0],
	};

	hr = mpDevice->SetDataFormat(&kJoystickDataFormat);
	if (FAILED(hr)) {
		Shutdown();
		return false;
	}

	hr = mpDevice->SetCooperativeLevel(hwnd, DISCL_BACKGROUND | DISCL_NONEXCLUSIVE);
	if (FAILED(hr)) {
		Shutdown();
		return false;
	}

	// Set the axis ranges.
	//
	// It'd be better to do this by enumerating the axis objects, but it seems that in
	// XP this can give completely erroneous dwOfs values, i.e. Y Axis = 0, X Axis = 4,
	// RZ axis = 12, etc. Therefore, we just loop through all of the axes.
	DIPROPRANGE range;
	range.diph.dwSize = sizeof(DIPROPRANGE);
	range.diph.dwHeaderSize = sizeof(DIPROPHEADER);
	range.diph.dwHow = DIPH_BYOFFSET;
	range.lMin = -1024;
	range.lMax = +1024;

	static const int kOffsets[6]={
		DIJOFS_X,
		DIJOFS_Y,
		DIJOFS_Z,
		DIJOFS_RX,
		DIJOFS_RY,
		DIJOFS_RZ,
	};

	for(int i=0; i<6; ++i) {
		range.diph.dwObj = kOffsets[i];
		mpDevice->SetProperty(DIPROP_RANGE, &range.diph);
	}

//	mpDevice->EnumObjects(StaticInitAxisCallback, this, DIDFT_ABSAXIS);

	mpInputManager = inputMan;

	memcpy(&mId, &devInst->guidInstance, sizeof mId);

	// VID 054C - Sony
	// PID 05C4 - DualShock 4 controller
	// PID 09CC - DualShock 4 Slim controller
	const bool mbIsDualShock4 = (devInst->guidProduct.Data1 == 0x05C4054C || devInst->guidProduct.Data1 == 0x09CC054C);
	mUnit = inputMan->RegisterInputUnit(mId, devInst->tszInstanceName, mbIsDualShock4 ? this : NULL);

	return true;
}

void ATControllerDirectInput::Shutdown() {
	if (mUnit >= 0) {
		UpdateButtons(kATInputCode_JoyButton0, 0, mLastSentState.mButtonStates);
		UpdateButtons(kATInputCode_JoyStick1Left, 0, mLastSentState.mAxisButtonStates);
		memset(&mLastSentState, 0, sizeof mLastSentState);

		mpInputManager->UnregisterInputUnit(mUnit);
		mUnit = -1;
	}

	mpInputManager = NULL;
	mpDevice = NULL;
}

bool ATControllerDirectInput::Poll(bool& bigActivity) {
	DecodedState state;
	PollState(state);

	bool change = false;

	uint32 buttonDelta = (state.mButtonStates ^ mLastSentState.mButtonStates);
	if (buttonDelta) {
		change = true;
		UpdateButtons(kATInputCode_JoyButton0, state.mButtonStates, buttonDelta);
	}

	uint32 axisButtonDelta = (state.mAxisButtonStates ^ mLastSentState.mAxisButtonStates);
	if (axisButtonDelta) {
		change = true;
		UpdateButtons(kATInputCode_JoyStick1Left, state.mAxisButtonStates, axisButtonDelta);
	}

	if (buttonDelta || axisButtonDelta)
		bigActivity = true;

	for(int i=0; i<6; ++i) {
		if (state.mAxisVals[i] != mLastSentState.mAxisVals[i] ||
			state.mAxisDeadVals[i] != mLastSentState.mAxisDeadVals[i])
		{
			change = true;
			// We set DirectInput to use [-1024, 1024], but we want +/-64K.
			mpInputManager->OnAxisInput(mUnit, kATInputCode_JoyHoriz1 + i, state.mAxisVals[i], state.mAxisDeadVals[i]);
		}
	}

	mLastSentState = state;
	mLastPolledState = state;

	return change;
}

bool ATControllerDirectInput::PollForCapture(int& unit, uint32& inputCode, uint32& inputCode2) {
	DecodedState state;
	PollState(state);

	const uint32 newButtons = state.mButtonStates & ~mLastPolledState.mButtonStates;
	const uint32 newAxisButtons = state.mAxisButtonStates & ~mLastPolledState.mAxisButtonStates;

	mLastPolledState = state;

	if (newButtons) {
		unit = mUnit;
		inputCode = kATInputCode_JoyButton0 + VDFindLowestSetBitFast(newButtons);
		inputCode2 = 0;
		return true;
	}

	if (newAxisButtons) {
		unit = mUnit;

		const int idx = VDFindLowestSetBitFast(newAxisButtons);
		inputCode = kATInputCode_JoyStick1Left + idx;
		inputCode2 = kATInputCode_JoyHoriz1 + (idx >> 1);
		return true;
	}

	return false;
}

void ATControllerDirectInput::PollForCapture(ATJoystickState& dst) {
	DecodedState state;
	PollState(state);

	mLastPolledState = state;

	dst.mUnit = mUnit;
	dst.mButtons = state.mButtonStates;
	dst.mAxisButtons = state.mAxisButtonStates;
	memcpy(dst.mAxisVals, state.mAxisVals, sizeof dst.mAxisVals);
	memcpy(dst.mDeadifiedAxisVals, state.mAxisDeadVals, sizeof dst.mDeadifiedAxisVals);
}

bool ATControllerDirectInput::GetInputCodeName(uint32 id, VDStringW& name) const {
	static const wchar_t *const kButtonNames[]={
		L"Square button",
		L"Cross button",
		L"Circle button",
		L"Triangle button",
		L"L1 button",
		L"R1 button",
		L"L2 button",
		L"R2 button",
		L"Share button",
		L"Options button",
		L"L3 button",
		L"R3 button",
		L"PS button",
		L"Trackpad button"
	};

	uint32 buttonIdx = (uint32)(id - kATInputCode_JoyButton0);

	if (buttonIdx < vdcountof(kButtonNames)) {
		name = kButtonNames[buttonIdx];
		return true;
	}

	static const wchar_t *const kAxisButtonNames[]={
		L"Left stick left",
		L"Left stick right",
		L"Left stick up",
		L"Left stick down",
		L"Right stick left",
		L"Right stick right",
		NULL,
		L"Left trigger pressed",
		NULL,
		L"Right trigger pressed",
		L"Right stick up",
		L"Right stick down",
		L"D-pad left",
		L"D-pad right",
		L"D-pad up",
		L"D-pad down",
	};

	const uint32 axisButtonIdx = (uint32)(id - kATInputCode_JoyStick1Left);
	if (axisButtonIdx < vdcountof(kAxisButtonNames) && kAxisButtonNames[axisButtonIdx]) {
		name = kAxisButtonNames[axisButtonIdx];
		return true;
	}

	static const wchar_t *const kAxisNames[]={
		L"Left stick horiz.",
		L"Left stick vert.",
		L"Right stick horiz.",
		L"Left trigger",
		L"Right trigger",
		L"Right stick vert.",
	};

	const uint32 axisIdx = (uint32)(id - kATInputCode_JoyHoriz1);
	if (axisIdx < vdcountof(kAxisNames)) {
		name = kAxisNames[axisIdx];
		return true;
	}

	return false;
}

void ATControllerDirectInput::UpdateButtons(int baseId, uint32 states, uint32 mask) {
	for(int i=0; i<32; ++i) {
		uint32 bit = (1 << i);
		if (mask & bit) {
			if (states & bit)
				mpInputManager->OnButtonDown(mUnit, baseId + i);
			else
				mpInputManager->OnButtonUp(mUnit, baseId + i);
		}
	}
}

void ATControllerDirectInput::PollState(DecodedState& state) {
	HRESULT hr = mpDevice->Poll();

	if (FAILED(hr))
		hr = mpDevice->Acquire();

	if (SUCCEEDED(hr))
		hr = mpDevice->GetDeviceState(sizeof(DIJOYSTATE), &mState);

	if (FAILED(hr)) {
		memset(&mState, 0, sizeof mState);
		mState.rgdwPOV[0] = 0xFFFFFFFFU;
	}

	uint32 axisButtonStates = 0;

	uint32 pov = mState.rgdwPOV[0] & 0xffff;
	if (pov < 0xffff) {
		uint32 octant = ((pov + 2250) / 4500) & 7;

		static const uint32 kPOVLookup[8]={
			(1 << 14),	// up
			(1 << 14) | (1 << 13),
			(1 << 13),	// right
			(1 << 15) | (1 << 13),
			(1 << 15),	// down
			(1 << 15) | (1 << 12),
			(1 << 12),	// left
			(1 << 12) | (1 << 14),
		};

		axisButtonStates = kPOVLookup[octant];
	}

	state.mAxisVals[0] = mState.lX << 6;
	state.mAxisVals[1] = mState.lY << 6;
	state.mAxisVals[2] = mState.lZ << 6;
	state.mAxisVals[3] = mState.lRx << 6;
	state.mAxisVals[4] = mState.lRy << 6;
	state.mAxisVals[5] = mState.lRz << 6;

	axisButtonStates |= ConvertAnalogToDirectionMask(state.mAxisVals[0], -state.mAxisVals[1], mTransforms.mStickDigitalDeadZone);
	axisButtonStates |= ConvertAnalogToDirectionMask(state.mAxisVals[3], -state.mAxisVals[4], mTransforms.mStickDigitalDeadZone) << 6;
	if (state.mAxisVals[2] < -mTransforms.mTriggerDigitalDeadZone) axisButtonStates |= (1 << 4);
	if (state.mAxisVals[2] > +mTransforms.mTriggerDigitalDeadZone) axisButtonStates |= (1 << 5);
	if (state.mAxisVals[5] < -mTransforms.mTriggerDigitalDeadZone) axisButtonStates |= (1 << 10);
	if (state.mAxisVals[5] > +mTransforms.mTriggerDigitalDeadZone) axisButtonStates |= (1 << 11);

	// We treat the X/Y axes as one 2D controller, and lRx/lRy as another.
	// The Z axes are deadified as 1D vertical axes.
	const float stickAnalogDeadZone = (float)mTransforms.mStickAnalogDeadZone;
	const float triggerAnalogDeadZone = (float)mTransforms.mTriggerAnalogDeadZone;
	for(int i=0; i<=3; i+=3) {
		float x = (float)state.mAxisVals[i];
		float y = (float)state.mAxisVals[i+1];

		float lensq = x*x + y*y;
		if (lensq < stickAnalogDeadZone * stickAnalogDeadZone) {
			state.mAxisDeadVals[i] = 0;
			state.mAxisDeadVals[i+1] = 0;
		} else {
			float len = sqrtf(lensq);
			float scale = (len - stickAnalogDeadZone) / (len * (65536.0f - stickAnalogDeadZone));

			scale *= powf(len * scale, mTransforms.mStickAnalogPower - 1.0f);
			scale *= 65536.0f;

			state.mAxisDeadVals[i] = VDRoundToInt32(x * scale);
			state.mAxisDeadVals[i+1] = VDRoundToInt32(y * scale);
		}

		float z = (float)state.mAxisVals[i+2];
		float zlen = fabsf(z);
		if (zlen < triggerAnalogDeadZone)
			state.mAxisDeadVals[i+2] = 0;
		else {
			float zscale = (zlen - triggerAnalogDeadZone) / (zlen * (65536.0f - triggerAnalogDeadZone));

			zscale *= powf(zlen * zscale, mTransforms.mTriggerAnalogPower - 1.0f);
			zscale *= 65536.0f;

			state.mAxisDeadVals[i+2] = VDRoundToInt32(z * zscale);
		}
	}

	uint32 buttonStates = 0;
	for(int i=0; i<32; ++i) {
		buttonStates >>= 1;
		if (mState.rgbButtons[i])
			buttonStates |= 0x80000000;
	}

	state.mButtonStates = buttonStates;
	state.mAxisButtonStates = axisButtonStates;
}

///////////////////////////////////////////////////////////////////////////////

class ATJoystickManager final : public IATJoystickManager {
public:
	ATJoystickManager();
	~ATJoystickManager();

	bool Init(void *hwnd, ATInputManager *inputMan) override;
	void Shutdown() override;

	ATJoystickTransforms GetTransforms() const override;
	void SetTransforms(const ATJoystickTransforms&) override;

	void SetCaptureMode(bool capture) override { mbCaptureMode = capture; }

	void SetOnActivity(const vdfunction<void()>& fn) override { mpActivityFn = fn; }

	void RescanForDevices() override;
	PollResult Poll() override;
	bool PollForCapture(int& unit, uint32& inputCode, uint32& inputCode2) override;
	const ATJoystickState *PollForCapture(uint32& count) override;

	uint32 GetJoystickPortStates() const override;

protected:
	static BOOL CALLBACK StaticJoystickCallback(LPCDIDEVICEINSTANCE devInst, LPVOID pThis);
	BOOL JoystickCallback(LPCDIDEVICEINSTANCE devInst);

	bool mbCOMInitialized = false;
	bool mbDIAttempted = false;
	bool mbCaptureMode = false;
	HWND mhwnd = nullptr;
	vdrefptr<IDirectInput8> mpDI;
	ATInputManager *mpInputManager = nullptr;

	vdfunction<void()> mpActivityFn;

	ATJoystickTransforms mTransforms = ATJoystickTransforms {
		0x2666,	// 15%
		0x7333,	// 45%
		1,
		0x0CCC,	// 5%
		0x3333,	// 20%
		1
	};

	typedef vdfastvector<ATController *> Controllers;
	Controllers mControllers;

	vdfastvector<DWORD> mXInputDeviceIds;
	ATXInputBinding mXInputBinding;

	vdfastvector<ATJoystickState> mJoyStates;
};

IATJoystickManager *ATCreateJoystickManager() {
	return new ATJoystickManager;
}

ATJoystickManager::ATJoystickManager() {
}

ATJoystickManager::~ATJoystickManager() {
	Shutdown();
}

bool ATJoystickManager::Init(void *hwnd, ATInputManager *inputMan) {
	HRESULT hr;
	
	if (!mbCOMInitialized) {
		hr = CoInitializeEx(NULL, COINIT_APARTMENTTHREADED);
		if (FAILED(hr))
			return false;

		mbCOMInitialized = true;
	}

	if (!mbDIAttempted) {
		mbDIAttempted = true;
		hr = CoCreateInstance(CLSID_DirectInput8, NULL, CLSCTX_INPROC_SERVER, IID_IDirectInput8, (void **)~mpDI);
		if (SUCCEEDED(hr)) {
			hr = mpDI->Initialize(GetModuleHandle(NULL), DIRECTINPUT_VERSION);
			if (FAILED(hr))
				mpDI = nullptr;
		}
	}

	// try to initialize XInput -- OK if this fails
	mXInputBinding.Init();

	mhwnd = (HWND)hwnd;
	mpInputManager = inputMan;

	RescanForDevices();
	return true;
}

void ATJoystickManager::Shutdown() {
	while(!mControllers.empty()) {
		ATController *ctrl = mControllers.back();
		mControllers.pop_back();

		delete ctrl;
	}

	mpDI.clear();

	mXInputBinding.Shutdown();

	mpInputManager = NULL;

	if (mbCOMInitialized) {
		CoUninitialize();
		mbCOMInitialized = false;
	}
}

ATJoystickTransforms ATJoystickManager::GetTransforms() const {
	return mTransforms;
}

void ATJoystickManager::SetTransforms(const ATJoystickTransforms& transforms) {
	mTransforms = transforms;

	for(auto *ctrl : mControllers) {
		ctrl->SetTransforms(mTransforms);
	}
}

void ATJoystickManager::RescanForDevices() {
	Controllers::iterator it(mControllers.begin()), itEnd(mControllers.end());
	for(; it!=itEnd; ++it) {
		ATController *ctrl = *it;
		ctrl->SetMarked(false);
	}

	if (mXInputBinding.IsInited()) {
		mXInputDeviceIds.clear();
		GetXInputDevices(mXInputDeviceIds);

		// Since we're XInput-based, we don't necessarily have a GUID. Therefore, we
		// make one up.
		// {B1C7FF47-2B26-4F71-9ADB-2B23C08BA8C3}
		static const ATInputUnitIdentifier kBaseXInputId = {
			{
				(char)0x47, (char)0xFF, (char)0xC7, (char)0xB1, (char)0x26, (char)0x2B, (char)0x71, (char)0x4F,
				(char)0x9A, (char)0xDB, (char)0x2B, (char)0x23, (char)0xC0, (char)0x8B, (char)0xA8, (char)0xC3
			}
		};

		ATInputUnitIdentifier id = kBaseXInputId;

		XINPUT_STATE xis;

		for(int i=0; i<4; ++i) {
			++id.buf[15];

			if (ERROR_SUCCESS == mXInputBinding.mpXInputGetState(i, &xis)) {
				ATInputUnitIdentifier id = kBaseXInputId;
				id.buf[15] += i;

				Controllers::const_iterator it(mControllers.begin()), itEnd(mControllers.end());
				bool found = false;

				for(; it!=itEnd; ++it) {
					ATController *ctrl = *it;

					if (ctrl->GetId() == id) {
						ctrl->SetMarked(true);
						found = true;
						break;
					}
				}

				if (!found) {
					vdautoptr<ATControllerXInput> dev(new ATControllerXInput(mXInputBinding, i, mpInputManager, id));

					if (dev) {
						dev->SetTransforms(mTransforms);
						dev->SetMarked(true);
						mControllers.push_back(dev);
						dev.release();
					}
				}
			}
		}
	}

	std::sort(mXInputDeviceIds.begin(), mXInputDeviceIds.end());

	if (mpDI) {
		mpDI->EnumDevices(DI8DEVCLASS_GAMECTRL, StaticJoystickCallback, this, DIEDFL_ATTACHEDONLY);

		// garbage collect dead controllers
		it = mControllers.begin();
		while(it != mControllers.end()) {
			ATController *ctrl = *it;

			if (ctrl->IsMarked()) {
				++it;
			} else {
				delete ctrl;

				if (&*it == &mControllers.back()) {
					mControllers.pop_back();
					break;
				}

				*it = mControllers.back();
				mControllers.pop_back();
			}
		}
	}
}

ATJoystickManager::PollResult ATJoystickManager::Poll() {
	if (mbCaptureMode || mControllers.empty())
		return kPollResult_NoControllers;

	Controllers::const_iterator it(mControllers.begin()), itEnd(mControllers.end());
	bool change = false;
	bool bigChange = false;

	for(; it!=itEnd; ++it) {
		ATController *ctrl = *it;

		change = ctrl->Poll(bigChange);
	}

	if (bigChange) {
		if (mpActivityFn)
			mpActivityFn();
	}

	return change ? kPollResult_OK : kPollResult_NoActivity;
}

bool ATJoystickManager::PollForCapture(int& unit, uint32& inputCode, uint32& inputCode2) {
	Controllers::const_iterator it(mControllers.begin()), itEnd(mControllers.end());
	for(; it!=itEnd; ++it) {
		ATController *ctrl = *it;

		if (ctrl->PollForCapture(unit, inputCode, inputCode2))
			return true;
	}

	return false;
}

const ATJoystickState *ATJoystickManager::PollForCapture(uint32& count) {
	size_t n = mControllers.size();

	mJoyStates.resize(n);

	for(size_t i=0; i<n; ++i)
		mControllers[i]->PollForCapture(mJoyStates[i]);

	count = (uint32)n;
	return mJoyStates.data();
}

uint32 ATJoystickManager::GetJoystickPortStates() const {
	return 0;
}

BOOL CALLBACK ATJoystickManager::StaticJoystickCallback(LPCDIDEVICEINSTANCE devInst, LPVOID pvThis) {
	ATJoystickManager *pThis = (ATJoystickManager *)pvThis;

	return pThis->JoystickCallback(devInst);
}

BOOL ATJoystickManager::JoystickCallback(LPCDIDEVICEINSTANCE devInst) {
	// check if this device is already covered under XInput
	if (std::binary_search(mXInputDeviceIds.begin(), mXInputDeviceIds.end(), devInst->guidProduct.Data1))
		return DIENUM_CONTINUE;

	ATInputUnitIdentifier id;
	memcpy(&id, &devInst->guidInstance, sizeof id);

	Controllers::const_iterator it(mControllers.begin()), itEnd(mControllers.end());
	for(; it!=itEnd; ++it) {
		ATController *ctrl = *it;

		if (ctrl->GetId() == id) {
			ctrl->SetMarked(true);
			return DIENUM_CONTINUE;
		}
	}

	vdautoptr<ATControllerDirectInput> dev(new ATControllerDirectInput(mpDI));

	if (dev) {
		if (dev->Init(devInst, mhwnd, mpInputManager)) {
			dev->SetTransforms(mTransforms);
			dev->SetMarked(true);
			mControllers.push_back(dev);
			dev.release();
		}
	}

	return DIENUM_CONTINUE;
}
