#ifndef f_AT_UIONSCREENKEYBOARD_H
#define f_AT_UIONSCREENKEYBOARD_H

#include <at/atui/uicontainer.h>
#include "callback.h"

class ATUIButton;

class ATUIOnScreenKeyboard final : public ATUIContainer {
public:
	enum {
		kActionLeft = kActionCustom,
		kActionRight,
		kActionUp,
		kActionDown,
		kActionHoldShift,
		kActionHoldControl
	};

	ATUIOnScreenKeyboard();
	~ATUIOnScreenKeyboard();

	void AutoSize();

public:
	virtual void OnCreate();
	virtual void OnDestroy();
	virtual void OnSize();

	virtual void OnActionStart(uint32 id) override;
	virtual void OnActionRepeat(uint32 id) override;
	virtual void OnActionStop(uint32 id) override;

protected:
	void OnButtonPressed(uint32 index);
	void OnButtonReleased(uint32 index);
	void OnButtonHeld(bool state, int index);
	void UpdateLabels();

	struct KeyEntry;

	struct ButtonEntry {
		int mX;
		int mY;
		int mNavOrder[4];
		ATUIButton *mpButton;
		const KeyEntry *mpKeyEntry;

		ButtonEntry()
			: mX(0)
			, mY(0)
			, mpButton(NULL)
			, mpKeyEntry(NULL)
		{
			mNavOrder[0] = -1;
			mNavOrder[1] = -1;
			mNavOrder[2] = -1;
			mNavOrder[3] = -1;
		}
	};

	enum {
		kCols = 15,
		kRows = 6,
		kSubRows = 6*4 + 1
	};

	sint32	mButtonWidth;
	sint32	mButtonHeight;
	bool	mbShiftHeld;
	bool	mbShiftSticky;
	bool	mbControlHeld;
	bool	mbControlSticky;

	vdfastvector<int> mControlButtons;
	vdfastvector<int> mShiftButtons;

	ButtonEntry mButtons[62];

	static const KeyEntry kEntries[];
	static const int kRowBreaks[];
};

#endif
