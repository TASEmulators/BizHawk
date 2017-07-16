/******************************************************************************/
/* Mednafen NEC PC-FX Emulation Module                                        */
/******************************************************************************/
/* pcfx.cpp:
**  Copyright (C) 2006-2017 Mednafen Team
**
** This program is free software; you can redistribute it and/or
** modify it under the terms of the GNU General Public License
** as published by the Free Software Foundation; either version 2
** of the License, or (at your option) any later version.
**
** This program is distributed in the hope that it will be useful,
** but WITHOUT ANY WARRANTY; without even the implied warranty of
** MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
** GNU General Public License for more details.
**
** You should have received a copy of the GNU General Public License
** along with this program; if not, write to the Free Software Foundation, Inc.,
** 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
*/

#include "pcfx.h"
#include "soundbox.h"
#include "input.h"
#include "king.h"
#include "timer.h"
#include "interrupt.h"
#include "rainbow.h"
#include "huc6273.h"
#include "fxscsi.h"
#include "cdrom/cdromif.h"
#include "cdrom/scsicd.h"
//#include <mednafen/mempatcher.h>

#include <errno.h>
#include <string.h>
#include <math.h>

#include "../emulibc/emulibc.h"
#include "../emulibc/waterboxcore.h"

namespace MDFN_IEN_PCFX
{

/* FIXME:  soundbox, vce, vdc, rainbow, and king store wait states should be 4, not 2, but V810 has write buffers which can mask wait state penalties.
  This is a hack to somewhat address the issue, but to really fix it, we need to handle write buffer emulation in the V810 emulation core itself.
*/
static std::vector<CDIF *> *cdifs = NULL;

V810 PCFX_V810;

static uint8 *BIOSROM = NULL;   // 1MB
static uint8 *RAM = NULL;		// 2MB
static uint8 *FXSCSIROM = NULL; // 512KiB

static uint32 RAM_LPA; // Last page access

static const int RAM_PageSize = 2048;
static const int RAM_PageNOTMask = ~(RAM_PageSize - 1);

static uint16 Last_VDC_AR[2];

static bool WantHuC6273 = FALSE;

//static
VDC *fx_vdc_chips[2];

static uint16 BackupControl;
static uint8 BackupRAM[0x8000], ExBackupRAM[0x8000];
static uint8 ExBusReset; // I/O Register at 0x0700

static bool Lagged;
static void (*InputCallback)();

// Checks to see if this main-RAM-area access
// is in the same DRAM page as the last access.
#define RAMLPCHECK                            \
	{                                         \
		if ((A & RAM_PageNOTMask) != RAM_LPA) \
		{                                     \
			timestamp += 3;                   \
			RAM_LPA = A & RAM_PageNOTMask;    \
		}                                     \
	}

static v810_timestamp_t next_pad_ts, next_timer_ts, next_adpcm_ts, next_king_ts;

void PCFX_FixNonEvents(void)
{
	if (next_pad_ts & 0x40000000)
		next_pad_ts = PCFX_EVENT_NONONO;

	if (next_timer_ts & 0x40000000)
		next_timer_ts = PCFX_EVENT_NONONO;

	if (next_adpcm_ts & 0x40000000)
		next_adpcm_ts = PCFX_EVENT_NONONO;

	if (next_king_ts & 0x40000000)
		next_king_ts = PCFX_EVENT_NONONO;
}

void PCFX_Event_Reset(void)
{
	next_pad_ts = PCFX_EVENT_NONONO;
	next_timer_ts = PCFX_EVENT_NONONO;
	next_adpcm_ts = PCFX_EVENT_NONONO;
	next_king_ts = PCFX_EVENT_NONONO;
}

static INLINE uint32 CalcNextTS(void)
{
	v810_timestamp_t next_timestamp = next_king_ts;

	if (next_timestamp > next_pad_ts)
		next_timestamp = next_pad_ts;

	if (next_timestamp > next_timer_ts)
		next_timestamp = next_timer_ts;

	if (next_timestamp > next_adpcm_ts)
		next_timestamp = next_adpcm_ts;

	return (next_timestamp);
}

static void RebaseTS(const v810_timestamp_t timestamp, const v810_timestamp_t new_base_timestamp)
{
	assert(next_pad_ts > timestamp);
	assert(next_timer_ts > timestamp);
	assert(next_adpcm_ts > timestamp);
	assert(next_king_ts > timestamp);

	next_pad_ts -= (timestamp - new_base_timestamp);
	next_timer_ts -= (timestamp - new_base_timestamp);
	next_adpcm_ts -= (timestamp - new_base_timestamp);
	next_king_ts -= (timestamp - new_base_timestamp);

	//printf("RTS: %d %d %d %d\n", next_pad_ts, next_timer_ts, next_adpcm_ts, next_king_ts);
}

void PCFX_SetEvent(const int type, const v810_timestamp_t next_timestamp)
{
	//assert(next_timestamp > PCFX_V810.v810_timestamp);

	if (type == PCFX_EVENT_PAD)
		next_pad_ts = next_timestamp;
	else if (type == PCFX_EVENT_TIMER)
		next_timer_ts = next_timestamp;
	else if (type == PCFX_EVENT_ADPCM)
		next_adpcm_ts = next_timestamp;
	else if (type == PCFX_EVENT_KING)
		next_king_ts = next_timestamp;

	if (next_timestamp < PCFX_V810.GetEventNT())
		PCFX_V810.SetEventNT(next_timestamp);
}

int32 MDFN_FASTCALL pcfx_event_handler(const v810_timestamp_t timestamp)
{
	if (timestamp >= next_king_ts)
		next_king_ts = KING_Update(timestamp);

	if (timestamp >= next_pad_ts)
		next_pad_ts = FXINPUT_Update(timestamp);

	if (timestamp >= next_timer_ts)
		next_timer_ts = FXTIMER_Update(timestamp);

	if (timestamp >= next_adpcm_ts)
		next_adpcm_ts = SoundBox_ADPCMUpdate(timestamp);

#if 1
	assert(next_king_ts > timestamp);
	assert(next_pad_ts > timestamp);
	assert(next_timer_ts > timestamp);
	assert(next_adpcm_ts > timestamp);
#endif
	return (CalcNextTS());
}

static void ForceEventUpdates(const uint32 timestamp)
{
	next_king_ts = KING_Update(timestamp);
	next_pad_ts = FXINPUT_Update(timestamp);
	next_timer_ts = FXTIMER_Update(timestamp);
	next_adpcm_ts = SoundBox_ADPCMUpdate(timestamp);

	//printf("Meow: %d\n", CalcNextTS());
	PCFX_V810.SetEventNT(CalcNextTS());

	//printf("FEU: %d %d %d %d\n", next_pad_ts, next_timer_ts, next_adpcm_ts, next_king_ts);
}

#include "io-handler.inc"
#include "mem-handler.inc"

typedef struct
{
	int8 tracknum;
	int8 format;
	uint32 lba;
} CDGameEntryTrack;

typedef struct
{
	const char *name;
	const char *name_original;		 // Original non-Romanized text.
	const uint32 flags;				 // Emulation flags.
	const unsigned int discs;		 // Number of discs for this game.
	CDGameEntryTrack tracks[2][100]; // 99 tracks and 1 leadout track
} CDGameEntry;

#define CDGE_FORMAT_AUDIO 0
#define CDGE_FORMAT_DATA 1

#define CDGE_FLAG_ACCURATE_V810 0x01
#define CDGE_FLAG_FXGA 0x02

static uint32 EmuFlags;

static const CDGameEntry GameList[] =
	{
#include "gamedb.inc"
};

static void Emulate(EmulateSpecStruct *espec)
{
	FXINPUT_Frame();

	KING_StartFrame(fx_vdc_chips, espec); //espec->surface, &espec->DisplayRect, espec->LineWidths, espec->skip);

	v810_timestamp_t v810_timestamp;
	v810_timestamp = PCFX_V810.Run(pcfx_event_handler);

	PCFX_FixNonEvents();

	// Call before resetting v810_timestamp
	ForceEventUpdates(v810_timestamp);

	//
	// Call KING_EndFrame() before SoundBox_Flush(), otherwise CD-DA audio distortion will occur due to sound data being updated
	// after it was needed instead of before.
	//
	KING_EndFrame(v810_timestamp);

	//
	// new_base_ts is guaranteed to be <= v810_timestamp
	//
	v810_timestamp_t new_base_ts;
	espec->SoundBufSize = SoundBox_Flush(v810_timestamp, &new_base_ts, espec->SoundBuf, espec->SoundBufMaxSize, false);

	KING_ResetTS(new_base_ts);
	FXTIMER_ResetTS(new_base_ts);
	FXINPUT_ResetTS(new_base_ts);
	SoundBox_ResetTS(new_base_ts);

	// Call this AFTER all the EndFrame/Flush/ResetTS stuff
	RebaseTS(v810_timestamp, new_base_ts);

	espec->MasterCycles = v810_timestamp - new_base_ts;

	PCFX_V810.ResetTS(new_base_ts);
}

static void PCFX_Reset(void)
{
	const uint32 timestamp = PCFX_V810.v810_timestamp;

	//printf("Reset: %d\n", timestamp);

	// Make sure all devices are synched to current timestamp before calling their Reset()/Power()(though devices should already do this sort of thing on their
	// own, but it's not implemented for all of them yet, and even if it was all implemented this is also INSURANCE).
	ForceEventUpdates(timestamp);

	PCFX_Event_Reset();

	RAM_LPA = 0;

	ExBusReset = 0;
	BackupControl = 0;

	Last_VDC_AR[0] = 0;
	Last_VDC_AR[1] = 0;

	memset(RAM, 0x00, 2048 * 1024);

	for (int i = 0; i < 2; i++)
	{
		int32 dummy_ne MDFN_NOWARN_UNUSED;

		dummy_ne = fx_vdc_chips[i]->Reset();
	}

	KING_Reset(timestamp); // SCSICD_Power() is called from KING_Reset()
	SoundBox_Reset(timestamp);
	RAINBOW_Reset();

	if (WantHuC6273)
		HuC6273_Reset();

	PCFXIRQ_Reset();
	FXTIMER_Reset();
	PCFX_V810.Reset();

	// Force device updates so we can get new next event timestamp values.
	ForceEventUpdates(timestamp);
}

static void PCFX_Power(void)
{
	PCFX_Reset();
}

static void VDCA_IRQHook(bool asserted)
{
	PCFXIRQ_Assert(PCFXIRQ_SOURCE_VDCA, asserted);
}

static void VDCB_IRQHook(bool asserted)
{
	PCFXIRQ_Assert(PCFXIRQ_SOURCE_VDCB, asserted);
}

static MDFN_COLD void LoadCommon(std::vector<CDIF *> *CDInterfaces, const uint8_t *bios)
{
	V810_Emu_Mode cpu_mode;

	cpu_mode = (V810_Emu_Mode)Setting_CpuEmulation;
	if (cpu_mode == _V810_EMU_MODE_COUNT)
	{
		cpu_mode = (EmuFlags & CDGE_FLAG_ACCURATE_V810) ? V810_EMU_MODE_ACCURATE : V810_EMU_MODE_FAST;
	}

	if (EmuFlags & CDGE_FLAG_FXGA)
	{
		//WantHuC6273 = TRUE;
	}

	MDFN_printf(_("V810 Emulation Mode: %s\n"), (cpu_mode == V810_EMU_MODE_ACCURATE) ? _("Accurate") : _("Fast"));
	PCFX_V810.Init(cpu_mode, false);

	uint32 RAM_Map_Addresses[1] = {0x00000000};
	uint32 BIOSROM_Map_Addresses[1] = {0xFFF00000};

	RAM = PCFX_V810.SetFastMap(RAM_Map_Addresses, 0x00200000, 1, _("RAM"), true);
	BIOSROM = PCFX_V810.SetFastMap(BIOSROM_Map_Addresses, 0x00100000, 1, _("BIOS ROM"), false);

	memcpy(BIOSROM, bios, 1024 * 1024);

	/*{
		std::string fxscsi_path = MDFN_GetSettingS("pcfx.fxscsi"); // For developers only, so don't make it convenient.

		if (fxscsi_path != "0" && fxscsi_path != "" && fxscsi_path != "none")
		{
			FileStream FXSCSIFile(fxscsi_path, FileStream::MODE_READ);
			uint32 FXSCSI_Map_Addresses[1] = {0x80780000};

			FXSCSIROM = PCFX_V810.SetFastMap(FXSCSI_Map_Addresses, 0x0080000, 1, _("FX-SCSI ROM"), false);

			FXSCSIFile.read(FXSCSIROM, 1024 * 512);
		}
	}*/

	for (int i = 0; i < 2; i++)
	{
		fx_vdc_chips[i] = new VDC();
		fx_vdc_chips[i]->SetUnlimitedSprites(Setting_NoSpriteLimit);
		fx_vdc_chips[i]->SetVRAMSize(65536);
		fx_vdc_chips[i]->SetWSHook(NULL);
		fx_vdc_chips[i]->SetIRQHook(i ? VDCB_IRQHook : VDCA_IRQHook);

		//fx_vdc_chips[0] = FXVDC_Init(PCFXIRQ_SOURCE_VDCA, Setting_NoSpriteLimit);
		//fx_vdc_chips[1] = FXVDC_Init(PCFXIRQ_SOURCE_VDCB, Setting_NoSpriteLimit);
	}

	SoundBox_Init(Setting_AdpcmBuggy, Setting_AdpcmNoClicks);
	RAINBOW_Init(Setting_ChromaInterpolate);
	FXINPUT_Init();
	FXTIMER_Init();

	if (WantHuC6273)
		HuC6273_Init();

	KING_Init();

	SCSICD_SetDisc(true, NULL, true);

#ifdef WANT_DEBUGGER
	for (unsigned disc = 0; disc < CDInterfaces->size(); disc++)
	{
		CDUtility::TOC toc;

		(*CDInterfaces)[disc]->ReadTOC(&toc);

		for (int32 track = toc.first_track; track <= toc.last_track; track++)
		{
			if (toc.tracks[track].control & 0x4)
			{
				char tmpn[256], tmpln[256];
				uint32 sectors;

				trio_snprintf(tmpn, 256, "track%d-%d-%d", disc, track, toc.tracks[track].lba);
				trio_snprintf(tmpln, 256, "CD - Disc %d/%d - Track %d/%d", disc + 1, (int)CDInterfaces->size(), track, toc.last_track - toc.first_track + 1);

				sectors = toc.tracks[(track == toc.last_track) ? 100 : track + 1].lba - toc.tracks[track].lba;
				ASpace_Add(PCFXDBG_GetAddressSpaceBytes, PCFXDBG_PutAddressSpaceBytes, tmpn, tmpln, 0, sectors * 2048);
			}
		}
	}
#endif

	// MDFNGameInfo->fps = (uint32)((double)7159090.90909090 / 455 / 263 * 65536 * 256);

	//BackupSignalDirty = false;
	//BackupSaveDelay = 0;

	// Initialize backup RAM
	memset(BackupRAM, 0, sizeof(BackupRAM));
	memset(ExBackupRAM, 0, sizeof(ExBackupRAM));

	static const uint8 BRInit00[] = {0x24, 0x8A, 0xDF, 0x50, 0x43, 0x46, 0x58, 0x53, 0x72, 0x61, 0x6D, 0x80,
									 0x00, 0x01, 0x01, 0x00, 0x01, 0x40, 0x00, 0x00, 0x01, 0xF9, 0x03, 0x00,
									 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00};
	static const uint8 BRInit80[] = {0xF9, 0xFF, 0xFF};

	memcpy(BackupRAM + 0x00, BRInit00, sizeof(BRInit00));
	memcpy(BackupRAM + 0x80, BRInit80, sizeof(BRInit80));

	static const uint8 ExBRInit00[] = {0x24, 0x8A, 0xDF, 0x50, 0x43, 0x46, 0x58, 0x43, 0x61, 0x72, 0x64, 0x80,
									   0x00, 0x01, 0x01, 0x00, 0x01, 0x40, 0x00, 0x00, 0x01, 0xF9, 0x03, 0x00,
									   0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00};
	static const uint8 ExBRInit80[] = {0xF9, 0xFF, 0xFF};

	memcpy(ExBackupRAM + 0x00, ExBRInit00, sizeof(ExBRInit00));
	memcpy(ExBackupRAM + 0x80, ExBRInit80, sizeof(ExBRInit80));

	// Default to 16-bit bus.
	for (int i = 0; i < 256; i++)
	{
		PCFX_V810.SetMemReadBus32(i, FALSE);
		PCFX_V810.SetMemWriteBus32(i, FALSE);
	}

	// 16MiB RAM area.
	PCFX_V810.SetMemReadBus32(0, TRUE);
	PCFX_V810.SetMemWriteBus32(0, TRUE);

	// Bitstring read range
	for (int i = 0xA0; i <= 0xAF; i++)
	{
		PCFX_V810.SetMemReadBus32(i, FALSE); // Reads to the read range are 16-bit, and
		PCFX_V810.SetMemWriteBus32(i, TRUE); // writes are 32-bit.
	}

	// Bitstring write range
	for (int i = 0xB0; i <= 0xBF; i++)
	{
		PCFX_V810.SetMemReadBus32(i, TRUE);   // Reads to the write range are 32-bit,
		PCFX_V810.SetMemWriteBus32(i, FALSE); // but writes are 16-bit!
	}

	// BIOS area
	for (int i = 0xF0; i <= 0xFF; i++)
	{
		PCFX_V810.SetMemReadBus32(i, FALSE);
		PCFX_V810.SetMemWriteBus32(i, FALSE);
	}

	PCFX_V810.SetMemReadHandlers(mem_rbyte, mem_rhword, mem_rword);
	PCFX_V810.SetMemWriteHandlers(mem_wbyte, mem_whword, mem_wword);

	PCFX_V810.SetIOReadHandlers(port_rbyte, port_rhword, NULL);
	PCFX_V810.SetIOWriteHandlers(port_wbyte, port_whword, NULL);
}

static void DoMD5CDVoodoo(std::vector<CDIF *> *CDInterfaces)
{
	const CDGameEntry *found_entry = NULL;
	CDUtility::TOC toc;

#if 0
 puts("{");
 puts(" ,");
 puts(" ,");
 puts(" 0,");
 puts(" 1,");
 puts(" {");
 puts("  {");

 for(int i = CDIF_GetFirstTrack(); i <= CDIF_GetLastTrack(); i++)
 {
  CDIF_Track_Format tf;

  CDIF_GetTrackFormat(i, tf);
  
  printf("   { %d, %s, %d },\n", i, (tf == CDIF_FORMAT_AUDIO) ? "CDIF_FORMAT_AUDIO" : "CDIF_FORMAT_MODE1", CDIF_GetTrackStartPositionLBA(i));
 }
 printf("   { -1, (CDIF_Track_Format)-1, %d },\n", CDIF_GetSectorCountLBA());
 puts("  }");
 puts(" }");
 puts("},");
 //exit(1);
#endif

	for (unsigned if_disc = 0; if_disc < CDInterfaces->size(); if_disc++)
	{
		(*CDInterfaces)[if_disc]->ReadTOC(&toc);

		if (toc.first_track == 1)
		{
			for (unsigned int g = 0; g < sizeof(GameList) / sizeof(CDGameEntry); g++)
			{
				const CDGameEntry *entry = &GameList[g];

				assert(entry->discs == 1 || entry->discs == 2);

				for (unsigned int disc = 0; disc < entry->discs; disc++)
				{
					const CDGameEntryTrack *et = entry->tracks[disc];
					bool GameFound = TRUE;

					while (et->tracknum != -1 && GameFound)
					{
						assert(et->tracknum > 0 && et->tracknum < 100);

						if (toc.tracks[et->tracknum].lba != et->lba)
							GameFound = FALSE;

						if (((et->format == CDGE_FORMAT_DATA) ? 0x4 : 0x0) != (toc.tracks[et->tracknum].control & 0x4))
							GameFound = FALSE;

						et++;
					}

					if (et->tracknum == -1)
					{
						if ((et - 1)->tracknum != toc.last_track)
							GameFound = FALSE;

						if (et->lba != toc.tracks[100].lba)
							GameFound = FALSE;
					}

					if (GameFound)
					{
						found_entry = entry;
						goto FoundIt;
					}
				} // End disc count loop
			}
		}

	FoundIt:;

		if (found_entry)
		{
			EmuFlags = found_entry->flags;

			printf("%s\n", found_entry->name);
			printf("%s\n", found_entry->name_original);
			break;
		}
	} // end: for(unsigned if_disc = 0; if_disc < CDInterfaces->size(); if_disc++)
}

// PC-FX BIOS will look at all data tracks(not just the first one), in contrast to the PCE CD BIOS, which only looks
// at the first data track.
static bool TestMagicCD(std::vector<CDIF *> *CDInterfaces)
{
	CDIF *cdiface = (*CDInterfaces)[0];
	CDUtility::TOC toc;
	uint8 sector_buffer[2048];

	memset(sector_buffer, 0, sizeof(sector_buffer));

	cdiface->ReadTOC(&toc);

	for (int32 track = toc.first_track; track <= toc.last_track; track++)
	{
		if (toc.tracks[track].control & 0x4)
		{
			cdiface->ReadSector(sector_buffer, toc.tracks[track].lba, 1);
			if (!strncmp("PC-FX:Hu_CD-ROM", (char *)sector_buffer, strlen("PC-FX:Hu_CD-ROM")))
			{
				return (TRUE);
			}

			if (!strncmp((char *)sector_buffer + 64, "PPPPHHHHOOOOTTTTOOOO____CCCCDDDD", 32))
				return (true);
		}
	}
	return (FALSE);
}

static MDFN_COLD void LoadCD(std::vector<CDIF *> *CDInterfaces, const uint8_t *bios)
{
	EmuFlags = 0;

	cdifs = CDInterfaces;

	DoMD5CDVoodoo(CDInterfaces);

	LoadCommon(CDInterfaces, bios);

	MDFN_printf(_("Emulated CD-ROM drive speed: %ux\n"), (unsigned int)Setting_CdSpeed);

	PCFX_Power();
}
}

