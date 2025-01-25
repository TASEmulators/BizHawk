#ifndef NOR_H_
#define NOR_H_

void nor_flash_init(nor_state *state, uint8_t *buffer, uint32_t size, uint32_t page_size, uint16_t product_id, uint8_t bus_flags);
uint8_t nor_flash_read_b(uint32_t address, void *vcontext);
uint16_t nor_flash_read_w(uint32_t address, void *context);
void *nor_flash_write_b(uint32_t address, void *vcontext, uint8_t value);
void *nor_flash_write_w(uint32_t address, void *vcontext, uint16_t value);

#endif //NOR_H_
