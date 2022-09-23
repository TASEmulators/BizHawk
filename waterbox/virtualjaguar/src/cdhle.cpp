#include "cdhle.h"
#include "gpu.h"
#include "memory.h"
#include "jaguar.h"
#include "event.h"
#include "m68000/m68kinterface.h"

#include <assert.h>
#include <stdio.h>
#include <string.h>

#define SET_ERR() jaguarMainRAM[0x3E00] = -1
#define NO_ERR() jaguarMainRAM[0x3E00] = 0

#define TOC_BASE_ADDR 0x2C00

static bool cd_setup;
static bool cd_initm;
static bool cd_muted;
static bool cd_paused;
static uint8_t cd_mode;
static uint8_t cd_osamp;

static bool cd_is_reading;
static uint32_t cd_read_addr_start;
static uint32_t cd_read_addr_end;
static int32_t cd_read_lba;
static uint8_t cd_buf2352[2352 * 4]; // also 64 * 147
static uint32_t cd_buf_block_num;

extern void (*cd_toc_callback)(void * dest);
extern void (*cd_read_callback)(int32_t lba, void * dest);
extern bool jaguarCdInserted;

struct Track {
	uint8_t track_num;
	uint8_t start_mins;
	uint8_t start_secs;
	uint8_t start_frames;
	uint8_t session_num;
	uint8_t dur_mins;
	uint8_t dur_secs;
	uint8_t dur_frames;
};

struct TOC {
	uint8_t padding_0;
	uint8_t padding_1;
	uint8_t num_sessions;
	uint8_t min_track_num;
	uint8_t max_track_num;
	uint8_t last_lead_out_mins;
	uint8_t last_lead_out_secs;
	uint8_t last_lead_out_frames;
	Track tracks[127];
};

static_assert(sizeof(TOC) == 1024);

static TOC toc;
static uint32_t cd_boot_addr;
static uint32_t cd_boot_len;
static int32_t cd_boot_lba;
static uint32_t cd_boot_off;

void CDHLEInit(void)
{
	if (cd_toc_callback && cd_read_callback)
	{
		jaguarCdInserted = true;
		cd_toc_callback(&toc);
		assert(toc.num_sessions >= 2); // need at least 2 sessions
		int32_t bootTrackNum = 0;
		for (uint32_t i = 0; i < 127; i++)
		{
			if (toc.tracks[i].session_num == 1)
			{
				bootTrackNum = i;
				break;
			}
		}
		assert(bootTrackNum != 0);
		Track& bootTrack = toc.tracks[bootTrackNum];
		int32_t startLba = bootTrack.start_mins * 4500 + bootTrack.start_secs * 75 + bootTrack.start_frames - 150;
		fprintf(stderr, "timecode: %02d:%02d:%02d, startLba %04X\n", bootTrack.start_mins, bootTrack.start_secs, bootTrack.start_frames, startLba);
		int32_t numLbas = bootTrack.dur_mins * 4500 + bootTrack.dur_secs * 75 + bootTrack.dur_frames;
		uint8_t buf2352[2352];
		bool foundHeader = false;
		for (int32_t i = 0; i < numLbas; i++)
		{
			cd_read_callback(startLba + i, buf2352);
			static const char* atariHeader = "ATARI APPROVED DATA HEADER ATRI\x20";
			
			for (uint32_t j = 0; j < (2352 - 32 - 4 - 4); j += 2)
			{
				if (!memcmp(&buf2352[j], atariHeader, 32))
				{
					fprintf(stderr, "startLba + i %04X\n", startLba + i);
					cd_boot_addr = GET32(buf2352, j + 32);
					cd_boot_len = GET32(buf2352, j + 32 + 4);
					cd_boot_lba = startLba + i;
					cd_boot_off = j + 32 + 4 + 4;
					foundHeader = true;
					break;
				}
			}

			if (foundHeader) break;
		}
		assert(foundHeader);
	}

	CDHLEReset();
}

