/*

    This file is part of Emu-Pizza

    Emu-Pizza is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Emu-Pizza is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Emu-Pizza.  If not, see <http://www.gnu.org/licenses/>.

*/

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <strings.h>
#include <unistd.h>
#include "mmu.h"
#include "z80_gameboy_regs.h"

/* main struct describing CPU state */

typedef struct z80_state_s
{
    uint8_t        spare;
    uint8_t        a;
#if __BYTE_ORDER__ == __ORDER_LITTLE_ENDIAN__
    uint8_t        c;
    uint8_t        b;
#else
    uint8_t        b;
    uint8_t        c;
#endif
#if __BYTE_ORDER__ == __ORDER_LITTLE_ENDIAN__
    uint8_t        e;
    uint8_t        d;
#else
    uint8_t        d;
    uint8_t        e;
#endif
#if __BYTE_ORDER__ == __ORDER_LITTLE_ENDIAN__
    uint8_t        l;
    uint8_t        h;
#else
    uint8_t        h;
    uint8_t        l;
#endif

    uint16_t       sp;
    uint16_t       pc;

    /* shortcuts */
    uint16_t       *bc;
    uint16_t       *de;
    uint16_t       *hl;
    uint8_t        *f;

    uint32_t       spare4;
    uint32_t       skip_cycle;

    z80_flags_t    flags;
    uint8_t        int_enable;

    /* latest T-state */
    uint16_t       spare3;

    /* total cycles */
    uint64_t       cycles;

} z80_state_t;

#define Z80_MAX_MEMORY 65536


/* state of the Z80 CPU */
z80_state_t   state;

/* precomputed flags masks */
uint8_t       zc[1 << 9];
uint8_t       z[1 << 9];

/* macro to access addresses passed as parameters */
#define ADDR  mmu_read_16(state.pc + 1)
#define NN    mmu_read_16(state.pc + 2)

/* dummy value for 0x06 regs resulution table */
uint8_t dummy;

/* Registers table */
uint8_t **regs_dst;    
uint8_t **regs_src;    

#define FLAG_MASK_Z  (1 << FLAG_OFFSET_Z)
#define FLAG_MASK_AC (1 << FLAG_OFFSET_AC)
#define FLAG_MASK_N  (1 << FLAG_OFFSET_N)
#define FLAG_MASK_CY (1 << FLAG_OFFSET_CY)


/********************************/
/*                              */
/*        FLAGS OPS             */
/*                              */
/********************************/

/* calc flags SZ with 16 bit param */
void static inline z80_set_flags_z(unsigned int v)
{
    state.flags.z  = (v & 0xff) == 0;
}

/* calc flags SZC with 16 bit param */
void static inline z80_set_flags_zc(unsigned int v)
{
    state.flags.z = (v & 0xff) == 0;
    state.flags.cy = (v > 0xff);
}

/* calc flags SZC with 32 bit result */
void static inline z80_set_flags_zc_16(unsigned int v)
{
    state.flags.z  = (v & 0xffff) == 0;
    state.flags.cy = (v > 0xffff);
}

/* calc AC for given operands */
void static inline z80_set_flags_ac(uint8_t a, uint8_t b,
                                    unsigned int r)
{
    /* calc xor for AC and overflow */
    unsigned int c = (a ^ b ^ r);

    /* set AC */
    state.flags.ac = ((c & 0x10) != 0);

    return;
}

/* calc AC and overflow flag given operands (16 bit flavour) */
void static inline z80_set_flags_ac_16(unsigned int a, 
                                       unsigned int b, 
                                       unsigned int r)
{
    /* calc xor for AC and overflow */
    unsigned int c = (a ^ b ^ r);

    /* set AC */
    state.flags.ac = ((c & 0x01000) != 0);

    return;
}

/* calc AC flag given operands and result */
char static inline z80_calc_ac(uint8_t a, uint8_t b, unsigned int r)
{
    /* calc xor for AC and overflow */
    unsigned int c = a ^ b ^ r;

    /* AC */
    if (c & 0x10)
        return 1;

    return 0;
}

/* calculate flags mask array */
void static z80_calc_flags_mask_array()
{
    z80_flags_t f;
    unsigned int i;

 //   bzero(sz53pc, sizeof(sz53pc));

    /* create a mask for bit 5 and 3 and its reverse */
/*    f.spare = 0= 1; f.z = 1; f.u5 = 0; f.ac = 1; f.u3 = 0; f.p = 1; f.n = 1; f.cy = 1;
    u53_mask = *((uint8_t *) &f);
    r53_mask = ~(u53_mask); */

    bzero(&f, 1);
 
    for (i=0; i<512; i++)
    {
        f.z  = ((i & 0xff) == 0);
        f.cy = (i > 0xff);

        zc[i] = *((uint8_t *) &f);

        f.cy = 0;

        z[i] = *((uint8_t *) &f);

        /* no CY and parity */
/*        f.cy = 0; 
        f.p  = parity[i & 0xff]; 
        sz53p[i] = *((uint8_t *) &f);
*/
        /* similar but with no u3 and u5 */
/*        f.u3 = 0;
        f.u5 = 0;
        szp[i] = *((uint8_t *) &f);
*/
        /* similar but with carry and no p */
/*        f.cy = (i > 0xff);
        f.p = 0;
        szc[i] = *((uint8_t *) &f); */
    } 
}


/********************************/
/*                              */
/*    INSTRUCTIONS SEGMENT      */
/*       ordered by name        */
/*                              */
/********************************/


/* add A register, b parameter and Carry flag, then calculate flags */
void static inline z80_adc(uint8_t b)
{
    /* calc result */
    unsigned int result = state.a + b + state.flags.cy;

    /* set flags - SZ5H3V0C */
    *state.f = zc[result & 0x1ff];

    /* set AC and overflow flags */
    z80_set_flags_ac(state.a, b, result);

    /* save result into A register */
    state.a = (uint8_t) result;

    return; 
}

/* add a and b parameters (both 16 bits) and the carry, thencalculate flags */
unsigned int static inline z80_adc_16(unsigned int a, unsigned int b)
{
    /* calc result */
    unsigned int result = a + b + state.flags.cy;

    /* set them - SZ5H3V0C */
    z80_set_flags_zc_16(result);
    state.flags.n  = 0;

    /* get only high byte */
    // unsigned int r16 = (result >> 8);
     
    /* set AC and overflow flags */
    z80_set_flags_ac_16(a, b, result);

    return result; 
}

/* add A register and b parameter and calculate flags */
void static inline z80_add(uint8_t b)
{
    /* calc result */
    unsigned int result = state.a + b;

    /* set them - SZ5H3P0C */
    *state.f = zc[result & 0x1ff];

    /* set AC and overflow flags - given AC and V set to 0 */
    z80_set_flags_ac(state.a, b, result);

    /* save result into A register */
    state.a = result; 

    return; 
}

/* add a and b parameters (both 16 bits), then calculate flags */
unsigned int static inline z80_add_16(unsigned int a, unsigned int b)
{
    /* calc result */
    unsigned int result = a + b;

    /* get only high byte */
    // uint8_t r16 = (result >> 8);

    /* not a subtraction */
    state.flags.n = 0;

    /* calc xor for AC  */
    z80_set_flags_ac(a, b, result);

    /* set CY */
    state.flags.cy = (result > 0xffff);

    return result; 
}

/*  b AND A register and calculate flags */
void static inline z80_ana(uint8_t b)
{
    /* calc result */
    uint8_t result = state.a & b;

    /* set them */
    *state.f = zc[result] | FLAG_MASK_AC;

    /* save result into A register */
    state.a = result;

    return;
}

