//
// TOM Processing
//
// Originally by David Raingeard (cal2)
// GCC/SDL port by Niels Wagenaar (Linux/WIN32) and Caz (BeOS)
// Cleanups, endian wrongness amelioration, and extensive fixes by James Hammons
// (C) 2010 Underground Software
//
// JLH = James Hammons <jlhamm@acm.org>
//
// Who  When        What
// ---  ----------  -----------------------------------------------------------
// JLH  01/16/2010  Created this log ;-)
// JLH  01/20/2011  Change rendering to RGBA, removed unnecessary code
//

#include "tom.h"

#include <string.h>
#include <stdlib.h>
#include "blitter.h"
#include "cry2rgb.h"
#include "event.h"
#include "gpu.h"
#include "jaguar.h"
#include "m68000/m68kinterface.h"
#include "op.h"
#include "settings.h"

// TOM registers (offset from $F00000)

#define MEMCON1		0x00
#define MEMCON2		0x02
#define HC			0x04
#define VC			0x06
#define OLP			0x20	// Object list pointer
#define OBF			0x26	// Object processor flag
#define VMODE		0x28
#define   MODE		0x0006	// Line buffer to video generator mode
#define   BGEN		0x0080	// Background enable (CRY & RGB16 only)
#define   VARMOD	0x0100	// Mixed CRY/RGB16 mode (only works in MODE 0!)
#define   PWIDTH	0x0E00	// Pixel width in video clock cycles (value written + 1)
#define BORD1		0x2A	// Border green/red values (8 BPP)
#define BORD2		0x2C	// Border blue value (8 BPP)
#define HP			0x2E	// Values range from 1 - 1024 (value written + 1)
#define HBB			0x30	// Horizontal blank begin
#define HBE			0x32
#define HS			0x34	// Horizontal sync
#define HVS			0x36	// Horizontal vertical sync
#define HDB1		0x38	// Horizontal display begin 1
#define HDB2		0x3A
#define HDE			0x3C
#define VP			0x3E	// Value ranges from 1 - 2048 (value written + 1)
#define VBB			0x40	// Vertical blank begin
#define VBE			0x42
#define VS			0x44	// Vertical sync
#define VDB			0x46	// Vertical display begin
#define VDE			0x48
#define VEB			0x4A	// Vertical equalization begin
#define VEE			0x4C	// Vertical equalization end
#define VI			0x4E	// Vertical interrupt
#define PIT0		0x50
#define PIT1		0x52
#define HEQ			0x54	// Horizontal equalization end
#define BG			0x58	// Background color
#define INT1		0xE0
#define INT2		0xE2

#define LEFT_VISIBLE_HC			(208 - 16 - (1 * 4))
#define RIGHT_VISIBLE_HC		(LEFT_VISIBLE_HC + (VIRTUAL_SCREEN_WIDTH * 4))
#define TOP_VISIBLE_VC			31
#define BOTTOM_VISIBLE_VC		511

#define LEFT_VISIBLE_HC_PAL		(208 - 16 - (-3 * 4))
#define RIGHT_VISIBLE_HC_PAL	(LEFT_VISIBLE_HC_PAL + (VIRTUAL_SCREEN_WIDTH * 4))
#define TOP_VISIBLE_VC_PAL		67
#define BOTTOM_VISIBLE_VC_PAL	579

uint8_t tomRam8[0x4000];
uint32_t tomWidth, tomHeight;
uint32_t tomTimerPrescaler;
uint32_t tomTimerDivider;
int32_t tomTimerCounter;
uint16_t tom_jerry_int_pending, tom_timer_int_pending, tom_object_int_pending,
	tom_gpu_int_pending, tom_video_int_pending;

uint32_t * screenBuffer;
uint32_t screenPitch;

typedef void (render_xxx_scanline_fn)(uint32_t *);

// Private function prototypes

void tom_render_16bpp_cry_scanline(uint32_t * backbuffer);
void tom_render_24bpp_scanline(uint32_t * backbuffer);
void tom_render_16bpp_direct_scanline(uint32_t * backbuffer);
void tom_render_16bpp_rgb_scanline(uint32_t * backbuffer);
void tom_render_16bpp_cry_rgb_mix_scanline(uint32_t * backbuffer);

