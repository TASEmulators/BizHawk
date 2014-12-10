#ifndef __MDFN_PSX_CPU_H
#define __MDFN_PSX_CPU_H

/*
 Load delay notes:

	// Takes 1 less
	".set noreorder\n\t"
	".set nomacro\n\t"
	"lw %0, 0(%2)\n\t"
	"nop\n\t"
	"nop\n\t"
	"or %0, %1, %1\n\t"

	// cycle than this:
	".set noreorder\n\t"
	".set nomacro\n\t"
	"lw %0, 0(%2)\n\t"
	"nop\n\t"
	"or %0, %1, %1\n\t"
	"nop\n\t"


	// Both of these
	".set noreorder\n\t"
	".set nomacro\n\t"
	"lw %0, 0(%2)\n\t"
	"nop\n\t"
	"nop\n\t"
	"or %1, %0, %0\n\t"

	// take same...(which is kind of odd).
	".set noreorder\n\t"
	".set nomacro\n\t"
	"lw %0, 0(%2)\n\t"
	"nop\n\t"
	"or %1, %0, %0\n\t"
	"nop\n\t"
*/

#include "gte.h"

namespace MDFN_IEN_PSX
{

#define PS_CPU_EMULATE_ICACHE 1

class PS_CPU
{
 public:

 PS_CPU();
 ~PS_CPU();

 // FAST_MAP_* enums are in BYTES(8-bit), not in 32-bit units("words" in MIPS context), but the sizes
 // will always be multiples of 4.
 enum { FAST_MAP_SHIFT = 16 };
 enum { FAST_MAP_PSIZE = 1 << FAST_MAP_SHIFT };

 void SetFastMap(void *region_mem, uint32_t region_address, uint32_t region_size);

 INLINE void SetEventNT(const pscpu_timestamp_t next_event_ts_arg)
 {
  next_event_ts = next_event_ts_arg;
 }

 pscpu_timestamp_t Run(pscpu_timestamp_t timestamp_in, const bool ILHMode);

 void Power(void);

 // which ranges 0-5, inclusive
 void AssertIRQ(int which, bool asserted);

 void SetHalt(bool status);

 // TODO eventually: factor BIU address decoding directly in the CPU core somehow without hurting speed.
 void SetBIU(uint32_t val);
 uint32_t GetBIU(void);

 int StateAction(StateMem *sm, int load, int data_only);

 private:

 struct
 {
  uint32_t GPR[32];
  uint32_t GPR_dummy;	// Used in load delay simulation(indexing past the end of GPR)
 };
 uint32_t LO;
 uint32_t HI;


 uint32_t BACKED_PC;
 uint32_t BACKED_new_PC;
 uint32_t BACKED_new_PC_mask;

 uint32_t IPCache;
 void RecalcIPCache(void);
 bool Halted;

 uint32_t BACKED_LDWhich;
 uint32_t BACKED_LDValue;
 uint32_t LDAbsorb;

 pscpu_timestamp_t next_event_ts;
 pscpu_timestamp_t gte_ts_done;
 pscpu_timestamp_t muldiv_ts_done;

 uint32_t BIU;

 struct __ICache
 {
  uint32_t TV;
  uint32_t Data;
 };

 union
 {
  __ICache ICache[1024];
  uint32 ICache_Bulk[2048];
 };

 enum
 {
  CP0REG_BPC = 3,		// PC breakpoint address.
  CP0REG_BDA = 5,		// Data load/store breakpoint address.
  CP0REG_TAR = 6,		// Target address(???)
  CP0REG_DCIC = 7,		// Cache control
  CP0REG_BDAM = 9,		// Data load/store address mask.
  CP0REG_BPCM = 11,		// PC breakpoint address mask.
  CP0REG_SR = 12,
  CP0REG_CAUSE = 13,
  CP0REG_EPC = 14,
  CP0REG_PRID = 15,		// Product ID
  CP0REG_ERREG = 16
 };

