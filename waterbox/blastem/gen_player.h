#ifndef GEN_PLAYER_H_
#define GEN_PLAYER_H_

#include "render.h"
#include "system.h"
#include "vdp.h"
#include "psg.h"
#include "ym2612.h"
#include "event_log.h"

typedef struct {
	system_header   header;
	
	vdp_context     *vdp;
	ym2612_context  *ym;
	psg_context     *psg;
#ifndef IS_LIB
	render_thread   thread;
#endif
	event_reader    reader;
} gen_player;

gen_player *alloc_config_gen_player(void *stream, uint32_t rom_size);
gen_player *alloc_config_gen_player_reader(event_reader *reader);

#endif //GEN_PLAYER_H_
