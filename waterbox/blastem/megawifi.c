#include <stdlib.h>
#include <stdint.h>
#include <string.h>
#include <sys/types.h>
#ifdef _WIN32
#define WINVER 0x501
#include <winsock2.h>
#include <ws2tcpip.h>
#include <sys/param.h>
#else
#include <sys/socket.h>
#include <arpa/inet.h>
#include <unistd.h>
#include <netinet/in.h>
#include <netdb.h>
#endif
#include <errno.h>
#include <fcntl.h>
#include <time.h>
#include "genesis.h"
#include "net.h"
#include "util.h"

#if defined(__APPLE__) || !defined(_WIN32) || defined(__MINGW32__)
#  if BYTE_ORDER == LITTLE_ENDIAN
#define htobe64(val)   ((((uint64_t)htonl((val)&0xFFFFFFFF))<<32) | htonl((val)>>32))
#  else
#define htobe64(val)	(val)
#  endif
#endif

enum {
	TX_IDLE,
	TX_LEN1,
	TX_LEN2,
	TX_PAYLOAD,
	TX_WAIT_ETX
};
#define STX 0x7E
#define ETX 0x7E
#define MAX_RECV_SIZE 1460

#define E(N) N
enum {
#include "mw_commands.c"
	CMD_ERROR = 255
};
#undef E
#define E(N) #N
static const char *cmd_names[] = {
#include "mw_commands.c"
	[255] = "CMD_ERROR"
};

#ifndef MSG_NOSIGNAL
#define MSG_NOSIGNAL 0
#endif

enum mw_state {
	STATE_IDLE=1,
	STATE_AP_JOIN,
	STATE_SCAN,
	STATE_READY,
	STATE_TRANSPARENT
};

enum {
	SOCKST_NONE = 0,
	SOCKST_TCP_LISTEN,
	SOCKST_TCP_EST,
	SOCKST_UDP_READY
};

// TCP/UDP address message
struct mw_addr_msg {
	char dst_port[6];
	char src_port[6];
	uint8_t channel;
	char host[];
};

#define FLAG_ONLINE 

typedef struct {
	uint32_t transmit_bytes;
	uint32_t expected_bytes;
	uint32_t receive_bytes;
	uint32_t receive_read;
	int      sock_fds[15];
	uint16_t channel_flags;
	uint8_t  channel_state[15];
	uint8_t  scratchpad;
	uint8_t  transmit_channel;
	uint8_t  transmit_state;
	uint8_t  module_state;
	uint8_t  flags;
	uint8_t  transmit_buffer[4096];
	uint8_t  receive_buffer[4096];
	struct sockaddr_in remote_addr[15];	// Needed for UDP sockets
} megawifi;

static megawifi *get_megawifi(void *context)
{
	m68k_context *m68k = context;
	genesis_context *gen = m68k->system;
	if (!gen->extra) {
		socket_init();
		gen->extra = calloc(1, sizeof(megawifi));
		megawifi *mw = gen->extra;
		mw->module_state = STATE_IDLE;
		mw->flags = 0xE0; // cfg_ok, dt_ok, online
		for (int i = 0; i < 15; i++) {
			mw->sock_fds[i] = -1;
		}
	}
	return gen->extra;
}

static void mw_putc(megawifi *mw, uint8_t v)
{
	if (mw->receive_bytes == sizeof(mw->receive_buffer)) {
		return;
	}
	mw->receive_buffer[mw->receive_bytes++] = v;
}

static void mw_set(megawifi *mw, uint8_t val, uint32_t count)
{
	if (count + mw->receive_bytes > sizeof(mw->receive_buffer)) {
		count = sizeof(mw->receive_buffer) - mw->receive_bytes;
	}
	memset(mw->receive_buffer + mw->receive_bytes, val, count);
	mw->receive_bytes += count;
}

static void mw_copy(megawifi *mw, const uint8_t *src, uint32_t count)
{
	if (count + mw->receive_bytes > sizeof(mw->receive_buffer)) {
		count = sizeof(mw->receive_buffer) - mw->receive_bytes;
	}
	memcpy(mw->receive_buffer + mw->receive_bytes, src, count);
	mw->receive_bytes += count;
}

