#include <stdio.h>
#include <stdbool.h>
#include "gb.h"


typedef void GB_opcode_t(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc);

static void ill(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    GB_log(gb, ".BYTE $%02x\n", opcode);
    (*pc)++;
}

static void nop(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    GB_log(gb, "NOP\n");
    (*pc)++;
}

static void stop(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    uint8_t next = GB_read_memory(gb, (*pc)++);
    if (next) {
        GB_log(gb, "CORRUPTED STOP (%02x)\n", next);
    }
    else {
        GB_log(gb, "STOP\n");
    }
}

static char *register_names[] = {"af", "bc", "de", "hl", "sp"};

static void ld_rr_d16(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    uint8_t register_id;
    uint16_t value;
    register_id = (GB_read_memory(gb, (*pc)++) >> 4) + 1;
    value = GB_read_memory(gb, (*pc)++);
    value |= GB_read_memory(gb, (*pc)++) << 8;
    const char *symbol = GB_debugger_name_for_address(gb, value);
    if (symbol) {
        GB_log(gb, "LD %s, %s ; =$%04x\n", register_names[register_id], symbol, value);
    }
    else {
        GB_log(gb, "LD %s, $%04x\n", register_names[register_id], value);
    }
}

static void ld_drr_a(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    uint8_t register_id;
    register_id = (GB_read_memory(gb, (*pc)++) >> 4) + 1;
    GB_log(gb, "LD [%s], a\n", register_names[register_id]);
}

static void inc_rr(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    uint8_t register_id;
    register_id = (GB_read_memory(gb, (*pc)++) >> 4) + 1;
    GB_log(gb, "INC %s\n", register_names[register_id]);
}

static void inc_hr(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    uint8_t register_id;
    (*pc)++;
    register_id = ((opcode >> 4) + 1) & 0x03;
    GB_log(gb, "INC %c\n", register_names[register_id][0]);

}
static void dec_hr(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    uint8_t register_id;
    (*pc)++;
    register_id = ((opcode >> 4) + 1) & 0x03;
    GB_log(gb, "DEC %c\n", register_names[register_id][0]);
}

static void ld_hr_d8(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    uint8_t register_id;
    (*pc)++;
    register_id = ((opcode >> 4) + 1) & 0x03;
    GB_log(gb, "LD %c, $%02x\n", register_names[register_id][0], GB_read_memory(gb, (*pc)++));
}

static void rlca(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "RLCA\n");
}

static void rla(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "RLA\n");
}

static void ld_da16_sp(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc){
    uint16_t addr;
    (*pc)++;
    addr = GB_read_memory(gb, (*pc)++);
    addr |= GB_read_memory(gb, (*pc)++) << 8;
    const char *symbol = GB_debugger_name_for_address(gb, addr);
    if (symbol) {
        GB_log(gb, "LD [%s], sp ; =$%04x\n", symbol, addr);
    }
    else {
        GB_log(gb, "LD [$%04x], sp\n", addr);
    }
}

static void add_hl_rr(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    uint8_t register_id;
    (*pc)++;
    register_id = (opcode >> 4) + 1;
    GB_log(gb, "ADD hl, %s\n", register_names[register_id]);
}

static void ld_a_drr(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    uint8_t register_id;
    register_id = (GB_read_memory(gb, (*pc)++) >> 4) + 1;
    GB_log(gb, "LD a, [%s]\n", register_names[register_id]);
}

static void dec_rr(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    uint8_t register_id;
    register_id = (GB_read_memory(gb, (*pc)++) >> 4) + 1;
    GB_log(gb, "DEC %s\n", register_names[register_id]);
}

static void inc_lr(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    uint8_t register_id;
    register_id = (GB_read_memory(gb, (*pc)++) >> 4) + 1;

    GB_log(gb, "INC %c\n", register_names[register_id][1]);
}
static void dec_lr(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    uint8_t register_id;
    register_id = (GB_read_memory(gb, (*pc)++) >> 4) + 1;

    GB_log(gb, "DEC %c\n", register_names[register_id][1]);
}

static void ld_lr_d8(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    uint8_t register_id;
    register_id = (GB_read_memory(gb, (*pc)++) >> 4) + 1;

    GB_log(gb, "LD %c, $%02x\n", register_names[register_id][1], GB_read_memory(gb, (*pc)++));
}

static void rrca(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    GB_log(gb, "RRCA\n");
    (*pc)++;
}

static void rra(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    GB_log(gb, "RRA\n");
    (*pc)++;
}

