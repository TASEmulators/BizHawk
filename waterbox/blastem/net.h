#ifndef NET_H_
#define NET_H_
#include <stdint.h>

typedef struct {
	uint8_t ip[4];
	uint8_t net_mask[4];
} iface_info;

uint8_t get_host_address(iface_info *out);

#endif //NET_H_
