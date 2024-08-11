#include <n64/n64.hpp>

#include <emulibc.h>
#include <waterboxcore.h>

struct CallinFenvGuard {
	nall::float_env saved_fenv;
	nall::float_env& fenv;

	CallinFenvGuard(float_env& fenv_) : fenv(fenv_)
	{
		if (fenv.getRound() != saved_fenv.getRound())
		{
			fenv.setRound(fenv.getRound());
		}
	}

	~CallinFenvGuard()
	{
		if (fenv.getRound() != saved_fenv.getRound())
		{
			saved_fenv.setRound(saved_fenv.getRound());
		}
	}
};

struct CallbackFenvGuard {
	nall::float_env& saved_fenv;

	CallbackFenvGuard(float_env& saved_fenv_) : saved_fenv(saved_fenv_)
	{
		nall::float_env cur_fenv;
		if (cur_fenv.getRound() != nall::float_env::toNearest)
		{
			cur_fenv.setRound(nall::float_env::toNearest);
		}
	}

	~CallbackFenvGuard()
	{
		nall::float_env cur_fenv;
		if (cur_fenv.getRound() != saved_fenv.getRound())
		{
			saved_fenv.setRound(saved_fenv.getRound());
		}
	}
};

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

struct BizPlatform : ares::Platform
{
	auto attach(ares::Node::Object) -> void override;
	auto pak(ares::Node::Object) -> ares::VFS::Pak override;
	auto audio(ares::Node::Audio::Stream) -> void override;
	auto input(ares::Node::Input::Input) -> void override;
	auto time() -> n64 override;

	ares::VFS::Pak bizpak = nullptr;
	u16* soundbuf = alloc_invisible<u16>(1024 * 2);
	u32 nsamps = 0;
	bool hack = false;
	void (*inputcb)() = nullptr;
	bool lagged = true;
	u64 biztime = 0;
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

auto BizPlatform::audio(ares::Node::Audio::Stream stream) -> void
{
	while (stream->pending())
	{
		f64 buf[2];
		stream->read(buf);
		soundbuf[nsamps * 2 + 0] = (s16)std::clamp(buf[0] * 32768, -32768.0, 32767.0);
		soundbuf[nsamps * 2 + 1] = (s16)std::clamp(buf[1] * 32768, -32768.0, 32767.0);
		if (nsamps < 1023) nsamps++;
	}
}

auto BizPlatform::input(ares::Node::Input::Input node) -> void
{
	if (auto input = node->cast<ares::Node::Input::Button>())
	{
		if (input->name() == "Start" || input->name() == "Left Click")
		{
			lagged = false;
			if (inputcb)
			{
				CallbackFenvGuard guard(ares::Nintendo64::cpu.fenv);
				inputcb();
			}
		}
	}
}

auto BizPlatform::time() -> n64
{
	return biztime;
}

static ares::Node::System root = nullptr;
static BizPlatform* platform = nullptr;
static array_view<u8>* pifData = nullptr;
static array_view<u8>* iplData = nullptr;
static array_view<u8>* romData = nullptr;
static array_view<u8>* diskData = nullptr;
static array_view<u8>* diskErrorData = nullptr;
static array_view<u8>* saveData = nullptr;
static array_view<u8>* rtcData = nullptr;
static array_view<u8>* gbRomData[4] = { nullptr, nullptr, nullptr, nullptr, };

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
	if (id == "NPY") ret = EEPROM512;
	if (id == "NPT") ret = EEPROM512;
	if (id == "NRA") ret = EEPROM512;
	if (id == "NWQ") ret = EEPROM512;
	if (id == "NSU") ret = EEPROM512;
	if (id == "NSN") ret = EEPROM512;
	if (id == "NK2") ret = EEPROM512;
	if (id == "NSV") ret = EEPROM512;
	if (id == "NFX") ret = EEPROM512;
	if (id == "NS6") ret = EEPROM512;
	if (id == "NNA") ret = EEPROM512;
	if (id == "NRS") ret = EEPROM512;
	if (id == "NSW") ret = EEPROM512;
	if (id == "NSC") ret = EEPROM512;
	if (id == "NSA") ret = EEPROM512;
	if (id == "NB6") ret = EEPROM512;
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
	if (id == "NWC") ret = EEPROM512;
	if (id == "NAD") ret = EEPROM512;
	if (id == "NWU") ret = EEPROM512;
	if (id == "NYK") ret = EEPROM512;
	if (id == "NMZ") ret = EEPROM512;
	if (id == "NSM") ret = EEPROM512;
	if (id == "NWR") ret = EEPROM512;
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
	if (id == "NK4") ret = EEPROM2KB;
	if (id == "ND3" && region_code == 'J') ret = EEPROM2KB;
	if (id == "ND4" && region_code == 'J') ret = EEPROM2KB;

