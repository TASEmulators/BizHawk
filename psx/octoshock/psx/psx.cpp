/* Mednafen - Multi-system Emulator
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

#include "octoshock.h"
#include "psx.h"
#include "mdec.h"
#include "frontio.h"
#include "timer.h"
#include "sio.h"
#include "cdc.h"
#include "Stream.h"
#include "spu.h"
#include "error.h"
#include "tests.h"
#include "endian.h"
#include "emuware/EW_state.h"

#include "input/dualshock.h"
#include "input/dualanalog.h"
#include "input/gamepad.h"
#include "input/memcard.h"


#include <stdarg.h>
#include <ctype.h>

//I apologize for the absolute madness of the resolution management and framebuffer management and normalizing in here.
//It's grown entirely out of control. The main justification for the original design was not wrecking mednafen internals too much.

//we're a bit sloppy right now.. use this to make sure theres adequate room for double-sizing a 400px wide screen
#define FB_WIDTH 800
#define FB_HEIGHT 576

#define kScanlineWidthHeuristicIndex 64

//extern MDFNGI EmulatedPSX;

int16 soundbuf[1024 * 1024]; //how big? big enough.
int VTBackBuffer = 0;
bool GpuFrameForLag = false;
static MDFN_Rect VTDisplayRects[2];
#include	"video/Deinterlacer.h"
static bool PrevInterlaced;
static Deinterlacer deint;
static EmulateSpecStruct espec;

namespace MDFN_IEN_PSX
{


#if PSX_DBGPRINT_ENABLE
static unsigned psx_dbg_level = 0;

void PSX_DBG_BIOS_PUTC(uint8 c) noexcept
{
 if(psx_dbg_level >= PSX_DBG_BIOS_PRINT)
 {
  if(c == 0x1B)
   return;

  fputc(c, stdout);

  //if(c == '\n')
  //{
  // fputc('%', stdout);
  // fputc(' ', stdout);
  //}
  fflush(stdout);
 }
}

void PSX_DBG(unsigned level, const char *format, ...) noexcept
{
 if(psx_dbg_level >= level)
 {
  va_list ap;

  va_start(ap, format);

  trio_vprintf(format, ap);

  va_end(ap);
 }
}
#else
static unsigned const psx_dbg_level = 0;
#endif

struct MDFN_PseudoRNG	// Based off(but not the same as) public-domain "JKISS" PRNG.
{
 MDFN_PseudoRNG()
 {
  ResetState();
 }

 u32 RandU32(void)
 {
  u64 t;

  x = 314527869 * x + 1234567;
  y ^= y << 5; y ^= y >> 7; y ^= y << 22;
  t = 4294584393ULL * z + c; c = t >> 32; z = t;
  lcgo = (19073486328125ULL * lcgo) + 1;

  return (x + y + z) ^ (lcgo >> 16);
 }

 uint32 RandU32(uint32 mina, uint32 maxa)
 {
  const uint32 range_m1 = maxa - mina;
  uint32 range_mask;
  uint32 tmp;

  range_mask = range_m1;
  range_mask |= range_mask >> 1;
  range_mask |= range_mask >> 2;
  range_mask |= range_mask >> 4;
  range_mask |= range_mask >> 8;
  range_mask |= range_mask >> 16;

  do
  {
   tmp = RandU32() & range_mask;
  } while(tmp > range_m1);
 
  return(mina + tmp);
 }

 void ResetState(void)	// Must always reset to the same state.
 {
  x = 123456789;
  y = 987654321;
  z = 43219876;
  c = 6543217;
  lcgo = 0xDEADBEEFCAFEBABEULL;
 }

 uint32 x,y,z,c;
 uint64 lcgo;
};

static MDFN_PseudoRNG PSX_PRNG;

uint32 PSX_GetRandU32(uint32 mina, uint32 maxa)
{
 return PSX_PRNG.RandU32(mina, maxa);
}

static std::vector<CDIF*> *cdifs = NULL;
static std::vector<const char *> cdifs_scex_ids;

static uint64 Memcard_PrevDC[8];
static int64 Memcard_SaveDelay[8];

PS_CPU *CPU = NULL;
PS_SPU *SPU = NULL;
PS_GPU *GPU = NULL;
PS_CDC *CDC = NULL;
FrontIO *FIO = NULL;

static MultiAccessSizeMem<512 * 1024, false> *BIOSROM = NULL;
static MultiAccessSizeMem<65536, false> *PIOMem = NULL;

MultiAccessSizeMem<2048 * 1024, false> MainRAM;

static uint32 TextMem_Start;
static std::vector<uint8> TextMem;

static const uint32 SysControl_Mask[9] = { 0x00ffffff, 0x00ffffff, 0xffffffff, 0x2f1fffff,
					   0xffffffff, 0x2f1fffff, 0x2f1fffff, 0xffffffff,
					   0x0003ffff };

static const uint32 SysControl_OR[9] = { 0x1f000000, 0x1f000000, 0x00000000, 0x00000000,
					 0x00000000, 0x00000000, 0x00000000, 0x00000000,
					 0x00000000 };

static struct
{
 union
 {
  struct
  {
   uint32 PIO_Base;	// 0x1f801000	// BIOS Init: 0x1f000000, Writeable bits: 0x00ffffff(assumed, verify), FixedOR = 0x1f000000
   uint32 Unknown0;	// 0x1f801004	// BIOS Init: 0x1f802000, Writeable bits: 0x00ffffff, FixedOR = 0x1f000000
   uint32 Unknown1;	// 0x1f801008	// BIOS Init: 0x0013243f, ????
   uint32 Unknown2;	// 0x1f80100c	// BIOS Init: 0x00003022, Writeable bits: 0x2f1fffff, FixedOR = 0x00000000
   
   uint32 BIOS_Mapping;	// 0x1f801010	// BIOS Init: 0x0013243f, ????
   uint32 SPU_Delay;	// 0x1f801014	// BIOS Init: 0x200931e1, Writeable bits: 0x2f1fffff, FixedOR = 0x00000000 - Affects bus timing on access to SPU
   uint32 CDC_Delay;	// 0x1f801018	// BIOS Init: 0x00020843, Writeable bits: 0x2f1fffff, FixedOR = 0x00000000
   uint32 Unknown4;	// 0x1f80101c	// BIOS Init: 0x00070777, ????
   uint32 Unknown5;	// 0x1f801020	// BIOS Init: 0x00031125(but rewritten with other values often), Writeable bits: 0x0003ffff, FixedOR = 0x00000000 -- Possibly CDC related
  };
  uint32 Regs[9];
 };
} SysControl;

static unsigned DMACycleSteal = 0;	// Doesn't need to be saved in save states, since it's recalculated in the ForceEventUpdates() call chain.

void PSX_SetDMACycleSteal(unsigned stealage)
{
 if(stealage > 200)	// Due to 8-bit limitations in the CPU core.
  stealage = 200;

 DMACycleSteal = stealage;
}

//
// Event stuff
//

static pscpu_timestamp_t Running;	// Set to -1 when not desiring exit, and 0 when we are.

struct event_list_entry
{
 uint32 which;
 pscpu_timestamp_t event_time;
 event_list_entry *prev;
 event_list_entry *next;
};

static event_list_entry events[PSX_EVENT__COUNT];

static void EventReset(void)
{
 for(unsigned i = 0; i < PSX_EVENT__COUNT; i++)
 {
  events[i].which = i;

  if(i == PSX_EVENT__SYNFIRST)
   events[i].event_time = 0;
  else if(i == PSX_EVENT__SYNLAST)
   events[i].event_time = 0x7FFFFFFF;
  else
   events[i].event_time = PSX_EVENT_MAXTS;

  events[i].prev = (i > 0) ? &events[i - 1] : NULL;
  events[i].next = (i < (PSX_EVENT__COUNT - 1)) ? &events[i + 1] : NULL;
 }
}

//static void RemoveEvent(event_list_entry *e)
//{
// e->prev->next = e->next;
// e->next->prev = e->prev;
//}

static void RebaseTS(const pscpu_timestamp_t timestamp)
{
 for(unsigned i = 0; i < PSX_EVENT__COUNT; i++)
 {
  if(i == PSX_EVENT__SYNFIRST || i == PSX_EVENT__SYNLAST)
   continue;

  assert(events[i].event_time > timestamp);
  events[i].event_time -= timestamp;
 }

 CPU->SetEventNT(events[PSX_EVENT__SYNFIRST].next->event_time);
}

void PSX_SetEventNT(const int type, const pscpu_timestamp_t next_timestamp)
{
 event_list_entry *e = &events[type];

 if(next_timestamp < e->event_time)
 {
  event_list_entry *fe = e;

  do
  {
   fe = fe->prev;
  }
  while(next_timestamp < fe->event_time);

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
 else if(next_timestamp > e->event_time)
 {
  event_list_entry *fe = e;

  do
  {
   fe = fe->next;
  } while(next_timestamp > fe->event_time);

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

 CPU->SetEventNT(events[PSX_EVENT__SYNFIRST].next->event_time & Running);
}

// Called from debug.cpp too.
void ForceEventUpdates(const pscpu_timestamp_t timestamp)
{
 PSX_SetEventNT(PSX_EVENT_GPU, GPU->Update(timestamp));
 PSX_SetEventNT(PSX_EVENT_CDC, CDC->Update(timestamp));

 PSX_SetEventNT(PSX_EVENT_TIMER, TIMER_Update(timestamp));

 PSX_SetEventNT(PSX_EVENT_DMA, DMA_Update(timestamp));

 PSX_SetEventNT(PSX_EVENT_FIO, FIO->Update(timestamp));

 CPU->SetEventNT(events[PSX_EVENT__SYNFIRST].next->event_time);
}

bool PSX_EventHandler(const pscpu_timestamp_t timestamp)
{
 event_list_entry *e = events[PSX_EVENT__SYNFIRST].next;

 while(timestamp >= e->event_time)	// If Running = 0, PSX_EventHandler() may be called even if there isn't an event per-se, so while() instead of do { ... } while
 {
  event_list_entry *prev = e->prev;
  pscpu_timestamp_t nt;

  switch(e->which)
  {
   default: abort();

   case PSX_EVENT_GPU:
	nt = GPU->Update(e->event_time);
	break;

   case PSX_EVENT_CDC:
	nt = CDC->Update(e->event_time);
	break;

   case PSX_EVENT_TIMER:
	nt = TIMER_Update(e->event_time);
	break;

   case PSX_EVENT_DMA:
	nt = DMA_Update(e->event_time);
	break;

   case PSX_EVENT_FIO:
	nt = FIO->Update(e->event_time);
	break;
  }
#if PSX_EVENT_SYSTEM_CHECKS
  assert(nt > e->event_time);
#endif

  PSX_SetEventNT(e->which, nt);

  // Order of events can change due to calling PSX_SetEventNT(), this prev business ensures we don't miss an event due to reordering.
  e = prev->next;
 }

 return(Running);
}


void PSX_RequestMLExit(void)
{
 Running = 0;
 CPU->SetEventNT(0);
}


//
// End event stuff
//


// Remember to update MemPeek<>() and MemPoke<>() when we change address decoding in MemRW()
template<typename T, bool IsWrite, bool Access24> static INLINE void MemRW(pscpu_timestamp_t &timestamp, uint32 A, uint32 &V)
{
 #if 0
 if(IsWrite)
  printf("Write%d: %08x(orig=%08x), %08x\n", (int)(sizeof(T) * 8), A & mask[A >> 29], A, V);
 else
  printf("Read%d: %08x(orig=%08x)\n", (int)(sizeof(T) * 8), A & mask[A >> 29], A);
 #endif

 if(!IsWrite)
  timestamp += DMACycleSteal;

 if(A < 0x00800000)
 {
  if(IsWrite)
  {
   //timestamp++;	// Best-case timing.
  }
  else
  {
   timestamp += 3;
  }

  if(Access24)
  {
   if(IsWrite)
    MainRAM.WriteU24(A & 0x1FFFFF, V);
   else
    V = MainRAM.ReadU24(A & 0x1FFFFF);
  }
  else
  {
   if(IsWrite)
    MainRAM.Write<T>(A & 0x1FFFFF, V);
   else
    V = MainRAM.Read<T>(A & 0x1FFFFF);
  }

  return;
 }

 if(A >= 0x1FC00000 && A <= 0x1FC7FFFF)
 {
  if(!IsWrite)
  {
   if(Access24)
    V = BIOSROM->ReadU24(A & 0x7FFFF);
   else
    V = BIOSROM->Read<T>(A & 0x7FFFF);
  }

  return;
 }

 if(timestamp >= events[PSX_EVENT__SYNFIRST].next->event_time)
  PSX_EventHandler(timestamp);

 if(A >= 0x1F801000 && A <= 0x1F802FFF)
 {
  //if(IsWrite)
  // printf("HW Write%d: %08x %08x\n", (unsigned int)(sizeof(T)*8), (unsigned int)A, (unsigned int)V);
  //else
  // printf("HW Read%d: %08x\n", (unsigned int)(sizeof(T)*8), (unsigned int)A);

  if(A >= 0x1F801C00 && A <= 0x1F801FFF) // SPU
  {
   if(sizeof(T) == 4 && !Access24)
   {
    if(IsWrite)
    {
     //timestamp += 15;

     //if(timestamp >= events[PSX_EVENT__SYNFIRST].next->event_time)
     // PSX_EventHandler(timestamp);

     SPU->Write(timestamp, A | 0, V);
     SPU->Write(timestamp, A | 2, V >> 16);
    }
    else
    {
     timestamp += 36;

     if(timestamp >= events[PSX_EVENT__SYNFIRST].next->event_time)
      PSX_EventHandler(timestamp);

		 //0.9.36.5 - clarified read order by turning into two statements
     V = SPU->Read(timestamp, A);
     V |= SPU->Read(timestamp, A | 2) << 16;
    }
   }
   else
   {
    if(IsWrite)
    {
     //timestamp += 8;

     //if(timestamp >= events[PSX_EVENT__SYNFIRST].next->event_time)
     // PSX_EventHandler(timestamp);

     SPU->Write(timestamp, A & ~1, V);
    }
    else
    {
     timestamp += 16; // Just a guess, need to test.

     if(timestamp >= events[PSX_EVENT__SYNFIRST].next->event_time)
      PSX_EventHandler(timestamp);

     V = SPU->Read(timestamp, A & ~1);
    }
   }
   return;
  }		// End SPU


  // CDC: TODO - 8-bit access.
  if(A >= 0x1f801800 && A <= 0x1f80180F)
  {
   if(!IsWrite) 
   {
    timestamp += 6 * sizeof(T); //24;
   }

   if(IsWrite)
    CDC->Write(timestamp, A & 0x3, V);
   else
    V = CDC->Read(timestamp, A & 0x3);

   return;
  }

  if(A >= 0x1F801810 && A <= 0x1F801817)
  {
   if(!IsWrite)
    timestamp++;

   if(IsWrite)
    GPU->Write(timestamp, A, V);
   else
    V = GPU->Read(timestamp, A);

   return;
  }

  if(A >= 0x1F801820 && A <= 0x1F801827)
  {
   if(!IsWrite)
    timestamp++;

	 if (IsWrite)
	 {
		 if (A == 0x1F801820)
		 {
			 //per pcsx-rr:
			 GpuFrameForLag = true;
		 }
		 MDEC_Write(timestamp, A, V);
	 }
   else
    V = MDEC_Read(timestamp, A);

   return;
  }

  if(A >= 0x1F801000 && A <= 0x1F801023)
  {
   unsigned index = (A & 0x1F) >> 2;

   if(!IsWrite)
    timestamp++;

   //if(A == 0x1F801014 && IsWrite)
   // fprintf(stderr, "%08x %08x\n",A,V);

   if(IsWrite)
   {
    V <<= (A & 3) * 8;
    SysControl.Regs[index] = V & SysControl_Mask[index];
   }
   else
   {
    V = SysControl.Regs[index] | SysControl_OR[index];
    V >>= (A & 3) * 8;
   }
   return;
  }

  if(A >= 0x1F801040 && A <= 0x1F80104F)
  {
   if(!IsWrite)
    timestamp++;

   if(IsWrite)
    FIO->Write(timestamp, A, V);
   else
    V = FIO->Read(timestamp, A);
   return;
  }

  if(A >= 0x1F801050 && A <= 0x1F80105F)
  {
   if(!IsWrite)
    timestamp++;

#if 0
   if(IsWrite)
   {
    PSX_WARNING("[SIO] Write: 0x%08x 0x%08x %u", A, V, (unsigned)sizeof(T));
   }
   else
   {
    PSX_WARNING("[SIO] Read: 0x%08x", A);
   }
#endif

   if(IsWrite)
    SIO_Write(timestamp, A, V);
   else
    V = SIO_Read(timestamp, A);
   return;
  }

#if 0
  if(A >= 0x1F801060 && A <= 0x1F801063)
  {
   if(IsWrite)
   {

   }
   else
   {

   }

   return;
  }
#endif

  if(A >= 0x1F801070 && A <= 0x1F801077)	// IRQ
  {
   if(!IsWrite)
    timestamp++;

   if(IsWrite)
    IRQ_Write(A, V);
   else
    V = IRQ_Read(A);
   return;
  }

  if(A >= 0x1F801080 && A <= 0x1F8010FF) 	// DMA
  {
   if(!IsWrite)
    timestamp++;

   if(IsWrite)
    DMA_Write(timestamp, A, V);
   else
    V = DMA_Read(timestamp, A);

   return;
  }

  if(A >= 0x1F801100 && A <= 0x1F80113F)	// Root counters
  {
   if(!IsWrite)
    timestamp++;

   if(IsWrite)
    TIMER_Write(timestamp, A, V);
   else
    V = TIMER_Read(timestamp, A);

   return;
  }
 }


 if(A >= 0x1F000000 && A <= 0x1F7FFFFF)
 {
  if(!IsWrite)
  {
   //if((A & 0x7FFFFF) <= 0x84)
   //PSX_WARNING("[PIO] Read%d from 0x%08x at time %d", (int)(sizeof(T) * 8), A, timestamp);

   V = ~0U;	// A game this affects:  Tetris with Cardcaptor Sakura

   if(PIOMem)
   {
    if((A & 0x7FFFFF) < 65536)
    {
     if(Access24)
      V = PIOMem->ReadU24(A & 0x7FFFFF);
     else
      V = PIOMem->Read<T>(A & 0x7FFFFF);
    }
    else if((A & 0x7FFFFF) < (65536 + TextMem.size()))
    {
     if(Access24)
      V = MDFN_de24lsb(&TextMem[(A & 0x7FFFFF) - 65536]);
     else switch(sizeof(T))
     {
      case 1: V = TextMem[(A & 0x7FFFFF) - 65536]; break;
      case 2: V = MDFN_de16lsb<false>(&TextMem[(A & 0x7FFFFF) - 65536]); break;
      case 4: V = MDFN_de32lsb<false>(&TextMem[(A & 0x7FFFFF) - 65536]); break;
     }
    }
   }
  }
  return;
 }

 if(A == 0xFFFE0130) // Per tests on PS1, ignores the access(sort of, on reads the value is forced to 0 if not aligned) if not aligned to 4-bytes.
 {
  if(!IsWrite)
   V = CPU->GetBIU();
  else
   CPU->SetBIU(V);

  return;
 }

 if(IsWrite)
 {
  PSX_WARNING("[MEM] Unknown write%d to %08x at time %d, =%08x(%d)", (int)(sizeof(T) * 8), A, timestamp, V, V);
 }
 else
 {
  V = 0;
  PSX_WARNING("[MEM] Unknown read%d from %08x at time %d", (int)(sizeof(T) * 8), A, timestamp);
 }
}

void  PSX_MemWrite8(pscpu_timestamp_t timestamp, uint32 A, uint32 V)
{
 MemRW<uint8, true, false>(timestamp, A, V);
}

void  PSX_MemWrite16(pscpu_timestamp_t timestamp, uint32 A, uint32 V)
{
 MemRW<uint16, true, false>(timestamp, A, V);
}

void  PSX_MemWrite24(pscpu_timestamp_t timestamp, uint32 A, uint32 V)
{
 MemRW<uint32, true, true>(timestamp, A, V);
}

void  PSX_MemWrite32(pscpu_timestamp_t timestamp, uint32 A, uint32 V)
{
 MemRW<uint32, true, false>(timestamp, A, V);
}

uint8  PSX_MemRead8(pscpu_timestamp_t &timestamp, uint32 A)
{
 uint32 V;

 MemRW<uint8, false, false>(timestamp, A, V);

 return(V);
}

uint16  PSX_MemRead16(pscpu_timestamp_t &timestamp, uint32 A)
{
 uint32 V;

 MemRW<uint16, false, false>(timestamp, A, V);

 return(V);
}

uint32  PSX_MemRead24(pscpu_timestamp_t &timestamp, uint32 A)
{
 uint32 V;

 MemRW<uint32, false, true>(timestamp, A, V);

 return(V);
}

uint32  PSX_MemRead32(pscpu_timestamp_t &timestamp, uint32 A)
{
 uint32 V;

 MemRW<uint32, false, false>(timestamp, A, V);

 return(V);
}

template<typename T, bool Access24> static INLINE uint32 MemPeek(pscpu_timestamp_t timestamp, uint32 A)
{
 if(A < 0x00800000)
 {
  if(Access24)
   return(MainRAM.ReadU24(A & 0x1FFFFF));
  else
   return(MainRAM.Read<T>(A & 0x1FFFFF));
 }

 if(A >= 0x1FC00000 && A <= 0x1FC7FFFF)
 {
  if(Access24)
   return(BIOSROM->ReadU24(A & 0x7FFFF));
  else
   return(BIOSROM->Read<T>(A & 0x7FFFF));
 }

 if(A >= 0x1F801000 && A <= 0x1F802FFF)
 {
  if(A >= 0x1F801C00 && A <= 0x1F801FFF) // SPU
  {
   // TODO

  }		// End SPU


  // CDC: TODO - 8-bit access.
  if(A >= 0x1f801800 && A <= 0x1f80180F)
  {
   // TODO

  }

  if(A >= 0x1F801810 && A <= 0x1F801817)
  {
   // TODO

  }

  if(A >= 0x1F801820 && A <= 0x1F801827)
  {
   // TODO

  }

  if(A >= 0x1F801000 && A <= 0x1F801023)
  {
   unsigned index = (A & 0x1F) >> 2;
   return((SysControl.Regs[index] | SysControl_OR[index]) >> ((A & 3) * 8));
  }

  if(A >= 0x1F801040 && A <= 0x1F80104F)
  {
   // TODO

  }

  if(A >= 0x1F801050 && A <= 0x1F80105F)
  {
   // TODO

  }


  if(A >= 0x1F801070 && A <= 0x1F801077)	// IRQ
  {
   // TODO

  }

  if(A >= 0x1F801080 && A <= 0x1F8010FF) 	// DMA
  {
   // TODO

  }

  if(A >= 0x1F801100 && A <= 0x1F80113F)	// Root counters
  {
   // TODO

  }
 }


 if(A >= 0x1F000000 && A <= 0x1F7FFFFF)
 {
  if(PIOMem)
  {
   if((A & 0x7FFFFF) < 65536)
   {
    if(Access24)
     return(PIOMem->ReadU24(A & 0x7FFFFF));
    else
     return(PIOMem->Read<T>(A & 0x7FFFFF));
   }
   else if((A & 0x7FFFFF) < (65536 + TextMem.size()))
   {
    if(Access24)
     return(MDFN_de24lsb(&TextMem[(A & 0x7FFFFF) - 65536]));
    else switch(sizeof(T))
    {
     case 1: return(TextMem[(A & 0x7FFFFF) - 65536]); break;
     case 2: return(MDFN_de16lsb<false>(&TextMem[(A & 0x7FFFFF) - 65536])); break;
     case 4: return(MDFN_de32lsb<false>(&TextMem[(A & 0x7FFFFF) - 65536])); break;
    }
   }
  }
  return(~0U);
 }

 if(A == 0xFFFE0130)
  return CPU->GetBIU();

 return(0);
}

uint8 PSX_MemPeek8(uint32 A)
{
 return MemPeek<uint8, false>(0, A);
}

uint16 PSX_MemPeek16(uint32 A)
{
 return MemPeek<uint16, false>(0, A);
}

uint32 PSX_MemPeek32(uint32 A)
{
 return MemPeek<uint32, false>(0, A);
}

template<typename T, bool Access24> static INLINE void MemPoke(pscpu_timestamp_t timestamp, uint32 A, T V)
{
 if(A < 0x00800000)
 {
  if(Access24)
   MainRAM.WriteU24(A & 0x1FFFFF, V);
  else
   MainRAM.Write<T>(A & 0x1FFFFF, V);

  return;
 }

 if(A >= 0x1FC00000 && A <= 0x1FC7FFFF)
 {
  if(Access24)
   BIOSROM->WriteU24(A & 0x7FFFF, V);
  else
   BIOSROM->Write<T>(A & 0x7FFFF, V);

  return;
 }

 if(A >= 0x1F801000 && A <= 0x1F802FFF)
 {
  if(A >= 0x1F801000 && A <= 0x1F801023)
  {
   unsigned index = (A & 0x1F) >> 2;
   SysControl.Regs[index] = (V << ((A & 3) * 8)) & SysControl_Mask[index];
   return;
  }
 }

 if(A == 0xFFFE0130)
 {
  CPU->SetBIU(V);
  return;
 }
}

void PSX_MemPoke8(uint32 A, uint8 V)
{
 MemPoke<uint8, false>(0, A, V);
}

void PSX_MemPoke16(uint32 A, uint16 V)
{
 MemPoke<uint16, false>(0, A, V);
}

void PSX_MemPoke32(uint32 A, uint32 V)
{
 MemPoke<uint32, false>(0, A, V);
}

static void PSX_Power(bool powering_up)
{
 PSX_PRNG.ResetState();	// Should occur first!

 memset(MainRAM.data8, 0, 2048 * 1024);

 for(unsigned i = 0; i < 9; i++)
  SysControl.Regs[i] = 0;

 CPU->Power();

 EventReset();

 TIMER_Power();

 DMA_Power();

 FIO->Reset(powering_up);
 SIO_Power();

 MDEC_Power();
 CDC->Power();
 GPU->Power();
 //SPU->Power();	// Called from CDC->Power()
 IRQ_Power();

 ForceEventUpdates(0);

 deint.ClearState();
}


void PSX_GPULineHook(const pscpu_timestamp_t timestamp, const pscpu_timestamp_t line_timestamp, bool vsync, uint32 *pixels, const MDFN_PixelFormat* const format, const unsigned width, const unsigned pix_clock_offset, const unsigned pix_clock, const unsigned pix_clock_divider)
{
 FIO->GPULineHook(timestamp, line_timestamp, vsync, pixels, format, width, pix_clock_offset, pix_clock, pix_clock_divider);
}

}

using namespace MDFN_IEN_PSX;

struct ShockConfig
{
	//// multires is a hint that, if set, indicates that the system has fairly programmable video modes(particularly, the ability
	//// to display multiple horizontal resolutions, such as the PCE, PC-FX, or Genesis).  In practice, it will cause the driver
	//// code to set the linear interpolation on by default. (TRUE for psx)
	//// lcm_width and lcm_height are the least common multiples of all possible
	//// resolutions in the frame buffer as specified by DisplayRect/LineWidths(Ex for PCE: widths of 256, 341.333333, 512,
	//// lcm = 1024)
	//// nominal_width and nominal_height specify the resolution that Mednafen should display
	//// the framebuffer image in at 1x scaling, scaled from the dimensions of DisplayRect, and optionally the LineWidths array
	//// passed through espec to the Emulate() function.
	//int lcm_width;
	//int lcm_height;
	//int nominal_width;
	//int nominal_height;
	int fb_width;		// Width of the framebuffer(not necessarily width of the image).  MDFN_Surface width should be >= this.
	int fb_height;		// Height of the framebuffer passed to the Emulate() function(not necessarily height of the image)

	//last used render options
	ShockRenderOptions opts;
} s_ShockConfig;


struct ShockState
{
	bool power;
	bool eject;
} s_ShockState;


struct ShockPeripheral
{
	ePeripheralType type;
	u8 buffer[32]; //must be larger than 16+3+1 or thereabouts because the dualshock writes some rumble data into it. bleck, ill fix it later
};

struct {

	//This is kind of redundant with the frontIO code, and should be merged with it eventually, when the configurability gets more advanced

	ShockPeripheral ports[2];

	void Initialize()
	{
		for(int i=0;i<2;i++)
		{
			ports[i].type = ePeripheralType_None;
			memset(ports[i].buffer,0,sizeof(ports[i].buffer));
		}
	}

	s32 Connect(s32 address, s32 type)
	{
		//check the port address
		int portnum = address&0x0F;
		if(portnum != 1 && portnum != 2)
			return SHOCK_INVALID_ADDRESS;
		portnum--;

		//check whats already there
		if(ports[portnum].type == ePeripheralType_None && type == ePeripheralType_None) return SHOCK_OK; //NOP
		if(ports[portnum].type != ePeripheralType_None && type != ePeripheralType_None) return SHOCK_NOCANDO; //cant re-connect something without disconnecting first

		//disconnecting:
		if(type == ePeripheralType_None) {
			ports[portnum].type = ePeripheralType_None;
			memset(ports[portnum].buffer,0,sizeof(ports[portnum].buffer));
			FIO->SetInput(portnum, "none", ports[portnum].buffer);
			return SHOCK_OK;
		}

		//connecting:
		const char* name = NULL;
		switch(type)
		{
		case ePeripheralType_Pad: name = "gamepad"; break;
		case ePeripheralType_DualShock: name = "dualshock"; break;
		case ePeripheralType_DualAnalog: name = "dualanalog"; break;
		default:
			return SHOCK_ERROR;
		}
		ports[portnum].type = (ePeripheralType)type;
		memset(ports[portnum].buffer,0,sizeof(ports[portnum].buffer));
		FIO->SetInput(portnum, name, ports[portnum].buffer);

		return SHOCK_OK;
	}

	s32 PollActive(s32 address, bool clear)
	{
		//check the port address
		int portnum = address&0x0F;
		if(portnum != 1 && portnum != 2)
			return SHOCK_INVALID_ADDRESS;
		portnum--;

		s32 ret = SHOCK_FALSE;

		u8* buf = ports[portnum].buffer;
		switch(ports[portnum].type)
		{
		case ePeripheralType_DualShock:
			{
				IO_Dualshock* io_dualshock = (IO_Dualshock*)buf;
				if(io_dualshock->active) ret = SHOCK_TRUE;
				if(clear) io_dualshock->active = 0;
				return ret;
				break;
			}
		case ePeripheralType_DualAnalog:
			{
				IO_DualAnalog* io_dualanalog = (IO_DualAnalog*)buf;
				if(io_dualanalog->active) ret = SHOCK_TRUE;
				if(clear) io_dualanalog->active = 0;
				return ret;
				break;
			}
		case ePeripheralType_Pad:
			{
				IO_Gamepad* io_gamepad = (IO_Gamepad*)buf;
				if(io_gamepad->active) ret = SHOCK_TRUE;
				if(clear) io_gamepad->active = 0;
				return ret;
				break;
			}

		case ePeripheralType_None:
			return SHOCK_NOCANDO;

		default:
			return SHOCK_ERROR;
		}
	}

	s32 SetPadInput(s32 address, u32 buttons, u8 left_x, u8 left_y, u8 right_x, u8 right_y)
	{
		//check the port address
		int portnum = address&0x0F;
		if(portnum != 1 && portnum != 2)
			return SHOCK_INVALID_ADDRESS;
		portnum--;

		u8* buf = ports[portnum].buffer;
		switch(ports[portnum].type)
		{
		case ePeripheralType_DualShock:
			{
				IO_Dualshock* io_dualshock = (IO_Dualshock*)buf;
				io_dualshock->buttons[0] = (buttons>>0)&0xFF;
				io_dualshock->buttons[1] = (buttons>>8)&0xFF;
				io_dualshock->buttons[2] = (buttons>>16)&0xFF; //this is only the analog mode button
				io_dualshock->right_x = right_x;
				io_dualshock->right_y = right_y;
				io_dualshock->left_x = left_x;
				io_dualshock->left_y = left_y;
				return SHOCK_OK;
			}
		case ePeripheralType_Pad:
			{
				IO_Gamepad* io_gamepad = (IO_Gamepad*)buf;
				io_gamepad->buttons[0] = (buttons>>0)&0xFF;
				io_gamepad->buttons[1] = (buttons>>8)&0xFF;
				return SHOCK_OK;
			}
		case ePeripheralType_DualAnalog:
			{
				IO_DualAnalog* io_dualanalog = (IO_DualAnalog*)buf;
				io_dualanalog->buttons[0] = (buttons>>0)&0xFF;
				io_dualanalog->buttons[1] = (buttons>>8)&0xFF;
				io_dualanalog->right_x = right_x;
				io_dualanalog->right_y = right_y;
				io_dualanalog->left_x = left_x;
				io_dualanalog->left_y = left_y;
				return SHOCK_OK;
			}
		
		default:
			return SHOCK_ERROR;
		}
	}

	s32 MemcardTransact(s32 address, ShockMemcardTransaction* transaction)
	{
		//check the port address
		int portnum = address&1;
		if(portnum != 1 && portnum != 2)
			return SHOCK_INVALID_ADDRESS;
		portnum--;

		//TODO - once we get flexible here, do some extra condition checks.. whether memcards exist, etc. much like devices.
		switch(transaction->transaction)
		{
			case eShockMemcardTransaction_Connect: 
				//cant connect when a memcard is already connected
				if(!strcmp(FIO->MCPorts[portnum]->GetName(),"InputDevice_Memcard"))
					return SHOCK_NOCANDO;
				delete FIO->MCPorts[portnum]; //delete dummy
				FIO->MCPorts[portnum] = Device_Memcard_Create();
			
			case eShockMemcardTransaction_Disconnect: 
				return SHOCK_ERROR; //not supported yet

			case eShockMemcardTransaction_Write:
				FIO->MCPorts[portnum]->WriteNV((uint8*)transaction->buffer128k,0,128*1024);
				FIO->MCPorts[portnum]->ResetNVDirtyCount();
				return SHOCK_OK;

			case eShockMemcardTransaction_Read:
			{
				const u8* ptr = FIO->MCPorts[portnum]->ReadNV();
				memcpy(transaction->buffer128k,ptr,128*1024);
				FIO->MCPorts[portnum]->ResetNVDirtyCount();
				return SHOCK_OK;
			}

			case eShockMemcardTransaction_CheckDirty:
				if(FIO->GetMemcardDirtyCount(portnum))
					return SHOCK_TRUE;
				else return SHOCK_FALSE;

			default:
				return SHOCK_ERROR;
		}
	}

} s_ShockPeripheralState;

EW_EXPORT s32 shock_Peripheral_Connect(void* psx, s32 address, s32 type)
{
	return s_ShockPeripheralState.Connect(address, type);
}

EW_EXPORT s32 shock_Peripheral_SetPadInput(void* psx, s32 address, u32 buttons, u8 left_x, u8 left_y, u8 right_x, u8 right_y)
{
	return s_ShockPeripheralState.SetPadInput(address, buttons, left_x, left_y, right_x, right_y);
}

EW_EXPORT s32 shock_Peripheral_PollActive(void* psx, s32 address, s32 clear)
{
	return s_ShockPeripheralState.PollActive(address, clear!=SHOCK_FALSE);
}

EW_EXPORT s32 shock_Peripheral_MemcardTransact(void* psx, s32 address, ShockMemcardTransaction* transaction)
{
	return s_ShockPeripheralState.MemcardTransact(address, transaction);
}

static void MountCPUAddressSpace()
{
	for(uint32 ma = 0x00000000; ma < 0x00800000; ma += 2048 * 1024)
	{
		CPU->SetFastMap(MainRAM.data8, 0x00000000 + ma, 2048 * 1024);
		CPU->SetFastMap(MainRAM.data8, 0x80000000 + ma, 2048 * 1024);
		CPU->SetFastMap(MainRAM.data8, 0xA0000000 + ma, 2048 * 1024);
	}

	CPU->SetFastMap(BIOSROM->data8, 0x1FC00000, 512 * 1024);
	CPU->SetFastMap(BIOSROM->data8, 0x9FC00000, 512 * 1024);
	CPU->SetFastMap(BIOSROM->data8, 0xBFC00000, 512 * 1024);

	if(PIOMem)
	{
		CPU->SetFastMap(PIOMem->data8, 0x1F000000, 65536);
		CPU->SetFastMap(PIOMem->data8, 0x9F000000, 65536);
		CPU->SetFastMap(PIOMem->data8, 0xBF000000, 65536);
	}
}

static MDFN_Surface *VTBuffer[2] = { NULL, NULL };
static int *VTLineWidths[2] = { NULL, NULL };
static bool s_FramebufferNormalized;
static int s_FramebufferCurrent;
static int s_FramebufferCurrentWidth;

EW_EXPORT s32 shock_Create(void** psx, s32 region, void* firmware512k)
{
	//TODO
 //psx_dbg_level = MDFN_GetSettingUI("psx.dbg_level");
 //DBG_Init();
	
	*psx = NULL;

	//PIO Mem: why wouldn't we want this?
	static const bool WantPIOMem = true;

	BIOSROM = new MultiAccessSizeMem<512 * 1024, false>();
	memcpy(BIOSROM->data8, firmware512k, 512 * 1024);

	if(WantPIOMem) PIOMem = new MultiAccessSizeMem<65536, false>();
	else PIOMem = NULL;

	CPU = new PS_CPU();
	SPU = new PS_SPU();
	CDC = new PS_CDC();
	DMA_Init();

	//these steps can't be done without more information
	GPU = new PS_GPU(region == REGION_EU);

	//setup gpu output surfaces
	MDFN_PixelFormat nf(MDFN_COLORSPACE_RGB, 16, 8, 0, 24);
	for(int i=0;i<2;i++)
	{
		VTBuffer[i] = new MDFN_Surface(NULL, FB_WIDTH, FB_HEIGHT, FB_WIDTH, nf);
		VTLineWidths[i] = (int *)calloc(FB_HEIGHT, sizeof(int));
	}

	FIO = new FrontIO();
	s_ShockPeripheralState.Initialize();

	MountCPUAddressSpace();

	s_ShockState.power = false;
	s_ShockState.eject = false;

	//do we need to do anything particualr with the CDC disc/tray state? survey says... no.

	return SHOCK_OK;
}

EW_EXPORT s32 shock_Destroy(void* psx)
{
	//TODO
	return SHOCK_OK;
}

//Sets the power to ON. It is an error to turn an already-on console ON again
EW_EXPORT s32 shock_PowerOn(void* psx)
{
	if(s_ShockState.power) return SHOCK_NOCANDO;

	s_ShockState.power = true;	
	PSX_Power(true);

	return SHOCK_OK;
}

//Triggers a soft reset immediately. Returns SHOCK_NOCANDO if console is powered off.
EW_EXPORT s32 shock_SoftReset(void *psx)
{
	if (!s_ShockState.power) return SHOCK_NOCANDO;

	PSX_Power(false);

	return SHOCK_OK;
}

//Sets the power to OFF. It is an error to turn an already-off console OFF again
EW_EXPORT s32 shock_PowerOff(void* psx)
{
	if(!s_ShockState.power) return SHOCK_NOCANDO;

	//not supported yet
	return SHOCK_ERROR;
}

EW_EXPORT s32 shock_Step(void* psx, eShockStep step)
{
	//only eShockStep_Frame is supported

	pscpu_timestamp_t timestamp = 0;

	memset(&espec, 0, sizeof(EmulateSpecStruct));

	espec.VideoFormatChanged = true; //shouldnt do this every frame..
	espec.surface = (MDFN_Surface *)VTBuffer[VTBackBuffer];
	espec.LineWidths = (int *)VTLineWidths[VTBackBuffer];
	espec.skip = false;
	espec.soundmultiplier = 1.0;
	espec.NeedRewind = false;

	espec.MasterCycles = 0;

	espec.SoundBufMaxSize = 1024*1024;
	espec.SoundRate = 44100;
	espec.SoundBuf = soundbuf;
	espec.SoundBufSize = 0;
	espec.SoundVolume = 1.0;

	//not sure about this
	espec.skip = s_ShockConfig.opts.skip;

	if (s_ShockConfig.opts.deinterlaceMode == eShockDeinterlaceMode_Weave)
		deint.SetType(Deinterlacer::DEINT_WEAVE);
	if (s_ShockConfig.opts.deinterlaceMode == eShockDeinterlaceMode_Bob)
		deint.SetType(Deinterlacer::DEINT_BOB);
	if (s_ShockConfig.opts.deinterlaceMode == eShockDeinterlaceMode_BobOffset)
		deint.SetType(Deinterlacer::DEINT_BOB_OFFSET);

	//-------------------------

	FIO->UpdateInput();
	
	//GPU->StartFrame(psf_loader ? NULL : espec); //a reminder that when we do psf, we will be telling the gpu not to draw
	GPU->StartFrame(&espec);
	
	//not that it matters, but we may need to control this at some point
	static const int ResampleQuality = 5;
	SPU->StartFrame(espec.SoundRate, ResampleQuality); 

	GpuFrameForLag = false;

	Running = -1;
	timestamp = CPU->Run(timestamp, psx_dbg_level >= PSX_DBG_BIOS_PRINT, /*psf_loader != NULL*/ false); //huh?
	assert(timestamp);

	ForceEventUpdates(timestamp);
	if(GPU->GetScanlineNum() < 100)
		printf("[BUUUUUUUG] Frame timing end glitch; scanline=%u, st=%u\n", GPU->GetScanlineNum(), timestamp);

	espec.SoundBufSize = SPU->EndFrame(espec.SoundBuf);

	CDC->ResetTS();
	TIMER_ResetTS();
	DMA_ResetTS();
	GPU->ResetTS();
	FIO->ResetTS();

	RebaseTS(timestamp);

	espec.MasterCycles = timestamp;

	//(memcard saving happened here)

	//----------------------

	VTDisplayRects[VTBackBuffer] = espec.DisplayRect;

	//if interlacing is active, do that processing now
	if(espec.InterlaceOn)
	{
		if(!PrevInterlaced)
			deint.ClearState();

		deint.Process(espec.surface, espec.DisplayRect, espec.LineWidths, espec.InterlaceField);

		PrevInterlaced = true;

		espec.InterlaceOn = false;
		espec.InterlaceField = 0;
	}

	//new frame, hasnt been normalized
	s_FramebufferNormalized = false;
	s_FramebufferCurrent = 0;
	s_FramebufferCurrentWidth = FB_WIDTH;

	//just in case we debug printed or something like that
	fflush(stdout);
	fflush(stderr);


	return SHOCK_OK;
}

