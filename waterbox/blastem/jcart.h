#ifndef JCART_H_
#define JCART_H_

void *jcart_write_w(uint32_t address, void *context, uint16_t value);
void *jcart_write_b(uint32_t address, void *context, uint8_t value);
uint16_t jcart_read_w(uint32_t address, void *context);
uint8_t jcart_read_b(uint32_t address, void *context);
void jcart_adjust_cycles(genesis_context *context, uint32_t deduction);
void jcart_gamepad_down(genesis_context *context, uint8_t gamepad_num, uint8_t button);
void jcart_gamepad_up(genesis_context *context, uint8_t gamepad_num, uint8_t button);

#endif //JCART_H_