static void mw_puts(megawifi *mw, const char *s)
{
	size_t len = strlen(s);
	mw_copy(mw, (uint8_t*)s, len);
}

static void udp_recv(megawifi *mw, uint8_t idx)
{
	ssize_t recvd;
	int s = mw->sock_fds[idx];
	struct sockaddr_in remote;
	socklen_t addr_len = sizeof(struct sockaddr_in);

	if (mw->remote_addr[idx].sin_addr.s_addr != htonl(INADDR_ANY)) {
		// Receive only from specified address
		recvd = recvfrom(s, (char*)mw->receive_buffer + 3, MAX_RECV_SIZE, 0,
				(struct sockaddr*)&remote, &addr_len);
		if (recvd > 0) {
			if (remote.sin_addr.s_addr != mw->remote_addr[idx].sin_addr.s_addr) {
				printf("Discarding UDP packet from unknown addr %s:%d\n",
						inet_ntoa(remote.sin_addr), ntohs(remote.sin_port));
				recvd = 0;
			}
		}
	} else {
		// Reuse mode, data is preceded by remote IPv4 and port
		recvd = recvfrom(s, (char*)mw->receive_buffer + 9, MAX_RECV_SIZE - 6,
				0, (struct sockaddr*)&remote, &addr_len);
		if (recvd > 0) {
			mw->receive_buffer[3] = remote.sin_addr.s_addr;
			mw->receive_buffer[4] = remote.sin_addr.s_addr>>8;
			mw->receive_buffer[5] = remote.sin_addr.s_addr>>16;
			mw->receive_buffer[6] = remote.sin_addr.s_addr>>24;
			mw->receive_buffer[7] = remote.sin_port;
			mw->receive_buffer[8] = remote.sin_port>>8;
			recvd += 6;
		}
	}

	if (recvd > 0) {
		mw_putc(mw, STX);
		mw_putc(mw, (recvd >> 8) | ((idx+1) << 4));
		mw_putc(mw, recvd);
		mw->receive_bytes += recvd;
		mw_putc(mw, ETX);
		//should this set the channel flag?
	} else if (recvd < 0 && !socket_error_is_wouldblock()) {
		socket_close(mw->sock_fds[idx]);
		mw->channel_state[idx] = SOCKST_NONE;
		mw->channel_flags |= 1 << (idx + 1);
	}
}

static void udp_send(megawifi *mw, uint8_t idx)
{
	struct sockaddr_in remote;
	int s = mw->sock_fds[idx];
	int sent;
	char *data = (char*)mw->transmit_buffer;

	if (mw->remote_addr[idx].sin_addr.s_addr != htonl(INADDR_ANY)) {
		sent = sendto(s, data, mw->transmit_bytes, 0, (struct sockaddr*)&mw->remote_addr[idx],
				sizeof(struct sockaddr_in));
	} else {
		// Reuse mode, extract address from leading bytes
		// NOTE: mw->remote_addr[idx].sin_addr.s_addr == INADDR_ANY
		remote.sin_addr.s_addr = *((int32_t*)data);
		remote.sin_port = *((int16_t*)(data + 4));
		remote.sin_family = AF_INET;
		memset(remote.sin_zero, 0, sizeof(remote.sin_zero));
		sent = sendto(s, data + 6, mw->transmit_bytes - 6, 0, (struct sockaddr*)&remote,
				sizeof(struct sockaddr_in)) + 6;
	}
	if (sent < 0 && !socket_error_is_wouldblock()) {
		socket_close(s);
		mw->sock_fds[idx] = -1;
		mw->channel_state[idx] = SOCKST_NONE;
		mw->channel_flags |= 1 << (idx + 1);
	} else if (sent < mw->transmit_bytes) {
		//TODO: save this data somewhere so it can be sent in poll_socket
		printf("Sent %d bytes on channel %d, but %d were requested\n", sent, idx + 1, mw->transmit_bytes);
	}
}