/* BIT instruction, test pos-th bit and set flags */
void static inline z80_bit(uint8_t *v, uint8_t pos, uint8_t muffa)
{
    uint8_t r = *v & (0x01 << pos);

    /* set flags AC,Z, N = 0 */
    state.flags.n = 0;
    state.flags.ac = 1;
    state.flags.z = (r == 0) ;

    return;
}

/* push the current PC on the stack and move PC to the function addr */
int static inline z80_call(unsigned int addr)
{
    /* move to the next instruction */
    state.pc += 3;

    /* add 4 more cycles */
    cycles_step();

    /* save it into stack */
    mmu_write_16(state.sp - 2, state.pc);

    /* update stack pointer */
    state.sp -= 2;

    /* move PC to the called function address */
    state.pc = addr;

    return 0;
}

/* compare b parameter against A register and calculate flags */
void static inline z80_cmp(uint8_t b)
{
    /* calc result */ 
    unsigned int result = state.a - b;

    /* set flags - SZ5H3PN* */
    *state.f = zc[result & 0x1ff] |
               FLAG_MASK_N;

    /* set AC and overflow flags */
    z80_set_flags_ac(state.a, b, result);

    return;
}

/* compare b parameter against A register and calculate flags */
void static inline z80_cpid(uint8_t b, int8_t add)
{
    /* calc result */
    unsigned int result = state.a - b;

    /* calc AC */
    state.flags.ac = z80_calc_ac(state.a, b, result); 

    /* increase (add = +1) or decrease (add = -1) HL */
    *state.hl += add;

    /* decrease BC */
    *state.bc = *state.bc - 1;

    /* calc n as result - half carry flag */
    // unsigned int n = result - state.flags.ac;

    /* set flags - SZ5H3P1* */
    state.flags.z = (result & 0xff) == 0;

//    z80_set_flags_z(result);

    /* cmp = subtraction */
    state.flags.n  = 1;

    /* set P if BC != 0 */
 //   state.flags.p = (*state.bc != 0);

    /* flag 3 and 5 are taken from (result - ac) and not the result */
    /* and u5 is taken exceptionally from the bit 1                 */
//    state.flags.u5 = (n & 0x0002) != 0;
//    state.flags.u3 = (n & 0x0008) != 0;

    return;
}

/* DAA instruction... what else? */
void static inline z80_daa()
{
    unsigned int a = state.a;
    uint8_t al = state.a & 0x0f;

    if (state.flags.n)
    {
        if (state.flags.ac)
            a = (a - 6) & 0xFF;

        if (state.flags.cy)
            a -= 0x60; 
    }
    else
    {
        if (al > 9 || state.flags.ac)
        {
            state.flags.ac = (al > 9);
            a += 6;
        }

        if (state.flags.cy || ((a & 0x1f0) > 0x90))
            a += 0x60;
    }

    if (a & 0x0100) state.flags.cy = 1;

    /* set computer A value */
    state.a = a & 0xff;

    state.flags.ac = 0;
    state.flags.z = (state.a == 0);

    return;
}

/* DAA instruction... what else? */
void static inline z80_daa_ignore_n()
{
    unsigned int a = state.a;
    uint8_t al = state.a & 0x0f;

    if (al > 9 || state.flags.ac)
    {
        state.flags.ac = (al > 9);
        a += 6;
    }

    if (state.flags.cy || ((a & 0x1f0) > 0x90))
        a += 0x60;

    if (a & 0x0100) state.flags.cy = 1;

    /* set computer A value */
    state.a = a & 0xff;

    /* reset H flag */
    state.flags.z = (state.a == 0x00);
    state.flags.ac = 0;

    /* and its flags */
    // z80_set_flags_sz53p(state.a);

    return;
}

/* add a and b parameters (both 16 bits) and the carry, thencalculate flags */
unsigned int static inline dad_16(unsigned int a, unsigned int b)
{
    /* calc result */
    unsigned int result = a + b;

    /* reset n */
    state.flags.n = 0;

    /* calc xor for AC and overflow */
    unsigned int c = a ^ b ^ result;

    /* set AC */
    state.flags.ac = ((c & 0x1000) != 0);

    /* set CY */
    state.flags.cy = (result > 0xffff);

    return result; 
}

/* dec the operand and return result increased by one */
uint8_t static inline z80_dcr(uint8_t b)
{
    unsigned int result = b - 1;

    /* set flags - SZ5H3V1* */ 
    z80_set_flags_z(result);

    /* it's a subtraction */
    state.flags.n = 1;

    /* set overflow and AC */
    z80_set_flags_ac(b, 1, result);

//    state.flags.ac = 0;

    return result; 
}

/* inc the operand and return result increased by one */
uint8_t static inline z80_inr(uint8_t b)
{
    unsigned int result = b + 1;
  
    /* set flags - SZ5H3V1* */
    z80_set_flags_z(result);

    /* it's not a subtraction */
    state.flags.n = 0;

    /* set overflow and AC */
    z80_set_flags_ac(1, b, result);

    return result; 
}

/* same as call, but save on the stack the current PC instead of next instr */
int static inline z80_intr(unsigned int addr)
{
    /* push the current PC into stack */
    mmu_write_16(state.sp - 2, state.pc);

    cycles_step();

    /* update stack pointer */
    state.sp -= 2;

    /* move PC to the called function address */
    state.pc = addr;

    return 0;
}

/* copy (HL) in (DE) and decrease HL, DE and BC */
void static inline z80_ldd()
{
    uint8_t byte;

    /* copy! */
    mmu_move(*state.de, *state.hl);

    /* get last moved byte and sum A */
    byte = mmu_read(*state.de);
    byte += state.a;

    /* decrease HL, DE and BC */
    *state.hl = *state.hl - 1;
    *state.de = *state.de - 1;
    *state.bc = *state.bc - 1;

    /* reset flags - preserve ZC  */
    *state.f &= FLAG_MASK_Z | 
                FLAG_MASK_CY;
                
    return;
}

/* copy (HL) in (DE) and increase HL and DE. BC is decreased */
void static inline z80_ldi()
{
    uint8_t byte;

    /* copy! */
    mmu_move(*state.de, *state.hl);

    /* get last moved byte and sum A */
    byte = mmu_read(*state.de);
    byte += state.a;

    /* u5 flag is bit 1 of last moved byte + A (WTF?) */
    // state.flags.u5 = (byte & 0x02) >> 1;
    
    /* u3 flag is bit 3 of last moved byte + A (WTF?) */
    // state.flags.u3 = (byte & 0x08) >> 3;
    
    /* decrease HL, DE and BC */
    *state.hl = *state.hl + 1;
    *state.de = *state.de + 1;
    *state.bc = *state.bc - 1;

    /* reset negative, half carry and parity flags */
    state.flags.n = 0;
    state.flags.ac = 0;
    // state.flags.p = (*state.bc != 0);

    return;
}

/* negate register A */
void static inline z80_neg()
{
    /* calc result */
    unsigned int result = 0 - state.a;

    /* set flags - SZ5H3V1C */
    *state.f = zc[result & 0x1ff] | FLAG_MASK_N;

    /* set AC and overflow */
    z80_set_flags_ac(0, state.a, result);

    /* save result into A register */
    state.a = (uint8_t) result;

    return;
}

/* OR b parameter and A register and calculate flags */
void static inline z80_ora(uint8_t b)
{
    state.a |= b;

    /* set them SZ503P0C */
    *state.f = zc[state.a]; 

    return;
}

