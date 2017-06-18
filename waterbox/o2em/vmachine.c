/*
 *   O2EM Free Odyssey2 / Videopac+ Emulator
 *
 *   Created by Daniel Boris <dboris@comcast.net>  (c) 1997,1998
 *
 *   Developed by Andre de la Rocha   <adlroc@users.sourceforge.net>
 *             Arlindo M. de Oliveira <dgtec@users.sourceforge.net>
 *
 *   http://o2em.sourceforge.net
 *
 *
 *
 *   Main O2 machine emulation
 */

#include <stdlib.h>
#include <stdio.h>
#include <time.h>
#include "audio.h"
#include "types.h"
#include "cpu.h"
#include "keyboard.h"
#include "config.h"
#include "vdc.h"
#include "vpp.h"
#include "voice.h"
#include "vmachine.h"

static Byte x_latch, y_latch;
static int romlatch = 0;
static Byte line_count;
static int fps = FPS_NTSC;

static Byte snapedlines[MAXLINES + 2 * MAXSNAP][256][2];

int evblclk = EVBLCLK_NTSC;

struct resource app_data;
int frame = 0;

int int_clk;	/* counter for length of /INT pulses */
int master_clk; /* Master clock */
int h_clk;		/* horizontal clock */
unsigned long clk_counter;
int last_line;
int key2vcnt = 0;
int mstate;

int pendirq = 0;
int enahirq = 1;
int useforen = 0;
long regionoff = 0xffff;
int mxsnap = 2;
int sproff = 0;
int tweakedaudio = 0;

Byte rom_table[8][4096];

Byte intRAM[64];
Byte extRAM[256];
Byte extROM[1024];
Byte VDCwrite[256];
Byte ColorVector[MAXLINES];
Byte AudioVector[MAXLINES];
Byte *rom;
Byte *megarom;

int key2[128];

static void do_kluges(void);
static void setvideomode(int t);

void handle_vbl(void)
{
	update_audio();
	update_voice();
	draw_region();
	ext_IRQ();
	mstate = 1;
}

void handle_evbl(void)
{
	static long last = 0;
	int i;

	last_line = 0;
	master_clk -= evblclk;
	frame++;
	finish_display();

	if (app_data.crc == 0xA7344D1F)
	{
		for (i = 0; i < 140; i++)
		{
			ColorVector[i] = (VDCwrite[0xA3] & 0x7f) | (p1 & 0x80);
			AudioVector[i] = VDCwrite[0xAA];
		}
	} /*Atlantis*/
	else
	{
		for (i = 0; i < MAXLINES; i++)
		{
			ColorVector[i] = (VDCwrite[0xA3] & 0x7f) | (p1 & 0x80);
			AudioVector[i] = VDCwrite[0xAA];
		}
	}
	if (key2vcnt++ > 10)
	{
		key2vcnt = 0;
		for (i = 0; i < 128; i++)
			key2[i] = 0;
	}
	mstate = 0;
}

void handle_evbll(void)
{
	static long last = 0;
	int i;

	/******************* 150 */

	for (i = 150; i < MAXLINES; i++)
	{
		ColorVector[i] = (VDCwrite[0xA3] & 0x7f) | (p1 & 0x80);
		AudioVector[i] = VDCwrite[0xAA];
	}

	if (key2vcnt++ > 10)
	{
		key2vcnt = 0;
		for (i = 0; i < 128; i++)
			key2[i] = 0;
	}
	mstate = 0;
}

void init_system(void)
{
	int i, j, k;

	last_line = 0;
	mstate = 0;
	master_clk = 0;
	h_clk = 0;
	line_count = 0;
	itimer = 0;
	clk_counter = 0;
	init_roms();
	for (i = 0; i < 256; i++)
	{
		VDCwrite[i] = 0;
		extRAM[i] = 0;
	}
	for (i = 0; i < 64; i++)
	{
		intRAM[i] = 0;
	}
	for (i = 0; i < MAXLINES; i++)
		AudioVector[i] = ColorVector[i] = 0;

	for (i = 0; i < MAXLINES + 2 * MAXSNAP; i++)
		for (j = 0; j < 256; j++)
			for (k = 0; k < 2; k++)
				snapedlines[i][j][k] = 0;

	for (i = 0; i < 128; i++)
		key2[i] = 0;
	key2vcnt = 0;
	if (app_data.euro)
		setvideomode(1);
	else
		setvideomode(0);
	do_kluges();
	init_vpp();
	clear_collision();
}

