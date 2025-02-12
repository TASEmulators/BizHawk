// Emu 0.7.0. http://www.slack.net/~ant/nes-emu/

#include "cpu.hpp"
#include "core.hpp"
#include <climits>
#include <cstdio>
#include <cstring>

/**
 * Optimizations by Sergio Martin (eien86) 2023-2024
 * The license below (LGPLv2) applies.
 */

/* Copyright (C) 2003-2006 Shay Green. This module is free software; you
can redistribute it and/or modify it under the terms of the GNU Lesser
General Public License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version. This
module is distributed in the hope that it will be useful, but WITHOUT ANY
WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for
more details. You should have received a copy of the GNU Lesser General
Public License along with this module; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA */

namespace quickerNES
{

#define st_n 0x80
#define st_v 0x40
#define st_r 0x20
#define st_b 0x10
#define st_d 0x08
#define st_i 0x04
#define st_z 0x02
#define st_c 0x01

// Macros

#define GET_OPERAND(addr) page[addr]
#define GET_OPERAND16(addr) *(uint16_t *)(&page[addr])

#define ADD_PAGE (pc++, data += 0x100 * GET_OPERAND(pc));
#define GET_ADDR() GET_OPERAND16(pc)

#define HANDLE_PAGE_CROSSING(lsb) clock_count += (lsb) >> 8;

#define INC_DEC_XY(reg, n)     \
  reg = uint8_t(nz = reg + n); \
  goto loop;

#define IND_Y(r, c)                                    \
  {                                                    \
    int32_t temp = READ_LOW(data) + y;                 \
    data = temp + 0x100 * READ_LOW(uint8_t(data + 1)); \
    if (c) HANDLE_PAGE_CROSSING(temp);                 \
    if (!(r) || (temp & 0x100))                        \
      READ(data - (temp & 0x100));                     \
  }

#define IND_X                                                             \
  {                                                                       \
    int32_t temp = data + x;                                              \
    data = 0x100 * READ_LOW(uint8_t(temp + 1)) + READ_LOW(uint8_t(temp)); \
  }

#define ARITH_ADDR_MODES(op)      \
  case op - 0x04: /* (ind,x) */   \
    IND_X                         \
    goto ptr##op;                 \
  case op + 0x0C: /* (ind),y */   \
    IND_Y(true, true)             \
    goto ptr##op;                 \
  case op + 0x10: /* zp,X */      \
    data = uint8_t(data + x);     \
  case op + 0x00: /* zp */        \
    data = READ_LOW(data);        \
    goto imm##op;                 \
  case op + 0x14: /* abs,Y */     \
    data += y;                    \
    goto ind##op;                 \
  case op + 0x18: /* abs,X */     \
    data += x;                    \
    ind##op:                      \
    {                             \
      HANDLE_PAGE_CROSSING(data); \
      uint32_t temp = data;       \
      ADD_PAGE                    \
      if (temp & 0x100)           \
        READ(data - 0x100);       \
      goto ptr##op;               \
    }                             \
  case op + 0x08: /* abs */       \
    ADD_PAGE                      \
    ptr##op : data = READ(data);  \
  case op + 0x04: /* imm */       \
    imm##op:

#define ARITH_ADDR_MODES_PTR(op)   \
  case op - 0x04: /* (ind,x) */    \
    IND_X                          \
    goto imm##op;                  \
  case op + 0x0C:                  \
    IND_Y(false, false)            \
    goto imm##op;                  \
  case op + 0x10: /* zp,X */       \
    data = uint8_t(data + x);      \
    goto imm##op;                  \
  case op + 0x14: /* abs,Y */      \
    data += y;                     \
    goto ind##op;                  \
  case op + 0x18: /* abs,X */      \
    data += x;                     \
    ind##op:                       \
    {                              \
      uint32_t temp = data;        \
      ADD_PAGE                     \
      READ(data - (temp & 0x100)); \
      goto imm##op;                \
    }                              \
  case op + 0x08: /* abs */        \
    ADD_PAGE                       \
  case op + 0x00: /* zp */         \
    imm##op:

// Adding likely to fail because typically for loops exit conditions fail until the last one
#define BRANCH(cond)                        \
  {                                         \
    pc++;                                   \
    int offset = (int8_t)data;              \
    int extra_clock = (pc & 0xFF) + offset; \
    if (!(cond))                            \
    {                                       \
      clock_count--;                        \
      goto loop;                            \
    }                                       \
    pc += offset;                           \
    pc = uint16_t(pc);                      \
    clock_count += (extra_clock >> 8) & 1;  \
    goto loop;                              \
  }

void Cpu::reset(void const *unmapped_page)
{
  r.status = 0;
  r.sp = 0;
  r.pc = 0;
  r.a = 0;
  r.x = 0;
  r.y = 0;

  error_count_ = 0;
  clock_count = 0;
  clock_limit = 0;
  irq_time_ = LONG_MAX / 2 + 1;
  end_time_ = LONG_MAX / 2 + 1;

  set_code_page(0, low_mem);
  set_code_page(1, low_mem);
  set_code_page(2, low_mem);
  set_code_page(3, low_mem);
  for (int32_t i = 4; i < page_count + 1; i++)
    set_code_page(i, (uint8_t *)unmapped_page);

  isCorrectExecution = true;
}

// Note: 'addr' is evaulated more than once in the following macros, so it
// must not contain side-effects.

// static void log_read( int32_t opcode ) { LOG_FREQ( "read", 256, opcode ); }

#define READ_LIKELY_PPU(addr) (NES_CPU_READ_PPU(this, (addr), (clock_count)))
#define READ(addr) (NES_CPU_READ(this, (addr), (clock_count)))
#define WRITE(addr, data)                               \
  {                                                     \
    NES_CPU_WRITE(this, (addr), (data), (clock_count)); \
  }

#define READ_LOW(addr) (low_mem[int32_t(addr)])
#define WRITE_LOW(addr, data) (void)(READ_LOW(addr) = (data))

#define READ_PROG(addr) (code_map[(addr) >> page_bits][addr])
#define READ_PROG16(addr) GET_LE16(&READ_PROG(addr))

#define SET_SP(v) (sp = ((v) + 1) | 0x100)
#define GET_SP() ((sp - 1) & 0xFF)
#define PUSH(v) ((sp = (sp - 1) | 0x100), WRITE_LOW(sp, v))

#define IS_NEG (nz & 0x880)

#define CALC_STATUS(out)                 \
  do {                                   \
    out = status & (st_v | st_d | st_i); \
    out |= (c >> 8) & st_c;              \
    if (IS_NEG) out |= st_n;             \
    if (!(nz & 0xFF)) out |= st_z;       \
  } while (0)

#define SET_STATUS(in)                  \
  do {                                  \
    status = in & (st_v | st_d | st_i); \
    c = in << 8;                        \
    nz = (in << 4) & 0x800;             \
    nz |= ~in & st_z;                   \
  } while (0)

inline int32_t Cpu::read(nes_addr_t addr)
{
  return READ(addr);
}

inline void Cpu::write(nes_addr_t addr, int value)
{
  WRITE(addr, value);
}

// status flags
uint8_t clock_table[256] = {
  //  0 1 2 3 4 5 6 7 8 9 A B C D E F
  7,
  6,
  2,
  8,
  3,
  3,
  5,
  5,
  3,
  2,
  2,
  2,
  4,
  4,
  6,
  6, // 0
  3,
  5,
  2,
  8,
  4,
  4,
  6,
  6,
  2,
  4,
  2,
  7,
  4,
  4,
  7,
  7, // 1
  6,
  6,
  2,
  8,
  3,
  3,
  5,
  5,
  4,
  2,
  2,
  2,
  4,
  4,
  6,
  6, // 2
  3,
  5,
  2,
  8,
  4,
  4,
  6,
  6,
  2,
  4,
  2,
  7,
  4,
  4,
  7,
  7, // 3
  6,
  6,
  2,
  8,
  3,
  3,
  5,
  5,
  3,
  2,
  2,
  2,
  3,
  4,
  6,
  6, // 4
  3,
  5,
  2,
  8,
  4,
  4,
  6,
  6,
  2,
  4,
  2,
  7,
  4,
  4,
  7,
  7, // 5
  6,
  6,
  2,
  8,
  3,
  3,
  5,
  5,
  4,
  2,
  2,
  2,
  5,
  4,
  6,
  6, // 6
  3,
  5,
  2,
  8,
  4,
  4,
  6,
  6,
  2,
  4,
  2,
  7,
  4,
  4,
  7,
  7, // 7
  2,
  6,
  2,
  6,
  3,
  3,
  3,
  3,
  2,
  2,
  2,
  2,
  4,
  4,
  4,
  4, // 8
  3,
  6,
  2,
  6,
  4,
  4,
  4,
  4,
  2,
  5,
  2,
  5,
  5,
  5,
  5,
  5, // 9
  2,
  6,
  2,
  6,
  3,
  3,
  3,
  3,
  2,
  2,
  2,
  2,
  4,
  4,
  4,
  4, // A
  3,
  5,
  2,
  5,
  4,
  4,
  4,
  4,
  2,
  4,
  2,
  4,
  4,
  4,
  4,
  4, // B
  2,
  6,
  2,
  8,
  3,
  3,
  5,
  5,
  2,
  2,
  2,
  2,
  4,
  4,
  6,
  6, // C
  3,
  5,
  2,
  8,
  4,
  4,
  6,
  6,
  2,
  4,
  2,
  7,
  4,
  4,
  7,
  7, // D
  2,
  6,
  2,
  8,
  3,
  3,
  5,
  5,
  2,
  2,
  2,
  2,
  4,
  4,
  6,
  6, // E
  3,
  5,
  2,
  8,
  4,
  4,
  6,
  6,
  2,
  4,
  2,
  7,
  4,
  4,
  7,
  7 // F
};


// This optimization is only possible with the GNU compiler -- MSVC does not allow function alignment
#ifdef __GNUC__
__attribute__((optimize("align-functions=1024")))
#endif
Cpu::result_t
Cpu::run(nes_time_t end)
{
  set_end_time_(end);
  clock_count = 0;
  isCorrectExecution = true;

  volatile result_t result = result_cycles;

  // registers
  uint32_t pc = r.pc;
  int32_t sp;
  SET_SP(r.sp);
  int32_t a = r.a;
  int32_t x = r.x;
  int32_t y = r.y;

  int32_t status;
  int32_t c;  // carry set if (c & 0x100) != 0
  int32_t nz; // Z set if (nz & 0xFF) == 0, N set if (nz & 0x880) != 0
  {
    int32_t temp = r.status;
    SET_STATUS(temp);
  }

  uint32_t data;
  uint8_t const *page;
  uint8_t opcode;

loop:

  page = code_map[pc >> page_bits];
  opcode = page[pc++];
  data = page[pc];

  if (clock_count >= clock_limit) [[unlikely]]
    goto stop;

// If traceback support is enabled, trigger it here
#ifdef _QUICKERNES_ENABLE_TRACEBACK_SUPPORT
  if (tracecb)
  {
    unsigned int scratch[7];
    scratch[0] = a;
    scratch[1] = x;
    scratch[2] = y;
    scratch[3] = sp;
    scratch[4] = pc - 1;
    scratch[5] = status;
    scratch[6] = opcode;
    tracecb(scratch);
  }
#endif

  clock_count += clock_table[opcode];

  switch (opcode)
  {
    // Often-Used

  case 0xB5: // LDA zp,x
    data = uint8_t(data + x);
  case 0xA5: // LDA zp
    a = nz = READ_LOW(data);
    pc++;
    goto loop;

  case 0xD0: // BNE
    BRANCH((uint8_t)nz);

  case 0x20:
  { // JSR
    int32_t temp = pc + 1;
    pc = GET_OPERAND16(pc);
    WRITE_LOW(0x100 | (sp - 1), temp >> 8);
    sp = (sp - 2) | 0x100;
    WRITE_LOW(sp, temp);
    goto loop;
  }

  case 0x4C: // JMP abs
    pc = GET_OPERAND16(pc);
    goto loop;

  case 0xE8: INC_DEC_XY(x, 1) // INX

  case 0x10: // BPL
    BRANCH(!IS_NEG)

    ARITH_ADDR_MODES(0xC5) // CMP
    nz = a - data;
    pc++;
    c = ~nz;
    nz &= 0xFF;
    goto loop;

  case 0x30: // BMI
    BRANCH(IS_NEG)

  case 0xF0: // BEQ
    BRANCH(!(uint8_t)nz);

  case 0x95: // STA zp,x
    data = uint8_t(data + x);
  case 0x85: // STA zp
    pc++;
    WRITE_LOW(data, a);
    goto loop;

  case 0xC8: INC_DEC_XY(y, 1) // INY

  case 0xA8: // TAY
    y = a;
  case 0x98: // TYA
    a = nz = y;
    goto loop;

  case 0xAD: // LDA abs
    data = GET_ADDR();
    pc += 2;
    a = nz = READ_LIKELY_PPU(data);
    goto loop;

  case 0x60: // RTS
    pc = 1 + READ_LOW(sp);
    pc += READ_LOW(0x100 | (sp - 0xFF)) * 0x100;
    sp = (sp - 0xFE) | 0x100;
    goto loop;

  case 0x99: // STA abs,Y
    data += y;
    goto sta_ind_common;

  case 0x9D: // STA abs,X
    data += x;
  sta_ind_common:
    ADD_PAGE
    READ(data - (data & 0x100));
    goto sta_ptr;

  case 0x8D: // STA abs
    ADD_PAGE
  sta_ptr:
    pc++;
    WRITE(data, a);
    goto loop;

  case 0xA9: // LDA #imm
    pc++;
    a = data;
    nz = data;
    goto loop;

  case 0xB9: // LDA abs,Y
    data += y;
    data -= x;
  case 0xBD:
  { // LDA abs,X
    pc++;
    uint32_t msb = GET_OPERAND(pc);
    data += x;
    // indexed common
    pc++;
    HANDLE_PAGE_CROSSING(data);
    int32_t temp = data;
    data += msb * 0x100;
    a = nz = READ_PROG(uint16_t(data));
    if ((uint32_t)(data - 0x2000) >= 0x6000)
      goto loop;
    if (temp & 0x100)
      READ(data - 0x100);
    a = nz = READ(data);
    goto loop;
  }

  case 0xB1:
  { // LDA (ind),Y
    uint32_t msb = READ_LOW((uint8_t)(data + 1));
    data = READ_LOW(data) + y;
    // indexed common
    pc++;
    HANDLE_PAGE_CROSSING(data);
    int32_t temp = data;
    data += msb * 0x100;
    a = nz = READ_PROG(uint16_t(data));
    if ((uint32_t)(data - 0x2000) >= 0x6000)
      goto loop;
    if (temp & 0x100)
      READ(data - 0x100);
    a = nz = READ(data);
    goto loop;
  }

  case 0xA1: // LDA (ind,X)
    IND_X
    a = nz = READ(data);
    pc++;
    goto loop;

    // Branch

  case 0x50: // BVC
    BRANCH(!(status & st_v))

  case 0x70: // BVS
    BRANCH(status & st_v)

  case 0xB0: // BCS
    BRANCH(c & 0x100)

  case 0x90: // BCC
    BRANCH(!(c & 0x100))

    // Load/store

  case 0x94: // STY zp,x
    data = uint8_t(data + x);
  case 0x84: // STY zp
    pc++;
    WRITE_LOW(data, y);
    goto loop;

  case 0x96: // STX zp,y
    data = uint8_t(data + y);
  case 0x86: // STX zp
    pc++;
    WRITE_LOW(data, x);
    goto loop;

  case 0xB6: // LDX zp,y
    data = uint8_t(data + y);
  case 0xA6: // LDX zp
    data = READ_LOW(data);
  case 0xA2: // LDX #imm
    pc++;
    x = data;
    nz = data;
    goto loop;

  case 0xB4: // LDY zp,x
    data = uint8_t(data + x);
  case 0xA4: // LDY zp
    data = READ_LOW(data);
  case 0xA0: // LDY #imm
    pc++;
    y = data;
    nz = data;
    goto loop;

  case 0x91: // STA (ind),Y
    IND_Y(false, false)
    goto sta_ptr;

  case 0x81: // STA (ind,X)
    IND_X
    goto sta_ptr;

  case 0xBC: // LDY abs,X
    data += x;
    HANDLE_PAGE_CROSSING(data);
  case 0xAC:
  { // LDY abs
    pc++;
    uint32_t addr = data + 0x100 * GET_OPERAND(pc);
    if (data & 0x100)
      READ(addr - 0x100);
    pc++;
    y = nz = READ(addr);
    goto loop;
  }

  case 0xBE: // LDX abs,y
    data += y;
    HANDLE_PAGE_CROSSING(data);
  case 0xAE:
  { // LDX abs
    pc++;
    uint32_t addr = data + 0x100 * GET_OPERAND(pc);
    pc++;
    if (data & 0x100)
      READ(addr - 0x100);
    x = nz = READ(addr);
    goto loop;
  }

    {
      int32_t temp;
    case 0x8C: // STY abs
      temp = y;
      goto store_abs;

    case 0x8E: // STX abs
      temp = x;
    store_abs:
      uint32_t addr = GET_ADDR();
      WRITE(addr, temp);
      pc += 2;
      goto loop;
    }

    // Compare

  case 0xEC:
  { // CPX abs
    uint32_t addr = GET_ADDR();
    pc++;
    data = READ(addr);
    goto cpx_data;
  }

  case 0xE4: // CPX zp
    data = READ_LOW(data);
  case 0xE0: // CPX #imm
  cpx_data:
    nz = x - data;
    pc++;
    c = ~nz;
    nz &= 0xFF;
    goto loop;

  case 0xCC:
  { // CPY abs
    uint32_t addr = GET_ADDR();
    pc++;
    data = READ(addr);
    goto cpy_data;
  }

  case 0xC4: // CPY zp
    data = READ_LOW(data);
  case 0xC0: // CPY #imm
  cpy_data:
    nz = y - data;
    pc++;
    c = ~nz;
    nz &= 0xFF;
    goto loop;

    // Logical

    ARITH_ADDR_MODES(0x25) // AND
    nz = (a &= data);
    pc++;
    goto loop;

    ARITH_ADDR_MODES(0x45) // EOR
    nz = (a ^= data);
    pc++;
    goto loop;

    ARITH_ADDR_MODES(0x05) // ORA
    nz = (a |= data);
    pc++;
    goto loop;

  case 0x2C:
  { // BIT abs
    uint32_t addr = GET_ADDR();
    pc += 2;
    status &= ~st_v;
    nz = READ_LIKELY_PPU(addr);
    status |= nz & st_v;
    if (a & nz)
      goto loop;
    // result must be zero, even if N bit is set
    nz = nz << 4 & 0x800;
    goto loop;
  }

  case 0x24: // BIT zp
    nz = READ_LOW(data);
    pc++;
    status &= ~st_v;
    status |= nz & st_v;
    if (a & nz)
      goto loop;
    // result must be zero, even if N bit is set
    nz = nz << 4 & 0x800;
    goto loop;

    // Add/subtract

    ARITH_ADDR_MODES(0xE5) // SBC
  case 0xEB:               // unofficial equivalent
    data ^= 0xFF;
    goto adc_imm;

    ARITH_ADDR_MODES(0x65) // ADC
  adc_imm:
  {
    int32_t carry = (c >> 8) & 1;
    int32_t ov = (a ^ 0x80) + carry + (int8_t)data; // sign-extend
    status &= ~st_v;
    status |= (ov >> 2) & 0x40;
    c = nz = a + data + carry;
    pc++;
    a = (uint8_t)nz;
    goto loop;
  }

    // Shift/rotate

  case 0x4A: // LSR A
  lsr_a:
    c = 0;
  case 0x6A:              // ROR A
    nz = (c >> 1) & 0x80; // could use bit insert macro here
    c = a << 8;
    nz |= a >> 1;
    a = nz;
    goto loop;

  case 0x0A: // ASL A
    nz = a << 1;
    c = nz;
    a = (uint8_t)nz;
    goto loop;

  case 0x2A:
  { // ROL A
    nz = a << 1;
    int32_t temp = (c >> 8) & 1;
    c = nz;
    nz |= temp;
    a = (uint8_t)nz;
    goto loop;
  }

  case 0x3E: // ROL abs,X
    data += x;
    goto rol_abs;

  case 0x1E: // ASL abs,X
    data += x;
  case 0x0E: // ASL abs
    c = 0;
  case 0x2E: // ROL abs
  rol_abs:
  {
    int32_t temp = data;
    ADD_PAGE
    if (opcode == 0x1E || opcode == 0x3E) READ(data - (temp & 0x100));
    WRITE(data, temp = READ(data));
    nz = (c >> 8) & 1;
    nz |= (c = temp << 1);
  }
  rotate_common:
    pc++;
    WRITE(data, (uint8_t)nz);
    goto loop;

  case 0x7E: // ROR abs,X
    data += x;
    goto ror_abs;

  case 0x5E: // LSR abs,X
    data += x;
  case 0x4E: // LSR abs
    c = 0;
  case 0x6E: // ROR abs
  ror_abs:
  {
    int32_t temp = data;
    ADD_PAGE
    if (opcode == 0x5E || opcode == 0x7E) READ(data - (temp & 0x100));
    WRITE(data, temp = READ(data));
    nz = ((c >> 1) & 0x80) | (temp >> 1);
    c = temp << 8;
    goto rotate_common;
  }

  case 0x76: // ROR zp,x
    data = uint8_t(data + x);
    goto ror_zp;

  case 0x56: // LSR zp,x
    data = uint8_t(data + x);
  case 0x46: // LSR zp
    c = 0;
  case 0x66: // ROR zp
  ror_zp:
  {
    int32_t temp = READ_LOW(data);
    nz = ((c >> 1) & 0x80) | (temp >> 1);
    c = temp << 8;
    goto write_nz_zp;
  }

  case 0x36: // ROL zp,x
    data = uint8_t(data + x);
    goto rol_zp;

  case 0x16: // ASL zp,x
    data = uint8_t(data + x);
  case 0x06: // ASL zp
    c = 0;
  case 0x26: // ROL zp
  rol_zp:
    nz = (c >> 8) & 1;
    nz |= (c = READ_LOW(data) << 1);
    goto write_nz_zp;

    // Increment/decrement

  case 0xCA: INC_DEC_XY(x, -1) // DEX

  case 0x88: INC_DEC_XY(y, -1) // DEY

  case 0xF6: // INC zp,x
    data = uint8_t(data + x);
  case 0xE6: // INC zp
    nz = 1;
    goto add_nz_zp;

  case 0xD6: // DEC zp,x
    data = uint8_t(data + x);
  case 0xC6: // DEC zp
    nz = -1;
  add_nz_zp:
    nz += READ_LOW(data);
  write_nz_zp:
    pc++;
    WRITE_LOW(data, nz);
    goto loop;

  case 0xFE:
  { // INC abs,x
    int32_t temp = data + x;
    data = x + GET_ADDR();
    READ(data - (temp & 0x100));
    goto inc_ptr;
  }

  case 0xEE: // INC abs
    data = GET_ADDR();
  inc_ptr:
    nz = 1;
    goto inc_common;

  case 0xDE:
  { // DEC abs,x
    int32_t temp = data + x;
    data = x + GET_ADDR();
    READ(data - (temp & 0x100));
    goto dec_ptr;
  }

  case 0xCE: // DEC abs
    data = GET_ADDR();
  dec_ptr:
    nz = -1;
  inc_common:
  {
    int32_t temp;
    WRITE(data, temp = READ(data));
    nz += temp;
    pc += 2;
    WRITE(data, (uint8_t)nz);
    goto loop;
  }

    // Transfer

  case 0xAA: // TAX
    x = a;
  case 0x8A: // TXA
    a = nz = x;
    goto loop;

  case 0x9A:   // TXS
    SET_SP(x); // verified (no flag change)
    goto loop;

  case 0xBA: // TSX
    x = nz = GET_SP();
    goto loop;

    // Stack

  case 0x48: // PHA
    PUSH(a); // verified
    goto loop;

  case 0x68: // PLA
    a = nz = READ_LOW(sp);
    sp = (sp - 0xFF) | 0x100;
    goto loop;

  case 0x40: // RTI
  {
    int32_t temp = READ_LOW(sp);
    pc = READ_LOW(0x100 | (sp - 0xFF));
    pc |= READ_LOW(0x100 | (sp - 0xFE)) * 0x100;
    sp = (sp - 0xFD) | 0x100;
    data = status;
    SET_STATUS(temp);
  }
    if (!((data ^ status) & st_i))
      goto loop; // I flag didn't change
  i_flag_changed:
    // dprintf( "%6d %s\n", time(), (status & st_i ? "SEI" : "CLI") );
    this->r.status = status; // update externally-visible I flag
    // update clock_limit based on modified I flag
    clock_limit = end_time_;
    if (end_time_ <= irq_time_)
      goto loop;
    if (status & st_i)
      goto loop;
    clock_limit = irq_time_;
    goto loop;

  case 0x28:
  { // PLP
    int32_t temp = READ_LOW(sp);
    sp = (sp - 0xFF) | 0x100;
    data = status;
    SET_STATUS(temp);
    if (!((data ^ status) & st_i))
      goto loop; // I flag didn't change
    if (!(status & st_i))
      goto handle_cli;
    goto handle_sei;
  }

  case 0x08:
  { // PHP
    int32_t temp;
    CALC_STATUS(temp);
    PUSH(temp | st_b | st_r);
    goto loop;
  }

  case 0x6C: // JMP (ind)
    data = GET_ADDR();
    pc = READ(data);
    pc |= READ((data & 0xFF00) | ((data + 1) & 0xFF)) << 8;
    goto loop;

  case 0x00:
  { // BRK
    pc++;
    WRITE_LOW(0x100 | (sp - 1), pc >> 8);
    WRITE_LOW(0x100 | (sp - 2), pc);
    int32_t temp;
    CALC_STATUS(temp);
    sp = (sp - 3) | 0x100;
    WRITE_LOW(sp, temp | st_b | st_r);
    pc = *(uint16_t *)(&code_map[0xFFFE >> page_bits][0xFFFE]);
    status |= st_i;
    goto i_flag_changed;
  }

    // Flags

  case 0x38: // SEC
    c = ~0;
    goto loop;

  case 0x18: // CLC
    c = 0;
    goto loop;

  case 0xB8: // CLV
    status &= ~st_v;
    goto loop;

  case 0xD8: // CLD
    status &= ~st_d;
    goto loop;

  case 0xF8: // SED
    status |= st_d;
    goto loop;

  case 0x58: // CLI
    if (!(status & st_i))
      goto loop;
    status &= ~st_i;
  handle_cli:
    // dprintf( "%6d CLI\n", time() );
    this->r.status = status; // update externally-visible I flag
    if (clock_count < end_time_)
    {
      if (end_time_ <= irq_time_)
        goto loop; // irq is later
      if (clock_count >= irq_time_)
        irq_time_ = clock_count + 1; // delay IRQ until after next instruction
      clock_limit = irq_time_;
      goto loop;
    }
    // execution is stopping now, so delayed CLI must be handled by caller
    result = result_cli;
    goto end;

  case 0x78: // SEI
    if (status & st_i)
      goto loop;
    status |= st_i;
  handle_sei:
    // dprintf( "%6d SEI\n", time() );
    this->r.status = status; // update externally-visible I flag
    clock_limit = end_time_;
    if (clock_count < irq_time_)
      goto loop;
    result = result_sei; // IRQ will occur now, even though I flag is set
    goto end;

    // Unofficial
  case 0x1C:
  case 0x3C:
  case 0x5C:
  case 0x7C:
  case 0xDC:
  case 0xFC:
  { // SKW
    data += x;
    HANDLE_PAGE_CROSSING(data);
    int32_t addr = GET_ADDR() + x;
    if (data & 0x100)
      READ(addr - 0x100);
    READ(addr);
  }
  case 0x0C: // SKW
    pc++;
  case 0x74:
  case 0x04:
  case 0x14:
  case 0x34:
  case 0x44:
  case 0x54:
  case 0x64: // SKB
  case 0x80:
  case 0x82:
  case 0x89:
  case 0xC2:
  case 0xD4:
  case 0xE2:
  case 0xF4:
    pc++;
  case 0xEA:
  case 0x1A:
  case 0x3A:
  case 0x5A:
  case 0x7A:
  case 0xDA:
  case 0xFA: // NOP
    goto loop;

    ARITH_ADDR_MODES_PTR(0xC7) // DCP
    WRITE(data, nz = READ(data));
    nz = uint8_t(nz - 1);
    WRITE(data, nz);
    pc++;
    nz = a - nz;
    c = ~nz;
    nz &= 0xFF;
    goto loop;

    ARITH_ADDR_MODES_PTR(0xE7) // ISC
    WRITE(data, nz = READ(data));
    nz = uint8_t(nz + 1);
    WRITE(data, nz);
    data = nz ^ 0xFF;
    goto adc_imm;

    ARITH_ADDR_MODES_PTR(0x27)
    { // RLA
      WRITE(data, nz = READ(data));
      int32_t temp = c;
      c = nz << 1;
      nz = uint8_t(c) | ((temp >> 8) & 0x01);
      WRITE(data, nz);
      pc++;
      nz = a &= nz;
      goto loop;
    }

    ARITH_ADDR_MODES_PTR(0x67)
    { // RRA
      int32_t temp;
      WRITE(data, temp = READ(data));
      nz = ((c >> 1) & 0x80) | (temp >> 1);
      WRITE(data, nz);
      data = nz;
      c = temp << 8;
      goto adc_imm;
    }

    ARITH_ADDR_MODES_PTR(0x07) // SLO
    WRITE(data, nz = READ(data));
    c = nz << 1;
    nz = uint8_t(c);
    WRITE(data, nz);
    nz = (a |= nz);
    pc++;
    goto loop;

    ARITH_ADDR_MODES_PTR(0x47) // SRE
    WRITE(data, nz = READ(data));
    c = nz << 8;
    nz >>= 1;
    WRITE(data, nz);
    nz = a ^= nz;
    pc++;
    goto loop;

  case 0x4B: // ALR
    nz = (a &= data);
    pc++;
    goto lsr_a;

  case 0x0B: // ANC
  case 0x2B:
    nz = a &= data;
    c = a << 1;
    pc++;
    goto loop;

  case 0x6B: // ARR
    nz = a = uint8_t(((data & a) >> 1) | ((c >> 1) & 0x80));
    c = a << 2;
    status = (status & ~st_v) | ((a ^ a << 1) & st_v);
    pc++;
    goto loop;

  case 0xAB: // LXA
    a = data;
    x = data;
    nz = data;
    pc++;
    goto loop;

  case 0xA3: // LAX
    IND_X
    goto lax_ptr;

  case 0xB3:
    IND_Y(true, true)
    goto lax_ptr;

  case 0xB7:
    data = uint8_t(data + y);

  case 0xA7:
    data = READ_LOW(data);
    goto lax_imm;

  case 0xBF:
  {
    data += y;
    HANDLE_PAGE_CROSSING(data);
    int32_t temp = data;
    ADD_PAGE;
    if (temp & 0x100)
      READ(data - 0x100);
    goto lax_ptr;
  }

  case 0xAF:
    ADD_PAGE

  lax_ptr:
    data = READ(data);
  lax_imm:
    nz = x = a = data;
    pc++;
    goto loop;

  case 0x83: // SAX
    IND_X
    goto sax_imm;

  case 0x97:
    data = uint8_t(data + y);
    goto sax_imm;

  case 0x8F:
    ADD_PAGE

  case 0x87:
  sax_imm:
    WRITE(data, a & x);
    pc++;
    goto loop;

  case 0xCB: // SBX
    data = (a & x) - data;
    c = (data <= 0xFF) ? 0x100 : 0;
    nz = x = uint8_t(data);
    pc++;
    goto loop;

  case 0x93: // SHA (ind),Y
    IND_Y(false, false)
    pc++;
    WRITE(data, uint8_t(a & x & ((data >> 8) + 1)));
    goto loop;

  case 0x9F:
  { // SHA abs,Y
    data += y;
    int32_t temp = data;
    ADD_PAGE
    READ(data - (temp & 0x100));
    pc++;
    WRITE(data, uint8_t(a & x & ((data >> 8) + 1)));
    goto loop;
  }

  case 0x9E:
  { // SHX abs,Y
    data += y;
    int32_t temp = data;
    ADD_PAGE
    READ(data - (temp & 0x100));
    pc++;
    if (!(temp & 0x100))
      WRITE(data, uint8_t(x & ((data >> 8) + 1)));
    goto loop;
  }

  case 0x9C:
  { // SHY abs,X
    data += x;
    int32_t temp = data;
    ADD_PAGE
    READ(data - (temp & 0x100));
    pc++;
    if (!(temp & 0x100))
      WRITE(data, uint8_t(y & ((data >> 8) + 1)));
    goto loop;
  }

  case 0x9B:
  { // SHS abs,Y
    data += y;
    int32_t temp = data;
    ADD_PAGE
    READ(data - (temp & 0x100));
    pc++;
    SET_SP(a & x);
    WRITE(data, uint8_t(a & x & ((data >> 8) + 1)));
    goto loop;
  }

  case 0xBB:
  { // LAS abs,Y
    data += y;
    HANDLE_PAGE_CROSSING(data);
    int32_t temp = data;
    ADD_PAGE
    if (temp & 0x100)
      READ(data - 0x100);
    pc++;
    a = GET_SP();
    x = a &= READ(data);
    SET_SP(a);
    goto loop;
  }

  // KIL (JAM) [HLT]
  default:
    // case 0x02: case 0x12: case 0x22: case 0x32: case 0x42: case 0x52: case 0x62: case 0x72: case 0x92: case 0xB2: case 0xD2: case 0xF2:
    isCorrectExecution = false;
    goto stop;

    // Unimplemented

    // case page_wrap_opcode: // HLT
    //  if ( pc > 0x10000 )
    //  {
    //   // handle wrap-around (assumes caller has put page of HLT at 0x10000)
    //   pc = (pc - 1) & 0xFFFF;
    //   clock_count -= 2;
    //   goto loop;
    //  }
    // fall through
    // default:
    //  // skip over proper number of bytes
    //  static uint32_t char const row [8] = { 0x95, 0x95, 0x95, 0xd5, 0x95, 0x95, 0xd5, 0xf5 };
    //  int len = row [opcode >> 2 & 7] >> (opcode << 1 & 6) & 3;
    //  if ( opcode == 0x9C )
    //   len = 3;
    //  pc += len - 1;
    //  error_count_++;
    //  goto loop;

    // result = result_badop; // TODO: re-enable
    goto stop;
  }

stop:
  pc--;
end:

{
  int temp;
  CALC_STATUS(temp);
  r.status = temp;
}
  
  this->clock_count = clock_count;
  r.pc = pc;
  r.sp = GET_SP();
  r.a = a;
  r.x = x;
  r.y = y;
  irq_time_ = LONG_MAX / 2 + 1;

  return result;
}

} // namespace quickerNES