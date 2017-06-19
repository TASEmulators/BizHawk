#include "sgb.h"
#include "utils.h"
#include <stdlib.h>
#include <string.h>

typedef struct
{
	// writes to FF00
	uint64_t last_write_time;
	uint8_t last_write_value;

	// recv packets
	uint8_t read_index; // 0-127, index of the next bit read.  if 255, not currently reading
	uint8_t packet[16]; // a packet in the process of being formed

	uint8_t command[16 * 7];  // a command in the process of being formed
	uint8_t expected_packets; // total number of packets expected for a command
	uint8_t next_packet;	  // index of the next packet to be read

	// joypad reading
	uint8_t joypad_index;		  // index of currently reading joypad
	uint8_t num_joypads;		  // number of currently selected joypads (MLT_REQ)
	uint8_t joypad_data[4];		  // data for each joypad
	uint8_t joypad_has_been_read; // state for advancing joypad_index.  extermely weird; logic lifted from VBA and probably wrong

	// palettes
	uint32_t palette[8][16];
	uint32_t auxpalette[512][4];

	// frame data
	uint8_t frame[160 * 144];		// the most recent obtained full frame
	uint8_t frozenframe[160 * 144]; // the most recent saved full frame (MASK_EN)
	uint8_t attr[20 * 18];			// current attr map for the GB screen
	uint8_t auxattr[45][20 * 18];   // 45 attr files

	// MASK_EN
	uint8_t waiting_mask; // true if waiting to capture a mask
	uint8_t active_mask;  // true if mask is currently being used

	// transfers
	uint32_t waiting_transfer;
#define TRN_NONE 0
#define TRN_SOUND 1
#define TRN_PAL 2
#define TRN_CHR_LOW 3
#define TRN_CHR_HI 4
#define TRN_PCT 5
#define TRN_ATTR 6
	int32_t transfer_countdown; // number of frames until transfer.  not entirely accurate
} sgb_t;

static sgb_t sgb;

static uint32_t makecol(uint16_t c)
{
	return c >> 7 & 0xf8 | c >> 12 & 0x07 | c << 6 & 0xf800 | c << 1 & 0x0700 | c << 19 & 0xf80000 | c << 14 & 0x070000 | 0xff000000;
}

static void cmd_trn(uint32_t which)
{
	if ((sgb.command[0] & 7) == 1)
	{
		if (sgb.waiting_transfer == TRN_NONE)
		{
			sgb.waiting_transfer = which;
			sgb.transfer_countdown = 4;
		}
		else
		{
			utils_log("SGB: TRN already queued!");
		}
	}
	else
	{
		utils_log("SGB: cmd_trn bad length");
	}
}

static void cmd_pal(int a, int b)
{
	if ((sgb.command[0] & 7) == 1)
	{
		uint32_t c[7];
		for (int i = 0; i < 7; i++)
			c[i] = makecol(sgb.command[i * 2 + 1] | sgb.command[i * 2 + 2] << 8);
		sgb.palette[0][0] = c[0];
		sgb.palette[a][1] = c[1];
		sgb.palette[a][2] = c[2];
		sgb.palette[a][3] = c[3];
		sgb.palette[b][1] = c[4];
		sgb.palette[b][2] = c[5];
		sgb.palette[b][3] = c[6];
	}
	else
	{
		utils_log("SGB: cmd_pal bad length");
	}
}

static void cmd_pal_set(void)
{
	if ((sgb.command[0] & 7) == 1)
	{
		for (int i = 0; i < 4; i++)
		{
			int p = sgb.command[i * 2 + 1] | sgb.command[i * 2 + 2] << 8 & 0x100;
			for (int j = 0; j < 4; j++)
				sgb.palette[i][j] = sgb.auxpalette[p][j];
		}
		if (sgb.command[9] & 0x80) // change attribute
		{
			int attr = sgb.command[9] & 0x3f;
			if (attr >= 45)
				attr = 44;
			memcpy(sgb.attr, sgb.auxattr[attr], sizeof(sgb.attr));
		}
		if (sgb.command[9] & 0x40) // cancel mask
		{
			sgb.waiting_mask = 0;
			sgb.active_mask = 0;
		}
	}
	else
	{
		utils_log("SGB: cmd_pal bad length");
	}
}

static void cmd_attr_blk()
{
}