void init_roms(void)
{
	rom = rom_table[0];
	romlatch = 0;
}

Byte read_t1(void)
{
	/*17*/
	if ((h_clk > 16) || (master_clk > VBLCLK))
		return 1;
	else
		return 0;
}

void write_p1(Byte d)
{
	if ((d & 0x80) != (p1 & 0x80))
	{
		int i, l;
		l = snapline((int)((float)master_clk / 22.0 + 0.1), VDCwrite[0xA3],
					 1);
		for (i = l; i < MAXLINES; i++)
			ColorVector[i] = (VDCwrite[0xA3] & 0x7f) | (d & 0x80);
	}
	p1 = d;
	if (app_data.bank == 2)
	{
		rom = rom_table[~p1 & 0x01];
	}
	else if (app_data.bank == 3)
	{
		rom = rom_table[~p1 & 0x03];
	}
	else if (app_data.bank == 4)
	{
		rom = rom_table[(p1 & 1) ? 0 : romlatch];
	}
}

Byte read_P2(void)
{
	int i, si, so, km;

	return 0xff;
	// TODO
	/*if (NeedsPoll)
		poll_keyboard();

	if (!(p1 & 0x04))
	{
		si = (p2 & 7);
		so = 0xff;
		if (si < 6)
		{
			for (i = 0; i < 8; i++)
			{
				km = key_map[si][i];
				if ((key[km] && ((!joykeystab[km]) || (key_shifts & KB_CAPSLOCK_FLAG))) || (key2[km]))
				{
					so = i ^ 0x07;
				}
			}
		}
		if (so != 0xff)
		{
			p2 = p2 & 0x0F;
			p2 = p2 | (so << 5);
		}
		else
		{
			p2 = p2 | 0xF0;
		}
	}
	else
	{
		p2 = p2 | 0xF0;
	}
	return (p2);*/
}

Byte ext_read(ADDRESS adr)
{
	Byte d;
	Byte si;
	Byte m;
	int i;

	if (!(p1 & 0x08) && !(p1 & 0x40))
	{
		/* Handle VDC Read */
		switch (adr)
		{
		case 0xA1:
			d = VDCwrite[0xA0] & 0x02;
			if (master_clk > VBLCLK)
				d = d | 0x08;
			if (h_clk < (LINECNT - 7))
				d = d | 0x01;
			if (sound_IRQ)
				d = d | 0x04;
			sound_IRQ = 0;
			return d;
		case 0xA2:
			si = VDCwrite[0xA2];
			m = 0x01;
			d = 0;
			for (i = 0; i < 8; i++)
			{
				if (si & m)
				{
					if (coltab[1] & m)
						d = d | (coltab[1] & (m ^ 0xFF));
					if (coltab[2] & m)
						d = d | (coltab[2] & (m ^ 0xFF));
					if (coltab[4] & m)
						d = d | (coltab[4] & (m ^ 0xFF));
					if (coltab[8] & m)
						d = d | (coltab[8] & (m ^ 0xFF));
					if (coltab[0x10] & m)
						d = d | (coltab[0x10] & (m ^ 0xFF));
					if (coltab[0x20] & m)
						d = d | (coltab[0x20] & (m ^ 0xFF));
					if (coltab[0x80] & m)
						d = d | (coltab[0x80] & (m ^ 0xFF));
				}
				m = m << 1;
			}
			clear_collision();
			return d;
		case 0xA5:
			if (!(VDCwrite[0xA0] & 0x02))
			{
				return x_latch;
			}
			else
			{
				x_latch = h_clk * 12;
				return x_latch;
			}
		case 0xA4:
			if (!(VDCwrite[0xA0] & 0x02))
			{
				return y_latch;
			}
			else
			{
				y_latch = master_clk / 22;
				if (y_latch > 241)
					y_latch = 0xFF;
				return y_latch;
			}
		default:
			return VDCwrite[adr];
		}
	}
	else if (!(p1 & 0x10))
	{
		/* Handle ext RAM Read */
		if (app_data.megaxrom && (adr >= 0x80))
		{
			/* MegaCART registers are mirrored every 4 bytes */
			if ((adr & 0x83) == 0x83)
			{
				/* TODO: emulate EEPROM data in */
				return 0xff;
			}
			else
				return extRAM[adr & 0x83];
		}
		else
			return extRAM[adr & 0xFF];
	}
	else if (!(p1 & 0x20))
	{
		/* Read a Videopac+ register */
		return vpp_read(adr);
	}
	else if (app_data.exrom && (p1 & 0x02))
	{
		/* Handle read from exrom */
		return extROM[(p2 << 8) | (adr & 0xFF)];
	}
	else if (app_data.megaxrom && !(p1 & 0x02) && !(p1 & 0x40))
	{
		/* Handle data read from MegaCART */
		return megarom[(extRAM[0x81] << 12) | ((p2 & 0x0f) << 8) | (adr & 0xff)];
	}

	return 0;
}