struct FramebufferCropInfo
{
	int width, height, xo, yo;
};

static void _shock_AnalyzeFramebufferCropInfo(int fbIndex, FramebufferCropInfo* info)
{
	//presently, except for contrived test programs, it is safe to assume this is the same for the entire frame (no known use by games)
	//however, due to the dump_framebuffer, it may be incorrect at scanline 0. so lets use another one for the heuristic here
	//you'd think we could use FirstLine instead of kScanlineWidthHeuristicIndex, but sometimes it hasnt been set (screen off) so it's confusing
	int width = VTLineWidths[fbIndex][kScanlineWidthHeuristicIndex];
	int height = espec.DisplayRect.h;
	int yo = espec.DisplayRect.y;

	//fix a common error here from disabled screens (?)
	//I think we're lucky in selecting these lines kind of randomly. need a better plan.
	if (width <= 0) width = VTLineWidths[fbIndex][0];

	if (s_ShockConfig.opts.renderType == eShockRenderType_Framebuffer)
	{
		//printf("%d %d %d %d | %d | %d\n",yo,height, GPU->GetVertStart(), GPU->GetVertEnd(), espec.DisplayRect.y, GPU->FirstLine);

		height = GPU->GetVertEnd() - GPU->GetVertStart();
		yo = GPU->FirstLine;

		if (espec.DisplayRect.h == 288 || espec.DisplayRect.h == 240)
		{
		}
		else
		{
			height *= 2;
			//only return even scanlines to avoid bouncing the interlacing
			if (yo & 1) yo--;
		}

		//this can happen when the display turns on mid-frame
		//maybe an off by one error here..?
		if (yo + height >= espec.DisplayRect.h)
			yo = espec.DisplayRect.h - height;

		//sometimes when changing modes we have trouble..?
		if (yo<0) yo = 0;
	}

	info->width = width;
	info->height = height;
	info->xo = 0;
	info->yo = yo;
}