render_xxx_scanline_fn * scanline_render[] =
{
	tom_render_16bpp_cry_scanline,
	tom_render_24bpp_scanline,
	tom_render_16bpp_direct_scanline,
	tom_render_16bpp_rgb_scanline,
	tom_render_16bpp_cry_rgb_mix_scanline,
	tom_render_24bpp_scanline,
	tom_render_16bpp_direct_scanline,
	tom_render_16bpp_rgb_scanline
};

uint32_t RGB16ToRGB32[0x10000];
uint32_t CRY16ToRGB32[0x10000];
uint32_t MIX16ToRGB32[0x10000];

void TOMFillLookupTables(void)
{
	for(uint32_t i=0; i<0x10000; i++)
		RGB16ToRGB32[i] =
			  ((i & 0xF800) << 8)
			| ((i & 0x003F) << 10)
			| ((i & 0x07C0) >> 3);

	for(uint32_t i=0; i<0x10000; i++)
	{
		uint32_t cyan = (i & 0xF000) >> 12,
			red = (i & 0x0F00) >> 8,
			intensity = (i & 0x00FF);

		uint32_t r = (((uint32_t)redcv[cyan][red]) * intensity) >> 8,
			g = (((uint32_t)greencv[cyan][red]) * intensity) >> 8,
			b = (((uint32_t)bluecv[cyan][red]) * intensity) >> 8;

		CRY16ToRGB32[i] = (r << 16) | (g << 8) | b;
		MIX16ToRGB32[i] = (i & 0x01 ? RGB16ToRGB32[i] : CRY16ToRGB32[i]);
	}
}

void TOMSetPendingJERRYInt(void)
{
	tom_jerry_int_pending = 1;
}

void TOMSetPendingTimerInt(void)
{
	tom_timer_int_pending = 1;
}

void TOMSetPendingObjectInt(void)
{
	tom_object_int_pending = 1;
}

void TOMSetPendingGPUInt(void)
{
	tom_gpu_int_pending = 1;
}

void TOMSetPendingVideoInt(void)
{
	tom_video_int_pending = 1;
}

uint8_t * TOMGetRamPointer(void)
{
	return tomRam8;
}

uint8_t TOMGetVideoMode(void)
{
	uint16_t vmode = GET16(tomRam8, VMODE);
	return ((vmode & VARMOD) >> 6) | ((vmode & MODE) >> 1);
}

uint16_t TOMGetVDB(void)
{
	return GET16(tomRam8, VDB);
}

uint16_t TOMGetHC(void)
{
	return GET16(tomRam8, HC);
}

uint16_t TOMGetVP(void)
{
	return GET16(tomRam8, VP);
}

uint16_t TOMGetMEMCON1(void)
{
	return GET16(tomRam8, MEMCON1);
}

//
// 16 BPP CRY/RGB mixed mode rendering
//
void tom_render_16bpp_cry_rgb_mix_scanline(uint32_t * backbuffer)
{
	uint16_t width = tomWidth;
	uint8_t * current_line_buffer = (uint8_t *)&tomRam8[0x1800];

	uint8_t pwidth = ((GET16(tomRam8, VMODE) & PWIDTH) >> 9) + 1;
	int16_t startPos = GET16(tomRam8, HDB1) - (vjs.hardwareTypeNTSC ? LEFT_VISIBLE_HC : LEFT_VISIBLE_HC_PAL);
	startPos /= pwidth;

	if (startPos < 0)
		current_line_buffer += 2 * -startPos;
	else
	{
		uint8_t g = tomRam8[BORD1], r = tomRam8[BORD1 + 1], b = tomRam8[BORD2 + 1];
		uint32_t pixel = (r << 16) | (g << 8) | b;

		for(int16_t i=0; i<startPos; i++)
			*backbuffer++ = pixel;

		width -= startPos;
	}

	while (width)
	{
		uint16_t color = (*current_line_buffer++) << 8;
		color |= *current_line_buffer++;
		*backbuffer++ = MIX16ToRGB32[color];
		width--;
	}
}