static void cmd_attr_lin()
{
	int nset = sgb.command[1];
	if (nset <= 0 || nset >= 111)
	{
		utils_log("SGB: cmd_attr_lin bad nset");
		return;
	}
	int npacket = (nset + 17) / 16;
	if ((sgb.command[0] & 7) != npacket)
	{
		utils_log("SGB: cmd_attr_lin bad length");
		return;
	}
	for (int i = 0; i < nset; i++)
	{
		uint8_t v = sgb.command[i + 2];
		int line = v & 31;
		int a = v >> 5 & 3;
		if (v & 0x80) // horizontal
		{
			if (line > 17)
				line = 17;
			memset(sgb.attr + line * 20, a, 20);
		}
		else // vertical
		{
			if (line > 19)
				line = 19;
			uint8_t *dst = sgb.attr + line;
			for (int i = 0; i < 18; i++, dst += 20)
				dst[0] = a;
		}
	}
}

static void cmd_attr_div()
{
	if ((sgb.command[0] & 7) == 1)
	{
		uint8_t v = sgb.command[1];

		int c = v & 3;
		int a = v >> 2 & 3;
		int b = v >> 4 & 3;

		int pos = sgb.command[2];
		uint8_t *dst = sgb.attr;
		if (v & 0x40) // horizontal
		{
			if (pos > 17)
				pos = 17;
			int i;
			for (i = 0; i < pos; i++, dst += 20)
				memset(dst, a, 20);
			memset(dst, b, 20);
			i++, dst += 20;
			for (; i < 18; i++, dst += 20)
				memset(dst, c, 20);
		}
		else // vertical
		{
			if (pos > 19)
				pos = 19;
			for (int j = 0; j < 18; j++)
			{
				int i;
				for (i = 0; i < pos; i++)
					*dst++ = a;
				*dst++ = b;
				i++;
				for (; i < 20; i++)
					*dst++ = c;
			}
		}
	}
	else
	{
		utils_log("SGB: cmd_attr_div bad length");
	}
}

static void cmd_attr_chr()
{
	int x = sgb.command[1];
	int y = sgb.command[2];
	int n = sgb.command[3] | sgb.command[4] << 8;
	if (n > 360)
	{
		utils_log("SGB: cmd_attr_chr bad n");
		return;
	}
	int npacket = (n + 87) / 64;
	if ((sgb.command[0] & 7) != npacket)
	{
		utils_log("SGB: cmd_attr_chr bad length");
		return;
	}
	uint8_t *dst = sgb.attr;
	if (x > 19)
		x = 19;
	if (y > 17)
		y = 17;
	int vertical = sgb.command[5];
	for (int i = 0; i < 360; i++)
	{
		uint8_t v = i / 4 + 6;
		int a = v >> (2 * (3 - (i & 3))) & 3;
		dst[y * 20 + x] = a;
		if (vertical)
		{
			y++;
			if (y == 18)
			{
				y = 0;
				x++;
				if (x == 20)
					return;
			}
		}
		else
		{
			x++;
			if (x == 20)
			{
				x = 0;
				y++;
				if (y == 18)
					return;
			}
		}
	}
}

static void cmd_attr_set()
{
	if ((sgb.command[0] & 7) == 1)
	{
		int attr = sgb.command[1] & 0x3f;
		if (attr >= 45)
			attr = 44;
		memcpy(sgb.attr, sgb.auxattr[attr], sizeof(sgb.attr));
		if (sgb.command[1] & 0x40)
		{
			sgb.waiting_mask = 0;
			sgb.active_mask = 0;
		}
	}
	else
	{
		utils_log("SGB: cmd_attr_set bad length");
	}
}

static void cmd_mlt_req(void)
{
	if ((sgb.command[0] & 7) == 1)
	{
		switch (sgb.command[1] & 3)
		{
		case 0:
		case 2:
			sgb.num_joypads = 1;
			sgb.joypad_index = 0;
			break;
		case 1:
			sgb.num_joypads = 2;
			sgb.joypad_index = 1;
			break;
		case 3:
			sgb.num_joypads = 4;
			sgb.joypad_index = 1;
			break;
		}
		utils_log("SGB: %u joypads", sgb.num_joypads);
	}
	else
	{
		utils_log("SGB: cmd_mlt_req bad length");
	}
}

