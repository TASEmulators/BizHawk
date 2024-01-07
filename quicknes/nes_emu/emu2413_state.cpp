#include "emu2413_state.h"
#include <stdint.h>

#ifdef __cplusplus
extern "C"
{
#endif

int OPLL_serialize_size()
{
	return sizeof(OPLL_STATE);
}

void OPLL_serialize(const OPLL * opll, OPLL_STATE* state)
{
	int i;

	state->pm_phase = opll->pm_phase;
	state->am_phase = opll->am_phase;

	for (i = 0; i < 12; i++)
	{
		OPLL_SLOT_STATE *slotState = &(state->slot[i]);
		const OPLL_SLOT *slot= &(opll->slot[i]);
		slotState->feedback = slot->feedback;
		slotState->output[0] = slot->output[0];
		slotState->output[1] = slot->output[1];
		slotState->phase = slot->phase;
		slotState->pgout = slot->pgout;
		slotState->eg_mode = slot->eg_mode;
		slotState->eg_phase = slot->eg_phase;
		slotState->eg_dphase = slot->eg_dphase;
		slotState->egout = slot->egout;
	}
}

#define BYTESWAP(xxxx) {uint32_t _temp = (uint32_t)(xxxx);\
((uint8_t*)&(xxxx))[0] = (uint8_t)((_temp) >> 24);\
((uint8_t*)&(xxxx))[1] = (uint8_t)((_temp) >> 16);\
((uint8_t*)&(xxxx))[2] = (uint8_t)((_temp) >> 8);\
((uint8_t*)&(xxxx))[3] = (uint8_t)((_temp) >> 0);\
}


#define SET(xxxx,yyyy) { if ((xxxx) != (yyyy)) {\
(xxxx) = (yyyy);\
}

void OPLL_deserialize(OPLL * opll, const OPLL_STATE* state)
{
	int i;

	opll->pm_phase = state->pm_phase;
	opll->am_phase = state->am_phase;

	for (i = 0; i < 12; i++)
	{
		const OPLL_SLOT_STATE *slotState = &(state->slot[i]);
		OPLL_SLOT *slot = &(opll->slot[i]);
		slot->feedback = slotState->feedback;
		slot->output[0] = slotState->output[0];
		slot->output[1] = slotState->output[1];
		slot->phase = slotState->phase;
		slot->pgout = slotState->pgout;
		slot->eg_mode = slotState->eg_mode;
		slot->eg_phase = slotState->eg_phase;
		slot->eg_dphase = slotState->eg_dphase;
		slot->egout = slotState->egout;
	}
}

static bool IsLittleEndian()
{
	int i = 42;
	if (((char*)&i)[0] == 42)
	{
		return true;
	}
	return false;
}

void OPLL_state_byteswap(OPLL_STATE *state)
{
	int i;
	if (IsLittleEndian()) return;

	BYTESWAP(state->pm_phase);
	BYTESWAP(state->am_phase);

	for (i = 0; i < 12; i++)
	{
		OPLL_SLOT_STATE *slotState = &(state->slot[i]);
		BYTESWAP(slotState->feedback);
		BYTESWAP(slotState->output[0]);
		BYTESWAP(slotState->output[1]);
		BYTESWAP(slotState->phase);
		BYTESWAP(slotState->pgout);
		BYTESWAP(slotState->eg_mode);
		BYTESWAP(slotState->eg_phase);
		BYTESWAP(slotState->eg_dphase);
		BYTESWAP(slotState->egout);
	}
}

#ifdef __cplusplus
}
#endif