using namespace MDFN_IEN_PCFX;

#define EXPORT extern "C" ECL_EXPORT

struct FrontendTOC
{
	int32 FirstTrack;
	int32 LastTrack;
	int32 DiskType;
	struct
	{
		int32 Adr;
		int32 Control;
		int32 Lba;
		int32 Valid;
	} Tracks[101];
};

static void (*ReadTOCCallback)(int disk, FrontendTOC *dest);
static void (*ReadSector2448Callback)(int disk, int lba, uint8 *dest);

EXPORT void SetCDCallbacks(void (*toccallback)(int disk, FrontendTOC *dest), void (*sectorcallback)(int disk, int lba, uint8 *dest))
{
	ReadTOCCallback = toccallback;
	ReadSector2448Callback = sectorcallback;
}

class MyCDIF : public CDIF
{
  private:
	int disk;

  public:
	MyCDIF(int disk) : disk(disk)
	{
		FrontendTOC t;
		ReadTOCCallback(disk, &t);
		disc_toc.first_track = t.FirstTrack;
		disc_toc.last_track = t.LastTrack;
		disc_toc.disc_type = t.DiskType;
		for (int i = 0; i < 101; i++)
		{
			disc_toc.tracks[i].adr = t.Tracks[i].Adr;
			disc_toc.tracks[i].control = t.Tracks[i].Control;
			disc_toc.tracks[i].lba = t.Tracks[i].Lba;
			disc_toc.tracks[i].valid = t.Tracks[i].Valid;
		}
	}

