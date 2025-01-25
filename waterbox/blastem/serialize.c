#include <string.h>
#include <stdlib.h>
#include <stdio.h>
#include "serialize.h"
#include "util.h"

void init_serialize(serialize_buffer *buf)
{
	buf->storage = SERIALIZE_DEFAULT_SIZE;
	buf->size = 0;
	buf->current_section_start = 0;
	buf->data = malloc(SERIALIZE_DEFAULT_SIZE);
}

static void reserve(serialize_buffer *buf, size_t amount)
{
	if (amount > (buf->storage - buf->size)) {
		if (amount < buf->storage) {
			buf->storage *= 2;
		} else {
			//doublign isn't enough, increase by the precise amount needed
			buf->storage += amount - (buf->storage - buf->size);
		}
		buf->data = realloc(buf->data, buf->storage + sizeof(*buf));
	}
}

void save_int32(serialize_buffer *buf, uint32_t val)
{
	reserve(buf, sizeof(val));
	buf->data[buf->size++] = val >> 24;
	buf->data[buf->size++] = val >> 16;
	buf->data[buf->size++] = val >> 8;
	buf->data[buf->size++] = val;
}

void save_int16(serialize_buffer *buf, uint16_t val)
{
	reserve(buf, sizeof(val));
	buf->data[buf->size++] = val >> 8;
	buf->data[buf->size++] = val;
}

void save_int8(serialize_buffer *buf, uint8_t val)
{
	reserve(buf, sizeof(val));
	buf->data[buf->size++] = val;
}

void save_string(serialize_buffer *buf, char *val)
{
	size_t len = strlen(val);
	save_buffer8(buf, val, len);
}

void save_buffer8(serialize_buffer *buf, void *val, size_t len)
{
	reserve(buf, len);
	memcpy(&buf->data[buf->size], val, len);
	buf->size += len;
}

void save_buffer16(serialize_buffer *buf, uint16_t *val, size_t len)
{
	reserve(buf, len * sizeof(*val));
	for(; len != 0; len--, val++) {
		buf->data[buf->size++] = *val >> 8;
		buf->data[buf->size++] = *val;
	}
}

void save_buffer32(serialize_buffer *buf, uint32_t *val, size_t len)
{
	reserve(buf, len * sizeof(*val));
	for(; len != 0; len--, val++) {
		buf->data[buf->size++] = *val >> 24;
		buf->data[buf->size++] = *val >> 16;
		buf->data[buf->size++] = *val >> 8;
		buf->data[buf->size++] = *val;
	}
}

void start_section(serialize_buffer *buf, uint16_t section_id)
{
	save_int16(buf, section_id);
	//reserve some space for size once we end this section
	reserve(buf, sizeof(uint32_t));
	buf->size += sizeof(uint32_t);
	//save start point for use in end_device
	buf->current_section_start = buf->size;
}

void end_section(serialize_buffer *buf)
{
	size_t section_size = buf->size - buf->current_section_start;
	if (section_size > 0xFFFFFFFFU) {
		fatal_error("Sections larger than 4GB are not supported");
	}
	uint32_t size = section_size;
	uint8_t *field = buf->data + buf->current_section_start - sizeof(uint32_t);
	*(field++) = size >> 24;
	*(field++) = size >> 16;
	*(field++) = size >> 8;
	*(field++) = size;
	buf->current_section_start = 0;
}

void register_section_handler(deserialize_buffer *buf, section_handler handler, uint16_t section_id)
{
	if (section_id > buf->max_handler) {
		uint16_t old_max = buf->max_handler;
		if (buf->max_handler < 0x8000) {
			buf->max_handler *= 2;
		} else {
			buf->max_handler = 0xFFFF;
		}
		buf->handlers = realloc(buf->handlers, (buf->max_handler+1) * sizeof(handler));
		memset(buf->handlers + old_max + 1, 0, (buf->max_handler - old_max) * sizeof(handler));
	}
	if (!buf->handlers) {
		buf->handlers = calloc(buf->max_handler + 1, sizeof(handler));
	}
	buf->handlers[section_id] = handler;
}

void init_deserialize(deserialize_buffer *buf, uint8_t *data, size_t size)
{
	buf->size = size;
	buf->cur_pos = 0;
	buf->data = data;
	buf->handlers = NULL;
	buf->max_handler = 8;
}

uint32_t load_int32(deserialize_buffer *buf)
{
	uint32_t val;
	if ((buf->size - buf->cur_pos) < sizeof(val)) {
		fatal_error("Failed to load required int32 field");
	}
	val = buf->data[buf->cur_pos++] << 24;
	val |= buf->data[buf->cur_pos++] << 16;
	val |= buf->data[buf->cur_pos++] << 8;
	val |= buf->data[buf->cur_pos++];
	return val;
}

