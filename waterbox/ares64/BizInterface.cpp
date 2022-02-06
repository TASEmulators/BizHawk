#include <n64/n64.hpp>

#include "../emulibc/emulibc.h"
#include "../emulibc/waterboxcore.h"

#define EXPORT extern "C" ECL_EXPORT

struct BizPlatform : ares::Platform {
	auto attach(ares::Node::Object) -> void override;
	auto detach(ares::Node::Object) -> void override;
	auto pak(ares::Node::Object) -> shared_pointer<vfs::directory> override;
	auto event(ares::Event) -> void override;
	auto log(string_view message) -> void override;
	auto video(ares::Node::Video::Screen, const u32* data, u32 pitch, u32 width, u32 height) -> void override;
	auto audio(ares::Node::Audio::Stream) -> void override;
	auto input(ares::Node::Input::Input) -> void override;

	shared_pointer<vfs::directory> bizpak = new vfs::directory;
};

auto BizPlatform::attach(ares::Node::Object) -> void { puts("called bizplatform attach"); }
auto BizPlatform::detach(ares::Node::Object) -> void { puts("called bizplatform detach"); }
auto BizPlatform::pak(ares::Node::Object) -> shared_pointer<vfs::directory> { return bizpak; }
auto BizPlatform::event(ares::Event) -> void { puts("called bizplatform event"); }
auto BizPlatform::log(string_view) -> void { puts("called bizplatform log"); }
auto BizPlatform::video(ares::Node::Video::Screen, const u32*, u32, u32, u32) -> void { puts("called bizplatform video"); };
auto BizPlatform::audio(ares::Node::Audio::Stream) -> void { puts("called bizplatform audio"); };
auto BizPlatform::input(ares::Node::Input::Input) -> void { puts("called bizplatform input"); };

static ares::Node::System root;
static BizPlatform platform;

EXPORT bool Init(bool pal)
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

	ares::platform = &platform;

	string region = pal ? "PAL" : "NTSC";
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
	// input
};

EXPORT void FrameAdvance(MyFrameInfo* f)
{
	// handle input
	// frame advance
	// handle a/v and lag (somehow)
}

EXPORT void SetInputCallback(void (*callback)())
{
	
}