/* RES instruction, put a 0 on pos-th bit and set flags */
uint8_t static inline z80_res(uint8_t *v, uint8_t pos)
{
    *v &= ~(0x01 << pos);

    return *v;
}

/* pop the return address from the stack and move PC to that address */
int static inline z80_ret()
{
    state.pc = mmu_read_16(state.sp); 
    state.sp += 2;

    /* add 4 cycles */
    cycles_step();

    return 0;
}

/* RL (Rotate Left) instruction */
uint8_t static inline z80_rl(uint8_t *v, char with_carry)
{
    uint8_t carry;

    /* apply RLC to the memory pointed byte */
    carry = (*v & 0x80) >> 7;
    *v = *v << 1;

    if (with_carry)
        *v |= carry;
    else
        *v |= state.flags.cy;

    /* set flags - SZ503P0C */
    *state.f = 0; // sz53p[*v]; 

    state.flags.z = (*v == 0);
    state.flags.cy = carry;

    return *v;
}

/* RLA instruction */
uint8_t static inline z80_rla(uint8_t *v, char with_carry)
{
    uint8_t carry;

    /* apply RLA to the memory pointed byte */
    carry = (*v & 0x80) >> 7;
    *v = *v << 1;

    if (with_carry)
        *v |= carry;
    else
        *v |= state.flags.cy;

    /* reset flags */
    *state.f = 0;

    /* just set carry */
    state.flags.cy = carry;

    return *v;
}

/* RLD instruction */
void static inline z80_rld()
{
    uint8_t hl = mmu_read(*state.hl);

    /* save lowest A 4 bits */
    uint8_t al = state.a & 0x0f;

    /* A lowest bits are overwritten by (HL) highest ones */
    state.a &= 0xf0;
    state.a |= (hl >> 4);

    /* (HL) highest bits are overwritten by (HL) lowest ones */
    hl <<= 4;

    /* finally, (HL) lowest bits are overwritten by A lowest */
    hl &= 0xf0;
    hl |= al;

    /* set (HL) with his new value ((HL) low | A low) */
    mmu_write(*state.hl, hl);

    /* reset flags - preserve CY */
    *state.f &= FLAG_MASK_CY;

    /* set flags - SZ503P0* */
    *state.f |= z[state.a];

    return;
}

/* RR instruction */
uint8_t static inline z80_rr(uint8_t *v, char with_carry)
{
    uint8_t carry;

    /* apply RRC to the memory pointed byte */
    carry = *v & 0x01;
    *v = (*v >> 1);

    /* 7th bit taken from old bit 0 or from CY */
    if (with_carry)
        *v |= (carry << 7);
    else
        *v |= (state.flags.cy << 7);

    /* set flags - SZ503P0C */
    *state.f = z[*v];

    state.flags.cy = carry;

    return *v;
}

/* RRA instruction */
uint8_t static inline z80_rra(uint8_t *v, char with_carry)
{
    uint8_t carry;

    /* apply RRC to the memory pointed byte */
    carry = *v & 0x01;
    *v = (*v >> 1); 

    /* 7th bit taken from old bit 0 or from CY */
    if (with_carry)
        *v |= (carry << 7);
    else
        *v |= (state.flags.cy << 7);

    /* reset flags */
    *state.f = 0; 

    state.flags.cy = carry;

//    state.flags.n = 0;
//    state.flags.ac = 0;

    /* copy bit 3 and 5 of the result */
    // state.flags.u3 = ((*v & 0x08) != 0);
    // state.flags.u5 = ((*v & 0x20) != 0);

    return *v;
}

/* RRD instruction */
void static inline z80_rrd()
{
    uint8_t hl = mmu_read(*state.hl);

    /* save lowest (HL) 4 bits */
    uint8_t hll = hl & 0x0f;

    /* (HL) lowest bits are overwritten by (HL) highest ones */
    hl >>= 4;

    /* (HL) highest bits are overwritten by A lowest ones */
    hl |= ((state.a & 0x0f) << 4);

    /* set (HL) with his new value (A low | (HL) high) */
    mmu_write(*state.hl, hl);
 
    /* finally, A lowest bits are overwritten by (HL) lowest */
    state.a &= 0xf0;
    state.a |= hll;   

    /* reset flags - preserve CY */
    *state.f &= FLAG_MASK_CY;

    /* set flags - SZ503P0* */
    *state.f |= z[state.a];

    return;
}

/* subtract b parameter and Carry from A register and calculate flags */
void static inline z80_sbc(uint8_t b)
{
    /* calc result */
    unsigned int result = state.a - b - state.flags.cy;

    /* set flags - ZC and N = 1 */
    *state.f = zc[result & 0x1ff] | FLAG_MASK_N;

    /* set AC */
    z80_set_flags_ac(state.a, b, result);

    /* save result into A register */
    state.a = (uint8_t) result;

    return;
}

/* subtract a and b parameters (both 16 bits) and the carry, then calculate flags */
unsigned int static inline z80_sbc_16(unsigned int a, unsigned int b)
{
    /* calc result */
    unsigned int result = a - b - state.flags.cy;

    /* set flags - SZ5H3V1C */
    z80_set_flags_zc_16(result);
    state.flags.n  = 1;

    /* get only high byte */
    // unsigned int r16 = (result >> 8);

    /* set AC and overflow flags */
    z80_set_flags_ac_16(a, b, result);

    return result; 
}   

/* SET instruction, put a 1 on pos-th bit and set flags */
uint8_t static inline z80_set(uint8_t *v, uint8_t pos)
{
    *v |= (0x01 << pos);

    return *v;
}

/* SL instruction (SLA = v * 2, SLL = v * 2 + 1) */
uint8_t static inline z80_sl(uint8_t *v, char one_insertion)
{
    /* move pointed value to local (gives an huge boost in perf!) */
    uint8_t l = *v;

    /* apply SL to the memory pointed byte */
    uint8_t cy = (l & 0x80) != 0;
    l = (l << 1) | one_insertion; 

    /* set flags - SZ503P0C */
    *state.f = z[l];

    state.flags.cy = cy;

    /* re-assign local value */
    *v = l;

    return l;
}

/* SR instruction (SRA = preserve 8th bit, SRL = discard 8th bit) */
uint8_t static inline z80_sr(uint8_t *v, char preserve)
{
    uint8_t bit = 0;

    /* save the bit 0 */
    uint8_t cy = (*v & 0x01);

    /* apply SL to the memory pointed byte */
    if (preserve)
        bit = *v & 0x80;

    /* move 1 pos right and restore highest bit (in case of SRA) */
    *v = (*v >> 1) | bit;

    /* set flags - SZ503P0C */
    *state.f = z[*v]; 

    state.flags.cy = cy;

    return *v;
}

/* subtract b parameter from A register and calculate flags */
void static inline z80_sub(uint8_t b)
{
    /* calc result */
    unsigned int result = state.a - b;

    /* set them - SZ5H3V1C */ 
    *state.f = zc[result & 0x1ff] | FLAG_MASK_N;

    /* set AC and overflow flags */
    z80_set_flags_ac(state.a, b, result);

    /* save result into A register */
    state.a = (uint8_t) result;

    return;
}

/* xor b parameter and A register and calculate flags */
void static inline z80_xra(uint8_t b)
{
    /* calc result */
    state.a ^= b;

    /* set them SZ503P00 */
    *state.f = z[state.a];

    return;
}



/********************************/
/*                              */
/*    INSTRUCTIONS BRANCHES     */
/*                              */
/********************************/


