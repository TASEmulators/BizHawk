/******************************************************************************/
/* Mednafen Sega Saturn Emulation Module                                      */
/******************************************************************************/
/* ss.cpp - Saturn Core Emulation and Support Functions
**  Copyright (C) 2015-2017 Mednafen Team
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

// WARNING: Be careful with 32-bit access to 16-bit space, bus locking, etc. in respect to DMA and event updates(and where they can occur).

/*#include <mednafen/general.h>
#include <mednafen/FileStream.h>
#include <mednafen/compress/GZFileStream.h>
#include <mednafen/mempatcher.h>
#include <mednafen/hash/sha256.h>
#include <mednafen/hash/md5.h>
#include <mednafen/Time.h>*/

#include <ctype.h>

#include <bitset>

//#include <zlib.h>

//extern MDFNGI EmulatedSS;

#include "ss.h"
#include "cdrom/cdromif.h"
#include "sound.h"
#include "scsp.h" // For debug.inc
#include "smpc.h"
#include "cdb.h"
#include "vdp1.h"
#include "vdp2.h"
#include "scu.h"
#include "cart.h"
#include "db.h"

namespace MDFN_IEN_SS
{

static sscpu_timestamp_t MidSync(const sscpu_timestamp_t timestamp);

#ifdef MDFN_SS_DEV_BUILD
uint32 ss_dbg_mask;
#endif
static const uint8 BRAM_Init_Data[0x10] = {0x42, 0x61, 0x63, 0x6b, 0x55, 0x70, 0x52, 0x61, 0x6d, 0x20, 0x46, 0x6f, 0x72, 0x6d, 0x61, 0x74};

static void SaveBackupRAM(void);
static void LoadBackupRAM(void);
static void SaveCartNV(void);
static void LoadCartNV(void);
static void SaveRTC(void);
static void LoadRTC(void);

static MDFN_COLD void BackupBackupRAM(void);
static MDFN_COLD void BackupCartNV(void);

#include "sh7095.h"

static uint8 SCU_MSH2VectorFetch(void);
static uint8 SCU_SSH2VectorFetch(void);

static void INLINE MDFN_HOT CheckEventsByMemTS(void);

SH7095 CPU[2]{{"SH2-M", SS_EVENT_SH2_M_DMA, SCU_MSH2VectorFetch}, {"SH2-S", SS_EVENT_SH2_S_DMA, SCU_SSH2VectorFetch}};
static uint16* BIOSROM;
static uint16 WorkRAML[1024 * 1024 / sizeof(uint16)];
static uint16 WorkRAMH[1024 * 1024 / sizeof(uint16)]; // Effectively 32-bit in reality, but 16-bit here because of CPU interpreter design(regarding fastmap).
static uint8 BackupRAM[32768];
static bool BackupRAM_Dirty;

static int64 EmulatedSS_MasterClock;

#define SH7095_EXT_MAP_GRAN_BITS 16
static uintptr_t SH7095_FastMap[1U << (32 - SH7095_EXT_MAP_GRAN_BITS)];

int32 SH7095_mem_timestamp;
uint32 SH7095_BusLock;
#include "scu.inc"

#include "debug.inc"

//static sha256_digest BIOS_SHA256;	// SHA-256 hash of the currently-loaded BIOS; used for save state sanity checks.
static std::vector<CDIF *> *cdifs = NULL;
static std::bitset<1U << (27 - SH7095_EXT_MAP_GRAN_BITS)> FMIsWriteable;

template <typename T>
static void INLINE SH7095_BusWrite(uint32 A, T V, const bool BurstHax, int32 *SH2DMAHax);

template <typename T>
static T INLINE SH7095_BusRead(uint32 A, const bool BurstHax, int32 *SH2DMAHax);

// SH-2 region
//  0: 0x00000000-0x01FFFFFF
//  1: 0x02000000-0x03FFFFFF
//  2: 0x04000000-0x05FFFFFF
//  3: 0x06000000-0x07FFFFFF
//
// Never add anything to SH7095_mem_timestamp when DMAHax is true.
//
// When BurstHax is true and we're accessing high work RAM, don't add anything.
//
template <typename T, bool IsWrite>
static INLINE void BusRW(uint32 A, T &V, const bool BurstHax, int32 *SH2DMAHax)
{
	//
	// High work RAM
	//
	if (A >= 0x06000000 && A <= 0x07FFFFFF)
	{
		ne16_rwbo_be<T, IsWrite>(WorkRAMH, A & 0xFFFFF, &V);

		if (!BurstHax)
		{
			if (!SH2DMAHax)
			{
				if (IsWrite)
				{
					SH7095_mem_timestamp = (SH7095_mem_timestamp + 4) & ~3;
				}
				else
				{
					SH7095_mem_timestamp += 7;
				}
			}
			else
				*SH2DMAHax -= IsWrite ? 3 : 6;
		}

		return;
	}

	//
	//
	// SH-2 region 0
	//
	//  Note: 0x00400000 - 0x01FFFFFF: Open bus for accesses to 0x00000000-0x01FFFFFF(SH-2 area 0)
	//
	if (A < 0x02000000)
	{
		if (sizeof(T) == 4)
		{
			if (IsWrite)
			{
				uint16 tmp;

				tmp = V >> 16;
				BusRW<uint16, true>(A, tmp, BurstHax, SH2DMAHax);

				tmp = V >> 0;
				BusRW<uint16, true>(A | 2, tmp, BurstHax, SH2DMAHax);
			}
			else
			{
				uint16 tmp = 0;

				BusRW<uint16, false>(A | 2, tmp, BurstHax, SH2DMAHax);
				V = tmp << 0;

				BusRW<uint16, false>(A, tmp, BurstHax, SH2DMAHax);
				V |= tmp << 16;
			}

			return;
		}

		//
		// Low(and kinda slow) work RAM
		//
		if (A >= 0x00200000 && A <= 0x003FFFFF)
		{
			ne16_rwbo_be<T, IsWrite>(WorkRAML, A & 0xFFFFF, &V);

			if (!SH2DMAHax)
				SH7095_mem_timestamp += 7;
			else
				*SH2DMAHax -= 7;

			return;
		}

		//
		// BIOS ROM
		//
		if (A >= 0x00000000 && A <= 0x000FFFFF)
		{
			if (!SH2DMAHax)
				SH7095_mem_timestamp += 8;
			else
				*SH2DMAHax -= 8;

			if (!IsWrite)
				V = ne16_rbo_be<T>(BIOSROM, A & 0x7FFFF);

			return;
		}

		//
		// SMPC
		//
		if (A >= 0x00100000 && A <= 0x0017FFFF)
		{
			const uint32 SMPC_A = (A & 0x7F) >> 1;

			if (!SH2DMAHax)
			{
				// SH7095_mem_timestamp += 2;
				CheckEventsByMemTS();
			}

			if (IsWrite)
			{
				if (sizeof(T) == 2 || (A & 1))
					SMPC_Write(SH7095_mem_timestamp, SMPC_A, V);
			}
			else
			{
				if (sizeof(T) == 2)
					V = 0xFF00 | SMPC_Read(SH7095_mem_timestamp, SMPC_A);
				else if (sizeof(T) == 1 && (A & 1))
					V = SMPC_Read(SH7095_mem_timestamp, SMPC_A);
				else
					V = 0xFF;
			}

			return;
		}

		//
		// Backup RAM
		//
		if (A >= 0x00180000 && A <= 0x001FFFFF)
		{
			if (!SH2DMAHax)
				SH7095_mem_timestamp += 8;
			else
				*SH2DMAHax -= 8;

			if (IsWrite)
			{
				if (sizeof(T) != 1 || (A & 1))
				{
					BackupRAM[(A >> 1) & 0x7FFF] = V;
					BackupRAM_Dirty = true;
				}
			}
			else
				V = ((BackupRAM[(A >> 1) & 0x7FFF] << 0) | (0xFF << 8)) >> (((A & 1) ^ (sizeof(T) & 1)) << 3);

			return;
		}

		//
		// FRT trigger region
		//
		if (A >= 0x01000000 && A <= 0x01FFFFFF)
		{
			if (!SH2DMAHax)
				SH7095_mem_timestamp += 8;
			else
				*SH2DMAHax -= 8;

			//printf("FT FRT%08x %zu %08x %04x %d %d\n", A, sizeof(T), A, V, SMPC_IsSlaveOn(), SH7095_mem_timestamp);

			if (IsWrite)
			{
				if (sizeof(T) != 1)
				{
					const unsigned c = ((A >> 23) & 1) ^ 1;

					if (!c || SMPC_IsSlaveOn())
					{
						CPU[c].SetFTI(true);
						CPU[c].SetFTI(false);
					}
				}
			}
			return;
		}

		//
		//
		//
		if (!SH2DMAHax)
			SH7095_mem_timestamp += 4;
		else
			*SH2DMAHax -= 4;

		if (IsWrite)
			SS_DBG(SS_DBG_WARNING, "[SH2 BUS] Unknown %zu-byte write of 0x%08x to 0x%08x\n", sizeof(T), V, A);
		else
		{
			SS_DBG(SS_DBG_WARNING, "[SH2 BUS] Unknown %zu-byte read from 0x%08x\n", sizeof(T), A);

			V = 0;
		}

		return;
	}

	//
	// SCU
	//
	{
		uint32 DB;

		if (IsWrite)
			DB = V << (((A & 3) ^ (4 - sizeof(T))) << 3);
		else
			DB = 0;

		SCU_FromSH2_BusRW_DB<T, IsWrite>(A, &DB, SH2DMAHax);

		if (!IsWrite)
			V = DB >> (((A & 3) ^ (4 - sizeof(T))) << 3);
	}
}

template <typename T>
static void INLINE SH7095_BusWrite(uint32 A, T V, const bool BurstHax, int32 *SH2DMAHax)
{
	BusRW<T, true>(A, V, BurstHax, SH2DMAHax);
}

template <typename T>
static T INLINE SH7095_BusRead(uint32 A, const bool BurstHax, int32 *SH2DMAHax)
{
	T ret = 0;

	BusRW<T, false>(A, ret, BurstHax, SH2DMAHax);

	return ret;
}

//
//
//
static MDFN_COLD uint8 CheatMemRead(uint32 A)
{
	A &= (1U << 27) - 1;

#ifdef MSB_FIRST
	return *(uint8 *)(SH7095_FastMap[A >> SH7095_EXT_MAP_GRAN_BITS] + (A ^ 0));
#else
	return *(uint8 *)(SH7095_FastMap[A >> SH7095_EXT_MAP_GRAN_BITS] + (A ^ 1));
#endif
}

static MDFN_COLD void CheatMemWrite(uint32 A, uint8 V)
{
	A &= (1U << 27) - 1;

	if (FMIsWriteable[A >> SH7095_EXT_MAP_GRAN_BITS])
	{
#ifdef MSB_FIRST
		*(uint8 *)(SH7095_FastMap[A >> SH7095_EXT_MAP_GRAN_BITS] + (A ^ 0)) = V;
#else
		*(uint8 *)(SH7095_FastMap[A >> SH7095_EXT_MAP_GRAN_BITS] + (A ^ 1)) = V;
#endif

		for (unsigned c = 0; c < 2; c++)
		{
			if (CPU[c].CCR & SH7095::CCR_CE)
			{
				for (uint32 Abase = 0x00000000; Abase < 0x20000000; Abase += 0x08000000)
				{
					CPU[c].Write_UpdateCache<uint8>(Abase + A, V);
				}
			}
		}
	}
}
//
//
//
static void SetFastMemMap(uint32 Astart, uint32 Aend, uint16 *ptr, uint32 length, bool is_writeable)
{
	const uint64 Abound = (uint64)Aend + 1;
	assert((Astart & ((1U << SH7095_EXT_MAP_GRAN_BITS) - 1)) == 0);
	assert((Abound & ((1U << SH7095_EXT_MAP_GRAN_BITS) - 1)) == 0);
	assert((length & ((1U << SH7095_EXT_MAP_GRAN_BITS) - 1)) == 0);
	assert(length > 0);
	assert(length <= (Abound - Astart));

	for (uint64 A = Astart; A < Abound; A += (1U << SH7095_EXT_MAP_GRAN_BITS))
	{
		uintptr_t tmp = (uintptr_t)ptr + ((A - Astart) % length);

		if (A < (1U << 27))
			FMIsWriteable[A >> SH7095_EXT_MAP_GRAN_BITS] = is_writeable;

		SH7095_FastMap[A >> SH7095_EXT_MAP_GRAN_BITS] = tmp - A;
	}
}

static uint16 fmap_dummy[(1U << SH7095_EXT_MAP_GRAN_BITS) / sizeof(uint16)];

static MDFN_COLD void InitFastMemMap(void)
{
	for (unsigned i = 0; i < sizeof(fmap_dummy) / sizeof(fmap_dummy[0]); i++)
	{
		fmap_dummy[i] = 0;
	}

	FMIsWriteable.reset();
	//MDFNMP_Init(1ULL << SH7095_EXT_MAP_GRAN_BITS, (1ULL << 27) / (1ULL << SH7095_EXT_MAP_GRAN_BITS));

	for (uint64 A = 0; A < 1ULL << 32; A += (1U << SH7095_EXT_MAP_GRAN_BITS))
	{
		SH7095_FastMap[A >> SH7095_EXT_MAP_GRAN_BITS] = (uintptr_t)fmap_dummy - A;
	}
}

void SS_SetPhysMemMap(uint32 Astart, uint32 Aend, uint16 *ptr, uint32 length, bool is_writeable)
{
	assert(Astart < 0x20000000);
	assert(Aend < 0x20000000);

	if (!ptr)
	{
		ptr = fmap_dummy;
		length = sizeof(fmap_dummy);
	}

	for (uint32 Abase = 0; Abase < 0x40000000; Abase += 0x20000000)
		SetFastMemMap(Astart + Abase, Aend + Abase, ptr, length, is_writeable);
}

#include "sh7095.inc"

static bool Running;
event_list_entry events[SS_EVENT__COUNT];
static sscpu_timestamp_t next_event_ts;

template <unsigned c>
static sscpu_timestamp_t SH_DMA_EventHandler(sscpu_timestamp_t et)
{
	if (et < SH7095_mem_timestamp)
	{
		//printf("SH-2 DMA %d reschedule %d->%d\n", c, et, SH7095_mem_timestamp);
		return SH7095_mem_timestamp;
	}

	// Must come after the (et < SH7095_mem_timestamp) check.
	if (MDFN_UNLIKELY(SH7095_BusLock))
		return et + 1;

	return CPU[c].DMA_Update(et);
}

//
//
//

static MDFN_COLD void InitEvents(void)
{
	for (unsigned i = 0; i < SS_EVENT__COUNT; i++)
	{
		if (i == SS_EVENT__SYNFIRST)
			events[i].event_time = 0;
		else if (i == SS_EVENT__SYNLAST)
			events[i].event_time = 0x7FFFFFFF;
		else
			events[i].event_time = 0; //SS_EVENT_DISABLED_TS;

		events[i].prev = (i > 0) ? &events[i - 1] : NULL;
		events[i].next = (i < (SS_EVENT__COUNT - 1)) ? &events[i + 1] : NULL;
	}

	events[SS_EVENT_SH2_M_DMA].event_handler = &SH_DMA_EventHandler<0>;
	events[SS_EVENT_SH2_S_DMA].event_handler = &SH_DMA_EventHandler<1>;

	events[SS_EVENT_SCU_DMA].event_handler = SCU_UpdateDMA;
	events[SS_EVENT_SCU_DSP].event_handler = SCU_UpdateDSP;

	events[SS_EVENT_SMPC].event_handler = SMPC_Update;

	events[SS_EVENT_VDP1].event_handler = VDP1::Update;
	events[SS_EVENT_VDP2].event_handler = VDP2::Update;

	events[SS_EVENT_CDB].event_handler = CDB_Update;

	events[SS_EVENT_SOUND].event_handler = SOUND_Update;

	events[SS_EVENT_CART].event_handler = CART_GetEventHandler();

	events[SS_EVENT_MIDSYNC].event_handler = MidSync;
	events[SS_EVENT_MIDSYNC].event_time = SS_EVENT_DISABLED_TS;
}

static void RebaseTS(const sscpu_timestamp_t timestamp)
{
	for (unsigned i = 0; i < SS_EVENT__COUNT; i++)
	{
		if (i == SS_EVENT__SYNFIRST || i == SS_EVENT__SYNLAST)
			continue;

		assert(events[i].event_time > timestamp);

		if (events[i].event_time != SS_EVENT_DISABLED_TS)
			events[i].event_time -= timestamp;
	}

	next_event_ts = events[SS_EVENT__SYNFIRST].next->event_time;
}

void SS_SetEventNT(event_list_entry *e, const sscpu_timestamp_t next_timestamp)
{
	if (next_timestamp < e->event_time)
	{
		event_list_entry *fe = e;

		do
		{
			fe = fe->prev;
		} while (next_timestamp < fe->event_time);

		// Remove this event from the list, temporarily of course.
		e->prev->next = e->next;
		e->next->prev = e->prev;

		// Insert into the list, just after "fe".
		e->prev = fe;
		e->next = fe->next;
		fe->next->prev = e;
		fe->next = e;

		e->event_time = next_timestamp;
	}
	else if (next_timestamp > e->event_time)
	{
		event_list_entry *fe = e;

		do
		{
			fe = fe->next;
		} while (next_timestamp > fe->event_time);

		// Remove this event from the list, temporarily of course
		e->prev->next = e->next;
		e->next->prev = e->prev;

		// Insert into the list, just BEFORE "fe".
		e->prev = fe->prev;
		e->next = fe;
		fe->prev->next = e;
		fe->prev = e;

		e->event_time = next_timestamp;
	}

	next_event_ts = (Running ? events[SS_EVENT__SYNFIRST].next->event_time : 0);
}

// Called from debug.cpp too.
void ForceEventUpdates(const sscpu_timestamp_t timestamp)
{
	CPU[0].ForceInternalEventUpdates();

	if (SMPC_IsSlaveOn())
		CPU[1].ForceInternalEventUpdates();

	for (unsigned evnum = SS_EVENT__SYNFIRST + 1; evnum < SS_EVENT__SYNLAST; evnum++)
	{
		if (events[evnum].event_time != SS_EVENT_DISABLED_TS)
			SS_SetEventNT(&events[evnum], events[evnum].event_handler(timestamp));
	}

	next_event_ts = (Running ? events[SS_EVENT__SYNFIRST].next->event_time : 0);
}

static INLINE bool EventHandler(const sscpu_timestamp_t timestamp)
{
	event_list_entry *e;

	while (timestamp >= (e = events[SS_EVENT__SYNFIRST].next)->event_time) // If Running = 0, EventHandler() may be called even if there isn't an event per-se, so while() instead of do { ... } while
	{
#ifdef MDFN_SS_DEV_BUILD
		const sscpu_timestamp_t etime = e->event_time;
#endif
		sscpu_timestamp_t nt;

		nt = e->event_handler(e->event_time);

#ifdef MDFN_SS_DEV_BUILD
		if (MDFN_UNLIKELY(nt <= etime))
		{
			fprintf(stderr, "which=%d event_time=%d nt=%d timestamp=%d\n", (int)(e - events), etime, nt, timestamp);
			assert(nt > etime);
		}
#endif

		SS_SetEventNT(e, nt);
	}

	return (Running);
}

static void NO_INLINE MDFN_HOT CheckEventsByMemTS_Sub(void)
{
	EventHandler(SH7095_mem_timestamp);
}

static void INLINE CheckEventsByMemTS(void)
{
	if (MDFN_UNLIKELY(SH7095_mem_timestamp >= next_event_ts))
	{
		//puts("Woot");
		CheckEventsByMemTS_Sub();
	}
}

void SS_RequestMLExit(void)
{
	Running = 0;
	next_event_ts = 0;
}

#pragma GCC push_options
#if !defined(__clang__) && defined(__GNUC__) && __GNUC__ < 5
// gcc 5.3.0 and 6.1.0 produce some braindead code for the big switch() statement at -Os.
#pragma GCC optimize("Os,no-unroll-loops,no-peel-loops,no-crossjumping")
#else
#pragma GCC optimize("O2,no-unroll-loops,no-peel-loops,no-crossjumping")
#endif
template <bool DebugMode>
static int32 NO_INLINE MDFN_HOT RunLoop(EmulateSpecStruct *espec)
{
	sscpu_timestamp_t eff_ts = 0;

	//printf("%d %d\n", SH7095_mem_timestamp, CPU[0].timestamp);

	do
	{
		do
		{
			if (DebugMode)
				DBG_CPUHandler<0>(eff_ts);

			CPU[0].Step<0, DebugMode>();

			while (MDFN_LIKELY(CPU[0].timestamp > CPU[1].timestamp))
			{
				if (DebugMode)
					DBG_CPUHandler<1>(eff_ts);

				CPU[1].Step<1, DebugMode>();
			}

			eff_ts = CPU[0].timestamp;
			if (SH7095_mem_timestamp > eff_ts)
				eff_ts = SH7095_mem_timestamp;
			else
				SH7095_mem_timestamp = eff_ts;
		} while (MDFN_LIKELY(eff_ts < next_event_ts));
	} while (MDFN_LIKELY(EventHandler(eff_ts)));

	//printf(" End: %d %d -- %d\n", SH7095_mem_timestamp, CPU[0].timestamp, eff_ts);
	return eff_ts;
}
#pragma GCC pop_options

// Must not be called within an event or read/write handler.
void SS_Reset(bool powering_up)
{
	SH7095_BusLock = 0;

	if (powering_up)
	{
		memset(WorkRAML, 0x00, sizeof(WorkRAML)); // TODO: Check
		memset(WorkRAMH, 0x00, sizeof(WorkRAMH)); // TODO: Check
	}

	if (powering_up)
	{
		CPU[0].TruePowerOn();
		CPU[1].TruePowerOn();
	}

	SCU_Reset(powering_up);
	CPU[0].Reset(powering_up);

	SMPC_Reset(powering_up);

	VDP1::Reset(powering_up);
	VDP2::Reset(powering_up);

	CDB_Reset(powering_up);

	SOUND_Reset(powering_up);

	CART_Reset(powering_up);
}

static EmulateSpecStruct *espec;
static int32 cur_clock_div;

static int64 UpdateInputLastBigTS;
static INLINE void UpdateSMPCInput(const sscpu_timestamp_t timestamp)
{
	int32 elapsed_time = (((int64)timestamp * cur_clock_div * 1000 * 1000) - UpdateInputLastBigTS) / (EmulatedSS_MasterClock / MDFN_MASTERCLOCK_FIXED(1));

	UpdateInputLastBigTS += (int64)elapsed_time * (EmulatedSS_MasterClock / MDFN_MASTERCLOCK_FIXED(1));

	SMPC_UpdateInput(elapsed_time);
}

static sscpu_timestamp_t MidSync(const sscpu_timestamp_t timestamp)
{
	return SS_EVENT_DISABLED_TS;
}

void Emulate(EmulateSpecStruct *espec_arg)
{
	int32 end_ts;

	espec = espec_arg;

	cur_clock_div = SMPC_StartFrame(espec);
	UpdateSMPCInput(0);
	VDP2::StartFrame(espec, cur_clock_div == 61);
	SOUND_StartFrame(44100, 5);
	CART_SetCPUClock(EmulatedSS_MasterClock / MDFN_MASTERCLOCK_FIXED(1), cur_clock_div);
	espec->SoundBufSize = 0;
	espec->MasterCycles = 0;
	//
	//
	//
	Running = true; // Set before ForceEventUpdates()
	ForceEventUpdates(0);

#ifdef WANT_DEBUGGER
	if (DBG_NeedCPUHooks())
		end_ts = RunLoop<true>(espec);
	else
#endif
		end_ts = RunLoop<false>(espec);

	ForceEventUpdates(end_ts);
	//
	//
	//
	RebaseTS(end_ts);

	CDB_ResetTS();
	SOUND_ResetTS();
	VDP1::AdjustTS(-end_ts);
	VDP2::AdjustTS(-end_ts);
	SMPC_ResetTS();
	SCU_AdjustTS(-end_ts);
	CART_AdjustTS(-end_ts);

	UpdateInputLastBigTS -= (int64)end_ts * cur_clock_div * 1000 * 1000;

	if (!(SH7095_mem_timestamp & 0x40000000)) // or maybe >= 0 instead?
		SH7095_mem_timestamp -= end_ts;

	CPU[0].AdjustTS(-end_ts);

	if (SMPC_IsSlaveOn())
		CPU[1].AdjustTS(-end_ts);
	//
	//
	//
	espec->MasterCycles = end_ts * cur_clock_div;
	espec->SoundBufSize += SOUND_FlushOutput(espec->SoundBuf + (espec->SoundBufSize * 2), espec->SoundBufMaxSize - espec->SoundBufSize, false);
	//
	//
	//
	SMPC_UpdateOutput();
}

static INLINE void CalcGameID(uint8 *fd_id_out16, char *sgid)
{
	std::unique_ptr<uint8[]> buf(new uint8[2048]);

	for (size_t x = 0; x < cdifs->size(); x++)
	{
		auto *c = (*cdifs)[x];
		CDUtility::TOC toc;

		c->ReadTOC(&toc);

		for (unsigned i = 0; i < 512; i++)
		{
			if (c->ReadSector(&buf[0], i, 1, true) >= 0x1)
			{
				if (i == 0)
				{
					char *tmp;
					memcpy(sgid, &buf[0x20], 16);
					sgid[16] = 0;
					if ((tmp = strrchr(sgid, 'V')))
					{
						do
						{
							*tmp = 0;
						} while (tmp-- != sgid && (signed char)*tmp <= 0x20);
					}
				}
			}
		}
	}
}

//
// Remember to rebuild region database in db.cpp if changing the order of entries in this table(and be careful about game id collisions, e.g. with some Korean games).
//
static const struct
{
	const char c;
	const char *str; // Community-defined region string that may appear in filename.
	unsigned region;
} region_strings[] =
	{
		// Listed in order of preference for multi-region games.
		{'U', "USA", SMPC_AREA_NA},
		{'J', "Japan", SMPC_AREA_JP},
		{'K', "Korea", SMPC_AREA_KR},

		{'E', "Europe", SMPC_AREA_EU_PAL},
		{'E', "Germany", SMPC_AREA_EU_PAL},
		{'E', "France", SMPC_AREA_EU_PAL},
		{'E', "Spain", SMPC_AREA_EU_PAL},

		{'B', "Brazil", SMPC_AREA_CSA_NTSC},

		{'T', nullptr, SMPC_AREA_ASIA_NTSC},
		{'A', nullptr, SMPC_AREA_ASIA_PAL},
		{'L', nullptr, SMPC_AREA_CSA_PAL},
};

static INLINE bool DetectRegion(unsigned *const region)
{
	std::unique_ptr<uint8[]> buf(new uint8[2048 * 16]);
	uint64 possible_regions = 0;

	for (auto &c : *cdifs)
	{
		if (c->ReadSector(&buf[0], 0, 16, true) != 0x1)
			continue;

		for (unsigned i = 0; i < 16; i++)
		{
			for (auto const &rs : region_strings)
			{
				if (rs.c == buf[0x40 + i])
				{
					possible_regions |= (uint64)1 << rs.region;
					break;
				}
			}
		}
		break;
	}

	for (auto const &rs : region_strings)
	{
		if (possible_regions & ((uint64)1 << rs.region))
		{
			*region = rs.region;
			return true;
		}
	}

	return false;
}

extern bool CorrectAspect;
extern bool ShowHOverscan;
extern bool DoHBlend;
extern int LineVisFirst;
extern int LineVisLast;
static bool MDFN_COLD InitCommon(const unsigned cart_type, const unsigned smpc_area)
{
#ifdef MDFN_SS_DEV_BUILD
	ss_dbg_mask = MDFN_GetSettingUI("ss.dbg_mask");
#endif
	//
	/*{
		MDFN_printf(_("Region: 0x%01x\n"), smpc_area);
		const struct
		{
			const unsigned type;
			const char *name;
		} CartNames[] =
			{
				{CART_NONE, _("None")},
				{CART_BACKUP_MEM, _("Backup Memory")},
				{CART_EXTRAM_1M, _("1MiB Extended RAM")},
				{CART_EXTRAM_4M, _("4MiB Extended RAM")},
				{CART_KOF95, _("King of Fighters '95 ROM")},
				{CART_ULTRAMAN, _("Ultraman ROM")},
				{CART_CS1RAM_16M, _("16MiB CS1 RAM")},
				{CART_NLMODEM, _("Netlink Modem")},
				{CART_MDFN_DEBUG, _("Mednafen Debug")},
			};
		const char *cn = _("Unknown");

		for (auto const &cne : CartNames)
		{
			if (cne.type == cart_type)
			{
				cn = cne.name;
				break;
			}
		}
		MDFN_printf(_("Cart: %s\n"), cn);
	}*/
	//

	for (unsigned c = 0; c < 2; c++)
	{
		CPU[c].Init();
		CPU[c].SetMD5((bool)c);
	}

	//
	// Initialize backup memory.
	//
	memset(BackupRAM, 0x00, sizeof(BackupRAM));
	for (unsigned i = 0; i < 0x40; i++)
		BackupRAM[i] = BRAM_Init_Data[i & 0x0F];
	AddMemoryDomain("Backup Ram", BackupRAM, sizeof(BackupRAM), true);

	// Call InitFastMemMap() before functions like SOUND_Init()
	InitFastMemMap();
	BIOSROM = (uint16*)alloc_sealed(524288);
	AddMemoryDomain("Boot Rom", BIOSROM, 524288, false);
	SS_SetPhysMemMap(0x00000000, 0x000FFFFF, BIOSROM, 524288);
	SS_SetPhysMemMap(0x00200000, 0x003FFFFF, WorkRAML, sizeof(WorkRAML), true);
	SS_SetPhysMemMap(0x06000000, 0x07FFFFFF, WorkRAMH, sizeof(WorkRAMH), true);
	AddMemoryDomain("Work Ram Low", WorkRAML, sizeof(WorkRAML), true);
	AddMemoryDomain("Work Ram High", WorkRAMH, sizeof(WorkRAMH), true);

	CART_Init(cart_type);
	//
	//
	//
	const bool PAL = (smpc_area & SMPC_AREA__PAL_MASK);
	const int32 MasterClock = PAL ? 1734687500 : 1746818182; // NTSC: 1746818181.8181818181, PAL: 1734687500-ish
	const char *biospath;
	int sls = PAL ? setting_ss_slstartp : setting_ss_slstart;
	int sle = PAL ? setting_ss_slendp : setting_ss_slend;

	if (PAL)
	{
		sls += 16;
		sle += 16;
	}

	if (sls > sle)
		std::swap(sls, sle);

	if (smpc_area == SMPC_AREA_JP)
		biospath = "BIOS_J";
	else if (smpc_area == SMPC_AREA_ASIA_NTSC)
		biospath = "BIOS_A";
	else if (PAL)
		biospath = "BIOS_E";
	else
		biospath = "BIOS_U";

	if (FirmwareSizeCallback(biospath) != 524288)
	{
		printf("BIOS file is of an incorrect size.\n");
		return false;
	}

	FirmwareDataCallback(biospath, (uint8*)&BIOSROM[0]);
	for (unsigned i = 0; i < 262144; i++)
		BIOSROM[i] = MDFN_de16msb(&BIOSROM[i]);

	EmulatedSS_MasterClock = MDFN_MASTERCLOCK_FIXED(MasterClock);

	SCU_Init();
	SMPC_Init(smpc_area, MasterClock);
	VDP1::Init();
	VDP2::Init(PAL);
	CDB_Init();
	SOUND_Init();

	InitEvents();
	UpdateInputLastBigTS = 0;

	DBG_Init();
	//
	//
	//
	MDFN_printf("\n");
	{
		CorrectAspect = setting_ss_correct_aspect;
		ShowHOverscan = setting_ss_h_overscan;
		DoHBlend = setting_ss_h_blend;
 		LineVisFirst = sls;
 		LineVisLast = sle;

		MDFN_printf(_("Displayed scanlines: [%u,%u]\n"), sls, sle);
		MDFN_printf(_("Correct Aspect Ratio: %s\n"), correct_aspect ? _("Enabled") : _("Disabled"));
		MDFN_printf(_("Show H Overscan: %s\n"), h_overscan ? _("Enabled") : _("Disabled"));
		MDFN_printf(_("H Blend: %s\n"), h_blend ? _("Enabled") : _("Disabled"));

		// VDP2::SetGetVideoParams(&EmulatedSS, correct_aspect, sls, sle, h_overscan, h_blend);
	}

	MDFN_printf("\n");
	for (unsigned sp = 0; sp < 2; sp++)
	{
		SMPC_SetMultitap(sp, false);

		MDFN_printf(_("Multitap on Saturn Port %u: %s\n"), sp + 1, sv ? _("Enabled") : _("Disabled"));
	}
	//
	//
	//
	/*try
	{
		LoadRTC();
	}
	catch (MDFN_Error &e)
	{
		if (e.GetErrno() != ENOENT)
			throw;
	}
	try
	{
		LoadBackupRAM();
	}
	catch (MDFN_Error &e)
	{
		if (e.GetErrno() != ENOENT)
			throw;
	}
	try
	{
		LoadCartNV();
	}
	catch (MDFN_Error &e)
	{
		if (e.GetErrno() != ENOENT)
			throw;
	}*/

	BackupRAM_Dirty = false;

	CART_GetClearNVDirty();

	/*
	if (MDFN_GetSettingB("ss.smpc.autortc"))
	{
		struct tm ht = Time::LocalTime();

		SMPC_SetRTC(&ht, MDFN_GetSettingUI("ss.smpc.autortc.lang"));
	}
	*/
	SS_Reset(true);
	return true;
}

MDFN_COLD bool LoadCD(std::vector<CDIF *> *CDInterfaces)
{
	const unsigned region_default = setting_ss_region_default;
	unsigned region = region_default;
	int cart_type;
	uint8 fd_id[16];
	char sgid[16 + 1];
	cdifs = CDInterfaces;
	CalcGameID(fd_id, sgid);

	if (setting_ss_region_autodetect)
		if (!DB_LookupRegionDB(fd_id, &region))
			DetectRegion(&region);
	//
	//
	if ((cart_type = setting_ss_cart) == CART__RESERVED)
	{
		cart_type = CART_BACKUP_MEM;
		DB_LookupCartDB(sgid, fd_id, &cart_type);
	}

	// TODO: auth ID calc

	if (!InitCommon(cart_type, region))
		return false;

	return true;
}

/*static const FileExtensionSpecStruct KnownExtensions[] =
{
 { ".elf", gettext_noop("SS Homebrew ELF Executable") },

 { NULL, NULL }
};*/

/*static const MDFNSetting_EnumList Region_List[] =
{
 { "jp", SMPC_AREA_JP, gettext_noop("Japan") },
 { "na", SMPC_AREA_NA, gettext_noop("North America") },
 { "eu", SMPC_AREA_EU_PAL, gettext_noop("Europe") },
 { "kr", SMPC_AREA_KR, gettext_noop("South Korea") },

 { "tw", SMPC_AREA_ASIA_NTSC, gettext_noop("Taiwan") },	// Taiwan, Philippines
 { "as", SMPC_AREA_ASIA_PAL, gettext_noop("China") },	// China, Middle East

 { "br", SMPC_AREA_CSA_NTSC, gettext_noop("Brazil") },
 { "la", SMPC_AREA_CSA_PAL, gettext_noop("Latin America") },

 { NULL, 0 },
};

static const MDFNSetting_EnumList RTCLang_List[] =
{
 { "english", SMPC_RTC_LANG_ENGLISH, gettext_noop("English") },
 { "german", SMPC_RTC_LANG_GERMAN, gettext_noop("Deutsch") },
 { "french", SMPC_RTC_LANG_FRENCH, gettext_noop("Français") },
 { "spanish", SMPC_RTC_LANG_SPANISH, gettext_noop("Español") },
 { "italian", SMPC_RTC_LANG_ITALIAN, gettext_noop("Italiano") },
 { "japanese", SMPC_RTC_LANG_JAPANESE, gettext_noop("日本語") },

 { "deutsch", SMPC_RTC_LANG_GERMAN, NULL },
 { "français", SMPC_RTC_LANG_FRENCH, NULL },
 { "español", SMPC_RTC_LANG_SPANISH, NULL },
 { "italiano", SMPC_RTC_LANG_ITALIAN, NULL },
 { "日本語", SMPC_RTC_LANG_JAPANESE, NULL},

 { NULL, 0 },
};

static const MDFNSetting_EnumList Cart_List[] =
{
 { "auto", CART__RESERVED, gettext_noop("Automatic") },
 { "none", CART_NONE, gettext_noop("None") },
 { "backup", CART_BACKUP_MEM, gettext_noop("Backup Memory(512KiB)") },
 { "extram1", CART_EXTRAM_1M, gettext_noop("1MiB Extended RAM") },
 { "extram4", CART_EXTRAM_4M, gettext_noop("4MiB Extended RAM") },
 { "cs1ram16", CART_CS1RAM_16M, gettext_noop("16MiB RAM mapped in A-bus CS1") },
// { "nlmodem", CART_NLMODEM, gettext_noop("NetLink Modem") },

 { NULL, 0 },
};

static const MDFNSetting SSSettings[] =
{
 { "ss.bios_jp", MDFNSF_EMU_STATE, gettext_noop("Path to the Japan ROM BIOS"), NULL, MDFNST_STRING, "sega_101.bin" },
 { "ss.bios_na_eu", MDFNSF_EMU_STATE, gettext_noop("Path to the North America and Europe ROM BIOS"), NULL, MDFNST_STRING, "mpr-17933.bin" },

 { "ss.scsp.resamp_quality", MDFNSF_NOFLAGS, gettext_noop("SCSP output resampler quality."),
	gettext_noop("0 is lowest quality and CPU usage, 10 is highest quality and CPU usage.  The resampler that this setting refers to is used for converting from 44.1KHz to the sampling rate of the host audio device Mednafen is using.  Changing Mednafen's output rate, via the \"sound.rate\" setting, to \"44100\" may bypass the resampler, which can decrease CPU usage by Mednafen, and can increase or decrease audio quality, depending on various operating system and hardware factors."), MDFNST_UINT, "4", "0", "10" },

 { "ss.region_autodetect", MDFNSF_EMU_STATE | MDFNSF_UNTRUSTED_SAFE, gettext_noop("Attempt to auto-detect region of game."), NULL, MDFNST_BOOL, "1" },
 { "ss.region_default", MDFNSF_EMU_STATE | MDFNSF_UNTRUSTED_SAFE, gettext_noop("Default region to use."), gettext_noop("Used if region autodetection fails or is disabled."), MDFNST_ENUM, "jp", NULL, NULL, NULL, NULL, Region_List },

 { "ss.input.mouse_sensitivity", MDFNSF_NOFLAGS, gettext_noop("Emulated mouse sensitivity."), NULL, MDFNST_FLOAT, "0.50", NULL, NULL },
 { "ss.input.sport1.multitap", MDFNSF_EMU_STATE | MDFNSF_UNTRUSTED_SAFE, gettext_noop("Enable multitap on Saturn port 1."), NULL, MDFNST_BOOL, "0", NULL, NULL },
 { "ss.input.sport2.multitap", MDFNSF_EMU_STATE | MDFNSF_UNTRUSTED_SAFE, gettext_noop("Enable multitap on Saturn port 2."), NULL, MDFNST_BOOL, "0", NULL, NULL },

 { "ss.smpc.autortc", MDFNSF_NOFLAGS, gettext_noop("Automatically set RTC on game load."), gettext_noop("Automatically set the SMPC's emulated Real-Time Clock to the host system's current time and date upon game load."), MDFNST_BOOL, "1" },
 { "ss.smpc.autortc.lang", MDFNSF_NOFLAGS, gettext_noop("BIOS language."), gettext_noop("Also affects language used in some games(e.g. the European release of \"Panzer Dragoon\")."), MDFNST_ENUM, "english", NULL, NULL, NULL, NULL, RTCLang_List },

 { "ss.cart", MDFNSF_EMU_STATE | MDFNSF_UNTRUSTED_SAFE, gettext_noop("Expansion cart."), NULL, MDFNST_ENUM, "auto", NULL, NULL, NULL, NULL, Cart_List },
 { "ss.cart.kof95_path", MDFNSF_EMU_STATE, gettext_noop("Path to KoF 95 ROM image."), NULL, MDFNST_STRING, "mpr-18811-mx.ic1" },
 { "ss.cart.ultraman_path", MDFNSF_EMU_STATE, gettext_noop("Path to Ultraman ROM image."), NULL, MDFNST_STRING, "mpr-19367-mx.ic1" },
 
 { "ss.bios_sanity", MDFNSF_NOFLAGS, gettext_noop("Enable BIOS ROM image sanity checks."), NULL, MDFNST_BOOL, "1" },

 { "ss.cd_sanity", MDFNSF_NOFLAGS, gettext_noop("Enable CD (image) sanity checks."), NULL, MDFNST_BOOL, "1" },

 { "ss.slstart", MDFNSF_NOFLAGS, gettext_noop("First displayed scanline in NTSC mode."), NULL, MDFNST_INT, "0", "0", "239" },
 { "ss.slend", MDFNSF_NOFLAGS, gettext_noop("Last displayed scanline in NTSC mode."), NULL, MDFNST_INT, "239", "0", "239" },

 { "ss.h_overscan", MDFNSF_NOFLAGS, gettext_noop("Show horizontal overscan area."), NULL, MDFNST_BOOL, "1" },

 { "ss.h_blend", MDFNSF_NOFLAGS, gettext_noop("Enable horizontal blend(blur) filter."), gettext_noop("Intended for use in combination with the \"goat\" OpenGL shader, or with bilinear interpolation or linear interpolation on the X axis enabled.  Has a more noticeable effect with the Saturn's higher horizontal resolution modes(640/704)."), MDFNST_BOOL, "0" },

 { "ss.correct_aspect", MDFNSF_NOFLAGS, gettext_noop("Correct aspect ratio."), gettext_noop("Disabling aspect ratio correction with this setting should be considered a hack.\n\nIf disabling it to allow for sharper pixels by also separately disabling interpolation(though using Mednafen's \"autoipsharper\" OpenGL shader is usually a better option), remember to use scale factors that are multiples of 2, or else games that use high-resolution and interlaced modes will have distorted pixels.\n\nDisabling aspect ratio correction with this setting will allow for the QuickTime movie recording feature to produce much smaller files using much less CPU time."), MDFNST_BOOL, "1" },

 { "ss.slstartp", MDFNSF_NOFLAGS, gettext_noop("First displayed scanline in PAL mode."), NULL, MDFNST_INT, "0", "-16", "271" },
 { "ss.slendp", MDFNSF_NOFLAGS, gettext_noop("Last displayed scanline in PAL mode."), NULL, MDFNST_INT, "255", "-16", "271" },

 { "ss.midsync", MDFNSF_NOFLAGS, gettext_noop("Enable mid-frame synchronization."), gettext_noop("Mid-frame synchronization can reduce input latency, but it will increase CPU requirements."), MDFNST_BOOL, "0" },

#ifdef MDFN_SS_DEV_BUILD
 { "ss.dbg_mask", MDFNSF_SUPPRESS_DOC, gettext_noop("Debug printf mask."), NULL, MDFNST_UINT, "0x00001", "0x00000", "0xFFFFF" },
 { "ss.dbg_exe_cdpath", MDFNSF_SUPPRESS_DOC, gettext_noop("CD image to use with homebrew executable loading."), NULL, MDFNST_STRING, "" },
#endif

 { NULL },
};

static const CheatInfoStruct CheatInfo =
{
 NULL,
 NULL,

 CheatMemRead,
 CheatMemWrite,

 CheatFormatInfo_Empty,

 true
};*/
}

