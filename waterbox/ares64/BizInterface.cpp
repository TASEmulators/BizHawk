#include <n64/n64.hpp>

#include <emulibc.h>
#include <waterboxcore.h>

#include <vector>

#define EXPORT extern "C" ECL_EXPORT

typedef enum
{
	Unplugged,
	Standard,
	Mempak,
	Rumblepak,
	Transferpak,
	Mouse,
} ControllerType;

typedef enum
{
	UP      = 1 <<  0,
	DOWN    = 1 <<  1,
	LEFT    = 1 <<  2,
	RIGHT   = 1 <<  3,
	B       = 1 <<  4,
	A       = 1 <<  5,
	C_UP    = 1 <<  6,
	C_DOWN  = 1 <<  7,
	C_LEFT  = 1 <<  8,
	C_RIGHT = 1 <<  9,
	L       = 1 << 10,
	R       = 1 << 11,
	Z       = 1 << 12,
	START   = 1 << 13,
} Buttons_t;

static u64 biztime = 0;

static u64 GetBizTime()
{
	return biztime;
}

struct BizPlatform : ares::Platform
{
	auto attach(ares::Node::Object) -> void override;
	auto pak(ares::Node::Object) -> ares::VFS::Pak override;
	auto video(ares::Node::Video::Screen, const u32*, u32, u32, u32) -> void override;
	auto audio(ares::Node::Audio::Stream) -> void override;
	auto input(ares::Node::Input::Input) -> void override;

	ares::VFS::Pak bizpak = nullptr;
	u32* videobuf = nullptr;
	u32 pitch = 0;
	u32 width = 0;
	u32 height = 0;
	u16* soundbuf = alloc_invisible<u16>(1024 * 2);
	u32 nsamps = 0;
	bool hack = false;
	void (*inputcb)() = nullptr;
	bool lagged = true;
};

auto BizPlatform::attach(ares::Node::Object node) -> void
{
	if (auto stream = node->cast<ares::Node::Audio::Stream>())
	{
		stream->setResamplerFrequency(44100);
	}
}

auto BizPlatform::pak(ares::Node::Object) -> ares::VFS::Pak
{
	return bizpak;
}

auto BizPlatform::video(ares::Node::Video::Screen screen, const u32* data, u32 pitch, u32 width, u32 height) -> void
{
	videobuf = (u32*)data;
	this->pitch = pitch >> 2;
	this->width = width;
	this->height = height;
}

auto BizPlatform::audio(ares::Node::Audio::Stream stream) -> void
{
	while (stream->pending())
	{
		f64 buf[2];
		stream->read(buf);
		soundbuf[nsamps * 2 + 0] = (s16)std::clamp(buf[0] * 32768, -32768.0, 32767.0);
		soundbuf[nsamps * 2 + 1] = (s16)std::clamp(buf[1] * 32768, -32768.0, 32767.0);
		if (!hack) nsamps++;
	}
}

auto BizPlatform::input(ares::Node::Input::Input node) -> void
{
	if (auto input = node->cast<ares::Node::Input::Button>())
	{
		if (input->name() == "Start" || input->name() == "Left Click")
		{
			lagged = false;
			if (inputcb) inputcb();
		}
	}
};

static ares::Node::System root = nullptr;
static BizPlatform* platform = nullptr;
static array_view<u8>* pifData = nullptr;
static array_view<u8>* romData = nullptr;
static array_view<u8>* saveData = nullptr;
static array_view<u8>* gbRomData[4] = { nullptr, nullptr, nullptr, nullptr, };

static inline void HackeryDoo()
{
	platform->hack = true;
	root->run();
	root->run();
	platform->hack = false;
}

typedef enum
{
	NONE,
	EEPROM512,
	EEPROM2KB,
	SRAM32KB,
	SRAM96KB,
	SRAM128KB,
	FLASH128KB,
} SaveType;

