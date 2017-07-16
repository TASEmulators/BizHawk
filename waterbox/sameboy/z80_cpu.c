#include <stdio.h>
#include <stdbool.h>
#include "gb.h"


typedef void GB_opcode_t(GB_gameboy_t *gb, uint8_t opcode);

static void ill(GB_gameboy_t *gb, uint8_t opcode)
{
    GB_log(gb, "Illegal Opcode. Halting.\n");
    gb->interrupt_enable = 0;
    gb->halted = true;
}

static void nop(GB_gameboy_t *gb, uint8_t opcode)
{
    GB_advance_cycles(gb, 4);
}

static void stop(GB_gameboy_t *gb, uint8_t opcode)
{
    GB_advance_cycles(gb, 4);
    if (gb->io_registers[GB_IO_KEY1] & 0x1) {
        /* Make sure we don't leave display_cycles not divisble by 4 in single speed mode */
        if (gb->display_cycles % 4 == 2) {
            GB_advance_cycles(gb, 4);
        }
        
        /* Todo: the switch is not instant. We should emulate this. */
        gb->cgb_double_speed ^= true;
        gb->io_registers[GB_IO_KEY1] = 0;
    }
    else {
        gb->stopped = true;
    }
    gb->pc++;
}

/* Operand naming conventions for functions:
   r = 8-bit register
   lr = low 8-bit register
   hr = high 8-bit register
   rr = 16-bit register
   d8 = 8-bit imm
   d16 = 16-bit imm
   d.. = [..]
   cc = condition code (z, nz, c, nc)
   */

static void ld_rr_d16(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t register_id;
    uint16_t value;
    GB_advance_cycles(gb, 4);
    register_id = (opcode >> 4) + 1;
    value = GB_read_memory(gb, gb->pc++);
    GB_advance_cycles(gb, 4);
    value |= GB_read_memory(gb, gb->pc++) << 8;
    GB_advance_cycles(gb, 4);
    gb->registers[register_id] = value;
}

static void ld_drr_a(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t register_id;
    GB_advance_cycles(gb, 4);
    register_id = (opcode >> 4) + 1;
    GB_write_memory(gb, gb->registers[register_id], gb->registers[GB_REGISTER_AF] >> 8);
    GB_advance_cycles(gb, 4);
}

static void inc_rr(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t register_id;
    GB_advance_cycles(gb, 8);
    register_id = (opcode >> 4) + 1;
    gb->registers[register_id]++;
}

