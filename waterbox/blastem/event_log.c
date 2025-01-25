#ifdef _WIN32
#define WINVER 0x501
#include <winsock2.h>
#include <ws2tcpip.h>
#else
#include <sys/types.h>
#include <sys/socket.h>
#include <unistd.h>
#include <netdb.h>
#include <netinet/tcp.h>
#endif

#include <stdlib.h>
#include <string.h>
#include <errno.h>
#include "event_log.h"
#include "util.h"
#include "blastem.h"
#include "saves.h"
#include "zlib/zlib.h"

enum {
	CMD_GAMEPAD_DOWN,
	CMD_GAMEPAD_UP,
};

static uint8_t active, fully_active;
static FILE *event_file;
static serialize_buffer buffer;
static uint8_t *compressed;
static size_t compressed_storage;
static z_stream output_stream;
static uint32_t last;

static void event_log_common_init(void)
{
	init_serialize(&buffer);
	compressed_storage = 128*1024;
	compressed = malloc(compressed_storage);
	deflateInit(&output_stream, 9);
	output_stream.avail_out = compressed_storage;
	output_stream.next_out = compressed;
	output_stream.avail_in = 0;
	output_stream.next_in = buffer.data;
	last = 0;
	active = 1;
}

static uint8_t multi_count;
static size_t multi_start;
static void finish_multi(void)
{
	buffer.data[multi_start] |= multi_count - 2;
	multi_count = 0;
}

static void file_finish(void)
{
	fwrite(compressed, 1, output_stream.next_out - compressed, event_file);
	output_stream.next_out = compressed;
	output_stream.avail_out = compressed_storage;
	int result = deflate(&output_stream, Z_FINISH);
	if (Z_STREAM_END != result) {
		fatal_error("Final deflate call returned %d\n", result);
	}
	fwrite(compressed, 1, output_stream.next_out - compressed, event_file);
	fclose(event_file);
}

static const char el_ident[] = "BLSTEL\x02\x00";
void event_log_file(char *fname)
{
	event_file = fopen(fname, "wb");
	if (!event_file) {
		warning("Failed to open event file %s for writing\n", fname);
		return;
	}
	fwrite(el_ident, 1, sizeof(el_ident) - 1, event_file);
	event_log_common_init();
	fully_active = 1;
	atexit(file_finish);
}

typedef struct {
	uint8_t  *send_progress;
	int      sock;
	uint8_t  players[1]; //TODO: Expand when support for multiple players per remote is added
	uint8_t  num_players;
} remote;

static int listen_sock;
static remote remotes[7];
static int num_remotes;
static uint8_t available_players[7] = {2,3,4,5,6,7,8};
static int num_available_players = 7;
void event_log_tcp(char *address, char *port)
{
	struct addrinfo request, *result;
	socket_init();
	memset(&request, 0, sizeof(request));
	request.ai_family = AF_INET;
	request.ai_socktype = SOCK_STREAM;
	request.ai_flags = AI_PASSIVE;
	getaddrinfo(address, port, &request, &result);
	
	listen_sock = socket(result->ai_family, result->ai_socktype, result->ai_protocol);
	if (listen_sock < 0) {
		warning("Failed to open event log listen socket on %s:%s\n", address, port);
		goto cleanup_address;
	}
	int param = 1;
	setsockopt(listen_sock, SOL_SOCKET, SO_REUSEADDR, (const char *)&param, sizeof(param));
	if (bind(listen_sock, result->ai_addr, result->ai_addrlen) < 0) {
		warning("Failed to bind event log listen socket on %s:%s\n", address, port);
		socket_close(listen_sock);
		goto cleanup_address;
	}
	if (listen(listen_sock, 3) < 0) {
		warning("Failed to listen for event log remotes on %s:%s\n", address, port);
		socket_close(listen_sock);
		goto cleanup_address;
	}
	socket_blocking(listen_sock, 0);
	event_log_common_init();
cleanup_address:
	freeaddrinfo(result);
}

static uint8_t *system_start;
static size_t system_start_size;
void event_system_start(system_type stype, vid_std video_std, char *name)
{
	if (!active) {
		return;
	}
	save_int8(&buffer, stype);
	save_int8(&buffer, video_std);
	size_t name_len = strlen(name);
	if (name_len > 255) {
		name_len = 255;
	}
	save_int8(&buffer, name_len);
	save_buffer8(&buffer, name, strlen(name));
	if (listen_sock) {
		system_start = malloc(buffer.size);
		system_start_size = buffer.size;
		memcpy(system_start, buffer.data, buffer.size);
	} else {
		//system start header is never compressed, so write to file immediately
		fwrite(buffer.data, 1, buffer.size, event_file);
	}
	buffer.size = 0;
}