static void jr_r8(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    uint16_t addr = *pc + (int8_t) GB_read_memory(gb, (*pc)) + 1;
    const char *symbol = GB_debugger_name_for_address(gb, addr);
    if (symbol) {
        GB_attributed_log(gb, GB_LOG_UNDERLINE, "JR %s ; =$%04x\n", symbol, addr);
    }
    else {
        GB_attributed_log(gb, GB_LOG_UNDERLINE, "JR $%04x\n", addr);
    }
    (*pc)++;
}

static const char *condition_code(uint8_t opcode)
{
    switch ((opcode >> 3) & 0x3) {
        case 0:
            return "nz";
        case 1:
            return "z";
        case 2:
            return "nc";
        case 3:
            return "c";
    }

    return NULL;
}

static void jr_cc_r8(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    uint16_t addr = *pc + (int8_t) GB_read_memory(gb, (*pc)) + 1;
    const char *symbol = GB_debugger_name_for_address(gb, addr);
    if (symbol) {
        GB_attributed_log(gb,  GB_LOG_DASHED_UNDERLINE, "JR %s, %s ; =$%04x\n", condition_code(opcode), symbol, addr);
    }
    else {
        GB_attributed_log(gb,  GB_LOG_DASHED_UNDERLINE, "JR %s, $%04x\n", condition_code(opcode), addr);
    }
    (*pc)++;
}

static void daa(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    GB_log(gb, "DAA\n");
    (*pc)++;
}

static void cpl(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    GB_log(gb, "CPL\n");
    (*pc)++;
}

static void scf(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    GB_log(gb, "SCF\n");
    (*pc)++;
}

static void ccf(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    GB_log(gb, "CCF\n");
    (*pc)++;
}

static void ld_dhli_a(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    GB_log(gb, "LD [hli], a\n");
    (*pc)++;
}

static void ld_dhld_a(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    GB_log(gb, "LD [hld], a\n");
    (*pc)++;
}

static void ld_a_dhli(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    GB_log(gb, "LD a, [hli]\n");
    (*pc)++;
}

static void ld_a_dhld(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    GB_log(gb, "LD a, [hld]\n");
    (*pc)++;
}

static void inc_dhl(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    GB_log(gb, "INC [hl]\n");
    (*pc)++;
}

static void dec_dhl(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    GB_log(gb, "DEC [hl]\n");
    (*pc)++;
}

static void ld_dhl_d8(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "LD [hl], $%02x\n", GB_read_memory(gb, (*pc)++));
}

static const char *get_src_name(uint8_t opcode)
{
    uint8_t src_register_id;
    uint8_t src_low;
    src_register_id = ((opcode >> 1) + 1) & 3;
    src_low = (opcode & 1);
    if (src_register_id == GB_REGISTER_AF) {
        return src_low? "a": "[hl]";
    }
    if (src_low) {
        return register_names[src_register_id] + 1;
    }
    static const char *high_register_names[] = {"a", "b", "d", "h"};
    return high_register_names[src_register_id];
}

static const char *get_dst_name(uint8_t opcode)
{
    uint8_t dst_register_id;
    uint8_t dst_low;
    dst_register_id = ((opcode >> 4) + 1) & 3;
    dst_low = opcode & 8;
    if (dst_register_id == GB_REGISTER_AF) {
        return dst_low? "a": "[hl]";
    }
    if (dst_low) {
        return register_names[dst_register_id] + 1;
    }
    static const char *high_register_names[] = {"a", "b", "d", "h"};
    return high_register_names[dst_register_id];
}

static void ld_r_r(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "LD %s, %s\n", get_dst_name(opcode), get_src_name(opcode));
}

static void add_a_r(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "ADD %s\n",  get_src_name(opcode));
}

static void adc_a_r(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "ADC %s\n",  get_src_name(opcode));
}

static void sub_a_r(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "SUB %s\n",  get_src_name(opcode));
}

static void sbc_a_r(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "SBC %s\n",  get_src_name(opcode));
}

static void and_a_r(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "AND %s\n",  get_src_name(opcode));
}

static void xor_a_r(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "XOR %s\n",  get_src_name(opcode));
}

static void or_a_r(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "OR %s\n",  get_src_name(opcode));
}

static void cp_a_r(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "CP %s\n",  get_src_name(opcode));
}

static void halt(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "HALT\n");
}

static void ret_cc(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_attributed_log(gb,  GB_LOG_DASHED_UNDERLINE, "RET %s\n", condition_code(opcode));
}

static void pop_rr(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    uint8_t register_id;
    register_id = ((GB_read_memory(gb, (*pc)++) >> 4) + 1) & 3;
    GB_log(gb, "POP %s\n", register_names[register_id]);
}

