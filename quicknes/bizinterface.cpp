#include <cstdlib>
#include <cstring>
#include <emu.hpp>
#include <jaffarCommon/file.hpp>
#include <jaffarCommon/serializers/contiguous.hpp>
#include <jaffarCommon/deserializers/contiguous.hpp>

#ifdef _WIN32
#define QN_EXPORT extern "C" __declspec(dllexport)
#else
#define QN_EXPORT extern "C" __attribute__((visibility("default")))
#endif

// Relevant defines for video output
#define VIDEO_BUFFER_SIZE 65536
#define DEFAULT_WIDTH 256
#define DEFAULT_HEIGHT 240


QN_EXPORT quickerNES::Emu *qn_new()
{
	// Zero intialized emulator to make super sure no side effects from previous data remains
	auto ptr = calloc(1, sizeof(quickerNES::Emu));
	auto e = new (ptr) quickerNES::Emu();

	// Creating video buffer
	auto videoBuffer = (uint8_t *) malloc(VIDEO_BUFFER_SIZE);
	e->set_pixels(videoBuffer, DEFAULT_WIDTH + 8);

	return e;
}

QN_EXPORT void qn_delete(quickerNES::Emu *e)
{ 
	free(e->get_pixels_base_ptr());
	e->~Emu(); // make sure to explicitly call the dtor
	free(e);
}

QN_EXPORT const char *qn_loadines(quickerNES::Emu *e, const void *data, int length)
{
	e->load_ines((const uint8_t*)data);
	return 0;
}

QN_EXPORT const char *qn_set_sample_rate(quickerNES::Emu *e, int rate)
{
	const char *ret = e->set_sample_rate(rate);
	if (!ret) e->set_equalizer(quickerNES::Emu::nes_eq);
	return ret;
}


QN_EXPORT const char *qn_emulate_frame(quickerNES::Emu *e, uint32_t pad1, uint32_t pad2, uint8_t arkanoidPosition, uint8_t arkanoidFire, int controllerType)
{
	e->setControllerType((quickerNES::Core::controllerType_t)controllerType);

	uint32_t arkanoidLatch = 0;

	if ((quickerNES::Core::controllerType_t) controllerType == quickerNES::Core::controllerType_t::arkanoidNES_t ||
	    (quickerNES::Core::controllerType_t) controllerType == quickerNES::Core::controllerType_t::arkanoidFamicom_t)
	{
        e->setControllerType((quickerNES::Core::controllerType_t) controllerType);

        // This is where we calculate the stream of bits required by the NES / Famicom to correctly interpret the Arkanoid potentiometer signal
		// The logic and procedure were created based on the information in https://www.nesdev.org/wiki/Arkanoid_controller
		// - The arkanoidPosition variable is the intended value 
		// - The centeringPotValue is a calibration parameter. The arkanoidPosition value is passed to the console as a relative value to this. 
		//   This can be change tod calibrate a misaligned physical potentiomenter (not relevant in emulation)
		//   The minumum / maximum ranges for this values are (0x0D-0xAD) to (0x5C-0xFC). NesHawk seems to be calibrated at: 0xAB (171).

		// The value of centeringPotValue is calibrated to coincide exactly with that of the NesHawk emulator
		uint8_t centeringPotValue = 0xAB;

		// Procedure, as expected by the console:
		// 1) Obtain the relative value of arkanoidPosition from the centeringPotValue
		uint8_t relativePosition = centeringPotValue - arkanoidPosition;

		// 2) The result is bit-inverted (required by the console)
		//    The easiest solution is simply to do this per bit
		if ((relativePosition & 128) > 0) arkanoidLatch += 1;
		if ((relativePosition & 64) > 0)  arkanoidLatch += 2;
		if ((relativePosition & 32) > 0)  arkanoidLatch += 4;
		if ((relativePosition & 16) > 0)  arkanoidLatch += 8;
		if ((relativePosition & 8) > 0)   arkanoidLatch += 16;
		if ((relativePosition & 4) > 0)   arkanoidLatch += 32;
		if ((relativePosition & 2) > 0)   arkanoidLatch += 64;
		if ((relativePosition & 1) > 0)   arkanoidLatch += 128;
	}

	return e->emulate_frame(pad1, pad2, arkanoidLatch, arkanoidFire);
}

