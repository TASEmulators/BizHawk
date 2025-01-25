#ifndef GEN_H_
#define GEN_H_
#include <stdint.h>

#if defined(X86_64) || defined(X86_32)
typedef uint8_t code_word;
#define RESERVE_WORDS 5 //opcode + 4-byte displacement
#else
typedef uint32_t code_word;
#define RESERVE_WORDS 4 //1 push + 1 ldr + 1bx + 1 constant
#endif
typedef code_word * code_ptr;
#define CODE_ALLOC_SIZE (1024*1024)

typedef struct {
	code_ptr cur;
	code_ptr last;
	uint32_t stack_off;
} code_info;

void check_alloc_code(code_info *code, uint32_t inst_size);

void init_code_info(code_info *code);
void call(code_info *code, code_ptr fun);
void jmp(code_info *code, code_ptr dest);
void jmp_r(code_info *code, uint8_t dst);
//standard return from subroutine instruction
void rts(code_info *code);
//call a function and put the arguments in the appropriate place according to the host ABI
void call_args(code_info *code, code_ptr fun, uint32_t num_args, ...);
//like the above, but call a function pointer stored in a register
void call_args_r(code_info *code, uint8_t fun_reg, uint32_t num_args, ...);
//like the above, but follows other aspects of the ABI like stack alignment
//void call_args_abi(code_info *code, code_ptr fun, uint32_t num_args, ...);
#define call_args_abi call_args
void save_callee_save_regs(code_info *code);
void restore_callee_save_regs(code_info *code);

#endif //GEN_H_
