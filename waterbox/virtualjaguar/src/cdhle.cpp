#include "cdhle.h"
#include "gpu.h"
#include "memory.h"
#include "jaguar.h"
#include "jerry.h"
#include "tom.h"
#include "dac.h"
#include "dsp.h"
#include "event.h"
#include "settings.h"
#include "m68000/m68kinterface.h"

#include <assert.h>
#include <stdio.h>
#include <string.h>

#define SET_ERR() SET16(jaguarMainRAM, 0x3E00, -1)
// 0 should be no error, yet some games expect 1?
// todo: investigate
#define NO_ERR() SET16(jaguarMainRAM, 0x3E00, 1)

#define TOC_BASE_ADDR 0x2C00

// arbitrary number, but something is needed here
// too short, and games FMVs misbehave (Space Ace is entirely FMVs!!!)
// too long, and games will time out on CD reads
// NTSC and PAL probably shouldn't have different timings here... 
#define CD_DELAY_USECS (vjs.hardwareTypeNTSC ? 270 : 280)

static bool cd_setup;
static bool cd_initm;
static bool cd_muted;
static bool cd_paused;
static bool cd_jerry;
static uint8_t cd_mode;
static uint8_t cd_osamp;

static bool cd_is_reading;
static uint32_t cd_read_orig_addr_start;
static uint32_t cd_read_addr_start;
static uint32_t cd_read_addr_end;
static int32_t cd_read_lba;
static uint8_t cd_buf2352[2352 + 128];
static uint32_t cd_buf_pos;
static uint32_t cd_buf_rm;
static uint32_t cd_buf_circular_size;

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
	uint8_t min_track_num;
	uint8_t max_track_num;
	uint8_t num_sessions;
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

static bool cd_byte_swapped;
static uint32_t cd_word_alignment;

void CDHLEInit(void)
{
	if (cd_toc_callback && cd_read_callback)
	{
		jaguarCdInserted = true;
		cd_toc_callback(&toc);
		if (toc.num_sessions < 2)
		{
			fprintf(stderr, "%s\n", "need at least 2 sessions!!!");
			return;
		}
		uint32_t bootTrackNum = 0;
		for (uint32_t i = 1; i < 127; i++)
		{
			if (toc.tracks[i].session_num == 1)
			{
				bootTrackNum = i;
				break;
			}
		}
		if (bootTrackNum == 0)
		{
			fprintf(stderr, "%s\n", "could not find boot track!!!");
			return;
		}
		Track& bootTrack = toc.tracks[bootTrackNum];
		int32_t startLba = bootTrack.start_mins * 4500 + bootTrack.start_secs * 75 + bootTrack.start_frames - 150;
		fprintf(stderr, "timecode: %02d:%02d:%02d, startLba %04X\n", bootTrack.start_mins, bootTrack.start_secs, bootTrack.start_frames, startLba);
		int32_t numLbas = bootTrack.dur_mins * 4500 + bootTrack.dur_secs * 75 + bootTrack.dur_frames;
		uint8_t buf2352[2352];
		bool foundHeader = false;
		for (int32_t i = 0; i < numLbas; i++)
		{
			cd_read_callback(startLba + i, buf2352);
			// the ? here represents a wildcard, these are suppose to be ' ' for the first data track, but it appears the CD bios doesn't actually check it?
			static const char* atariHeader = "ATARI APPROVED DATA HEADER ATRI?";
			static const char* byteSwappedHeader = "TARA IPARPVODED TA AEHDAREA RT?I"; // some dumps are byteswapped, detect these and fix them
			
			for (uint32_t j = 0; j < (2352 - 32 - 4 - 4); j++)
			{
				if (!memcmp(&buf2352[j], atariHeader, 32 - 1))
				{
					fprintf(stderr, "startLba + i %04X\n", startLba + i);
					cd_boot_addr = GET32(buf2352, j + 32);
					cd_boot_len = GET32(buf2352, j + 32 + 4);
					cd_boot_lba = startLba + i;
					cd_boot_off = j + 32 + 4 + 4;
					cd_byte_swapped = false;
					cd_word_alignment = -j & 3;
					foundHeader = true;
					break;
				}

				if (!memcmp(&buf2352[j], byteSwappedHeader, 32 - 2))
				{
					fprintf(stderr, "(byteswapped) startLba + i %04X\n", startLba + i);
					cd_boot_addr = *(uint16_t*)&buf2352[j + 32] << 16 | *(uint16_t*)&buf2352[j + 32 + 2];
					cd_boot_len = *(uint16_t*)&buf2352[j + 32 + 4] << 16 | *(uint16_t*)&buf2352[j + 32 + 4 + 2];
					cd_boot_lba = startLba + i;
					cd_boot_off = j + 32 + 4 + 4;
					cd_byte_swapped = true;
					cd_word_alignment = -j & 3;
					foundHeader = true;
					break;
				}
			}

			if (foundHeader) break;
		}
		if (!foundHeader)
		{
			fprintf(stderr, "%s\n", "could not find boot track header!!!");
			return;
		}
	}

	CDHLEReset();
}