static void jp_cc_a16(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    uint16_t addr = GB_read_memory(gb, *pc) | (GB_read_memory(gb, *pc + 1) << 8);
    const char *symbol = GB_debugger_name_for_address(gb, addr);
    if (symbol) {
        GB_attributed_log(gb, GB_LOG_DASHED_UNDERLINE, "JP %s, %s ; =$%04x\n", condition_code(opcode), symbol, addr);
    }
    else {
        GB_attributed_log(gb, GB_LOG_DASHED_UNDERLINE, "JP %s, $%04x\n", condition_code(opcode), addr);
    }
    (*pc) += 2;
}

static void jp_a16(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    uint16_t addr = GB_read_memory(gb, *pc) | (GB_read_memory(gb, *pc + 1) << 8);
    const char *symbol = GB_debugger_name_for_address(gb, addr);
    if (symbol) {
        GB_log(gb, "JP %s ; =$%04x\n", symbol, addr);
    }
    else {
        GB_log(gb, "JP $%04x\n", addr);
    }
    (*pc) += 2;
}

static void call_cc_a16(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    uint16_t addr = GB_read_memory(gb, *pc) | (GB_read_memory(gb, *pc + 1) << 8);
    const char *symbol = GB_debugger_name_for_address(gb, addr);
    if (symbol) {
        GB_log(gb, "CALL %s, %s ; =$%04x\n", condition_code(opcode), symbol, addr);
    }
    else {
        GB_log(gb, "CALL %s, $%04x\n", condition_code(opcode), addr);
    }
    (*pc) += 2;
}

static void push_rr(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    uint8_t register_id;
    register_id = ((GB_read_memory(gb, (*pc)++) >> 4) + 1) & 3;
    GB_log(gb, "PUSH %s\n", register_names[register_id]);
}

static void add_a_d8(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "ADD $%02x\n", GB_read_memory(gb, (*pc)++));
}

static void adc_a_d8(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "ADC $%02x\n", GB_read_memory(gb, (*pc)++));
}

static void sub_a_d8(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "SUB $%02x\n", GB_read_memory(gb, (*pc)++));
}

static void sbc_a_d8(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "SBC $%02x\n", GB_read_memory(gb, (*pc)++));
}

static void and_a_d8(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "AND $%02x\n", GB_read_memory(gb, (*pc)++));
}

static void xor_a_d8(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "XOR $%02x\n", GB_read_memory(gb, (*pc)++));
}

static void or_a_d8(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "OR $%02x\n", GB_read_memory(gb, (*pc)++));
}

static void cp_a_d8(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "CP $%02x\n", GB_read_memory(gb, (*pc)++));
}

static void rst(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "RST $%02x\n", opcode ^ 0xC7);

}

static void ret(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_attributed_log(gb, GB_LOG_UNDERLINE, "RET\n");
}

static void reti(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_attributed_log(gb, GB_LOG_UNDERLINE, "RETI\n");
}

static void call_a16(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    uint16_t addr = GB_read_memory(gb, *pc) | (GB_read_memory(gb, *pc + 1) << 8);
    const char *symbol = GB_debugger_name_for_address(gb, 0xff00 + addr);
    if (symbol) {
        GB_log(gb, "CALL %s ; =$%04x\n", symbol, addr);
    }
    else {
        GB_log(gb, "CALL $%04x\n", addr);
    }
    (*pc) += 2;
}

static void ld_da8_a(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    uint8_t addr = GB_read_memory(gb, (*pc)++);
    const char *symbol = GB_debugger_name_for_address(gb, 0xff00 + addr);
    if (symbol) {
        GB_log(gb, "LDH [%s & $FF], a ; =$%02x\n", symbol, addr);
    }
    else {
        GB_log(gb, "LDH [$%02x], a\n", addr);
    }
}

static void ld_a_da8(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    uint8_t addr = GB_read_memory(gb, (*pc)++);
    const char *symbol = GB_debugger_name_for_address(gb, 0xff00 + addr);
    if (symbol) {
        GB_log(gb, "LDH a, [%s & $FF] ; =$%02x\n", symbol, addr);
    }
    else {
        GB_log(gb, "LDH a, [$%02x]\n", addr);
    }
}

static void ld_dc_a(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "LDH [c], a\n");
}

static void ld_a_dc(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "LDH a, [c]\n");
}

static void add_sp_r8(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    int8_t temp = GB_read_memory(gb, (*pc)++);
    GB_log(gb, "ADD SP, %s$%02x\n", temp < 0? "-" : "", temp < 0? -temp : temp);
}

static void jp_hl(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "JP hl\n");
}