//header formats
//Single byte: 4 bit type, 4 bit delta (16-31)
//Three Byte: 8 bit type, 16-bit delta
//Four byte: 8-bit type, 24-bit signed delta
#define FORMAT_3BYTE 0xE0
#define FORMAT_4BYTE 0xF0
static uint8_t last_event_type = 0xFF;
static uint32_t last_delta;
static void event_header(uint8_t type, uint32_t cycle)
{
	uint32_t delta = cycle - last;
	if (multi_count) {
		if (type != last_event_type || delta != last_delta) {
			finish_multi();
		} else {
			++multi_count;
			if (multi_count == 17) {
				finish_multi();
				last_event_type = 0xFF;
			}
			return;
		}
	} else if (type == last_event_type && delta == last_delta && type != EVENT_FLUSH) {
		//make some room
		save_int8(&buffer, 0);
		//shift existing command
		memmove(buffer.data + multi_start + 1, buffer.data + multi_start, buffer.size - multi_start - 1);
		buffer.data[multi_start] = EVENT_MULTI << 4;
		multi_count = 2;
		return;
	}
	multi_start = buffer.size;
	last_event_type = type;
	last_delta = delta;
	
	if (delta > 65535) {
		save_int8(&buffer, FORMAT_4BYTE | type);
		save_int8(&buffer, delta >> 16);
		save_int16(&buffer, delta);
	} else if (delta >= 16 && delta < 32) {
		save_int8(&buffer, type << 4 | (delta - 16));
	} else {
		save_int8(&buffer, FORMAT_3BYTE | type);
		save_int16(&buffer, delta);
	}
}

void event_cycle_adjust(uint32_t cycle, uint32_t deduction)
{
	if (!fully_active) {
		return;
	}
	event_header(EVENT_ADJUST, cycle);
	last = cycle - deduction;
	save_int32(&buffer, deduction);
}

static uint8_t next_available_player(void)
{
	uint8_t lowest = 0xFF;
	int lowest_index = -1;
	for (int i = 0; i < num_available_players; i++)
	{
		if (available_players[i] < lowest) {
			lowest = available_players[i];
			lowest_index = i;
		}
	}
	if (lowest_index >= 0) {
		available_players[lowest_index] = available_players[num_available_players - 1];
		--num_available_players;
	}
	return lowest;
}

static void flush_socket(void)
{
	int remote_sock = accept(listen_sock, NULL, NULL);
	if (remote_sock != -1) {
		if (num_remotes == 7) {
			socket_close(remote_sock);
		} else {
			printf("remote %d connected\n", num_remotes);
			uint8_t player = next_available_player();
			remotes[num_remotes++] = (remote){
				.sock = remote_sock,
				.send_progress = NULL,
				.players = {player},
				.num_players = player == 0xFF ? 0 : 1
			};
			current_system->save_state = EVENTLOG_SLOT + 1;
		}
	}
	uint8_t *min_progress = compressed;
	for (int i = 0; i < num_remotes; i++) {
		if (remotes[i].send_progress) {
			uint8_t recv_buffer[1500];
			int bytes = recv(remotes[i].sock, recv_buffer, sizeof(recv_buffer), 0);
			for (int j = 0; j < bytes; j++)
			{
				uint8_t cmd = recv_buffer[j];
				switch(cmd)
				{
				case CMD_GAMEPAD_DOWN:
				case CMD_GAMEPAD_UP: {
					++j;
					if (j < bytes) {
						uint8_t button = recv_buffer[j];
						uint8_t pad = (button >> 5) - 1;
						button &= 0x1F;
						if (pad <  remotes[i].num_players) {
							pad = remotes[i].players[pad];
							if (cmd == CMD_GAMEPAD_DOWN) {
								current_system->gamepad_down(current_system, pad, button);
							} else {
								current_system->gamepad_up(current_system, pad, button);
							}
						}
					} else {
						warning("Received incomplete command %X\n", cmd);
					}
					break;
				}
				default:
					warning("Unrecognized remote command %X\n", cmd);
					j = bytes;
				}
			}
			int sent = 1;
			while (sent && output_stream.next_out > remotes[i].send_progress)
			{
				sent = send(remotes[i].sock, remotes[i].send_progress, output_stream.next_out - remotes[i].send_progress, 0);
				if (sent >= 0) {
					remotes[i].send_progress += sent;
				} else if (!socket_error_is_wouldblock()) {
					socket_close(remotes[i].sock);
					for (int j = 0; j < remotes[i].num_players; j++) {
						available_players[num_available_players++] = remotes[i].players[j];
					}
					remotes[i] = remotes[num_remotes-1];
					num_remotes--;
					if (!num_remotes) {
						//last remote disconnected, reset buffers/deflate
						fully_active = 0;
						deflateReset(&output_stream);
						output_stream.next_out = compressed;
						output_stream.avail_out = compressed_storage;
						buffer.size = 0;
					}
					i--;
					break;
				}
				if (remotes[i].send_progress > min_progress) {
					min_progress = remotes[i].send_progress;
				}
			}
		}
	}
	if (min_progress == output_stream.next_out) {
		output_stream.next_out = compressed;
		output_stream.avail_out = compressed_storage;
		for (int i = 0; i < num_remotes; i++) {
			if (remotes[i].send_progress) {
				remotes[i].send_progress = compressed;
			}
		}
	}
}