static void poll_socket(megawifi *mw, uint8_t channel)
{
	if (mw->sock_fds[channel] < 0) {
		return;
	}
	if (mw->channel_state[channel] == SOCKST_TCP_LISTEN) {
		int res = accept(mw->sock_fds[channel], NULL, NULL);
		if (res >= 0) {
			socket_close(mw->sock_fds[channel]);
			socket_blocking(res, 0);
			mw->sock_fds[channel] = res;
			mw->channel_state[channel] = SOCKST_TCP_EST;
			mw->channel_flags |= 1 << (channel + 1);
		} else if (errno != EAGAIN && errno != EWOULDBLOCK) {
			socket_close(mw->sock_fds[channel]);
			mw->channel_state[channel] = SOCKST_NONE;
			mw->channel_flags |= 1 << (channel + 1);
		}
	} else if (mw->channel_state[channel] == SOCKST_TCP_EST && mw->receive_bytes < (sizeof(mw->receive_buffer) - 4)) {
		size_t max = sizeof(mw->receive_buffer) - 4 - mw->receive_bytes;
		if (max > MAX_RECV_SIZE) {
			max = MAX_RECV_SIZE;
		}
		int bytes = recv(mw->sock_fds[channel], (char*)(mw->receive_buffer + mw->receive_bytes + 3), max, 0);
		if (bytes > 0) {
			mw_putc(mw, STX);
			mw_putc(mw, bytes >> 8 | (channel+1) << 4);
			mw_putc(mw, bytes);
			mw->receive_bytes += bytes;
			mw_putc(mw, ETX);
			//should this set the channel flag?
		} else if (bytes < 0 && !socket_error_is_wouldblock()) {
			socket_close(mw->sock_fds[channel]);
			mw->channel_state[channel] = SOCKST_NONE;
			mw->channel_flags |= 1 << (channel + 1);
		}
	} else if (mw->channel_state[channel] == SOCKST_UDP_READY && !mw->receive_bytes) {
		udp_recv(mw, channel);
	}
}

static void poll_all_sockets(megawifi *mw)
{
	for (int i = 0; i < 15; i++)
	{
		poll_socket(mw, i);
	}
}


static void start_reply(megawifi *mw, uint8_t cmd)
{
	mw_putc(mw, STX);
	//reserve space for length
	mw->receive_bytes += 2;
	//cmd
	mw_putc(mw, 0);
	mw_putc(mw, cmd);
	//reserve space for length
	mw->receive_bytes += 2;
}

static void end_reply(megawifi *mw)
{
	uint32_t len = mw->receive_bytes - 3;
	//LSD packet length
	mw->receive_buffer[1] = len >> 8;
	mw->receive_buffer[2] = len;
	//command length
	len -= 4;
	mw->receive_buffer[5] = len >> 8;
	mw->receive_buffer[6] = len;
	mw_putc(mw, ETX);
}

static void cmd_ap_cfg_get(megawifi *mw)
{
	char ssid[32] = {0};
	char pass[64] = {0};
	uint8_t slot = mw->transmit_buffer[4];

	sprintf(ssid, "BLASTEM! SSID %d", slot + 1);
	sprintf(pass, "BLASTEM! PASS %d", slot + 1);
	start_reply(mw, CMD_OK);
	mw_putc(mw, slot);
	mw_putc(mw, 7);	/// 11bgn
	mw_copy(mw, (uint8_t*)ssid, 32);
	mw_copy(mw, (uint8_t*)pass, 64);
	end_reply(mw);
}

static void cmd_ip_cfg_get(megawifi *mw)
{
	uint32_t ipv4s[5] = {0};

	start_reply(mw, CMD_OK);
	mw_putc(mw, mw->transmit_buffer[4]);
	mw_putc(mw, 0);
	mw_putc(mw, 0);
	mw_putc(mw, 0);
	mw_copy(mw, (uint8_t*)ipv4s, sizeof(ipv4s));
	end_reply(mw);
}

