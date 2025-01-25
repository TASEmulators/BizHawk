/*
 Copyright 2014 Michael Pavone
 This file is part of BlastEm.
 BlastEm is free software distributed under the terms of the GNU General Public License version 3 or greater. See COPYING for full license text.
*/
#ifndef M68K_INTERNAL_H_
#define M68K_INTERNAL_H_

#include "68kinst.h"

//functions implemented in host CPU specfic file
void translate_out_of_bounds(m68k_options *opts, uint32_t address);
void areg_to_native(m68k_options *opts, uint8_t reg, uint8_t native_reg);
void dreg_to_native(m68k_options *opts, uint8_t reg, uint8_t native_reg);
void areg_to_native_sx(m68k_options *opts, uint8_t reg, uint8_t native_reg);
void dreg_to_native_sx(m68k_options *opts, uint8_t reg, uint8_t native_reg);
void native_to_areg(m68k_options *opts, uint8_t native_reg, uint8_t reg);
void native_to_dreg(m68k_options *opts, uint8_t native_reg, uint8_t reg);
void ldi_areg(m68k_options *opts, int32_t value, uint8_t reg);
void ldi_native(m68k_options *opts, int32_t value, uint8_t reg);
void addi_native(m68k_options *opts, int32_t value, uint8_t reg);
void subi_native(m68k_options *opts, int32_t value, uint8_t reg);
void push_native(m68k_options *opts, uint8_t reg);
void pop_native(m68k_options *opts, uint8_t reg);
void sign_extend16_native(m68k_options *opts, uint8_t reg);
void addi_areg(m68k_options *opts, int32_t val, uint8_t reg);
void subi_areg(m68k_options *opts, int32_t val, uint8_t reg);
void add_areg_native(m68k_options *opts, uint8_t reg, uint8_t native_reg);
void add_dreg_native(m68k_options *opts, uint8_t reg, uint8_t native_reg);
void calc_areg_displace(m68k_options *opts, m68k_op_info *op, uint8_t native_reg);
void calc_index_disp8(m68k_options *opts, m68k_op_info *op, uint8_t native_reg);
void calc_areg_index_disp8(m68k_options *opts, m68k_op_info *op, uint8_t native_reg);
void nop_fill_or_jmp_next(code_info *code, code_ptr old_end, code_ptr next_inst);
void check_user_mode_swap_ssp_usp(m68k_options *opts);
void m68k_set_last_prefetch(m68k_options *opts, uint32_t address);
void translate_m68k_odd(m68k_options *opts, m68kinst *inst);
void m68k_trap_if_not_supervisor(m68k_options *opts, m68kinst *inst);
void m68k_breakpoint_patch(m68k_context *context, uint32_t address, m68k_debug_handler bp_handler, code_ptr native_addr);
void m68k_check_cycles_int_latch(m68k_options *opts);
uint8_t translate_m68k_op(m68kinst * inst, host_ea * ea, m68k_options * opts, uint8_t dst);

//functions implemented in m68k_core.c
int8_t native_reg(m68k_op_info * op, m68k_options * opts);
size_t dreg_offset(uint8_t reg);
size_t areg_offset(uint8_t reg);
size_t reg_offset(m68k_op_info *op);
void m68k_read_size(m68k_options *opts, uint8_t size);
void m68k_write_size(m68k_options *opts, uint8_t size, uint8_t lowfirst);
void m68k_save_result(m68kinst * inst, m68k_options * opts);
void jump_m68k_abs(m68k_options * opts, uint32_t address);
void swap_ssp_usp(m68k_options * opts);
code_ptr get_native_address(m68k_options *opts, uint32_t address);
uint8_t m68k_is_terminal(m68kinst * inst);
code_ptr get_native_address_trans(m68k_context * context, uint32_t address);
void * m68k_retranslate_inst(uint32_t address, m68k_context * context);
m68k_context *m68k_bp_dispatcher(m68k_context *context, uint32_t address);

//individual instructions
void translate_m68k_bcc(m68k_options * opts, m68kinst * inst);
void translate_m68k_scc(m68k_options * opts, m68kinst * inst);
void translate_m68k_dbcc(m68k_options * opts, m68kinst * inst);
void translate_m68k_trapv(m68k_options *opts, m68kinst *inst);
void translate_m68k_move(m68k_options * opts, m68kinst * inst);
void translate_m68k_movep(m68k_options * opts, m68kinst * inst);
void translate_m68k_arith(m68k_options *opts, m68kinst * inst, uint32_t flag_mask, host_ea *src_op, host_ea *dst_op);
void translate_m68k_unary(m68k_options *opts, m68kinst *inst, uint32_t flag_mask, host_ea *dst_op);
void translate_m68k_cmp(m68k_options * opts, m68kinst * inst);
void translate_m68k_tas(m68k_options * opts, m68kinst * inst);
void translate_m68k_ext(m68k_options * opts, m68kinst * inst);
void translate_m68k_abcd_sbcd(m68k_options *opts, m68kinst *inst, host_ea *src_op, host_ea *dst_op);
void translate_m68k_sl(m68k_options *opts, m68kinst *inst, host_ea *src_op, host_ea *dst_op);
void translate_m68k_asr(m68k_options *opts, m68kinst *inst, host_ea *src_op, host_ea *dst_op);
void translate_m68k_lsr(m68k_options *opts, m68kinst *inst, host_ea *src_op, host_ea *dst_op);
void translate_m68k_bit(m68k_options *opts, m68kinst *inst, host_ea *src_op, host_ea *dst_op);
void translate_m68k_chk(m68k_options *opts, m68kinst *inst, host_ea *src_op, host_ea *dst_op);
void translate_m68k_div(m68k_options *opts, m68kinst *inst, host_ea *src_op, host_ea *dst_op);
void translate_m68k_exg(m68k_options *opts, m68kinst *inst, host_ea *src_op, host_ea *dst_op);
void translate_m68k_mul(m68k_options *opts, m68kinst *inst, host_ea *src_op, host_ea *dst_op);
void translate_m68k_negx(m68k_options *opts, m68kinst *inst, host_ea *src_op, host_ea *dst_op);
void translate_m68k_rot(m68k_options *opts, m68kinst *inst, host_ea *src_op, host_ea *dst_op);
void translate_m68k_andi_ori_ccr_sr(m68k_options *opts, m68kinst *inst);
void translate_m68k_eori_ccr_sr(m68k_options *opts, m68kinst *inst);
void translate_m68k_move_ccr_sr(m68k_options *opts, m68kinst *inst, host_ea *src_op, host_ea *dst_op);
void translate_m68k_stop(m68k_options *opts, m68kinst *inst);
void translate_m68k_move_from_sr(m68k_options *opts, m68kinst *inst, host_ea *src_op, host_ea *dst_op);
void translate_m68k_reset(m68k_options *opts, m68kinst *inst);

//flag update bits
#define X0  0x0001
#define X1  0x0002
#define X   0x0004
#define N0  0x0008
#define N1  0x0010
#define N   0x0020
#define Z0  0x0040
#define Z1  0x0080
#define Z   0x0100
#define V0  0x0200
#define V1  0x0400
#define V   0x0800
#define C0  0x1000
#define C1  0x2000
#define C   0x4000

#define BUS 4
#define PREDEC_PENALTY 2
extern char disasm_buf[1024];

m68k_context * sync_components(m68k_context * context, uint32_t address);

void m68k_invalid();
void bcd_add();
void bcd_sub();

#endif //M68K_INTERNAL_H_
