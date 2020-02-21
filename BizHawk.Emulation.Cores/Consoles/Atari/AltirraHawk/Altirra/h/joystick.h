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

#ifndef AT_JOYSTICK_H
#define AT_JOYSTICK_H

#include <vd2/system/function.h>

class ATInputManager;

struct ATJoystickState {
	uint32 mUnit;
	uint32 mButtons;
	uint32 mAxisButtons;
	sint32 mAxisVals[6];
	sint32 mDeadifiedAxisVals[6];
};

struct ATJoystickTransforms {
	sint32 mStickAnalogDeadZone;
	sint32 mStickDigitalDeadZone;
	float mStickAnalogPower;
	sint32 mTriggerAnalogDeadZone;
	sint32 mTriggerDigitalDeadZone;
	float mTriggerAnalogPower;
};

class IATJoystickManager {
public:
	virtual ~IATJoystickManager() = default;

	virtual bool Init(void *hwnd, ATInputManager *inputMan) = 0;
	virtual void Shutdown() = 0;

	virtual ATJoystickTransforms GetTransforms() const = 0;
	virtual void SetTransforms(const ATJoystickTransforms& transforms) = 0;

	virtual void SetCaptureMode(bool capture) = 0;

	virtual void SetOnActivity(const vdfunction<void()>& fn) = 0;

	virtual void RescanForDevices() = 0;

	enum PollResult {
		kPollResult_OK,
		kPollResult_NoActivity,
		kPollResult_NoControllers
	};

	virtual PollResult Poll() = 0;

	virtual bool PollForCapture(int& unit, uint32& inputCode, uint32& inputCode2) = 0;
	virtual const ATJoystickState *PollForCapture(uint32& n) = 0;

	virtual uint32 GetJoystickPortStates() const = 0;
};

IATJoystickManager *ATCreateJoystickManager();

#endif