static void cmd_tcp_con(megawifi *mw, uint32_t size)
{
	struct mw_addr_msg *addr = (struct mw_addr_msg*)(mw->transmit_buffer + 4);
	struct addrinfo hints;
	struct addrinfo *res = NULL;
	int s;
	int err;

	uint8_t channel = addr->channel;
	if (!channel || channel > 15 || mw->sock_fds[channel - 1] >= 0) {
		start_reply(mw, CMD_ERROR);
		end_reply(mw);
		return;
	}
	channel--;

	memset(&hints, 0, sizeof(hints));
	hints.ai_family = AF_INET;
#ifndef _WIN32
	hints.ai_flags = AI_NUMERICSERV;
#endif
	hints.ai_socktype = SOCK_STREAM;

	if ((err = getaddrinfo(addr->host, addr->dst_port, &hints, &res)) != 0) {
		printf("getaddrinfo failed: %s\n", gai_strerror(err));
		start_reply(mw, CMD_ERROR);
		end_reply(mw);
		return;
	}

	s = socket(AF_INET, SOCK_STREAM, 0);
	if (s < 0) {
		goto err;
	}

	// Should this be handled in a separate thread to avoid blocking emulation?
	if (connect(s, res->ai_addr, res->ai_addrlen) != 0) {
		goto err;
	}

	socket_blocking(s, 0);
	mw->sock_fds[channel] = s;
	mw->channel_state[channel] = SOCKST_TCP_EST;
	mw->channel_flags |= 1 << (channel + 1);
	printf("Connection established on ch %d with %s:%s\n", channel + 1,
			addr->host, addr->dst_port);

	if (res) {
		freeaddrinfo(res);
	}
	start_reply(mw, CMD_OK);
	end_reply(mw);
	return;

err:
	freeaddrinfo(res);
	printf("Connection to %s:%s failed, %s\n", addr->host, addr->dst_port, strerror(errno));
	start_reply(mw, CMD_ERROR);
	end_reply(mw);
}

static void cmd_close(megawifi *mw)
{
	int channel = mw->transmit_buffer[4] - 1;

	if (channel >= 15 || mw->sock_fds[channel] < 0) {
		start_reply(mw, CMD_ERROR);
		end_reply(mw);
		return;
	}

	socket_close(mw->sock_fds[channel]);
	mw->sock_fds[channel] = -1;
	mw->channel_state[channel] = SOCKST_NONE;
	mw->channel_flags |= 1 << (channel + 1);
	start_reply(mw, CMD_OK);
	end_reply(mw);
}

static void cmd_udp_set(megawifi *mw)
{
	struct mw_addr_msg *addr = (struct mw_addr_msg*)(mw->transmit_buffer + 4);
	unsigned int local_port, remote_port;
	int s;
	struct addrinfo *raddr;
	struct addrinfo hints;
	struct sockaddr_in local;
	int err;

	uint8_t channel = addr->channel;
	if (!channel || channel > 15 || mw->sock_fds[channel - 1] >= 0) {
		goto err;
	}
	channel--;
	local_port = atoi(addr->src_port);
	remote_port = atoi(addr->dst_port);

	if ((s = socket(PF_INET, SOCK_DGRAM, 0)) < 0) {
		printf("Datagram socket creation failed\n");
		goto err;
	}

	memset(local.sin_zero, 0, sizeof(local.sin_zero));
	local.sin_family = AF_INET;
	local.sin_addr.s_addr = htonl(INADDR_ANY);
	local.sin_port = htons(local_port);
	if (remote_port && addr->host[0]) {
		// Communication with remote peer
		printf("Set UDP ch %d, port %d to addr %s:%d\n", addr->channel,
				local_port, addr->host, remote_port);

		memset(&hints, 0, sizeof(hints));
		hints.ai_family = AF_INET;
#ifndef _WIN32
		hints.ai_flags = AI_NUMERICSERV;
#endif
		hints.ai_socktype = SOCK_DGRAM;

		if ((err = getaddrinfo(addr->host, addr->dst_port, &hints, &raddr)) != 0) {
			printf("getaddrinfo failed: %s\n", gai_strerror(err));
			goto err;
		}
		mw->remote_addr[channel] = *((struct sockaddr_in*)raddr->ai_addr);
		freeaddrinfo(raddr);
	} else if (local_port) {
		// Server in reuse mode
		printf("Set UDP ch %d, src port %d\n", addr->channel, local_port);
		mw->remote_addr[channel] = local;
	} else {
		printf("Invalid UDP socket data\n");
		goto err;
	}

	if (bind(s, (struct sockaddr*)&local, sizeof(struct sockaddr_in)) < 0) {
		printf("bind to port %d failed\n", local_port);
		goto err;
	}

	socket_blocking(s, 0);
	mw->sock_fds[channel] = s;
	mw->channel_state[channel] = SOCKST_UDP_READY;
	mw->channel_flags |= 1 << (channel + 1);

	start_reply(mw, CMD_OK);
	end_reply(mw);

	return;

err:
	start_reply(mw, CMD_ERROR);
	end_reply(mw);
}

