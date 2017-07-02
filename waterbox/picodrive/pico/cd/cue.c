/*
 * cuefile handling
 * (C) notaz, 2008
 *
 * This work is licensed under the terms of MAME license.
 * See COPYING file in the top-level directory.
 */
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "cue.h"

#include "../pico_int.h"
// #define elprintf(w,f,...) printf(f "\n",##__VA_ARGS__);

#ifdef _MSC_VER
#define snprintf _snprintf
#endif
#ifdef __EPOC32__
#define snprintf(b,s,...) sprintf(b,##__VA_ARGS__)
#endif

#define BEGINS(buff,str) (strncmp(buff,str,sizeof(str)-1) == 0)

void cue_destroy(cue_data_t *data)
{
	int c;

	if (data == NULL) return;

	for (c = data->track_count; c > 0; c--)
		if (data->tracks[c].fname != NULL)
			free(data->tracks[c].fname);
	free(data);
}