 struct
 {
  union
  {
   uint32_t Regs[32];
   struct
   {
    uint32_t Unused00;
    uint32_t Unused01;
    uint32_t Unused02;
    uint32_t BPC;		// RW
    uint32_t Unused04;
    uint32_t BDA;		// RW
    uint32_t TAR;
    uint32_t DCIC;	// RW
    uint32_t Unused08;	
    uint32_t BDAM;	// R/W
    uint32_t Unused0A;
    uint32_t BPCM;	// R/W
    uint32_t SR;		// R/W
    uint32_t CAUSE;	// R/W(partial)
    uint32_t EPC;		// R
    uint32_t PRID;	// R
    uint32_t ERREG;	// ?(may not exist, test)
   };
  };
 } CP0;

#if 1
 //uint32_t WrAbsorb;
 //uint8_t WrAbsorbShift;

 // On read:
 //WrAbsorb = 0;
 //WrAbsorbShift = 0;

 // On write:
 //WrAbsorb >>= (WrAbsorbShift >> 2) & 8;
 //WrAbsorbShift -= (WrAbsorbShift >> 2) & 8;

 //WrAbsorb |= (timestamp - pre_write_timestamp) << WrAbsorbShift;
 //WrAbsorbShift += 8;
#endif

 struct
 {
  uint8_t ReadAbsorb[0x20];
  uint8_t ReadAbsorbDummy;
 };
 uint8_t ReadAbsorbWhich;
 uint8_t ReadFudge;

 //uint32_t WriteAbsorb;
 //uint8_t WriteAbsorbCount;
 //uint8_t WriteAbsorbMonkey;

 MultiAccessSizeMem<1024, uint32, false> ScratchRAM;

 //PS_GTE GTE;

 uint8_t *FastMap[1 << (32 - FAST_MAP_SHIFT)];
 uint8_t DummyPage[FAST_MAP_PSIZE];

 enum
 {
  EXCEPTION_INT = 0,
  EXCEPTION_MOD = 1,
  EXCEPTION_TLBL = 2,
  EXCEPTION_TLBS = 3,
  EXCEPTION_ADEL = 4, // Address error on load
  EXCEPTION_ADES = 5, // Address error on store
  EXCEPTION_IBE = 6, // Instruction bus error
  EXCEPTION_DBE = 7, // Data bus error
  EXCEPTION_SYSCALL = 8, // System call
  EXCEPTION_BP = 9, // Breakpoint
  EXCEPTION_RI = 10, // Reserved instruction
  EXCEPTION_COPU = 11,  // Coprocessor unusable
  EXCEPTION_OV = 12	// Arithmetic overflow
 };

 uint32_t Exception(uint32_t code, uint32_t PC, const uint32_t NPM) MDFN_WARN_UNUSED_RESULT;

 template<bool DebugMode, bool ILHMode> pscpu_timestamp_t RunReal(pscpu_timestamp_t timestamp_in);

 template<typename T> T PeekMemory(uint32_t address) MDFN_COLD;
 template<typename T> T ReadMemory(pscpu_timestamp_t &timestamp, uint32_t address, bool DS24 = false, bool LWC_timing = false);
 template<typename T> void WriteMemory(pscpu_timestamp_t &timestamp, uint32_t address, uint32_t value, bool DS24 = false);


 //
 // Mednafen debugger stuff follows:
 //
 public:
 void SetCPUHook(void (*cpuh)(const pscpu_timestamp_t timestamp, uint32_t pc), void (*addbt)(uint32_t from, uint32_t to, bool exception));
 void CheckBreakpoints(void (*callback)(bool write, uint32_t address, unsigned int len), uint32_t instr);

 enum
 {
  GSREG_GPR = 0,
  GSREG_PC = 32,
  GSREG_PC_NEXT,
  GSREG_IN_BD_SLOT,
  GSREG_LO,
  GSREG_HI,
  GSREG_SR,
  GSREG_CAUSE,
  GSREG_EPC,
 };

 uint32_t GetRegister(unsigned int which, char *special, const uint32_t special_len);
 void SetRegister(unsigned int which, uint32_t value);
 bool PeekCheckICache(uint32_t PC, uint32_t *iw);
 uint8_t PeekMem8(uint32_t A);
 uint16_t PeekMem16(uint32_t A);
 uint32_t PeekMem32(uint32_t A);
 private:
 void (*CPUHook)(const pscpu_timestamp_t timestamp, uint32_t pc);
 void (*ADDBT)(uint32_t from, uint32_t to, bool exception);
};

}

#endif