	virtual void HintReadSector(int32 lba) {}
	virtual bool ReadRawSector(uint8 *buf, int32 lba)
	{
		ReadSector2448Callback(disk, lba, buf);
		return true;
	}
};

static std::vector<CDIF *> CDInterfaces;
static uint32_t InputData[8];

struct MyFrameInfo : public FrameInfo
{
	uint32_t Buttons[3]; // port 1, port 2, console
};
static EmulateSpecStruct Ess;
static int32_t LineWidths[480];
ECL_INVISIBLE static uint32_t FrameBuffer[1024 * 480];

EXPORT bool Init(int numDisks, const uint8_t *bios)
{
	for (int i = 0; i < numDisks; i++)
		CDInterfaces.push_back(new MyCDIF(i));

	if (!TestMagicCD(&CDInterfaces))
		return false;

	LoadCD(&CDInterfaces, bios);
	KING_SetPixelFormat();
	SoundBox_SetSoundRate(44100);
	SCSICD_SetDisc(false, CDInterfaces[0]);
	// multitap is experimental emulation for a never release peripheral, so let's ignore it for now
	FXINPUT_SetMultitap(false, false);
	for (int i = 0; i < 2; i++)
		FXINPUT_SetInput(i, Setting_PortDevice[i], &InputData[i]); // FXIT_GAMEPAD

	PCFX_Power();
	Ess.pixels = FrameBuffer;
	Ess.pitch32 = 1024;
	Ess.LineWidths = LineWidths;
	Ess.SoundBufMaxSize = 2048;

	return true;
}