void CDHLEReset(void)
{
	cd_setup = false;
	cd_initm = false;
	cd_muted = false;
	cd_paused = false;
	cd_mode = 0;
	cd_osamp = 0;

	if (cd_read_callback)
	{
		// copy TOC to RAM
		memcpy(&jaguarMainRAM[TOC_BASE_ADDR], &toc, sizeof(TOC));

		// copy bootcode to RAM
		// maximum of 64KiB is allowed
		uint32_t dstStart = cd_boot_addr;
		uint32_t dstEnd = cd_boot_addr + (cd_boot_len > 0x10000 ? 0x10000 : cd_boot_len);
		int32_t lba = cd_boot_lba;
		uint8_t buf2352[2352];

		cd_read_callback(lba++, buf2352);
		for (uint32_t i = cd_boot_off; i < 2352 && dstStart < dstEnd;)
		{
			uint32_t end = (i + 64) > 2352 ? 2352 : (i + 64);
			for (; i < end; i++, dstStart++)
			{
				JaguarWriteByte(dstStart, buf2352[i], GPU);
			}
		}

		while (dstStart < dstEnd)
		{
			cd_read_callback(lba++, buf2352);
			for (uint32_t i = 0; i < 2352 && dstStart < dstEnd;)
			{
				uint32_t end = (i + 64) > 2352 ? 2352 : (i + 64);
				for (; i < end; i++, dstStart++)
				{
					JaguarWriteByte(dstStart, buf2352[i], GPU);
				}
			}
		}

		cd_read_addr_start = dstStart;

		SET32(jaguarMainRAM, 4, cd_boot_addr);
		SET16(jaguarMainRAM, 0x3004, 0x0403); // BIOS VER
	}
}

void CDHLEDone(void)
{
}

static void CDSendBlock(void)
{
	if (cd_buf_block_num == 0)
	{
		for (uint32_t i = 0; i < 4; i++)
		{
			cd_read_callback(cd_read_lba + i, &cd_buf2352[2352 * i]); 
		}

		cd_read_lba += 4;
	}

	// send one block of data
	for (uint32_t i = 0; i < 64; i++)
	{
		JaguarWriteByte(cd_read_addr_start + i, cd_buf2352[i + 64 * cd_buf_block_num], GPU);
	}

	cd_read_addr_start += 64;
	cd_buf_block_num = (cd_buf_block_num + 1) % 147;

	if (cd_read_addr_start >= cd_read_addr_end)
	{
		cd_is_reading = false;
	}
}

static void CDHLECallback(void)
{
	RemoveCallback(CDHLECallback);
	if (cd_is_reading)
	{
		if (!cd_paused)
		{
			CDSendBlock();
		}
		SetCallbackTime(CDHLECallback, 180 >> (cd_mode & 1));
	}
}

static void CD_init(void);
static void CD_mode(void);
static void CD_ack(void);
static void CD_jeri(void);
static void CD_spin(void);
static void CD_stop(void);
static void CD_mute(void);
static void CD_umute(void);
static void CD_paus(void);
static void CD_upaus(void);
static void CD_read(void);
static void CD_uread(void);
static void CD_setup(void);
static void CD_ptr(void);
static void CD_osamp(void);
static void CD_getoc(void);
static void CD_initm(void);
static void CD_initf(void);
static void CD_switch(void);

static void (* CD_functions[19])() =
{
	CD_init,	CD_mode,	CD_ack,		CD_jeri,
	CD_spin,	CD_stop,	CD_mute,	CD_umute,
	CD_paus,	CD_upaus,	CD_read,	CD_uread,
	CD_setup,	CD_ptr,		CD_osamp,	CD_getoc,
	CD_initm,	CD_initf,	CD_switch,
};

static const char * cd_func_strs[19] = {
	"CD_init",	"CD_mode",	"CD_ack",	"CD_jeri",
	"CD_spin",	"CD_stop",	"CD_mute",	"CD_umute",
	"CD_paus",	"CD_upaus",	"CD_read",	"CD_uread",
	"CD_setup",	"CD_ptr",	"CD_osamp",	"CD_getoc",
	"CD_initm",	"CD_initf",	"CD_switch",
};

