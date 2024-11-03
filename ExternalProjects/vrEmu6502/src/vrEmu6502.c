/*
 * Troy's 6502 Emulator
 *
 * Copyright (c) 2022 Troy Schrapel
 *
 * This code is licensed under the MIT license
 *
 * https://github.com/visrealm/vrEmu6502
 *
 */

#include "vrEmu6502.h"

#include <stdlib.h>
#include <stdio.h>
#include <assert.h>

#if PICO_BUILD
#include "pico/stdlib.h"
#else
#define __not_in_flash(x)
#define __time_critical_func(fn) fn
#endif

 /*
  * address mode function prototype
  */
typedef uint16_t(*vrEmu6502AddrModeFn)(VrEmu6502*);

/*
 * instruction function prototype
 */
typedef void(*vrEmu6502InstructionFn)(VrEmu6502*, vrEmu6502AddrModeFn);

struct vrEmu6502Opcode
{
  const vrEmu6502InstructionFn  instruction;
  const vrEmu6502AddrModeFn     addrMode;
  const uint8_t                 cycles;
};
typedef struct vrEmu6502Opcode vrEmu6502Opcode;


/* ------------------------------------------------------------------
 * PRIVATE DATA STRUCTURE
 */
struct vrEmu6502_s
{
  vrEmu6502Model model;

  vrEmu6502MemRead readFn;
  vrEmu6502MemWrite writeFn;

  vrEmu6502Interrupt intPin;
  vrEmu6502Interrupt nmiPin;

  uint8_t step;
  uint8_t currentOpcode;
  uint16_t currentOpcodeAddr;

  bool wai;
  bool stp;

  uint16_t pc;

  uint8_t  ac;
  uint8_t  ix;
  uint8_t  iy;
  uint8_t  sp;

  uint8_t  flags;

  uint16_t zpBase;
  uint16_t spBase;
  uint16_t tmpAddr;

  const vrEmu6502Opcode* opcodes;
  const char* mnemonicNames[256];
  vrEmu6502AddrMode addrModes[256];
};

/* ------------------------------------------------------------------
 * PRIVATE CONSTANTS
 */


static const uint16_t NMI_VEC = 0xfffa;
static const uint16_t RESET_VEC = 0xfffc;
static const uint16_t INT_VEC = 0xfffe;

static const uint8_t UNSTABLE_MAGIC_CONST = 0xee; // common values are 0x00, 0xee, 0xff

/* ------------------------------------------------------------------
 *  HELPER FUNCTIONS
 * ----------------------------------------------------------------*/
inline static void push(VrEmu6502* vr6502, uint8_t val)
{
  vr6502->writeFn(vr6502->spBase | vr6502->sp--, val);
}

/*
 * pop a value from the stack
 */
inline static uint8_t pop(VrEmu6502* vr6502)
{
  return vr6502->readFn(vr6502->spBase | (++vr6502->sp), false);
}

/*
 * read a 16-bit value from memory
 */
inline static uint16_t read16(VrEmu6502* vr6502, uint16_t addr)
{
  return vr6502->readFn(addr, false) | (vr6502->readFn(addr + 1, false) << 8);
}

/*
 * read a 16-bit value from memory (with 6502 page crossing bug)
 */
inline static uint16_t read16Bug(VrEmu6502* vr6502, uint16_t addr)
{
  if ((addr & 0xff) == 0xff) /* 6502 bug */
  {
    return vr6502->readFn(addr, false) | (vr6502->readFn(addr & 0xff00, false) << 8);
  }
  return vr6502->readFn(addr, false) | (vr6502->readFn(addr + 1, false) << 8);
}

/*
 * read a 16-bit value from memory (wrap high byte)
 */
inline static uint16_t read16Wrapped(VrEmu6502* vr6502, uint16_t addr)
{
  return vr6502->readFn(addr, false) | (vr6502->readFn((addr + 1) & 0xff, false) << 8);
}

/*
 * additional step if page boundary crossed
 */
inline static void pageBoundary(VrEmu6502* vr6502, uint16_t addr1, uint16_t addr2)
{
  vr6502->step += !!((addr1 ^ addr2) & 0xff00);
}

/*
 * set a processor status flag bit
 */
inline static void setBit(VrEmu6502* vr6502, vrEmu6502Flag flag)
{
  vr6502->flags |= flag;
}

/*
 * clear a processor status flag bit
 */
inline static void clearBit(VrEmu6502* vr6502, vrEmu6502Flag flag)
{
  vr6502->flags &= ~flag;
}

/*
 * set/clear a processor status flag bit
 */
inline static void setOrClearBit(VrEmu6502* vr6502, vrEmu6502Flag flag, bool set)
{
  vr6502->flags &= ~flag;
  vr6502->flags |= set * flag;
}

/*
 * test a processor status flag bit (return true if set, false if clear)
 */
inline static bool testBit(VrEmu6502* vr6502, vrEmu6502Flag flag)
{
  return vr6502->flags & flag;
}

/*
 * set negative and zero flags based on given value
 */
inline static void setNZ(VrEmu6502* vr6502, uint8_t val)
{
  vr6502->flags &= ~(FlagN | FlagZ);
  vr6502->flags |= val & FlagN;
  vr6502->flags |= !val * FlagZ;
}

/*
 * is the model in the 65C02 family?
 */
inline static bool is65c02(VrEmu6502* vr6502)
{
  return vr6502->model == CPU_65C02 ||
    vr6502->model == CPU_W65C02 ||
    vr6502->model == CPU_R65C02;
}

/* ------------------------------------------------------------------
 *  DECLARE OPCODE TABLES
 * ----------------------------------------------------------------*/
static const vrEmu6502Opcode* std6502;
static const vrEmu6502Opcode* std6502U;
static const vrEmu6502Opcode* std65c02;
static const vrEmu6502Opcode* wdc65c02;
static const vrEmu6502Opcode* r65c02;

static vrEmu6502AddrMode opcodeToAddrMode(VrEmu6502* vr6502, uint8_t opcode);
static const char* opcodeToMnemonicStr(VrEmu6502* vr6502, uint8_t opcode);

/* ------------------------------------------------------------------
 *
 * create a new 6502
 */
VR_EMU_6502_DLLEXPORT VrEmu6502* vrEmu6502New(
  vrEmu6502Model model,
  vrEmu6502MemRead readFn,
  vrEmu6502MemWrite writeFn)
{
  assert(readFn);
  assert(writeFn);

  VrEmu6502* vr6502 = (VrEmu6502*)malloc(sizeof(VrEmu6502));
  if (vr6502 != NULL)
  {
    vr6502->model = model;
    vr6502->readFn = readFn;
    vr6502->writeFn = writeFn;

    vr6502->zpBase = 0x0;
    vr6502->spBase = 0x100;

    switch (model)
    {
      case CPU_65C02:
        vr6502->opcodes = std65c02;
        break;

      case CPU_W65C02:
        vr6502->opcodes = wdc65c02;
        break;

      case CPU_R65C02:
        vr6502->opcodes = r65c02;
        break;

      case CPU_6502U:
        vr6502->model = CPU_6502;
        vr6502->opcodes = std6502U;
        break;

      default:
        vr6502->opcodes = std6502;
        break;
    }

    /* build mnemonic name cache */
    for (int i = 0; i <= 0xff; ++i)
    {
      vr6502->mnemonicNames[i] = opcodeToMnemonicStr(vr6502, i & 0xff);
      vr6502->addrModes[i] = opcodeToAddrMode(vr6502, i & 0xff);
    }

    vrEmu6502Reset(vr6502);
  }

  return vr6502;
}

/* ------------------------------------------------------------------
 *
 * destroy a 6502
 */
VR_EMU_6502_DLLEXPORT void vrEmu6502Destroy(VrEmu6502* vr6502)
{
  if (vr6502)
  {
    /* destruction */
    free(vr6502);
  }
}

/* ------------------------------------------------------------------
 *
 * reset the 6502
 */
VR_EMU_6502_DLLEXPORT void vrEmu6502Reset(VrEmu6502* vr6502)
{
  if (vr6502)
  {
    /* initialization */
    vr6502->intPin = IntCleared;
    vr6502->nmiPin = IntCleared;
    vr6502->pc = read16(vr6502, RESET_VEC);
    vr6502->step = 0;
    vr6502->wai = false;
    vr6502->stp = false;
    vr6502->currentOpcode = 0;
    vr6502->tmpAddr = 0;

    /* 65c02 clears D and sets I on reset */
    if (is65c02(vr6502))
    {
      vr6502->flags &= ~FlagD;
      vr6502->flags |= FlagI;
    }

    /* B and U don't exist so always 1's */
    vr6502->flags |= FlagU | FlagB;
  }
}

static void beginInterrupt(VrEmu6502* vr6502, uint16_t addr)
{
  push(vr6502, vr6502->pc >> 8);
  push(vr6502, vr6502->pc & 0xff);
  push(vr6502, (vr6502->flags | FlagU) & ~FlagB);
  setBit(vr6502, FlagI);
  vr6502->wai = false;
  vr6502->pc = read16(vr6502, addr);
}

/* ------------------------------------------------------------------
 *
 * a single clock tick
 */
VR_EMU_6502_DLLEXPORT void __time_critical_func(vrEmu6502Tick)(VrEmu6502* vr6502)
{
  if (vr6502->stp) return;

  if (vr6502->step == 0)
  {
    /* interrupts are ignored during the 1-cycle nops*/
    if (vr6502->opcodes[vr6502->currentOpcode].cycles != 1)
    {
      /* check NMI */
      if (vr6502->nmiPin == IntRequested)
      {
        vr6502->wai = false;
        vr6502->nmiPin = IntCleared;
        beginInterrupt(vr6502, NMI_VEC);
        return;
      }

      /* check INT */
      if (vr6502->intPin == IntRequested)
      {
        vr6502->wai = false;
        if (!testBit(vr6502, FlagI))
        {
          beginInterrupt(vr6502, INT_VEC);
          return;
        }
      }
    }

    if (!vr6502->wai)
    {
      vr6502->currentOpcodeAddr = vr6502->pc++;
      vr6502->currentOpcode = vr6502->readFn(vr6502->currentOpcodeAddr, false);

      /* find the instruction in the table */
      const vrEmu6502Opcode* opcode = &vr6502->opcodes[vr6502->currentOpcode];

      /* set cycles here as they may be adjusted by addressing mode */
      vr6502->step = opcode->cycles;

      /* execute the instruction */
      opcode->instruction(vr6502, opcode->addrMode);
    }
  }

  if (vr6502->step) --vr6502->step;
}

/* ------------------------------------------------------------------
 *
 * a single instruction cycle
 */
VR_EMU_6502_DLLEXPORT uint8_t __time_critical_func(vrEmu6502InstCycle)(VrEmu6502* vr6502)
{
  vrEmu6502Tick(vr6502);
  uint8_t ticks = vr6502->step + 1;
  vr6502->step = 0;
  return ticks;
}


/* ------------------------------------------------------------------
 *
 * returns a pointer to the interrupt signal.
 * externally, you can modify it to set/reset the interrupt signal
 */
VR_EMU_6502_DLLEXPORT vrEmu6502Interrupt* vrEmu6502Int(VrEmu6502* vr6502)
{
  if (vr6502)
  {
    return &vr6502->intPin;
  }
  return NULL;
}

/* ------------------------------------------------------------------
 *
 * returns a pointer to the nmi signal.
 * externally, you can modify it to set/reset the interrupt signal
 */
VR_EMU_6502_DLLEXPORT vrEmu6502Interrupt* vrEmu6502Nmi(VrEmu6502* vr6502)
{
  if (vr6502)
  {
    return &vr6502->nmiPin;
  }
  return NULL;
}

/* ------------------------------------------------------------------
 *
 * return the program counter
 */
VR_EMU_6502_DLLEXPORT uint16_t vrEmu6502GetPC(VrEmu6502* vr6502)
{
  return vr6502->pc;
}

