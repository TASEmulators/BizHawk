#ifndef I2C_H_
#define I2C_H_

typedef struct {
	char        *buffer;
	uint32_t    size;
	uint16_t    address;
	uint8_t     host_sda;
	uint8_t     slave_sda;
	uint8_t     scl;
	uint8_t     state;
	uint8_t     counter;
	uint8_t     latch;
} eeprom_state;

void eeprom_init(eeprom_state *state, uint8_t *buffer, uint32_t size);
void * write_eeprom_i2c_w(uint32_t address, void * context, uint16_t value);
void * write_eeprom_i2c_b(uint32_t address, void * context, uint8_t value);
uint16_t read_eeprom_i2c_w(uint32_t address, void * context);
uint8_t read_eeprom_i2c_b(uint32_t address, void * context);

#endif //I2C_H_
