#ifndef SAVES_H_
#define SAVES_H_

#include <time.h>
#include <stdint.h>
#include "system.h"

#define QUICK_SAVE_SLOT 10
#define SERIALIZE_SLOT 11
#define EVENTLOG_SLOT 12

typedef struct {
	char   *desc;
	time_t modification_time;
} save_slot_info;

char *get_slot_name(system_header *system, uint32_t slot_index, char *ext);
save_slot_info *get_slot_info(system_header *system, uint32_t *num_out);
void free_slot_info(save_slot_info *slots);

#endif //SAVES_H_
