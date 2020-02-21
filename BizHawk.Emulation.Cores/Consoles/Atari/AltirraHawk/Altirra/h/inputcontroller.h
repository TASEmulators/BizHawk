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

#ifndef f_AT_INPUTCONTROLLER_H
#define f_AT_INPUTCONTROLLER_H

#include <vd2/system/vdstl.h>
#include <at/atcore/scheduler.h>

class ATGTIAEmulator;
class ATPokeyEmulator;
class ATInputManager;
class ATAnticEmulator;
class ATPIAEmulator;
class ATPortInputController;

///////////////////////////////////////////////////////////////////////////

enum ATInputTrigger : uint32 {
	kATInputTrigger_Button0		= 0x0000,
	kATInputTrigger_Up			= 0x0100,
	kATInputTrigger_Down		= 0x0101,
	kATInputTrigger_Left		= 0x0102,
	kATInputTrigger_Right		= 0x0103,
	kATInputTrigger_Start		= 0x0200,
	kATInputTrigger_Select		= 0x0201,
	kATInputTrigger_Option		= 0x0202,
	kATInputTrigger_Turbo		= 0x0203,
	kATInputTrigger_ColdReset	= 0x0204,
	kATInputTrigger_WarmReset	= 0x0205,
	kATInputTrigger_KeySpace	= 0x0300,
	kATInputTrigger_5200_0		= 0x0400,
	kATInputTrigger_5200_1		= 0x0401,
	kATInputTrigger_5200_2		= 0x0402,
	kATInputTrigger_5200_3		= 0x0403,
	kATInputTrigger_5200_4		= 0x0404,
	kATInputTrigger_5200_5		= 0x0405,
	kATInputTrigger_5200_6		= 0x0406,
	kATInputTrigger_5200_7		= 0x0407,
	kATInputTrigger_5200_8		= 0x0408,
	kATInputTrigger_5200_9		= 0x0409,
	kATInputTrigger_5200_Star	= 0x040A,
	kATInputTrigger_5200_Pound	= 0x040B,
	kATInputTrigger_5200_Start	= 0x040C,
	kATInputTrigger_5200_Pause	= 0x040D,
	kATInputTrigger_5200_Reset	= 0x040E,
	kATInputTrigger_UILeft		= 0x0500,
	kATInputTrigger_UIRight		= 0x0501,
	kATInputTrigger_UIUp		= 0x0502,
	kATInputTrigger_UIDown		= 0x0503,
	kATInputTrigger_UIAccept	= 0x0504,	// PSx[X], Xbox[A]
	kATInputTrigger_UIReject	= 0x0505,	// PSx[O], Xbox[B]
	kATInputTrigger_UIMenu		= 0x0506,	// PSx[T], Xbox[Y]
	kATInputTrigger_UIOption	= 0x0507,	// PSx[S], Xbox[X]
	kATInputTrigger_UISwitchLeft	= 0x0508,
	kATInputTrigger_UISwitchRight	= 0x0509,
	kATInputTrigger_UILeftShift		= 0x050A,
	kATInputTrigger_UIRightShift	= 0x050B,
	kATInputTrigger_Axis0		= 0x0800,
	kATInputTrigger_Flag0		= 0x0900,
	kATInputTrigger_ClassMask	= 0xFF00,
	kATInputTrigger_Mask		= 0xFFFF,

	// D2D: Button state as usual.
	// D2A: Absolute positioning.
	// A2D: Threshold.
	// A2A: Absolute positioning.
	kATInputTriggerMode_Default		= 0x00000000,

	kATInputTriggerMode_AutoFire	= 0x00010000,

	kATInputTriggerMode_Toggle		= 0x00020000,
	kATInputTriggerMode_ToggleAF	= 0x00030000,

	// D2D: N/A
	// D2A: Accumulate deltas.
	// A2D: N/A
	// A2A: Accumulate deltas.
	kATInputTriggerMode_Relative	= 0x00040000,

	// D2D: N/A
	// D2A: Position -> Value.
	// A2D: N/A
	// A2A: Position -> Value.
	kATInputTriggerMode_Absolute	= 0x00050000,

	// D2D: Starts on, invert state.
	// D2A: N/A
	// A2D: N/A
	// A2A: Axis reversed.
	kATInputTriggerMode_Inverted	= 0x00060000,

	kATInputTriggerMode_Mask		= 0x000F0000,
	kATInputTriggerSpeed_Mask		= 0x00F00000,
	kATInputTriggerSpeed_Shift		= 20,
	kATInputTriggerAccel_Mask		= 0x0F000000,
	kATInputTriggerAccel_Shift		= 24
};