//
// 16 BPP CRY mode rendering
//
void tom_render_16bpp_cry_scanline(uint32_t * backbuffer)
{
	uint16_t width = tomWidth;
	uint8_t * current_line_buffer = (uint8_t *)&tomRam8[0x1800];

	uint8_t pwidth = ((GET16(tomRam8, VMODE) & PWIDTH) >> 9) + 1;
	int16_t startPos = GET16(tomRam8, HDB1) - (vjs.hardwareTypeNTSC ? LEFT_VISIBLE_HC : LEFT_VISIBLE_HC_PAL);
	startPos /= pwidth;

	if (startPos < 0)
		current_line_buffer += 2 * -startPos;
	else
	{
		uint8_t g = tomRam8[BORD1], r = tomRam8[BORD1 + 1], b = tomRam8[BORD2 + 1];
		uint32_t pixel = (r << 16) | (g << 8) | b;

		for(int16_t i=0; i<startPos; i++)
			*backbuffer++ = pixel;

		width -= startPos;
	}

	while (width)
	{
		uint16_t color = (*current_line_buffer++) << 8;
		color |= *current_line_buffer++;
		*backbuffer++ = CRY16ToRGB32[color];
		width--;
	}
}

//
// 24 BPP mode rendering
//
void tom_render_24bpp_scanline(uint32_t * backbuffer)
{
	uint16_t width = tomWidth;
	uint8_t * current_line_buffer = (uint8_t *)&tomRam8[0x1800];

	uint8_t pwidth = ((GET16(tomRam8, VMODE) & PWIDTH) >> 9) + 1;
	int16_t startPos = GET16(tomRam8, HDB1) - (vjs.hardwareTypeNTSC ? LEFT_VISIBLE_HC : LEFT_VISIBLE_HC_PAL);
	startPos /= pwidth;

	if (startPos < 0)
		current_line_buffer += 4 * -startPos;
	else
	{
		uint8_t g = tomRam8[BORD1], r = tomRam8[BORD1 + 1], b = tomRam8[BORD2 + 1];
		uint32_t pixel = (r << 16) | (g << 8) | b;

		for(int16_t i=0; i<startPos; i++)
			*backbuffer++ = pixel;

		width -= startPos;
	}

	while (width)
	{
		uint32_t g = *current_line_buffer++;
		uint32_t r = *current_line_buffer++;
		current_line_buffer++;
		uint32_t b = *current_line_buffer++;
		*backbuffer++ = (r << 16) | (g << 8) | b;
		width--;
	}
}

//
// 16 BPP direct mode rendering
//
void tom_render_16bpp_direct_scanline(uint32_t * backbuffer)
{
	uint16_t width = tomWidth;
	uint8_t * current_line_buffer = (uint8_t *)&tomRam8[0x1800];

	while (width)
	{
		uint16_t color = (*current_line_buffer++) << 8;
		color |= *current_line_buffer++;
		*backbuffer++ = color >> 1;
		width--;
	}
}

//
// 16 BPP RGB mode rendering
//
void tom_render_16bpp_rgb_scanline(uint32_t * backbuffer)
{
	uint16_t width = tomWidth;
	uint8_t * current_line_buffer = (uint8_t *)&tomRam8[0x1800];

	uint8_t pwidth = ((GET16(tomRam8, VMODE) & PWIDTH) >> 9) + 1;
	int16_t startPos = GET16(tomRam8, HDB1) - (vjs.hardwareTypeNTSC ? LEFT_VISIBLE_HC : LEFT_VISIBLE_HC_PAL);
	startPos /= pwidth;

	if (startPos < 0)
		current_line_buffer += 2 * -startPos;
	else
	{
		uint8_t g = tomRam8[BORD1], r = tomRam8[BORD1 + 1], b = tomRam8[BORD2 + 1];
		uint32_t pixel = (r << 16) | (g << 8) | b;

		for(int16_t i=0; i<startPos; i++)
			*backbuffer++ = pixel;

		width -= startPos;
	}

	while (width)
	{
		uint32_t color = (*current_line_buffer++) << 8;
		color |= *current_line_buffer++;
		*backbuffer++ = RGB16ToRGB32[color];
		width--;
	}
}

