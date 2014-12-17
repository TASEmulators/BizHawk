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

#include "psx.h"
#include "cpu.h"
#include "gte.h"

/* TODO
	Make sure load delays are correct.

	Consider preventing IRQs being taken while in a branch delay slot, to prevent potential problems with games that like to be too clever and perform
	un-restartable sequences of instructions.
*/

#define BIU_ENABLE_ICACHE_S1	0x00000800	// Enable I-cache, set 1
#define BIU_ENABLE_DCACHE	0x00000080	// Enable D-cache
#define BIU_TAG_TEST_MODE	0x00000004	// Enable TAG test mode(IsC must be set to 1 as well presumably?)
#define BIU_INVALIDATE_MODE	0x00000002	// Enable Invalidate mode(IsC must be set to 1 as well presumably?)
#define BIU_LOCK		0x00000001	// Enable Lock mode(IsC must be set to 1 as well presumably?)
						// Does lock mode prevent the actual data payload from being modified, while allowing tags to be modified/updated???

namespace MDFN_IEN_PSX
{


PS_CPU::PS_CPU()
{
   Halted = false;

   memset(FastMap, 0, sizeof(FastMap));
   memset(DummyPage, 0xFF, sizeof(DummyPage));	// 0xFF to trigger an illegal instruction exception, so we'll know what's up when debugging.

   for(uint64 a = 0x00000000; a < (1ULL << 32); a += FAST_MAP_PSIZE)
      SetFastMap(DummyPage, a, FAST_MAP_PSIZE);

   CPUHook = NULL;
   ADDBT = NULL;

 GTE_Init();

 for(unsigned i = 0; i < 24; i++)
 {
  uint8 v = 7;

  if(i < 12)
   v += 4;

  if(i < 21)
   v += 3;

  MULT_Tab24[i] = v;
 }
}

PS_CPU::~PS_CPU()
{


}

void PS_CPU::SetFastMap(void *region_mem, uint32_t region_address, uint32_t region_size)
{
   uint64_t A;
   // FAST_MAP_SHIFT
   // FAST_MAP_PSIZE

   for(A = region_address; A < (uint64)region_address + region_size; A += FAST_MAP_PSIZE)
      FastMap[A >> FAST_MAP_SHIFT] = ((uint8_t *)region_mem - region_address);
}

INLINE void PS_CPU::RecalcIPCache(void)
{
   IPCache = 0;

   if(((CP0.SR & CP0.CAUSE & 0xFF00) && (CP0.SR & 1)) || Halted)
      IPCache = 0x80;
}

void PS_CPU::SetHalt(bool status)
{
   Halted = status;
   RecalcIPCache();
}

void PS_CPU::Power(void)
{
   unsigned i;

   assert(sizeof(ICache) == sizeof(ICache_Bulk));

   memset(GPR, 0, sizeof(GPR));
   memset(&CP0, 0, sizeof(CP0));
   LO = 0;
   HI = 0;

   gte_ts_done = 0;
   muldiv_ts_done = 0;

   BACKED_PC = 0xBFC00000;
   BACKED_new_PC = 4;
   BACKED_new_PC_mask = ~0U;

   BACKED_LDWhich = 0x20;
   BACKED_LDValue = 0;
   LDAbsorb = 0;
   memset(ReadAbsorb, 0, sizeof(ReadAbsorb));
   ReadAbsorbWhich = 0;
   ReadFudge = 0;

   //WriteAbsorb = 0;
   //WriteAbsorbCount = 0;
   //WriteAbsorbMonkey = 0;

   CP0.SR |= (1 << 22);	// BEV
   CP0.SR |= (1 << 21);	// TS

   CP0.PRID = 0x2;

   RecalcIPCache();


   BIU = 0;

   memset(ScratchRAM.data8, 0, 1024);

   // Not quite sure about these poweron/reset values:
   for(i = 0; i < 1024; i++)
   {
      ICache[i].TV = 0x2 | ((BIU & 0x800) ? 0x0 : 0x1);
      ICache[i].Data = 0;
   }

   GTE_Power();
}

void PS_CPU::AssertIRQ(unsigned which, bool asserted)
{
	assert(which <= 5);

   CP0.CAUSE &= ~(1 << (10 + which));

   if(asserted)
      CP0.CAUSE |= 1 << (10 + which);

   RecalcIPCache();
}

void PS_CPU::SetBIU(uint32_t val)
{
   unsigned i;
   const uint32_t old_BIU = BIU;

   BIU = val & ~(0x440);

   if((BIU ^ old_BIU) & 0x800)
   {
      if(BIU & 0x800)	// ICache enabled
      {
         for(i = 0; i < 1024; i++)
            ICache[i].TV &= ~0x1;
      }
      else			// ICache disabled
      {
         for(i = 0; i < 1024; i++)
            ICache[i].TV |= 0x1;
      }
   }

   PSX_DBG(PSX_DBG_SPARSE, "[CPU] Set BIU=0x%08x\n", BIU);
}

uint32_t PS_CPU::GetBIU(void)
{
   return BIU;
}

static const uint32_t addr_mask[8] = { 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF,
				     0x7FFFFFFF, 0x1FFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF };

template<typename T>
INLINE T PS_CPU::PeekMemory(uint32_t address)
{
   T ret;
   address &= addr_mask[address >> 29];

   if(address >= 0x1F800000 && address <= 0x1F8003FF)
      return ScratchRAM.Read<T>(address & 0x3FF);

   //assert(!(CP0.SR & 0x10000));

   if(sizeof(T) == 1)
      ret = PSX_MemPeek8(address);
   else if(sizeof(T) == 2)
      ret = PSX_MemPeek16(address);
   else
      ret = PSX_MemPeek32(address);

   return(ret);
}

template<typename T>
INLINE T PS_CPU::ReadMemory(pscpu_timestamp_t &timestamp, uint32_t address, bool DS24, bool LWC_timing)
{
   T ret;

   //WriteAbsorb >>= WriteAbsorbMonkey * 8;
   //WriteAbsorbCount -= WriteAbsorbMonkey;
   //WriteAbsorbMonkey = WriteAbsorbCount;

   ReadAbsorb[ReadAbsorbWhich] = 0;
   ReadAbsorbWhich = 0;

   address &= addr_mask[address >> 29];

   if(address >= 0x1F800000 && address <= 0x1F8003FF)
   {
      LDAbsorb = 0;

      if(DS24)
         return ScratchRAM.ReadU24(address & 0x3FF);
      else
         return ScratchRAM.Read<T>(address & 0x3FF);
   }

   timestamp += (ReadFudge >> 4) & 2;

   //assert(!(CP0.SR & 0x10000));

   pscpu_timestamp_t lts = timestamp;

   if(sizeof(T) == 1)
      ret = PSX_MemRead8(lts, address);
   else if(sizeof(T) == 2)
      ret = PSX_MemRead16(lts, address);
   else
   {
      if(DS24)
         ret = PSX_MemRead24(lts, address) & 0xFFFFFF;
      else
         ret = PSX_MemRead32(lts, address);
   }

   if(LWC_timing)
      lts += 1;
   else
      lts += 2;

   LDAbsorb = (lts - timestamp);
   timestamp = lts;

   return(ret);
}

template<typename T>
INLINE void PS_CPU::WriteMemory(pscpu_timestamp_t &timestamp, uint32_t address, uint32_t value, bool DS24)
{
   if(MDFN_LIKELY(!(CP0.SR & 0x10000)))
   {
      address &= addr_mask[address >> 29];

      if(address >= 0x1F800000 && address <= 0x1F8003FF)
      {
         if(DS24)
            ScratchRAM.WriteU24(address & 0x3FF, value);
         else
            ScratchRAM.Write<T>(address & 0x3FF, value);

         return;
      }

      //if(WriteAbsorbCount == 4)
      //{
      // WriteAbsorb >>= 8;
      // WriteAbsorbCount--;
      //
      // if(WriteAbsorbMonkey)
      //  WriteAbsorbMonkey--;
      //}
      //timestamp += 3;
      //WriteAbsorb |= (3U << (WriteAbsorbCount * 8));
      //WriteAbsorbCount++;

      if(sizeof(T) == 1)
         PSX_MemWrite8(timestamp, address, value);
      else if(sizeof(T) == 2)
         PSX_MemWrite16(timestamp, address, value);
      else
      {
         if(DS24)
            PSX_MemWrite24(timestamp, address, value);
         else
            PSX_MemWrite32(timestamp, address, value);
      }
   }
   else
   {
      if(BIU & 0x800)	// Instruction cache is enabled/active
      {
         if(BIU & 0x4)	// TAG test mode.
         {
            // TODO: Respect written value.
            __ICache *ICI = &ICache[((address & 0xFF0) >> 2)];
            const uint8_t valid_bits = 0x00;

            ICI[0].TV = ((valid_bits & 0x01) ? 0x00 : 0x02) | ((BIU & 0x800) ? 0x0 : 0x1);
            ICI[1].TV = ((valid_bits & 0x02) ? 0x00 : 0x02) | ((BIU & 0x800) ? 0x0 : 0x1);
            ICI[2].TV = ((valid_bits & 0x04) ? 0x00 : 0x02) | ((BIU & 0x800) ? 0x0 : 0x1);
            ICI[3].TV = ((valid_bits & 0x08) ? 0x00 : 0x02) | ((BIU & 0x800) ? 0x0 : 0x1);
         }
         else if(!(BIU & 0x1))
         {
            ICache[(address & 0xFFC) >> 2].Data = value << ((address & 0x3) * 8);
         }
      }

      if((BIU & 0x081) == 0x080)	// Writes to the scratchpad(TODO test)
      {
         if(DS24)
            ScratchRAM.WriteU24(address & 0x3FF, value);
         else
            ScratchRAM.Write<T>(address & 0x3FF, value);
      }
      //printf("IsC WRITE%d 0x%08x 0x%08x -- CP0.SR=0x%08x\n", (int)sizeof(T), address, value, CP0.SR);
   }
}

uint32_t PS_CPU::Exception(uint32_t code, uint32_t PC, const uint32_t NPM)
{
   const bool InBDSlot = !(NPM & 0x3);
   uint32_t handler = 0x80000080;

   assert(code < 16);

   if(code != EXCEPTION_INT && code != EXCEPTION_BP && code != EXCEPTION_SYSCALL)
   {
      PSX_DBG(PSX_DBG_WARNING, "Exception: %08x @ PC=0x%08x(IBDS=%d) -- IPCache=0x%02x -- IPEND=0x%02x -- SR=0x%08x ; IRQC_Status=0x%04x -- IRQC_Mask=0x%04x\n", code, PC, InBDSlot, IPCache, (CP0.CAUSE >> 8) & 0xFF, CP0.SR,
            IRQ_GetRegister(IRQ_GSREG_STATUS, NULL, 0), IRQ_GetRegister(IRQ_GSREG_MASK, NULL, 0));
   }

   if(CP0.SR & (1 << 22))	// BEV
      handler = 0xBFC00180;

   CP0.EPC = PC;
   if(InBDSlot)
      CP0.EPC -= 4;

   if(ADDBT)
      ADDBT(PC, handler, true);

   // "Push" IEc and KUc(so that the new IEc and KUc are 0)
   CP0.SR = (CP0.SR & ~0x3F) | ((CP0.SR << 2) & 0x3F);

   // Setup cause register
   CP0.CAUSE &= 0x0000FF00;
   CP0.CAUSE |= code << 2;

   // If EPC was adjusted -= 4 because we were in a branch delay slot, set the bit.
   if(InBDSlot)
      CP0.CAUSE |= 0x80000000;

   RecalcIPCache();

   return(handler);
}

#define BACKING_TO_ACTIVE			\
	PC = BACKED_PC;				\
	new_PC = BACKED_new_PC;			\
	new_PC_mask = BACKED_new_PC_mask;	\
	LDWhich = BACKED_LDWhich;		\
	LDValue = BACKED_LDValue;

#define ACTIVE_TO_BACKING			\
	BACKED_PC = PC;				\
	BACKED_new_PC = new_PC;			\
	BACKED_new_PC_mask = new_PC_mask;	\
	BACKED_LDWhich = LDWhich;		\
	BACKED_LDValue = LDValue;

#define GPR_DEPRES_BEGIN { uint8_t back = ReadAbsorb[0];
#define GPR_DEP(n) { unsigned tn = (n); ReadAbsorb[tn] = 0; }
#define GPR_RES(n) { unsigned tn = (n); ReadAbsorb[tn] = 0; }
#define GPR_DEPRES_END ReadAbsorb[0] = back; }

template<bool DebugMode, bool BIOSPrintMode, bool ILHMode>
pscpu_timestamp_t PS_CPU::RunReal(pscpu_timestamp_t timestamp_in)
{
   register pscpu_timestamp_t timestamp = timestamp_in;

   register uint32_t PC;
   register uint32_t new_PC;
   register uint32_t new_PC_mask;
   register uint32_t LDWhich;
   register uint32_t LDValue;

   //printf("%d %d\n", gte_ts_done, muldiv_ts_done);

   gte_ts_done += timestamp;
   muldiv_ts_done += timestamp;

   BACKING_TO_ACTIVE;

   do
   {
      //printf("Running: %d %d\n", timestamp, next_event_ts);
      while(MDFN_LIKELY(timestamp < next_event_ts))
      {
         uint32_t instr;
         uint32_t opf;

         // Zero must be zero...until the Master Plan is enacted.
         GPR[0] = 0;

         if(DebugMode && CPUHook)
         {
            ACTIVE_TO_BACKING;

            // For save states in step mode.
            gte_ts_done -= timestamp;
            muldiv_ts_done -= timestamp;

            CPUHook(timestamp, PC);

            // For save states in step mode.
            gte_ts_done += timestamp;
            muldiv_ts_done += timestamp;

            BACKING_TO_ACTIVE;
         }


				 if(BIOSPrintMode)
				 {
					if(PC == 0xB0)
					{
					 if(MDFN_UNLIKELY(GPR[9] == 0x3D))
					 {
						PSX_DBG_BIOS_PUTC(GPR[4]);
					 }
					}
				 }

				 // We can't fold this into the ICache[] != PC handling, since the lower 2 bits of TV
				 // are already used for cache management purposes and it assumes that the lower 2 bits of PC will be 0.
				 if(MDFN_UNLIKELY(PC & 0x3))
				 {
					// This will block interrupt processing, but since we're going more for keeping broken homebrew/hacks from working
					// than super-duper-accurate pipeline emulation, it shouldn't be a problem.
					new_PC = Exception(EXCEPTION_ADEL, PC, new_PC_mask);
					new_PC_mask = 0;
					goto OpDone;
				 }

         instr = ICache[(PC & 0xFFC) >> 2].Data;

         if(ICache[(PC & 0xFFC) >> 2].TV != PC)
         {
            //WriteAbsorb = 0;
            //WriteAbsorbCount = 0;
            //WriteAbsorbMonkey = 0;
            ReadAbsorb[ReadAbsorbWhich] = 0;
            ReadAbsorbWhich = 0;

            // FIXME: Handle executing out of scratchpad.
            if(PC >= 0xA0000000 || !(BIU & 0x800))
            {
     instr = MDFN_de32lsb<true>(&FastMap[PC >> FAST_MAP_SHIFT][PC]);
     timestamp += 4;	// Approximate best-case cache-disabled time, per PS1 tests(executing out of 0xA0000000+); it can be 5 in *some* sequences of code(like a lot of sequential "nop"s, probably other simple instructions too).
    }
    else
    {
     __ICache *ICI = &ICache[((PC & 0xFF0) >> 2)];
     const uint8 *FMP = &FastMap[(PC &~ 0xF) >> FAST_MAP_SHIFT][PC &~ 0xF];


               // | 0x2 to simulate (in)validity bits.
               ICI[0x00].TV = (PC &~ 0xF) | 0x00 | 0x2;
               ICI[0x01].TV = (PC &~ 0xF) | 0x04 | 0x2;
               ICI[0x02].TV = (PC &~ 0xF) | 0x08 | 0x2;
               ICI[0x03].TV = (PC &~ 0xF) | 0x0C | 0x2;

               timestamp += 3;


							 switch(PC & 0xC)
							 {
							 case 0x0:
								 timestamp++;
								 ICI[0x00].TV &= ~0x2;
								 ICI[0x00].Data = MDFN_de32lsb<true>(&FMP[0x0]);
							 case 0x4:
								 timestamp++;
								 ICI[0x01].TV &= ~0x2;
								 ICI[0x01].Data = MDFN_de32lsb<true>(&FMP[0x4]);
							 case 0x8:
								 timestamp++;
								 ICI[0x02].TV &= ~0x2;
								 ICI[0x02].Data = MDFN_de32lsb<true>(&FMP[0x8]);
							 case 0xC:
								 timestamp++;
								 ICI[0x03].TV &= ~0x2;
								 ICI[0x03].Data = MDFN_de32lsb<true>(&FMP[0xC]);
								 break;
							 }
							 instr = ICache[(PC & 0xFFC) >> 2].Data;
						}
				 }

         //printf("PC=%08x, SP=%08x - op=0x%02x - funct=0x%02x - instr=0x%08x\n", PC, GPR[29], instr >> 26, instr & 0x3F, instr);
         //for(int i = 0; i < 32; i++)
         // printf("%02x : %08x\n", i, GPR[i]);
         //printf("\n");

         opf = instr & 0x3F;

         if(instr & (0x3F << 26))
            opf = 0x40 | (instr >> 26);

         opf |= IPCache;

#if 0
         {
            uint32_t tmp = (ReadAbsorb[ReadAbsorbWhich] + 0x7FFFFFFF) >> 31;
            ReadAbsorb[ReadAbsorbWhich] -= tmp;
            timestamp = timestamp + 1 - tmp;
         }
#else
         if(ReadAbsorb[ReadAbsorbWhich])
            ReadAbsorb[ReadAbsorbWhich]--;
         //else if((uint8)WriteAbsorb)
         //{
         // WriteAbsorb--;
         // if(!WriteAbsorb)
         // {
         //  WriteAbsorbCount--;
         //  if(WriteAbsorbMonkey)
         //   WriteAbsorbMonkey--;
         //  WriteAbsorb >>= 8;
         // }
         //}
         else
            timestamp++;
#endif

#define DO_LDS() { GPR[LDWhich] = LDValue; ReadAbsorb[LDWhich] = LDAbsorb; ReadFudge = LDWhich; ReadAbsorbWhich |= LDWhich & 0x1F; LDWhich = 0x20; }
#define BEGIN_OPF(name, arg_op, arg_funct) { op_##name: /*assert( ((arg_op) ? (0x40 | (arg_op)) : (arg_funct)) == opf); */
#define END_OPF goto OpDone; }

#define DO_BRANCH(offset, mask)			\
         {						\
            if(ILHMode)					\
            {								\
               uint32_t old_PC = PC;						\
               PC = (PC & new_PC_mask) + new_PC;				\
               if(old_PC == ((PC & (mask)) + (offset)))			\
               {								\
							 if(MDFN_densb<uint32, true>(&FastMap[PC >> FAST_MAP_SHIFT][PC]) == 0)	\
                  {								\
                     if(next_event_ts > timestamp) /* Necessary since next_event_ts might be set to something like "0" to force a call to the event handler. */		\
                     {								\
                        timestamp = next_event_ts;					\
                     }								\
                  }								\
               }								\
            }						\
            else						\
            PC = (PC & new_PC_mask) + new_PC;		\
            new_PC = (offset);				\
            new_PC_mask = (mask) & ~3;			\
            /* Lower bits of new_PC_mask being clear signifies being in a branch delay slot. (overloaded behavior for performance) */	\
            \
            if(DebugMode && ADDBT)                 	\
            {						\
               ADDBT(PC, (PC & new_PC_mask) + new_PC, false);	\
            }						\
            goto SkipNPCStuff;				\
         }

#define ITYPE uint32 rs MDFN_NOWARN_UNUSED = (instr >> 21) & 0x1F; uint32 rt MDFN_NOWARN_UNUSED = (instr >> 16) & 0x1F; uint32 immediate = (int32)(int16)(instr & 0xFFFF); /*printf(" rs=%02x(%08x), rt=%02x(%08x), immediate=(%08x) ", rs, GPR[rs], rt, GPR[rt], immediate);*/
#define ITYPE_ZE uint32_t rs MDFN_NOWARN_UNUSED = (instr >> 21) & 0x1F; uint32_t rt MDFN_NOWARN_UNUSED = (instr >> 16) & 0x1F; uint32_t immediate = instr & 0xFFFF; /*printf(" rs=%02x(%08x), rt=%02x(%08x), immediate=(%08x) ", rs, GPR[rs], rt, GPR[rt], immediate);*/
#define JTYPE uint32_t target = instr & ((1 << 26) - 1); /*printf(" target=(%08x) ", target);*/
#define RTYPE uint32_t rs MDFN_NOWARN_UNUSED = (instr >> 21) & 0x1F; uint32_t rt MDFN_NOWARN_UNUSED = (instr >> 16) & 0x1F; uint32_t rd MDFN_NOWARN_UNUSED = (instr >> 11) & 0x1F; uint32_t shamt MDFN_NOWARN_UNUSED = (instr >> 6) & 0x1F; /*printf(" rs=%02x(%08x), rt=%02x(%08x), rd=%02x(%08x) ", rs, GPR[rs], rt, GPR[rt], rd, GPR[rd]);*/

#if 1
#include "cpu_bigswitch.inc"
#else
#include "cpu_coputedgoto.inc"
#endif

OpDone: ;

        PC = (PC & new_PC_mask) + new_PC;
        new_PC_mask = ~0U;
        new_PC = 4;

SkipNPCStuff:	;

               //printf("\n");
      }
   } while(MDFN_LIKELY(PSX_EventHandler(timestamp)));

   if(gte_ts_done > 0)
      gte_ts_done -= timestamp;

   if(muldiv_ts_done > 0)
      muldiv_ts_done -= timestamp;

   ACTIVE_TO_BACKING;

   return(timestamp);
}

pscpu_timestamp_t PS_CPU::Run(pscpu_timestamp_t timestamp_in, bool BIOSPrintMode, bool ILHMode)
{
 if(CPUHook || ADDBT)
  return(RunReal<true, true, false>(timestamp_in));
 else
 {
  if(ILHMode)
   return(RunReal<false, false, true>(timestamp_in));
  else
  {
   if(BIOSPrintMode)
    return(RunReal<false, true, false>(timestamp_in));
   else
    return(RunReal<false, false, false>(timestamp_in));
  }
 }
}


void PS_CPU::SetCPUHook(void (*cpuh)(const pscpu_timestamp_t timestamp, uint32_t pc), void (*addbt)(uint32_t from, uint32_t to, bool exception))
{
   ADDBT = addbt;
   CPUHook = cpuh;
}

uint32_t PS_CPU::GetRegister(unsigned int which, char *special, const uint32_t special_len)
{
   uint32_t ret = 0;

   if(which >= GSREG_GPR && which < (GSREG_GPR + 32))
      return GPR[which];

   switch(which)
   {
      case GSREG_PC:
         ret = BACKED_PC;
         break;

      case GSREG_PC_NEXT:
         ret = BACKED_new_PC;
         break;

      case GSREG_IN_BD_SLOT:
         ret = !(BACKED_new_PC_mask & 3);
         break;

      case GSREG_LO:
         ret = LO;
         break;

      case GSREG_HI:
         ret = HI;
         break;

      case GSREG_SR:
         ret = CP0.SR;
         break;

      case GSREG_CAUSE:
         ret = CP0.CAUSE;
         break;

      case GSREG_EPC:
         ret = CP0.EPC;
         break;

   }

   return ret;
}

void PS_CPU::SetRegister(unsigned int which, uint32_t value)
{
   if(which >= GSREG_GPR && which < (GSREG_GPR + 32))
   {
      if(which != (GSREG_GPR + 0))
         GPR[which] = value;
   }
   else switch(which)
   {
      case GSREG_PC:
         BACKED_PC = value & ~0x3;	// Remove masking if we ever add proper misaligned PC exception
         break;

      case GSREG_LO:
         LO = value;
         break;

      case GSREG_HI:
         HI = value;
         break;

      case GSREG_SR:
         CP0.SR = value;		// TODO: mask
         break;

      case GSREG_CAUSE:
         CP0.CAUSE = value;
         break;

      case GSREG_EPC:
         CP0.EPC = value & ~0x3U;
         break;
   }
}

bool PS_CPU::PeekCheckICache(uint32_t PC, uint32_t *iw)
{
   if(ICache[(PC & 0xFFC) >> 2].TV == PC)
   {
      *iw = ICache[(PC & 0xFFC) >> 2].Data;
      return(true);
   }

   return(false);
}


uint8_t PS_CPU::PeekMem8(uint32_t A)
{
 return PeekMemory<uint8>(A);
}

uint16_t PS_CPU::PeekMem16(uint32_t A)
{
 return PeekMemory<uint16>(A);
}

uint32_t PS_CPU::PeekMem32(uint32_t A)
{
 return PeekMemory<uint32>(A);
}


#undef BEGIN_OPF
#undef END_OPF
#undef MK_OPF

#define MK_OPF(op, funct)	((op) ? (0x40 | (op)) : (funct))
#define BEGIN_OPF(op, funct) case MK_OPF(op, funct): {
#define END_OPF } break;

// FIXME: should we breakpoint on an illegal address?  And with LWC2/SWC2 if CP2 isn't enabled?
void PS_CPU::CheckBreakpoints(void (*callback)(bool write, uint32_t address, unsigned int len), uint32_t instr)
{
 uint32_t opf;

 opf = instr & 0x3F;

 if(instr & (0x3F << 26))
  opf = 0x40 | (instr >> 26);


 switch(opf)
 {
  default:
	break;

    //
    // LB - Load Byte
    //
    BEGIN_OPF(0x20, 0);
	ITYPE;
	uint32_t address = GPR[rs] + immediate;

        callback(false, address, 1);
    END_OPF;

    //
    // LBU - Load Byte Unsigned
    //
    BEGIN_OPF(0x24, 0);
        ITYPE;
        uint32_t address = GPR[rs] + immediate;

        callback(false, address, 1);
    END_OPF;

    //
    // LH - Load Halfword
    //
    BEGIN_OPF(0x21, 0);
        ITYPE;
        uint32_t address = GPR[rs] + immediate;

        callback(false, address, 2);
    END_OPF;

    //
    // LHU - Load Halfword Unsigned
    //
    BEGIN_OPF(0x25, 0);
        ITYPE;
        uint32_t address = GPR[rs] + immediate;

        callback(false, address, 2);
    END_OPF;


    //
    // LW - Load Word
    //
    BEGIN_OPF(0x23, 0);
        ITYPE;
        uint32_t address = GPR[rs] + immediate;

        callback(false, address, 4);
    END_OPF;

    //
    // SB - Store Byte
    //
    BEGIN_OPF(0x28, 0);
	ITYPE;
	uint32_t address = GPR[rs] + immediate;

        callback(true, address, 1);
    END_OPF;

    // 
    // SH - Store Halfword
    //
    BEGIN_OPF(0x29, 0);
        ITYPE;
        uint32_t address = GPR[rs] + immediate;

        callback(true, address, 2);
    END_OPF;

    // 
    // SW - Store Word
    //
    BEGIN_OPF(0x2B, 0);
        ITYPE;
        uint32_t address = GPR[rs] + immediate;

        callback(true, address, 4);
    END_OPF;

    //
    // LWL - Load Word Left
    //
    BEGIN_OPF(0x22, 0);
	ITYPE;
	uint32_t address = GPR[rs] + immediate;

	do
	{
         callback(false, address, 1);
	} while((address--) & 0x3);

    END_OPF;

    //
    // SWL - Store Word Left
    //
    BEGIN_OPF(0x2A, 0);
        ITYPE;
        uint32_t address = GPR[rs] + immediate;

        do
        {
	 callback(true, address, 1);
        } while((address--) & 0x3);

    END_OPF;

    //
    // LWR - Load Word Right
    //
    BEGIN_OPF(0x26, 0);
        ITYPE;
        uint32_t address = GPR[rs] + immediate;

        do
        {
	 callback(false, address, 1);
        } while((++address) & 0x3);

    END_OPF;

    //
    // SWR - Store Word Right
    //
    BEGIN_OPF(0x2E, 0);
        ITYPE;
        uint32_t address = GPR[rs] + immediate;

        do
        {
	 callback(true, address, 1);
        } while((++address) & 0x3);

    END_OPF;

    //
    // LWC2
    //
    BEGIN_OPF(0x32, 0);
        ITYPE;
        uint32_t address = GPR[rs] + immediate;

	callback(false, address, 4);
    END_OPF;

    //
    // SWC2
    //
    BEGIN_OPF(0x3A, 0);
        ITYPE;
        uint32_t address = GPR[rs] + immediate;

	callback(true, address, 4);
    END_OPF;

 }
}



SYNCFUNC(PS_CPU)
{
	NSS(GPR);
	NSS(LO);
	NSS(HI);
	NSS(BACKED_PC);
	NSS(BACKED_new_PC);
	NSS(BACKED_new_PC_mask);

	NSS(IPCache);
	NSS(Halted);

	NSS(BACKED_LDWhich);
	NSS(BACKED_LDValue);
	NSS(LDAbsorb);

	NSS(next_event_ts);
	NSS(gte_ts_done);
	NSS(muldiv_ts_done);

	NSS(BIU);
	NSS(ICache_Bulk);

	NSS(CP0.Regs);

	NSS(ReadAbsorb);
	NSS(ReadAbsorbDummy);
	NSS(ReadAbsorbWhich);
	NSS(ReadFudge);

	NSS(ScratchRAM.data8);

} //SYNCFUNC(CPU)

} //namespace MDFN_IEN_PSX
