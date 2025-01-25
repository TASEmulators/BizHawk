#include <string.h>
#include "system.h"
#include "genesis.h"
#include "gen_player.h"
#include "sms.h"

uint8_t safe_cmp(char *str, long offset, uint8_t *buffer, long filesize)
{
	long len = strlen(str);
	return filesize >= offset+len && !memcmp(str, buffer + offset, len);
}

system_type detect_system_type(system_media *media)
{
	if (safe_cmp("SEGA", 0x100, media->buffer, media->size)) {
		//TODO: Differentiate between vanilla Genesis and Sega CD/32X games
		return SYSTEM_GENESIS;
	}
	if (safe_cmp("TMR SEGA", 0x1FF0, media->buffer, media->size)
		|| safe_cmp("TMR SEGA", 0x3FF0, media->buffer, media->size)
		|| safe_cmp("TMR SEGA", 0x7FF0, media->buffer, media->size)
	) {
		return SYSTEM_SMS;
	}
	if (safe_cmp("BLSTEL\x02", 0, media->buffer, media->size)) {
		uint8_t *buffer = media->buffer;
		if (media->size > 9 && buffer[7] == 0) {
			return buffer[8] + 1;
		}
	}
		
	
	//TODO: Detect Jaguar ROMs here
	
	//Header based detection failed, examine filename for clues
	if (media->extension) {
		if (!strcmp("md", media->extension) || !strcmp("gen", media->extension)) {
			return SYSTEM_GENESIS;
		}
		if (!strcmp("sms", media->extension)) {
			return SYSTEM_SMS;
		}
		if (!strcmp("j64", media->extension)) {
			return SYSTEM_JAGUAR;
		}
	}
	
	//More certain checks failed, look for a valid 68K reset vector
	if (media->size >= 8) {
		char *rom = media->buffer;
		uint32_t reset = rom[5] << 16 | rom[6] << 8 | rom[7];
		if (!(reset & 1) && reset < media->size) {
			//we have a valid looking reset vector, assume it's a Genesis ROM
			return SYSTEM_GENESIS;
		}
	}
	return SYSTEM_UNKNOWN;
}

system_header *alloc_config_system(system_type stype, system_media *media, uint32_t opts, uint8_t force_region)
{
	void *lock_on = NULL;
	uint32_t lock_on_size = 0;
	if (media->chain) {
		lock_on = media->chain->buffer;
		lock_on_size = media->chain->size;
	}
	switch (stype)
	{
	case SYSTEM_GENESIS:
		return &(alloc_config_genesis(media->buffer, media->size, lock_on, lock_on_size, opts, force_region))->header;
	case SYSTEM_GENESIS_PLAYER:
		return &(alloc_config_gen_player(media->buffer, media->size))->header;
#ifndef NO_Z80
	case SYSTEM_SMS:
		return &(alloc_configure_sms(media, opts, force_region))->header;
#endif
	default:
		return NULL;
	}
}

system_header *alloc_config_player(system_type stype, event_reader *reader)
{
	switch(stype)
	{
	case SYSTEM_GENESIS:
		return &(alloc_config_gen_player_reader(reader))->header;
	}
	return NULL;
}

void system_request_exit(system_header *system, uint8_t force_release)
{
	system->force_release = force_release;
	system->request_exit(system);
}
