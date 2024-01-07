#ifndef EMU2413_STATE_H
#define EMU2413_STATE_H

#include "emu2413.h"

typedef struct {
	e_int32 feedback;
	e_int32 output[2];
	e_uint32 phase;
	e_uint32 pgout;
	e_int32 eg_mode;
	e_uint32 eg_phase;
	e_uint32 eg_dphase;
	e_uint32 egout;
} OPLL_SLOT_STATE;

typedef struct {
	e_uint32 pm_phase;
	e_int32 am_phase;
	OPLL_SLOT_STATE slot[6 * 2];
} OPLL_STATE;

#ifdef __cplusplus
extern "C"
{
#endif

int OPLL_serialize_size();
void OPLL_serialize(const OPLL * opll, OPLL_STATE* state);
void OPLL_deserialize(OPLL * opll, const OPLL_STATE* state);
void OPLL_state_byteswap(OPLL_STATE *state);

#ifdef __cplusplus
}
#endif

#endif