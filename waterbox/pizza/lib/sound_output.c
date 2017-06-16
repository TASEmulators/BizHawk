#include "../blip_buf/blip_buf.h"
#include "sound_output.h"
#include "cycles.h"

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
int sound_output_read(int16_t* output)
{
	blip_end_frame(lb, RELATIVECLOCK);
	blip_end_frame(rb, RELATIVECLOCK);
	startclock = cycles.sampleclock;
	int ret = blip_read_samples(lb, output, 2048, 1);
	blip_read_samples(rb, output + 1, 2048, 1);
	return ret;
}