/* ------------------------------------------------------------------
 *
 * set the program counter
 */
VR_EMU_6502_DLLEXPORT void vrEmu6502SetPC(VrEmu6502* vr6502, uint16_t pc)
{
  vr6502->pc = pc;
  vr6502->step = 0;
}


/* ------------------------------------------------------------------
 *
 * return the accumulator
 */
VR_EMU_6502_DLLEXPORT uint8_t vrEmu6502GetAcc(VrEmu6502* vr6502)
{
  return vr6502->ac;
}

/* ------------------------------------------------------------------
 *
 * return the x index register
 */
VR_EMU_6502_DLLEXPORT uint8_t vrEmu6502GetX(VrEmu6502* vr6502)
{
  return vr6502->ix;
}

/* ------------------------------------------------------------------
 *
 * return the y index register
 */
VR_EMU_6502_DLLEXPORT uint8_t vrEmu6502GetY(VrEmu6502* vr6502)
{
  return vr6502->iy;
}

/* ------------------------------------------------------------------
 *
 * return the processor status register
 */
VR_EMU_6502_DLLEXPORT uint8_t vrEmu6502GetStatus(VrEmu6502* vr6502)
{
  return vr6502->flags;
}

/* ------------------------------------------------------------------
 *
 * return the stack pointer register
 */
VR_EMU_6502_DLLEXPORT uint8_t vrEmu6502GetStackPointer(VrEmu6502* vr6502)
{
  return vr6502->sp;
}

/* ------------------------------------------------------------------
 *
 * return the current opcode
 */
VR_EMU_6502_DLLEXPORT uint8_t vrEmu6502GetCurrentOpcode(VrEmu6502* vr6502)
{
  return vr6502->currentOpcode;
}

/* ------------------------------------------------------------------
 *
 * return the current opcode address
 */
VR_EMU_6502_DLLEXPORT uint16_t vrEmu6502GetCurrentOpcodeAddr(VrEmu6502* vr6502)
{
  return vr6502->currentOpcodeAddr;
}

/* ------------------------------------------------------------------
 *
 * return the next opcode
 */
VR_EMU_6502_DLLEXPORT uint8_t vrEmu6502GetNextOpcode(VrEmu6502* vr6502)
{
  return vr6502->readFn(vr6502->pc, true);
}


/* ------------------------------------------------------------------
 *
 * return the opcode cycle
 */
VR_EMU_6502_DLLEXPORT uint8_t vrEmu6502GetOpcodeCycle(VrEmu6502* vr6502)
{
  return vr6502->step;
}

/* ------------------------------------------------------------------
 *
 * return the opcode mnemonic string
 */
VR_EMU_6502_DLLEXPORT const char* vrEmu6502OpcodeToMnemonicStr(VrEmu6502* vr6502, uint8_t opcode)
{
  return vr6502->mnemonicNames[opcode];
}

/* ------------------------------------------------------------------
 *
 * return the opcode address mode
 */
VR_EMU_6502_DLLEXPORT
vrEmu6502AddrMode vrEmu6502GetOpcodeAddrMode(VrEmu6502* vr6502, uint8_t opcode)
{
  return vr6502->addrModes[opcode];
}

/* ------------------------------------------------------------------
 *
 * get disassembled instruction as a string. returns next instruction address
 */
VR_EMU_6502_DLLEXPORT
uint16_t vrEmu6502DisassembleInstruction(
  VrEmu6502* vr6502,
  uint16_t addr,
  int bufferSize,
  char* buffer,
  uint16_t* refAddr,
  const char* const labelMap[0x10000])
{
  if (vr6502)
  {
    uint8_t opcode = vr6502->readFn(addr, true);
    uint8_t arg8 = vr6502->readFn(addr + 1, true);
    uint16_t arg16 = (vr6502->readFn(addr + 2, true) << 8) | arg8;
    const char* mnemonic = vrEmu6502OpcodeToMnemonicStr(vr6502, opcode);

    const char* addr8Label = labelMap ? labelMap[arg8] : NULL;
    const char* addr16Label = labelMap ? labelMap[arg16] : NULL;

    int offset = snprintf(buffer, bufferSize, "%s ", mnemonic);
    buffer += offset;
    bufferSize -= offset;

    switch (vrEmu6502GetOpcodeAddrMode(vr6502, opcode))
    {
      case AddrModeAbs:
        if (addr16Label)
          snprintf(buffer, bufferSize, "%s", addr16Label);
        else
          snprintf(buffer, bufferSize, "$%04x", arg16);
        if (refAddr) *refAddr = arg16;
        return addr + 3;

      case AddrModeAbsX:
        if (addr16Label)
          snprintf(buffer, bufferSize, "%s, x", addr16Label);
        else
          snprintf(buffer, bufferSize, "$%04x, x", arg16);
        if (refAddr) *refAddr = arg16 + vr6502->ix;
        return addr + 3;

      case AddrModeAbsY:
        if (addr16Label)
          snprintf(buffer, bufferSize, "%s, y", addr16Label);
        else
          snprintf(buffer, bufferSize, "$%04x, y", arg16);
        if (refAddr) *refAddr = arg16 + vr6502->iy;
        return addr + 3;

      case AddrModeImm:
        if (addr8Label)
          snprintf(buffer, bufferSize, "#%s", addr8Label);
        else
          snprintf(buffer, bufferSize, "#$%02x", arg8);
        if (refAddr) *refAddr = addr + 1;
        return addr + 2;

      case AddrModeAbsInd:
        if (addr16Label)
          snprintf(buffer, bufferSize, "(%s)", addr16Label);
        else
          snprintf(buffer, bufferSize, "($%04x)", arg16);
        if (refAddr) *refAddr = arg16;
        return addr + 3;

      case AddrModeAbsIndX:
        if (addr16Label)
          snprintf(buffer, bufferSize, "(%s, x)", addr16Label);
        else
          snprintf(buffer, bufferSize, "($%04x, x)", arg16);
        if (refAddr) *refAddr = arg16 + vr6502->ix;
        return addr + 3;

      case AddrModeIndX:
        if (addr8Label)
          snprintf(buffer, bufferSize, "(%s, x)", addr8Label);
        else
          snprintf(buffer, bufferSize, "($%02x, x)", arg8);
        if (refAddr) *refAddr = arg8 + vr6502->ix;
        return addr + 2;

      case AddrModeIndY:
        if (addr8Label)
          snprintf(buffer, bufferSize, "(%s), y", addr8Label);
        else
          snprintf(buffer, bufferSize, "($%02x), y", arg8);
        if (refAddr) *refAddr = arg8;
        return addr + 2;

      case AddrModeRel:
        if (labelMap && labelMap[addr + (int8_t)arg8 + 2])
          snprintf(buffer, bufferSize, "%s", labelMap[addr + (int8_t)arg8 + 2]);
        else
          snprintf(buffer, bufferSize, "$%04x", addr + (int8_t)arg8 + 2);
        if (refAddr) *refAddr = addr + (int8_t)arg8 + 2;
        return addr + 2;

      case AddrModeZP:
        if (addr8Label)
          snprintf(buffer, bufferSize, "%s", addr8Label);
        else
          snprintf(buffer, bufferSize, "$%02x", arg8);
        if (refAddr) *refAddr = arg8;
        return addr + 2;

      case AddrModeZPI:
        if (addr8Label)
          snprintf(buffer, bufferSize, "(%s)", addr8Label);
        else
          snprintf(buffer, bufferSize, "($%02x)", arg8);
        if (refAddr) *refAddr = arg8;
        return addr + 2;

      case AddrModeZPX:
        if (addr8Label)
          snprintf(buffer, bufferSize, "%s, x", addr8Label);
        else
          snprintf(buffer, bufferSize, "$%02x, x", arg8);
        if (refAddr) *refAddr = arg8 + vr6502->ix;
        return addr + 2;

      case AddrModeZPY:
        if (addr8Label)
          snprintf(buffer, bufferSize, "%s, y", addr8Label);
        else
          snprintf(buffer, bufferSize, "$%02x, y", arg8);
        if (refAddr) *refAddr = arg8 + vr6502->iy;
        return addr + 2;

      case AddrModeAcc:
      case AddrModeImp:
        return addr + 1;

      default:
        break;

    }
  }
  return 0;
}



/* ------------------------------------------------------------------
 *  ADDRESS MODES
 * ----------------------------------------------------------------*/


 /*
  * absolute mode - eg. lda $1234
  */
static uint16_t ab(VrEmu6502* vr6502)
{
  uint16_t addr = read16(vr6502, vr6502->pc++);
  ++vr6502->pc;
  return addr;
}

/*
 * absolute indexed x - eg. lda $1234, x
 */
static uint16_t abx(VrEmu6502* vr6502)
{
  uint16_t base = read16(vr6502, vr6502->pc++); ++vr6502->pc;
  return base + vr6502->ix;
}

/*
 * absolute indexed y - eg. lda $1234, y
 */
static uint16_t aby(VrEmu6502* vr6502)
{
  uint16_t base = read16(vr6502, vr6502->pc++); ++vr6502->pc;
  return base + vr6502->iy;
}

/*
 * absolute indexed x (with page boundary cycle check) - eg. lda $1234, x
 */
static uint16_t axp(VrEmu6502* vr6502)
{
  uint16_t base = read16(vr6502, vr6502->pc++); ++vr6502->pc;
  uint16_t addr = base + vr6502->ix;
  pageBoundary(vr6502, addr, base);
  return addr;
}

/*
 * absolute indexed y (with page boundary cycle check) - eg. lda $1234, y
 */
static uint16_t ayp(VrEmu6502* vr6502)
{
  uint16_t base = read16(vr6502, vr6502->pc++); ++vr6502->pc;
  uint16_t addr = base + vr6502->iy;
  pageBoundary(vr6502, addr, base);
  return addr;
}

/*
 * immediate mdoe - eg. lda #12
 */
static uint16_t imm(VrEmu6502* vr6502)
{
  return vr6502->pc++;
}

/*
 * indirect mode (jmp only) - eg. jmp ($1234)
 */
static uint16_t ind(VrEmu6502* vr6502)
{
  if (vr6502->model == CPU_6502)
  {
    /* 6502 had a bug if the low byte was $ff, the high address would come from
       the same page rather than the next page */
    uint16_t addr = read16Bug(vr6502, vr6502->pc++);
    return read16(vr6502, addr);
  }

  uint16_t addr = read16(vr6502, vr6502->pc++);
  return read16(vr6502, addr);
}

/*
 * indirect x mode (jmp only) - eg. jmp ($1234, x)
 */
static uint16_t indx(VrEmu6502* vr6502)
{
  uint16_t addr = read16(vr6502, vr6502->pc++) + vr6502->ix;
  return read16(vr6502, addr);
}

/*
 * relative mode (branch instructions) - eg. bne -5
 */
static uint16_t rel(VrEmu6502* vr6502)
{
  int8_t offset = (int8_t)vr6502->readFn(vr6502->pc, false);
  return vr6502->pc++ + offset + 1;
}

/*
 * indexed indirect mode - eg. lda ($12, x)
 */
static uint16_t xin(VrEmu6502* vr6502)
{
  return read16Wrapped(vr6502, (vr6502->zpBase + vr6502->readFn(vr6502->pc++, false) + vr6502->ix) & 0xff);
}

/*
 * indirect indexed mode - eg. lda ($12), y
 */
static uint16_t yin(VrEmu6502* vr6502)
{
  uint16_t base = read16Wrapped(vr6502, vr6502->zpBase + vr6502->readFn(vr6502->pc++, false));
  uint16_t addr = base + vr6502->iy;
  return addr;
}

/*
 * indirect indexed mode (with page boundary cycle check) - eg. lda ($12), y
 */
static uint16_t yip(VrEmu6502* vr6502)
{
  uint16_t base = read16Wrapped(vr6502, vr6502->zpBase + vr6502->readFn(vr6502->pc++, false));
  uint16_t addr = base + vr6502->iy;
  pageBoundary(vr6502, base, addr);
  return addr;
}