///////////////////////////////////////////////////////////////////////////

class ATLightPenPort {
public:
	ATLightPenPort();

	void Init(ATAnticEmulator *antic);

	void SetAdjust(int x, int y) {
		mAdjustX = x;
		mAdjustY = y;
	}

	int GetAdjustX() const { return mAdjustX; }
	int GetAdjustY() const { return mAdjustY; }

	void SetIgnorePort34(bool ignore);
	void SetColorClockPhase(bool odd);
	void SetPortTriggerState(int index, bool state);

protected:
	ATAnticEmulator *mpAntic;
	uint8 mTriggerState;
	uint8 mTriggerStateMask;
	int mAdjustX;
	int mAdjustY;
	bool mbOddPhase;
};

///////////////////////////////////////////////////////////////////////////

class ATPortController {
public:
	ATPortController();
	~ATPortController();

	void Init(ATGTIAEmulator *gtia, ATPokeyEmulator *pokey, ATPIAEmulator *pia, ATLightPenPort *lightPen, int index);
	void Shutdown();

	void SetMultiMask(uint8 mask);

	ATGTIAEmulator& GetGTIA() const { return *mpGTIA; }
	ATPokeyEmulator& GetPokey() const { return *mpPokey; }
	uint8 GetPortValue() const { return mPortValue; }

	int AllocatePortInput(bool port2, int multiIndex);
	void FreePortInput(int index);

	void SetPortInput(int index, uint32 portBits);
	void ResetPotPositions();
	void SetPotPosition(int offset, uint8 pos);
	void SetPotHiPosition(int offset, int hipos, bool grounded = false);

	int AllocatePortOutput(ATPortInputController *target, uint8 mask);
	void SetPortOutputMask(int index, uint8 mask);
	void FreePortOutput(int index);
	uint8 GetPortOutputState() const;

protected:
	void UpdatePortValue();
	void UpdatePortOutputRegistration();

	static void OnPortOutputUpdated(void *data, uint32 outputState);

	int mPIAInputIndex;
	int mPIAOutputIndex;
	uint8 mPortValue;
	bool mbTrigger1;
	bool mbTrigger2;
	uint32 mMultiMask;

	ATGTIAEmulator *mpGTIA;
	ATPokeyEmulator *mpPokey;
	ATPIAEmulator *mpPIA;

	ATLightPenPort *mpLightPen;
	int mTriggerIndex;

	typedef vdfastvector<uint32> PortInputs;
	PortInputs mPortInputs;

	struct PortOutput {
		ATPortInputController *mpTarget;
		uint8 mMask;
	};

	typedef vdfastvector<PortOutput> PortOutputs;
	PortOutputs mPortOutputs;
};

///////////////////////////////////////////////////////////////////////////

class ATPortInputController {
	ATPortInputController(const ATPortInputController&) = delete;
	ATPortInputController& operator=(const ATPortInputController&) = delete;
public:
	friend class ATPortController;

	ATPortInputController();
	~ATPortInputController();

	void Attach(ATPortController *pc, bool port2, int multiIndex = -1);
	void Detach();

	virtual void Tick() {}

	virtual bool IsActive() const { return true; }

	virtual bool Select5200Controller(int index, bool potsEnabled) { return false; }
	virtual void SetDigitalTrigger(uint32 trigger, bool state) {}

	/// Apply a delta to an analog target. The delta is in 16:16 fixed point, where
	/// +/-2 is full range (since the value range is [-1, 1]).
	virtual void ApplyImpulse(uint32 trigger, int ds) {}

	/// Set the position of an analog target. The position is in 16:16 fixed point,
	/// where the range is [-1, 1].
	virtual void ApplyAnalogInput(uint32 trigger, int ds) {}

protected:
	void SetPortOutput(uint32 portBits);
	void SetPotPosition(bool second, uint8 pos);
	void SetPotHiPosition(bool second, int pos, bool grounded = false);
	void SetOutputMonitorMask(uint8 mask);
	void UpdateOutput(uint8 state);

	virtual void OnAttach() {}
	virtual void OnDetach() {}
	virtual void OnPortOutputChanged(uint8 outputState) {}

	ATPortController *mpPortController;
	int mPortInputIndex;
	bool mbPort2;
	uint8 mPortOutputMask;
	uint8 mPortOutputState;
	int mPortOutputIndex;
};

///////////////////////////////////////////////////////////////////////////

class ATMouseController final : public ATPortInputController, public IATSchedulerCallback {
	ATMouseController(const ATMouseController&) = delete;
	ATMouseController& operator=(const ATMouseController&) = delete;
public:
	ATMouseController(bool amigaMode);
	~ATMouseController();