	if (id == "NTE") ret = SRAM32KB;
	if (id == "NVB") ret = SRAM32KB;
	if (id == "NB5") ret = SRAM32KB;
	if (id == "CFZ") ret = SRAM32KB;
	if (id == "NFZ") ret = SRAM32KB;
	if (id == "NSI") ret = SRAM32KB;
	if (id == "NG6") ret = SRAM32KB;
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
	if (id == "NPM") ret = SRAM32KB;
	if (id == "NRE") ret = SRAM32KB;
	if (id == "NAL") ret = SRAM32KB;
	if (id == "NT3") ret = SRAM32KB;
	if (id == "NS4") ret = SRAM32KB;
	if (id == "NA2") ret = SRAM32KB;
	if (id == "NVP") ret = SRAM32KB;
	if (id == "NWL") ret = SRAM32KB;
	if (id == "NW2") ret = SRAM32KB;
	if (id == "NWX") ret = SRAM32KB;
	if (id == "N3H" && region_code == 'J') ret = SRAM32KB;
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

	if (id[1] == 'E' && id[2] == 'D')
	{
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

static inline bool DetectRtc(u8* rom)
{
	string id;
	id.append((char)rom[0x3B]);
	id.append((char)rom[0x3C]);
	id.append((char)rom[0x3D]);

	u8 revision = rom[0x3f];

	if (id == "NAF") return true;

	if (id[1] == 'E' && id[2] == 'D')
	{
		n8 config = revision;
		return config.bit(0) == 1;
	}

	return false;
}

namespace ares::Nintendo64
{
	extern bool BobDeinterlace;
	extern bool FastVI;
}

typedef struct
{
	u8* GbRomData;
	u64 GbRomLen;
} GbRom;

typedef struct
{
	u8* PifData;
	u64 PifLen;
	u8* IplData;
	u64 IplLen;
	u8* RomData;
	u64 RomLen;
	u8* DiskData;
	u64 DiskLen;
	u8* DiskErrorData;
	u64 DiskErrorLen;
	GbRom GbRoms[4];
} LoadData;

static bool LoadRom(LoadData* loadData, bool isPal)
{
	u8* data;
	u32 len;
	string name;

	name = "program.rom";
	len = loadData->RomLen;
	data = new u8[len];
	memcpy(data, loadData->RomData, len);
	romData = new array_view<u8>(data, len);
	platform->bizpak->append(name, *romData);

	string cic = isPal ? "CIC-NUS-7101" : "CIC-NUS-6102";
	u32 crc32 = Hash::CRC32({&data[0x40], 0x9C0}).value();
	if (crc32 == 0x1DEB51A9) cic = "CIC-NUS-6101";
	if (crc32 == 0xEC8B1325) cic = "CIC-NUS-7102";
	if (crc32 == 0xC08E5BD6) cic = isPal ? "CIC-NUS-7101" : "CIC-NUS-6102";
	if (crc32 == 0x03B8376A) cic = isPal ? "CIC-NUS-7103" : "CIC-NUS-6103";
	if (crc32 == 0xCF7F41DC) cic = isPal ? "CIC-NUS-7105" : "CIC-NUS-6105";
	if (crc32 == 0xD1059C6A) cic = isPal ? "CIC-NUS-7106" : "CIC-NUS-6106";
	if (crc32 == 0x0C965795) cic = "CIC-NUS-8303";
	if (crc32 == 0x10C68B18) cic = "CIC-NUS-8401";
	if (crc32 == 0x8FEBA21E) cic = "CIC-NUS-DDUS";
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

	if (DetectRtc(data))
	{
		len = 32, name = "save.rtc";
		data = new u8[len];
		memset(data, 0xFF, len);
		rtcData = new array_view<u8>(data, len);
		platform->bizpak->append(name, *rtcData);
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

	return true;
}

static bool LoadDisk(LoadData* loadData)
{
	u8* data;
	u32 len;
	string name;

	name = "program.disk";
	len = loadData->DiskLen;
	data = new u8[len];
	memcpy(data, loadData->DiskData, len);
	diskData = new array_view<u8>(data, len);
	platform->bizpak->append(name, *diskData);

	name = "program.disk.error";
	len = loadData->DiskErrorLen;
	data = new u8[len];
	memcpy(data, loadData->DiskErrorData, len);
	diskErrorData = new array_view<u8>(data, len);
	platform->bizpak->append(name, *diskErrorData);

	if (auto port = root->find<ares::Node::Port>("Nintendo 64DD/Disk Drive"))
	{
		port->allocate();
		port->connect();
	}
	else
	{
		return false;
	}

	return true;
}

namespace angrylion
{
	extern u32 * OutFrameBuffer;
	extern u32 OutHeight;
}

ECL_EXPORT bool Init(LoadData* loadData, ControllerType* controllers, bool isPal, u64 initTime)
{
	CallinFenvGuard guard(ares::Nintendo64::cpu.fenv);

	platform = new BizPlatform;
	platform->bizpak = new vfs::directory;
	ares::platform = platform;

	platform->biztime = initTime;

	angrylion::OutFrameBuffer = NULL;
	angrylion::OutHeight = isPal ? 576 : 480;

	u8* data;
	u32 len;
	string name;

	name = isPal ? "pif.pal.rom" : "pif.ntsc.rom";
	len = loadData->PifLen;
	data = new u8[len];
	memcpy(data, loadData->PifData, len);
	pifData = new array_view<u8>(data, len);
	platform->bizpak->append(name, *pifData);

	// needs to be loaded before ares::Nintendo64::load
	if (loadData->IplData)
	{
		name = "64dd.ipl.rom";
		len = loadData->IplLen;
		data = new u8[len];
		memcpy(data, loadData->IplData, len);
		iplData = new array_view<u8>(data, len);
		platform->bizpak->append(name, *iplData);
	}

	string region = isPal ? "PAL" : "NTSC";
	platform->bizpak->setAttribute("region", region);

	name = {"[Nintendo] Nintendo 64 (", region, ")"};
	if (loadData->DiskData) name = "[Nintendo] Nintendo 64DD (NTSC-J)"; // todo: handle this better (name doesn't really matter at this point)

	if (!ares::Nintendo64::load(root, name))
	{
		return false;
	}

	if (loadData->RomData)
	{
		if (!LoadRom(loadData, isPal))
		{
			return false;
		}
	}

	if (loadData->DiskData)
	{
		if (!LoadDisk(loadData))
		{
			return false;
		}
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

	root->power(false);
	root->run(); // HACK, first frame dirties a ton of memory, so we emulate it then seal (this should be investigated, not sure why 60MBish of memory would be dirtied in a single frame?)
	return true;
}

// todo: might need to account for mbc5 rumble?
// largely pointless tho
ECL_EXPORT bool GetRumbleStatus(u32 num)
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
		m[i].Flags = MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_SWAPPED | MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE; \
		i++; \
\
		m[i].Data = c->transferPak.ram.data; \
		m[i].Name = "GB SRAM " #NUM; \
		m[i].Size = c->transferPak.ram.size; \
		m[i].Flags = MEMORYAREA_FLAGS_ONEFILLED | MEMORYAREA_FLAGS_SAVERAMMABLE | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_SWAPPED | MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE; \
		i++; \
	} \
} while (0)

static inline u8 GetByteFromWord(u32 word, u32 addr)
{
	switch (addr & 3)
	{
		case 0: return (word >> 24) & 0xFF;
		case 1: return (word >> 16) & 0xFF;
		case 2: return (word >>  8) & 0xFF;
		case 3: return (word >>  0) & 0xFF;
		default: __builtin_unreachable();
	}
}

static u8 PeekFunc(u64 address)
{
	address &= 0x1fff'ffff;
	const u32 addr = address;

	if (addr > 0x0403'ffff && addr <= 0x0407'ffff) // RSP
	{
		address = (address & 0x3ffff) >> 2;
		if (address == 7) // SP_SEMAPHORE
		{
			return GetByteFromWord(ares::Nintendo64::rsp.status.semaphore & 1, addr);
		}
	}
	else if (addr > 0x0407'ffff && addr <= 0x040f'ffff) // RSP Status
	{
		address = (address & 0x7ffff) >> 2;
		if (address == 0) // SP_PC_REG
		{
			return GetByteFromWord(ares::Nintendo64::rsp.ipu.pc & 0xFFF, addr);
		}
	}
	else if (addr > 0x046f'ffff && addr <= 0x047f'ffff) // RI
	{
		address = (address & 0xfffff) >> 2;
		if (address == 3) // RI_SELECT
		{
			return GetByteFromWord(ares::Nintendo64::ri.io.select, addr);
		}
	}

	ares::Nintendo64::Thread unused;
	return ares::Nintendo64::bus.read<ares::Nintendo64::Byte>(addr, unused, nullptr);
}

static void SysBusAccess(u8* buffer, u64 address, u64 count, bool write)
{
	if (write)
	{
		ares::Nintendo64::Thread unused;
		while (count--)
			ares::Nintendo64::bus.write<ares::Nintendo64::Byte>(address++, *buffer++, unused, nullptr);
	}
	else
	{
		while (count--)
			*buffer++ = PeekFunc(address++);
	}
}

ECL_EXPORT void GetMemoryAreas(MemoryArea *m)
{
	int i = 0;
	ADD_MEMORY_DOMAIN(rdram.ram, "RDRAM", MEMORYAREA_FLAGS_PRIMARY);
	ADD_MEMORY_DOMAIN(cartridge.rom, "ROM", 0);
	ADD_MEMORY_DOMAIN(pif.rom, "PIF ROM", 0);
	ADD_MEMORY_DOMAIN(pif.ram, "PIF RAM", 0);
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

	m[i].Data = (void*)SysBusAccess;
	m[i].Name = "System Bus";
	m[i].Size = 1ull << 32;
	m[i].Flags = MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_SWAPPED | MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_FUNCTIONHOOK;
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

	bool BobDeinterlace;
	bool FastVI;
	bool SkipDraw;
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

ECL_EXPORT void FrameAdvance(MyFrameInfo* f)
{
	CallinFenvGuard guard(ares::Nintendo64::cpu.fenv);

	ares::Nintendo64::BobDeinterlace = f->BobDeinterlace;
	ares::Nintendo64::FastVI = f->FastVI;

	angrylion::OutFrameBuffer = f->SkipDraw ? NULL : f->VideoBuffer;

	platform->biztime = f->Time;

	if (f->Power)
	{
		root->power(false);
	}
	else if (f->Reset)
	{
		root->power(true);
	}

	UPDATE_CONTROLLER(1);
	UPDATE_CONTROLLER(2);
	UPDATE_CONTROLLER(3);
	UPDATE_CONTROLLER(4);

	platform->lagged = true;
	platform->nsamps = 0;

	root->run();

	f->Width = 640;
	f->Height = angrylion::OutHeight;

	f->Samples = platform->nsamps;
	memcpy(f->SoundBuffer, platform->soundbuf, f->Samples * 4);

	f->Lagged = platform->lagged;
}

ECL_EXPORT void SetInputCallback(void (*callback)())
{
	platform->inputcb = callback;
}

ECL_EXPORT void PostLoadState()
{
	// fixme: make it so we can actually use this approach (there's various invalidation problems with the recompiler atm)
#if false
	ares::Nintendo64::cpu.recompiler.allocator.release(bump_allocator::zero_fill);
	ares::Nintendo64::cpu.recompiler.reset();
	ares::Nintendo64::rsp.recompiler.allocator.release(bump_allocator::zero_fill);
	ares::Nintendo64::rsp.recompiler.reset();
#endif
}

ECL_EXPORT void GetDisassembly(u32 address, u32 instruction, char* buf)
{
	auto s = ares::Nintendo64::cpu.disassembler.disassemble(address, instruction).strip();
	strcpy(buf, s.data());
}

ECL_EXPORT void GetRegisters(u64* buf)
{
	for (int i = 0; i < 32; i++)
	{
		buf[i] = ares::Nintendo64::cpu.ipu.r[i].u64;
	}

	buf[32] = ares::Nintendo64::cpu.ipu.lo.u64;
	buf[33] = ares::Nintendo64::cpu.ipu.hi.u64;
	buf[34] = ares::Nintendo64::cpu.ipu.pc;
}
