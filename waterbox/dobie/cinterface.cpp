#include "../emulibc/emulibc.h"
#include "../emulibc/waterboxcore.h"
#include <stdint.h>
#include "dobiestation/src/core/emulator.hpp"
#include "dobiestation/src/core/iop/cdvd/cdvd_container.hpp"


static size_t cd_length;
static void (*cdcallback)(size_t sector, uint8_t* dest);
static size_t cd_pos;

class DVD: public CDVD_Container
{
	virtual bool open(std::string name) override
	{
		return true;
	}
	virtual void close() override {}
	virtual size_t read(uint8_t* buff, size_t bytes)
	{
		uint8_t* buff_end = buff + bytes;
		if (cd_pos % 2048 != 0)
		{
			auto offset = cd_pos % 2048;
			auto nread = std::min(2048 - offset, bytes);
			uint8_t tmp[2048];
			cdcallback(cd_pos / 2048, tmp);
			memcpy(buff, tmp + offset, nread);
			buff += nread;
			cd_pos += nread;
		}
		while (buff_end >= buff + 2048)
		{
			cdcallback(cd_pos / 2048, buff);
			buff += 2048;
			cd_pos += 2048;
		}
		if (buff_end > buff)
		{
			auto nread = buff_end - buff;
			uint8_t tmp[2048];
			cdcallback(cd_pos / 2048, tmp);
			memcpy(buff, tmp, nread);
			cd_pos += nread;
		}
		return bytes;
	}
	virtual void seek(size_t pos, std::ios::seekdir whence) override
	{
		cd_pos = pos * 2048;
	}
	virtual bool is_open() override
	{
		return true;
	}
	virtual size_t get_size() override
	{
		return cd_length;
	}
};


struct MyFrameInfo: public FrameInfo
{
	uint32_t Buttons;
	uint32_t Axes;
};

Emulator* emu;

struct SyncSettings
{
	bool EEJit;
	bool VU0Jit;
	bool VU1Jit;
};

ECL_EXPORT bool Initialize(
	const uint8_t* bios,
	size_t cd_length_,
	void (*cdcallback_)(size_t sector, uint8_t* dest),
	const SyncSettings& syncSettings
)
{
	cd_length = cd_length_;
	cdcallback = cdcallback_;
	emu = new Emulator();
	emu->reset();
	emu->set_skip_BIOS_hack(LOAD_DISC);
	emu->load_BIOS(bios);
	emu->load_memcard(0, "MEMCARD0");
	if (!emu->load_CDVD_Container("", std::unique_ptr<CDVD_Container>(new DVD())))
		return false;
	emu->set_ee_mode(syncSettings.EEJit ? JIT : INTERPRETER);
	printf("EE Mode: %s\n", syncSettings.EEJit ? "JIT" : "INTERPRETER");
	emu->set_vu0_mode(syncSettings.VU0Jit ? JIT : INTERPRETER);
	printf("VU0 Mode: %s\n", syncSettings.VU0Jit ? "JIT" : "INTERPRETER");
	emu->set_vu1_mode(syncSettings.VU1Jit ? JIT : INTERPRETER);
	printf("VU1 Mode: %s\n", syncSettings.VU1Jit ? "JIT" : "INTERPRETER");
	emu->set_wav_output(true);
	return true;
}

static int16_t* audio_pos;
void hacky_enqueue_audio(stereo_sample s)
{
	*audio_pos++ = s.left;
	*audio_pos++ = s.right;
}

ECL_EXPORT void FrameAdvance(MyFrameInfo& f)
{
	for (auto i = 0; i < 16; i++)
	{
		if (f.Buttons & 1 << i)
		{
			emu->press_button((PAD_BUTTON)i);
		}
		else
		{
			emu->release_button((PAD_BUTTON)i);
		}
	}
	for (auto i = 0; i < 4; i++)
	{
		emu->update_joystick((JOYSTICK)(i >> 1), (JOYSTICK_AXIS)(i & 1), f.Axes >> (i * 8));
	}
	audio_pos = f.SoundBuffer;
	emu->run();
	emu->get_inner_resolution(f.Width, f.Height);
	{
		const uint32_t* src = emu->get_framebuffer();
		const uint32_t* srcend = src + f.Width * f.Height;
		uint32_t* dst = f.VideoBuffer;
		while (src < srcend)
		{
			*dst = *src;
			std::swap(((uint8_t*)dst)[2], ((uint8_t*)dst)[0]);
			src++;
			dst++;
		}
	}

	f.Samples = (audio_pos - f.SoundBuffer) / 2;
	audio_pos = nullptr;
}

static uint8_t junkus[14];

ECL_EXPORT void GetMemoryAreas(MemoryArea *m)
{
	m[0].Data = junkus;
	m[0].Name = "JUNKUS";
	m[0].Size = sizeof(junkus);
	m[0].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_PRIMARY;
}

ECL_EXPORT void SetInputCallback(void (*callback)())
{
}
