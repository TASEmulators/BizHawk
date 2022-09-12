//
// Joystick handler
//
// by cal2
// GCC/SDL port by Niels Wagenaar (Linux/WIN32) and Caz (BeOS)
// Extensive rewrite by James Hammons
// (C) 2013 Underground Software
//
// JLH = James Hammons <jlhamm@acm.org>
//
// Who  When        What
// ---  ----------  -------------------------------------------------------------
// JLH  01/16/2010  Created this log ;-)
//

#include "joystick.h"
#include <string.h>			// For memset()
#include "gpu.h"
#include "jaguar.h"
#include "log.h"
#include "settings.h"

// Global vars

static uint8_t joystick_ram[4];
uint8_t joypad0Buttons[21];
uint8_t joypad1Buttons[21];
bool audioEnabled = false;
bool joysticksEnabled = false;


bool GUIKeyHeld = false;
extern int start_logging;
int gpu_start_log = 0;
int op_start_log = 0;
int blit_start_log = 0;
int effect_start = 0;
int effect_start2 = 0, effect_start3 = 0, effect_start4 = 0, effect_start5 = 0, effect_start6 = 0;
bool interactiveMode = false;
bool iLeft, iRight, iToggle = false;
bool keyHeld1 = false, keyHeld2 = false, keyHeld3 = false;
int objectPtr = 0;
bool startMemLog = false;
extern bool doDSPDis, doGPUDis;

bool blitterSingleStep = false;
bool bssGo = false;
bool bssHeld = false;

extern bool lagged;
extern void (*inputcb)();


void JoystickInit(void)
{
	JoystickReset();
}


void JoystickExec(void)
{
	gpu_start_log = 0;							// Only log while key down!
	effect_start = 0;
	effect_start2 = effect_start3 = effect_start4 = effect_start5 = effect_start6 = 0;
	blit_start_log = 0;
	iLeft = iRight = false;
}


void JoystickReset(void)
{
	memset(joystick_ram, 0x00, 4);
	memset(joypad0Buttons, 0, 21);
	memset(joypad1Buttons, 0, 21);
}


void JoystickDone(void)
{
}


uint16_t JoystickReadWord(uint32_t offset)
{
	lagged = false;
	if (__builtin_expect(!!inputcb, false))
		inputcb();

	// E, D, B, 7
	uint8_t joypad0Offset[16] = {
		0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x0C, 0xFF, 0xFF, 0xFF, 0x08, 0xFF, 0x04, 0x00, 0xFF
	};
	uint8_t joypad1Offset[16] = {
		0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0xFF, 0xFF, 0xFF, 0x04, 0xFF, 0x08, 0x0C, 0xFF
	};

#warning "No bounds checking done in JoystickReadByte!"
	offset &= 0x03;

	if (offset == 0)
	{
		if (!joysticksEnabled)
			return 0xFFFF;

		// Joystick data returns active low for buttons pressed, high for non-
		// pressed.
		uint16_t data = 0xFFFF;
		uint8_t offset0 = joypad0Offset[joystick_ram[1] & 0x0F];
		uint8_t offset1 = joypad1Offset[(joystick_ram[1] >> 4) & 0x0F];

		if (offset0 != 0xFF)
		{
			uint16_t mask[4] = { 0xFEFF, 0xFDFF, 0xFBFF, 0xF7FF };
			uint16_t msk2[4] = { 0xFFFF, 0xFFFD, 0xFFFB, 0xFFF7 };

			for(uint8_t i=0; i<4; i++)
				data &= (joypad0Buttons[offset0 + i] ? mask[i] : 0xFFFF);

			data &= msk2[offset0 / 4];
		}

		if (offset1 != 0xFF)
		{
			uint16_t mask[4] = { 0xEFFF, 0xDFFF, 0xBFFF, 0x7FFF };
			uint16_t msk2[4] = { 0xFF7F, 0xFFBF, 0xFFDF, 0xFFEF };

			for(uint8_t i=0; i<4; i++)
				data &= (joypad1Buttons[offset1 + i] ? mask[i] : 0xFFFF);

			data &= msk2[offset1 / 4];
		}

		return data;
	}
	else if (offset == 2)
	{
		// Hardware ID returns NTSC/PAL identification bit here
		// N.B.: On real H/W, bit 7 is *always* zero...!
		uint16_t data = 0xFF6F | (vjs.hardwareTypeNTSC ? 0x10 : 0x00);

		if (!joysticksEnabled)
			return data;

		// Joystick data returns active low for buttons pressed, high for non-
		// pressed.
		uint8_t offset0 = joypad0Offset[joystick_ram[1] & 0x0F];
		uint8_t offset1 = joypad1Offset[(joystick_ram[1] >> 4) & 0x0F];

		if (offset0 != 0xFF)
		{
			offset0 /= 4;	// Make index 0, 1, 2, 3 instead of 0, 4, 8, 12
			uint8_t mask[4][2] = { { BUTTON_A, BUTTON_PAUSE }, { BUTTON_B, 0xFF }, { BUTTON_C, 0xFF }, { BUTTON_OPTION, 0xFF } };
			data &= (joypad0Buttons[mask[offset0][0]] ? 0xFFFD : 0xFFFF);

			if (mask[offset0][1] != 0xFF)
				data &= (joypad0Buttons[mask[offset0][1]] ? 0xFFFE : 0xFFFF);
		}

		if (offset1 != 0xFF)
		{
			offset1 /= 4;	// Make index 0, 1, 2, 3 instead of 0, 4, 8, 12
			uint8_t mask[4][2] = { { BUTTON_A, BUTTON_PAUSE }, { BUTTON_B, 0xFF }, { BUTTON_C, 0xFF }, { BUTTON_OPTION, 0xFF } };
			data &= (joypad1Buttons[mask[offset1][0]] ? 0xFFF7 : 0xFFFF);

			if (mask[offset1][1] != 0xFF)
				data &= (joypad1Buttons[mask[offset1][1]] ? 0xFFFB : 0xFFFF);
		}

		return data;
	}

	return 0xFFFF;
}


void JoystickWriteWord(uint32_t offset, uint16_t data)
{
#warning "No bounds checking done for JoystickWriteWord!"
	offset &= 0x03;
	joystick_ram[offset + 0] = (data >> 8) & 0xFF;
	joystick_ram[offset + 1] = data & 0xFF;

	if (offset == 0)
	{
		audioEnabled = (data & 0x0100 ? true : false);
		joysticksEnabled = (data & 0x8000 ? true : false);
	}
}