/* Z80 extended OPs */
int static inline z80_ext_cb_execute()
{
    uint8_t byte = 1;
    int     b = 2;

    /* CB family (ROT, BIT, RES, SET)    */
    uint8_t cbfam;

    /* CB operation     */
    uint8_t cbop;

    /* choosen register   */
    uint8_t reg;

    /* get CB code */
    uint8_t code = mmu_read(state.pc + 1);

    /* extract family */
    cbfam = code >> 6;

    /* extract involved register */
    reg = code & 0x07;

    /* if reg == 0x06, refresh the pointer */
    // if (reg == 0x06 && code != 0x36)
   //  {
        /* add 4 more cycles for reading data from memory */
        // cycles_step();

       //  regs_src[0x06] = mmu_addr(*state.hl); 
    // }

    switch (cbfam)
    {
        /* Rotate Family */
        case 0x00: cbop = code & 0xf8;

                   switch(cbop)
                   {
                       /* RLC REG */
                       case 0x00: if (reg == 0x06)
                                  {
                                      byte = mmu_read(*state.hl);
                                      mmu_write(*state.hl, z80_rl(&byte, 1));
                                  }
                                  else
                                      z80_rl(regs_src[reg], 1);
                                  break;

                       /* RRC REG */
                       case 0x08: if (reg == 0x06)
                                  {
                                      byte = mmu_read(*state.hl);
                                      mmu_write(*state.hl, z80_rr(&byte, 1));
                                  }
                                  else
                                      z80_rr(regs_src[reg], 1);

                                  break;

                       /* RL  REG */
                       case 0x10: if (reg == 0x06)
                                  {
                                      byte = mmu_read(*state.hl);
                                      mmu_write(*state.hl, z80_rl(&byte, 0));
                                  }
                                  else
                                      z80_rl(regs_src[reg], 0);

                                  break;

                       /* RR  REG */
                       case 0x18: if (reg == 0x06)
                                  {
                                      byte = mmu_read(*state.hl);
                                      mmu_write(*state.hl, z80_rr(&byte, 0));
                                  }
                                  else
                                      z80_rr(regs_src[reg], 0);

                                  break;

                       /* SLA REG */
                       case 0x20: if (reg == 0x06)
                                  {
                                      byte = mmu_read(*state.hl);
                                      mmu_write(*state.hl, z80_sl(&byte, 0));
                                  }
                                  else
                                      z80_sl(regs_src[reg], 0);

                                  break;

                       /* SRA REG */
                       case 0x28: if (reg == 0x06)
                                  {
                                      byte = mmu_read(*state.hl);
                                      mmu_write(*state.hl, z80_sr(&byte, 1));
                                  }
                                  else
                                      z80_sr(regs_src[reg], 1);

                                  break;

                       /* SWAP */
                       case 0x30: 
                           switch (code & 0x37)
                           {
                               /* SWAP B */ 
                               case 0x30: byte = state.b;
                                          state.b = ((byte & 0xf0) >> 4) | 
                                                    ((byte & 0x0f) << 4);
                                          break;

                               /* SWAP C */ 
                               case 0x31: byte = state.c;
                                          state.c = ((byte & 0xf0) >> 4) | 
                                                    ((byte & 0x0f) << 4);
                                          break;

                               /* SWAP D */ 
                               case 0x32: byte = state.d;
                                          state.d = ((byte & 0xf0) >> 4) | 
                                                    ((byte & 0x0f) << 4);
                                          break;

                               /* SWAP E */ 
                               case 0x33: byte = state.e;
                                          state.e = ((byte & 0xf0) >> 4) | 
                                                    ((byte & 0x0f) << 4);
                                          break;

                               /* SWAP H */ 
                               case 0x34: byte = state.h;
                                          state.h = ((byte & 0xf0) >> 4) | 
                                                    ((byte & 0x0f) << 4);
                                          break;

                               /* SWAP L */ 
                               case 0x35: byte = state.l;
                                          state.l = ((byte & 0xf0) >> 4) | 
                                                    ((byte & 0x0f) << 4);
                                          break;

                               /* SWAP *HL */ 
                               case 0x36: byte = mmu_read(*state.hl);
                                          mmu_write(*state.hl, 
                                                    ((byte & 0xf0) >> 4) | 
                                                    ((byte & 0x0f) << 4));
                            
                                          break;

                               /* SWAP A */ 
                               case 0x37: 
                                          byte = state.a;
                                          state.a = ((byte & 0xf0) >> 4) | 
                                                    ((byte & 0x0f) << 4);
                                          break;
   
                           }

                           /* swap functions set Z flags */
                           state.flags.z = (byte == 0x00);
                   
                           /* reset all the others */
                           state.flags.ac = 0;
                           state.flags.cy = 0;
                           state.flags.n  = 0;
  
                           break;

                       /* SRL REG */
                       case 0x38: if (reg == 0x06)
                                  {
                                      byte = mmu_read(*state.hl);
                                      mmu_write(*state.hl, z80_sr(&byte, 0));
                                  }
                                  else
                                      z80_sr(regs_src[reg], 0);

                                  break;
                   }

                   /* accessing HL needs more T-cycles */
                   //if (reg == 0x06 && code != 0x36)
                   //    cycles_step();

                   /* accessing HL needs more T-cycles */
//                   if (reg == 0x06)
//                       cycles_step();

                   break;

        /* BIT Family */
        case 0x01: if (reg == 0x06)
                   {
                       byte = mmu_read(*state.hl);
                       z80_bit(&byte, (code >> 3) & 0x07, 
                               (uint8_t) *state.hl);
                   }
                   else
                       z80_bit(regs_src[reg], (code >> 3) & 0x07, 
                               *regs_src[reg]);
                   break;

        /* RES Family */
        case 0x02: if (reg == 0x06)
                   {
                       byte = mmu_read(*state.hl);
                       mmu_write(*state.hl, z80_res(&byte, (code >> 3) & 0x07));
                   }
                   else
                       z80_res(regs_src[reg], (code >> 3) & 0x07);

                   break;

        /* SET Family */
        case 0x03: if (reg == 0x06)
                   {
                       byte = mmu_read(*state.hl);
                       mmu_write(*state.hl, z80_set(&byte, (code >> 3) & 0x07));
                   }
                   else
                       z80_set(regs_src[reg], (code >> 3) & 0x07);

                   break;

//        default: printf("Unimplemented CB family: %02x\n",
//                        cbfam);

    }

    return b;
}


/* really execute the OP. Could be ran by normal execution or *
 * because an interrupt occours                               */
