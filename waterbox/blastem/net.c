#include <sys/types.h>
#include <ifaddrs.h>
#include <netinet/in.h>
#include "net.h"

static uint8_t is_loopback(struct sockaddr_in *addr)
{
	return (addr->sin_addr.s_addr & 0xFF) == 127;
}

static void format_address(uint8_t *dst, struct sockaddr_in *addr)
{
	long ip = addr->sin_addr.s_addr;
	dst[0] = ip;
	dst[1] = ip >> 8;
	dst[2] = ip >> 16;
	dst[3] = ip >> 24;
}

uint8_t get_host_address(iface_info *out)
{
#ifdef __ANDROID__
	//TODO: write an implementation for Android
	return 0;
#else
	struct ifaddrs *entries, *current, *localhost;
	if (getifaddrs(&entries)) {
		return 0;
	}
	
	for (current = entries; current; current = current->ifa_next)
	{
		if (current->ifa_addr && current->ifa_addr->sa_family == AF_INET) {
			struct sockaddr_in *addr = (struct sockaddr_in *)current->ifa_addr;
			if (is_loopback(addr)) {
				localhost = current;
			} else {
				break;
			}
		}
	}
	if (!current && localhost) {
		current = localhost;
	}
	uint8_t ret = 0;
	if (current) {
		ret = 1;
		format_address(out->ip, (struct sockaddr_in *)current->ifa_addr);
		format_address(out->net_mask, (struct sockaddr_in *)current->ifa_netmask);
	}
	freeifaddrs(entries);
	return ret;
#endif
}