static int ActiveDisk;
static uint32_t PrevConsoleButtons;

static void Blit(MyFrameInfo &f)
{
	// two widths to deal with: 256 and "341" (which can be 256, 341, or 1024 wide depending on settings)
	// two heights: 240 and 480, but watch out for scanlinestart / scanline end

	// in pixel pro mode, 341 width is forced to 1024.  we upsize 256 to 1024 as well, and double 240 tall

	const uint32_t *src = FrameBuffer;
	uint32_t *dst = f.VideoBuffer;
	const int srcp = 1024;
	src += Ess.y * srcp;

	if (Setting_PixelPro)
	{
		f.Width = 1024;
		f.Height = Ess.h;

		const int dstp = 1024;

		if (Ess.h > 240) // interlace
		{
			if (Ess.w == 256)
			{
				for (int j = 0; j < Ess.h; j++, src += srcp, dst += dstp)
				{
					for (int i = 0; i < 256; i++)
					{
						auto c = src[i];
						dst[i * 4 + 0] = c;
						dst[i * 4 + 1] = c;
						dst[i * 4 + 2] = c;
						dst[i * 4 + 3] = c;
					}
				}
			}
			else
			{
				for (int j = 0; j < Ess.h; j++, src += srcp, dst += dstp)
				{
					memcpy(dst, src, LineWidths[j + Ess.y] * sizeof(uint32_t));
				}
			}
		}
		else // progressive: line double
		{
			f.Height *= 2;
			if (Ess.w == 256)
			{
				for (int j = 0; j < Ess.h; j++, src += srcp, dst += dstp * 2)
				{
					for (int i = 0; i < 256; i++)
					{
						auto c = src[i];
						dst[i * 4 + 0] = c;
						dst[i * 4 + 1] = c;
						dst[i * 4 + 2] = c;
						dst[i * 4 + 3] = c;
					}
					memcpy(dst + dstp, dst, 4096);
				}
			}
			else
			{
				for (int j = 0; j < Ess.h; j++, src += srcp, dst += dstp * 2)
				{
					memcpy(dst, src, 4096);
					memcpy(dst + dstp, src, 4096);
				}
			}
		}
	}
	else
	{
		f.Width = Ess.w;
		f.Height = Ess.h;

		const int dstp = Ess.w;
		for (int j = 0; j < Ess.h; j++, src += srcp, dst += dstp)
		{
			memcpy(dst, src, LineWidths[j + Ess.y] * sizeof(uint32_t));
		}
	}
}

