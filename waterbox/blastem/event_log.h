#ifndef EVENT_LOG_H_
#define EVENT_LOG_H_

enum {
	EVENT_FLUSH = 0,
	EVENT_ADJUST = 1,
	EVENT_PSG_REG = 2,
	EVENT_YM_REG = 3,
	EVENT_VDP_REG = 4,
	EVENT_VRAM_BYTE = 5,
	EVENT_VRAM_BYTE_DELTA = 6,
	EVENT_VRAM_BYTE_ONE = 7,
	EVENT_VRAM_BYTE_AUTO = 8,
	EVENT_VRAM_WORD = 9,
	EVENT_VRAM_WORD_DELTA = 10,
	EVENT_VDP_INTRAM = 11,
	EVENT_STATE = 12,
	EVENT_MULTI = 13
	//14 and 15 are reserved for header types
};

#include "serialize.h"
#include "zlib/zlib.h"
typedef struct {
	size_t storage;
	uint8_t *socket_buffer;
	size_t socket_buffer_size;
	int socket;
	uint32_t last_cycle;
	uint32_t last_word_address;
	uint32_t last_byte_address;
	uint32_t repeat_delta;
	deserialize_buffer buffer;
	z_stream input_stream;
	uint8_t repeat_event;
	uint8_t repeat_remaining;
} event_reader;

#include "system.h"
#include "render.h"

void event_log_file(char *fname);
void event_log_tcp(char *address, char *port);
void event_system_start(system_type stype, vid_std video_std, char *name);
void event_cycle_adjust(uint32_t cycle, uint32_t deduction);
void event_log(uint8_t type, uint32_t cycle, uint8_t size, uint8_t *payload);
void event_vram_word(uint32_t cycle, uint32_t address, uint16_t value);
void event_vram_byte(uint32_t cycle, uint16_t address, uint8_t byte, uint8_t auto_inc);
void event_state(uint32_t cycle, serialize_buffer *state);
void event_flush(uint32_t cycle);
void event_soft_flush(uint32_t cycle);

void init_event_reader(event_reader *reader, uint8_t *data, size_t size);
void init_event_reader_tcp(event_reader *reader, char *address, char *port);
uint8_t reader_next_event(event_reader *reader, uint32_t *cycle_out);
void reader_ensure_data(event_reader *reader, size_t bytes);
uint8_t reader_system_type(event_reader *reader);
void reader_send_gamepad_event(event_reader *reader, uint8_t pad, uint8_t button, uint8_t down);

#endif //EVENT_LOG_H_