//
// Process a single halfline
//
void TOMExecHalfline(uint16_t halfline, bool render)
{
	uint16_t field2 = halfline & 0x0800;
	halfline &= 0x07FF;
	bool inActiveDisplayArea = true;

	if (halfline & 0x01)
		return;

	uint16_t startingHalfline = GET16(tomRam8, VDB);
	uint16_t endingHalfline = GET16(tomRam8, VDE);

	if (endingHalfline > GET16(tomRam8, VP))
		startingHalfline = 0;

	if ((halfline >= startingHalfline) && (halfline < endingHalfline))
	{
		if (render)
		{
			uint8_t * current_line_buffer = (uint8_t *)&tomRam8[0x1800];
			uint8_t bgHI = tomRam8[BG], bgLO = tomRam8[BG + 1];

			if (GET16(tomRam8, VMODE) & BGEN)
				for(uint32_t i=0; i<720; i++)
					*current_line_buffer++ = bgHI, *current_line_buffer++ = bgLO;

			OPProcessList(halfline, render);
		}
	}
	else
		inActiveDisplayArea = false;

	uint16_t topVisible = (vjs.hardwareTypeNTSC ? TOP_VISIBLE_VC : TOP_VISIBLE_VC_PAL),
		bottomVisible = (vjs.hardwareTypeNTSC ? BOTTOM_VISIBLE_VC : BOTTOM_VISIBLE_VC_PAL);
	uint32_t * TOMCurrentLine = 0;

	if (tomRam8[VP + 1] & 0x01)
		TOMCurrentLine = &(screenBuffer[((halfline - topVisible) / 2) * screenPitch]);
	else
		TOMCurrentLine = &(screenBuffer[(((halfline - topVisible) / 2) * screenPitch * 2) + (field2 ? 0 : screenPitch)]);

	if ((halfline >= topVisible) && (halfline < bottomVisible))
	{
		if (inActiveDisplayArea)
		{
			scanline_render[TOMGetVideoMode()](TOMCurrentLine);
		}
		else
		{
			uint32_t * currentLineBuffer = TOMCurrentLine;
			uint8_t g = tomRam8[BORD1], r = tomRam8[BORD1 + 1], b = tomRam8[BORD2 + 1];
			uint32_t pixel = (r << 16) | (g << 8) | b;

			for(uint32_t i=0; i<tomWidth; i++)
				*currentLineBuffer++ = pixel;
		}
	}
}

//
// TOM initialization
//
void TOMInit(void)
{
	TOMFillLookupTables();
	OPInit();
	BlitterInit();
	TOMReset();
}

void TOMDone(void)
{
	OPDone();
	BlitterDone();
}


uint32_t TOMGetVideoModeWidth(void)
{
	uint16_t pwidth = ((GET16(tomRam8, VMODE) & PWIDTH) >> 9) + 1;
	return (vjs.hardwareTypeNTSC ? RIGHT_VISIBLE_HC - LEFT_VISIBLE_HC : RIGHT_VISIBLE_HC_PAL - LEFT_VISIBLE_HC_PAL) / pwidth;
}


uint32_t TOMGetVideoModeHeight(void)
{
	return (vjs.hardwareTypeNTSC ? 240 : 256);
}

//
// TOM reset code
// Now PAL friendly!
//
void TOMReset(void)
{
	OPReset();
	BlitterReset();
	memset(tomRam8, 0x00, 0x4000);

	if (vjs.hardwareTypeNTSC)
	{
		SET16(tomRam8, MEMCON1, 0x1861);
		SET16(tomRam8, MEMCON2, 0x35CC);
		SET16(tomRam8, HP, 844);
		SET16(tomRam8, HBB, 1713);
		SET16(tomRam8, HBE, 125);
		SET16(tomRam8, HDE, 1665);
		SET16(tomRam8, HDB1, 203);
		SET16(tomRam8, VP, 523);
		SET16(tomRam8, VBE, 24);
		SET16(tomRam8, VDB, 38);
		SET16(tomRam8, VDE, 518);
		SET16(tomRam8, VBB, 500);
		SET16(tomRam8, VS, 517);
		SET16(tomRam8, VMODE, 0x06C1);
	}
	else
	{
		SET16(tomRam8, MEMCON1, 0x1861);
		SET16(tomRam8, MEMCON2, 0x35CC);
		SET16(tomRam8, HP, 850);
		SET16(tomRam8, HBB, 1711);
		SET16(tomRam8, HBE, 158);
		SET16(tomRam8, HDE, 1665);
		SET16(tomRam8, HDB1, 203);
		SET16(tomRam8, VP, 623);
		SET16(tomRam8, VBE, 34);
		SET16(tomRam8, VDB, 38);
		SET16(tomRam8, VDE, 518);
		SET16(tomRam8, VBB, 600);
		SET16(tomRam8, VS, 618);
		SET16(tomRam8, VMODE, 0x06C1);
	}

	tomWidth = 0;
	tomHeight = 0;

	tom_jerry_int_pending = 0;
	tom_timer_int_pending = 0;
	tom_object_int_pending = 0;
	tom_gpu_int_pending = 0;
	tom_video_int_pending = 0;

	tomTimerPrescaler = 0;
	tomTimerDivider = 0;
	tomTimerCounter = 0;
}

