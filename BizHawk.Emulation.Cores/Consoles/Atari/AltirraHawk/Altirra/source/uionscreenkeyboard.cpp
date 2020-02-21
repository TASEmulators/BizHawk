#include <stdafx.h>
#include "uionscreenkeyboard.h"
#include <at/atuicontrols/uibutton.h>
#include "uikeyboard.h"
#include <at/atui/uimanager.h>
#include "simulator.h"
#include "pokey.h"

extern ATUIKeyboardOptions g_kbdOpts;

extern ATSimulator g_sim;

struct ATUIOnScreenKeyboard::KeyEntry {
	uint8 mPad;
	uint8 mWidth;
	uint8 mScanCode;
	const wchar_t *mpNormalText;
	const wchar_t *mpShiftText;
	const wchar_t *mpControlText;
	bool mbToggle;
};

const ATUIOnScreenKeyboard::KeyEntry ATUIOnScreenKeyboard::kEntries[]={
	{ 0, 4, 0x11, L"Help", NULL, NULL },
	{ 1, 6, 0x48, L"Start", NULL, NULL },
	{ 0, 6, 0x49, L"Select", NULL, NULL },
	{ 0, 6, 0x4A, L"Option", NULL, NULL },
	{ 2, 4, 0x4B, L"Reset", NULL, NULL },
	{ 2, 8, 0x40, L"Break", NULL, NULL },

	{ 0, 4, 0x1C, L"Esc", NULL, NULL },
	{ 0, 4, 0x1F, L"1", L"!", NULL },
	{ 0, 4, 0x1E, L"2", L"\"", NULL },
	{ 0, 4, 0x1A, L"3", L"#", NULL },
	{ 0, 4, 0x18, L"4", L"$", NULL },
	{ 0, 4, 0x1D, L"5", L"%", NULL },
	{ 0, 4, 0x1B, L"6", L"&", NULL },
	{ 0, 4, 0x33, L"7", L"'", NULL },
	{ 0, 4, 0x35, L"8", L"@", NULL },
	{ 0, 4, 0x30, L"9", L"(", NULL },
	{ 0, 4, 0x32, L"0", L")", NULL },
	{ 0, 4, 0x36, L"<", L"Clr" },
	{ 0, 4, 0x37, L">", L"Ins", L"Ins" },
	{ 0, 4, 0x34, L"BkSp", L"Del", L"Del" },

	{ 0, 5, 0x2C, L"Tab", L"Set", L"Clr" },
	{ 0, 4, 0x2F, L"Q", NULL, NULL },
	{ 0, 4, 0x2E, L"W", NULL, NULL },
	{ 0, 4, 0x2A, L"E", NULL, NULL },
	{ 0, 4, 0x28, L"R", NULL, NULL },
	{ 0, 4, 0x2D, L"T", NULL, NULL },
	{ 0, 4, 0x2B, L"Y", NULL, NULL },
	{ 0, 4, 0x0B, L"U", NULL, NULL },
	{ 0, 4, 0x0D, L"I", NULL, NULL },
	{ 0, 4, 0x08, L"O", NULL, NULL },
	{ 0, 4, 0x0A, L"P", NULL, NULL },
	{ 0, 4, 0x0E, L"-", L"_", L"Up" },
	{ 0, 4, 0x0F, L"=", L"|", L"Down" },
	{ 0, 4, 0x0C, L"Return", NULL, NULL },

	{ 0, 6, 0x41, L"Ctrl", NULL, NULL, true },
	{ 0, 4, 0x3F, L"A", NULL, NULL },
	{ 0, 4, 0x3E, L"S", NULL, NULL },
	{ 0, 4, 0x3A, L"D", NULL, NULL },
	{ 0, 4, 0x38, L"F", NULL, NULL },
	{ 0, 4, 0x3d, L"G", NULL, NULL },
	{ 0, 4, 0x39, L"H", NULL, NULL },
	{ 0, 4, 0x01, L"J", NULL, NULL },
	{ 0, 4, 0x05, L"K", NULL, NULL },
	{ 0, 4, 0x00, L"L", NULL, NULL },
	{ 0, 4, 0x02, L";", L":", NULL },
	{ 0, 4, 0x06, L"+", L"\\", L"Left" },
	{ 0, 4, 0x07, L"*", L"^", L"Right" },
	{ 0, 4, 0x3C, L"Caps", NULL, NULL },

	{ 0, 8, 0x42, L"Shift", NULL, NULL, true },
	{ 0, 4, 0x17, L"Z", NULL, NULL },
	{ 0, 4, 0x16, L"X", NULL, NULL },
	{ 0, 4, 0x12, L"C", NULL, NULL },
	{ 0, 4, 0x10, L"V", NULL, NULL },
	{ 0, 4, 0x15, L"B", NULL, NULL },
	{ 0, 4, 0x23, L"N", NULL, NULL },
	{ 0, 4, 0x25, L"M", NULL, NULL },
	{ 0, 4, 0x20, L",", L"[", NULL },
	{ 0, 4, 0x22, L".", L"]", NULL },
	{ 0, 4, 0x26, L"/", L"?", NULL },
	{ 0, 8, 0x42, L"Shift", NULL, NULL, true },
	{ 0, 4, 0x27, L"Inv", NULL, NULL },

	{ 0, 4, 0x21, L"Space", NULL, NULL },
};