/*
 * zero page - eg. lda $12
 */
static uint16_t zp(VrEmu6502* vr6502)
{
  return vr6502->zpBase + vr6502->readFn(vr6502->pc++, false);
}

/*
 * indirect mode - eg. lda ($12)
 */
static uint16_t zpi(VrEmu6502* vr6502)
{
  return read16Wrapped(vr6502, vr6502->zpBase + vr6502->readFn(vr6502->pc++, false));
}

/*
 * zero page indexed x - eg. lda $12, x
 */
static uint16_t zpx(VrEmu6502* vr6502)
{
  return vr6502->zpBase + ((vr6502->readFn(vr6502->pc++, false) + vr6502->ix) & 0xff);
}

/*
 * zero page indexed y - eg. lda $12, y
 */
static uint16_t zpy(VrEmu6502* vr6502)
{
  return vr6502->zpBase + ((vr6502->readFn(vr6502->pc++, false) + vr6502->iy) & 0xff);
}

/*
 * temporary addresss used in illegal opcodes
 */
static uint16_t tmp(VrEmu6502* vr6502)
{
  return vr6502->tmpAddr;
}

/*
 * accumulator mode (no address) - eg. ror
 */
#define acc NULL

 /*
  * implied mode (no address) - eg. tax
  */
#define imp NULL


  /* ------------------------------------------------------------------
   *  INSTRUCTIONS
   * ----------------------------------------------------------------*/

   /*
    * add with carry (deicmal mode)
    */
static void adcd(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  uint8_t value = vr6502->readFn(modeAddr(vr6502), false);

  uint8_t vu = value & 0x0f;
  uint8_t vt = (value & 0xf0) >> 4;

  uint8_t au = vr6502->ac & 0x0f;
  uint8_t at = (vr6502->ac & 0xf0) >> 4;

  uint8_t units = vu + au + testBit(vr6502, FlagC);
  uint8_t tens = vt + at;

  uint8_t tc = 0;

  if (units > 0x09)
  {
    tc = 1;
    tens += 0x01;
    units += 0x06;
  }
  if (tens > 0x09)
  {
    tens += 0x06;
  }

  /* 65c02 takes one extra cycle in decimal mode */
  if (is65c02(vr6502))
  {
    ++vr6502->step;

    /* set V flag */
    if (at & 0x08) at |= 0xf0;
    if (vt & 0x08) vt |= 0xf0;

    int8_t res = (int8_t)(at + vt + tc);

    setOrClearBit(vr6502, FlagV, res < -8 || res > 7);

    setNZ(vr6502, vr6502->ac = (tens << 4) | (units & 0x0f));
  }
  else
  {
    uint16_t binResult = vr6502->ac + value + testBit(vr6502, FlagC);
    setNZ(vr6502, vr6502->ac = (tens << 4) | (units & 0x0f));
    setOrClearBit(vr6502, FlagZ, (binResult & 0xff) == 0);
  }

  setOrClearBit(vr6502, FlagC, tens & 0xf0);
}

/*
 * add with carry
 */
static void adc(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  if (testBit(vr6502, FlagD))
  {
    adcd(vr6502, modeAddr);
  }
  else
  {
    uint8_t opr = vr6502->readFn(modeAddr(vr6502), false);
    uint16_t result = vr6502->ac + opr + testBit(vr6502, FlagC);

    setOrClearBit(vr6502, FlagC, result > 0xff);
    setOrClearBit(vr6502, FlagV, ((vr6502->ac ^ result) & (opr ^ result) & 0x80));

    setNZ(vr6502, vr6502->ac = (uint8_t)result);
  }
}

/*
 * bitwise and
 */
static void and (VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  setNZ(vr6502, vr6502->ac &= vr6502->readFn(modeAddr(vr6502), false));
}

/*
 * aritmetic shift left
 */
static void asl(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  if (modeAddr == acc)
  {
    setOrClearBit(vr6502, FlagC, vr6502->ac & 0x80);
    setNZ(vr6502, vr6502->ac <<= 1);
    return;
  }

  uint16_t addr = modeAddr(vr6502);
  uint8_t result = vr6502->readFn(addr, false);
  setOrClearBit(vr6502, FlagC, result & 0x80);
  setNZ(vr6502, result <<= 1);
  vr6502->writeFn(addr, result);
}

/*
 * branch (always)
 */
static void bra(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  ++vr6502->step; /* extra because we branched */

  uint16_t addr = modeAddr(vr6502);
  pageBoundary(vr6502, vr6502->pc, addr);
  vr6502->pc = addr;
}

/*
 * branch if carry clear
 */
static void bcc(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  if (!testBit(vr6502, FlagC)) bra(vr6502, modeAddr); else modeAddr(vr6502);
}

/*
 * branch if carry set
 */
static void bcs(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  if (testBit(vr6502, FlagC)) bra(vr6502, modeAddr); else modeAddr(vr6502);
}

/*
 * branch if equal (if zero set)
 */
static void beq(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  if (testBit(vr6502, FlagZ)) bra(vr6502, modeAddr); else modeAddr(vr6502);
}

/*
 * bit test
 */
static void bit(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  uint8_t val = vr6502->readFn(modeAddr(vr6502), false);

  if (modeAddr != imm)
  {
    /* set N and V to match bits 7 and 6 of value at addr */
    setOrClearBit(vr6502, FlagV, val & 0x40);
    setOrClearBit(vr6502, FlagN, val & 0x80);
  }

  /* set Z if (acc & value at addr) is zero */
  setOrClearBit(vr6502, FlagZ, !(val & vr6502->ac));
}

/*
 * branch if minus (if N bit set)
 */
static void bmi(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  if (testBit(vr6502, FlagN)) bra(vr6502, modeAddr); else modeAddr(vr6502);
}

/*
 * branch if not equal (if Z bit not set)
 */
static void bne(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  if (!testBit(vr6502, FlagZ)) bra(vr6502, modeAddr); else modeAddr(vr6502);
}

/*
 * branch if plus (if N bit not set)
 */
static void bpl(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  if (!testBit(vr6502, FlagN)) bra(vr6502, modeAddr); else modeAddr(vr6502);
}

/*
 * trigger a software interrupt
 */
static void brk(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  push(vr6502, (++vr6502->pc) >> 8);
  push(vr6502, (vr6502->pc) & 0xff);
  push(vr6502, vr6502->flags | FlagU | FlagB);
  setBit(vr6502, FlagI);

  if (is65c02(vr6502))
  {
    clearBit(vr6502, FlagD);
  }

  vr6502->pc = read16(vr6502, 0xFFFE);
}

/*
 * branch if overflow clear
 */
static void bvc(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  if (!testBit(vr6502, FlagV)) bra(vr6502, modeAddr); else modeAddr(vr6502);
}

/*
 * branch if overflow set
 */
static void bvs(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  if (testBit(vr6502, FlagV)) bra(vr6502, modeAddr); else modeAddr(vr6502);
}

/*
 * clear carry flag
 */
static void clc(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  clearBit(vr6502, FlagC);
}

/*
 * clear decimal flag
 */
static void cld(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  clearBit(vr6502, FlagD);
}

/*
 * clear interrupt flag
 */
static void cli(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  clearBit(vr6502, FlagI);
}

/*
 * clear overflow flag
 */
static void clv(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  clearBit(vr6502, FlagV);
}

/*
 * compare value with accumulator
 */
static void cmp(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  uint8_t opr = ~vr6502->readFn(modeAddr(vr6502), false);
  uint16_t result = vr6502->ac + opr + 1;

  setOrClearBit(vr6502, FlagC, result > 0xff);
  setNZ(vr6502, (uint8_t)result);
}

/*
 * compare value with x register
 */
static void cpx(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  uint8_t opr = ~vr6502->readFn(modeAddr(vr6502), false);
  uint16_t result = vr6502->ix + opr + 1;

  setOrClearBit(vr6502, FlagC, result > 0xff);
  setNZ(vr6502, (uint8_t)result);
}

/*
 * compare value with y register
 */
static void cpy(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  uint8_t opr = ~vr6502->readFn(modeAddr(vr6502), false);
  uint16_t result = vr6502->iy + opr + 1;

  setOrClearBit(vr6502, FlagC, result > 0xff);
  setNZ(vr6502, (uint8_t)result);
}

/*
 * decrement value
 */
static void dec(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  if (modeAddr == acc)
  {
    setNZ(vr6502, --vr6502->ac);
    return;
  }

  uint16_t addr = modeAddr(vr6502);
  uint8_t val = vr6502->readFn(addr, false) - 1;
  setNZ(vr6502, val);
  vr6502->writeFn(addr, val);
}

/*
 * decrement x register
 */
static void dex(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  setNZ(vr6502, --vr6502->ix);
}

/*
 * decrement y register
 */
static void dey(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  setNZ(vr6502, --vr6502->iy);
}

/*
 * exclusive or with accumulator
 */
static void eor(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  setNZ(vr6502, vr6502->ac ^= vr6502->readFn(modeAddr(vr6502), false));
}

/*
 * increment value
 */
static void inc(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  if (modeAddr == acc)
  {
    setNZ(vr6502, ++vr6502->ac);
    return;
  }

  uint16_t addr = modeAddr(vr6502);
  uint8_t val = vr6502->readFn(addr, false) + 1;
  setNZ(vr6502, val);
  vr6502->writeFn(addr, val);
}

/*
 * increment x register
 */
static void inx(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  setNZ(vr6502, ++vr6502->ix);
}

/*
 * increment y register
 */
static void iny(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  setNZ(vr6502, ++vr6502->iy);
}

/*
 * jump to address
 */
static void jmp(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  vr6502->pc = modeAddr(vr6502);
}

/*
 * jump to subroutine
 */
static void jsr(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  uint16_t addr = modeAddr(vr6502);
  push(vr6502, --vr6502->pc >> 8);
  push(vr6502, vr6502->pc & 0xff);
  vr6502->pc = addr;
}

/*
 * load a value to the accumulator
 */
static void lda(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  setNZ(vr6502, vr6502->ac = vr6502->readFn(modeAddr(vr6502), false));
}

/*
 * load a value to the x register
 */
static void ldx(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  setNZ(vr6502, vr6502->ix = vr6502->readFn(modeAddr(vr6502), false));
}

/*
 * load a value to the y register
 */
static void ldy(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  setNZ(vr6502, vr6502->iy = vr6502->readFn(modeAddr(vr6502), false));
}

/*
 * logical shift left
 */
static void lsr(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  if (modeAddr == acc)
  {
    setOrClearBit(vr6502, FlagC, vr6502->ac & 0x01);
    setNZ(vr6502, vr6502->ac >>= 1);
    return;
  }

  uint16_t addr = modeAddr(vr6502);
  uint8_t result = vr6502->readFn(addr, false);
  setOrClearBit(vr6502, FlagC, result & 0x01);
  setNZ(vr6502, result >>= 1);
  vr6502->writeFn(addr, result);
}

/*
 * no operation
 */
static void nop(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  if (modeAddr) modeAddr(vr6502);
}

/*
 * load and discard (other 65c02 nops)
 */
static void ldd(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  if (modeAddr)
  {
    /* we still want to read the data (and discard it) */
    vr6502->readFn(modeAddr(vr6502), false);
  }
}

/*
 * bitwise or with acculmulator
 */
static void ora(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  setNZ(vr6502, vr6502->ac |= vr6502->readFn(modeAddr(vr6502), false));
}

/*
 * push accumulator to stack
 */
static void pha(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  push(vr6502, vr6502->ac);
}

/*
 * push status register to stack
 */
static void php(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  push(vr6502, vr6502->flags | FlagB | FlagU);
}

/*
 * push x register to stack
 */
static void phx(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  push(vr6502, vr6502->ix);
}

/*
 * push y register to stack
 */
static void phy(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  push(vr6502, vr6502->iy);
}

/*
 * pop from stack to accumulator
 */