//`normalizes` the framebuffer to 700x480 (or 800x576 for PAL) by pixel doubling and wrecking the AR a little bit as needed
void NormalizeFramebuffer()
{
	//mednafen's advised solution for smooth gaming: "scale the output width to z * nominal_width, and the output height to z * nominal_height, where nominal_width and nominal_height are members of the MDFNGI struct"
	//IOW, mednafen's strategy is to put everything in a 320x240 and scale it up 3x to 960x720 by default (which is adequate to contain the largest PSX framebuffer of 700x480)
	
	//psxtech says horizontal resolutions can be:  256, 320, 512, 640, 368 pixels
	//mednafen will turn those into 2800/{ 10, 8, 5, 4, 7 } -> 280,350,560,700,400
	//additionally with the crop options we can cut it down by 160/X -> { 16, 20, 32, 40, 22 } -> { 264, 330, 528, 660, 378 }
	//this means our virtual area for doubling is no longer 800 but 756

	//heres my strategy: 
	//try to do the smart thing, try to get aspect ratio near the right value
	//intended AR = 320/240 = 1.3333
	//280x240 - ok (AR 1.1666666666666666666666666666667)
	//350x240 - ok (AR 1.4583333333333333333333333333333)
	//400x240 - ok (AR 1.6666666666666666666666666666667)
	//560x240 - scale vertically by 2 = 560x480 ~ 280x240
	//700x240 - scale vertically by 2 = 700x480 ~ 350x240
	//280x480 - scale horizontally by 2 = 560x480 ~ 280x240
	//350x480 - scale horizontally by 2 = 700x480 ~ 350x240
	//400x480 - scale horizontally by 2 = 800x480 ~ 400x240
	//560x480 - ok ~ 280x240
	//700x480 - ok ~ 350x240

	//NOTE: this approach is very redundant with the displaymanager AR tracking stuff
	//however, it will help us avoid stressing the displaymanager (for example, a 700x240 will freak it out kind of. we could send it a much more sensible 700x480)

	//always fetch description
	FramebufferCropInfo cropInfo;
	_shock_AnalyzeFramebufferCropInfo(0, &cropInfo);
	int width = cropInfo.width;
	int height = cropInfo.height;

	int virtual_width = 800;
	int virtual_height = 480;
	if (GPU->HardwarePALType)
		virtual_height = 576;

	if (s_ShockConfig.opts.renderType == eShockRenderType_ClipOverscan)
		virtual_width = 756;
	if (s_ShockConfig.opts.renderType == eShockRenderType_Framebuffer)
	{
		//not quite sure what to here yet
		//virtual_width = width * 2; ?
		virtual_width = 736;
	}

	int xs=1,ys=1;

	//I. as described above
	//if(width == 280 && height == 240) {}
	//if(width == 350 && height == 240) {}
	//if(width == 400 && height == 240) {}
	//if(width == 560 && height == 240) ys=2;
	//if(width == 700 && height == 240) ys=2;
	//if(width == 280 && height == 480) xs=2;
	//if(width == 350 && height == 480) xs=2;
	//if(width == 400 && height == 480) xs=2;
	//if(width == 560 && height == 480) {}
	//if(width == 700 && height == 480) {}

	//II. as the snes 'always double size framebuffer'. I think thats a better idea, and we already have the concept
	//ORIGINALLY (as of r8528 when PAL support was added) a threshold of 276 was used. I'm not sure where that came from.
	//288 seems to be a more correct value? (it's the typical PAL half resolution, corresponding to 240 for NTSC)
	//maybe I meant to type 576, but that doesnt make sense--the height can't exceed that.
	if(width <= 400 && height <= 288) xs=ys=2;
	if(width > 400 && height <= 288) ys=2;
	if(width <= 400 && height > 288) xs=2;
	if(width > 400 && height > 288) {}
	
	//TODO - shrink it entirely if cropping. EDIT-any idea what this means? if you figure it out, just do it.
	
	int xm = (virtual_width - width*xs) / 2;
	int ym = (virtual_height - height*ys) / 2;

	int curr = 0;

	//1. double the height, while cropping down
	if(height != virtual_height)
	{
		uint32* src = VTBuffer[curr]->pixels + (s_FramebufferCurrentWidth * (espec.DisplayRect.y + cropInfo.yo)) + espec.DisplayRect.x; //?
		uint32* dst = VTBuffer[curr^1]->pixels;
		int tocopy = width*4;

		//float from top as needed
		memset(dst, 0, ym*tocopy);
		dst += width * ym;

		if(ys==2)
		{
			for(int y=0;y<height;y++)
			{
				memcpy(dst,src,tocopy);
				dst += width;
				memcpy(dst,src,tocopy);
				dst += width;
				src += s_FramebufferCurrentWidth;
			}
		}
		else
		{
			for(int y=0;y<height;y++)
			{
				memcpy(dst, src, tocopy);
				dst += width;
				src += s_FramebufferCurrentWidth;
			}
		}


		//fill bottom
		int remaining_lines = virtual_height - ym - height*ys;
		memset(dst, 0, remaining_lines*tocopy);

		//patch up the metrics
		height = virtual_height; //we floated the content vertically, so this becomes the new height
		espec.DisplayRect.x = 0;
		espec.DisplayRect.y = 0;
		espec.DisplayRect.h = height;
		s_FramebufferCurrentWidth = width;
		VTLineWidths[curr^1][0] = VTLineWidths[curr][0];
		VTLineWidths[curr^1][kScanlineWidthHeuristicIndex] = VTLineWidths[curr][kScanlineWidthHeuristicIndex];

		curr ^= 1;
	}

	//2. double the width as needed. but always float it.
	//note, theres nothing to be done here if the framebuffer is already wide enough
	if(width != virtual_width)
	{
		uint32* src = VTBuffer[curr]->pixels + (s_ShockConfig.fb_width*espec.DisplayRect.y) + espec.DisplayRect.x;
		uint32* dst = VTBuffer[curr^1]->pixels;

		for(int y=0;y<height;y++)
		{
			//float the content horizontally
			for(int x=0;x<xm;x++)
				*dst++ = 0;

			if(xs==2)
			{
				for(int x=0;x<width;x++)
				{
					*dst++ = *src;
					*dst++ = *src++;
				}
				src += s_FramebufferCurrentWidth - width;
			}
			else
			{
				memcpy(dst,src,width*4);
				dst += width;
				src += s_FramebufferCurrentWidth;
			}

			//float the content horizontally
			int remaining_pixels = virtual_width - xm - width*xs;
			for(int x=0;x<remaining_pixels;x++)
				*dst++ = 0;
		}

		//patch up the metrics
		width = virtual_width; //we floated the content horizontally, so this becomes the new width
		espec.DisplayRect.x = 0;
		espec.DisplayRect.y = 0;
		VTLineWidths[curr^1][0] = width;
		VTLineWidths[curr ^ 1][kScanlineWidthHeuristicIndex] = width;
		s_FramebufferCurrentWidth = width;

		curr ^= 1;
	}

	s_FramebufferCurrent = curr;
}