QN_EXPORT void qn_blit(quickerNES::Emu *e, int32_t *dest, const int32_t *colors, int cropleft, int croptop, int cropright, int cropbottom)
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

QN_EXPORT const quickerNES::Emu::rgb_t *qn_get_default_colors()
{
	return quickerNES::Emu::nes_colors;
}

QN_EXPORT int qn_get_joypad_read_count(quickerNES::Emu *e)
{
	return e->get_joypad_read_count();
}

QN_EXPORT void qn_get_audio_info(quickerNES::Emu *e, int *sample_count, int *chan_count)
{
	if (sample_count)
		*sample_count = e->frame().sample_count;
	if (chan_count)
		*chan_count = e->frame().chan_count;
}

QN_EXPORT int qn_read_audio(quickerNES::Emu *e, short *dest, int max_samples)
{
	return e->read_samples(dest, max_samples);
}

QN_EXPORT void qn_reset(quickerNES::Emu *e, int hard)
{
	e->reset(hard);
}

QN_EXPORT const char *qn_state_size(quickerNES::Emu *e, int *size)
{
	jaffarCommon::serializer::Contiguous s;
	e->serializeState(s);
	*size = s.getOutputSize();
	return 0;
}

QN_EXPORT const char *qn_state_save(quickerNES::Emu *e, void *dest, int size)
{
	jaffarCommon::serializer::Contiguous s(dest, size);
	e->serializeState(s);
	return 0;
}

QN_EXPORT const char *qn_state_load(quickerNES::Emu *e, const void *src, int size)
{
	jaffarCommon::deserializer::Contiguous d(src, size);
	e->deserializeState(d);
	return 0;
}

QN_EXPORT int qn_has_battery_ram(quickerNES::Emu *e)
{
	return e->has_battery_ram();
}

QN_EXPORT const char *qn_battery_ram_size(quickerNES::Emu *e, int *size)
{
	*size = e->get_high_mem_size();
	return 0;
}

QN_EXPORT const char *qn_battery_ram_save(quickerNES::Emu *e, void *dest, int size)
{
	memcpy(dest, e->high_mem(), size);
	return 0;
}

QN_EXPORT const char *qn_battery_ram_load(quickerNES::Emu *e, const void *src, int size)
{
	memcpy(e->high_mem(), src, size);
	return 0;
}

QN_EXPORT const char *qn_battery_ram_clear(quickerNES::Emu *e)
{
	int size = 0;
	qn_battery_ram_size(e, &size);
	std::memset(e->high_mem(), 0xff, size);
	return 0;
}

QN_EXPORT void qn_set_sprite_limit(quickerNES::Emu *e, int n)
{
	e->set_sprite_mode((quickerNES::Emu::sprite_mode_t)n);
}

QN_EXPORT int qn_get_memory_area(quickerNES::Emu *e, int which, const void **data, int *size, int *writable, const char **name)
{
	if (!data || !size || !writable || !name)
		return 0;
	switch (which)
	{
	default:
		return 0;
	case 0:
		*data = e->get_low_mem();
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
		*writable = 1;
		*name = "CHR";
		return 1;
	case 3:
		*data = e->nametable_mem();
		*size = e->nametable_size();
		*writable = 1;
		*name = "CIRAM (nametables)";
		return 1;
	case 4:
		*data = e->cart()->prg();
		*size = e->cart()->prg_size();
		*writable = 1;
		*name = "PRG ROM";
		return 1;
	case 5:
		*data = e->cart()->chr();
		*size = e->cart()->chr_size();
		*writable = 1;
		*name = "CHR VROM";
		return 1;
	case 6:
		*data = e->pal_mem();
		*size = e->pal_mem_size();
		*writable = 1;
		*name = "PALRAM";
		return 1;
	case 7:
	    *data = e->spr_mem();
		*size = e->spr_mem_size();
		*writable = 1;
		*name = "OAM";
		return 1;
	}
}