static void cmd_mask(void)
{
	if ((sgb.command[0] & 7) == 1)
	{
		switch (sgb.command[1] & 3)
		{
		case 0:
			sgb.waiting_mask = 0;
			sgb.active_mask = 0;
			break;
		case 1:
			sgb.waiting_mask = 1;
			break;
		case 2:
		case 3:
			sgb.waiting_mask = 0;
			sgb.active_mask = 1;
			memset(sgb.frozenframe, 0, sizeof(sgb.frozenframe));
			break;
		}
	}
	else
	{
		utils_log("SGB: cmd_mask bad length");
	}
}

static void do_command(void)
{
	const int command = sgb.command[0] >> 3;
	switch (command)
	{
	default:
		utils_log("SGB: Unknown or unimplemented command %02xh", command);
		break;

	case 0x00: // PAL01
		utils_log("SGB: PAL01");
		cmd_pal(0, 1);
		break;
	case 0x01: // PAL23
		utils_log("SGB: PAL23");
		cmd_pal(2, 3);
		break;
	case 0x02: // PAL03
		utils_log("SGB: PAL03");
		cmd_pal(0, 3);
		break;
	case 0x03: // PAL12
		utils_log("SGB: PAL12");
		cmd_pal(1, 2);
		break;
	case 0x0a: // PAL_SET
		utils_log("SGB: PAL_SET");
		cmd_pal_set();
		break;

	case 0x04: // ATTR_BLK
		utils_log("SGB: ATTR_BLK");
		cmd_attr_blk();
		break;
	case 0x05: // ATTR_LIN
		utils_log("SGB: ATTR_LIN");
		cmd_attr_lin();
		break;
	case 0x06: // ATTR_DIV
		utils_log("SGB: ATTR_DIV");
		cmd_attr_div();
		break;
	case 0x07: // ATTR_CHR
		utils_log("SGB: ATTR_CHR");
		cmd_attr_chr();
		break;
	case 0x16: // ATTR_SET
		utils_log("SGB: ATTR_SET");
		cmd_attr_set();
		break;

	case 0x17: // MASK_EN
		utils_log("SGB: MASK_EN");
		cmd_mask();
		break;

	// unknown functions
	case 0x0c: // ATRC_EN
		utils_log("SGB: ATRC_EN??");
		break;
	case 0x0d: // TEST_EN
		utils_log("SGB: TEST_EN??");
		break;
	case 0x0e: // ICON_EN
		utils_log("SGB: ICON_EN??");
		break;
	case 0x18: // OBJ_TRN
		// no game used this
		utils_log("SGB: OBJ_TRN??");
		break;

	// unimplementable functions
	case 0x0f: // DATA_SND
		// TODO: Is it possible for this (and DATA_TRN) to write data to
		// memory areas used for the attribute file, etc?
		// If so, do games do this?
		utils_log("SGB: DATA_SND!!");
		break;
	case 0x10: // DATA_TRN
		utils_log("SGB: DATA_TRN!!");
		break;
	case 0x12: // JUMP
		utils_log("SGB: JUMP!!");
		break;

	// joypad
	case 0x11: // MLT_REQ
		utils_log("SGB: MLT_REQ");
		cmd_mlt_req();
		break;

	// all vram transfers
	case 0x09: // SOU_TRN
		utils_log("SGB: SOU_TRN");
		cmd_trn(TRN_SOUND);
		break;
	case 0x0b: // PAL_TRN
		utils_log("SGB: PAL_TRN");
		cmd_trn(TRN_PAL);
		break;
	case 0x13: // CHR_TRN
		utils_log("SGB: CHR_TRN");
		cmd_trn(sgb.command[1] & 1 ? TRN_CHR_HI : TRN_CHR_LOW);
		break;
	case 0x14: // PCT_TRN
		utils_log("SGB: PCT_TRN");
		cmd_trn(TRN_PCT);
		break;
	case 0x15: // ATTR_TRN
		utils_log("SGB: ATTR_TRN");
		cmd_trn(TRN_ATTR);
		break;
	}
}