	void Init(ATScheduler *scheduler);

	void SetPosition(int x, int y);
	void AddDelta(int dx, int dy);

	void SetButtonState(int button, bool state);

	virtual void SetDigitalTrigger(uint32 trigger, bool state);
	virtual void ApplyImpulse(uint32 trigger, int ds);

protected:
	void EnableUpdate();

	void OnScheduledEvent(uint32 id);
	void Update(bool doX, bool doY);
	void UpdatePort();

	void OnAttach();
	void OnDetach();

	uint32	mPortBits;
	uint32	mTargetX;
	uint32	mTargetY;
	uint32	mAccumX;
	uint32	mAccumY;
	bool	mbButtonState[2];
	bool	mbAmigaMode;

	ATEvent *mpUpdateXEvent;
	ATEvent *mpUpdateYEvent;
	ATScheduler *mpScheduler;
};

///////////////////////////////////////////////////////////////////////////

class ATTrackballController final : public ATPortInputController, public IATSchedulerCallback {
	ATTrackballController(const ATTrackballController&) = delete;
	ATTrackballController& operator=(const ATTrackballController&) = delete;
public:
	ATTrackballController();
	~ATTrackballController();

	void Init(ATScheduler *scheduler);

	void SetPosition(int x, int y);
	void AddDelta(int dx, int dy);

	void SetButtonState(int button, bool state);

	virtual void SetDigitalTrigger(uint32 trigger, bool state);
	virtual void ApplyImpulse(uint32 trigger, int ds);

protected:
	void OnScheduledEvent(uint32 id);
	void Update();

	void OnAttach();
	void OnDetach();

	uint32	mPortBits;
	uint32	mTargetX;
	uint32	mTargetY;
	uint32	mAccumX;
	uint32	mAccumY;
	bool	mbButtonState;

	ATEvent *mpUpdateEvent;
	ATScheduler *mpScheduler;
};

///////////////////////////////////////////////////////////////////////////

class ATPaddleController final : public ATPortInputController {
	ATPaddleController(const ATPaddleController&) = delete;
	ATPaddleController& operator=(const ATPaddleController&) = delete;
public:
	ATPaddleController();
	~ATPaddleController();

	void SetHalf(bool second);

	void AddDelta(int delta);
	void SetTrigger(bool enable);

	virtual void SetDigitalTrigger(uint32 trigger, bool state);
	virtual void ApplyAnalogInput(uint32 trigger, int ds);
	virtual void ApplyImpulse(uint32 trigger, int ds);

	virtual void Tick();

	virtual void OnDetach();

protected:
	bool mbSecond;
	uint32 mPortBits;
	int mRawPos;
	int mRotIndex;
	int mRotX[4];
	int mRotY[4];
	float mRotXLast;
	float mRotYLast;
};

///////////////////////////////////////////////////////////////////////////

class ATTabletController final : public ATPortInputController {
	ATTabletController(const ATTabletController&) = delete;
	ATTabletController& operator=(const ATTabletController&) = delete;
public:
	ATTabletController(int styUpPos, bool invertY);
	~ATTabletController();

	virtual void SetDigitalTrigger(uint32 trigger, bool state);
	virtual void ApplyAnalogInput(uint32 trigger, int ds);
	virtual void ApplyImpulse(uint32 trigger, int ds);

protected:
	void AddDelta(int axis, int delta);
	void SetPos(int axis, int pos);

	uint32 mPortBits;
	int mRawPos[2];
	bool	mbInvertY;
	bool	mbStylusUp;
	int		mStylusUpPos;
};

///////////////////////////////////////////////////////////////////////////

class ATJoystickController final : public ATPortInputController {
	ATJoystickController(const ATJoystickController&) = delete;
	ATJoystickController& operator=(const ATJoystickController&) = delete;
public:
	ATJoystickController();
	~ATJoystickController();

	virtual void SetDigitalTrigger(uint32 trigger, bool state);

protected:
	void UpdatePortOutput();

	uint32 mPortBits;
};

///////////////////////////////////////////////////////////////////////////

class ATDrivingController final : public ATPortInputController {
	ATDrivingController(const ATDrivingController&) = delete;
	ATDrivingController& operator=(const ATDrivingController&) = delete;
public:
	ATDrivingController();
	~ATDrivingController();

	void SetDigitalTrigger(uint32 trigger, bool state) override;
	void ApplyImpulse(uint32 trigger, int ds) override;

protected:
	void AddDelta(int delta);