EW_EXPORT s32 shock_GetSamples(void* psx, void* buffer)
{
	//if buffer is NULL, user just wants to know how many samples, so dont do any copying
	if(buffer != NULL)
	{
		memcpy(buffer,espec.SoundBuf,espec.SoundBufSize*4);
	}

	return espec.SoundBufSize;
}


EW_EXPORT s32 shock_GetFramebuffer(void* psx, ShockFramebufferInfo* fb)
{
	//TODO - fastpath for emitting to the final framebuffer, although if we did that, we'd have to regenerate it every time
	//TODO - let the frontend do this, anyway. need a new filter for it. this was in the plans from the beginning, i just havent done it yet

	//if user requires normalization, do it now
	if(fb->flags & eShockFramebufferFlags_Normalize)
		if(!s_FramebufferNormalized)
		{
			NormalizeFramebuffer();
			s_FramebufferNormalized = true;
		}

	int fbIndex = s_FramebufferCurrent;

	//always fetch description
	FramebufferCropInfo cropInfo;
	_shock_AnalyzeFramebufferCropInfo(fbIndex, &cropInfo);
	int width = cropInfo.width;
	int height = cropInfo.height;
	int yo = cropInfo.yo;

	//sloppy, but the above AnalyzeFramebufferCropInfo() will give us too short of a buffer
	if(fb->flags & eShockFramebufferFlags_Normalize)
	{
		height = espec.DisplayRect.h;
		yo = 0;
	}

	fb->width = width;
	fb->height = height;
		
	//is that all we needed?
	if(fb->ptr == NULL)
	{
		return SHOCK_OK;
	}

	//maybe we need to output the framebuffer
	//do a raster loop and copy it to the target
	uint32* src = VTBuffer[fbIndex]->pixels + (s_FramebufferCurrentWidth*yo) + espec.DisplayRect.x;
	uint32* dst = (u32*)fb->ptr;
	int tocopy = width*4;
	for(int y=0;y<height;y++)
	{
		memcpy(dst,src,tocopy);
		src += s_FramebufferCurrentWidth;
		dst += width;
	}

	return SHOCK_OK;
}