//
// TOM byte access (read)
//
uint8_t TOMReadByte(uint32_t offset, uint32_t who)
{
	if ((offset >= GPU_CONTROL_RAM_BASE) && (offset < GPU_CONTROL_RAM_BASE+0x20))
		return GPUReadByte(offset, who);
	else if ((offset >= GPU_WORK_RAM_BASE) && (offset < GPU_WORK_RAM_BASE+0x1000))
		return GPUReadByte(offset, who);
	else if ((offset >= 0xF02200) && (offset < 0xF022A0))
		return BlitterReadByte(offset, who);
	else if (offset == 0xF00050)
		return tomTimerPrescaler >> 8;
	else if (offset == 0xF00051)
		return tomTimerPrescaler & 0xFF;
	else if (offset == 0xF00052)
		return tomTimerDivider >> 8;
	else if (offset == 0xF00053)
		return tomTimerDivider & 0xFF;

	return tomRam8[offset & 0x3FFF];
}

//
// TOM word access (read)
//
uint16_t TOMReadWord(uint32_t offset, uint32_t who)
{
	if (offset == 0xF000E0)
	{
		uint16_t data = (tom_jerry_int_pending << 4) | (tom_timer_int_pending << 3)
			| (tom_object_int_pending << 2) | (tom_gpu_int_pending << 1)
			| (tom_video_int_pending << 0);
		return data;
	}
	else if (offset == 0xF00004)
		return rand() & 0x03FF;
	else if ((offset >= GPU_CONTROL_RAM_BASE) && (offset < GPU_CONTROL_RAM_BASE + 0x20))
		return GPUReadWord(offset, who);
	else if ((offset >= GPU_WORK_RAM_BASE) && (offset < GPU_WORK_RAM_BASE + 0x1000))
		return GPUReadWord(offset, who);
	else if ((offset >= 0xF02200) && (offset < 0xF022A0))
		return BlitterReadWord(offset, who);
	else if (offset == 0xF00050)
		return tomTimerPrescaler;
	else if (offset == 0xF00052)
		return tomTimerDivider;

	offset &= 0x3FFF;
	return (TOMReadByte(offset, who) << 8) | TOMReadByte(offset + 1, who);
}

//
// TOM byte access (write)
//
void TOMWriteByte(uint32_t offset, uint8_t data, uint32_t who)
{
	if ((offset >= 0xF08000) && (offset <= 0xF0BFFF))
		offset &= 0xFF7FFF;

	if ((offset < 0xF00000) || (offset > 0xF03FFF))
		return;

	if ((offset >= GPU_CONTROL_RAM_BASE) && (offset < GPU_CONTROL_RAM_BASE+0x20))
	{
		GPUWriteByte(offset, data, who);
	}
	else if ((offset >= GPU_WORK_RAM_BASE) && (offset < GPU_WORK_RAM_BASE+0x1000))
	{
		GPUWriteByte(offset, data, who);
	}
	else if ((offset >= 0xF02200) && (offset < 0xF022A0))
	{
		BlitterWriteByte(offset, data, who);
	}
	else if (offset == 0xF00050)
	{
		tomTimerPrescaler = (tomTimerPrescaler & 0x00FF) | (data << 8);
		TOMResetPIT();
	}
	else if (offset == 0xF00051)
	{
		tomTimerPrescaler = (tomTimerPrescaler & 0xFF00) | data;
		TOMResetPIT();
		return;
	}
	else if (offset == 0xF00052)
	{
		tomTimerDivider = (tomTimerDivider & 0x00FF) | (data << 8);
		TOMResetPIT();
	}
	else if (offset == 0xF00053)
	{
		tomTimerDivider = (tomTimerDivider & 0xFF00) | data;
		TOMResetPIT();
	}
	else if (offset >= 0xF00400 && offset <= 0xF007FF)
	{
		offset &= 0x5FF;
		tomRam8[offset] = data, tomRam8[offset + 0x200] = data;
	}
	else
	{
		tomRam8[offset & 0x3FFF] = data;
	}
}