EXPORT void FrameAdvance(MyFrameInfo &f)
{
	for (int i = 0; i < 2; i++)
		InputData[i] = f.Buttons[i];
	Lagged = true;
	uint32_t ConsoleButtons = f.Buttons[2];
	int NewActiveDisk = ActiveDisk;
#define ROSE(n) ((ConsoleButtons & 1 << (n)) > (PrevConsoleButtons & 1 << (n)))
	if (ROSE(0))
		PCFX_Power();
	if (ROSE(1))
		PCFX_Reset();
	if (ROSE(2))
		NewActiveDisk--;
	if (ROSE(3))
		NewActiveDisk++;
#undef ROSE
	NewActiveDisk = std::max(NewActiveDisk, -1);
	NewActiveDisk = std::min<int>(NewActiveDisk, CDInterfaces.size() - 1);
	if (NewActiveDisk != ActiveDisk)
		SCSICD_SetDisc(NewActiveDisk == -1, NewActiveDisk == -1 ? nullptr : CDInterfaces[NewActiveDisk]);
	ActiveDisk = NewActiveDisk;
	PrevConsoleButtons = ConsoleButtons;

	Ess.SoundBuf = f.SoundBuffer;
	Emulate(&Ess);
	f.Cycles = Ess.MasterCycles;
	f.Samples = Ess.SoundBufSize;
	f.Lagged = Lagged;

	Blit(f);
}

