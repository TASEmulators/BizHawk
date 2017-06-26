#include "../blip_buf/blip_buf.h"
#include "sound_output.h"
#include "cycles.h"
#include "sgb.h"
#include "global.h"

static blip_t* lb;
static blip_t* rb;
static uint64_t startclock;

#define RELATIVECLOCK (cycles.sampleclock - startclock)

void blip_left(int delta)
{
	if (delta)
		blip_add_delta(lb, RELATIVECLOCK, delta);
}
void blip_right(int delta)
{
	if (delta)
		blip_add_delta(rb, RELATIVECLOCK, delta);
}

void sound_output_init(double clock_rate, double sample_rate)
{
	lb = blip_new(1024);
	rb = blip_new(1024);
	blip_set_rates(lb, clock_rate, sample_rate);
	blip_set_rates(rb, clock_rate, sample_rate);
}

static int32_t sgb_last_l;
static int32_t sgb_last_r;

static void sgb_audio_callback(int16_t l, int16_t r, uint64_t time)
{
	uint64_t t = time - startclock;
	int32_t ld = l - sgb_last_l;
	int32_t rd = r - sgb_last_r;
	blip_add_delta(lb, t, ld);
	blip_add_delta(rb, t, rd);
	sgb_last_l = l;
	sgb_last_r = r;
}

int sound_output_read(int16_t* output)
{
	if (global_sgb)
		sgb_render_audio(cycles.sampleclock, sgb_audio_callback);

	blip_end_frame(lb, RELATIVECLOCK);
	blip_end_frame(rb, RELATIVECLOCK);
	startclock = cycles.sampleclock;
	int ret = blip_read_samples(lb, output, 2048, 1);
	blip_read_samples(rb, output + 1, 2048, 1);
	return ret;
}