//
// TOM word access (write)
//
void TOMWriteWord(uint32_t offset, uint16_t data, uint32_t who)
{
	if ((offset >= 0xF08000) && (offset <= 0xF0BFFF))
		offset &= 0xFF7FFF;

	if ((offset < 0xF00000) || (offset > 0xF03FFF))
		return;

	if ((offset >= GPU_CONTROL_RAM_BASE) && (offset < GPU_CONTROL_RAM_BASE+0x20))
	{
		GPUWriteWord(offset, data, who);
		return;
	}
	else if ((offset >= GPU_WORK_RAM_BASE) && (offset < GPU_WORK_RAM_BASE+0x1000))
	{
		GPUWriteWord(offset, data, who);
		return;
	}
	else if (offset == 0xF00050)
	{
		tomTimerPrescaler = data;
		TOMResetPIT();
		return;
	}
	else if (offset == 0xF00052)
	{
		tomTimerDivider = data;
		TOMResetPIT();
		return;
	}
	else if (offset == 0xF000E0)
	{
		if (data & 0x0100)
			tom_video_int_pending = 0;
		if (data & 0x0200)
			tom_gpu_int_pending = 0;
		if (data & 0x0400)
			tom_object_int_pending = 0;
		if (data & 0x0800)
			tom_timer_int_pending = 0;
		if (data & 0x1000)
			tom_jerry_int_pending = 0;
	}
	else if ((offset >= 0xF02200) && (offset <= 0xF0229F))
	{
		BlitterWriteWord(offset, data, who);
		return;
	}
	else if (offset >= 0xF00400 && offset <= 0xF007FE)
	{
		offset &= 0x5FF;
		SET16(tomRam8, offset, data);
		SET16(tomRam8, offset + 0x200, data);
	}

	offset &= 0x3FFF;

	if (offset >= 0x30 && offset <= 0x4E)
		data &= 0x07FF;
	if (offset == 0x2E || offset == 0x36 || offset == 0x54)
		data &= 0x03FF;

	if ((offset >= 0x28) && (offset <= 0x4F))
	{
		uint32_t width = TOMGetVideoModeWidth(), height = TOMGetVideoModeHeight();

		if ((width != tomWidth) || (height != tomHeight))
		{
			tomWidth = width, tomHeight = height;
		}
	}

	tomRam8[(offset + 0) & 0x3FFF] = data >> 8;
	tomRam8[(offset + 1) & 0x3FFF] = data & 0xFF;
}

int TOMIRQEnabled(int irq)
{
	return tomRam8[INT1 + 1] & (1 << irq);
}

void TOMPITCallback(void);

void TOMResetPIT(void)
{
	RemoveCallback(TOMPITCallback);

	if (tomTimerPrescaler)
	{
		double usecs = (float)(tomTimerPrescaler + 1) * (float)(tomTimerDivider + 1) * RISC_CYCLE_IN_USEC;
		SetCallbackTime(TOMPITCallback, usecs);
	}
}

void TOMExecPIT(uint32_t cycles)
{
	if (tomTimerPrescaler)
	{
		tomTimerCounter -= cycles;

		if (tomTimerCounter <= 0)
		{
			TOMSetPendingTimerInt();
			GPUSetIRQLine(GPUIRQ_TIMER, ASSERT_LINE);

			if (TOMIRQEnabled(IRQ_TIMER))
				m68k_set_irq(2);

			TOMResetPIT();
		}
	}
}

void TOMPITCallback(void)
{
	TOMSetPendingTimerInt();
    GPUSetIRQLine(GPUIRQ_TIMER, ASSERT_LINE);

	if (TOMIRQEnabled(IRQ_TIMER))
		m68k_set_irq(2);

	TOMResetPIT();
}