EXPORT void GetMemoryAreas(MemoryArea *m)
{
	m[0].Data = BackupRAM;
	m[0].Name = "Backup RAM";
	m[0].Size = sizeof(BackupRAM);
	m[0].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_SAVERAMMABLE;

	m[1].Data = ExBackupRAM;
	m[1].Name = "Extra Backup RAM";
	m[1].Size = sizeof(ExBackupRAM);
	m[1].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_SAVERAMMABLE;

	m[2].Data = BIOSROM;
	m[2].Name = "BIOS ROM";
	m[2].Size = 1024 * 1024;
	m[2].Flags = MEMORYAREA_FLAGS_WORDSIZE4;

	m[3].Data = RAM;
	m[3].Name = "Main RAM";
	m[3].Size = 2 * 1024 * 1024;
	m[3].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_PRIMARY;

	// m[4].Data = FXSCSIROM;
	// m[4].Name = "Scsi Rom";
	// m[4].Size = 512 * 1024;
	// m[4].Flags = MEMORYAREA_FLAGS_WORDSIZE4;

	for (int i = 0; i < 2; i++)
	{
		m[i + 5].Data = fx_vdc_chips[i]->GetVramPointer();
		m[i + 5].Name = i == 0 ? "VDC A VRAM" : "VDC B VRAM";
		m[i + 5].Size = fx_vdc_chips[i]->GetVramByteSize();
		m[i + 5].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE2;
	}
}