static void LoadEXE(const uint8 *data, const uint32 size, bool ignore_pcsp = false)
{
 uint32 PC;
 uint32 SP;
 uint32 TextStart;
 uint32 TextSize;

 //TODO ERROR HANDLING
 //if(size < 0x800)
 // throw(MDFN_Error(0, "PS-EXE is too small."));

 PC = MDFN_de32lsb<false>(&data[0x10]);
 SP = MDFN_de32lsb<false>(&data[0x30]);
 TextStart = MDFN_de32lsb<false>(&data[0x18]);
 TextSize = MDFN_de32lsb<false>(&data[0x1C]);

 if(ignore_pcsp)
  printf("TextStart=0x%08x\nTextSize=0x%08x\n", TextStart, TextSize);
 else
  printf("PC=0x%08x\nSP=0x%08x\nTextStart=0x%08x\nTextSize=0x%08x\n", PC, SP, TextStart, TextSize);

 TextStart &= 0x1FFFFF;

 if(TextSize > 2048 * 1024)
 {
	 //TODO ERROR HANDLING
  //throw(MDFN_Error(0, "Text section too large"));
 }

 //TODO ERROR HANDLING
 /*if(TextSize > (size - 0x800))
  throw(MDFN_Error(0, "Text section recorded size is larger than data available in file.  Header=0x%08x, Available=0x%08x", TextSize, size - 0x800));

 if(TextSize < (size - 0x800))
  throw(MDFN_Error(0, "Text section recorded size is smaller than data available in file.  Header=0x%08x, Available=0x%08x", TextSize, size - 0x800));*/

 if(!TextMem.size())
 {
  TextMem_Start = TextStart;
  TextMem.resize(TextSize);
 }

 if(TextStart < TextMem_Start)
 {
  uint32 old_size = TextMem.size();

  //printf("RESIZE: 0x%08x\n", TextMem_Start - TextStart);

  TextMem.resize(old_size + TextMem_Start - TextStart);
  memmove(&TextMem[TextMem_Start - TextStart], &TextMem[0], old_size);

  TextMem_Start = TextStart;
 }

 if(TextMem.size() < (TextStart - TextMem_Start + TextSize))
  TextMem.resize(TextStart - TextMem_Start + TextSize);

 memcpy(&TextMem[TextStart - TextMem_Start], data + 0x800, TextSize);


 //
 //
 //

 // BIOS patch
 BIOSROM->WriteU32(0x6990, (3 << 26) | ((0xBF001000 >> 2) & ((1 << 26) - 1)));
// BIOSROM->WriteU32(0x691C, (3 << 26) | ((0xBF001000 >> 2) & ((1 << 26) - 1)));

// printf("INSN: 0x%08x\n", BIOSROM->ReadU32(0x6990));
// exit(1);
 uint8 *po;

 po = &PIOMem->data8[0x0800];

 MDFN_en32lsb<false>(po, (0x0 << 26) | (31 << 21) | (0x8 << 0));	// JR
 po += 4;
 MDFN_en32lsb<false>(po, 0);	// NOP(kinda)
 po += 4;

 po = &PIOMem->data8[0x1000];

 // Load cacheable-region target PC into r2
 MDFN_en32lsb<false>(po, (0xF << 26) | (0 << 21) | (1 << 16) | (0x9F001010 >> 16));      // LUI
 po += 4;
 MDFN_en32lsb<false>(po, (0xD << 26) | (1 << 21) | (2 << 16) | (0x9F001010 & 0xFFFF));   // ORI
 po += 4;

 // Jump to r2
 MDFN_en32lsb<false>(po, (0x0 << 26) | (2 << 21) | (0x8 << 0));	// JR
 po += 4;
 MDFN_en32lsb<false>(po, 0);	// NOP(kinda)
 po += 4;

 //
 // 0x9F001010:
 //

 // Load source address into r8
 uint32 sa = 0x9F000000 + 65536;
 MDFN_en32lsb<false>(po, (0xF << 26) | (0 << 21) | (1 << 16) | (sa >> 16));	// LUI
 po += 4;
 MDFN_en32lsb<false>(po, (0xD << 26) | (1 << 21) | (8 << 16) | (sa & 0xFFFF)); 	// ORI
 po += 4;

 // Load dest address into r9
 MDFN_en32lsb<false>(po, (0xF << 26) | (0 << 21) | (1 << 16)  | (TextMem_Start >> 16));	// LUI
 po += 4;
 MDFN_en32lsb<false>(po, (0xD << 26) | (1 << 21) | (9 << 16) | (TextMem_Start & 0xFFFF)); 	// ORI
 po += 4;

 // Load size into r10
 MDFN_en32lsb<false>(po, (0xF << 26) | (0 << 21) | (1 << 16)  | (TextMem.size() >> 16));	// LUI
 po += 4;
 MDFN_en32lsb<false>(po, (0xD << 26) | (1 << 21) | (10 << 16) | (TextMem.size() & 0xFFFF)); 	// ORI
 po += 4;

 //
 // Loop begin
 //
 
 MDFN_en32lsb<false>(po, (0x24 << 26) | (8 << 21) | (1 << 16));	// LBU to r1
 po += 4;

 MDFN_en32lsb<false>(po, (0x08 << 26) | (10 << 21) | (10 << 16) | 0xFFFF);	// Decrement size
 po += 4;

 MDFN_en32lsb<false>(po, (0x28 << 26) | (9 << 21) | (1 << 16));	// SB from r1
 po += 4;

 MDFN_en32lsb<false>(po, (0x08 << 26) | (8 << 21) | (8 << 16) | 0x0001);	// Increment source addr
 po += 4;

 MDFN_en32lsb<false>(po, (0x05 << 26) | (0 << 21) | (10 << 16) | (-5 & 0xFFFF));
 po += 4;
 MDFN_en32lsb<false>(po, (0x08 << 26) | (9 << 21) | (9 << 16) | 0x0001);	// Increment dest addr
 po += 4;

 //
 // Loop end
 //

 // Load SP into r29
 if(ignore_pcsp)
 {
  po += 16;
 }
 else
 {
  MDFN_en32lsb<false>(po, (0xF << 26) | (0 << 21) | (1 << 16)  | (SP >> 16));	// LUI
  po += 4;
  MDFN_en32lsb<false>(po, (0xD << 26) | (1 << 21) | (29 << 16) | (SP & 0xFFFF)); 	// ORI
  po += 4;

  // Load PC into r2
  MDFN_en32lsb<false>(po, (0xF << 26) | (0 << 21) | (1 << 16)  | ((PC >> 16) | 0x8000));      // LUI
  po += 4;
  MDFN_en32lsb<false>(po, (0xD << 26) | (1 << 21) | (2 << 16) | (PC & 0xFFFF));   // ORI
  po += 4;
 }

 // Half-assed instruction cache flush. ;)
 for(unsigned i = 0; i < 1024; i++)
 {
  MDFN_en32lsb<false>(po, 0);
  po += 4;
 }



 // Jump to r2
 MDFN_en32lsb<false>(po, (0x0 << 26) | (2 << 21) | (0x8 << 0));	// JR
 po += 4;
 MDFN_en32lsb<false>(po, 0);	// NOP(kinda)
 po += 4;
}