uint16_t load_int16(deserialize_buffer *buf)
{
	uint16_t val;
	if ((buf->size - buf->cur_pos) < sizeof(val)) {
		fatal_error("Failed to load required int16 field");
	}
	val = buf->data[buf->cur_pos++] << 8;
	val |= buf->data[buf->cur_pos++];
	return val;
}

uint8_t load_int8(deserialize_buffer *buf)
{
	uint8_t val;
	if ((buf->size - buf->cur_pos) < sizeof(val)) {
		fatal_error("Failed to load required int8 field");
	}
	val = buf->data[buf->cur_pos++];
	return val;
}

void load_buffer8(deserialize_buffer *buf, void *dst, size_t len)
{
	if ((buf->size - buf->cur_pos) < len) {
		fatal_error("Failed to load required buffer of size %d", len);
	}
	memcpy(dst, buf->data + buf->cur_pos, len);
	buf->cur_pos += len;
}

void load_buffer16(deserialize_buffer *buf, uint16_t *dst, size_t len)
{
	if ((buf->size - buf->cur_pos) < len * sizeof(uint16_t)) {
		fatal_error("Failed to load required buffer of size %d\n", len);
	}
	for(; len != 0; len--, dst++) {
		uint16_t value = buf->data[buf->cur_pos++] << 8;
		value |= buf->data[buf->cur_pos++];
		*dst = value;
	}
}
void load_buffer32(deserialize_buffer *buf, uint32_t *dst, size_t len)
{
	if ((buf->size - buf->cur_pos) < len * sizeof(uint32_t)) {
		fatal_error("Failed to load required buffer of size %d\n", len);
	}
	for(; len != 0; len--, dst++) {
		uint32_t value = buf->data[buf->cur_pos++] << 24;
		value |= buf->data[buf->cur_pos++] << 16;
		value |= buf->data[buf->cur_pos++] << 8;
		value |= buf->data[buf->cur_pos++];
		*dst = value;
	}
}

int load_section(deserialize_buffer *buf)
{
	if (!buf->handlers) {
		fatal_error("load_section called on a deserialize_buffer with no handlers registered\n");
	}
	uint16_t section_id = load_int16(buf);
	if (section_id == SECTION_END_OF_SERIALIZATION) {
		return 0;
	}
	uint32_t size = load_int32(buf);
	if (size > (buf->size - buf->cur_pos)) {
		fatal_error("Section is bigger than remaining space in file");
	}
	if (section_id > buf->max_handler || !buf->handlers[section_id].fun) {
		warning("No handler for section ID %d, save state may be from a newer version\n", section_id);
		buf->cur_pos += size;
		return 1;
	}
	deserialize_buffer section;
	init_deserialize(&section, buf->data + buf->cur_pos, size);
	buf->handlers[section_id].fun(&section, buf->handlers[section_id].data);
	buf->cur_pos += size;
	return 1;
}

static const char sz_ident[] = "BLSTSZ\x01\x07";

uint8_t save_to_file(serialize_buffer *buf, char *path)
{
	FILE *f = fopen(path, "wb");
	if (!f) {
		return 0;
	}
	if (fwrite(sz_ident, 1, sizeof(sz_ident)-1, f) != sizeof(sz_ident)-1) {
		fclose(f);
		return 0;
	}
	if (fwrite(buf->data, 1, buf->size, f) != buf->size) {
		fclose(f);
		return 0;
	}
	fclose(f);
	return 1;
}

uint8_t load_from_file(deserialize_buffer *buf, char *path)
{
	FILE *f = fopen(path, "rb");
	if (!f) {
		return 0;
	}
	char ident[sizeof(sz_ident)-1];
	long size = file_size(f);
	if (size < sizeof(ident)) {
		fclose(f);
		return 0;
	}
	if (fread(ident, 1, sizeof(ident), f) != sizeof(ident)) {
		fclose(f);
		return 0;
	}
	if (memcmp(ident, sz_ident, sizeof(ident))) {
		return 0;
	}
	buf->size = size - sizeof(ident);
	buf->cur_pos = 0;
	buf->data = malloc(buf->size);
	buf->handlers = NULL;
	buf->max_handler = 8;
	if (fread(buf->data, 1, buf->size, f) != buf->size) {
		fclose(f);
		free(buf->data);
		buf->data = NULL;
		buf->size = 0;
		return 0;
	}
	fclose(f);
	return 1;
}