#define AVATAR_BYTES	(32 * 48 / 2)
static void cmd_gamertag_get(megawifi *mw)
{
	uint32_t id = htonl(1);
	char buf[AVATAR_BYTES];

	start_reply(mw, CMD_OK);
	// TODO Get items from config file
	mw_copy(mw, (uint8_t*)&id, 4);
	strncpy(buf, "doragasu on Blastem!", 32);
	mw_copy(mw, (uint8_t*)buf, 32);
	strncpy(buf, "My cool password", 32);
	mw_copy(mw, (uint8_t*)buf, 32);
	strncpy(buf, "All your WiFi are belong to me!", 32);
	mw_copy(mw, (uint8_t*)buf, 32);
	memset(buf, 0, 64); // Telegram token
	mw_copy(mw, (uint8_t*)buf, 64);
	mw_copy(mw, (uint8_t*)buf, AVATAR_BYTES); // Avatar tiles
	mw_copy(mw, (uint8_t*)buf, 32); // Avatar palette
	end_reply(mw);
}

static void cmd_hrng_get(megawifi *mw)
{
	uint16_t len = (mw->transmit_buffer[4]<<8) + mw->transmit_buffer[5];
	if (len > (MAX_RECV_SIZE - 4)) {
		start_reply(mw, CMD_ERROR);
		end_reply(mw);
		return;
	}
	// Pseudo-random, but who cares
	start_reply(mw, CMD_OK);
	srand(time(NULL));
	for (uint16_t i = 0; i < len; i++) {
		mw_putc(mw, rand());
	}
	end_reply(mw);
}

static void cmd_datetime(megawifi *mw)
{
	start_reply(mw, CMD_OK);
#ifdef _WIN32
	__time64_t t = _time64(NULL);
	int64_t t_be = htobe64(t);
	mw_copy(mw, (uint8_t*)&t_be, sizeof(int64_t));
	mw_puts(mw, _ctime64(&t));
#else
	time_t t = time(NULL);
	int64_t t_be = htobe64(t);
	mw_copy(mw, (uint8_t*)&t_be, sizeof(int64_t));
	mw_puts(mw, ctime(&t));
#endif

	mw_putc(mw, '\0');
	end_reply(mw);
}