uint8_t wrote_since_last_flush;
void event_log(uint8_t type, uint32_t cycle, uint8_t size, uint8_t *payload)
{
	if (!fully_active) {
		return;
	}
	event_header(type, cycle);
	last = cycle;
	save_buffer8(&buffer, payload, size);
	if (!multi_count) {
		last_event_type = 0xFF;
		output_stream.avail_in = buffer.size - (output_stream.next_in - buffer.data);
		int result = deflate(&output_stream, Z_NO_FLUSH);
		if (result != Z_OK) {
			fatal_error("deflate returned %d\n", result);
		}
		if (listen_sock) {
			if ((output_stream.next_out - compressed) > 1280 || !output_stream.avail_out) {
				flush_socket();
				wrote_since_last_flush = 1;
			}
		} else if (!output_stream.avail_out) {
			fwrite(compressed, 1, compressed_storage, event_file);
			output_stream.next_out = compressed;
			output_stream.avail_out = compressed_storage;
		}
		if (!output_stream.avail_in) {
			buffer.size = 0;
			output_stream.next_in = buffer.data;
		}
	}
}

static uint32_t last_word_address;
void event_vram_word(uint32_t cycle, uint32_t address, uint16_t value)
{
	uint32_t delta = address - last_word_address;
	if (delta < 256) {
		uint8_t buffer[3] = {delta, value >> 8, value};
		event_log(EVENT_VRAM_WORD_DELTA, cycle, sizeof(buffer), buffer);
	} else {
		uint8_t buffer[5] = {address >> 16, address >> 8, address, value >> 8, value};
		event_log(EVENT_VRAM_WORD, cycle, sizeof(buffer), buffer);
	}
	last_word_address = address;
}

static uint32_t last_byte_address;
void event_vram_byte(uint32_t cycle, uint16_t address, uint8_t byte, uint8_t auto_inc)
{
	uint32_t delta = address - last_byte_address;
	if (delta == 1) {
		event_log(EVENT_VRAM_BYTE_ONE, cycle, sizeof(byte), &byte);
	} else if (delta == auto_inc) {
		event_log(EVENT_VRAM_BYTE_AUTO, cycle, sizeof(byte), &byte);
	} else if (delta < 256) {
		uint8_t buffer[2] = {delta, byte};
		event_log(EVENT_VRAM_BYTE_DELTA, cycle, sizeof(buffer), buffer);
	} else {
		uint8_t buffer[3] = {address >> 8, address, byte};
		event_log(EVENT_VRAM_BYTE, cycle, sizeof(buffer), buffer);
	}
	last_byte_address = address;
}

static size_t send_all(int sock, uint8_t *data, size_t size, int flags)
{
	size_t total = 0, sent = 1;
	while(sent > 0 && total < size)
	{
		sent = send(sock, data + total, size - total, flags);
		if (sent > 0) {
			total += sent;
		}
	}
	return total;
}