static void do_packet(void)
{
	memcpy(sgb.command + sgb.next_packet * 16, sgb.packet, sizeof(sgb.packet));
	sgb.next_packet++;

	if (sgb.expected_packets == 0) // not in the middle of a command
		sgb.expected_packets = sgb.command[0] & 7;

	if (sgb.expected_packets == 0) // huh?
	{
		utils_log("SGB: zero packet command");
		sgb.expected_packets = 0;
		sgb.next_packet = 0;
	}
	else if (sgb.next_packet == sgb.expected_packets)
	{
		do_command();
		sgb.expected_packets = 0;
		sgb.next_packet = 0;
	}
}

void sgb_init(void)
{
	memset(&sgb, 0, sizeof(sgb));
	sgb.read_index = 255;
	sgb.num_joypads = 1;
}

void sgb_write_ff00(uint8_t val, uint64_t time)
{
	val &= 0x30;

	//utils_log("ZZ: %02x, %llu", val, time);
	const int p14_fell = (val & 0x10) < (sgb.last_write_value & 0x10);
	const int p15_fell = (val & 0x20) < (sgb.last_write_value & 0x20);
	const int p14_rose = (val & 0x10) > (sgb.last_write_value & 0x10);
	const int p15_rose = (val & 0x20) > (sgb.last_write_value & 0x20);

	if (val == 0) // reset command processing
	{
		sgb.read_index = 0;
		memset(sgb.packet, 0, sizeof(sgb.packet));
	}
	else if (sgb.read_index != 255) // currently reading a packet
	{
		if (p14_fell || p15_fell)
		{
			if (sgb.read_index == 128) // end of packet
			{
				if (p14_fell)
					do_packet();
				else
					utils_log("SGB: Stop bit not present");
				sgb.read_index = 255;
			}
			else
			{
				if (p15_fell)
				{
					int byte = sgb.read_index >> 3;
					int bit = sgb.read_index & 7;
					sgb.packet[byte] |= 1 << bit;
				}
				sgb.read_index++;
			}
		}
	}
	else // joypad processing
	{
		if (val == 0x10)
			sgb.joypad_has_been_read |= 2; // reading P15
		if (val == 0x20)
			sgb.joypad_has_been_read |= 1; // reading P14
		if (val == 0x30 && (p14_rose || p15_rose))
		{
			if (sgb.joypad_has_been_read == 7)
			{
				sgb.joypad_has_been_read = 0;
				sgb.joypad_index++;
				sgb.joypad_index &= sgb.num_joypads - 1;
				//utils_log("SGB: joypad index to %u", sgb.joypad_index);
			}
			else
			{
				sgb.joypad_has_been_read &= 3; // the other line must be asserted and a read must happen before joypad_index inc??
			}
		}
	}

	sgb.last_write_value = val;
	sgb.last_write_time = time;
}

uint8_t sgb_read_ff00(uint64_t time)
{
	uint8_t ret = sgb.last_write_value & 0xf0 | 0xc0;
	const int p14 = !(ret & 0x10);
	const int p15 = !(ret & 0x20);
	const int ji = sgb.joypad_index;

	// TODO: is this "reset" correct?
	sgb.joypad_has_been_read |= 4; // read occured
	sgb.read_index = 255;
	sgb.next_packet = 0;
	sgb.expected_packets = 0;

	if (!p14 && !p15)
	{
		//utils_log("SGB: SCAN%u", ji);
		// scan id
		return ret | (15 - ji);
	}
	else
	{
		// get data
		const uint8_t j = sgb.joypad_data[ji];
		if (p14)
			ret |= j >> 4;
		if (p15)
			ret |= j & 0x0f;
		//utils_log("SGB: READ%u %02x", ji, ret ^ 0x0f);
		return ret ^ 0x0f;
	}
}

// for each of 4 joypads:
// 7......0
// DULRSsBA
void sgb_set_controller_data(const uint8_t *buttons)
{
	memcpy(sgb.joypad_data, buttons, sizeof(sgb.joypad_data));
}

static void trn_pal(const uint8_t *data)
{
	const uint16_t *src = (const uint16_t *)data;
	uint32_t *dst = (uint32_t *)sgb.auxpalette;
	for (int i = 0; i < 2048; i++)
		dst[i] = makecol(src[i]);
}

static void trn_attr(const uint8_t *data)
{
	uint8_t *dst = (uint8_t *)sgb.auxattr;
	for (int n = 0; n < 45 * 90; n++)
	{
		uint8_t s = *data++;
		*dst++ = s >> 6 & 3;
		*dst++ = s >> 4 & 3;
		*dst++ = s >> 2 & 3;
		*dst++ = s >> 0 & 3;
	}
}