const int ATUIOnScreenKeyboard::kRowBreaks[]={
	0,
	6,
	20,
	34,
	48,
	61,
	62
};

ATUIOnScreenKeyboard::ATUIOnScreenKeyboard()
	: mButtonWidth(32)
	, mButtonHeight(32)
	, mbShiftHeld(false)
	, mbShiftSticky(false)
	, mbControlHeld(false)
	, mbControlSticky(false)
{
	for(size_t i=0; i<vdcountof(mButtons); ++i)
		mButtons[i].mpButton = NULL;

	SetCursorImage(kATUICursorImage_Arrow);
}

ATUIOnScreenKeyboard::~ATUIOnScreenKeyboard() {
}

void ATUIOnScreenKeyboard::AutoSize() {
	ATUIContainer *c = GetParent();
	if (c) {
		const vdsize32& size = c->GetArea().size();

		int buttonSize = std::min<int>( 
			std::max<int>(16, (size.h * 2) / kSubRows),
			std::max<int>(16, size.w / kCols));

		mButtonHeight = buttonSize;
		mButtonWidth = buttonSize;
	}
	
	const vdsize32 alphaSize = mpManager->GetThemeFont(kATUIThemeFont_Default)->MeasureString(L"ABCDEFGHIJKLMNOPQRSTUVWXYZ", 26, true);
	int textBasedSize = std::max<int>(alphaSize.w * 3 / 26, alphaSize.h * 3 / 2) + 4;

	mButtonWidth = textBasedSize;
	mButtonHeight = textBasedSize;

	SetSize(vdsize32(mButtonWidth * kCols, (mButtonHeight * kSubRows) / 4));
}

void ATUIOnScreenKeyboard::OnCreate() {
	VDASSERTCT(vdcountof(kEntries) == vdcountof(mButtons));

	VDASSERTCT(vdcountof(kRowBreaks) == kRows + 1);

//	SetFillColor(0xD4D0C8);
	SetFillColor(0xD4D0C8 / 4 * 3);

	int x = 0;
	int y = -1;

	for(int i=0; i<(int)vdcountof(mButtons); ++i) {
		if (i >= kRowBreaks[y+1]) {
			x = 0;
			++y;
		}

		x += kEntries[i].mPad;

		ATUIButton *button = new ATUIButton;
		AddChild(button);

		ButtonEntry& entry = mButtons[i];
		entry.mpButton = button;
		entry.mX = x;
		entry.mY = y;
		entry.mpKeyEntry = &kEntries[i];

		button->SetText(entry.mpKeyEntry->mpNormalText);
		button->SetToggleMode(entry.mpKeyEntry->mbToggle);

		button->BindAction(kATUIVK_UIAccept, ATUIButton::kActionActivate);

		button->OnPressedEvent() = [i,this] { OnButtonPressed(i); };
		button->OnActivatedEvent() = [i,this] { OnButtonReleased(i); };

		if (kEntries[i].mbToggle)
			button->OnHeldEvent() = [i, this](bool state) { OnButtonHeld(state, i); };

		switch(entry.mpKeyEntry->mScanCode) {
			case 0x34:	// backspace
				BindAction(kATUIVK_UIOption, ATUIButton::kActionActivate, 0, button->GetInstanceId());
				break;

			case 0x21:	// space
				BindAction(kATUIVK_UIMenu, ATUIButton::kActionActivate, 0, button->GetInstanceId());
				break;

			case 0x41:	// control
				mControlButtons.push_back(i);
				break;

			case 0x42:	// shift
				mShiftButtons.push_back(i);
				break;
		}

		x += entry.mpKeyEntry->mWidth;
	}

	for(size_t row=0; row<vdcountof(kRowBreaks)-1; ++row) {
		const int base0 = row ? kRowBreaks[row-1] : 0;
		const int base1 = kRowBreaks[row];
		const int base2 = kRowBreaks[row+1];
		const int base3 = row+2 < vdcountof(kRowBreaks) ? kRowBreaks[row+2] : base2;

		for(int i=base1; i<base2; ++i) {
			if (i != base1)
				mButtons[i].mNavOrder[0] = i-1;

			if (i+1 != base2)
				mButtons[i].mNavOrder[1] = i+1;

			if (base0 != base1)
				mButtons[i].mNavOrder[2] = std::min<int>(i - base1 + base0, base1-1);

			if (base2 != base3)
				mButtons[i].mNavOrder[3] = std::min<int>(i - base1 + base2, base3-1);
		}
	}

	BindAction(kATUIVK_UIUp, kActionUp);
	BindAction(kATUIVK_UIDown, kActionDown);
	BindAction(kATUIVK_UILeft, kActionLeft);
	BindAction(kATUIVK_UIRight, kActionRight);
	BindAction(kATUIVK_UILeftShift, kActionHoldShift);
	BindAction(kATUIVK_UIRightShift, kActionHoldControl);
}