void CDHLEReset(void)
{
	cd_setup = false;
	cd_initm = false;
	cd_muted = false;
	cd_paused = false;
	cd_jerry = false;
	cd_mode = 0;
	cd_osamp = 0;

	cd_is_reading = false;
	cd_read_orig_addr_start = 0;
	cd_read_addr_start = 0;
	cd_read_addr_end = 0;
	cd_read_lba = 0;
	memset(cd_buf2352, 0, sizeof(cd_buf2352));
	cd_buf_pos = 0;
	cd_buf_rm = 0;
	cd_buf_circular_size = 0;

	if (cd_read_callback)
	{
		// copy TOC to RAM
		memcpy(&jaguarMainRAM[TOC_BASE_ADDR], &toc, sizeof(TOC));

		// copy bootcode to RAM
		// supposedly a maximum of 64KiB is allowed
		// but some games will expect past 64KiB to be copied?
		uint32_t dstStart = cd_boot_addr;
		uint32_t dstEnd = cd_boot_addr + cd_boot_len;
		fprintf(stderr, "boot track dstStart %04X dstEnd %04X\n", dstStart, dstEnd);
		int32_t lba = cd_boot_lba;
		uint8_t buf2352[2352];

		cd_read_callback(lba++, buf2352);

		if (cd_byte_swapped)
		{
			uint16_t* cd16buf = (uint16_t*)buf2352;
			for (uint32_t i = 0; i < (2352 / 2); i++)
			{
				cd16buf[i] = __builtin_bswap16(cd16buf[i]);
			}
		}

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

			if (cd_byte_swapped)
			{
				uint16_t* cd16buf = (uint16_t*)buf2352;
				for (uint32_t i = 0; i < (2352 / 2); i++)
				{
					cd16buf[i] = __builtin_bswap16(cd16buf[i]);
				}
			}

			for (uint32_t i = 0; i < 2352 && dstStart < dstEnd;)
			{
				uint32_t end = (i + 64) > 2352 ? 2352 : (i + 64);
				for (; i < end; i++, dstStart++)
				{
					JaguarWriteByte(dstStart, buf2352[i], GPU);
				}
			}
		}

		//eh not sure why I did this?
		//cd_read_addr_start = dstStart;

		SET32(jaguarMainRAM, 4, cd_boot_addr);
		SET16(jaguarMainRAM, 0x3004, 0x0403); // BIOS VER
		DACWriteByte(0xF1A153, 9); // set SCLK to 9
		if (jaguarMainROMCRC32 == 0xFDF37F47)
		{
			TOMWriteWord(0xF00000, (TOMGetMEMCON1() & ~6u) | 4); // set ROM width to 32 bit
			memcpy(&jaguarMainRAM[0x2400], &jaguarMainROM[0x6D60], 0x790); // copy memtrack "bios" to ram
		}
	}
}

static void RefillCDBuf()
{
	memmove(&cd_buf2352[0], &cd_buf2352[cd_buf_pos], cd_buf_rm);
	cd_read_callback(cd_read_lba++, &cd_buf2352[cd_buf_rm]);

	// hack to force word alignment
	if (cd_word_alignment)
	{
		uint8_t temp2352[2352];
		cd_read_callback(cd_read_lba, temp2352);
		memmove(&cd_buf2352[cd_buf_rm], &cd_buf2352[cd_buf_rm + cd_word_alignment], 2352 - cd_word_alignment);
		memcpy(&cd_buf2352[cd_buf_rm + 2352 - cd_word_alignment], &temp2352[0], cd_word_alignment);
	}

	if (cd_byte_swapped)
	{
		uint16_t* cd16buf = (uint16_t*)&cd_buf2352[cd_buf_rm];
		for (uint32_t i = 0; i < (2352 / 2); i++)
		{
			cd16buf[i] = __builtin_bswap16(cd16buf[i]);
		}
	}

	cd_buf_pos = 0;
	cd_buf_rm += 2352;
}

