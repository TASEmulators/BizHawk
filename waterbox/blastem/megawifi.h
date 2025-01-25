#ifndef MEGAWIFI_H_
#define MEGAWIFI_H_

void *megawifi_write_w(uint32_t address, void *context, uint16_t value);
void *megawifi_write_b(uint32_t address, void *context, uint8_t value);
uint16_t megawifi_read_w(uint32_t address, void *context);
uint8_t megawifi_read_b(uint32_t address, void *context);

#endif //MEGAWIFI_H_