EXPORT void EnableLayers(int mask)
{
	// BG0 = 0
	// BG1
	// BG2
	// BG3
	// VDC-A BG
	// VDC-A SPR
	// VDC-B BG
	// VDC-B SPR
	// RAINBOW
	KING_SetLayerEnableMask(mask);
}

EXPORT void SetInputCallback(void (*callback)())
{
	InputCallback = callback;
}

// settings
ECL_SEALED int Setting_HighDotclockWidth = 341;
ECL_SEALED int Setting_CdSpeed = 2;
ECL_SEALED int Setting_SlStart = 4;
ECL_SEALED int Setting_SlEnd = 235;

ECL_SEALED double Setting_ResampRateError = 0.0000009;
ECL_SEALED int Setting_ResampQuality = 3;

ECL_SEALED int Setting_CpuEmulation = 2; // 0 = fast, 1 = accurate, 2 = auto
ECL_SEALED bool Setting_NoSpriteLimit;
ECL_SEALED bool Setting_AdpcmBuggy = false;
ECL_SEALED bool Setting_AdpcmNoClicks = true;
ECL_SEALED bool Setting_ChromaInterpolate = false;

ECL_SEALED int Setting_PortDevice[2];

ECL_SEALED bool Setting_PixelPro;

struct FrontendSettings
{
	int32_t AdpcmEmulateBuggyCodec;
	int32_t AdpcmSuppressChannelResetClicks;
	int32_t HiResEmulation;
	int32_t DisableSpriteLimit;
	int32_t ChromaInterpolation;
	int32_t ScanlineStart;
	int32_t ScanlineEnd;
	int32_t CdSpeed;
	int32_t CpuEmulation;
	int32_t Port1;
	int32_t Port2;
	int32_t PixelPro;
};

