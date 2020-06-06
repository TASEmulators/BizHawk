#include "sgb.h"
#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include "snes_spc/spc.h"
#include "../emulibc/emulibc.h"

#define utils_log printf

const uint8_t iplrom[64] = {
	/*ffc0*/ 0xcd, 0xef,	   //mov   x,#$ef
	/*ffc2*/ 0xbd,			   //mov   sp,x
	/*ffc3*/ 0xe8, 0x00,	   //mov   a,#$00
	/*ffc5*/ 0xc6,			   //mov   (x),a
	/*ffc6*/ 0x1d,			   //dec   x
	/*ffc7*/ 0xd0, 0xfc,	   //bne   $ffc5
	/*ffc9*/ 0x8f, 0xaa, 0xf4, //mov   $f4,#$aa
	/*ffcc*/ 0x8f, 0xbb, 0xf5, //mov   $f5,#$bb
	/*ffcf*/ 0x78, 0xcc, 0xf4, //cmp   $f4,#$cc
	/*ffd2*/ 0xd0, 0xfb,	   //bne   $ffcf
	/*ffd4*/ 0x2f, 0x19,	   //bra   $ffef
	/*ffd6*/ 0xeb, 0xf4,	   //mov   y,$f4
	/*ffd8*/ 0xd0, 0xfc,	   //bne   $ffd6
	/*ffda*/ 0x7e, 0xf4,	   //cmp   y,$f4
	/*ffdc*/ 0xd0, 0x0b,	   //bne   $ffe9
	/*ffde*/ 0xe4, 0xf5,	   //mov   a,$f5
	/*ffe0*/ 0xcb, 0xf4,	   //mov   $f4,y
	/*ffe2*/ 0xd7, 0x00,	   //mov   ($00)+y,a
	/*ffe4*/ 0xfc,			   //inc   y
	/*ffe5*/ 0xd0, 0xf3,	   //bne   $ffda
	/*ffe7*/ 0xab, 0x01,	   //inc   $01
	/*ffe9*/ 0x10, 0xef,	   //bpl   $ffda
	/*ffeb*/ 0x7e, 0xf4,	   //cmp   y,$f4
	/*ffed*/ 0x10, 0xeb,	   //bpl   $ffda
	/*ffef*/ 0xba, 0xf6,	   //movw  ya,$f6
	/*fff1*/ 0xda, 0x00,	   //movw  $00,ya
	/*fff3*/ 0xba, 0xf4,	   //movw  ya,$f4
	/*fff5*/ 0xc4, 0xf4,	   //mov   $f4,a
	/*fff7*/ 0xdd,			   //mov   a,y
	/*fff8*/ 0x5d,			   //mov   x,a
	/*fff9*/ 0xd0, 0xdb,	   //bne   $ffd6
	/*fffb*/ 0x1f, 0x00, 0x00, //jmp   ($0000+x)
	/*fffe*/ 0xc0, 0xff		   //reset vector location ($ffc0)
};

// the "reference clock" is tied to the GB cpu.  35112 of these should equal one GB LCD frame.
// it is always increasing and never resets/rebases

const int refclocks_per_spc_sample = 67; // ~32055hz

typedef struct
{
	// writes to FF00
	uint64_t last_write_time; // last write time relative to reference clock
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

	// border
	uint8_t tiles[256][64]; // tiles stored in packed form
	uint16_t tilemap[32 * 32];

	// frame data
	uint8_t frame[160 * 144];		 // the most recent obtained full frame
	uint32_t frozenframe[256 * 224]; // the most recent saved full frame (MASK_EN)
	uint8_t attr[20 * 18];			 // current attr map for the GB screen
	uint8_t auxattr[45][20 * 18];	// 45 attr files

	// MASK_EN
	uint8_t active_mask; // true if mask is currently being used

	// audio
	SNES_SPC *spc;
	uint64_t frame_start;	 // when the current audio frame started relative to reference clock
	uint32_t clock_remainder; // number of reference clocks not sent to the SPC last frame
	uint8_t sound_control[4]; // TODO...

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
			utils_log("SGB: TRN already queued!\n");
		}
	}
	else
	{
		utils_log("SGB: cmd_trn bad length\n");
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
		sgb.palette[1][0] = c[0];
		sgb.palette[2][0] = c[0];
		sgb.palette[3][0] = c[0];
		sgb.palette[a][1] = c[1];
		sgb.palette[a][2] = c[2];
		sgb.palette[a][3] = c[3];
		sgb.palette[b][1] = c[4];
		sgb.palette[b][2] = c[5];
		sgb.palette[b][3] = c[6];
	}
	else
	{
		utils_log("SGB: cmd_pal bad length\n");
	}
}

