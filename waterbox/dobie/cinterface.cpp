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
};

Emulator* emu;

ECL_EXPORT bool Initialize(const uint8_t* bios, size_t cd_length_, void (*cdcallback_)(size_t sector, uint8_t* dest))
{
	cd_length = cd_length_;
	cdcallback = cdcallback_;
	emu = new Emulator();
	emu->load_BIOS(bios);
	// load memcards
	if (!emu->load_CDVD_Container("", std::unique_ptr<CDVD_Container>(new DVD())))
		return false;
	return true;
}


ECL_EXPORT void FrameAdvance(MyFrameInfo& f)
{
	emu->run();
	emu->get_inner_resolution(f.Width, f.Height);
	{
		uint32_t* src = emu->get_framebuffer();
		memcpy(f.VideoBuffer, src, f.Width * f.Height * 4);
	}

	f.Samples = 735; // TODO
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