EW_EXPORT s32 shock_MountEXE(void* psx, void* exebuf, s32 size, s32 ignore_pcsp)
{
	LoadEXE((uint8*)exebuf, (uint32)size, !!ignore_pcsp);
	return SHOCK_OK;
}

static void Cleanup(void)
{
 TextMem.resize(0);

 if(CDC)
 {
  delete CDC;
  CDC = NULL;
 }

 if(SPU)
 {
  delete SPU;
  SPU = NULL;
 }

 if(GPU)
 {
  delete GPU;
  GPU = NULL;
 }

 if(CPU)
 {
  delete CPU;
  CPU = NULL;
 }

 if(FIO)
 {
  delete FIO;
  FIO = NULL;
 }

 DMA_Kill();

 if(BIOSROM)
 {
  delete BIOSROM;
  BIOSROM = NULL;
 }

 if(PIOMem)
 {
  delete PIOMem;
  PIOMem = NULL;
 }

 cdifs = NULL;
}

static void CloseGame(void)
{
 Cleanup();
}

EW_EXPORT s32 shock_CreateDisc(ShockDiscRef** outDisc, void *Opaque, s32 lbaCount, ShockDisc_ReadTOC ReadTOC, ShockDisc_ReadLBA ReadLBA2448, bool suppliesDeinterleavedSubcode)
{
	*outDisc = new ShockDiscRef(Opaque, lbaCount, ReadTOC, ReadLBA2448, suppliesDeinterleavedSubcode);
	return SHOCK_OK;
}

ShockDiscRef* s_CurrDisc = NULL;
ShockDiscInfo s_CurrDiscInfo;

static s32 _shock_SetOrPokeDisc(void* psx, ShockDiscRef* disc, bool poke)
{
	ShockDiscInfo info;
	strcpy(info.id,"\0\0\0\0");
	info.region = REGION_NONE;
	if(disc != NULL)
	{
		shock_AnalyzeDisc(disc,&info);
	}

	s_CurrDiscInfo = info;
	s_CurrDisc = disc;

	CDC->SetDisc(s_CurrDisc,s_CurrDiscInfo.id, poke);

	return SHOCK_OK;
}

//Sets the disc in the tray. Returns SHOCK_NOCANDO if it's closed (TODO). You can pass NULL to remove a disc from the tray
EW_EXPORT s32 shock_SetDisc(void* psx, ShockDiscRef* disc)
{
	return _shock_SetOrPokeDisc(psx,disc,false);
}

EW_EXPORT s32 shock_PokeDisc(void* psx, ShockDiscRef* disc)
{
	//let's talk about why this function is needed. well, let's paste an old comment on the subject:
	//heres a comment from some old savestating code. something to keep in mind (maybe or maybe not a surprise depending on your point of view)
	//"Call SetDisc() BEFORE we load CDC state, since SetDisc() has emulation side effects.  We might want to clean this up in the future."
	//I'm not really sure I like how SetDisc works, so I'm glad this was brought to our attention

	return _shock_SetOrPokeDisc(psx,disc,true);
}

EW_EXPORT s32 shock_OpenTray(void* psx)
{
	if(s_ShockState.eject) return SHOCK_NOCANDO;
	s_ShockState.eject = true;
	CDC->OpenTray();
	return SHOCK_OK;
}

EW_EXPORT s32 shock_CloseTray(void* psx)
{
	if(!s_ShockState.eject) return SHOCK_NOCANDO;
	s_ShockState.eject = false;
	CDC->CloseTray(false);
	return SHOCK_OK;
}


EW_EXPORT s32 shock_DestroyDisc(ShockDiscRef* disc)
{
	delete disc;
	return SHOCK_OK;
}