static void pla(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  setNZ(vr6502, vr6502->ac = pop(vr6502));
}

/*
 * pop from stack to status register
 */
static void plp(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  vr6502->flags = pop(vr6502) | FlagU | FlagB;
}

/*
 * pop from stack to x register
 */
static void plx(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  setNZ(vr6502, vr6502->ix = pop(vr6502));
}

/*
 * pop from stack to y register
 */
static void ply(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  setNZ(vr6502, vr6502->iy = pop(vr6502));
}

/*
 * rotate value left
 */
static void rol(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  if (modeAddr == acc)
  {
    bool tc = vr6502->ac & 0x80;
    vr6502->ac = (vr6502->ac << 1) | testBit(vr6502, FlagC);
    setOrClearBit(vr6502, FlagC, tc);
    setNZ(vr6502, vr6502->ac);
    return;
  }

  uint16_t addr = modeAddr(vr6502);
  uint8_t val = vr6502->readFn(addr, false);
  bool tc = val & 0x80;
  val = (val << 1) | testBit(vr6502, FlagC);
  setOrClearBit(vr6502, FlagC, tc);
  setNZ(vr6502, val);
  vr6502->writeFn(addr, val);
}

/*
 * rotate value right
 */
static void ror(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  if (modeAddr == acc)
  {
    bool tc = vr6502->ac & 0x01;
    vr6502->ac = (vr6502->ac >> 1) | (testBit(vr6502, FlagC) * 0x80);
    setOrClearBit(vr6502, FlagC, tc);
    setNZ(vr6502, vr6502->ac);
    return;
  }

  uint16_t addr = modeAddr(vr6502);
  uint8_t val = vr6502->readFn(addr, false);
  bool tc = val & 0x01;
  val = (val >> 1) | (testBit(vr6502, FlagC) * 0x80);
  setOrClearBit(vr6502, FlagC, tc);
  setNZ(vr6502, val);
  vr6502->writeFn(addr, val);
}

/*
 * return from interrupt
 */
static void rti(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  vr6502->flags = pop(vr6502) | FlagU | FlagB;
  vr6502->pc = pop(vr6502);
  vr6502->pc |= pop(vr6502) << 8;
}

/*
 * return from subroutine
 */
static void rts(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  vr6502->pc = pop(vr6502);
  vr6502->pc |= pop(vr6502) << 8;
  ++vr6502->pc;
}

/*
 * subtract with carry (decimal mode)
 */
static void sbcd(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  uint8_t value = vr6502->readFn(modeAddr(vr6502), false);
  uint16_t binResult = vr6502->ac + ~value + testBit(vr6502, FlagC);

  uint16_t result = 0;
  if (vr6502->model == CPU_6502)
  {
    uint16_t tmp = (vr6502->ac & 0x0f) - (value & 0x0f) - !testBit(vr6502, FlagC);

    if (tmp & 0x8000)
    {
      tmp = ((tmp - 0x06) & 0x0f) - 0x10;
    }

    result = (vr6502->ac & 0xf0) - (value & 0xf0) + tmp;
    if (result & 0x8000)
    {
      result -= 0x60;
    }

    setNZ(vr6502, (uint8_t)binResult);
    setOrClearBit(vr6502, FlagV, (int16_t)result < -128 || (int16_t)result > 127);
  }
  else
  {
    uint16_t tmp = (vr6502->ac & 0x0f) - (value & 0x0f) - !testBit(vr6502, FlagC);

    result = vr6502->ac - value - !testBit(vr6502, FlagC);

    if (result & 0x8000)
    {
      result -= 0x60;
    }
    if (tmp & 0x8000)
    {
      result -= 0x06;
    }

    ++vr6502->step;

    /* set V flag */
    setOrClearBit(vr6502, FlagV, ((vr6502->ac ^ binResult) & (~value ^ binResult) & 0x80));
    setNZ(vr6502, (uint8_t)result);
  }


  setOrClearBit(vr6502, FlagC, (uint16_t)result <= (uint16_t)vr6502->ac || (result & 0xff0) == 0xff0);

  vr6502->ac = result & 0xff;
}

/*
 * subtract with carry
 */
static void sbc(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  if (testBit(vr6502, FlagD))
  {
    sbcd(vr6502, modeAddr);
    return;
  }

  uint8_t opr = ~vr6502->readFn(modeAddr(vr6502), false);
  uint16_t result = vr6502->ac + opr + testBit(vr6502, FlagC);

  setOrClearBit(vr6502, FlagC, result > 0xff);
  setOrClearBit(vr6502, FlagV, ((vr6502->ac ^ result) & (opr ^ result) & 0x80));
  vr6502->ac = (uint8_t)result;

  setNZ(vr6502, (uint8_t)result);
}

/*
 * set carry flag
 */
static void sec(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  setBit(vr6502, FlagC);
}

/*
 * set decimal flag
 */
static void sed(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  setBit(vr6502, FlagD);
}

/*
 * set interrupt disable flag
 */
static void sei(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  setBit(vr6502, FlagI);
}

/*
 * store accumulator to address
 */
static void sta(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  vr6502->writeFn(modeAddr(vr6502), vr6502->ac);
}

/*
 * store x register to address
 */
static void stx(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  vr6502->writeFn(modeAddr(vr6502), vr6502->ix);
}

/*
 * store y register to address
 */
static void sty(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  vr6502->writeFn(modeAddr(vr6502), vr6502->iy);
}

/*
 * store 0 to address
 */
static void stz(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  vr6502->writeFn(modeAddr(vr6502), 0);
}

/*
 * transfer accumulator to x register
 */
static void tax(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  setNZ(vr6502, vr6502->ix = vr6502->ac);
}

/*
 * transfer accumulator to y register
 */
static void tay(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  setNZ(vr6502, vr6502->iy = vr6502->ac);
}

/*
 * transfer stack pointer register to x register
 */
static void tsx(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  setNZ(vr6502, vr6502->ix = vr6502->sp);
}

/*
 * transfer x register to accumulator
 */
static void txa(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  setNZ(vr6502, vr6502->ac = vr6502->ix);
}

/*
 * transfer  x register to stack pointer register
 */
static void txs(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  vr6502->sp = vr6502->ix;
}

/*
 * transfer y register to accumulator
 */
static void tya(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  setNZ(vr6502, vr6502->ac = vr6502->iy);
}

/*
 * test and reset (clear) bit
 */
static void trb(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  uint16_t addr = modeAddr(vr6502);
  uint8_t temp = vr6502->readFn(addr, false);
  vr6502->writeFn(addr, temp & ~vr6502->ac);
  setOrClearBit(vr6502, FlagZ, !(temp & vr6502->ac));
}

/*
 * test and set bit
 */
static void tsb(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  uint16_t addr = modeAddr(vr6502);
  uint8_t temp = vr6502->readFn(addr, false);
  vr6502->writeFn(addr, temp | vr6502->ac);
  setOrClearBit(vr6502, FlagZ, !(temp & vr6502->ac));
}

/*
 * reset a bit (zero page only)
 */
static void rmb(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr, int bitIndex)
{
  uint16_t addr = modeAddr(vr6502);
  vr6502->writeFn(addr, vr6502->readFn(addr, false) & ~(0x01 << bitIndex));
}