static void process_command(megawifi *mw)
{
	uint32_t command = mw->transmit_buffer[0] << 8 | mw->transmit_buffer[1];
	uint32_t size = mw->transmit_buffer[2] << 8 | mw->transmit_buffer[3];
	if (size > mw->transmit_bytes - 4) {
		size = mw->transmit_bytes - 4;
	}
	int orig_receive_bytes = mw->receive_bytes;
	switch (command)
	{
	case CMD_VERSION:
		start_reply(mw, CMD_OK);
		mw_putc(mw, 1);
		mw_putc(mw, 3);
		mw_putc(mw, 0);
		mw_puts(mw, "blastem");
		mw_putc(mw, '\0');
		end_reply(mw);
		break;
	case CMD_ECHO:
		mw->receive_bytes = mw->transmit_bytes;
		memcpy(mw->receive_buffer, mw->transmit_buffer, mw->transmit_bytes);
		break;
	case CMD_AP_CFG_GET:
		cmd_ap_cfg_get(mw);
		break;
	case CMD_IP_CURRENT: {
		iface_info i;
		if (get_host_address(&i)) {
			start_reply(mw, CMD_OK);
			//config number and reserved bytes
			mw_set(mw, 0, 4);
			//ip
			mw_copy(mw, i.ip, sizeof(i.ip));
			//net mask
			mw_copy(mw, i.net_mask, sizeof(i.net_mask));
			//gateway guess
			mw_putc(mw, i.ip[0] & i.net_mask[0]);
			mw_putc(mw, i.ip[1] & i.net_mask[1]);
			mw_putc(mw, i.ip[2] & i.net_mask[2]);
			mw_putc(mw, (i.ip[3] & i.net_mask[3]) + 1);
			//dns
			static const uint8_t localhost[] = {127,0,0,1};
			mw_copy(mw, localhost, sizeof(localhost));
			mw_copy(mw, localhost, sizeof(localhost));
			
		} else {
			start_reply(mw, CMD_ERROR);
		}
		end_reply(mw);
		break;
	}
	case CMD_IP_CFG_GET:
		cmd_ip_cfg_get(mw);
		break;
	case CMD_DEF_AP_CFG_GET:
		start_reply(mw, CMD_OK);
		mw_putc(mw, 0);
		end_reply(mw);
		break;
	case CMD_AP_JOIN:
		mw->module_state = STATE_READY;
		start_reply(mw, CMD_OK);
		end_reply(mw);
		break;
	case CMD_TCP_CON:
		cmd_tcp_con(mw, size);
		break;
	case CMD_TCP_BIND:{
		if (size < 7){
			start_reply(mw, CMD_ERROR);
			end_reply(mw);
			break;
		}
		uint8_t channel = mw->transmit_buffer[10];
		if (!channel || channel > 15) {
			start_reply(mw, CMD_ERROR);
			end_reply(mw);
			break;
		}
		channel--;
		if (mw->sock_fds[channel] >= 0) {
			socket_close(mw->sock_fds[channel]);
		}
		mw->sock_fds[channel] = socket(AF_INET, SOCK_STREAM, 0);
		if (mw->sock_fds[channel] < 0) {
			start_reply(mw, CMD_ERROR);
			end_reply(mw);
			break;
		}
		int value = 1;
		setsockopt(mw->sock_fds[channel], SOL_SOCKET, SO_REUSEADDR, (char*)&value, sizeof(value));
		struct sockaddr_in bind_addr;
		memset(&bind_addr, 0, sizeof(bind_addr));
		bind_addr.sin_family = AF_INET;
		bind_addr.sin_port = htons(mw->transmit_buffer[8] << 8 | mw->transmit_buffer[9]);
		if (bind(mw->sock_fds[channel], (struct sockaddr *)&bind_addr, sizeof(bind_addr)) != 0) {
			socket_close(mw->sock_fds[channel]);
			mw->sock_fds[channel] = -1;
			start_reply(mw, CMD_ERROR);
			end_reply(mw);
			break;
		}
		int res = listen(mw->sock_fds[channel], 2);
		start_reply(mw, res ? CMD_ERROR : CMD_OK);
		if (res) {
			socket_close(mw->sock_fds[channel]);
			mw->sock_fds[channel] = -1;
		} else {
			mw->channel_flags |= 1 << (channel + 1);
			mw->channel_state[channel] = SOCKST_TCP_LISTEN;
			socket_blocking(mw->sock_fds[channel], 0);
		}
		end_reply(mw);
		break;
	}
	case CMD_CLOSE:
		cmd_close(mw);
		break;
	case CMD_UDP_SET:
		cmd_udp_set(mw);
		break;
	case CMD_SOCK_STAT: {
		uint8_t channel = mw->transmit_buffer[4];
		if (!channel || channel > 15) {
			start_reply(mw, CMD_ERROR);
			end_reply(mw);
			break;
		}
		mw->channel_flags &= ~(1 << channel);
		channel--;
		poll_socket(mw, channel);
		start_reply(mw, CMD_OK);
		mw_putc(mw, mw->channel_state[channel]);
		end_reply(mw);
		break;
	}
	case CMD_DATETIME:
		cmd_datetime(mw);
		break;
	case CMD_SYS_STAT:
		poll_all_sockets(mw);
		start_reply(mw, CMD_OK);
		mw_putc(mw, mw->module_state);
		mw_putc(mw, mw->flags);
		mw_putc(mw, mw->channel_flags >> 8);
		mw_putc(mw, mw->channel_flags);
		end_reply(mw);
		break;
	case CMD_GAMERTAG_GET:
		cmd_gamertag_get(mw);
		break;
	case CMD_LOG:
		start_reply(mw, CMD_OK);
		puts((char*)&mw->transmit_buffer[4]);
		end_reply(mw);
		break;
	case CMD_HRNG_GET:
		cmd_hrng_get(mw);
		break;
	case CMD_SERVER_URL_GET:
		start_reply(mw, CMD_OK);
		// FIXME: This should be get from config file
		mw_puts(mw, "doragasu.com");
		mw_putc(mw,'\0');
		end_reply(mw);
		break;
	default:
		printf("Unhandled MegaWiFi command %s(%d) with length %X\n", cmd_names[command], command, size);
		break;
	}
}