class CDIF_Stream_Thing : public Stream
{
 public:

 CDIF_Stream_Thing(CDIF *cdintf_arg, uint32 lba_arg, uint32 sector_count_arg);
 ~CDIF_Stream_Thing();

 virtual uint64 attributes(void);
 virtual uint8 *map(void);
 virtual void unmap(void);
  
 virtual uint64 read(void *data, uint64 count, bool error_on_eos = true);
 virtual void write(const void *data, uint64 count);

 virtual void seek(int64 offset, int whence);
 virtual int64 tell(void);
 virtual int64 size(void);
 virtual void close(void);

 private:
 CDIF *cdintf;
 const uint32 start_lba;
 const uint32 sector_count;
 int64 position;
};

//THIS CODE SHOULD BE REMOVED WHEN A MORE ROBUST ISO PARSER IS ADDED.
class ShockDiscRef_Stream_Thing : public Stream
{
 public:

 ShockDiscRef_Stream_Thing(ShockDiscRef *cdintf_arg, uint32 lba_arg, uint32 sector_count_arg);
 ~ShockDiscRef_Stream_Thing();

 virtual uint64 attributes(void);
 virtual uint8 *map(void);
 virtual void unmap(void);
  
 virtual uint64 read(void *data, uint64 count, bool error_on_eos = true);
 virtual void write(const void *data, uint64 count);

 virtual void seek(int64 offset, int whence);
 virtual int64 tell(void);
 virtual int64 size(void);
 virtual void close(void);

 private:
 ShockDiscRef *cdintf;
 const uint32 start_lba;
 const uint32 sector_count;
 int64 position;
};

ShockDiscRef_Stream_Thing::ShockDiscRef_Stream_Thing(ShockDiscRef *cdintf_arg, uint32 start_lba_arg, uint32 sector_count_arg)
	: cdintf(cdintf_arg)
	, start_lba(start_lba_arg)
	, sector_count(sector_count_arg)
{

}

ShockDiscRef_Stream_Thing::~ShockDiscRef_Stream_Thing()
{

}

uint64 ShockDiscRef_Stream_Thing::attributes(void)
{
 return(ATTRIBUTE_READABLE | ATTRIBUTE_SEEKABLE);
}

uint8 *ShockDiscRef_Stream_Thing::map(void)
{
 return NULL;
}

void ShockDiscRef_Stream_Thing::unmap(void)
{

}
  
uint64 ShockDiscRef_Stream_Thing::read(void *data, uint64 count, bool error_on_eos)
{
 if(count > (((uint64)sector_count * 2048) - position))
 {
  //if(error_on_eos)
  //{
  // throw "EOF";
  //}

  count = ((uint64)sector_count * 2048) - position;
 }

 if(!count)
  return(0);

 for(uint64 rp = position; rp < (position + count); rp = (rp &~ 2047) + 2048)
 {
  uint8 buf[2048];  

  if(!cdintf->ReadLBA2048(start_lba + (rp / 2048), buf))
  {
   //throw MDFN_Error(ErrnoHolder(EIO));
		return 0; //???????????
  }
  
  //::printf("Meow: %08llx -- %08llx\n", count, (rp - position) + std::min<uint64>(2048 - (rp & 2047), count - (rp - position)));
  memcpy((uint8*)data + (rp - position), buf + (rp & 2047), std::min<uint64>(2048 - (rp & 2047), count - (rp - position)));
 }

 position += count;

 return count;
}

void ShockDiscRef_Stream_Thing::write(const void *data, uint64 count)
{
 
}

void ShockDiscRef_Stream_Thing::seek(int64 offset, int whence)
{
 int64 new_position;

 switch(whence)
 {
  default:
	
	break;

  case SEEK_SET:
	new_position = offset;
	break;

  case SEEK_CUR:
	new_position = position + offset;
	break;

  case SEEK_END:
	new_position = ((int64)sector_count * 2048) + offset;
	break;
 }

 if(new_position < 0 || new_position > ((int64)sector_count * 2048))
  //throw MDFN_Error(ErrnoHolder(EINVAL));
 {
 }

 position = new_position;
}

int64 ShockDiscRef_Stream_Thing::tell(void)
{
 return position;
}

int64 ShockDiscRef_Stream_Thing::size(void)
{
 return(sector_count * 2048);
}

void ShockDiscRef_Stream_Thing::close(void)
{

}

//Analyzes the disc by inspecting other things, in case the system.cnf determination failed
static s32 AnalyzeDiscEx(ShockDiscRef* disc, ShockDiscInfo* info)
{
	const char *id = NULL;
	uint8 buf[2048];
	uint8 fbuf[2048 + 1];
	unsigned ipos, opos;

	//clear it out in case of error
	info->region = REGION_NONE;
	info->id[0] = 0;

	memset(fbuf, 0, sizeof(fbuf));

	//if it wasnt mode 2, we failed
	s32 readed = disc->ReadLBA2048(4, buf);
	if(readed != 0x02)
		return SHOCK_ERROR;

	//lowercase strings for searching
	for(ipos = 0, opos = 0; ipos < 0x48; ipos++)
	{
		if(buf[ipos] > 0x20 && buf[ipos] < 0x80)
		{
			fbuf[opos++] = tolower(buf[ipos]);
		}
	}

	fbuf[opos++] = 0;

	PSX_DBG(PSX_DBG_SPARSE, "License string: %s", (char *)fbuf);

	if(strstr((char *)fbuf, "licensedby") != NULL)
	{
		if(strstr((char *)fbuf, "america") != NULL)
		{
			strcpy(info->id,"SCEA");
			info->region = REGION_NA;
			return SHOCK_OK;
		}
		else if(strstr((char *)fbuf, "europe") != NULL)
		{
			strcpy(info->id,"SCEE");
			info->region = REGION_EU;
			return SHOCK_OK;
		}
		else if(strstr((char *)fbuf, "japan") != NULL)
		{
			strcpy(info->id,"SCEI");
			info->region = REGION_JP;
			return SHOCK_OK;
		}
		else if(strstr((char *)fbuf, "sonycomputerentertainmentinc.") != NULL)
		{
			strcpy(info->id,"SCEI");
			info->region = REGION_JP;
			return SHOCK_OK;
		}
	}

	return SHOCK_ERROR;
}

//this is kind of lame. cant we get a proper iso fs parser here?
EW_EXPORT s32 shock_AnalyzeDisc(ShockDiscRef* disc, ShockDiscInfo* info)
{
	const char *ret = NULL;
	Stream *fp = NULL;
	CDUtility::TOC toc;

	//(*CDInterfaces)[disc]->ReadTOC(&toc);

	//if(toc.first_track > 1 || toc.

	try
	{
		uint8 pvd[2048];
		unsigned pvd_search_count = 0;

		fp = new ShockDiscRef_Stream_Thing(disc, 0, ~0);
		fp->seek(0x8000, SEEK_SET);

		do
		{
			if((pvd_search_count++) == 32)
				throw "PVD search count limit met.";

			fp->read(pvd, 2048);

			if(memcmp(&pvd[1], "CD001", 5))
				throw "Not ISO-9660";

			if(pvd[0] == 0xFF)
				throw "Missing Primary Volume Descriptor";
		} while(pvd[0] != 0x01);
		//[156 ... 189], 34 bytes
		uint32 rdel = MDFN_de32lsb<false>(&pvd[0x9E]);
		uint32 rdel_len = MDFN_de32lsb<false>(&pvd[0xA6]);

		if(rdel_len >= (1024 * 1024 * 10))	// Arbitrary sanity check.
			throw "Root directory table too large";

		fp->seek((int64)rdel * 2048, SEEK_SET);
		//printf("%08x, %08x\n", rdel * 2048, rdel_len);

		//I think this loop is scanning directory entries until it finds system.cnf and if it never finishes we'll jsut fall out
		while(fp->tell() < (((int64)rdel * 2048) + rdel_len))
		{
			uint8 len_dr = fp->get_u8();
			uint8 dr[256 + 1];

			memset(dr, 0xFF, sizeof(dr));

			if(!len_dr)
				break;

			memset(dr, 0, sizeof(dr));
			dr[0] = len_dr;
			fp->read(dr + 1, len_dr - 1);

			uint8 len_fi = dr[0x20];

			if(len_fi == 12 && !memcmp(&dr[0x21], "SYSTEM.CNF;1", 12))
			{
				uint32 file_lba = MDFN_de32lsb<false>(&dr[0x02]);
				//uint32 file_len = MDFN_de32lsb<false>(&dr[0x0A]);
				uint8 fb[2048 + 1];
				char *bootpos;

				memset(fb, 0, sizeof(fb));
				fp->seek(file_lba * 2048, SEEK_SET);
				fp->read(fb, 2048);

				bootpos = strstr((char*)fb, "BOOT") + 4;
				while(*bootpos == ' ' || *bootpos == '\t') bootpos++;
				if(*bootpos == '=')
				{
					bootpos++;
					while(*bootpos == ' ' || *bootpos == '\t') bootpos++;
					if(!strncasecmp(bootpos, "cdrom:\\", 7))
					{ 
						bootpos += 7;
						char *tmp;

						if((tmp = strchr(bootpos, '_'))) *tmp = 0;
						if((tmp = strchr(bootpos, '.'))) *tmp = 0;
						if((tmp = strchr(bootpos, ';'))) *tmp = 0;
						//puts(bootpos);

						if(strlen(bootpos) == 4 && bootpos[0] == 'S' && (bootpos[1] == 'C' || bootpos[1] == 'L' || bootpos[1] == 'I'))
						{
							switch(bootpos[2])
							{
							case 'E':
								info->region = REGION_EU;
								strcpy(info->id,"SCEE");
								goto Breakout;

							case 'U':
								info->region = REGION_NA;
								strcpy(info->id,"SCEA");
								goto Breakout;

							case 'K':	// Korea?
							case 'B':
							case 'P':
								info->region = REGION_JP;
								strcpy(info->id,"SCEI");
								goto Breakout;
							}
						}
					}
				}

				//puts((char*)fb);
				//puts("ASOFKOASDFKO");
			}
		}
	}
	catch(const char* str)
	{
		//puts(e.what());
		int zzz=9;
	}
	catch(...)
	{
		int zzz=9;
	}

	//uhmm couldnt find system.cnf. try another way
	return AnalyzeDiscEx(disc,info);

Breakout:

	return SHOCK_OK;
}

bool ShockDiscRef::ReadLBA_PW(uint8* pwbuf96, int32 lba, bool hint_fullread)
{
	//TODO - whats that hint mean
	//reference:  static const int32 LBA_Read_Minimum = -150;
 //reference:  static const int32 LBA_Read_Maximum = 449849;	// 100 * 75 * 60 - 150 - 1
	u8 tmp[2448];
	s32 ret = ReadLBA2448(lba,tmp);
	if(ret != SHOCK_OK)
		return false;
	memcpy(pwbuf96,tmp+2352,96);
	return true;
}

