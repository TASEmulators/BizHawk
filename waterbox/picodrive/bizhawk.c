#include <stdio.h>
#include <stdint.h>
#include <stdarg.h>

#include "../emulibc/emulibc.h"
#include "../emulibc/waterboxcore.h"
#include "pico/pico.h"
#include "pico/cd/cue.h"

void lprintf(const char *fmt, ...)
{
	va_list args;
	va_start(args, fmt);
	vprintf(fmt, args);
	va_end(args);
}

int PicoCartResize(int newsize)
{
	// TODO: change boards that use this to store their extra data elsewhere
	return -1;
}

int PicoCdCheck(const char *fname_in, int *pregion)
{
	return CIT_NOT_CD;
}

cue_data_t *cue_parse(const char *fname)
{
	return NULL;
}

pm_file *pm_open(const char *path)
{
	FILE *f = fopen(path, "rb");
	if (!f)
		return NULL;
	fseek(f, 0, SEEK_END);
	pm_file *ret = calloc(1, sizeof(*ret));
	ret->file = f;
	ret->size = ftell(f);
	fseek(f, 0, SEEK_SET);
	ret->type = PMT_UNCOMPRESSED;
	return ret;
}
size_t pm_read(void *ptr, size_t bytes, pm_file *stream)
{
	return fread(ptr, 1, bytes, (FILE *)stream->file);
}
int pm_seek(pm_file *stream, long offset, int whence)
{
	return fseek((FILE *)stream->file, offset, whence);
}
int pm_close(pm_file *fp)
{
	int ret = fclose((FILE *)fp->file);
	fp->file = NULL;
	free(fp);
	return ret;
}

static int video_start_line;
static int video_line_count;
static int video_width;
static uint16_t* video_buffer;

void emu_video_mode_change(int start_line, int line_count, int is_32cols)
{
	video_start_line = start_line;
	video_line_count = line_count;
	video_width = is_32cols ? 256 : 320;
}

// "switch to 16bpp mode?"
void emu_32x_startup(void)
{
}

int mp3_get_bitrate(void *f, int size) { return 0; }
void mp3_start_play(void *f, int pos) {}
void mp3_update(int *buffer, int length, int stereo) {}

ECL_EXPORT int Init(void)
{
	video_buffer = alloc_invisible(512 * 512 * sizeof(uint16_t));

	PicoInit();
	PicoSetInputDevice(0, PICO_INPUT_PAD_6BTN);
	PicoSetInputDevice(1, PICO_INPUT_PAD_6BTN);
	PicoDrawSetOutBuf(video_buffer, 512 * sizeof(uint16_t));

	if (PicoLoadMedia("romfile.md", NULL, NULL, NULL, PM_MD_CART) != PM_MD_CART)
		return 0;
	PicoDrawSetOutFormat(PDF_RGB555, 0); // TODO: what is "use_32x_line_mode"?
	PicoPower();

	return 1;
}

typedef struct
{
	FrameInfo b;
} MyFrameInfo;

static void Blit(const uint16_t *src, FrameInfo *f)
{
	f->Width = video_width;
	f->Height = video_line_count;
	uint8_t *dst = (uint8_t *)f->VideoBuffer;
	src += 512 * video_start_line;
	if (video_width == 256)
		src += 32;

	for (int j = 0; j < video_line_count; j++)
	{
		for (int i = 0; i < video_width; i++)
		{
			uint16_t c = src[i];
			*dst++ = c << 3 & 0xf8 | c >> 2 & 7;
			*dst++ = c >> 3 & 0xfa | c >> 9 & 3;
			*dst++ = c >> 8 & 0xf8 | c >> 13 & 7;
			*dst++ = 0xff;
		}
		src += 512;
	}
}

ECL_EXPORT void FrameAdvance(MyFrameInfo *f)
{
	PicoLoopPrepare();
	PicoFrame();
	Blit(video_buffer, &f->b);
	f->b.Samples = 735; // TODO
}

static uint8_t dumbo[16];
ECL_EXPORT void GetMemoryAreas(MemoryArea *m)
{
	m[0].Data = dumbo;
	m[0].Name = "TODO";
	m[0].Size = 16;
	m[0].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_PRIMARY;
}

ECL_EXPORT void SetInputCallback(void (*callback)(void))
{
}

int main(void)
{
	return 0;
}