static void ld_da16_a(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    uint16_t addr = GB_read_memory(gb, *pc) | (GB_read_memory(gb, *pc + 1) << 8);
    const char *symbol = GB_debugger_name_for_address(gb, addr);
    if (symbol) {
        GB_log(gb, "LD [%s], a ; =$%04x\n", symbol, addr);
    }
    else {
        GB_log(gb, "LD [$%04x], a\n", addr);
    }
    (*pc) += 2;
}

static void ld_a_da16(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    uint16_t addr = GB_read_memory(gb, *pc) | (GB_read_memory(gb, *pc + 1) << 8);
    const char *symbol = GB_debugger_name_for_address(gb, addr);
    if (symbol) {
        GB_log(gb, "LD a, [%s] ; =$%04x\n", symbol, addr);
    }
    else {
        GB_log(gb, "LD a, [$%04x]\n", addr);
    }
    (*pc) += 2;
}

static void di(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "DI\n");
}

static void ei(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "EI\n");
}

static void ld_hl_sp_r8(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    int8_t temp = GB_read_memory(gb, (*pc)++);
    GB_log(gb, "LD hl, sp, %s$%02x\n", temp < 0? "-" : "", temp < 0? -temp : temp);
}

static void ld_sp_hl(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "LD sp, hl\n");
}

static void rlc_r(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "RLC %s\n",  get_src_name(opcode));
}

static void rrc_r(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "RRC %s\n",  get_src_name(opcode));
}

static void rl_r(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "RL %s\n",  get_src_name(opcode));
}

static void rr_r(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "RR %s\n",  get_src_name(opcode));
}

static void sla_r(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "SLA %s\n",  get_src_name(opcode));
}

static void sra_r(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "SRA %s\n",  get_src_name(opcode));
}

static void srl_r(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "SRL %s\n",  get_src_name(opcode));
}

static void swap_r(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    (*pc)++;
    GB_log(gb, "RLC %s\n",  get_src_name(opcode));
}

static void bit_r(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    uint8_t bit;
    (*pc)++;
    bit = ((opcode >> 3) & 7);
    if ((opcode & 0xC0) == 0x40) { /* Bit */
        GB_log(gb, "BIT %s, %d\n",  get_src_name(opcode), bit);
    }
    else if ((opcode & 0xC0) == 0x80) { /* res */
        GB_log(gb, "RES %s, %d\n",  get_src_name(opcode), bit);
    }
    else if ((opcode & 0xC0) == 0xC0) { /* set */
        GB_log(gb, "SET %s, %d\n",  get_src_name(opcode), bit);
    }
}

static void cb_prefix(GB_gameboy_t *gb, uint8_t opcode, uint16_t *pc)
{
    opcode = GB_read_memory(gb, ++*pc);
    switch (opcode >> 3) {
        case 0:
            rlc_r(gb, opcode, pc);
            break;
        case 1:
            rrc_r(gb, opcode, pc);
            break;
        case 2:
            rl_r(gb, opcode, pc);
            break;
        case 3:
            rr_r(gb, opcode, pc);
            break;
        case 4:
            sla_r(gb, opcode, pc);
            break;
        case 5:
            sra_r(gb, opcode, pc);
            break;
        case 6:
            swap_r(gb, opcode, pc);
            break;
        case 7:
            srl_r(gb, opcode, pc);
            break;
        default:
            bit_r(gb, opcode, pc);
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
    ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     /* 4X */
    ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,
    ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     /* 5X */
    ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,
    ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     /* 6X */
    ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,
    ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     halt,       ld_r_r,     /* 7X */
    ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,     ld_r_r,
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



void GB_cpu_disassemble(GB_gameboy_t *gb, uint16_t pc, uint16_t count)
{
    const GB_bank_symbol_t *function_symbol = GB_debugger_find_symbol(gb, pc);

    if (function_symbol && pc - function_symbol->addr > 0x1000) {
        function_symbol = NULL;
    }

    if (function_symbol && pc != function_symbol->addr) {
        GB_log(gb, "%s:\n", function_symbol->name);
    }

    uint16_t current_function = function_symbol? function_symbol->addr : 0;

    while (count--) {
        function_symbol = GB_debugger_find_symbol(gb, pc);
        if (function_symbol && function_symbol->addr == pc) {
            if (current_function != function_symbol->addr) {
                GB_log(gb, "\n");
            }
            GB_log(gb, "%s:\n", function_symbol->name);
        }
        if (function_symbol) {
            GB_log(gb, "%s%04x <+%03x>: ", pc == gb->pc? "  ->": "    ", pc, pc - function_symbol->addr);
        }
        else {
            GB_log(gb, "%s%04x: ", pc == gb->pc? "  ->": "    ", pc);
        }
        uint8_t opcode = GB_read_memory(gb, pc);
        opcodes[opcode](gb, opcode, &pc);
    }
}