void deflate_flush(uint8_t full)
{
	output_stream.avail_in = buffer.size - (output_stream.next_in - buffer.data);
	uint8_t force = full;
	while (output_stream.avail_in || force)
	{
		if (!output_stream.avail_out) {
			size_t old_storage = compressed_storage;
			uint8_t *old_compressed = compressed;
			compressed_storage *= 2;
			compressed = realloc(compressed, compressed_storage);
			output_stream.next_out = compressed + old_storage;
			output_stream.avail_out = old_storage;
			for (int i = 0; i < num_remotes; i++) {
				if (remotes[i].send_progress) {
					remotes[i].send_progress = compressed + (remotes[i].send_progress - old_compressed);
				}
			}
		}
		int result = deflate(&output_stream, full ? Z_FINISH : Z_SYNC_FLUSH);
		if (result != (full ? Z_STREAM_END : Z_OK)) {
			fatal_error("deflate returned %d\n", result);
		}
		if (full && result == Z_STREAM_END) {
			result = deflateReset(&output_stream);
			if (result != Z_OK) {
				fatal_error("deflateReset returned %d\n", result);
			}
		}
		force = 0;
	}
	output_stream.next_in = buffer.data;
	buffer.size = 0;
}

void event_state(uint32_t cycle, serialize_buffer *state)
{
	if (!fully_active) {
		last = cycle;
	}
	uint8_t header[] = {
		EVENT_STATE << 4, last >> 24, last >> 16, last >> 8, last,
		last_word_address >> 16, last_word_address >> 8, last_word_address,
		last_byte_address >> 8, last_byte_address,
		state->size >> 16, state->size >> 8, state->size
	};
	uint8_t sent_system_start = 0;
	for (int i = 0; i < num_remotes; i++)
	{
		if (!remotes[i].send_progress) {
			if (send_all(remotes[i].sock, system_start, system_start_size, 0) == system_start_size) {
				sent_system_start = 1;
			} else {
				socket_close(remotes[i].sock);
				remotes[i] = remotes[num_remotes-1];
				num_remotes--;
				i--;
			}
		}
	}
	if (sent_system_start) {
		if (fully_active) {
			if (multi_count) {
				finish_multi();
			}
			//full flush is needed so new and old clients can share a stream
			deflate_flush(1);
		}
		save_buffer8(&buffer, header, sizeof(header));
		save_buffer8(&buffer, state->data, state->size);
		size_t old_compressed_size = output_stream.next_out - compressed;
		deflate_flush(1);
		size_t state_size = output_stream.next_out - compressed - old_compressed_size;
		for (int i = 0; i < num_remotes; i++) {
			if (!remotes[i].send_progress) {
				if (send_all(remotes[i].sock, compressed + old_compressed_size, state_size, 0) == state_size) {
					remotes[i].send_progress = compressed + old_compressed_size;
					socket_blocking(remotes[i].sock, 0);
					int flag = 1;
					setsockopt(remotes[i].sock, IPPROTO_TCP, TCP_NODELAY, (const char *)&flag, sizeof(flag));
					fully_active = 1;
				} else {
					socket_close(remotes[i].sock);
					remotes[i] = remotes[num_remotes-1];
					num_remotes--;
					i--;
				}
			}
		}
		output_stream.next_out = compressed + old_compressed_size;
		output_stream.avail_out = compressed_storage - old_compressed_size;
	}
}

void event_flush(uint32_t cycle)
{
	if (!active) {
		return;
	}
	if (fully_active) {
		event_header(EVENT_FLUSH, cycle);
		last = cycle;
		
		deflate_flush(0);
	}
	if (event_file) {
		fwrite(compressed, 1, output_stream.next_out - compressed, event_file);
		fflush(event_file);
		output_stream.next_out = compressed;
		output_stream.avail_out = compressed_storage;
	} else if (listen_sock) {
		flush_socket();
		wrote_since_last_flush = 0;
	}
}

void event_soft_flush(uint32_t cycle)
{
	if (!fully_active || wrote_since_last_flush || event_file) {
		return;
	}
	event_header(EVENT_FLUSH, cycle);
	last = cycle;
	
	deflate_flush(0);
	flush_socket();
}

static void init_event_reader_common(event_reader *reader)
{
	reader->last_cycle = 0;
	reader->repeat_event = 0xFF;
	reader->storage = 512 * 1024;
	init_deserialize(&reader->buffer, malloc(reader->storage), reader->storage);
	reader->buffer.size = 0;
	memset(&reader->input_stream, 0, sizeof(reader->input_stream));
	
}