static void process_packet(megawifi *mw)
{
	if (mw->transmit_channel == 0) {
		process_command(mw);
	} else {
		uint8_t channel = mw->transmit_channel - 1;
		int channel_state = mw->channel_state[channel];
		int sock_fd = mw->sock_fds[channel];
		if (sock_fd >= 0 && channel_state == SOCKST_TCP_EST) {
			int sent = send(sock_fd, (char*)mw->transmit_buffer, mw->transmit_bytes, 0);
			if (sent < 0 && !socket_error_is_wouldblock()) {
				socket_close(sock_fd);
				mw->sock_fds[channel] = -1;
				mw->channel_state[channel] = SOCKST_NONE;
				mw->channel_flags |= 1 << mw->transmit_channel;
			} else if (sent < mw->transmit_bytes) {
				//TODO: save this data somewhere so it can be sent in poll_socket
				printf("Sent %d bytes on channel %d, but %d were requested\n", sent, mw->transmit_channel, mw->transmit_bytes);
			}
		} else if (sock_fd >= 0 && channel_state == SOCKST_UDP_READY) {
			udp_send(mw, channel);
		} else {
			printf("Unhandled receive of MegaWiFi data on channel %d\n", mw->transmit_channel);
		}
	}
	mw->transmit_bytes = mw->expected_bytes = 0;
}

void *megawifi_write_b(uint32_t address, void *context, uint8_t value)
{
	if (!(address & 1)) {
		return context;
	}
	megawifi *mw = get_megawifi(context);
	address = address >> 1 & 7;
	switch (address)
	{
	case 0:
		switch (mw->transmit_state)
		{
		case TX_IDLE:
			if (value == STX) {
				mw->transmit_state = TX_LEN1;
			}
			break;
		case TX_LEN1:
			mw->transmit_channel = value >> 4;
			mw->expected_bytes = value << 8 & 0xF00;
			mw->transmit_state = TX_LEN2;
			break;
		case TX_LEN2:
			mw->expected_bytes |= value;
			mw->transmit_state = TX_PAYLOAD;
			break;
		case TX_PAYLOAD:
			mw->transmit_buffer[mw->transmit_bytes++] = value;
			if (mw->transmit_bytes == mw->expected_bytes) {
				mw->transmit_state = TX_WAIT_ETX;
			}
			break;
		case TX_WAIT_ETX:
			if (value == ETX) {
				mw->transmit_state = TX_IDLE;
				process_packet(mw);
			}
			break;
		}
		break;
	case 7:
		mw->scratchpad = value;
		break;
	default:
		printf("Unhandled write to MegaWiFi UART register %X: %X\n", address, value);
	}
	return context;
}

void *megawifi_write_w(uint32_t address, void *context, uint16_t value)
{
	return megawifi_write_b(address | 1, context, value);
}

uint8_t megawifi_read_b(uint32_t address, void *context)
{
	
	if (!(address & 1)) {
		return 0xFF;
	}
	megawifi *mw = get_megawifi(context);
	address = address >> 1 & 7;
	switch (address)
	{
	case 0:
		poll_all_sockets(mw);
		if (mw->receive_read < mw->receive_bytes) {
			uint8_t ret = mw->receive_buffer[mw->receive_read++];
			if (mw->receive_read == mw->receive_bytes) {
				mw->receive_read = mw->receive_bytes = 0;
			}
			return ret;
		}
		return 0xFF;
	case 5:
		poll_all_sockets(mw);
		//line status
		return 0x60 | (mw->receive_read < mw->receive_bytes);
	case 7:
		return mw->scratchpad;
	default:
		printf("Unhandled read from MegaWiFi UART register %X\n", address);
		return 0xFF;
	}
}

uint16_t megawifi_read_w(uint32_t address, void *context)
{
	return 0xFF00 | megawifi_read_b(address | 1, context);
}
