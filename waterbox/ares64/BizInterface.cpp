#include <n64/n64.hpp>

#include <emulibc.h>
#include <waterboxcore.h>

#define EXPORT extern "C" ECL_EXPORT

typedef enum
{
	Unplugged,
	Standard,
	Mempak,
	Rumblepak,
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
	auto video(ares::Node::Video::Screen, const u32*, u32, u32, u32) -> void override;
	auto input(ares::Node::Input::Input) -> void override;

	ares::VFS::Pak bizpak = new vfs::directory;
	ares::Node::Audio::Stream stream = nullptr;
	u32* videobuf = nullptr;
	u32 pitch = 0;
	u32 width = 0;
	u32 height = 0;
	bool newframe = false;
	void (*inputcb)() = nullptr;
	bool lagged = true;
};

auto BizPlatform::attach(ares::Node::Object node) -> void
{
	if (auto stream = node->cast<ares::Node::Audio::Stream>())
	{
		stream->setResamplerFrequency(44100);
		this->stream = stream;
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
	newframe = true;
}

auto BizPlatform::input(ares::Node::Input::Input node) -> void
{
	if (auto input = node->cast<ares::Node::Input::Button>())
	{
		if (input->name() == "Start")
		{
			lagged = false;
			if (inputcb) inputcb();
		}
	}
};

static ares::Node::System root;
static BizPlatform platform;

static inline void HackAroundCrash()
{
	f64 buf[2];
	u32 ns = 0;
	root->run();
	while (platform.stream->pending()) { platform.stream->read(buf); ns++; }
	printf("%d\n", ns);
	root->run();
	ns = 0;
	while (platform.stream->pending()) { platform.stream->read(buf); ns++; }
	printf("%d\n", ns);
	platform.newframe = false;
}

EXPORT bool Init(ControllerType* controllers, bool pal)
{
	FILE* f;
	array_view<u8>* data;
	u32 len;
	string name;

	name = pal ? "pif.pal.rom" : "pif.ntsc.rom";
	f = fopen(name, "rb");
	fseek(f, 0, SEEK_END);
	len = ftell(f);
	data = new array_view<u8>(new u8[len], len);
	fseek(f, 0, SEEK_SET);
	fread((void*)data->data(), 1, len, f);
	fclose(f);
	platform.bizpak->append(name, *data);

	name = "program.rom";
	f = fopen(name, "rb");
	fseek(f, 0, SEEK_END);
	len = ftell(f);
	data = new array_view<u8>(new u8[len], len);
	fseek(f, 0, SEEK_SET);
	fread((void*)data->data(), 1, len, f);
	fclose(f);
	platform.bizpak->append(name, *data);

	string region = pal ? "PAL" : "NTSC";
	platform.bizpak->setAttribute("region", region);

	string cic = pal ? "CIC-NUS-7101" : "CIC-NUS-6102";
	u32 crc32 = Hash::CRC32({&((u8*)data->data())[0x40], 0x9C0}).value();
	if (crc32 == 0x1DEB51A9) cic = pal ? "CIC-NUS-7102" : "CIC-NUS-6101";
	if (crc32 == 0xC08E5BD6) cic = pal ? "CIC-NUS-7101" : "CIC-NUS-6102";
	if (crc32 == 0x03B8376A) cic = pal ? "CIC-NUS-7103" : "CIC-NUS-6103";
	if (crc32 == 0xCF7F41DC) cic = pal ? "CIC-NUS-7105" : "CIC-NUS-6105";
	if (crc32 == 0xD1059C6A) cic = pal ? "CIC-NUS-7106" : "CIC-NUS-6106";
	platform.bizpak->setAttribute("cic", cic);

	ares::platform = &platform;

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

	for (int i = 0; i < 4; i++)
	{
		if (auto port = root->find<ares::Node::Port>({"Controller Port ", 1 + i}))
		{
			if (controllers[i] == Unplugged) continue;

			auto peripheral = port->allocate("Gamepad");
			port->connect();

			string name;
			switch (controllers[i])
			{
				case Mempak: name = "Controller Pak"; break;
				case Rumblepak: name = "Rumble Pak"; break;
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
	HackAroundCrash();
	return true;
}

u8 dummy[1];

EXPORT void GetMemoryAreas(MemoryArea *m)
{
	m[0].Data = dummy;
	m[0].Name = "Dummy";
	m[0].Size = 1;
	m[0].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_PRIMARY;
}

struct MyFrameInfo : public FrameInfo
{
	Buttons_t P1Buttons;
	s16 P1XAxis;
	s16 P1YAxis;

	Buttons_t P2Buttons;
	s16 P2XAxis;
	s16 P2YAxis;

	Buttons_t P3Buttons;
	s16 P3XAxis;
	s16 P3YAxis;

	Buttons_t P4Buttons;
	s16 P4XAxis;
	s16 P4YAxis;

	bool Reset;
	bool Power;
};

#define UPDATE_CONTROLLER(NUM) \
if (auto c = (ares::Nintendo64::Gamepad*)ares::Nintendo64::controllerPort##NUM.device.data()) \
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
}

EXPORT void FrameAdvance(MyFrameInfo* f)
{
	if (f->Power)
	{
		root->power(false);
		HackAroundCrash();
	}
	else if (f->Reset)
	{
		root->power(true);
		HackAroundCrash();
	}

	UPDATE_CONTROLLER(1)
	UPDATE_CONTROLLER(2)
	UPDATE_CONTROLLER(3)
	UPDATE_CONTROLLER(4)

	platform.lagged = true;

	root->run();

	if (platform.newframe)
	{
		f->Width = platform.width;
		f->Height = platform.height;
		u32 pitch = platform.pitch;
		u32* src = platform.videobuf;
		u32* dst = f->VideoBuffer;
		for (int i = 0; i < f->Height; i++)
		{
			memcpy(dst, src, f->Width * 4);
			dst += f->Width;
			src += pitch;
		}
		platform.newframe = false;
	}

	s16* soundbuf = f->SoundBuffer;
	while (platform.stream->pending())
	{
		f64 buf[2];
		platform.stream->read(buf);
		*soundbuf++ = (s16)std::clamp(buf[0] * 32768, -32768.0, 32767.0);
		*soundbuf++ = (s16)std::clamp(buf[1] * 32768, -32768.0, 32767.0);
		f->Samples++;
	}

	f->Lagged = platform.lagged;
}

EXPORT void SetInputCallback(void (*callback)())
{
	platform.inputcb = callback;
}
