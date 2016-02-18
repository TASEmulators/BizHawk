#include <cstdlib>
#include <cstring>
#include "nes_emu/Nes_Emu.h"

// simulate the write so we'll know how long the buffer needs to be
class Sim_Writer : public Data_Writer
{
	long size_;
public:
	Sim_Writer():size_(0) { }
	error_t write(const void *, long size)
	{
		size_ += size;
		return 0;
	}
	long size() const { return size_; }
};

// 0 filled new just for kicks
void *operator new(std::size_t n)
{
	if (!n)
		n = 1;
	void *p = std::malloc(n);
	std::memset(p, 0, n);
	return p;
}

void operator delete(void *p)
{
	std::free(p);
}

#ifdef _MSC_VER
#define EXPORT extern "C" __declspec(dllexport)
#else
#define EXPORT extern "C" __declspec(dllexport) __attribute__((force_align_arg_pointer))
#endif

EXPORT void qn_setup_mappers()
{
	register_optional_mappers();
}

EXPORT Nes_Emu *qn_new()
{
	return new Nes_Emu();
}

EXPORT void qn_delete(Nes_Emu *e)
{
	delete e;
}

EXPORT const char *qn_loadines(Nes_Emu *e, const void *data, int length)
{
	Mem_File_Reader r(data, length);
	Auto_File_Reader a(r);
	return e->load_ines(a);
}

EXPORT const char *qn_set_sample_rate(Nes_Emu *e, int rate)
{
	const char *ret = e->set_sample_rate(rate);
	if (!ret)
		e->set_equalizer(Nes_Emu::nes_eq);
	return ret;
}

EXPORT const char *qn_emulate_frame(Nes_Emu *e, int pad1, int pad2)
{
	return e->emulate_frame(pad1, pad2);
}

EXPORT void qn_blit(Nes_Emu *e, int32_t *dest, const int32_t *colors, int cropleft, int croptop, int cropright, int cropbottom)
{
	// what is the point of the 256 color bitmap and the dynamic color allocation to it?
	// why not just render directly to a 512 color bitmap with static palette positions?

	const int srcpitch = e->frame().pitch;
	const unsigned char *src = e->frame().pixels;
	const unsigned char *const srcend = src + (e->image_height - cropbottom) * srcpitch;

	const short *lut = e->frame().palette;

	const int rowlen = 256 - cropleft - cropright;

	src += cropleft;
	src += croptop * srcpitch;

	for (; src < srcend; src += srcpitch)
	{
		for (int i = 0; i < rowlen; i++)
		{
			*dest++ = colors[lut[src[i]]];
		}
	}
}

EXPORT const Nes_Emu::rgb_t *qn_get_default_colors()
{
	return Nes_Emu::nes_colors;
}

EXPORT int qn_get_joypad_read_count(Nes_Emu *e)
{
	return e->frame().joypad_read_count;
}

EXPORT void qn_get_audio_info(Nes_Emu *e, int *sample_count, int *chan_count)
{
	if (sample_count)
		*sample_count = e->frame().sample_count;
	if (chan_count)
		*chan_count = e->frame().chan_count;
}

EXPORT int qn_read_audio(Nes_Emu *e, short *dest, int max_samples)
{
	return e->read_samples(dest, max_samples);
}

EXPORT void qn_reset(Nes_Emu *e, int hard)
{
	e->reset(hard);
}

EXPORT const char *qn_state_size(Nes_Emu *e, int *size)
{
	Sim_Writer w;
	Auto_File_Writer a(w);
	const char *ret = e->save_state(a);
	if (size)
		*size = w.size();
	return ret;
}

EXPORT const char *qn_state_save(Nes_Emu *e, void *dest, int size)
{
	Mem_Writer w(dest, size, 0);
	Auto_File_Writer a(w);
	const char *ret = e->save_state(a);
	if (!ret && w.size() != size)
		return "Buffer Underrun!";
	return ret;
}

EXPORT const char *qn_state_load(Nes_Emu *e, const void *src, int size)
{
	Mem_File_Reader r(src, size);
	Auto_File_Reader a(r);
	return e->load_state(a);
}

EXPORT int qn_has_battery_ram(Nes_Emu *e)
{
	return e->has_battery_ram();
}

EXPORT const char *qn_battery_ram_size(Nes_Emu *e, int *size)
{
	Sim_Writer w;
	Auto_File_Writer a(w);
	const char *ret = e->save_battery_ram(a);
	if (size)
		*size = w.size();
	return ret;
}

