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
} sgb_t;

static sgb_t sgb;

static uint32_t makecol(uint16_t c)
{
	return c >> 7 & 0xf8 | c >> 12 & 0x07 | c << 6 & 0xf800 | c << 1 & 0x0700 | c << 19 & 0xf80000 | c << 14 & 0x070000 | 0xff000000;
}

static void cmd_pal(int a, int b)
{
	if ((sgb.command[0] & 7) == 1)
	{
		uint32_t c[7];
		for (int i = 0; i < 7; i++)
			c[i] = makecol(sgb.command[1 + i] | sgb.command[2 + i] << 8);
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
	case 0x10: // DATA_TRN
		// TODO: Is it possible for this to write data to
		// memory areas used for the attribute file, etc?
		// If so, do games do this?
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

	utils_log("ZZ: %02x, %llu", val, time);
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
				utils_log("SGB: joypad index to %u", sgb.joypad_index);
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
		utils_log("SGB: SCAN%u", ji);
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
		utils_log("SGB: READ%u %02x", ji, ret ^ 0x0f);
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