void ATUIOnScreenKeyboard::OnDestroy() {
	for(size_t i=0; i<vdcountof(mButtons); ++i)
		mButtons[i].mpButton = NULL;

	ATUIContainer::OnDestroy();
}

void ATUIOnScreenKeyboard::OnSize() {
	const int kRowVPos[]={
		0*4,
		1*4+1,
		2*4+1,
		3*4+1,
		4*4+1,
		5*4+1,
	};

	VDASSERTCT(vdcountof(kRowVPos) == kRows);

	const int bw = mButtonWidth;
	const int bh = mButtonHeight;

	for(int i=0; i<(int)vdcountof(mButtons); ++i) {
		const ButtonEntry& entry = mButtons[i];
		ATUIButton *const button = entry.mpButton;

		if (button) {
			const int x = entry.mX;
			const int y = kRowVPos[entry.mY];

			vdrect32 r(((x * bw) >> 2), (y * bh) >> 2, ((x + entry.mpKeyEntry->mWidth) * bw) >> 2, ((y + 4)*bh) >> 2);

			if (x == 0)
				r.left = 0;

			if (i == kRowBreaks[entry.mY+1]-1)
				r.right = kCols*mButtonWidth;

			button->SetArea(r);
		}
	}
}

void ATUIOnScreenKeyboard::OnActionStart(uint32 id) {
	switch(id) {
		case kActionHoldShift:
			mbShiftHeld = true;
			g_sim.GetPokey().SetShiftKeyState(true, !g_kbdOpts.mbFullRawKeys);
			UpdateLabels();
			break;

		case kActionHoldControl:
			mbControlHeld = true;
			g_sim.GetPokey().SetControlKeyState(true);
			UpdateLabels();
			break;
	}

	if (id >= kActionCustom)
		return OnActionRepeat(id);

	return ATUIContainer::OnActionStart(id);
}

void ATUIOnScreenKeyboard::OnActionRepeat(uint32 id) {
	switch(id) {
		case kActionLeft:
		case kActionRight:
		case kActionUp:
		case kActionDown:
			{
				ATUIWidget *focus = mpManager->GetFocusWindow();

				for(size_t i=0; i<vdcountof(mButtons); ++i) {
					if (mButtons[i].mpButton == focus) {
						int j = mButtons[i].mNavOrder[id - kActionLeft];

						if (j >= 0)
							mButtons[j].mpButton->Focus();
						break;
					}
				}
			}
			break;

		default:
			return ATUIContainer::OnActionRepeat(id);
	}
}

void ATUIOnScreenKeyboard::OnActionStop(uint32 id) {
	switch(id) {
		case kActionHoldShift:
			mbShiftSticky = false;
			mbShiftHeld = false;
			g_sim.GetPokey().SetShiftKeyState(false, !g_kbdOpts.mbFullRawKeys);
			UpdateLabels();
			break;

		case kActionHoldControl:
			mbControlSticky = false;
			mbControlHeld = false;
			g_sim.GetPokey().SetControlKeyState(false);
			UpdateLabels();
			break;
	}
}