EXPORT const char *qn_battery_ram_save(Nes_Emu *e, void *dest, int size)
{
	Mem_Writer w(dest, size, 0);
	Auto_File_Writer a(w);
	const char *ret = e->save_battery_ram(a);
	if (!ret && w.size() != size)
		return "Buffer Underrun!";
	return ret;
}

EXPORT const char *qn_battery_ram_load(Nes_Emu *e, const void *src, int size)
{
	Mem_File_Reader r(src, size);
	Auto_File_Reader a(r);
	return e->load_battery_ram(a);
}

EXPORT const char *qn_battery_ram_clear(Nes_Emu *e)
{
	int size = 0;
	const char *ret = qn_battery_ram_size(e, &size);
	if (ret)
		return ret;
	void *data = std::malloc(size);
	if (!data)
		return "Out of Memory!";
	std::memset(data, 0xff, size);
	ret = qn_battery_ram_load(e, data, size);
	std::free(data);
	return ret;
}

EXPORT void qn_set_sprite_limit(Nes_Emu *e, int n)
{
	e->set_sprite_mode((Nes_Emu::sprite_mode_t)n);
}

EXPORT int qn_get_memory_area(Nes_Emu *e, int which, const void **data, int *size, int *writable, const char **name)
{
	if (!data || !size || !writable || !name)
		return 0;
	switch (which)
	{
	default:
		return 0;
	case 0:
		*data = e->low_mem();
		*size = e->low_mem_size;
		*writable = 1;
		*name = "RAM";
		return 1;
	case 1:
		*data = e->high_mem();
		*size = e->high_mem_size;
		*writable = 1;
		*name = "WRAM";
		return 1;
	case 2:
		*data = e->chr_mem();
		*size = e->chr_size();
		*writable = 0;
		*name = "CHR";
		return 1;
	case 3:
		*data = e->nametable_mem();
		*size = e->nametable_size();
		*writable = 0;
		*name = "CIRAM (nametables)";
		return 1;
	case 4:
		*data = e->cart()->prg();
		*size = e->cart()->prg_size();
		*writable = 0;
		*name = "PRG ROM";
		return 1;
	case 5:
		*data = e->cart()->chr();
		*size = e->cart()->chr_size();
		*writable = 0;
		*name = "CHR VROM";
		return 1;
	case 6:
		*data = e->pal_mem();
		*size = 32;
		*writable = 1;
		*name = "PALRAM";
		return 1;
	case 7:
		*data = e->oam_mem();
		*size = 256;
		*writable = 1;
		*name = "OAM";
		return 1;
	}
}

EXPORT unsigned char qn_peek_prgbus(Nes_Emu *e, int addr)
{
	return e->peek_prg(addr & 0xffff);
}

EXPORT void qn_poke_prgbus(Nes_Emu *e, int addr, unsigned char val)
{
	e->poke_prg(addr & 0xffff, val);
}

EXPORT void qn_get_cpuregs(Nes_Emu *e, unsigned int *dest)
{
	e->get_regs(dest);
}

EXPORT const char *qn_get_mapper(Nes_Emu *e, int *number)
{
	int m = e->cart()->mapper_code();
	if (number)
		*number = m;
	switch (m)
	{
	default: return "unknown";
	case 0: return "nrom";
	case 1: return "mmc1";
	case 2: return "unrom";
	case 3: return "cnrom";
	case 4: return "mmc3";
	case 7: return "aorom";
	case 69: return "fme7";
	case 5: return "mmc5";
	case 19: return "namco106";
	case 24: return "vrc6a";
	case 26: return "vrc6b";
	case 11: return "color_dreams";
	case 34: return "nina1";
	case 66: return "gnrom";
	case 87: return "mapper_87";
	case 232: return "quattro";
	case 9: return "mmc2";
	case 10: return "mmc4";
	}
}

EXPORT byte qn_get_reg2000(Nes_Emu *e)
{
	return e->get_ppu2000();
}

EXPORT byte *qn_get_palmem(Nes_Emu *e)
{
	return e->pal_mem();
}

EXPORT byte *qn_get_oammem(Nes_Emu *e)
{
	return e->oam_mem();
}

EXPORT byte qn_peek_ppu(Nes_Emu *e, int addr)
{
	return e->peek_ppu(addr);
}

EXPORT void qn_peek_ppubus(Nes_Emu *e, byte *dest)
{
	for (int i = 0; i < 0x3000; i++)
		dest[i] = e->peek_ppu(i);
}

EXPORT void qn_set_tracecb(Nes_Emu *e, void (*cb)(unsigned int *dest))
{
	e->set_tracecb(cb);
}