using namespace MDFN_IEN_SS;

/*MDFNGI EmulatedSS =
{
 "ss",
 "Sega Saturn",
 KnownExtensions,
 MODPRIO_INTERNAL_HIGH,
 #ifdef WANT_DEBUGGER
 &DBGInfo,
 #else
 NULL,
 #endif
 SMPC_PortInfo,
#ifdef MDFN_SS_DEV_BUILD
 Load,
 TestMagic,
#else
 NULL,
 NULL,
#endif
 LoadCD,
 TestMagicCD,
 CloseGame,

 VDP2::SetLayerEnableMask,
 "NBG0\0NBG1\0NBG2\0NBG3\0RBG0\0RBG1\0Sprite\0",

 NULL,
 NULL,

 NULL,
 0,

 CheatInfo,

 false,
 NULL, //StateAction,
 Emulate,
 NULL,
 SMPC_SetInput,
 SetMedia,
 DoSimpleCommand,
 NULL,
 SSSettings,
 0,
 0,

 true, // Multires possible?

 //
 // Note: Following video settings will be overwritten during game load.
 //
 320,	// lcm_width
 240,	// lcm_height
 NULL,  // Dummy

 302,   // Nominal width
 240,   // Nominal height

 0,   // Framebuffer width
 0,   // Framebuffer height
 //
 //
 //

 2,     // Number of output sound channels
};*/