QN_EXPORT unsigned char qn_peek_prgbus(quickerNES::Emu *e, int addr)
{
	return e->peek_prg(addr & 0xffff);
}

QN_EXPORT void qn_poke_prgbus(quickerNES::Emu *e, int addr, unsigned char val)
{
	e->poke_prg(addr & 0xffff, val);
}

QN_EXPORT void qn_get_cpuregs(quickerNES::Emu *e, unsigned int *dest)
{
	e->get_regs(dest);
}

QN_EXPORT const char *qn_get_mapper(quickerNES::Emu *e, int *number)
{
	int m = e->cart()->mapper_code();
	if (number)
		*number = m;
	switch (m)
	{
	default: return "unknown";
	case   0: return "nrom";
	case   1: return "mmc1";
	case   2: return "unrom";
	case   3: return "cnrom";
	case   4: return "mmc3";
	case   5: return "mmc5";
	case   7: return "aorom";
	case   9: return "mmc2";
	case  10: return "mmc4";
	case  11: return "color_dreams";
	case  15: return "k1029/30P";
	case  19: return "namco106";
	case  21: return "vrc2,vrc4(21)";
	case  22: return "vrc2,vrc4(22)";
	case  23: return "vrc2,vrc4(23)";
	case  24: return "vrc6a";
	case  25: return "vrc2,vrc4(25)";
	case  26: return "vrc6b";
	case  30: return "Unrom512";
	case  32: return "Irem_G101";
	case  33: return "TaitoTC0190";
	case  34: return "nina1";
	case  60: return "NROM-128";
	case  66: return "gnrom";
	case  69: return "fme7";
	case  70: return "74x161x162x32(70)";
	case  71: return "camerica";
	case  73: return "vrc3";
	case  75: return "vrc1";
	case  78: return "mapper_78";
	case  79: return "nina03,nina06(79)";
	case  85: return "vrc7";
	case  86: return "mapper_86";
	case  87: return "mapper_87";
	case  88: return "namco34(88)";
	case  89: return "sunsoft2b";
	case  93: return "sunsoft2a";
	case  94: return "Un1rom";
    case  97: return "irem_tam_s1";
	case 113: return "nina03,nina06(113)";
	case 140: return "jaleco_jf11";
	case 152: return "74x161x162x32(152)";
	case 154: return "namco34(154)";
	case 156: return "dis23c01_daou";
	case 180: return "uxrom(inverted)";
	case 184: return "sunsoft1";
	case 190: return "magickidgoogoo";
	case 193: return "tc112";
	case 206: return "namco34(206)";
	case 207: return "taitox1005";
	case 232: return "quattro";
	case 240: return "mapper_240";
	case 241: return "mapper_241";
	case 246: return "mapper_246";
	}
}

QN_EXPORT uint8_t qn_get_reg2000(quickerNES::Emu *e)
{
	return e->get_ppu2000();
}

QN_EXPORT uint8_t *qn_get_palmem(quickerNES::Emu *e)
{
	return e->pal_mem();
}

QN_EXPORT uint8_t *qn_get_oammem(quickerNES::Emu *e)
{
	return e->pal_mem();
}

QN_EXPORT uint8_t qn_peek_ppu(quickerNES::Emu *e, int addr)
{
	return e->peek_ppu(addr);
}

QN_EXPORT void qn_peek_ppubus(quickerNES::Emu *e, uint8_t *dest)
{
	for (int i = 0; i < 0x3000; i++)
		dest[i] = e->peek_ppu(i);
}

QN_EXPORT void qn_set_tracecb(quickerNES::Emu *e, void (*cb)(unsigned int *dest))
{
	e->set_tracecb(cb);
}