static void inc_hr(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t register_id;
    GB_advance_cycles(gb, 4);
    register_id = ((opcode >> 4) + 1) & 0x03;
    gb->registers[register_id] += 0x100;
    gb->registers[GB_REGISTER_AF] &= ~(GB_SUBSTRACT_FLAG | GB_ZERO_FLAG | GB_HALF_CARRY_FLAG);

    if ((gb->registers[register_id] & 0x0F00) == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_HALF_CARRY_FLAG;
    }

    if ((gb->registers[register_id] & 0xFF00) == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
}
static void dec_hr(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t register_id;
    GB_advance_cycles(gb, 4);
    register_id = ((opcode >> 4) + 1) & 0x03;
    gb->registers[register_id] -= 0x100;
    gb->registers[GB_REGISTER_AF] &= ~(GB_ZERO_FLAG | GB_HALF_CARRY_FLAG);
    gb->registers[GB_REGISTER_AF] |= GB_SUBSTRACT_FLAG;

    if ((gb->registers[register_id] & 0x0F00) == 0xF00) {
        gb->registers[GB_REGISTER_AF] |= GB_HALF_CARRY_FLAG;
    }

    if ((gb->registers[register_id] & 0xFF00) == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
}

static void ld_hr_d8(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t register_id;
    GB_advance_cycles(gb, 4);
    register_id = ((opcode >> 4) + 1) & 0x03;
    gb->registers[register_id] &= 0xFF;
    gb->registers[register_id] |= GB_read_memory(gb, gb->pc++) << 8;
    GB_advance_cycles(gb, 4);
}

static void rlca(GB_gameboy_t *gb, uint8_t opcode)
{
    bool carry = (gb->registers[GB_REGISTER_AF] & 0x8000) != 0;

    GB_advance_cycles(gb, 4);
    gb->registers[GB_REGISTER_AF] = (gb->registers[GB_REGISTER_AF] & 0xFF00) << 1;
    if (carry) {
        gb->registers[GB_REGISTER_AF] |= GB_CARRY_FLAG | 0x0100;
    }
}

static void rla(GB_gameboy_t *gb, uint8_t opcode)
{
    bool bit7 = (gb->registers[GB_REGISTER_AF] & 0x8000) != 0;
    bool carry = (gb->registers[GB_REGISTER_AF] & GB_CARRY_FLAG) != 0;

    GB_advance_cycles(gb, 4);
    gb->registers[GB_REGISTER_AF] = (gb->registers[GB_REGISTER_AF] & 0xFF00) << 1;
    if (carry) {
        gb->registers[GB_REGISTER_AF] |= 0x0100;
    }
    if (bit7) {
        gb->registers[GB_REGISTER_AF] |= GB_CARRY_FLAG;
    }
}

static void ld_da16_sp(GB_gameboy_t *gb, uint8_t opcode)
{
    /* Todo: Verify order is correct */
    uint16_t addr;
    GB_advance_cycles(gb, 4);
    addr = GB_read_memory(gb, gb->pc++);
    GB_advance_cycles(gb, 4);
    addr |= GB_read_memory(gb, gb->pc++) << 8;
    GB_advance_cycles(gb, 4);
    GB_write_memory(gb, addr, gb->registers[GB_REGISTER_SP] & 0xFF);
    GB_advance_cycles(gb, 4);
    GB_write_memory(gb, addr+1, gb->registers[GB_REGISTER_SP] >> 8);
    GB_advance_cycles(gb, 4);
}

static void add_hl_rr(GB_gameboy_t *gb, uint8_t opcode)
{
    uint16_t hl = gb->registers[GB_REGISTER_HL];
    uint16_t rr;
    uint8_t register_id;
    GB_advance_cycles(gb, 8);
    register_id = (opcode >> 4) + 1;
    rr = gb->registers[register_id];
    gb->registers[GB_REGISTER_HL] = hl + rr;
    gb->registers[GB_REGISTER_AF] &= ~(GB_SUBSTRACT_FLAG | GB_CARRY_FLAG | GB_HALF_CARRY_FLAG);

    /* The meaning of the Half Carry flag is really hard to track -_- */
    if (((hl & 0xFFF) + (rr & 0xFFF)) & 0x1000) {
        gb->registers[GB_REGISTER_AF] |= GB_HALF_CARRY_FLAG;
    }

    if ( ((unsigned long) hl) + ((unsigned long) rr) & 0x10000) {
        gb->registers[GB_REGISTER_AF] |= GB_CARRY_FLAG;
    }
}

static void ld_a_drr(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t register_id;
    register_id = (opcode >> 4) + 1;
    GB_advance_cycles(gb, 4);
    gb->registers[GB_REGISTER_AF] &= 0xFF;
    gb->registers[GB_REGISTER_AF] |= GB_read_memory(gb, gb->registers[register_id]) << 8;
    GB_advance_cycles(gb, 4);
}

static void dec_rr(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t register_id;
    GB_advance_cycles(gb, 8);
    register_id = (opcode >> 4) + 1;
    gb->registers[register_id]--;
}

static void inc_lr(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t register_id;
    uint8_t value;
    GB_advance_cycles(gb, 4);
    register_id = (opcode >> 4) + 1;

    value = (gb->registers[register_id] & 0xFF) + 1;
    gb->registers[register_id] = (gb->registers[register_id] & 0xFF00) | value;

    gb->registers[GB_REGISTER_AF] &= ~(GB_SUBSTRACT_FLAG | GB_ZERO_FLAG | GB_HALF_CARRY_FLAG);

    if ((gb->registers[register_id] & 0x0F) == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_HALF_CARRY_FLAG;
    }

    if ((gb->registers[register_id] & 0xFF) == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
}
static void dec_lr(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t register_id;
    uint8_t value;
    GB_advance_cycles(gb, 4);
    register_id = (opcode >> 4) + 1;

    value = (gb->registers[register_id] & 0xFF) - 1;
    gb->registers[register_id] = (gb->registers[register_id] & 0xFF00) | value;

    gb->registers[GB_REGISTER_AF] &= ~(GB_ZERO_FLAG | GB_HALF_CARRY_FLAG);
    gb->registers[GB_REGISTER_AF] |= GB_SUBSTRACT_FLAG;

    if ((gb->registers[register_id] & 0x0F) == 0xF) {
        gb->registers[GB_REGISTER_AF] |= GB_HALF_CARRY_FLAG;
    }

    if ((gb->registers[register_id] & 0xFF) == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
}

static void ld_lr_d8(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t register_id;
    GB_advance_cycles(gb, 4);
    register_id = (opcode >> 4) + 1;
    gb->registers[register_id] &= 0xFF00;
    gb->registers[register_id] |= GB_read_memory(gb, gb->pc++);
    GB_advance_cycles(gb, 4);
}

static void rrca(GB_gameboy_t *gb, uint8_t opcode)
{
    bool carry = (gb->registers[GB_REGISTER_AF] & 0x100) != 0;

    GB_advance_cycles(gb, 4);
    gb->registers[GB_REGISTER_AF] = (gb->registers[GB_REGISTER_AF] >> 1) & 0xFF00;
    if (carry) {
        gb->registers[GB_REGISTER_AF] |= GB_CARRY_FLAG | 0x8000;
    }
}

static void rra(GB_gameboy_t *gb, uint8_t opcode)
{
    bool bit1 = (gb->registers[GB_REGISTER_AF] & 0x0100) != 0;
    bool carry = (gb->registers[GB_REGISTER_AF] & GB_CARRY_FLAG) != 0;

    GB_advance_cycles(gb, 4);
    gb->registers[GB_REGISTER_AF] = (gb->registers[GB_REGISTER_AF] >> 1) & 0xFF00;
    if (carry) {
        gb->registers[GB_REGISTER_AF] |= 0x8000;
    }
    if (bit1) {
        gb->registers[GB_REGISTER_AF] |= GB_CARRY_FLAG;
    }
}

static void jr_r8(GB_gameboy_t *gb, uint8_t opcode)
{
    /* Todo: Verify cycles are not 8 and 4 instead */
    GB_advance_cycles(gb, 4);
    gb->pc += (int8_t) GB_read_memory(gb, gb->pc++);
    GB_advance_cycles(gb, 8);
}

static bool condition_code(GB_gameboy_t *gb, uint8_t opcode)
{
    switch ((opcode >> 3) & 0x3) {
        case 0:
            return !(gb->registers[GB_REGISTER_AF] & GB_ZERO_FLAG);
        case 1:
            return (gb->registers[GB_REGISTER_AF] & GB_ZERO_FLAG);
        case 2:
            return !(gb->registers[GB_REGISTER_AF] & GB_CARRY_FLAG);
        case 3:
            return (gb->registers[GB_REGISTER_AF] & GB_CARRY_FLAG);
    }

    return false;
}

static void jr_cc_r8(GB_gameboy_t *gb, uint8_t opcode)
{
    if (condition_code(gb, opcode)) {
        GB_advance_cycles(gb, 4);
        gb->pc += (int8_t)GB_read_memory(gb, gb->pc++);
        GB_advance_cycles(gb, 8);
    }
    else {
        GB_advance_cycles(gb, 8);
        gb->pc += 1;
    }
}

static void daa(GB_gameboy_t *gb, uint8_t opcode)
{
    /* This function is UGLY and UNREADABLE! But it passes Blargg's daa test! */
    GB_advance_cycles(gb, 4);
    gb->registers[GB_REGISTER_AF] &= ~GB_ZERO_FLAG;
    if (gb->registers[GB_REGISTER_AF] & GB_SUBSTRACT_FLAG) {
        if (gb->registers[GB_REGISTER_AF] & GB_HALF_CARRY_FLAG) {
            gb->registers[GB_REGISTER_AF] &= ~GB_HALF_CARRY_FLAG;
            if (gb->registers[GB_REGISTER_AF] & GB_CARRY_FLAG) {
                gb->registers[GB_REGISTER_AF] += 0x9A00;
            }
            else {
                gb->registers[GB_REGISTER_AF] += 0xFA00;
            }
        }
        else if(gb->registers[GB_REGISTER_AF] & GB_CARRY_FLAG) {
            gb->registers[GB_REGISTER_AF] += 0xA000;
        }
    }
    else {
        if (gb->registers[GB_REGISTER_AF] & GB_HALF_CARRY_FLAG) {
            uint16_t number = gb->registers[GB_REGISTER_AF] >> 8;
            if (gb->registers[GB_REGISTER_AF] & GB_CARRY_FLAG) {
                number += 0x100;
            }
            gb->registers[GB_REGISTER_AF] = 0;
            number += 0x06;
            if (number >= 0xa0) {
                number -= 0xa0;
                gb->registers[GB_REGISTER_AF] |= GB_CARRY_FLAG;
            }
            gb->registers[GB_REGISTER_AF] |= number << 8;
        }
        else {
            uint16_t number = gb->registers[GB_REGISTER_AF] >> 8;
            if (gb->registers[GB_REGISTER_AF] & GB_CARRY_FLAG) {
                number += 0x100;
            }
            if (number > 0x99) {
                number += 0x60;
            }
            number = (number & 0x0F) + ((number & 0x0F) > 9 ? 6 : 0) + (number & 0xFF0);
            gb->registers[GB_REGISTER_AF] = number << 8;
            if (number & 0xFF00) {
                gb->registers[GB_REGISTER_AF]  |= GB_CARRY_FLAG;
            }
        }
    }
    if ((gb->registers[GB_REGISTER_AF] & 0xFF00) == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
}

static void cpl(GB_gameboy_t *gb, uint8_t opcode)
{
    GB_advance_cycles(gb, 4);
    gb->registers[GB_REGISTER_AF] ^= 0xFF00;
    gb->registers[GB_REGISTER_AF] |= GB_HALF_CARRY_FLAG | GB_SUBSTRACT_FLAG;
}

static void scf(GB_gameboy_t *gb, uint8_t opcode)
{
    GB_advance_cycles(gb, 4);
    gb->registers[GB_REGISTER_AF] |= GB_CARRY_FLAG;
    gb->registers[GB_REGISTER_AF] &= ~(GB_HALF_CARRY_FLAG | GB_SUBSTRACT_FLAG);
}

static void ccf(GB_gameboy_t *gb, uint8_t opcode)
{
    GB_advance_cycles(gb, 4);
    gb->registers[GB_REGISTER_AF] ^= GB_CARRY_FLAG;
    gb->registers[GB_REGISTER_AF] &= ~(GB_HALF_CARRY_FLAG | GB_SUBSTRACT_FLAG);
}

static void ld_dhli_a(GB_gameboy_t *gb, uint8_t opcode)
{
    GB_advance_cycles(gb, 4);
    GB_write_memory(gb, gb->registers[GB_REGISTER_HL]++, gb->registers[GB_REGISTER_AF] >> 8);
    GB_advance_cycles(gb, 4);
}

static void ld_dhld_a(GB_gameboy_t *gb, uint8_t opcode)
{
    GB_advance_cycles(gb, 4);
    GB_write_memory(gb, gb->registers[GB_REGISTER_HL]--, gb->registers[GB_REGISTER_AF] >> 8);
    GB_advance_cycles(gb, 4);
}

static void ld_a_dhli(GB_gameboy_t *gb, uint8_t opcode)
{
    GB_advance_cycles(gb, 4);
    gb->registers[GB_REGISTER_AF] &= 0xFF;
    gb->registers[GB_REGISTER_AF] |= GB_read_memory(gb, gb->registers[GB_REGISTER_HL]++) << 8;
    GB_advance_cycles(gb, 4);
}

static void ld_a_dhld(GB_gameboy_t *gb, uint8_t opcode)
{
    GB_advance_cycles(gb, 4);
    gb->registers[GB_REGISTER_AF] &= 0xFF;
    gb->registers[GB_REGISTER_AF] |= GB_read_memory(gb, gb->registers[GB_REGISTER_HL]--) << 8;
    GB_advance_cycles(gb, 4);
}

static void inc_dhl(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t value;
    GB_advance_cycles(gb, 4);
    value = GB_read_memory(gb, gb->registers[GB_REGISTER_HL]) + 1;
    GB_advance_cycles(gb, 4);
    GB_write_memory(gb, gb->registers[GB_REGISTER_HL], value);
    GB_advance_cycles(gb, 4);

    gb->registers[GB_REGISTER_AF] &= ~(GB_SUBSTRACT_FLAG | GB_ZERO_FLAG | GB_HALF_CARRY_FLAG);
    if ((value & 0x0F) == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_HALF_CARRY_FLAG;
    }

    if ((value & 0xFF) == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
}

static void dec_dhl(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t value;
    GB_advance_cycles(gb, 4);
    value = GB_read_memory(gb, gb->registers[GB_REGISTER_HL]) - 1;
    GB_advance_cycles(gb, 4);
    GB_write_memory(gb, gb->registers[GB_REGISTER_HL], value);
    GB_advance_cycles(gb, 4);

    gb->registers[GB_REGISTER_AF] &= ~( GB_ZERO_FLAG | GB_HALF_CARRY_FLAG);
    gb->registers[GB_REGISTER_AF] |= GB_SUBSTRACT_FLAG;
    if ((value & 0x0F) == 0x0F) {
        gb->registers[GB_REGISTER_AF] |= GB_HALF_CARRY_FLAG;
    }

    if ((value & 0xFF) == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
}

static void ld_dhl_d8(GB_gameboy_t *gb, uint8_t opcode)
{
    GB_advance_cycles(gb, 4);
    uint8_t data = GB_read_memory(gb, gb->pc++);
    GB_advance_cycles(gb, 4);
    GB_write_memory(gb, gb->registers[GB_REGISTER_HL], data);
    GB_advance_cycles(gb, 4);
}

uint8_t get_src_value(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t src_register_id;
    uint8_t src_low;
    src_register_id = ((opcode >> 1) + 1) & 3;
    src_low = opcode & 1;
    if (src_register_id == GB_REGISTER_AF) {
        if (src_low) {
            return gb->registers[GB_REGISTER_AF] >> 8;
        }
        uint8_t ret = GB_read_memory(gb, gb->registers[GB_REGISTER_HL]);
        GB_advance_cycles(gb, 4);
        return ret;
    }
    if (src_low) {
        return gb->registers[src_register_id] & 0xFF;
    }
    return gb->registers[src_register_id] >> 8;
}

static void set_src_value(GB_gameboy_t *gb, uint8_t opcode, uint8_t value)
{
    uint8_t src_register_id;
    uint8_t src_low;
    src_register_id = ((opcode >> 1) + 1) & 3;
    src_low = opcode & 1;

    if (src_register_id == GB_REGISTER_AF) {
        if (src_low) {
            gb->registers[GB_REGISTER_AF] &= 0xFF;
            gb->registers[GB_REGISTER_AF] |= value << 8;
        }
        else {
            GB_write_memory(gb, gb->registers[GB_REGISTER_HL], value);
            GB_advance_cycles(gb, 4);
        }
    }
    else {
        if (src_low) {
            gb->registers[src_register_id] &= 0xFF00;
            gb->registers[src_register_id] |= value;
        }
        else {
            gb->registers[src_register_id] &= 0xFF;
            gb->registers[src_register_id] |= value << 8;
        }
    }
}

/* The LD r,r instruction is extremely common and extremely simple. Decoding this opcode at runtime is a significent
   performance hit, so we generate functions for every ld x,y couple (including [hl]) at compile time using macros. */

/* Todo: It's probably wise to do the same to all opcodes. */

#define LD_X_Y(x, y) \
static void ld_##x##_##y(GB_gameboy_t *gb, uint8_t opcode) \
{ \
    GB_advance_cycles(gb, 4); \
    gb->x = gb->y;\
}

#define LD_X_DHL(x) \
static void ld_##x##_##dhl(GB_gameboy_t *gb, uint8_t opcode) \
{ \
GB_advance_cycles(gb, 4); \
gb->x = GB_read_memory(gb, gb->registers[GB_REGISTER_HL]); \
GB_advance_cycles(gb, 4);\
}

#define LD_DHL_Y(y) \
static void ld_##dhl##_##y(GB_gameboy_t *gb, uint8_t opcode) \
{ \
GB_advance_cycles(gb, 4); \
GB_write_memory(gb, gb->registers[GB_REGISTER_HL], gb->y); \
GB_advance_cycles(gb, 4);\
}

LD_X_Y(b,c) LD_X_Y(b,d) LD_X_Y(b,e) LD_X_Y(b,h) LD_X_Y(b,l)             LD_X_DHL(b) LD_X_Y(b,a)
LD_X_Y(c,b)             LD_X_Y(c,d) LD_X_Y(c,e) LD_X_Y(c,h) LD_X_Y(c,l) LD_X_DHL(c) LD_X_Y(c,a)
LD_X_Y(d,b) LD_X_Y(d,c)             LD_X_Y(d,e) LD_X_Y(d,h) LD_X_Y(d,l) LD_X_DHL(d) LD_X_Y(d,a)
LD_X_Y(e,b) LD_X_Y(e,c) LD_X_Y(e,d)             LD_X_Y(e,h) LD_X_Y(e,l) LD_X_DHL(e) LD_X_Y(e,a)
LD_X_Y(h,b) LD_X_Y(h,c) LD_X_Y(h,d) LD_X_Y(h,e)             LD_X_Y(h,l) LD_X_DHL(h) LD_X_Y(h,a)
LD_X_Y(l,b) LD_X_Y(l,c) LD_X_Y(l,d) LD_X_Y(l,e) LD_X_Y(l,h)             LD_X_DHL(l) LD_X_Y(l,a)
LD_DHL_Y(b) LD_DHL_Y(c) LD_DHL_Y(d) LD_DHL_Y(e) LD_DHL_Y(h) LD_DHL_Y(l)             LD_DHL_Y(a)
LD_X_Y(a,b) LD_X_Y(a,c) LD_X_Y(a,d) LD_X_Y(a,e) LD_X_Y(a,h) LD_X_Y(a,l) LD_X_DHL(a)


static void add_a_r(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t value, a;
    GB_advance_cycles(gb, 4);
    value = get_src_value(gb, opcode);
    a = gb->registers[GB_REGISTER_AF] >> 8;
    gb->registers[GB_REGISTER_AF] = (a + value) << 8;
    if ((uint8_t)(a + value) == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
    if ((a & 0xF) + (value & 0xF) > 0x0F) {
        gb->registers[GB_REGISTER_AF] |= GB_HALF_CARRY_FLAG;
    }
    if (((unsigned long) a) + ((unsigned long) value) > 0xFF) {
        gb->registers[GB_REGISTER_AF] |= GB_CARRY_FLAG;
    }
}

static void adc_a_r(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t value, a, carry;
    GB_advance_cycles(gb, 4);
    value = get_src_value(gb, opcode);
    a = gb->registers[GB_REGISTER_AF] >> 8;
    carry = (gb->registers[GB_REGISTER_AF] & GB_CARRY_FLAG) != 0;
    gb->registers[GB_REGISTER_AF] = (a + value + carry) << 8;

    if ((uint8_t)(a + value + carry) == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
    if ((a & 0xF) + (value & 0xF) + carry > 0x0F) {
        gb->registers[GB_REGISTER_AF] |= GB_HALF_CARRY_FLAG;
    }
    if (((unsigned long) a) + ((unsigned long) value) + carry > 0xFF) {
        gb->registers[GB_REGISTER_AF] |= GB_CARRY_FLAG;
    }
}

static void sub_a_r(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t value, a;
    GB_advance_cycles(gb, 4);
    value = get_src_value(gb, opcode);
    a = gb->registers[GB_REGISTER_AF] >> 8;
    gb->registers[GB_REGISTER_AF] = ((a - value) << 8) | GB_SUBSTRACT_FLAG;
    if (a == value) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
    if ((a & 0xF) < (value & 0xF)) {
        gb->registers[GB_REGISTER_AF] |= GB_HALF_CARRY_FLAG;
    }
    if (a < value) {
        gb->registers[GB_REGISTER_AF] |= GB_CARRY_FLAG;
    }
}

static void sbc_a_r(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t value, a, carry;
    GB_advance_cycles(gb, 4);
    value = get_src_value(gb, opcode);
    a = gb->registers[GB_REGISTER_AF] >> 8;
    carry = (gb->registers[GB_REGISTER_AF] & GB_CARRY_FLAG) != 0;
    gb->registers[GB_REGISTER_AF] = ((a - value - carry) << 8) | GB_SUBSTRACT_FLAG;

    if ((uint8_t) (a - value - carry) == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
    if ((a & 0xF) < (value & 0xF) + carry) {
        gb->registers[GB_REGISTER_AF] |= GB_HALF_CARRY_FLAG;
    }
    if (((unsigned long) a) - ((unsigned long) value) - carry > 0xFF) {
        gb->registers[GB_REGISTER_AF] |= GB_CARRY_FLAG;
    }
}

static void and_a_r(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t value, a;
    GB_advance_cycles(gb, 4);
    value = get_src_value(gb, opcode);
    a = gb->registers[GB_REGISTER_AF] >> 8;
    gb->registers[GB_REGISTER_AF] = ((a & value) << 8) | GB_HALF_CARRY_FLAG;
    if ((a & value) == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
}

static void xor_a_r(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t value, a;
    GB_advance_cycles(gb, 4);
    value = get_src_value(gb, opcode);
    a = gb->registers[GB_REGISTER_AF] >> 8;
    gb->registers[GB_REGISTER_AF] = (a ^ value) << 8;
    if ((a ^ value) == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
}

static void or_a_r(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t value, a;
    GB_advance_cycles(gb, 4);
    value = get_src_value(gb, opcode);
    a = gb->registers[GB_REGISTER_AF] >> 8;
    gb->registers[GB_REGISTER_AF] = (a | value) << 8;
    if ((a | value) == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
}

static void cp_a_r(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t value, a;
    GB_advance_cycles(gb, 4);
    value = get_src_value(gb, opcode);
    a = gb->registers[GB_REGISTER_AF] >> 8;
    gb->registers[GB_REGISTER_AF] &= 0xFF00;
    gb->registers[GB_REGISTER_AF] |= GB_SUBSTRACT_FLAG;
    if (a == value) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
    if ((a & 0xF) < (value & 0xF)) {
        gb->registers[GB_REGISTER_AF] |= GB_HALF_CARRY_FLAG;
    }
    if (a < value) {
        gb->registers[GB_REGISTER_AF] |= GB_CARRY_FLAG;
    }
}

static void halt(GB_gameboy_t *gb, uint8_t opcode)
{
    GB_advance_cycles(gb, 4);
    gb->halted = true;
    /* Despite what some online documentations say, the HALT bug also happens on a CGB, in both CGB and DMG modes. */
    if (!gb->ime && (gb->interrupt_enable & gb->io_registers[GB_IO_IF] & 0x1F) != 0) {
        gb->halted = false;
        gb->halt_bug = true;
    }
}

static void ret_cc(GB_gameboy_t *gb, uint8_t opcode)
{
    /* Todo: Verify timing */
    if (condition_code(gb, opcode)) {
        GB_debugger_ret_hook(gb);
        GB_advance_cycles(gb, 8);
        gb->pc = GB_read_memory(gb, gb->registers[GB_REGISTER_SP]);
        GB_advance_cycles(gb, 4);
        gb->pc |= GB_read_memory(gb, gb->registers[GB_REGISTER_SP] + 1) << 8;
        GB_advance_cycles(gb, 8);
        gb->registers[GB_REGISTER_SP] += 2;
    }
    else {
        GB_advance_cycles(gb, 8);
    }
}

static void pop_rr(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t register_id;
    GB_advance_cycles(gb, 4);
    register_id = ((opcode >> 4) + 1) & 3;
    gb->registers[register_id] = GB_read_memory(gb, gb->registers[GB_REGISTER_SP]);
    GB_advance_cycles(gb, 4);
    gb->registers[register_id] |= GB_read_memory(gb, gb->registers[GB_REGISTER_SP] + 1) << 8;
    GB_advance_cycles(gb, 4);
    gb->registers[GB_REGISTER_AF] &= 0xFFF0; // Make sure we don't set impossible flags on F! See Blargg's PUSH AF test.
    gb->registers[GB_REGISTER_SP] += 2;
}

static void jp_cc_a16(GB_gameboy_t *gb, uint8_t opcode)
{
    if (condition_code(gb, opcode)) {
        GB_advance_cycles(gb, 4);
        uint16_t addr = GB_read_memory(gb, gb->pc);
        GB_advance_cycles(gb, 4);
        addr |= (GB_read_memory(gb, gb->pc + 1) << 8);
        GB_advance_cycles(gb, 8);
        gb->pc = addr;

    }
    else {
        GB_advance_cycles(gb, 12);
        gb->pc += 2;
    }
}

static void jp_a16(GB_gameboy_t *gb, uint8_t opcode)
{
    GB_advance_cycles(gb, 4);
    uint16_t addr = GB_read_memory(gb, gb->pc);
    GB_advance_cycles(gb, 4);
    addr |= (GB_read_memory(gb, gb->pc + 1) << 8);
    GB_advance_cycles(gb, 8);
    gb->pc = addr;}

static void call_cc_a16(GB_gameboy_t *gb, uint8_t opcode)
{
    uint16_t call_addr = gb->pc - 1;
    if (condition_code(gb, opcode)) {
        GB_advance_cycles(gb, 4);
        gb->registers[GB_REGISTER_SP] -= 2;
        uint16_t addr = GB_read_memory(gb, gb->pc++);
        GB_advance_cycles(gb, 4);
        addr |= (GB_read_memory(gb, gb->pc++) << 8);
        GB_advance_cycles(gb, 8);
        GB_write_memory(gb, gb->registers[GB_REGISTER_SP] + 1, (gb->pc) >> 8);
        GB_advance_cycles(gb, 4);
        GB_write_memory(gb, gb->registers[GB_REGISTER_SP], (gb->pc) & 0xFF);
        GB_advance_cycles(gb, 4);
        gb->pc = addr;

        GB_debugger_call_hook(gb, call_addr);
    }
    else {
        GB_advance_cycles(gb, 12);
        gb->pc += 2;
    }
}

static void push_rr(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t register_id;
    GB_advance_cycles(gb, 8);
    register_id = ((opcode >> 4) + 1) & 3;
    gb->registers[GB_REGISTER_SP] -= 2;
    GB_write_memory(gb, gb->registers[GB_REGISTER_SP] + 1, (gb->registers[register_id]) >> 8);
    GB_advance_cycles(gb, 4);
    GB_write_memory(gb, gb->registers[GB_REGISTER_SP], (gb->registers[register_id]) & 0xFF);
    GB_advance_cycles(gb, 4);
}

static void add_a_d8(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t value, a;
    GB_advance_cycles(gb, 4);
    value = GB_read_memory(gb, gb->pc++);
    GB_advance_cycles(gb, 4);
    a = gb->registers[GB_REGISTER_AF] >> 8;
    gb->registers[GB_REGISTER_AF] = (a + value) << 8;
    if ((uint8_t) (a + value) == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
    if ((a & 0xF) + (value & 0xF) > 0x0F) {
        gb->registers[GB_REGISTER_AF] |= GB_HALF_CARRY_FLAG;
    }
    if (((unsigned long) a) + ((unsigned long) value) > 0xFF) {
        gb->registers[GB_REGISTER_AF] |= GB_CARRY_FLAG;
    }
}

static void adc_a_d8(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t value, a, carry;
    GB_advance_cycles(gb, 4);
    value = GB_read_memory(gb, gb->pc++);
    GB_advance_cycles(gb, 4);
    a = gb->registers[GB_REGISTER_AF] >> 8;
    carry = (gb->registers[GB_REGISTER_AF] & GB_CARRY_FLAG) != 0;
    gb->registers[GB_REGISTER_AF] = (a + value + carry) << 8;

    if (gb->registers[GB_REGISTER_AF] == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
    if ((a & 0xF) + (value & 0xF) + carry > 0x0F) {
        gb->registers[GB_REGISTER_AF] |= GB_HALF_CARRY_FLAG;
    }
    if (((unsigned long) a) + ((unsigned long) value) + carry > 0xFF) {
        gb->registers[GB_REGISTER_AF] |= GB_CARRY_FLAG;
    }
}

static void sub_a_d8(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t value, a;
    GB_advance_cycles(gb, 4);
    value = GB_read_memory(gb, gb->pc++);
    GB_advance_cycles(gb, 4);
    a = gb->registers[GB_REGISTER_AF] >> 8;
    gb->registers[GB_REGISTER_AF] = ((a - value) << 8) | GB_SUBSTRACT_FLAG;
    if (a == value) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
    if ((a & 0xF) < (value & 0xF)) {
        gb->registers[GB_REGISTER_AF] |= GB_HALF_CARRY_FLAG;
    }
    if (a < value) {
        gb->registers[GB_REGISTER_AF] |= GB_CARRY_FLAG;
    }
}

static void sbc_a_d8(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t value, a, carry;
    GB_advance_cycles(gb, 4);
    value = GB_read_memory(gb, gb->pc++);
    GB_advance_cycles(gb, 4);
    a = gb->registers[GB_REGISTER_AF] >> 8;
    carry = (gb->registers[GB_REGISTER_AF] & GB_CARRY_FLAG) != 0;
    gb->registers[GB_REGISTER_AF] = ((a - value - carry) << 8) | GB_SUBSTRACT_FLAG;

    if ((uint8_t) (a - value - carry) == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
    if ((a & 0xF) < (value & 0xF) + carry) {
        gb->registers[GB_REGISTER_AF] |= GB_HALF_CARRY_FLAG;
    }
    if (((unsigned long) a) - ((unsigned long) value) - carry > 0xFF) {
        gb->registers[GB_REGISTER_AF] |= GB_CARRY_FLAG;
    }
}

static void and_a_d8(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t value, a;
    GB_advance_cycles(gb, 4);
    value = GB_read_memory(gb, gb->pc++);
    GB_advance_cycles(gb, 4);
    a = gb->registers[GB_REGISTER_AF] >> 8;
    gb->registers[GB_REGISTER_AF] = ((a & value) << 8) | GB_HALF_CARRY_FLAG;
    if ((a & value) == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
}

static void xor_a_d8(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t value, a;
    GB_advance_cycles(gb, 4);
    value = GB_read_memory(gb, gb->pc++);
    GB_advance_cycles(gb, 4);
    a = gb->registers[GB_REGISTER_AF] >> 8;
    gb->registers[GB_REGISTER_AF] = (a ^ value) << 8;
    if ((a ^ value) == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
}

static void or_a_d8(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t value, a;
    GB_advance_cycles(gb, 4);
    value = GB_read_memory(gb, gb->pc++);
    GB_advance_cycles(gb, 4);
    a = gb->registers[GB_REGISTER_AF] >> 8;
    gb->registers[GB_REGISTER_AF] = (a | value) << 8;
    if ((a | value) == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
}

static void cp_a_d8(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t value, a;
    GB_advance_cycles(gb, 4);
    value = GB_read_memory(gb, gb->pc++);
    GB_advance_cycles(gb, 4);
    a = gb->registers[GB_REGISTER_AF] >> 8;
    gb->registers[GB_REGISTER_AF] &= 0xFF00;
    gb->registers[GB_REGISTER_AF] |= GB_SUBSTRACT_FLAG;
    if (a == value) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
    if ((a & 0xF) < (value & 0xF)) {
        gb->registers[GB_REGISTER_AF] |= GB_HALF_CARRY_FLAG;
    }
    if (a < value) {
        gb->registers[GB_REGISTER_AF] |= GB_CARRY_FLAG;
    }
}

static void rst(GB_gameboy_t *gb, uint8_t opcode)
{
    uint16_t call_addr = gb->pc - 1;
    GB_advance_cycles(gb, 8);
    gb->registers[GB_REGISTER_SP] -= 2;
    GB_write_memory(gb, gb->registers[GB_REGISTER_SP] + 1, (gb->pc) >> 8);
    GB_advance_cycles(gb, 4);
    GB_write_memory(gb, gb->registers[GB_REGISTER_SP], (gb->pc) & 0xFF);
    GB_advance_cycles(gb, 4);
    gb->pc = opcode ^ 0xC7;
    GB_debugger_call_hook(gb, call_addr);
}

static void ret(GB_gameboy_t *gb, uint8_t opcode)
{
    GB_debugger_ret_hook(gb);
    GB_advance_cycles(gb, 4);
    gb->pc = GB_read_memory(gb, gb->registers[GB_REGISTER_SP]);
    GB_advance_cycles(gb, 4);
    gb->pc |= GB_read_memory(gb, gb->registers[GB_REGISTER_SP] + 1) << 8;
    GB_advance_cycles(gb, 8);
    gb->registers[GB_REGISTER_SP] += 2;
}

static void reti(GB_gameboy_t *gb, uint8_t opcode)
{
    ret(gb, opcode);
    gb->ime = true;
}

static void call_a16(GB_gameboy_t *gb, uint8_t opcode)
{
    uint16_t call_addr = gb->pc - 1;
    GB_advance_cycles(gb, 4);
    gb->registers[GB_REGISTER_SP] -= 2;
    uint16_t addr = GB_read_memory(gb, gb->pc++);
    GB_advance_cycles(gb, 4);
    addr |= (GB_read_memory(gb, gb->pc++) << 8);
    GB_advance_cycles(gb, 8);
    GB_write_memory(gb, gb->registers[GB_REGISTER_SP] + 1, (gb->pc) >> 8);
    GB_advance_cycles(gb, 4);
    GB_write_memory(gb, gb->registers[GB_REGISTER_SP], (gb->pc) & 0xFF);
    GB_advance_cycles(gb, 4);
    gb->pc = addr;
    GB_debugger_call_hook(gb, call_addr);
}

static void ld_da8_a(GB_gameboy_t *gb, uint8_t opcode)
{
    GB_advance_cycles(gb, 4);
    uint8_t temp = GB_read_memory(gb, gb->pc++);
    GB_advance_cycles(gb, 4);
    GB_write_memory(gb, 0xFF00 + temp, gb->registers[GB_REGISTER_AF] >> 8);
    GB_advance_cycles(gb, 4);
}

static void ld_a_da8(GB_gameboy_t *gb, uint8_t opcode)
{
    GB_advance_cycles(gb, 4);
    gb->registers[GB_REGISTER_AF] &= 0xFF;
    uint8_t temp = GB_read_memory(gb, gb->pc++);
    GB_advance_cycles(gb, 4);
    gb->registers[GB_REGISTER_AF] |= GB_read_memory(gb, 0xFF00 + temp) << 8;
    GB_advance_cycles(gb, 4);
}

static void ld_dc_a(GB_gameboy_t *gb, uint8_t opcode)
{
    GB_advance_cycles(gb, 4);
    GB_write_memory(gb, 0xFF00 + (gb->registers[GB_REGISTER_BC] & 0xFF), gb->registers[GB_REGISTER_AF] >> 8);
    GB_advance_cycles(gb, 4);
}

static void ld_a_dc(GB_gameboy_t *gb, uint8_t opcode)
{
    GB_advance_cycles(gb, 4);
    gb->registers[GB_REGISTER_AF] &= 0xFF;
    gb->registers[GB_REGISTER_AF] |= GB_read_memory(gb, 0xFF00 + (gb->registers[GB_REGISTER_BC] & 0xFF)) << 8;
    GB_advance_cycles(gb, 4);
}

static void add_sp_r8(GB_gameboy_t *gb, uint8_t opcode)
{
    int16_t offset;
    uint16_t sp = gb->registers[GB_REGISTER_SP];
    GB_advance_cycles(gb, 4);
    offset = (int8_t) GB_read_memory(gb, gb->pc++);
    GB_advance_cycles(gb, 12);
    gb->registers[GB_REGISTER_SP] += offset;

    gb->registers[GB_REGISTER_AF] &= 0xFF00;

    /* A new instruction, a new meaning for Half Carry! */
    if ((sp & 0xF) + (offset & 0xF) > 0xF) {
        gb->registers[GB_REGISTER_AF] |= GB_HALF_CARRY_FLAG;
    }

    if ((sp & 0xFF) + (offset & 0xFF) > 0xFF)  {
        gb->registers[GB_REGISTER_AF] |= GB_CARRY_FLAG;
    }
}

static void jp_hl(GB_gameboy_t *gb, uint8_t opcode)
{
    GB_advance_cycles(gb, 4);
    gb->pc = gb->registers[GB_REGISTER_HL];
}

static void ld_da16_a(GB_gameboy_t *gb, uint8_t opcode)
{
    uint16_t addr;
    GB_advance_cycles(gb, 4);
    addr = GB_read_memory(gb, gb->pc++);
    GB_advance_cycles(gb, 4);
    addr |= GB_read_memory(gb, gb->pc++) << 8;
    GB_advance_cycles(gb, 4);
    GB_write_memory(gb, addr, gb->registers[GB_REGISTER_AF] >> 8);
    GB_advance_cycles(gb, 4);
}

static void ld_a_da16(GB_gameboy_t *gb, uint8_t opcode)
{
    uint16_t addr;
    GB_advance_cycles(gb, 4);
    gb->registers[GB_REGISTER_AF] &= 0xFF;
    addr = GB_read_memory(gb, gb->pc++);
    GB_advance_cycles(gb, 4);
    addr |= GB_read_memory(gb, gb->pc++) << 8 ;
    GB_advance_cycles(gb, 4);
    gb->registers[GB_REGISTER_AF] |= GB_read_memory(gb, addr) << 8;
    GB_advance_cycles(gb, 4);
}

static void di(GB_gameboy_t *gb, uint8_t opcode)
{
    /* DI is NOT delayed, not even on a CGB. Mooneye's di_timing-GS test fails on a CGB
       for different reasons.*/
    GB_advance_cycles(gb, 4);
    gb->ime = false;
}

static void ei(GB_gameboy_t *gb, uint8_t opcode)
{
    /* ei is actually "disable interrupts for one instruction, then enable them". */
    GB_advance_cycles(gb, 4);
    gb->ime = false;
    gb->ime_toggle = true;
}

static void ld_hl_sp_r8(GB_gameboy_t *gb, uint8_t opcode)
{
    int16_t offset;
    GB_advance_cycles(gb, 4);
    gb->registers[GB_REGISTER_AF] &= 0xFF00;
    offset = (int8_t) GB_read_memory(gb, gb->pc++);
    GB_advance_cycles(gb, 8);
    gb->registers[GB_REGISTER_HL] = gb->registers[GB_REGISTER_SP] + offset;

    if ((gb->registers[GB_REGISTER_SP] & 0xF) + (offset & 0xF) > 0xF) {
        gb->registers[GB_REGISTER_AF] |= GB_HALF_CARRY_FLAG;
    }

    if ((gb->registers[GB_REGISTER_SP] & 0xFF)  + (offset & 0xFF) > 0xFF) {
        gb->registers[GB_REGISTER_AF] |= GB_CARRY_FLAG;
    }
}

static void ld_sp_hl(GB_gameboy_t *gb, uint8_t opcode)
{
    GB_advance_cycles(gb, 8);
    gb->registers[GB_REGISTER_SP] = gb->registers[GB_REGISTER_HL];
}

static void rlc_r(GB_gameboy_t *gb, uint8_t opcode)
{
    bool carry;
    uint8_t value;
    GB_advance_cycles(gb, 4);
    value = get_src_value(gb, opcode);
    carry = (value & 0x80) != 0;
    gb->registers[GB_REGISTER_AF] &= 0xFF00;
    set_src_value(gb, opcode, (value << 1) | carry);
    if (carry) {
        gb->registers[GB_REGISTER_AF] |= GB_CARRY_FLAG;
    }
    if (!(value << 1)) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
}

static void rrc_r(GB_gameboy_t *gb, uint8_t opcode)
{
    bool carry;
    uint8_t value;
    GB_advance_cycles(gb, 4);
    value = get_src_value(gb, opcode);
    carry = (value & 0x01) != 0;
    gb->registers[GB_REGISTER_AF] &= 0xFF00;
    value = (value >> 1) | (carry << 7);
    set_src_value(gb, opcode, value);
    if (carry) {
        gb->registers[GB_REGISTER_AF] |= GB_CARRY_FLAG;
    }
    if (value == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
}

static void rl_r(GB_gameboy_t *gb, uint8_t opcode)
{
    bool carry;
    uint8_t value;
    bool bit7;
    GB_advance_cycles(gb, 4);
    value = get_src_value(gb, opcode);
    carry = (gb->registers[GB_REGISTER_AF] & GB_CARRY_FLAG) != 0;
    bit7 = (value & 0x80) != 0;

    gb->registers[GB_REGISTER_AF] &= 0xFF00;
    value = (value << 1) | carry;
    set_src_value(gb, opcode, value);
    if (bit7) {
        gb->registers[GB_REGISTER_AF] |= GB_CARRY_FLAG;
    }
    if (value == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
}

static void rr_r(GB_gameboy_t *gb, uint8_t opcode)
{
    bool carry;
    uint8_t value;
    bool bit1;

    GB_advance_cycles(gb, 4);
    value = get_src_value(gb, opcode);
    carry = (gb->registers[GB_REGISTER_AF] & GB_CARRY_FLAG) != 0;
    bit1 = (value & 0x1) != 0;

    gb->registers[GB_REGISTER_AF] &= 0xFF00;
    value = (value >> 1) | (carry << 7);
    set_src_value(gb, opcode, value);
    if (bit1) {
        gb->registers[GB_REGISTER_AF] |= GB_CARRY_FLAG;
    }
    if (value == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
}

static void sla_r(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t value;
    bool carry;
    GB_advance_cycles(gb, 4);
    value = get_src_value(gb, opcode);
    carry = (value & 0x80) != 0;
    gb->registers[GB_REGISTER_AF] &= 0xFF00;
    set_src_value(gb, opcode, (value << 1));
    if (carry) {
        gb->registers[GB_REGISTER_AF] |= GB_CARRY_FLAG;
    }
    if ((value & 0x7F) == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
}

static void sra_r(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t bit7;
    uint8_t value;
    GB_advance_cycles(gb, 4);
    value = get_src_value(gb, opcode);
    bit7 = value & 0x80;
    gb->registers[GB_REGISTER_AF] &= 0xFF00;
    if (value & 1) {
        gb->registers[GB_REGISTER_AF] |= GB_CARRY_FLAG;
    }
    value = (value >> 1) | bit7;
    set_src_value(gb, opcode, value);
    if (value == 0) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
}

static void srl_r(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t value;
    GB_advance_cycles(gb, 4);
    value = get_src_value(gb, opcode);
    gb->registers[GB_REGISTER_AF] &= 0xFF00;
    set_src_value(gb, opcode, (value >> 1));
    if (value & 1) {
        gb->registers[GB_REGISTER_AF] |= GB_CARRY_FLAG;
    }
    if (!(value >> 1)) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
}

static void swap_r(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t value;
    GB_advance_cycles(gb, 4);
    value = get_src_value(gb, opcode);
    gb->registers[GB_REGISTER_AF] &= 0xFF00;
    set_src_value(gb, opcode, (value >> 4) | (value << 4));
    if (!value) {
        gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
    }
}

static void bit_r(GB_gameboy_t *gb, uint8_t opcode)
{
    uint8_t value;
    uint8_t bit;
    GB_advance_cycles(gb, 4);
    value = get_src_value(gb, opcode);
    bit = 1 << ((opcode >> 3) & 7);
    if ((opcode & 0xC0) == 0x40) { /* Bit */
        gb->registers[GB_REGISTER_AF] &= 0xFF00 | GB_CARRY_FLAG;
        gb->registers[GB_REGISTER_AF] |= GB_HALF_CARRY_FLAG;
        if (!(bit & value)) {
            gb->registers[GB_REGISTER_AF] |= GB_ZERO_FLAG;
        }
    }
    else if ((opcode & 0xC0) == 0x80) { /* res */
        set_src_value(gb, opcode, value & ~bit) ;
    }
    else if ((opcode & 0xC0) == 0xC0) { /* set */
        set_src_value(gb, opcode, value | bit) ;
    }
}

static void cb_prefix(GB_gameboy_t *gb, uint8_t opcode)
{
    GB_advance_cycles(gb, 4);
    opcode = GB_read_memory(gb, gb->pc++);
    switch (opcode >> 3) {
        case 0:
            rlc_r(gb, opcode);
            break;
        case 1:
            rrc_r(gb, opcode);
            break;
        case 2:
            rl_r(gb, opcode);
            break;
        case 3:
            rr_r(gb, opcode);
            break;
        case 4:
            sla_r(gb, opcode);
            break;
        case 5:
            sra_r(gb, opcode);
            break;
        case 6:
            swap_r(gb, opcode);
            break;
        case 7:
            srl_r(gb, opcode);
            break;
        default:
            bit_r(gb, opcode);
            break;
    }
}

static GB_opcode_t *opcodes[256] = {
    /*  X0          X1          X2          X3          X4          X5          X6          X7                */
    /*  X8          X9          Xa          Xb          Xc          Xd          Xe          Xf                */
    nop,        ld_rr_d16,  ld_drr_a,   inc_rr,     inc_hr,     dec_hr,     ld_hr_d8,   rlca,       /* 0X */
    ld_da16_sp, add_hl_rr,  ld_a_drr,   dec_rr,     inc_lr,     dec_lr,     ld_lr_d8,   rrca,
    stop,       ld_rr_d16,  ld_drr_a,   inc_rr,     inc_hr,     dec_hr,     ld_hr_d8,   rla,        /* 1X */
    jr_r8,      add_hl_rr,  ld_a_drr,   dec_rr,     inc_lr,     dec_lr,     ld_lr_d8,   rra,
    jr_cc_r8,   ld_rr_d16,  ld_dhli_a,  inc_rr,     inc_hr,     dec_hr,     ld_hr_d8,   daa,        /* 2X */
    jr_cc_r8,   add_hl_rr,  ld_a_dhli,  dec_rr,     inc_lr,     dec_lr,     ld_lr_d8,   cpl,
    jr_cc_r8,   ld_rr_d16,  ld_dhld_a,  inc_rr,     inc_dhl,    dec_dhl,    ld_dhl_d8,  scf,        /* 3X */
    jr_cc_r8,   add_hl_rr,  ld_a_dhld,  dec_rr,     inc_hr,     dec_hr,     ld_hr_d8,   ccf,
    nop,        ld_b_c,     ld_b_d,     ld_b_e,     ld_b_h,     ld_b_l,     ld_b_dhl,   ld_b_a,     /* 4X */
    ld_c_b,     nop,        ld_c_d,     ld_c_e,     ld_c_h,     ld_c_l,     ld_c_dhl,   ld_c_a,
    ld_d_b,     ld_d_c,     nop,        ld_d_e,     ld_d_h,     ld_d_l,     ld_d_dhl,   ld_d_a,     /* 5X */
    ld_e_b,     ld_e_c,     ld_e_d,     nop,        ld_e_h,     ld_e_l,     ld_e_dhl,   ld_e_a,
    ld_h_b,     ld_h_c,     ld_h_d,     ld_h_e,     nop,        ld_h_l,     ld_h_dhl,   ld_h_a,     /* 6X */
    ld_l_b,     ld_l_c,     ld_l_d,     ld_l_e,     ld_l_h,     nop,        ld_l_dhl,   ld_l_a,
    ld_dhl_b,   ld_dhl_c,   ld_dhl_d,   ld_dhl_e,   ld_dhl_h,   ld_dhl_l,   halt,       ld_dhl_a,   /* 7X */
    ld_a_b,     ld_a_c,     ld_a_d,     ld_a_e,     ld_a_h,     ld_a_l,     ld_a_dhl,   nop,
    add_a_r,    add_a_r,    add_a_r,    add_a_r,    add_a_r,    add_a_r,    add_a_r,    add_a_r,    /* 8X */
    adc_a_r,    adc_a_r,    adc_a_r,    adc_a_r,    adc_a_r,    adc_a_r,    adc_a_r,    adc_a_r,
    sub_a_r,    sub_a_r,    sub_a_r,    sub_a_r,    sub_a_r,    sub_a_r,    sub_a_r,    sub_a_r,    /* 9X */
    sbc_a_r,    sbc_a_r,    sbc_a_r,    sbc_a_r,    sbc_a_r,    sbc_a_r,    sbc_a_r,    sbc_a_r,
    and_a_r,    and_a_r,    and_a_r,    and_a_r,    and_a_r,    and_a_r,    and_a_r,    and_a_r,    /* aX */
    xor_a_r,    xor_a_r,    xor_a_r,    xor_a_r,    xor_a_r,    xor_a_r,    xor_a_r,    xor_a_r,
    or_a_r,     or_a_r,     or_a_r,     or_a_r,     or_a_r,     or_a_r,     or_a_r,     or_a_r,     /* bX */
    cp_a_r,     cp_a_r,     cp_a_r,     cp_a_r,     cp_a_r,     cp_a_r,     cp_a_r,     cp_a_r,
    ret_cc,     pop_rr,     jp_cc_a16,  jp_a16,     call_cc_a16,push_rr,    add_a_d8,   rst,        /* cX */
    ret_cc,     ret,        jp_cc_a16,  cb_prefix,  call_cc_a16,call_a16,   adc_a_d8,   rst,
    ret_cc,     pop_rr,     jp_cc_a16,  ill,        call_cc_a16,push_rr,    sub_a_d8,   rst,        /* dX */
    ret_cc,     reti,       jp_cc_a16,  ill,        call_cc_a16,ill,        sbc_a_d8,   rst,
    ld_da8_a,   pop_rr,     ld_dc_a,    ill,        ill,        push_rr,    and_a_d8,   rst,        /* eX */
    add_sp_r8,  jp_hl,      ld_da16_a,  ill,        ill,        ill,        xor_a_d8,   rst,
    ld_a_da8,   pop_rr,     ld_a_dc,    di,         ill,        push_rr,    or_a_d8,    rst,        /* fX */
    ld_hl_sp_r8,ld_sp_hl,   ld_a_da16,  ei,         ill,        ill,        cp_a_d8,    rst,
};
void GB_cpu_run(GB_gameboy_t *gb)
{
    gb->vblank_just_occured = false;
    bool interrupt = gb->interrupt_enable & gb->io_registers[GB_IO_IF] & 0x1F;

    if (interrupt) {
        gb->halted = false;
    }

    if (gb->hdma_on) {
        GB_advance_cycles(gb, 4);
        return;
    }

    bool effecitve_ime = gb->ime;
    if (gb->ime_toggle) {
        gb->ime = !gb->ime;
        gb->ime_toggle = false;
    }

    if (effecitve_ime && interrupt) {
        uint8_t interrupt_bit = 0;
        uint8_t interrupt_queue = gb->interrupt_enable & gb->io_registers[GB_IO_IF] & 0x1F;
        while (!(interrupt_queue & 1)) {
            interrupt_queue >>= 1;
            interrupt_bit++;
        }
        gb->io_registers[GB_IO_IF] &= ~(1 << interrupt_bit);
        gb->ime = false;
        nop(gb, 0);
        /* Run pseudo instructions rst 40-60*/
        rst(gb, 0x87 + interrupt_bit * 8);
    }
    else if(!gb->halted && !gb->stopped) {
        uint8_t opcode = GB_read_memory(gb, gb->pc++);
        if (gb->halt_bug) {
            gb->pc--;
            gb->halt_bug = false;
        }
        opcodes[opcode](gb, opcode);
    }
    else {
        GB_advance_cycles(gb, 4);
    }
}
