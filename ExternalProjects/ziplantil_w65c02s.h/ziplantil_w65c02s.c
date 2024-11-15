

#ifdef _WIN32
#define W_EXPORT __declspec(dllexport)
#else
#define W_EXPORT __attribute__((visibility("default")))
#endif // _WIN32

#include "w65c02s.h/include/w65c02s.h"

W_EXPORT void exp_w65c02s_init(struct w65c02s_cpu* cpu,
	uint8_t(*mem_read)(struct w65c02s_cpu*, uint16_t),
	void (*mem_write)(struct w65c02s_cpu*, uint16_t, uint8_t),
	void* cpu_data) {
	w65c02s_init(cpu, mem_read, mem_write, cpu_data);
}

W_EXPORT size_t exp_w65c02s_cpu_size(void) {
    return w65c02s_cpu_size();
}

W_EXPORT unsigned long exp_w65c02s_run_cycles(struct w65c02s_cpu* cpu, unsigned long cycles) {
	return w65c02s_run_cycles(cpu, cycles);
}

W_EXPORT unsigned long exp_w65c02s_step_instruction(struct w65c02s_cpu* cpu) {
	return w65c02s_step_instruction(cpu);
}

W_EXPORT unsigned long exp_w65c02s_run_instructions(struct w65c02s_cpu* cpu,
	unsigned long instructions,
	bool finish_existing) {
	return w65c02s_run_instructions(cpu, instructions, finish_existing);
}

W_EXPORT unsigned long exp_w65c02s_get_cycle_count(const struct w65c02s_cpu* cpu) {
	return w65c02s_get_cycle_count(cpu);
}

W_EXPORT unsigned long exp_w65c02s_get_instruction_count(const struct w65c02s_cpu* cpu) {
	return w65c02s_get_instruction_count(cpu);
}

W_EXPORT void* exp_w65c02s_get_cpu_data(const struct w65c02s_cpu* cpu) {
	return w65c02s_get_cpu_data(cpu);
}

W_EXPORT void exp_w65c02s_reset_cycle_count(struct w65c02s_cpu* cpu) {
	w65c02s_reset_cycle_count(cpu);
}

W_EXPORT void exp_w65c02s_reset_instruction_count(struct w65c02s_cpu* cpu) {
	w65c02s_reset_instruction_count(cpu);
}

W_EXPORT bool exp_w65c02s_is_cpu_waiting(const struct w65c02s_cpu* cpu) {
	return w65c02s_is_cpu_waiting(cpu);
}

W_EXPORT bool exp_w65c02s_is_cpu_stopped(const struct w65c02s_cpu* cpu) {
	return w65c02s_is_cpu_stopped(cpu);
}

W_EXPORT void exp_w65c02s_break(struct w65c02s_cpu* cpu) {
	w65c02s_break(cpu);
}

W_EXPORT void exp_w65c02s_stall(struct w65c02s_cpu* cpu, unsigned long cycles) {
	w65c02s_stall(cpu, cycles);
}

W_EXPORT void exp_w65c02s_nmi(struct w65c02s_cpu* cpu) {
	w65c02s_nmi(cpu);
}

W_EXPORT void exp_w65c02s_reset(struct w65c02s_cpu* cpu) {
	w65c02s_reset(cpu);
}

W_EXPORT void exp_w65c02s_irq(struct w65c02s_cpu* cpu) {
	w65c02s_irq(cpu);
}

W_EXPORT void exp_w65c02s_irq_cancel(struct w65c02s_cpu* cpu) {
	w65c02s_irq_cancel(cpu);
}

W_EXPORT void exp_w65c02s_set_overflow(struct w65c02s_cpu* cpu) {
	w65c02s_set_overflow(cpu);
}

W_EXPORT bool exp_w65c02s_hook_brk(struct w65c02s_cpu* cpu, bool (*brk_hook)(uint8_t)) {
	return w65c02s_hook_brk(cpu, brk_hook);
}

W_EXPORT bool exp_w65c02s_hook_stp(struct w65c02s_cpu* cpu, bool (*stp_hook)(void)) {
	return w65c02s_hook_stp(cpu, stp_hook);
}

W_EXPORT bool exp_w65c02s_hook_end_of_instruction(struct w65c02s_cpu* cpu,
	void (*instruction_hook)(void)) {
	return w65c02s_hook_end_of_instruction(cpu, instruction_hook);
}

W_EXPORT uint8_t exp_w65c02s_reg_get_a(const struct w65c02s_cpu* cpu) {
	return w65c02s_reg_get_a(cpu);
}

W_EXPORT uint8_t exp_w65c02s_reg_get_x(const struct w65c02s_cpu* cpu) {
	return w65c02s_reg_get_x(cpu);
}

W_EXPORT uint8_t exp_w65c02s_reg_get_y(const struct w65c02s_cpu* cpu) {
	return w65c02s_reg_get_y(cpu);
}

W_EXPORT uint8_t exp_w65c02s_reg_get_p(const struct w65c02s_cpu* cpu) {
	return w65c02s_reg_get_p(cpu);
}

W_EXPORT uint8_t exp_w65c02s_reg_get_s(const struct w65c02s_cpu* cpu) {
	return w65c02s_reg_get_s(cpu);
}

W_EXPORT uint16_t exp_w65c02s_reg_get_pc(const struct w65c02s_cpu* cpu) {
	return w65c02s_reg_get_pc(cpu);
}

W_EXPORT void exp_w65c02s_reg_set_a(struct w65c02s_cpu* cpu, uint8_t value) {
	w65c02s_reg_set_a(cpu, value);
}

W_EXPORT void exp_w65c02s_reg_set_x(struct w65c02s_cpu* cpu, uint8_t value) {
	w65c02s_reg_set_x(cpu, value);
}

W_EXPORT void exp_w65c02s_reg_set_y(struct w65c02s_cpu* cpu, uint8_t value) {
	w65c02s_reg_set_y(cpu, value);
}

W_EXPORT void exp_w65c02s_reg_set_p(struct w65c02s_cpu* cpu, uint8_t value) {
	w65c02s_reg_set_p(cpu, value);
}

W_EXPORT void exp_w65c02s_reg_set_s(struct w65c02s_cpu* cpu, uint8_t value) {
	w65c02s_reg_set_s(cpu, value);
}

W_EXPORT void exp_w65c02s_reg_set_pc(struct w65c02s_cpu* cpu, uint16_t value) {
	w65c02s_reg_set_pc(cpu, value);
}