static void cmd_pal_set(void)
{
	if ((sgb.command[0] & 7) == 1)
	{
		int p0 = sgb.command[1] | sgb.command[2] << 8 & 0x100;
		for (int i = 0; i < 4; i++)
		{
			int p = sgb.command[i * 2 + 1] | sgb.command[i * 2 + 2] << 8 & 0x100;
			sgb.palette[i][0] = sgb.auxpalette[p0][0];
			sgb.palette[i][1] = sgb.auxpalette[p][1];
			sgb.palette[i][2] = sgb.auxpalette[p][2];
			sgb.palette[i][3] = sgb.auxpalette[p][3];
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
			sgb.active_mask = 0;
		}
	}
	else
	{
		utils_log("SGB: cmd_pal bad length\n");
	}
}

static void cmd_attr_blk()
{
	int nset = sgb.command[1];
	if (nset <= 0 || nset >= 19)
	{
		utils_log("SGB: cmd_attr_blk bad nset\n");
		return;
	}
	int npacket = (nset * 6 + 16) / 16;
	if ((sgb.command[0] & 7) != npacket)
	{
		utils_log("SGB: cmd_attr_blk bad length\n");
		return;
	}
	for (int i = 0; i < nset; i++)
	{
		int ctrl = sgb.command[i * 6 + 2] & 7;
		int pals = sgb.command[i * 6 + 3];
		int x1 = sgb.command[i * 6 + 4];
		int y1 = sgb.command[i * 6 + 5];
		int x2 = sgb.command[i * 6 + 6];
		int y2 = sgb.command[i * 6 + 7];
		int inside = ctrl & 1;
		int line = ctrl & 2;
		int outside = ctrl & 4;
		int insidepal = pals & 3;
		int linepal = pals >> 2 & 3;
		int outsidepal = pals >> 4 & 3;
		if (ctrl == 1)
		{
			ctrl = 3;
			linepal = insidepal;
		}
		else if (ctrl == 4)
		{
			ctrl = 6;
			linepal = outsidepal;
		}
		uint8_t *dst = sgb.attr;
		for (int y = 0; y < 18; y++)
		{
			for (int x = 0; x < 20; x++)
			{
				if (outside && (x < x1 || x > x2 || y < y1 || y > y2))
					*dst = outsidepal;
				else if (inside && x > x1 && x < x2 && y > y1 && y < y2)
					*dst = insidepal;
				else if (line)
					*dst = linepal;
				dst++;
			}
		}
	}
}

static void cmd_attr_lin()
{
	int nset = sgb.command[1];
	if (nset <= 0 || nset >= 111)
	{
		utils_log("SGB: cmd_attr_lin bad nset\n");
		return;
	}
	int npacket = (nset + 17) / 16;
	if ((sgb.command[0] & 7) != npacket)
	{
		utils_log("SGB: cmd_attr_lin bad length\n");
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
		utils_log("SGB: cmd_attr_div bad length\n");
	}
}

static void cmd_attr_chr()
{
	int x = sgb.command[1];
	int y = sgb.command[2];
	int n = sgb.command[3] | sgb.command[4] << 8;
	if (n > 360)
	{
		utils_log("SGB: cmd_attr_chr bad n\n");
		return;
	}
	int npacket = (n + 87) / 64;
	if ((sgb.command[0] & 7) < npacket)
	{
		utils_log("SGB: cmd_attr_chr bad length\n");
		return;
	}
	uint8_t *dst = sgb.attr;
	if (x > 19)
		x = 19;
	if (y > 17)
		y = 17;
	int vertical = sgb.command[5];
	for (int i = 0; i < n; i++)
	{
		uint8_t v = sgb.command[i / 4 + 6];
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
			sgb.active_mask = 0;
		}
	}
	else
	{
		utils_log("SGB: cmd_attr_set bad length\n");
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
		utils_log("SGB: %u joypads\n", sgb.num_joypads);
	}
	else
	{
		utils_log("SGB: cmd_mlt_req bad length\n");
	}
}