#include "mmu.h"
static void do_vram_transfer(void)
{
	uint8_t vram[4096];
	for (int tilenum = 0; tilenum < 256; tilenum++)
	{
		const int ty = tilenum / 20;
		const int tx = tilenum % 20;
		const uint8_t *src = sgb.frame + ty * 8 * 160 + tx * 8;
		uint8_t *dst = vram + 16 * tilenum;
		for (int j = 0; j < 8; j++)
		{
			uint32_t a = 0, b = 0;
			a |= (src[7] & 1) << 0;
			a |= (src[6] & 1) << 1;
			a |= (src[5] & 1) << 2;
			a |= (src[4] & 1) << 3;
			a |= (src[3] & 1) << 4;
			a |= (src[2] & 1) << 5;
			a |= (src[1] & 1) << 6;
			a |= (src[0] & 1) << 7;

			b |= (src[7] & 2) >> 1;
			b |= (src[6] & 2) << 0;
			b |= (src[5] & 2) << 1;
			b |= (src[4] & 2) << 2;
			b |= (src[3] & 2) << 3;
			b |= (src[2] & 2) << 4;
			b |= (src[1] & 2) << 5;
			b |= (src[0] & 2) << 6;
			*dst++ = a;
			*dst++ = b;
			src += 160;
		}
	}

	switch (sgb.waiting_transfer)
	{
	case TRN_SOUND:
		break;
	case TRN_PAL:
		trn_pal(vram);
		break;
	case TRN_CHR_LOW:
		break;
	case TRN_CHR_HI:
		break;
	case TRN_PCT:
		break;
	case TRN_ATTR:
		trn_attr(vram);
		break;
	}
}

// 160x144 32bpp pixel data
// assumed to contain exact pixel values 00, 55, aa, ff
void sgb_take_frame(uint32_t *vbuff)
{
	for (int i = 0; i < 160 * 144; i++)
	{
		sgb.frame[i] = 3 - (vbuff[i] >> 6 & 3); // 0, 1, 2, or 3 for each pixel
	}
	if (sgb.waiting_transfer != TRN_NONE)
	{
		if (!--sgb.transfer_countdown)
		{
			do_vram_transfer();
			sgb.waiting_transfer = TRN_NONE;
		}
	}
	if (sgb.waiting_mask)
	{
		memcpy(sgb.frozenframe, sgb.frame, sizeof(sgb.frame));
		sgb.waiting_mask = 0;
		sgb.active_mask = 1;
	}
}

static void sgb_render_frame_gb(uint32_t *vbuff)
{
	/*sgb.palette[0][0] = 0xff000000;
	sgb.palette[0][1] = 0xff550055;
	sgb.palette[0][2] = 0xffaa00aa;
	sgb.palette[0][3] = 0xffff00ff;

	sgb.palette[1][0] = 0xff00003f;
	sgb.palette[1][1] = 0xff00007f;
	sgb.palette[1][2] = 0xff0000bf;
	sgb.palette[1][3] = 0xff0000ff;
	
	sgb.palette[2][0] = 0xff003f00;
	sgb.palette[2][1] = 0xff007f00;
	sgb.palette[2][2] = 0xff00bf00;
	sgb.palette[2][3] = 0xff00ff00;
	
	sgb.palette[3][0] = 0xff3f0000;
	sgb.palette[3][1] = 0xff7f0000;
	sgb.palette[3][2] = 0xffbf0000;
	sgb.palette[3][3] = 0xffff0000;*/

	const uint8_t *attr = sgb.attr;
	const uint8_t *src = sgb.active_mask ? sgb.frozenframe : sgb.frame;
	uint32_t *dst = vbuff + ((224 - 144) / 2 * 256 + (256 - 160) / 2);

	for (int j = 0; j < 144; j++)
	{
		const uint8_t *attr_line = attr + j / 8 * 20;
		for (int i = 0; i < 160; i++)
		{
			const int attr_index = i / 8;
			*dst++ = sgb.palette[attr_line[attr_index]][*src++];
		}
		dst += 256 - 160;
	}
}

void sgb_render_frame(uint32_t *vbuff)
{
	sgb_render_frame_gb(vbuff);
}