static inline SaveType DetectSaveType(u8* rom)
{
	string id;
	id.append((char)rom[0x3B]);
	id.append((char)rom[0x3C]);
	id.append((char)rom[0x3D]);

	char region_code = rom[0x3E];
	u8 revision = rom[0x3F];

	SaveType ret = NONE;
	if (id == "NTW") ret = EEPROM512;
	if (id == "NHF") ret = EEPROM512;
	if (id == "NOS") ret = EEPROM512;
	if (id == "NTC") ret = EEPROM512;
	if (id == "NER") ret = EEPROM512;
	if (id == "NAG") ret = EEPROM512;
	if (id == "NAB") ret = EEPROM512;
	if (id == "NS3") ret = EEPROM512;
	if (id == "NTN") ret = EEPROM512;
	if (id == "NBN") ret = EEPROM512;
	if (id == "NBK") ret = EEPROM512;
	if (id == "NFH") ret = EEPROM512;
	if (id == "NMU") ret = EEPROM512;
	if (id == "NBC") ret = EEPROM512;
	if (id == "NBH") ret = EEPROM512;
	if (id == "NHA") ret = EEPROM512;
	if (id == "NBM") ret = EEPROM512;
	if (id == "NBV") ret = EEPROM512;
	if (id == "NBD") ret = EEPROM512;
	if (id == "NCT") ret = EEPROM512;
	if (id == "NCH") ret = EEPROM512;
	if (id == "NCG") ret = EEPROM512;
	if (id == "NP2") ret = EEPROM512;
	if (id == "NXO") ret = EEPROM512;
	if (id == "NCU") ret = EEPROM512;
	if (id == "NCX") ret = EEPROM512;
	if (id == "NDY") ret = EEPROM512;
	if (id == "NDQ") ret = EEPROM512;
	if (id == "NDR") ret = EEPROM512;
	if (id == "NN6") ret = EEPROM512;
	if (id == "NDU") ret = EEPROM512;
	if (id == "NJM") ret = EEPROM512;
	if (id == "NFW") ret = EEPROM512;
	if (id == "NF2") ret = EEPROM512;
	if (id == "NKA") ret = EEPROM512;
	if (id == "NFG") ret = EEPROM512;
	if (id == "NGL") ret = EEPROM512;
	if (id == "NGV") ret = EEPROM512;
	if (id == "NGE") ret = EEPROM512;
	if (id == "NHP") ret = EEPROM512;
	if (id == "NPG") ret = EEPROM512;
	if (id == "NIJ") ret = EEPROM512;
	if (id == "NIC") ret = EEPROM512;
	if (id == "NFY") ret = EEPROM512;
	if (id == "NKI") ret = EEPROM512;
	if (id == "NLL") ret = EEPROM512;
	if (id == "NLR") ret = EEPROM512;
	if (id == "NKT") ret = EEPROM512;
	if (id == "CLB") ret = EEPROM512;
	if (id == "NLB") ret = EEPROM512;
	if (id == "NMW") ret = EEPROM512;
	if (id == "NML") ret = EEPROM512;
	if (id == "NTM") ret = EEPROM512;
	if (id == "NMI") ret = EEPROM512;
	if (id == "NMG") ret = EEPROM512;
	if (id == "NMO") ret = EEPROM512;
	if (id == "NMS") ret = EEPROM512;
	if (id == "NMR") ret = EEPROM512;
	if (id == "NCR") ret = EEPROM512;
	if (id == "NEA") ret = EEPROM512;
	if (id == "NPW") ret = EEPROM512;
	if (id == "NPM") ret = EEPROM512;
	if (id == "NPY") ret = EEPROM512;
	if (id == "NPT") ret = EEPROM512;
	if (id == "NRA") ret = EEPROM512;
	if (id == "NWQ") ret = EEPROM512;
	if (id == "NSU") ret = EEPROM512;
	if (id == "NSN") ret = EEPROM512;
	if (id == "NK2") ret = EEPROM512;
	if (id == "NSV") ret = EEPROM512;
	if (id == "NFX") ret = EEPROM512;
	if (id == "NFP") ret = EEPROM512;
	if (id == "NS6") ret = EEPROM512;
	if (id == "NNA") ret = EEPROM512;
	if (id == "NRS") ret = EEPROM512;
	if (id == "NSW") ret = EEPROM512;
	if (id == "NSC") ret = EEPROM512;
	if (id == "NSA") ret = EEPROM512;
	if (id == "NB6") ret = EEPROM512;
	if (id == "NSM") ret = EEPROM512;
	if (id == "NSS") ret = EEPROM512;
	if (id == "NTX") ret = EEPROM512;
	if (id == "NT6") ret = EEPROM512;
	if (id == "NTP") ret = EEPROM512;
	if (id == "NTJ") ret = EEPROM512;
	if (id == "NRC") ret = EEPROM512;
	if (id == "NTR") ret = EEPROM512;
	if (id == "NTB") ret = EEPROM512;
	if (id == "NGU") ret = EEPROM512;
	if (id == "NIR") ret = EEPROM512;
	if (id == "NVL") ret = EEPROM512;
	if (id == "NVY") ret = EEPROM512;
	if (id == "NWR") ret = EEPROM512;
	if (id == "NWC") ret = EEPROM512;
	if (id == "NAD") ret = EEPROM512;
	if (id == "NWU") ret = EEPROM512;
	if (id == "NYK") ret = EEPROM512;
	if (id == "NMZ") ret = EEPROM512;
	if (id == "NDK" && region_code == 'J') ret = EEPROM512;
	if (id == "NWT" && region_code == 'J') ret = EEPROM512;

	if (id == "NB7") ret = EEPROM2KB;
	if (id == "NGT") ret = EEPROM2KB;
	if (id == "NFU") ret = EEPROM2KB;
	if (id == "NCW") ret = EEPROM2KB;
	if (id == "NCZ") ret = EEPROM2KB;
	if (id == "ND6") ret = EEPROM2KB;
	if (id == "NDO") ret = EEPROM2KB;
	if (id == "ND2") ret = EEPROM2KB;
	if (id == "N3D") ret = EEPROM2KB;
	if (id == "NMX") ret = EEPROM2KB;
	if (id == "NGC") ret = EEPROM2KB;
	if (id == "NIM") ret = EEPROM2KB;
	if (id == "NK4") ret = EEPROM2KB;
	if (id == "NNB") ret = EEPROM2KB;
	if (id == "NMV") ret = EEPROM2KB;
	if (id == "NM8") ret = EEPROM2KB;
	if (id == "NEV") ret = EEPROM2KB;
	if (id == "NPP") ret = EEPROM2KB;
	if (id == "NUB") ret = EEPROM2KB;
	if (id == "NPD") ret = EEPROM2KB;
	if (id == "NRZ") ret = EEPROM2KB;
	if (id == "NR7") ret = EEPROM2KB;
	if (id == "NEP") ret = EEPROM2KB;
	if (id == "NYS") ret = EEPROM2KB;
	if (id == "ND3" && region_code == 'J') ret = EEPROM2KB;
	if (id == "ND4" && region_code == 'J') ret = EEPROM2KB;

	if (id == "NTE") ret = SRAM32KB;
	if (id == "NVB") ret = SRAM32KB;
	if (id == "CFZ") ret = SRAM32KB;
	if (id == "NFZ") ret = SRAM32KB;
	if (id == "NSI") ret = SRAM32KB;
	if (id == "NG6") ret = SRAM32KB;
	if (id == "N3H") ret = SRAM32KB;
	if (id == "NGP") ret = SRAM32KB;
	if (id == "NYW") ret = SRAM32KB;
	if (id == "NHY") ret = SRAM32KB;
	if (id == "NIB") ret = SRAM32KB;
	if (id == "NPS") ret = SRAM32KB;
	if (id == "NPA") ret = SRAM32KB;
	if (id == "NP4") ret = SRAM32KB;
	if (id == "NJ5") ret = SRAM32KB;
	if (id == "NP6") ret = SRAM32KB;
	if (id == "NPE") ret = SRAM32KB;
	if (id == "NJG") ret = SRAM32KB;
	if (id == "CZL") ret = SRAM32KB;
	if (id == "NZL") ret = SRAM32KB;
	if (id == "NKG") ret = SRAM32KB;
	if (id == "NMF") ret = SRAM32KB;
	if (id == "NRI") ret = SRAM32KB;
	if (id == "NUT") ret = SRAM32KB;
	if (id == "NUM") ret = SRAM32KB;
	if (id == "NOB") ret = SRAM32KB;
	if (id == "CPS") ret = SRAM32KB;
	if (id == "NB5") ret = SRAM32KB;
	if (id == "NRE") ret = SRAM32KB;
	if (id == "NAL") ret = SRAM32KB;
	if (id == "NT3") ret = SRAM32KB;
	if (id == "NS4") ret = SRAM32KB;
	if (id == "NA2") ret = SRAM32KB;
	if (id == "NVP") ret = SRAM32KB;
	if (id == "NWL") ret = SRAM32KB;
	if (id == "NW2") ret = SRAM32KB;
	if (id == "NWX") ret = SRAM32KB;
	if (id == "NK4" && region_code == 'J' && revision < 2) ret = SRAM32KB;

	if (id == "CDZ") ret = SRAM96KB;

	if (id == "NCC") ret = FLASH128KB;
	if (id == "NDA") ret = FLASH128KB;
	if (id == "NAF") ret = FLASH128KB;
	if (id == "NJF") ret = FLASH128KB;
	if (id == "NKJ") ret = FLASH128KB;
	if (id == "NZS") ret = FLASH128KB;
	if (id == "NM6") ret = FLASH128KB;
	if (id == "NCK") ret = FLASH128KB;
	if (id == "NMQ") ret = FLASH128KB;
	if (id == "NPN") ret = FLASH128KB;
	if (id == "NPF") ret = FLASH128KB;
	if (id == "NPO") ret = FLASH128KB;
	if (id == "CP2") ret = FLASH128KB;
	if (id == "NP3") ret = FLASH128KB;
	if (id == "NRH") ret = FLASH128KB;
	if (id == "NSQ") ret = FLASH128KB;
	if (id == "NT9") ret = FLASH128KB;
	if (id == "NW4") ret = FLASH128KB;
	if (id == "NDP") ret = FLASH128KB;

	if(id[1] == 'E' && id[2] == 'D') {
		n8 config = revision;
		if (config.bit(4,7) == 1) ret = EEPROM512;
		else if (config.bit(4,7) == 2) ret = EEPROM2KB;
		else if (config.bit(4,7) == 3) ret = SRAM32KB;
		else if (config.bit(4,7) == 4) ret = SRAM96KB;
		else if (config.bit(4,7) == 5) ret = FLASH128KB;
		else if (config.bit(4,7) == 6) ret = SRAM128KB;
	}

	return ret;
}

