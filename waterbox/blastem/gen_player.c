#include <stdlib.h>
#include "gen_player.h"
#include "event_log.h"
#include "render.h"

#define MCLKS_NTSC 53693175
#define MCLKS_PAL  53203395
#define MCLKS_PER_YM  7
#define MCLKS_PER_Z80 15
#define MCLKS_PER_PSG (MCLKS_PER_Z80*16)

#ifdef IS_LIB
#define MAX_SOUND_CYCLES (MCLKS_PER_YM*NUM_OPERATORS*6*4)
#else
#define MAX_SOUND_CYCLES 100000	
#endif

static void sync_sound(gen_player *gen, uint32_t target)
{
	//printf("YM | Cycle: %d, bpos: %d, PSG | Cycle: %d, bpos: %d\n", gen->ym->current_cycle, gen->ym->buffer_pos, gen->psg->cycles, gen->psg->buffer_pos * 2);
	while (target > gen->psg->cycles && target - gen->psg->cycles > MAX_SOUND_CYCLES) {
		uint32_t cur_target = gen->psg->cycles + MAX_SOUND_CYCLES;
		//printf("Running PSG to cycle %d\n", cur_target);
		psg_run(gen->psg, cur_target);
		//printf("Running YM-2612 to cycle %d\n", cur_target);
		ym_run(gen->ym, cur_target);
	}
	psg_run(gen->psg, target);
	ym_run(gen->ym, target);

	//printf("Target: %d, YM bufferpos: %d, PSG bufferpos: %d\n", target, gen->ym->buffer_pos, gen->psg->buffer_pos * 2);
}

static void run(gen_player *player)
{
	while(player->reader.socket || player->reader.buffer.cur_pos < player->reader.buffer.size)
	{
		uint32_t cycle;
		uint8_t event = reader_next_event(&player->reader, &cycle);
		switch (event)
		{
		case EVENT_FLUSH:
			sync_sound(player, cycle);
			vdp_run_context(player->vdp, cycle);
			break;
		case EVENT_ADJUST: {
			sync_sound(player, cycle);
			vdp_run_context(player->vdp, cycle);
			uint32_t deduction = load_int32(&player->reader.buffer);
			ym_adjust_cycles(player->ym, deduction);
			vdp_adjust_cycles(player->vdp, deduction);
			player->psg->cycles -= deduction;
			break;
		case EVENT_PSG_REG:
			sync_sound(player, cycle);
			reader_ensure_data(&player->reader, 1);
			psg_write(player->psg, load_int8(&player->reader.buffer));
			break;
		case EVENT_YM_REG: {
			sync_sound(player, cycle);
			reader_ensure_data(&player->reader, 3);
			uint8_t part = load_int8(&player->reader.buffer);
			uint8_t reg = load_int8(&player->reader.buffer);
			uint8_t value = load_int8(&player->reader.buffer);
			if (part) {
				ym_address_write_part2(player->ym, reg);
			} else {
				ym_address_write_part1(player->ym, reg);
			}
			ym_data_write(player->ym, value);
			break;
		case EVENT_STATE: {
			reader_ensure_data(&player->reader, 3);
			uint32_t size = load_int8(&player->reader.buffer) << 16;
			size |= load_int16(&player->reader.buffer);
			reader_ensure_data(&player->reader, size);
			deserialize_buffer buffer;
			init_deserialize(&buffer, player->reader.buffer.data + player->reader.buffer.cur_pos, size);
			register_section_handler(&buffer, (section_handler){.fun = vdp_deserialize, .data = player->vdp}, SECTION_VDP);
			register_section_handler(&buffer, (section_handler){.fun = ym_deserialize, .data = player->ym}, SECTION_YM2612);
			register_section_handler(&buffer, (section_handler){.fun = psg_deserialize, .data = player->psg}, SECTION_PSG);
			while (buffer.cur_pos < buffer.size)
			{
				if (!load_section(&buffer))
					break;
			}
			player->reader.buffer.cur_pos += size;
			free(buffer.handlers);
			break;
		}
		default:
			vdp_run_context(player->vdp, cycle);
			vdp_replay_event(player->vdp, event, &player->reader);
		}
		}
			
		}
		if (!player->reader.socket) {
			reader_ensure_data(&player->reader, 1);
		}
	}
}

static int thread_main(void *player)
{
	run(player);
	return 0;
}

void start_context(system_header *sys, char *statefile)
{
	gen_player *player = (gen_player *)sys;
	if (player->reader.socket) {
#ifndef IS_LIB
		render_create_thread(&player->thread, "player", thread_main, player);
#endif
	} else {
		run(player);
	}
}

static void gamepad_down(system_header *system, uint8_t gamepad_num, uint8_t button)
{
	gen_player *player = (gen_player *)system;
	reader_send_gamepad_event(&player->reader, gamepad_num, button, 1);
}

static void gamepad_up(system_header *system, uint8_t gamepad_num, uint8_t button)
{
	gen_player *player = (gen_player *)system;
	reader_send_gamepad_event(&player->reader, gamepad_num, button, 0);
}

static void config_common(gen_player *player)
{
	uint8_t vid_std = load_int8(&player->reader.buffer);
	uint8_t name_len = load_int8(&player->reader.buffer);
	player->header.info.name = calloc(1, name_len + 1);
	load_buffer8(&player->reader.buffer, player->header.info.name, name_len);
	
	player->vdp = init_vdp_context(vid_std == VID_PAL, 0);
	render_set_video_standard(vid_std);
	uint32_t master_clock = vid_std == VID_NTSC ? MCLKS_NTSC : MCLKS_PAL;
	
	player->ym = malloc(sizeof(ym2612_context));
	ym_init(player->ym, master_clock, MCLKS_PER_YM, 0);
	
	player->psg = malloc(sizeof(psg_context));
	psg_init(player->psg, master_clock, MCLKS_PER_PSG);
	
	player->header.start_context = start_context;
	player->header.gamepad_down = gamepad_down;
	player->header.gamepad_up = gamepad_up;
	player->header.type = SYSTEM_GENESIS_PLAYER;
	player->header.info.save_type = SAVE_NONE;
}

gen_player *alloc_config_gen_player(void *stream, uint32_t rom_size)
{
	uint8_t *data = stream;
	gen_player *player = calloc(1, sizeof(gen_player));
	init_event_reader(&player->reader, data + 9, rom_size - 9);
	config_common(player);
	return player;
}

gen_player *alloc_config_gen_player_reader(event_reader *reader)
{
	gen_player *player = calloc(1, sizeof(gen_player));
	player->reader = *reader;
	inflateCopy(&player->reader.input_stream, &reader->input_stream);
	render_set_external_sync(1);
	config_common(player);
	return player;
}