void ATUIOnScreenKeyboard::OnButtonPressed(uint32 i) {
	const KeyEntry& ke = *mButtons[i].mpKeyEntry;

	ATPokeyEmulator& pokey = g_sim.GetPokey();
	ATGTIAEmulator& gtia = g_sim.GetGTIA();

	switch(ke.mScanCode) {
		default:
			pokey.PushRawKey(ke.mScanCode + (pokey.GetControlKeyState() ? 0x80 : 0x00) + (pokey.GetShiftKeyState() ? 0x40 : 0x00), !g_kbdOpts.mbFullRawKeys);
			break;

		case 0x40:
			pokey.PushBreak();
			break;

		case 0x41:
		case 0x42:
			break;

		case 0x48:
			gtia.SetConsoleSwitch(0x01, true);
			break;

		case 0x49:
			gtia.SetConsoleSwitch(0x02, true);
			break;

		case 0x4A:
			gtia.SetConsoleSwitch(0x04, true);
			break;

		case 0x4B:
			g_sim.WarmReset();
			break;
	}
}

void ATUIOnScreenKeyboard::OnButtonReleased(uint32 i) {
	const KeyEntry& ke = *mButtons[i].mpKeyEntry;

	ATPokeyEmulator& pokey = g_sim.GetPokey();
	ATGTIAEmulator& gtia = g_sim.GetGTIA();

	switch(ke.mScanCode) {
		case 0x40:
			break;
		case 0x43:
			break;
		case 0x48:
			gtia.SetConsoleSwitch(0x01, false);
			break;

		case 0x49:
			gtia.SetConsoleSwitch(0x02, false);
			break;

		case 0x4A:
			gtia.SetConsoleSwitch(0x04, false);
			break;

		default:
			pokey.ReleaseRawKey(ke.mScanCode, !g_kbdOpts.mbFullRawKeys);
			break;
	}

	// release any buttons that are toggles -- do this AFTER we've pushed keys
	bool needLabelChange = false;

	if (mbShiftSticky) {
		mbShiftSticky = false;
		
		if (!mbShiftHeld) {
			pokey.SetShiftKeyState(false, !g_kbdOpts.mbFullRawKeys);
			needLabelChange = true;

			for(int index : mShiftButtons) {
				ATUIButton *b = mButtons[index].mpButton;

				if (b) {
					b->SetDepressed(false);
				}
			}
		}
	}

	if (mbControlSticky) {
		mbControlSticky = false;

		if (!mbControlHeld) {
			pokey.SetControlKeyState(false);
			needLabelChange = true;

			for(int index : mControlButtons) {
				ATUIButton *b = mButtons[index].mpButton;

				if (b) {
					b->SetDepressed(false);
				}
			}
		}
	}

	if (needLabelChange)
		UpdateLabels();
}

void ATUIOnScreenKeyboard::OnButtonHeld(bool held, int index) {
	const KeyEntry& ke = *mButtons[index].mpKeyEntry;

	ATPokeyEmulator& pokey = g_sim.GetPokey();
	ATGTIAEmulator& gtia = g_sim.GetGTIA();

	switch(ke.mScanCode) {
		case 0x41:
			mbControlHeld = held;
			if (held) {
				mbControlSticky = !mbControlSticky;
				pokey.SetControlKeyState(true);
				UpdateLabels();
			} else {
				if (!mbControlSticky) {
					pokey.SetControlKeyState(false);
					UpdateLabels();
				}
			}
			break;

		case 0x42:
			mbShiftHeld = held;
			if (held) {
				mbShiftSticky = !mbShiftSticky;
				pokey.SetShiftKeyState(true, !g_kbdOpts.mbFullRawKeys);
				UpdateLabels();
			} else {
				if (!mbShiftSticky) {
					pokey.SetShiftKeyState(false, !g_kbdOpts.mbFullRawKeys);
					UpdateLabels();
				}
			}
			break;
	}
}

void ATUIOnScreenKeyboard::UpdateLabels() {
	for(int index : mShiftButtons) {
		ATUIButton *b = mButtons[index].mpButton;

		if (b) {
			b->SetDepressed(mbShiftHeld || mbShiftSticky);
		}
	}

	for(int index : mControlButtons) {
		ATUIButton *b = mButtons[index].mpButton;

		if (b) {
			b->SetDepressed(mbControlHeld || mbControlSticky);
		}
	}

	for(size_t i=0; i<vdcountof(mButtons); ++i) {
		const ButtonEntry& be = mButtons[i];

		if (be.mpButton) {
			const KeyEntry& ke = *be.mpKeyEntry;
			const wchar_t *s = ke.mpNormalText;

			if (mbControlHeld || mbControlSticky) {
				if (ke.mpControlText)
					s = ke.mpControlText;
			} else if (mbShiftHeld || mbShiftSticky) {
				if (ke.mpShiftText)
					s = ke.mpShiftText;
			}

			be.mpButton->SetText(s);
		}
	}
}