namespace ares::Nintendo64 { extern bool RestrictAnalogRange; extern bool BobDeinterlace; }

typedef struct
{
	u8* GbRomData;
	u32 GbRomLen;
} GbRom;

typedef struct
{
	u8* PifData;
	u32 PifLen;
	u8* RomData;
	u32 RomLen;
	GbRom GbRoms[4];
} LoadData;

typedef enum
{
	RESTRICT_ANALOG_RANGE = 1 << 0,
	IS_PAL = 1 << 1,
	BOB_DEINTERLACE = 1 << 2, // weave otherwise (todo: implement this)
} LoadFlags;

#define SET_RTC_CALLBACK(NUM) do { \
	if (auto pad = dynamic_cast<ares::Nintendo64::Gamepad*>(ares::Nintendo64::controllerPort##NUM.device.data())) \
	{ \
		if (auto mbc3 = dynamic_cast<ares::Nintendo64::Mbc3*>(pad->transferPak.mbc.data())) \
		{ \
			mbc3->rtcCallback = GetBizTime; \
		} \
	} \
} while (0)

EXPORT bool Init(LoadData* loadData, ControllerType* controllers, LoadFlags loadFlags)
{
	platform = new BizPlatform;
	platform->bizpak = new vfs::directory;

	u8* data;
	u32 len;
	string name;

	bool pal = loadFlags & IS_PAL;

	name = pal ? "pif.pal.rom" : "pif.ntsc.rom";
	len = loadData->PifLen;
	data = new u8[len];
	memcpy(data, loadData->PifData, len);
	pifData = new array_view<u8>(data, len);
	platform->bizpak->append(name, *pifData);

	name = "program.rom";
	len = loadData->RomLen;
	data = new u8[len];
	memcpy(data, loadData->RomData, len);
	romData = new array_view<u8>(data, len);
	platform->bizpak->append(name, *romData);

	string region = pal ? "PAL" : "NTSC";
	platform->bizpak->setAttribute("region", region);

	string cic = pal ? "CIC-NUS-7101" : "CIC-NUS-6102";
	u32 crc32 = Hash::CRC32({&data[0x40], 0x9C0}).value();
	if (crc32 == 0x1DEB51A9) cic = pal ? "CIC-NUS-7102" : "CIC-NUS-6101";
	if (crc32 == 0xC08E5BD6) cic = pal ? "CIC-NUS-7101" : "CIC-NUS-6102";
	if (crc32 == 0x03B8376A) cic = pal ? "CIC-NUS-7103" : "CIC-NUS-6103";
	if (crc32 == 0xCF7F41DC) cic = pal ? "CIC-NUS-7105" : "CIC-NUS-6105";
	if (crc32 == 0xD1059C6A) cic = pal ? "CIC-NUS-7106" : "CIC-NUS-6106";
	platform->bizpak->setAttribute("cic", cic);

	SaveType save = DetectSaveType(data);
	if (save != NONE)
	{
		switch (save)
		{
			case EEPROM512: len = 512; name = "save.eeprom"; break;
			case EEPROM2KB: len = 2 * 1024; name = "save.eeprom"; break;
			case SRAM32KB: len = 32 * 1024; name = "save.ram"; break;
			case SRAM96KB: len = 96 * 1024; name = "save.ram"; break;
			case SRAM128KB: len = 128 * 1024; name = "save.ram"; break;
			case FLASH128KB: len = 128 * 1024; name = "save.flash"; break;
			default: return false;
		}
		data = new u8[len];
		memset(data, 0xFF, len);
		saveData = new array_view<u8>(data, len);
		platform->bizpak->append(name, *saveData);
	}

	for (int i = 0; i < 4; i++)
	{
		if (loadData->GbRoms[i].GbRomData)
		{
			len = loadData->GbRoms[i].GbRomLen;
			data = new u8[len];
			memcpy(data, loadData->GbRoms[i].GbRomData, len);
			gbRomData[i] = new array_view<u8>(data, len);
		}
	}

	ares::platform = platform;

	if (!ares::Nintendo64::load(root, {"[Nintendo] Nintendo 64 (", region, ")"}))
	{
		return false;
	}

	if (auto port = root->find<ares::Node::Port>("Cartridge Slot"))
	{
		port->allocate();
		port->connect();
	}
	else
	{
		return false;
	}

	for (int i = 0, j = 0; i < 4; i++)
	{
		if (auto port = root->find<ares::Node::Port>({"Controller Port ", 1 + i}))
		{
			if (controllers[i] == Unplugged) continue;

			if (controllers[i] == Mouse)
			{
				port->allocate("Mouse");
				port->connect();
				continue;
			}

			if (controllers[i] == Transferpak)
			{
				if (gbRomData[j])
				{
					platform->bizpak->remove("gbrom.pak");
					platform->bizpak->append("gbrom.pak", *gbRomData[j++]);
				}
			}

			auto peripheral = port->allocate("Gamepad");
			port->connect();

			switch (controllers[i])
			{
				case Mempak: name = "Controller Pak"; break;
				case Rumblepak: name = "Rumble Pak"; break;
				case Transferpak: name = "Transfer Pak"; break;
				default: continue;
			}

			if (auto port = peripheral->find<ares::Node::Port>("Pak"))
			{
				port->allocate(name);
				port->connect();
			}
			else
			{
				return false;
			}
		}
		else
		{
			return false;
		}
	}

	SET_RTC_CALLBACK(1);
	SET_RTC_CALLBACK(2);
	SET_RTC_CALLBACK(3);
	SET_RTC_CALLBACK(4);

	ares::Nintendo64::RestrictAnalogRange = loadFlags & RESTRICT_ANALOG_RANGE;
	ares::Nintendo64::BobDeinterlace = loadFlags & BOB_DEINTERLACE;

	root->power(false);
	HackeryDoo();
	return true;
}

// todo: might need to account for mbc5 rumble?
// largely pointless tho
EXPORT bool GetRumbleStatus(u32 num)
{
	ares::Nintendo64::Gamepad* c = nullptr;
	switch (num)
	{
		case 0: c = dynamic_cast<ares::Nintendo64::Gamepad*>(ares::Nintendo64::controllerPort1.device.data()); break;
		case 1: c = dynamic_cast<ares::Nintendo64::Gamepad*>(ares::Nintendo64::controllerPort2.device.data()); break;
		case 2: c = dynamic_cast<ares::Nintendo64::Gamepad*>(ares::Nintendo64::controllerPort3.device.data()); break;
		case 3: c = dynamic_cast<ares::Nintendo64::Gamepad*>(ares::Nintendo64::controllerPort4.device.data()); break;
	}
	return c ? c->motor->enable() : false;
}

#define ADD_MEMORY_DOMAIN(mem, name, flags) do { \
	m[i].Data = ares::Nintendo64::mem.data; \
	m[i].Name = name; \
	m[i].Size = ares::Nintendo64::mem.size; \
	m[i].Flags = flags | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_SWAPPED | MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE; \
	i++; \
} while (0)