static void rmb0(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { rmb(vr6502, modeAddr, 0); }
static void rmb1(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { rmb(vr6502, modeAddr, 1); }
static void rmb2(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { rmb(vr6502, modeAddr, 2); }
static void rmb3(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { rmb(vr6502, modeAddr, 3); }
static void rmb4(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { rmb(vr6502, modeAddr, 4); }
static void rmb5(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { rmb(vr6502, modeAddr, 5); }
static void rmb6(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { rmb(vr6502, modeAddr, 6); }
static void rmb7(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { rmb(vr6502, modeAddr, 7); }


/*
 * set a bit (zero page only)
 */
static void smb(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr, int bitIndex)
{
  uint16_t addr = modeAddr(vr6502);
  vr6502->writeFn(addr, vr6502->readFn(addr, false) | (0x01 << bitIndex));
}

static void smb0(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { smb(vr6502, modeAddr, 0); }
static void smb1(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { smb(vr6502, modeAddr, 1); }
static void smb2(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { smb(vr6502, modeAddr, 2); }
static void smb3(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { smb(vr6502, modeAddr, 3); }
static void smb4(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { smb(vr6502, modeAddr, 4); }
static void smb5(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { smb(vr6502, modeAddr, 5); }
static void smb6(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { smb(vr6502, modeAddr, 6); }
static void smb7(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { smb(vr6502, modeAddr, 7); }

/*
 * branch if bit reset (clear)
 */
static void bbr(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr, int bitIndex)
{
  uint8_t val = vr6502->readFn(modeAddr(vr6502), false);
  if (!(val & (0x01 << bitIndex)))
  {
    vr6502->pc = rel(vr6502);
  }
  else
  {
    ++vr6502->pc;
  }
}

static void bbr0(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { bbr(vr6502, modeAddr, 0); }
static void bbr1(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { bbr(vr6502, modeAddr, 1); }
static void bbr2(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { bbr(vr6502, modeAddr, 2); }
static void bbr3(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { bbr(vr6502, modeAddr, 3); }
static void bbr4(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { bbr(vr6502, modeAddr, 4); }
static void bbr5(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { bbr(vr6502, modeAddr, 5); }
static void bbr6(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { bbr(vr6502, modeAddr, 6); }
static void bbr7(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { bbr(vr6502, modeAddr, 7); }

/*
 * branch if bit set (clear)
 */
static void bbs(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr, int bitIndex)
{
  uint8_t val = vr6502->readFn(modeAddr(vr6502), false);
  if ((val & (0x01 << bitIndex)))
  {
    vr6502->pc = rel(vr6502);
  }
  else
  {
    ++vr6502->pc;
  }
}

static void bbs0(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { bbs(vr6502, modeAddr, 0); }
static void bbs1(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { bbs(vr6502, modeAddr, 1); }
static void bbs2(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { bbs(vr6502, modeAddr, 2); }
static void bbs3(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { bbs(vr6502, modeAddr, 3); }
static void bbs4(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { bbs(vr6502, modeAddr, 4); }
static void bbs5(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { bbs(vr6502, modeAddr, 5); }
static void bbs6(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { bbs(vr6502, modeAddr, 6); }
static void bbs7(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr) { bbs(vr6502, modeAddr, 7); }

/*
 * stop the processor
 */
static void stp(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  vr6502->stp = true;
}

/*
 * wait for interrupt
 */
static void wai(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  vr6502->wai = true;
}

/*
 * invalid opcode (not supported by this emulator)
 */
static void err(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  if (modeAddr) modeAddr(vr6502);
}

/* ------------------------------------------------------------------
 *  UNDOCUMENTED OPCODES
 * ----------------------------------------------------------------*/

 /*
  * aritmetic shift left and bitwise-or with accumulator
  */
static void slo(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  vr6502->tmpAddr = modeAddr(vr6502);
  asl(vr6502, tmp);
  ora(vr6502, tmp);
}

/*
 * rotate left and bitwise-and with accumulator
 */
static void rla(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  vr6502->tmpAddr = modeAddr(vr6502);
  rol(vr6502, tmp);
  and (vr6502, tmp);
}

/*
 * logical shift right and bitwise-xor with accumulator
 */
static void sre(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  vr6502->tmpAddr = modeAddr(vr6502);
  lsr(vr6502, tmp);
  eor(vr6502, tmp);
}

/*
 * rotate right and add with carry
 */
static void rra(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  vr6502->tmpAddr = modeAddr(vr6502);
  ror(vr6502, tmp);
  adc(vr6502, tmp);
}

/*
 * store accumulator bitwise-and x register to address
 */
static void sax(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  vr6502->writeFn(modeAddr(vr6502), vr6502->ac & vr6502->ix);
}

/*
 * load accumulator and x register from address
 */
static void lax(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  setNZ(vr6502, vr6502->ac = vr6502->ix = vr6502->readFn(modeAddr(vr6502), false));
}

/*
 * decrement then compare address
 */
static void dcp(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  vr6502->tmpAddr = modeAddr(vr6502);
  dec(vr6502, tmp);
  cmp(vr6502, tmp);
}

/*
 * increment then subtract with carry
 */
static void isc(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  vr6502->tmpAddr = modeAddr(vr6502);
  inc(vr6502, tmp);
  sbc(vr6502, tmp);
}

/*
 * bitwise-and then set carry to bit 7
 */
static void anc(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  and (vr6502, modeAddr);
  setOrClearBit(vr6502, FlagC, testBit(vr6502, FlagN));
}

/*
 * bitwise-and with value then shift right in accumulator
 */
static void alr(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  and (vr6502, modeAddr);
  lsr(vr6502, acc);
}

/*
 * bitwise-and with value then rotate right in accumulator (sets V)
 */
static void arr(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  and (vr6502, modeAddr);
  setOrClearBit(vr6502, FlagV, ((bool)vr6502->ac & 0x80) ^ ((bool)vr6502->ac & 0x40));
  vr6502->ac = (vr6502->ac >> 1) | (testBit(vr6502, FlagC) * 0x80);
}

/*
 * bitwise-and x with accumulator, subtract value and store in x
 */
static void sbx(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  uint8_t opr = ~vr6502->readFn(modeAddr(vr6502), false);
  uint16_t result = (vr6502->ac & vr6502->ix) + opr + 1;

  setOrClearBit(vr6502, FlagC, result > 0xff);
  setNZ(vr6502, vr6502->ix = (uint8_t)result);
}

/*
 * bitwise-and x with sp, store in accumulator, x and sp
 */
static void las(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  vr6502->sp &= vr6502->readFn(modeAddr(vr6502), false);
  setNZ(vr6502, vr6502->ac = vr6502->ix = vr6502->sp);
}

/*
 * store bitwise-and of accumulator, x and ((high byte of addr) + 1)
 */
static void sha(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  uint16_t addr = modeAddr(vr6502);
  vr6502->writeFn(addr, vr6502->ac & vr6502->ix & (uint8_t)((addr >> 8) + 1));
}

/*
 * store bitwise-and of x and ((high byte of addr) + 1)
 */
static void shx(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  uint16_t addr = modeAddr(vr6502);
  vr6502->writeFn(addr, vr6502->ix & (uint8_t)((addr >> 8) + 1));
}

/*
 * store bitwise-and of y and ((high byte of addr) + 1)
 */
static void shy(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  uint16_t addr = modeAddr(vr6502);
  vr6502->writeFn(addr, vr6502->iy & (uint8_t)((addr >> 8) + 1));
}

/*
 * store bitwise-and of y and ((high byte of addr) + 1)
 */
static void tas(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  uint16_t addr = modeAddr(vr6502);
  vr6502->sp = vr6502->ac & vr6502->ix;
  vr6502->writeFn(addr, vr6502->sp & (uint8_t)((addr >> 8) + 1));
}

/*
 * bitwise-or acc with magic, bitwise-and with x, bitwise-and
 * with value, store in accumulator
 */
static void ane(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  vr6502->ac = (vr6502->ac | UNSTABLE_MAGIC_CONST) & vr6502->ix;
  vr6502->ac &= vr6502->readFn(modeAddr(vr6502), false);
  setNZ(vr6502, vr6502->ac);
}

/*
 * bitwise-or acc with magic, bitwise-and with value, store in accumulator
 */
static void lxa(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  vr6502->ac = (vr6502->ac | UNSTABLE_MAGIC_CONST);
  vr6502->ac &= vr6502->readFn(modeAddr(vr6502), false);
  setNZ(vr6502, vr6502->ac);
}

/*
 * halt/jam cpu
 */
static void jam(VrEmu6502* vr6502, vrEmu6502AddrModeFn modeAddr)
{
  (void)modeAddr;

  vr6502->readFn(imm(vr6502), false);
  vr6502->stp = true;
}

/* ------------------------------------------------------------------
 *  OPCODE TABLES
 * ----------------------------------------------------------------*/

#define invalid { err, imp, 2 }
#define unnop11 { nop, imp, 1 }
#define ldd_imm { ldd, imm, 2 }
#define kil     { jam, imp, 1 }


static const vrEmu6502Opcode _std6502[256] = {
  /*      |      _0      |      _1      |      _2      |      _3      |      _4      |      _5      |      _6      |      _7      |      _8      |      _9      |      _A      |      _B      |      _C      |      _D      |      _E      |      _F      | */
  /* 0_ */ {brk, imp, 7}, {ora, xin, 6},    invalid   ,    invalid   ,    invalid   , {ora,  zp, 3}, {asl,  zp, 5},    invalid   , {php, imp, 3}, {ora, imm, 2}, {asl, acc, 2},    invalid   ,    invalid   , {ora,  ab, 4}, {asl,  ab, 6},    invalid   ,
  /* 1_ */ {bpl, rel, 2}, {ora, yip, 5},    invalid   ,    invalid   ,    invalid   , {ora, zpx, 4}, {asl, zpx, 6},    invalid   , {clc, imp, 2}, {ora, ayp, 4},    invalid   ,    invalid   ,    invalid   , {ora, axp, 4}, {asl, abx, 7},    invalid   ,
  /* 2_ */ {jsr,  ab, 6}, { and, xin, 6},    invalid   ,    invalid   , {bit,  zp, 3}, { and,  zp, 3}, {rol,  zp, 5},    invalid   , {plp, imp, 4}, { and, imm, 2}, {rol, acc, 2},    invalid   , {bit,  ab, 4}, { and,  ab, 4}, {rol,  ab, 6},    invalid   ,
  /* 3_ */ {bmi, rel, 2}, { and, yip, 5},    invalid   ,    invalid   ,    invalid   , { and, zpx, 4}, {rol, zpx, 6},    invalid   , {sec, imp, 2}, { and, ayp, 4},    invalid   ,    invalid   ,    invalid   , { and, axp, 4}, {rol, abx, 7},    invalid   ,
  /* 4_ */ {rti, imp, 6}, {eor, xin, 6},    invalid   ,    invalid   ,    invalid   , {eor,  zp, 3}, {lsr,  zp, 5},    invalid   , {pha, imp, 3}, {eor, imm, 2}, {lsr, acc, 2},    invalid   , {jmp,  ab, 3}, {eor,  ab, 4}, {lsr,  ab, 6},    invalid   ,
  /* 5_ */ {bvc, rel, 2}, {eor, yip, 5},    invalid   ,    invalid   ,    invalid   , {eor, zpx, 4}, {lsr, zpx, 6},    invalid   , {cli, imp, 2}, {eor, ayp, 4},    invalid   ,    invalid   ,    invalid   , {eor, axp, 4}, {lsr, abx, 7},    invalid   ,
  /* 6_ */ {rts, imp, 6}, {adc, xin, 6},    invalid   ,    invalid   ,    invalid   , {adc,  zp, 3}, {ror,  zp, 5},    invalid   , {pla, imp, 4}, {adc, imm, 2}, {ror, acc, 2},    invalid   , {jmp, ind, 5}, {adc,  ab, 4}, {ror,  ab, 6},    invalid   ,
  /* 7_ */ {bvs, rel, 2}, {adc, yip, 5},    invalid   ,    invalid   ,    invalid   , {adc, zpx, 4}, {ror, zpx, 6},    invalid   , {sei, imp, 2}, {adc, ayp, 4},    invalid   ,    invalid   ,    invalid   , {adc, axp, 4}, {ror, abx, 7},    invalid   ,
  /* 8_ */    invalid   , {sta, xin, 6},    invalid   ,    invalid   , {sty,  zp, 3}, {sta,  zp, 3}, {stx,  zp, 3},    invalid   , {dey, imp, 2},    invalid   , {txa, imp, 2},    invalid   , {sty,  ab, 4}, {sta,  ab, 4}, {stx,  ab, 4},    invalid   ,
  /* 9_ */ {bcc, rel, 2}, {sta, yin, 6},    invalid   ,    invalid   , {sty, zpx, 4}, {sta, zpx, 4}, {stx, zpy, 4},    invalid   , {tya, imp, 2}, {sta, aby, 5}, {txs, imp, 2},    invalid   ,    invalid   , {sta, abx, 5},    invalid   ,    invalid   ,
  /* A_ */ {ldy, imm, 2}, {lda, xin, 6}, {ldx, imm, 2},    invalid   , {ldy,  zp, 3}, {lda,  zp, 3}, {ldx,  zp, 3},    invalid   , {tay, imp, 2}, {lda, imm, 2}, {tax, imp, 2},    invalid   , {ldy,  ab, 4}, {lda,  ab, 4}, {ldx,  ab, 4},    invalid   ,
  /* B_ */ {bcs, rel, 2}, {lda, yip, 5},    invalid   ,    invalid   , {ldy, zpx, 4}, {lda, zpx, 4}, {ldx, zpy, 4},    invalid   , {clv, imp, 2}, {lda, ayp, 4}, {tsx, imp, 2},    invalid   , {ldy, axp, 4}, {lda, axp, 4}, {ldx, ayp, 4},    invalid   ,
  /* C_ */ {cpy, imm, 2}, {cmp, xin, 6},    invalid   ,    invalid   , {cpy,  zp, 3}, {cmp,  zp, 3}, {dec,  zp, 5},    invalid   , {iny, imp, 2}, {cmp, imm, 2}, {dex, imp, 2},    invalid   , {cpy,  ab, 4}, {cmp,  ab, 4}, {dec,  ab, 6},    invalid   ,
  /* D_ */ {bne, rel, 2}, {cmp, yip, 5},    invalid   ,    invalid   ,    invalid   , {cmp, zpx, 4}, {dec, zpx, 6},    invalid   , {cld, imp, 2}, {cmp, ayp, 4},    invalid   ,    invalid   ,    invalid   , {cmp, axp, 4}, {dec, abx, 7},    invalid   ,
  /* E_ */ {cpx, imm, 2}, {sbc, xin, 6},    invalid   ,    invalid   , {cpx,  zp, 3}, {sbc,  zp, 3}, {inc,  zp, 5},    invalid   , {inx, imp, 2}, {sbc, imm, 2}, {nop, imp, 2},    invalid   , {cpx,  ab, 4}, {sbc,  ab, 4}, {inc,  ab, 6},    invalid   ,
  /* F_ */ {beq, rel, 2}, {sbc, yip, 5},    invalid   ,    invalid   ,    invalid   , {sbc, zpx, 4}, {inc, zpx, 6},    invalid   , {sed, imp, 2}, {sbc, ayp, 4},    invalid   ,    invalid   ,    invalid   , {sbc, axp, 4}, {inc, abx, 7},    invalid };

static const vrEmu6502Opcode _std6502U[256] = {
  /*      |      _0      |      _1      |      _2      |      _3      |      _4      |      _5      |      _6      |      _7      |      _8      |      _9      |      _A      |      _B      |      _C      |      _D      |      _E      |      _F      | */
  /* 0_ */ {brk, imp, 7}, {ora, xin, 6},      kil     , {slo, xin, 8}, {nop,  zp, 3}, {ora,  zp, 3}, {asl,  zp, 5}, {slo,  zp, 5}, {php, imp, 3}, {ora, imm, 2}, {asl, acc, 2}, {anc, imm, 2}, {nop,  ab, 4}, {ora,  ab, 4}, {asl,  ab, 6}, {slo,  ab, 6},
  /* 1_ */ {bpl, rel, 2}, {ora, yip, 5},      kil     , {slo, yin, 8}, {nop, zpx, 4}, {ora, zpx, 4}, {asl, zpx, 6}, {slo, zpx, 6}, {clc, imp, 2}, {ora, ayp, 4}, {nop, imp, 2}, {slo, aby, 7}, {nop, axp, 4}, {ora, axp, 4}, {asl, abx, 7}, {slo, abx, 7},
  /* 2_ */ {jsr,  ab, 6}, { and, xin, 6},      kil     , {rla, xin, 8}, {bit,  zp, 3}, { and,  zp, 3}, {rol,  zp, 5}, {rla,  zp, 5}, {plp, imp, 4}, { and, imm, 2}, {rol, acc, 2}, {anc, imm, 2}, {bit,  ab, 4}, { and,  ab, 4}, {rol,  ab, 6}, {rla,  ab, 6},
  /* 3_ */ {bmi, rel, 2}, { and, yip, 5},      kil     , {rla, yin, 8}, {nop, zpx, 4}, { and, zpx, 4}, {rol, zpx, 6}, {rla, zpx, 6}, {sec, imp, 2}, { and, ayp, 4}, {nop, imp, 2}, {rla, aby, 7}, {nop, axp, 4}, { and, axp, 4}, {rol, abx, 7}, {rla, abx, 7},
  /* 4_ */ {rti, imp, 6}, {eor, xin, 6},      kil     , {sre, xin, 8}, {nop,  zp, 3}, {eor,  zp, 3}, {lsr,  zp, 5}, {sre,  zp, 5}, {pha, imp, 3}, {eor, imm, 2}, {lsr, acc, 2}, {alr, imm, 2}, {jmp,  ab, 3}, {eor,  ab, 4}, {lsr,  ab, 6}, {sre,  ab, 6},
  /* 5_ */ {bvc, rel, 2}, {eor, yip, 5},      kil     , {sre, yin, 8}, {nop, zpx, 4}, {eor, zpx, 4}, {lsr, zpx, 6}, {sre, zpx, 6}, {cli, imp, 2}, {eor, ayp, 4}, {nop, imp, 2}, {sre, aby, 7}, {nop, axp, 4}, {eor, axp, 4}, {lsr, abx, 7}, {sre, abx, 7},
  /* 6_ */ {rts, imp, 6}, {adc, xin, 6},      kil     , {rra, xin, 8}, {nop,  zp, 3}, {adc,  zp, 3}, {ror,  zp, 5}, {rra,  zp, 5}, {pla, imp, 4}, {adc, imm, 2}, {ror, acc, 2}, {arr, imm, 2}, {jmp, ind, 5}, {adc,  ab, 4}, {ror,  ab, 6}, {rra,  ab, 6},
  /* 7_ */ {bvs, rel, 2}, {adc, yip, 5},      kil     , {rra, yin, 8}, {nop, zpx, 4}, {adc, zpx, 4}, {ror, zpx, 6}, {rra, zpx, 6}, {sei, imp, 2}, {adc, ayp, 4}, {nop, imp, 2}, {rra, aby, 7}, {nop, axp, 4}, {adc, axp, 4}, {ror, abx, 7}, {rra, abx, 7},
  /* 8_ */ {nop, imm, 2}, {sta, xin, 6}, {nop, imm, 2}, {sax, xin, 6}, {sty,  zp, 3}, {sta,  zp, 3}, {stx,  zp, 3}, {sax,  zp, 3}, {dey, imp, 2}, {nop, imm, 2}, {txa, imp, 2}, {ane, imm, 2}, {sty,  ab, 4}, {sta,  ab, 4}, {stx,  ab, 4}, {sax,  ab, 4},
  /* 9_ */ {bcc, rel, 2}, {sta, yin, 6},      kil     , {sha, zpy, 6}, {sty, zpx, 4}, {sta, zpx, 4}, {stx, zpy, 4}, {sax, zpy, 4}, {tya, imp, 2}, {sta, aby, 5}, {txs, imp, 2}, {tas, aby, 5}, {shy, abx, 5}, {sta, abx, 5}, {shx, aby, 5}, {sha, aby, 5},
  /* A_ */ {ldy, imm, 2}, {lda, xin, 6}, {ldx, imm, 2}, {lax, xin, 6}, {ldy,  zp, 3}, {lda,  zp, 3}, {ldx,  zp, 3}, {lax,  zp, 3}, {tay, imp, 2}, {lda, imm, 2}, {tax, imp, 2}, {lxa, imm, 2}, {ldy,  ab, 4}, {lda,  ab, 4}, {ldx,  ab, 4}, {lax,  ab, 4},
  /* B_ */ {bcs, rel, 2}, {lda, yip, 5},      kil     , {lax, yip, 5}, {ldy, zpx, 4}, {lda, zpx, 4}, {ldx, zpy, 4}, {lax, zpy, 4}, {clv, imp, 2}, {lda, ayp, 4}, {tsx, imp, 2}, {las, ayp, 4}, {ldy, axp, 4}, {lda, axp, 4}, {ldx, ayp, 4}, {lax, ayp, 4},
  /* C_ */ {cpy, imm, 2}, {cmp, xin, 6}, {nop, imm, 2}, {dcp, xin, 8}, {cpy,  zp, 3}, {cmp,  zp, 3}, {dec,  zp, 5}, {dcp,  zp, 5}, {iny, imp, 2}, {cmp, imm, 2}, {dex, imp, 2}, {sbx, imm, 2}, {cpy,  ab, 4}, {cmp,  ab, 4}, {dec,  ab, 6}, {dcp,  ab, 6},
  /* D_ */ {bne, rel, 2}, {cmp, yip, 5},      kil     , {dcp, yin, 8}, {nop, zpx, 4}, {cmp, zpx, 4}, {dec, zpx, 6}, {dcp, zpx, 6}, {cld, imp, 2}, {cmp, ayp, 4}, {nop, imp, 2}, {dcp, aby, 7}, {nop, axp, 4}, {cmp, axp, 4}, {dec, abx, 7}, {dcp, abx, 7},
  /* E_ */ {cpx, imm, 2}, {sbc, xin, 6}, {nop, imm, 2}, {isc, xin, 8}, {cpx,  zp, 3}, {sbc,  zp, 3}, {inc,  zp, 5}, {isc,  zp, 5}, {inx, imp, 2}, {sbc, imm, 2}, {nop, imp, 2}, {sbc, imm, 2}, {cpx,  ab, 4}, {sbc,  ab, 4}, {inc,  ab, 6}, {isc,  ab, 6},
  /* F_ */ {beq, rel, 2}, {sbc, yip, 5},      kil     , {isc, yin, 8}, {nop, zpx, 4}, {sbc, zpx, 4}, {inc, zpx, 6}, {isc, zpx, 6}, {sed, imp, 2}, {sbc, ayp, 4}, {nop, imp, 2}, {isc, aby, 7}, {nop, axp, 4}, {sbc, axp, 4}, {inc, abx, 7}, {isc, abx, 7} };

static const vrEmu6502Opcode _std65c02[256] = {
  /*      |      _0      |      _1      |      _2      |      _3      |      _4      |      _5      |      _6      |      _7      |      _8      |      _9      |      _A      |      _B      |      _C      |      _D      |      _E      |      _F      | */
  /* 0_ */ {brk, imp, 7}, {ora, xin, 6},    ldd_imm   ,    unnop11   , {tsb,  zp, 5}, {ora,  zp, 3}, {asl,  zp, 5},    unnop11   , {php, imp, 3}, {ora, imm, 2}, {asl, acc, 2},    unnop11   , {tsb,  ab, 6}, {ora,  ab, 4}, {asl,  ab, 6},    unnop11   ,
  /* 1_ */ {bpl, rel, 2}, {ora, yip, 5}, {ora, zpi, 5},    unnop11   , {trb,  zp, 5}, {ora, zpx, 4}, {asl, zpx, 6},    unnop11   , {clc, imp, 2}, {ora, ayp, 4}, {inc, acc, 2},    unnop11   , {trb,  ab, 6}, {ora, axp, 4}, {asl, axp, 6},    unnop11   ,
  /* 2_ */ {jsr,  ab, 6}, { and, xin, 6},    ldd_imm   ,    unnop11   , {bit,  zp, 3}, { and,  zp, 3}, {rol,  zp, 5},    unnop11   , {plp, imp, 4}, { and, imm, 2}, {rol, acc, 2},    unnop11   , {bit,  ab, 4}, { and,  ab, 4}, {rol,  ab, 6},    unnop11   ,
  /* 3_ */ {bmi, rel, 2}, { and, yip, 5}, { and, zpi, 5},    unnop11   , {bit, zpx, 4}, { and, zpx, 4}, {rol, zpx, 6},    unnop11   , {sec, imp, 2}, { and, ayp, 4}, {dec, acc, 2},    unnop11   , {bit, abx, 4}, { and, axp, 4}, {rol, axp, 6},    unnop11   ,
  /* 4_ */ {rti, imp, 6}, {eor, xin, 6},    ldd_imm   ,    unnop11   , {ldd,  zp, 3}, {eor,  zp, 3}, {lsr,  zp, 5},    unnop11   , {pha, imp, 3}, {eor, imm, 2}, {lsr, acc, 2},    unnop11   , {jmp,  ab, 3}, {eor,  ab, 4}, {lsr,  ab, 6},    unnop11   ,
  /* 5_ */ {bvc, rel, 2}, {eor, yip, 5}, {eor, zpi, 5},    unnop11   , {ldd, zpx, 4}, {eor, zpx, 4}, {lsr, zpx, 6},    unnop11   , {cli, imp, 2}, {eor, ayp, 4}, {phy, imp, 3},    unnop11   , {ldd,  ab, 8}, {eor, axp, 4}, {lsr, axp, 6},    unnop11   ,
  /* 6_ */ {rts, imp, 6}, {adc, xin, 6},    ldd_imm   ,    unnop11   , {stz,  zp, 3}, {adc,  zp, 3}, {ror,  zp, 5},    unnop11   , {pla, imp, 4}, {adc, imm, 2}, {ror, acc, 2},    unnop11   , {jmp, ind, 6}, {adc,  ab, 4}, {ror,  ab, 6},    unnop11   ,
  /* 7_ */ {bvs, rel, 2}, {adc, yip, 5}, {adc, zpi, 5},    unnop11   , {stz, zpx, 4}, {adc, zpx, 4}, {ror, zpx, 6},    unnop11   , {sei, imp, 2}, {adc, ayp, 4}, {ply, imp, 4},    unnop11   , {jmp,indx, 6}, {adc, axp, 4}, {ror, axp, 6},    unnop11   ,
  /* 8_ */ {bra, rel, 2}, {sta, xin, 6},    ldd_imm   ,    unnop11   , {sty,  zp, 3}, {sta,  zp, 3}, {stx,  zp, 3},    unnop11   , {dey, imp, 2}, {bit, imm, 2}, {txa, imp, 2},    unnop11   , {sty,  ab, 4}, {sta,  ab, 4}, {stx,  ab, 4},    unnop11   ,
  /* 9_ */ {bcc, rel, 2}, {sta, yin, 6}, {sta, zpi, 5},    unnop11   , {sty, zpx, 4}, {sta, zpx, 4}, {stx, zpy, 4},    unnop11   , {tya, imp, 2}, {sta, aby, 5}, {txs, imp, 2},    unnop11   , {stz,  ab, 4}, {sta, abx, 5}, {stz, abx, 5},    unnop11   ,
  /* A_ */ {ldy, imm, 2}, {lda, xin, 6}, {ldx, imm, 2},    unnop11   , {ldy,  zp, 3}, {lda,  zp, 3}, {ldx,  zp, 3},    unnop11   , {tay, imp, 2}, {lda, imm, 2}, {tax, imp, 2},    unnop11   , {ldy,  ab, 4}, {lda,  ab, 4}, {ldx,  ab, 4},    unnop11   ,
  /* B_ */ {bcs, rel, 2}, {lda, yip, 5}, {lda, zpi, 5},    unnop11   , {ldy, zpx, 4}, {lda, zpx, 4}, {ldx, zpy, 4},    unnop11   , {clv, imp, 2}, {lda, ayp, 4}, {tsx, imp, 2},    unnop11   , {ldy, axp, 4}, {lda, axp, 4}, {ldx, ayp, 4},    unnop11   ,
  /* C_ */ {cpy, imm, 2}, {cmp, xin, 6},    ldd_imm   ,    unnop11   , {cpy,  zp, 3}, {cmp,  zp, 3}, {dec,  zp, 5},    unnop11   , {iny, imp, 2}, {cmp, imm, 2}, {dex, imp, 2},    unnop11   , {cpy,  ab, 4}, {cmp,  ab, 4}, {dec,  ab, 6},    unnop11   ,
  /* D_ */ {bne, rel, 2}, {cmp, yip, 5}, {cmp, zpi, 5},    unnop11   , {ldd, zpx, 4}, {cmp, zpx, 4}, {dec, zpx, 6},    unnop11   , {cld, imp, 2}, {cmp, ayp, 4}, {phx, imp, 3},    unnop11,   {ldd,  ab, 4}, {cmp, axp, 4}, {dec, abx, 7},    unnop11   ,
  /* E_ */ {cpx, imm, 2}, {sbc, xin, 6},    ldd_imm   ,    unnop11   , {cpx,  zp, 3}, {sbc,  zp, 3}, {inc,  zp, 5},    unnop11   , {inx, imp, 2}, {sbc, imm, 2}, {nop, imp, 2},    unnop11   , {cpx,  ab, 4}, {sbc,  ab, 4}, {inc,  ab, 6},    unnop11   ,
  /* F_ */ {beq, rel, 2}, {sbc, yip, 5}, {sbc, zpi, 5},    unnop11   , {ldd, zpx, 4}, {sbc, zpx, 4}, {inc, zpx, 6},    unnop11   , {sed, imp, 2}, {sbc, ayp, 4}, {plx, imp, 4},    unnop11   , {ldd,  ab, 4}, {sbc, axp, 4}, {inc, abx, 7},    unnop11 };


static const vrEmu6502Opcode __not_in_flash("cpu") _wdc65c02[256] = {
  /*      |      _0      |      _1      |      _2      |      _3      |      _4      |      _5      |      _6      |      _7      |      _8      |      _9      |      _A      |      _B      |      _C      |      _D      |      _E      |      _F      | */
  /* 0_ */ {brk, imp, 7}, {ora, xin, 6},    ldd_imm   ,    unnop11   , {tsb,  zp, 5}, {ora,  zp, 3}, {asl,  zp, 5}, {rmb0, zp, 5}, {php, imp, 3}, {ora, imm, 2}, {asl, acc, 2},    unnop11   , {tsb,  ab, 6}, {ora,  ab, 4}, {asl,  ab, 6}, {bbr0, zp, 5},
  /* 1_ */ {bpl, rel, 2}, {ora, yip, 5}, {ora, zpi, 5},    unnop11   , {trb,  zp, 5}, {ora, zpx, 4}, {asl, zpx, 6}, {rmb1, zp, 5}, {clc, imp, 2}, {ora, ayp, 4}, {inc, acc, 2},    unnop11   , {trb,  ab, 6}, {ora, axp, 4}, {asl, axp, 6}, {bbr1, zp, 5},
  /* 2_ */ {jsr,  ab, 6}, { and, xin, 6},    ldd_imm   ,    unnop11   , {bit,  zp, 3}, { and,  zp, 3}, {rol,  zp, 5}, {rmb2, zp, 5}, {plp, imp, 4}, { and, imm, 2}, {rol, acc, 2},    unnop11   , {bit,  ab, 4}, { and,  ab, 4}, {rol,  ab, 6}, {bbr2, zp, 5},
  /* 3_ */ {bmi, rel, 2}, { and, yip, 5}, { and, zpi, 5},    unnop11   , {bit, zpx, 4}, { and, zpx, 4}, {rol, zpx, 6}, {rmb3, zp, 5}, {sec, imp, 2}, { and, ayp, 4}, {dec, acc, 2},    unnop11   , {bit, abx, 4}, { and, axp, 4}, {rol, axp, 6}, {bbr3, zp, 5},
  /* 4_ */ {rti, imp, 6}, {eor, xin, 6},    ldd_imm   ,    unnop11   , {ldd,  zp, 3}, {eor,  zp, 3}, {lsr,  zp, 5}, {rmb4, zp, 5}, {pha, imp, 3}, {eor, imm, 2}, {lsr, acc, 2},    unnop11   , {jmp,  ab, 3}, {eor,  ab, 4}, {lsr,  ab, 6}, {bbr4, zp, 5},
  /* 5_ */ {bvc, rel, 2}, {eor, yip, 5}, {eor, zpi, 5},    unnop11   , {ldd, zpx, 4}, {eor, zpx, 4}, {lsr, zpx, 6}, {rmb5, zp, 5}, {cli, imp, 2}, {eor, ayp, 4}, {phy, imp, 3},    unnop11   , {ldd,  ab, 8}, {eor, axp, 4}, {lsr, axp, 6}, {bbr5, zp, 5},
  /* 6_ */ {rts, imp, 6}, {adc, xin, 6},    ldd_imm   ,    unnop11   , {stz,  zp, 3}, {adc,  zp, 3}, {ror,  zp, 5}, {rmb6, zp, 5}, {pla, imp, 4}, {adc, imm, 2}, {ror, acc, 2},    unnop11   , {jmp, ind, 6}, {adc,  ab, 4}, {ror,  ab, 6}, {bbr6, zp, 5},
  /* 7_ */ {bvs, rel, 2}, {adc, yip, 5}, {adc, zpi, 5},    unnop11   , {stz, zpx, 4}, {adc, zpx, 4}, {ror, zpx, 6}, {rmb7, zp, 5}, {sei, imp, 2}, {adc, ayp, 4}, {ply, imp, 4},    unnop11   , {jmp,indx, 6}, {adc, axp, 4}, {ror, axp, 6}, {bbr7, zp, 5},
  /* 8_ */ {bra, rel, 2}, {sta, xin, 6},    ldd_imm   ,    unnop11   , {sty,  zp, 3}, {sta,  zp, 3}, {stx,  zp, 3}, {smb0, zp, 5}, {dey, imp, 2}, {bit, imm, 2}, {txa, imp, 2},    unnop11   , {sty,  ab, 4}, {sta,  ab, 4}, {stx,  ab, 4}, {bbs0, zp, 5},
  /* 9_ */ {bcc, rel, 2}, {sta, yin, 6}, {sta, zpi, 5},    unnop11   , {sty, zpx, 4}, {sta, zpx, 4}, {stx, zpy, 4}, {smb1, zp, 5}, {tya, imp, 2}, {sta, aby, 5}, {txs, imp, 2},    unnop11   , {stz,  ab, 4}, {sta, abx, 5}, {stz, abx, 5}, {bbs1, zp, 5},
  /* A_ */ {ldy, imm, 2}, {lda, xin, 6}, {ldx, imm, 2},    unnop11   , {ldy,  zp, 3}, {lda,  zp, 3}, {ldx,  zp, 3}, {smb2, zp, 5}, {tay, imp, 2}, {lda, imm, 2}, {tax, imp, 2},    unnop11   , {ldy,  ab, 4}, {lda,  ab, 4}, {ldx,  ab, 4}, {bbs2, zp, 5},
  /* B_ */ {bcs, rel, 2}, {lda, yip, 5}, {lda, zpi, 5},    unnop11   , {ldy, zpx, 4}, {lda, zpx, 4}, {ldx, zpy, 4}, {smb3, zp, 5}, {clv, imp, 2}, {lda, ayp, 4}, {tsx, imp, 2},    unnop11   , {ldy, axp, 4}, {lda, axp, 4}, {ldx, ayp, 4}, {bbs3, zp, 5},
  /* C_ */ {cpy, imm, 2}, {cmp, xin, 6},    ldd_imm   ,    unnop11   , {cpy,  zp, 3}, {cmp,  zp, 3}, {dec,  zp, 5}, {smb4, zp, 5}, {iny, imp, 2}, {cmp, imm, 2}, {dex, imp, 2}, {wai, imp, 3}, {cpy,  ab, 4}, {cmp,  ab, 4}, {dec,  ab, 6}, {bbs4, zp, 5},
  /* D_ */ {bne, rel, 2}, {cmp, yip, 5}, {cmp, zpi, 5},    unnop11   , {ldd, zpx, 4}, {cmp, zpx, 4}, {dec, zpx, 6}, {smb5, zp, 5}, {cld, imp, 2}, {cmp, ayp, 4}, {phx, imp, 3}, {stp, imp, 3}, {ldd,  ab, 4}, {cmp, axp, 4}, {dec, abx, 7}, {bbs5, zp, 5},
  /* E_ */ {cpx, imm, 2}, {sbc, xin, 6},    ldd_imm   ,    unnop11   , {cpx,  zp, 3}, {sbc,  zp, 3}, {inc,  zp, 5}, {smb6, zp, 5}, {inx, imp, 2}, {sbc, imm, 2}, {nop, imp, 2},    unnop11   , {cpx,  ab, 4}, {sbc,  ab, 4}, {inc,  ab, 6}, {bbs6, zp, 5},
  /* F_ */ {beq, rel, 2}, {sbc, yip, 5}, {sbc, zpi, 5},    unnop11   , {ldd, zpx, 4}, {sbc, zpx, 4}, {inc, zpx, 6}, {smb7, zp, 5}, {sed, imp, 2}, {sbc, ayp, 4}, {plx, imp, 4},    unnop11   , {ldd,  ab, 4}, {sbc, axp, 4}, {inc, abx, 7}, {bbs7, zp, 5} };

static const vrEmu6502Opcode _r65c02[256] = {
  /*      |      _0      |      _1      |      _2      |      _3      |      _4      |      _5      |      _6      |      _7      |      _8      |      _9      |      _A      |      _B      |      _C      |      _D      |      _E      |      _F      | */
  /* 0_ */ {brk, imp, 7}, {ora, xin, 6},    ldd_imm   ,    unnop11   , {tsb,  zp, 5}, {ora,  zp, 3}, {asl,  zp, 5}, {rmb0, zp, 5}, {php, imp, 3}, {ora, imm, 2}, {asl, acc, 2},    unnop11   , {tsb,  ab, 6}, {ora,  ab, 4}, {asl,  ab, 6}, {bbr0, zp, 5},
  /* 1_ */ {bpl, rel, 2}, {ora, yip, 5}, {ora, zpi, 5},    unnop11   , {trb,  zp, 5}, {ora, zpx, 4}, {asl, zpx, 6}, {rmb1, zp, 5}, {clc, imp, 2}, {ora, ayp, 4}, {inc, acc, 2},    unnop11   , {trb,  ab, 6}, {ora, axp, 4}, {asl, axp, 6}, {bbr1, zp, 5},
  /* 2_ */ {jsr,  ab, 6}, { and, xin, 6},    ldd_imm   ,    unnop11   , {bit,  zp, 3}, { and,  zp, 3}, {rol,  zp, 5}, {rmb2, zp, 5}, {plp, imp, 4}, { and, imm, 2}, {rol, acc, 2},    unnop11   , {bit,  ab, 4}, { and,  ab, 4}, {rol,  ab, 6}, {bbr2, zp, 5},
  /* 3_ */ {bmi, rel, 2}, { and, yip, 5}, { and, zpi, 5},    unnop11   , {bit, zpx, 4}, { and, zpx, 4}, {rol, zpx, 6}, {rmb3, zp, 5}, {sec, imp, 2}, { and, ayp, 4}, {dec, acc, 2},    unnop11   , {bit, abx, 4}, { and, axp, 4}, {rol, axp, 6}, {bbr3, zp, 5},
  /* 4_ */ {rti, imp, 6}, {eor, xin, 6},    ldd_imm   ,    unnop11   , {ldd,  zp, 3}, {eor,  zp, 3}, {lsr,  zp, 5}, {rmb4, zp, 5}, {pha, imp, 3}, {eor, imm, 2}, {lsr, acc, 2},    unnop11   , {jmp,  ab, 3}, {eor,  ab, 4}, {lsr,  ab, 6}, {bbr4, zp, 5},
  /* 5_ */ {bvc, rel, 2}, {eor, yip, 5}, {eor, zpi, 5},    unnop11   , {ldd, zpx, 4}, {eor, zpx, 4}, {lsr, zpx, 6}, {rmb5, zp, 5}, {cli, imp, 2}, {eor, ayp, 4}, {phy, imp, 3},    unnop11   , {ldd,  ab, 8}, {eor, axp, 4}, {lsr, axp, 6}, {bbr5, zp, 5},
  /* 6_ */ {rts, imp, 6}, {adc, xin, 6},    ldd_imm   ,    unnop11   , {stz,  zp, 3}, {adc,  zp, 3}, {ror,  zp, 5}, {rmb6, zp, 5}, {pla, imp, 4}, {adc, imm, 2}, {ror, acc, 2},    unnop11   , {jmp, ind, 6}, {adc,  ab, 4}, {ror,  ab, 6}, {bbr6, zp, 5},
  /* 7_ */ {bvs, rel, 2}, {adc, yip, 5}, {adc, zpi, 5},    unnop11   , {stz, zpx, 4}, {adc, zpx, 4}, {ror, zpx, 6}, {rmb7, zp, 5}, {sei, imp, 2}, {adc, ayp, 4}, {ply, imp, 4},    unnop11   , {jmp,indx, 6}, {adc, axp, 4}, {ror, axp, 6}, {bbr7, zp, 5},
  /* 8_ */ {bra, rel, 2}, {sta, xin, 6},    ldd_imm   ,    unnop11   , {sty,  zp, 3}, {sta,  zp, 3}, {stx,  zp, 3}, {smb0, zp, 5}, {dey, imp, 2}, {bit, imm, 2}, {txa, imp, 2},    unnop11   , {sty,  ab, 4}, {sta,  ab, 4}, {stx,  ab, 4}, {bbs0, zp, 5},
  /* 9_ */ {bcc, rel, 2}, {sta, yin, 6}, {sta, zpi, 5},    unnop11   , {sty, zpx, 4}, {sta, zpx, 4}, {stx, zpy, 4}, {smb1, zp, 5}, {tya, imp, 2}, {sta, aby, 5}, {txs, imp, 2},    unnop11   , {stz,  ab, 4}, {sta, abx, 5}, {stz, abx, 5}, {bbs1, zp, 5},
  /* A_ */ {ldy, imm, 2}, {lda, xin, 6}, {ldx, imm, 2},    unnop11   , {ldy,  zp, 3}, {lda,  zp, 3}, {ldx,  zp, 3}, {smb2, zp, 5}, {tay, imp, 2}, {lda, imm, 2}, {tax, imp, 2},    unnop11   , {ldy,  ab, 4}, {lda,  ab, 4}, {ldx,  ab, 4}, {bbs2, zp, 5},
  /* B_ */ {bcs, rel, 2}, {lda, yip, 5}, {lda, zpi, 5},    unnop11   , {ldy, zpx, 4}, {lda, zpx, 4}, {ldx, zpy, 4}, {smb3, zp, 5}, {clv, imp, 2}, {lda, ayp, 4}, {tsx, imp, 2},    unnop11   , {ldy, axp, 4}, {lda, axp, 4}, {ldx, ayp, 4}, {bbs3, zp, 5},
  /* C_ */ {cpy, imm, 2}, {cmp, xin, 6},    ldd_imm   ,    unnop11   , {cpy,  zp, 3}, {cmp,  zp, 3}, {dec,  zp, 5}, {smb4, zp, 5}, {iny, imp, 2}, {cmp, imm, 2}, {dex, imp, 2},    unnop11   , {cpy,  ab, 4}, {cmp,  ab, 4}, {dec,  ab, 6}, {bbs4, zp, 5},
  /* D_ */ {bne, rel, 2}, {cmp, yip, 5}, {cmp, zpi, 5},    unnop11   , {ldd, zpx, 4}, {cmp, zpx, 4}, {dec, zpx, 6}, {smb5, zp, 5}, {cld, imp, 2}, {cmp, ayp, 4}, {phx, imp, 3},    unnop11   , {ldd,  ab, 4}, {cmp, axp, 4}, {dec, abx, 7}, {bbs5, zp, 5},
  /* E_ */ {cpx, imm, 2}, {sbc, xin, 6},    ldd_imm   ,    unnop11   , {cpx,  zp, 3}, {sbc,  zp, 3}, {inc,  zp, 5}, {smb6, zp, 5}, {inx, imp, 2}, {sbc, imm, 2}, {nop, imp, 2},    unnop11   , {cpx,  ab, 4}, {sbc,  ab, 4}, {inc,  ab, 6}, {bbs6, zp, 5},
  /* F_ */ {beq, rel, 2}, {sbc, yip, 5}, {sbc, zpi, 5},    unnop11   , {ldd, zpx, 4}, {sbc, zpx, 4}, {inc, zpx, 6}, {smb7, zp, 5}, {sed, imp, 2}, {sbc, ayp, 4}, {plx, imp, 4},    unnop11   , {ldd,  ab, 4}, {sbc, axp, 4}, {inc, abx, 7}, {bbs7, zp, 5} };


static const vrEmu6502Opcode* std6502 = _std6502;
static const vrEmu6502Opcode* std6502U = _std6502U;
static const vrEmu6502Opcode* std65c02 = _std65c02;
static const vrEmu6502Opcode* wdc65c02 = _wdc65c02;
static const vrEmu6502Opcode* r65c02 = _r65c02;

/* ------------------------------------------------------------------
 *  return addressing mode for an opcode
 *  Note: all values are cached in VrEmu6502.addrModes, so calling
 *        the public vrEmu6502GetOpcodeAddrMode() function will be fast
 * ----------------------------------------------------------------*/
static vrEmu6502AddrMode opcodeToAddrMode(VrEmu6502* vr6502, uint8_t opcode)
{
#define opCodeAddrMode(x, y) if (oc->addrMode == x) return y

  const vrEmu6502Opcode* oc = &vr6502->opcodes[opcode];

  opCodeAddrMode(ab, AddrModeAbs);
  opCodeAddrMode(abx, AddrModeAbsX);
  opCodeAddrMode(aby, AddrModeAbsY);
  opCodeAddrMode(axp, AddrModeAbsX);
  opCodeAddrMode(ayp, AddrModeAbsY);
  opCodeAddrMode(imm, AddrModeImm);
  opCodeAddrMode(ind, AddrModeAbsInd);
  opCodeAddrMode(ind, AddrModeAbsIndX);
  opCodeAddrMode(indx, AddrModeAbsIndX);
  opCodeAddrMode(rel, AddrModeRel);
  opCodeAddrMode(xin, AddrModeIndX);
  opCodeAddrMode(yin, AddrModeIndY);
  opCodeAddrMode(yip, AddrModeIndY);
  opCodeAddrMode(zp, AddrModeZP);
  opCodeAddrMode(zpi, AddrModeZPI);
  opCodeAddrMode(zpx, AddrModeZPX);
  opCodeAddrMode(zpy, AddrModeZPY);

  return AddrModeImp;
}

/* ------------------------------------------------------------------
 *  return opcode mnemonic string from opcode value
 *  Note: all values are cached in VrEmu6502.mnemonicNames, so calling
 *        the public vrEmu6502OpcodeToMnemonicStr() function will be fast
 * ----------------------------------------------------------------*/
static const char* opcodeToMnemonicStr(VrEmu6502* vr6502, uint8_t opcode)
{
#define mnemonicToStr(x) if (oc->instruction == x) return #x

  const vrEmu6502Opcode* oc = &vr6502->opcodes[opcode];

  mnemonicToStr(adc);
  mnemonicToStr(and);
  mnemonicToStr(asl);
  mnemonicToStr(bra);
  mnemonicToStr(bcc);
  mnemonicToStr(bcs);
  mnemonicToStr(beq);
  mnemonicToStr(bit);
  mnemonicToStr(bmi);
  mnemonicToStr(bne);
  mnemonicToStr(bpl);
  mnemonicToStr(brk);
  mnemonicToStr(bvc);
  mnemonicToStr(bvs);
  mnemonicToStr(clc);
  mnemonicToStr(cld);
  mnemonicToStr(cli);
  mnemonicToStr(clv);
  mnemonicToStr(cmp);
  mnemonicToStr(cpx);
  mnemonicToStr(cpy);
  mnemonicToStr(dec);
  mnemonicToStr(dex);
  mnemonicToStr(dey);
  mnemonicToStr(eor);
  mnemonicToStr(inc);
  mnemonicToStr(inx);
  mnemonicToStr(iny);
  mnemonicToStr(jmp);
  mnemonicToStr(jsr);
  mnemonicToStr(lda);
  mnemonicToStr(ldd);
  mnemonicToStr(ldx);
  mnemonicToStr(ldy);
  mnemonicToStr(lsr);
  mnemonicToStr(nop);
  mnemonicToStr(ora);
  mnemonicToStr(pha);
  mnemonicToStr(php);
  mnemonicToStr(phx);
  mnemonicToStr(phy);
  mnemonicToStr(pla);
  mnemonicToStr(plp);
  mnemonicToStr(plx);
  mnemonicToStr(ply);
  mnemonicToStr(rol);
  mnemonicToStr(ror);
  mnemonicToStr(rti);
  mnemonicToStr(rts);
  mnemonicToStr(sbc);
  mnemonicToStr(sec);
  mnemonicToStr(sed);
  mnemonicToStr(sei);
  mnemonicToStr(sta);
  mnemonicToStr(stp);
  mnemonicToStr(stx);
  mnemonicToStr(sty);
  mnemonicToStr(stz);
  mnemonicToStr(tax);
  mnemonicToStr(tay);
  mnemonicToStr(tsx);
  mnemonicToStr(txa);
  mnemonicToStr(txs);
  mnemonicToStr(tya);
  mnemonicToStr(trb);
  mnemonicToStr(tsb);
  mnemonicToStr(rmb0);
  mnemonicToStr(rmb1);
  mnemonicToStr(rmb2);
  mnemonicToStr(rmb3);
  mnemonicToStr(rmb4);
  mnemonicToStr(rmb5);
  mnemonicToStr(rmb6);
  mnemonicToStr(rmb7);
  mnemonicToStr(smb0);
  mnemonicToStr(smb1);
  mnemonicToStr(smb2);
  mnemonicToStr(smb3);
  mnemonicToStr(smb4);
  mnemonicToStr(smb5);
  mnemonicToStr(smb6);
  mnemonicToStr(smb7);
  mnemonicToStr(bbr0);
  mnemonicToStr(bbr1);
  mnemonicToStr(bbr2);
  mnemonicToStr(bbr3);
  mnemonicToStr(bbr4);
  mnemonicToStr(bbr5);
  mnemonicToStr(bbr6);
  mnemonicToStr(bbr7);
  mnemonicToStr(bbs0);
  mnemonicToStr(bbs1);
  mnemonicToStr(bbs2);
  mnemonicToStr(bbs3);
  mnemonicToStr(bbs4);
  mnemonicToStr(bbs5);
  mnemonicToStr(bbs6);
  mnemonicToStr(bbs7);
  mnemonicToStr(wai);
  mnemonicToStr(slo);
  mnemonicToStr(rla);
  mnemonicToStr(sre);
  mnemonicToStr(rra);
  mnemonicToStr(sax);
  mnemonicToStr(lax);
  mnemonicToStr(dcp);
  mnemonicToStr(isc);
  mnemonicToStr(anc);
  mnemonicToStr(alr);
  mnemonicToStr(arr);
  mnemonicToStr(sbx);
  mnemonicToStr(las);
  mnemonicToStr(sha);
  mnemonicToStr(shx);
  mnemonicToStr(shy);
  mnemonicToStr(tas);
  mnemonicToStr(ane);
  mnemonicToStr(lxa);
  mnemonicToStr(jam);

  return "err";
}