	uint32 mPortBits = 0;
	uint32 mRawPos = 0;
};

///////////////////////////////////////////////////////////////////////////

class ATConsoleController final : public ATPortInputController {
	ATConsoleController(const ATConsoleController&) = delete;
	ATConsoleController& operator=(const ATConsoleController&) = delete;
public:
	ATConsoleController(ATInputManager *im);
	~ATConsoleController();

	virtual void SetDigitalTrigger(uint32 trigger, bool state);

protected:
	ATInputManager *const mpParent;
};

///////////////////////////////////////////////////////////////////////////

class ATInputStateController final : public ATPortInputController {
	ATInputStateController(const ATInputStateController&) = delete;
	ATInputStateController& operator=(const ATInputStateController&) = delete;
public:
	ATInputStateController(ATInputManager *im, uint32 flagBase);
	~ATInputStateController();

	virtual void SetDigitalTrigger(uint32 trigger, bool state);

protected:
	ATInputManager *const mpParent;
	const uint32 mFlagBase;
};

///////////////////////////////////////////////////////////////////////////

class AT5200ControllerController final : public ATPortInputController {
	AT5200ControllerController(const AT5200ControllerController&) = delete;
	AT5200ControllerController& operator=(const AT5200ControllerController&) = delete;
public:
	AT5200ControllerController(int index, bool trackball);
	~AT5200ControllerController();

	virtual bool Select5200Controller(int index, bool potsEnabled);
	virtual void SetDigitalTrigger(uint32 trigger, bool state);
	virtual void ApplyImpulse(uint32 trigger, int ds);
	virtual void ApplyAnalogInput(uint32 trigger, int ds);
	virtual void Tick();

protected:
	virtual void OnAttach();
	virtual void OnDetach();
	void SetKeyState(uint8 index, bool state);
	void UpdateTopButtonState();
	void SetPot(int index, int pos);
	void UpdatePot(int index);

	bool mbActive;
	bool mbPotsEnabled;
	bool mbTrackball;
	int mIndex;
	int mPot[2];
	int mJitter[2];

	bool mbUp = false;
	bool mbDown = false;
	bool mbLeft = false;
	bool mbRight = false;
	bool mbTopButton = false;
	bool mbImpulseMode = false;
	float mImpulseAccum[2] = {};
	int mImpulseAccum2[2] = {};

	bool mbKeyMatrix[64];
};

///////////////////////////////////////////////////////////////////////////

class ATLightPenController final : public ATPortInputController, public IATSchedulerCallback {
	ATLightPenController(const ATLightPenController&) = delete;
	ATLightPenController& operator=(const ATLightPenController&) = delete;
public:
	ATLightPenController();
	~ATLightPenController();

	void Init(ATScheduler *fastScheduler, ATLightPenPort *lpp);

	virtual bool IsActive() const { return mbPenDown; }

	virtual void SetDigitalTrigger(uint32 trigger, bool state);
	virtual void ApplyImpulse(uint32 trigger, int ds);
	virtual void ApplyAnalogInput(uint32 trigger, int ds);
	virtual void Tick();

protected:
	virtual void OnScheduledEvent(uint32 id);
	virtual void OnAttach();
	virtual void OnDetach();

	uint32	mPortBits;
	sint32	mPosX;
	sint32	mPosY;
	bool	mbPenDown;
	bool	mbOddPhase;

	ATEvent *mpLPEvent;
	ATScheduler *mpScheduler;
	ATLightPenPort *mpLightPen;
};

///////////////////////////////////////////////////////////////////////////

class ATKeypadController final : public ATPortInputController {
	ATKeypadController(const ATKeypadController&) = delete;
	ATKeypadController& operator=(const ATKeypadController&) = delete;
public:
	ATKeypadController();
	~ATKeypadController();

	virtual void SetDigitalTrigger(uint32 trigger, bool state);

protected:
	virtual void OnAttach();
	virtual void OnDetach();

	uint32	mPortBits;
};

///////////////////////////////////////////////////////////////////////////

class ATKeyboardController final : public ATPortInputController {
	ATKeyboardController(const ATKeyboardController&) = delete;
	ATKeyboardController& operator=(const ATKeyboardController&) = delete;
public:
	ATKeyboardController();
	~ATKeyboardController();

	void SetDigitalTrigger(uint32 trigger, bool state) override;

protected:
	void OnAttach() override;
	void OnDetach() override;
	void OnPortOutputChanged(uint8 outputState) override;

	void UpdatePortOutput();

	uint32	mKeyState;
};

///////////////////////////////////////////////////////////////////////////

#endif	// f_AT_INPUTCONTROLLER_H