Byte in_bus(void)
{
	Byte si = 0, d = 0, mode = 0, jn = 0, sticknum = 0;

	if ((p1 & 0x08) && (p1 & 0x10))
	{
		/* Handle joystick read */
		if (!(p1 & 0x04))
		{
			si = (p2 & 7);
		}
		d = 0xFF;
		// si == 1 => joystick 0
		// si != 1 => joystick 1
		// &= 0xfe up
		// &= 0xfd right
		// &= 0xfb down
		// &= 0xf7 left
		// &= 0xef both (?) buttons
	}
	return d;
}

void ext_write(Byte dat, ADDRESS adr)
{
	int i;

	if (!(p1 & 0x08))
	{
		/* Handle VDC Write */
		if (adr == 0xA0)
		{
			if ((VDCwrite[0xA0] & 0x02) && !(dat & 0x02))
			{
				y_latch = master_clk / 22;
				x_latch = h_clk * 12;
				if (y_latch > 241)
					y_latch = 0xFF;
			}
			if ((master_clk <= VBLCLK) && (VDCwrite[0xA0] != dat))
			{
				draw_region();
			}
		}
		else if (adr == 0xA3)
		{
			int l;
			l = snapline((int)((float)master_clk / 22.0 + 0.5), dat, 1);
			for (i = l; i < MAXLINES; i++)
				ColorVector[i] = (dat & 0x7f) | (p1 & 0x80);
		}
		else if (adr == 0xAA)
		{
			for (i = master_clk / 22; i < MAXLINES; i++)
				AudioVector[i] = dat;
		}
		else if ((adr >= 0x40) && (adr <= 0x7f) && ((adr & 2) == 0))
		{
			/* simulate quad: all 4 sub quad position registers
             * are mapped to the same internal register */
			adr = adr & 0x71;
			/* Another minor thing: the y register always returns
             * bit 0 as 0 */
			if ((adr & 1) == 0)
				dat = dat & 0xfe;
			VDCwrite[adr] = VDCwrite[adr + 4] = VDCwrite[adr + 8] = VDCwrite[adr + 12] = dat;
		}
		VDCwrite[adr] = dat;
	}
	else if (!(p1 & 0x10) && !(p1 & 0x40))
	{
		adr = adr & 0xFF;

		if (adr < 0x80)
		{
			/* Handle ext RAM Write */
			extRAM[adr] = dat;
		}
		else
		{
			if (app_data.bank == 4)
			{
				romlatch = (~dat) & 7;
				rom = rom_table[(p1 & 1) ? 0 : romlatch];
			}

			/* Handle The Voice */
			if (!(dat & 0x20))
				reset_voice();
			else
			{
				if (adr == 0xE4)
					set_voice_bank(0);
				else if ((adr >= 0xE8) && (adr <= 0xEF))
					set_voice_bank(adr - 0xE7);
				else if (((adr >= 0x80) && (adr <= 0xDF)) || ((adr >= 0xF0) && (adr <= 0xFF)))
					trigger_voice(adr);
			}
		}
	}
	else if (!(p1 & 0x20))
	{
		/* Write to a Videopac+ register */
		vpp_write(dat, adr);
	}
}

