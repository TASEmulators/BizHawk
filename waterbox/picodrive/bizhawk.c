#include <stdio.h>
#include <stdint.h>
#include <stdarg.h>
#include <string.h>

#include "../emulibc/emulibc.h"
#include "../emulibc/waterboxcore.h"
#include "pico/pico.h"
#include "pico/pico_int.h"
#include "pico/cd/cdd.h"

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
	abort();
	return -1;
}

int PicoCdCheck(const char *fname_in, int *pregion)
{
	uint8_t buff[2048];
	CDReadSector(0, buff, 0);
	int region;
	switch (buff[0x20b])
	{
	case 0x64:
		region = 8; // EU
		printf("Detected CD region EU\n");
		break;
	case 0xa1:
		region = 1; // JP
		printf("Detected CD region JP\n");
		break;
	default:
		region = 4; // US
		printf("Detected CD region US\n");
		break;
	}
	if (pregion)
		*pregion = region;

	return CIT_BIN;
}

static const char *GetBiosFilename(int *region, const char *cd_fname)
{
	switch (*region)
	{
	case 8: // EU
		return "cd.eu";
	case 1: // JP
		return "cd.jp";
	default: // US?
		return "cd.us";
	}
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

typedef struct
{
	FrameInfo b;
	uint32_t Buttons;
} MyFrameInfo;

static int video_start_line;
static int video_line_count;
static int video_width;
static uint16_t *video_buffer;
static int16_t *sound_buffer;
static MyFrameInfo *current_frame;

static void SoundCallback(int len)
{
	current_frame->b.Samples = len / (2 * sizeof(int16_t));
	memcpy(current_frame->b.SoundBuffer, sound_buffer, len);
}

void emu_video_mode_change(int start_line, int line_count, int is_32cols)
{
	video_start_line = start_line;
	video_line_count = line_count;
	video_width = is_32cols ? 256 : 320;
	PicoDrawSetOutBuf(video_buffer, video_width * sizeof(uint16_t));
}

// "switch to 16bpp mode?"
void emu_32x_startup(void)
{
}

static const uint8_t *TryLoadBios(const char *name)
{
	FILE *f = fopen(name, "rb");
	if (!f)
		return NULL;
	fseek(f, 0, SEEK_END);
	int size = ftell(f);
	uint8_t *ret = alloc_sealed(size);
	fseek(f, 0, SEEK_SET);
	fread(ret, 1, size, f);
	fclose(f);
	return ret;
}

ECL_EXPORT int Init(int cd, int _32xPreinit, int regionAutoOrder, int regionOverride)
{
	PicoAutoRgnOrder = regionAutoOrder;
	PicoRegionOverride = regionOverride;

	p32x_bios_g = TryLoadBios("32x.g");
	p32x_bios_m = TryLoadBios("32x.m");
	p32x_bios_s = TryLoadBios("32x.s");

	PicoOpt = POPT_EN_FM | POPT_EN_PSG | POPT_EN_Z80 | POPT_EN_STEREO | POPT_ACC_SPRITES | POPT_DIS_32C_BORDER | POPT_EN_MCD_PCM | POPT_EN_MCD_CDDA | POPT_EN_MCD_GFX | POPT_EN_32X | POPT_EN_PWM | POPT_DIS_IDLE_DET;

	PicoInit();
	if (cd)
	{
		if (PicoLoadMedia(NULL, NULL, GetBiosFilename, NULL, PM_CD) != PM_CD)
			return 0;
	}
	else
	{
		if (PicoLoadMedia("romfile.md", NULL, NULL, NULL, PM_MD_CART) != PM_MD_CART)
			return 0;
	}
	PicoLoopPrepare();

	video_buffer = alloc_invisible(512 * 512 * sizeof(uint16_t));
	sound_buffer = alloc_invisible(2048 * sizeof(int16_t));
	PsndRate = 44100;
	PsndOut = sound_buffer;
	PicoWriteSound = SoundCallback;

	PicoSetInputDevice(0, PICO_INPUT_PAD_6BTN);
	PicoSetInputDevice(1, PICO_INPUT_PAD_6BTN);
	PsndRerate(0);

	PicoDrawSetOutFormat(PDF_RGB555, 0); // TODO: what is "use_32x_line_mode"?
	if (_32xPreinit)
	{
		// this is only needed so that the memory domains will show up on the memory domain list
		// otherwise, 32x will run fine without it.
		Pico32xMem = malloc(sizeof(*Pico32xMem));
	}

	PicoPower();

	return 1;
}

static void Blit(void)
{
	const uint16_t *src = video_buffer;
	FrameInfo *f = &current_frame->b;
	f->Width = video_width;
	f->Height = video_line_count;
	uint8_t *dst = (uint8_t *)f->VideoBuffer;
	src += video_width * video_start_line;

	for (int j = 0; j < video_line_count * video_width; j++)
	{
		uint16_t c = *src++;
		*dst++ = c << 3 & 0xf8 | c >> 2 & 7;
		*dst++ = c >> 3 & 0xfa | c >> 9 & 3;
		*dst++ = c >> 8 & 0xf8 | c >> 13 & 7;
		*dst++ = 0xff;
	}
}

static uint32_t PrevButtons;
ECL_EXPORT void FrameAdvance(MyFrameInfo *f)
{
	current_frame = f;
	PicoInputWasRead = 0;
	PicoPad[0] = f->Buttons & 0xfff;
	PicoPad[1] = f->Buttons >> 12 & 0xfff;
	if ((f->Buttons & 0x1000000) > (PrevButtons & 0x1000000))
		PicoPower();
	if ((f->Buttons & 0x2000000) > (PrevButtons & 0x2000000))
		PicoReset();
	PrevButtons = f->Buttons;

	PicoFrame();
	Blit();
	f->b.Lagged = !PicoInputWasRead;
	current_frame = NULL;
}

ECL_EXPORT void GetMemoryAreas(MemoryArea *m)
{
	m[0].Data = Pico.ram;
	m[0].Name = "68K RAM";
	m[0].Size = 0x10000;
	m[0].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_PRIMARY | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_SWAPPED;

	m[1].Data = Pico.vram;
	m[1].Name = "VRAM";
	m[1].Size = 0x10000;
	m[1].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_SWAPPED;

	m[2].Data = Pico.zram;
	m[2].Name = "Z80 RAM";
	m[2].Size = 0x2000;
	m[2].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE1;

	m[3].Data = Pico.cram;
	m[3].Name = "CRAM";
	m[3].Size = 0x80;
	m[3].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE1;

	m[4].Data = Pico.vsram;
	m[4].Name = "VSRAM";
	m[4].Size = 0x80;
	m[4].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE1;

	m[5].Data = Pico.rom;
	m[5].Name = "MD CART";
	m[5].Size = Pico.romsize;
	m[5].Flags = MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_SWAPPED | MEMORYAREA_FLAGS_YUGEENDIAN;

	if (Pico32xMem)
	{
		m[6].Data = Pico32xMem->sdram;
		m[6].Name = "32X RAM";
		m[6].Size = 0x40000;
		m[6].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_SWAPPED;

		m[7].Data = Pico32xMem->dram;
		m[7].Name = "32X FB";
		m[7].Size = 0x40000;
		m[7].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_SWAPPED;
	}

	if (SRam.data != NULL)
	{
		m[8].Data = SRam.data;
		m[8].Name = "SRAM";
		m[8].Size = SRam.size;
		m[8].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_SAVERAMMABLE;
	}
}

ECL_EXPORT void SetInputCallback(void (*callback)(void))
{
	PicoInputCallback = callback;
}
ECL_EXPORT void SetCDReadCallback(void (*callback)(int lba, void *dest, int audio))
{
	CDReadSector = callback;
}
ECL_EXPORT int IsPal(void)
{
	return Pico.m.pal;
}
ECL_EXPORT int Is32xActive(void)
{
	return !!(PicoAHW & PAHW_32X);
}

int main(void)
{
	return 0;
}