static void cmd_mask(void)
{
	if ((sgb.command[0] & 7) == 1)
	{
		switch (sgb.command[1] & 3)
		{
		case 0:
			sgb.active_mask = 0;
			break;
		case 1:
			sgb.active_mask = 1;
			break;
		case 2:
		case 3:
			sgb.active_mask = 1;
			for (int i = 0; i < 256 * 224; i++)
				sgb.frozenframe[i] = sgb.palette[0][0];
			break;
		}
	}
	else
	{
		utils_log("SGB: cmd_mask bad length\n");
	}
}

static void cmd_sound(void)
{
	if ((sgb.command[0] & 7) == 1)
	{
		sgb.sound_control[1] = sgb.command[1];
		sgb.sound_control[2] = sgb.command[2];
		sgb.sound_control[3] = sgb.command[3];
		sgb.sound_control[0] = sgb.command[4];
	}
	else
	{
		utils_log("SGB: cmd_sound bad length\n");
	}
}

static void do_command(void)
{
	const int command = sgb.command[0] >> 3;
	switch (command)
	{
	default:
		utils_log("SGB: Unknown or unimplemented command %02xh\n", command);
		break;

	case 0x00: // PAL01
		utils_log("SGB: PAL01\n");
		cmd_pal(0, 1);
		break;
	case 0x01: // PAL23
		utils_log("SGB: PAL23\n");
		cmd_pal(2, 3);
		break;
	case 0x02: // PAL03
		utils_log("SGB: PAL03\n");
		cmd_pal(0, 3);
		break;
	case 0x03: // PAL12
		utils_log("SGB: PAL12\n");
		cmd_pal(1, 2);
		break;
	case 0x0a: // PAL_SET
		utils_log("SGB: PAL_SET\n");
		cmd_pal_set();
		break;

	case 0x04: // ATTR_BLK
		utils_log("SGB: ATTR_BLK\n");
		cmd_attr_blk();
		break;
	case 0x05: // ATTR_LIN
		utils_log("SGB: ATTR_LIN\n");
		cmd_attr_lin();
		break;
	case 0x06: // ATTR_DIV
		utils_log("SGB: ATTR_DIV\n");
		cmd_attr_div();
		break;
	case 0x07: // ATTR_CHR
		utils_log("SGB: ATTR_CHR\n");
		cmd_attr_chr();
		break;
	case 0x16: // ATTR_SET
		utils_log("SGB: ATTR_SET\n");
		cmd_attr_set();
		break;

	case 0x17: // MASK_EN
		utils_log("SGB: MASK_EN\n");
		cmd_mask();
		break;

	// unknown functions
	case 0x0c: // ATRC_EN
		utils_log("SGB: ATRC_EN??\n");
		break;
	case 0x0d: // TEST_EN
		utils_log("SGB: TEST_EN??\n");
		break;
	case 0x0e: // ICON_EN
		utils_log("SGB: ICON_EN??\n");
		break;
	case 0x18: // OBJ_TRN
		// no game used this
		utils_log("SGB: OBJ_TRN??\n");
		break;

	// unimplementable functions
	case 0x0f: // DATA_SND
		// TODO: Is it possible for this (and DATA_TRN) to write data to
		// memory areas used for the attribute file, etc?
		// If so, do games do this?
		utils_log("SGB: DATA_SND!! %02x:%02x%02x [%02x]\n", sgb.command[3], sgb.command[2], sgb.command[1], sgb.command[4]);
		break;
	case 0x10: // DATA_TRN
		utils_log("SGB: DATA_TRN!!\n");
		break;
	case 0x12: // JUMP
		utils_log("SGB: JUMP!!\n");
		break;

	// joypad
	case 0x11: // MLT_REQ
		utils_log("SGB: MLT_REQ\n");
		cmd_mlt_req();
		break;

	// sound
	case 0x08: // SOUND
		utils_log("SGB: SOUND %02x %02x %02x %02x\n", sgb.command[1], sgb.command[2], sgb.command[3], sgb.command[4]);
		cmd_sound();
		break;

	// all vram transfers
	case 0x09: // SOU_TRN
		utils_log("SGB: SOU_TRN\n");
		cmd_trn(TRN_SOUND);
		break;
	case 0x0b: // PAL_TRN
		utils_log("SGB: PAL_TRN\n");
		cmd_trn(TRN_PAL);
		break;
	case 0x13: // CHR_TRN
		utils_log("SGB: CHR_TRN\n");
		cmd_trn(sgb.command[1] & 1 ? TRN_CHR_HI : TRN_CHR_LOW);
		break;
	case 0x14: // PCT_TRN
		utils_log("SGB: PCT_TRN\n");
		cmd_trn(TRN_PCT);
		break;
	case 0x15: // ATTR_TRN
		utils_log("SGB: ATTR_TRN\n");
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
		utils_log("SGB: zero packet command\n");
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

int sgb_init(const uint8_t *spc, int length)
{
	memset(&sgb, 0, sizeof(sgb));
	sgb.read_index = 255;
	sgb.num_joypads = 1;
	sgb.palette[0][0] = 0xffffffff;
	sgb.palette[0][1] = 0xffaaaaaa;
	sgb.palette[0][2] = 0xff555555;
	sgb.palette[0][3] = 0xff000000;

	sgb.spc = spc_new();
	spc_init_rom(sgb.spc, iplrom);
	spc_reset(sgb.spc);
	if (spc_load_spc(sgb.spc, spc, length) != NULL)
	{
		utils_log("SGB: Failed to load SPC\n");
		return 0;
	}

	// make a scratch buffer in a predictable (not stack) place because spc stores multiple pointers to it
	// which is kind of nasty...
	int16_t *sound_buffer = alloc_invisible(4096 * sizeof(int16_t));

	// the combination of the sameboy bootrom plus the built in SPC file we use means
	// that the SPC doesn't finish its init fast enough for donkey kong, which starts poking
	// data too early.  it's just a combination of various HLE concerns not meshing...
	spc_set_output(sgb.spc, sound_buffer, 4096);
	for (int i = 0; i < 240; i++)
	{
		spc_end_frame(sgb.spc, 35104);
	}

	return 1;
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
					utils_log("SGB: Stop bit not present\n");
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

static void trn_sound(const uint8_t *data)
{
	const uint8_t *const dataend = data + 0x10000;
	uint8_t *const dst = spc_get_ram(sgb.spc);

	while (1)
	{
		if (data + 4 > dataend)
		{
			utils_log("TRN_SOUND header overflow\n");
			break;
		}
		int len = data[0] | data[1] << 8;
		int addr = data[2] | data[3] << 8;
		if (!len)
		{
			utils_log("TRN_SOUND END %04x\n", addr);
			break;
		}
		data += 4;
		if (data + len > dataend)
		{
			utils_log("TRN_SOUND src overflow\n");
			break;
		}
		if (addr + len >= 0x10000)
		{
			utils_log("TRN_SOUND dst overflow\n");
			return;
		}
		utils_log("TRN_SOUND addr %04x len %04x\n", addr, len);
		memcpy(dst + addr, data, len);
		data += len;
	}
}

static void trn_pal(const uint8_t *data)
{
	const uint16_t *src = (const uint16_t *)data;
	uint32_t *dst = sgb.auxpalette[0];
	for (int i = 0; i < 2048; i++)
		dst[i] = makecol(src[i]);
}

static void trn_attr(const uint8_t *data)
{
	uint8_t *dst = sgb.auxattr[0];
	for (int n = 0; n < 45 * 90; n++)
	{
		uint8_t s = *data++;
		*dst++ = s >> 6 & 3;
		*dst++ = s >> 4 & 3;
		*dst++ = s >> 2 & 3;
		*dst++ = s >> 0 & 3;
	}
}

static void trn_pct(const uint8_t *data)
{
	memcpy(sgb.tilemap, data, sizeof(sgb.tilemap));
	const uint16_t *palettes = (const uint16_t *)(data + sizeof(sgb.tilemap));
	uint32_t *dst = sgb.palette[4];
	for (int i = 0; i < 64; i++)
		dst[i] = makecol(palettes[i]);
}

static void trn_chr(const uint8_t *data, int bank)
{
	uint8_t *dst = sgb.tiles[128 * bank];
	for (int n = 0; n < 128; n++)
	{
		for (int y = 0; y < 8; y++)
		{
			int a = data[0];
			int b = data[1] << 1;
			int c = data[16] << 2;
			int d = data[17] << 3;
			for (int x = 7; x >= 0; x--)
			{
				dst[x] = a & 1 | b & 2 | c & 4 | d & 8;
				a >>= 1;
				b >>= 1;
				c >>= 1;
				d >>= 1;
			}
			dst += 8;
			data += 2;
		}
		data += 16;
	}
}

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
		trn_sound(vram);
		break;
	case TRN_PAL:
		trn_pal(vram);
		break;
	case TRN_CHR_LOW:
		trn_chr(vram, 0);
		break;
	case TRN_CHR_HI:
		trn_chr(vram, 1);
		break;
	case TRN_PCT:
		trn_pct(vram);
		break;
	case TRN_ATTR:
		trn_attr(vram);
		break;
	}
}

static void sgb_render_frame_gb(uint32_t *vbuff)
{
	const uint8_t *attr = sgb.attr;
	const uint8_t *src = sgb.frame;
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

static void draw_tile(uint16_t entry, uint32_t *dest)
{
	const uint8_t *tile = sgb.tiles[entry & 0xff];
	const uint32_t *palette = sgb.palette[entry >> 10 & 7];
	int hflip = entry & 0x4000;
	int vflip = entry & 0x8000;
	int hinc, vinc;
	if (hflip)
	{
		hinc = -1;
		dest += 7;
	}
	else
	{
		hinc = 1;
	}
	if (vflip)
	{
		vinc = -256;
		dest += 7 * 256;
	}
	else
	{
		vinc = 256;
	}
	vinc -= 8 * hinc;
	for (int y = 0; y < 8; y++, dest += vinc)
	{
		for (int x = 0; x < 8; x++, dest += hinc)
		{
			int c = *tile++;
			if (c)
				*dest = palette[c];
		}
	}
}

static void sgb_render_border(uint32_t *vbuff)
{
	const uint16_t *tilemap = sgb.tilemap;
	for (int n = 0; n < 32 * 28; n++)
	{
		draw_tile(*tilemap++, vbuff);
		vbuff += 8;
		if ((n & 31) == 31)
			vbuff += 256 * 7;
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
	if (!sgb.active_mask)
	{
		// render the frame now
		for (int i = 0; i < 256 * 224; i++)
			sgb.frozenframe[i] = sgb.palette[0][0];
		sgb_render_frame_gb(sgb.frozenframe);
		sgb_render_border(sgb.frozenframe);
	}
}

void sgb_render_frame(uint32_t *vbuff)
{
	memcpy(vbuff, sgb.frozenframe, sizeof(sgb.frozenframe));
}

void sgb_render_audio(uint64_t time, void (*callback)(int16_t l, int16_t r, uint64_t time))
{
	int16_t sound_buffer[4096];
	uint32_t diff = time - sgb.frame_start + sgb.clock_remainder;
	//utils_log("%ul", diff);

	uint32_t samples = diff / refclocks_per_spc_sample;
	uint32_t new_remainder = diff % refclocks_per_spc_sample;

	spc_set_output(sgb.spc, sound_buffer, sizeof(sound_buffer) / sizeof(sound_buffer[0]));
	int matched = 1;
	for (int p = 0; p < 4; p++)
	{
		if (spc_read_port(sgb.spc, 0, p) != sgb.sound_control[p])
			matched = 0;
	}
	if (matched) // recived
	{
		sgb.sound_control[0] = 0;
		sgb.sound_control[1] = 0;
		sgb.sound_control[2] = 0;
	}
	else
	{
		utils_log("SPC: %02x %02x %02x %02x => %02x %02x %02x %02x\n",
				  spc_read_port(sgb.spc, 0, 0),
				  spc_read_port(sgb.spc, 0, 1),
				  spc_read_port(sgb.spc, 0, 2),
				  spc_read_port(sgb.spc, 0, 3),
				  sgb.sound_control[0],
				  sgb.sound_control[1],
				  sgb.sound_control[2],
				  sgb.sound_control[3]);
	}
	for (int p = 0; p < 4; p++)
	{
		spc_write_port(sgb.spc, 0, p, sgb.sound_control[p]);
	}

	spc_end_frame(sgb.spc, samples * 32);

	uint64_t t = sgb.frame_start + refclocks_per_spc_sample - sgb.clock_remainder;
	for (int i = 0; i < samples; i++, t += refclocks_per_spc_sample)
		callback(sound_buffer[i * 2], sound_buffer[i * 2 + 1], t);

	sgb.frame_start = time;
	sgb.clock_remainder = new_remainder;
}
