#include <stdafx.h>
#include <windows.h>
#include <at/atnativeui/dialog.h>
#include "inputmanager.h"
#include "inputcontroller.h"
#include "resource.h"
#include "joystick.h"

class ATUIDialogRebindInput : public VDDialogFrameW32 {
public:
	ATUIDialogRebindInput(ATInputManager& iman, IATJoystickManager *ijoy);

	void SetAnchorPos(const vdpoint32& pt) { mAnchorPos = pt; }
	void SetTarget(uint32 target) { mTargetCode = target; }
	void SetControllerType(ATInputControllerType type) { mControllerType = type; }

	uint32 GetInputCode() const { return mInputCode; }

protected:
	enum { kTimerId_JoyPoll = 100 };

	virtual VDZINT_PTR DlgProc(VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam);
	bool OnLoaded();
	void OnDestroy();
	bool OnTimer(uint32 id);
	bool OnCommand(uint32 id, uint32 extcode);

	uint32 mTargetCode;
	ATInputCode mInputCode;
	ATInputControllerType mControllerType;
	bool mbRightShiftWasDown;

	vdpoint32 mAnchorPos;

	ATInputManager& mInputMan;
	IATJoystickManager *const mpJoyMan;
};

ATUIDialogRebindInput::ATUIDialogRebindInput(ATInputManager& iman, IATJoystickManager *ijoy)
	: VDDialogFrameW32(IDD_INPUTMAP_REBIND)
	, mTargetCode(kATInputTrigger_Button0)
	, mControllerType(kATInputControllerType_None)
	, mInputCode(kATInputCode_None)
	, mbRightShiftWasDown(false)
	, mAnchorPos(0, 0)
	, mInputMan(iman)
	, mpJoyMan(ijoy)
{
}

VDZINT_PTR ATUIDialogRebindInput::DlgProc(VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam) {
	// Unfortunately, in order to trap keys, we must give in to the dark side and
	// use direct Win32 code here.

	switch(msg) {
		case WM_GETDLGCODE:
			SetWindowLongPtr(mhdlg, DWLP_MSGRESULT, DLGC_WANTALLKEYS);
			return TRUE;

		case WM_KEYDOWN:
		case WM_SYSKEYDOWN:
			// Block Alt, as we don't allow it to be bound.
			if (wParam == VK_MENU)
				return TRUE;

			// Shift is a special case -- we only trap that on key up so we can
			// detect Shift+Esc.
			if (wParam == VK_SHIFT) {
				// More Win32 API goofiness: the extended bit isn't set for right Shift.
				// We have to detect the situation here as by the time WM_KEYUP arrives there
				// is no information to determine which shift key was pressed.
				mbRightShiftWasDown = (GetKeyState(VK_RSHIFT) < 0);
			} else {
				// Check if we have Shift+Esc, which means to exit.
				if (wParam == VK_ESCAPE && GetKeyState(VK_SHIFT) < 0) {
					End(false);
					return TRUE;
				}

				mInputCode = (ATInputCode)wParam;

				// Special handling for Control and Return keys.
				if (wParam == VK_RETURN) {
					if (lParam & (1 << 24))
						mInputCode = kATInputCode_KeyNumpadEnter;
					else
						mInputCode = kATInputCode_KeyReturn;
				} else if (wParam == VK_CONTROL) {
					if (lParam & (1 << 24))
						mInputCode = kATInputCode_KeyRControl;
					else
						mInputCode = kATInputCode_KeyLControl;
				}

				End(true);
			}
			return TRUE;

		case WM_KEYUP:
		case WM_SYSKEYUP:
			if (wParam == VK_SHIFT) {
				if (mbRightShiftWasDown)
					mInputCode = kATInputCode_KeyRShift;
				else
					mInputCode = kATInputCode_KeyLShift;

				End(true);
			}
			return TRUE;

		case WM_SETFOCUS:
			// Prevent default dialog procedure from redirecting to button.
			return TRUE;
	}

	return VDDialogFrameW32::DlgProc(msg, wParam, lParam);
}

bool ATUIDialogRebindInput::OnLoaded() {
	VDStringW s;
	mInputMan.GetNameForTargetCode(mTargetCode, mControllerType, s);

	SetPosition(mAnchorPos);
	AdjustPosition();

	SetControlText(IDC_STATIC_TARGET, s.c_str());

	if (mpJoyMan) {
		mpJoyMan->SetCaptureMode(true);
		SetPeriodicTimer(kTimerId_JoyPoll, 20);
	}

	::SetFocus(mhdlg);

	return true;
}

void ATUIDialogRebindInput::OnDestroy() {
	if (mpJoyMan)
		mpJoyMan->SetCaptureMode(false);
}

bool ATUIDialogRebindInput::OnTimer(uint32 id) {
	if (id == kTimerId_JoyPoll) {
		if (mpJoyMan) {
			int unit;
			uint32 digitalInputCode;
			uint32 analogInputCode;
			if (mpJoyMan->PollForCapture(unit, digitalInputCode, analogInputCode)) {
				if (analogInputCode && mInputMan.IsAnalogTrigger(mTargetCode, mControllerType))
					mInputCode = (ATInputCode)analogInputCode;
				else
					mInputCode = (ATInputCode)digitalInputCode;

				End(true);
			}
		}
		return true;
	}

	return false;
}

bool ATUIDialogRebindInput::OnCommand(uint32 id, uint32 extcode) {
	switch(id) {
		case IDC_NEXT:
			mInputCode = kATInputCode_None;
			End(true);
			return true;

		case IDC_STOP:
			End(false);
			return true;
	}

	return false;
}

bool ATUIShowRebindDialog(VDGUIHandle hParent,
						  ATInputManager& inputManager,
						  IATJoystickManager *joyMan,
						  uint32 targetCode,
						  ATInputControllerType controllerType,
						  const vdpoint32& anchorPos,
						  uint32& inputCode)
{
	ATUIDialogRebindInput dlg(inputManager, joyMan);

	dlg.SetAnchorPos(anchorPos);
	dlg.SetTarget(targetCode);
	dlg.SetControllerType(controllerType);

	if (!dlg.ShowDialog(hParent))
		return false;

	inputCode = dlg.GetInputCode();
	return true;
}
