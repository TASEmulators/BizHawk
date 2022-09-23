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
#include <string.h>
#include "gpu.h"
#include "jaguar.h"
#include "settings.h"

// Global vars

static uint8_t joystick_ram[4];
uint8_t joypad0Buttons[21];
uint8_t joypad1Buttons[21];
static bool joysticksEnabled;

extern bool lagged;

void JoystickInit(void)
{
	JoystickReset();
}

void JoystickExec(void)
{
}

void JoystickReset(void)
{
	memset(joystick_ram, 0x00, 4);
	memset(joypad0Buttons, 0, 21);
	memset(joypad1Buttons, 0, 21);
	joysticksEnabled = false;
}

void JoystickDone(void)
{
}

uint16_t JoystickReadWord(uint32_t offset)
{
	lagged = false;
	MAYBE_CALLBACK(InputCallback);

	uint8_t joypad0Offset[16] = {
		0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x0C, 0xFF, 0xFF, 0xFF, 0x08, 0xFF, 0x04, 0x00, 0xFF
	};
	uint8_t joypad1Offset[16] = {
		0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0xFF, 0xFF, 0xFF, 0x04, 0xFF, 0x08, 0x0C, 0xFF
	};

	offset &= 0x03;

	if (offset == 0)
	{
		if (!joysticksEnabled)
			return 0xFFFF;

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
		uint16_t data = 0xFF6F | (vjs.hardwareTypeNTSC ? 0x10 : 0x00);

		if (!joysticksEnabled)
			return data;

		uint8_t offset0 = joypad0Offset[joystick_ram[1] & 0x0F];
		uint8_t offset1 = joypad1Offset[(joystick_ram[1] >> 4) & 0x0F];

		if (offset0 != 0xFF)
		{
			offset0 /= 4;
			uint8_t mask[4][2] = { { BUTTON_A, BUTTON_PAUSE }, { BUTTON_B, 0xFF }, { BUTTON_C, 0xFF }, { BUTTON_OPTION, 0xFF } };
			data &= (joypad0Buttons[mask[offset0][0]] ? 0xFFFD : 0xFFFF);

			if (mask[offset0][1] != 0xFF)
				data &= (joypad0Buttons[mask[offset0][1]] ? 0xFFFE : 0xFFFF);
		}

		if (offset1 != 0xFF)
		{
			offset1 /= 4;
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
	offset &= 0x03;
	joystick_ram[offset + 0] = (data >> 8) & 0xFF;
	joystick_ram[offset + 1] = data & 0xFF;

	if (offset == 0)
	{
		joysticksEnabled = (data & 0x8000 ? true : false);
	}
}
