#include <string.h>
#include <stdlib.h>
#include "saves.h"
#include "util.h"

#ifdef _WIN32
#define localtime_r(a,b) localtime(a)
#include <windows.h>
#endif
//0123456789012345678901234678
//Slot N - December 31st, XXXX
#define MAX_DESC_SIZE 40

char *get_slot_name(system_header *system, uint32_t slot_index, char *ext)
{
	if (!system->save_dir) {
		return NULL;
	}
	char *fname;
	if (slot_index < 10) {
		size_t name_len = strlen("slot_N.") + strlen(ext) + 1;
		fname = malloc(name_len);
		snprintf(fname, name_len, "slot_%d.%s", slot_index, ext);
	} else {
		size_t name_len = strlen("quicksave.") + strlen(ext) + 1;
		fname = malloc(name_len);
		snprintf(fname, name_len, "quicksave.%s", ext);
	}
	char const *parts[] = {system->save_dir, PATH_SEP, fname};
	char *ret = alloc_concat_m(3, parts);
	free(fname);
	return ret;
}

save_slot_info *get_slot_info(system_header *system, uint32_t *num_out)
{
	save_slot_info *dst = calloc(11, sizeof(save_slot_info));
	time_t modtime;
	struct tm ltime;
	for (uint32_t i = 0; i <= QUICK_SAVE_SLOT; i++)
	{
		char * cur = dst[i].desc = malloc(MAX_DESC_SIZE);
		char * fname = get_slot_name(system, i, "state");
		modtime = get_modification_time(fname);
		free(fname);
		if (!modtime && system->type == SYSTEM_GENESIS) {
			fname = get_slot_name(system, i, "gst");
			modtime = get_modification_time(fname);
			free(fname);
		}
		if (i == QUICK_SAVE_SLOT) {
			cur += snprintf(cur, MAX_DESC_SIZE, "Quick - ");
		} else {
			cur += snprintf(cur, MAX_DESC_SIZE, "Slot %d - ", i);
		}
		if (modtime) {
			strftime(cur, MAX_DESC_SIZE - (cur - dst->desc), "%c", localtime_r(&modtime, &ltime));
		} else {
			strcpy(cur, "EMPTY");
		}
		dst[i].modification_time = modtime;
	}
	*num_out = QUICK_SAVE_SLOT + 1;
	return dst;
}

void free_slot_info(save_slot_info *slots)
{
	if (!slots) {
		return;
	}
	for (uint32_t i = 0; i <= QUICK_SAVE_SLOT; i++)
	{
		free(slots[i].desc);
	}
	free(slots);
}