void init_event_reader(event_reader *reader, uint8_t *data, size_t size)
{
	reader->socket = 0;
	reader->last_cycle = 0;
	reader->repeat_event = 0xFF;
	init_event_reader_common(reader);
	uint8_t name_len = data[1];
	reader->buffer.size = name_len + 2;
	memcpy(reader->buffer.data, data, reader->buffer.size);
	reader->input_stream.next_in = data + reader->buffer.size;
	reader->input_stream.avail_in = size - reader->buffer.size;
	
	int result = inflateInit(&reader->input_stream);
	if (Z_OK != result) {
		fatal_error("inflateInit returned %d\n", result);
	}
	reader->input_stream.next_out = reader->buffer.data + reader->buffer.size;
	reader->input_stream.avail_out = reader->storage - reader->buffer.size;
	result = inflate(&reader->input_stream, Z_NO_FLUSH);
	if (Z_OK != result && Z_STREAM_END != result) {
		fatal_error("inflate returned %d\n", result);
	}
	reader->buffer.size = reader->input_stream.next_out - reader->buffer.data;
}

void init_event_reader_tcp(event_reader *reader, char *address, char *port)
{
	struct addrinfo request, *result;
	socket_init();
	memset(&request, 0, sizeof(request));
	request.ai_family = AF_INET;
	request.ai_socktype = SOCK_STREAM;
	request.ai_flags = AI_PASSIVE;
	getaddrinfo(address, port, &request, &result);
	
	reader->socket = socket(result->ai_family, result->ai_socktype, result->ai_protocol);
	if (reader->socket < 0) {
		fatal_error("Failed to create socket for event log connection to %s:%s\n", address, port);
	}
	if (connect(reader->socket, result->ai_addr, result->ai_addrlen) < 0) {
		fatal_error("Failed to connect to %s:%s for event log stream\n", address, port);
	}
	
	init_event_reader_common(reader);
	reader->socket_buffer_size = 256 * 1024;
	reader->socket_buffer = malloc(reader->socket_buffer_size);
	
	while(reader->buffer.size < 3 || reader->buffer.size < 3 + reader->buffer.data[2])
	{
		int bytes = recv(reader->socket, reader->buffer.data + reader->buffer.size, reader->storage - reader->buffer.size, 0);
		if (bytes < 0) {
			fatal_error("Failed to receive system init from %s:%s\n", address, port);
		}
		reader->buffer.size += bytes;
	}
	size_t init_msg_len = 3 + reader->buffer.data[2];
	memcpy(reader->socket_buffer, reader->buffer.data + init_msg_len, reader->buffer.size - init_msg_len);
	reader->input_stream.next_in = reader->socket_buffer;
	reader->input_stream.avail_in = reader->buffer.size - init_msg_len;
	reader->buffer.size = init_msg_len;
	int res = inflateInit(&reader->input_stream);
	if (Z_OK != res) {
		fatal_error("inflateInit returned %d\n", res);
	}
	reader->input_stream.next_out = reader->buffer.data + init_msg_len;
	reader->input_stream.avail_out = reader->storage - init_msg_len;
	res = inflate(&reader->input_stream, Z_NO_FLUSH);
	if (Z_OK != res && Z_BUF_ERROR != res) {
		fatal_error("inflate returned %d in init_event_reader_tcp\n", res);
	}
	int flag = 1;
	setsockopt(reader->socket, IPPROTO_TCP, TCP_NODELAY, (const char *)&flag, sizeof(flag));
}

static void read_from_socket(event_reader *reader)
{
	if (reader->socket_buffer_size - reader->input_stream.avail_in < 128 * 1024) {
		reader->socket_buffer_size *= 2;
		uint8_t *new_buf = malloc(reader->socket_buffer_size);
		memcpy(new_buf, reader->input_stream.next_in, reader->input_stream.avail_in);
		free(reader->socket_buffer);
		reader->socket_buffer = new_buf;
		reader->input_stream.next_in = new_buf;
	} else if (
		reader->input_stream.next_in - reader->socket_buffer >= reader->input_stream.avail_in 
		&& reader->input_stream.next_in - reader->socket_buffer + reader->input_stream.avail_in >= reader->socket_buffer_size/2
	) {
		memmove(reader->socket_buffer, reader->input_stream.next_in, reader->input_stream.avail_in);
		reader->input_stream.next_in = reader->socket_buffer;
	}
	uint8_t *space_start = reader->input_stream.next_in + reader->input_stream.avail_in;
	size_t space = (reader->socket_buffer + reader->socket_buffer_size) - space_start;
	int bytes = recv(reader->socket, space_start, space, 0);
	if (bytes >= 0) {
		reader->input_stream.avail_in += bytes;
	} else if (!socket_error_is_wouldblock()) {
		fatal_error("Connection closed, error = %X\n", socket_last_error());
	}
}

