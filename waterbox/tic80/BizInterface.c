#include <tic80.h>
#include <tic.h>
#include <api.h>
#include <core.h>

#include <emulibc.h>
#include <waterboxcore.h>

#include <string.h>

static time_t biz_time = 0;
static u32 biz_clock_rm = 0;

time_t BizTimeCallback()
{
	return biz_time;
}

clock_t BizClockCallback()
{
	return biz_time * CLOCKS_PER_SEC + (biz_clock_rm * CLOCKS_PER_SEC / 60);
}

static tic80* tic;
static tic80_input biz_inputs;

typedef struct
{
	u8 gamepad[4];
	u8 mouse;
	u8 keyboard;
} InputsEnabled;

ECL_EXPORT bool Init(u8* rom, u32 sz, InputsEnabled* inputsEnable)
{
	tic = tic80_create(TIC80_SAMPLERATE, TIC80_PIXEL_COLOR_BGRA8888);
	if (!tic)
	{
		return false;
	}

	tic80_load(tic, rom, sz);

	// advance one frame to initialize things
	// if initializing failed we can know after it advances
	memset(&biz_inputs, 0, sizeof(biz_inputs));
	tic80_tick(tic, biz_inputs);
	tic80_sound(tic); // should this be done?

	tic_mem* mem = (tic_mem*)tic;
	if (!mem->input.gamepad)
	{
		memset(inputsEnable->gamepad, 0, sizeof(inputsEnable->gamepad));
	}
	if (!mem->input.mouse)
	{
		inputsEnable->mouse = false;
	}
	if (!mem->input.keyboard)
	{
		inputsEnable->keyboard = false;
	}

	tic_core* core = (tic_core*)tic;
	return core->state.initialized;
}

ECL_EXPORT void GetMemoryAreas(MemoryArea* m)
{
	tic_mem* mem = (tic_mem*)tic;

	m[0].Data = mem->ram->data;
	m[0].Name = "RAM";
	m[0].Size = sizeof(mem->ram->data);
	m[0].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_PRIMARY;

	m[1].Data = mem->ram->persistent.data;
	m[1].Name = "SaveRAM";
	m[1].Size = sizeof(mem->ram->persistent.data);
	m[1].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_SAVERAMMABLE;

	m[2].Data = mem->ram->vram.data;
	m[2].Name = "VRAM";
	m[2].Size = sizeof(mem->ram->vram.data);
	m[2].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE;
}

ECL_EXPORT void SetInputs(tic80_input* inputs)
{
	memcpy(&biz_inputs, inputs, sizeof(tic80_input));
}

ECL_EXPORT bool IsMouseRelative()
{
	tic_mem* mem = (tic_mem*)tic;
	return mem->ram->input.mouse.relative;
}

typedef struct
{
	FrameInfo b;
	u64 time;
	bool crop;
	bool reset;
} MyFrameInfo;

bool lagged;
void (*inputcb)() = 0;

ECL_EXPORT void FrameAdvance(MyFrameInfo* f)
{
	if (f->reset)
	{
		tic_api_reset((tic_mem*)tic);
	}

	lagged = true;
	biz_time = f->time;

	tic80_tick(tic, biz_inputs);
	tic80_sound(tic);

	f->b.Samples = tic->samples.count / TIC80_SAMPLE_CHANNELS;
	memcpy(f->b.SoundBuffer, tic->samples.buffer, tic->samples.count * TIC80_SAMPLESIZE);

	u32* src;
	u32 width;
	u32 height;
	if (f->crop)
	{
		src = (u32*)tic->screen + (TIC80_FULLWIDTH * TIC80_OFFSET_TOP) + TIC80_OFFSET_LEFT;
		width = TIC80_WIDTH;
		height = TIC80_HEIGHT;
	}
	else
	{
		src = tic->screen;
		width = TIC80_FULLWIDTH;
		height = TIC80_FULLHEIGHT;
	}
	
	u32* dst = f->b.VideoBuffer;
	for (u32 i = 0; i < height; i++)
	{
		memcpy(dst, src, width * sizeof(u32));
		dst += width;
		src += TIC80_FULLWIDTH;
	}

	f->b.Width = width;
	f->b.Height = height;

	f->b.Lagged = lagged;

	biz_clock_rm = (biz_clock_rm + 1) % 60;
}

ECL_EXPORT void SetInputCallback(void (*callback)())
{
	inputcb = callback;
}