int static inline z80_execute(unsigned char code)
{
    int          b = 1;
    uint8_t     *p;
    uint8_t      byte = 1;
    uint8_t      byte2 = 1;
    unsigned int result;
    uint_fast16_t     addr;

    switch (code)
    {
        /* NOP       */
        case 0x00: break;                           

        /* LXI  B    */
        case 0x01: *state.bc = ADDR;   
                   b = 3;
                   break;

        /* STAX B    */
        case 0x02: mmu_write(*state.bc, state.a); 
                   break;                          

        /* INX  B    */
        case 0x03: (*state.bc)++;                    
                   cycles_step();
                   break;        

        /* INR  B    */
        case 0x04: state.b = z80_inr(state.b);                   
                   break;   

        /* DCR  B    */
        case 0x05: state.b = z80_dcr(state.b);                     
                   break;

        /* MVI  B    */
        case 0x06: state.b = mmu_read(state.pc + 1);  
                   b = 2;
                   break;

        /* RLCA      */
        case 0x07: z80_rla(&state.a, 1);
                   break;

        /* LD (NN),SP */
        case 0x08: mmu_write_16(ADDR, state.sp);
                   b = 3;
                   break;

        /* DAD  B    */
        case 0x09: *state.hl = dad_16(*state.hl, *state.bc);    

                   /* needs 4 more cycles */
                   cycles_step();

                   break;

        /* LDAX B    */
        case 0x0A: state.a = mmu_read(*state.bc);          
                   break;

        /* DCX  B    */
        case 0x0B: (*state.bc)--;
                   cycles_step();
                   break;

        /* INR  C    */
        case 0x0C: state.c = z80_inr(state.c);                 
                   break;   

        /* DCR  C    */
        case 0x0D: state.c = z80_dcr(state.c);            
                   break;   

        /* MVI  C    */
        case 0x0E: state.c = mmu_read(state.pc + 1); 
                   b = 2;
                   break;

        /* RRC       */
        case 0x0F: z80_rra(&state.a, 1);       
                   break;

        /* STOP       */
        case 0x10: b = 2;
                   break;

        /* LXI  D    */
        case 0x11: *state.de = ADDR;   
                   b = 3;
                   break;

        /* STAX D    */
        case 0x12: mmu_write(*state.de, state.a);            
                   break;

        /* INX  D    */
        case 0x13: (*state.de)++;
                   cycles_step();
                   break;

        /* INR  D    */
        case 0x14: state.d = z80_inr(state.d);               
                   break;   

        /* DCR  D    */
        case 0x15: state.d = z80_dcr(state.d);              
                   break;   

        /* MVI  D    */
        case 0x16: state.d = mmu_read(state.pc + 1);  
                   b = 2;
                   break;

        /* RLA       */
        case 0x17: z80_rla(&state.a, 0);
                   break;

        /* JR        */
        case 0x18: cycles_step();
                   state.pc += (int8_t) mmu_read(state.pc + 1);
                   b = 2;
                   break; 

        /* DAD  D    */
        case 0x19: *state.hl = dad_16(*state.hl, *state.de);

                   /* needs 4 more cycles */
                   cycles_step();

                   break;

        /* LDAX D    */
        case 0x1A: state.a = mmu_read(*state.de);            
                   break;

        /* DCX  D    */
        case 0x1B: (*state.de)--;
                   cycles_step();
                   break;

        /* INR  E    */
        case 0x1C: state.e = z80_inr(state.e);                  
                   break;

        /* DCR  E    */
        case 0x1D: state.e = z80_dcr(state.e);                       
                   break;

        /* MVI  E    */
        case 0x1E: state.e = mmu_read(state.pc + 1);   
                   b = 2;
                   break;

        /* RRA       */
        case 0x1F: z80_rra(&state.a, 0);
                   break;

        /* JRNZ       */
        case 0x20: cycles_step();

                   if (!state.flags.z)
                       state.pc += (int8_t) mmu_read(state.pc + 1);

                   b = 2;
                   break;

        /* LXI  H    */
        case 0x21: *state.hl = ADDR; 
                   b = 3;
                   break;

        /* LDI (HL), A     */
        case 0x22: mmu_write(*state.hl, state.a);
                   (*state.hl)++;
                   break;

        /* INX  H    */
        case 0x23: (*state.hl)++;
                   cycles_step();
                   break;

        /* INR  H    */
        case 0x24: state.h = z80_inr(state.h);                      
                   break;

        /* DCR  H    */
        case 0x25: state.h = z80_dcr(state.h);                       
                   break;

        /* MVI  H    */
        case 0x26: state.h = mmu_read(state.pc + 1);   
                   b = 2;
                   break;

        /* DAA       */
        case 0x27: z80_daa();
                   break;                            

        /* JRZ       */
        case 0x28: cycles_step();
                   if (state.flags.z)
                       state.pc += (int8_t) mmu_read(state.pc + 1);

                   b = 2;
                   break;                           

        /* DAD  H    */
        case 0x29: *state.hl = dad_16(*state.hl, *state.hl);

                   /* needs 4 more cycles */
                   cycles_step();

                   break;

        /* LDI  A,(HL)     */ 
        case 0x2A: state.a = mmu_read(*state.hl);
                   (*state.hl)++;
                   break;

        /* DCX  H    */
        case 0x2B: (*state.hl)--;
                   cycles_step();
                   break;

        /* INR  L    */
        case 0x2C: state.l = z80_inr(state.l);                       
                   break;

        /* DCR  L    */
        case 0x2D: state.l = z80_dcr(state.l);                      
                   break;

        /* MVI  L    */
        case 0x2E: state.l = mmu_read(state.pc + 1);  
                   b = 2;
                   break;

        /* CMA  A    */
        case 0x2F: state.a = ~state.a;             
                   state.flags.ac = 1; 
                   state.flags.n  = 1; 
                   break;

        /* JRNC      */
        case 0x30: cycles_step(); 

                   if (!state.flags.cy)
                       state.pc += (int8_t) mmu_read(state.pc + 1);

                   b = 2;
                   break;                     
 
        /* LXI  SP   */
        case 0x31: state.sp = ADDR;
                   b = 3;
                   break;

        /* LDD (HL), A     */
        case 0x32: mmu_write(*state.hl, state.a);
                   (*state.hl)--;
                   break;

        /* INX  SP   */
        case 0x33: state.sp++;           
                   cycles_step();
                   break;

        /* INR  M    */
        case 0x34: mmu_write(*state.hl, z80_inr(mmu_read(*state.hl)));
                   break;

        /* DCR  M    */
        case 0x35: mmu_write(*state.hl, z80_dcr(mmu_read(*state.hl)));
                   break;

        /* MVI  M    */
        case 0x36: mmu_move(*state.hl, state.pc + 1);
                   b = 2;
                   break;

        /* STC       */
        case 0x37: state.flags.cy = 1;              
                   state.flags.ac = 0;
                   state.flags.n  = 0;
                   break;

        /* JRC       */
        case 0x38: cycles_step();
                   if (state.flags.cy)
                       state.pc += (int8_t) mmu_read(state.pc + 1);

                   b = 2;
                   break;                          

        /* DAD  SP   */
        case 0x39: *state.hl = dad_16(*state.hl, state.sp);

                   /* needs 4 more cycles */
                   cycles_step();

                   break;

        /* LDD  A,(HL)     */ 
        case 0x3A: state.a = mmu_read(*state.hl);
                   (*state.hl)--;
                   break;

        /* DCX  SP   */
        case 0x3B: state.sp--;                    
                   cycles_step();
                   break;

        /* INR  A    */
        case 0x3C: state.a = z80_inr(state.a);                      
                   break;

        /* DCR  A    */
        case 0x3D: state.a = z80_dcr(state.a);                
                   break;

        /* MVI  A   */
        case 0x3E: state.a = mmu_read(state.pc + 1);   
                   b = 2;
                   break;

        /* CCF      */
        case 0x3F: state.flags.ac = 0;
                   state.flags.cy = !state.flags.cy;
                   state.flags.n  = 0;

                   break;

        /* MOV  B,B  */
        case 0x40: state.b = state.b; 
                   break;  

        /* MOV  B,C  */
        case 0x41: state.b = state.c; 
                   break;  

        /* MOV  B,D  */
        case 0x42: state.b = state.d; 
                   break;  

        /* MOV  B,E  */
        case 0x43: state.b = state.e; 
                   break;  

        /* MOV  B,H  */
        case 0x44: state.b = state.h; 
                   break;  

        /* MOV  B,L  */
        case 0x45: state.b = state.l; 
                   break;  

        /* MOV  B,M  */
        case 0x46: state.b = mmu_read(*state.hl); 
                   break;  

        /* MOV  B,A  */
        case 0x47: state.b = state.a; 
                   break;  

        /* MOV  C,B  */
        case 0x48: state.c = state.b; 
                   break;  

        /* MOV  C,C  */
        case 0x49: state.c = state.c; 
                   break;  

        /* MOV  C,D  */
        case 0x4A: state.c = state.d; 
                   break;  

        /* MOV  C,E  */
        case 0x4B: state.c = state.e; 
                   break;  

        /* MOV  C,H  */
        case 0x4C: state.c = state.h; 
                   break;  

        /* MOV  C,L  */
        case 0x4D: state.c = state.l; 
                   break;  

        /* MOV  C,M  */
        case 0x4E: state.c = mmu_read(*state.hl); 
                   break;  

        /* MOV  C,A  */
        case 0x4F: state.c = state.a; 
                   break;  

        /* MOV  D,B  */
        case 0x50: state.d = state.b; 
                   break;  

        /* MOV  D,C  */
        case 0x51: state.d = state.c; 
                   break;  

        /* MOV  D,D  */
        case 0x52: state.d = state.d; 
                   break;  

        /* MOV  D,E  */
        case 0x53: state.d = state.e; 
                   break;  

        /* MOV  D,H  */
        case 0x54: state.d = state.h; 
                   break;  

        /* MOV  D,L  */
        case 0x55: state.d = state.l; 
                   break;  

        /* MOV  D,M  */
        case 0x56: state.d = mmu_read(*state.hl); 
                   break;  

        /* MOV  D,A  */
        case 0x57: state.d = state.a; 
                   break;  

        /* MOV  E,B  */
        case 0x58: state.e = state.b; 
                   break;  

        /* MOV  E,C  */
        case 0x59: state.e = state.c; 
                   break;  

        /* MOV  E,D  */
        case 0x5A: state.e = state.d; 
                   break;  

        /* MOV  E,E  */
        case 0x5B: state.e = state.e; 
                   break;  

        /* MOV  E,H  */
        case 0x5C: state.e = state.h; 
                   break;  

        /* MOV  E,L  */
        case 0x5D: state.e = state.l; 
                   break;  

        /* MOV  E,M  */
        case 0x5E: state.e = mmu_read(*state.hl); 
                   break;  

        /* MOV  E,A  */
        case 0x5F: state.e = state.a; 
                   break;  

        /* MOV  H,B  */
        case 0x60: state.h = state.b; 
                   break;  

        /* MOV  H,C  */
        case 0x61: state.h = state.c; 
                   break;  

        /* MOV  H,D  */
        case 0x62: state.h = state.d; 
                   break;  

        /* MOV  H,E  */
        case 0x63: state.h = state.e; 
                   break;  

        /* MOV  H,H  */
        case 0x64: state.h = state.h; 
                   break;  

        /* MOV  H,L  */
        case 0x65: state.h = state.l; 
                   break;  

        /* MOV  H,M  */
        case 0x66: state.h = mmu_read(*state.hl); 
                   break;  

        /* MOV  H,A  */
        case 0x67: state.h = state.a; 
                   break;  

        /* MOV  L,B  */
        case 0x68: state.l = state.b; 
                   break;  

        /* MOV  L,C  */
        case 0x69: state.l = state.c; 
                   break;  

        /* MOV  L,D  */
        case 0x6A: state.l = state.d; 
                   break;  

        /* MOV  L,E  */
        case 0x6B: state.l = state.e; 
                   break;  

        /* MOV  L,H  */
        case 0x6C: state.l = state.h; 
                   break;  

        /* MOV  L,L  */
        case 0x6D: state.l = state.l; 
                   break;  

        /* MOV  L,M  */
        case 0x6E: state.l = mmu_read(*state.hl); 
                   break;  

        /* MOV  L,A  */
        case 0x6F: state.l = state.a; 
                   break;  

        /* MOV  M,B  */
        case 0x70: mmu_write(*state.hl, state.b);
                   break;

        /* MOV  M,C  */
        case 0x71: mmu_write(*state.hl, state.c);
                   break;

        /* MOV  M,D  */
        case 0x72: mmu_write(*state.hl, state.d);
                   break;

        /* MOV  M,E  */
        case 0x73: mmu_write(*state.hl, state.e);
                   break;

        /* MOV  M,H  */
        case 0x74: mmu_write(*state.hl, state.h);
                   break;

        /* MOV  M,L  */
        case 0x75: mmu_write(*state.hl, state.l);
                   break;

        /* HLT       */
        case 0x76: return 1;

        /* MOV  M,A  */
        case 0x77: mmu_write(*state.hl, state.a);
                   break;

        /* MOV  A,B  */
        case 0x78: state.a = state.b; 
                   break;  

        /* MOV  A,C  */
        case 0x79: state.a = state.c; 
                   break;  

        /* MOV  A,D  */
        case 0x7A: state.a = state.d; 
                   break;  

        /* MOV  A,E  */
        case 0x7B: state.a = state.e; 
                   break;  

        /* MOV  A,H  */
        case 0x7C: state.a = state.h; 
                   break;  

        /* MOV  A,L  */
        case 0x7D: state.a = state.l; 
                   break;  

        /* MOV  A,M  */
        case 0x7E: state.a = mmu_read(*state.hl); 
                   break;  

        /* MOV  A,A  */
        case 0x7F: state.a = state.a; 
                   break;  

        /* ADD  B    */
        case 0x80: z80_add(state.b);
                   break;   

        /* ADD  C    */
        case 0x81: z80_add(state.c);
                   break;   

        /* ADD  D    */
        case 0x82: z80_add(state.d);
                   break;   

        /* ADD  E    */
        case 0x83: z80_add(state.e);
                   break;   

        /* ADD  H    */
        case 0x84: z80_add(state.h);
                   break;   

        /* ADD  L    */
        case 0x85: z80_add(state.l);
                   break;   

        /* ADD  M    */
        case 0x86: z80_add(mmu_read(*state.hl)); 
                   break;   

        /* ADD  A    */
        case 0x87: z80_add(state.a);
                   break;   

        /* ADC  B    */
        case 0x88: z80_adc(state.b); 
                   break;   

        /* ADC  C    */
        case 0x89: z80_adc(state.c);
                   break;   

        /* ADC  D    */
        case 0x8A: z80_adc(state.d);
                   break;   

        /* ADC  E    */
        case 0x8B: z80_adc(state.e);
                   break;   

        /* ADC  H    */
        case 0x8C: z80_adc(state.h); 
                   break;   

        /* ADC  L    */
        case 0x8D: z80_adc(state.l);
                   break;   

        /* ADC  M    */
        case 0x8E: z80_adc(mmu_read(*state.hl));
                   break;   

        /* ADC  A    */
        case 0x8F: z80_adc(state.a);
                   break;   

        /* SUB  B    */
        case 0x90: z80_sub(state.b);
                   break;   

        /* SUB  C    */
        case 0x91: z80_sub(state.c);
                   break;   

        /* SUB  D    */
        case 0x92: z80_sub(state.d);
                   break;   

        /* SUB  E    */
        case 0x93: z80_sub(state.e);
                   break;   

        /* SUB  H    */
        case 0x94: z80_sub(state.h);
                   break;   

        /* SUB  L    */
        case 0x95: z80_sub(state.l);
                   break;   

        /* SUB  M    */
        case 0x96: z80_sub(mmu_read(*state.hl));
                   break;   

        /* SUB  A    */
        case 0x97: z80_sub(state.a);
                   break;   

        /* SBC  B    */
        case 0x98: z80_sbc(state.b);
                   break;   

        /* SBC  C    */
        case 0x99: z80_sbc(state.c);
                   break;   

        /* SBC  D    */
        case 0x9a: z80_sbc(state.d);
                   break;   

        /* SBC  E    */
        case 0x9b: z80_sbc(state.e);
                   break;   

        /* SBC  H    */
        case 0x9c: z80_sbc(state.h);
                   break;   

        /* SBC  L    */
        case 0x9d: z80_sbc(state.l);
                   break;   

        /* SBC  M    */
        case 0x9E: z80_sbc(mmu_read(*state.hl)); 
                   break;   

        /* SBC  A    */
        case 0x9f: z80_sbc(state.a); 
                   break;   

        /* ANA  B    */
        case 0xA0: z80_ana(state.b);
                   break;

        /* ANA  C    */
        case 0xA1: z80_ana(state.c);
                   break;

        /* ANA  D    */ 
        case 0xA2: z80_ana(state.d);
                   break;

        /* ANA  E    */
        case 0xA3: z80_ana(state.e);
                   break;

        /* ANA  H    */
        case 0xA4: z80_ana(state.h);
                   break;

        /* ANA  L    */
        case 0xA5: z80_ana(state.l);
                   break;

        /* ANA  M    */
        case 0xA6: z80_ana(mmu_read(*state.hl));
                   break;

        /* ANA  A    */
        case 0xA7: z80_ana(state.a);
                   break;

        /* XRA  B    */
        case 0xA8: z80_xra(state.b);
                   break;

        /* XRA  C    */
        case 0xA9: z80_xra(state.c);
                   break;

        /* XRA  D    */
        case 0xAA: z80_xra(state.d);
                   break;

        /* XRA  E    */
        case 0xAB: z80_xra(state.e);
                   break;

        /* XRA  H    */
        case 0xAC: z80_xra(state.h);
                   break;

        /* XRA  L    */
        case 0xAD: z80_xra(state.l);
                   break;

        /* XRA  M    */
        case 0xAE: z80_xra(mmu_read(*state.hl));
                   break;

        /* XRA  A    */
        case 0xAF: z80_xra(state.a);
                   break;

        /* ORA  B    */
        case 0xB0: z80_ora(state.b);
                   break;

        /* ORA  C    */
        case 0xB1: z80_ora(state.c);
                   break;

        /* ORA  D    */ 
        case 0xB2: z80_ora(state.d);
                   break;

        /* ORA  E    */
        case 0xB3: z80_ora(state.e);
                   break;

        /* ORA  H    */
        case 0xB4: z80_ora(state.h);
                   break;

        /* ORA  L    */
        case 0xB5: z80_ora(state.l);
                   break;

        /* ORA  M    */
        case 0xB6: z80_ora(mmu_read(*state.hl));
                   break;

        /* ORA  A    */
        case 0xB7: z80_ora(state.a);
                   break;

        /* CMP  B    */
        case 0xB8: z80_cmp(state.b);
                   break;

        /* CMP  C    */
        case 0xB9: z80_cmp(state.c);
                   break;

        /* CMP  D    */
        case 0xBA: z80_cmp(state.d);
                   break;

        /* CMP  E    */
        case 0xBB: z80_cmp(state.e);
                   break;

        /* CMP  H    */
        case 0xBC: z80_cmp(state.h);
                   break;

        /* CMP  L    */
        case 0xBD: z80_cmp(state.l);
                   break;

        /* CMP  M    */
        case 0xBE: z80_cmp(mmu_read(*state.hl));
                   break;

        /* CMP  A    */
        case 0xBF: z80_cmp(state.a);
                   break;

        /* RNZ       */
        case 0xC0: cycles_step();

                   if (state.flags.z == 0)
                       return z80_ret();
 
                   break;

        /* POP  B    */
        case 0xC1: *state.bc = mmu_read_16(state.sp); 
                   state.sp += 2;
                   break;

        /* JNZ  addr */
        case 0xC2: /* this will add 8 cycles */
                   addr = ADDR;

                   if (state.flags.z == 0)
                   {
                       /* add 4 more cycles */
                       cycles_step();

                       state.pc = addr;
                       return 0;
                   } 

                   b = 3;                      
                   break;

        /* JMP  addr */
        case 0xC3: state.pc = ADDR;                

                   /* add 4 cycles */
                   cycles_step();

                   return 0;

        /* CNZ        */
        case 0xC4: addr = ADDR;

                   if (state.flags.z == 0)
                       return z80_call(addr);

                   b = 3;
                   break;

        /* PUSH B    */
        case 0xC5: cycles_step();
                   mmu_write_16(state.sp - 2, *state.bc);
                   state.sp -= 2;
                   break;

        /* ADI       */
        case 0xC6: z80_add(mmu_read(state.pc + 1));
                   b = 2;
                   break;

        /* RST  0    */
        case 0xC7: state.pc++;
                   return z80_intr(0x0008 * 0);
                  
        /* RZ        */
        case 0xC8: cycles_step();

                   if (state.flags.z)
                       return z80_ret();
 
                   break;

        /* RET       */
        case 0xC9: return z80_ret();

        /* JZ        */
        case 0xCA: /* add 8 cycles */
                   addr = ADDR;

                   if (state.flags.z)
                   {
                       /* add 4 more cycles */
                       cycles_step();

                       state.pc = addr;
                       return 0;
                   }

                   b = 3;
                   break;
       
        /* CB        */
        case 0xCB: b = z80_ext_cb_execute();
                   break;
 
        /* CZ        */
        case 0xCC: addr = ADDR;

                   if (state.flags.z)
                       return z80_call(addr);

                   b = 3;
                   break;
 
        /* CALL addr */
        case 0xCD: return z80_call(ADDR);

        /* ACI       */
        case 0xCE: z80_adc(mmu_read(state.pc + 1));
                   b = 2;
                   break;

        /* RST  1    */
        case 0xCF: state.pc++;
                   return z80_intr(0x0008 * 1);
                  
        /* RNC       */
        case 0xD0: cycles_step();

                   if (state.flags.cy == 0)
                       return z80_ret();
 
                   break;

        /* POP  D    */
        case 0xD1: *state.de = mmu_read_16(state.sp); 
                   state.sp += 2;
                   break;

        /* JNC       */
        case 0xD2: /* add 8 cycles */
                   addr = ADDR;

                   if (state.flags.cy == 0)
                   {
                       /* add 4 more cycles */
                       cycles_step();

                       state.pc = addr;
                       return 0;
                   }

                   b = 3;
                   break;

        /* not present       */
        case 0xD3: // b = 2;
                   break;

        /* CNC        */
        case 0xD4: addr = ADDR;
 
                   if (state.flags.cy == 0)
                       return z80_call(addr);

                   b = 3;
                   break;

        /* PUSH D    */
        case 0xD5: cycles_step(); 
                   mmu_write_16(state.sp - 2, *state.de);
                   state.sp -= 2;
                   break;

        /* SUI       */
        case 0xD6: z80_sub(mmu_read(state.pc + 1));
                   b = 2;
                   break;

        /* RST  2    */
        case 0xD7: state.pc++;
                   return z80_intr(0x0008 * 2);

        /* RC        */
        case 0xD8: cycles_step();

                   if (state.flags.cy)
                       return z80_ret();
 
                   break;

        /* RETI      */
        case 0xD9: state.int_enable = 1;
                   return z80_ret(); 
                   break;

        /* JC        */
        case 0xDA: /* add 8 cycles */
                   addr = ADDR;

                   if (state.flags.cy)
                   {
                       /* add 4 more cycles */
                       cycles_step();

                       state.pc = addr;
                       return 0;
                   }

                   b = 3;
                   break;

        /* not present        */
        case 0xDB: break;

        /* CC        */
        case 0xDC: addr = ADDR;

                   if (state.flags.cy)
                       return z80_call(addr);

                   b = 3;
                   break;

        /* SBI       */
        case 0xDE: z80_sbc(mmu_read(state.pc + 1));
                   b = 2;
                   break;

        /* RST  3    */
        case 0xDF: state.pc++;
                   return z80_intr(0x0008 * 3);

        /* LD   (FF00+N),A */
        case 0xE0: mmu_write(0xFF00 + mmu_read(state.pc + 1), state.a);
                   b = 2;
                   break;

        /* POP  H    */
        case 0xE1: *state.hl = mmu_read_16(state.sp); 
                   state.sp += 2;
                   break;

        /* LD   (FF00+C),A */
        case 0xE2: mmu_write(0xFF00 + state.c, state.a);
                   break;
    
        /* not present on Gameboy Z80 */
        case 0xE3:
        case 0xE4: break;

        /* PUSH H    */
        case 0xE5: cycles_step();
                   mmu_write_16(state.sp - 2, *state.hl);
                   state.sp -= 2;
                   break;

        /* ANI       */
        case 0xE6: z80_ana(mmu_read(state.pc + 1));
                   b = 2;                      
                   break;

        /* RST  4    */
        case 0xE7: state.pc++;
                   return z80_intr(0x0008 * 4);

        /* ADD  SP,dd      */
        case 0xE8: byte = mmu_read(state.pc + 1);
                   byte2 = (uint8_t) (state.sp & 0x00ff);
                   result = byte2 + byte; 

                   state.flags.z = 0;
                   state.flags.n = 0;

                   state.flags.cy = (result > 0xff);

                   /* add 8 cycles */
                   cycles_step();
                   cycles_step();

                   /* calc xor for AC  */
                   z80_set_flags_ac(byte2, byte, result);

                   /* set sp */
                   state.sp += (int8_t) byte; // result & 0xffff;

                   b = 2;
                   break;

        /* PCHL      */
        case 0xE9: state.pc = *state.hl; 
                   return 0;

        /* LD  (NN),A */
        case 0xEA: mmu_write(ADDR, state.a);
                   b = 3; 
                   break;

        /* not present on Gameboy Z80 */
        case 0xEB:
        case 0xEC:
        case 0xED: break;

        /* XRI       */
        case 0xEE: z80_xra(mmu_read(state.pc + 1));
                   b = 2;
                   break;

        /* RST  5    */
        case 0xEF: state.pc++;
                   return z80_intr(0x0008 * 5);
        
        /* LD  A,(FF00+N) */
        case 0xF0: state.a = mmu_read(0xFF00 + mmu_read(state.pc + 1));
                   b = 2;
                   break;
          
        /* POP  PSW  */
        case 0xF1: p = (uint8_t *) &state.flags;
                   *p        = (mmu_read(state.sp) & 0xf0);
                   state.a   = mmu_read(state.sp + 1);

                   state.sp += 2;
                   break;  

        /* LD  A,(FF00+C) */
        case 0xF2: state.a = mmu_read(0xFF00 + state.c);
                   break;

        /* DI        */
        case 0xF3: state.int_enable = 0;
                   break;

        /* not present on Gameboy Z80 */
        case 0xF4: break;
 
        /* PUSH PSW  */
        case 0xF5: p = (uint8_t *) &state.flags;

                   cycles_step();

                   mmu_write(state.sp - 1, state.a);
                   mmu_write(state.sp - 2, *p);
                   state.sp -= 2;
                   break;

        /* ORI       */
        case 0xF6: z80_ora(mmu_read(state.pc + 1));
                   b = 2;
                   break;

        /* RST  6    */
        case 0xF7: state.pc++;
                   return z80_intr(0x0008 * 6);

        /* LD  HL,SP+dd   */
        case 0xF8: byte = mmu_read(state.pc + 1);
                   byte2 = (uint8_t) (state.sp & 0x00ff);
                   result = byte2 + byte;

                   state.flags.z = 0;
                   state.flags.n = 0;

                   state.flags.cy = (result > 0xff);

                   /* add 4 cycles */
                   cycles_step();

                   /* calc xor for AC  */
                   z80_set_flags_ac(byte2, byte, result);

                   /* set sp */
                   *state.hl = state.sp + (int8_t) byte; // result & 0xffff;

                   b = 2;
                   break;
                  
        /* SPHL     */
        case 0xF9: cycles_step(); 
                   state.sp = *state.hl;
                   break;

        /* LD  A, (NN)    */
        case 0xFA: state.a = mmu_read(ADDR);
                   b = 3;
                   break;

        /* EI       */
        case 0xFB: state.int_enable = 1; 
                   break;

        /* not present on Gameboy Z80 */
        case 0xFC: 
        case 0xFD: break;

        /* CPI      */
        case 0xFE: z80_cmp(mmu_read(state.pc + 1));
                   b = 2;                      
                   break;

        /* RST  7    */
        case 0xFF: state.pc++;
                   return z80_intr(0x0008 * 7);
                  
        default: return 1;
    }

    /* make the PC points to the next instruction */
    state.pc += b;

    return 0;
}