static void inflate_flush(event_reader *reader)
{
	if (reader->buffer.cur_pos > reader->storage / 2) {
		memmove(reader->buffer.data, reader->buffer.data + reader->buffer.cur_pos, reader->buffer.size - reader->buffer.cur_pos);
		reader->buffer.size -= reader->buffer.cur_pos;
		reader->buffer.cur_pos = 0;
		reader->input_stream.next_out = reader->buffer.data + reader->buffer.size;
		reader->input_stream.avail_out = reader->storage - reader->buffer.size;
	}
	int result = inflate(&reader->input_stream, Z_SYNC_FLUSH);
	if (Z_OK != result && Z_STREAM_END != result) {
		fatal_error("inflate returned %d\n", result);
	}
	reader->buffer.size = reader->input_stream.next_out - reader->buffer.data;
	if (result == Z_STREAM_END && (reader->socket || reader->input_stream.avail_in)) {
		inflateReset(&reader->input_stream);
		if (reader->input_stream.avail_in) {
			inflate_flush(reader);
		}
	}
	
}

void reader_ensure_data(event_reader *reader, size_t bytes)
{
	if (reader->buffer.size - reader->buffer.cur_pos < bytes) {
		if (reader->input_stream.avail_in) {
			inflate_flush(reader);
		}
		if (reader->socket) {
			while (reader->buffer.size - reader->buffer.cur_pos < bytes) {
				read_from_socket(reader);
				inflate_flush(reader);
			}
		}
	}
}

uint8_t reader_next_event(event_reader *reader, uint32_t *cycle_out)
{
	if (reader->repeat_remaining) {
		reader->repeat_remaining--;
		*cycle_out = reader->last_cycle + reader->repeat_delta;
		reader->last_cycle = *cycle_out;
		return reader->repeat_event;
	}
	reader_ensure_data(reader, 1);
	uint8_t header = load_int8(&reader->buffer);
	uint8_t ret;
	uint32_t delta;
	uint8_t multi_start = 0;
	if ((header & 0xF0) == (EVENT_MULTI << 4)) {
		reader->repeat_remaining = (header & 0xF) + 1;
		multi_start = 1;
		reader_ensure_data(reader, 1);
		header = load_int8(&reader->buffer);
	}
	if ((header & 0xF0) < FORMAT_3BYTE) {
		delta = (header & 0xF) + 16;
		ret = header >> 4;
	} else if ((header & 0xF0) == FORMAT_3BYTE) {
		reader_ensure_data(reader, 2);
		delta = load_int16(&reader->buffer);
		ret = header & 0xF;
	} else {
		reader_ensure_data(reader, 3);
		delta = load_int8(&reader->buffer) << 16;
		//sign extend 24-bit delta to 32-bit
		if (delta & 0x800000) {
			delta |= 0xFF000000;
		}
		delta |= load_int16(&reader->buffer);
		ret = header & 0xF;
	}
	if (multi_start) {
		reader->repeat_event = ret;
		reader->repeat_delta = delta;
	}
	*cycle_out = reader->last_cycle + delta;
	reader->last_cycle = *cycle_out;
	if (ret == EVENT_ADJUST) {
		reader_ensure_data(reader, 4);
		size_t old_pos = reader->buffer.cur_pos;
		uint32_t adjust = load_int32(&reader->buffer);
		reader->buffer.cur_pos = old_pos;
		reader->last_cycle -= adjust;
	} else if (ret == EVENT_STATE) {
		reader_ensure_data(reader, 8);
		reader->last_cycle = load_int32(&reader->buffer);
		reader->last_word_address = load_int8(&reader->buffer) << 16;
		reader->last_word_address |= load_int16(&reader->buffer);
		reader->last_byte_address = load_int16(&reader->buffer);
	}
	return ret;
}

uint8_t reader_system_type(event_reader *reader)
{
	return load_int8(&reader->buffer);
}

void reader_send_gamepad_event(event_reader *reader, uint8_t pad, uint8_t button, uint8_t down)
{
	uint8_t buffer[] = {down ? CMD_GAMEPAD_DOWN : CMD_GAMEPAD_UP, pad << 5 | button};
	//TODO: Deal with the fact that we're not in blocking mode so this may not actually send all
	//if the buffer is full
	send_all(reader->socket, buffer, sizeof(buffer), 0);
}