static void CDHLECallback(void)
{
	if (cd_is_reading)
	{
		if (!GPUIsRunning())
			fprintf(stderr, "CDHLECallback called with GPU inactive\n");

		if (GPUIsRunning() && !cd_paused)
		{
			if (cd_buf_rm < 64)
			{
				RefillCDBuf();
			}

			// send one block of data, one long at a time
			for (uint32_t i = 0; i < 64; i += 4)
			{
				GPUWriteLong(cd_read_addr_start + i, GET32(cd_buf2352, cd_buf_pos + i), GPU);
			}

			cd_read_addr_start += 64;
			cd_buf_pos += 64;
			cd_buf_rm -= 64;

			if (cd_read_addr_start > cd_read_addr_end)
			{
				cd_is_reading = false;
			}
			else if (cd_buf_circular_size && (cd_read_addr_start - cd_read_orig_addr_start) >= cd_buf_circular_size)
			{
				cd_read_addr_start = cd_read_orig_addr_start;
			}

			//GPUSetIRQLine(GPUIRQ_DSP, ASSERT_LINE);
		}

		SetCallbackTime(CDHLECallback, CD_DELAY_USECS >> (cd_mode & 1));
	}
}

// called from JERRYI2SCallback
bool CDHLEJerryCallback(void)
{
	if (!cd_is_reading || !cd_jerry || cd_paused)
	{
		return false;
	}

	if (cd_buf_rm < 4)
	{
		RefillCDBuf();
	}

	DACWriteWord(0xF1A14A, GET16(cd_buf2352, cd_buf_pos + 0));
	DACWriteWord(0xF1A14E, GET16(cd_buf2352, cd_buf_pos + 2));

	//cd_read_addr_start += 4;
	cd_buf_pos += 4;
	cd_buf_rm -= 4;

	// don't think this is right
	/*if (cd_read_addr_start > cd_read_addr_end && !((cd_read_addr_start - cd_read_orig_addr_start) & 0x3F))
	{
		cd_is_reading = false;
	}*/

	return true;
}

static void ResetCallback(void)
{
	RemoveCallback(CDHLECallback);
	if (!cd_jerry)
	{
		SetCallbackTime(CDHLECallback, CD_DELAY_USECS >> (cd_mode & 1));
	}
}

static void LoadISRStub(void)
{
	uint32_t isrAddr = m68k_get_reg(M68K_REG_A0);
	uint32_t addr = 0xF03010;

	#define WRITE_GASM(x) do { GPUWriteWord(addr, x, M68K); addr += 2; } while (0)

	WRITE_GASM(0x981E); WRITE_GASM(isrAddr & 0xFFFF); WRITE_GASM(isrAddr >> 16); // movei ISR_ADDR, r30
	WRITE_GASM(0xD3C0); // jump (r30)
	WRITE_GASM(0xE400); // nop

	addr = isrAddr;

	WRITE_GASM(0x981E); WRITE_GASM(0x2100); WRITE_GASM(0x00F0); // movei $F02100, r30
	WRITE_GASM(0xA7DD); // load (r30), r29
	WRITE_GASM(0x3C7D); // bclr 3, r29
	WRITE_GASM(0x395D); // bset 10, r29
	WRITE_GASM(0xA7FC); // load (r31), r28
	WRITE_GASM(0x085C); // addq 2, r28
	WRITE_GASM(0x089F); // addq 4, r31
	WRITE_GASM(0xD380); // jump (r28)
	WRITE_GASM(0xBFDD); // store r29, (r30)

	#undef WRITE_GASM
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
	fprintf(stderr, "CD_init called %08X\n", m68k_get_reg(M68K_REG_A0));
	LoadISRStub();
	cd_initm = false;
}

static void CD_mode(void)
{
	// bit 0 = speed (0 = single, 1 = double)
	// bit 1 = mode (0 = audio, 1 = data)
	cd_mode = m68k_get_reg(M68K_REG_D0) & 3;
	fprintf(stderr, "CD_mode mode = %d, speed = %d\n", cd_mode >> 1, cd_mode & 1);
	NO_ERR();
}

static void CD_ack(void)
{
	NO_ERR();
}

static void CD_jeri(void)
{
	bool njerry = m68k_get_reg(M68K_REG_D0) & 1;
	if (cd_jerry ^ njerry)
	{
		fprintf(stderr, "changing jerry mode %d -> %d\n", cd_jerry, njerry);
		cd_jerry = njerry;
		ResetCallback();
	}
}

