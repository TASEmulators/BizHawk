#ifndef SERIALIZE_H_
#define SERIALIZE_H_

#include <stdint.h>
#include <stddef.h>

#ifndef SERIALIZE_DEFAULT_SIZE
#define SERIALIZE_DEFAULT_SIZE (256*1024) //default to enough for a Genesis save state
#endif

typedef struct {
	size_t  size;
	size_t  storage;
	size_t  current_section_start;
	uint8_t *data;
} serialize_buffer;

typedef struct deserialize_buffer deserialize_buffer;
typedef void (*section_fun)(deserialize_buffer *buf, void *data);

typedef struct  {
	section_fun fun;
	void        *data;
} section_handler;

struct deserialize_buffer {
	size_t          size;
	size_t          cur_pos;
	uint8_t         *data;
	section_handler *handlers;
	uint16_t        max_handler;
};

enum {
	SECTION_END_OF_SERIALIZATION,
	SECTION_68000,
	SECTION_Z80,
	SECTION_VDP,
	SECTION_YM2612,
	SECTION_PSG,
	SECTION_GEN_BUS_ARBITER,
	SECTION_SEGA_IO_1,
	SECTION_SEGA_IO_2,
	SECTION_SEGA_IO_EXT,
	SECTION_MAIN_RAM,
	SECTION_SOUND_RAM,
	SECTION_MAPPER,
	SECTION_EEPROM,
	SECTION_CART_RAM,
	SECTION_TMSS
};

void init_serialize(serialize_buffer *buf);
void save_int32(serialize_buffer *buf, uint32_t val);
void save_int16(serialize_buffer *buf, uint16_t val);
void save_int8(serialize_buffer *buf, uint8_t val);
void save_string(serialize_buffer *buf, char *val);
void save_buffer8(serialize_buffer *buf, void *val, size_t len);
void save_buffer16(serialize_buffer *buf, uint16_t *val, size_t len);
void save_buffer32(serialize_buffer *buf, uint32_t *val, size_t len);
void start_section(serialize_buffer *buf, uint16_t section_id);
void end_section(serialize_buffer *buf);
void register_section_handler(deserialize_buffer *buf, section_handler handler, uint16_t section_id);
void init_deserialize(deserialize_buffer *buf, uint8_t *data, size_t size);
uint32_t load_int32(deserialize_buffer *buf);
uint16_t load_int16(deserialize_buffer *buf);
uint8_t load_int8(deserialize_buffer *buf);
void load_buffer8(deserialize_buffer *buf, void *dst, size_t len);
void load_buffer16(deserialize_buffer *buf, uint16_t *dst, size_t len);
void load_buffer32(deserialize_buffer *buf, uint32_t *dst, size_t len);
int load_section(deserialize_buffer *buf);
uint8_t save_to_file(serialize_buffer *buf, char *path);
uint8_t load_from_file(deserialize_buffer *buf, char *path);
#endif //SERIALIZE_H