#define ADD_MEMPAK_DOMAIN(NUM) do { \
	if (auto c = dynamic_cast<ares::Nintendo64::Gamepad*>(ares::Nintendo64::controllerPort##NUM.device.data())) \
	{ \
		m[i].Data = c->ram.data; \
		m[i].Name = "MEMPAK " #NUM; \
		m[i].Size = c->ram.size; \
		m[i].Flags = MEMORYAREA_FLAGS_ONEFILLED | MEMORYAREA_FLAGS_SAVERAMMABLE | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_SWAPPED | MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE; \
		i++; \
	} \
} while (0)

#define ADD_GB_DOMAINS(NUM) do { \
	if (auto c = dynamic_cast<ares::Nintendo64::Gamepad*>(ares::Nintendo64::controllerPort##NUM.device.data())) \
	{ \
		m[i].Data = c->transferPak.rom.data; \
		m[i].Name = "GB ROM " #NUM; \
		m[i].Size = c->transferPak.rom.size; \
		m[i].Flags = MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_SWAPPED | MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE; \
		i++; \
\
		m[i].Data = c->transferPak.ram.data; \
		m[i].Name = "GB SRAM " #NUM; \
		m[i].Size = c->transferPak.ram.size; \
		m[i].Flags = MEMORYAREA_FLAGS_ONEFILLED | MEMORYAREA_FLAGS_SAVERAMMABLE | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_SWAPPED | MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE; \
		i++; \
	} \
} while (0)

EXPORT void GetMemoryAreas(MemoryArea *m)
{
	int i = 0;
	ADD_MEMORY_DOMAIN(rdram.ram, "RDRAM", MEMORYAREA_FLAGS_PRIMARY);
	ADD_MEMORY_DOMAIN(cartridge.rom, "ROM", 0);
	ADD_MEMORY_DOMAIN(pi.rom, "PI ROM", 0);
	ADD_MEMORY_DOMAIN(pi.ram, "PI RAM", 0);
	ADD_MEMORY_DOMAIN(rsp.dmem, "RSP DMEM", 0);
	ADD_MEMORY_DOMAIN(rsp.imem, "RSP IMEM", 0);
	ADD_MEMORY_DOMAIN(cartridge.ram, "SRAM", MEMORYAREA_FLAGS_ONEFILLED | MEMORYAREA_FLAGS_SAVERAMMABLE);
	ADD_MEMORY_DOMAIN(cartridge.eeprom, "EEPROM", MEMORYAREA_FLAGS_ONEFILLED | MEMORYAREA_FLAGS_SAVERAMMABLE);
	ADD_MEMORY_DOMAIN(cartridge.flash, "FLASH", MEMORYAREA_FLAGS_ONEFILLED | MEMORYAREA_FLAGS_SAVERAMMABLE);
	ADD_MEMPAK_DOMAIN(1);
	ADD_MEMPAK_DOMAIN(2);
	ADD_MEMPAK_DOMAIN(3);
	ADD_MEMPAK_DOMAIN(4);
	ADD_GB_DOMAINS(1);
	ADD_GB_DOMAINS(2);
	ADD_GB_DOMAINS(3);
	ADD_GB_DOMAINS(4);
}

struct MyFrameInfo : public FrameInfo
{
	u64 Time;

	Buttons_t P1Buttons;
	Buttons_t P2Buttons;
	Buttons_t P3Buttons;
	Buttons_t P4Buttons;

	s16 P1XAxis;
	s16 P1YAxis;

	s16 P2XAxis;
	s16 P2YAxis;

	s16 P3XAxis;
	s16 P3YAxis;

	s16 P4XAxis;
	s16 P4YAxis;

	bool Reset;
	bool Power;
};

#define UPDATE_CONTROLLER(NUM) do { \
	if (auto c = dynamic_cast<ares::Nintendo64::Gamepad*>(ares::Nintendo64::controllerPort##NUM.device.data())) \
	{ \
		c->x->setValue(f->P##NUM##XAxis); \
		c->y->setValue(f->P##NUM##YAxis); \
		c->up->setValue(f->P##NUM##Buttons & UP); \
		c->down->setValue(f->P##NUM##Buttons & DOWN); \
		c->left->setValue(f->P##NUM##Buttons & LEFT); \
		c->right->setValue(f->P##NUM##Buttons & RIGHT); \
		c->b->setValue(f->P##NUM##Buttons & B); \
		c->a->setValue(f->P##NUM##Buttons & A); \
		c->cameraUp->setValue(f->P##NUM##Buttons & C_UP); \
		c->cameraDown->setValue(f->P##NUM##Buttons & C_DOWN); \
		c->cameraLeft->setValue(f->P##NUM##Buttons & C_LEFT); \
		c->cameraRight->setValue(f->P##NUM##Buttons & C_RIGHT); \
		c->l->setValue(f->P##NUM##Buttons & L); \
		c->r->setValue(f->P##NUM##Buttons & R); \
		c->z->setValue(f->P##NUM##Buttons & Z); \
		c->start->setValue(f->P##NUM##Buttons & START); \
	} \
	else if (auto m = dynamic_cast<ares::Nintendo64::Mouse*>(ares::Nintendo64::controllerPort##NUM.device.data())) \
	{ \
		m->x->setValue(f->P##NUM##XAxis); \
		m->y->setValue(f->P##NUM##YAxis); \
		m->rclick->setValue(f->P##NUM##Buttons & B); \
		m->lclick->setValue(f->P##NUM##Buttons & A); \
	} \
} while (0)

EXPORT void FrameAdvance(MyFrameInfo* f)
{
	if (f->Power)
	{
		root->power(false);
		HackeryDoo();
	}
	else if (f->Reset)
	{
		root->power(true);
		HackeryDoo();
	}

	UPDATE_CONTROLLER(1);
	UPDATE_CONTROLLER(2);
	UPDATE_CONTROLLER(3);
	UPDATE_CONTROLLER(4);

	platform->lagged = true;
	platform->nsamps = 0;
	biztime = f->Time;

	root->run();

	f->Width = platform->width;
	f->Height = platform->height;
	u32* src = platform->videobuf;
	u32* dst = f->VideoBuffer;
	for (int i = 0; i < f->Height; i++)
	{
		memcpy(dst, src, f->Width * 4);
		dst += f->Width;
		src += platform->pitch;
	}

	f->Samples = platform->nsamps;
	memcpy(f->SoundBuffer, platform->soundbuf, f->Samples * 4);

	f->Lagged = platform->lagged;
}

EXPORT void SetInputCallback(void (*callback)())
{
	platform->inputcb = callback;
}