static void CD_spin(void)
{
	fprintf(stderr, "CD_spin: new session %04X\n", m68k_get_reg(M68K_REG_D1) & 0xFFFF);
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
	uint32_t dstStart = m68k_get_reg(M68K_REG_A0);
	uint32_t dstEnd = m68k_get_reg(M68K_REG_A1);

	fprintf(stderr, "CD READ: dstStart %08X, dstEnd %08X\n", dstStart, dstEnd);

	if (dstEnd <= dstStart)
	{
		fprintf(stderr, "CD READ ERROR: dstEnd < dstStart\n");
		SET_ERR();
		return;
	}

	uint32_t timecode = m68k_get_reg(M68K_REG_D0);

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
		if (cd_initm)
		{
			uint32_t marker = m68k_get_reg(M68K_REG_D1);
			uint32_t circBufSz = m68k_get_reg(M68K_REG_D2);
			fprintf(stderr, "cd_initm read: marker %04X, circBufSz %04X\n", marker, circBufSz);
			uint32_t lba = (minutes * 60 + seconds) * 75 + frames - 150;
			uint8_t buf2352[2352 + 128];
			uint32_t bufPos = 0;
			uint32_t bufRm = 0;
			while (true)
			{
				if (bufRm < 64)
				{
					memmove(&buf2352[0], &buf2352[bufPos], bufRm);
					cd_read_callback(lba++, &buf2352[bufRm]);

					if (cd_word_alignment)
					{
						uint8_t temp2352[2352];
						cd_read_callback(lba, temp2352);
						memmove(&buf2352[bufRm], &buf2352[bufRm + cd_word_alignment], 2352 - cd_word_alignment);
						memcpy(&buf2352[bufRm + 2352 - cd_word_alignment], &temp2352[0], cd_word_alignment);
					}

					if (cd_byte_swapped)
					{
						uint16_t* cd16buf = (uint16_t*)&buf2352[bufRm];
						for (uint32_t i = 0; i < (2352 / 2); i++)
						{
							cd16buf[i] = __builtin_bswap16(cd16buf[i]);
						}
					}

					bufPos = 0;
					bufRm += 2352;
				}

				if (GET32(buf2352, bufPos) == marker)
				{
					bool foundMarker = true;
					for (uint32_t i = 4; i < 64; i += 4)
					{
						foundMarker &= GET32(buf2352, bufPos + i) == marker;
					}

					if (foundMarker)
					{
						bufPos += 64;
						bufRm -= 64;
						memcpy(&cd_buf2352[0], &buf2352[bufPos], bufRm);
						cd_is_reading = true;
						cd_read_orig_addr_start = dstStart;
						cd_read_addr_start = dstStart;
						cd_read_addr_end = dstEnd;
						cd_read_lba = lba;
						cd_buf_pos = 0;
						cd_buf_rm = bufRm;
						cd_buf_circular_size = circBufSz ? (1 << circBufSz) : 0;
						ResetCallback();
						JERRYWriteWord(0xF10020, 0, M68K);
						//GPUWriteLong(0xF02100, GPUReadLong(0xF02100, M68K) | 0x20, M68K);
						break;
					}
				}

				bufPos += 4;
				bufRm -= 4;
			}
		}
		else
		{
			cd_is_reading = true;
			cd_read_orig_addr_start = dstStart;
			cd_read_addr_start = dstStart;
			cd_read_addr_end = dstEnd;
			cd_read_lba = (minutes * 60 + seconds) * 75 + frames - 150;
			cd_buf_pos = 0;
			cd_buf_rm = 0;
			cd_buf_circular_size = 0;
			ResetCallback();
			JERRYWriteWord(0xF10020, 0, M68K);
			//GPUWriteLong(0xF02100, GPUReadLong(0xF02100, M68K) | 0x20, M68K);
		}
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
	// but to be safe, let's just reset everything
	cd_initm = false;
	cd_muted = false;
	cd_paused = false;
	cd_jerry = false;
	cd_mode = 0;
	cd_osamp = 0;

	cd_is_reading = false;
	cd_read_orig_addr_start = 0;
	cd_read_addr_start = 0;
	cd_read_addr_end = 0;
	cd_read_lba = 0;
	memset(cd_buf2352, 0, sizeof(cd_buf2352));
	cd_buf_pos = 0;
	cd_buf_rm = 0;
	cd_buf_circular_size = 0;

	cd_setup = true;
}

static void CD_ptr(void)
{
	m68k_set_reg(M68K_REG_A0, cd_read_addr_start);
	m68k_set_reg(M68K_REG_A1, 0);
}

static void CD_osamp(void)
{
	cd_osamp = m68k_get_reg(M68K_REG_D0) & 3;
	NO_ERR();
}

static void CD_getoc(void)
{
	// this is for debugging only, retail games will not call this
	// although some homebrew games seem to call it?
	uint32_t addr = m68k_get_reg(M68K_REG_A0);
	for (uint32_t i = 0; i < sizeof(TOC); i++)
	{
		JaguarWriteByte(addr + i, ((uint8_t*)&toc)[i], M68K);
	}
}

static void CD_initm(void)
{
	fprintf(stderr, "CD_initm called %08X\n", m68k_get_reg(M68K_REG_A0));
	LoadISRStub();
	cd_initm = true;
}

static void CD_initf(void)
{
	fprintf(stderr, "CD_initf called %08X\n", m68k_get_reg(M68K_REG_A0));
	LoadISRStub();
	cd_initm = false;
}

static void CD_switch(void)
{
	// not supporting CD switching, so
}