static void do_kluges(void)
{
	if (app_data.crc == 0xA7344D1F)
		pendirq = 1; /* Atlantis */
	if (app_data.crc == 0xFB83171E)
		pendirq = 1; /* Blockout */
	if (app_data.crc == 0xD38153F6)
		pendirq = 1; /* Blockout (french) */
	if (app_data.crc == 0x881CEAE4)
		pendirq = 1; /* Wall Street */

	if (app_data.crc == 0x9E42E766)
		useforen = 1; /* Turtles */
	if (app_data.crc == 0x1C750349)
		useforen = 1; /* Turtles (European version) */
	if (app_data.crc == 0x202F2749)
		useforen = 1; /* Q*bert */
	if (app_data.crc == 0x06861A9C)
		useforen = 1; /* Flashpoint 5 (Videopac adaption) */

	if (app_data.crc == 0xFB83171E)
		enahirq = 0; /* Blockout*/
	if (app_data.crc == 0xD38153F6)
		enahirq = 0; /* Blockout (french) */

	if (app_data.crc == 0xFB83171E)
		regionoff = 1; /* Blockout*/
	if (app_data.crc == 0xD38153F6)
		regionoff = 1; /* Blockout (french) */
	if (app_data.crc == 0x202F2749)
		regionoff = 0; /* Q*bert */
	if (app_data.crc == 0x5216771A)
		regionoff = 1; /* Popeye */
	if (app_data.crc == 0x0C2E4811)
		regionoff = 11; /* Out of this World! / Helicopter Rescue! */
	if (app_data.crc == 0x67069924)
		regionoff = 11; /* Smithereens! */
	if (app_data.crc == 0x44D1A8A5)
		regionoff = 11; /* Smithereens! (European version) */
	if (app_data.crc == 0x2391C2FB)
		regionoff = 11; /* Smithereens! + */
	if (app_data.crc == 0xBB4AD548)
		regionoff = 11; /* Smithereens! modified 1 */
	if (app_data.crc == 0x25057C11)
		regionoff = 11; /* Smithereens! modified 2 */
	if (app_data.crc == 0xB936BD78)
		regionoff = 12; /* Type & Tell */
	if (app_data.crc == 0xAD8B9AE0)
		regionoff = 2; /* Type & Tell modified 1 */
	if (app_data.crc == 0x5C02BEE6)
		regionoff = 2; /* Type & Tell modified 2 */
	if (app_data.crc == 0xDC30AD3D)
		regionoff = 10; /* Dynasty! */
	if (app_data.crc == 0x7810BAD5)
		regionoff = 8; /* Dynasty! (European) */
	if (app_data.crc == 0xA7344D1F)
		regionoff = 0; /* Atlantis */
	if (app_data.crc == 0xD0BC4EE6)
		regionoff = 12; /* Frogger */
	if (app_data.crc == 0xA57D84F3)
		regionoff = 8; /* Frogger BR */
	if (app_data.crc == 0x825976A9)
		regionoff = 0; /* Mousing Cat 8kb */
	if (app_data.crc == 0xF390BFEC)
		regionoff = 0; /* Mousing Cat 4kb */
	if (app_data.crc == 0x61A350E6)
		regionoff = 0; /* Mousing Cat (french) */
	if (app_data.crc == 0x3BFEF56B)
		regionoff = 1; /* Four in 1 Row! */
	if (app_data.crc == 0x7C747245)
		regionoff = 1; /* Four in 1 Row! modified */
	if (app_data.crc == 0x9B5E9356)
		regionoff = 1; /* Four in 1 Row! (french) */

	if (app_data.crc == 0x6CEBAB74)
		regionoff = 12; /* P.T. Barnum's Acrobats! (European version) */
	if (app_data.crc == 0xE7B26A56)
		regionoff = 12; /* P.T. Barnum's Acrobats! (European version - Extra keys) */

	if (app_data.crc == 0xFB83171E)
		mxsnap = 3; /* Blockout*/
	if (app_data.crc == 0xD38153F6)
		mxsnap = 3; /* Blockout (french) */
	if (app_data.crc == 0xA57E1724)
		mxsnap = 12; /* Catch the Ball / Noughts and Crosses */
	if (app_data.crc == 0xBE4FF48E)
		mxsnap = 12; /* Catch the Ball / Noughts and Crosses modified */
	if (app_data.crc == 0xFD179F6D)
		mxsnap = 3; /* Clay Pigeon! */
	if (app_data.crc == 0x9C9DDDF9)
		mxsnap = 3; /* Verkehr */
	if (app_data.crc == 0x95936B07)
		mxsnap = 3; /* Super Cobra */
	if (app_data.crc == 0x881CEAE4)
		mxsnap = 3; /* Wall Street */
	if (app_data.crc == 0x9E42E766)
		mxsnap = 0; /* Turtles */
	if (app_data.crc == 0x1C750349)
		mxsnap = 0; /* Turtles (European version) */
	if (app_data.crc == 0xD0BC4EE6)
		mxsnap = 3; /* Frogger */
	if (app_data.crc == 0xA57D84F3)
		mxsnap = 3; /* Frogger BR */
	if (app_data.crc == 0x3BFEF56B)
		mxsnap = 6; /* Four in 1 Row! */
	if (app_data.crc == 0x9B5E9356)
		mxsnap = 6; /* Four in 1 Row! (french) */
	if (app_data.crc == 0x7C747245)
		mxsnap = 6; /* Four in 1 Row! modified */

	if (app_data.crc == 0xA7344D1F)
		setvideomode(1); /* Atlantis */
	if (app_data.crc == 0x39E31BF0)
		setvideomode(1); /* Jake */
	if (app_data.crc == 0x92D0177B)
		setvideomode(1); /* Jake (hack) */
	if (app_data.crc == 0x3351FEDA)
		setvideomode(1); /* Power Lords */
	if (app_data.crc == 0x40AE062D)
		setvideomode(1); /* Power Lords (alternate) */
	if (app_data.crc == 0xD158EEBA)
		setvideomode(1); /* Labirinth */
	if (app_data.crc == 0x26B0FF5B)
		setvideomode(1); /* Nightmare */
	if (app_data.crc == 0xDF36683F)
		setvideomode(1); /* Shark Hunter */
	if (app_data.crc == 0xAF307559)
		setvideomode(1); /* Super Bee 8Kb */
	if (app_data.crc == 0x9585D511)
		setvideomode(1); /* Super Bee 4Kb */
	if (app_data.crc == 0x58FA6766)
		setvideomode(1); /* War of the Nerves */
	if (app_data.crc == 0x58FA6766)
		setvideomode(1); /* War of the Nerves */
	if (app_data.crc == 0x39989464)
		setvideomode(1); /* Hockey! / Soccer! */
	if (app_data.crc == 0x3BFEF56B)
		setvideomode(1); /* Four in 1 Row! */
	if (app_data.crc == 0x9B5E9356)
		setvideomode(1); /* Four in 1 Row! (french) */
	if (app_data.crc == 0x7C747245)
		setvideomode(1); /* Four in 1 Row! modified */
	if (app_data.crc == 0x68560DC7)
		setvideomode(1); /* Jopac Moto Crash */
	if (app_data.crc == 0x020FCA15)
		setvideomode(1); /* Jopac Moto Crash modified (non VP+) */
	if (app_data.crc == 0xC4134DF8)
		setvideomode(1); /* Helicopter Rescue + */
	if (app_data.crc == 0x0D2D721D)
		setvideomode(1); /* Trans American Rally + */
	if (app_data.crc == 0x9D72D4E9)
		setvideomode(1); /* Blobbers */
	if (app_data.crc == 0xB2F0F0B4)
		setvideomode(1); /* Le Tresor Englouti + */
	if (app_data.crc == 0x0B2DEB61)
		setvideomode(1); /* Tutankham */
	if (app_data.crc == 0x313547EB)
		setvideomode(1); /* VP53 */
	if (app_data.crc == 0x06861A9C)
		setvideomode(1); /* Flashpoint 5 (Videopac adaption) */
	if (app_data.crc == 0xA57E1724)
		setvideomode(0); /* Catch the Ball / Noughts and Crosses */
	if (app_data.crc == 0xBE4FF48E)
		setvideomode(0); /* Catch the Ball / Noughts and Crosses modified */
	if (app_data.crc == 0xFB83171E)
		setvideomode(0); /* Blockout*/
	if (app_data.crc == 0xD38153F6)
		setvideomode(0); /* Blockout (french) */
	if (app_data.crc == 0x9BFC3E01)
		setvideomode(0); /* Demon Attack */
	if (app_data.crc == 0x50AF9D45)
		setvideomode(0); /* Demon Attack + */
	if (app_data.crc == 0x9884EF36)
		setvideomode(0); /* Demon Attack + modified */
	if (app_data.crc == 0x4A578DFE)
		setvideomode(0); /* Restaurant ntsc */
	if (app_data.crc == 0x863D5E2D)
		setvideomode(0); /* Shark Hunter ntsc */

	if (app_data.crc == 0xD62814A3)
		evblclk = 12000; /* Pick Axe Pete */
	if (app_data.crc == 0xB2FFB353)
		evblclk = 12000; /* Pick Axe Pete + */
	if (app_data.crc == 0x81C20196)
		evblclk = 12000; /* Pick Axe Pete + (modified) */

	if ((app_data.crc == 0xF390BFEC) || (app_data.crc == 0x825976A9) || (app_data.crc == 0x61A350E6))
	{ /* Mousing Cat */
		setvideomode(1);
		evblclk = 7642;
	}

	if (app_data.crc == 0xD0BC4EE6)
	{ /* Frogger */
		setvideomode(1);
		evblclk = 7642;
	}
	if (app_data.crc == 0x26517E77)
	{ /* Commando Noturno */
		setvideomode(1);
		evblclk = 6100;
		regionoff = 12;
	}
	if (app_data.crc == 0xA57E1724)
	{ /* Catch the ball*/
		regionoff = 5;
		sproff = 1;
	}

	if ((app_data.crc == 0x2DCB77F0) || (app_data.crc == 0xF6882734))
	{ /* Depth Charge / Marksman */
		setvideomode(1);
		evblclk = 8000;
	}
	if (app_data.crc == 0x881CEAE4)
	{ /* Wall Street */
		setvideomode(1);
		evblclk = 6100;
	}
	if (app_data.crc == 0xD0BC4EE6)
		tweakedaudio = 1; /* Frogger */
	if (app_data.crc == 0xA57D84F3)
		tweakedaudio = 1; /* Frogger BR */
	if (app_data.crc == 0x5216771A)
		tweakedaudio = 1; /* Popeye */
	if (app_data.crc == 0xAFB23F89)
		tweakedaudio = 1; /* Musician */
	if (app_data.crc == 0xC4134DF8)
		tweakedaudio = 1; /* Helicopter Rescue + */
	if (app_data.crc == 0x0D2D721D)
		tweakedaudio = 1; /* Trans American Rally + */

	if (app_data.crc == 0xD3B09FEC)
		sproff = 1; /* Volleyball! */
	if (app_data.crc == 0x551E38A2)
		sproff = 1; /* Volleyball! (french) */
}

int snapline(int pos, Byte reg, int t)
{
	int i;
	if (pos < MAXLINES + MAXSNAP + MAXSNAP)
	{
		for (i = 0; i < mxsnap; i++)
		{
			if (snapedlines[pos + MAXSNAP - i][reg][t])
				return pos - i;
			if (snapedlines[pos + MAXSNAP + i][reg][t])
				return pos + i;
		}
		snapedlines[pos + MAXSNAP][reg][t] = 1;
	}
	return pos;
}

static void setvideomode(int t)
{
	if (t)
	{
		evblclk = EVBLCLK_PAL;
		fps = FPS_PAL;
	}
	else
	{
		evblclk = EVBLCLK_NTSC;
		fps = FPS_NTSC;
	}
}