s32 ShockDiscRef::ReadLBA2448(s32 lba, void* dst2448)
{
	return InternalReadLBA2448(lba, dst2448, true);
}

s32 ShockDiscRef::InternalReadLBA2448(s32 lba, void* dst2448, bool needSubcode)
{
	int ret = mcbReadLBA2448(mOpaque, lba, dst2448);
	if(ret != SHOCK_OK)
		return ret;
	
	if(needSubcode && mSuppliesDeinterleavedSubcode)
	{
		//presently, CDC consumes deinterleaved subcode.
		//perhaps this could be optimized in the future
		u8 tmp[96];
		CDUtility::subpw_interleave((u8*)dst2448+2352,tmp);
		memcpy((u8*)dst2448+2352,tmp,96);
	}

	return SHOCK_OK;
}

//adapts the ReadLBA2448 results to a 2048 byte sector and returns the mode, as required
s32 ShockDiscRef::ReadLBA2048(s32 lba, void* dst2048)
{
	union Sector {
		struct {
			u8 sync[12];
			u8 adr[3];
			u8 mode;
			union {
				struct {
					u8 data2048[2048];
					u8 ecc[4];
					u8 reserved[8];
					u8 ecm[276];
				};
				u8 data2336[2336];
			};
		};
		u8 buf[2352];
	};

	union XASector {
		struct {
			u8 sync[12];
			u8 adr[3];
			u8 mode;
			u8 subheader[8];
			union {
				u8 data2048[2048];
				u8 ecc[4];
				u8 ecm[276];
			} form1;
			union {
				u8 data2334[2334];
				u8 ecc[4];
			} form2;
		};
		u8 buf[2352];
	};

	static union {
		struct {
			union {
				XASector xasector;
				Sector sector;
			};
			u8 subcode[96];
		};
		u8 buf2448[2448];
	};

	s32 ret = InternalReadLBA2448(lba,buf2448,false);
	if(ret != SHOCK_OK)
		return ret;

	if(sector.mode == 1)
		memcpy(dst2048,sector.data2048,2048);
	else
		memcpy(dst2048,xasector.form1.data2048,2048);

	return sector.mode;
}

//Returns information about a memory buffer for peeking (main memory, spu memory, etc.)
EW_EXPORT s32 shock_GetMemData(void* psx, void** ptr, s32* size, s32 memType)
{
	switch(memType)
	{
	case eMemType_MainRAM: *ptr = MainRAM.data8; *size = 2048*1024; break;
	case eMemType_BiosROM: *ptr = BIOSROM->data8; *size = 512*1024; break;
	case eMemType_PIOMem: *ptr = PIOMem->data8; *size = 64*1024; break;
	case eMemType_GPURAM: *ptr = GPU->GPURAM; *size = 2*512*1024; break;
	case eMemType_SPURAM: *ptr = SPU->SPURAM; *size = 512*1024; break;
	case eMemType_DCache: *ptr = CPU->debug_GetScratchRAMPtr(); *size = 1024; break;
	default:
		return SHOCK_ERROR;
	}
	return SHOCK_OK;
}

class PSX
{
public:
	template<bool isReader>void SyncState(EW::NewState *ns);
} s_PSX;

namespace MDFN_IEN_PSX {
void DMA_SyncState(bool isReader, EW::NewState *ns);
void GTE_SyncState(bool isReader, EW::NewState *ns);
void TIMER_SyncState(bool isReader, EW::NewState *ns);
void SIO_SyncState(bool isReader, EW::NewState *ns);
void MDEC_SyncState(bool isReader, EW::NewState *ns);
void IRQ_SyncState(bool isReader, EW::NewState *ns);
}

SYNCFUNC(PSX)
{
  NSS(s_ShockState);
  PSS(MainRAM.data8, 2*1024*1024);
  NSS(SysControl.Regs);
	NSS(PSX_PRNG.lcgo);
	NSS(PSX_PRNG.x);
	NSS(PSX_PRNG.y);
	NSS(PSX_PRNG.z);
	NSS(PSX_PRNG.c);

	//note: mednafen used to save the current disc index. that's kind of nice, I guess, if you accept that responsibility in the core.
	//but we're not doing things that way.
	//I think instead maybe we should generate a hash of the inserted disc and save that, and then check if theres a mismatch between the disc at the time of the savestate and the current disc
	//but we'll do that in the frontend for now

	//old:
	// "TODO: Remember to increment dirty count in memory card state loading routine." 
	//not sure what this means or whether I like it

	//I've kept the ordering of these sections the same, in case its important for some unknown reason.. for now.

	TSS(CPU);

	ns->EnterSection("GTE");
	GTE_SyncState(isReader,ns);
	ns->ExitSection("GTE");

	ns->EnterSection("DMA");
	DMA_SyncState(isReader,ns);
	ns->ExitSection("DMA");

	ns->EnterSection("TIMER");
	TIMER_SyncState(isReader,ns);
	ns->ExitSection("TIMER");

	ns->EnterSection("SIO");
	SIO_SyncState(isReader,ns);
	ns->ExitSection("SIO");

	TSS(CDC);

	ns->EnterSection("MDEC");
	MDEC_SyncState(isReader,ns);
	ns->ExitSection("MDEC");

	TSS(GPU); //did some special logic for the CPU, ordering may matter, but probably not

	TSS(SPU);
	TSS(FIO); //TODO - DUALSHOCK, MC


	//"Do it last." the comments say. And all this other nonsense about IRQs in the other state syncing functions. weird.....
	 //ret &= IRQ_StateAction(sm, load, data_only);	// 

	ns->EnterSection("IRQ");
	IRQ_SyncState(isReader,ns);
	ns->ExitSection("IRQ");

	if(isReader)
	{
		//the purpose of this is to restore the sorting of the event list
		//event updates are programmed to have no effect if the time step is 0
		//and at this point, the time base timestamp will be 0 (it always is after a frame advance)
		//so the event updaters just run, do nothing, and restore themselves in the list
		ForceEventUpdates(0);	// FIXME to work with debugger step mode.
	}
}

EW_EXPORT s32 shock_StateTransaction(void *psx, ShockStateTransaction* transaction)
{
	switch(transaction->transaction)
	{
	case eShockStateTransaction_BinarySize:
		{
			EW::NewStateDummy dummy;
			s_PSX.SyncState<false>(&dummy);
			return dummy.GetLength();
		}
	case eShockStateTransaction_BinaryLoad:
		{
			if(transaction->buffer == NULL) return SHOCK_ERROR;
			EW::NewStateExternalBuffer loader((char*)transaction->buffer, transaction->bufferLength);
			s_PSX.SyncState<true>(&loader);
			if(!loader.Overflow() && loader.GetLength() == transaction->bufferLength)
				return SHOCK_OK;
			else return SHOCK_ERROR;
		}
	case eShockStateTransaction_BinarySave:
		{
			if(transaction->buffer == NULL) return SHOCK_ERROR;
			EW::NewStateExternalBuffer saver((char*)transaction->buffer, transaction->bufferLength);
			s_PSX.SyncState<false>(&saver);
			if(!saver.Overflow() && saver.GetLength() == transaction->bufferLength)
				return SHOCK_OK;
			else return SHOCK_ERROR;
		}
	case eShockStateTransaction_TextLoad:
		{
			EW::NewStateExternalFunctions saver(&transaction->ff);
			s_PSX.SyncState<true>(&saver);
			return SHOCK_OK;
		}
	case eShockStateTransaction_TextSave:
		{
			EW::NewStateExternalFunctions loader(&transaction->ff);
			s_PSX.SyncState<false>(&loader);
			return SHOCK_OK;
		}
		return SHOCK_ERROR;

	default:
		return SHOCK_ERROR;
	}
}

EW_EXPORT s32 shock_GetRegisters_CPU(void* psx, ShockRegisters_CPU* buffer)
{
	memcpy(buffer->GPR,CPU->debug_GetGPRPtr(),32*4);
	buffer->PC = CPU->GetRegister(PS_CPU::GSREG_PC_NEXT,NULL,0);
	buffer->PC_NEXT = CPU->GetRegister(PS_CPU::GSREG_PC_NEXT,NULL,0);
	buffer->IN_BD_SLOT = CPU->GetRegister(PS_CPU::GSREG_IN_BD_SLOT,NULL,0);
	buffer->LO  = CPU->GetRegister(PS_CPU::GSREG_LO,NULL,0);
	buffer->HI = CPU->GetRegister(PS_CPU::GSREG_HI,NULL,0);
	buffer->SR = CPU->GetRegister(PS_CPU::GSREG_SR,NULL,0);
	buffer->CAUSE = CPU->GetRegister(PS_CPU::GSREG_CAUSE,NULL,0);
	buffer->EPC = CPU->GetRegister(PS_CPU::GSREG_EPC,NULL,0);
	
	return SHOCK_OK;
}

//Sets a CPU register. Rather than have an enum for the registers, lets just use the index (not offset) within the struct
EW_EXPORT s32 shock_SetRegister_CPU(void* psx, s32 index, u32 value)
{
	//takes advantage of layout of GSREG_ matchign our struct (not an accident!)
	CPU->SetRegister((u32)index,value);
	
	return SHOCK_OK;
}

EW_EXPORT s32 shock_SetRenderOptions(void* pxs, ShockRenderOptions* opts)
{
	GPU->SetRenderOptions(opts);
	s_ShockConfig.opts = *opts;
	return SHOCK_OK;
}

extern void* g_ShockTraceCallbackOpaque;
extern ShockCallback_Trace g_ShockTraceCallback;
extern ShockCallback_Mem g_ShockMemCallback;
extern eShockMemCb g_ShockMemCbType;

//Sets the callback to be used for CPU tracing
EW_EXPORT s32 shock_SetTraceCallback(void* psx, void* opaque, ShockCallback_Trace callback)
{
	g_ShockTraceCallbackOpaque = opaque;
	g_ShockTraceCallback = callback;

	return SHOCK_OK;
}

//Sets the callback to be used for memory hook events
EW_EXPORT s32 shock_SetMemCb(void* psx, ShockCallback_Mem callback, eShockMemCb cbMask)
{
	g_ShockMemCallback = callback;
	g_ShockMemCbType = cbMask;
	return SHOCK_OK;
}

//Sets whether LEC is enabled (sector level error correction). Defaults to FALSE (disabled)
EW_EXPORT s32 shock_SetLEC(void* psx, bool enabled)
{
	CDC->SetLEC(enabled);
	return SHOCK_OK;
}

//whether "determine lag from GPU frames" signal is set (GPU did something considered non-lag)
//returns SHOCK_TRUE or SHOCK_FALSE
EW_EXPORT s32 shock_GetGPUUnlagged(void* psx)
{
	return GpuFrameForLag ? SHOCK_TRUE : SHOCK_FALSE;
}