/* init registers, flags and state.memory of Gameboy Z80 CPU */
z80_state_t static *z80_init()
{
    /* wipe all the structs */
    bzero(&state, sizeof(z80_state_t));

/* 16 bit values just point to the first reg of the pairs */
#if __BYTE_ORDER__ == __ORDER_LITTLE_ENDIAN__
        state.hl = (uint16_t *) &state.l;
        state.bc = (uint16_t *) &state.c;
        state.de = (uint16_t *) &state.e;
#else
        state.hl = (uint16_t *) &state.h;
        state.bc = (uint16_t *) &state.b;
        state.de = (uint16_t *) &state.d;
#endif

    state.sp = 0xffff;
    state.a  = 0xff;

    state.b  = 0x7f;
    state.c  = 0xbc;
    state.d  = 0x00;
    state.e  = 0x00;
    state.h  = 0x34;
    state.l  = 0xc0;

    regs_dst = malloc(8 * sizeof(uint8_t *));

    regs_dst[0x00] = &state.b;
    regs_dst[0x01] = &state.c;
    regs_dst[0x02] = &state.d;
    regs_dst[0x03] = &state.e;
    regs_dst[0x04] = &state.h;
    regs_dst[0x05] = &state.l;
    regs_dst[0x06] = &dummy;
    regs_dst[0x07] = &state.a;

    regs_src = malloc(8 * sizeof(uint8_t *));

    regs_src[0x00] = &state.b;
    regs_src[0x01] = &state.c;
    regs_src[0x02] = &state.d;
    regs_src[0x03] = &state.e;
    regs_src[0x04] = &state.h;
    regs_src[0x05] = &state.l;
    regs_src[0x06] = mmu_addr(*state.hl);
    regs_src[0x07] = &state.a;
 
    state.flags.cy = 1;
    state.flags.n  = 1;
    state.flags.ac = 1;
    state.flags.z  = 1;

    /* flags shortcut */
    state.f = (uint8_t *) &state.flags;
 
    /* flags mask array  */
    z80_calc_flags_mask_array();

    return &state;
}