EXPORT void PutSettingsBeforeInit(const FrontendSettings &s)
{
	Setting_AdpcmBuggy = s.AdpcmEmulateBuggyCodec;
	Setting_AdpcmNoClicks = s.AdpcmSuppressChannelResetClicks;
	Setting_HighDotclockWidth = s.PixelPro ? 1024 : s.HiResEmulation;
	Setting_NoSpriteLimit = s.DisableSpriteLimit;
	Setting_ChromaInterpolate = s.ChromaInterpolation;
	Setting_SlStart = s.ScanlineStart;
	Setting_SlEnd = s.ScanlineEnd;
	Setting_CdSpeed = s.CdSpeed;
	Setting_CpuEmulation = s.CpuEmulation;
	Setting_PortDevice[0] = s.Port1;
	Setting_PortDevice[1] = s.Port2;
	Setting_PixelPro = s.PixelPro;
}

/*MDFNGI EmulatedPCFX =
	{
		FXINPUT_SetInput,
		SetMedia,
		DoSimpleCommand,
		NULL,
		PCFXSettings,
		MDFN_MASTERCLOCK_FIXED(PCFX_MASTER_CLOCK),
		0,
		TRUE, // Multires possible?

		288, // Nominal width
		240, // Nominal height

		1024, // Framebuffer width
		512,  // Framebuffer height

};*/

int main()
{
	return 0;
}