void CDHLEHook(uint32_t which)
{
	//fprintf(stderr, "CD HLE Hook %s\n", cd_func_strs[which]);
	CD_functions[which]();
}

static void CD_init(void)
{
	fprintf(stderr, "do CD_init");
	cd_initm = false;
}

static void CD_mode(void)
{
	// bit 0 = speed (0 = single, 1 = double)
	// bit 1 = mode (0 = audio, 1 = data)
	cd_mode = m68k_get_reg(NULL, M68K_REG_D0) & 3;
	fprintf(stderr, "CD_mode mode = %d, speed = %d\n", cd_mode >> 1, cd_mode & 1);
	NO_ERR();
}

static void CD_ack(void)
{
	if (!cd_paused)
	{
		while (cd_is_reading)
		{
			CDSendBlock();
		}
	}

	NO_ERR();
}

static void CD_jeri(void)
{
	fprintf(stderr, "CD_jeri called %d!!!\n", m68k_get_reg(NULL, M68K_REG_D0) & 1);
}

static void CD_spin(void)
{
	NO_ERR();
}

static void CD_stop(void)
{
	NO_ERR();
}

static void CD_mute(void)
{
	if (!(cd_mode & 2))
	{
		cd_muted = true;
		NO_ERR();
	}
	else
	{
		SET_ERR();
	}
}

static void CD_umute(void)
{
	if (!(cd_mode & 2))
	{
		cd_muted = false;
		NO_ERR();
	}
	else
	{
		SET_ERR();
	}
}

static void CD_paus(void)
{
	cd_paused = true;
	NO_ERR();
}

static void CD_upaus(void)
{
	cd_paused = false;
	NO_ERR();
}

static void CD_read(void)
{
	uint32_t dstStart = m68k_get_reg(NULL, M68K_REG_A0);
	uint32_t dstEnd = m68k_get_reg(NULL, M68K_REG_A1);

	fprintf(stderr, "CD READ: dstStart %08X, dstEnd %08X\n", dstStart, dstEnd);

	if (dstEnd <= dstStart)
	{
		fprintf(stderr, "CD READ ERROR: dstEnd < dstStart\n");
		SET_ERR();
		return;
	}

	uint32_t timecode = m68k_get_reg(NULL, M68K_REG_D0);

	uint32_t frames = timecode & 0xFF;
	uint32_t seconds = (timecode >> 8) & 0xFF;
	uint32_t minutes = (timecode >> 16) & 0xFF;

	fprintf(stderr, "CD READ: is seeking %d, mins %02d, secs %02d, frames %02d\n", !!(timecode & 0x80000000), minutes, seconds, frames);

	if (frames >= 75 || seconds >= 60 || minutes >= 73)
	{
		fprintf(stderr, "CD READ ERROR: timecode too large\n");
		SET_ERR();
		return;
	}

	if (!(timecode & 0x80000000))
	{
		cd_is_reading = true;
		cd_read_addr_start = dstStart;
		cd_read_addr_end = dstEnd;
		cd_read_lba = (minutes * 60 + seconds) * 75 + frames - 150;
		cd_buf_block_num = 0;
		RemoveCallback(CDHLECallback);
		SetCallbackTime(CDHLECallback, 180 >> (cd_mode & 1));
	}

	NO_ERR();
}

static void CD_uread(void)
{
	if (cd_is_reading)
	{
		cd_is_reading = false;
		NO_ERR();
	}
	else
	{
		SET_ERR();
	}
}

static void CD_setup(void)
{
	// probaby don't really care about this
	cd_setup = true;
}

static void CD_ptr(void)
{
	m68k_set_reg(M68K_REG_A0, cd_read_addr_start);
	m68k_set_reg(M68K_REG_A1, 0);
}

static void CD_osamp(void)
{
	cd_osamp = m68k_get_reg(NULL, M68K_REG_D0) & 3;
	NO_ERR();
}

static void CD_getoc(void)
{
	// this is for debugging only, retail games will not call this
}

static void CD_initm(void)
{
	cd_initm = true;
}

static void CD_initf(void)
{
	cd_initm = false;
}

static void CD_switch(void)
{
	// not supporting CD switching, so
}
